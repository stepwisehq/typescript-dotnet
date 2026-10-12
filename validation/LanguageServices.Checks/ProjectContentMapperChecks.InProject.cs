// Whole original TestContentMapperInProject, five sibling groups. Outside compilation.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectContentMapperChecks
{
    public static void TestContentMapperInProject()
    {
        const string config = "{\n\t\t\t\"compilerOptions\": { \"target\": \"es2020\", \"module\": \"esnext\", \"moduleResolution\": \"bundler\", \"strict\": true },\n\t\t\t\"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [\".box\"] } ]\n\t\t}";
        const string main = "import { version } from \"./app.box\";\nexport const twice: number = version * 2;\n";
        const string box = "export const version = #{target};\n";
        Input[] files = [new("/home/project/tsconfig.json", config), new("/home/project/node_modules/mapper/package.json", ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.TransformingMapper)), new("/home/project/app.box", box), new("/home/project/main.ts", main)];
        List<Exception>? failures = null;
        foreach (string group in new[]
        {
            "trusted workspace transforms the content-mapped file",
            "untrusted workspace does not run the content mapper",
            "editing an open content-mapped file reparses it through the mapper",
            "watch change to a content-mapped file updates the program",
            "unchanged content-mapped file is reused from the cache across a full rebuild"
        }

        )
        {
            try
            {
                bool trusted = !group.StartsWith("untrusted", StringComparison.Ordinal);
                var options = Options(trusted) with { LoggingEnabled = true };
                using var fixture = new ProjectSessionFixture(files, options, spawner: ContentMapperProjectFixture.NewSpawner());
                var session = fixture.Session;
                var context = ProtocolRequestContext.Default;
                var mainURI = Uri("file:///home/project/main.ts");
                session.DidOpenFile(context, mainURI, 1, ByteStringValue.FromString(main), LanguageKind.TypeScript);
                if (group.StartsWith("editing", StringComparison.Ordinal))
                    session.DidOpenFile(context, Uri("file:///home/project/app.box"), 1, ByteStringValue.FromString(box), new LanguageKind(ByteStringValue.FromString("box")));
                var(service, error) = session.GetLanguageService(context, mainURI);
                Require(error is null, error?.Message ?? "");
                Require(service is not null);
                var original = service.GetProgram().GetSourceFile(RootedFilePath.FromGoString("/home/project/app.box"));
                if (!trusted)
                {
                    Require(original is null, "app.box should not be loaded without trust");
                    session.WaitForBackgroundTasks();
                    foreach (var call in fixture.Client.RegisterContentMapperExtensionsCalls())
                        Require(call.Length == 0);
                    continue;
                }

                Require(original is not null, "expected app.box to be loaded");
                if (group.StartsWith("trusted", StringComparison.Ordinal))
                {
                    Require(ByteStringValue.FromView(original.TextView()).Contains("export const version = 7;"u8));
                    session.WaitForBackgroundTasks();
                    var calls = fixture.Client.RegisterContentMapperExtensionsCalls();
                    Require(calls.Length > 0);
                    Require(calls[^1].Length == 1 && calls[^1][0].EqualsString(".box"));
                    continue;
                }

                if (group.StartsWith("editing", StringComparison.Ordinal))
                {
                    session.DidChangeFile(context, Uri("file:///home/project/app.box"), 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new() { Text = ByteStringValue.FromString("export const version = #{target};\nexport const extra = 1;\n") } }]));
                }
                else if (group.StartsWith("watch", StringComparison.Ordinal))
                {
                    session.WaitForBackgroundTasks();
                    Require(fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/home/project/app.box"), "export const version = #{target};\nexport const watched = true;\n")is null);
                    session.DidChangeWatchedFiles(context, Slice<FileEvent>.FromArray([new() { Uri = Uri("file:///home/project/app.box"), Type = FileChangeType.Changed }]));
                }
                else
                {
                    Require(ByteStringValue.FromView(original.TextView()).Contains("export const version = 7;"u8));
                    Require(fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/home/project/tsconfig.json"), "{\n\t\t\t\"compilerOptions\": { \"target\": \"es2020\", \"module\": \"esnext\", \"moduleResolution\": \"bundler\", \"strict\": false },\n\t\t\t\"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [\".box\"] } ]\n\t\t}")is null);
                    session.DidChangeWatchedFiles(context, Slice<FileEvent>.FromArray([new() { Uri = Uri("file:///home/project/tsconfig.json"), Type = FileChangeType.Changed }]));
                }

                (service, error) = session.GetLanguageService(context, mainURI);
                Require(error is null, error?.Message ?? "");
                Require(service is not null);
                var updated = service.GetProgram().GetSourceFile(RootedFilePath.FromGoString("/home/project/app.box"));
                if (group.StartsWith("unchanged", StringComparison.Ordinal))
                {
                    Require(ReferenceEquals(updated, original));
                    continue;
                }

                Require(updated is not null);
                Require(ByteStringValue.FromView(updated.TextView()).Contains("export const version = 7;"u8));
                if (group.StartsWith("editing", StringComparison.Ordinal))
                    Require(ByteStringValue.FromView(updated.TextView()).Contains("export const extra = 1;"u8));
                else
                {
                    var snapshot = session.Snapshot();
                    var project = snapshot.GetDefaultProject(mainURI);
                    Require(project is not null);
                    Require(project.ProgramUpdateKind == ProgramUpdateKind.Cloned);
                    Require(project.ProgramLastUpdate == snapshot.ID());
                    Require(!ReferenceEquals(updated, original));
                    Require(ByteStringValue.FromView(updated.TextView()).Contains("export const watched = true;"u8));
                }
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(new InvalidOperationException(group, error));
            }
        }

        if (failures is not null)
            throw new AggregateException(failures);
    }
}

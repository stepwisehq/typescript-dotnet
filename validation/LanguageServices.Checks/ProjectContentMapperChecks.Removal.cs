// Whole original mapper removal/old-snapshot process ownership assertion.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectContentMapperChecks
{
    public static void TestContentMapperRemovalWithOpenFile()
    {
        const string box = "export const version = #{target};\n";
        Input[] files = [new("/home/project/tsconfig.json", "{\n\t\t\t\"compilerOptions\": { \"target\": \"es2020\", \"module\": \"esnext\", \"moduleResolution\": \"bundler\" },\n\t\t\t\"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [\".box\"] } ]\n\t\t}"), new("/home/project/node_modules/mapper/package.json", ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.TransformingMapper)), new("/home/project/app.box", box)];
        var spawner = new RecordingSpawner(ContentMapperProjectFixture.NewSpawner());
        using var fixture = new ProjectSessionFixture(files, Options(), spawner: spawner);
        var session = fixture.Session;
        var ctx = ProtocolRequestContext.Default;
        var uri = Uri("file:///home/project/app.box");
        session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString(box), new LanguageKind(ByteStringValue.FromString("box")));
        var(service, error) = session.GetLanguageService(ctx, uri);
        Require(error is null, error?.Message ?? "");
        Require(service is not null);
        var file = service.GetProgram().GetSourceFile(File("/home/project/app.box"));
        Require(file is not null);
        Require(file.ContentMapper() != "");
        Require(Volatile.Read(ref spawner.Spawns) == 1);
        Require(Volatile.Read(ref spawner.Closes) == 0);
        for (int version = 2; version <= 4; version++)
        {
            session.DidChangeFile(ctx, uri, version, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new() { Text = ByteStringValue.FromString("export const version = " + version + ";\n") } }]));
            (_, error) = session.GetLanguageService(ctx, uri);
            Require(error is null, error?.Message ?? "");
            Require(Volatile.Read(ref spawner.Spawns) == 1);
            Require(Volatile.Read(ref spawner.Closes) == 0);
        }

        var(releaseOld, errorOld) = session.WithLanguageServiceAndSnapshot(ctx, uri, static (_, _) => (() => null, null));
        Require(errorOld is null, errorOld?.Message ?? "");
        Require(releaseOld is not null);
        Require(fixture.MutableFS.WriteFile(File("/home/project/tsconfig.json"), "{\n\t\t\"compilerOptions\": { \"target\": \"es2020\", \"module\": \"esnext\", \"moduleResolution\": \"bundler\" }\n\t}")is null);
        Watched(session, "/home/project/tsconfig.json");
        (_, error) = session.GetLanguageService(ctx, uri);
        Require(error is not null && error.Message.Contains("no project found", StringComparison.Ordinal));
        Require(session.Snapshot().GetFile(File("/home/project/app.box"))is not null);
        Require(session.Snapshot().GetDefaultProject(uri)is null);
        session.WaitForBackgroundTasks();
        Require(Volatile.Read(ref spawner.Closes) == 0);
        Require(releaseOld()is null);
        Require(Volatile.Read(ref spawner.Closes) == 1);
        var calls = fixture.Client.RegisterContentMapperExtensionsCalls();
        Require(calls.Length > 0);
        Require(calls[^1].Length == 0);
        session.DidCloseFile(ctx, uri);
        (_, error) = session.GetLanguageService(ctx, uri);
        Require(error is not null && error.Message.Contains("no project found", StringComparison.Ordinal));
    }
}

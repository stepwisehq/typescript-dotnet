// Whole original cross-project process lifetime assertion.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectContentMapperChecks
{
    public static void TestContentMapperProcessSharedAcrossProjects()
    {
        static string Config(string extension) => "{\n\t\t\t\"compilerOptions\": { \"target\": \"es2020\", \"module\": \"esnext\", \"moduleResolution\": \"bundler\" },\n\t\t\t\"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [" + "\"" + extension + "\"" + "] } ]\n\t\t}";
        var manifest = ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.TransformingMapper);
        Input[] files = [new("/home/a/tsconfig.json", Config(".box")), new("/home/a/node_modules/mapper/package.json", manifest), new("/home/a/app.box", "export const a = 1;\n"), new("/home/b/tsconfig.json", Config(".panel")), new("/home/b/node_modules/mapper/package.json", manifest), new("/home/b/app.panel", "export const b = 1;\n")];
        var options = Options()with
        {
            CurrentDirectory = RootedDirectoryPath.FromGoString("/home")
        };
        var spawner = new RecordingSpawner(ContentMapperProjectFixture.NewSpawner());
        using var fixture = new ProjectSessionFixture(files, options, spawner: spawner);
        var session = fixture.Session;
        var ctx = ProtocolRequestContext.Default;
        var a = Uri("file:///home/a/app.box");
        var b = Uri("file:///home/b/app.panel");
        session.DidOpenFile(ctx, a, 1, ByteStringValue.FromString("export const a = 1;\n"), new LanguageKind(ByteStringValue.FromString("box")));
        var(_, error) = session.GetLanguageService(ctx, a);
        Require(error is null, error?.Message ?? "");
        session.DidOpenFile(ctx, b, 1, ByteStringValue.FromString("export const b = 1;\n"), new LanguageKind(ByteStringValue.FromString("panel")));
        (_, error) = session.GetLanguageService(ctx, b);
        Require(error is null, error?.Message ?? "");
        Require(Volatile.Read(ref spawner.Spawns) == 1);
        Require(fixture.MutableFS.WriteFile(File("/home/a/tsconfig.json"), "{}")is null);
        Watched(session, "/home/a/tsconfig.json");
        (_, error) = session.GetLanguageService(ctx, a);
        Require(error is not null && error.Message.Contains("no project found", StringComparison.Ordinal));
        Require(Volatile.Read(ref spawner.Closes) == 0);
        Require(fixture.MutableFS.WriteFile(File("/home/b/tsconfig.json"), "{}")is null);
        Watched(session, "/home/b/tsconfig.json");
        (_, error) = session.GetLanguageService(ctx, b);
        Require(error is not null && error.Message.Contains("no project found", StringComparison.Ordinal));
        Require(Volatile.Read(ref spawner.Closes) == 1);
    }
}

using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectContentMapperChecks
{
    public static void TestContentMapperPackageManifestChangeReloadsConfig()
    {
        const string package = "/home/mapper/package.json";
        const string main = "import { version } from \"./app.box\";\n";
        Input[] files = [new("/home/project/tsconfig.json", "{\n\t\t\t\"compilerOptions\": { \"target\": \"es2020\", \"module\": \"esnext\", \"moduleResolution\": \"bundler\" },\n\t\t\t\"contentMappers\": [{ \"package\": \"mapper\", \"extensions\": [\".box\"] }]\n\t\t}"), new("/home/project/node_modules/mapper", Symlink: "/home/mapper"), new(package, "{\n\t\t\t\"name\": \"mapper\",\n\t\t\t\"version\": \"1.0.0\",\n\t\t\t\"typescript\": { \"contentMapper\": { \"exec\": [\"compiler-test-mapper\"] } }\n\t\t}"), new("/home/project/app.box", "export const version = #{target};\n"), new("/home/project/main.ts", main)];
        ResolvedClientCapabilities capabilities = default;
        capabilities.Workspace.DidChangeWatchedFiles.RelativePatternSupport = true;
        var ctx = ProtocolContexts.WithClientCapabilities(ProtocolRequestContext.Default, capabilities);
        var options = Options() with { WatchEnabled = true };
        using var fixture = new ProjectSessionFixture(files, options, spawner: ContentMapperProjectFixture.NewSpawner(), backgroundContext: ctx);
        var session = fixture.Session;
        var uri = Uri("file:///home/project/main.ts");
        session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString(main), LanguageKind.TypeScript);
        var(_, error) = session.GetLanguageService(ctx, uri);
        Require(error is null, error?.Message ?? "");
        var project = session.Snapshot().GetDefaultProject(uri);
        Require(project is not null);
        var mappers = project.CommandLine!.ContentMappers();
        Require(mappers is not null && mappers.Count == 1);
        Require(mappers[0].PackageDirectory.AsValue().EqualsString("/home/mapper"));
        session.WaitForBackgroundTasks();
        Require(fixture.WatchesFile(ByteStringValue.FromString(package)));
        bool external = false;
        foreach (var call in fixture.Client.WatchFilesCalls())
            foreach (var watcher in call.Watchers)
            {
                var relative = watcher.GlobPattern.RelativePattern;
                if (relative is not null && relative.BaseUri.URI is { } baseUri && baseUri.Value.EqualsString("file:///home/mapper") && relative.Pattern.EqualsString("**/*"))
                    external = true;
            }

        Require(external);
        var fixedManifest = ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.TransformingMapper).Replace("\"version\": \"1.0.0\"", "\"version\": \"2.0.0\"", StringComparison.Ordinal);
        Require(fixedManifest.Contains("\"version\": \"2.0.0\"", StringComparison.Ordinal));
        Require(fixture.MutableFS.WriteFile(File(package), fixedManifest)is null);
        session.DidChangeWatchedFiles(ctx, Slice<FileEvent>.FromArray([new() { Uri = Uri("file://" + package), Type = FileChangeType.Changed }]));
        var(service, serviceError) = session.GetLanguageService(ctx, uri);
        Require(serviceError is null, serviceError?.Message ?? "");
        Require(service is not null);
        var box = service.GetProgram().GetSourceFile(File("/home/project/app.box"));
        Require(box is not null);
        Require(ByteStringValue.FromView(box.TextView()).Contains("export const version = 7;"u8));
    }
}

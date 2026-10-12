using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectContentMapperChecks
{
    public static void TestContentMapperOpenFileExcludedByConfigChange()
    {
        const string box = "export const version = #{target};\n";
        const string main = "export const main = true;\n";
        const string config = "{\n\t\t\t\"compilerOptions\": { \"target\": \"es2020\", \"module\": \"esnext\", \"moduleResolution\": \"bundler\", \"strict\": true },\n\t\t\t\"include\": [\"src\"],\n\t\t\t\"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [\".box\"] } ]\n\t\t}";
        Input[] files = [new("/home/project/tsconfig.json", config), new("/home/project/node_modules/mapper/package.json", ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.TransformingMapper)), new("/home/project/src/app.box", box), new("/home/project/src/main.ts", main)];
        using var fixture = new ProjectSessionFixture(files, Options(), spawner: ContentMapperProjectFixture.NewSpawner());
        var session = fixture.Session;
        var ctx = ProtocolRequestContext.Default;
        var uri = Uri("file:///home/project/src/app.box");
        session.SetContentMapperContributions(ctx, Contributions("/home/project"), default);
        session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString(box), new LanguageKind(ByteStringValue.FromString("box")));
        var(service, error) = session.GetLanguageService(ctx, uri);
        Require(error is null, error?.Message ?? "");
        Require(service is not null);
        Require(service.GetProgram().GetSourceFile(File("/home/project/src/app.box"))is not null);
        Require(fixture.MutableFS.WriteFile(File("/home/project/tsconfig.json"), "{\n\t\t\"compilerOptions\": { \"target\": \"es2020\", \"module\": \"esnext\", \"moduleResolution\": \"bundler\", \"strict\": true },\n\t\t\"include\": [\"src/**/*.ts\"],\n\t\t\"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [\".box\"] } ]\n\t}")is null);
        Watched(session, "/home/project/tsconfig.json");
        session.WaitForBackgroundTasks();
        Require(session.Snapshot().GetDefaultProject(uri)is null);
        session.DidOpenFile(ctx, Uri("file:///home/project/src/main.ts"), 1, ByteStringValue.FromString(main), LanguageKind.TypeScript);
        (service, error) = session.GetLanguageService(ctx, uri);
        Require(error is null, error?.Message ?? "");
        Require(service is not null);
        var project = session.Snapshot().GetDefaultProject(uri);
        Require(project is not null);
        Require(project.Kind == ProjectKind.Inferred);
        var file = service.GetProgram().GetSourceFile(File("/home/project/src/app.box"));
        Require(file is not null);
        Require(file.ContentMapper() != "");
        Require(!ByteStringValue.FromView(file.TextView()).Contains("#{target}"u8));
    }
}

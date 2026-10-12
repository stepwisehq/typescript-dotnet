using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectContentMapperChecks
{
    public static void TestContentMapperCreatedFileAdoptedByConfiguredProject()
    {
        const string main = "export const main = true;\n";
        const string box = "export const version = #{target};\n";
        Input[] files = [new("/home/project/tsconfig.json", "{\n\t\t\t\"compilerOptions\": { \"target\": \"es2020\", \"module\": \"esnext\", \"moduleResolution\": \"bundler\" },\n\t\t\t\"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [\".box\"] } ]\n\t\t}"), new("/home/project/node_modules/mapper/package.json", ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.TransformingMapper)), new("/home/project/main.ts", main)];
        using var fixture = new ProjectSessionFixture(files, Options(), spawner: ContentMapperProjectFixture.NewSpawner());
        var session = fixture.Session;
        var ctx = ProtocolRequestContext.Default;
        var uri = Uri("file:///home/project/main.ts");
        session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString(main), LanguageKind.TypeScript);
        var(_, error) = session.GetLanguageService(ctx, uri);
        Require(error is null, error?.Message ?? "");
        Require(fixture.MutableFS.WriteFile(File("/home/project/new.box"), box)is null);
        session.DidChangeWatchedFiles(ctx, Slice<FileEvent>.FromArray([new() { Uri = Uri("file:///home/project/new.box"), Type = FileChangeType.Created }]));
        var boxURI = Uri("file:///home/project/new.box");
        session.DidOpenFile(ctx, boxURI, 1, ByteStringValue.FromString(box), new LanguageKind(ByteStringValue.FromString("box")));
        var(service, serviceError) = session.GetLanguageService(ctx, boxURI);
        Require(serviceError is null, serviceError?.Message ?? "");
        Require(service is not null);
        var project = session.Snapshot().GetDefaultProject(boxURI);
        Require(project is not null);
        Require(project.Kind == ProjectKind.Configured);
        var mapped = service.GetProgram().GetSourceFile(File("/home/project/new.box"));
        Require(mapped is not null);
        Require(!ByteStringValue.FromView(mapped.TextView()).Contains("#{target}"u8));
    }
}

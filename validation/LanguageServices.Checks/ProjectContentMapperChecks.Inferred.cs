using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using TypeScript.ContentMapper;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectContentMapperChecks
{
    private static ContentMapperContributions Contributions(string directory) => new()
    {
        Mappers = Slice<Mapper>.FromArray([new() { Package = "test.extension", Extensions = [".box"], Name = "mapper", Version = "1.0.0", Exec = [ContentMapperProjectFixture.TransformingMapper], CompilerOptions = ["target", "jsx"], PackageDirectory = RootedDirectoryPath.FromGoString(directory), ContributionID = "test.extension[0]" }]),
        Extensions = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString(".box")])
    };
    public static void TestContentMapperInferredProjectUsesExtensionContributions() => Inferred(false);
    public static void TestContentMapperInferredProjectSurvivesTypingsInstall() => Inferred(true);
    private static void Inferred(bool typings)
    {
        const string main = "export const main = true;\n";
        const string box = "export const version = #{target};\n";
        var files = new List<Input>
        {
            new("/home/configured/tsconfig.json", "{\n\t\t\t\"compilerOptions\": { \"target\": \"es2020\", \"module\": \"esnext\", \"moduleResolution\": \"bundler\" },\n\t\t\t\"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [\".box\"] } ]\n\t\t}"),
            new("/home/configured/node_modules/mapper/package.json", ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.TransformingMapper)),
            new("/home/configured/main.ts", main),
            new("/home/loose/app.box", box)
        };
        if (typings)
            files.Add(new("/home/package.json", "{\"name\":\"loose\",\"dependencies\":{\"jquery\":\"^3.1.0\"}}"));
        var options = Options()with
        {
            CurrentDirectory = RootedDirectoryPath.FromGoString("/home"),
            LoggingEnabled = typings
        };
        using var fixture = new ProjectSessionFixture(files, options, packages: typings ? new() { ["jquery"] = "declare const $: { x: number }" } : null, spawner: ContentMapperProjectFixture.NewSpawner());
        var session = fixture.Session;
        var ctx = ProtocolRequestContext.Default;
        var mainURI = Uri("file:///home/configured/main.ts");
        var boxURI = Uri("file:///home/loose/app.box");
        if (typings)
            session.SetContentMapperContributions(ctx, Contributions("/home"), default);
        session.DidOpenFile(ctx, mainURI, 1, ByteStringValue.FromString(main), LanguageKind.TypeScript);
        var(_, error) = session.GetLanguageService(ctx, mainURI);
        Require(error is null, error?.Message ?? "");
        session.DidOpenFile(ctx, boxURI, 1, ByteStringValue.FromString(box), new LanguageKind(ByteStringValue.FromString("box")));
        (_, error) = session.GetLanguageService(ctx, boxURI);
        if (!typings)
        {
            Require(error is not null && error.Message.Contains("no project found", StringComparison.Ordinal));
            session.SetContentMapperContributions(ctx, Contributions("/home"), Slice<DocumentUri>.FromArray([boxURI]));
        }
        else
        {
            Require(error is null, error?.Message ?? "");
            var initial = session.Snapshot().GetDefaultProject(boxURI);
            Require(initial is not null);
            Require(initial.Kind == ProjectKind.Inferred);
            session.WaitForBackgroundTasks();
            Require(fixture.Npm.NpmInstallCalls().Length > 0);
        }

        var(service, serviceError) = session.GetLanguageService(ctx, boxURI);
        Require(serviceError is null, serviceError?.Message ?? "");
        Require(service is not null);
        var project = session.Snapshot().GetDefaultProject(boxURI);
        Require(project is not null);
        if (!typings)
            Require(project.Kind == ProjectKind.Inferred);
        var file = service.GetProgram().GetSourceFile(File("/home/loose/app.box"));
        Require(file is not null);
        Require(file.ContentMapper() != "");
        Require(!ByteStringValue.FromView(file.TextView()).Contains("#{target}"u8));
        if (typings)
        {
            SourceFile? found = null;
            foreach (var candidate in service.GetProgram().SourceFiles())
                if (candidate.FileNameGoValue().EndsWith("@types/jquery/index.d.ts"u8))
                {
                    found = candidate;
                    break;
                }

            Require(found is not null);
        }
    }
}

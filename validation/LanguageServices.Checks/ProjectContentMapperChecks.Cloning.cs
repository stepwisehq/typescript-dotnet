// Whole original supplemental/module clone assertions, outside compilation.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectContentMapperChecks
{
    private static RootedFilePath File(string path) => RootedFilePath.FromGoString(path);
    private static void Watched(Session session, string path) => session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Uri = Uri("file://" + path), Type = FileChangeType.Changed }]));
    public static void TestContentMapperSupplementalFileClonedOnEdit()
    {
        const string main = "const value: number = supplementalValue;\n";
        Input[] files = [new("/home/project/tsconfig.json", "{ \"compilerOptions\": { \"strict\": true }, \"contentMappers\": [{ \"package\": \"mapper\", \"extensions\": [\".box\"] }] }"), new("/home/project/node_modules/mapper/package.json", ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.SupplementalMapper)), new("/home/project/app.box", "declare const supplementalValue: number;\n"), new("/home/project/extra.d.ts", "interface Extra {}\n"), new("/home/project/main.ts", main)];
        using var fixture = new ProjectSessionFixture(files, Options(), spawner: ContentMapperProjectFixture.NewSpawner());
        var session = fixture.Session;
        var context = ProtocolRequestContext.Default;
        var uri = Uri("file:///home/project/main.ts");
        session.DidOpenFile(context, uri, 1, ByteStringValue.FromString(main), LanguageKind.TypeScript);
        var(service, error) = session.GetLanguageService(context, uri);
        Require(error is null, error?.Message ?? "");
        Require(service is not null);
        var oldProgram = service.GetProgram();
        var oldCanonical = oldProgram.GetSourceFile(File("/home/project/app.box"));
        Require(oldCanonical is not null);
        var oldSupplemental = oldCanonical.SupplementalSourceFiles();
        Require(oldSupplemental.Length == 1);
        Require(oldSupplemental[0].FileNameGoValue().EqualsString("/home/project/app.box.0.ts"));
        var oldPath = PathKey.FromValue(oldSupplemental[0].PathKeyValue());
        Require(oldPath.AsValue().EqualsString("/home/project/app.box.0.ts"));
        Require(oldSupplemental[0].Hash == oldCanonical.Hash);
        Require(ReferenceEquals(oldProgram.GetSourceFileByPath(oldPath), oldSupplemental[0]));
        Require(ReferenceEquals(oldProgram.FilesByPath()[ByteStringKey.FromValue(oldPath.AsValue())], oldSupplemental[0]));
        Require(fixture.MutableFS.WriteFile(File("/home/project/app.box"), "declare const supplementalValue: string;\n")is null);
        Watched(session, "/home/project/app.box");
        (service, error) = session.GetLanguageService(context, uri);
        Require(error is null, error?.Message ?? "");
        Require(service is not null);
        var configured = session.Snapshot().GetDefaultProject(uri);
        Require(configured is not null);
        Require(configured.ProgramUpdateKind == ProgramUpdateKind.Cloned);
        var newProgram = service.GetProgram();
        var newCanonical = newProgram.GetSourceFile(File("/home/project/app.box"));
        Require(newCanonical is not null);
        var newSupplemental = newCanonical.SupplementalSourceFiles();
        Require(newSupplemental.Length == 1);
        var newPath = PathKey.FromValue(newSupplemental[0].PathKeyValue());
        Require(newPath.AsValue().EqualsValue(oldPath.AsValue()));
        Require(!ReferenceEquals(newCanonical, oldCanonical));
        Require(!ReferenceEquals(newSupplemental[0], oldSupplemental[0]));
        Require(newSupplemental[0].Hash == newCanonical.Hash);
        Require(newSupplemental[0].Hash != oldSupplemental[0].Hash);
        Require(ReferenceEquals(newProgram.FilesByPath()[ByteStringKey.FromValue(newPath.AsValue())], newSupplemental[0]));
        Require(ByteStringValue.FromView(newSupplemental[0].TextView()).Contains("supplementalValue: string"u8));
        var mainFile = newProgram.GetSourceFile(File("/home/project/main.ts"));
        Require(mainFile is not null);
        bool found = false;
        foreach (var diagnostic in newProgram.GetSemanticDiagnostics(context, mainFile))
            if (diagnostic.Code() == 2322)
                found = true;
        Require(found);
        Require(fixture.MutableFS.WriteFile(File("/home/project/app.box"), "/// <reference path=\"./extra.d.ts\" />\ndeclare const supplementalValue: string;\n")is null);
        Watched(session, "/home/project/app.box");
        (_, error) = session.GetLanguageService(context, uri);
        Require(error is null, error?.Message ?? "");
        configured = session.Snapshot().GetDefaultProject(uri);
        Require(configured is not null);
        Require(configured.ProgramUpdateKind == ProgramUpdateKind.SameFileNames);
    }

    public static void TestContentMapperModuleExtensionClonedOnUnrelatedEdit()
    {
        const string main = "import { value } from \"./app.box\"; value;";
        Input[] files = [new("/home/project/tsconfig.json", "{ \"contentMappers\": [{ \"package\": \"mapper\", \"extensions\": [\".box\"] }] }"), new("/home/project/node_modules/mapper/package.json", ContentMapperProjectFixture.PackageJSON(ContentMapperProjectFixture.ModuleVerbatimMapper)), new("/home/project/app.box", "export const value = 1;\n"), new("/home/project/main.ts", main)];
        using var fixture = new ProjectSessionFixture(files, Options(), spawner: ContentMapperProjectFixture.NewSpawner());
        var session = fixture.Session;
        var ctx = ProtocolRequestContext.Default;
        var uri = Uri("file:///home/project/main.ts");
        session.DidOpenFile(ctx, uri, 1, ByteStringValue.FromString(main), LanguageKind.TypeScript);
        var(service, error) = session.GetLanguageService(ctx, uri);
        Require(error is null, error?.Message ?? "");
        Require(service is not null);
        var mapped = service.GetProgram().GetSourceFile(File("/home/project/app.box"));
        Require(mapped is not null);
        Require(mapped.VirtualFileName().AsValue().EqualsString("/home/project/app.box.mts"));
        Require(mapped.ParseOptions().ExternalModuleIndicatorOptions.Force);
        session.DidChangeFile(ctx, uri, 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new() { Text = ByteStringValue.FromString("import { value } from \"./app.box\"; value + 1;") } }]));
        (service, error) = session.GetLanguageService(ctx, uri);
        Require(error is null, error?.Message ?? "");
        Require(service is not null);
        var project = session.Snapshot().GetDefaultProject(uri);
        Require(project is not null);
        Require(project.ProgramUpdateKind == ProgramUpdateKind.Cloned);
        Require(ReferenceEquals(service.GetProgram().GetSourceFile(File("/home/project/app.box")), mapped));
    }
}

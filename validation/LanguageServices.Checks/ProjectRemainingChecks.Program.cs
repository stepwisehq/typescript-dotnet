// Whole original internal/project/project_test.go:41, all seven sibling groups.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Range = TypeScript.Lsp.Protocol.Range;
using TypeScript.TsOptions;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectRemainingChecks
{
    private static DocumentUri Uri(string path) => new(ByteStringValue.FromString("file://" + path));
    private static void Open(ProjectSessionFixture fixture, string path, string text) => fixture.Session.DidOpenFile(ProtocolRequestContext.Default, Uri(path), 1, ByteStringValue.FromString(text), LanguageKind.TypeScript);
    private static TypeScript.LanguageServices.LanguageService Service(ProjectSessionFixture fixture, string path)
    {
        var(service, error) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default, Uri(path));
        Check(error is null);
        return service!;
    }

    private static Project Configured(ProjectSessionFixture fixture)
    {
        var project = fixture.Session.Snapshot().ProjectCollection.ConfiguredProject(PathKey.FromGoString("/src/tsconfig.json"));
        Check(project is not null);
        return project!;
    }

    private static void Change(ProjectSessionFixture fixture, string path, string text, Range range) => fixture.Session.DidChangeFile(ProtocolRequestContext.Default, Uri(path), 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { Partial = new() { Text = ByteStringValue.FromString(text), Range = range } }]));
    private static void WatchChange(ProjectSessionFixture fixture, string path, FileChangeType type) => fixture.Session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Uri = Uri(path), Type = type }]));
    internal static void TestProjectProgramUpdateKind()
    {
        Groups(("NewFiles on initial build", () =>
        {
            using var fixture = new ProjectSessionFixture(InitialFiles);
            Open(fixture, "/src/index.ts", InitialFiles[1].Contents);
            _ = Service(fixture, "/src/index.ts");
            Check(Configured(fixture).ProgramUpdateKind == ProgramUpdateKind.NewFiles);
        }), ("Cloned on single-file change", () =>
        {
            using var fixture = new ProjectSessionFixture(SingleChangeFiles);
            Open(fixture, "/src/index.ts", SingleChangeFiles[1].Contents);
            _ = Service(fixture, "/src/index.ts");
            Change(fixture, "/src/index.ts", "\n", new Range { Start = new Position { Line = 0, Character = 20 }, End = new Position { Line = 0, Character = 20 } });
            _ = Service(fixture, "/src/index.ts");
            Check(Configured(fixture).ProgramUpdateKind == ProgramUpdateKind.Cloned);
        }), ("compiler options update inferred project", () =>
        {
            using var fixture = new ProjectSessionFixture([new Input("/src/index.ts", "export const x = 1;")]);
            Open(fixture, "/src/index.ts", "export const x = 1;");
            var oldProject = fixture.Session.Snapshot().ProjectCollection.InferredProject();
            Check(oldProject is not null);
            var oldProgram = oldProject!.Program!;
            Check(oldProgram.Options().Strict == Tristate.Unknown);
            fixture.Session.DidChangeCompilerOptionsForInferredProjects(ProtocolRequestContext.Default, new CompilerOptions { NoLib = Tristate.True, Strict = Tristate.True });
            _ = Service(fixture, "/src/index.ts");
            var updated = fixture.Session.Snapshot().ProjectCollection.InferredProject();
            Check(updated is not null);
            Check(updated!.CommandLine!.CompilerOptions().Strict == Tristate.True);
            Check(!ReferenceEquals(updated.Program, oldProgram));
            Check(updated.Program!.Options().Strict == Tristate.True);
            Check(oldProgram.Options().Strict == Tristate.Unknown);
        }), ("NewFiles when import resolution mode changes", () =>
        {
            using var fixture = new ProjectSessionFixture(ResolutionFiles);
            Open(fixture, "/src/index.ts", ResolutionFiles[1].Contents);
            var program = Service(fixture, "/src/index.ts").GetProgram();
            var request = ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString("0"));
            Check(program.GetSemanticDiagnostics(request, program.GetSourceFile(RootedFilePath.FromGoString("/src/index.ts"))).Length == 0);
            fixture.Session.DidChangeFile(ProtocolRequestContext.Default, Uri("/src/index.ts"), 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new() { Text = ByteStringValue.FromString("import type { Value } from \"pkg\" with { \"resolution-mode\": \"import\" };\nconst value: Value = { mode: \"require\" };") } }]));
            program = Service(fixture, "/src/index.ts").GetProgram();
            var diagnostics = program.GetSemanticDiagnostics(request, program.GetSourceFile(RootedFilePath.FromGoString("/src/index.ts")));
            Check(diagnostics.Length == 1);
            Check(diagnostics[0].Code() == Diagnostics.Type_0_is_not_assignable_to_type_1.Code());
            Check(Configured(fixture).ProgramUpdateKind == ProgramUpdateKind.NewFiles);
        }), ("SameFileNames on config change without root changes", () =>
        {
            using var fixture = new ProjectSessionFixture(ConfigChangeFiles);
            Open(fixture, "/src/index.ts", ConfigChangeFiles[1].Contents);
            _ = Service(fixture, "/src/index.ts");
            Check(fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/src/tsconfig.json"), "{\"compilerOptions\": {\"strict\": false}}")is null);
            WatchChange(fixture, "/src/tsconfig.json", FileChangeType.Changed);
            _ = Service(fixture, "/src/index.ts");
            Check(Configured(fixture).ProgramUpdateKind == ProgramUpdateKind.SameFileNames);
        }), ("NewFiles on root addition", () =>
        {
            using var fixture = new ProjectSessionFixture(RootAdditionFiles);
            Open(fixture, "/src/index.ts", RootAdditionFiles[1].Contents);
            _ = Service(fixture, "/src/index.ts");
            const string content = "export const y = 2;";
            Check(fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/src/newfile.ts"), content)is null);
            WatchChange(fixture, "/src/newfile.ts", FileChangeType.Created);
            Open(fixture, "/src/newfile.ts", content);
            _ = Service(fixture, "/src/newfile.ts");
            Check(Configured(fixture).ProgramUpdateKind == ProgramUpdateKind.NewFiles);
        }), ("SameFileNames when adding an unresolvable import with multi-file change", () =>
        {
            using var fixture = new ProjectSessionFixture(MissingImportFiles);
            Open(fixture, "/src/index.ts", MissingImportFiles[1].Contents);
            _ = Service(fixture, "/src/index.ts");
            Change(fixture, "/src/index.ts", "\nimport \"./does-not-exist\";\n", new Range { Start = new Position(), End = new Position() });
            _ = Service(fixture, "/src/index.ts");
            Check(Configured(fixture).ProgramUpdateKind == ProgramUpdateKind.SameFileNames);
        }));
    }
}

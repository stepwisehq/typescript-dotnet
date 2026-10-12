// Original internal/project/project_test.go:230, complete two sibling groups.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectRemainingChecks
{
    internal static void TestProject()
    {
        Groups(("commandLineWithTypingsFiles is reset on CommandLine change", () =>
        {
            using var fixture = new ProjectSessionFixture([new Input("/user/username/projects/project1/app.js", ""), new Input("/user/username/projects/project1/package.json", "{\"name\":\"p1\",\"dependencies\":{\"jquery\":\"^3.1.0\"}}"), new Input("/user/username/projects/project2/app.js", "")], packages: new() { ["jquery"] = "declare const $: { x: number }" });
            var session = fixture.Session;
            var first = new DocumentUri(ByteStringValue.FromString("file:///user/username/projects/project1/app.js"));
            var second = new DocumentUri(ByteStringValue.FromString("file:///user/username/projects/project2/app.js"));
            session.DidOpenFile(ProtocolRequestContext.Default, first, 1, default, LanguageKind.JavaScript);
            session.WaitForBackgroundTasks();
            Check(fixture.Npm.NpmInstallCalls().Length > 0);
            var(_, error) = session.GetLanguageService(ProtocolRequestContext.Default, first);
            Check(error is null);
            session.DidOpenFile(ProtocolRequestContext.Default, second, 1, default, LanguageKind.JavaScript);
            (_, error) = session.GetLanguageService(ProtocolRequestContext.Default, second);
            Check(error is null);
        }), ("inferred project rebuilt twice in one snapshot after typings install does not crash", () =>
        {
            using var fixture = new ProjectSessionFixture([new Input("/user/username/projects/project1/a.js", ""), new Input("/user/username/projects/project1/package.json", "{\"name\":\"p1\",\"dependencies\":{\"jquery\":\"^3.1.0\"}}"), new Input("/user/username/projects/project1/b.ts", "export const y = 1;")], packages: new() { ["jquery"] = "declare const $: { x: number }" });
            var session = fixture.Session;
            var first = new DocumentUri(ByteStringValue.FromString("file:///user/username/projects/project1/a.js"));
            var second = new DocumentUri(ByteStringValue.FromString("file:///user/username/projects/project1/b.ts"));
            session.DidOpenFile(ProtocolRequestContext.Default, first, 1, default, LanguageKind.JavaScript);
            session.DidOpenFile(ProtocolRequestContext.Default, second, 1, ByteStringValue.FromString("export const y = 1;"), LanguageKind.TypeScript);
            session.WaitForBackgroundTasks();
            Check(fixture.Npm.NpmInstallCalls().Length > 0);
            var(_, error) = session.GetLanguageService(ProtocolRequestContext.Default, first);
            Check(error is null);
            session.DidChangeFile(ProtocolRequestContext.Default, first, 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new() { Text = ByteStringValue.FromString("// changed") } }]));
            Check(fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/user/username/projects/project1/tsconfig.json"), "{\"compilerOptions\":{},\"files\":[\"b.ts\"]}")is null);
            session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Created, Uri = new DocumentUri(ByteStringValue.FromString("file:///user/username/projects/project1/tsconfig.json")) }]));
            (_, error) = session.GetLanguageService(ProtocolRequestContext.Default, first);
            Check(error is null);
        }));
    }
}

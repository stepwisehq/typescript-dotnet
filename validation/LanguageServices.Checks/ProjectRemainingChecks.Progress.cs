// Original internal/project/project_test.go:860, complete three sibling groups.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectRemainingChecks
{
    internal static void TestProgressNotifications()
    {
        static void Run(bool configured, bool balance)
        {
            Input[] files = balance ? [new("/home/projects/tsconfig.json", "{}"), new("/home/projects/a.ts", "export const a = 1;"), new("/home/projects/b.ts", "export const b = 2;")] : configured ? [new("/home/projects/tsconfig.json", "{}"), new("/home/projects/index.ts", "export const x = 1;")] : [new("/home/projects/index.ts", "export const x = 1;")];
            using var fixture = new ProjectSessionFixture(files);
            var uri = new DocumentUri(ByteStringValue.FromString(balance ? "file:///home/projects/a.ts" : "file:///home/projects/index.ts"));
            fixture.Session.DidOpenFile(ProtocolRequestContext.Default, uri, 1, ByteStringValue.FromString(balance ? "export const a = 1;" : "export const x = 1;"), LanguageKind.TypeScript);
            var(_, error) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default, uri);
            Check(error is null);
            var starts = fixture.Client.ProgressStartCalls();
            var finishes = fixture.Client.ProgressFinishCalls();
            if (balance)
            {
                Check(starts.Count(call => ReferenceEquals(call.Message, Diagnostics.Project_0)) == finishes.Count(call => ReferenceEquals(call.Message, Diagnostics.Project_0)));
                return;
            }

            Check(starts.Length > 0);
            Check(finishes.Length > 0);
            Check(starts.Any(call => ReferenceEquals(call.Message, Diagnostics.Project_0)));
            if (configured)
                Check(finishes.Any(call => ReferenceEquals(call.Message, Diagnostics.Project_0)));
        }

        Groups(("emits progress for configured project loading", () => Run(true, false)), ("emits progress for inferred project loading", () => Run(false, false)), ("each start has a matching finish", () => Run(true, true)));
    }
}

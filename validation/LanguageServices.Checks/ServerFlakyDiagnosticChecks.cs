// Whole original server_flakydiagnostics_test.go, both noEmitOnError siblings.
using TypeScript.Lsp;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ServerIntegrationChecks
{
    public static void TestFlakyDiagnosticTrackingParallelEmit()
    {
        List<Exception>? failures = null;
        foreach (bool noEmitOnError in new[]
        {
            false,
            true
        }

        )
        {
            try
            {
                string config = "{\n\t\t\t\t\t\"compilerOptions\": { \"strict\": true, \"declaration\": true, \"noEmitOnError\": " + (noEmitOnError ? "true" : "false") + ", \"outDir\": \"out\" }\n\t\t\t\t}";
                const string a = "export function box<T>(value: T) { return { value }; }";
                Input[] files = [new("/src/tsconfig.json", config), new("/src/a.ts", a), new("/src/b.ts", "import { box } from \"./a\"; export const b = box(\"b\");"), new("/src/c.ts", "import { box } from \"./a\"; export const c = box(1);")];
                var fs = TypeScript.Bundled.Bundled.WrapFS(new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, files));
                Check(fs.CaseSensitivity() == CaseSensitivity.CaseInsensitive, "case sensitivity differs");
                using var client = new LspTestClient(new ServerOptions { Err = Stream.Null, Cwd = RootedDirectoryPath.FromGoString("/src"), FS = fs, DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath() }, DefaultServerRequest);
                var(message, _, ok) = client.SendRequest(Method.InitializeInfo, new InitializeParams { Capabilities = new ClientCapabilities(), InitializationOptions = new InitializationOptionsOrNull { InitializationOptions = new InitializationOptions { TrackFlakyDiagnostics = DiagnosticFlakeLogLevel.Panic } } });
                Check(ok && message!.AsResponse().Error is null, "initialize failed");
                client.SendNotification(Method.InitializedInfo, new InitializedParams());
                client.Server.InitComplete().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
                var uri = new DocumentUri(B("file:///src/a.ts"));
                client.SendNotification(Method.TextDocumentDidOpenInfo, new DidOpenTextDocumentParams { TextDocument = new TextDocumentItem { Uri = uri, LanguageId = LanguageKind.TypeScript, Text = B(a) } });
                var(response, diagnostics, received) = client.SendRequest(Method.TextDocumentDiagnosticInfo, new DocumentDiagnosticParams { TextDocument = new TextDocumentIdentifier { Uri = uri } });
                Check(received && response!.AsResponse().Error is null, "diagnostics request failed");
                Check(diagnostics.FullDocumentDiagnosticReport is not null, "full report missing");
                Equal(diagnostics.FullDocumentDiagnosticReport!.Items.Length, 0);
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(new InvalidOperationException("noEmitOnError=" + noEmitOnError, error));
            }
        }

        if (failures is not null)
            throw new AggregateException(failures);
    }
}

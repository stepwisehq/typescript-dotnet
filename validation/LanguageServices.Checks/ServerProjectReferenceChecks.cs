// Whole original server_projectreference_updates_test.go and mutable client helper.
using TypeScript.JsonRpc;
using TypeScript.Lsp;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Scanner;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ServerIntegrationChecks
{
    public static void TestReferencesAfterAncestorProjectConfigDeletion1()
    {
        const string main = "export function helloWorld() {}\nhelloWorld()\n";
        Input[] files = [new("/root/tsconfig.json", "{\n\t\t\t\"files\": [],\n\t\t\t\"references\": [{ \"path\": \"./project\" }]\n\t\t}"), new("/root/project/tsconfig.json", "{\n\t\t\t\"compilerOptions\": { \"composite\": true },\n\t\t\t\"include\": [\"src/**/*.ts\"]\n\t\t}"), new("/root/project/src/main.ts", main)];
        var preferences = new UserPreferences();
        var mutable = new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, files);
        ResponseMessage? OnServerRequest(ProtocolRequestContext _, RequestMessage request)
        {
            if (request.Method == Method.WorkspaceConfiguration)
                return new ResponseMessage
                {
                    ID = request.ID,
                    Result = new List<object?>
                    {
                        preferences.ToConfiguration()
                    }
                };
            if (request.Method == Method.ClientRegisterCapability || request.Method == Method.ClientUnregisterCapability)
                return new ResponseMessage
                {
                    ID = request.ID,
                    Result = new ProtocolPayload<Null>(ProtocolCodec.GetType_fa1d2db62d4d952e(), new Null())
                };
            return null;
        }

        using var client = new LspTestClient(new ServerOptions { Err = Stream.Null, Cwd = RootedDirectoryPath.FromGoString("/root"), FS = TypeScript.Bundled.Bundled.WrapFS(mutable), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath() }, OnServerRequest);
        var(initialized, _, ready) = client.SendRequest(Method.InitializeInfo, new InitializeParams { Capabilities = new ClientCapabilities() });
        Check(ready && initialized!.AsResponse().Error is null, "Initialize failed");
        client.SendNotification(Method.InitializedInfo, new InitializedParams());
        client.Server.InitComplete().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        client.SendNotification(Method.WorkspaceDidChangeConfigurationInfo, new DidChangeConfigurationParams { Settings = new Dictionary<string, object> { ["typescript"] = preferences.ToConfiguration() } });
        var uri = new DocumentUri(B("file:///root/project/src/main.ts"));
        client.SendNotification(Method.TextDocumentDidOpenInfo, new DidOpenTextDocumentParams { TextDocument = new TextDocumentItem { Uri = uri, LanguageId = LanguageKind.TypeScript, Text = B(main) } });
        var(prime, _, primed) = client.SendRequest(Method.TextDocumentDocumentSymbolInfo, new DocumentSymbolParams { TextDocument = new TextDocumentIdentifier { Uri = uri } });
        Check(primed && prime!.AsResponse().Error is null, "expected document-symbol response");
        // Original MapFS relative root/tsconfig.json is the same canonical /root owner.
        Check(mutable.Remove(RootedPath.FromGoString("/root/tsconfig.json"))is null, "failed config removal");
        client.SendNotification(Method.WorkspaceDidChangeWatchedFilesInfo, new DidChangeWatchedFilesParams { Changes = Slice<FileEvent?>.FromArray([new FileEvent { Uri = new DocumentUri(B("file:///root/tsconfig.json")), Type = FileChangeType.Deleted }]) });
        var(message, response, ok) = client.SendRequest(Method.TextDocumentReferencesInfo, new ReferenceParams { TextDocument = new TextDocumentIdentifier { Uri = uri }, Position = new Position { Line = 1, Character = 3 }, Context = new ReferenceContext { IncludeDeclaration = true } });
        Check(ok && message!.AsResponse().Error is null, "expected references response");
        Check(response.Locations is not null, "reference locations missing");
        var locations = response.Locations!.Value;
        Equal(locations.Length, 2);
        Check(locations[0]!.Uri.Equals(uri), "first reference URI differs");
        Equal(locations[0]!.Range, new TypeScript.Lsp.Protocol.Range { Start = new Position { Line = 0, Character = 16 }, End = new Position { Line = 0, Character = 26 } });
        Check(locations[1]!.Uri.Equals(uri), "second reference URI differs");
        Equal(locations[1]!.Range, new TypeScript.Lsp.Protocol.Range { Start = new Position { Line = 1, Character = 0 }, End = new Position { Line = 1, Character = 10 } });
    }
}

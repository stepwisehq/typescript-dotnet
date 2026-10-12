// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Whole server_completion_test.go five original functions and actual shared helpers.
using TypeScript.JsonRpc;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;
using CompletionItem = TypeScript.Lsp.Protocol.CompletionItem;

namespace LanguageServices.Checks;
public static partial class ServerIntegrationChecks
{
    private static LspTestClient InitializeCompletionClient(Input[] files, UserPreferences preferences)
    {
        ResponseMessage? HandleServerRequest(ProtocolRequestContext _, RequestMessage request)
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

        var client = new LspTestClient(Options(files), HandleServerRequest);
        try
        {
            var(message, _, ok) = client.SendRequest(Method.InitializeInfo, new InitializeParams { Capabilities = new ClientCapabilities() });
            Check(ok && message!.AsResponse().Error is null, "Initialize failed");
            client.SendNotification(Method.InitializedInfo, new InitializedParams());
            client.Server.InitComplete().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
            client.SendNotification(Method.WorkspaceDidChangeConfigurationInfo, new DidChangeConfigurationParams { Settings = new Dictionary<string, object> { ["typescript"] = preferences.ToConfiguration() } });
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static CompletionItem? FindCompletionItem(CompletionItemsOrListOrNull response, string label)
    {
        var items = response.List?.Items ?? response.Items ?? default;
        foreach (var item in items)
            if (item!.Label.EqualsValue(B(label)))
                return item;
        return null;
    }

    public static void TestCompletionAfterFileClose() => CompletionCase("afterClose");
    public static void TestCompletionWithConcurrentFileClose() => CompletionCase("concurrentClose");
    public static void TestCompletionForUnopenedFile() => CompletionCase("unopened");
    public static void TestAutoImportCompletionForUnopenedFile() => CompletionCase("autoImportUnopened");
    public static void TestCompletionSnapshotFreezing() => CompletionCase("snapshotFreezing");
    private static void CompletionCase(string name)
    {
        bool localOnly = name == "unopened";
        bool unopened = localOnly || name == "autoImportUnopened";
        bool freezing = name == "snapshotFreezing";
        var preferences = localOnly ? default : new UserPreferences
        {
            IncludeCompletionsForModuleExports = Tristate.True,
            IncludeCompletionsForImportStatements = Tristate.True
        };
        const string config = "{\"compilerOptions\": {\"module\": \"esnext\", \"target\": \"esnext\"}}";
        var files = new List<Input>
        {
            new("/home/projects/tsconfig.json", config)
        };
        if (!localOnly)
            files.Add(new("/home/projects/a.ts", "export const someVar = 10;"));
        string targetName = unopened ? "c.ts" : "b.ts";
        string initial = localOnly ? "let xyz = 1;\nxy" : freezing ? "someV" : "s";
        files.Add(new("/home/projects/" + targetName, initial));
        using var client = InitializeCompletionClient(files.ToArray(), preferences);
        var uri = new DocumentUri(B("file:///home/projects/" + targetName));
        if (!unopened)
        {
            var a = new DocumentUri(B("file:///home/projects/a.ts"));
            client.SendNotification(Method.TextDocumentDidOpenInfo, new DidOpenTextDocumentParams { TextDocument = new TextDocumentItem { Uri = a, LanguageId = LanguageKind.TypeScript, Text = B("export const someVar = 10;") } });
            client.SendNotification(Method.TextDocumentDidOpenInfo, new DidOpenTextDocumentParams { TextDocument = new TextDocumentItem { Uri = uri, LanguageId = LanguageKind.TypeScript, Text = B(initial) } });
        }

        if (name == "afterClose")
            client.SendNotification(Method.TextDocumentDidCloseInfo, new DidCloseTextDocumentParams { TextDocument = new TextDocumentIdentifier { Uri = uri } });
        var parameters = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier
            {
                Uri = uri
            },
            Position = new Position
            {
                Line = localOnly ? 1u : 0u,
                Character = localOnly ? 2u : freezing ? 5u : 1u
            },
            Context = new CompletionContext()
        };
        var waitForCompletion = client.SendRequestAsync(Method.TextDocumentCompletionInfo, parameters);
        if (name == "concurrentClose")
            client.SendNotification(Method.TextDocumentDidCloseInfo, new DidCloseTextDocumentParams { TextDocument = new TextDocumentIdentifier { Uri = uri } });
        else if (freezing)
            client.SendNotification(Method.TextDocumentDidChangeInfo, new DidChangeTextDocumentParams { TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 2 }, ContentChanges = Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new TextDocumentContentChangeWholeDocument { Text = B("notMatching") } }]) });
        var(message, response, ok) = waitForCompletion();
        Check(ok, "expected a response");
        Check(message!.AsResponse().Error is null);
        var item = FindCompletionItem(response, localOnly ? "xyz" : "someVar");
        Check(item is not null, freezing ? "expected someVar (snapshot freezing must preserve original content)" : "expected completion item");
        if (!localOnly)
        {
            Check(item!.Data is not null && item.Data.AutoImport is not null);
            Check(item.Data!.AutoImport!.ModuleSpecifier.EqualsValue(B("./a")));
        }
    }
}

// Whole original server_contentmapper_test.go, genuine accepted component IPC fixture.
using System.Threading.Channels;
using TypeScript.JsonRpc;
using TypeScript.Lsp;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ServerIntegrationChecks
{
    public static void TestSetContentMapperContributionsBeforeDidOpen()
    {
        const string component = "<component name=\"ProfileCard\">\n<template><h1>{{ title }}</h1></template>\n<script lang=\"ts\">\nexport const title = \"Profile\";\n</script>";
        Input[] files = [new("/home/project/tsconfig.json", "{\n\t\t\t\"compilerOptions\": { \"target\": \"es2020\", \"module\": \"esnext\", \"moduleResolution\": \"bundler\", \"strict\": true },\n\t\t\t\"contentMappers\": [ { \"package\": \"mapper\", \"extensions\": [\".vue\"] } ]\n\t\t}"), new("/home/project/node_modules/mapper/package.json", ContentMapperComponentFixture.PackageJSON), new("/home/project/ProfileCard.vue", component)];
        object mutex = new();
        List<Registration?> registrations = new();
        List<Unregistration?> unregistrations = new();
        var unregisteredSignal = Channel.CreateBounded<bool>(1);
        ResponseMessage? OnServerRequest(ProtocolRequestContext _, RequestMessage request)
        {
            if (request.Method == Method.WorkspaceConfiguration)
                return new ResponseMessage
                {
                    ID = request.ID,
                    Result = new object? []
                    {
                        null,
                        null,
                        null,
                        null
                    }
                };
            if (request.Method == Method.ClientRegisterCapability)
            {
                var(parameters, error) = request.UnmarshalParams(Method.ClientRegisterCapabilityInfo.Parameters);
                Check(error is null, error?.Message ?? "");
                lock (mutex)
                    foreach (var item in parameters!.Registrations)
                        registrations.Add(item);
            }
            else if (request.Method == Method.ClientUnregisterCapability)
            {
                var(parameters, error) = request.UnmarshalParams(Method.ClientUnregisterCapabilityInfo.Parameters);
                Check(error is null, error?.Message ?? "");
                lock (mutex)
                    foreach (var item in parameters!.Unregisterations)
                        unregistrations.Add(item);
                unregisteredSignal.Writer.WriteAsync(true).AsTask().GetAwaiter().GetResult();
            }
            else
                return null;
            return new ResponseMessage
            {
                ID = request.ID,
                Result = new ProtocolPayload<Null>(ProtocolCodec.GetType_fa1d2db62d4d952e(), new Null())
            };
        }

        var mutable = new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, files);
        var spawner = ContentMapperComponentFixture.NewSpawner();
        using var client = new LspTestClient(new ServerOptions { Err = Stream.Null, Cwd = RootedDirectoryPath.FromGoString("/home/project"), FS = TypeScript.Bundled.Bundled.WrapFS(mutable), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), Spawn = spawner.Spawn }, OnServerRequest);
        // Exact wire shape of the original explicit capabilities struct literal.
        // Decoded by the genuine closed protocol value descriptor, with empty arrays retained.
        const string capabilitiesJSON = """
        {"workspace":{"fileOperations":{"dynamicRegistration":true,"willRename":true}},
         "textDocument":{"synchronization":{"dynamicRegistration":true},"documentSymbol":{"dynamicRegistration":true},
          "foldingRange":{"dynamicRegistration":true},"selectionRange":{"dynamicRegistration":true},
          "inlayHint":{"dynamicRegistration":true},"codeLens":{"dynamicRegistration":true},
          "codeAction":{"dynamicRegistration":true},"formatting":{"dynamicRegistration":true},
          "rangeFormatting":{"dynamicRegistration":true},"onTypeFormatting":{"dynamicRegistration":true},
          "linkedEditingRange":{"dynamicRegistration":true},"callHierarchy":{"dynamicRegistration":true},
          "semanticTokens":{"dynamicRegistration":true,"requests":{},"tokenTypes":[],"tokenModifiers":[],"formats":["relative"]}}}
        """;
        ClientCapabilities caps = new();
        ProtocolCodec.Unmarshal(ProtocolCodec.GetType_ae5beebabf5633ee(), B(capabilitiesJSON), ref caps);
        var(initialized, _, initOK) = client.SendRequest(Method.InitializeInfo, new InitializeParams { Capabilities = caps, InitializationOptions = new InitializationOptionsOrNull { InitializationOptions = new InitializationOptions { RunExternalCode = true } } });
        Check(initOK && initialized!.AsResponse().Error is null, "initialize failed");
        client.SendNotification(Method.InitializedInfo, new InitializedParams());
        client.Server.InitComplete().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        var uri = new DocumentUri(B("file:///home/project/ProfileCard.vue"));
        var(message, _, ok) = client.SendRequest(Method.CustomSetContentMapperContributionsInfo, new SetContentMapperContributionsParams { OpenDocuments = Slice<TextDocumentIdentifier>.FromArray([new() { Uri = uri }]), Contributions = Slice<ContentMapperContribution?>.FromArray([new() { ContributorId = B("test"), Extensions = Slice<ByteStringValue>.FromArray([B(".vue"), B(".svelte")]) }]) });
        Check(ok && message!.AsResponse().Error is null, "contributions request failed");
        Registration? [] registered;
        lock (mutex)
            registered = registrations.ToArray();
        Check(registered.Length > 0, "expected dynamic registrations");
        string[] ids = ["content-mapper-did-open", "content-mapper-did-change", "content-mapper-did-close", "content-mapper-semantic-tokens", "content-mapper-document-symbol", "content-mapper-folding-range", "content-mapper-selection-range", "content-mapper-inlay-hint", "content-mapper-code-lens", "content-mapper-code-action", "content-mapper-formatting", "content-mapper-range-formatting", "content-mapper-on-type-formatting", "content-mapper-linked-editing", "content-mapper-call-hierarchy", "content-mapper-will-rename-files"];
        var expected = ids.ToDictionary(static value => ByteStringKey.FromValue(B(value)), static _ => false);
        foreach (var registration in registered)
        {
            var id = ByteStringKey.FromValue(registration!.Id);
            if (expected.ContainsKey(id))
                expected[id] = true;
            else
                Check(!registration.Id.StartsWith(B("content-mapper-")), "unsupported mapper registration");
            if (registration.Id.EqualsValue(B("content-mapper-did-open")))
            {
                Check(registration.RegisterOptions?.TextDocumentDidOpen is not null, "did-open options missing");
                AssertVueSelector(registration.RegisterOptions!.TextDocumentDidOpen!.DocumentSelector.DocumentSelector);
            }

            if (registration.Id.EqualsValue(B("content-mapper-semantic-tokens")))
            {
                Check(registration.RegisterOptions?.TextDocumentSemanticTokens is not null, "semantic options missing");
                AssertVueSelector(registration.RegisterOptions!.TextDocumentSemanticTokens!.DocumentSelector.DocumentSelector);
            }

            if (registration.Id.EqualsValue(B("content-mapper-code-action")))
            {
                Check(registration.RegisterOptions?.TextDocumentCodeAction is not null, "action options missing");
                var kinds = registration.RegisterOptions!.TextDocumentCodeAction!.CodeActionKinds!.Value;
                Check(kinds.AsSpan().SequenceEqual(new[] { CodeActionKind.QuickFix, CodeActionKind.SourceOrganizeImportsTs, CodeActionKind.SourceRemoveUnusedImportsTs, CodeActionKind.SourceSortImportsTs, CodeActionKind.SourceFixAllTs }), "code-action kinds differ");
            }
        }

        foreach (var pair in expected)
            Check(pair.Value, "missing mapper registration: " + pair.Key.Value.Materialize());
        client.SendNotification(Method.TextDocumentDidOpenInfo, new DidOpenTextDocumentParams { TextDocument = new TextDocumentItem { Uri = uri, LanguageId = new LanguageKind(B("vue")), Version = 1, Text = B(component) } });
        var hoverParams = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier
            {
                Uri = uri
            },
            Position = new Position { Line = 3, Character = 15 }
        };
        var(hoverMessage, hover, hoverOK) = client.SendRequest(Method.TextDocumentHoverInfo, hoverParams);
        Check(hoverOK && hoverMessage!.AsResponse().Error is null, "hover request failed");
        Check(hover.Hover is not null, "expected hover after first foreign didOpen");
        Check(mutable.WriteFile(RootedFilePath.FromGoString("/home/project/tsconfig.json"), "{\n\t\t\"compilerOptions\": { \"target\": \"es2020\", \"module\": \"esnext\", \"moduleResolution\": \"bundler\", \"strict\": true }\n\t}")is null, "config write failed");
        client.SendNotification(Method.WorkspaceDidChangeWatchedFilesInfo, new DidChangeWatchedFilesParams { Changes = Slice<FileEvent?>.FromArray([new() { Uri = new DocumentUri(B("file:///home/project/tsconfig.json")), Type = FileChangeType.Changed }]) });
        (hoverMessage, hover, _) = client.SendRequest(Method.TextDocumentHoverInfo, hoverParams);
        Check(hoverMessage is not null && hoverMessage.AsResponse().Error is null, "request before didClose should return null result");
        Check(hover.Hover is null, "expected null hover");
        var(diagnosticMessage, diagnostics, diagnosticOK) = client.SendRequest(Method.TextDocumentDiagnosticInfo, new DocumentDiagnosticParams { TextDocument = new TextDocumentIdentifier { Uri = uri } });
        Check(diagnosticOK && diagnosticMessage!.AsResponse().Error is null, "diagnostic request failed");
        Check(diagnostics.FullDocumentDiagnosticReport is not null, "full report missing");
        Equal(diagnostics.FullDocumentDiagnosticReport!.Items.Length, 0);
        var(completionMessage, completion, completionOK) = client.SendRequest(Method.TextDocumentCompletionInfo, new CompletionParams { TextDocument = new TextDocumentIdentifier { Uri = uri }, Position = new Position { Line = 3, Character = 15 } });
        Check(completionOK && completionMessage!.AsResponse().Error is null, "completion request failed");
        Check(completion.Items is null && completion.List is null, "completion should be null");
        var(referencesMessage, references, referencesOK) = client.SendRequest(Method.TextDocumentReferencesInfo, new ReferenceParams { TextDocument = new TextDocumentIdentifier { Uri = uri }, Position = new Position { Line = 3, Character = 15 }, Context = new ReferenceContext { IncludeDeclaration = true } });
        Check(referencesOK && referencesMessage!.AsResponse().Error is null, "references request failed");
        Check(references.Locations is null, "references should be null");
        var(renameMessage, rename, renameOK) = client.SendRequest(Method.TextDocumentRenameInfo, new RenameParams { TextDocument = new TextDocumentIdentifier { Uri = uri }, Position = new Position { Line = 3, Character = 15 }, NewName = B("renamed") });
        Check(renameOK && renameMessage!.AsResponse().Error is null, "rename request failed");
        Check(rename.WorkspaceEdit is null, "rename should be null");
        unregisteredSignal.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        Unregistration? [] unregistered;
        lock (mutex)
            unregistered = unregistrations.ToArray();
        Check(unregistered.Length > 0, "expected dynamic unregistration");
        foreach (var key in expected.Keys.ToArray())
            expected[key] = false;
        foreach (var unregistration in unregistered)
        {
            var key = ByteStringKey.FromValue(unregistration!.Id);
            if (expected.ContainsKey(key))
                expected[key] = true;
            else
                Check(!unregistration.Id.StartsWith(B("content-mapper-")), "unsupported mapper unregistration");
        }

        foreach (var pair in expected)
            Check(pair.Value, "missing mapper unregistration: " + pair.Key.Value.Materialize());
        client.SendNotification(Method.TextDocumentDidCloseInfo, new DidCloseTextDocumentParams { TextDocument = new TextDocumentIdentifier { Uri = uri } });
    }

    private static void AssertVueSelector(Slice<TextDocumentFilterLanguageOrSchemeOrPattern>? selector)
    {
        Check(selector is not null && selector.Value.Length == 1, "expected one selector");
        Check(selector!.Value[0].Pattern!.Pattern.Pattern!.Value.EqualsValue(B("**/*.vue")), "selector pattern differs");
    }
}

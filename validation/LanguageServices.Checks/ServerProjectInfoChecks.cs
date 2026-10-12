// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete server_projectinfo_test.go three original functions and exact helpers.
using TypeScript.JsonRpc;
using TypeScript.Lsp;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ServerIntegrationChecks
{
    private static ByteStringValue B(string text) => ByteStringValue.FromString(text);
    private static void Check(bool condition, string message = "original LSP assertion failed")
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T actual, T expected) => Check(EqualityComparer<T>.Default.Equals(actual, expected), $"expected {expected}; actual {actual}");
    private static ServerOptions Options(Input[] files) => new()
    {
        Err = Stream.Null,
        Cwd = RootedDirectoryPath.FromGoString("/home/projects"),
        FS = TypeScript.Bundled.Bundled.WrapFS(new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, files)),
        DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath()
    };
    private static ResponseMessage? DefaultServerRequest(ProtocolRequestContext context, RequestMessage request) => request.Method == Method.ClientRegisterCapability || request.Method == Method.ClientUnregisterCapability || request.Method == Method.WindowWorkDoneProgressCreate ? new ResponseMessage
    {
        ID = request.ID,
        Result = new ProtocolPayload<Null>(ProtocolCodec.GetType_fa1d2db62d4d952e(), new Null())
    }

    : null;
    private static LspTestClient InitializeProjectInfoClient(Input[] files)
    {
        var client = new LspTestClient(Options(files), DefaultServerRequest);
        try
        {
            var(message, _, ok) = client.SendRequest(Method.InitializeInfo, new InitializeParams { Capabilities = new ClientCapabilities() });
            Check(ok && message!.AsResponse().Error is null, "Initialize failed");
            client.SendNotification(Method.InitializedInfo, new InitializedParams());
            // Original untimed InitComplete receive; only test deadlock-safety bound.
            client.Server.InitComplete().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public static void TestInitializeCodeActionKinds()
    {
        using var client = new LspTestClient(Options([]));
        var(message, result, ok) = client.SendRequest(Method.InitializeInfo, new InitializeParams { Capabilities = new ClientCapabilities() });
        Check(ok && message!.AsResponse().Error is null, "Initialize failed");
        var expected = new[]
        {
            CodeActionKind.QuickFix,
            CodeActionKind.SourceOrganizeImportsTs,
            CodeActionKind.SourceRemoveUnusedImportsTs,
            CodeActionKind.SourceSortImportsTs,
            CodeActionKind.SourceFixAllTs
        };
        var actual = result!.Capabilities!.CodeActionProvider!.CodeActionOptions!.CodeActionKinds!.Value;
        Check(actual.AsSpan().SequenceEqual(expected));
    }

    public static void TestProjectInfoConfiguredProject()
    {
        using var client = InitializeProjectInfoClient([new("/home/projects/tsconfig.json", "{}"), new("/home/projects/index.ts", "export const x = 1;")]);
        var uri = new DocumentUri(B("file:///home/projects/index.ts"));
        client.SendNotification(Method.TextDocumentDidOpenInfo, new DidOpenTextDocumentParams { TextDocument = new TextDocumentItem { Uri = uri, LanguageId = LanguageKind.TypeScript, Text = B("export const x = 1;") } });
        var(message, result, ok) = client.SendRequest(Method.CustomProjectInfoInfo, new ProjectInfoParams { TextDocument = new TextDocumentIdentifier { Uri = uri } });
        Check(ok, "expected a response");
        Check(message!.AsResponse().Error is null);
        Check(result!.ConfigFilePath.EqualsValue(B("/home/projects/tsconfig.json")));
    }

    public static void TestProjectInfoInferredProject()
    {
        using var client = InitializeProjectInfoClient([new("/home/projects/index.ts", "export const x = 1;")]);
        var uri = new DocumentUri(B("file:///home/projects/index.ts"));
        client.SendNotification(Method.TextDocumentDidOpenInfo, new DidOpenTextDocumentParams { TextDocument = new TextDocumentItem { Uri = uri, LanguageId = LanguageKind.TypeScript, Text = B("export const x = 1;") } });
        var(message, result, ok) = client.SendRequest(Method.CustomProjectInfoInfo, new ProjectInfoParams { TextDocument = new TextDocumentIdentifier { Uri = uri } });
        Check(ok, "expected a response");
        Check(message!.AsResponse().Error is null);
        Check(result!.ConfigFilePath.Length == 0);
    }
}

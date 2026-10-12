// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete original server_semantictokens_test.go two functions; genuine framed client.
using TypeScript.Collections;
using TypeScript.Lsp;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ServerIntegrationChecks
{
    private static LspTestClient InitializeSemanticClient(ServerOptions options)
    {
        var client = new LspTestClient(options, DefaultServerRequest);
        try
        {
            var(message, _, ok) = client.SendRequest(Method.InitializeInfo, new InitializeParams { Capabilities = new ClientCapabilities { TextDocument = new TextDocumentClientCapabilities { SemanticTokens = new SemanticTokensClientCapabilities { Requests = new ClientSemanticTokensRequestOptions { Full = new BooleanOrClientSemanticTokensRequestFullDelta { Boolean = true } }, TokenTypes = Slice<ByteStringValue>.FromArray([B("namespace"), B("type"), B("class"), B("enum"), B("interface"), B("struct"), B("typeParameter"), B("parameter"), B("variable"), B("property"), B("enumMember"), B("event"), B("function"), B("method"), B("macro"), B("keyword"), B("modifier"), B("comment"), B("string"), B("number"), B("regexp"), B("operator"), B("decorator")]), TokenModifiers = Slice<ByteStringValue>.FromArray([B("declaration"), B("definition"), B("readonly"), B("static"), B("deprecated"), B("abstract"), B("async"), B("modification"), B("documentation"), B("defaultLibrary"), B("local")]) } } } });
            Check(ok && message!.AsResponse().Error is null, "Initialize failed");
            client.SendNotification(Method.InitializedInfo, new InitializedParams());
            client.Server.InitComplete().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public static void TestSemanticTokensCRLF()
    {
        const string disk = "var x\nvar x\nvar x\nvar x\nvar x\nvar x\nconst a = 1\n";
        string editor = disk.Replace("\n", "\r\n", StringComparison.Ordinal);
        using var client = InitializeSemanticClient(Options([new("/home/projects/tsconfig.json", "{}"), new("/home/projects/test.ts", disk), new("/home/projects/other.ts", "export {}")]));
        var other = new DocumentUri(B("file:///home/projects/other.ts"));
        client.SendNotification(Method.TextDocumentDidOpenInfo, new DidOpenTextDocumentParams { TextDocument = new TextDocumentItem { Uri = other, LanguageId = LanguageKind.TypeScript, Text = B("export {}") } });
        var(first, _, _) = client.SendRequest(Method.TextDocumentSemanticTokensFullInfo, new SemanticTokensParams { TextDocument = new TextDocumentIdentifier { Uri = other } });
        Check(first!.AsResponse().Error is null, "Initial request failed");
        var uri = new DocumentUri(B("file:///home/projects/test.ts"));
        client.SendNotification(Method.TextDocumentDidOpenInfo, new DidOpenTextDocumentParams { TextDocument = new TextDocumentItem { Uri = uri, LanguageId = LanguageKind.TypeScript, Text = B(editor) } });
        var(message, _, _) = client.SendRequest(Method.TextDocumentSemanticTokensFullInfo, new SemanticTokensParams { TextDocument = new TextDocumentIdentifier { Uri = uri } });
        Check(message!.AsResponse().Error is null, "Semantic tokens request failed");
    }

    public static void TestSemanticTokensDefaultLibraryCaseInsensitive()
    {
        const string library = "/// <reference no-default-lib=\"true\"/>\ninterface Boolean {}\ninterface Function {}\ninterface CallableFunction {}\ninterface NewableFunction {}\ninterface IArguments {}\ninterface Number { toExponential: any; }\ninterface Object {}\ninterface RegExp {}\ninterface String { charAt: any; }\ninterface Array<T> { length: number; [n: number]: T; }\ninterface ReadonlyArray<T> {}\ndeclare const console: { log(msg: any): void; };\n";
        var options = new ServerOptions
        {
            Err = Stream.Null,
            Cwd = RootedDirectoryPath.FromGoString("/home/projects"),
            FS = new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, [new Input("/home/projects/tsconfig.json", "{\"compilerOptions\": {\"lib\": [\"es5\"]}}"), new Input("/home/projects/test.ts", "console.log(\"hi\");"), new Input("/TSLib/lib.es5.d.ts", library)]),
            DefaultLibraryPath = RootedDirectoryPath.FromGoString("/TSLib")
        };
        using var client = InitializeSemanticClient(options);
        var uri = new DocumentUri(B("file:///home/projects/test.ts"));
        client.SendNotification(Method.TextDocumentDidOpenInfo, new DidOpenTextDocumentParams { TextDocument = new TextDocumentItem { Uri = uri, LanguageId = LanguageKind.TypeScript, Text = B("console.log(\"hi\");") } });
        var(message, result, ok) = client.SendRequest(Method.TextDocumentSemanticTokensFullInfo, new SemanticTokensParams { TextDocument = new TextDocumentIdentifier { Uri = uri } });
        Check(ok, "Semantic tokens request did not return a result");
        Check(message!.AsResponse().Error is null, "Semantic tokens request failed");
        Check(result!.SemanticTokens is not null, "Expected non-null semantic tokens");
        var data = result.SemanticTokens!.Data;
        Check(data.Length >= 5 && data.Length % 5 == 0, "Malformed semantic tokens data");
        const uint defaultLibraryBit = 1 << 9;
        bool hasDefaultLibrary = false;
        for (int i = 4; i < data.Length; i += 5)
            if ((data[i] & defaultLibraryBit) != 0)
            {
                hasDefaultLibrary = true;
                break;
            }

        Check(hasDefaultLibrary, "Expected at least one token with the defaultLibrary modifier");
    }
}

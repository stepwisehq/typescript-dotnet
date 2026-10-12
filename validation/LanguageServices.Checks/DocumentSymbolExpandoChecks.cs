// Authored complete-response differential; no original-function credit.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static class DocumentSymbolExpandoChecks
{
    public static int RunAll()
    {
        RunCase(true);
        RunCase(false);
        return 2;
    }

    public static void RunCase(bool hierarchical)
    {
        const string text = "function f() {}\nfunction f() {}\nf.a = 1;\nf.b = 2;\n";
        using var fixture = new ProjectSessionFixture([new BaselineFS.Input("/fixture/tsconfig.json", "{\"compilerOptions\":{\"skipLibCheck\":true,\"allowJs\":true,\"checkJs\":true},\"files\":[\"input.js\"]}"), new BaselineFS.Input("/fixture/input.js", text)]);
        var uri = new DocumentUri(ByteStringValue.FromString("file:///fixture/input.js"));
        fixture.Session.DidOpenFile(ProtocolRequestContext.Default, uri, 1, ByteStringValue.FromString(text), LanguageKind.JavaScript);
        var(service, error) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default, uri);
        if (error is not null)
            throw error;
        var capabilities = new ResolvedClientCapabilities();
        capabilities.TextDocument.DocumentSymbol.HierarchicalDocumentSymbolSupport = hierarchical;
        var context = ProtocolRequestContext.Default.WithClientCapabilities(capabilities);
        var(response, responseError) = service!.ProvideDocumentSymbols(context, uri);
        if (responseError is not null)
            throw responseError;
        var actual = ProtocolCodec.Marshal(Method.TextDocumentDocumentSymbolInfo.Result, response);
        var expected = hierarchical ? Hierarchical : Flat;
        if (!actual.EqualsValue(ByteStringValue.FromString(expected)))
            throw new InvalidOperationException("Complete document-symbol response differs from pinned Go: " + GoString.ToString(actual.Materialize()!));
    }

    private const string Hierarchical = "[{\"name\":\"f\",\"kind\":12,\"range\":{\"start\":{\"line\":0,\"character\":0},\"end\":{\"line\":0,\"character\":15}},\"selectionRange\":{\"start\":{\"line\":0,\"character\":9},\"end\":{\"line\":0,\"character\":10}},\"children\":[{\"name\":\"a\",\"kind\":13,\"range\":{\"start\":{\"line\":2,\"character\":0},\"end\":{\"line\":2,\"character\":3}},\"selectionRange\":{\"start\":{\"line\":2,\"character\":2},\"end\":{\"line\":2,\"character\":3}},\"children\":[]},{\"name\":\"b\",\"kind\":13,\"range\":{\"start\":{\"line\":3,\"character\":0},\"end\":{\"line\":3,\"character\":3}},\"selectionRange\":{\"start\":{\"line\":3,\"character\":2},\"end\":{\"line\":3,\"character\":3}},\"children\":[]}]},{\"name\":\"f\",\"kind\":12,\"range\":{\"start\":{\"line\":1,\"character\":0},\"end\":{\"line\":1,\"character\":15}},\"selectionRange\":{\"start\":{\"line\":1,\"character\":9},\"end\":{\"line\":1,\"character\":10}},\"children\":[{\"name\":\"a\",\"kind\":13,\"range\":{\"start\":{\"line\":2,\"character\":0},\"end\":{\"line\":2,\"character\":3}},\"selectionRange\":{\"start\":{\"line\":2,\"character\":2},\"end\":{\"line\":2,\"character\":3}},\"children\":[]},{\"name\":\"b\",\"kind\":13,\"range\":{\"start\":{\"line\":3,\"character\":0},\"end\":{\"line\":3,\"character\":3}},\"selectionRange\":{\"start\":{\"line\":3,\"character\":2},\"end\":{\"line\":3,\"character\":3}},\"children\":[]}]}]";
    private const string Flat = "[{\"name\":\"f\",\"kind\":12,\"location\":{\"uri\":\"file:///fixture/input.js\",\"range\":{\"start\":{\"line\":0,\"character\":0},\"end\":{\"line\":0,\"character\":15}}}},{\"name\":\"a\",\"kind\":13,\"containerName\":\"f\",\"location\":{\"uri\":\"file:///fixture/input.js\",\"range\":{\"start\":{\"line\":2,\"character\":0},\"end\":{\"line\":2,\"character\":3}}}},{\"name\":\"b\",\"kind\":13,\"containerName\":\"f\",\"location\":{\"uri\":\"file:///fixture/input.js\",\"range\":{\"start\":{\"line\":3,\"character\":0},\"end\":{\"line\":3,\"character\":3}}}},{\"name\":\"f\",\"kind\":12,\"location\":{\"uri\":\"file:///fixture/input.js\",\"range\":{\"start\":{\"line\":1,\"character\":0},\"end\":{\"line\":1,\"character\":15}}}},{\"name\":\"a\",\"kind\":13,\"containerName\":\"f\",\"location\":{\"uri\":\"file:///fixture/input.js\",\"range\":{\"start\":{\"line\":2,\"character\":0},\"end\":{\"line\":2,\"character\":3}}}},{\"name\":\"b\",\"kind\":13,\"containerName\":\"f\",\"location\":{\"uri\":\"file:///fixture/input.js\",\"range\":{\"start\":{\"line\":3,\"character\":0},\"end\":{\"line\":3,\"character\":3}}}}]";
}

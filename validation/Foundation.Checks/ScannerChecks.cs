// Copyright (c) Microsoft Corporation. All rights reserved.
// Apache-2.0: licenses/TypeScript-LICENSE.txt.
// Original assertions from pinned tsc/internal/scanner/scanner_test.go.
// Upstream fed0bf24149fb1ed36039212648bafdafc1ea10e; representation notes in docs/foundation-scanner-tests.json.
using TypeScript.Scanner;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.Foundation;
public static partial class ScannerChecks
{
    private static readonly Dictionary<string, Action> checks = CreateChecks();
    public static IReadOnlyList<string> Cases { get; } = checks.Keys.ToArray();

    public static void Run(string caseName)
    {
        if (!checks.TryGetValue(caseName, out Action? check))
            throw new ArgumentException("Unknown scanner foundation case: " + caseName, nameof(caseName));
        check();
    }

    private static Dictionary<string, Action> CreateChecks()
    {
        var cases = new Dictionary<string, Action>(StringComparer.Ordinal);
        cases.Add("TestScanStringPreservesLoneSurrogates", () =>
        {
            var scanner = new ScannerApi();
            scanner.SetText("\"🦀\\ud7ff\\ud800\\ud801\\uD83E\\uDD80\"");
            Equal(Kind.StringLiteral, scanner.Scan(), "Scan kind");
            string expected = GoString.ToUtf8("🦀") + StringUtil.EncodeJSStringRune(0xD7FF) + StringUtil.EncodeJSStringRune(0xD800) + StringUtil.EncodeJSStringRune(0xD801) + GoString.ToUtf8("🦀");
            Equal(expected, scanner.TokenValueByteString(), "TokenValue byte string");
        });
        (string Name, string Text, string[] ExpectedLines)[] normalization = [("single line", " \t* \tFoo", ["Foo"]), ("ECMAScript line breaks", "Foo\r\n * Bar\r\t* Baz\u2028 * Qux\u2029* Quux", ["Foo", "Bar", "Baz", "Qux", "Quux"]), ("blank and trailing lines", "Foo\r\n *\r\n", ["Foo", "", ""]), ("line without marker", "Foo\n  Bar", ["Foo", "Bar"]), ("only leading marker", "**Foo", ["*Foo"])];
        foreach (var test in normalization)
            cases.Add("TestNormalizeJSDocTypeSourceText/" + test.Name, () => Equal(GoString.ToUtf8(string.Join("\n", test.ExpectedLines)), ScannerApi.normalizeJSDocTypeSourceText(GoString.ToUtf8(test.Text)), test.Name));
        var jsDocType = new Node
        {
            Kind = Kind.TypeReference,
            Flags = NodeFlags.JSDoc
        };
        var jsDocTypeChild = new Node
        {
            Kind = Kind.Identifier,
            Flags = NodeFlags.JSDoc,
            Parent = jsDocType
        };
        var reparsedType = new Node
        {
            Kind = Kind.TypeLiteral,
            Flags = NodeFlags.Reparsed
        };
        var reparsedTypeChild = new Node
        {
            Kind = Kind.Identifier,
            Flags = NodeFlags.Reparsed,
            Parent = reparsedType
        };
        var ordinaryType = new Node
        {
            Kind = Kind.TypeReference
        };
        var jsDocTag = new Node
        {
            Kind = Kind.JSDocParameterTag,
            Flags = NodeFlags.JSDoc
        };
        var jsDocTagChild = new Node
        {
            Kind = Kind.Identifier,
            Flags = NodeFlags.JSDoc,
            Parent = jsDocTag
        };
        (string Name, Node Node, bool Expected)[] predicates = [("type expression", new Node { Kind = Kind.JSDocTypeExpression }, true), ("JSDoc type", jsDocType, true), ("JSDoc type child", jsDocTypeChild, true), ("reparsed type", reparsedType, true), ("reparsed type child", reparsedTypeChild, true), ("ordinary type", ordinaryType, false), ("other JSDoc child", jsDocTagChild, false)];
        foreach (var test in predicates)
            cases.Add("TestIsJSDocTypeExpressionOrChild/" + test.Name, () => Equal(test.Expected, ScannerApi.isJSDocTypeExpressionOrChild(test.Node), test.Name));
        cases.Add("TestGetTextOfNodeFromJSDocTypePreservesAsteriskType", () =>
        {
            string source = string.Join("\n", "", " * *");
            var node = new Node
            {
                Kind = Kind.JSDocAllType,
                Flags = NodeFlags.JSDoc,
                Loc = Core.NewTextRange(0, source.Length)
            };
            Equal("*", ScannerApi.GetTextOfNodeFromSourceText(source, node, false), "JSDoc all-type source text");
        });
        cases.Add("TestScanSourceKeyword", () =>
        {
            var scanner = new ScannerApi();
            scanner.SetText("source sourceValue");
            Equal(Kind.SourceKeyword, scanner.Scan(), "First token");
            Equal("source", ScannerApi.TokenToString(Kind.SourceKeyword), "TokenToString");
            Equal(Kind.SourceKeyword, ScannerApi.StringToToken("source"), "StringToToken");
            Equal(Kind.Identifier, scanner.Scan(), "Second token");
            Equal("sourceValue", scanner.TokenValue(), "TokenValue");
        });
        AddPathChecks(cases);
        AddNumberChecks(cases);
        AddHelperChecks(cases);
        AddSyntaxUtilityChecks(cases);
        return cases;
    }

    private static void Equal<T>(T expected, T actual, string context)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{context}: expected {expected}, actual {actual}");
    }
}

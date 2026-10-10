// Authored ownership/public-boundary controls, not upstream compiler test ports.
// Covers ast_generated.go text factories/clones and the managed atomic projection
// needed when multiple checkers read one immutable parsed tree.
using System.Runtime.InteropServices;
using TypeScript.Scanner;

namespace TypeScript.Foundation;

public static class AstTextValueChecks
{
    public static IReadOnlyList<string> Cases { get; } =
        ["owned-null-set-clone", "borrowed-factory-hooks", "parsed-text-lifetime", "concurrent-first-read", "borrowed-symbol-lookup"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "owned-null-set-clone": PublicLifecycle(); break;
            case "borrowed-factory-hooks": BorrowedHooks(); break;
            case "parsed-text-lifetime": ParsedLifetime(); break;
            case "concurrent-first-read": ConcurrentRead(); break;
            case "borrowed-symbol-lookup": Lookup(); break;
            default: throw new ArgumentException("Unknown AST text check", nameof(name));
        }
    }

    private static readonly Kind[] Kinds =
        [Kind.Identifier, Kind.PrivateIdentifier, Kind.StringLiteral, Kind.NumericLiteral, Kind.BigIntLiteral,
         Kind.RegularExpressionLiteral, Kind.NoSubstitutionTemplateLiteral, Kind.TemplateHead,
         Kind.TemplateMiddle, Kind.TemplateTail, Kind.JsxText];
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private static string? Text(Node node) => node.Kind == Kind.JsxText ? node.AsJsxText().Text : node.Text();
    private static void SetText(Node node, string? text)
    {
        if (node.Kind == Kind.Identifier) node.AsIdentifier().Text = text!;
        else if (node.Kind == Kind.PrivateIdentifier) node.AsPrivateIdentifier().Text = text!;
        else if (node.Kind == Kind.JsxText) node.AsJsxText().Text = text!;
        else ((LiteralLikeNodeBase)node.data).Text = text!;
    }
    private static Node Owned(NodeFactory f, Kind kind, string? text) => kind switch
    {
        Kind.Identifier => f.NewIdentifier(text!),
        Kind.PrivateIdentifier => f.NewPrivateIdentifier(text!),
        Kind.StringLiteral => f.NewStringLiteral(text!, 0),
        Kind.NumericLiteral => f.NewNumericLiteral(text!, 0),
        Kind.BigIntLiteral => f.NewBigIntLiteral(text!, 0),
        Kind.RegularExpressionLiteral => f.NewRegularExpressionLiteral(text!, 0),
        Kind.NoSubstitutionTemplateLiteral => f.NewNoSubstitutionTemplateLiteral(text!, 0),
        Kind.TemplateHead => f.NewTemplateHead(text!, text!, 0),
        Kind.TemplateMiddle => f.NewTemplateMiddle(text!, text!, 0),
        Kind.TemplateTail => f.NewTemplateTail(text!, text!, 0),
        Kind.JsxText => f.NewJsxText(text!, false),
        _ => throw new InvalidOperationException()
    };
    private static Node Borrowed(NodeFactory f, Kind kind, ByteStringValue text) => kind switch
    {
        Kind.Identifier => f.NewIdentifierWithTextValue(text),
        Kind.PrivateIdentifier => f.NewPrivateIdentifierWithTextValue(text),
        Kind.StringLiteral => f.NewStringLiteralWithTextValue(text, 0),
        Kind.NumericLiteral => f.NewNumericLiteralWithTextValue(text, 0),
        Kind.BigIntLiteral => f.NewBigIntLiteralWithTextValue(text, 0),
        Kind.RegularExpressionLiteral => f.NewRegularExpressionLiteralWithTextValue(text, 0),
        Kind.NoSubstitutionTemplateLiteral => f.NewNoSubstitutionTemplateLiteralWithTextValue(text, 0),
        Kind.TemplateHead => f.NewTemplateHeadWithTextValue(text, text, 0),
        Kind.TemplateMiddle => f.NewTemplateMiddleWithTextValue(text, text, 0),
        Kind.TemplateTail => f.NewTemplateTailWithTextValue(text, text, 0),
        Kind.JsxText => f.NewJsxTextWithTextValue(text, false),
        _ => throw new InvalidOperationException()
    };

    private static void PublicLifecycle()
    {
        var f = new NodeFactory();
        string supplied = new(['o', 'w', 'n', 'e', 'd']);
        foreach (var kind in Kinds)
        {
            var node = Owned(f, kind, supplied);
            Require(ReferenceEquals(supplied, Text(node)), "Supplied owned string remains the public value");
            var clone = node.Clone(f);
            Require(!ReferenceEquals(node, clone) && ReferenceEquals(supplied, Text(clone)), "Clone retains owned text, independently of node identity");
            SetText(node, "changed");
            Require(Text(node) == "changed" && Text(clone) == "owned", "Setter replacement does not change the clone");
            SetText(node, null);
            Require(Text(node) is null && Text(node.Clone(f)) is null, "Public null text remains null through clone");
            SetText(node, "");
            Require(Text(node) == "" && !node.TextValue().IsNull, "Empty remains distinct from supplied null");
            Require(Text(Borrowed(f, kind, default)) == "", "Default internal header preserves initialized empty text");
        }
    }

    private static void BorrowedHooks()
    {
        byte[] bytes = [0x70, 0xc3, 0xa9, 0xed, 0xa0, 0x80, 0xff, 0x73];
        var value = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes).Slice(1, 7));
        var expected = GoString.FromBytes(bytes.AsSpan(1, 6));
        int created = 0, cloned = 0;
        var f = new NodeFactory(new NodeFactoryHooks
        {
            OnCreate = node =>
            {
                Require(node.TextValue().EqualsBytes(bytes.AsSpan(1, 6)), "OnCreate sees complete borrowed text before publication");
                created++;
            },
            OnClone = (node, original) =>
            {
                Require(Text(node) == expected && Text(original) == expected, "OnClone sees complete text");
                cloned++;
            }
        });
        foreach (var kind in Kinds)
        {
            var node = Borrowed(f, kind, value);
            var header = node.TextValue();
            Require(MemoryMarshal.TryGetArray(header.ByteView.Memory, out var region) && ReferenceEquals(region.Array, bytes) && region.Offset == 1 && region.Count == 6, "AST retains exact owner and logical byte range");
            var clone = node.Clone(f);
            Require(header.EqualsValue(clone.TextValue()) && Text(node) == expected, "Malformed UTF8/WTF8 text remains opaque raw bytes");
            SetText(node, "replacement");
            Require(Text(clone) == expected && header.EqualsBytes(bytes.AsSpan(1, 6)), "Borrowed snapshot/clone survive setter detach");
        }
        Require(created == Kinds.Length * 2 && cloned == Kinds.Length && f.TextCount() == Kinds.Length * 2, "Existing factory/clone publication counts remain unchanged");
    }

    private static void ParsedLifetime()
    {
        const string text = "const borrowed = 'value'; const template = `head${borrowed}tail`;";
        var file = Parser.ParseSourceFile(new("/text.ts", "/text.ts"), GoString.Bytes(text), ScriptKind.TS);
        Require((file.Diagnostics()?.Count ?? 0) == 0, "Source parses without diagnostics");
        var found = new List<Node>();
        void Walk(Node node)
        {
            if (node.Kind is Kind.Identifier or Kind.StringLiteral or Kind.TemplateHead or Kind.TemplateTail) found.Add(node);
            node.ForEachChild(child => { Walk(child); return false; });
        }
        Walk(file.AsNode());
        Require(MemoryMarshal.TryGetArray(file.TextView().Memory, out var source), "Source owns a byte array");
        foreach (var node in found)
        {
            var header = node.TextValue();
            Require(header.IsBytes && MemoryMarshal.TryGetArray(header.ByteView.Memory, out var region) && ReferenceEquals(source.Array, region.Array), "Unchanged parsed text shares canonical source bytes");
            var before = header.Materialize();
            _ = Parser.ParseSourceFile(new("/later.ts", "/later.ts"), GoString.Bytes("const replacement = 0;"), ScriptKind.TS);
            Require(Text(node) == before && node.Clone(new NodeFactory()).TextValue().EqualsString(before), "Later parse does not invalidate detached AST token text");
        }
        Require(found.Any(n => n.Kind == Kind.TemplateHead) && found.Any(n => n.Kind == Kind.TemplateTail), "Both raw template fragment factories exercised");
    }

    private static void ConcurrentRead()
    {
        var bytes = "prefix-concurrent-suffix"u8.ToArray();
        var value = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes).Slice(7, 17));
        foreach (var kind in Kinds)
        {
            var node = Borrowed(new NodeFactory(), kind, value);
            var results = new string?[16];
            Parallel.For(0, results.Length, i =>
            {
                for (int read = 0; read < 16; read++)
                {
                    Require(node.TextValue().EqualsBytes("concurrent"u8), "Concurrent descriptor reads retain content while projection occurs");
                    results[i] = Text(node);
                }
            });
            Require(results.All(s => s == "concurrent" && ReferenceEquals(s, results[0])), "One canonical first public projection for one shared node");
        }
    }

    private static void Lookup()
    {
        var table = new SymbolTable();
        foreach (byte[] bytes in new byte[][] { Enumerable.Range(0, 256).Select(i => (byte)i).ToArray(), [0x61, 0xc3, 0xa9, 0xff, 0xed, 0xa0, 0x80], Enumerable.Repeat((byte)'x', 513).ToArray(), [] })
        {
            var key = GoString.FromBytes(bytes);
            var symbol = new Symbol { Name = "mutated public name" };
            table.Add(key, symbol);
            var value = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes));
            Require(table.TryGetValue(value, out var actualKey, out var actual) && ReferenceEquals(actual, symbol) && ReferenceEquals(actualKey, key), "Borrowed lookup returns the stored key, independent of Symbol.Name");
            Require(table.ContainsKey(value) && ReferenceEquals(table[value], symbol), "Borrowed read APIs agree");
            Require(ByteStringValue.FromString(key).TryGetValue(table, out actual) && ReferenceEquals(actual, symbol), "Owned raw string follows the same ordinal lookup");
        }
        Require(!table.ContainsKey(ByteStringValue.FromString("missing")), "Miss preserves false");
        try { table.ContainsKey(ByteStringValue.FromString(null)); }
        catch (ArgumentNullException) { return; }
        throw new InvalidOperationException("Original null dictionary-key failure must remain");
    }
}

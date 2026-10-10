// Authored public token/AST regression checks, plus one unchanged original test body.
// Pinned source: scanner.go scanString:1586, scanNumberFragment:2044;
// parser.go getTemplateLiteralRawText:3774. No allocation-count assertions.
using TypeScript.Scanner;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.Foundation;

public static class TokenProjectionChecks
{
    public static IReadOnlyList<string> Cases { get; } =
        ["original-lone-surrogates", "strings-and-escapes", "unterminated-strings",
         "numeric-separators", "template-raw-bounds"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "original-lone-surrogates": ScannerChecks.Run("TestScanStringPreservesLoneSurrogates"); break;
            case "strings-and-escapes": Strings(); break;
            case "unterminated-strings": Unterminated(); break;
            case "numeric-separators": Numbers(); break;
            case "template-raw-bounds": Templates(); break;
            default: throw new ArgumentException("Unknown token projection check: " + name, nameof(name));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Equal<T>(T expected, T actual, string message) =>
        Require(EqualityComparer<T>.Default.Equals(expected, actual), message);

    private static void Strings()
    {
        foreach (var (source, expected) in new[]
        {
            ("'café🦀'", "café🦀"),
            ("'a\\nb\\\\c\\'d'", "a\nb\\c'd"),
            ("'a\\\r\nb'", "ab"),
            ("''", "")
        })
        {
            var scanner = new ScannerApi(); var errors = new List<int>();
            scanner.SetOnError((message, _, _, _) => errors.Add(message.Code()));
            scanner.SetText(source);
            Equal(Kind.StringLiteral, scanner.Scan(), "String token kind");
            Equal(GoString.ToUtf8(expected), scanner.TokenValueByteString(), "Decoded string bytes");
            Equal(GoString.ToUtf8(source), scanner.TokenTextByteString(), "Original token spelling");
            Equal(GoString.ToUtf8(source).Length, scanner.TokenEnd(), "Byte token end");
            Require((scanner.TokenFlags() & TokenFlags.SingleQuote) != 0, "Quote flag retained");
            Equal(0, errors.Count, "Valid string diagnostics");
        }
        byte[] malformed = [0x27, 0xff, 0xed, 0xa0, 0x80, 0x27];
        var raw = new ScannerApi(); raw.SetText(malformed);
        Equal(Kind.StringLiteral, raw.Scan(), "Malformed raw bytes remain literal data");
        Require(raw.TokenValueBytes().AsSpan().SequenceEqual(malformed.AsSpan(1, 4)), "Malformed/WTF8 bytes survive final projection");
        var jsx = new ScannerApi(); jsx.SetText("'a\\n\nb'");
        Equal(Kind.StringLiteral, jsx.ScanJsxAttributeValue(), "JSX attribute string kind");
        Equal("a\\n\nb", jsx.TokenValueByteString(), "JSX does not decode escapes or reject embedded LF");
    }

    private static void Unterminated()
    {
        foreach (string source in new[] { "'ab", "'ab\n", "'ab\r" })
        {
            var scanner = new ScannerApi(); var errors = new List<int>();
            scanner.SetOnError((message, _, _, _) => errors.Add(message.Code()));
            scanner.SetText(source);
            Equal(Kind.StringLiteral, scanner.Scan(), "Recovery token kind");
            Equal("ab", scanner.TokenValueByteString(), "Recovery excludes line break");
            Equal(3, scanner.TokenEnd(), "Recovery byte end excludes line break");
            Require((scanner.TokenFlags() & TokenFlags.Unterminated) != 0, "Recovery flag");
            Require(errors.SequenceEqual(new[] { 1002 }), "Exactly the unterminated-string diagnostic");
        }
    }

    private static void Numbers()
    {
        foreach (var (source, expected, invalid) in new[]
        {
            ("1_234", "1234", false), ("1_2_3.4_5", "123.45", false),
            ("1__2", "12", true), ("12_", "12", true)
        })
        {
            var scanner = new ScannerApi(); var errors = new List<int>();
            scanner.SetOnError((message, _, _, _) => errors.Add(message.Code()));
            scanner.SetText(source);
            Equal(Kind.NumericLiteral, scanner.Scan(), "Numeric token kind");
            Equal(expected, scanner.TokenValueByteString(), "Normalized numeric spelling");
            Equal(source, scanner.TokenTextByteString(), "Original numeric spelling");
            Equal(source.Length, scanner.TokenEnd(), "Numeric byte bounds");
            Require((scanner.TokenFlags() & TokenFlags.ContainsSeparator) != 0, "Separator flag");
            Equal(invalid, (scanner.TokenFlags() & TokenFlags.ContainsInvalidSeparator) != 0, "Invalid separator flag");
            Equal(invalid, errors.Count != 0, "Invalid separator diagnostics preserved");
        }
    }

    private static List<Node> TemplateParts(byte[] source)
    {
        var file = Parser.ParseSourceFile(new("/template.ts", "/template.ts"), source, ScriptKind.TS);
        var parts = new List<Node>();
        void Visit(Node node)
        {
            if (node.Kind is Kind.TemplateHead or Kind.TemplateMiddle or Kind.TemplateTail) parts.Add(node);
            node.ForEachChild(child => { Visit(child); return false; });
        }
        Visit(file.AsNode());
        return parts;
    }

    private static void Templates()
    {
        var parts = TemplateParts("const value = `a\\n${1}b${2}c`;"u8.ToArray());
        Require(parts.Select(x => x.Kind).SequenceEqual(new[] { Kind.TemplateHead, Kind.TemplateMiddle, Kind.TemplateTail }), "Template part order");
        Require(parts.Select(x => x.RawText()).SequenceEqual(new[] { "a\\n", "b", "c" }), "Raw AST text excludes delimiters exactly once");
        Require(parts.Select(x => x.Text()).SequenceEqual(new[] { "a\n", "b", "c" }), "Cooked AST text remains distinct");
        byte[] malformed = "const value = `a${1}"u8.ToArray().Concat(new byte[] { 0xff, 0xed, 0xa0, 0x80 }).ToArray();
        var recovery = TemplateParts(malformed);
        Equal(2, recovery.Count, "Unterminated template still has head and tail");
        Equal(Kind.TemplateTail, recovery[1].Kind, "Recovery tail kind");
        Equal("\u00ff\u00ed\u00a0\u0080", recovery[1].RawText(), "Unterminated tail keeps all malformed raw bytes without closing-delimiter subtraction");
        Equal(recovery[1].RawText(), recovery[1].Text(), "Unescaped malformed cooked bytes unchanged");
        Require((recovery[1].AsTemplateTail().TemplateFlags & TokenFlags.Unterminated) != 0, "Unterminated template flag");
    }
}

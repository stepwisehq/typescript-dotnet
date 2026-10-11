// Authored equivalence checks for Go strings byte comparisons, not upstream tests.
// Source semantics: strings.HasPrefix/HasSuffix/Index and byte sequence equality;
// scanner.go:357–397 and parser.go:6500–6622 retain their original consumers.
using System.Runtime.InteropServices;
using TypeScript.Scanner;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.Foundation;

public static class ByteSpanComparisonChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    ["binary-and-empty", "raw-string-equivalence", "raw-versus-unicode", "borrowed-suffix",
     "scanner-static-patterns", "parser-static-patterns"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "binary-and-empty": Binary(); break;
            case "raw-string-equivalence": Equivalence(); break;
            case "raw-versus-unicode": RawRoles(); break;
            case "borrowed-suffix": BorrowedSuffix(); break;
            case "scanner-static-patterns": ScannerPatterns(); break;
            case "parser-static-patterns": ParserPatterns(); break;
            default: throw new ArgumentException("Unknown byte comparison check: " + name, nameof(name));
        }
    }
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private static void Equal<T>(T expected, T actual, string message) =>
        Require(EqualityComparer<T>.Default.Equals(expected, actual), message);

    private static void Binary()
    {
        ReadOnlySpan<byte> value = [0x61, 0, 0xff, 0xc3, 0xa9, 0x61];
        Require(GoStrings.HasPrefix(value, [0x61, 0]), "NUL is an ordinary prefix byte");
        Require(GoStrings.HasSuffix(value, [0xc3, 0xa9, 0x61]), "Non-ASCII suffix preserves encoded bytes");
        Equal(2, GoStrings.Index(value, [0xff, 0xc3]), "Malformed byte is ordinary search data");
        Equal(-1, GoStrings.Index(value, [0xff, 0xff]), "Missing byte pattern");
        Require(!GoStrings.HasPrefix(value, [0x61, 0, 0xff, 0xc3, 0xa9, 0x61, 0]), "Overlong prefix fails");
        Require(GoStrings.HasPrefix(value, ReadOnlySpan<byte>.Empty) && GoStrings.HasSuffix(value, ReadOnlySpan<byte>.Empty), "Empty patterns match");
        Equal(0, GoStrings.Index(value, ReadOnlySpan<byte>.Empty), "Empty search starts at zero");
        Equal(0, GoStrings.Index(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty), "Empty haystack and pattern");
        Require(GoStrings.Equal(value, [0x61, 0, 0xff, 0xc3, 0xa9, 0x61]), "Exact sequence equality");
        Require(!GoStrings.Equal(value, [0x61, 0, 0xff, 0xc3, 0xa9]), "Equality requires identical lengths");
        Require(!GoStrings.Equal(value, [0x61, 0, 0xff, 0xc3, 0xa8, 0x61]), "Equality compares every byte");
    }

    private static void Equivalence()
    {
        // An independent ordinal raw-string reference also covers potential SIMD
        // boundaries. Byte values cover the full 0..255 range without UTF8 decoding.
        int[] lengths = [0, 1, 15, 16, 31, 32, 63, 64, 191, 192, 193, 257];
        foreach (int length in lengths)
        {
            byte[] bytes = Enumerable.Range(0, length).Select(i => unchecked((byte)(i * 137 + 17))).ToArray();
            string raw = GoString.FromBytes(bytes);
            byte[][] patterns = [[], bytes, [0], [0xff], bytes.Take(length / 2).ToArray(),
                bytes.Skip(length / 2).ToArray(), [0xff, 0, 0x80, 0xed, 0xa0, 0x80]];
            foreach (byte[] pattern in patterns)
            {
                string needle = GoString.FromBytes(pattern);
                Equal(raw.StartsWith(needle, StringComparison.Ordinal), GoStrings.HasPrefix(bytes.AsSpan(), pattern.AsSpan()), "Byte prefix agrees with ordinal raw-string reference");
                Equal(raw.EndsWith(needle, StringComparison.Ordinal), GoStrings.HasSuffix(bytes.AsSpan(), pattern.AsSpan()), "Byte suffix agrees with ordinal raw-string reference");
                Equal(raw.IndexOf(needle, StringComparison.Ordinal), GoStrings.Index(bytes.AsSpan(), pattern.AsSpan()), "Byte search agrees with ordinal raw-string reference");
                Equal(raw == needle, GoStrings.Equal(bytes.AsSpan(), pattern.AsSpan()), "Byte equality agrees with ordinal raw-string reference");
                Equal(GoStrings.HasPrefix(bytes.AsSpan(), needle), GoStrings.HasPrefix(bytes.AsSpan(), pattern.AsSpan()), "Mixed raw prefix keeps existing behavior");
                Equal(GoStrings.HasSuffix(bytes.AsSpan(), needle), GoStrings.HasSuffix(bytes.AsSpan(), pattern.AsSpan()), "Mixed raw suffix keeps existing behavior");
                Equal(GoStrings.Index(bytes.AsSpan(), needle), GoStrings.Index(bytes.AsSpan(), pattern.AsSpan()), "Mixed raw search keeps existing behavior");
            }
        }

        // Raw byte-character membership is not ASCII or Unicode encoding. Use
        // independent CLR characters to exercise every allowed value and the
        // actual concatenation, builder and strict ingress branches. Retained
        // Latin1 key hash coherence is covered by the existing AstTextValue
        // Lookup case; the nonbyte ordinal fallback is checked below.
        string latin1 = new(Enumerable.Range(0, 256).Select(i => (char)i).ToArray());
        byte[] allBytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        var rawValue = ByteStringValue.FromString(latin1);
        var byteValue = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(allBytes));
        Require(GoString.IsByteText(ReadOnlySpan<char>.Empty), "Empty raw text has no invalid character");
        Require(ByteStringValue.Concat(default, default).Length == 0, "Empty fixed concatenation retains Go empty text");
        Equal(0, FileSpec.FromGoString("").AsValue().Length, "Empty typed input remains valid empty text");
        Require(GoString.IsByteText(latin1.AsSpan()), "NUL, control, DEL and every high Latin1 character are byte text");
        Require(FileSpec.FromGoString(latin1).AsValue().EqualsValue(byteValue), "String typed ingress accepts all256 raw byte values");
        Require(FileSpec.FromValue(rawValue).AsValue().EqualsValue(byteValue), "Descriptor typed ingress accepts the same raw byte role");
        var byteConcat = ByteStringValue.Concat(rawValue, byteValue);
        Require(byteConcat.IsBytes && byteConcat.EqualsString(latin1 + latin1), "Mixed fixed concatenation retains exact byte content");
        var byteBuilder = new GoBuilder();
        byteBuilder.WriteString(latin1);
        Require(byteBuilder.Value().IsBytes && byteBuilder.Value().EqualsValue(byteValue), "Byte builder accepts high Latin1 without Unicode conversion");

        foreach (string nonByte in new[] { "\u0100", "\uffff", "\ud800", "\udfff", "\ud83d\ude00" })
        {
            Require(!GoString.IsByteText(nonByte.AsSpan()), "Nonbyte BMP and paired/unpaired surrogate units fail byte membership");
            bool stringRejected = false;
            try { _ = FileSpec.FromGoString(nonByte); }
            catch (ArgumentException) { stringRejected = true; }
            Require(stringRejected, "Public raw typed ingress rejects nonbyte characters");
            bool descriptorRejected = false;
            try { _ = FileSpec.FromValue(ByteStringValue.FromString(nonByte)); }
            catch (ArgumentException) { descriptorRejected = true; }
            Require(descriptorRejected, "Descriptor typed ingress preserves strict rejection");

            var legacyKey = ByteStringKey.FromString(nonByte);
            var sameLegacyKey = ByteStringKey.FromValue(ByteStringValue.FromString(new string(nonByte.AsSpan())));
            Require(legacyKey.Equals(sameLegacyKey), "Legacy key comparison retains nonbyte character content");
            Equal(string.GetHashCode(nonByte.AsSpan(), StringComparison.Ordinal), legacyKey.GetHashCode(), "Nonbyte key hashing uses the existing ordinal role");
            Equal(legacyKey.GetHashCode(), sameLegacyKey.GetHashCode(), "Legacy key hash is independent of string owner");

            var suffix = ByteStringValue.FromString("\u00ff");
            var legacyConcat = ByteStringValue.Concat(byteValue, ByteStringValue.FromString(nonByte), suffix);
            Require(!legacyConcat.IsBytes && legacyConcat.EqualsString(latin1 + nonByte + "\u00ff"), "Fixed concatenation preserves legacy UTF16 instead of encoding or truncating");
            var soleLegacy = ByteStringValue.Concat(default, ByteStringValue.FromString(nonByte));
            Require(!soleLegacy.IsBytes && soleLegacy.EqualsString(nonByte), "Sole nonempty legacy operand retains its original role");
            var legacyBuilder = new GoBuilder();
            legacyBuilder.WriteString(byteValue);
            var publishedBytes = legacyBuilder.Value();
            legacyBuilder.WriteString(nonByte);
            legacyBuilder.WriteString(suffix);
            Require(!legacyBuilder.Value().IsBytes && legacyBuilder.Value().EqualsString(latin1 + nonByte + "\u00ff"), "Builder switches to legacy UTF16 and retains following raw bytes");
            Require(publishedBytes.IsBytes && publishedBytes.EqualsValue(byteValue), "Legacy transition leaves an earlier published byte prefix unchanged");
        }
    }

    private static void RawRoles()
    {
        ReadOnlySpan<byte> utf8 = "é"u8;
        Require(GoStrings.HasPrefix(utf8, "é"u8), "u8 literal supplies actual UTF8 bytes");
        Require(GoStrings.HasPrefix(utf8, "\u00c3\u00a9"), "Dynamic raw string already contains encoded byte characters");
        Require(!GoStrings.HasPrefix(utf8, "é"), "Dynamic raw string is not implicitly Unicode-encoded");
        Require(GoStrings.HasPrefix(new byte[] { 0xe9 }.AsSpan(), "é"), "Single raw byte matches same-valued string character");
        Require(!GoStrings.HasPrefix(new byte[] { 0 }.AsSpan(), "\u0100"), "Mixed comparison does not truncate an out-of-byte character");
        Equal(-1, GoStrings.Index(new byte[] { 0xc4, 0x80 }.AsSpan(), "\u0100"), "Mixed search does not encode Unicode input");
    }

    private static void BorrowedSuffix()
    {
        byte[] owner = "prefix-comment*/suffix"u8.ToArray();
        var value = ByteStringView.FromOwnedBytes(owner).Slice(7, 16);
        var trimmed = GoStrings.TrimSuffix(value, "*/"u8);
        Require(MemoryMarshal.TryGetArray(trimmed.Memory, out var memory) && ReferenceEquals(owner, memory.Array), "Suffix trim retains source owner");
        Equal(7, memory.Offset, "Trim keeps existing offset");
        Equal(7, trimmed.Length, "Trim removes only matching suffix bytes");
        Equal("comment", trimmed.Materialize(), "Trimmed contents");
        Equal(value.Materialize(), GoStrings.TrimSuffix(value, "x*/"u8).Materialize(), "Nonmatching suffix leaves contents unchanged");
        Equal(value.Materialize(), GoStrings.TrimSuffix(value, ReadOnlySpan<byte>.Empty).Materialize(), "Empty suffix leaves contents unchanged");
    }

    private static void ScannerPatterns()
    {
        foreach (string tag in new[] { "deprecated", "see", "link", "linkcode", "linkplain" })
        {
            foreach (char terminator in new[] { ' ', '\t', '\n', '\r', '}', '*' })
            {
                var scanner = new ScannerApi(); scanner.SetSkipTrivia(true);
                scanner.SetText("/** @" + tag + terminator + " text */ const value = 1;");
                Equal(Kind.ConstKeyword, scanner.Scan(), "JSDoc tag lookahead retains next token");
                Equal(tag == "deprecated", scanner.HasPrecedingJSDocWithDeprecatedTag(), "Deprecated tag flag");
                Equal(tag != "deprecated", scanner.HasPrecedingJSDocWithSeeOrLink(), "See/link tag flag");
            }
        }
        var invalid = new ScannerApi(); invalid.SetSkipTrivia(true);
        invalid.SetText("/** @deprecatedExtra @seeExtra @linkExtra @linkcodeExtra @linkplainExtra */ const value = 1;");
        Equal(Kind.ConstKeyword, invalid.Scan(), "Invalid tag prefixes retain token");
        Require(!invalid.HasPrecedingJSDocWithDeprecatedTag() && !invalid.HasPrecedingJSDocWithSeeOrLink(), "Prefix alone does not make a valid JSDoc tag");
        var directives = new ScannerApi(); directives.SetSkipTrivia(true);
        directives.SetText("// @ts-ignore\n// @ts-expect-error\nconst value = 1;");
        Equal(Kind.ConstKeyword, directives.Scan(), "Comment directive lookahead token");
        var found = directives.CommentDirectives()!;
        Equal(2, found.Length, "Both static directive patterns");
        Equal(CommentDirectiveKind.Ignore, found[0].Kind, "Ignore directive order");
        Equal(CommentDirectiveKind.ExpectError, found[1].Kind, "Expect directive order");
    }

    private static void ParserPatterns()
    {
        var file = Parser.ParseSourceFile(new("/patterns.ts", "/patterns.ts"),
            "/** @jsx h */\n/// <reference path=\"café.ts\" />\nconst value = 1;", ScriptKind.TS);
        Equal(0, file.Diagnostics().Length, "Static pragma patterns parse without errors");
        var jsx = GoSlices.Values(file.Pragmas).Single(x => x.Name == "jsx");
        Equal("h", jsx.Args[ByteStringKey.FromString("factory")].Value, "Multiline pragma search and suffix trim");
        var reference = GoSlices.Values(file.Pragmas).Single(x => x.Name == "reference");
        Equal(GoString.ToUtf8("café.ts"), reference.Args[ByteStringKey.FromString("path")].Value, "Pragma attribute remains raw-byte text");
        var text = ByteStringView.CopyFrom("x import('a'); require('b');"u8);
        Equal((2, 6), Ast.findImportOrRequire(text, 0), "Import exact static pattern");
        Equal((15, 7), Ast.findImportOrRequire(text, 8), "Require exact static pattern");
        Equal((-1, 0), Ast.findImportOrRequire(ByteStringView.CopyFrom("imporX requirX"u8), 0), "Near matches fail");
    }
}

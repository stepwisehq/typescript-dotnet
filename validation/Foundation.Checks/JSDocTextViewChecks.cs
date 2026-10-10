// Authored regression checks for immutable Go-byte-string views, not upstream test ports.
// Parser/scanner behavior follows pinned parser/jsdoc.go:parseJSDocComment and
// scanner/scanner.go:SetText, Mark, Rewind. Original assertion bodies are reused below.
using System.Runtime.InteropServices;
using TypeScript.Scanner;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.Foundation;

public static class JSDocTextViewChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    [
        "view-sharing", "view-empty", "view-byte-preservation", "view-bounds",
        "scanner-prefix-bound", "scanner-offset-bound", "scanner-unicode-offsets",
        "scanner-malformed-comment", "scanner-restored-text",
        "parser-lazy-typescript", "parser-eager-javascript", "parser-eager-link",
        "parser-multiple-comments", "parser-malformed-comment",
        "original-jsdoc-source-reparse", "original-jsdoc-constructed-reparse",
        "original-jsdoc-import-parents"
    ];

    public static void Run(string name)
    {
        switch (name)
        {
            case "view-sharing": Sharing(); break;
            case "view-empty": Empty(); break;
            case "view-byte-preservation": Bytes(); break;
            case "view-bounds": Bounds(); break;
            case "scanner-prefix-bound": ScannerPrefix(); break;
            case "scanner-offset-bound": ScannerOffset(); break;
            case "scanner-unicode-offsets": ScannerUnicode(); break;
            case "scanner-malformed-comment": ScannerMalformed(); break;
            case "scanner-restored-text": ScannerRestored(); break;
            case "parser-lazy-typescript": ParserLazy(); break;
            case "parser-eager-javascript": ParserEager(ScriptKind.JS, false); break;
            case "parser-eager-link": ParserEager(ScriptKind.TS, true); break;
            case "parser-multiple-comments": ParserMultiple(); break;
            case "parser-malformed-comment": ParserMalformed(); break;
            case "original-jsdoc-source-reparse": ParserAstChecks.Run("TestJSDocTypeSourceSurvivesReparse"); break;
            case "original-jsdoc-constructed-reparse": ParserAstChecks.Run("TestJSDocTypeSourcePropagatesToConstructedReparse"); break;
            case "original-jsdoc-import-parents": ParserAstChecks.Run("TestJSDocImportTypeParentChain"); break;
            default: throw new ArgumentException("Unknown text-view check: " + name, nameof(name));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T expected, T actual, string message) =>
        Require(EqualityComparer<T>.Default.Equals(expected, actual), message);

    private static void Sharing()
    {
        byte[] backing = "prefix-abcdef-suffix"u8.ToArray();
        ByteStringView full = ByteStringView.FromOwnedBytes(backing);
        var middle = full.Slice(7, 13);
        var nested = middle.Slice(2, 5);
        Require(MemoryMarshal.TryGetArray(nested.Memory, out var actual), "Byte-array-backed view");
        Require(ReferenceEquals(backing, actual.Array), "Nested slice shares original owned backing");
        Equal(9, actual.Offset, "Nested byte offset");
        Equal(3, actual.Count, "Nested byte length");
        Equal('c', nested[0], "Nested first byte");
        Equal("cde", nested.Materialize(), "Nested materialization");
        Equal("prefix-abcdef-suffix", full.Materialize(), "Explicit raw-string materialization preserves bytes");
        Require(MemoryMarshal.TryGetArray(full.Memory, out var whole) && ReferenceEquals(backing, whole.Array), "Materialization leaves owned source backing unchanged");
        Equal("bc", GoString.FromBytes(middle.AsSpan(1, 2)), "Bounded span");
        Equal("def", GoString.FromBytes(middle.AsSpan(3)), "Suffix span");
        Equal(6, middle.Length, "Slicing does not change original header");
    }

    private static void Empty()
    {
        ByteStringView nil = default;
        ByteStringView empty = string.Empty;
        Require(nil.IsDefault && !empty.IsDefault, "Default and explicit empty headers remain distinct");
        Equal(0, nil.Length, "Default length");
        Equal(0, empty.Length, "Empty length");
        Require(nil.AsSpan().IsEmpty && empty.AsSpan().IsEmpty, "Empty spans");
        Equal(string.Empty, nil.Materialize(), "Default materialization");
        Equal(string.Empty, nil.Slice(0, 0).Materialize(), "Default empty slice");
        ByteStringView full = "abc";
        var end = full.Slice(3, 3);
        Require(!end.IsDefault && end.AsSpan().IsEmpty, "Empty suffix retains header backing");
        Equal(0, end.Length, "Empty suffix length");
    }

    private static void Bytes()
    {
        // Includes two-/four-byte UTF-8, a lone-surrogate WTF-8 sequence and invalid bytes.
        byte[] source = [0x61, 0xc3, 0xa9, 0xf0, 0x9f, 0xa6, 0x80, 0xed, 0xa0, 0x80, 0xff, 0xc0, 0x62];
        ByteStringView full = ByteStringView.CopyFrom(source);
        var body = full.Slice(1, 12);
        Require(source.AsSpan(1, 11).SequenceEqual(GoString.Bytes(body.Materialize())), "Materialization preserves raw bytes without decoding");
        Equal(11, body.Length, "Length is bytes, not UTF-16 or Unicode scalars");
        Equal((char)0xff, body[9], "Invalid byte preserved");
        Equal(GoString.FromBytes(source.AsSpan(7, 3)), body.Substring(6, 3), "WTF-8 bytes preserved");
        Require(MemoryMarshal.TryGetArray(body.Memory, out var memory), "Raw-byte backing");
        Equal(1, memory.Offset, "Raw byte offset");
        Equal(11, memory.Count, "Raw byte memory length");
    }

    private static void Reject(Action operation)
    {
        try { operation(); }
        catch (ArgumentOutOfRangeException) { return; }
        throw new InvalidOperationException("Invalid view range was accepted");
    }

    private static void Bounds()
    {
        ByteStringView backing = "prefix-abc-suffix";
        var v = backing.Slice(7, 10);
        Reject(() => _ = v[-1]);
        Reject(() => _ = v[3]);
        Reject(() => _ = v.Slice(-1));
        Reject(() => _ = v.Slice(4));
        Reject(() => _ = v.Slice(0, 4));
        Reject(() => _ = v.Slice(2, 1));
        Reject(() => _ = v.AsSpan(-1).Length);
        Reject(() => _ = v.AsSpan(4).Length);
        Reject(() => _ = v.AsSpan(1, 3).Length);
        Reject(() => _ = v.Substring(1, 3));
        Reject(() => _ = v.Substring(int.MaxValue, 1));
        Reject(() => _ = v.Substring(1, int.MaxValue));
        Equal(string.Empty, v.Substring(3, 0), "Empty at logical end is valid");
    }

    private static void ScannerPrefix()
    {
        const string source = "/** @deprecated */ const visible = 1; leaked";
        ByteStringView full = source;
        int end = source.IndexOf(';') + 1;
        var scanner = new ScannerApi();
        scanner.SetByteTextInternal(full.Slice(0, end));
        Equal(Kind.ConstKeyword, scanner.Scan(), "Token after JSDoc");
        Require(scanner.HasPrecedingJSDocComment() && scanner.HasPrecedingJSDocWithDeprecatedTag(), "JSDoc flags survive bounded view");
        Equal(source.IndexOf("const", StringComparison.Ordinal), scanner.TokenStart(), "Prefix token offset");
        Equal(Kind.Identifier, scanner.Scan(), "Visible identifier");
        Equal("visible", scanner.TokenValue(), "Visible identifier spelling");
        Equal(Kind.EqualsToken, scanner.Scan(), "Equals");
        Equal(Kind.NumericLiteral, scanner.Scan(), "Numeric literal");
        Equal(Kind.SemicolonToken, scanner.Scan(), "Semicolon");
        Equal(Kind.EndOfFile, scanner.Scan(), "Backing suffix is not scanned");
        Equal(end, scanner.TokenEnd(), "Logical end");
        Equal(source[..end], scanner.Text(), "Text exposes logical prefix");
    }

    private static void ScannerOffset()
    {
        ByteStringView full = "carrier left ignored";
        var scanner = new ScannerApi();
        scanner.SetByteTextInternal(full.Slice(8, 12));
        Equal(Kind.Identifier, scanner.Scan(), "Offset view token");
        Equal("left", scanner.TokenValue(), "Offset view spelling");
        Equal(0, scanner.TokenStart(), "Offsets are relative to visible view");
        Equal(4, scanner.TokenEnd(), "Offset view end");
        Equal(Kind.EndOfFile, scanner.Scan(), "Offset view does not expose suffix");
        Equal("left", scanner.Text(), "Public text excludes carrier");
    }

    private static void ScannerUnicode()
    {
        ByteStringView full = GoString.ToUtf8("é café ignored");
        var scanner = new ScannerApi();
        scanner.SetByteTextInternal(full.Slice(0, 8));
        Equal(Kind.Identifier, scanner.Scan(), "Unicode first token");
        Equal("é", scanner.TokenValue(), "Unicode first spelling");
        Equal(2, scanner.TokenEnd(), "Two-byte first identifier");
        Equal(Kind.Identifier, scanner.Scan(), "Unicode second token");
        Equal("café", scanner.TokenValue(), "Unicode second spelling");
        Equal(3, scanner.TokenStart(), "Second identifier byte offset");
        Equal(8, scanner.TokenEnd(), "Second identifier byte end");
        Equal(Kind.EndOfFile, scanner.Scan(), "Unicode bounded end");
    }

    private static void ScannerMalformed()
    {
        ByteStringView full = "prefix" + GoString.FromBytes([0x2f, 0x2a, 0xff, 0x2a, 0x2f]) + "abc suffix";
        var scanner = new ScannerApi();
        scanner.SetByteTextInternal(full.Slice(6, 14));
        Equal(Kind.Identifier, scanner.Scan(), "Token after malformed byte in comment");
        Equal("abc", scanner.TokenValue(), "Malformed-comment trailing token");
        Equal(5, scanner.TokenStart(), "Comment consumes exactly five bytes");
        Equal(8, scanner.TokenEnd(), "Malformed-comment byte end");
        Equal(Kind.EndOfFile, scanner.Scan(), "Malformed-comment logical end");
    }

    private static void ScannerRestored()
    {
        const string source = "/** @type {number} */ var after = 1;";
        ByteStringView full = source;
        var scanner = new ScannerApi();
        scanner.SetByteTextInternal(full);
        Equal(Kind.VarKeyword, scanner.Scan(), "Outer token");
        var state = scanner.Mark();
        int savedStart = scanner.TokenStart(), savedEnd = scanner.TokenEnd(), savedFullStart = scanner.TokenFullStart();
        var savedFlags = scanner.TokenFlags();
        scanner.SetByteTextInternal(full.Slice(0, source.IndexOf("*/", StringComparison.Ordinal)));
        scanner.ResetPos(3);
        Equal(Kind.WhitespaceTrivia, scanner.ScanJSDocToken(), "Inner JSDoc whitespace");
        Equal(Kind.AtToken, scanner.ScanJSDocToken(), "Inner JSDoc token");
        // This is the source parseJSDocComment ordering: restore text before Rewind.
        scanner.SetByteTextInternal(full);
        scanner.Rewind(state);
        Equal(Kind.VarKeyword, scanner.Token(), "Restored outer token");
        Equal(savedStart, scanner.TokenStart(), "Restored start");
        Equal(savedEnd, scanner.TokenEnd(), "Restored end");
        Equal(savedFullStart, scanner.TokenFullStart(), "Restored full start");
        Equal(savedFlags, scanner.TokenFlags(), "Restored flags");
        Equal(Kind.Identifier, scanner.Scan(), "Scan continues after saved token");
        Equal("after", scanner.TokenValue(), "Restored text suffix available");
    }

    private static SourceFile Parse(byte[] source, ScriptKind kind) =>
        Parser.ParseSourceFile(new("/text-view." + (kind == ScriptKind.JS ? "js" : "ts"), "/text-view"), source, kind);

    private static SourceFile Parse(string source, ScriptKind kind) => Parse(GoString.Bytes(GoString.ToUtf8(source)), kind);

    private static void NoDiagnostics(SourceFile file)
    {
        Equal(0, file.Diagnostics()?.Count ?? 0, "Syntax diagnostics");
        Equal(0, file.JSDocDiagnostics()?.Count ?? 0, "JSDoc diagnostics");
    }

    private static void TrailingStatement(SourceFile file, string source)
    {
        NoDiagnostics(file);
        Equal(2, file.Statements.Nodes.Count, "Statement count after JSDoc restore");
        var last = file.Statements.Nodes[1];
        Equal(Kind.VariableStatement, last.Kind, "Trailing ordinary statement");
        Equal(source.IndexOf("const after", StringComparison.Ordinal), ScannerApi.GetTokenPosOfNode(last, file, false), "Trailing statement byte position");
        Equal(source, file.Text(), "Full source remains intact");
    }

    private static void ParserLazy()
    {
        const string source = "/** @deprecated */\nfunction f() {}\nconst after = 2;";
        var file = Parse(source, ScriptKind.TS);
        var function = file.Statements.Nodes[0];
        Require((function.Flags & NodeFlags.HasJSDoc) != 0, "Lazy flag");
        Require(function.EagerJSDoc(file) is null, "TS comment is not eagerly parsed");
        var docs = function.JSDoc(file);
        Equal(1, docs.Count, "Lazy JSDoc count");
        Require(ReferenceEquals(function, docs[0].Parent), "Lazy JSDoc parent");
        Equal(Kind.JSDocDeprecatedTag, docs[0].AsJSDoc().Tags.Nodes[0].Kind, "Lazy tag kind");
        Require(ReferenceEquals(docs, function.JSDoc(file)) && ReferenceEquals(docs, function.EagerJSDoc(file)), "Lazy parse cached");
        TrailingStatement(file, source);
    }

    private static void ParserEager(ScriptKind kind, bool link)
    {
        string source = (link ? "/** See {@link after} */" : "/** @deprecated */") + "\nfunction f() {}\nconst after = 2;";
        var file = Parse(source, kind);
        var function = file.Statements.Nodes[0];
        var eager = function.EagerJSDoc(file);
        Require(eager is not null, "JS and TS link comments parse eagerly");
        Equal(1, eager!.Count, "Eager JSDoc count");
        Require(ReferenceEquals(function, eager[0].Parent), "Eager parent");
        Require(ReferenceEquals(eager, function.JSDoc(file)), "Eager cache reused");
        TrailingStatement(file, source);
    }

    private static void ParserMultiple()
    {
        const string first = "/** @deprecated */";
        const string second = "/** @see after */";
        string source = first + "\n" + second + "\nfunction f() {}\nconst after = 2;";
        var file = Parse(source, ScriptKind.JS);
        var function = file.Statements.Nodes[0];
        var docs = function.EagerJSDoc(file);
        Equal(2, docs.Count, "Multiple comments parsed independently");
        Equal(first.Length, docs[0].End(), "First comment bound");
        Equal(first.Length + 1 + second.Length, docs[1].End(), "Second comment bound");
        Require(docs.All(d => ReferenceEquals(d.Parent, function)), "Each comment parent restored");
        TrailingStatement(file, source);
    }

    private static void ParserMalformed()
    {
        string source = GoString.ToUtf8("/** café 🦀") + GoString.FromBytes([0xff]) + " @deprecated */\nfunction f() {}\nconst after = 2;";
        var file = Parse(GoString.Bytes(source), ScriptKind.TS);
        var function = file.Statements.Nodes[0];
        Require(function.EagerJSDoc(file) is null, "Malformed-byte TS prose still lazy");
        var docs = function.JSDoc(file);
        Equal(1, docs.Count, "Malformed-byte comment count");
        Equal(source.IndexOf("*/", StringComparison.Ordinal) + 2, docs[0].End(), "UTF-8/malformed-byte JSDoc end");
        Require(ReferenceEquals(docs, function.JSDoc(file)), "Malformed-byte cache stable");
        TrailingStatement(file, source);
    }
}

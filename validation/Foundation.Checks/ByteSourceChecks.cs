// Authored byte-ingress compatibility checks, not upstream test ports.
// Source contracts: Go unicode/utf8 decoding; parser.ParseSourceFile/scanner.SetText
// immutable string inputs; ast.SourceFile byte positions and source lifetime.
using System.Runtime.InteropServices;
using TypeScript.Scanner;
using TypeScript.Vfs;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.Foundation;

public static class ByteSourceChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    ["owned-and-copied-ingress", "parser-public-byte-isolation", "scanner-public-byte-isolation",
     "unicode-string-ingress", "utf8-rune-decoding", "wtf8-js-rune-decoding",
     "byte-positions-and-utf16", "source-clone-and-update", "go-string-range",
     "vfs-bom-boundary", "vfs-utf16-decoding", "source-text-fs-transport"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "owned-and-copied-ingress": Ownership(); break;
            case "parser-public-byte-isolation": ParserIsolation(); break;
            case "scanner-public-byte-isolation": ScannerIsolation(); break;
            case "unicode-string-ingress": UnicodeIngress(); break;
            case "utf8-rune-decoding": DecodeUtf8(); break;
            case "wtf8-js-rune-decoding": DecodeWtf8(); break;
            case "byte-positions-and-utf16": Positions(); break;
            case "source-clone-and-update": SourceLifecycle(); break;
            case "go-string-range": Range(); break;
            case "vfs-bom-boundary": BomBoundary(); break;
            case "vfs-utf16-decoding": Utf16Files(); break;
            case "source-text-fs-transport": FileTransport(); break;
            default: throw new ArgumentException("Unknown byte-source check: " + name, nameof(name));
        }
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private static void Equal<T>(T expected, T actual, string message) =>
        Require(EqualityComparer<T>.Default.Equals(expected, actual), message);
    private static void Bytes(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual, string message) =>
        Require(expected.SequenceEqual(actual), message);
    private static SourceFile Parse(byte[] source) => Parser.ParseSourceFile(new("/source.ts", "/source.ts"), source, ScriptKind.TS);

    private static void Ownership()
    {
        byte[] array = [0x78, 0xc3, 0xa9, 0xff, 0x79];
        var owned = ByteStringView.FromOwnedBytes(array);
        var middle = owned.Slice(1, 4);
        Require(MemoryMarshal.TryGetArray(middle.Memory, out var segment) && ReferenceEquals(array, segment.Array), "Owned ingress transfers original buffer without copying");
        Equal(1, segment.Offset, "Owned subview offset"); Equal(3, segment.Count, "Owned subview length");
        Bytes([0xc3, 0xa9, 0xff], middle.AsSpan(), "Subview raw bytes");
        byte[] external = array.ToArray();
        var copied = ByteStringView.CopyFrom(external);
        Require(MemoryMarshal.TryGetArray(copied.Memory, out var detached) && !ReferenceEquals(external, detached.Array), "External ingress defensively copies");
        external[0] = 0x7a; // Transferred buffers are never mutated; only caller-owned input is changed.
        Equal('x', copied[0], "Copied ingress independent of original caller array");
        Equal(GoString.FromBytes([0xc3, 0xa9, 0xff]), middle.Materialize(), "Raw materialization does not Unicode-decode malformed bytes");
        ByteStringView nil = default;
        Require(nil.IsDefault && nil.AsSpan().IsEmpty, "Default view retains nil header");
        Require(!ByteStringView.FromOwnedBytes([]).IsDefault, "Explicit empty owned view is nonnil");
    }

    private static void ParserIsolation()
    {
        byte[] input = "const café = \"🦀\";"u8.ToArray();
        byte[] original = input.ToArray();
        var file = Parse(input);
        Array.Fill(input, (byte)'!');
        Bytes(original, file.TextView().AsSpan(), "Parser immutable source survives caller mutation");
        Bytes(original, file.TextBytes(), "Public source bytes preserve encoded source");
        Equal("const café = \"🦀\";", file.DecodedText(), "Public decoded source is Unicode");
        Equal(0, file.Diagnostics().Length, "Parser diagnostics unchanged by mutation");
        byte[] returned = file.TextBytes(); Array.Fill(returned, (byte)'?');
        Bytes(original, file.TextView().AsSpan(), "Public TextBytes cannot mutate source");
        string raw = file.Text();
        Require(ReferenceEquals(raw, file.Text()), "SourceFile lazy raw-string projection is canonical");
        Equal(GoString.FromBytes(original), raw, "Public raw-string role remains one character per byte");
    }

    private static void ScannerIsolation()
    {
        byte[] input = "café 🦀"u8.ToArray(); byte[] original = input.ToArray();
        var scanner = new ScannerApi(); scanner.SetText(input);
        Array.Fill(input, (byte)'!');
        Equal(Kind.Identifier, scanner.Scan(), "Scanner retains immutable external byte input");
        Equal("café", scanner.TokenValue(), "Public token is decoded Unicode");
        Equal(5, scanner.TokenEnd(), "Scanner token position counts bytes");
        Bytes(original, scanner.TextBytes(), "Scanner public source bytes unchanged");
        var returned = scanner.TextBytes(); Array.Fill(returned, (byte)'?');
        Bytes(original, scanner.TextBytes(), "Scanner TextBytes returns independent array");
    }

    private static void UnicodeIngress()
    {
        const string unicode = "const café = \"🦀\";";
        byte[] expected = "const café = \"🦀\";"u8.ToArray();
        var view = ByteStringView.FromUnicode(unicode);
        Bytes(expected, view.AsSpan(), "Unicode ingress encodes UTF8 rather than truncating char values");
        var file = Parser.ParseSourceFile(new("/unicode.ts", "/unicode.ts"), unicode, ScriptKind.TS);
        Bytes(expected, file.TextBytes(), "Public string parser retains Unicode input contract");
        var surrogate = Parser.ParseSourceFile(new("/surrogate.ts", "/surrogate.ts"), "//\uD800", ScriptKind.TS);
        Bytes([0x2f, 0x2f, 0xef, 0xbf, 0xbd], surrogate.TextBytes(), "Existing public UTF16 ingress replaces isolated surrogate");
        Bytes([0xed, 0xa0, 0x80], ByteStringView.FromGoBytes("\u00ed\u00a0\u0080").AsSpan(), "Raw ingress preserves WTF8 rather than reencoding Unicode");
    }

    private static void DecodeUtf8()
    {
        (byte[] Bytes, int Rune, int Size)[] rows =
        [([], 0xfffd, 0), ([0x61], 0x61, 1), ([0xc3, 0xa9], 0xe9, 2),
         ([0xe2, 0x80, 0xa8], 0x2028, 3), ([0xf0, 0x9f, 0xa6, 0x80], 0x1f980, 4),
         ([0xf4, 0x8f, 0xbf, 0xbf], 0x10ffff, 4), ([0x80], 0xfffd, 1),
         ([0xc0, 0xaf], 0xfffd, 1), ([0xc3], 0xfffd, 1), ([0xe0, 0x9f, 0x80], 0xfffd, 1),
         ([0xed, 0xa0, 0x80], 0xfffd, 1), ([0xf4, 0x90, 0x80, 0x80], 0xfffd, 1), ([0xff], 0xfffd, 1)];
        foreach (var row in rows)
        {
            Equal((row.Rune, row.Size), Utf8.DecodeRuneInString(row.Bytes.AsSpan()), "Byte decoder exact Go rune and consumed width");
            Equal((row.Rune, row.Size), Utf8.DecodeRuneInString(GoString.FromBytes(row.Bytes).AsSpan()), "Legacy raw-string decoder agrees");
        }
        Equal((0xe9, 2), Utf8.DecodeLastRuneInString(new byte[] { 0x61, 0xc3, 0xa9 }.AsSpan()), "Reverse decoder valid suffix");
        Equal((0xfffd, 1), Utf8.DecodeLastRuneInString(new byte[] { 0xc3 }.AsSpan()), "Reverse decoder invalid trailing byte");
        Equal((0xfffd, 0), Utf8.DecodeLastRuneInString(ReadOnlySpan<byte>.Empty), "Reverse empty decoder");
    }

    private static void DecodeWtf8()
    {
        byte[] high = [0xed, 0xa0, 0x80], low = [0xed, 0xbf, 0xbf];
        Equal((0xd800, 3), StringUtil.DecodeJSStringRune(high.AsSpan()), "JS decoder accepts encoded lone high surrogate");
        Equal((0xdfff, 3), StringUtil.DecodeJSStringRune(low.AsSpan()), "JS decoder accepts encoded lone low surrogate");
        Equal((0xfffd, 1), Utf8.DecodeRuneInString(high.AsSpan()), "Ordinary UTF8 decoder rejects same WTF8 bytes");
        Equal((0xfffd, 1), StringUtil.DecodeJSStringRune(new byte[] { 0xed, 0xa0 }.AsSpan()), "Truncated WTF8 still consumes one byte");
        var scanner = new ScannerApi(); scanner.SetText(new byte[] { 0x22, 0xed, 0xa0, 0x80, 0x22 });
        Equal(Kind.StringLiteral, scanner.Scan(), "WTF8 token scans as string");
        Bytes(high, scanner.TokenValueBytes(), "WTF8 token value byte output remains exact");
        Equal("\uD800", scanner.TokenValue(), "Public JS token keeps isolated UTF16 surrogate");
    }

    private static void Positions()
    {
        var file = Parse("//à🦀\nconst café = 1;"u8.ToArray());
        var map = file.GetPositionMap();
        var declaration = file.Statements.Nodes[0].AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes[0];
        Equal(14, declaration.Name().Pos(), "Identifier full-start position includes leading trivia in byte offsets");
        Equal(15, ScannerApi.SkipTriviaByteString(file.TextView(), declaration.Name().Pos()), "Identifier token start skips trivia in byte offsets");
        Equal((1, 6), ScannerApi.GetECMALineAndUTF16CharacterOfPosition(file, 15), "Line and UTF16 column at identifier");
        Equal(6, map.UTF8ToUTF16(9), "UTF8 to UTF16 accounts supplementary rune");
        Equal(9, map.UTF16ToUTF8(6), "UTF16 to UTF8 inverse boundary");
        var malformed = Parse([0x2f, 0x2f, 0xff, 0x80, 0x0a]);
        Equal(5, malformed.GetPositionMap().UTF8ToUTF16(5), "Each malformed UTF8 byte contributes one character");
    }

    private static void SourceLifecycle()
    {
        byte[] owner = "prefix-/** docs */ const value = 1;-suffix"u8.ToArray();
        var view = ByteStringView.FromOwnedBytes(owner).Slice(7, owner.Length - 7);
        var file = Parser.ParseSourceFileOwned(new("/owned.ts", "/owned.ts"), view, ScriptKind.TS);
        Require(MemoryMarshal.TryGetArray(file.TextView().Memory, out var source) && ReferenceEquals(owner, source.Array), "Owned parser keeps transferred source owner");
        Equal(7, source.Offset, "Source offset retained");
        var factory = new NodeFactory();
        var clone = file.AsNode().Clone(factory).AsSourceFile();
        var update = factory.UpdateSourceFile(file, factory.NewNodeList(GoSlices.Clone(file.Statements.Nodes)), file.EndOfFileToken).AsSourceFile();
        foreach (var other in new[] { clone, update })
        {
            Require(MemoryMarshal.TryGetArray(other.TextView().Memory, out var memory) && ReferenceEquals(owner, memory.Array) && memory.Offset == source.Offset && memory.Count == source.Count, "Clone/update retain exact immutable view header");
            Equal(file.Text(), other.Text(), "Clone/update raw public text unchanged");
        }
        var replacement = Parse("const replacement = 2;"u8.ToArray());
        Require(MemoryMarshal.TryGetArray(replacement.TextView().Memory, out var independent) && !ReferenceEquals(owner, independent.Array), "New source does not reuse unrelated source storage");
        Equal("/** docs */ const value = 1;", clone.DecodedText(), "Detached clone retains source lifetime");
    }

    // vfs/internal/internal.go:159–181. Decoding is a filesystem boundary;
    // direct parser byte input is already source text and retains a UTF8 BOM.
    private static void BomBoundary()
    {
        byte[] owned = [0xef, 0xbb, 0xbf, 0x61, 0xff, 0xed, 0xa0, 0x80];
        var (decoded, ok) = VfsInternal.DecodeSourceBytes(owned);
        Require(ok, "UTF8 BOM decoding succeeds even with malformed/WTF8 contents");
        Bytes([0x61, 0xff, 0xed, 0xa0, 0x80], decoded.AsSpan(), "Filesystem strips only BOM and preserves other encoded bytes");
        Require(MemoryMarshal.TryGetArray(decoded.Memory, out var memory) && ReferenceEquals(owned, memory.Array), "UTF8 BOM removal shares owned input");
        Equal(3, memory.Offset, "UTF8 BOM view offset");
        Equal(5, memory.Count, "UTF8 BOM view byte length");
        var file = Parse([0xef, 0xbb, 0xbf, 0x63, 0x6f, 0x6e, 0x73, 0x74, 0x20, 0x78, 0x3d, 0x31, 0x3b]);
        Bytes([0xef, 0xbb, 0xbf, 0x63, 0x6f, 0x6e, 0x73, 0x74, 0x20, 0x78, 0x3d, 0x31, 0x3b], file.TextView().AsSpan(), "Direct parser input preserves BOM and byte positions");
        Equal(0, file.Diagnostics().Length, "Scanner recognizes BOM as whitespace");
        byte[] partial = [0xef, 0xbb];
        Bytes(partial, VfsInternal.DecodeSourceBytes(partial).Contents.AsSpan(), "Incomplete BOM is ordinary raw input");
        Require(VfsInternal.DecodeSourceBytes([]).Contents.AsSpan().IsEmpty, "Empty filesystem source is successful empty text");
    }

    private static void Utf16Files()
    {
        // Explicit bytes: a, U+1F600, lone high surrogate, b, lone low surrogate,
        // and an odd trailing byte. Go utf16.Decode replaces isolated units;
        // decodeUtf16 reads floor(length/2) units and ignores the final byte.
        byte[][] inputs =
        [ [0xff, 0xfe, 0x61, 0x00, 0x3d, 0xd8, 0x00, 0xde, 0x00, 0xd8, 0x62, 0x00, 0x00, 0xdc, 0xff],
          [0xfe, 0xff, 0x00, 0x61, 0xd8, 0x3d, 0xde, 0x00, 0xd8, 0x00, 0x00, 0x62, 0xdc, 0x00, 0xff] ];
        byte[] expected = [0x61, 0xf0, 0x9f, 0x98, 0x80, 0xef, 0xbf, 0xbd, 0x62, 0xef, 0xbf, 0xbd];
        foreach (var input in inputs)
        {
            var (decoded, ok) = VfsInternal.DecodeSourceBytes(input);
            Require(ok, "UTF16 BOM source decodes");
            Bytes(expected, decoded.AsSpan(), "UTF16 byte order, paired/isolated surrogates and odd tail match Go");
            Equal(GoString.FromBytes(expected), VfsInternal.DecodeBytes(GoString.FromBytes(input)).Contents, "Legacy public filesystem result remains byte-identical");
        }
    }

    private sealed class LegacyFS(string? contents = null) : IFS
    {
        internal readonly string Contents = contents ?? GoString.FromBytes([0x61, 0xc3, 0xa9, 0xff, 0xed, 0xa0, 0x80]);
        internal int Reads;
        public CaseSensitivity CaseSensitivity() => global::TypeScript.Scanner.CaseSensitivity.CaseSensitive;
        public bool FileExists(RootedFilePath path) => path.AsString() == "/source.ts";
        public (string Contents, bool Ok) ReadFile(RootedFilePath path)
        { Reads++; return FileExists(path) ? (Contents, true) : ("", false); }
        public bool DirectoryExists(RootedDirectoryPath path) => path.AsString() == "/";
        public Entries GetAccessibleEntries(RootedDirectoryPath path) => new() { Files = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("source.ts")]), Directories = Slice<ByteStringValue>.Make(0) };
        public TypeScript.Vfs.FileInfo? Stat(RootedPath path) => null;
        public RootedPath Realpath(RootedPath path) => path;
    }

    private static void FileTransport()
    {
        var path = RootedFilePath.FromGoString("/source.ts");
        var legacy = new LegacyFS();
        IFS[] filesystems = [legacy, TypeScript.Vfs.CachedVfs.FS.From(legacy), TypeScript.Bundled.Bundled.WrapFS(legacy)];
        foreach (var fs in filesystems)
        {
            var (source, ok) = SourceTextReaders.Read(fs, path);
            Require(ok, "Custom filesystem fallback succeeds through direct/cached/bundled adapters");
            Equal(legacy.Contents, source.Materialize(), "Legacy ReadFile is raw Go bytes, never scalar Unicode");
            var missing = SourceTextReaders.Read(fs, RootedFilePath.FromGoString("/missing.ts"));
            Require(!missing.Ok, "Missing customFS source stays missing");
        }
        Equal(6, legacy.Reads, "ReadSourceText does not introduce content caching in cachedvfs");
        var legacyBom = new LegacyFS(GoString.FromBytes([0xef, 0xbb, 0xbf, 0x61]));
        Bytes([0xef, 0xbb, 0xbf, 0x61], SourceTextReaders.Read(legacyBom, path).Contents.AsSpan(), "Legacy custom ReadFile already supplies source text; fallback does not strip another BOM");
        var library = RootedFilePath.FromGoString(TypeScript.Bundled.Bundled.LibPath().AsString() + "/lib.es5.d.ts");
        var (embedded, found) = SourceTextReaders.Read(filesystems[2], library);
        Require(found && embedded.Length > 0, "Bundled library uses actual embedded source");
        Equal(legacy.Contents, SourceTextReaders.Read(legacy, path).Contents.Materialize(), "Later fallback read remains exact");
        string temporary = Path.Combine(Path.GetTempPath(), "byte-source-" + Guid.NewGuid().ToString("N") + ".ts");
        try
        {
            File.WriteAllBytes(temporary, [0xef, 0xbb, 0xbf, 0x61, 0xff]);
            var os = TypeScript.Vfs.OsVfs.OsVfs.FS();
            var (bytes, exists) = SourceTextReaders.Read(os, RootedFilePath.FromUnicode(temporary));
            Require(exists, "Actual OS owned-source read succeeds");
            Bytes([0x61, 0xff], bytes.AsSpan(), "Actual OS byte transport strips BOM and preserves malformed byte");
        }
        finally { File.Delete(temporary); }
    }

    private static void Range()
    {
        string raw = GoString.FromBytes([0x61, 0xc3, 0xa9, 0xf0, 0x9f, 0xa6, 0x80, 0xed, 0xa0, 0x80, 0xff, 0x7a]);
        int[] offsets = [0, 1, 3, 7, 8, 9, 10, 11], runes = [0x61, 0xe9, 0x1f980, 0xfffd, 0xfffd, 0xfffd, 0xfffd, 0x7a];
        Require(offsets.SequenceEqual(GoRuntime.RangeKeys(raw)), "Go range keys count bytes and malformed one-byte widths");
        var values = GoRuntime.Range(raw).ToArray();
        Require(offsets.SequenceEqual(values.Select(x => x.Key)) && runes.SequenceEqual(values.Select(x => x.Value)), "Go range offsets and runes exact");
        Require(!GoRuntime.Range("").Any() && !GoRuntime.RangeKeys("").Any(), "Empty Go ranges remain empty");
    }
}

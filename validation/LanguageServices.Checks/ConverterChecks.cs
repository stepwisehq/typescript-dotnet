// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original converters_test.go assertions at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TypeScript.Scanner;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using SpanMapFactory = TypeScript.SpanMap.SpanMaps;
using SpanMapValue = TypeScript.SpanMap.SpanMap;

namespace LanguageServices.Checks;
public static class ConverterChecks
{
    public static readonly string[] Names = ["TestDocumentURIToFileName", "TestFileNameToDocumentURI", "TestNonFileDocumentURIRoundTripsThroughNormalizedFileName", "TestConvertersSourceFileProjectionExpansion", "TestConvertersInvalidUTF8", "TestConvertersAgainstJSReference"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestDocumentURIToFileName":
                TestDocumentURIToFileName();
                break;
            case "TestFileNameToDocumentURI":
                TestFileNameToDocumentURI();
                break;
            case "TestNonFileDocumentURIRoundTripsThroughNormalizedFileName":
                TestNonFileDocumentURIRoundTripsThroughNormalizedFileName();
                break;
            case "TestConvertersSourceFileProjectionExpansion":
                TestConvertersSourceFileProjectionExpansion();
                break;
            case "TestConvertersInvalidUTF8":
                TestConvertersInvalidUTF8();
                break;
            case "TestConvertersAgainstJSReference":
                TestConvertersAgainstJSReference();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    private static ByteStringValue Bytes(string text) => ByteStringValue.FromString(GoString.ToUtf8(text));
    private static DocumentUri Uri(string text) => new(Bytes(text));
    private static void Equal<T>(T actual, T expected)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
            throw new InvalidOperationException($"Original converter assertion: {actual} != {expected}");
    }

    private static void Assert(bool value)
    {
        if (!value)
            throw new InvalidOperationException("Original converter assertion failed");
    }

    private static void TestDocumentURIToFileName()
    {
        (string First, string Second)[] cases = [("file:///path/to/file.ts", "/path/to/file.ts"), ("file://server/share/file.ts", "//server/share/file.ts"), ("file:///d%3A/work/tsgo932/lib/utils.ts", "d:/work/tsgo932/lib/utils.ts"), ("file:///D%3A/work/tsgo932/lib/utils.ts", "D:/work/tsgo932/lib/utils.ts"), ("file:///d%3A/work/tsgo932/app/%28test%29/comp/comp-test.tsx", "d:/work/tsgo932/app/(test)/comp/comp-test.tsx"), ("file:///path/to/file.ts#section", "/path/to/file.ts"), ("file:///c:/test/me", "c:/test/me"), ("file://shares/files/c%23/p.cs", "//shares/files/c#/p.cs"), ("file:///c:/Source/Z%C3%BCrich%20or%20Zurich%20(%CB%88zj%CA%8A%C9%99r%C9%AAk,/Code/resources/app/plugins/c%23/plugin.json", "c:/Source/Zürich or Zurich (ˈzjʊərɪk,/Code/resources/app/plugins/c#/plugin.json"), ("file:///c:/test %25/path", "c:/test %/path"), ("file:///_:/path", "/_:/path"), ("file:///users/me/c%23-projects/", "/users/me/c#-projects"), ("file:///a/../b.ts", "/b.ts"), ("file://localhost/c%24/GitDevelopment/express", "//localhost/c$/GitDevelopment/express"), ("file:///c%3A/test%20with%20%2525/c%23code", "c:/test with %25/c#code"), ("untitled:Untitled-1", "^/~ts-uri~/untitled/ts-nul-authority/Untitled-1"), ("untitled:Untitled-1#fragment", "^/~ts-uri~/untitled/ts-nul-authority/~ts-uri-escape~556e7469746c65642d310023667261676d656e74~"), ("untitled:c:/Users/jrieken/Code/abc.txt", "^/~ts-uri~/untitled/ts-nul-authority/~ts-uri-escape~633a~/Users/jrieken/Code/abc.txt"), ("untitled:C:/Users/jrieken/Code/abc.txt", "^/~ts-uri~/untitled/ts-nul-authority/~ts-uri-escape~433a~/Users/jrieken/Code/abc.txt"), ("untitled://wsl%2Bubuntu/home/jabaile/work/TypeScript/newfile.ts", "^/~ts-uri~/untitled/wsl%2Bubuntu/home/jabaile/work/TypeScript/newfile.ts"), ];
        foreach (var row in cases)
        {
            Equal(Uri(row.First).FileName(), RootedFilePath.FromValue(Bytes(row.Second)));
        }
    }

    private static void TestFileNameToDocumentURI()
    {
        (string First, string Second)[] cases = [("/path/to/file.ts", "file:///path/to/file.ts"), ("//server/share/file.ts", "file://server/share/file.ts"), ("d:/work/tsgo932/lib/utils.ts", "file:///d%3A/work/tsgo932/lib/utils.ts"), ("D:/work/tsgo932/lib/utils.ts", "file:///d%3A/work/tsgo932/lib/utils.ts"), ("d:/work/tsgo932/app/(test)/comp/comp-test.tsx", "file:///d%3A/work/tsgo932/app/%28test%29/comp/comp-test.tsx"), ("/path/to/file.ts", "file:///path/to/file.ts"), ("c:/test/me", "file:///c%3A/test/me"), ("//shares/files/c#/p.cs", "file://shares/files/c%23/p.cs"), ("c:/Source/Zürich or Zurich (ˈzjʊərɪk,/Code/resources/app/plugins/c#/plugin.json", "file:///c%3A/Source/Z%C3%BCrich%20or%20Zurich%20%28%CB%88zj%CA%8A%C9%99r%C9%AAk%2C/Code/resources/app/plugins/c%23/plugin.json"), ("c:/test %/path", "file:///c%3A/test%20%25/path"), ("/", "file:///"), ("/_:/path", "file:///_%3A/path"), ("/users/me/c#-projects/", "file:///users/me/c%23-projects"), ("//localhost/c$/GitDevelopment/express", "file://localhost/c%24/GitDevelopment/express"), ("c:/test with %25/c#code", "file:///c%3A/test%20with%20%2525/c%23code"), ("^/untitled/ts-nul-authority/Untitled-1", "untitled:Untitled-1"), ("^/untitled/ts-nul-authority/c:/Users/jrieken/Code/abc.txt", "untitled:c:/Users/jrieken/Code/abc.txt"), ("^/untitled/wsl%2Bubuntu/home/jabaile/work/TypeScript/newfile.ts", "untitled://wsl%2Bubuntu/home/jabaile/work/TypeScript/newfile.ts"), ];
        foreach (var row in cases)
        {
            var path = row.First == "/users/me/c#-projects/" ? TsPath.RootedFilePathFromAbsolute(Bytes(row.First)) : RootedFilePath.FromValue(Bytes(row.First));
            Equal(Lsconv.FileNameToDocumentURI(path), Uri(row.Second));
        }
    }

    private const string JsReferenceScript = "\nconst inChunks = [];\nprocess.stdin.on('data', c => inChunks.push(c));\nprocess.stdin.on('end', () => {\n  const buf = Buffer.concat(inChunks);\n  let off = 0;\n  const readU32 = () => { const v = buf.readUInt32LE(off); off += 4; return v; };\n  const n = readU32();\n  const buffers = [];\n  for (let i = 0; i < n; i++) {\n    const len = readU32();\n    buffers.push(buf.subarray(off, off + len));\n    off += len;\n  }\n\n  const decoder = new TextDecoder('utf-8', { fatal: true });\n  const out = buffers.map(bytes => {\n    // Decode the raw UTF-8 bytes to a JS string (this is what sys.ts does with file contents).\n    const text = decoder.decode(bytes);\n\n    // LSP line starts in the *decoded* JS string: \\\\n, \\\\r, \\\\r\\\\n only.\n    const lineStartsJs = [0];\n    for (let i = 0; i < text.length; i++) {\n      const c = text.charCodeAt(i);\n      if (c === 13) {\n        if (i + 1 < text.length && text.charCodeAt(i + 1) === 10) i++;\n        lineStartsJs.push(i + 1);\n      } else if (c === 10) {\n        lineStartsJs.push(i + 1);\n      }\n    }\n\n    // Walk the original UTF-8 byte buffer to find codepoint boundaries. Inputs are\n    // valid UTF-8, so we advance bytePos by the sequence length of each lead byte\n    // and jsIdx by the corresponding UTF-16 code unit count (1 for BMP, 2 for\n    // surrogate pair) of the codepoint at jsIdx in the decoded string.\n    const boundaries = [{ bytePos: 0, jsIdx: 0 }];\n    let bytePos = 0, jsIdx = 0;\n    while (bytePos < bytes.length) {\n      const seq = utf8SeqLen(bytes[bytePos]);\n      const cp = text.codePointAt(jsIdx);\n      bytePos += seq;\n      jsIdx += cp > 0xFFFF ? 2 : 1;\n      boundaries.push({ bytePos, jsIdx });\n    }\n\n    return boundaries.map(({ bytePos, jsIdx }) => {\n      let lo = 0, hi = lineStartsJs.length - 1;\n      while (lo < hi) {\n        const mid = (lo + hi + 1) >> 1;\n        if (lineStartsJs[mid] <= jsIdx) lo = mid;\n        else hi = mid - 1;\n      }\n      return { bytePos, line: lo, char: jsIdx - lineStartsJs[lo] };\n    });\n  });\n\n  process.stdout.write(JSON.stringify(out));\n});\n\nfunction utf8SeqLen(b) {\n  if (b < 0x80) return 1;\n  if ((b & 0xE0) === 0xC0) return 2;\n  if ((b & 0xF0) === 0xE0) return 3;\n  if ((b & 0xF8) === 0xF0) return 4;\n  throw new Error('invalid UTF-8 lead byte 0x' + b.toString(16));\n}\n";
    private static void TestConvertersAgainstJSReference()
    {
        (string Name, string Text)[] cases = [("empty", ""), ("ascii", "hello\nworld"), ("ascii_crlf", "hello\r\nworld\r\n!"), ("ascii_cr_only", "a\rb\rc"), ("trailing_newline", "abc\n"), ("bmp_em_dash", "ab—cd\nef"), ("bmp_multi", "α\nβ\nγδε\nzz"), ("supplementary_emoji", "x😀y\nz"), ("supplementary_at_lineend", "ab😀\ncd😊"), ("supplementary_only", "😀😁😂"), ("mixed", "α — 😀\r\nβ\nγ\r"), ("long_mixed_ws", "  \tαβ\n\t😀  end\n"), ("zwj_emoji", "👨‍💻\nnext"), ("only_newlines", "\n\n\r\n\r"), ];
        using var input = new MemoryStream();
        Span<byte> u32 = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(u32, (uint)cases.Length);
        input.Write(u32);
        foreach (var item in cases)
        {
            var bytes = Encoding.UTF8.GetBytes(item.Text);
            BinaryPrimitives.WriteUInt32LittleEndian(u32, (uint)bytes.Length);
            input.Write(u32);
            input.Write(bytes);
        }

        var start = new ProcessStartInfo("node")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("-e");
        start.ArgumentList.Add(JsReferenceScript);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Node reference could not start");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.StandardInput.BaseStream.Write(input.ToArray());
        process.StandardInput.Close();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException("node failed: " + stderr.GetAwaiter().GetResult());
        using var refs = JsonDocument.Parse(stdout.GetAwaiter().GetResult());
        Equal(refs.RootElement.GetArrayLength(), cases.Length);
        for (int i = 0; i < cases.Length; i++)
        {
            var(conv, script) = NewTestConverters(Bytes(cases[i].Text));
            foreach (var tuple in refs.RootElement[i].EnumerateArray())
            {
                int bytePos = tuple.GetProperty("bytePos").GetInt32();
                var expected = new Position
                {
                    Line = tuple.GetProperty("line").GetUInt32(),
                    Character = tuple.GetProperty("char").GetUInt32()
                };
                Equal(conv.ToLSPPosition(script, bytePos).Position, expected);
                var positions = conv.FromLSPPosition(script, expected, Feature.All);
                Equal(positions.Count, 1);
                Equal(positions[0].Position, bytePos);
            }
        }
    }

    // Authored public-coordinate boundaries, separately counted from the six originals.
    public static int RunCoordinateBoundaryControls()
    {
        (string Text, PositionEncodingKind Encoding, uint Line, uint Character, int Expected)[] cases =
        [
            ("a\nbc", PositionEncodingKind.UTF16, 1, uint.MaxValue, 2),
            ("a\nbc", PositionEncodingKind.UTF16, 1, int.MaxValue, 2),
            ("éx", PositionEncodingKind.UTF16, 0, uint.MaxValue, 0),
            ("éx", PositionEncodingKind.UTF8, 0, uint.MaxValue, 0)
        ];
        foreach (var item in cases)
        {
            var text = Bytes(item.Text);
            var script = new TestScript(text);
            var map = Lsconv.ComputeLSPLineStarts(text);
            var converter = Converters.NewConverters(item.Encoding, _ => map);
            Equal(converter.FromLSPPosition(script, new Position { Line = item.Line, Character = item.Character }, Feature.All)[0].Position, item.Expected);
        }
        return cases.Length;
    }

    private sealed class TestScript(ByteStringValue text) : IScript
    {
        public RootedFilePath FileName() => RootedFilePath.FromGoString("/test.ts");
        public RootedFilePath OriginalFileName() => FileName();
        public ByteStringValue TextValue() => text;
        public ByteStringValue OriginalTextValue() => text;
        public SpanMapValue? SpanMap() => null;
    }

    private static (Converters Converters, TestScript Script) NewTestConverters(ByteStringValue text)
    {
        var script = new TestScript(text);
        var lineMap = Lsconv.ComputeLSPLineStarts(text);
        return (Converters.NewConverters(PositionEncodingKind.UTF16, _ => lineMap), script);
    }

    private static void TestConvertersInvalidUTF8()
    {
        var text = ByteStringValue.FromString("a\u0080b\ncd");
        var(conv, script) = NewTestConverters(text);
        (uint Line, uint Character, int BytePos)[] mappings = [(0, 0, 0), (0, 1, 1), (0, 2, 2), (0, 3, 3), (1, 0, 4), (1, 1, 5), (1, 2, 6)];
        foreach (var item in mappings)
        {
            var coordinate = new Position
            {
                Line = item.Line,
                Character = item.Character
            };
            var positions = conv.FromLSPPosition(script, coordinate, Feature.All);
            Equal(positions.Count, 1);
            Equal(positions[0].Position, item.BytePos);
            Equal(conv.ToLSPPosition(script, item.BytePos).Position, coordinate);
        }

        for (int bytePos = 0; bytePos <= text.Length; bytePos++)
        {
            var coordinate = conv.ToLSPPosition(script, bytePos).Position;
            var positions = conv.FromLSPPosition(script, coordinate, Feature.All);
            Equal(positions.Count, 1);
            Equal(positions[0].Position, bytePos);
        }
    }

    private static void TestConvertersSourceFileProjectionExpansion()
    {
        var canonical = Parser.ParseSourceFile(new SourceFileParseOptions("/component.vue", "/component.vue"), " x", ScriptKind.TS);
        var supplemental = Parser.ParseSourceFile(new SourceFileParseOptions("/component.vue", "/component.vue::supplemental"), "  x", ScriptKind.TS);
        canonical.SetContentMapperInfo(new ContentMapperSourceFileInfo { OriginalText = "x", ContentMapper = "mapper", SpanMap = SpanMapFactory.New([new Segment(1, 2, 0, 1, TypeScript.SpanMap.Kind.Verbatim, Feature.All)]), SupplementalSourceFiles = Slice<SourceFile>.FromArray([supplemental]) });
        supplemental.SetContentMapperInfo(new ContentMapperSourceFileInfo { OriginalText = "x", ContentMapper = "mapper", SpanMap = SpanMapFactory.New([new Segment(2, 3, 0, 1, TypeScript.SpanMap.Kind.Verbatim, Feature.All)]), CanonicalSourceFile = canonical });
        var lineMap = Lsconv.ComputeLSPLineStarts(ByteStringValue.FromString("x"));
        var conv = Converters.NewConverters(PositionEncodingKind.UTF16, _ => lineMap);
        var positions = conv.FromLSPPositionForSourceFile(canonical, default, Feature.Hover);
        Equal(positions.Count, 2);
        SourceFile projectedFile = positions[0].Script;
        Assert(ReferenceEquals(projectedFile, canonical));
        Assert(ReferenceEquals(positions[0].Script, canonical));
        Equal(positions[0].Position, 1);
        Assert(ReferenceEquals(positions[1].Script, supplemental));
        Equal(positions[1].Position, 2);
    }

    private static void TestNonFileDocumentURIRoundTripsThroughNormalizedFileName()
    {
        Equal(Uri("custom:folder/../~ts-uri~/café\\file.ts").FileName(), TsPath.RootedFilePathFromNormalized(Bytes("^/~ts-uri~/custom/ts-nul-authority/folder/~ts-uri-escape~2e2e~/~ts-uri~/~ts-uri-escape~636166c3a95c66696c65~.ts")));
        Equal(Uri("custom:.git/file.ts").FileName(), RootedFilePath.FromGoString("^/~ts-uri~/custom/ts-nul-authority/.git/file.ts"));
        Equal(Uri("custom:~ts-uri-escape~dir.js/file.ts?x=1").FileName(), TsPath.RootedFilePathFromNormalized("^/~ts-uri~/custom/ts-nul-authority/~ts-uri-escape~7e74732d7572692d6573636170657e6469722e6a73~/~ts-uri-escape~66696c65003f783d31~.ts"));
        Equal(Uri("custom:c:/dir/file.ts?x=1").FileName().Directory(), Uri("custom:c:/dir/other.ts").FileName().Directory());
        string[] uris = ["untitled:folder/../file.ts", "vscode-vfs://github/path//file.ts", "custom:/path/./file.ts/", "custom:", "custom:///path", "custom://authority", "custom://authority/", "custom:path/file.ts?rev=a/b#frag/c", "custom://authority/path/file.ts#frag/a", "custom:path\\file.ts", "custom:.git/file.ts", "custom:..hidden/file.ts", "custom://~ts-uri~/path", "custom://ts-nul-authority/path", "custom:~ts-uri-escape~file.ts", "custom:~ts-uri-escape~no-path", "custom://authority/~ts-uri-no-path~~", "custom:~ts-uri-spec~666f6f~/file.ts?x=1", "custom:folder/../~ts-uri~/café\\file.ts", "custom:name.ts\\", "custom:name..ts"];
        foreach (var text in uris)
        {
            var uri = Uri(text);
            var file = uri.FileName();
            Equal(TsPath.RootedFilePathFromNormalized(file.AsValue()), file);
            Equal(Lsconv.FileNameToDocumentURI(file), uri);
        }

        foreach (var text in new[]
        {
            "custom:path\\file.ts",
            "custom:~ts-uri~file.ts",
            "custom:~ts-uri-escape~file.ts"
        }

        )
            Equal(Uri(text).FileName().Extension(), TsPath.ExtensionTs);
        foreach (var text in new[]
        {
            "custom:~ts-uri-escape~types.d.ts",
            "custom:~ts-uri-escape~types.d.mts",
            "custom:~ts-uri-escape~types.d.css.ts"
        }

        )
            Equal(Uri(text).FileName().IsDeclarationFile(), true);
        Equal(Uri("custom:~ts-uri-escape~types.d.ts").FileName().Extension(), TsPath.ExtensionDts);
        Equal(Uri("custom:~ts-uri-escape~types.d.mts").FileName().Extension(), TsPath.ExtensionDmts);
        Equal(Uri("custom:~ts-uri-escape~types.d.css.ts").FileName().AsString().EndsWith(".d.css.ts", StringComparison.Ordinal), true);
        Equal(Uri("custom:folder/main\\file.ts").FileName().Directory(), Uri("custom:folder/dep.ts").FileName().Directory());
        Equal(Uri("custom://a/main.ts").FileName().Directory().ResolveFile("../b/dep.ts"), Uri("custom://a/b/dep.ts").FileName());
        Equal(Uri("custom://a").FileName().Directory().ResolveFile("dep.ts"), Uri("custom://a/dep.ts").FileName());
        var queryFile = Uri("custom:path/file.ts?rev=a/b").FileName();
        Equal(queryFile.Extension(), TsPath.ExtensionTs);
        Equal(queryFile.Directory(), Uri("custom:path/other.ts").FileName().Directory());
        Assert(Uri("custom:~ts-uri-escape~Foo.ts").PathKey(CaseSensitivity.CaseInsensitive) != Uri("custom:~ts-uri-escape~foo.ts").PathKey(CaseSensitivity.CaseInsensitive));
        Equal(Lsconv.FileNameToDocumentURI(TsPath.RootedFilePathFromNormalized("^/custom/ts-nul-authority/~ts-uri-escape~666f6f~.ts")), Uri("custom:~ts-uri-escape~666f6f~.ts"));
        Equal(Lsconv.FileNameToDocumentURI(TsPath.RootedFilePathFromNormalized("^/~ts-uri~/custom/ts-nul-authority/~ts-uri-escape~ff~")), Uri("custom:~ts-uri-escape~ff~"));
        Assert(Uri("custom:name.ts\\").FileName() != Uri("custom:name..ts").FileName());
    }
}

// Copyright 2010 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 regexp/exec_test.go selected original MatchString assertion channels.
// Capture/index assertions are outside the selected public dependency closure.
// Fixture Go literals are losslessly decoded/base64 transported by the source generator.
using System.IO.Compression;
using System.Text;
using TypeScript.Scanner;
using TypeScript.RegularExpressions;

namespace LanguageServices.Checks;
public static class RegexRe2Checks
{
    public static readonly string[] Names = ["TestRE2Search.MatchString", "TestRE2Exhaustive.MatchString"];
    public static int RunAll() { foreach (var name in Names) Run(name); return Names.Length; }
    public static void Run(string name)
    {
        string resource = name switch
        {
            "TestRE2Search.MatchString" => "re2-search.txt.gz",
            "TestRE2Exhaustive.MatchString" => "re2-exhaustive.txt.gz",
            _ => throw new ArgumentException(name)
        };
        using var source = typeof(RegexRe2Checks).Assembly.GetManifestResourceStream("LanguageServices.Checks.RegexData." + resource)
            ?? throw new InvalidOperationException("missing original RE2 resource: " + resource);
        using var compressed = new GZipStream(source, CompressionMode.Decompress);
        using var reader = new StreamReader(compressed, Encoding.UTF8);
        var strings = new List<ByteStringValue>();
        bool inStrings = false;
        GoRegexp? regex = null, fullRegex = null;
        int inputIndex = 0, inputCount = 0, lineNumber = 0, cases = 0;
        int matchedRows = 0, matchAssertions = 0, byteEscapeSkips = 0, byteBoundarySkips = 0;
        var errors = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            if (line.Length == 0) throw new InvalidOperationException($"{resource}:{lineNumber}: unexpected blank line");
            if (line[0] == '#' || line[0] is >= 'A' and <= 'Z') continue;
            if (line == "strings") { strings.Clear(); inStrings = true; continue; }
            if (line == "regexps") { inStrings = false; continue; }
            if (line[0] == '@')
            {
                var quoted = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(Convert.FromBase64String(line.AsSpan(1).ToString())));
                if (inStrings) { strings.Add(quoted); continue; }
                if (inputIndex != inputCount) throw new InvalidOperationException($"{resource}:{lineNumber}: input remains before pattern");
                var compiled = TryCompile(quoted);
                regex = compiled.Regex;
                if (compiled.Error != null)
                {
                    // Original Go excludes unsupported RE2 byte-escape \C.
                    if (compiled.Error == "error parsing regexp: invalid escape sequence: `\\C`") continue;
                    Fail($"{resource}:{lineNumber}: compile: {compiled.Error}");
                    continue;
                }
                var fullPattern = ByteStringValue.Concat(ByteStringValue.FromString("\\A(?:"), quoted, ByteStringValue.FromString(")\\z"));
                var full = TryCompile(fullPattern);
                if (full.Error != null || full.Regex == null) throw new InvalidOperationException($"{resource}:{lineNumber}: compile full: {full.Error}");
                fullRegex = full.Regex;
                inputIndex = 0;
                inputCount = strings.Count;
                continue;
            }
            if (line[0] == '-' || line[0] is >= '0' and <= '9')
            {
                cases++;
                if (regex == null) { byteEscapeSkips++; continue; }
                if (inputIndex == inputCount) throw new InvalidOperationException($"{resource}:{lineNumber}: no input remaining");
                var text = strings[inputIndex++];
                // Original RE2 treats \B between UTF8 bytes; Go only visits rune boundaries.
                if (!IsSingleBytes(text) && regex.expr.Contains("\\B"u8)) { byteBoundarySkips++; continue; }
                matchedRows++;
                var expected = line.Split(';');
                if (expected.Length != 4) throw new InvalidOperationException($"{resource}:{lineNumber}: expected four result channels");
                for (int i = 0; i < expected.Length; i++)
                {
                    var owner = (i & 1) == 0 ? fullRegex! : regex;
                    owner.longest = i >= 2;
                    bool actual = owner.MatchString(text);
                    matchAssertions++;
                    bool want = expected[i] != "-";
                    if (actual != want) Fail($"{resource}:{lineNumber}: MatchString channel{i}: actual{actual}, expected{want}");
                }
                continue;
            }
            throw new InvalidOperationException($"{resource}:{lineNumber}: out of sync: {line}");
        }
        if (inputIndex != inputCount) throw new InvalidOperationException($"{resource}: input remains at EOF");
        if (cases == 0) throw new InvalidOperationException("no original RE2 cases observed");
        Console.WriteLine($"REGEX_RE2_SUMMARY {name} rows={cases} matchedRows={matchedRows} matchAssertions={matchAssertions} unsupportedByteEscapeSkips={byteEscapeSkips} utf8ByteBoundarySkips={byteBoundarySkips} failures={errors.Count}");
        if (errors.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        void Fail(string error)
        {
            errors.Add(error);
            if (errors.Count >= 100) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }
    }
    private static bool IsSingleBytes(ByteStringValue text)
    {
        for (int i = 0; i < text.Length; i++) if (text[i] >= 128) return false;
        return true;
    }
    private static (GoRegexp? Regex, string? Error) TryCompile(ByteStringValue pattern)
    {
        try
        {
            var (regex, error) = GoRegexp.Compile(pattern);
            return (regex, error?.Message);
        }
        catch (Exception error) { return (null, "panic: " + error.Message); }
    }
}

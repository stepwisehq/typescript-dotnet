// Copyright 2009 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Generated from pinned Go1.27.1 regexp/all_test.go and find_test.go.
// TestMatch retains its MatchString assertion channel; byte Match API is outside selected closure.
using TypeScript.Scanner;
using TypeScript.RegularExpressions;
using TypeScript.RegularExpressions.Syntax;
namespace LanguageServices.Checks;
public static class RegexChecks
{
    public static readonly string[] Names = ["TestGoodCompile", "TestBadCompile", "TestMatch.MatchString", "TestCompileOnePass", "TestRunOnePass", "TestOnePassCutoff", "TestMinInputLen", "TestProgramTooLongForBacktrack", "TestMergeRuneSet", "TestSwitchBacktrack.MatchStringAdaptation"];
    public static int RunAll() { foreach (var name in Names) Run(name); return Names.Length; }
    public static void Run(string name)
    {
        switch (name)
        {
            case "TestGoodCompile":
                foreach (var pattern in Good)
                    if (GoRegexp.Compile(ByteStringValue.FromString(pattern)).Error != null)
                        throw new InvalidOperationException("unexpected compile error: " + pattern);
                break;
            case "TestBadCompile":
                foreach (var (pattern, expected) in Bad)
                {
                    var error = GoRegexp.Compile(ByteStringValue.FromString(pattern)).Error;
                    if (error == null || !error.Message.Contains(expected, StringComparison.Ordinal))
                        throw new InvalidOperationException("wrong compile error: " + error?.Message);
                }
                break;
            case "TestMatch.MatchString":
                foreach (var (pattern, text, expected) in Matches)
                {
                    var (regex, error) = GoRegexp.Compile(ByteStringValue.FromString(pattern));
                    if (error != null || regex == null) throw new InvalidOperationException(error?.Message);
                    if (regex.MatchString(ByteStringValue.FromString(text)) != expected)
                        throw new InvalidOperationException("MatchString failure: " + pattern);
                }
                break;
            case "TestMergeRuneSet": RegexMergeChecks.RunAll(); break;
            case "TestSwitchBacktrack.MatchStringAdaptation":
                var (switchRegex, switchError) = GoRegexp.Compile(ByteStringValue.FromString("a|b"));
                if (switchError != null || switchRegex == null) throw new InvalidOperationException(switchError?.Message);
                var longInput = ByteStringValue.FromString(new string('\0', 262144 + 1));
                switchRegex.MatchString(longInput);
                switchRegex.MatchString(longInput.Slice(0, 1));
                break;
            case "TestCompileOnePass":
                foreach (var (pattern, expected) in OnePass)
                {
                    var (tree, error) = RegexParser.Parse(ByteStringValue.FromString(pattern), RegexFlags.Perl);
                    if (error != null || tree == null) throw new InvalidOperationException(error?.Message);
                    var program = RegexCompiler.Compile(RegexNode.Simplify(tree)!);
                    if ((GoRegexp.CompileOnePass(program) != null) != expected)
                        throw new InvalidOperationException("wrong one-pass classification: " + pattern);
                }
                break;
            case "TestRunOnePass":
                var (one, oneError) = GoRegexp.Compile(ByteStringValue.FromString("^a(/b+(#c+)*)*$"));
                if (oneError != null || one == null || one.onepass == null || !one.MatchString(ByteStringValue.FromString("a/b#c")))
                    throw new InvalidOperationException("one-pass execution failed");
                break;
            case "TestOnePassCutoff":
                var (cutoff, cutoffError) = RegexParser.Parse(ByteStringValue.FromString("^x{1,1000}y{1,1000}$"), RegexFlags.Perl);
                if (cutoffError != null || cutoff == null || GoRegexp.CompileOnePass(RegexCompiler.Compile(RegexNode.Simplify(cutoff)!)) != null)
                    throw new InvalidOperationException("one-pass cutoff failed");
                break;
            case "TestMinInputLen":
                foreach (var (pattern, expected) in MinimumLengths)
                {
                    var (tree, _) = RegexParser.Parse(ByteStringValue.FromString(pattern), RegexFlags.Perl);
                    if (tree == null || GoRegexp.MinInputLength(tree) != expected)
                        throw new InvalidOperationException("wrong minimum input length: " + pattern);
                }
                break;
            case "TestProgramTooLongForBacktrack":
                var (longRegex, longError) = GoRegexp.Compile(ByteStringValue.FromString(LongPattern));
                if (longError != null || longRegex == null || !longRegex.MatchString(ByteStringValue.FromString("two")) || longRegex.MatchString(ByteStringValue.FromString("xxx")))
                    throw new InvalidOperationException("long regex execution failed");
                break;
            default: throw new ArgumentException(name);
        }
    }
    private static readonly string[] Good = [
        "",
        ".",
        "^.$",
        "a",
        "a*",
        "a+",
        "a?",
        "a|b",
        "a*|b*",
        "(a*|b)(c*|d)",
        "[a-z]",
        "[a-abc-c\\-\\]\\[]",
        "[a-z]+",
        "[abc]",
        "[^1234]",
        "[^\\n]",
        "\\!\\\\"
    ];
    private static readonly (string Pattern, string Error)[] Bad = [
        ("*", "missing argument to repetition operator: `*`"),
        ("+", "missing argument to repetition operator: `+`"),
        ("?", "missing argument to repetition operator: `?`"),
        ("(abc", "missing closing ): `(abc`"),
        ("abc)", "unexpected ): `abc)`"),
        ("x[a-z", "missing closing ]: `[a-z`"),
        ("[z-a]", "invalid character class range: `z-a`"),
        ("abc\\", "trailing backslash at end of expression"),
        ("a**", "invalid nested repetition operator: `**`"),
        ("a*+", "invalid nested repetition operator: `*+`"),
        ("\\x", "invalid escape sequence: `\\x`"),
        (Repeat("\\pL", 27000), "expression too large")
    ];
    private static string Raw(string text) => ByteStringView.FromUnicode(text).Materialize();
    private static string Repeat(string text, int count) => string.Concat(Enumerable.Repeat(text, count));
    private static readonly (string Pattern, string Text, bool Expected)[] Matches = [
        ("", "", true),
        ("^abcdefg", "abcdefg", true),
        ("a+", "baaab", true),
        ("a", "bababaab", true),
        ("a", "bababaab", true),
        ("a", "bababaab", true),
        ("a", "bababaab", true),
        ("a", "bababaab", true),
        ("a", "bababaab", true),
        ("abcd..", "abcdef", true),
        ("a", "a", true),
        ("x", "y", false),
        ("b", "abc", true),
        (".", "a", true),
        (".*", "abcdef", true),
        ("^", "abcde", true),
        ("$", "abcde", true),
        ("^abcd$", "abcd", true),
        ("^bcd'", "abcdef", false),
        ("^abcd$", "abcde", false),
        ("a+", "baaab", true),
        ("a*", "baaab", true),
        ("[a-z]+", "abcd", true),
        ("[^a-z]+", "ab1234cd", true),
        ("[a\\-\\]z]+", "az]-bcz", true),
        ("[^\\n]+", "abcd\n", true),
        (Raw("[日本語]+"), Raw("日本語日本語"), true),
        (Raw("日本語+"), Raw("日本語"), true),
        (Raw("日本語+"), Raw("日本語語語語"), true),
        ("()", "", true),
        ("(a)", "a", true),
        ("(.)(.)", Raw("日a"), true),
        ("(.*)", "", true),
        ("(.*)", "abcd", true),
        ("(..)(..)", "abcd", true),
        ("(([^xyz]*)(d))", "abcd", true),
        ("((a|b|c)*(d))", "abcd", true),
        ("(((a|b|c)*)(d))", "abcd", true),
        ("\\a\\f\\n\\r\\t\\v", "\u0007\f\n\r\t\u000b", true),
        ("[\\a\\f\\n\\r\\t\\v]+", "\u0007\f\n\r\t\u000b", true),
        ("a*(|(b))c*", "aacc", true),
        ("(.*).*", "ab", true),
        ("[.]", ".", true),
        ("/$", "/abc/", true),
        ("/$", "/abc", false),
        (".", "abc", true),
        ("(.)", "abc", true),
        (".(.)", "abcd", true),
        ("ab*", "abbaab", true),
        ("a(b*)", "abbaab", true),
        ("ab$", "cab", true),
        ("axxb$", "axxcb", false),
        ("data", "daXY data", true),
        ("da(.)a$", "daXY data", true),
        ("zx+", "zzx", true),
        ("ab$", "abcab", true),
        ("(aa)*$", "a", true),
        ("(?:.|(?:.a))", "", false),
        ("(?:A(?:A|a))", "Aa", true),
        ("(?:A|(?:A|a))", "a", true),
        ("(a){0}", "", true),
        ("(?-s)(?:(?:^).)", "\n", false),
        ("(?s)(?:(?:^).)", "\n", true),
        ("(?:(?:^).)", "\n", false),
        ("\\b", "x", true),
        ("\\b", "xx", true),
        ("\\b", "x y", true),
        ("\\b", "xx yy", true),
        ("\\B", "x", false),
        ("\\B", "xx", true),
        ("\\B", "x y", false),
        ("\\B", "xx yy", true),
        ("(|a)*", "aa", true),
        ("0A|0[aA]", "0a", true),
        ("0[aA]|0A", "0a", true),
        ("[^\\S\\s]", "abcd", false),
        ("[^\\S[:space:]]", "abcd", false),
        ("[^\\D\\d]", "abcd", false),
        ("[^\\D[:digit:]]", "abcd", false),
        ("(?i)\\W", "x", false),
        ("(?i)\\W", "k", false),
        ("(?i)\\W", "s", false),
        ("\\!\\\"\\#\\$\\%\\&\\'\\(\\)\\*\\+\\,\\-\\.\\/\\:\\;\\<\\=\\>\\?\\@\\[\\\\\\]\\^\\_\\{\\|\\}\\~", "!\"#$%&'()*+,-./:;<=>?@[\\]^_{|}~", true),
        ("[\\!\\\"\\#\\$\\%\\&\\'\\(\\)\\*\\+\\,\\-\\.\\/\\:\\;\\<\\=\\>\\?\\@\\[\\\\\\]\\^\\_\\{\\|\\}\\~]+", "!\"#$%&'()*+,-./:;<=>?@[\\]^_{|}~", true),
        ("\\`", "`", true),
        ("[\\`]+", "`", true),
        (Raw("�"), "\u00ff", true),
        (Raw("�"), "hello\u00ffworld", true),
        (".*", "hello\u00ffworld", true),
        ("\\x{fffd}", "\u00c2\u0000", true),
        (Raw("[�]"), "\u00ff", true),
        ("[\\x{fffd}]", "\u00c2\u0000", true),
        (".", "qwertyuiopasdfghjklzxcvbnm1234567890", true)
    ];
    private static readonly (string Pattern, bool Expected)[] OnePass = [
        ("^(?:a|(?:a*))$", false),
        ("^(?:(a)|(?:a*))$", false),
        ("^(?:(?:(?:.(?:$))?))$", true),
        ("^abcd$", true),
        ("^abcd", true),
        ("^(?:(?:a{0,})*?)$", false),
        ("^(?:(?:a+)*)$", true),
        ("^(?:(?:a|(?:aa)))$", true),
        ("^(?:[^\\s\\S])$", true),
        ("^(?:(?:a{3,4}){0,})$", false),
        ("^(?:(?:(?:a*)+))$", true),
        ("^[a-c]+$", true),
        ("^[a-c]*$", true),
        ("^(?:a*)$", true),
        ("^(?:(?:aa)|a)$", true),
        ("^[a-c]*", false),
        ("^...$", true),
        ("^...", true),
        ("^(?:a|(?:aa))$", true),
        ("^a((b))c$", true),
        ("^a.[l-nA-Cg-j]?e$", true),
        ("^a((b))$", true),
        ("^a(?:(b)|(c))c$", true),
        ("^a(?:(b*)|(c))c$", false),
        ("^a(?:b|c)$", true),
        ("^a(?:b?|c)$", true),
        ("^a(?:b?|c?)$", false),
        ("^a(?:b?|c+)$", true),
        ("^a(?:b+|(bc))d$", false),
        ("^a(?:bc)+$", true),
        ("^a(?:[bcd])+$", true),
        ("^a((?:[bcd])+)$", true),
        ("^a(:?b|c)*d$", true),
        ("^.bc(d|e)*$", true),
        ("^(?:(?:aa)|.)$", false),
        ("^(?:(?:a{1,2}){1,2})$", false),
        ("^loooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong$", true)
    ];
    private static readonly (string Pattern, int Expected)[] MinimumLengths = [
        ("", 0),
        ("a", 1),
        ("aa", 2),
        ("(aa)a", 3),
        ("(?:aa)a", 3),
        ("a?a", 1),
        ("(aaa)|(aa)", 2),
        ("(aa)+a", 3),
        ("(aa)*a", 1),
        ("(aa){3,5}", 6),
        ("[a-z]", 1),
        (Raw("日"), 3)
    ];
    private const string LongPattern = "(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen|twenty|twentyone|twentytwo|twentythree|twentyfour|twentyfive|twentysix|twentyseven|twentyeight|twentynine|thirty|thirtyone|thirtytwo|thirtythree|thirtyfour|thirtyfive|thirtysix|thirtyseven|thirtyeight|thirtynine|forty|fortyone|fortytwo|fortythree|fortyfour|fortyfive|fortysix|fortyseven|fortyeight|fortynine|fifty|fiftyone|fiftytwo|fiftythree|fiftyfour|fiftyfive|fiftysix|fiftyseven|fiftyeight|fiftynine|sixty|sixtyone|sixtytwo|sixtythree|sixtyfour|sixtyfive|sixtysix|sixtyseven|sixtyeight|sixtynine|seventy|seventyone|seventytwo|seventythree|seventyfour|seventyfive|seventysix|seventyseven|seventyeight|seventynine|eighty|eightyone|eightytwo|eightythree|eightyfour|eightyfive|eightysix|eightyseven|eightyeight|eightynine|ninety|ninetyone|ninetytwo|ninetythree|ninetyfour|ninetyfive|ninetysix|ninetyseven|ninetyeight|ninetynine|onehundred)";
}

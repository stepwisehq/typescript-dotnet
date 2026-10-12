// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 regexp/syntax/parse_test.go original selected parser assertions.
using System.Text;
using System.Globalization;
using TypeScript.Scanner;
using TypeScript.RegularExpressions.Syntax;
namespace LanguageServices.Checks;
public static class RegexSyntaxChecks
{
    public static readonly string[] Names = ["TestParseSimple", "TestParseFoldCase", "TestParseLiteral", "TestParseMatchNL", "TestParseNoMatchNL", "TestParseInvalidRegexps", "TestFoldConstants", "TestAppendRangeCollapse"];
    public static int RunAll() { foreach (var name in Names) Run(name); return Names.Length; }
    public static void Run(string name)
    {
        switch (name)
        {
            case "TestParseSimple": TestParseDump(ParseTests, RegexFlags.MatchNL | RegexFlags.PerlX | RegexFlags.UnicodeGroups); break;
            case "TestParseFoldCase": TestParseDump(FoldcaseTests, RegexFlags.FoldCase); break;
            case "TestParseLiteral": TestParseDump(LiteralTests, RegexFlags.Literal); break;
            case "TestParseMatchNL": TestParseDump(MatchnlTests, RegexFlags.MatchNL); break;
            case "TestParseNoMatchNL": TestParseDump(NomatchnlTests, 0); break;
            case "TestParseInvalidRegexps":
                foreach (var pattern in InvalidRegexps) { CheckAdmission(pattern, RegexFlags.Perl, false); CheckAdmission(pattern, RegexFlags.POSIX, false); }
                foreach (var pattern in OnlyPerl) { CheckAdmission(pattern, RegexFlags.Perl, true); CheckAdmission(pattern, RegexFlags.POSIX, false); }
                foreach (var pattern in OnlyPOSIX) { CheckAdmission(pattern, RegexFlags.Perl, false); CheckAdmission(pattern, RegexFlags.POSIX, true); }
                break;
            case "TestFoldConstants":
                int last = -1;
                for (int i = 0; i <= 0x10ffff; i++)
                {
                    if (UnicodeCase.SimpleFold(i) == i) continue;
                    if (last == -1 && RegexParser.minFold != i) throw new InvalidOperationException("wrong minimum fold rune");
                    last = i;
                }
                if (RegexParser.maxFold != last) throw new InvalidOperationException("wrong maximum fold rune");
                break;
            case "TestAppendRangeCollapse":
                RegexSlice<int> ranges = default;
                for (int i = 'A'; i <= 'Z'; i++)
                {
                    ranges = RegexParser.appendRange(ranges, i, i);
                    ranges = RegexParser.appendRange(ranges, i + 'a' - 'A', i + 'a' - 'A');
                }
                var rangeText = new StringBuilder();
                foreach (var rune in ranges) rangeText.Append(GoString.FromRune(rune));
                string actual = rangeText.ToString();
                if (actual != "AZaz") throw new InvalidOperationException("appendRange interlaced A-Z a-z = " + actual);
                break;
            default: throw new ArgumentException(name);
        }
    }
    private static void CheckAdmission(string pattern, RegexFlags flags, bool expected)
    {
        var (_, error) = RegexParser.Parse(ByteStringValue.FromString(pattern), flags);
        if ((error == null) != expected) throw new InvalidOperationException("wrong parser admission: " + pattern);
    }
    private static void TestParseDump((string Pattern, string Dump)[] tests, RegexFlags flags)
    {
        foreach (var test in tests)
        {
            var (tree, error) = RegexParser.Parse(ByteStringValue.FromString(test.Pattern), flags);
            if (error != null || tree == null) throw new InvalidOperationException(error?.Message);
            if (test.Dump.Length != 0 && Dump(tree) != test.Dump)
                throw new InvalidOperationException("wrong parser dump: " + test.Pattern + " actual=" + Dump(tree) + " expected=" + test.Dump);
        }
    }
    private static string Raw(string text) => ByteStringView.FromUnicode(text).Materialize();
    private static string Repeat(string text, int count) => string.Concat(Enumerable.Repeat(text, count));
    private static string Dump(RegexNode tree) { var output = new StringBuilder(); DumpRegexp(output, tree); return output.ToString(); }
    private static readonly string[] OpNames = ["", "no", "emp", "lit", "cc", "dnl", "dot", "bol", "eol", "bot", "eot", "wb", "nwb", "cap", "star", "plus", "que", "rep", "cat", "alt"];
    private static void DumpRegexp(StringBuilder output, RegexNode tree)
    {
        if ((int)tree.Op >= OpNames.Length || OpNames[(int)tree.Op].Length == 0) output.Append("op").Append((int)tree.Op);
        else
        {
            switch (tree.Op)
            {
                default: output.Append(OpNames[(int)tree.Op]); break;
                case RegexOp.Star: case RegexOp.Plus: case RegexOp.Quest: case RegexOp.Repeat:
                    if ((tree.Flags & RegexFlags.NonGreedy) != 0) output.Append('n');
                    output.Append(OpNames[(int)tree.Op]); break;
                case RegexOp.Literal:
                    output.Append(tree.Rune.Length > 1 ? "str" : "lit");
                    if ((tree.Flags & RegexFlags.FoldCase) != 0)
                        foreach (var rune in tree.Rune)
                            if (UnicodeCase.SimpleFold(rune) != rune) { output.Append("fold"); break; }
                    break;
            }
        }
        output.Append('{');
        switch (tree.Op)
        {
            case RegexOp.EndText: if ((tree.Flags & RegexFlags.WasDollar) == 0) output.Append("\\z"); break;
            case RegexOp.Literal: foreach (var rune in tree.Rune) output.Append(GoString.FromRune(rune)); break;
            case RegexOp.Concat: case RegexOp.Alternate: foreach (var sub in tree.Sub) DumpRegexp(output, sub!); break;
            case RegexOp.Star: case RegexOp.Plus: case RegexOp.Quest: DumpRegexp(output, tree.Sub[0]!); break;
            case RegexOp.Repeat: output.Append(tree.Min.ToString(CultureInfo.InvariantCulture)).Append(',').Append(tree.Max.ToString(CultureInfo.InvariantCulture)).Append(' '); DumpRegexp(output, tree.Sub[0]!); break;
            case RegexOp.Capture:
                if (tree.Name.Length != 0) output.Append(tree.Name.Materialize()).Append(':');
                DumpRegexp(output, tree.Sub[0]!); break;
            case RegexOp.CharClass:
                string separator = "";
                for (int i = 0; i < tree.Rune.Length; i += 2)
                {
                    output.Append(separator); separator = " ";
                    output.Append("0x").Append(tree.Rune[i].ToString("x", CultureInfo.InvariantCulture));
                    if (tree.Rune[i] != tree.Rune[i + 1]) output.Append("-0x").Append(tree.Rune[i + 1].ToString("x", CultureInfo.InvariantCulture));
                }
                break;
        }
        output.Append('}');
    }
    private static string MkCharClass(Func<int, bool> predicate)
    {
        var tree = new RegexNode { Op = RegexOp.CharClass };
        int low = -1;
        for (int i = 0; i <= 0x10ffff; i++)
        {
            if (predicate(i)) { if (low < 0) low = i; }
            else if (low >= 0) { tree.Rune = tree.Rune.Append(low, i - 1); low = -1; }
        }
        if (low >= 0) tree.Rune = tree.Rune.Append(low, 0x10ffff);
        return Dump(tree);
    }
    private static bool IsUpperFold(int rune)
    {
        if (GoUnicode.Is(GoUnicode.Lu, rune)) return true;
        for (int fold = UnicodeCase.SimpleFold(rune); fold != rune; fold = UnicodeCase.SimpleFold(fold))
            if (GoUnicode.Is(GoUnicode.Lu, fold)) return true;
        return false;
    }
    private static readonly (string Pattern, string Dump)[] ParseTests = [
        ("a", "lit{a}"),
        ("a.", "cat{lit{a}dot{}}"),
        ("a.b", "cat{lit{a}dot{}lit{b}}"),
        ("ab", "str{ab}"),
        ("a.b.c", "cat{lit{a}dot{}lit{b}dot{}lit{c}}"),
        ("abc", "str{abc}"),
        ("a|^", "alt{lit{a}bol{}}"),
        ("a|b", "cc{0x61-0x62}"),
        ("(a)", "cap{lit{a}}"),
        ("(a)|b", "alt{cap{lit{a}}lit{b}}"),
        ("a*", "star{lit{a}}"),
        ("a+", "plus{lit{a}}"),
        ("a?", "que{lit{a}}"),
        ("a{2}", "rep{2,2 lit{a}}"),
        ("a{2,3}", "rep{2,3 lit{a}}"),
        ("a{2,}", "rep{2,-1 lit{a}}"),
        ("a*?", "nstar{lit{a}}"),
        ("a+?", "nplus{lit{a}}"),
        ("a??", "nque{lit{a}}"),
        ("a{2}?", "nrep{2,2 lit{a}}"),
        ("a{2,3}?", "nrep{2,3 lit{a}}"),
        ("a{2,}?", "nrep{2,-1 lit{a}}"),
        ("x{1001", "str{x{1001}"),
        ("x{9876543210", "str{x{9876543210}"),
        ("x{9876543210,", "str{x{9876543210,}"),
        ("x{2,1", "str{x{2,1}"),
        ("x{1,9876543210", "str{x{1,9876543210}"),
        ("", "emp{}"),
        ("|", "emp{}"),
        ("|x|", "alt{emp{}lit{x}emp{}}"),
        (".", "dot{}"),
        ("^", "bol{}"),
        ("$", "eol{}"),
        ("\\|", "lit{|}"),
        ("\\(", "lit{(}"),
        ("\\)", "lit{)}"),
        ("\\*", "lit{*}"),
        ("\\+", "lit{+}"),
        ("\\?", "lit{?}"),
        ("{", "lit{{}"),
        ("}", "lit{}}"),
        ("\\.", "lit{.}"),
        ("\\^", "lit{^}"),
        ("\\$", "lit{$}"),
        ("\\\\", "lit{\\}"),
        ("[ace]", "cc{0x61 0x63 0x65}"),
        ("[abc]", "cc{0x61-0x63}"),
        ("[a-z]", "cc{0x61-0x7a}"),
        ("[a]", "lit{a}"),
        ("\\-", "lit{-}"),
        ("-", "lit{-}"),
        ("\\_", "lit{_}"),
        ("abc", "str{abc}"),
        ("abc|def", "alt{str{abc}str{def}}"),
        ("abc|def|ghi", "alt{str{abc}str{def}str{ghi}}"),
        ("[[:lower:]]", "cc{0x61-0x7a}"),
        ("[a-z]", "cc{0x61-0x7a}"),
        ("[^[:lower:]]", "cc{0x0-0x60 0x7b-0x10ffff}"),
        ("[[:^lower:]]", "cc{0x0-0x60 0x7b-0x10ffff}"),
        ("(?i)[[:lower:]]", "cc{0x41-0x5a 0x61-0x7a 0x17f 0x212a}"),
        ("(?i)[a-z]", "cc{0x41-0x5a 0x61-0x7a 0x17f 0x212a}"),
        ("(?i)[^[:lower:]]", "cc{0x0-0x40 0x5b-0x60 0x7b-0x17e 0x180-0x2129 0x212b-0x10ffff}"),
        ("(?i)[[:^lower:]]", "cc{0x0-0x40 0x5b-0x60 0x7b-0x17e 0x180-0x2129 0x212b-0x10ffff}"),
        ("\\d", "cc{0x30-0x39}"),
        ("\\D", "cc{0x0-0x2f 0x3a-0x10ffff}"),
        ("\\s", "cc{0x9-0xa 0xc-0xd 0x20}"),
        ("\\S", "cc{0x0-0x8 0xb 0xe-0x1f 0x21-0x10ffff}"),
        ("\\w", "cc{0x30-0x39 0x41-0x5a 0x5f 0x61-0x7a}"),
        ("\\W", "cc{0x0-0x2f 0x3a-0x40 0x5b-0x5e 0x60 0x7b-0x10ffff}"),
        ("(?i)\\w", "cc{0x30-0x39 0x41-0x5a 0x5f 0x61-0x7a 0x17f 0x212a}"),
        ("(?i)\\W", "cc{0x0-0x2f 0x3a-0x40 0x5b-0x5e 0x60 0x7b-0x17e 0x180-0x2129 0x212b-0x10ffff}"),
        ("[^\\\\]", "cc{0x0-0x5b 0x5d-0x10ffff}"),
        ("\\p{Braille}", "cc{0x2800-0x28ff}"),
        ("\\P{Braille}", "cc{0x0-0x27ff 0x2900-0x10ffff}"),
        ("\\p{^Braille}", "cc{0x0-0x27ff 0x2900-0x10ffff}"),
        ("\\P{^Braille}", "cc{0x2800-0x28ff}"),
        ("\\pZ", "cc{0x20 0xa0 0x1680 0x2000-0x200a 0x2028-0x2029 0x202f 0x205f 0x3000}"),
        ("[\\p{Braille}]", "cc{0x2800-0x28ff}"),
        ("[\\P{Braille}]", "cc{0x0-0x27ff 0x2900-0x10ffff}"),
        ("[\\p{^Braille}]", "cc{0x0-0x27ff 0x2900-0x10ffff}"),
        ("[\\P{^Braille}]", "cc{0x2800-0x28ff}"),
        ("[\\pZ]", "cc{0x20 0xa0 0x1680 0x2000-0x200a 0x2028-0x2029 0x202f 0x205f 0x3000}"),
        ("\\p{Lu}", MkCharClass(static r => GoUnicode.Is(GoUnicode.Lu, r))),
        ("\\p{Uppercase_Letter}", MkCharClass(static r => GoUnicode.Is(GoUnicode.Lu, r))),
        ("\\p{upper case-let ter}", MkCharClass(static r => GoUnicode.Is(GoUnicode.Lu, r))),
        ("\\p{__upper case-let ter}", MkCharClass(static r => GoUnicode.Is(GoUnicode.Lu, r))),
        ("\\p{Canadian_Aboriginal}", MkCharClass(static r => GoUnicode.Is(RegexUnicodeTables._Canadian_Aboriginal, r))),
        ("[\\p{Lu}]", MkCharClass(static r => GoUnicode.Is(GoUnicode.Lu, r))),
        ("(?i)[\\p{Lu}]", MkCharClass(IsUpperFold)),
        ("\\p{Any}", "dot{}"),
        ("\\p{^Any}", "cc{}"),
        ("(?i)\\p{ascii}", "cc{0x0-0x7f 0x17f 0x212a}"),
        ("\\p{Assigned}", MkCharClass(static r => !GoUnicode.Is(RegexUnicodeTables._Cn, r))),
        ("\\p{^Assigned}", MkCharClass(static r => GoUnicode.Is(RegexUnicodeTables._Cn, r))),
        ("[\\012-\\234]\\141", "cat{cc{0xa-0x9c}lit{a}}"),
        ("[\\x{41}-\\x7a]\\x61", "cat{cc{0x41-0x7a}lit{a}}"),
        ("a{,2}", "str{a{,2}}"),
        ("\\.\\^\\$\\\\", "str{.^$\\}"),
        ("[a-zABC]", "cc{0x41-0x43 0x61-0x7a}"),
        ("[^a]", "cc{0x0-0x60 0x62-0x10ffff}"),
        (Raw("[α-ε☺]"), "cc{0x3b1-0x3b5 0x263a}"),
        ("a*{", "cat{star{lit{a}}lit{{}}"),
        ("(?:ab)*", "star{str{ab}}"),
        ("(ab)*", "star{cap{str{ab}}}"),
        ("ab|cd", "alt{str{ab}str{cd}}"),
        ("a(b|c)d", "cat{lit{a}cap{cc{0x62-0x63}}lit{d}}"),
        ("(?:a)", "lit{a}"),
        ("(?:ab)(?:cd)", "str{abcd}"),
        ("(?:a+b+)(?:c+d+)", "cat{plus{lit{a}}plus{lit{b}}plus{lit{c}}plus{lit{d}}}"),
        ("(?:a+|b+)|(?:c+|d+)", "alt{plus{lit{a}}plus{lit{b}}plus{lit{c}}plus{lit{d}}}"),
        ("(?:a|b)|(?:c|d)", "cc{0x61-0x64}"),
        ("a|.", "dot{}"),
        (".|a", "dot{}"),
        ("(?:[abc]|A|Z|hello|world)", "alt{cc{0x41 0x5a 0x61-0x63}str{hello}str{world}}"),
        ("(?:[abc]|A|Z)", "cc{0x41 0x5a 0x61-0x63}"),
        ("\\Q+|*?{[\\E", "str{+|*?{[}"),
        ("\\Q+\\E+", "plus{lit{+}}"),
        ("\\Qab\\E+", "cat{lit{a}plus{lit{b}}}"),
        ("\\Q\\\\E", "lit{\\}"),
        ("\\Q\\\\\\E", "str{\\\\}"),
        ("(?m)^", "bol{}"),
        ("(?m)$", "eol{}"),
        ("(?-m)^", "bot{}"),
        ("(?-m)$", "eot{}"),
        ("(?m)\\A", "bot{}"),
        ("(?m)\\z", "eot{\\z}"),
        ("(?-m)\\A", "bot{}"),
        ("(?-m)\\z", "eot{\\z}"),
        ("(?P<name>a)", "cap{name:lit{a}}"),
        ("(?<name>a)", "cap{name:lit{a}}"),
        ("[Aa]", "litfold{A}"),
        ("[\\x{100}\\x{101}]", Raw("litfold{Ā}")),
        (Raw("[Δδ]"), Raw("litfold{Δ}")),
        ("abcde", "str{abcde}"),
        ("[Aa][Bb]cd", "cat{strfold{AB}str{cd}}"),
        ("abc|abd|aef|bcx|bcy", "alt{cat{lit{a}alt{cat{lit{b}cc{0x63-0x64}}str{ef}}}cat{str{bc}cc{0x78-0x79}}}"),
        ("ax+y|ax+z|ay+w", "cat{lit{a}alt{cat{plus{lit{x}}lit{y}}cat{plus{lit{x}}lit{z}}cat{plus{lit{y}}lit{w}}}}"),
        ("(?:.)", "dot{}"),
        ("(?:x|(?:xa))", "cat{lit{x}alt{emp{}lit{a}}}"),
        ("(?:.|(?:.a))", "cat{dot{}alt{emp{}lit{a}}}"),
        ("(?:A(?:A|a))", "cat{lit{A}litfold{A}}"),
        ("(?:A|a)", "litfold{A}"),
        ("A|(?:A|a)", "litfold{A}"),
        ("(?s).", "dot{}"),
        ("(?-s).", "dnl{}"),
        ("(?:(?:^).)", "cat{bol{}dot{}}"),
        ("(?-s)(?:(?:^).)", "cat{bol{}dnl{}}"),
        ("[\\s\\S]a", "cat{cc{0x0-0x10ffff}lit{a}}"),
        ("abc|abd", "cat{str{ab}cc{0x63-0x64}}"),
        ("a(?:b)c|abd", "cat{str{ab}cc{0x63-0x64}}"),
        ("abc|abd|aef|bcx|bcy", "alt{cat{lit{a}alt{cat{lit{b}cc{0x63-0x64}}str{ef}}}" + "cat{str{bc}cc{0x78-0x79}}}"),
        ("abc|x|abd", "alt{str{abc}lit{x}str{abd}}"),
        ("(?i)abc|ABD", "cat{strfold{AB}cc{0x43-0x44 0x63-0x64}}"),
        ("[ab]c|[ab]d", "cat{cc{0x61-0x62}cc{0x63-0x64}}"),
        (".c|.d", "cat{dot{}cc{0x63-0x64}}"),
        ("x{2}|x{2}[0-9]", "cat{rep{2,2 lit{x}}alt{emp{}cc{0x30-0x39}}}"),
        ("x{2}y|x{2}[0-9]y", "cat{rep{2,2 lit{x}}alt{lit{y}cat{cc{0x30-0x39}lit{y}}}}"),
        ("a.*?c|a.*?b", "cat{lit{a}alt{cat{nstar{dot{}}lit{c}}cat{nstar{dot{}}lit{b}}}}"),
        ("((((((((((x{2}){2}){2}){2}){2}){2}){2}){2}){2}))", ""),
        ("((((((((((x{1}){2}){2}){2}){2}){2}){2}){2}){2}){2})", ""),
        (Repeat("(", 999) + Repeat(")", 999), ""),
        (Repeat("(?:", 999) + Repeat(")*", 999), ""),
        ("(" + Repeat("|", 12345) + ")", "")
    ];
    private static readonly (string Pattern, string Dump)[] FoldcaseTests = [
        ("AbCdE", "strfold{ABCDE}"),
        ("[Aa]", "litfold{A}"),
        ("a", "litfold{A}"),
        ("A[F-g]", "cat{litfold{A}cc{0x41-0x7a 0x17f 0x212a}}"),
        ("[[:upper:]]", "cc{0x41-0x5a 0x61-0x7a 0x17f 0x212a}"),
        ("[[:lower:]]", "cc{0x41-0x5a 0x61-0x7a 0x17f 0x212a}")
    ];
    private static readonly (string Pattern, string Dump)[] LiteralTests = [
        ("(|)^$.[*+?]{5,10},\\", "str{(|)^$.[*+?]{5,10},\\}")
    ];
    private static readonly (string Pattern, string Dump)[] MatchnlTests = [
        (".", "dot{}"),
        ("\n", "lit{\n}"),
        ("[^a]", "cc{0x0-0x60 0x62-0x10ffff}"),
        ("[a\\n]", "cc{0xa 0x61}")
    ];
    private static readonly (string Pattern, string Dump)[] NomatchnlTests = [
        (".", "dnl{}"),
        ("\n", "lit{\n}"),
        ("[^a]", "cc{0x0-0x9 0xb-0x60 0x62-0x10ffff}"),
        ("[a\\n]", "cc{0xa 0x61}")
    ];
    private static readonly string[] InvalidRegexps = [
        "(",
        ")",
        "(a",
        "a)",
        "(a))",
        "(a|b|",
        "a|b|)",
        "(a|b|))",
        "(a|b",
        "a|b)",
        "(a|b))",
        "[a-z",
        "([a-z)",
        "[a-z)",
        "([a-z]))",
        "x{1001}",
        "x{9876543210}",
        "x{2,1}",
        "x{1,9876543210}",
        "\u00ff",
        "[\u00ff]",
        "[\\\u00ff]",
        "\\\u00ff",
        "(?P<name>a",
        "(?P<name>",
        "(?P<name",
        "(?P<x y>a)",
        "(?P<>a)",
        "(?<name>a",
        "(?<name>",
        "(?<name",
        "(?<x y>a)",
        "(?<>a)",
        "[a-Z]",
        "(?i)[a-Z]",
        "\\Q\\E*",
        "a{100000}",
        "a{100000,}",
        "((((((((((x{2}){2}){2}){2}){2}){2}){2}){2}){2}){2})",
        Repeat("(", 1000) + Repeat(")", 1000),
        Repeat("(?:", 1000) + Repeat(")*", 1000),
        "(" + Repeat("(xx?)", 1000) + "){1000}",
        Repeat("(xx?){1000}", 1000),
        Repeat("\\pL", 27000)
    ];
    private static readonly string[] OnlyPerl = [
        "[a-b-c]",
        "\\Qabc\\E",
        "\\Q*+?{[\\E",
        "\\Q\\\\E",
        "\\Q\\\\\\E",
        "\\Q\\\\\\\\E",
        "\\Q\\\\\\\\\\E",
        "(?:a)",
        "(?P<name>a)"
    ];
    private static readonly string[] OnlyPOSIX = [
        "a++",
        "a**",
        "a?*",
        "a+*",
        "a{1}*",
        ".{1}{2}.{3}"
    ];
}

from pathlib import Path
import re,runpy
# Definitions only: running this draft does not execute production or tests.
g=Path('tools/generate-regex-checks.py').read_text().split('alltext=')[0]
n={};exec(g.replace('root=Path(__file__).resolve().parents[1]','root=Path.cwd()'),n)
raw,cs=n['raw'],n['cs'];src=Path('tools/.cache/go/src/regexp/syntax/parse_test.go').read_text()
tokpat=r'//[^\n]*|`[^`]*`|"(?:\\.|[^"\\])*"|[^\s]'
def split(text,sep):
 out=[];start=0;depth=0
 for m in re.finditer(tokpat,text):
  t=m.group()
  if t.startswith('//'):continue
  if t[0] in '`"':continue
  if t in '({[':depth+=1
  elif t in ')}]':depth-=1
  elif t==sep and depth==0:out.append(text[start:m.start()]);start=m.end()
 out.append(text[start:]);return out

def rows(name):
 text=src.split('var '+name+' = ',1)[1];a=text.index('{');depth=0
 for m in re.finditer(tokpat,text[a:]):
  t=m.group()
  if t.startswith('//') or t[0] in '`"':continue
  if t=='{':depth+=1
  elif t=='}':
   depth-=1
   if depth==0:
    body=text[a+1:a+m.start()];break
 body=re.sub(r'//[^\n]*','',body)
 return [x.strip() for x in split(body,',') if x.strip()]
def expr(text):
 text=text.strip()
 chunks=split(text,'+')
 if len(chunks)>1:return ' + '.join(expr(c) for c in chunks)
 if text.startswith('strings.Repeat('):
  parts=split(text[len('strings.Repeat('):-1],',');return 'Repeat('+expr(parts[0])+', '+parts[1].strip()+')'
 if text.startswith('mkCharClass'):
  if 'unicode.IsUpper' in text:return 'MkCharClass(static r => GoUnicode.Is(GoUnicode.Lu, r))'
  if 'isUpperFold' in text:return 'MkCharClass(IsUpperFold)'
  if 'Canadian_Aboriginal' in text:return 'MkCharClass(static r => GoUnicode.Is(RegexUnicodeTables._Canadian_Aboriginal, r))'
  if '!unicode.In' in text:return 'MkCharClass(static r => !GoUnicode.Is(RegexUnicodeTables._Cn, r))'
  if 'unicode.Cn' in text:return 'MkCharClass(static r => GoUnicode.Is(RegexUnicodeTables._Cn, r))'
  raise ValueError(text)
 return cs(raw(text))
header='''// Copyright 2011 The Go Authors. All rights reserved.
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
            case RegexOp.EndText: if ((tree.Flags & RegexFlags.WasDollar) == 0) output.Append("\\\\z"); break;
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
'''
for name in ['parseTests','foldcaseTests','literalTests','matchnlTests','nomatchnlTests']:
 rs=rows(name);out=[]
 for row in rs:
  fields=split(row[1:-1],',');assert len(fields)==2,(name,row,fields)
  out.append('        ('+expr(fields[0])+', '+expr(fields[1])+')')
 header+='    private static readonly (string Pattern, string Dump)[] '+name[0].upper()+name[1:]+' = [\n'+',\n'.join(out)+'\n    ];\n'
 print(name,len(rs))
for name in ['invalidRegexps','onlyPerl','onlyPOSIX']:
 header+='    private static readonly string[] '+name[0].upper()+name[1:]+' = [\n'+',\n'.join('        '+expr(row) for row in rows(name))+'\n    ];\n'
header+='}\n';Path('validation/LanguageServices.Checks/RegexSyntaxChecks.cs').write_text(header)

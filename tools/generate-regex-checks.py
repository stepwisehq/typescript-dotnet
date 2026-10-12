#!/usr/bin/env python3
"""Copy pinned Go literal tables for the selected Compile/MatchString assertions."""
from pathlib import Path
import re, json
root=Path(__file__).resolve().parents[1]
def raw(lit):
 if lit[0]=='`': return lit[1:-1].replace('\r','').encode('utf8')
 out=bytearray();s=lit[1:-1];i=0
 escapes={'a':7,'b':8,'f':12,'n':10,'r':13,'t':9,'v':11,'\\':92,'"':34,"'":39}
 while i<len(s):
  c=s[i];i+=1
  if c!='\\':out.extend(c.encode('utf8'));continue
  c=s[i];i+=1
  if c in escapes:out.append(escapes[c]);continue
  width={'x':2,'u':4,'U':8}.get(c)
  if width:
   n=int(s[i:i+width],16);i+=width
   out.extend(bytes([n]) if c=='x' else chr(n).encode('utf8'));continue
  if c in '01234567':out.append(int(c+s[i:i+2],8));i+=2;continue
  raise ValueError(c)
 return bytes(out)
def cs(data):
 try: text=data.decode('utf8')
 except UnicodeDecodeError: return json.dumps(data.decode('latin1'),ensure_ascii=True)
 return json.dumps(text,ensure_ascii=True) if data.isascii() else 'Raw('+json.dumps(text,ensure_ascii=False)+')'
strpat=r'`[^`]*`|"(?:\\.|[^"\\])*"'
alltext=(root/'tools/.cache/go/src/regexp/all_test.go').read_text()
good=alltext.split('var goodRe = []string{',1)[1].split('\n}',1)[0]
good=[raw(x.group()) for x in re.finditer(strpat,good)]
bad=alltext.split('var badRe = []stringError{',1)[1].split('\n}',1)[0]
badrows=[]
for line in bad.splitlines():
 toks=list(re.finditer(strpat,line))
 if not toks:continue
 pattern=raw(toks[0].group());error=raw(toks[1].group())
 if 'strings.Repeat' in line:pattern*=27000
 badrows.append((pattern,error))
find=(root/'tools/.cache/go/src/regexp/find_test.go').read_text().split('var findTests = []FindTest{',1)[1].split('\n}',1)[0]
find=re.sub(r'//[^\n]*','',find)
rows=[]
for row in re.findall(r'\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}',find):
 toks=list(re.finditer(strpat,row))
 if len(toks)!=2:raise ValueError(row)
 tail=row[toks[1].end():];m=re.match(r'\s*,\s*(-?\d+)\s*,\s*(nil|build\(\s*(\d+))',tail)
 if not m:raise ValueError(tail)
 if int(m[1])==0:continue
 rows.append((raw(toks[0].group()),raw(toks[1].group()),m[2]!='nil'))
header='''// Copyright 2009 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Generated from pinned Go1.27.1 regexp/all_test.go and find_test.go.
// TestMatch retains its MatchString assertion channel; byte Match API is outside selected closure.
using TypeScript.Scanner;
using TypeScript.RegularExpressions;
namespace LanguageServices.Checks;
public static class RegexChecks
{
    public static readonly string[] Names = ["TestGoodCompile", "TestBadCompile", "TestMatch.MatchString"];
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
                var longInput = ByteStringValue.FromString(new string('\\0', 262144 + 1));
                switchRegex.MatchString(longInput);
                switchRegex.MatchString(longInput.Slice(0, 1));
                break;
            default: throw new ArgumentException(name);
        }
    }
'''
header+='    private static readonly string[] Good = [\n'+',\n'.join('        '+cs(x) for x in good)+'\n    ];\n'
header+='    private static readonly (string Pattern, string Error)[] Bad = [\n'+',\n'.join('        ('+(cs(a) if len(a)<10000 else 'Repeat("\\\\pL", 27000)')+', '+cs(b)+')' for a,b in badrows)+'\n    ];\n'
header+='    private static string Raw(string text) => ByteStringView.FromUnicode(text).Materialize();\n    private static string Repeat(string text, int count) => string.Concat(Enumerable.Repeat(text, count));\n'
header+='    private static readonly (string Pattern, string Text, bool Expected)[] Matches = [\n'+',\n'.join('        ('+cs(a)+', '+cs(b)+', '+str(c).lower()+')' for a,b,c in rows)+'\n    ];\n}\n'
(root/'validation/LanguageServices.Checks/RegexChecks.cs').write_text(header)
print(json.dumps({'good':len(good),'bad':len(badrows),'matchStringRows':len(rows)}))

from pathlib import Path
import runpy,re
m=globals();root=Path(__file__).resolve().parents[1]
p=root/'validation/LanguageServices.Checks/RegexChecks.cs';s=p.read_text()
s=s.replace('"TestMatch.MatchString"]','"TestMatch.MatchString", "TestCompileOnePass", "TestRunOnePass", "TestOnePassCutoff", "TestMinInputLen", "TestProgramTooLongForBacktrack", "TestMergeRuneSet", "TestSwitchBacktrack.MatchStringAdaptation"]')
s=s.replace('using TypeScript.RegularExpressions;','using TypeScript.RegularExpressions;\nusing TypeScript.RegularExpressions.Syntax;')
needle='            default: throw new ArgumentException(name);'
new='''            case "TestCompileOnePass":
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
'''+needle
s=s.replace(needle,new)
text=(root/'tools/.cache/go/src/regexp/onepass_test.go').read_text();table=text.split('var onePassTests = []struct',1)[1].split('}{',1)[1].split('\n}',1)[0]
rows=[]
for line in table.splitlines():
 toks=list(re.finditer(m['strpat'],line))
 if not toks:continue
 pat=raw(toks[0].group())
 if 'strings.Repeat' in line:pat+=b'o'*(2<<8)+raw(toks[-1].group())
 rows.append((pat,'true' if re.search(r',\s*true',line) else 'false'))
s=s[:-2]+'    private static readonly (string Pattern, bool Expected)[] OnePass = [\n'+',\n'.join('        ('+cs(a)+', '+b+')' for a,b in rows)+'\n    ];\n}\n'
table=m['alltext'].split('var minInputLenTests = []struct',1)[1].split('}{',1)[1].split('\n}',1)[0];rows=[]
for line in table.splitlines():
 toks=list(re.finditer(m['strpat'],line))
 if toks:rows.append((raw(toks[0].group()),int(re.search(r',\s*(\d+)',line[toks[0].end():])[1])))
s=s[:-2]+'    private static readonly (string Pattern, int Expected)[] MinimumLengths = [\n'+',\n'.join('        ('+cs(a)+', '+str(b)+')' for a,b in rows)+'\n    ];\n}\n'
execsrc=(root/'tools/.cache/go/src/regexp/exec_test.go').read_text().split('func TestProgramTooLongForBacktrack',1)[1]
longpat=raw(re.search(m['strpat'],execsrc).group());s=s[:-2]+'    private const string LongPattern = '+cs(longpat)+';\n}\n';p.write_text(s)
print('onepass',len(re.findall(r', (?:true|false)\)',s.split('[] OnePass',1)[1].split('];',1)[0])))

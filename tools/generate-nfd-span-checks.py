from pathlib import Path
import re
scope={};exec(Path('tools/generate-regex-checks.py').read_text().split('alltext=')[0].replace('root=Path(__file__).resolve().parents[1]','root=Path.cwd()'),scope)
src=Path('/home/greg/go/pkg/mod/golang.org/x/text@v0.42.0/unicode/norm/normalize_test.go').read_text();rows=[]
for name in ['quickSpanTests','quickSpanNFDTests']:
 body=src.split('var '+name+' = []spanTest{',1)[1].split('\n}',1)[0]
 for line in body.splitlines():
  m=re.match(r'\s*\{(.*), (true|false), (\d+), (nil|transform.Err\w+)\}',line)
  if not m:continue
  expr,eof,n,err=m.groups();parts=[]
  for term in re.split(r'\s*\+\s*',expr):
   if term=='cgj':parts.append(chr(0x034f).encode())
   elif term.startswith('rep('):
    r,c=map(lambda x:int(x.strip(),0),term[4:-1].split(','));parts.append(chr(r).encode()*c)
   else:parts.append(scope['raw'](term))
  rows.append((b''.join(parts),eof,n,{'nil':0,'transform.ErrEndOfSpan':1,'transform.ErrShortSrc':2}[err]))
text='''// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// normalize_test.go TestSpan: selected NFD rows through its actual quickSpan worker.
// The test-only error projection copies normalize.go:288 SpanString; public byte
// and non-NFD form APIs are not implemented or credited by this channel.
using TypeScript.Scanner;
using TypeScript.UnicodeNormalization;
namespace LanguageServices.Checks;
public static class NormalizationSpanChecks
{
    private static string Raw(string text) => ByteStringView.FromUnicode(text).Materialize();
    public static int RunAll()
    {
        (string Input, bool AtEof, int Length, int Error)[] rows = [
'''
for b,e,n,err in rows:text+='            ('+scope['cs'](b)+', '+e+', '+n+', '+str(err)+'),\n'
text+='''        ];
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var (length, complete) = Norm.QuickSpan(ByteStringValue.FromString(row.Input), 0, row.Input.Length, row.AtEof);
            int error = length < row.Input.Length ? (complete ? 2 : 1) : 0;
            if (length != row.Length || error != row.Error)
                throw new InvalidOperationException("Original NFD span channel row " + index);
        }
        return 1;
    }
}
'''
Path('validation/LanguageServices.Checks/NormalizationSpanChecks.cs').write_text(text)
p=Path('src/TypeScript.Scanner/UnicodeNormalization/Normalize.cs');s=p.read_text().replace('private static (int Length, bool Complete) QuickSpan','internal static (int Length, bool Complete) QuickSpan');p.write_text(s)
p=Path('tests/LanguageServices.Tests/NormalizationTests.cs');s=p.read_text().replace('    [Fact]\n    public void OriginalCanonicalPropertyChannel()', '    [Fact]\n    public void OriginalNfdSpanChannel() => Assert.Equal(1, NormalizationSpanChecks.RunAll());\n    [Fact]\n    public void OriginalCanonicalPropertyChannel()');p.write_text(s)
print(len(rows))

#!/usr/bin/env python3
"""Retain original Fowler compile-admission/MatchString channels without capture APIs."""
from pathlib import Path
import re,json
scope={};prefix=Path('tools/generate-regex-checks.py').read_text().split('alltext=')[0]
exec(prefix.replace('root=Path(__file__).resolve().parents[1]','root=Path.cwd()'),scope)
raw,cs=scope['raw'],scope['cs'];rows=[];skips=[]
for p in sorted(Path('tools/.cache/go/src/regexp/testdata').glob('*.dat')):
 last=b''
 for n,line in enumerate(p.read_text().splitlines(),1):
  if not line or line.startswith('#'):continue
  field=[f for f in line.split('\t') if f]
  if 'NIL' in field:skips.append((p.name,n,'original NIL'));continue
  field=['' if f=='NULL' else f for f in field]
  if not field:continue
  flag=field[0]
  if not flag:raise ValueError((p,n,line))
  if flag[0] in '?&|;{}':
   flag=flag[1:]
   if not flag:skips.append((p.name,n,'original control'));continue
  elif flag[0]==':':
   if ':' not in flag[1:]:skips.append((p.name,n,'original comment'));continue
   flag=flag[1:].split(':',1)[1]
  elif flag[0] in 'CNT0123456789':skips.append((p.name,n,'original note/control'));continue
  if len(field)<4:raise ValueError((p,n,line))
  pattern=raw('"'+field[1]+'"') if '$' in flag else field[1].encode('utf8')
  text=raw('"'+field[2]+'"') if '$' in flag else field[2].encode('utf8')
  if pattern==b'SAME':pattern=last
  last=pattern;result=field[3]
  compiled=result=='' or result=='NOMATCH' or not ('A'<=result[0]<='Z')
  matched=compiled and result!='NOMATCH'
  if result and result[0]=='(':assert re.fullmatch(r'(?:\((?:-?\d+|\?),(?:-?\d+|\?)\))+',result),(p,n,result)
  modes=[mode for mode in flag if mode in 'EL']
  if not modes:skips.append((p.name,n,'original unsupported modes'))
  for mode in modes:
   pat=pattern
   if mode=='L':pat=b''.join((b'\\'+bytes([c])) if c in b'\\.+*?()|[]{}^$' else bytes([c]) for c in pat)
   rows.append((p.name,n,pat,text,'RegexFlags.ClassNL'+(' | RegexFlags.FoldCase' if 'i' in flag else ''),compiled,matched))
header='''// Copyright 2010 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Original regexp/exec_test.go TestFowler compile-admission/MatchString channels.
// Capture indices are excluded; original supported-mode/control skips retained.
// Go C literal unquote/QuoteMeta fixture transport occurs losslessly in generation.
using TypeScript.Scanner;
using TypeScript.RegularExpressions;
using TypeScript.RegularExpressions.Syntax;
namespace LanguageServices.Checks;
public static class RegexFowlerChecks
{
    public static int RunAll()
    {
        int matchedRows = 0, rejectedRows = 0;
        foreach (var row in Rows)
        {
            var (regex, error) = GoRegexp.Compile(ByteStringValue.FromString(row.Pattern), row.Flags, true);
            if ((error == null) != row.Compiled)
                throw new InvalidOperationException($"{row.File}:{row.Line}: incorrect compile admission: {error?.Message}");
            if (error != null) { rejectedRows++; continue; }
            matchedRows++;
            if (regex == null || regex.MatchString(ByteStringValue.FromString(row.Input)) != row.Matched)
                throw new InvalidOperationException($"{row.File}:{row.Line}: incorrect Fowler match");
        }
        Console.WriteLine($"REGEX_FOWLER_SUMMARY consumedRows={Rows.Length} matchedRows={matchedRows} originalCompileRejections={rejectedRows}");
        PrintOriginalSkips();
        return 1;
    }
    private static string Raw(string text) => ByteStringView.FromUnicode(text).Materialize();
    private static readonly (string File, int Line, string Pattern, string Input, RegexFlags Flags, bool Compiled, bool Matched)[] Rows = [
'''
from collections import Counter
counts=Counter(reason for _,_,reason in skips)
header+=',\n'.join('        ('+json.dumps(a)+', '+str(b)+', '+cs(c)+', '+cs(d)+', '+e+', '+str(f).lower()+', '+str(g).lower()+')' for a,b,c,d,e,f,g in rows)+'\n    ];\n    private static void PrintOriginalSkips()\n    {\n'+''.join('        Console.WriteLine('+json.dumps('REGEX_FOWLER_SKIP '+reason+' rows='+str(count))+');\n' for reason,count in sorted(counts.items()))+'    }\n}\n'
Path('validation/LanguageServices.Checks/RegexFowlerChecks.cs').write_text(header)
print(json.dumps({'selectedRows':len(rows),'originalSkipRows':len(skips),'byFile':{p:sum(x[0]==p for x in rows) for p in sorted({x[0] for x in rows})}}))

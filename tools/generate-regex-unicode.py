#!/usr/bin/env python3
# Generate exact Go1.27.1 Unicode17 regexp matching-table closure; no runtime introspection.
from pathlib import Path
import re, hashlib
import argparse
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--source',type=Path,default=root/'tools/.cache/go/src/unicode/tables.go')
args=parser.parse_args()
source=args.source
s=source.read_text(); assert 'const Version = "17.0.0"' in s
maps={}
for name in ['Categories','Scripts','FoldCategory','FoldScript']:
 body=re.search(r'var '+name+r' = map\[string\]\*RangeTable\{(.*?)\n\}',s,re.S).group(1)
 maps[name]=dict(re.findall(r'"([^"]+)":\s*(\w+)',body))
# Public category/script variables alias their original generated storage; retain map identity.
aliases=dict(re.findall(r'^\s*(\w+)\s*=\s*(_\w+)\s*(?://[^\n]*)?$',s,re.M))
needed={aliases.get(v,v) for entries in maps.values() for v in entries.values()}
out=['// Copyright 2009 The Go Authors. All rights reserved.','// BSD-style license: licenses/Go-LICENSE.txt.','// Go 1.27.1 Unicode 17.0.0 exact selected regexp category/script/fold tables.','// Source SHA256: '+hashlib.sha256(source.read_bytes()).hexdigest(),'using TypeScript.Scanner;','namespace TypeScript.RegularExpressions.Syntax;','internal static class RegexUnicodeTables','{']
for name in sorted(needed):
 if name in ['_Mn','_Lu','_Ll','_L','_Nd']:continue
 m=re.search(r'var '+re.escape(name)+r' = &RangeTable\{(.*?)\n\}',s,re.S)
 assert m,name
 body=m.group(1);fields=[]
 for width in ['16','32']:
  match=None if re.search(r'R'+width+r': \[\]Range'+width+r'\{\}',body) else re.search(r'R'+width+r': \[\]Range'+width+r'\{(.*?)\n\s*\}',body,re.S)
  # Empty inline arrays occur in several Unicode17 scripts.
  rows=[] if not match or match.group(1).strip()=='' else re.findall(r'\{(0x[\da-fA-F]+|\d+), (0x[\da-fA-F]+|\d+), (\d+)\}',match.group(1))
  fields.append('['+','.join('new('+','.join(row)+')' for row in rows)+']')
 offset=re.search(r'LatinOffset:\s*(\d+)',body)
 out.append('    internal static readonly GoUnicode.RangeTable '+name+' = new('+','.join(fields)+','+(offset.group(1) if offset else '0')+');')
for name,entries in maps.items():
 out+=['    internal static readonly Dictionary<ByteStringKey, GoUnicode.RangeTable> '+name+' = new()','    {']
 for key,value in entries.items():out.append('        [ByteStringKey.FromString("'+key+'")] = '+({'_Mn':'GoUnicode.Mn','_Lu':'GoUnicode.Lu','_Ll':'GoUnicode.Ll','_L':'GoUnicode.L','_Nd':'GoUnicode.Nd'}.get(aliases.get(value,value),aliases.get(value,value)))+',')
 out+=['    };']
body=re.search(r'var CategoryAliases = map\[string\]string\{(.*?)\n\}',s,re.S).group(1)
out+=['    internal static readonly Dictionary<ByteStringKey, ByteStringValue> CategoryAliases = new()','    {']
for key,value in re.findall(r'"([^"]+)":\s*"([^"]+)"',body):out.append('        [ByteStringKey.FromString("'+key+'")] = ByteStringValue.FromString("'+value+'"),')
out+=['    };','}']
(root/'src/TypeScript.Scanner/RegularExpressions/Syntax/UnicodeTables.Generated.cs').write_text('\n'.join(out)+'\n')
print(len(needed), 'selected range table owners;', sum(map(len,maps.values())), 'map entries')

# Shared service consumers and regexp category maps retain these same table owners.
shared = ['// Copyright 2009 The Go Authors. All rights reserved.',
          '// BSD-style license: licenses/Go-LICENSE.txt.',
          '// Go1.27.1 unicode/tables.go Unicode17 shared Ll/L/Nd owners.',
          '// Source SHA256: '+hashlib.sha256(source.read_bytes()).hexdigest(),
          'namespace TypeScript.Scanner;', 'internal static partial class GoUnicode', '{']
for name in ['Ll', 'L', 'Nd']:
 body=re.search(r'var _'+name+r' = &RangeTable\{(.*?)\n\}',s,re.S).group(1)
 fields=[]
 for width in ['16','32']:
  match=re.search(r'R'+width+r': \[\]Range'+width+r'\{(.*?)\n\s*\}',body,re.S)
  rows=[] if not match else re.findall(r'\{(0x[\da-fA-F]+|\d+), (0x[\da-fA-F]+|\d+), (\d+)\}',match.group(1))
  fields.append('['+','.join('new('+','.join(row)+')' for row in rows)+']')
 offset=re.search(r'LatinOffset:\s*(\d+)',body)
 shared.append('    internal static readonly RangeTable '+name+' = new('+','.join(fields)+','+(offset.group(1) if offset else '0')+');')
shared.append('}')
(root/'src/TypeScript.Scanner/Runtime/UnicodeServiceCategories.Generated.cs').write_text('\n'.join(shared)+'\n')

#!/usr/bin/env python3
# Exact Go1.27.1 regexp/syntax/perl_groups.go selected parser data.
from pathlib import Path
import argparse, hashlib, re, json
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--source',type=Path,default=root/'tools/.cache/go/src/regexp/syntax/perl_groups.go')
source=parser.parse_args().source
text=source.read_text()
output=['// Copyright 2013 The Go Authors. All rights reserved.', '// BSD-style license: licenses/Go-LICENSE.txt.', '// Go1.27.1 regexp/syntax/perl_groups.go exact generated data.', '// Source SHA256: '+hashlib.sha256(source.read_bytes()).hexdigest(), 'using TypeScript.Scanner;', 'namespace TypeScript.RegularExpressions.Syntax;', 'internal readonly record struct CharGroup(int Sign, RegexSlice<int> Class);', 'internal static class RegexCharGroups', '{']
for name,body in re.findall(r'var (code\d+) = \[\]rune\{(.*?)\n\}',text,re.S):
 body=re.sub(r'/\*.*?\*/','',body,flags=re.S)
 values=re.findall(r'0x[\da-fA-F]+|\d+',body)
 output.append('    private static readonly RegexSlice<int> '+name+' = RegexSlice<int>.FromArray(['+','.join(values)+']);')
for name in ['perlGroup','posixGroup']:
 body=re.search(r'var '+name+r' = map\[string\]charGroup\{(.*?)\n\}',text,re.S).group(1)
 output+=['    internal static readonly Dictionary<ByteStringKey, CharGroup> '+name+' = new()', '    {']
 for key,sign,array in re.findall(r'`([^`]+)`:\s*\{([+-]1), (code\d+)\}',body):
  output.append('        [ByteStringKey.FromString('+json.dumps(key)+')] = new('+sign+','+array+'),')
 output+=['    };']
output+=['}']
(root/'src/TypeScript.Scanner/RegularExpressions/Syntax/PerlGroups.Generated.cs').write_text('\n'.join(output)+'\n')

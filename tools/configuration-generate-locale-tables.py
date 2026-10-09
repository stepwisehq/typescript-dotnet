#!/usr/bin/env python3
"""Copy complete x/text v0.42.0 parse/lookup registry slices from pinned source."""
import argparse, hashlib, json, re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--source-root', type=Path, default=Path.home()/'go/pkg/mod/golang.org/x/text@v0.42.0')
parser.add_argument('--check', action='store_true')
args=parser.parse_args()
SOURCE=args.source_root/'internal/language/tables.go'
OUTPUT=ROOT/'src/TypeScript.Scanner/Locale/XText/InternalLanguage/Tables.cs'
if hashlib.sha256(SOURCE.read_bytes()).hexdigest() != '876a73fb8b8fffd239efa592ffdf903c25757b4d17b715fc80d7aee8db2b985e': raise SystemExit('Pinned x/text registry source drift')
text=SOURCE.read_text()
header='''// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by the BSD-style license in licenses/XText-LICENSE.txt.
// Generated from golang.org/x/text v0.42.0 internal/language/tables.go.
// Only complete registry/alias tables demanded by Parse; matching/likely-subtag tables excluded.
namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
'''
def csstr(s):
 return '"'+''.join('\\u%04x'%ord(c) if ord(c)<32 or ord(c)>126 else '\\'+c if c in '\\"' else c for c in s)+'"'
# Go interpreted string literals used in table constants consist of ASCII+byte escapes.
def gostr(lit):
 return bytes(lit[1:-1], 'ascii').decode('unicode_escape')
for name in ['lang','altLangISO3','script','regionISO','altRegionISO3']:
 m=re.search(r'const '+name+r' (?:tag.Index|string) = (.*?)(?=\n\n)',text,re.S)
 lits=re.findall(r'"(?:\\.|[^"\\])*"', re.sub(r'//[^\n]*','',m.group(1)))
 values=''.join(gostr(s) for s in lits)
 header+='    private const string '+name+' =\n'+''.join('        '+csstr(values[i:i+80])+(' +\n' if i+80<len(values) else ';\n') for i in range(0,len(values),80))
for name in ['langNoIndex','altLangIndex','AliasTypes','altRegionIDs','m49','m49Index','fromM49']:
 m=re.search(r'var '+name+r' = \[(\d+)\]\w+\{(.*?)\n\}',text,re.S)
 body=re.sub(r'//[^\n]*','',m.group(2))
 vals=re.findall(r'0x[0-9a-f]+|\b\d+\b',body)
 assert len(vals)==int(m.group(1)),(name,len(vals))
 header+='    private static readonly int[] '+name+' =\n    [\n'+''.join('        '+', '.join(vals[i:i+12])+',\n' for i in range(0,len(vals),12))+'    ];\n'
for name in ['AliasMap','regionOldMap']:
 m=re.search(r'var '+name+r' = \[(\d+)\]FromTo\{(.*?)\n\}',text,re.S)
 vals=re.findall(r'From: (0x[0-9a-f]+|\d+), To: (0x[0-9a-f]+|\d+)',m.group(2))
 assert len(vals)==int(m.group(1))
 header+='    private static readonly (int From, int To)[] '+name+' =\n    [\n'+''.join('        ('+a+', '+b+'),\n' for a,b in vals)+'    ];\n'
body=re.search(r'var variantIndex = map\[string\]uint8\{(.*?)\n\}',text,re.S).group(1)
vals=re.findall(r'"([^"]+)":\s*(0x[0-9a-f]+|\d+)',body)
header+='    private static readonly Dictionary<string, int> variantIndex = new(StringComparer.Ordinal)\n    {\n'+''.join('        ["'+a+'"] = '+b+',\n' for a,b in vals)+'    };\n'
for name in ['langNoIndexOffset','nonCanonicalUnd','isoRegionOffset']:
 val=re.search(r'const '+name+r' = (\d+)',text).group(1)
 header+='    private const int '+name+' = '+val+';\n'
compact_source=SOURCE.parent/'compact/tables.go'
if hashlib.sha256(compact_source.read_bytes()).hexdigest() != '99415fb6642ebd291f825497d32e70ae32da8569815d8f4ef587664c6c5ce7d5': raise SystemExit('Pinned x/text compact registry source drift')
compact=compact_source.read_text()
body=re.search(r'var coreTags = \[\]language.CompactCoreInfo\{(.*?)\n\}',compact,re.S).group(1)
body=re.sub(r'//[^\n]*','',body)
values=re.findall(r'0x[0-9a-f]+|\b\d+\b',body)
assert len(values)==773, 'Complete compact core registry count'
header+='    private static readonly uint[] coreTags =\n    [\n'+''.join('        '+', '.join(values[i:i+10])+',\n' for i in range(0,len(values),10))+'    ];\n'
header+='}\n'
provenance = json.dumps({'repository':'https://go.googlesource.com/text','revision':'v0.42.0','source':'internal/language/tables.go','sourceSha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'compactSource':'internal/language/compact/tables.go','compactSourceSha256':hashlib.sha256(compact_source.read_bytes()).hexdigest(),'generatedSha256':hashlib.sha256(header.encode()).hexdigest(),'scope':'complete Parse registry/alias tables; matcher/likely-subtag tables omitted'},indent=2)+'\n'
if args.check:
    if OUTPUT.read_text()!=header or OUTPUT.with_suffix('.provenance.json').read_text()!=provenance:
        raise SystemExit('x/text registry tables or provenance are stale')
else:
    OUTPUT.write_text(header)
    OUTPUT.with_suffix('.provenance.json').write_text(provenance)

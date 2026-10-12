#!/usr/bin/env python3
"""Pinned x/text matching tables; drafts stay outside compilation until owner closure."""
from pathlib import Path
import argparse,hashlib,json,re
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--source-root',type=Path,default=Path.home()/'go/pkg/mod/golang.org/x/text@v0.42.0')
parser.add_argument('--check',action='store_true')
args=parser.parse_args()
source=args.source_root
root=Path(__file__).resolve().parents[1]
out=root/'src/TypeScript.Scanner/Locale/XText/InternalLanguage'
sources=[source/'internal/language/tables.go',source/'language/tables.go']
expected=['876a73fb8b8fffd239efa592ffdf903c25757b4d17b715fc80d7aee8db2b985e','cb3c6aa474902c2728c47bcdebd5fcac0c6aa0f7e29934e984d46434c98ce716']
for path,digest in zip(sources,expected):
    if hashlib.sha256(path.read_bytes()).hexdigest()!=digest: raise SystemExit('Pinned x/text matcher table drift: '+str(path))
texts=[p.read_text() for p in sources]
header='''// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by licenses/XText-LICENSE.txt.
// Generated from x/text v0.42.0; complete matching/likely-subtag table owners.
namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
'''
for go,fields in [('likelyLangRegion',['lang','region']),('likelyScriptRegion',['region','script','flags']),('likelyLangScript',['lang','script','flags']),('likelyTag',['lang','region','script']),('mutualIntelligibility',['want','have','distance','oneway']),('scriptIntelligibility',['wantLang','haveLang','wantScript','haveScript','distance']),('regionIntelligibility',['lang','script','group','distance'])]:
    header+='    private readonly record struct '+go+'('+', '.join(('bool' if f=='oneway' else 'int')+' '+f for f in fields)+');\n'
    pattern=r'var (\w+) = \[(\d*)\]'+go+r'\{(.*?)\n\}'
    for text in texts:
        for name,count,body in re.findall(pattern,text,re.S):
            entries=re.findall(r'(\d+):\s*\{([^}]+)\}',body)
            if not entries: raise ValueError(name)
            size=int(count) if count else max(int(i) for i,_ in entries)+1
            values=['new('+', '.join('false' if f=='oneway' else '0' for f in fields)+')']*size
            for index,item in entries:
                pairs=dict(re.findall(r'(\w+):\s*(0x[0-9a-f]+|\d+|true|false)',item))
                values[int(index)]='new('+', '.join(pairs.get(f,'false' if f=='oneway' else '0') for f in fields)+')'
            header+='    private static readonly '+go+'[] '+name+' =\n    [\n'+''.join('        '+v+',\n' for v in values)+'    ];\n'
for text in texts:
    for name,count,kind,body in re.findall(r'var (regionInclusion|regionInclusionBits|regionInclusionNext|regionContainment|regionToGroups|suppressScript) = \[(\d*)\](uint8|uint64)\{(.*?)\n\}',text,re.S):
        body=re.sub(r'//[^\n]*','',body)
        values=re.findall(r'0x[0-9a-f]+|\b\d+\b',body)
        if count: assert len(values)==int(count),(name,len(values),count)
        header+='    private static readonly '+('ulong' if kind=='uint64' else 'int')+'[] '+name+' =\n    [\n'+''.join('        '+', '.join(values[i:i+8])+',\n' for i in range(0,len(values),8))+'    ];\n'
body=re.search(r'var paradigmLocales = \[\]\[3\]uint16\{(.*?)\n\}',texts[1],re.S).group(1)
entries=re.findall(r'\d+: \[3\]uint16\{([^}]+)\}',body)
assert len(entries)==3
header+='    private static readonly int[][] paradigmLocales =\n    [\n'+''.join('        ['+entry+'],\n' for entry in entries)+'    ];\n'
header+='    private const int nRegionGroups = 33;\n}\n'
provenance=json.dumps({'revision':'v0.42.0','sources':{str(p.relative_to(source)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sources},'outputSha256':hashlib.sha256(header.encode()).hexdigest(),'scope':'complete matching/likely-subtag table owners required by diagnostic locale matcher'},indent=2)+'\n'
output=out/'MatcherTables.Generated.cs'
manifest=out/'MatcherTables.provenance.json'
if args.check:
    if output.read_text()!=header or manifest.read_text()!=provenance: raise SystemExit('Matcher tables/provenance are stale')
else:
    output.write_text(header)
    manifest.write_text(provenance)

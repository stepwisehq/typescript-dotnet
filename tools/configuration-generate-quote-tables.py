#!/usr/bin/env python3
"""Copy only the pinned Go strconv print tables; no runtime-derived expectations."""
import hashlib
import json
import re
from pathlib import Path
import sys
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'tools/.cache/go/src/strconv/isprint.go'
OUTPUT = ROOT / 'src/TypeScript.Scanner/Runtime/GoQuoteTables.cs'
PROVENANCE = ROOT / 'src/TypeScript.Scanner/Runtime/GoQuoteTables.provenance.json'
text = SOURCE.read_text()
header = '''// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by the BSD-style license in licenses/Go-LICENSE.txt.
// Generated from go1.27.1 src/strconv/isprint.go by tools/configuration-generate-quote-tables.py.
namespace TypeScript.Scanner;
internal static partial class GoQuote
{
'''
for name in ['isPrint16', 'isNotPrint16', 'isPrint32', 'isNotPrint32']:
    body = re.search('var ' + name + r' = \[\]uint(?:16|32)\{(.*?)\n\}', text, re.S).group(1)
    body = re.sub(r'//[^\n]*', '', body)
    values = re.findall(r'0x[0-9a-f]+', body)
    header += '    private static readonly int[] ' + name + ' =\n    [\n' + ''.join('        ' + ', '.join(values[i:i+10]) + ',\n' for i in range(0, len(values), 10)) + '    ];\n'
header += '}\n'
provenance = json.dumps({'repository': 'https://go.googlesource.com/go', 'revision': 'go1.27.1', 'source': 'src/strconv/isprint.go', 'sourceSha256': hashlib.sha256(SOURCE.read_bytes()).hexdigest(), 'generatedSha256': hashlib.sha256(header.encode()).hexdigest()}, indent=2) + '\n'
if '--check' in sys.argv:
    if OUTPUT.read_text() != header or PROVENANCE.read_text() != provenance:
        raise SystemExit('GoQuote print tables or provenance are stale')
else:
    OUTPUT.write_text(header)
    PROVENANCE.write_text(provenance)

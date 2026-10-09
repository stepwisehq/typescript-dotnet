"""Extract pinned typed-path test literals, never managed implementation outputs."""
from pathlib import Path
import ast
import hashlib
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'upstream/TypeScript/tsc/internal/tspath/typed_paths_test.go'
OUTPUT = ROOT / 'validation/Configuration.Checks/TypedPathChecks.Generated.cs'
STR = r'"(?:\\.|[^"\\])*"'
text = SOURCE.read_text()

def function(name):
    start = re.search(r'^func ' + name + r'\(', text, re.M)
    end = re.search(r'^func ', text[start.end():], re.M)
    return text[start.start():start.end() + end.start()] if end else text[start.start():]

def literal(value):
    return json.dumps(ast.literal_eval(value), ensure_ascii=True)

stems = re.findall(r'\{\s*(' + STR + r'),\s*(' + STR + r'),\s*(' + STR + r')\s*\}', function('TestFileNameStemsPreserveFilenamePrefixes').split('t.Run(')[0])
comparisons = re.findall(r'\{\s*(' + STR + r'),\s*(' + STR + r'),\s*(true|false),\s*(true|false)\s*\}', function('TestCompareFileNameStemsKeepsLiteralDotComponentsAndCasingPolicy'))
contains = []
for line in function('TestContainsFilePath').splitlines():
    if not re.match(r'\s*\{name:', line):
        continue
    pairs = dict(re.findall(r'(\w+):\s*(' + STR + r'|true|false|CaseSensitive|CaseInsensitive)', line))
    contains.append([literal(pairs['directory']), literal(pairs['fileName']), literal(pairs.get('relative', '""')), str(pairs['caseSensitivity'] == 'CaseInsensitive').lower(), pairs.get('contained', 'false')])
if (len(stems), len(comparisons), len(contains)) != (11, 6, 11):
    raise SystemExit('Typed path literal domain drift')
output = '''// Copyright (c) Microsoft Corporation. Apache 2.0: licenses/TypeScript-LICENSE.txt.
// Generated unchanged original typed_paths_test.go literals, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace Configuration.Checks;
public static partial class TypedPathChecks
{
    private sealed record StemRow(string File, string Stem, string Output);
    private sealed record StemCompareRow(string First, string Second, bool Sensitive, bool Insensitive);
    private sealed record ContainsRow(string Directory, string File, string Relative, bool Insensitive, bool Contained);
    private static readonly StemRow[] stemRows =
    [
'''
output += ''.join('        new(' + ', '.join(literal(item) for item in row) + '),\n' for row in stems)
output += '    ];\n    private static readonly StemCompareRow[] stemCompareRows =\n    [\n'
output += ''.join('        new(' + ', '.join([literal(row[0]), literal(row[1]), row[2], row[3]]) + '),\n' for row in comparisons)
output += '    ];\n    private static readonly ContainsRow[] containsRows =\n    [\n'
output += ''.join('        new(' + ', '.join(row) + '),\n' for row in contains)
output += '    ];\n}\n'
if '--check' in sys.argv:
    if OUTPUT.read_text() != output:
        raise SystemExit('Typed path generated literals drift')
else:
    OUTPUT.write_text(output)
print(json.dumps({'sourceSha256': hashlib.sha256(SOURCE.read_bytes()).hexdigest(), 'stemRows': len(stems), 'comparisonRows': len(comparisons), 'containmentRows': len(contains)}))

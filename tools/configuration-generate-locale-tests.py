"""Extract only locked x/text literal expectations; no implementation produces them."""
from pathlib import Path
import ast
import argparse
import hashlib
import json
import re
import sys
import subprocess

ROOT = Path(__file__).resolve().parents[1]
arguments = argparse.ArgumentParser(description=__doc__)
arguments.add_argument('--source', type=Path, help='Locked golang.org/x/text v0.42.0 source directory')
arguments.add_argument('--check', action='store_true')
options = arguments.parse_args()
if options.source is not None:
    SOURCE = options.source
else:
    go = ROOT / 'tools/.cache/go/bin/go'
    module_cache = subprocess.check_output([str(go), 'env', 'GOMODCACHE'], text=True).strip()
    SOURCE = Path(module_cache) / 'golang.org/x/text@v0.42.0'
if not SOURCE.is_dir():
    raise SystemExit('Locked x/text v0.42.0 source is missing; provide --source or populate GOMODCACHE')
OUTPUT = ROOT / 'validation/Configuration.Checks/LocaleChecks.Generated.cs'
PROVENANCE = ROOT / 'validation/Configuration.Checks/locale-test-provenance.json'
FILES = ['internal/language/parse_test.go', 'internal/tag/tag_test.go', 'language/language_test.go', 'language/parse_test.go', 'internal/language/compact/parse_test.go', 'internal/language/compact/language_test.go', 'internal/language/language_test.go', 'internal/language/lookup_test.go', 'language/examples_test.go']
texts = {name: (SOURCE / name).read_text() for name in FILES}
STR = r'"(?:\\.|[^"\\])*"'

def function(text, name):
    start = re.search(r'^func ' + re.escape(name) + r'\(', text, re.M)
    if start is None:
        raise ValueError(name)
    end = re.search(r'^func |^type ', text[start.end():], re.M)
    return text[start.start():start.end() + end.start()] if end else text[start.start():]

def cs(value):
    return json.dumps(value, ensure_ascii=True)

def quoted(value):
    return ast.literal_eval(value)

def strings(values):
    return '[' + ', '.join(cs(value) for value in values) + ']'

parse = texts[FILES[0]]
scan_block = parse[parse.index('var tests = []scanTest{'):parse.index('func TestScan')]
scan_rows = re.findall(r'\{(true|false),\s*(' + STR + r'),\s*\[\]string\{([^}]*)\}\}', scan_block)
if len(scan_rows) != 25:
    raise ValueError('Scanner rows changed')
block = function(parse, 'parseTests')
rows = []
for line in block.splitlines():
    if not re.match(r'^\s*\{in:', line):
        continue
    pairs = dict(re.findall(r'(\w+):\s*(' + STR + r'|true|false)', line))
    extension_list = re.search(r'extList:\s*\[\]string\{([^}]*)\}', line)
    ext = quoted(pairs.get('ext', '""'))
    if extension_list:
        ext = '-'.join(quoted(value) for value in re.findall(STR, extension_list[1]))
    row = [quoted(pairs.get(key, '""')) for key in ['in', 'lang', 'script', 'region', 'variants']]
    row += [ext] + [pairs.get(key, 'false') == 'true' for key in ['invalid', 'rewrite', 'changed']]
    rows.append(row)
if len(rows) != 100:
    raise ValueError('Parse rows changed: ' + str(len(rows)))
tag = texts[FILES[1]]
strdata = re.findall(STR, tag[tag.index('var strdata'):tag.index('var testCases')])
index_rows = re.findall(r'(' + STR + r'):\s*(-?\d+)', tag[tag.index('var testCases'):tag.index('func TestIndex')])
fix = re.findall(STR, function(tag, 'TestFixCase').split('for tc :=')[0])
if len(index_rows) != 12 or len(fix) != 21:
    raise ValueError('Tag rows changed')
public_strings = re.findall(STR, function(texts[FILES[2]], 'TestString').split('for i, s :=')[0])
if len(public_strings) != 1:
    raise ValueError('Public String rows changed')
output = '''// Copyright 2013, 2015 The Go Authors. All rights reserved.
// BSD license: licenses/XText-LICENSE.txt.
// Generated locked x/text v0.42.0 literal assertion tables; no checker-derived expectations.
namespace Configuration.Checks;
public static partial class LocaleChecks
{
    private static readonly ScanRow[] scanRows =
    [
'''
output += ''.join(f'        new({ok}, {cs(quoted(text))}, {strings([quoted(v) for v in re.findall(STR, toks)])}),\n' for ok, text, toks in scan_rows)
output += '    ];\n    private static readonly ParseRow[] parseRows =\n    [\n'
output += ''.join('        new(' + ', '.join([cs(x) for x in row[:6]] + [str(x).lower() for x in row[6:]]) + '),\n' for row in rows)
output += '    ];\n    private const string indexTable = ' + cs(''.join(map(quoted, strdata))) + ';\n'
output += '    private static readonly (string Key, int Expected)[] indexRows =\n    [\n'
output += ''.join(f'        ({cs(quoted(k))}, {v}),\n' for k, v in index_rows)
output += '    ];\n    private static readonly (string Form, string Input, string Expected)[] fixRows =\n    [\n'
output += ''.join('        (' + ', '.join(cs(quoted(x)) for x in fix[i:i+3]) + '),\n' for i in range(0, len(fix), 3))
output += '    ];\n    private static readonly string[] publicStringRows = ' + strings(list(map(quoted, public_strings))) + ';\n'
equality_counts = {}
for source, field in [('language/parse_test.go', 'publicEqualityInputs'), ('internal/language/compact/parse_test.go', 'compactEqualityInputs')]:
    inputs = re.findall(r'^\s*\{in:\s*(' + STR + ')', function(texts[source], 'parseTests'), re.M)
    if len(inputs) != 103:
        raise ValueError('Equality input row drift: ' + source)
    equality_counts[field] = len(inputs)
    output += '    private static readonly string[] ' + field + ' = ' + strings(list(map(quoted, inputs))) + ';\n'
grandfathered = re.findall(r'\{(' + STR + r'),\s*(' + STR + r')\}', function(texts['internal/language/lookup_test.go'], 'TestGrandfathered'))
make_rows = re.findall(r'\{(' + STR + r'),\s*(' + STR + r')\}', function(texts['internal/language/language_test.go'], 'TestMakeString'))
script_block = function(texts['internal/language/lookup_test.go'], 'TestGetScriptID')
script_table = re.search(r'tag.Index\((' + STR + r')\)', script_block)[1]
script_rows = re.findall(r'\{(' + STR + r'),\s*(\d+)\}', script_block)
if len(grandfathered) != 32 or len(make_rows) != 7 or len(script_rows) != 11:
    raise ValueError('Selected mixed locale domains changed')
for field, values in [('grandfatheredRows', grandfathered), ('makeStringRows', make_rows)]:
    output += '    private static readonly (string Input, string Expected)[] ' + field + ' =\n    [\n'
    output += ''.join('        (' + ', '.join(cs(quoted(x)) for x in row) + '),\n' for row in values)
    output += '    ];\n'
output += '    private const string scriptTestTable = ' + cs(quoted(script_table)) + ';\n'
output += '    private static readonly (string Input, int Expected)[] scriptIDRows =\n    [\n'
output += ''.join(f'        ({cs(quoted(key))}, {value}),\n' for key, value in script_rows)
output += '    ];\n'
lookup = texts['internal/language/lookup_test.go']
language_internal = texts['internal/language/language_test.go']
lang_rows = []
for line in function(lookup, 'TestLangID').splitlines():
    if not re.match(r'^\s*\{id:', line):
        continue
    fields = dict(re.findall(r'(id|bcp47|iso3|norm):\s*(' + STR + ')', line))
    error = 'syntax' if 'ErrSyntax' in line else 'value:xxx' if 'NewValueError' in line else ''
    lang_rows.append([quoted(fields.get(key, '""')) for key in ['id', 'bcp47', 'iso3', 'norm']] + [error])
region_rows = re.findall(r'\{(' + STR + r'),\s*(' + STR + r')\}', function(lookup, 'TestRegionID'))
m49_rows = re.findall(r'\{(-?\d+),\s*(' + STR + r')\}', function(lookup, 'TestRegionM49').split('for _, tt := range fromTests')[0])
deprecation_rows = re.findall(r'\{(' + STR + r'),\s*(' + STR + r')\}', function(lookup, 'TestRegionDeprecation'))
region_canonical_rows = re.findall(r'\{(' + STR + r'),\s*(' + STR + r')\}', function(language_internal, 'TestRegionCanonicalize'))
if [len(lang_rows), len(region_rows), len(m49_rows), len(deprecation_rows), len(region_canonical_rows)] != [27, 15, 25, 22, 6]:
    raise ValueError('Selected lookup domain drift')
output += '    private static readonly (string Input, string BCP47, string ISO3, string Normalized, string Error)[] langIDRows =\n    [\n'
output += ''.join('        (' + ', '.join(map(cs, row)) + '),\n' for row in lang_rows) + '    ];\n'
for field, values in [('regionIDRows', region_rows), ('regionDeprecationRows', deprecation_rows), ('regionCanonicalRows', region_canonical_rows)]:
    output += '    private static readonly (string Input, string Expected)[] ' + field + ' =\n    [\n'
    output += ''.join('        (' + ', '.join(cs(quoted(x)) for x in row) + '),\n' for row in values) + '    ];\n'
output += '    private static readonly (int Input, string Expected)[] m49Rows =\n    [\n'
# Go decimal literals with leading zeros retain the same values for 001 only.
output += ''.join(f'        ({int(number)}, {cs(quoted(expected))}),\n' for number, expected in m49_rows) + '    ];\n'
component_counts = {}
for name, field in [('TestParseBase', 'baseRows'), ('TestParseScript', 'scriptRows'), ('TestParseRegion', 'regionRows')]:
    values = re.findall(r'\{(' + STR + r'),\s*(' + STR + r'),\s*(true|false)\}', function(language_internal, name))
    component_counts[field] = len(values)
    output += '    private static readonly (string Input, string Expected, bool Ok)[] ' + field + ' =\n    [\n'
    output += ''.join(f'        ({cs(quoted(a))}, {cs(quoted(b))}, {ok}),\n' for a, b, ok in values) + '    ];\n'
if component_counts != {'baseRows': 9, 'scriptRows': 7, 'regionRows': 9}:
    raise ValueError('Component domain drift')
canonical_rows = re.findall(r'\{(' + STR + r'),\s*(' + STR + r'),\s*([A-Za-z |]+)\}', function(texts['language/language_test.go'], 'TestCanonicalize'))
selected_canonical = []
# Explicit compatible original domains: Default adds no other transformation
# for these actual Legacy/Deprecated inputs; original Raw rows stay Raw.
default_pairs = {('sh', 'Legacy'), ('sh-HR', 'Legacy'), ('sh-Cyrl-HR', 'Legacy'), ('tl', 'Legacy'), ('no', 'Legacy'), ('cmn', 'Legacy'), ('iw', 'DeprecatedBase'), ('iw', 'Deprecated | CLDR'), ('mo', 'Deprecated'), ('alb', 'Legacy'), ('dut', 'Legacy'), ('und-AN', 'Deprecated'), ('und-YD', 'DeprecatedRegion'), ('und-Qaai', 'DeprecatedScript')}
excluded_canonical = []
for a, b, flags in canonical_rows:
    flags = flags.strip()
    if flags == 'Raw' or (quoted(a), flags) in default_pairs:
        selected_canonical.append((quoted(a), quoted(b), flags == 'Raw', flags))
    else:
        excluded_canonical.append({'input': quoted(a), 'flags': flags, 'reason': 'Original non-Default transformation or intentionally suppressed alias differs; not projected through Default.'})
if len(selected_canonical) != 24:
    raise ValueError('Canonical selected domain drift: ' + str(len(selected_canonical)))
output += '    private static readonly (string Input, string Expected, bool Raw)[] canonicalRows =\n    [\n'
output += ''.join(f'        ({cs(a)}, {cs(b)}, {str(raw).lower()}),\n' for a, b, raw, _ in selected_canonical) + '    ];\n'
public_parse_rows = []
for line in function(texts['language/parse_test.go'], 'parseTests').splitlines():
    if not re.match(r'^\s*\{in:', line):
        continue
    pairs = dict(re.findall(r'(\w+):\s*(' + STR + r'|true|false)', line))
    ext_list = re.search(r'extList:\s*\[\]string\{([^}]*)\}', line)
    ext = '-'.join(quoted(v) for v in re.findall(STR, ext_list[1])) if ext_list else quoted(pairs.get('ext', '""'))
    public_parse_rows.append([quoted(pairs.get(key, '""')) for key in ['in', 'lang', 'script', 'region', 'variants']] + [ext] + [pairs.get(key, 'false') == 'true' for key in ['invalid', 'rewrite', 'changed']])
if len(public_parse_rows) != 103:
    raise ValueError('Public raw partChecks domain drift')
output += '    private static readonly ParseRow[] publicRawRows =\n    [\n'
output += ''.join('        new(' + ', '.join([cs(x) for x in row[:6]] + [str(x).lower() for x in row[6:]]) + '),\n' for row in public_parse_rows) + '    ];\n}\n'
names = ['TestScan', 'TestAcceptMinSize', 'TestParseExtensions', 'TestParseTag', 'TestParse', 'TestErrors', 'TestIndex', 'TestFixCase', 'TestString']
provenance = {
    'dependency': 'golang.org/x/text', 'version': 'v0.42.0',
    'sourceFiles': {name: hashlib.sha256((SOURCE / name).read_bytes()).hexdigest() for name in FILES},
    'functions': names,
    'rows': {'scan': len(scan_rows), 'acceptMinSizesPerScan': 8, 'parse': len(rows), 'index': len(index_rows), 'fixCase': len(fix)//3, 'errors': 8, 'publicString': len(public_strings)},
    'expectations': 'Locked original literals only. Same source skip/rewrite domains and every applicable original assertion retained.',
    'adaptations': ['Byte buffers use managed char arrays whose elements retain byte values.', 'Go value-error interface equality compares exception type and original subtag, while shared syntax sentinel identity is asserted.', 'language.Make input is observed through its selected equivalent Default Parse/String path; no unselected Make API is introduced.'],
    'adaptedMixedHelpers': {'grandfathered': len(grandfathered), 'makeString': len(make_rows), 'scriptID': len(script_rows), 'differences': 'Grandfathered raw Go Tag equality uses all retained fields. MakeString copies the original three setup fields directly and retains both output assertions; Go allocation limit excluded. GetScriptID calls its existing FindIndex helper with the original custom table; no whole unselected GetScriptID API claim.'},
    'selectedMixedAssertions': {'langID': len(lang_rows), 'regionID': len(region_rows), 'm49From': len(m49_rows), 'regionDeprecation': len(deprecation_rows), 'regionCanonical': len(region_canonical_rows), **component_counts, 'canonicalization': len(selected_canonical), 'canonicalizationExcludedRows': excluded_canonical, 'publicRawPartChecks': len(public_parse_rows), 'publicErrors': 8, 'errorExample': 7, 'exclusions': ['Language ISO3 output method.', 'Numeric region inverse M49 method.', 'Non-Default canonical flags and All/Supported/Compose idempotence.', 'Public language wrapper API construction; selected IDs/raw payloads assert original expected fields.']},
    'adaptedRoundtrips': {'internalLanguage': len(rows), **equality_counts, 'assertion': 'Same original input domains and expected roundtrip comparable-state equality.', 'differences': 'Managed raw Tag field values replace Go struct equality; compact equality uses the selected canonical Locale value representation and omits private numeric IDs; public equality uses Locale Parse/String and omits unselected Compose assertion.'},
    'execution': 'Original assertion bodies pending managed/native execution; authored72 differentials are separate evidence.'
}
provenance_text = json.dumps(provenance, indent=2) + '\n'
if options.check:
    if OUTPUT.read_text() != output or PROVENANCE.read_text() != provenance_text:
        raise SystemExit('Locale assertion table/provenance drift')
else:
    OUTPUT.write_text(output)
    PROVENANCE.write_text(provenance_text)
print(json.dumps(provenance['rows']))

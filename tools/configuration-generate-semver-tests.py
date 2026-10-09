"""Translate only the pinned literal semver test tables, preserving all rows.

The assertion bodies are explicit shared C# code. This generator does not infer
expected results from either implementation. --check detects any source/table drift.
"""
from pathlib import Path
import hashlib
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'upstream/TypeScript/tsc/internal/semver'
OUTPUT = ROOT / 'validation/Configuration.Checks/SemverChecks.Generated.cs'
PROVENANCE = ROOT / 'validation/Configuration.Checks/semver-test-provenance.json'
VERSION = (SOURCE / 'version_test.go').read_text()
RANGES = (SOURCE / 'version_range_test.go').read_text()
STR = r'"(?:\\.|[^"\\])*"'


def function(text, name):
    match = re.search(r'^func ' + re.escape(name) + r'\(', text, re.M)
    if not match:
        raise ValueError(name)
    end = re.search(r'^func |^type ', text[match.end():], re.M)
    return text[match.start():match.end() + end.start()] if end else text[match.start():]


def quoted(text):
    return json.loads(text)


def cs(text):
    return json.dumps(text, ensure_ascii=True)


def collection(values):
    return '[' + ', '.join(cs(v) for v in values) + ']'


def version(text):
    scalar = {name: int(value) for name, value in re.findall(r'(major|minor|patch):\s*(\d+)', text)}
    lists = {name: re.findall(STR, value) for name, value in re.findall(r'(prerelease|build):\s*\[\]string\{([^}]*)\}', text)}
    return 'new Version { ' + ', '.join([f'{k} = {scalar.get(k, 0)}' for k in ['major', 'minor', 'patch']] + [f'{k} = {collection([quoted(v) for v in lists[k]])}' for k in ['prerelease', 'build'] if k in lists]) + ' }'


def expression(text):
    # The only nonliteral source value is strings.Repeat("1", 320).
    tokens = re.findall(STR + r'|lotsaOnes', text)
    remaining = re.sub(STR + r'|lotsaOnes|\s|\+', '', text)
    if remaining:
        raise ValueError('Unknown original expression: ' + text)
    return ''.join('1' * 320 if t == 'lotsaOnes' else quoted(t) for t in tokens)


methods = []
counts = {}
names = re.findall(r'^func (Test\w+)\(', VERSION + RANGES, re.M)
if len(names) != 11 or len(set(names)) != 11:
    raise ValueError('Expected all eleven original functions')

rows = re.findall(r'\{(' + STR + r'), Version\{([^\n]+)\}\}', function(VERSION, 'TestTryParseSemver'))
if len(rows) != 4:
    raise ValueError('Version parse row count')
methods.append('''    private static void TestTryParseSemver()
    {
        (string Input, Version Expected)[] rows =
        [
''' + ''.join(f'            ({text}, {version(v)}),\n' for text, v in rows) + '''        ];
        foreach (var row in rows)
        {
            var (actual, error) = S.TryParseVersion(row.Input);
            Check.True(error is null, row.Input + " parse error");
            AssertVersion(actual, row.Expected, row.Input);
        }
    }
''')
counts['TestTryParseSemver'] = {'rows': len(rows), 'assertionsPerRow': 6}

rows = re.findall(r'\{Version\{([^\n]+)\}, (' + STR + r')\}', function(VERSION, 'TestVersionString'))
if len(rows) != 6:
    raise ValueError('Version string row count')
methods.append('''    private static void TestVersionString()
    {
        (Version Input, string Expected)[] rows =
        [
''' + ''.join(f'            ({version(v)}, {text}),\n' for v, text in rows) + '''        ];
        foreach (var row in rows) Check.Equal(row.Input.String(), row.Expected, row.Expected);
    }
''')
counts['TestVersionString'] = {'rows': len(rows), 'assertionsPerRow': 1}

rows = re.findall(r'\{(' + STR + r'), (' + STR + r'), (comparison\w+)\}', function(VERSION, 'TestVersionCompare'))
comparison = {'comparisonLessThan': '-1', 'comparisonEqualTo': '0', 'comparisonGreaterThan': '1'}
if len(rows) != 36:
    raise ValueError('Version comparison row count: ' + str(len(rows)))
methods.append('''    private static void TestVersionCompare()
    {
        (string Left, string Right, int Expected)[] rows =
        [
''' + ''.join(f'            ({a}, {b}, {comparison[c]}),\n' for a, b, c in rows) + '''        ];
        foreach (var row in rows)
        {
            var (left, leftError) = S.TryParseVersion(row.Left);
            var (right, rightError) = S.TryParseVersion(row.Right);
            Check.True(leftError is null, row.Left + " parse error");
            Check.True(rightError is null, row.Right + " parse error");
            Check.Equal(left.Compare(right), row.Expected, row.Left + " <=> " + row.Right);
        }
    }
''')
counts['TestVersionCompare'] = {'rows': len(rows), 'assertionsPerRow': 3}

wildcards = re.findall(r'(\w+) := \[\]string\{([^}]+)\}', function(RANGES, 'TestWildcardsHaveSameString'))
if len(wildcards) != 4:
    raise ValueError('Wildcard matrix count')
methods.append('''    private static void TestWildcardsHaveSameString()
    {
        string[][] matrices =
        [
''' + ''.join(f'            {collection([quoted(v) for v in re.findall(STR, strings)])},\n' for name, strings in wildcards) + '''        ];
        foreach (var matrix in matrices)
            foreach (string leftText in matrix)
                foreach (string rightText in matrix)
                {
                    var (left, leftOk) = S.TryParseVersionRange(leftText);
                    var (right, rightOk) = S.TryParseVersionRange(rightText);
                    Check.True(leftOk, leftText + " parse");
                    Check.True(rightOk, rightText + " parse");
                    Check.Equal(left.String(), right.String(), leftText + " == " + rightText);
                }
    }
''')
counts['TestWildcardsHaveSameString'] = {'matrices': 4, 'orderedPairs': sum(len(re.findall(STR, v)) ** 2 for _, v in wildcards), 'assertionsPerPair': 3}

blocks = re.findall(r'assertRangesGoodBad\(t, (.*?), testGoodBad\{\s*good:\s*\[\]string\{([^}]+)\},\s*bad:\s*\[\]string\{([^}]+)\},\s*\}\)', function(RANGES, 'TestVersionRanges'), re.S)
if len(blocks) != 11:
    raise ValueError('Good/bad ranges count: ' + str(len(blocks)))
methods.append('''    private static void TestVersionRanges()
    {
        (string Range, string[] Good, string[] Bad)[] rows =
        [
''' + ''.join(f'            ({cs(expression(name))}, {collection([expression(v) for v in good.split(", ")])}, {collection([expression(v) for v in bad.split(", ")])}),\n' for name, good, bad in blocks) + '''        ];
        foreach (var row in rows)
        {
            var (range, ok) = S.TryParseVersionRange(row.Range);
            Check.True(ok, row.Range + " parse");
            foreach (string good in row.Good)
            {
                var (version, parsed) = S.TryParseVersion(good);
                Check.True(parsed is null, good + " parse");
                Check.True(range.Test(version), good + " should match " + row.Range);
            }
            foreach (string bad in row.Bad)
            {
                var (version, parsed) = S.TryParseVersion(bad);
                Check.True(parsed is null, bad + " parse");
                Check.True(!range.Test(version), bad + " should not match " + row.Range);
            }
        }
    }
''')
# The original helper parses each good/bad input as Version and asserts no parse error.
counts['TestVersionRanges'] = {'ranges': len(blocks), 'values': sum(len(good.split(', ')) + len(bad.split(', ')) for _, good, bad in blocks)}

for name in names[5:]:
    source = function(RANGES, name)
    rows = re.findall(r'\{(' + STR + r'), (' + STR + r'), (true|false)\}', source)
    candidates = re.findall(r'^\s*\{[^\n]*\},\s*$', source, re.M)
    if not rows or len(rows) != len(candidates):
        raise ValueError(name + ' table row completeness')
    methods.append('    private static void ' + name + '''()
    {
        (string Range, string Version, bool Expected)[] rows =
        [
''' + ''.join(f'            ({a}, {b}, {c}),\n' for a, b, c in rows) + '''        ];
        foreach (var row in rows) AssertRangeTest(row.Range, row.Version, row.Expected);
    }
''')
    counts[name] = {'rows': len(rows), 'assertionsPerRow': 3}

text = '''// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified assertion-preserving translation of tsc/internal/semver/*_test.go.
// Generated by tools/configuration-generate-semver-tests.py from pinned literals.
using Version = TypeScript.Semver.Version;
using S = TypeScript.Semver.Semver;

namespace Configuration.Checks;

public static class SemverChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    [
''' + ''.join(f'        "{n}",\n' for n in names) + '''    ];

    public static void Run(string name)
    {
        switch (name)
        {
''' + ''.join(f'            case "{n}": {n}(); break;\n' for n in names) + '''            default: throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown original semver test.");
        }
    }

    private static void AssertVersion(Version actual, Version expected, string context)
    {
        Check.Equal(actual.major, expected.major, context + " major");
        Check.Equal(actual.minor, expected.minor, context + " minor");
        Check.Equal(actual.patch, expected.patch, context + " patch");
        Check.Sequence(actual.prerelease, expected.prerelease, context + " prerelease");
        Check.Sequence(actual.build, expected.build, context + " build");
    }

    private static void AssertRangeTest(string rangeText, string versionText, bool expected)
    {
        var (range, ok) = S.TryParseVersionRange(rangeText);
        Check.True(ok, rangeText + " parse");
        var (version, error) = S.TryParseVersion(versionText);
        Check.True(error is null, versionText + " parse error");
        Check.Equal(range.Test(version), expected, versionText + " in " + rangeText);
    }

''' + '\n'.join(methods) + '}\n'
provenance = {
    'revision': 'fed0bf24149fb1ed36039212648bafdafc1ea10e',
    'sourceFiles': {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in [SOURCE / 'version_test.go', SOURCE / 'version_range_test.go']},
    'functions': counts,
    'mechanicsAdaptation': 'Parallel subtest scheduling becomes sequential shared dispatch; all input literal rows, Cartesian pairs and original helper assertions retained. Runtime values are not used to generate expectations.',
    'managedExecution': 'pending-source-snapshot',
}
encoded = json.dumps(provenance, indent=2) + '\n'
if '--check' in sys.argv:
    if OUTPUT.read_text() != text or PROVENANCE.read_text() != encoded:
        raise SystemExit('Generated semver assertion assets are stale')
else:
    OUTPUT.write_text(text)
    PROVENANCE.write_text(encoded)
print(json.dumps(counts))

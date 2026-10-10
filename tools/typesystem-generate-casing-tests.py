#!/usr/bin/env python3
"""Preserve the complete pinned TestJSCasing table; never derives expected values."""
import hashlib
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = 'upstream/TypeScript/tsc/internal/stringutil/js_case_test.go'
PIN = 'fed0bf24149fb1ed36039212648bafdafc1ea10e'
source = (ROOT / SOURCE).read_text()
rows = re.findall(r'\{name: ("(?:[^"\\]|\\.)*"), got: (.*?), want: (.*?)\},', source)
assert len(rows) == 26 and source.count('{name:') == 26

def expression(value):
    # Every quoted source literal is a Go UTF8 byte string; encoded surrogate
    # calls already return the source WTF8 byte representation and stay raw.
    value = re.sub(r'"(?:[^"\\]|\\.)*"', lambda m: 'B(' + m[0] + ')', value)
    for name in ['ToUpperJS', 'ToLowerJS', 'EncodeJSStringRune']:
        value = value.replace(name + '(', 'StringUtil.' + name + '(')
    return value

generated = '''// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# original assertion port; upstream ''' + PIN + '''.
// Generated from the complete internal/stringutil/js_case_test.go table.
using TypeScript.Scanner;

namespace TypeSystem.Checks;

public static class JsCasingChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestJSCasing"];
    public const int OriginalTableAssertions = 26;
    private static string B(string value) => GoString.ToUtf8(value);

    public static void Run(string name)
    {
        if (name != "TestJSCasing") throw new ArgumentException("Unknown original casing test.");
        var rows = new (string Name, string Got, string Want)[]
        {
'''
for name, got, want in rows:
    generated += '            (' + name + ', ' + expression(got) + ', ' + expression(want) + '),\n'
generated += '''        };
        foreach (var row in rows)
            if (!StringComparer.Ordinal.Equals(row.Got, row.Want))
                throw new InvalidOperationException("TestJSCasing: " + row.Name);
    }
}
'''
target = ROOT / 'validation/TypeSystem.Checks/JsCasingChecks.Generated.cs'
provenance = dict(upstream=PIN, source=SOURCE, sourceSha256=hashlib.sha256((ROOT/SOURCE).read_bytes()).hexdigest(),
                  originalFunctions=['TestJSCasing'], originalTableAssertions=len(rows),
                  names=[json.loads(row[0]) for row in rows],
                  adaptations=['Source t.Parallel scheduling is sequential; all original operations and expected bytes retained.',
                               'Source string literals explicitly use GoString.ToUtf8; actual EncodeJSStringRune retains lone surrogate WTF8 bytes.'],
                  generator='tools/typesystem-generate-casing-tests.py',
                  generated=target.relative_to(ROOT).as_posix(),
                  generatedSha256=hashlib.sha256(generated.encode()).hexdigest())
assets={target:generated,ROOT/'validation/TypeSystem.Checks/js-casing-test-provenance.json':json.dumps(provenance,indent=2)+'\n'}
for path, data in assets.items():
    if '--check' in sys.argv:
        if not path.is_file() or path.read_text()!=data:raise SystemExit('Stale original casing assertions: '+str(path))
    else:
        path.parent.mkdir(parents=True,exist_ok=True);path.write_text(data)
print(json.dumps(dict(originalFunctions=1,tableAssertions=len(rows),check='--check' in sys.argv)))

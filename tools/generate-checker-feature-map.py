#!/usr/bin/env python3
"""Extract the complete pinned utilities.go FeatureMap literal; emits a class fragment.
Usage: python3 tools/generate-checker-feature-map.py OUTPUT
The algorithm owner inserts this fragment in Checker/Utilities.cs, its 1:1 counterpart.
"""
import pathlib, re, sys
source = pathlib.Path('upstream/TypeScript/tsc/internal/checker/utilities.go').read_text()
start = source.index('var getFeatureMap = sync.OnceValue(')
end = source.index('\n})', start) + 3
literal = source[start:end]
key_blocks = list(re.finditer(r'^\t\t"([^"]+)": \{\n(.*?)^\t\t\},', literal, re.M | re.S))
if not key_blocks or len(re.findall(r'^\t\t"[^"]+":', literal, re.M)) != len(key_blocks):
    raise SystemExit('Unrecognized feature-map key syntax')
lines = ['// Generated from checker/utilities.go:1331-1608 by tools/generate-checker-feature-map.py.',
         'private readonly record struct FeatureMapEntry(string lib, List<string> props);',
         'private static readonly Lazy<Dictionary<string,List<FeatureMapEntry>>> featureMap = new(() => new(StringComparer.Ordinal)', '{']
entries = 0
for block in key_blocks:
    key, body = block.groups()
    matches = list(re.finditer(r'\{lib: "([^"]+)", props: \[\]string\{([^}]*)\}\}', body, re.S))
    remainder = re.sub(r'\{lib: "([^"]+)", props: \[\]string\{([^}]*)\}\},?', '', body, flags=re.S).strip()
    if remainder or not matches:
        raise SystemExit('Unrecognized feature-map entry: ' + key)
    lines += [f'    ["{key}"] = [']
    for match in matches:
        lib, props = match.groups()
        names = re.findall(r'"([^"]*)"', props)
        if re.sub(r'"[^"]*"|[\s,]', '', props):
            raise SystemExit('Unrecognized feature property')
        lines += [f'        new("{lib}", [{", ".join(chr(34)+p+chr(34) for p in names)}]),']
        entries += 1
    lines += ['    ],']
lines += ['});', 'private static Dictionary<string,List<FeatureMapEntry>> getFeatureMap() => featureMap.Value;', '']
pathlib.Path(sys.argv[1]).write_text('\n'.join(lines))
print(f'{len(key_blocks)} feature keys / {entries} ordered entries -> {sys.argv[1]}')

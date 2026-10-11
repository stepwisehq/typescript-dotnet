#!/usr/bin/env python3
"""Reproduce/check pinned original bundled library resources; never change old checker assets."""
import argparse
import hashlib
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
PIN = 'fed0bf24149fb1ed36039212648bafdafc1ea10e'
SOURCE = ROOT / 'upstream/TypeScript/tsc/internal/bundled/libs_generated.go'
DEST = ROOT / 'src/TypeScript.Scanner/Bundled'

def sha(data):
    return hashlib.sha256(data).hexdigest()

def outputs():
    names = re.findall(r'"(lib[^"]*\.d\.ts)"', SOURCE.read_text())
    if names != sorted(set(names)) or len(names) != 115:
        raise SystemExit('unexpected pinned library registry')
    files = []
    result = {}
    for name in names:
        data = (SOURCE.parent / 'libs' / name).read_bytes()
        result[DEST / 'Libs' / name] = data
        files.append({'name': name, 'sizeBytes': len(data), 'sha256': sha(data)})
    manifest = {'upstreamRevision': PIN, 'generatedSource': str(SOURCE.relative_to(ROOT)), 'generatedSourceSha256': sha(SOURCE.read_bytes()), 'files': files}
    result[DEST / 'manifest.json'] = (json.dumps(manifest, indent=2) + '\n').encode()
    header = '// Copyright (c) Microsoft Corporation. All rights reserved.\n// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.\n// Generated from tsc/internal/bundled/libs_generated.go at ' + PIN + '.\n// Reproduce/check with tools/program-generate-bundled-libs.py.\nnamespace TypeScript.Bundled;\npublic static partial class Bundled\n{\n    public static readonly TypeScript.Scanner.Slice<string> LibNames = TypeScript.Scanner.Slice<string>.FromArray(\n    [\n'
    result[DEST / 'LibsGenerated.cs'] = (header + ''.join('        "' + name + '",\n' for name in names) + '    ]);\n}\n').encode()
    return result

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    for path, data in outputs().items():
        if args.check:
            if not path.exists() or path.read_bytes() != data:
                raise SystemExit('generated resource drift: ' + str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
    print('115 pinned bundled library resources match' if args.check else 'generated 115 pinned bundled library resources')

if __name__ == '__main__':
    main()

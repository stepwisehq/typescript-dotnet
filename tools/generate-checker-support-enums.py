#!/usr/bin/env python3
"""Pinned checker enum slices; retain handwritten model suffixes and fail unfamiliar input."""
import re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUTPUT_ROOT = Path(sys.argv[1]) if len(sys.argv) == 2 else ROOT
UPSTREAM = ROOT / 'upstream/TypeScript/tsc/internal'
PIN = 'fed0bf24149fb1ed36039212648bafdafc1ea10e'


def enum(source, name):
    text = (UPSTREAM / source).read_text()
    kind = re.search(r'type ' + re.escape(name) + r' (uint32|int32)', text)
    if kind is None:
        raise ValueError((source, name))
    after = text[kind.end():]
    base = 'uint' if kind[1] == 'uint32' else 'int'
    block = re.search(r'const \((.*?)\n\)', after, re.S)[1]
    rows = []
    for raw in block.splitlines():
        declaration, _, comment = raw.partition('//')
        declaration = declaration.strip()
        if not declaration:
            continue
        match = re.fullmatch(name + r'(\w+)(?:\s+' + name + r')?\s*=\s*(.+)', declaration)
        if match is None:
            raise ValueError((source, raw))
        key, value = match.groups()
        value = re.sub(name + r'(\w+)', r'\1', value).replace('^', '~')
        value = re.sub(r'\b1\s*<<\s*(\d+)', r'(1u << \1)' if base == 'uint' else r'(1 << \1)', value)
        rows.append('    ' + key + ' = ' + value + ',' + (' // ' + comment.strip() if comment else ''))
    return '[Flags]\npublic enum ' + name + ' : ' + base + '\n{\n' + '\n'.join(rows) + '\n}\n'


def write(source, destination, namespace, names):
    source_path = ROOT / destination
    path = OUTPUT_ROOT / destination
    path.parent.mkdir(parents=True, exist_ok=True)
    suffix = ''
    if source_path.exists() and '// BEGIN MANUAL ' in source_path.read_text():
        suffix = '\n// BEGIN MANUAL ' + source_path.read_text().split('// BEGIN MANUAL ', 1)[1]
    header = ('// Copyright (c) Microsoft Corporation. All rights reserved.\n'
              '// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.\n'
              '// Modified C# port, upstream ' + PIN + '.\n'
              '// Partial counterpart of tsc/internal/' + source + '; generated enum slice.\n'
              'using TypeScript.Scanner;\n' + ('using CacheHashKey = TypeScript.Checking.Checker.CacheHashKey;\n' if source == 'checker/types.go' else '') + 'namespace ' + namespace + ';\n')
    path.write_text(header + '\n'.join(enum(source, name) for name in names) + suffix)


write('checker/types.go', 'src/TypeScript.Scanner/Checker/Types.cs', 'TypeScript.Checking',
      ['TypeFlags', 'ObjectFlags', 'NodeCheckFlags'])
write('ast/checkflags.go', 'src/TypeScript.Scanner/Ast/CheckFlags.cs', 'TypeScript.Scanner', ['CheckFlags'])

write('nodebuilder/types.go', 'src/TypeScript.Scanner/NodeBuilder/Types.cs', 'TypeScript.NodeBuilding', ['Flags', 'InternalFlags'])

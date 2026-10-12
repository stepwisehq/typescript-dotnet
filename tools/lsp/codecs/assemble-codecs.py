#!/usr/bin/env python3
"""Assemble closed typed draft codecs with source JSON streaming error context.
No production writes: the final model/codecs materialization remains atomic.
"""
from pathlib import Path
import hashlib
import json
import re

HERE = Path(__file__).parent
ROOT = HERE.parents[2]
schema = json.loads((ROOT / 'tools/lsp/model-schema.json').read_text())['types']
inputs = [
    'plain-readers.cs', 'default-readers.cs', 'simple-union-readers.cs',
    'object-union-readers.cs', 'literal-readers.cs', 'boolean-object-union-readers.cs',
    'trial-union-readers.cs', 'kind-union-readers.cs', 'discriminator-readers.cs',
    'registration-codecs.cs', 'shape-readers.cs', 'struct-writers.cs',
    'union-writers.cs', 'literal-writers.cs', 'write-shapes.cs',
]
body = '\n\n'.join((HERE / name).read_text() for name in inputs)
read_names = re.findall(r'internal static void Read(\w+)\(ref ProtocolDecoder decoder, ref (\w+) value\)', body)
expected = {name for name, spec in schema.items() if spec['kind'] == 'struct'}
actual = {name for name, typ in read_names if name == typ}
if actual != expected or len(read_names) != len(expected):
    raise ValueError(f'closed struct-reader coverage mismatch: missing {expected-actual}, duplicates/count {len(read_names)}')
write_names = re.findall(r'internal static void Write(\w+)\(ProtocolEncoder encoder, (\w+) value\)', body)
if {name for name, typ in write_names if name == typ} != expected or len(write_names) != len(expected):
    raise ValueError('closed struct-writer coverage mismatch')
# Actual json/v2 calls custom streaming methods with previous depth/length,
# enriches their returned semantic error and leaves syntactic failures intact.
# Keep the ported body in a separate worker so its source branch shape remains
# directly readable, while its generated callsites pass through this exact join.
wrappers = []
for name, typ in read_names:
    old = f'internal static void Read{name}(ref ProtocolDecoder decoder, ref {typ} value)'
    new = f'private static void Read{name}Core(ref ProtocolDecoder decoder, ref {typ} value)'
    if body.count(old) != 1:
        raise ValueError('ambiguous reader declaration ' + name)
    body = body.replace(old, new)
    wrappers.append(f'''internal static void Read{name}(ref ProtocolDecoder decoder, ref {typ} value)
{{
    var previous = decoder.Capture();
    try
    {{
        Read{name}Core(ref decoder, ref value);
    }}
    catch (System.Text.Json.JsonException error)
    {{
        throw decoder.Enrich("lsproto.{name}", previous, error);
    }}
}}''')
header = '''// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Generated closed codecs from pinned lsp_generated.go; tools/lsp/generate-codecs.py.
#nullable enable
using TypeScript.Scanner;
using TypeScript.JsonRpc;
using TypeScript.SpanMap;
namespace TypeScript.Lsp.Protocol;
internal static partial class ProtocolCodec
{
'''
(HERE / 'ProtocolCodec.Generated.cs').write_text(header + body + '\n' + '\n\n'.join(wrappers) + '\n}\n')
(HERE / 'codec-coverage.json').write_text(json.dumps({
    'structReaders': len(read_names), 'structWriters': len(write_names),
    'inputs': {name: hashlib.sha256((HERE / name).read_bytes()).hexdigest() for name in inputs},
    'draftOnly': True,
}, indent=2) + '\n')
print(f'Assembled {len(read_names)} complete struct readers and {len(write_names)} complete struct writers.')

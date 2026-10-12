#!/usr/bin/env python3
"""Closed virtual output join for actual Go `any` protocol model consumers."""
from pathlib import Path
import hashlib
import json
HERE=Path(__file__).parent
ROOT=HERE.parents[2]
types=json.loads((ROOT/'tools/lsp/model-schema.json').read_text())['types']
values={'Position','Range','BooleanOrEmptyObject','BooleanOrClientSemanticTokensRequestFullDelta'}|{n for n in types if n.startswith('Resolved')}
header='''// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Generated closed model joins; tools/lsp/generate-codecs.py.
#nullable enable
using TypeScript.Scanner;
namespace TypeScript.Lsp.Protocol;
'''
lines=[]
for name,spec in types.items():
    if spec['kind']!='struct':continue
    declaration='partial struct' if name in values or not spec['fields'] else 'sealed partial class'
    lines += [f'internal {declaration} {name} : IProtocolWritable, IProtocolReadable, IProtocolTypeNamed', '{', f'string IProtocolTypeNamed.GoType => "lsproto.{name}";',
        f'void IProtocolWritable.WriteProtocolJSON(ProtocolEncoder encoder) => ProtocolCodec.Write{name}(encoder, this);',
        'void IProtocolReadable.ReadProtocolJSON(ref ProtocolDecoder decoder)', '{',
        'var value = this;',
        *([f'try {{ ProtocolCodec.Read{name}(ref decoder, ref value); }}', 'finally { this = value; }']
          if declaration == 'partial struct' else [f'ProtocolCodec.Read{name}(ref decoder, ref value);']), '}', '}']
(HERE/'ModelFacades.Generated.cs').write_text(header+'\n'.join(lines)+'\n')
lines=['internal static partial class ProtocolCodec','{',
       'internal static bool TryWriteIntrinsicProtocolValue(ProtocolEncoder encoder, object value)',
       '{','switch (value)','{']
for name,spec in types.items():
    if spec['kind']!='scalar':continue
    identity=hashlib.sha256(name.encode()).hexdigest()[:16]
    lines += [f'case {name} item: WriteShape_{identity}(encoder, item); return true;']
for name in ['DocumentUri','URI','Method','Null','NoParams','EmptyProtocolObject']:
    lines += [f'case {name} item: Write{name}(encoder, item); return true;']
lines+=['default: return false;','}','}','}']
(HERE/'IntrinsicValues.Generated.cs').write_text(header+'\n'.join(lines)+'\n')
# Actual fmt %T dispatch remains closed. Typed payload wrappers retain their root
# descriptors, while direct model objects use their generated Go value identity.
lines=['internal static partial class ProtocolDynamicTypes','{',
       'private static string IntrinsicName(object value) => value switch','{']
for name,spec in types.items():
    if spec['kind']=='scalar': lines.append(f'{name} => "lsproto.{name}",')
for name in ['DocumentUri','URI','Method','Null','NoParams']:
    lines.append(f'{name} => "lsproto.{name}",')
lines += ['EmptyProtocolObject => "struct {}",',
          '_ => throw new InvalidOperationException("unregistered dynamic protocol JSON type"),',
          '};','}']
(HERE/'DynamicTypes.Generated.cs').write_text(header+'\n'.join(lines)+'\n')
lines=['internal static partial class ProtocolCodec','{',
       'internal static bool TryReadIntrinsicProtocolValue(ref ProtocolDecoder decoder, ref object? value)',
       '{','switch (value)','{']
for name,spec in types.items():
    if spec['kind']!='scalar': continue
    identity=hashlib.sha256(name.encode()).hexdigest()[:16]
    lines += [f'case {name} item:', f'try {{ ReadShape_{identity}(ref decoder, ref item); }}',
              'finally { value = item; }', 'return true;']
for name in ['DocumentUri','URI','Method','Null','NoParams','EmptyProtocolObject']:
    lines += [f'case {name} item:', f'try {{ Read{name}(ref decoder, ref item); }}',
              'finally { value = item; }', 'return true;']
lines += ['case int item:', 'try { item = decoder.ReadInt32(); }', 'finally { value = item; }', 'return true;',
          'case uint item:', 'try { item = decoder.ReadUint32(); }', 'finally { value = item; }', 'return true;',
          'case ByteStringValue item:', 'try { item = decoder.ReadString(); }', 'finally { value = item; }', 'return true;',
          'default: return false;', '}', '}', '}']
(HERE/'IntrinsicReaders.Generated.cs').write_text(header+'\n'.join(lines)+'\n')

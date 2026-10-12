#!/usr/bin/env python3
"""Closed structural equality for original assert.DeepEqual protocol assertions.
This is test-only; comparing re-marshaled JSON would lose nil/header distinctions.
"""
from pathlib import Path
import runpy,hashlib,json
HERE=Path(__file__).parent
ns=runpy.run_path(str(HERE/'generate-codecs.py'))
runpy.run_path(str(HERE/'shape-readers.py'))['install'](ns)
types=ns['types'];underlying=ns['underlying'];mapped=ns['mapped']
roots=json.loads((HERE/'codec-root-shapes.json').read_text())
# Include every referenced field/element shape, not only public method roots.
# Structural comparisons retain nil/header/value distinctions throughout.
closed=set()
def collect(t):
 t=underlying(t)
 if t in closed:return
 closed.add(t)
 if t.startswith('*'):collect(t[1:])
 elif t.startswith('[]'):collect(t[2:])
 elif t.startswith('[2]'):collect(t[3:])
 elif t.startswith('map['):collect(t[t.index(']')+1:])
 elif t in types and types[t]['kind']=='struct':
  for field in types[t]['fields']:collect(field['type'])
for root in roots:collect(root)
roots=sorted(closed)
value_structs={'Position','Range','BooleanOrEmptyObject','BooleanOrClientSemanticTokensRequestFullDelta'}|{n for n in types if n.startswith('Resolved')}
def eq(t):return 'EqualShape_'+hashlib.sha256(underlying(t).encode()).hexdigest()[:16]
def ident(n):return '@'+n if n in {'Range','String','Integer'} else n
lines=['// Copyright (c) Microsoft Corporation. All rights reserved.',
'// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.',
'// Closed test-only structural equality preserving Go nil, pointer and slice distinctions.',
'#nullable enable','using TypeScript.Scanner;','using TypeScript.Lsp.Protocol;',
'using Range = TypeScript.Lsp.Protocol.Range;', 'using Diagnostic = TypeScript.Lsp.Protocol.Diagnostic;', 'using FileSystemWatcher = TypeScript.Lsp.Protocol.FileSystemWatcher;','namespace LanguageServices.Checks;',
'public static partial class ProtocolCodecChecks','{']
seen=set()
for original in roots:
 t=underlying(original)
 if t in seen:continue
 seen.add(t)
 lines += [f'private static bool {eq(t)}({mapped(t)} a, {mapped(t)} b)', '{']
 if t.startswith('*'):
  inner=underlying(t[1:]);reference=inner=='any' or (inner in types and types[inner]['kind']=='struct' and inner not in value_structs and bool(types[inner]['fields']))
  lines += ['if (a is null) return b is null;', 'if (b is null) return false;', f'return {eq(inner)}('+('a.Value, b.Value' if inner=='any' or not reference else 'a, b')+');']
 elif t.startswith('[]'):
  lines += ['if (a.IsNil != b.IsNil || a.Length != b.Length) return false;', 'for (int index=0;index<a.Length;index++)', f'if (!{eq(t[2:])}(a[index], b[index])) return false;', 'return true;']
 elif t.startswith('[2]'):
  lines += ['for (int index=0;index<2;index++)',f'if (!{eq(t[3:])}(a.IsNil ? default : a[index], b.IsNil ? default : b[index])) return false;', 'return true;']
 elif t.startswith('map['):
  end=t.index(']');inner=t[end+1:]
  lines += ['if (a.IsNil != b.IsNil || a.Count != b.Count) return false;', 'foreach(var entry in a)',f'if (!b.TryGetValue(entry.Key, out var value) || !{eq(inner)}(entry.Value, value)) return false;', 'return true;']
 elif t=='string':lines += ['return a.EqualsValue(b);']
 elif t=='any':lines += ['return EqualAny(a,b);']
 elif t in {'Null','NoParams','struct{}'}:lines += ['return true;']
 elif t in types and types[t]['kind']=='struct':
  spec=types[t]
  if t not in value_structs and spec['fields']:lines+=['if (a is null) return b is null;', 'if (b is null) return false;']
  conditions=[f'{eq(f["type"])}(a.{ident(f["csName"])}, b.{ident(f["csName"])})' for f in spec['fields']]
  lines+=['return '+(' &&\n'.join(conditions) if conditions else 'true')+';']
 else:lines += [f'return EqualityComparer<{mapped(t)}>.Default.Equals(a,b);'] if t!='float64' else ['return a == b;']
 lines += ['}']
lines += ['private static bool EqualAny(object? a, object? b)','{',
'if (a is null) return b is null;', 'if (b is null) return false;', 'switch (a)','{',
'case string text: return b is string otherText && text==otherText;',
'case bool value: return b is bool otherBoolean && value==otherBoolean;',
'case double value: return b is double otherNumber && value==otherNumber;',
'case Dictionary<string,object?> map:',
'if (b is not Dictionary<string,object?> otherMap || map.Count!=otherMap.Count) return false;',
'foreach(var entry in map) if (!otherMap.TryGetValue(entry.Key,out var value) || !EqualAny(entry.Value,value)) return false;',
'return true;', 'case List<object?> array:',
'if (b is not List<object?> otherArray || array.Count!=otherArray.Count) return false;',
'for(int index=0;index<array.Count;index++) if(!EqualAny(array[index],otherArray[index])) return false;',
'return true;']
for name,spec in types.items():
 if spec['kind']=='alias':continue
 lines += [f'case {name} value: return b is {name} other{name} && {eq(name)}(value,other{name});']
lines += ['default: throw new InvalidOperationException("unregistered original protocol equality owner");','}', '}', '}']
(HERE/'ProtocolCodecChecks.Equality.Generated.cs').write_text('\n'.join(lines)+'\n')
declared=set(__import__('re').findall(r'private static bool (EqualShape_\w+)\(', '\n'.join(lines)))
referenced=set(__import__('re').findall(r'(EqualShape_\w+)\(', '\n'.join(lines)))
if referenced-declared:raise ValueError('unclosed equality helpers: '+str(referenced-declared))
print('Generated closed structural equality for',len(seen),'source shapes')

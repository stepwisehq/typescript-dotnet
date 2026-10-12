#!/usr/bin/env python3
"""Actual Go method root-shapes, retained separately even when CLR T coincides."""
from pathlib import Path
import hashlib
import json
import re
import runpy
HERE=Path(__file__).parent
ROOT=HERE.parents[2]
namespace=runpy.run_path(str(HERE/'generate-codecs.py'))
types=namespace['types'];underlying=namespace['underlying']
source=(ROOT/'upstream/TypeScript/tsc/internal/lsp/lsproto/lsp_generated.go').read_text()
infos=[]
for match in re.finditer(r'^var (\w+) = (RequestInfo|NotificationInfo)\[(.+)\]\{Method: Method(\w+)\}$',source,re.M):
    name,kind,args,method=match.groups()
    args=args.split(', ')
    if len(args)!=(2 if kind=='RequestInfo' else 1):raise ValueError('method type shape '+name)
    infos.append(dict(name=name,kind=kind,args=args,method=method,line=source.count('\n',0,match.start())+1))
if len(infos)!=104:raise ValueError('method info inventory '+str(len(infos)))
roots=set(types)|{'DocumentUri','URI','Method','Null','NoParams','struct{}'}
for info in infos:roots.update(info['args'])
roots={underlying(t) for t in roots}
(HERE/'codec-root-shapes.json').write_text(json.dumps(sorted(roots),indent=2)+'\n')
runpy.run_path(str(HERE/'shape-readers.py'))['install'](namespace)
mapped=namespace['mapped']

def go_type(t):
    t=underlying(t)
    if t.startswith('*'):return '*'+go_type(t[1:])
    if t.startswith('[]'):return '[]'+go_type(t[2:])
    if t.startswith('[2]'):return '[2]'+go_type(t[3:])
    if t.startswith('map['):
        close=t.index(']');return 'map['+go_type(t[4:close])+']'+go_type(t[close+1:])
    if t=='any':return 'interface {}'
    return 'lsproto.'+t if t in types or t in {'DocumentUri','URI','Method','Null','NoParams'} else t

def shape_id(t):return hashlib.sha256(underlying(t).encode()).hexdigest()[:16]
lines=['// Closed lazy descriptors retain Go root shapes, not merely CLR generic T.']
for t in sorted(roots):
    identity=shape_id(t);cs=mapped(t);typename=go_type(t)
    lines += [f'private static class TypeFor_{identity}', '{', f'static TypeFor_{identity}() {{ }}',
        f'internal static readonly ProtocolType<{cs}> Value = new("{typename}",',
        f'    static (ref ProtocolDecoder decoder, ref {cs} value) => ReadShape_{identity}(ref decoder, ref value),',
        f'    static (ProtocolEncoder encoder, {cs} value) => WriteShape_{identity}(encoder, value), {str(t=="NoParams").lower()});','}']
(HERE/'root-descriptors.cs').write_text('\n'.join(lines)+'\n')
method_lines=['// All104 original method infos; lazy shape holders avoid eager CLI metadata.']
for info in infos:
    args=[underlying(t) for t in info['args']]
    cs_args=', '.join(mapped(t) for t in args)
    kind=info['kind'];name=info['name']
    descriptors=', '.join('ProtocolCodec.GetType_'+shape_id(t)+'()' for t in args)
    method_lines += [f'// lsp_generated.go:{info["line"]}',
        f'internal static {kind}<{cs_args}> {name} => InfoFor{name}.Value;',
        f'private static class InfoFor{name}', '{', f'static InfoFor{name}() {{ }}',
        f'internal static readonly {kind}<{cs_args}> Value = new(Method.{info["method"]}, {descriptors});', '}']
(HERE/'method-infos.cs').write_text('\n'.join(method_lines)+'\n')
getters=[]
for t in sorted(roots):
    identity=shape_id(t)
    getters.append(f'internal static ProtocolType<{mapped(t)}> GetType_{identity}() => TypeFor_{identity}.Value;')
with (HERE/'root-descriptors.cs').open('a') as output:output.write('\n'.join(getters)+'\n')
(HERE/'method-info-schema.json').write_text(json.dumps(infos,indent=2)+'\n')
print(f'Retained {len(roots)} distinct root-shapes for all{len(infos)} original method infos.')

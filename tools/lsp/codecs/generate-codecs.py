#!/usr/bin/env python3
"""Retained closed lsproto codec generator draft; no production outputs yet.
Source schema is extracted by tools/lsp/generate-protocol.py; exact custom method
bodies in codec-methods.json retain original source ownership/control-flow review.
"""
from pathlib import Path
import json,re
ROOT=Path(__file__).resolve().parents[3]
schema=json.loads((ROOT/'tools/lsp/model-schema.json').read_text())
types=schema['types']
methods=json.loads((Path(__file__).parent/'codec-methods.json').read_text())
method_by_type={item['type']:item for item in methods if item['method']=='UnmarshalJSONFrom'}

def underlying(t):
    while t in types and types[t]['kind']=='alias':t=types[t]['type']
    return t

def nilable(t):
    t=underlying(t)
    return t.startswith(('*','[]','map['))

def ident(n):return '@'+n if n in {'Range','String'} else n

def plain_reader(name,spec):
    method=method_by_type.get(name)
    strict=method is not None and method['kind']=='plain'
    if not strict:
        spec = dict(spec, fields=[dict(f, json=f['json'] or f['name']) for f in spec['fields']])
    required=[f for f in spec['fields'] if 'required' in f['lsp'].split(',')] if strict else []
    if len(required)>64:raise ValueError('required mask exceeds source uint64: '+name)
    required_ids={f['name']:i for i,f in enumerate(required)}
    # Cache one source struct spec per concrete type, retaining the Go lazy cache
    # lifetime. No reflection and no whole-registry eager specification allocation.
    out=[f'private static class SpecFor{name}', '{',f'internal static readonly ProtocolStructSpec Value = new({len(spec["fields"])},', '[']
    for index,f in enumerate(spec['fields']):
        if not f['json']:continue
        raw=f['json'].split(',')[0]
        if raw=='-':continue
        reject=strict and nilable(f['type']) and 'nullable' not in f['lsp'].split(',')
        out.append(f'new("{raw}", {index}, {required_ids.get(f["name"],-1)}, {str(reject).lower()}),')
    out+= [']);','}',f'internal static void Read{name}(ref ProtocolDecoder decoder, ref {name} value)', '{']
    # Actual type layout and empty/value/reference initialization are reconciled
    # with the main generator at final materialization, not guessed here.
    if name not in {'Position','Range','BooleanOrEmptyObject','BooleanOrClientSemanticTokensRequestFullDelta'} and not name.startswith('Resolved') and spec['fields']:
        out.append('value ??= new();')
    if not strict:
        out += ["if (decoder.PeekKind() == 'n')", '{', "decoder.ReadToken((byte)'n');"]
        for field in spec['fields']:
            target=underlying(field['type'])
            zero='new()' if target in types and types[target]['kind']=='struct' and target not in {'Position','Range','BooleanOrEmptyObject','BooleanOrClientSemanticTokensRequestFullDelta'} and not target.startswith('Resolved') and types[target]['fields'] else 'default'
            out += [f'value.{ident(field["csName"])} = {zero};']
        out += ['return;', '}']
    out+= ['ulong seen = 0;', ('if (decoder.PeekKind() != \'{\') throw ProtocolErrors.NotObject(decoder.PeekKind());' if strict else f'if (decoder.PeekKind() != \'{{\') throw decoder.InvalidValueFor("lsproto.{name}");'), 'decoder.ReadToken((byte)\'{\');', 'while (decoder.PeekKind() != \'}\')', '{', ('var name = decoder.ReadPropertyName();' if strict else 'var name = decoder.ReadDecodedPropertyName();'), f'ReadField{name}(ref decoder, ref value, name, ref seen, true);', '}', 'decoder.ReadToken((byte)\'}\');', f'SpecFor{name}.Value.Finish(seen);','}']
    out += [f'private static bool ReadField{name}(ref ProtocolDecoder decoder, ref {name} value, ByteStringValue name, ref ulong seen, bool skipUnknown)', '{', 'var spec = SpecFor'+name+'.Value;', 'if (!spec.ByName.TryGetValue(ByteStringKey.FromValue(name), out var field))', '{ if (skipUnknown) decoder.SkipValue(); return false; }', 'if (field.RequiredID >= 0) seen |= 1UL << field.RequiredID;', 'if (field.RejectNull && decoder.PeekKind() == \'n\') throw ProtocolErrors.Null(name);',]
    if spec['fields']:out += ['switch (field.Index)', '{']
    for index,f in enumerate(spec['fields']):
        if not f['json'] or f['json'].split(',')[0]=='-':continue
        out+= [f'case {index}: {shape(f["type"])}(ref decoder, ref value.{ident(f["csName"])}); break;']
    if spec['fields']:out.append('}')
    out+= ['return true;', '}']
    return '\n'.join(out)

def simple_union_reader(method):
    name=method['type'];body=method['body']
    if method['kind']!='custom' or name.startswith('StringLiteral') or 'json.Unmarshal(' in body or 'jsonObject' in body:return None
    prefix=re.fullmatch(r'\s*\*o = '+name+r'\{\}\s*switch dec.PeekKind\(\) \{(.*)\}\s*',body,re.S)
    if not prefix:return None
    arms=re.split(r'(?m)^\s*(case .*?:|default:)',prefix[1])
    if len(arms)<3:return None
    out=[f'internal static void Read{name}(ref ProtocolDecoder decoder, ref {name} value)', '{']
    if name not in {'BooleanOrEmptyObject','BooleanOrClientSemanticTokensRequestFullDelta'}:out.append('value ??= new();')
    for field in types[name]['fields']:out.append(f'value.{ident(field["csName"])} = default;')
    out+=['switch (decoder.PeekKind())','{']
    for index in range(1,len(arms),2):
        label,code=arms[index:index+2]
        if label=='default:':
            if not re.fullmatch(r'\s*return errInvalidKind\("'+name+r'", dec.PeekKind\(\)\)\s*',code):raise ValueError('Unexpected default: '+name)
            out+=[f'default: throw ProtocolErrors.InvalidKind("{name}", decoder.PeekKind());'];continue
        labels=re.findall(r"'(.)'",label)
        if not labels:raise ValueError('Unexpected labels: '+name+label)
        out += [f"case (byte)'{label}':" for label in labels]
        if re.fullmatch(r'\s*_, err := dec.ReadToken\(\)\s*return err\s*',code):
            out+=['decoder.ReadToken(decoder.PeekKind()); return;'];continue
        assignment=re.fullmatch(r'\s*o\.(\w+) = new\((.*?)\)\s*return json.UnmarshalDecode\(dec, o\.\1\)\s*',code,re.S)
        if not assignment:raise ValueError('Unexpected simple union body: '+name+code)
        field,go_type=assignment.groups()
        out += [f'// Go new({go_type}) creates the pointer before decoding.',f'{shape("*"+go_type)}(ref decoder, ref value.{ident(field)});','return;']
    out+=['}','}'];return '\n'.join(out)

def field_shape_id(go_type):
    # Collision-free generated helper spelling, not a runtime type lookup.
    import hashlib
    return hashlib.sha256(underlying(go_type).encode()).hexdigest()[:16]

def literal_reader(method):
    name=method['type'];body=method['body']
    if not name.startswith('StringLiteral'):return None
    literal=re.search(r'if string\(v\) != `([^`]+)`',body)
    if not literal:raise ValueError('Unknown literal reader: '+name)
    quoted=literal[1]
    return '\n'.join([f'internal static void Read{name}(ref ProtocolDecoder decoder, ref {name} value)', '{', 'var raw = decoder.ReadValue();', f'if (!raw.TextValue.EqualsBytes({json.dumps(quoted)}u8))', f'    throw ProtocolErrors.LiteralMismatch("{name}", {json.dumps(quoted)}, raw.TextValue);', '}'])

if __name__=='__main__':
    import runpy
    shape_source=runpy.run_path(str(Path(__file__).parent/'shape-readers.py'))['install'](globals())
    (Path(__file__).parent/'shape-readers.cs').write_text(shape_source)
    readers=[]
    for name,spec in types.items():
        if spec['kind']=='struct' and method_by_type.get(name,{}).get('kind')=='plain':readers.append(plain_reader(name,spec))
    path=Path(__file__).parent/'plain-readers.cs'
    path.write_text('\n\n'.join(readers)+'\n')
    print(f'Staged all {len(readers)} original plain struct reader bodies; not production code.')
    unions=[]
    for method in methods:
        if method['method']=='UnmarshalJSONFrom':
            generated=simple_union_reader(method)
            if generated:unions.append(generated)
    (Path(__file__).parent/'simple-union-readers.cs').write_text('\n\n'.join(unions)+'\n')
    print(f'Staged {len(unions)} exact simple-kind union bodies; remaining custom shapes retained explicitly.')

    object_generator=runpy.run_path(str(Path(__file__).parent/'object-union-readers.py'))['generate']
    object_readers=[]
    for method in methods:
        if method['method']=='UnmarshalJSONFrom' and method['kind']=='custom':
            generated=object_generator(method,types,shape)
            if generated:object_readers.append(generated)
    (Path(__file__).parent/'object-union-readers.cs').write_text('\n\n'.join(object_readers)+'\n')
    print(f'Staged {len(object_readers)} exact raw-key object union readers.')
    literals=[literal_reader(method) for method in methods if method['method']=='UnmarshalJSONFrom' and method['type'].startswith('StringLiteral')]
    (Path(__file__).parent/'literal-readers.cs').write_text('\n\n'.join(literals)+'\n')
    print(f'Staged all {len(literals)} exact literal readers.')

    writers_module=runpy.run_path(str(Path(__file__).parent/'union-writers.py'))
    union_writers=[]
    for method in methods:
        generated=writers_module['generate'](method,types,writers_module['write_shape'])
        if generated:union_writers.append(generated)
    (Path(__file__).parent/'union-writers.cs').write_text('\n\n'.join(union_writers)+'\n')
    print(f'Staged {len(union_writers)} exact marshalUnion writer bodies.')

    default_writers=runpy.run_path(str(Path(__file__).parent/'struct-writers.py'))['install'](types,methods,mapped,underlying)
    (Path(__file__).parent/'struct-writers.cs').write_text(default_writers)

    shape_writers=runpy.run_path(str(Path(__file__).parent/'write-shapes.py'))['install'](types,mapped,underlying)
    (Path(__file__).parent/'write-shapes.cs').write_text(shape_writers)
    literal_writers=[]
    source=(ROOT/'upstream/TypeScript/tsc/internal/lsp/lsproto/lsp_generated.go').read_text()
    for match in re.finditer(r'func \(o (StringLiteral\w+)\) MarshalJSONTo\(enc \*json.Encoder\) error \{\s*return enc.WriteValue\(json.Value\(`([^`]+)`\)\)\s*\}',source):
        name,raw=match.groups()
        literal_writers.append(f'internal static void Write{name}(ProtocolEncoder encoder, {name} value) => encoder.WriteRaw({json.dumps(raw)}u8);')
    if len(literal_writers)!=19:raise ValueError('Original literal writer inventory changed')
    (Path(__file__).parent/'literal-writers.cs').write_text('\n'.join(literal_writers)+'\n')

    bool_generator=runpy.run_path(str(Path(__file__).parent/'boolean-object-unions.py'))['generate']
    bool_readers=[]
    for method in methods:
        if method['method']=='UnmarshalJSONFrom' and method['kind']=='custom':
            generated=bool_generator(method,types,shape,object_generator)
            if generated:bool_readers.append(generated)
    (Path(__file__).parent/'boolean-object-union-readers.cs').write_text('\n\n'.join(bool_readers)+'\n')
    print(f'Staged {len(bool_readers)} bool/raw-object-key union reader bodies.')

    trials=runpy.run_path(str(Path(__file__).parent/'trial-unions.py'))
    trial_readers=[]
    for method in methods:
        if method['method']=='UnmarshalJSONFrom' and method['kind']=='custom':
            generated=trials['generate'](method,types,mapped,shape)
            if generated:trial_readers.append(generated)
    (Path(__file__).parent/'trial-union-readers.cs').write_text('\n\n'.join(trial_readers)+'\n')
    print(f'Staged {len(trial_readers)} exact sequential trial union reader bodies.')

    known=set(re.findall(r'internal static void Read(\w+)\(', '\n'.join(unions+object_readers+bool_readers+trial_readers+literals)))
    kind_generator=runpy.run_path(str(Path(__file__).parent/'kind-unions.py'))['generate']
    kind_readers=[]
    for method in methods:
        if method['method']=='UnmarshalJSONFrom' and method['kind']=='custom' and method['type'] not in known:
            generated=kind_generator(method,types,mapped,shape,object_generator,trials['trial_block'])
            if generated:kind_readers.append(generated)
    (Path(__file__).parent/'kind-union-readers.cs').write_text('\n\n'.join(kind_readers)+'\n')
    print(f'Staged {len(kind_readers)} additional exact top-level kind union reader bodies.')

    registration=runpy.run_path(str(Path(__file__).parent/'registration-codecs.py'))['generate'](methods,types,mapped,shape,writers_module['write_shape'])
    (Path(__file__).parent/'registration-codecs.cs').write_text(registration)

    discriminators=[]
    discriminator_generator=runpy.run_path(str(Path(__file__).parent/'discriminator-codecs.py'))['generate']
    for method in methods:
        if method['kind']=='discriminator':discriminators.append(discriminator_generator(method,types))
    (Path(__file__).parent/'discriminator-readers.cs').write_text('\n\n'.join(discriminators)+'\n')
    print(f'Staged all {len(discriminators)} streaming discriminator union reader bodies.')

    default_readers=[]
    for name,spec in types.items():
        if spec['kind']=='struct' and name not in method_by_type:
            default_readers.append(plain_reader(name,spec))
    (Path(__file__).parent/'default-readers.cs').write_text('\n\n'.join(default_readers)+'\n')
    print(f'Staged {len(default_readers)} default Go struct reader counterparts.')

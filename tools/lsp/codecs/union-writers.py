"""Closed source marshalUnion writers; exactly one/at-most-one panic preserved."""
import re,hashlib

def generate(method, types, write_shape):
    if method['method']!='MarshalJSONTo' or method['kind']!='marshal-union':return None
    name=method['type'];match=re.fullmatch(r'\s*return marshalUnion\(o, enc, "'+name+r'", (true|false)\)\s*',method['body'])
    if not match:raise ValueError('Unexpected marshalUnion owner: '+name)
    nullable=match[1]=='true'
    fields=types[name]['fields']
    if any(not f['type'].startswith('*') for f in fields):raise ValueError('Nonpointer union member: '+name)
    out=[f'// lsp_generated.go:{method["line"]}; structcodec.go marshalUnion.', f'internal static void Write{name}(ProtocolEncoder encoder, {name} value)','{']
    if name not in {'BooleanOrEmptyObject','BooleanOrClientSemanticTokensRequestFullDelta'}:out.append('value ??= new();')
    out.append('int count = 0;')
    out += [f'if (value.{f["csName"]} is not null) count++;' for f in fields]
    if nullable:out += [f'if (count > 1) throw new InvalidOperationException("more than one element of {name} is set");','if (count == 0) { encoder.WriteNull(); return; }']
    else:out += [f'if (count != 1) throw new InvalidOperationException("exactly one element of {name} should be set");']
    for f in fields:out += [f'if (value.{f["csName"]} is not null)', '{',f'{write_shape(f["type"])}(encoder, value.{f["csName"]});','return;','}']
    out += ['}'];return '\n'.join(out)

def write_shape(t):return 'WriteShape_'+hashlib.sha256(t.encode()).hexdigest()[:16]

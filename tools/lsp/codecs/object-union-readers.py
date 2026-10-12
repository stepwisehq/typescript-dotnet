"""Generate exact raw-object-key and trial-decoding union patterns, fail closed."""
import re,json

def generate(method, types, shape):
    name=method['type']; body=method['body']
    prefix=r'\s*\*o = '+name+r'\{\}\s*data, err := dec.ReadValue\(\)\s*if err != nil \{\s*return err\s*\}\s*'
    match=re.match(prefix,body)
    if not match:return None
    tail=body[match.end():]
    keyed=re.fullmatch(r'switch jsonObjectHasKey\(data, (.*?)\) \{(.*?)\}\s*(?:return errInvalidValue\("'+name+r'", data\)\s*)?',tail,re.S)
    if not keyed:return None
    keys=re.findall(r'"([^"]+)"',keyed[1]); arms=re.split(r'(?m)^\s*(case \d+:|default:)(?:[^\n]*)\n',keyed[2])
    if len(arms)<3:return None
    out=[f'private static class KeysFor{name}', '{', 'internal static readonly ByteStringValue[] Value = ['+', '.join('ByteStringValue.FromString('+json.dumps(k)+')' for k in keys)+'];', '}', f'internal static void Read{name}(ref ProtocolDecoder decoder, ref {name} value)','{','value ??= new();']
    out += [f'value.{f["csName"]} = default;' for f in types[name]['fields']]
    out+=['var raw = decoder.ReadValue();', f'switch (ProtocolObjectHasKey(raw, KeysFor{name}.Value))','{']
    has_default=False
    for i in range(1,len(arms),2):
        label,code=arms[i:i+2]
        arm=re.fullmatch(r'\s*o\.(\w+) = new\((.*?)\)\s*return json.Unmarshal\(data, o\.\1\)\s*',code,re.S)
        if not arm:raise ValueError('Unexpected keyed arm: '+name+code)
        field,t=arm.groups(); field=next(f['csName'] for f in types[name]['fields'] if f['name']==field)
        if label=='default:':has_default=True
        out += [label, '{', f'value.{field} = new();', 'var nested = new ProtocolDecoder(raw.TextValue);',f'{shape("*"+t)}(ref nested, ref value.{field});','nested.Finish();','return;','}']
    if not has_default:out+=[f'default: throw ProtocolErrors.InvalidValue("{name}", raw.TextValue);']
    out+=['}','}'];return '\n'.join(out)

"""Exact sequential raw-value trial arms; succeeds/assigns only after full decode."""
import re

def trial_block(body,name,types,mapped,shape):
    body=body.strip();out=[]
    if body.startswith('if string(data) == "null"'):
        match=re.match(r'if string\(data\) == "null" \{\s*return nil\s*\}\s*',body)
        if not match:return None
        out += ['if (raw.TextValue.EqualsBytes("null"u8)) return;'];body=body[match.end():]
    arm=re.compile(r'var (\w+) (.*?)\n\s*if err := json.Unmarshal\(data, &\1\); err == nil \{\s*o\.(\w+) = &\1\s*return nil\s*\}\s*',re.S)
    while (match:=arm.match(body)):
        local,t,field=match.groups();field=next(f['csName'] for f in types[name]['fields'] if f['name']==field)
        value=f'new ProtocolPointer<object?>({local})' if t=='any' else local
        out += [f'{mapped(t)} {local} = default!;','try','{','var nested = new ProtocolDecoder(raw.TextValue);',f'{shape(t)}(ref nested, ref {local});','nested.Finish();',f'value.{field} = {value};','return;','}','catch (Exception error) when (error is System.Text.Json.JsonException or FormatException) { }']
        body=body[match.end():]
    if not re.fullmatch(r'return errInvalidValue\("'+name+r'", data\)',body.strip()):return None
    out += [f'throw ProtocolErrors.InvalidValue("{name}", raw.TextValue);'];return '\n'.join(out)

def generate(method,types,mapped,shape):
    name=method['type'];body=method['body']
    prefix=re.match(r'\s*\*o = '+name+r'\{\}\s*data, err := dec.ReadValue\(\)\s*if err != nil \{\s*return err\s*\}\s*',body)
    if not prefix:return None
    block=trial_block(body[prefix.end():],name,types,mapped,shape)
    if block is None:return None
    out=[f'internal static void Read{name}(ref ProtocolDecoder decoder, ref {name} value)','{','value ??= new();']
    out += [f'value.{f["csName"]} = default;' for f in types[name]['fields']]
    out += ['var raw = decoder.ReadValue();',block,'}'];return '\n'.join(out)

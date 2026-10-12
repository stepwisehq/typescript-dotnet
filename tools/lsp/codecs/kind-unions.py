"""Source top-level kind switches with exact raw-key or trial nested branches."""
import re

def generate(method,types,mapped,shape,object_generate,trial_block):
    name=method['type'];body=method['body']
    match=re.fullmatch(r'\s*\*o = '+name+r'\{\}\s*switch (?:kind := dec.PeekKind\(\); kind|dec.PeekKind\(\)) \{(.*)\n\t\}\s*',body,re.S)
    if not match:return None
    arms=re.split(r'(?m)^\t(case .*?:|default:)\s*\n',match[1]);prefixes=[];blocks=[]
    for i in range(1,len(arms),2):
        label,code=arms[i:i+2];labels=re.findall(r"'(.)'",label)
        block=[]
        if label=='default:':
            if not re.fullmatch(r'\s*return errInvalidKind\("'+name+r'", dec.PeekKind\(\)\)\s*',code):return None
            blocks += [f'default: throw ProtocolErrors.InvalidKind("{name}", decoder.PeekKind());'];continue
        if not labels:return None
        if re.fullmatch(r'\s*_, err := dec.ReadToken\(\)\s*return err\s*',code):block=['decoder.ReadToken(kind); return;']
        elif re.fullmatch(r'\s*o.Boolean = new\(kind == \'t\'\)\s*_, err := dec.ReadToken\(\)\s*return err\s*',code):block=["value.Boolean = kind == 't';",'decoder.ReadToken(kind); return;']
        elif (pointer:=re.fullmatch(r'\s*o\.(\w+) = new\((.*?)\)\s*return json.UnmarshalDecode\(dec, o\.\1\)\s*',code,re.S)):
            field,t=pointer.groups();field=next(f['csName'] for f in types[name]['fields'] if f['name']==field)
            block=[f'{shape("*"+t)}(ref decoder, ref value.{field});','return;']
        else:
            raw_prefix=re.match(r'\s*data, err := dec.ReadValue\(\)\s*if err != nil \{\s*return err\s*\}\s*',code)
            if not raw_prefix:return None
            tail=code[raw_prefix.end():]
            nested=object_generate({'type':name,'body':'\n*o = '+name+'{}\n'+code},types,shape)
            if nested:
                prefixes.append(nested[:nested.index('internal static void')])
                block=[nested[nested.index('var raw = decoder.ReadValue();'):].rsplit('\n}',1)[0]]
            else:
                trial=trial_block(tail,name,types,mapped,shape)
                if trial is None:return None
                block=['var raw = decoder.ReadValue();',trial]
        blocks += [f"case (byte)'{c}':" for c in labels]+['{']+block+['}']
    if not blocks:return None
    out=prefixes+[f'internal static void Read{name}(ref ProtocolDecoder decoder, ref {name} value)','{']
    if name not in {'BooleanOrEmptyObject','BooleanOrClientSemanticTokensRequestFullDelta'}:out.append('value ??= new();')
    out += [f'value.{f["csName"]} = default;' for f in types[name]['fields']]
    out += ['byte kind = decoder.PeekKind();','switch (kind)','{']+blocks+['}','}']
    return '\n'.join(out)

"""Exact bool-or-object raw-key union readers, preserving all source branches."""
import re

def generate(method,types,shape,object_generate):
    name=method['type'];body=method['body']
    match=re.fullmatch(r'\s*\*o = '+name+r'\{\}\s*switch kind := dec.PeekKind\(\); kind \{\s*case \'t\', \'f\':\s*o.Boolean = new\(kind == \'t\'\)\s*_, err := dec.ReadToken\(\)\s*return err\s*case \'\{\':(.*)\s*default:\s*return errInvalidKind\("'+name+r'", dec.PeekKind\(\)\)\s*\}\s*',body,re.S)
    if not match:return None
    inner={'type':name,'body':'\n*o = '+name+'{}\n'+match[1]}
    generated=object_generate(inner,types,shape)
    if not generated:return None
    prefix=generated[:generated.index('internal static void')]
    block=generated[generated.index('var raw = decoder.ReadValue();'):].rsplit('\n}',1)[0]
    out=[prefix,f'internal static void Read{name}(ref ProtocolDecoder decoder, ref {name} value)','{']
    if name not in {'BooleanOrEmptyObject','BooleanOrClientSemanticTokensRequestFullDelta'}:out.append('value ??= new();')
    out += [f'value.{f["csName"]} = default;' for f in types[name]['fields']]
    out+=['byte kind = decoder.PeekKind();','switch (kind)','{',"case (byte)'t': case (byte)'f':","value.Boolean = kind == 't';","decoder.ReadToken(kind); return;","case (byte)'{':","{",block,'}',f'default: throw ProtocolErrors.InvalidKind("{name}", decoder.PeekKind());','}','}']
    return '\n'.join(out)

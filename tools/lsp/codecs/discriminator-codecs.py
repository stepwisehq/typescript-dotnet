"""Streaming discriminator source pattern, including deferred-before-kind copies."""
import re,json

def generate(method,types):
    name=method['type'];body=method['body']
    scan=re.search(r'scanDiscriminatedStruct\(dec, "'+name+r'", "([^"]+)"\)',body)
    if not scan:raise ValueError('Missing original discriminator owner: '+name)
    discriminator=scan[1]
    switch=re.search(r'switch string\(state.discriminatorValue\) \{(.*?)\n\s*\}',body,re.S)
    if not switch:raise ValueError('Unexpected discriminator switch: '+name)
    arms=re.findall(r'(case `([^`]+)`|default):\s*return unmarshalDiscriminatedArm\(state, &o\.(\w+)\)',switch[1])
    if len(arms)!=switch[1].count('unmarshalDiscriminatedArm'):raise ValueError('Unexpected discriminator arm: '+name)
    fields={f['name']:f for f in types[name]['fields']};hasdefault=any(a[0]=='default' for a in arms)
    out=[f'internal static void Read{name}(ref ProtocolDecoder decoder, ref {name} value)','{','value ??= new();']
    out += [f'value.{f["csName"]} = default;' for f in fields.values()]
    if re.search(r"case 'n':",body):out += ["if (decoder.PeekKind() == 'n') { decoder.ReadToken((byte)'n'); return; }"]
    out += [f'var state = ScanDiscriminatedStruct(ref decoder, "{name}", ByteStringValue.FromString({json.dumps(discriminator)}));']
    for arm_index,(label,raw,field) in enumerate(arms):
        f=fields[field];t=f['type'][1:]
        if label!='default':out += [f'if (state.DiscriminatorValue is {{ }} discriminatorValue{arm_index} && discriminatorValue{arm_index}.TextValue.EqualsBytes({json.dumps(raw)}u8))','{']
        out += [f'UnmarshalDiscriminatedArm(ref decoder, state, ref value.{f["csName"]}, SpecFor{t}.Value, ReadField{t}, static () => new {t}());', 'return;']
        if label!='default':out += ['}']
    if not hasdefault:out += ['throw state.InvalidDiscriminator();']
    out += ['}'];return '\n'.join(out)

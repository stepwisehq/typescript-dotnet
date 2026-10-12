"""Registration externally tagged read/write source owners; exact arm ordering."""
import re,json

def generate(methods,types,mapped,shape,write_shape):
    read=next(m for m in methods if m['type']=='Registration' and m['method']=='UnmarshalJSONFrom')
    write=next(m for m in methods if m['type']=='Registration' and m['method']=='MarshalJSONTo')
    arms=re.findall(r'case Method(\w+):\s*var v (\w+)\s*if err := json.Unmarshal\(rawRegisterOptions, &v\); err != nil \{\s*return err\s*\}\s*s.RegisterOptions\.(\w+) = &v',read['body'])
    if len(arms)!=read['body'].count('case Method'):raise ValueError('Unrecognized Registration read arm')
    out=['internal static void ReadRegistration(ref ProtocolDecoder decoder, ref Registration value)','{','value ??= new();','value.Id = default; value.RegisterOptions = null;','uint missing = 3;','if (decoder.PeekKind() != \'{\') throw ProtocolErrors.NotObject(decoder.PeekKind());','decoder.ReadToken((byte)\'{\');','ByteStringValue method = default;','ProtocolRawValue? rawOptions = null;','while (decoder.PeekKind() != \'}\')','{','var name = decoder.ReadPropertyName();','if (name.EqualsBytes("id"u8)) { missing &= ~1U; value.Id = decoder.ReadString(); }','else if (name.EqualsBytes("method"u8)) { missing &= ~2U; method = decoder.ReadString(); }','else if (name.EqualsBytes("registerOptions"u8)) rawOptions = decoder.ReadValue();','else decoder.SkipValue();','}','decoder.ReadToken((byte)\'}\');','if (missing != 0)','{','var names = Slice<ByteStringValue>.Make(0);','if ((missing & 1) != 0) names = names.Append(ByteStringValue.FromString("id"));','if ((missing & 2) != 0) names = names.Append(ByteStringValue.FromString("method"));','throw ProtocolErrors.Missing(names);','}','if (rawOptions is null) throw ProtocolErrors.Text(ByteStringValue.Concat(ByteStringValue.FromString("missing registerOptions for method: "), method));','value.RegisterOptions = new();']
    for method,t,field in arms:
        out += [f'if (method.EqualsValue(Method.{method}.Value))','{',f'{mapped(t)} target = default!;','var nested = new ProtocolDecoder(rawOptions.Value.TextValue);',f'{shape(t)}(ref nested, ref target);','nested.Finish();',f'value.RegisterOptions.{field} = target;','return;','}']
    out += ['throw ProtocolErrors.Text(ByteStringValue.Concat(ByteStringValue.FromString("unknown registration method: "), method));','}']
    # Writer preserves source property order and fails before output when invalid.
    writer_arms=re.findall(r'case s.RegisterOptions\.(\w+) != nil:\s*method = json.Value\(`([^`]+)`\)\s*opts = s.RegisterOptions\.\1',write['body'])
    if len(writer_arms)!=write['body'].count('case s.RegisterOptions.'):raise ValueError('Unrecognized Registration writer arm')
    fields={f['name']:f for f in types['RegisterOptions']['fields']}
    out += ['internal static void WriteRegistration(ProtocolEncoder encoder, Registration value)','{','value ??= new();','if (value.RegisterOptions is null) throw new InvalidOperationException("RegisterOptions must be set");','int count = 0;']
    out += [f'if (value.RegisterOptions.{f["csName"]} is not null) count++;' for f in fields.values()]
    out += ['if (count != 1) throw new InvalidOperationException("exactly one element of RegisterOptions should be set");','encoder.BeginObject();','encoder.WriteProperty("id"u8); encoder.WriteString(value.Id);','encoder.WriteProperty("method"u8);']
    for field,raw in writer_arms:
        f=fields[field]
        out += [f'if (value.RegisterOptions.{f["csName"]} is not null)','{',f'encoder.WriteRaw({json.dumps(raw)}u8);','encoder.WriteProperty("registerOptions"u8);',f'{write_shape(f["type"])}(encoder, value.RegisterOptions.{f["csName"]});','encoder.EndObject();','return;','}']
    out += ['}'];return '\n'.join(out)+'\n'

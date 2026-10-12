"""Closed field-shape helpers for the retained codec generator draft.
No runtime reflection/type registry; each concrete call names its source shape.
"""
import re,hashlib,json
from pathlib import Path

def install(namespace):
    types=namespace['types'];underlying=namespace['underlying']
    value_structs={'Position','Range','BooleanOrEmptyObject','BooleanOrClientSemanticTokensRequestFullDelta'}|{n for n in types if n.startswith('Resolved')}
    def mapped(t):
        t=underlying(t)
        if t.startswith('*'):
            if underlying(t[1:])=='any':return 'ProtocolPointer<object?>?'
            v=mapped(t[1:]);return v if v.endswith('?') else v+'?'
        if t.startswith('[]'):return 'Slice<'+mapped(t[2:])+'>'
        if t.startswith('[2]'):return 'Slice<'+mapped(t[3:])+'>'
        if t.startswith('map['):
            end=t.index(']');return 'ProtocolMap<'+('ByteStringKey' if t[4:end]=='string' else mapped(t[4:end]))+', '+mapped(t[end+1:])+'>'
        return {'string':'ByteStringValue','bool':'bool','uint32':'uint','int32':'int','float64':'double','any':'object?','uint':'uint','int':'int','struct{}':'EmptyProtocolObject'}.get(t,t)
    def go_type(t):
        t=underlying(t)
        if t.startswith('*'):return '*'+go_type(t[1:])
        if t.startswith('[]'):return '[]'+go_type(t[2:])
        if t.startswith('[2]'):return '[2]'+go_type(t[3:])
        if t.startswith('map['):
            close=t.index(']');return 'map['+go_type(t[4:close])+']'+go_type(t[close+1:])
        if t=='any':return 'interface {}'
        return 'lsproto.'+t if t in types or t in {'DocumentUri','Method','URI','Null','NoParams'} else t
    shapes={}
    def shape(t):
        t=underlying(t)
        if t in shapes:return shapes[t]
        name='ReadShape_'+hashlib.sha256(t.encode()).hexdigest()[:16];shapes[t]=name
        if t.startswith('*'):shape(t[1:])
        elif t.startswith('[]'):shape(t[2:])
        elif t.startswith('[2]'):shape(t[3:])
        elif t.startswith('map['):shape(t[t.index(']')+1:])
        return name
    for root in json.loads((Path(__file__).parent/'codec-root-shapes.json').read_text()):shape(root)
    for spec in types.values():
        for f in spec.get('fields',[]):shape(f['type'])
    def is_reference(t):
        t=underlying(t)
        return t in types and types[t]['kind']=='struct' and t not in value_structs and bool(types[t]['fields'])
    for spec in types.values():
        if spec['kind']=='scalar':shape(spec['type'])
    outputs=[]
    for t,name in list(shapes.items()):
        lines=[f'// Closed source field shape: {t}',f'private static void {name}(ref ProtocolDecoder decoder, ref {mapped(t)} value)','{']
        if t.startswith('*'):
            inner=underlying(t[1:]);call=shape(inner)
            lines+=['if (decoder.PeekKind() == \'n\') { decoder.ReadToken((byte)\'n\'); value = null; return; }']
            if inner=='any':lines+=['value ??= new(null);',f'{call}(ref decoder, ref value.Value);']
            elif is_reference(inner):lines+=['value ??= new();',f'{call}(ref decoder, ref value);']
            else:lines += [f'var pointee = value.GetValueOrDefault();','value = pointee;',f'try {{ {call}(ref decoder, ref pointee); }}','finally { value = pointee; }']
        elif t.startswith('[]'):
            inner=t[2:];call=shape(inner)
            lines+=['if (decoder.PeekKind() == \'n\') { decoder.ReadToken((byte)\'n\'); value = default; return; }',f'decoder.ReadArrayStart("{go_type(t)}");','int capacity = value.Capacity;','if (capacity > 0) value = value.Slice2(0, capacity);','int count = 0;','while (decoder.PeekKind() != \']\')','{','if (count == capacity)','{','value = value.Grow(1); capacity = value.Capacity; value = value.Slice2(0, capacity);','}',f'{mapped(inner)} element = default!;','int index = count++;','value[index] = element;',f'try {{ {call}(ref decoder, ref element); }}','catch { value[index] = element; value = value.Slice2(0, count); throw; }','value[index] = element;','}','value = count == 0 ? '+mapped(t)+'.Make(0) : value.Slice2(0, count);','decoder.ReadToken((byte)\']\');']
        elif t.startswith('map['):
            end=t.index(']');key=t[4:end];inner=t[end+1:];call=shape(inner)
            if key not in {'string','DocumentUri'}:raise ValueError('Unsupported actual map key: '+key)
            key_read='ByteStringKey.FromValue(decoder.ReadDecodedPropertyName())' if key=='string' else 'new DocumentUri(decoder.ReadDecodedPropertyName())'
            lines+=['if (decoder.PeekKind() == \'n\') { decoder.ReadToken((byte)\'n\'); value = default; return; }',f'decoder.ReadObjectStart("{go_type(t)}");',f'if (value.IsNil) value = {mapped(t)}.Make();','while (decoder.PeekKind() != \'}\')','{',f'var key = {key_read};',f'{mapped(inner)} element = default!;',f'try {{ {call}(ref decoder, ref element); }}','finally { value[key] = element; }','}','decoder.ReadToken((byte)\'}\');']
        elif t.startswith('[2]'):
            inner=t[3:];call=shape(inner)
            lines+=['if (decoder.PeekKind() == \'n\') { decoder.ReadToken((byte)\'n\'); value = '+mapped(t)+'.Make(2); return; }',f'decoder.ReadArrayStart("{go_type(t)}");','if (value.IsNil) value = '+mapped(t)+'.Make(2);','int count = 0;','while (decoder.PeekKind() != \']\')','{','if (count >= 2) { decoder.SkipValue(); count++; continue; }',f'{mapped(inner)} element = default!;', 'value[count] = element;',f'try {{ {call}(ref decoder, ref element); }}','finally { value[count] = element; }','count++;','}','for (int i = count; i < 2; i++) value[i] = default!;','decoder.ReadToken((byte)\']\');',f'if (count != 2) throw decoder.ErrorAfter("{go_type(t)}", count > 2 ? "too many array elements" : "too few array elements");']
        elif t in {'string','bool','int32','uint32','float64','int','uint'}:
            method={'string':'ReadString','bool':'ReadBoolean','int32':'ReadInt32','uint32':'ReadUint32','float64':'ReadFloat64','int':'ReadInt32','uint':'ReadUint32'}[t];lines += ['decoder.ReadFloat64(ref value);'] if t=='float64' else [f'value = decoder.{method}();']
        elif t=='any':lines+=['decoder.ReadAny(ref value);']
        elif t in {'DocumentUri','URI','Method'}:lines+=[f'value = new(decoder.ReadString("lsproto.{t}"));']
        elif t in {'Null','NoParams','struct{}'}:lines += [f'Read{mapped(t)}(ref decoder, ref value);']
        elif types[t]['kind']=='scalar':
            base=types[t]['type'];call=shape(base)
            if base=='string':lines+=[f'var text = decoder.ReadString("lsproto.{t}");',f'value = new {t}(text);']
            elif base in {'int32','uint32','bool'}:
                method={'int32':'ReadInt32','uint32':'ReadUint32','bool':'ReadBoolean'}[base]
                lines += [f'value = ({t})decoder.{method}("lsproto.{t}");']
            elif base=='float64':
                lines += [f'var number = ({mapped(base)})value;',f'try {{ decoder.ReadFloat64(ref number, "lsproto.{t}"); }}',f'finally {{ value = ({t})number; }}']
            else:raise ValueError('unsupported named scalar '+t+':'+base)
        else:lines += [f'Read{t}(ref decoder, ref value);']
        outputs.append('\n'.join(lines+['}']))
    namespace['shape']=shape
    namespace['mapped']=mapped
    return '\n\n'.join(outputs)+'\n'

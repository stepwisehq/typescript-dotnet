"""Closed concrete writer calls, preserving Go pointer/header zero distinctions."""
import hashlib,json
from pathlib import Path

def install(types,mapped,underlying):
    shapes={}
    def name(t):return 'WriteShape_'+hashlib.sha256(t.encode()).hexdigest()[:16]
    def add(t):
        if t in shapes:return
        shapes[t]=underlying(t)
        target=shapes[t]
        if target.startswith('*'):add(target[1:])
        elif target.startswith('[]'):add(target[2:])
        elif target.startswith('[2]'):add(target[3:])
        elif target.startswith('map['):add(target[target.index(']')+1:])
        elif target in types and types[target]['kind']=='scalar':add(types[target]['type'])
    for root in json.loads((Path(__file__).parent/'codec-root-shapes.json').read_text()):add(root)
    for spec in types.values():
        for f in spec.get('fields',[]):add(f['type'])
    def go_type(t):
        if t.startswith('*'): return '*'+go_type(t[1:])
        if t.startswith('[]'): return '[]'+go_type(t[2:])
        if t.startswith('map['):
            end=t.index(']'); return 'map['+go_type(t[4:end])+']'+go_type(t[end+1:])
        if t in types or t in {'DocumentUri','URI','Method','Null','NoParams'}: return 'lsproto.'+t
        return 'interface {}' if t=='any' else t
    out=[]
    value_structs={'Position','Range','BooleanOrEmptyObject','BooleanOrClientSemanticTokensRequestFullDelta'}|{n for n in types if n.startswith('Resolved')}
    for original,t in shapes.items():
        lines=[f'private static void {name(original)}(ProtocolEncoder encoder, {mapped(original)} value)','{']
        if t.startswith('*'):
            inner=underlying(t[1:]);reference=inner in types and types[inner]['kind']=='struct' and inner not in value_structs and bool(types[inner]['fields'])
            v='value.Value' if inner=='any' or not reference else 'value'
            lines+=['if (value is null) { encoder.WriteNull(); return; }']
            if reference:
                lines += [f'bool visited = encoder.VisitPointer(value, "*lsproto.{inner}");',
                          'try', '{', f'{name(t[1:])}(encoder, {v});', '}',
                          f'finally {{ encoder.LeavePointer(value, visited, "*lsproto.{inner}"); }}']
            else:
                lines += [f'{name(t[1:])}(encoder, {v});']
        elif t.startswith('[]'):
            inner=t[2:]
            lines += [f'bool visited = encoder.VisitSlice(value, "{go_type(original)}");', 'try', '{',
                      'encoder.BeginArray();', 'foreach (var element in value)',
                      f'    {name(inner)}(encoder, element);', 'encoder.EndArray();', '}',
                      f'finally {{ encoder.LeaveSlice(value, visited, "{go_type(original)}"); }}']
        elif t.startswith('[2]'):
            inner=t[3:]
            lines+=['if (!value.IsNil && value.Length != 2) throw new InvalidOperationException("fixed protocol array requires exactly two elements");','encoder.BeginArray();','for (int i = 0; i < 2; i++)',f'    {name(inner)}(encoder, value.IsNil ? default : value[i]);','encoder.EndArray();']
        elif t.startswith('map['):
            end=t.index(']');key=t[4:end];inner=t[end+1:];keyvalue='entry.Key.Value' if key=='DocumentUri' else 'entry.Key.Value'
            lines += ['var owner = value.CycleOwner;',
                      f'bool visited = owner is not null && encoder.VisitPointer(owner, "{go_type(original)}");',
                      'try', '{', 'encoder.BeginObject(checkNames: true);', 'foreach (var entry in value)', '{',
                      f'encoder.WriteProperty({keyvalue});', f'{name(inner)}(encoder, entry.Value);',
                      '}', 'encoder.EndObject();', '}',
                      f'finally {{ if (owner is not null) encoder.LeavePointer(owner, visited, "{go_type(original)}"); }}']
        elif t=='any':lines+=['encoder.WriteAny(value);']
        elif t=='string':lines+=['encoder.WriteString(value);']
        elif t=='bool':lines+=['encoder.WriteBoolean(value);']
        elif t in {'uint','int','uint32','int32','float64'}:lines+=['encoder.WriteNumber(value);']
        elif t in {'DocumentUri','URI','Method'}:lines+=['encoder.WriteString(value.Value);']
        elif t in {'Null','NoParams','struct{}'}:lines+=['encoder.BeginObject(); encoder.EndObject();'] if t!='Null' else ['encoder.WriteNull();']
        elif types[t]['kind']=='scalar':
            base=types[t]['type'];v='value.Value' if base=='string' else '('+mapped(base)+')value';lines+=[f'{name(base)}(encoder, {v});']
        else:lines += [f'Write{t}(encoder, value);']
        out.append('\n'.join(lines+['}']))
    return '\n\n'.join(out)+'\n'

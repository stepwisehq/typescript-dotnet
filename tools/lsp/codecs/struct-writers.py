"""Default typed Go struct writers and zero tests; draft closed codec outputs."""
import json,re,hashlib

def install(types, methods, mapped, underlying):
    custom={m['type'] for m in methods if m['method']=='MarshalJSONTo'} | {n for n in types if n.startswith('StringLiteral')}
    def ident(n):return '@'+n if n in {'String','Range','Integer'} else n
    def zero(t,v):
        t=underlying(t)
        if t.startswith('*'):return v+' is null'
        if t.startswith(('[]','map[')):return v+'.IsNil'
        if t.startswith('[2]'):return '('+v+'.IsNil || ('+v+'[0] == 0 && '+v+'[1] == 0))'
        if t=='any':return v+' is null'
        if t=='string':return v+'.Length == 0'
        if t=='bool':return '!'+v
        if t in {'int','uint','int32','uint32','float64'}:return v+' == 0'
        if t in {'DocumentUri','URI','Method'}:return v+'.Value.Length == 0'
        if t in {'Null','NoParams','struct{}'}:return 'true'
        spec=types[t]
        if spec['kind']=='scalar':return v+'.Value.Length == 0' if spec['type']=='string' else v+' == 0'
        return f'IsZero{t}({v})'
    def writer(t):return 'WriteShape_'+hashlib.sha256(t.encode()).hexdigest()[:16]
    outputs=[]
    for name,spec in types.items():
        if spec['kind']!='struct':continue
        is_value=name in {'Position','Range','BooleanOrEmptyObject','BooleanOrClientSemanticTokensRequestFullDelta'} or name.startswith('Resolved') or not spec['fields']
        members=[zero(f['type'],'value.'+ident(f['csName'])) for f in spec['fields']]
        test=' && '.join('('+m+')' for m in members) or 'true'
        if not is_value:test='value is null || ('+test+')'
        outputs.append(f'private static bool IsZero{name}({name}{"" if is_value else "?"} value) => {test};')
        if name in custom:continue
        fields=[dict(f,json=f['json'] or f['name']) for f in spec['fields'] if not f['json'] or f['json'].split(',')[0]!='-']
        out=[f'// Go default struct marshaler; source declaration lsp_generated.go:{spec["line"]}.', f'internal static void Write{name}(ProtocolEncoder encoder, {name} value)','{']
        if not is_value:out+=['value ??= new();']
        out += ['encoder.BeginObject();']
        for f in fields:
            raw,*flags=f['json'].split(',');
            if not re.fullmatch(r'[A-Za-z_][A-Za-z_0-9]*',raw):raise ValueError('Nonliteral ASCII field name: '+raw)
            v='value.'+ident(f['csName'])
            if 'omitzero' in flags:out += ['if (!('+zero(f['type'],v)+'))','{']
            if 'omitempty' in flags:raise ValueError('Explicit empty semantics required: '+name+raw)
            out += ['encoder.WriteProperty('+json.dumps(raw)+'u8);',f'{writer(f["type"])}(encoder, {v});']
            if 'omitzero' in flags:out += ['}']
        out += ['encoder.EndObject();','}'];outputs.append('\n'.join(out))
    return '\n\n'.join(outputs)+'\n'

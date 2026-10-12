#!/usr/bin/env python3
# Source-derived model/config dispatch for the pinned Go preference schema owner.
from pathlib import Path
import re
root=Path(__file__).resolve().parents[1]
g=(root/'upstream/TypeScript/tsc/internal/ls/lsutil/userpreferences_generated.go').read_text()
u=(root/'upstream/TypeScript/tsc/internal/ls/lsutil/userpreferences.go').read_text()+'\n'+(root/'upstream/TypeScript/tsc/internal/ls/lsutil/formatcodeoptions.go').read_text()
out=['// Copyright (c) Microsoft Corporation. All rights reserved.','// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.','// Pinned internal/ls/lsutil/userpreferences_generated.go models; embedded structs remain value owners.','#nullable disable','using TypeScript.Scanner;','using TypeScript.ModuleSpecifiers;','namespace TypeScript.LanguageServices.Utilities;']
for typ,base,body in re.findall(r'type (\w+) (string|int|bool)\s*\n\s*const \((.*?)\)',u,re.S):
 vals=re.findall(r'('+typ+r'\w+)\s*(?:'+typ+r')?\s*=\s*("[^"]*"|\d+|true|false|iota)',body)
 if base=='string':
  out+=['internal readonly record struct '+typ,'{','    private readonly string value;','    internal '+typ+'(string value) => this.value = value;','    internal string Value => value ?? "";','    public override string ToString() => Value;','    public bool Equals('+typ+' other) => Value == other.Value;','    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);']
  for n,v in vals:out+=['    internal static readonly '+typ+' '+n[len(typ):]+' = new('+v+');']
  out+=['}']
 else:
  out+=['internal enum '+typ+(' : long' if base=='int' else ''),'{']
  for idx,(n,v) in enumerate(vals):out+=['    '+n[len(typ):]+' = '+(str(idx) if v=='iota' else '1' if v=='true' else '0' if v=='false' else v)+',']
  # iota following declarations omit '='
  if 'iota' in body:
   names=re.findall(r'^\s*('+typ+r'\w+)',body,re.M)
   out=out[:-len(vals)] if vals else out
   out += ['    '+n[len(typ):]+' = '+str(i)+',' for i,n in enumerate(names)]
  out+=['}']
models={}
maptype={'core.Tristate':'Tristate','int':'long','string':'string','bool':'bool','[]string':'Slice<string>','modulespecifiers.ImportModuleSpecifierPreference':'ImportModuleSpecifierPreference','modulespecifiers.ImportModuleSpecifierEndingPreference':'ImportModuleSpecifierEndingPreference'}
for name,body in re.findall(r'type (\w+) struct \{(.*?)\n\}',g,re.S):
 fields=[];embeds=[]
 for line in body.splitlines():
  line=line.strip()
  if not line or line.startswith('//'):continue
  m=re.match(r'(\w+)\s+(\S+)\s+`',line)
  if m: fields.append((m[1],maptype.get(m[2],m[2])))
  elif re.fullmatch(r'\w+',line): embeds.append(line)
 models[name]=(fields,embeds)
for name,(fields,embeds) in models.items():
 out+=['internal partial struct '+name,'{']
 for embed in embeds:out+=['    internal '+embed+' '+embed+';']
 for field,typ in fields:
  if typ=='string':out+=['    private string '+field+'Value;', '    internal string '+field+' { readonly get => '+field+'Value ?? \"\"; set => '+field+'Value = value; }']
  else:out+=['    internal '+typ+' '+field+';']
 def inherited(n,prefix):
  fs,es=models[n]
  for field,typ in fs:yield field,typ,prefix+'.'+field
  for e in es:yield from inherited(e,prefix+'.'+e)
 for e in embeds:
  for field,typ,path in inherited(e,e):out+=['    internal '+typ+' '+field+' { readonly get => '+path+'; set => '+path+' = value; }']
 out+=['}']
(root/'src/TypeScript.Scanner/LanguageServices/Utilities/UserPreferences.Models.cs').write_text('\n'.join(out)+'\n')

import json
# Source declarations, paths and dispatch order come directly from the pinned generated Go owner.
models={}
for name,body in re.findall(r'type (\w+) struct \{(.*?)\n\}',g,re.S):
 fs=[];es=[]
 for line in body.splitlines():
  m=re.match(r'\s*(\w+)\s+(\S+)\s+`json:"([^"]+)',line)
  if m:fs.append((m[1],m[2],m[3].split(',')[0]))
  elif re.fullmatch(r'\s*\w+\s*',line):es.append(line.strip())
 models[name]=(fs,es)
def flattened(name):
 fs,es=models[name]
 for e in es:yield from flattened(e)
 yield from fs
fields=list(flattened('UserPreferences'));types={n:t for n,t,k in fields};tags={n:k for n,t,k in fields}
header='''// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned internal/ls/lsutil/userpreferences_generated.go configuration dispatch.
#nullable disable
using TypeScript.Scanner;
using TypeScript.ModuleSpecifiers;
namespace TypeScript.LanguageServices.Utilities;
'''
out=[header,'internal static partial class LsUtil','{']
# Generated enum parsing is source-specific rather than Enum.Parse/reflection.
for m in re.finditer(r'func (parsePreference\w+)\(value any\) ([\w.]+) \{(.*?)(?=\nfunc |\Z)',g,re.S):
 name,typ,body=m.groups();typ=typ.split('.')[-1]
 pairs=re.findall(r'case "([^"]+)":\s*return ([\w.]+)',body)
 final=re.findall(r'\n\treturn ([^\n]+)',body)[-1]
 def constant(c):
  c=c.removeprefix("modulespecifiers.")
  if c.startswith(typ):
   suffix=c[len(typ):]
   return 'default' if suffix=='("")' else typ+'.'+suffix
  return c
 out += ['    // userpreferences_generated.go:'+str(g[:m.start()].count('\n')+1)+'.', '    internal static '+typ+' '+name+'(object value)','    {','        if (value is string text)','        {','            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())','            {']
 for key,c in pairs:out+=['                case '+json.dumps(key)+': return '+constant(c)+';']
 out+=['            }','        }']
 if typ=='IndentStyle':out+=['        if (value is long integer) return (IndentStyle)integer;','        if (value is int smallInteger) return (IndentStyle)smallInteger;','        if (value is double number) return (IndentStyle)unchecked((long)number);']
 out+=['        return '+constant(final)+';','    }']
for m in re.finditer(r'func (serializePreference\w+)\(value (\w+)\) string \{(.*?)(?=\nfunc |\Z)',g,re.S):
 name,typ,body=m.groups();pairs=re.findall(r'case '+typ+r'(\w+):\s*return "([^"]*)"',body);fallback=re.search(r'default:\s*return "([^"]*)"',body).group(1)
 out+=['    // userpreferences_generated.go:'+str(g[:m.start()].count('\n')+1)+'.', '    internal static string '+name+'('+typ+' value) => value switch','    {']+['        '+typ+'.'+key+' => '+json.dumps(val)+',' for key,val in pairs]+['        _ => '+json.dumps(fallback),'    };']
out+=['}','','internal partial struct UserPreferences','{','    // userpreferences_generated.go:415. Original raw-name dispatch and nil-value admission.','    internal void ApplyRawPreferences(Dictionary<string, object> settings)','    {','        if (settings == null) return;','        foreach (var (name, value) in settings)','        {','            if (value == null) continue;','            switch (name)','            {']
def assign(field,expr=None):
 typ=types[field]
 if expr and expr.startswith('parse'):
  return field+' = LsUtil.'+expr+'(value);'
 if typ=='core.Tristate':return field+' = LsUtil.parsePreferenceTristate(value);'
 if typ=='int':return 'if (value is long integer) '+field+' = integer; else if (value is int smallInteger) '+field+' = smallInteger; else if (value is double number) '+field+' = unchecked((long)number);'
 if typ=='bool':return 'if (value is bool boolean) '+field+' = boolean;'
 if typ=='string':return 'if (value is string text) '+field+' = text;'
 if typ=='[]string':return 'if (value is List<object> values) { var result = Slice<string>.Make(0, values.Count); foreach (var item in values) if (item is string text) result = result.Append(text); '+field+' = result; }'
 return field+' = LsUtil.parsePreference'+typ.split('.')[-1]+'(value);'
raw=g[g.index('func (p *UserPreferences) applyRawPreferences'):g.index('func (p UserPreferences) withConfig')]
for n,t,k in fields:out+=['                case '+json.dumps(k)+': { '+assign(n)+' break; }']
out+=['            }','        }','    }','    // userpreferences_generated.go:637. Each call copies embedded settings before applying precedence.','    internal UserPreferences WithConfig(Dictionary<string, object> config)','    {','        var p = this;','        p.ApplyRawPreferences(config);','        if (config != null && config.TryGetValue("unstable", out var unstable) && unstable is Dictionary<string, object> raw) p.ApplyRawPreferences(raw);']
config=g[g.index('func (p UserPreferences) withConfig'):g.index('func (p *UserPreferences) MarshalJSONTo')]
# Each getPreferenceConfigValue block has exactly one destination field (possibly repeated type cases).
starts=list(re.finditer(r'if value, ok := getPreferenceConfigValue\(config, (.*?)\); ok \{',config))
for i,m in enumerate(starts):
 body=config[m.end():starts[i+1].start() if i+1<len(starts) else len(config)]
 dest=re.search(r'p\.([\w.]+)\s*=',body).group(1).split('.')[-1]
 parser=re.search(r'= (parse\w+)\(value\)',body)
 args=m.group(1)
 stmt=assign(dest,parser.group(1) if parser else None)
 guard=' && value != null' if 'if value != nil' in body else ''
 out+=['        if (LsUtil.GetPreferenceConfigValue(config, out var value'+str(i)+', '+args+'))','        {','            var value = value'+str(i)+';']
 if guard:out+=['            if (value != null) { '+ 'p.'+stmt.replace(' '+dest+' =',' p.'+dest+' =').replace('; '+dest+' =','; p.'+dest+' =')+' }']
 else:out+=['            p.'+stmt]
 # statement prefixes need p on assignment, not if declaration.
 line=out[-1]
 line=line.replace('p.if (','if (').replace(' '+dest+' =',' p.'+dest+' =')
 out[-1]=line
 out+=['        }']
out+=['        p.CustomConfigFileName = LsUtil.NormalizeCustomConfigFileName(p.CustomConfigFileName);','        return p;','    }','}']
(root/'src/TypeScript.Scanner/LanguageServices/Utilities/UserPreferences.Generated.cs').write_text('\n'.join(out)+'\n')
# Marshal dispatch retains original conditional omission and unconditional enum/default fields.
out=['internal partial struct UserPreferences','{','    // userpreferences_generated.go:1053. Deterministic JSON is emitted from these original nested paths.','    internal readonly Dictionary<string, object> ToConfiguration()','    {','        var config = new Dictionary<string, object>(StringComparer.Ordinal);']
marshal=g[g.index('func (p *UserPreferences) MarshalJSONTo'):]
for m in re.finditer(r'setNestedValue\(config, "([^"]+)", ([^\n]+)\)',marshal):
 path,expr=m.groups();field=re.search(r'p\.([\w.]+)',expr).group(1).removesuffix('.IsTrue').split('.')[-1];typ=types[field]
 serial=re.search(r'(serializePreference\w+)\(',expr)
 if serial: val='LsUtil.'+serial.group(1)+'('+field+')';condition=None
 elif typ=='core.Tristate': val=field+(' != Tristate.True' if expr.lstrip().startswith('!') else ' == Tristate.True');condition=field+' == Tristate.True || '+field+' == Tristate.False'
 elif typ=='int' or typ=='IndentStyle':val='(long)'+field;condition=field+' != 0'
 elif typ=='bool':val=field;condition=None
 elif typ=='[]string':val='LsUtil.StringSliceToArray('+field+')';condition=field+'.Count != 0'
 elif typ=='string':val=field;condition=field+'.Length != 0'
 elif typ.startswith('modulespecifiers.'):
  val='LsUtil.ModulePreferenceText('+field+')';condition=field+'.Value.Length != 0'
 else:val=field+'.Value';condition=field+'.Value.Length != 0'
 out+=['        '+('if ('+condition+') ' if condition else '')+'LsUtil.SetNestedValue(config, '+json.dumps(path)+', '+val+');']
out+=['        return config;','    }','}']
with (root/'src/TypeScript.Scanner/LanguageServices/Utilities/UserPreferences.Generated.cs').open('a') as f:f.write('\n'.join(out)+'\n')

#!/usr/bin/env python3
"""Mechanical C# spelling/enum adaptation after the Go AST statement emitter."""
from pathlib import Path
import re
root=Path('upstream/TypeScript/tsc/internal/ast')
dst=Path('src/TypeScript.Scanner/Ast')
prefixes={'Kind':'Kind','NodeFlags':'NodeFlags','ModifierFlags':'ModifierFlags','JSDeclarationKind':'JSDeclarationKind','OperatorPrecedence':'OperatorPrecedence','OEK':'OuterExpressionKinds'}
def enum(s,name,prefix,ctype):
 block=next(b for b in re.findall(r'const \((.*?)\n\)',s,re.S) if re.search(r'\b'+prefix+r'\w+\s+'+name+r'\s*=.*iota|\b'+prefix+r'\w+\s+'+name+r'\s*=',b))
 out=[];index=0;prev=None
 for line in block.splitlines():
  line=line.split('//')[0].strip()
  if not line:continue
  m=re.fullmatch(prefix+r'(\w+)(?:\s+'+name+r')?(?:\s*=\s*(.*))?',line)
  if not m:continue
  n,v=m.groups()
  if v is None:v=prev
  else:prev=v
  assert v is not None,(name,line)
  v=v.replace('iota',str(index));index+=1
  v=re.sub(prefix+r'(\w+)',r'\1',v).replace('&^','& ~').replace('^','~')
  out.append('    '+n+' = '+v+',')
 return 'public enum '+name+' : '+ctype+'\n{\n'+'\n'.join(out)+'\n}\n'
for file in ['Utilities.cs','Precedence.cs']:
 p=dst/file;s=p.read_text()
 for prefix,name in prefixes.items():s=re.sub(r'(?<![.\w])'+prefix+r'(\w+)\b',name+r'.\1',s)
 s=s.replace('SliceLen(', 'GoRuntime.Len(').replace('slices.', 'GoSlices.').replace('max(', 'Math.Max(')
 s=s.replace('GoStrings.IndexAny(text.Substring(index), \"ir\")', 'text.AsSpan(index).IndexOfAny(\'i\', \'r\')')
 s=s.replace('GoSlices.Contains(kinds, node.Kind)', 'kinds.Contains(node.Kind)')
 s=re.sub(r'(return [^;\n]+;|throw [^;\n]+;)\n\s*break;',r'\1',s)
 src=(root/('utilities.go' if file=='Utilities.cs' else 'precedence.go')).read_text()
 if file=='Utilities.cs':s+='\n'+enum(src,'OuterExpressionKinds','OEK','ushort')+'\n'+enum(src,'JSDeclarationKind','JSDeclarationKind','int')
 else:s+='\n'+enum(src,'OperatorPrecedence','OperatorPrecedence','int')
 p.write_text(s)

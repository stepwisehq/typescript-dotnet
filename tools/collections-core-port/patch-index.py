#!/usr/bin/env python3
from pathlib import Path
import re,sys
s=Path(sys.argv[1]).read_text()
for e in ['AccessFlags','CachedTypeKind','AssignmentKind']:
 s=re.sub(r'\b'+e+r'([A-Z]\w*)\b',lambda m:e+'.'+m.group(1),s)
s=s.replace('& ^TypeFlags.Substitution','& ~TypeFlags.Substitution')
s=s.replace('this.isGenericTupleType(', 'isGenericTupleType(')
s=s.replace('keyBuilder b = default;', 'var b = new keyBuilder();')
s=re.sub(r'var (cached|t) = (this\.cachedTypes|this\.indexedAccessTypes)\[([^\n]+)\];',r'\2.TryGetValue(\3, out var \1);',s)
s=s.replace('this.TypeToString(', 'this.typeToStringByteString(')
s=s.replace('Scanner.GetTextOfNode(', 'TypeScript.Scanner.Scanner.GetTextOfNodeByteString(')
s=s.replace('jsnum.FromString(', 'JsNum.FromString(')
s=s.replace('accessFlags|core.IfElse','accessFlags | Core.IfElse')
s=s.replace('Core.IfElse(wasMissingProp, AccessFlags.SuppressNoImplicitAnyError, 0)', 'Core.IfElse(wasMissingProp, AccessFlags.SuppressNoImplicitAnyError, AccessFlags.None)')
s=s.replace('this.IsEmptyAnonymousObjectType(', 'IsEmptyAnonymousObjectType(')
Path(sys.argv[2]).write_text(s)

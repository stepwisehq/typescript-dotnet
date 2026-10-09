#!/usr/bin/env python3
from pathlib import Path
import re,sys
s=Path(sys.argv[1]).read_text()
for enum in ['UnionReduction','AccessFlags','ContextFlags','IterationUse']:
 s=re.sub(r'\b'+enum+r'([A-Z]\w*)\b',lambda m:enum+'.'+m.group(1),s)
s=s.replace('jsnum.Number((i - index))', 'new JsNum.Number(i - index)')
s=s.replace('return this.inferJsxTypeArguments(node, signature, checkMode, context);', 'throw unsupported("inferTypeArguments: JSX");')
s=s.replace('List<Diagnostic> diags = default;', 'var diags = new List<Diagnostic>();')
s=s.replace('this.isTupleType(', 'isTupleType(').replace('this.isGenericTupleType(', 'isGenericTupleType(')
Path(sys.argv[2]).write_text(s)

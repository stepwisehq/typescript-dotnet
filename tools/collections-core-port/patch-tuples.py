#!/usr/bin/env python3
"""Selected checker.go tuple scratch adaptations; hand-reviewed source authoritative."""
from pathlib import Path
import re,sys
sys.path.insert(0,str(Path(__file__).parent))
from install import methods
s=Path(sys.argv[1]).read_text()
# Go has package and receiver helpers with identical bodies. Keep the package
# boundary and qualify receiver callsites with the same selected static method.
for name,start,end,body in reversed(list(methods(s))):
 if name=='isGenericTupleType' and body.startswith('    private bool'):
  s=s[:start]+s[end:]
s=re.sub(r'\breadonly\b', '@readonly', s)
s=s.replace('keyBuilder b = default;', 'var b = new keyBuilder();')
s=s.replace("b.writeByte('","b.writeByte((byte)'")
s=s.replace('var t = this.tupleTypes[key];','this.tupleTypes.TryGetValue(key, out var t);')
s=s.replace('Strconv.Itoa(i)', 'i.ToString(System.Globalization.CultureInfo.InvariantCulture)')
s=s.replace('Ast.CheckFlagsReadonly','CheckFlags.Readonly')
s=s.replace('SymbolFlags.Optional, 0)', 'SymbolFlags.Optional, SymbolFlags.None)').replace('CheckFlags.Readonly, 0)', 'CheckFlags.Readonly, CheckFlags.None)')
s=s.replace('jsnum.Number(i2)', 'new JsNum.Number(i2)').replace('jsnum.Number(getTotalFixedElementCount(t2.TargetTupleType()))','new JsNum.Number(getTotalFixedElementCount(t2.TargetTupleType()))').replace('Number index','JsNum.Number index')
for e in ['UnionReduction']:
 s=re.sub(r'\b'+e+r'([A-Z]\w*)\b',lambda m:e+'.'+m.group(1),s)
n=Path(sys.argv[2]).read_text()
bodies=[]
for name,start,end,body in methods(n):
 body=body.replace('private bool normalize','internal bool normalize')
 body=body.replace('this.infos[i2].flags = ElementFlags.Required;', 'var info = this.infos[i2]; info.flags = ElementFlags.Required; this.infos[i2] = info;')
 bodies.append("\n".join("    " + line if line else line for line in body.splitlines()))
model='''
    // checker.go:23879. Required internal variadic normalization is retained even
    // though source generic variadic tuple composition is outside admission.
    private sealed class TupleNormalizer
    {
        internal Checker c;
        internal List<Type> types;
        internal List<TupleElementInfo> infos;
        private int lastRequiredIndex, firstRestIndex, lastOptionalOrRestIndex;
'''+ '\n'.join(bodies) + '\n    }\n'
s=s[:s.rfind('}')]+model+'}\n'
Path(sys.argv[3]).write_text(s)

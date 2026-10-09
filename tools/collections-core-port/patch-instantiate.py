#!/usr/bin/env python3
from pathlib import Path
import re,sys
s=Path(sys.argv[1]).read_text()
s=s.replace('return this.getTemplateLiteralType(t.AsTemplateLiteralType().texts, this.instantiateTypes(t.AsTemplateLiteralType().types, m));','throw unsupported("instantiateTypeWorker: template literal type");')
s=s.replace('return this.getStringMappingType(t.symbol, this.instantiateType(t.AsStringMappingType().target, m));','throw unsupported("instantiateTypeWorker: intrinsic string mapping");')
s=s.replace('alias.Symbol()', 'alias?.symbol').replace('t.alias.Symbol()', 't.alias?.symbol')
s=s.replace('this.isGenericTupleType(', 'isGenericTupleType(')
s=re.sub(r'\breadonly\b','@readonly',s)
s=s.replace('Strconv.Itoa(i)','i.ToString(System.Globalization.CultureInfo.InvariantCulture)')
for e in ['UnionReduction']:
 s=re.sub(r'\b'+e+r'([A-Z]\w*)\b',lambda m:e+'.'+m.group(1),s)
for flag in ['Optional','Required']:
 s=s.replace('newElementInfos[i].flags = ElementFlags.'+flag+';', 'var elementInfo = newElementInfos[i]; elementInfo.flags = ElementFlags.'+flag+'; newElementInfos[i] = elementInfo;')
Path(sys.argv[2]).write_text(s)

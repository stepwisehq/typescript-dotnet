#!/usr/bin/env python3
"""Review adaptations for the first generic declaration/reference slice; scratch only."""
from pathlib import Path
import re,sys
p=Path(sys.argv[1]);s=p.read_text()
s=s.replace('private List<Type> getTypeArguments(', 'internal List<Type> getTypeArguments(')
s=s.replace('copy(result, typeArguments);', 'if (typeArguments != null) result.SetRange(0, typeArguments);')
s=s.replace('result.SetRange(0, typeArguments);','for (int copied = 0; copied < Math.Min(result.Count, typeArguments.Count); copied++) result[copied] = typeArguments[copied];')
s=s.replace('Ast.CheckFlagsUnresolved','CheckFlags.Unresolved')
s=s.replace('var errorType = this.errorTypes[key];', 'this.errorTypes.TryGetValue(key, out var errorType);')
s=s.replace('var instantiation = links.instantiations[key];','links.instantiations.TryGetValue(key, out var instantiation);')
s=s.replace('this.TypeToStringEx(t, null, TypeFormatFlags.WriteArrayAsGenericType, null)','this.typeToStringWriteArrayAsGenericType(t)')
# Intrinsic string manipulation remains explicitly excluded; selected NoInfer closure
# is restored with the later operator/inference batch, never substituted with its input.
start=s.index('        if ((t == this.intrinsicMarkerType))',s.index('private Type getTypeAliasInstantiation'))
end=s.index('        var links = this.typeAliasLinks.Get(symbol);', start)
s=s[:start]+'        if (t == intrinsicMarkerType)\n            throw unsupported("getTypeAliasInstantiation: intrinsic type operator");\n'+s[end:]
s=re.sub(r'^    \s*$', '', s, flags=re.M)
p.write_text(s)

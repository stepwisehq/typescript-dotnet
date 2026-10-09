#!/usr/bin/env python3
from pathlib import Path
import re,sys
s=Path(sys.argv[1]).read_text()
s=s.replace('& ^t.flags','& ~t.flags')
for e in ['CachedTypeKind','RelationComparisonResult']:
 s=re.sub(r'\b'+e+r'([A-Z]\w*)\b',lambda m:e+'.'+m.group(1),s)
s=re.sub(r'var (cached|result) = (this\.cachedTypes|root\.instantiations|this\.substitutionTypes)\[([^\n]+)\];', r'\2.TryGetValue(\3, out var \1);',s)
s=s.replace('this.cachedTypes[new CachedTypeKey { kind = CachedTypeKind.RestrictiveInstantiation, typeId = t.id }] != t','this.cachedTypes.GetValueOrDefault(new CachedTypeKey { kind = CachedTypeKind.RestrictiveInstantiation, typeId = t.id }) != t')
s=s.replace('keyBuilder b = default;', 'var b = new keyBuilder();')
s=s.replace("b.writeByte('","b.writeByte((byte)'")
# Diagnostic builder is a separate counterpart and consumes these actual helper
# boundaries. Visibility adaptation only; no result substitution.
for name in ['getHomomorphicTypeVariable','getTypeParameterFromMappedType','getConstraintTypeFromMappedType','getNameTypeFromMappedType','getTemplateTypeFromMappedType','isMappedTypeWithKeyofConstraintDeclaration','getModifiersTypeFromMappedType','getTrueTypeFromConditionalType','getFalseTypeFromConditionalType','isNoInferType']:
 s=re.sub(r'private (\w+) '+name+r'\(',r'internal \1 '+name+'(',s)
s=s.replace('private static MappedTypeModifiers getMappedTypeModifiers','internal static MappedTypeModifiers getMappedTypeModifiers')
Path(sys.argv[2]).write_text(s)

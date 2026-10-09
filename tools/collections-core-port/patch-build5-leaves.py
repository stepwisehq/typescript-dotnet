#!/usr/bin/env python3
from pathlib import Path
import sys,re
s=Path(sys.argv[1]).read_text()
# The fixed environment has no Iterable and excludes async/custom iterator source.
# Retain original guard; cache/fast/slow negative probing is implemented separately.
start=s.index('        if ((iterableExists || allowAsyncIterables))');end=s.index('        var arrayType = inputType;',start)
s=s[:start]+'''        if (iterableExists || allowAsyncIterables)
            throw unsupported("getIteratedTypeOrElementType: custom sync/async iterator environment");
'''+s[end:]
s=re.sub(r'\bIterationUse([A-Z]\w*)\b',lambda m:'IterationUse.'+m.group(1),s)
s=s.replace('UnionReductionSubtype','UnionReduction.Subtype').replace('UnionReductionLiteral','UnionReduction.Literal')
s=s.replace('this.TypeToString(', 'this.typeToStringByteString(')
# Selected synchronous resolver probes the real iterable cache.
s=s.replace('var yieldType = this.getIterationTypeOfIterable(use, IterationTypeKindYield, inputType, null);','var yieldType = this.getIterationTypesOfIterable(inputType, use, null).yieldType;')
s=s.replace('private bool isStringIndexSignatureOnlyTypeWorker(', 'private bool isStringIndexSignatureOnlyType(')
Path(sys.argv[2]).write_text(s)

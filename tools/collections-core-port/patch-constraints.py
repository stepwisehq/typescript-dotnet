#!/usr/bin/env python3
from pathlib import Path
import sys
s=Path(sys.argv[1]).read_text().replace('this.isGenericTupleType(', 'isGenericTupleType(').replace('.readonly', '.@readonly')
s=s.replace('return this.getStringMappingType(t.symbol, constraint4);','throw unsupported("computeBaseConstraint: intrinsic string mapping");')
s=s.replace('this.TypeToString(', 'this.typeToStringByteString(')
Path(sys.argv[2]).write_text(s)

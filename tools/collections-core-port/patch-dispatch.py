#!/usr/bin/env python3
"""Adapt reviewed type-node/index-member scratch before manual integration."""
from pathlib import Path
import re,sys
s=Path(sys.argv[1]).read_text()
s=s.replace('GoSlices.Collect(maps.Values(this.getMembersOfSymbol(symbol)))', '(this.getMembersOfSymbol(symbol)?.Values.ToList())')
s=s.replace('UnionReductionSubtype','UnionReduction.Subtype')
s=s.replace('this.isArrayOrTupleType','isArrayOrTupleType')
s=s.replace('this.isTypeAssignableToKind(this.checkComputedPropertyName(name), TypeFlags.NumberLike)', 'this.isTypeAssignableToKind(this.checkComputedPropertyName(name), TypeFlags.NumberLike)')
s=s.replace('private Type getOrCreateTypeFromSignature(', 'internal Type getOrCreateTypeFromSignature(')
Path(sys.argv[2]).write_text(s)

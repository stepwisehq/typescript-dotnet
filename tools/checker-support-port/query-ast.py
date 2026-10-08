#!/usr/bin/env python3
"""Integrate selected emitted AST query predicates without overwriting prior slices.
Usage: query-ast.py EMITTED_QUERY_CS DESTINATION_UTILITIES_CS
Generate the input with checker-support-port and query-functions.txt first.
"""
from pathlib import Path
import sys
source=Path(sys.argv[1]).read_text().replace('GoSlices.Contains(parent.TypeArguments(), node)', '(parent.TypeArguments()?.Contains(node) == true)')
start=source.index('public static partial class Ast')
section='// BEGIN GENERATED CHECKER QUERY AST CLOSURE\n'+source[start:]+'// END GENERATED CHECKER QUERY AST CLOSURE\n\n'
p=Path(sys.argv[2]); text=p.read_text()
if '// BEGIN GENERATED CHECKER QUERY AST CLOSURE' in text:
 a=text.index('// BEGIN GENERATED CHECKER QUERY AST CLOSURE')
 b=text.index('// END GENERATED CHECKER QUERY AST CLOSURE',a)+len('// END GENERATED CHECKER QUERY AST CLOSURE')
 text=text[:a]+text[b:].lstrip('\n')
marker='// BEGIN GENERATED BINDER UTILITY CLOSURE'
if text.count(marker)!=1: raise SystemExit('Expected one existing binder slice marker')
text=text.replace(marker,section+marker)
p.write_text(text)

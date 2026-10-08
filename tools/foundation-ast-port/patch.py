#!/usr/bin/env python3
"""Narrow language adaptations for generated syntax fragments; no source overwrite."""
import sys
from pathlib import Path
p=Path(sys.argv[1]);s=p.read_text()
s=s.replace('fmt.Sprintf("Unexpected node kind %q", accessor.Kind)', '"Unexpected node kind \\\"" + accessor.Kind.String() + "\\\""')
s=s.replace('fmt.Sprintf("Node that does not have modifiers tried to have modifier replaced: %d", node.Kind)', '$"Node that does not have modifiers tried to have modifier replaced: {(int)node.Kind}"')
s=s.replace('if (visitNodes(v, node.JSDoc(sourceFile)))','if (VisitNodes(v, node.JSDoc(sourceFile)))')
s=s.replace('.AsImportClause().IsTypeOnly()', '.IsTypeOnly()')
s=s.replace('        Node @class = default;\n        bool isImplements = default;\n', '')
s=s.replace('return node.ForEachChild(traverse);', 'return node.ForEachChild(child => traverse(child));')
s=s.replace('var ok = pragma.Args.TryGetValue(name, out var arg);\n                if (ok)', 'if (pragma.Args != null && pragma.Args.TryGetValue(name, out var arg))')
p.write_text(s)

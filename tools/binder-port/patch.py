#!/usr/bin/env python3
"""Explicit raw-byte and bounded Go-runtime adaptations for binder.go emission.
Emit to scratch before regeneration; compare normalized output to reviewed C#.
"""
from pathlib import Path
import sys
root = Path(__file__).resolve().parents[2]
output = Path(sys.argv[1])
s = output.read_text()
s = s.replace('return ;', 'return;')
s = s.replace('Scanner.DeclarationNameToString(', 'Scanner.DeclarationNameToStringByteString(')
s = s.replace('Scanner.GetSourceTextOfNodeFromSourceFile(', 'Scanner.GetSourceTextOfNodeFromSourceFileByteString(')
s = s.replace('.notConstEnumOnlyModules.Has(', '.notConstEnumOnlyModules.Contains(')
# Go reads from a nil map return the zero value; preserve nil tables instead of allocating.
s = s.replace('localsContainer.Locals[name]', 'localsContainer.Locals?[name]')
s = s.replace('declaration.Symbol.Exports[name]', 'declaration.Symbol.Exports?[name]')
s = s.replace('moduleExports[Ast.InternalSymbolNameExportEquals]', 'moduleExports?[Ast.InternalSymbolNameExportEquals]')
# FileName/PathKey remain public .NET strings in SourceFileParseOptions; AST scalar strings are byte strings.
s = s.replace('this.file.FileName().RemoveFileExtension().AsString()', 'TsPath.RemoveFileExtension(GoString.ToUtf8(this.file.FileName() ?? ""))')
# Pinned Go's int is 64-bit; preserve the full identity when constructing private names.
s = s.replace('Strconv.Itoa((int)(Ast.GetSymbolId(containingClassSymbol)))', 'Strconv.Itoa(unchecked((long)Ast.GetSymbolId(containingClassSymbol)))')
s += '\n'    + (root/'tools/binder-port/declarations/Binder.cs.inc').read_text()
output.write_text(s)

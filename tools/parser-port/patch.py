#!/usr/bin/env python3
"""Explicit compatibility edits after the fail-fast Go AST scaffold emits parser C#.

Reviewed source files remain authoritative; use this only while rebuilding the scaffold.
"""
from pathlib import Path
import re
import sys
root=Path(__file__).resolve().parents[2]
output=Path(sys.argv[1]) if len(sys.argv)>1 else root/"src/TypeScript.Scanner/Parser"
for p in output.glob('*.cs'):
 if p.name=='Types.cs':continue
 s=p.read_text().replace('public static SourceFile ParseSourceFile(', 'private static SourceFile ParseSourceFileRaw(').replace('public static Node ParseIsolatedEntityName(', 'private static Node ParseIsolatedEntityNameRaw(')
 s=s.replace('.scanner.SetText(','.scanner.SetByteTextInternal(').replace('.scanner.SetOnError(','.scanner.SetOnErrorByteStrings(').replace('.scanner.TokenValue()', '.scanner.TokenValueByteString()').replace('.scanner.TokenText()', '.scanner.TokenTextByteString()')
 s=s.replace('Scanner.SkipTrivia(', 'Scanner.SkipTriviaByteString(').replace('Scanner.IsValidIdentifier(', 'Scanner.IsValidIdentifierByteString(')
 s=re.sub(r'Scanner.Get(Leading|Trailing)CommentRanges\(f, ',r'Scanner.Get\1CommentRangesByteString(',s)
 for var in ['commentRange','comment']:
  s=s.replace(var+'.Pos()',var+'.Pos').replace(var+'.End()',var+'.End')
 s=s.replace('return ;','return;').replace('this.notParenthesizedArrow.Has(', 'this.notParenthesizedArrow.Contains(')
 s=s.replace('result.Nodes = missingListNodes;\n        result.IsMissing = true;', 'result.Nodes = missingListNodes;')
 s=s.replace('result.Nodes = missingListNodes;', 'result.Nodes = missingListNodes;\n        result.IsMissing = true;')
 s=re.sub(r'(?<![.:])jsdocScannerInfo\.', 'global::TypeScript.Scanner.jsdocScannerInfo.',s)
 s=s.replace('result.CommentDirectives = this.scanner.CommentDirectives();','result.CommentDirectives = this.scanner.CommentDirectives()?.ToList();')
 s=s.replace('Core.IfElse(inForStatementInitializer,', 'Core.IfElse<Func<Parser,Node>>(inForStatementInitializer,')
 s=s.replace('var parseElement = (parserCallback)', 'Func<Parser,Node> parseElement = (parserCallback)')
 s=s.replace('return ModuleKind.CommonJS;', 'return ResolutionMode.CommonJS;').replace('return ModuleKind.ESNext;', 'return ResolutionMode.ESNext;')
 s=s.replace('Scanner.GetTextOfNodeFromSourceText(', 'Scanner.GetTextOfNodeFromSourceTextByteString(')
 # This upstream named result is never read; retaining its unused declaration fails warning-as-error.
 s=s.replace('Node node = default;\n        bool nameOk = default;', 'bool nameOk = default;')
 s=re.sub(r'Core\.(ExclusivelyPrefixedNodeCoreModules|UnprefixedNodeCoreModules)\[moduleName\]',r'Core.\1.Contains(moduleName)',s)
 s=s.replace('resolutionKind = ModuleKind.', 'resolutionKind = ResolutionMode.')
 s=s.replace('file.CheckJsDirective.Range.Pos()', 'file.CheckJsDirective.Range.Pos').replace('checkJsDirective.Range.Pos()', 'checkJsDirective.Range.Pos')
 s=s.replace('context.CheckJsDirective.Range.Pos()', 'context.CheckJsDirective.Range.Pos')
 template=root/'tools/parser-port/declarations'/(p.name+'.inc')
 if template.exists():
  declarations=template.read_text()
  marker=declarations.splitlines()[0]
  if marker not in s:s+='\n'+declarations
 p.write_text(s)

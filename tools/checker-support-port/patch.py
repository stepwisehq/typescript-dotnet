#!/usr/bin/env python3
"""Exact NameResolver declarations and Go nil-map-read adaptation; emit to scratch."""
from pathlib import Path
import sys

path = Path(sys.argv[1])
text = path.read_text().replace('return ;', 'return;')
for expression in ['moduleExports[Ast.InternalSymbolNameDefault]', 'moduleExports[name]', 'symbols[name]']:
    text = text.replace(expression, expression.replace('[', '?[', 1))
text += '''
// Declaration counterpart from binder/nameresolver.go; params delegates keep raw args.
public delegate Diagnostic NameResolverError(Node location, Message message, params object[] args);
public partial class NameResolver
{
    public CompilerOptions CompilerOptions;
    public Func<Node,Symbol> GetSymbolOfDeclaration;
    public NameResolverError Error;
    public SymbolTable Globals;
    public Symbol ArgumentsSymbol, RequireSymbol;
    public Func<SymbolTable,string,SymbolFlags,Symbol> Lookup;
    public Action<Symbol,SymbolFlags> SymbolReferenced;
    public Action<Node,Tristate> SetRequiresScopeChangeCache;
    public Func<Node,Tristate> GetRequiresScopeChangeCache;
    public Func<Node,string,Node,Symbol,bool> OnPropertyWithInvalidInitializer;
    public Action<Node,string,SymbolFlags,Message> OnFailedToResolveSymbol;
    public Action<Node,Symbol,SymbolFlags,Node,Node,bool> OnSuccessfullyResolvedSymbol;
}
'''
path.write_text(text)

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/binder/referenceresolver.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
namespace TypeScript.Scanner;
internal interface IReferenceResolver
{
    Node GetReferencedExportContainer(Node node, bool prefixLocals);
    Node GetReferencedImportDeclaration(Node node);
    Node GetReferencedValueDeclaration(Node node);
    Slice<Node> GetReferencedValueDeclarations(Node node);
    string GetElementAccessExpressionName(ElementAccessExpression expression);
    Node GetReferencedMemberValueDeclaration(Node node);
}
internal sealed class ReferenceResolverHooks
{
    internal Func<Node, ByteStringValue, SymbolFlags, Message, bool, bool, Symbol> ResolveName;
    internal Func<Node, Symbol> GetResolvedSymbol, GetSymbolOfDeclaration;
    internal Func<Symbol, Symbol> GetMergedSymbol, GetParentOfSymbol, GetExportSymbolOfValueSymbolIfExported;
    internal Func<Symbol, SymbolFlags, Node> GetTypeOnlyAliasDeclaration;
    internal Func<ElementAccessExpression, (ByteStringValue, bool)> GetElementAccessExpressionName;
}
internal sealed class ReferenceResolver : IReferenceResolver
{
    private NameResolver resolver;
    private readonly CompilerOptions options;
    private readonly ReferenceResolverHooks hooks;
    internal ReferenceResolver(CompilerOptions options, ReferenceResolverHooks hooks) { this.options = options; this.hooks = hooks; }
    private Symbol getResolvedSymbol(Node node) => node != null ? hooks.GetResolvedSymbol?.Invoke(node) : null;
    private Symbol getMergedSymbol(Symbol symbol) => symbol != null ? hooks.GetMergedSymbol != null ? hooks.GetMergedSymbol(symbol) : symbol : null;
    private Symbol getParentOfSymbol(Symbol symbol) => symbol != null ? hooks.GetParentOfSymbol != null ? hooks.GetParentOfSymbol(symbol) : symbol.Parent : null;
    private Symbol getSymbolOfDeclaration(Node declaration) => declaration != null ? hooks.GetSymbolOfDeclaration != null ? hooks.GetSymbolOfDeclaration(declaration) : declaration.Symbol() : null;
    private Symbol getReferencedValueSymbol(Node reference, bool startInDeclarationContainer)
    {
        var resolved = getResolvedSymbol(reference); if (resolved != null) return resolved;
        var location = reference;
        if (startInDeclarationContainer && reference.Parent != null && Ast.IsDeclaration(reference.Parent) && reference.Parent.Name() == reference) location = Ast.GetDeclarationContainer(reference.Parent);
        if (hooks.ResolveName != null) return hooks.ResolveName(location, reference.TextValue(), SymbolFlags.ExportValue | SymbolFlags.Value | SymbolFlags.Alias, null, false, false);
        resolver ??= new NameResolver { CompilerOptions = options };
        return resolver.Resolve(location, reference.TextValue(), SymbolFlags.ExportValue | SymbolFlags.Value | SymbolFlags.Alias, null, false, false);
    }
    private bool isTypeOnlyAliasDeclaration(Symbol symbol)
    {
        if (symbol == null) return false;
        if (hooks.GetTypeOnlyAliasDeclaration != null) return hooks.GetTypeOnlyAliasDeclaration(symbol, SymbolFlags.Value) != null;
        var node = getDeclarationOfAliasSymbol(symbol);
        while (node != null)
        {
            switch (node.Kind)
            {
                case Kind.ImportEqualsDeclaration: case Kind.ExportDeclaration: return node.IsTypeOnly();
                case Kind.ImportClause: case Kind.ImportSpecifier: case Kind.ExportSpecifier:
                    if (node.IsTypeOnly()) return true;
                    node = node.Parent; continue;
                case Kind.NamedImports: case Kind.NamedExports: node = node.Parent; continue;
            }
            break;
        }
        return false;
    }
    private static Node getDeclarationOfAliasSymbol(Symbol symbol) => Core.FindLast(symbol.Declarations, Ast.IsAliasSymbolDeclaration);
    private Symbol getExportSymbolOfValueSymbolIfExported(Symbol symbol)
    {
        if (symbol == null) return null;
        if (hooks.GetExportSymbolOfValueSymbolIfExported != null) return hooks.GetExportSymbolOfValueSymbolIfExported(symbol);
        if ((symbol.Flags & SymbolFlags.ExportValue) != 0 && symbol.ExportSymbol != null) symbol = symbol.ExportSymbol;
        return getMergedSymbol(symbol);
    }
    public Node GetReferencedExportContainer(Node node, bool prefixLocals)
    {
        bool startInDeclarationContainer = node.Parent != null && (node.Parent.Kind == Kind.ModuleDeclaration || node.Parent.Kind == Kind.EnumDeclaration) && node == node.Parent.Name();
        var symbol = getReferencedValueSymbol(node, startInDeclarationContainer);
        if (symbol != null)
        {
            if ((symbol.Flags & SymbolFlags.ExportValue) != 0)
            {
                var exportSymbol = getMergedSymbol(symbol.ExportSymbol);
                if (!prefixLocals && (exportSymbol.Flags & SymbolFlags.ExportHasLocal) != 0 && (exportSymbol.Flags & SymbolFlags.Variable) == 0) return null;
                symbol = exportSymbol;
            }
            var parentSymbol = getParentOfSymbol(symbol);
            if (parentSymbol != null)
            {
                if ((parentSymbol.Flags & SymbolFlags.ValueModule) != 0 && parentSymbol.ValueDeclaration != null && parentSymbol.ValueDeclaration.Kind == Kind.SourceFile)
                {
                    var symbolFile = parentSymbol.ValueDeclaration.AsSourceFile(); var referenceFile = Ast.GetSourceFileOfNode(node);
                    if (symbolFile != referenceFile) return null;
                    return symbolFile.AsNode();
                }
                return Ast.FindAncestor(node.Parent, (Resolver: this, ParentSymbol: parentSymbol), static (n, state) => (n.Kind == Kind.ModuleDeclaration || n.Kind == Kind.EnumDeclaration) && state.Resolver.getSymbolOfDeclaration(n) == state.ParentSymbol);
            }
        }
        return null;
    }
    public Node GetReferencedImportDeclaration(Node node)
    {
        var symbol = getReferencedValueSymbol(node, false);
        return symbol != null && Ast.IsNonLocalAlias(symbol, SymbolFlags.Value) && !isTypeOnlyAliasDeclaration(symbol) ? getDeclarationOfAliasSymbol(symbol) : null;
    }
    public Node GetReferencedValueDeclaration(Node node)
    { var symbol = getReferencedValueSymbol(node, false); return symbol != null ? getExportSymbolOfValueSymbolIfExported(symbol).ValueDeclaration : null; }
    public Slice<Node> GetReferencedValueDeclarations(Node node)
    {
        Slice<Node> declarations = default; var symbol = getReferencedValueSymbol(node, false);
        if (symbol != null)
        {
            symbol = getExportSymbolOfValueSymbolIfExported(symbol);
            foreach (var declaration in symbol.Declarations)
                switch (declaration.Kind)
                {
                    case Kind.VariableDeclaration: case Kind.Parameter: case Kind.BindingElement: case Kind.PropertyDeclaration:
                    case Kind.PropertyAssignment: case Kind.ShorthandPropertyAssignment: case Kind.EnumMember: case Kind.ObjectLiteralExpression:
                    case Kind.FunctionDeclaration: case Kind.FunctionExpression: case Kind.ArrowFunction: case Kind.ClassDeclaration:
                    case Kind.ClassExpression: case Kind.EnumDeclaration: case Kind.MethodDeclaration: case Kind.GetAccessor:
                    case Kind.SetAccessor: case Kind.ModuleDeclaration: declarations = declarations.Append(declaration); break;
                }
        }
        return declarations;
    }
    public string GetElementAccessExpressionName(ElementAccessExpression expression)
    {
        if (expression != null && hooks.GetElementAccessExpressionName != null)
        { var (name, ok) = hooks.GetElementAccessExpressionName(expression); if (ok) return name.Project(); }
        return "";
    }
    public Node GetReferencedMemberValueDeclaration(Node node)
    {
        var symbol = getResolvedSymbol(node);
        if (symbol == null && node.Symbol() != null) symbol = getMergedSymbol(node.Symbol());
        return symbol != null ? getExportSymbolOfValueSymbolIfExported(symbol).ValueDeclaration : null;
    }
}

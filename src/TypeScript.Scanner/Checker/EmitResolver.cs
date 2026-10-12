// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/checker/emitresolver.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using NodeBuilderFlags = TypeScript.NodeBuilding.Flags;
using NodeBuilderInternalFlags = TypeScript.NodeBuilding.InternalFlags;
using NodeBuilderSymbolTracker = TypeScript.NodeBuilding.SymbolTracker;
namespace TypeScript.Checking;
internal struct JSXLinks { internal Node importRef; }
internal struct DeclarationLinks { internal Tristate isVisible; }
internal struct DeclarationFileLinks { internal bool aliasesMarked; }
internal sealed class EmitResolverLinks
{
    internal readonly LinkStore<Node, JSXLinks> jsxLinks = new();
    internal readonly LinkStore<Node, DeclarationLinks> declarationLinks = new();
    internal readonly LinkStore<Node, DeclarationFileLinks> declarationFileLinks = new();
    internal ref DeclarationLinks Get(Node node) => ref declarationLinks.Get(node);
}
public sealed partial class Checker
{
    private static bool isConstEnumOrConstEnumOnlyModule(Symbol s) => isConstEnumSymbol(s) || (s.Flags & SymbolFlags.ConstEnumOnlyModule) != 0;
    internal IEmitResolver NewEmitResolver(TypeScript.DiagnosticPrinting.EmitContext context) => new EmitResolver(this, context);
    // Go's package-level private access becomes ordinary nested-type access.
    internal sealed class EmitResolver : IEmitResolver
    {
        private readonly Checker checker;
        private readonly SemaphoreSlim checkerMu;
        private readonly TypeScript.DiagnosticPrinting.EmitContext emitContext;
        private NodeBuilder requestNodeBuilder;
        private readonly Func<Node, bool> isValueAliasDeclaration;
        private readonly Visitor aliasMarkingVisitor;
        private IReferenceResolver referenceResolver;
        internal EmitResolver(Checker checker, TypeScript.DiagnosticPrinting.EmitContext context)
        {
            if (context == null) throw new InvalidOperationException("EmitResolver requires an EmitContext");
            this.checker = checker; emitContext = context; checkerMu = checker.mu;
            isValueAliasDeclaration = isValueAliasDeclarationWorker; aliasMarkingVisitor = aliasMarkingVisitorWorker;
        }
// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:61:1.
public EmitContext EmitContext()
{
    return this.emitContext;
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:65:1.
private NodeBuilder nodeBuilder()
{
    if ((this.requestNodeBuilder == null))
    {
        this.requestNodeBuilder = NewNodeBuilder(this.checker, this.emitContext);
    }
    return this.requestNodeBuilder;
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:72:1.
public Node GetJsxFactoryEntity(Node location)
{
    this.checkerMu.Wait();
    try
    {
        return this.checker.getJsxFactoryEntity(location);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:78:1.
public Node GetJsxFragmentFactoryEntity(Node location)
{
    this.checkerMu.Wait();
    try
    {
        return this.checker.getJsxFragmentFactoryEntity(location);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:84:1.
public bool IsOptionalParameter(Node node)
{
    this.checkerMu.Wait();
    try
    {
        return this.isOptionalParameter(node);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:90:1.
public bool IsLateBound(Node node)
{
    if ((node == null))
    {
        return false;
    }
    if (!Ast.IsParseTreeNode(node))
    {
        return false;
    }
    this.checkerMu.Wait();
    try
    {
        var symbol = this.checker.getSymbolOfDeclaration(node);
        if ((symbol == null))
        {
            return false;
        }
        return ((symbol.CheckFlags & CheckFlags.Late) != 0);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:108:1.
public Evaluator.Result GetEnumMemberValue(Node node)
{
    if (!Ast.IsParseTreeNode(node))
    {
        return Evaluator.NewResult(null, false, false, false);
    }
    this.checkerMu.Wait();
    try
    {
        this.checker.computeEnumMemberValues(node.Parent);
        if (!this.checker.enumMemberLinks.Has(node))
        {
            return Evaluator.NewResult(null, false, false, false);
        }
        return this.checker.enumMemberLinks.Get(node).value;
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:123:1.
public bool IsDeclarationVisible(Node node)
{
    this.checkerMu.Wait();
    try
    {
        return this.checker.isDeclarationVisible(node);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:130:1.
public void PrecalculateDeclarationEmitVisibility(SourceFile file)
{
    this.checkerMu.Wait();
    try
    {
        if (this.checker.emitResolverLinks.declarationFileLinks.Get(file.AsNode()).aliasesMarked)
        {
            return ;
        }
        this.checker.emitResolverLinks.declarationFileLinks.Get(file.AsNode()).aliasesMarked = true;
        file.AsNode().ForEachChild(this.aliasMarkingVisitor);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:143:1.
private static bool isCommonJSModuleExports(Node node)
{
    if ((((Ast.IsBinaryExpression(node) && Ast.IsExpressionStatement(node.Parent)) && Ast.IsSourceFile(node.Parent.Parent)) && (node.Parent.Parent.AsSourceFile().CommonJSModuleIndicator != null)))
    {
        switch (Ast.GetAssignmentDeclarationKind(node))
        {
            case JSDeclarationKind.ModuleExports:
            case JSDeclarationKind.ExportsProperty:
            {
                return true;
            }
        }
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:154:1.
private bool aliasMarkingVisitorWorker(Node node)
{
    switch (node.Kind)
    {
        case Kind.BinaryExpression:
        {
            if ((isCommonJSModuleExports(node) && Ast.IsIdentifier(node.AsBinaryExpression().Right)))
            {
                this.markLinkedAliases(node.AsBinaryExpression().Right);
            }
            break;
        }
        case Kind.ExportAssignment:
        {
            if ((node.Expression().Kind == Kind.Identifier))
            {
                this.markLinkedAliases(node.Expression());
            }
            break;
        }
        case Kind.ExportSpecifier:
        {
            this.markLinkedAliases(node.PropertyNameOrName());
            break;
        }
    }
    return node.ForEachChild(this.aliasMarkingVisitor);
}

// Sets the isVisible link on statements the Identifier or ExportName node points at
// Follows chains of import d = a.b.c
// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:172:1.
private void markLinkedAliases(Node node)
{
    Symbol exportSymbol = default;
    if ((((node.Kind != Kind.StringLiteral) && (node.Parent != null)) && ((Ast.IsExportAssignment(node.Parent) || isCommonJSModuleExports(node.Parent)))))
    {
        exportSymbol = this.checker.resolveName(node, node.TextValue(), (((SymbolFlags.Value | SymbolFlags.Type) | SymbolFlags.Namespace) | SymbolFlags.Alias), null, false, false);
    }
    else
    {
        if ((node.Parent.Kind == Kind.ExportSpecifier))
        {
            exportSymbol = this.checker.getTargetOfExportSpecifier(node.Parent, (((SymbolFlags.Value | SymbolFlags.Type) | SymbolFlags.Namespace) | SymbolFlags.Alias), false);
        }
    }
    var visited = new Dictionary<ulong, bool>(2);
    while ((exportSymbol != null))
    {
        var seen = visited.TryGetValue(Ast.GetSymbolId(exportSymbol), out var _);
        if (seen)
        {
            break;
        }
        visited[Ast.GetSymbolId(exportSymbol)] = true;
        Symbol nextSymbol = default;
        {
            foreach (var range1 in GoRuntime.Range(exportSymbol.Declarations))
            {
                var _2 = range1.Key;
                var declaration = range1.Value;
                this.checker.emitResolverLinks.declarationLinks.Get(declaration).isVisible = Tristate.True;
                if (Ast.IsInternalModuleImportEqualsDeclaration(declaration))
                {
                    var internalModuleReference = declaration.AsImportEqualsDeclaration().ModuleReference;
                    var firstIdentifier = Ast.GetFirstIdentifier(internalModuleReference);
                    var importSymbol = this.checker.resolveName(declaration, firstIdentifier.TextValue(), (((SymbolFlags.Value | SymbolFlags.Type) | SymbolFlags.Namespace) | SymbolFlags.Alias), null, false, false);
                    nextSymbol = importSymbol;
                }
            }
        }
        exportSymbol = nextSymbol;
    }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:205:1.
public SymbolAccessibilityResult IsEntityNameVisible(Node entityName, Node enclosingDeclaration)
{
    this.checkerMu.Wait();
    try
    {
        return this.checker.isEntityNameVisible(entityName, enclosingDeclaration, true);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:211:1.
public bool IsImplementationOfOverload(Node node)
{
    if (!Ast.IsParseTreeNode(node))
    {
        return false;
    }
    if (Ast.NodeIsPresent(node.Body()))
    {
        if ((Ast.IsGetAccessorDeclaration(node) || Ast.IsSetAccessorDeclaration(node)))
        {
            return false;
        }
        this.checkerMu.Wait();
        try
        {
            var symbol = this.checker.getSymbolOfDeclaration(node);
            var signaturesOfSymbol = this.checker.getSignaturesOfSymbol(symbol);
            if ((GoRuntime.Len(signaturesOfSymbol) > 1))
            {
                return true;
            }
            if ((GoRuntime.Len(signaturesOfSymbol) == 1))
            {
                var signature = signaturesOfSymbol[0];
                if ((signature == this.checker.getSignatureOfFullSignatureType(node)))
                {
                    return false;
                }
                var declaration = signature.declaration;
                if (((declaration != node) && ((declaration.Flags & NodeFlags.JSDoc) == 0)))
                {
                    return true;
                }
            }
        }
        finally { this.checkerMu.Release(); }
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:252:1.
public bool IsImportRequiredByAugmentation(ImportDeclaration decl)
{
    if (!Ast.IsParseTreeNode(decl.AsNode()))
    {
        return false;
    }
    var file = Ast.GetSourceFileOfNode(decl.AsNode());
    if ((file.Symbol == null))
    {
        return false;
    }
    var importTarget = this.GetExternalModuleFileFromDeclaration(decl.AsNode());
    if ((importTarget == null))
    {
        return false;
    }
    if ((importTarget == file))
    {
        return false;
    }
    this.checkerMu.Wait();
    try
    {
        var exports = this.checker.getExportsOfModule(file.Symbol);
        {
            foreach (var s in GoRuntime.RangeKeys((exports?.Values ?? Enumerable.Empty<Symbol>())))
            {
                var merged = this.checker.getMergedSymbol(s);
                if ((merged != s))
                {
                    if ((GoRuntime.Len(merged.Declarations) > 0))
                    {
                        {
                            foreach (var d in GoRuntime.RangeValues(merged.Declarations))
                            {
                                var declFile = Ast.GetSourceFileOfNode(d);
                                if ((declFile == importTarget))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        return false;
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:288:1.
public bool IsDefinitelyReferenceToGlobalSymbolObject(Node node)
{
    if (((!Ast.IsPropertyAccessExpression(node) || !Ast.IsIdentifier(node.Name())) || (!Ast.IsPropertyAccessExpression(node.Expression()) && !Ast.IsIdentifier(node.Expression()))))
    {
        return false;
    }
    if ((node.Expression().Kind == Kind.Identifier))
    {
        if ((!node.Expression().TextEquals("Symbol"u8)))
        {
            return false;
        }
        this.checkerMu.Wait();
        try
        {
            return (this.checker.getResolvedSymbol(node.Expression()) == this.checker.getGlobalSymbol("Symbol", (SymbolFlags.Value | SymbolFlags.ExportValue), null));
        }
        finally { this.checkerMu.Release(); }
    }
    if ((((node.Expression().Expression().Kind != Kind.Identifier) || (!node.Expression().Expression().TextEquals("globalThis"u8))) || (!node.Expression().Name().TextEquals("Symbol"u8))))
    {
        return false;
    }
    this.checkerMu.Wait();
    try
    {
        return (this.checker.getResolvedSymbol(node.Expression().Expression()) == this.checker.globalThisSymbol);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:312:1.
public bool RequiresAddingImplicitUndefined(Node declaration, Symbol symbol, Node enclosingDeclaration)
{
    if (!Ast.IsParseTreeNode(declaration))
    {
        return false;
    }
    this.checkerMu.Wait();
    try
    {
        return this.checker.requiresAddingImplicitUndefined(declaration, symbol, enclosingDeclaration);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:321:1.
public bool RequiresAddingImplicitUndefinedUnsafe(Node declaration, Symbol symbol, Node enclosingDeclaration)
{
    if (!Ast.IsParseTreeNode(declaration))
    {
        return false;
    }
    return this.checker.requiresAddingImplicitUndefined(declaration, symbol, enclosingDeclaration);
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:329:1.
private bool isOptionalParameter(Node node)
{
    return this.checker.isOptionalParameter(node);
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:333:1.
public bool IsLiteralConstDeclaration(Node node)
{
    if (!Ast.IsParseTreeNode(node))
    {
        return false;
    }
    if ((isDeclarationReadonly(node) || (Ast.IsVariableDeclaration(node) && Ast.IsVarConst(node))))
    {
        this.checkerMu.Wait();
        try
        {
            var s = this.checker.getSymbolOfDeclaration(node);
            if ((s == null))
            {
                return false;
            }
            return isFreshLiteralType(this.checker.getTypeOfSymbol(s));
        }
        finally { this.checkerMu.Release(); }
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:350:1.
public bool IsExpandoFunctionDeclarationUnsafe(Node node)
{
    if (!Ast.IsParseTreeNode(node))
    {
        return false;
    }
    var props = this.GetPropertiesOfContainerFunction(node);
    {
        foreach (var p in GoRuntime.RangeValues(props))
        {
            if (Ast.IsExpandoPropertyDeclaration(p.ValueDeclaration))
            {
                return true;
            }
        }
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:365:1.
public bool IsExpandoFunctionDeclaration(Node node)
{
    this.checkerMu.Wait();
    try
    {
        return this.IsExpandoFunctionDeclarationUnsafe(node);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:371:1.
private SymbolAccessibilityResult isSymbolAccessible(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning, bool shouldComputeAliasToMarkVisible)
{
    return this.checker.IsSymbolAccessible(symbol, enclosingDeclaration, meaning, shouldComputeAliasToMarkVisible);
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:375:1.
public SymbolAccessibilityResult IsSymbolAccessible(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning, bool shouldComputeAliasToMarkVisible)
{
    return this.isSymbolAccessible(symbol, enclosingDeclaration, meaning, shouldComputeAliasToMarkVisible);
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:387:1.
public bool IsReferencedAliasDeclaration(Node node)
{
    var c = this.checker;
    if ((!c.canCollectSymbolAliasAccessibilityData || !Ast.IsParseTreeNode(node)))
    {
        return true;
    }
    this.checkerMu.Wait();
    try
    {
        if (Ast.IsAliasSymbolDeclaration(node))
        {
            {
                var symbol = c.getSymbolOfDeclaration(node);
                if ((symbol != null))
                {
                    var aliasLinks = c.aliasSymbolLinks.Get(symbol);
                    if (aliasLinks.referenced)
                    {
                        return true;
                    }
                    var target = aliasLinks.aliasTarget;
                    if (((((target != null) && ((node.ModifierFlags() & ModifierFlags.Export) != 0)) && ((c.getSymbolFlags(target) & SymbolFlags.Value) != 0)) && ((c.compilerOptions.ShouldPreserveConstEnums() || !isConstEnumOrConstEnumOnlyModule(target)))))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:413:1.
public bool IsValueAliasDeclaration(Node node)
{
    var c = this.checker;
    if ((!c.canCollectSymbolAliasAccessibilityData || !Ast.IsParseTreeNode(node)))
    {
        return true;
    }
    this.checkerMu.Wait();
    try
    {
        return this.isValueAliasDeclarationWorker(node);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:425:1.
private bool isValueAliasDeclarationWorker(Node node)
{
    var c = this.checker;
    switch (node.Kind)
    {
        case Kind.ImportEqualsDeclaration:
        {
            return this.isAliasResolvedToValue(c.getSymbolOfDeclaration(node), false);
        }
        case Kind.ImportClause:
        case Kind.NamespaceImport:
        case Kind.ImportSpecifier:
        case Kind.ExportSpecifier:
        {
            var symbol = c.getSymbolOfDeclaration(node);
            return ((symbol != null) && this.isAliasResolvedToValue(symbol, true));
        }
        case Kind.ExportDeclaration:
        {
            var exportClause = node.AsExportDeclaration().ExportClause;
            return ((exportClause != null) && ((Ast.IsNamespaceExport(exportClause) || Core.Some(exportClause.Elements(), this.isValueAliasDeclaration))));
        }
        case Kind.ExportAssignment:
        {
            if (((node.Expression() != null) && (node.Expression().Kind == Kind.Identifier)))
            {
                return this.isAliasResolvedToValue(c.getSymbolOfDeclaration(node), true);
            }
            return true;
        }
        case Kind.BinaryExpression:
        {
            if ((isCommonJSModuleExports(node) && Ast.IsIdentifier(node.AsBinaryExpression().Right)))
            {
                return this.isAliasResolvedToValue(c.getSymbolOfDeclaration(node), true);
            }
            break;
        }
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:454:1.
private bool isAliasResolvedToValue(Symbol symbol, bool excludeTypeOnlyValues)
{
    var c = this.checker;
    if ((symbol == null))
    {
        return false;
    }
    if ((symbol.ValueDeclaration != null))
    {
        {
            var container = Ast.GetSourceFileOfNode(symbol.ValueDeclaration);
            if ((container != null))
            {
                var fileSymbol = c.getSymbolOfDeclaration(container.AsNode());
                c.resolveExternalModuleSymbol(fileSymbol, false);
            }
        }
    }
    var target = c.getExportSymbolOfValueSymbolIfExported(c.resolveAlias(symbol));
    if ((target == c.unknownSymbol))
    {
        return (!excludeTypeOnlyValues || (c.getTypeOnlyAliasDeclaration(symbol) == null));
    }
    return (((c.getSymbolFlagsEx(symbol, excludeTypeOnlyValues, true) & SymbolFlags.Value) != 0) && ((c.compilerOptions.ShouldPreserveConstEnums() || !isConstEnumOrConstEnumOnlyModule(target))));
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:479:1.
public bool IsTopLevelValueImportEqualsWithEntityName(Node node)
{
    var c = this.checker;
    if (!c.canCollectSymbolAliasAccessibilityData)
    {
        return true;
    }
    if (((!Ast.IsParseTreeNode(node) || (node.Kind != Kind.ImportEqualsDeclaration)) || (node.Parent.Kind != Kind.SourceFile)))
    {
        return false;
    }
    if ((Ast.IsImportEqualsDeclaration(node) && ((Ast.NodeIsMissing(node.AsImportEqualsDeclaration().ModuleReference) || (node.AsImportEqualsDeclaration().ModuleReference.Kind == Kind.ExternalModuleReference)))))
    {
        return false;
    }
    this.checkerMu.Wait();
    try
    {
        return this.isAliasResolvedToValue(c.getSymbolOfDeclaration(node), false);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:498:1.
public void MarkLinkedReferencesRecursively(SourceFile file)
{
    if (!Ast.IsParseTreeNode(file.AsNode()))
    {
        return ;
    }
    this.checkerMu.Wait();
    try
    {
        if ((file != null))
        {
            Visitor visit = default;
            visit = (n) =>
{
    if ((Ast.IsImportEqualsDeclaration(n) && ((n.ModifierFlags() & ModifierFlags.Export) == 0)))
    {
        return false;
    }
    if (Ast.IsImportDeclaration(n))
    {
        return false;
    }
    this.checker.markLinkedReferences(n, ReferenceHint.Unspecified, null, null);
    n.ForEachChild(visit);
    return false;
};
            file.ForEachChild(visit);
        }
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:522:1.
public SourceFile GetExternalModuleFileFromDeclaration(Node declaration)
{
    if (!Ast.IsParseTreeNode(declaration))
    {
        return null;
    }
    this.checkerMu.Wait();
    try
    {
        return this.checker.getExternalModuleFileFromDeclaration(declaration);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:532:1.
private IReferenceResolver getReferenceResolver()
{
    if ((this.referenceResolver == null))
    {
        this.referenceResolver = new ReferenceResolver(this.checker.compilerOptions, new ReferenceResolverHooks { ResolveName = this.checker.resolveName, GetResolvedSymbol = this.checker.getResolvedSymbolOrNil, GetMergedSymbol = this.checker.getMergedSymbol, GetParentOfSymbol = this.checker.getParentOfSymbol, GetSymbolOfDeclaration = this.checker.getSymbolOfDeclaration, GetTypeOnlyAliasDeclaration = this.checker.getTypeOnlyAliasDeclarationEx, GetExportSymbolOfValueSymbolIfExported = this.checker.getExportSymbolOfValueSymbolIfExported, GetElementAccessExpressionName = this.checker.tryGetElementAccessExpressionName });
    }
    return this.referenceResolver;
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:548:1.
public Node GetReferencedExportContainer(Node node, bool prefixLocals)
{
    if (!Ast.IsParseTreeNode(node))
    {
        return null;
    }
    this.checkerMu.Wait();
    try
    {
        return this.getReferenceResolver().GetReferencedExportContainer(node, prefixLocals);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:559:1.
public void SetReferencedImportDeclaration(Node node, Node @ref)
{
    this.checkerMu.Wait();
    try
    {
        this.checker.emitResolverLinks.jsxLinks.Get(node).importRef = @ref;
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:565:1.
public Node GetReferencedImportDeclaration(Node node)
{
    this.checkerMu.Wait();
    try
    {
        if (!Ast.IsParseTreeNode(node))
        {
            return this.checker.emitResolverLinks.jsxLinks.Get(node).importRef;
        }
        var symbol = this.checker.getReferencedValueOrAliasSymbol(node);
        if ((Ast.IsNonLocalAlias(symbol, SymbolFlags.Value) && (this.checker.getTypeOnlyAliasDeclarationEx(symbol, SymbolFlags.Value) == null)))
        {
            return this.checker.getDeclarationOfAliasSymbol(symbol);
        }
        return null;
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:579:1.
public Node GetReferencedValueDeclaration(Node node)
{
    if (!Ast.IsParseTreeNode(node))
    {
        return null;
    }
    this.checkerMu.Wait();
    try
    {
        return this.getReferenceResolver().GetReferencedValueDeclaration(node);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:590:1.
public Node GetReferencedValueDeclarationUnsafe(Node node)
{
    return this.getReferenceResolver().GetReferencedValueDeclaration(node);
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:594:1.
public Slice<Node> GetReferencedValueDeclarations(Node node)
{
    if (!Ast.IsParseTreeNode(node))
    {
        return default;
    }
    this.checkerMu.Wait();
    try
    {
        return this.getReferenceResolver().GetReferencedValueDeclarations(node);
    }
    finally { this.checkerMu.Release(); }
}

// IsNameResolvable returns `true` if the given `name` resolves to any symbol at `location`
// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:606:1.
public bool IsNameResolvable(Node location, string name) => IsNameResolvable(location, ByteStringValue.FromString(name));
// emitresolver.go:606: same lookup/lock with a borrowed Go string header.
public bool IsNameResolvable(Node location, ByteStringValue name)
{
    this.checkerMu.Wait();
    try
    {
        var symbol = this.checker.resolveName(location, name, ((SymbolFlags.Value | SymbolFlags.Type) | SymbolFlags.Namespace), null, false, false);
        return (symbol != null);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:614:1.
public string GetElementAccessExpressionName(ElementAccessExpression expression) => GetElementAccessExpressionNameValue(expression).Materialize();
// emitresolver.go:614: retained byte-header result; exact parse-tree guard and lock.
public ByteStringValue GetElementAccessExpressionNameValue(ElementAccessExpression expression)
{
    if (!Ast.IsParseTreeNode(expression.AsNode()))
    {
        return ByteStringValue.FromString("");
    }
    this.checkerMu.Wait();
    try
    {
        return this.getReferenceResolver().GetElementAccessExpressionNameValue(expression);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:625:1.
public Node GetReferencedMemberValueDeclaration(Node node)
{
    if (!Ast.IsParseTreeNode(node))
    {
        return null;
    }
    this.checkerMu.Wait();
    try
    {
        return this.getReferenceResolver().GetReferencedMemberValueDeclaration(node);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:640:1.
public Node CreateReturnTypeOfSignatureDeclaration(Node signatureDeclaration, Node enclosingDeclaration, NodeBuilderFlags flags, NodeBuilderInternalFlags internalFlags, NodeBuilderSymbolTracker tracker)
{
    var original = this.emitContext.ParseNode(signatureDeclaration);
    if ((original == null))
    {
        return this.emitContext.Factory.NewKeywordTypeNode(Kind.AnyKeyword);
    }
    this.checkerMu.Wait();
    try
    {
        return this.nodeBuilder().SerializeReturnTypeForSignature(original, enclosingDeclaration, flags, internalFlags, tracker);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:651:1.
public Slice<Node> CreateTypeParametersOfSignatureDeclaration(Node signatureDeclaration, Node enclosingDeclaration, NodeBuilderFlags flags, NodeBuilderInternalFlags internalFlags, NodeBuilderSymbolTracker tracker)
{
    var original = this.emitContext.ParseNode(signatureDeclaration);
    if ((original == null))
    {
        return default;
    }
    this.checkerMu.Wait();
    try
    {
        return this.nodeBuilder().SerializeTypeParametersForSignature(original, enclosingDeclaration, flags, internalFlags, tracker);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:662:1.
public Node CreateTypeOfDeclaration(Node declaration, Node enclosingDeclaration, NodeBuilderFlags flags, NodeBuilderInternalFlags internalFlags, NodeBuilderSymbolTracker tracker)
{
    var original = this.emitContext.ParseNode(declaration);
    if ((original == null))
    {
        return this.emitContext.Factory.NewKeywordTypeNode(Kind.AnyKeyword);
    }
    this.checkerMu.Wait();
    try
    {
        var symbol = this.checker.getSymbolOfDeclaration(declaration);
        return this.nodeBuilder().SerializeTypeForDeclaration(declaration, symbol, enclosingDeclaration, (flags | NodeBuilderFlags.MultilineObjectLiterals), internalFlags, tracker);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:735:1.
public Node CreateTypeOfExpression(Node expression, Node enclosingDeclaration, NodeBuilderFlags flags, NodeBuilderInternalFlags internalFlags, NodeBuilderSymbolTracker tracker)
{
    expression = this.emitContext.ParseNode(expression);
    if ((expression == null))
    {
        return this.emitContext.Factory.NewKeywordTypeNode(Kind.AnyKeyword);
    }
    this.checkerMu.Wait();
    try
    {
        return this.nodeBuilder().SerializeTypeForExpression(expression, enclosingDeclaration, (flags | NodeBuilderFlags.MultilineObjectLiterals), internalFlags, tracker);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:746:1.
public Slice<Node> CreateLateBoundIndexSignatures(Node container, Node enclosingDeclaration, NodeBuilderFlags flags, NodeBuilderInternalFlags internalFlags, NodeBuilderSymbolTracker tracker)
{
    container = this.emitContext.ParseNode(container);
    this.checkerMu.Wait();
    try
    {
        var sym = container.Symbol();
        var staticInfos = this.checker.getIndexInfosOfType(this.checker.getTypeOfSymbol(sym));
        var instanceIndexSymbol = this.checker.getIndexSymbol(sym);
        Slice<IndexInfo> instanceInfos = default;
        if ((instanceIndexSymbol != null))
        {
            var siblingSymbols = collectSymbolValues(this.checker.getMembersOfSymbol(sym));
            instanceInfos = this.checker.getIndexInfosOfIndexSymbol(instanceIndexSymbol, siblingSymbols);
        }
        var requestNodeBuilder = this.nodeBuilder();
        Slice<Node> result = default;
        {
            foreach (var range5 in GoRuntime.Range(Slice<Slice<IndexInfo>>.FromArray([staticInfos, instanceInfos])))
            {
                var i = range5.Key;
                var infoList = range5.Value;
                var isStatic = true;
                if ((i > 0))
                {
                    isStatic = false;
                }
                if ((GoRuntime.Len(infoList) == 0))
                {
                    continue;
                }
                {
                    foreach (var info in GoRuntime.RangeValues(infoList))
                    {
                        if ((info.declaration != null))
                        {
                            continue;
                        }
                        if ((info == this.checker.anyBaseTypeIndexInfo))
                        {
                            continue;
                        }
                        if ((GoRuntime.Len(info.components) != 0))
                        {
                            var allComponentComputedNamesSerializable = ((enclosingDeclaration != null) && Core.Every(info.components, (c) =>
{
    return ((((c.Name() != null) && Ast.IsComputedPropertyName(c.Name())) && Ast.IsEntityNameExpression(c.Name().Expression())) && (this.checker.isEntityNameVisible(c.Name().Expression(), enclosingDeclaration, false).Accessibility == SymbolAccessibility.Accessible));
}));
                            if (allComponentComputedNamesSerializable)
                            {
                                {
                                    foreach (var range7 in GoRuntime.Range(info.components))
                                    {
                                                                                var c2 = range7.Value;
                                        if (this.checker.hasLateBindableName(c2))
                                        {
                                            continue;
                                        }
                                        var firstIdentifier = Ast.GetFirstIdentifier(c2.Name().Expression());
                                        var name = this.checker.resolveName(firstIdentifier, firstIdentifier.TextValue(), (SymbolFlags.Value | SymbolFlags.ExportValue), null, true, false);
                                        if ((name != null))
                                        {
                                            tracker.TrackSymbol(name, enclosingDeclaration, SymbolFlags.Value);
                                        }
                                        var mods = Core.IfElse(isStatic, Slice<Node>.FromArray([this.emitContext.Factory.NewModifier(Kind.StaticKeyword)]), default(Slice<Node>));
                                        if (info.isReadonly)
                                        {
                                            mods = mods.Append(this.emitContext.Factory.NewModifier(Kind.ReadonlyKeyword));
                                        }
                                        var decl = this.emitContext.Factory.NewPropertyDeclaration(Core.IfElse((!mods.IsNil), this.emitContext.Factory.NewModifierList(mods), null), c2.Name(), c2.QuestionToken(), requestNodeBuilder.TypeToTypeNode(this.checker.getTypeOfSymbol(c2.Symbol()), enclosingDeclaration, flags, internalFlags, tracker), null);
                                        result = result.Append(decl);
                                    }
                                }
                                continue;
                            }
                        }
                        var node = requestNodeBuilder.IndexInfoToIndexSignatureDeclaration(info, enclosingDeclaration, flags, internalFlags, tracker);
                        if (((node != null) && isStatic))
                        {
                            var modNodes = Slice<Node>.FromArray([this.emitContext.Factory.NewModifier(Kind.StaticKeyword)]);
                            modNodes = modNodes.Append(node.ModifierNodes());
                            var mods2 = this.emitContext.Factory.NewModifierList(modNodes);
                            node = this.emitContext.Factory.UpdateIndexSignatureDeclaration(node.AsIndexSignatureDeclaration(), mods2, node.ParameterList(), node.Type());
                        }
                        if ((node != null))
                        {
                            result = result.Append(node);
                        }
                    }
                }
            }
        }
        return result;
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:836:1.
public ModifierFlags GetEffectiveDeclarationFlags(Node node, ModifierFlags flags)
{
    this.checkerMu.Wait();
    try
    {
        return this.checker.GetEffectiveDeclarationFlags(node, flags);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:843:1.
public object GetConstantValue(Node node)
{
    this.checkerMu.Wait();
    try
    {
        return this.checker.GetConstantValue(node);
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:850:1.
public TypeReferenceSerializationKind GetTypeReferenceSerializationKind(Node typeName, Node location)
{
    this.checkerMu.Wait();
    try
    {
        if (((typeName == null) || (location == null)))
        {
            return TypeReferenceSerializationKind.Unknown;
        }
        var isTypeOnly = false;
        if (Ast.IsQualifiedName(typeName))
        {
            var rootValueSymbol = this.checker.resolveEntityName(Ast.GetFirstIdentifier(typeName), SymbolFlags.Value, true, true, location);
            if (((rootValueSymbol != null) && (GoRuntime.Len(rootValueSymbol.Declarations) > 0)))
            {
                isTypeOnly = Core.Every(rootValueSymbol.Declarations, Ast.IsTypeOnlyImportOrExportDeclaration);
            }
        }
        var valueSymbol = this.checker.resolveEntityName(typeName, SymbolFlags.Value, true, true, location);
        var resolvedValueSymbol = valueSymbol;
        if (((valueSymbol != null) && ((valueSymbol.Flags & SymbolFlags.Alias) != 0)))
        {
            resolvedValueSymbol = this.checker.resolveAlias(valueSymbol);
        }
        isTypeOnly = (isTypeOnly || (((valueSymbol != null) && (this.checker.getTypeOnlyAliasDeclarationEx(valueSymbol, SymbolFlags.Value) != null))));
        var typeSymbol = this.checker.resolveEntityName(typeName, SymbolFlags.Type, true, true, location);
        var resolvedTypeSymbol = typeSymbol;
        if (((typeSymbol != null) && ((typeSymbol.Flags & SymbolFlags.Alias) != 0)))
        {
            resolvedTypeSymbol = this.checker.resolveAlias(typeSymbol);
        }
        isTypeOnly = (isTypeOnly || (((typeSymbol != null) && (this.checker.getTypeOnlyAliasDeclarationEx(typeSymbol, SymbolFlags.Type) != null))));
        if (((resolvedValueSymbol != null) && (resolvedValueSymbol == resolvedTypeSymbol)))
        {
            var globalPromiseSymbol = this.checker.getGlobalPromiseConstructorSymbol();
            if (((globalPromiseSymbol != null) && (resolvedValueSymbol == globalPromiseSymbol)))
            {
                return TypeReferenceSerializationKind.Promise;
            }
            var constructorType = this.checker.getTypeOfSymbol(resolvedValueSymbol);
            if (((constructorType != null) && this.checker.isConstructorType(constructorType)))
            {
                if (isTypeOnly)
                {
                    return TypeReferenceSerializationKind.TypeWithCallSignature;
                }
                return TypeReferenceSerializationKind.TypeWithConstructSignatureAndValue;
            }
        }
        if ((resolvedTypeSymbol == null))
        {
            if (isTypeOnly)
            {
                return TypeReferenceSerializationKind.ObjectType;
            }
            return TypeReferenceSerializationKind.Unknown;
        }
        var type_ = this.checker.getDeclaredTypeOfSymbol(resolvedTypeSymbol);
        if (this.checker.isErrorType(type_))
        {
            if (isTypeOnly)
            {
                return TypeReferenceSerializationKind.ObjectType;
            }
            return TypeReferenceSerializationKind.Unknown;
        }
        if (((type_.flags & TypeFlags.AnyOrUnknown) != 0))
        {
            return TypeReferenceSerializationKind.ObjectType;
        }
        else
        {
            if (this.checker.isTypeAssignableToKind(type_, ((TypeFlags.Void | TypeFlags.Nullable) | TypeFlags.Never)))
            {
                return TypeReferenceSerializationKind.VoidNullableOrNeverType;
            }
            else
            {
                if (this.checker.isTypeAssignableToKind(type_, TypeFlags.BooleanLike))
                {
                    return TypeReferenceSerializationKind.BooleanType;
                }
                else
                {
                    if (this.checker.isTypeAssignableToKind(type_, TypeFlags.NumberLike))
                    {
                        return TypeReferenceSerializationKind.NumberLikeType;
                    }
                    else
                    {
                        if (this.checker.isTypeAssignableToKind(type_, TypeFlags.BigIntLike))
                        {
                            return TypeReferenceSerializationKind.BigIntLikeType;
                        }
                        else
                        {
                            if (this.checker.isTypeAssignableToKind(type_, TypeFlags.StringLike))
                            {
                                return TypeReferenceSerializationKind.StringLikeType;
                            }
                            else
                            {
                                if (isTupleType(type_))
                                {
                                    return TypeReferenceSerializationKind.ArrayLikeType;
                                }
                                else
                                {
                                    if (this.checker.isTypeAssignableToKind(type_, TypeFlags.ESSymbolLike))
                                    {
                                        return TypeReferenceSerializationKind.ESSymbolType;
                                    }
                                    else
                                    {
                                        if (this.checker.isFunctionType(type_))
                                        {
                                            return TypeReferenceSerializationKind.TypeWithCallSignature;
                                        }
                                        else
                                        {
                                            if (this.checker.isArrayType(type_))
                                            {
                                                return TypeReferenceSerializationKind.ArrayLikeType;
                                            }
                                            else
                                            {
                                                return TypeReferenceSerializationKind.ObjectType;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    finally { this.checkerMu.Release(); }
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:942:1.
public Slice<Symbol> GetPropertiesOfContainerFunction(Node node)
{
    if ((node == null))
    {
        return Slice<Symbol>.FromArray([]);
    }
    var s = this.checker.getSymbolOfDeclaration(node);
    if ((s == null))
    {
        return Slice<Symbol>.FromArray([]);
    }
    return this.checker.getPropertiesOfType(this.checker.getTypeOfSymbol(s));
}

// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:957:1.
public Node TryJSTypeNodeToTypeNode(Node typeNode, Node enclosingDeclaration, NodeBuilderFlags flags, NodeBuilderInternalFlags internalFlags, NodeBuilderSymbolTracker tracker)
{
    typeNode = this.emitContext.ParseNode(typeNode);
    this.checkerMu.Wait();
    try
    {
        return this.nodeBuilder().TryJSTypeNodeToTypeNode(typeNode, enclosingDeclaration, flags, internalFlags, tracker);
    }
    finally { this.checkerMu.Release(); }
}

// IsThisPropertyAssignmentDeclarationRedundant reports whether a JS `this.<name> = ...` expando
// assignment should be omitted from declaration emit because the member it would synthesize is
// already provided by an `extends` base type. This mirrors the skip condition in the checker's
// serializePropertySymbol: an inherited member is redundant when it is identical to the assigned
// one (same readonly-ness, optionality and type). Inherited accessors and methods are always
// treated as redundant here, since accessors merge oddly with value assignments (and run via the
// accessor at runtime), and `this`-expando props carry the ReplaceableByMethod contract, so a
// rebind such as `this.method = this.method.bind(this)` must not override the base method.
//
// Only `extends` base types are considered. Members coming from `implements` clauses are not
// inherited, so the class must redeclare them, and they are always emitted.
// Upstream upstream/TypeScript/tsc/internal/checker/emitresolver.go:976:1.
public bool IsThisPropertyAssignmentDeclarationRedundant(Node node)
{
    if ((node == null))
    {
        return false;
    }
    this.checkerMu.Wait();
    try
    {
        var s = this.checker.getSymbolOfDeclaration(node);
        if (((s == null) || (s.Parent == null)))
        {
            return false;
        }
        var parentType = this.checker.getDeclaredTypeOfSymbol(s.Parent);
        if ((parentType == null))
        {
            return false;
        }
        {
            foreach (var @base in GoRuntime.RangeValues(this.checker.getBaseTypes(parentType)))
            {
                var baseProp = this.checker.getPropertyOfType(@base, s.NameValue);
                if ((baseProp == null))
                {
                    continue;
                }
                if (((baseProp.Flags & (((SymbolFlags.Accessor | SymbolFlags.Method) | SymbolFlags.Function))) != 0))
                {
                    return true;
                }
                if ((((this.checker.isReadonlySymbol(baseProp) == this.checker.isReadonlySymbol(s)) && (((s.Flags & SymbolFlags.Optional)) == ((baseProp.Flags & SymbolFlags.Optional)))) && this.checker.isTypeIdenticalTo(this.checker.getTypeOfSymbol(s), this.checker.getTypeOfSymbol(baseProp))))
                {
                    return true;
                }
            }
        }
        return false;
    }
    finally { this.checkerMu.Release(); }
}


        // emitresolver.go:677. Preserve the existing raw literal payload ownership.
        public Node CreateLiteralConstValue(Node node, NodeBuilderSymbolTracker tracker)
        {
            node = emitContext.ParseNode(node);
            checkerMu.Wait();
            var type = checker.getTypeOfSymbol(checker.getSymbolOfDeclaration(node));
            checkerMu.Release();
            if (type == null) return null;
            Node enumResult = null;
            if ((type.flags & TypeFlags.EnumLike) != 0)
            {
                checkerMu.Wait();
                try { enumResult = nodeBuilder().SymbolToExpression(type.symbol, SymbolFlags.Value, node, NodeBuilderFlags.None, NodeBuilderInternalFlags.None, tracker); }
                finally { checkerMu.Release(); }
            }
            else if (type == checker.trueType) enumResult = emitContext.Factory.NewKeywordExpression(Kind.TrueKeyword);
            else if (type == checker.falseType) enumResult = emitContext.Factory.NewKeywordExpression(Kind.FalseKeyword);
            if (enumResult != null) return enumResult;
            if ((type.flags & TypeFlags.Literal) == 0) return null;
            switch (type.AsLiteralType().value)
            {
                case ByteStringValue text: return emitContext.Factory.NewStringLiteralWithTextValue(text, TokenFlags.None);
                case string text: return emitContext.Factory.NewStringLiteral(text, TokenFlags.None);
                case JsNum.Number value:
                    if (value.IsInf()) return value.Value > 0 ? emitContext.Factory.NewIdentifier("Infinity") : emitContext.Factory.NewPrefixUnaryExpression(Kind.MinusToken, emitContext.Factory.NewIdentifier("Infinity"));
                    if (value.IsNaN()) return emitContext.Factory.NewIdentifier("NaN");
                    if (value.Abs() != value) return emitContext.Factory.NewPrefixUnaryExpression(Kind.MinusToken, emitContext.Factory.NewNumericLiteral((-value).String(), TokenFlags.None));
                    return emitContext.Factory.NewNumericLiteral(value.String(), TokenFlags.None);
                case JsNum.PseudoBigInt value: return emitContext.Factory.NewBigIntLiteral(pseudoBigIntToString(value) + "n", TokenFlags.None);
                case bool value: return emitContext.Factory.NewKeywordExpression(value ? Kind.TrueKeyword : Kind.FalseKeyword);
            }
            throw new InvalidOperationException("unhandled literal const value kind");
        }

    }
}

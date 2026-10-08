// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/symbolaccessibility.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Selected single-file data-symbol scope/qualification/cache algorithms; external aliases/modules excluded.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    private readonly symbolArenaLinkStore<ContainingSymbolLinks> symbolContainerLinks = new();
    private readonly Dictionary<ulong, List<Symbol>> symbolTableAliasCache = new();
    internal bool IsTypeSymbolAccessible(Symbol symbol, Node enclosingDeclaration) => isSymbolAccessibleWorker(symbol, enclosingDeclaration, SymbolFlags.Type, false, true).Accessibility == SymbolAccessibility.Accessible;
    internal SymbolAccessibilityResult IsSymbolAccessible(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning, bool shouldComputeAliasesToMakeVisible) => isSymbolAccessibleWorker(symbol, enclosingDeclaration, meaning, shouldComputeAliasesToMakeVisible, true);
    // symbolaccessibility.go:435/462. IDs encode table origin, not table contents.
    private const int stKindShift = 61;
    private const ulong stKindLocals = 0, stKindExports = 1UL << stKindShift, stKindMembers = 2UL << stKindShift, stKindGlobals = 3UL << stKindShift, stKindResolvedExports = 4UL << stKindShift, stKindMask = 4UL << stKindShift;
    private static ulong symbolTableIDFromLocals(Node node) => stKindLocals | Ast.GetNodeId(node);
    private static ulong symbolTableIDFromExports(Symbol symbol) => stKindExports | Ast.GetSymbolId(symbol);
    private static ulong symbolTableIDFromMembers(Symbol symbol) => stKindMembers | Ast.GetSymbolId(symbol);
    private static ulong symbolTableIDFromGlobals() => stKindGlobals;
    private sealed record accessibleSymbolChainContext(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning, bool useOnlyExternalAliasing, Dictionary<ulong, HashSet<ulong>> visitedSymbolTablesMap);
    internal List<Symbol> getAccessibleSymbolChain(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning, bool useOnlyExternalAliasing) => getAccessibleSymbolChainEx(new(symbol, enclosingDeclaration, meaning, useOnlyExternalAliasing, new()));
    private List<Symbol> getAccessibleSymbolChainEx(accessibleSymbolChainContext context)
    {
        if (context.symbol == null || isPropertyOrMethodDeclarationSymbol(context.symbol))
            return null;
        Node firstRelevantLocation = null;
        someSymbolTableInScope(context.enclosingDeclaration, (_, _, _, _, node) =>
        {
            firstRelevantLocation = node;
            return true;
        });
        var links = symbolContainerLinks.Get(context.symbol);
        var key = new accessibleChainCacheKey(context.useOnlyExternalAliasing, firstRelevantLocation, context.meaning);
        links.accessibleChainCache ??= new();
        if (links.accessibleChainCache.TryGetValue(key, out var existing))
            return existing;
        List<Symbol> result = null;
        someSymbolTableInScope(context.enclosingDeclaration, (table, tableId, ignoreQualification, isLocalNameLookup, _) =>
        {
            var chain = getAccessibleSymbolChainFromSymbolTable(context, table, tableId, ignoreQualification, isLocalNameLookup);
            if (chain is not { Count: > 0 })
                return false;
            result = chain;
            return true;
        });
        links.accessibleChainCache[key] = result;
        return result;
    }

    private List<Symbol> getAccessibleSymbolChainFromSymbolTable(accessibleSymbolChainContext context, SymbolTable table, ulong tableId, bool ignoreQualification, bool isLocalNameLookup)
    {
        ulong symbolId = Ast.GetSymbolId(context.symbol);
        if (!context.visitedSymbolTablesMap.TryGetValue(symbolId, out var visited))
            context.visitedSymbolTablesMap[symbolId] = visited = new();
        if (!visited.Add(tableId))
            return null;
        var result = trySymbolTable(context, table, tableId, ignoreQualification, isLocalNameLookup);
        visited.Remove(tableId);
        return result;
    }

    private List<Symbol> getSymbolTableAliases(SymbolTable table, ulong tableId)
    {
        ulong kind = tableId & stKindMask;
        if (kind == stKindMembers)
            return null;
        bool cache = kind == stKindGlobals || kind == stKindExports || kind == stKindResolvedExports;
        if (cache && symbolTableAliasCache.TryGetValue(tableId, out var existing))
            return existing;
        List<Symbol> result = null;
        foreach (var symbol in table?.Values ?? Enumerable.Empty<Symbol>())
            if ((symbol.Flags & SymbolFlags.Alias) != 0)
                (result ??= new()).Add(symbol);
        if (cache)
            symbolTableAliasCache[tableId] = result;
        return result;
    }

    private List<Symbol> trySymbolTable(accessibleSymbolChainContext context, SymbolTable table, ulong tableId, bool ignoreQualification, bool isLocalNameLookup)
    {
        var symbol = table?[context.symbol.Name];
        if (symbol != null && isAccessible(context, symbol, null, ignoreQualification))
            return[context.symbol];
        List<List<Symbol>> candidates = null;
        if (symbol?.ExportSymbol != null && isAccessible(context, getMergedSymbol(symbol.ExportSymbol), null, ignoreQualification))
            (candidates ??= new()).Add([context.symbol]);
        foreach (var alias in getSymbolTableAliases(table, tableId) ?? [])
            throw unsupported("trySymbolTable: import/export alias");
        if (candidates is not { Count: > 0 })
            return null;
        // The retained non-alias arm can produce at most one candidate chain.
        return candidates[0];
    }

    private bool isAccessible(accessibleSymbolChainContext context, Symbol fromTable, Symbol resolvedAliasSymbol, bool ignoreQualification)
    {
        bool like = context.symbol == resolvedAliasSymbol || context.symbol == fromTable;
        var symbol = getMergedSymbol(context.symbol);
        if (symbol == getMergedSymbol(resolvedAliasSymbol) || symbol == getMergedSymbol(fromTable))
            like = true;
        if (!like && resolvedAliasSymbol != null && (resolvedAliasSymbol.Flags & SymbolFlags.Alias) != 0)
            throw unsupported("isAccessible: import/export alias chain");
        if (!like)
            return false;
        return !(fromTable.Declarations?.Any(hasNonGlobalAugmentationExternalModuleSymbol) == true) && (ignoreQualification || canQualifySymbol(context, getMergedSymbol(fromTable), context.meaning));
    }

    private bool canQualifySymbol(accessibleSymbolChainContext context, Symbol symbol, SymbolFlags meaning) => !needsQualification(symbol, context.enclosingDeclaration, meaning) || getAccessibleSymbolChainEx(context with { symbol = symbol.Parent, meaning = getQualifiedLeftMeaning(meaning) })is { Count: > 0 };
    internal bool needsQualification(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning)
    {
        bool qualify = false;
        someSymbolTableInScope(enclosingDeclaration, (table, _, _, _, _) =>
        {
            var result = table?[symbol.Name];
            if (result == null)
                return false;
            var candidate = getMergedSymbol(result);
            if (candidate == null)
                return false;
            if (candidate == symbol)
                return true;
            if ((candidate.Flags & SymbolFlags.Alias) != 0)
                throw unsupported("needsQualification: import/export alias");
            if ((candidate.Flags & meaning) == 0)
                return false;
            qualify = true;
            return true;
        });
        return qualify;
    }

    private static bool isPropertyOrMethodDeclarationSymbol(Symbol symbol) => symbol.Declarations is { Count: > 0 } && symbol.Declarations.All(d => d.Kind is Kind.PropertyDeclaration or Kind.MethodDeclaration or Kind.GetAccessor or Kind.SetAccessor);
    private bool someSymbolTableInScope(Node enclosingDeclaration, Func<SymbolTable, ulong, bool, bool, Node, bool> callback)
    {
        for (var location = enclosingDeclaration; location != null; location = location.Parent)
        {
            if (canHaveLocals(location) && location.Locals() != null && !Ast.IsGlobalSourceFile(location))
                if (callback(location.Locals(), symbolTableIDFromLocals(location), false, true, location))
                    return true;
            if (location.Kind == Kind.SourceFile)
            {
                if (Ast.IsExternalOrCommonJSModule(location.AsSourceFile()))
                    throw unsupported("someSymbolTableInScope: external module");
            }
            else if (location.Kind == Kind.InterfaceDeclaration)
            {
                var symbol = getSymbolOfDeclaration(location);
                SymbolTable types = null;
                foreach (var entry in symbol.Members?.AsEnumerable() ?? Enumerable.Empty<KeyValuePair<string, Symbol>>())
                    if ((entry.Value.Flags & (SymbolFlags.Type & ~SymbolFlags.Assignment)) != 0)
                        (types ??= new())[entry.Key] = entry.Value;
                if (types != null && callback(types, symbolTableIDFromMembers(symbol), false, false, location))
                    return true;
            }
            else if (location.Kind is Kind.ModuleDeclaration or Kind.ClassDeclaration or Kind.ClassExpression)
                throw unsupported("someSymbolTableInScope: module/class");
        }

        return callback(globals, symbolTableIDFromGlobals(), false, true, null);
    }

    internal List<Symbol> getContainersOfSymbol(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning)
    {
        var container = getParentOfSymbol(symbol);
        if (container != null && (symbol.Flags & SymbolFlags.TypeParameter) == 0)
            throw unsupported("getContainersOfSymbol: external/property containing symbol");
        foreach (var declaration in symbol.Declarations ?? [])
            if (declaration.Parent != null && (hasNonGlobalAugmentationExternalModuleSymbol(declaration.Parent) || Ast.IsModuleBlock(declaration.Parent) || Ast.IsClassExpression(declaration)))
                throw unsupported("getContainersOfSymbol: module/class container");
        return null; // Original empty candidate arm for standalone type declarations.
    }

    internal Symbol getAliasForSymbolInContainer(Symbol container, Symbol symbol)
    {
        if (container == getParentOfSymbol(symbol))
            return symbol;
        throw unsupported("getAliasForSymbolInContainer: exports");
    }

    private SymbolAccessibilityResult IsAnySymbolAccessible(List<Symbol> symbols, Node enclosingDeclaration, Symbol initialSymbol, SymbolFlags meaning, bool computeAliases, bool allowModules)
    {
        if (symbols is not { Count: > 0 })
            return null;
        Symbol hadAccessibleChain = null;
        foreach (var symbol in symbols)
        {
            var chain = getAccessibleSymbolChain(symbol, enclosingDeclaration, meaning, false);
            if (chain is { Count: > 0 })
            {
                hadAccessibleChain = symbol;
                var visible = hasVisibleDeclarations(chain[0], computeAliases);
                if (visible != null)
                    return visible;
            }

            if (allowModules && symbol.Declarations?.Any(hasNonGlobalAugmentationExternalModuleSymbol) == true)
                throw unsupported("IsAnySymbolAccessible: module visibility");
            var containers = getContainersOfSymbol(symbol, enclosingDeclaration, meaning);
            var nextMeaning = initialSymbol == symbol ? getQualifiedLeftMeaning(meaning) : meaning;
            var parent = IsAnySymbolAccessible(containers, enclosingDeclaration, initialSymbol, nextMeaning, computeAliases, allowModules);
            if (parent != null)
                return parent;
        }

        return hadAccessibleChain != null ? new()
        {
            Accessibility = SymbolAccessibility.NotAccessible,
            ErrorSymbolName = symbolToStringEx(initialSymbol, enclosingDeclaration, meaning),
            ErrorModuleName = hadAccessibleChain != initialSymbol ? symbolToStringEx(hadAccessibleChain, enclosingDeclaration, SymbolFlags.Namespace) : ""
        }

        : null;
    }

    private SymbolAccessibilityResult isSymbolAccessibleWorker(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning, bool computeAliases, bool allowModules)
    {
        if (symbol != null && enclosingDeclaration != null)
        {
            var result = IsAnySymbolAccessible([symbol], enclosingDeclaration, symbol, meaning, computeAliases, allowModules);
            if (result != null)
                return result;
            if (symbol.Declarations?.Any(d => hasNonGlobalAugmentationExternalModuleSymbol(d) || d.Parent != null && hasNonGlobalAugmentationExternalModuleSymbol(d.Parent)) == true)
                throw unsupported("isSymbolAccessibleWorker: external module");
            return new()
            {
                Accessibility = SymbolAccessibility.NotAccessible,
                ErrorSymbolName = symbolToStringEx(symbol, enclosingDeclaration, meaning)
            };
        }

        return new()
        {
            Accessibility = SymbolAccessibility.Accessible
        };
    }

    private static bool hasNonGlobalAugmentationExternalModuleSymbol(Node node) => Ast.IsModuleWithStringLiteralName(node) || node.Kind == Kind.SourceFile && Ast.IsExternalOrCommonJSModule(node.AsSourceFile());
    internal static SymbolFlags getQualifiedLeftMeaning(SymbolFlags meaning) => meaning == SymbolFlags.Value ? SymbolFlags.Value : SymbolFlags.Namespace;
}

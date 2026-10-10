// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/symbolaccessibility.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Selected data/callable/collection/project alias, scope and container algorithms.
// Source container selection includes class-expression and CommonJS class assignments.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    // checker.go:700. Logical identity keys do not allocate SymbolIds on lookup.
    private readonly LinkStore<Symbol, ContainingSymbolLinks> symbolContainerLinks = new();
    private Dictionary<ulong, List<Symbol>> symbolTableAliasCache;
    internal bool IsValueSymbolAccessible(Symbol symbol, Node enclosingDeclaration) => isSymbolAccessibleWorker(symbol, enclosingDeclaration, SymbolFlags.Value, false, true).Accessibility == SymbolAccessibility.Accessible;
    internal bool IsTypeSymbolAccessible(Symbol symbol, Node enclosingDeclaration) => isSymbolAccessibleWorker(symbol, enclosingDeclaration, SymbolFlags.Type, false, true).Accessibility == SymbolAccessibility.Accessible;
    internal bool IsSymbolAccessibleByFlags(Symbol symbol, Node enclosingDeclaration, SymbolFlags flags) => isSymbolAccessibleWorker(symbol, enclosingDeclaration, flags, false, false).Accessibility == SymbolAccessibility.Accessible;
    internal SymbolAccessibilityResult IsSymbolAccessible(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning, bool shouldComputeAliasesToMakeVisible) => isSymbolAccessibleWorker(symbol, enclosingDeclaration, meaning, shouldComputeAliasesToMakeVisible, true);
    // symbolaccessibility.go:435/462. IDs encode table origin, not table contents.
    private const int stKindShift = 61;
    private const ulong stKindLocals = 0, stKindExports = 1UL << stKindShift, stKindMembers = 2UL << stKindShift, stKindGlobals = 3UL << stKindShift, stKindResolvedExports = 4UL << stKindShift, stKindMask = 4UL << stKindShift;
    private static ulong symbolTableIDFromLocals(Node node) => stKindLocals | Ast.GetNodeId(node);
    private static ulong symbolTableIDFromExports(Symbol symbol) => stKindExports | Ast.GetSymbolId(symbol);
    private static ulong symbolTableIDFromResolvedExports(Symbol symbol) => stKindResolvedExports | Ast.GetSymbolId(symbol);
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
        if (cache && symbolTableAliasCache != null && symbolTableAliasCache.TryGetValue(tableId, out var existing))
            return existing;
        List<Symbol> result = null;
        foreach (var symbol in table?.Values ?? Enumerable.Empty<Symbol>())
            if ((symbol.Flags & SymbolFlags.Alias) != 0)
                (result ??= new()).Add(symbol);
        if (cache)
            (symbolTableAliasCache ??= new())[tableId] = result;
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
        {
            if (alias.Name != Ast.InternalSymbolNameExportEquals && alias.Name != Ast.InternalSymbolNameDefault && !(isUMDExportSymbol(alias) && context.enclosingDeclaration != null && Ast.IsExternalModule(Ast.GetSourceFileOfNode(context.enclosingDeclaration))) && (!context.useOnlyExternalAliasing || alias.Declarations?.Any(Ast.IsExternalModuleImportEqualsDeclaration) == true) && (!isLocalNameLookup || alias.Declarations?.Any(isNamespaceReexportDeclaration) != true) && (ignoreQualification || Ast.GetDeclarationOfKind(alias, Kind.ExportSpecifier) == null))
            {
                var candidate = getCandidateListForSymbol(context, alias, resolveAlias(alias), ignoreQualification);
                if (candidate is { Count: > 0 })
                    (candidates ??= new()).Add(candidate);
            }
        }

        if (candidates is { Count: > 0 })
            // Enumerable.OrderBy is stable, preserving the source stable-sort tie arm.
            return candidates.OrderBy(chain => chain, Comparer<List<Symbol>>.Create(compareSymbolChainsWorker)).First();
        return tableId == stKindGlobals ? getCandidateListForSymbol(context, globalThisSymbol, globalThisSymbol, ignoreQualification) : null;
    }

    private int compareSymbolChainsWorker(List<Symbol> first, List<Symbol> second)
    {
        int length = first.Count - second.Count;
        if (length != 0)
            return length;
        for (int i = 0; i < first.Count; i++)
        {
            int comparison = compareSymbols(first[i], second[i]);
            if (comparison != 0)
                return comparison;
        }

        return 0;
    }

    private static bool isUMDExportSymbol(Symbol symbol) => symbol != null && symbol.Declarations is { Count: > 0 } && symbol.Declarations[0] != null && Ast.IsNamespaceExportDeclaration(symbol.Declarations[0]);
    private static bool isNamespaceReexportDeclaration(Node node) => Ast.IsNamespaceExport(node) && node.Parent.ModuleSpecifier() != null;
    private List<Symbol> getCandidateListForSymbol(accessibleSymbolChainContext context, Symbol symbolFromTable, Symbol resolvedImportedSymbol, bool ignoreQualification)
    {
        if (isAccessible(context, symbolFromTable, resolvedImportedSymbol, ignoreQualification))
            return[symbolFromTable];
        var table = getExportsOfSymbol(resolvedImportedSymbol);
        if (table == null)
            return null;
        var chain = getAccessibleSymbolChainFromSymbolTable(context, table, symbolTableIDFromResolvedExports(resolvedImportedSymbol), true, false);
        if (chain is not { Count: > 0 } || !canQualifySymbol(context, symbolFromTable, getQualifiedLeftMeaning(context.meaning)))
            return null;
        List<Symbol> result = [symbolFromTable];
        result.AddRange(chain);
        return result;
    }

    private bool isAccessible(accessibleSymbolChainContext context, Symbol fromTable, Symbol resolvedAliasSymbol, bool ignoreQualification)
    {
        bool like = context.symbol == resolvedAliasSymbol || context.symbol == fromTable;
        var symbol = getMergedSymbol(context.symbol);
        if (symbol == getMergedSymbol(resolvedAliasSymbol) || symbol == getMergedSymbol(fromTable))
            like = true;
        if (!like && resolvedAliasSymbol != null && (resolvedAliasSymbol.Flags & SymbolFlags.Alias) != 0)
        {
            var seenAliases = new HashSet<Symbol>();
            while ((resolvedAliasSymbol.Flags & SymbolFlags.Alias) != 0 && seenAliases.Add(resolvedAliasSymbol))
            {
                resolvedAliasSymbol = getMergedSymbol(resolveAlias(resolvedAliasSymbol));
                if (symbol == resolvedAliasSymbol)
                {
                    like = true;
                    break;
                }
            }
        }

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
            bool shouldResolveAlias = (candidate.Flags & SymbolFlags.Alias) != 0 && Ast.GetDeclarationOfKind(candidate, Kind.ExportSpecifier) == null;
            if (shouldResolveAlias)
                candidate = resolveAlias(candidate);
            var flags = shouldResolveAlias ? getSymbolFlags(candidate) : candidate.Flags;
            if ((flags & meaning) == 0)
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
            if (location.Kind is Kind.SourceFile or Kind.ModuleDeclaration)
            {
                if (location.Kind == Kind.SourceFile && !Ast.IsExternalOrCommonJSModule(location.AsSourceFile()))
                    continue;
                var symbol = getSymbolOfDeclaration(Ast.GetReparsedNodeForNode(location));
                if (callback(symbol.Exports, symbolTableIDFromExports(symbol), false, true, location))
                    return true;
            }
            else if (location.Kind is Kind.ClassDeclaration or Kind.ClassExpression or Kind.InterfaceDeclaration)
            {
                var symbol = getSymbolOfDeclaration(location);
                SymbolTable types = null;
                foreach (var entry in symbol.Members?.AsEnumerable() ?? Enumerable.Empty<KeyValuePair<string, Symbol>>())
                    if ((entry.Value.Flags & (SymbolFlags.Type & ~SymbolFlags.Assignment)) != 0)
                        (types ??= new())[entry.Key] = entry.Value;
                if (types != null && callback(types, symbolTableIDFromMembers(symbol), false, false, location))
                    return true;
                if (Ast.IsClassExpression(location) && location.Name() != null)
                {
                    var names = getClassExpressionNameTable(location);
                    if (names != null && callback(names, symbolTableIDFromLocals(location), false, true, location))
                        return true;
                }
            }
        }

        return callback(globals, symbolTableIDFromGlobals(), false, true, null);
    }

    // symbolaccessibility.go:119. Preserve preference for an in-scope parent,
    // then export-equals aliases, reexports and an object-literal variable.
    private List<Symbol> getWithAlternativeContainers(Symbol container, Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning)
    {
        var additionalContainers = Core.MapNonNil(container.Declarations, d => getFileSymbolIfFileSymbolExportEqualsContainer(d, container));
        var reexportContainers = enclosingDeclaration != null ? getAlternativeContainingModules(symbol, enclosingDeclaration) : null;
        var objectLiteralContainer = getVariableDeclarationOfObjectLiteral(container, meaning);
        var leftMeaning = getQualifiedLeftMeaning(meaning);
        if (enclosingDeclaration != null && (container.Flags & leftMeaning) != 0 && getAccessibleSymbolChain(container, enclosingDeclaration, SymbolFlags.Namespace, false)is { Count: > 0 })
        {
            List<Symbol> result = [container];
            if (additionalContainers != null)
                result.AddRange(additionalContainers);
            if (reexportContainers != null)
                result.AddRange(reexportContainers);
            if (objectLiteralContainer != null)
                result.Add(objectLiteralContainer);
            return result;
        }

        List<Symbol> variableMatches = null;
        if (meaning == SymbolFlags.Value && (container.Flags & leftMeaning) == 0 && (container.Flags & SymbolFlags.Type) != 0 && (getDeclaredTypeOfSymbol(container).flags & TypeFlags.Object) != 0)
        {
            someSymbolTableInScope(enclosingDeclaration, (table, _, _, _, _) =>
            {
                bool found = false;
                foreach (var candidate in table?.Values ?? Enumerable.Empty<Symbol>())
                    if ((candidate.Flags & leftMeaning) != 0 && getTypeOfSymbol(candidate) == getDeclaredTypeOfSymbol(container))
                    {
                        (variableMatches ??= new()).Add(candidate);
                        found = true;
                    }

                return found;
            });
            if (variableMatches != null)
                sortSymbols(variableMatches);
        }

        List<Symbol> alternatives = variableMatches ?? new();
        if (additionalContainers != null)
            alternatives.AddRange(additionalContainers);
        alternatives.Add(container);
        if (objectLiteralContainer != null)
            alternatives.Add(objectLiteralContainer);
        if (reexportContainers != null)
            alternatives.AddRange(reexportContainers);
        return alternatives;
    }

    private List<Symbol> getAlternativeContainingModules(Symbol symbol, Node enclosingDeclaration)
    {
        if (enclosingDeclaration == null)
            return null;
        var containingFile = Ast.GetSourceFileOfNode(enclosingDeclaration);
        ulong id = Ast.GetNodeId(containingFile.AsNode());
        var links = symbolContainerLinks.Get(symbol);
        links.extendedContainersByFile ??= new();
        if (links.extendedContainersByFile.TryGetValue(id, out var existing) && existing != null)
            return existing;
        List<Symbol> results = null;
        foreach (var importRef in containingFile.Imports() ?? [])
        {
            if (Ast.NodeIsSynthesized(importRef))
                continue;
            var resolvedModule = resolveExternalModuleName(enclosingDeclaration, importRef, true, getImportAttributesTypeForModuleSpecifier(importRef));
            if (resolvedModule == null || getAliasForSymbolInContainer(resolvedModule, symbol) == null)
                continue;
            (results ??= new()).Add(resolvedModule);
        }

        if (results is { Count: > 0 })
        {
            links.extendedContainersByFile[id] = results;
            return results;
        }

        if (links.extendedContainers != null)
            return links.extendedContainers.Value;
        results = getExternalModuleContainers(symbol);
        links.extendedContainers = new LinkCell<List<Symbol>>
        {
            Value = results
        };
        return results;
    }

    private sealed class externalModuleContainerIndex
    {
        internal bool complete;
        internal readonly Dictionary<Symbol, List<Symbol>> containersByTarget = new();
        internal readonly Dictionary<Symbol, int> moduleOrder = new();
        internal void add(Symbol target, Symbol container)
        {
            if (!containersByTarget.TryGetValue(target, out var existing))
                containersByTarget[target] = existing = new();
            if (existing.Count == 0 || existing[^1] != container)
                existing.Add(container);
        }
    }

    private externalModuleContainerIndex externalModuleContainers;
    private List<Symbol> getExternalModuleContainers(Symbol symbol)
    {
        if (externalModuleContainers == null)
            buildExternalModuleContainerIndex();
        var index = externalModuleContainers;
        if (!index.complete)
            return scanExternalModuleContainers(symbol);
        index.containersByTarget.TryGetValue(getResolvedTarget(symbol), out var containers);
        var parent = getParentOfSymbol(symbol);
        if (parent == null || !index.moduleOrder.TryGetValue(parent, out int parentOrder))
            return containers;
        int low = 0, high = containers?.Count ?? 0;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (index.moduleOrder[containers[middle]] < parentOrder)
                low = middle + 1;
            else
                high = middle;
        }

        if (containers != null && low < containers.Count && index.moduleOrder[containers[low]] == parentOrder)
            return containers;
        var result = containers != null ? new List<Symbol>(containers) : new();
        result.Insert(low, parent);
        return result;
    }

    private void buildExternalModuleContainerIndex()
    {
        var index = new externalModuleContainerIndex();
        externalModuleContainers = index;
        foreach (var file in files)
        {
            if (!Ast.IsExternalModule(file))
                continue;
            var container = getSymbolOfDeclaration(file.AsNode());
            index.moduleOrder[container] = index.moduleOrder.Count;
            foreach (var exported in getExportsOfSymbol(container)?.Values ?? Enumerable.Empty<Symbol>())
                index.add(getResolvedTarget(exported), container);
            var exportEquals = container.Exports?[Ast.InternalSymbolNameExportEquals];
            if (exportEquals != null)
                index.add(getResolvedTarget(exportEquals), container);
        }

        index.complete = true;
    }

    private List<Symbol> scanExternalModuleContainers(Symbol symbol)
    {
        List<Symbol> containers = null;
        foreach (var file in files)
        {
            if (!Ast.IsExternalModule(file))
                continue;
            var container = getSymbolOfDeclaration(file.AsNode());
            if (getAliasForSymbolInContainer(container, symbol) != null)
                (containers ??= new()).Add(container);
        }

        return containers;
    }

    private Symbol getVariableDeclarationOfObjectLiteral(Symbol symbol, SymbolFlags meaning)
    {
        if ((meaning & SymbolFlags.Value) == 0 || symbol.Declarations is not { Count: > 0 })
            return null;
        var declaration = symbol.Declarations[0];
        if (declaration.Parent == null || !Ast.IsVariableDeclaration(declaration.Parent))
            return null;
        if (Ast.IsObjectLiteralExpression(declaration) && declaration == declaration.Parent.Initializer() || Ast.IsTypeLiteralNode(declaration) && declaration == declaration.Parent.Type())
            return getSymbolOfDeclaration(declaration.Parent);
        return null;
    }

    private static bool hasExternalModuleSymbol(Node declaration) => Ast.IsAmbientModule(declaration) || declaration.Kind == Kind.SourceFile && Ast.IsExternalOrCommonJSModule(declaration.AsSourceFile());
    private Symbol getExternalModuleContainer(Node declaration)
    {
        var node = Ast.FindAncestor(declaration, hasExternalModuleSymbol);
        return node != null ? getSymbolOfDeclaration(node) : null;
    }

    internal Symbol getFileSymbolIfFileSymbolExportEqualsContainer(Node declaration, Symbol container)
    {
        var fileSymbol = getExternalModuleContainer(declaration);
        var exported = fileSymbol?.Exports?[Ast.InternalSymbolNameExportEquals];
        return exported != null && getSymbolIfSameReference(exported, container) != null ? fileSymbol : null;
    }

    internal List<Symbol> getContainersOfSymbol(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning)
    {
        var container = getParentOfSymbol(symbol);
        if (container != null && (symbol.Flags & SymbolFlags.TypeParameter) == 0)
            return getWithAlternativeContainers(container, symbol, enclosingDeclaration, meaning);
        List<Symbol> candidates = null;
        void add(Symbol candidate)
        {
            if (candidate != null && !(candidates?.Contains(candidate) ?? false))
                (candidates ??= new()).Add(candidate);
        }

        foreach (var declaration in symbol.Declarations ?? [])
        {
            if (!Ast.IsAmbientModule(declaration) && declaration.Parent != null)
            {
                if (hasNonGlobalAugmentationExternalModuleSymbol(declaration.Parent))
                {
                    add(getSymbolOfDeclaration(declaration.Parent));
                    continue;
                }

                if (Ast.IsModuleBlock(declaration.Parent) && declaration.Parent.Parent != null && resolveExternalModuleSymbol(getSymbolOfDeclaration(declaration.Parent.Parent), false) == symbol)
                {
                    add(getSymbolOfDeclaration(declaration.Parent.Parent));
                    continue;
                }
            }

            // symbolaccessibility.go:368. CommonJS exports are contained by the source
            // module; other class assignments use the actual resolved left expression.
            if (Ast.IsClassExpression(declaration) && Ast.IsBinaryExpression(declaration.Parent) &&
                declaration.Parent.AsBinaryExpression().OperatorToken.Kind == Kind.EqualsToken &&
                Ast.IsAccessExpression(declaration.Parent.AsBinaryExpression().Left) &&
                Ast.IsEntityNameExpression(declaration.Parent.AsBinaryExpression().Left.Expression()))
            {
                var left = declaration.Parent.AsBinaryExpression().Left;
                if (Ast.IsModuleExportsAccessExpression(left) || Ast.IsExportsIdentifier(left.Expression()))
                {
                    add(getSymbolOfDeclaration(Ast.GetSourceFileOfNode(declaration).AsNode()));
                    continue;
                }
                checkExpressionCached(left.Expression());
                add(symbolNodeLinks.Get(left.Expression()).resolvedSymbol);
                continue;
            }
        }

        if (candidates is not { Count: > 0 })
            return null;
        List<Symbol> bestContainers = null, alternativeContainers = null;
        foreach (var candidate in candidates)
        {
            if (getAliasForSymbolInContainer(candidate, symbol) == null)
                continue;
            var allAlternatives = getWithAlternativeContainers(candidate, symbol, enclosingDeclaration, meaning);
            if (allAlternatives is not { Count: > 0 })
                continue;
            (bestContainers ??= new()).Add(allAlternatives[0]);
            if (allAlternatives.Count > 1)
                (alternativeContainers ??= new()).AddRange(allAlternatives.GetRange(1, allAlternatives.Count - 1));
        }

        if (alternativeContainers != null)
            (bestContainers ??= new()).AddRange(alternativeContainers);
        return bestContainers;
    }

    internal Symbol getAliasForSymbolInContainer(Symbol container, Symbol symbol)
    {
        if (container == getParentOfSymbol(symbol))
            return symbol;
        var exportEquals = container.Exports?[Ast.InternalSymbolNameExportEquals];
        if (exportEquals != null && getSymbolIfSameReference(exportEquals, symbol) != null)
            return container;
        var exports = getExportsOfSymbol(container);
        var quick = exports?[symbol.Name];
        if (quick != null && getSymbolIfSameReference(quick, symbol) != null)
            return quick;
        List<Symbol> candidates = null;
        foreach (var exported in exports?.Values ?? Enumerable.Empty<Symbol>())
            if (getSymbolIfSameReference(exported, symbol) != null)
                (candidates ??= new()).Add(exported);
        if (candidates is not { Count: > 0 })
            return null;
        sortSymbols(candidates);
        return candidates[0];
    }

    private SymbolAccessibilityResult IsAnySymbolAccessible(List<Symbol> symbols, Node enclosingDeclaration, Symbol initialSymbol, SymbolFlags meaning, bool computeAliases, bool allowModules)
    {
        if (symbols is not { Count: > 0 })
            return null;
        Symbol hadAccessibleChain = null;
        bool earlyModuleBail = false;
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
            {
                if (computeAliases)
                {
                    earlyModuleBail = true;
                    continue;
                }

                return new()
                {
                    Accessibility = SymbolAccessibility.Accessible
                };
            }

            var containers = getContainersOfSymbol(symbol, enclosingDeclaration, meaning);
            var nextMeaning = initialSymbol == symbol ? getQualifiedLeftMeaning(meaning) : meaning;
            var parent = IsAnySymbolAccessible(containers, enclosingDeclaration, initialSymbol, nextMeaning, computeAliases, allowModules);
            if (parent != null)
                return parent;
        }

        if (earlyModuleBail)
            return new()
            {
                Accessibility = SymbolAccessibility.Accessible
            };
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
            Symbol symbolExternalModule = null;
            foreach (var declaration in symbol.Declarations ?? [])
            {
                symbolExternalModule = getExternalModuleContainer(declaration);
                if (symbolExternalModule != null)
                    break;
            }

            if (symbolExternalModule != null && symbolExternalModule != getExternalModuleContainer(enclosingDeclaration))
                return new()
                {
                    Accessibility = SymbolAccessibility.CannotBeNamed,
                    ErrorSymbolName = symbolToStringEx(symbol, enclosingDeclaration, meaning),
                    ErrorModuleName = symbolToString(symbolExternalModule),
                    ErrorNode = Ast.IsInJSFile(enclosingDeclaration) ? enclosingDeclaration : null
                };
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

    internal static bool hasNonGlobalAugmentationExternalModuleSymbol(Node node) => Ast.IsModuleWithStringLiteralName(node) || node.Kind == Kind.SourceFile && Ast.IsExternalOrCommonJSModule(node.AsSourceFile());
    internal static SymbolFlags getQualifiedLeftMeaning(SymbolFlags meaning) => meaning == SymbolFlags.Value ? SymbolFlags.Value : SymbolFlags.Namespace;
}

public sealed partial class Checker
{
    // symbolaccessibility.go:883. Anonymous class bindings do not live in locals.
    private SymbolTable getClassExpressionNameTable(Node location)
    {
        var id = Ast.GetNodeId(location);
        if (classExpressionNameTables != null && classExpressionNameTables.TryGetValue(id, out var cached))
            return cached;
        var symbol = getSymbolOfDeclaration(location);
        var name = location.Name().Text();
        if (name.Length == 0 || symbol == null)
            return null;
        var table = new SymbolTable { [name] = symbol };
        (classExpressionNameTables ??= new())[id] = table;
        return table;
    }
}

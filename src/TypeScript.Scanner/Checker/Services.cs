// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned internal/checker/services.go counterpart; actual source anchors below.
#nullable disable
using TypeScript.Scanner;
using TypeScript.AstNavigation;
using ScannerApi = TypeScript.Scanner.Scanner;
using Debug = TypeScript.Scanner.Debug;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    // Upstream internal/checker/services.go:16:1.
    public Slice<Symbol> GetSymbolsInScope(Node location, SymbolFlags meaning)
    {
        return this.getSymbolsInScope(location, meaning);
    }

    // Upstream internal/checker/services.go:20:1.
    private Slice<Symbol> getSymbolsInScope(Node location, SymbolFlags meaning)
    {
        if (((location.Flags & NodeFlags.InWithStatement) != 0))
        {
            return default;
        }

        var symbols = new SymbolTable();
        var isStaticSymbol = false;
        Action<Symbol, SymbolFlags> copySymbol = (symbol, meaning2) =>
        {
            if (((symbol.CombinedLocalAndExportSymbolFlags() & meaning2) != 0))
            {
                var id = symbol.NameValue;
                {
                    var ok = symbols.TryGetValue(id, out var _);
                    if (!ok)
                    {
                        symbols[id] = symbol;
                    }
                }
            }
        };
        Action<SymbolTable, SymbolFlags> copySymbols = (source, meaning3) =>
        {
            if (meaning3 != 0 && source != null)
            {
                {
                    foreach (var range1 in source)
                    {
                        var _2 = range1.Key;
                        var symbol2 = range1.Value;
                        copySymbol(symbol2, meaning3);
                    }
                }
            }
        };
        Action<SymbolTable, SymbolFlags> copyLocallyVisibleExportSymbols = (source2, meaning4) =>
        {
            if (meaning4 != 0 && source2 != null)
            {
                {
                    foreach (var range2 in source2)
                    {
                        var _3 = range2.Key;
                        var symbol3 = range2.Value;
                        if ((((Ast.GetDeclarationOfKind(symbol3, Kind.ExportSpecifier) == null) && (Ast.GetDeclarationOfKind(symbol3, Kind.NamespaceExport) == null)) && (symbol3.Name != Ast.InternalSymbolNameDefault)))
                        {
                            copySymbol(symbol3, meaning4);
                        }
                    }
                }
            }
        };
        Action populateSymbols = () =>
        {
            Node lastLocation = default;
            while ((location != null))
            {
                if (((Ast.IsModuleDeclaration(location) && (location.AsModuleDeclaration().Attributes != null)) && (lastLocation == location.AsModuleDeclaration().Attributes)))
                {
                    lastLocation = location;
                    location = location.Parent;
                    continue;
                }

                if (((canHaveLocals(location) && (location.Locals() != null)) && !Ast.IsGlobalSourceFile(location)))
                {
                    copySymbols(location.Locals(), meaning);
                }

                switch (location.Kind)
                {
                    case Kind.SourceFile:
                    {
                        if (!Ast.IsExternalModule(location.AsSourceFile()))
                        {
                            break;
                        }

                        goto case Kind.ModuleDeclaration;
                    }

                    case Kind.ModuleDeclaration:
                    {
                        copyLocallyVisibleExportSymbols(this.getSymbolOfDeclaration(location).Exports, (meaning & SymbolFlags.ModuleMember));
                        break;
                    }

                    case Kind.EnumDeclaration:
                    {
                        copySymbols(this.getSymbolOfDeclaration(location).Exports, (meaning & SymbolFlags.EnumMember));
                        break;
                    }

                    case Kind.ClassExpression:
                    {
                        var className = location.AsClassExpression().Name();
                        if ((className != null))
                        {
                            copySymbol(location.Symbol(), meaning);
                        }

                        goto case Kind.ClassDeclaration;
                    }

                    case Kind.ClassDeclaration:
                    case Kind.InterfaceDeclaration:
                    {
                        if (!isStaticSymbol)
                        {
                            copySymbols(this.getMembersOfSymbol(this.getSymbolOfDeclaration(location)), (meaning & SymbolFlags.Type));
                        }

                        break;
                    }

                    case Kind.FunctionExpression:
                    {
                        var funcName = location.Name();
                        if ((funcName != null))
                        {
                            copySymbol(location.Symbol(), meaning);
                        }

                        break;
                    }
                }

                if (introducesArgumentsExoticObject(location))
                {
                    copySymbol(this.argumentsSymbol, meaning);
                }

                isStaticSymbol = Ast.IsStatic(location);
                lastLocation = location;
                location = location.Parent;
            }

            copySymbols(this.globals, meaning);
        };
        populateSymbols();
        symbols.Remove(Ast.InternalSymbolNameThis);
        return symbolsToArray(symbols);
    }

    // Upstream internal/checker/services.go:131:1.
    public Slice<Symbol> GetExportsOfModule(Symbol symbol)
    {
        validateOwnedProjectSymbol(symbol);
        return symbolsToArray(this.getExportsOfModule(symbol));
    }

    // Upstream internal/checker/services.go:135:1.
    public void ForEachExportAndPropertyOfModule(Symbol moduleSymbol, Action<Symbol, string> cb)
    {
        {
            var moduleExports = this.getExportsOfModule(moduleSymbol);
            if (moduleExports != null)
                foreach (var range3 in moduleExports)
                {
                    var key = range3.Key;
                    var exportedSymbol = range3.Value;
                    if (!isReservedMemberName(key.Value))
                    {
                        cb(exportedSymbol, key.Value.Materialize());
                    }
                }
        }

        var exportEquals = this.resolveExternalModuleSymbol(moduleSymbol, false);
        if ((exportEquals == moduleSymbol))
        {
            return;
        }

        var typeOfSymbol = this.getTypeOfSymbol(exportEquals);
        if (!this.shouldTreatPropertiesOfExternalModuleAsExports(typeOfSymbol))
        {
            return;
        }

        var reducedType = this.getReducedApparentType(typeOfSymbol);
        if (((reducedType.flags & TypeFlags.StructuredType) == 0))
        {
            return;
        }

        {
            var structuredMembers = this.resolveStructuredTypeMembers(reducedType).members;
            if (structuredMembers != null)
                foreach (var range4 in structuredMembers)
                {
                    var name = range4.Key;
                    var symbol = range4.Value;
                    if (this.isNamedMember(symbol, name))
                    {
                        cb(symbol, name.Value.Materialize());
                    }
                }
        }
    }

    // Upstream internal/checker/services.go:164:1.
    public bool IsValidPropertyAccess(Node node, string propertyName)
    {
        return this.isValidPropertyAccess(node, propertyName);
    }

    // Upstream internal/checker/services.go:168:1.
    private bool isValidPropertyAccess(Node node, string propertyName)
    {
        switch (node.Kind)
        {
            case Kind.PropertyAccessExpression:
            {
                return this.isValidPropertyAccessWithType(node, (node.Expression().Kind == Kind.SuperKeyword), propertyName, this.getWidenedType(this.checkExpression(node.Expression())));
            }

            case Kind.QualifiedName:
            {
                return this.isValidPropertyAccessWithType(node, false, propertyName, this.getWidenedType(this.checkExpression(node.AsQualifiedName().Left)));
            }

            case Kind.ImportType:
            {
                return this.isValidPropertyAccessWithType(node, false, propertyName, this.getTypeFromTypeNode(node));
            }
        }

        throw new InvalidOperationException(("Unexpected node kind in isValidPropertyAccess: " + node.Kind.String()));
    }

    // Upstream internal/checker/services.go:180:1.
    private bool isValidPropertyAccessWithType(Node node, bool isSuper, string propertyName, Type t)
    {
        if (IsTypeAny(t))
        {
            return true;
        }

        var prop = this.getPropertyOfType(t, propertyName);
        return ((prop != null) && this.isPropertyAccessible(node, isSuper, false, t, prop));
    }

    // Checks if an existing property access is valid for completions purposes.
    // node: a property access-like node where we want to check if we can access a property.
    // This node does not need to be an access of the property we are checking.
    // e.g. in completions, this node will often be an incomplete property access node, as in `foo.`.
    // Besides providing a location (i.e. scope) used to check property accessibility, we use this node for
    // computing whether this is a `super` property access.
    // type: the type whose property we are checking.
    // property: the accessed property's symbol.
    // Upstream internal/checker/services.go:198:1.
    public bool IsValidPropertyAccessForCompletions(Node node, Type t, Symbol property)
    {
        return this.isPropertyAccessible(node, ((node.Kind == Kind.PropertyAccessExpression) && (node.Expression().Kind == Kind.SuperKeyword)), false, t, property);
    }

    // Upstream internal/checker/services.go:209:1.
    public Slice<Symbol> GetAllPossiblePropertiesOfTypes(Slice<Type> types)
    {
        var unionType = this.getUnionType(types);
        if (((unionType.flags & TypeFlags.Union) == 0))
        {
            return this.getAugmentedPropertiesOfType(unionType);
        }

        var props = new SymbolTable();
        {
            foreach (var memberType in types)
            {
                var augmentedProps = this.getAugmentedPropertiesOfType(memberType);
                {
                    foreach (var range6 in GoRuntime.Range(augmentedProps))
                    {
                        var _2 = range6.Key;
                        var p = range6.Value;
                        {
                            var ok = props.TryGetValue(p.NameValue, out var _3);
                            if (!ok)
                            {
                                var propertyName = p.NameValue;
                                var prop = this.createUnionOrIntersectionProperty(unionType, ref propertyName, false);
                                if ((prop != null))
                                {
                                    props[p.NameValue] = prop;
                                }
                            }
                        }
                    }
                }
            }
        }

        return props.Count == 0 ? default : Slice<Symbol>.FromArray(props.Values.ToArray());
    }

    // Upstream internal/checker/services.go:231:1.
    public bool IsUnknownSymbol(Symbol symbol)
    {
        return (symbol == this.unknownSymbol);
    }

    // Upstream internal/checker/services.go:235:1.
    public bool IsUndefinedSymbol(Symbol symbol)
    {
        return (symbol == this.undefinedSymbol);
    }

    // Originally from services.ts
    // Upstream internal/checker/services.go:244:1.
    public Type GetNonOptionalType(Type t)
    {
        return this.removeOptionalTypeMarker(t);
    }

    // Upstream internal/checker/services.go:248:1.
    public Type GetStringIndexType(Type t)
    {
        return this.getIndexTypeOfType(t, this.stringType);
    }

    // Upstream internal/checker/services.go:252:1.
    public Type GetNumberIndexType(Type t)
    {
        return this.getIndexTypeOfType(t, this.numberType);
    }

    // Upstream internal/checker/services.go:256:1.
    public Type GetElementTypeOfArrayType(Type t)
    {
        return this.getElementTypeOfArrayType(t);
    }

    // Upstream internal/checker/services.go:260:1.
    public Slice<Signature> GetCallSignatures(Type t)
    {
        return this.getSignaturesOfType(t, SignatureKind.Call);
    }

    // Upstream internal/checker/services.go:264:1.
    public Slice<Signature> GetConstructSignatures(Type t)
    {
        return this.getSignaturesOfType(t, SignatureKind.Construct);
    }

    // Upstream internal/checker/services.go:268:1.
    public Slice<Symbol> GetApparentProperties(Type t)
    {
        return this.getAugmentedPropertiesOfType(t);
    }

    // Upstream internal/checker/services.go:272:1.
    private Slice<Symbol> getAugmentedPropertiesOfType(Type t)
    {
        t = this.getApparentType(t);
        var propsByName = createSymbolTable(this.getPropertiesOfType(t));
        Type functionType = default;
        if ((GoRuntime.Len(this.getSignaturesOfType(t, SignatureKind.Call)) > 0))
        {
            functionType = this.globalCallableFunctionType;
        }
        else
        {
            if ((GoRuntime.Len(this.getSignaturesOfType(t, SignatureKind.Construct)) > 0))
            {
                functionType = this.globalNewableFunctionType;
            }
        }

        if ((propsByName == null))
        {
            propsByName = new SymbolTable();
        }

        if ((functionType != null))
        {
            {
                foreach (var p in this.getPropertiesOfType(functionType))
                {
                    {
                        var ok = propsByName.TryGetValue(p.NameValue, out var _2);
                        if (!ok)
                        {
                            propsByName[p.NameValue] = p;
                        }
                    }
                }
            }
        }

        return this.getNamedMembers(propsByName, null);
    }

    // Upstream internal/checker/services.go:295:1.
    public Symbol TryGetMemberInModuleExportsAndProperties(string memberName, Symbol moduleSymbol)
    {
        var symbol = this.TryGetMemberInModuleExports(memberName, moduleSymbol);
        if ((symbol != null))
        {
            return symbol;
        }

        var exportEquals = this.resolveExternalModuleSymbol(moduleSymbol, false);
        if ((exportEquals == moduleSymbol))
        {
            return null;
        }

        var t = this.getTypeOfSymbol(exportEquals);
        if (this.shouldTreatPropertiesOfExternalModuleAsExports(t))
        {
            return this.getPropertyOfType(t, memberName);
        }

        return null;
    }

    // Upstream internal/checker/services.go:313:1.
    public Symbol TryGetMemberInModuleExports(string memberName, Symbol moduleSymbol)
    {
        var symbolTable = this.getExportsOfModule(moduleSymbol);
        return symbolTable?[memberName];
    }

    // Upstream internal/checker/services.go:318:1.
    private bool shouldTreatPropertiesOfExternalModuleAsExports(Type resolvedExternalModuleType)
    {
        return (((((resolvedExternalModuleType.flags & TypeFlags.Primitive) == 0) || ((resolvedExternalModuleType.objectFlags & ObjectFlags.Class) != 0)) || this.isArrayType(resolvedExternalModuleType)) || isTupleType(resolvedExternalModuleType));
    }

    // Upstream internal/checker/services.go:326:1.
    internal Type GetContextualType(Node node, ContextFlags contextFlags = ContextFlags.None)
    {
        if (((contextFlags & ContextFlags.IgnoreNodeInferences) != 0))
        {
            return this.runWithInferenceBlockedFromSourceNode(node, () =>
            {
                return this.getContextualType(node, contextFlags);
            });
        }

        return this.getContextualType(node, contextFlags);
    }

    // Upstream internal/checker/services.go:333:1.
    private T runWithInferenceBlockedFromSourceNode<T>(Node node, Func<T> fn)
    {
        var containingCall = Ast.FindAncestor(node, Ast.IsCallLikeExpression);
        if ((containingCall != null))
        {
            var toMarkSkip = node;
            while (true)
            {
                (this.skipDirectInferenceNodes ??= new HashSet<Node>()).Add(toMarkSkip);
                toMarkSkip = toMarkSkip.Parent;
                if (((toMarkSkip == null) || (toMarkSkip == containingCall)))
                {
                    break;
                }
            }
        }

        this.isInferencePartiallyBlocked = true;
        var result = this.runWithoutResolvedSignatureCaching(node, fn);
        this.isInferencePartiallyBlocked = false;
        this.skipDirectInferenceNodes?.Clear();
        return result;
    }

    // Upstream internal/checker/services.go:366:1.
    private T runWithoutResolvedSignatureCaching<T>(Node node, Func<T> fn)
    {
        var ancestorNode = Ast.FindAncestor(node, Ast.IsCallLikeOrFunctionLikeExpression);
        if (ancestorNode is null)
            return fn();
        // Go keys are stable link pointers. LinkStore uses inline records; save the
        // corresponding reference-identity owners and reacquire their same slots.
        var cachedResolvedSignatures = new Dictionary<Node, Signature>();
        var cachedTypes = new Dictionary<Symbol, Type>();
        while (ancestorNode is not null)
        {
            ref var links = ref signatureLinks.Get(ancestorNode);
            cachedResolvedSignatures[ancestorNode] = links.resolvedSignature;
            links.resolvedSignature = null;
            if (Ast.IsFunctionExpressionOrArrowFunction(ancestorNode))
            {
                var symbol = getSymbolOfDeclaration(ancestorNode);
                ref var symbolLinks = ref valueSymbolLinks.Get(symbol);
                cachedTypes[symbol] = symbolLinks.resolvedType;
                symbolLinks.resolvedType = null;
            }

            ancestorNode = Ast.FindAncestor(ancestorNode.Parent, Ast.IsCallLikeOrFunctionLikeExpression);
        }

        try
        {
            return fn();
        }
        finally
        {
            foreach (var pair in cachedResolvedSignatures)
                signatureLinks.Get(pair.Key).resolvedSignature = pair.Value;
            foreach (var pair in cachedTypes)
                valueSymbolLinks.Get(pair.Key).resolvedType = pair.Value;
        }
    }

    // Upstream internal/checker/services.go:395:1.
    public Symbol SkipAlias(Symbol symbol)
    {
        if (((symbol.Flags & SymbolFlags.Alias) != 0))
        {
            return this.GetAliasedSymbol(symbol);
        }

        return symbol;
    }

    // Upstream internal/checker/services.go:402:1.
    public Slice<Symbol> GetRootSymbols(Symbol symbol)
    {
        var roots = this.getImmediateRootSymbols(symbol);
        if ((GoRuntime.Len(roots) == 0))
        {
            return Slice<Symbol>.FromArray([symbol]);
        }

        Slice<Symbol> result = default;
        {
            foreach (var root in roots)
            {
                result = result.Append(this.GetRootSymbols(root));
            }
        }

        return result;
    }

    // Upstream internal/checker/services.go:414:1.
    public Symbol GetMappedTypeSymbolOfProperty(Symbol symbol)
    {
        {
            var valueLinks = this.valueSymbolLinks.TryGet(symbol);
            if ((!valueLinks.IsEmpty))
            {
                return valueLinks.Span[0].containingType.symbol;
            }
        }

        return null;
    }

    // Upstream internal/checker/services.go:421:1.
    private Slice<Symbol> getImmediateRootSymbols(Symbol symbol)
    {
        if (((symbol.CheckFlags & CheckFlags.Synthetic) != 0))
        {
            return Core.MapNonNilSlice(this.valueSymbolLinks.Get(symbol).containingType.Types(), (t) =>
            {
                return this.getPropertyOfType(t, symbol.NameValue);
            });
        }

        if (((symbol.Flags & SymbolFlags.Transient) != 0))
        {
            if (this.spreadLinks.Has(symbol))
            {
                var leftSpread = this.spreadLinks.Get(symbol).leftSpread;
                var rightSpread = this.spreadLinks.Get(symbol).rightSpread;
                if ((leftSpread != null))
                {
                    return Slice<Symbol>.FromArray([leftSpread, rightSpread]);
                }
            }

            if (this.mappedSymbolLinks.Has(symbol))
            {
                var syntheticOrigin = this.mappedSymbolLinks.Get(symbol).syntheticOrigin;
                if ((syntheticOrigin != null))
                {
                    return Slice<Symbol>.FromArray([syntheticOrigin]);
                }
            }

            var target = this.tryGetTarget(symbol);
            if ((target != null))
            {
                return Slice<Symbol>.FromArray([target]);
            }
        }

        return default;
    }

    // Upstream internal/checker/services.go:452:1.
    private Symbol tryGetTarget(Symbol symbol)
    {
        Symbol target = default;
        var next = symbol;
        while (true)
        {
            if (this.valueSymbolLinks.Has(next))
            {
                next = this.valueSymbolLinks.Get(next).target;
            }
            else
            {
                if (this.exportTypeLinks.Has(next))
                {
                    next = this.exportTypeLinks.Get(next).target;
                }
                else
                {
                    next = null;
                }
            }

            if ((next == null))
            {
                break;
            }

            target = next;
        }

        return target;
    }

    // Upstream internal/checker/services.go:471:1.
    public Symbol GetExportSymbolOfSymbol(Symbol symbol)
    {
        return this.getMergedSymbol(Core.IfElse((symbol.ExportSymbol != null), symbol.ExportSymbol, symbol));
    }

    // Upstream internal/checker/services.go:475:1.
    public Symbol GetExportSpecifierLocalTargetSymbol(Node node)
    {
        switch (node.Kind)
        {
            case Kind.ExportSpecifier:
            {
                if ((node.Parent.Parent.ModuleSpecifier() != null))
                {
                    return this.getExternalModuleMember(node.Parent.Parent, node, false);
                }

                var name = node.PropertyNameOrName();
                if ((name.Kind == Kind.StringLiteral))
                {
                    return null;
                }

                return this.resolveEntityName(name, (((SymbolFlags.Value | SymbolFlags.Type) | SymbolFlags.Namespace) | SymbolFlags.Alias), true, false, null);
            }

            case Kind.Identifier:
            {
                return this.resolveEntityName(node, (((SymbolFlags.Value | SymbolFlags.Type) | SymbolFlags.Namespace) | SymbolFlags.Alias), true, false, null);
            }
        }

        throw new InvalidOperationException("Unhandled case in getExportSpecifierLocalTargetSymbol, node should be ExportSpecifier | Identifier");
    }

    // Upstream internal/checker/services.go:494:1.
    public Symbol GetShorthandAssignmentValueSymbol(Node location)
    {
        if (((location != null) && (location.Kind == Kind.ShorthandPropertyAssignment)))
        {
            return this.resolveEntityName(location.Name(), (SymbolFlags.Value | SymbolFlags.Alias), true, false, null);
        }

        return null;
    }

    // *
    // * Get symbols that represent parameter-property-declaration as parameter and as property declaration
    // * @param parameter a parameterDeclaration node
    // * @param parameterName a name of the parameter to get the symbols for.
    // * @return a tuple of two symbols
    //
    // Upstream internal/checker/services.go:507:1.
    public (Symbol, Symbol) GetSymbolsOfParameterPropertyDeclaration(Node parameter, string parameterName)
    {
        var constructorDeclaration = parameter.Parent;
        var classDeclaration = parameter.Parent.Parent;
        var parameterSymbol = this.getSymbol(constructorDeclaration.Locals(), parameterName, SymbolFlags.Value);
        var propertySymbol = this.getSymbol(this.getMembersOfSymbol(classDeclaration.Symbol()), parameterName, SymbolFlags.Value);
        if (((parameterSymbol != null) && (propertySymbol != null)))
        {
            return (parameterSymbol, propertySymbol);
        }

        throw new InvalidOperationException("There should exist two symbols, one as property declaration and one as parameter declaration");
    }

    // IsDeclarationUsed checks if an import declaration identifier is used in the source file.
    // This is primarily used for organizing imports to determine which imports can be removed.
    // Upstream internal/checker/services.go:523:1.
    public bool IsDeclarationUsed(SourceFile sourceFile, Node identifier, bool jsxElementsPresent, bool jsxModeNeedsExplicitImport)
    {
        if ((jsxElementsPresent && jsxModeNeedsExplicitImport))
        {
            var jsxNamespace = this.getJsxNamespace(sourceFile.AsNode());
            var jsxFragmentFactory = this.GetJsxFragmentFactory(sourceFile.AsNode());
            var identifierText = identifier.TextValue();
            if ((identifierText.EqualsValue(jsxNamespace)))
            {
                return true;
            }

            if (((jsxFragmentFactory.Length != 0) && (identifierText.EqualsString(jsxFragmentFactory))))
            {
                return true;
            }
        }

        var symbol = this.GetSymbolAtLocation(identifier.AsNode());
        if ((symbol == null))
        {
            return true;
        }

        return this.IsSymbolReferencedInFile(sourceFile, identifier, symbol);
    }

    // IsSymbolReferencedInFile checks if a symbol is referenced in the source file (besides its definition).
    // This is used as a quick check for whether a symbol is used at all in a file.
    // Upstream internal/checker/services.go:551:1.
    public bool IsSymbolReferencedInFile(SourceFile sourceFile, Node definition, Symbol symbol)
    {
        var identifierText = definition.TextValue();
        {
            foreach (var token in getPossibleSymbolReferenceNodes(sourceFile, identifierText, sourceFile.AsNode()))
            {
                if (!Ast.IsIdentifier(token))
                {
                    continue;
                }

                var id = token.AsIdentifier();
                if (((id == definition) || (!id.TextValue().EqualsValue(identifierText))))
                {
                    continue;
                }

                var refSymbol = this.GetSymbolAtLocation(token);
                if ((refSymbol == symbol))
                {
                    return true;
                }

                if (((token.Parent != null) && (token.Parent.Kind == Kind.ShorthandPropertyAssignment)))
                {
                    var shorthandSymbol = this.GetShorthandAssignmentValueSymbol(token.Parent);
                    if ((shorthandSymbol == symbol))
                    {
                        return true;
                    }
                }

                if (((token.Parent != null) && Ast.IsExportSpecifier(token.Parent)))
                {
                    var localSymbol = this.getLocalSymbolForExportSpecifier(token.AsIdentifier(), refSymbol, token.Parent.AsExportSpecifier());
                    if ((localSymbol == symbol))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // GetReferencesToSymbolInFile returns all identifier nodes in the file that reference the given symbol.
    // Upstream internal/checker/services.go:586:1.
    public Slice<Node> GetReferencesToSymbolInFile(SourceFile sourceFile, Symbol symbol)
    {
        var identifierText = symbol.NameValue;
        Slice<Node> result = default;
        {
            foreach (var token in getPossibleSymbolReferenceNodes(sourceFile, identifierText, sourceFile.AsNode()))
            {
                if (!Ast.IsIdentifier(token))
                {
                    continue;
                }

                var id = token.AsIdentifier();
                if ((!id.TextValue().EqualsValue(identifierText)))
                {
                    continue;
                }

                var refSymbol = this.GetSymbolAtLocation(token);
                if ((refSymbol == symbol))
                {
                    result = result.Append(token);
                    continue;
                }

                if (((token.Parent != null) && (token.Parent.Kind == Kind.ShorthandPropertyAssignment)))
                {
                    var shorthandSymbol = this.GetShorthandAssignmentValueSymbol(token.Parent);
                    if ((shorthandSymbol == symbol))
                    {
                        result = result.Append(token);
                        continue;
                    }
                }

                if (((token.Parent != null) && Ast.IsExportSpecifier(token.Parent)))
                {
                    var localSymbol = this.getLocalSymbolForExportSpecifier(token.AsIdentifier(), refSymbol, token.Parent.AsExportSpecifier());
                    if ((localSymbol == symbol))
                    {
                        result = result.Append(token);
                        continue;
                    }
                }
            }
        }

        return result;
    }

    // Upstream internal/checker/services.go:623:1.
    private Symbol getLocalSymbolForExportSpecifier(Node referenceLocation, Symbol referenceSymbol, Node exportSpecifier)
    {
        if (isExportSpecifierAlias(referenceLocation, exportSpecifier))
        {
            {
                var symbol = this.GetExportSpecifierLocalTargetSymbol(exportSpecifier.AsNode());
                if ((symbol != null))
                {
                    return symbol;
                }
            }
        }

        return referenceSymbol;
    }

    // Upstream internal/checker/services.go:632:1.
    private static bool isExportSpecifierAlias(Node referenceLocation, Node exportSpecifier)
    {
        Debug.Assert(((exportSpecifier.PropertyName() == referenceLocation.AsNode()) || (exportSpecifier.Name() == referenceLocation.AsNode())), "referenceLocation is not export specifier name or property name");
        var propertyName = exportSpecifier.PropertyName();
        if ((propertyName != null))
        {
            return (propertyName == referenceLocation.AsNode());
        }
        else
        {
            return (exportSpecifier.Parent.Parent.ModuleSpecifier() == null);
        }
    }

    // Upstream internal/checker/services.go:645:1.
    private static Slice<Node> getPossibleSymbolReferenceNodes(SourceFile sourceFile, ByteStringValue symbolName, Node container)
    {
        return Core.MapNonNilSlice(getPossibleSymbolReferencePositions(sourceFile, symbolName, container), (pos) =>
        {
            {
                var referenceLocation = AstNav.GetTouchingPropertyName(sourceFile, pos);
                if ((referenceLocation != sourceFile.AsNode()))
                {
                    return referenceLocation;
                }
            }

            return default;
        });
    }

    // Upstream internal/checker/services.go:654:1.
    private static Slice<int> getPossibleSymbolReferencePositions(SourceFile sourceFile, ByteStringValue symbolName, Node container)
    {
        var positions = Slice<int>.Make(0);
        if (symbolName.Length == 0)
        {
            return positions;
        }

        var text = ByteStringValue.FromView(sourceFile.TextView());
        var sourceLength = text.Length;
        var symbolNameLength = symbolName.Length;
        if ((container == null))
        {
            container = sourceFile.AsNode();
        }

        var position = text.Slice(container.Pos()).IndexOf(symbolName);
        var endPos = container.End();
        while (((position >= 0) && (position < endPos)))
        {
            var endPosition = (position + symbolNameLength);
            if (((((position == 0) || !ScannerApi.IsIdentifierPart((int)(text[(position - 1)])))) && (((endPosition == sourceLength) || !ScannerApi.IsIdentifierPart((int)(text[endPosition]))))))
            {
                positions = positions.Append(position);
            }

            var startIndex = ((position + symbolNameLength) + 1);
            if ((startIndex > text.Length))
            {
                break;
            }

            {
                var foundIndex = text.Slice(startIndex).IndexOf(symbolName);
                if ((foundIndex != -1))
                {
                    position = (startIndex + foundIndex);
                }
                else
                {
                    break;
                }
            }
        }

        return positions;
    }

    // Upstream internal/checker/services.go:699:1.
    public Type GetTypeArgumentConstraint(Node node)
    {
        if (!Ast.IsTypeNode(node))
        {
            return null;
        }

        return this.getTypeArgumentConstraint(node);
    }

    // getUninstantiatedSignatures gets generic signatures from the function's/constructor's type.
    // Upstream internal/checker/services.go:707:1.
    private Slice<Signature> getUninstantiatedSignatures(Node node)
    {
        switch (node.Kind)
        {
            case Kind.CallExpression:
            case Kind.Decorator:
            {
                return this.getSignaturesOfType(this.getTypeOfExpression(node.Expression()), SignatureKind.Call);
            }

            case Kind.NewExpression:
            {
                return this.getSignaturesOfType(this.getTypeOfExpression(node.Expression()), SignatureKind.Construct);
            }

            case Kind.JsxSelfClosingElement:
            case Kind.JsxOpeningElement:
            {
                if (isJsxIntrinsicTagName(node.TagName()))
                {
                    return default;
                }

                return this.getSignaturesOfType(this.getTypeOfExpression(node.TagName()), SignatureKind.Call);
            }

            case Kind.TaggedTemplateExpression:
            {
                return this.getSignaturesOfType(this.getTypeOfExpression(node.AsTaggedTemplateExpression().Tag), SignatureKind.Call);
            }

            case Kind.BinaryExpression:
            case Kind.JsxOpeningFragment:
            {
                return default;
            }
        }

        return default;
    }

    // Upstream internal/checker/services.go:726:1.
    private Type getTypeParameterConstraintForPositionAcrossSignatures(Slice<Signature> signatures, int position)
    {
        Slice<Type> relevantConstraints = default;
        {
            foreach (var signature in signatures)
            {
                if ((position >= GoRuntime.Len(signature.typeParameters)))
                {
                    continue;
                }

                var relevantTypeParameter = signature.typeParameters[position];
                var relevantConstraint = this.getConstraintOfTypeParameter(relevantTypeParameter);
                if ((relevantConstraint != null))
                {
                    relevantConstraints = relevantConstraints.Append(relevantConstraint);
                }
            }
        }

        return this.getUnionType(relevantConstraints);
    }

    // Upstream internal/checker/services.go:741:1.
    private Type getTypeArgumentConstraint(Node node)
    {
        int typeArgumentPosition = -1;
        if (Ast.HasTypeArguments(node.Parent))
        {
            var typeArgs = node.Parent.TypeArguments();
            {
                foreach (var range14 in GoRuntime.Range(typeArgs))
                {
                    var i = range14.Key;
                    var arg = range14.Value;
                    if ((arg == node))
                    {
                        typeArgumentPosition = i;
                        break;
                    }
                }
            }
        }

        if ((typeArgumentPosition >= 0))
        {
            if (Ast.IsCallLikeExpression(node.Parent))
            {
                return this.getTypeParameterConstraintForPositionAcrossSignatures(this.getUninstantiatedSignatures(node.Parent), typeArgumentPosition);
            }

            if (Ast.IsDecorator(node.Parent.Parent))
            {
                return this.getTypeParameterConstraintForPositionAcrossSignatures(this.getUninstantiatedSignatures(node.Parent.Parent), typeArgumentPosition);
            }

            if ((Ast.IsExpressionWithTypeArguments(node.Parent) && Ast.IsExpressionStatement(node.Parent.Parent)))
            {
                var uninstantiatedType = this.checkExpression(node.Parent.Expression());
                var callConstraint = this.getTypeParameterConstraintForPositionAcrossSignatures(this.getSignaturesOfType(uninstantiatedType, SignatureKind.Call), typeArgumentPosition);
                var constructConstraint = this.getTypeParameterConstraintForPositionAcrossSignatures(this.getSignaturesOfType(uninstantiatedType, SignatureKind.Construct), typeArgumentPosition);
                if (((constructConstraint.flags & TypeFlags.Never) != 0))
                {
                    return callConstraint;
                }

                if (((callConstraint.flags & TypeFlags.Never) != 0))
                {
                    return constructConstraint;
                }

                return this.getIntersectionType(Slice<Type>.FromArray([callConstraint, constructConstraint]));
            }

            if (Ast.IsTypeReferenceType(node.Parent))
            {
                var typeParameters = this.getTypeParametersForTypeReferenceOrImport(node.Parent);
                if ((GoRuntime.Len(typeParameters) == 0))
                {
                    return null;
                }

                if ((typeArgumentPosition >= GoRuntime.Len(typeParameters)))
                {
                    return null;
                }

                var relevantTypeParameter = typeParameters[typeArgumentPosition];
                var constraint = this.getConstraintOfTypeParameter(relevantTypeParameter);
                if ((constraint != null))
                {
                    return this.instantiateType(constraint, newTypeMapper(typeParameters, this.getEffectiveTypeArguments(node.Parent, typeParameters)));
                }
            }
        }

        return null;
    }

    // Upstream internal/checker/services.go:815:1.
    public bool IsTypeInvalidDueToUnionDiscriminant(Type contextualType, Node obj)
    {
        var properties = obj.Properties();
        return Core.Some(properties, (property) =>
        {
            Type nameType = default;
            var propertyName = property.Name();
            if ((propertyName != null))
            {
                if (Ast.IsJsxNamespacedName(propertyName))
                {
                    nameType = this.getStringLiteralType(propertyName.Text());
                }
                else
                {
                    nameType = this.getLiteralTypeFromPropertyName(propertyName);
                }
            }

            string name = "";
            if (((nameType != null) && isTypeUsableAsPropertyName(nameType)))
            {
                name = getPropertyNameFromType(nameType);
            }

            Type expected = default;
            if ((name != ""))
            {
                expected = this.getTypeOfPropertyOfType(contextualType, name);
            }

            return (((expected != null) && isLiteralType(expected)) && !this.isTypeAssignableTo(this.getTypeOfNode(property), expected));
        });
    }

    // Unlike `getExportsOfModule`, this includes properties of an `export =` value.
    // Upstream internal/checker/services.go:840:1.
    public Slice<Symbol> GetExportsAndPropertiesOfModule(Symbol moduleSymbol)
    {
        var exports = this.getExportsOfModuleAsArray(moduleSymbol);
        var exportEquals = this.resolveExternalModuleSymbol(moduleSymbol, false);
        if ((exportEquals != moduleSymbol))
        {
            var t = this.getTypeOfSymbol(exportEquals);
            if (this.shouldTreatPropertiesOfExternalModuleAsExports(t))
            {
                exports = exports.Append(this.getPropertiesOfType(t));
            }
        }

        return exports;
    }

    // Upstream internal/checker/services.go:852:1.
    private Slice<Symbol> getExportsOfModuleAsArray(Symbol moduleSymbol)
    {
        return symbolsToArray(this.getExportsOfModule(moduleSymbol));
    }

    // Returns all the properties of the Jsx.IntrinsicElements interface.
    // Upstream internal/checker/services.go:857:1.
    public Slice<Symbol> GetJsxIntrinsicTagNamesAt(Node location)
    {
        var intrinsics = this.getJsxType(JsxNames.IntrinsicElements, location);
        if ((intrinsics == null))
        {
            return default;
        }

        return this.GetPropertiesOfType(intrinsics);
    }

    // Upstream internal/checker/services.go:865:1.
    public Type GetContextualTypeForJsxAttribute(Node attribute)
    {
        return this.getContextualTypeForJsxAttribute(attribute, ContextFlags.None);
    }

    // Upstream internal/checker/services.go:869:1.
    public object GetConstantValue(Node node)
    {
        if ((node.Kind == Kind.EnumMember))
        {
            return this.getEnumMemberValue(node).Value;
        }

        if ((this.symbolNodeLinks.Get(node).resolvedSymbol == null))
        {
            this.checkExpressionCached(node);
        }

        var symbol = this.symbolNodeLinks.Get(node).resolvedSymbol;
        if (((symbol == null) && Ast.IsEntityNameExpression(node)))
        {
            symbol = this.resolveEntityName(node, SymbolFlags.Value, true, false, null);
        }

        if (((symbol != null) && ((symbol.Flags & SymbolFlags.EnumMember) != 0)))
        {
            var member = symbol.ValueDeclaration;
            if (Ast.IsEnumConst(member.Parent))
            {
                return this.getEnumMemberValue(member).Value;
            }
        }

        return null;
    }

    // Upstream internal/checker/services.go:898:1.
    private (Signature, Slice<Signature>) getResolvedSignatureWorker(Node node, CheckMode checkMode, int argumentCount)
    {
        this.apparentArgumentCount = argumentCount;
        var candidatesOutArray = new GoRef<Slice<Signature>>(Slice<Signature>.Make(0));
        Signature res = default;
        if (((node != null) && Ast.IsParseTreeNode(node)))
        {
            res = this.getResolvedSignature(node, candidatesOutArray, checkMode);
        }

        this.apparentArgumentCount = null;
        return (res, candidatesOutArray.Value);
    }

    // Upstream internal/checker/services.go:909:1.
    public Slice<Signature> GetCandidateSignaturesForStringLiteralCompletions(Node call, Node editingArgument)
    {
        var candidates = this.runWithInferenceBlockedFromSourceNode(editingArgument, () =>
        {
            var tuple15 = this.getResolvedSignatureWorker(call, CheckMode.Normal, 0);
            var blockedInferenceCandidates = tuple15.Item2;
            return blockedInferenceCandidates;
        });
        var candidatesSet = new HashSet<Signature>();
        foreach (var candidate in candidates)
            candidatesSet.Add(candidate);
        var otherCandidates = this.runWithoutResolvedSignatureCaching(editingArgument, () =>
        {
            var tuple16 = this.getResolvedSignatureWorker(call, CheckMode.Normal, 0);
            var inferenceCandidates = tuple16.Item2;
            return inferenceCandidates;
        });
        {
            foreach (var range17 in GoRuntime.Range(otherCandidates))
            {
                var _3 = range17.Key;
                var candidate = range17.Value;
                if (candidatesSet.Contains(candidate))
                {
                    continue;
                }

                candidates = candidates.Append(candidate);
            }
        }

        return candidates;
    }

    // GetTypeAtPosition returns the type of a parameter at a given index in a signature.
    // Upstream internal/checker/services.go:934:1.
    public Type GetTypeAtPosition(Signature s, int pos)
    {
        return this.getTypeAtPosition(s, pos);
    }

    // Upstream internal/checker/services.go:938:1.
    public Type GetTypeParameterAtPosition(Signature s, int pos)
    {
        var t = this.getTypeAtPosition(s, pos);
        if ((t.IsIndex() && isThisTypeParameter(t.AsIndexType().target)))
        {
            var constraint = this.getBaseConstraintOfType(t.AsIndexType().target);
            if ((constraint != null))
            {
                return this.getIndexType(constraint);
            }
        }

        return t;
    }

    // GetContextualTypeForArrayLiteralAtPosition returns the contextual type for an element at the given position
    // in an array with the given contextual type.
    // Upstream internal/checker/services.go:951:1.
    public Type GetContextualTypeForArrayLiteralAtPosition(Type contextualArrayType, Node arrayLiteral, int position)
    {
        if ((contextualArrayType == null))
        {
            return null;
        }

        var tuple18 = (-1, -1);
        var firstSpreadIndex = tuple18.Item1;
        var lastSpreadIndex = tuple18.Item2;
        var elementIndex = 0;
        var elements = arrayLiteral.Elements();
        {
            foreach (var range19 in GoRuntime.Range(elements))
            {
                var i = range19.Key;
                var elem = range19.Value;
                if ((elem.Pos() < position))
                {
                    elementIndex++;
                }

                if (Ast.IsSpreadElement(elem))
                {
                    if ((firstSpreadIndex == -1))
                    {
                        firstSpreadIndex = i;
                    }

                    lastSpreadIndex = i;
                }
            }
        }

        return this.getContextualTypeForElementExpression(contextualArrayType, elementIndex, -1, firstSpreadIndex, lastSpreadIndex);
    }

    // Upstream internal/checker/services.go:1002:1.
    // services.go:979. Retained package owner; lookups borrow symbol-name bytes.
    private static readonly HashSet<ByteStringKey> knownGenericTypeNames = new(ByteStringKey.Comparer)
    {
        ByteStringKey.FromValue(ByteStringValue.FromString("Array")),
        ByteStringKey.FromValue(ByteStringValue.FromString("ArrayLike")),
        ByteStringKey.FromValue(ByteStringValue.FromString("ReadonlyArray")),
        ByteStringKey.FromValue(ByteStringValue.FromString("Promise")),
        ByteStringKey.FromValue(ByteStringValue.FromString("PromiseLike")),
        ByteStringKey.FromValue(ByteStringValue.FromString("Iterable")),
        ByteStringKey.FromValue(ByteStringValue.FromString("IterableIterator")),
        ByteStringKey.FromValue(ByteStringValue.FromString("AsyncIterable")),
        ByteStringKey.FromValue(ByteStringValue.FromString("Set")),
        ByteStringKey.FromValue(ByteStringValue.FromString("WeakSet")),
        ByteStringKey.FromValue(ByteStringValue.FromString("ReadonlySet")),
        ByteStringKey.FromValue(ByteStringValue.FromString("Map")),
        ByteStringKey.FromValue(ByteStringValue.FromString("WeakMap")),
        ByteStringKey.FromValue(ByteStringValue.FromString("ReadonlyMap")),
        ByteStringKey.FromValue(ByteStringValue.FromString("Partial")),
        ByteStringKey.FromValue(ByteStringValue.FromString("Required")),
        ByteStringKey.FromValue(ByteStringValue.FromString("Readonly")),
        ByteStringKey.FromValue(ByteStringValue.FromString("Pick")),
        ByteStringKey.FromValue(ByteStringValue.FromString("Omit")),
        ByteStringKey.FromValue(ByteStringValue.FromString("NonNullable")),
    };
    private static bool isKnownGenericTypeName(ByteStringValue name) => knownGenericTypeNames.Contains(ByteStringKey.FromValue(name));
    // Upstream internal/checker/services.go:1007:1.
    public Type GetFirstTypeArgumentFromKnownType(Type t)
    {
        if (((((t.objectFlags & ObjectFlags.Reference) != 0) && (t.symbol != null)) && isKnownGenericTypeName(t.symbol.NameValue)))
        {
            var symbol = this.getGlobalSymbol(t.symbol.NameValue, SymbolFlags.Type, null);
            if (((symbol != null) && (symbol == t.Target().symbol)))
            {
                return Core.FirstOrNil(this.getTypeArguments(t));
            }
        }

        if (((t.alias != null) && isKnownGenericTypeName(t.alias.symbol.NameValue)))
        {
            var symbol2 = this.getGlobalSymbol(t.alias.symbol.NameValue, SymbolFlags.Type, null);
            if (((symbol2 != null) && (symbol2 == t.alias.symbol)))
            {
                return Core.FirstOrNil(t.alias.typeArguments);
            }
        }

        return null;
    }

    // Gets all symbols for one property. Does not get symbols for every property.
    // Upstream internal/checker/services.go:1024:1.
    public Slice<Symbol> GetPropertySymbolsFromContextualType(Node node, Type contextualType, bool unionSymbolOk)
    {
        var name = Ast.TryGetTextValueOfPropertyName(node.Name()).Item1;
        if (name.Length == 0)
        {
            return default;
        }

        if (((contextualType.flags & TypeFlags.Union) == 0))
        {
            {
                var symbol = this.getPropertyOfType(contextualType, name);
                if ((symbol != null))
                {
                    return Slice<Symbol>.FromArray([symbol]);
                }
            }

            return default;
        }

        var filteredTypes = contextualType.Types();
        if ((Ast.IsObjectLiteralExpression(node.Parent) || Ast.IsJsxAttributes(node.Parent)))
        {
            filteredTypes = Core.Filter(filteredTypes, (t) =>
            {
                return !this.IsTypeInvalidDueToUnionDiscriminant(t, node.Parent);
            });
        }

        var discriminatedPropertySymbols = Core.MapNonNilSlice(filteredTypes, (t2) =>
        {
            return this.getPropertyOfType(t2, name);
        });
        if ((unionSymbolOk && (((GoRuntime.Len(discriminatedPropertySymbols) == 0) || (GoRuntime.Len(discriminatedPropertySymbols) == GoRuntime.Len(contextualType.Types()))))))
        {
            {
                var symbol2 = this.getPropertyOfType(contextualType, name);
                if ((symbol2 != null))
                {
                    return Slice<Symbol>.FromArray([symbol2]);
                }
            }
        }

        if (((GoRuntime.Len(filteredTypes) == 0) && (GoRuntime.Len(discriminatedPropertySymbols) == 0)))
        {
            return Core.MapNonNilSlice(contextualType.Types(), (t3) =>
            {
                return this.getPropertyOfType(t3, name);
            });
        }

        return Core.Deduplicate(discriminatedPropertySymbols);
    }

    // Gets the property symbol corresponding to the property in destructuring assignment
    // 'property1' from
    //
    //	for ( { property1: a } of elems) {
    //	}
    //
    // 'property1' at location 'a' from:
    //
    //	[a] = [ property1, property2 ]
    // Upstream internal/checker/services.go:1069:1.
    public Symbol GetPropertySymbolOfDestructuringAssignment(Node location)
    {
        if (Ast.IsArrayLiteralOrObjectLiteralDestructuringPattern(location.Parent.Parent))
        {
            {
                var typeOfObjectLiteral = this.getTypeOfAssignmentPattern(location.Parent.Parent);
                if ((typeOfObjectLiteral != null))
                {
                    return this.getPropertyOfType(typeOfObjectLiteral, location.TextValue());
                }
            }
        }

        return null;
    }

    // Gets the type of object literal or array literal of destructuring assignment.
    // { a } from
    //
    //	for ( { a } of elems) {
    //	}
    //
    // [ a ] from
    //
    //	[a] = [ some array ...]
    // Upstream internal/checker/services.go:1088:1.
    private Type getTypeOfAssignmentPattern(Node expr)
    {
        if (Ast.IsForOfStatement(expr.Parent))
        {
            var iteratedType = this.checkRightHandSideOfForOf(expr.Parent);
            return this.checkDestructuringAssignment(expr, Core.OrElse(iteratedType, this.errorType), CheckMode.Normal, false);
        }

        if (Ast.IsBinaryExpression(expr.Parent))
        {
            var iteratedType2 = this.getTypeOfExpression(expr.Parent.AsBinaryExpression().Right);
            return this.checkDestructuringAssignment(expr, Core.OrElse(iteratedType2, this.errorType), CheckMode.Normal, false);
        }

        if (Ast.IsPropertyAssignment(expr.Parent))
        {
            var node = expr.Parent.Parent;
            var typeOfParentObjectLiteral = Core.OrElse(this.getTypeOfAssignmentPattern(node), this.errorType);
            var propertyIndex = GoSlices.Index(node.Properties(), expr.Parent);
            return this.checkObjectLiteralDestructuringPropertyAssignment(node, typeOfParentObjectLiteral, propertyIndex, null, false);
        }

        var node2 = expr.Parent;
        var typeOfArrayLiteral = Core.OrElse(this.getTypeOfAssignmentPattern(node2), this.errorType);
        var elementType = Core.OrElse(this.checkIteratedTypeOrElementType(IterationUse.Destructuring, typeOfArrayLiteral, this.undefinedType, expr.Parent), this.errorType);
        return this.checkArrayLiteralDestructuringElementAssignment(node2, typeOfArrayLiteral, GoSlices.Index(node2.Elements(), expr), elementType, CheckMode.Normal);
    }

    // Upstream internal/checker/services.go:1118:1.
    public Signature GetSignatureFromDeclaration(Node node)
    {
        return this.getSignatureFromDeclaration(node);
    }

    // IsLibSymbolForHoverVerbosity returns true if a symbol is declared in a lib file.
    // Upstream internal/checker/services.go:1123:1.
    public bool IsLibSymbolForHoverVerbosity(Symbol symbol)
    {
        if ((symbol == null))
        {
            return false;
        }

        {
            foreach (var decl in symbol.Declarations)
            {
                var sf = Ast.GetSourceFileOfNode(decl);
                if (((sf != null) && this.program.IsSourceFileDefaultLibrary(sf)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // IsLibTypeForHoverVerbosity returns true if a type is declared in a lib file.
    // Don't expand types like Array or Promise, instead treating them as opaque.
    // Upstream internal/checker/services.go:1138:1.
    public bool IsLibTypeForHoverVerbosity(Type t)
    {
        Symbol symbol = default;
        if (((t.objectFlags & ObjectFlags.Reference) != 0))
        {
            symbol = t.Target().Symbol();
        }
        else
        {
            symbol = t.Symbol();
        }

        if (this.IsLibSymbolForHoverVerbosity(symbol))
        {
            return true;
        }

        return isTupleType(t);
    }

    // Upstream internal/checker/services.go:354. Local Go result struct becomes a value tuple.
    public static (Signature Signature, Slice<Signature> Candidates) GetResolvedSignatureForSignatureHelp(Node node, int argumentCount, Checker checker) => checker.runWithoutResolvedSignatureCaching(node, () => checker.getResolvedSignatureWorker(node, CheckMode.IsForSignatureHelp, argumentCount));
    // Upstream internal/checker/utilities.go:1681. Original arguments-object introducing kinds.
    private static bool introducesArgumentsExoticObject(Node node) => node.Kind is Kind.MethodDeclaration or Kind.MethodSignature or Kind.Constructor or Kind.GetAccessor or Kind.SetAccessor or Kind.FunctionDeclaration or Kind.FunctionExpression;
    // Upstream internal/checker/checker.go:9856. Existing rest worker retains tuple/index ownership.
    private Type getRestTypeOfSignature(Signature signature) => tryGetRestTypeOfSignature(signature) ?? anyType;
}

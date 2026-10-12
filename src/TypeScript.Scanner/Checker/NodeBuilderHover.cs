// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned nodebuilder_hover.go request workers; ordinary emit remains disabled by default.
#nullable disable
using TypeScript.Scanner;
using NodeBuilderFlags = TypeScript.NodeBuilding.Flags;
using Scanner = TypeScript.Scanner.Scanner;

namespace TypeScript.Checking;
internal sealed partial class NodeBuilderImpl
{
    private record struct HoverStatement(Node node = null, bool isLocal = false);
    // isExpanding returns whether the node builder context is operating in hover-expansion mode.
    // Upstream internal/checker/nodebuilder_hover.go:16:1.
    private static bool isExpanding(NodeBuilderContext ctx)
    {
        return (ctx.maxExpansionDepth != -1);
    }

    // expandSymbolForHover produces declaration nodes (class, interface, enum, module) for a symbol
    // for expandable hover. This is a focused alternative to the full symbolTableToDeclarationStatements
    // machinery used by declaration emit — it directly builds the declaration nodes hover needs
    // without the declaration-emit scaffolding (deferred privates, symbol name remapping, export
    // modifier computation, alias resolution, visited symbols tracking).
    // Upstream internal/checker/nodebuilder_hover.go:25:1.
    internal Slice<Node> expandSymbolForHover(Symbol symbol)
    {
        Slice<Node> results = default;
        if (((symbol.Flags & SymbolFlags.Enum) != 0))
        {
            {
                var node = this.expandEnumDecl(symbol);
                if ((node != null))
                {
                    results = results.Append(node);
                }
            }
        }

        if (((symbol.Flags & SymbolFlags.Class) != 0))
        {
            {
                var node2 = this.expandClassDecl(symbol);
                if ((node2 != null))
                {
                    results = results.Append(node2);
                }
            }
        }

        if (((symbol.Flags & ((SymbolFlags.ValueModule | SymbolFlags.NamespaceModule))) != 0))
        {
            {
                var node3 = this.expandModuleDecl(symbol);
                if ((node3 != null))
                {
                    results = results.Append(node3);
                }
            }
        }

        if ((((symbol.Flags & SymbolFlags.Interface) != 0) && ((symbol.Flags & SymbolFlags.Class) == 0)))
        {
            {
                var node4 = this.expandInterfaceDecl(symbol);
                if ((node4 != null))
                {
                    results = results.Append(node4);
                }
            }
        }

        return results;
    }

    // expandEnumDecl produces an EnumDeclaration node with all members.
    // Upstream internal/checker/nodebuilder_hover.go:52:1.
    private Node expandEnumDecl(Symbol symbol)
    {
        var name = Ast.SymbolNameValue(symbol);
        this.ctx.approximateLength += (9 + GoRuntime.Len(name));
        var memberProps = Core.Filter(this.ch.getPropertiesOfType(this.ch.getTypeOfSymbol(symbol)), (p) =>
        {
            return ((p.Flags & SymbolFlags.EnumMember) != 0);
        });
        Slice<Node> members = default;
        {
            var range1Items = memberProps;
            for (int range1Index = 0; range1Index < range1Items.Count; range1Index++)
            {
                var i = range1Index;
                var p2 = range1Items[range1Index];
                if ((this.checkTruncationLengthIfExpanding() && ((i + 3) < (GoRuntime.Len(memberProps) - 1))))
                {
                    this.ctx.expansionTruncated = true;
                    members = members.Append(this.f.NewEnumMember(this.f.NewStringLiteral($" ... {memberProps.Count - i - 1} more ... ", 0), null));
                    var last = memberProps[(GoRuntime.Len(memberProps) - 1)];
                    members = members.Append(this.f.NewEnumMember(this.f.NewIdentifierWithTextValue(last.NameValue), this.enumMemberInitializer(last)));
                    break;
                }

                var memberDecl = Core.Find(p2.Declarations, Ast.IsEnumMember);
                Node initializer = default;
                if (((memberDecl != null) && (memberDecl.AsEnumMember().Initializer != null)))
                {
                    initializer = this.f.DeepCloneNode(memberDecl.AsEnumMember().Initializer);
                }
                else
                {
                    initializer = this.enumMemberInitializer(p2);
                }

                this.ctx.approximateLength += (4 + p2.NameValue.Length);
                if ((initializer != null))
                {
                    this.ctx.approximateLength += 5;
                }

                members = members.Append(this.f.NewEnumMember(this.f.NewIdentifierWithTextValue(p2.NameValue), initializer));
            }
        }

        var constModifier = ModifierFlags.None;
        if (Checker.isConstEnumSymbol(symbol))
        {
            constModifier = ModifierFlags.Const;
        }

        ModifierList mods = default;
        if ((constModifier != 0))
        {
            mods = this.f.NewModifierList(Ast.CreateModifiersFromModifierFlags(constModifier, this.f.NewModifier));
        }

        return this.f.NewEnumDeclaration(mods, this.f.NewIdentifierWithTextValue(name), this.f.NewNodeList(members));
    }

    // Upstream internal/checker/nodebuilder_hover.go:111:1.
    private Node expandClassDecl(Symbol symbol)
    {
        var name = Ast.SymbolNameValue(symbol);
        this.ctx.approximateLength += (9 + GoRuntime.Len(name));
        var classLikeDeclarations = Core.Filter(symbol.Declarations, Ast.IsClassLike);
        var originalDecl = Core.FirstOrNil(classLikeDeclarations);
        var oldEnclosing = this.ctx.enclosingDeclaration;
        if ((originalDecl != null))
        {
            this.ctx.enclosingDeclaration = originalDecl;
        }

        try
        {
            var localParams = this.ch.getLocalTypeParametersOfClassOrInterfaceOrTypeAlias(symbol);
            var typeParamDecls = Core.Map(localParams, (p) =>
            {
                return this.typeParameterToDeclaration(p);
            });
            var declaredType = this.ch.getDeclaredTypeOfClassOrInterface(symbol);
            var classType = this.ch.getTypeWithThisArgument(declaredType, null, false);
            var baseTypes = this.ch.getBaseTypes(Checker.getTargetType(classType));
            var staticType = this.ch.getTypeOfSymbol(symbol);
            var isClass = (((staticType.symbol != null) && (staticType.symbol.ValueDeclaration != null)) && Ast.IsClassLike(staticType.symbol.ValueDeclaration));
            Type staticBaseType = default;
            if (isClass)
            {
                staticBaseType = this.ch.getBaseConstructorTypeOfClass(declaredType);
            }
            else
            {
                staticBaseType = this.ch.anyType;
            }

            var heritageClauses = this.hoverHeritageClauses(classLikeDeclarations);
            var allProps = this.ch.getPropertiesOfType(classType);
            var symbolProps = this.filterInheritedProperties(classType, baseTypes, allProps);
            var publicProps = Core.Filter(symbolProps, (s) =>
            {
                return !isHashPrivate(s);
            });
            var hasPrivate = Core.Some(symbolProps, isHashPrivate);
            Slice<Node> instanceMembers = default;
            instanceMembers = this.serializePropertiesWithTruncation(publicProps, instanceMembers);
            instanceMembers = typeElementsToClassElements(this.f, instanceMembers);
            instanceMembers = this.addClassModifiers(instanceMembers, false);
            var staticProps = Core.Filter(this.ch.getPropertiesOfType(staticType), (p2) =>
            {
                return ((((p2.Flags & SymbolFlags.Prototype) == 0) && (!p2.NameValue.EqualsString("prototype"))) && !this.isNamespaceMember(p2));
            });
            Slice<Node> staticMembers = default;
            staticMembers = this.serializePropertiesWithTruncation(staticProps, staticMembers);
            staticMembers = typeElementsToClassElements(this.f, staticMembers);
            staticMembers = this.addClassModifiers(staticMembers, true);
            Slice<Node> privateMembers = default;
            if (hasPrivate)
            {
                privateMembers = this.serializePropertiesWithTruncation(Core.Filter(symbolProps, isHashPrivate), privateMembers);
                privateMembers = typeElementsToClassElements(this.f, privateMembers);
            }

            var constructors = this.serializeConstructors(staticType, staticBaseType, isClass, symbol);
            var indexSigs = this.serializeIndexSignaturesOfType(classType, Core.FirstOrNil(baseTypes));
            var allMembers = Slice<Node>.Make(0);
            allMembers = allMembers.Append(indexSigs);
            allMembers = allMembers.Append(staticMembers);
            allMembers = allMembers.Append(constructors);
            allMembers = allMembers.Append(instanceMembers);
            allMembers = allMembers.Append(privateMembers);
            return this.f.NewClassDeclaration(null, this.f.NewIdentifierWithTextValue(name), this.f.NewNodeList(typeParamDecls), this.f.NewNodeList(heritageClauses), this.f.NewNodeList(allMembers));
        }
        finally
        {
            this.ctx.enclosingDeclaration = oldEnclosing;
        }
    }

    // addClassModifiers post-processes class member nodes to add class-specific modifiers
    // (private, protected, public, abstract, static) based on the original symbol declarations.
    // Upstream internal/checker/nodebuilder_hover.go:187:1.
    private Slice<Node> addClassModifiers(Slice<Node> members, bool isStatic)
    {
        {
            var range2Items = members;
            for (int range2Index = 0; range2Index < range2Items.Count; range2Index++)
            {
                var i = range2Index;
                var m = range2Items[range2Index];
                Symbol memberSymbol = default;
                var memberName = m.Name();
                if ((memberName != null))
                {
                    {
                        var ok = this.idToSymbol.TryGetValue(memberName, out var sym);
                        if (ok)
                        {
                            memberSymbol = sym;
                        }
                    }
                }

                if ((memberSymbol == null))
                {
                    continue;
                }

                var modFlags = (Checker.getDeclarationModifierFlagsFromSymbol(memberSymbol) & ~ModifierFlags.Async);
                if (isStatic)
                {
                    modFlags |= ModifierFlags.Static;
                }

                if (((modFlags != 0) && Ast.CanHaveModifiers(m)))
                {
                    var existing = m.ModifierFlags();
                    if ((modFlags != existing))
                    {
                        members[i] = Ast.ReplaceModifiers(this.f, m, this.f.NewModifierList(Ast.CreateModifiersFromModifierFlags((modFlags | existing), this.f.NewModifier)));
                    }
                }
            }
        }

        return members;
    }

    // typeElementsToClassElements converts TypeElement nodes (PropertySignature, MethodSignature)
    // to their ClassElement equivalents (PropertyDeclaration, MethodDeclaration) so they can be
    // used as members of a ClassDeclaration. Nodes that are already ClassElements pass through unchanged.
    // Upstream internal/checker/nodebuilder_hover.go:217:1.
    private static Slice<Node> typeElementsToClassElements(TypeScript.Scanner.NodeFactory f, Slice<Node> members)
    {
        {
            var range3Items = members;
            for (int range3Index = 0; range3Index < range3Items.Count; range3Index++)
            {
                var i = range3Index;
                var m = range3Items[range3Index];
                switch (m.Kind)
                {
                    case Kind.PropertySignature:
                    {
                        var ps = m.AsPropertySignatureDeclaration();
                        members[i] = f.NewPropertyDeclaration(m.Modifiers(), ps.Name(), ps.QuestionToken(), ps.Type, null);
                        break;
                    }

                    case Kind.MethodSignature:
                    {
                        var ms = m.AsMethodSignatureDeclaration();
                        members[i] = f.NewMethodDeclaration(m.Modifiers(), null, ms.Name(), ms.QuestionToken(), ms.TypeParameters, ms.Parameters, ms.Type, null, null);
                        break;
                    }
                }
            }
        }

        return members;
    }

    // expandInterfaceDecl produces an InterfaceDeclaration with members.
    // Reuses addPropertyToElementList for property serialization and
    // signatureToSignatureDeclarationHelper for signatures.
    // Upstream internal/checker/nodebuilder_hover.go:234:1.
    private Node expandInterfaceDecl(Symbol symbol)
    {
        var name = Ast.SymbolNameValue(symbol);
        this.ctx.approximateLength += (14 + GoRuntime.Len(name));
        var interfaceType = this.ch.getDeclaredTypeOfClassOrInterface(symbol);
        var interfaceDeclarations = Core.Filter(symbol.Declarations, Ast.IsInterfaceDeclaration);
        var localParams = this.ch.getLocalTypeParametersOfClassOrInterfaceOrTypeAlias(symbol);
        var typeParamDecls = Core.Map(localParams, (p) =>
        {
            return this.typeParameterToDeclaration(p);
        });
        var baseTypes = this.ch.getBaseTypes(interfaceType);
        Type baseType = default;
        if ((GoRuntime.Len(baseTypes) > 0))
        {
            baseType = this.ch.getIntersectionType(baseTypes);
        }

        var resolved = this.ch.resolveStructuredTypeMembers(interfaceType);
        Slice<Node> members = default;
        members = members.Append(this.serializeIndexSignaturesOfType(interfaceType, baseType));
        {
            foreach (var sig in resolved.ConstructSignaturesSlice())
            {
                if (((sig.flags & SignatureFlags.Abstract) != 0))
                {
                    continue;
                }

                members = members.Append(this.signatureToSignatureDeclarationHelper(sig, Kind.ConstructSignature, null));
            }
        }

        {
            var range5Items = resolved.CallSignaturesSlice();
            for (int range5Index = 0; range5Index < range5Items.Count; range5Index++)
            {
                var _2 = range5Index;
                var sig2 = range5Items[range5Index];
                members = members.Append(this.signatureToSignatureDeclarationHelper(sig2, Kind.CallSignature, null));
            }
        }

        var filteredProps = this.filterInheritedProperties(interfaceType, baseTypes, resolved.properties);
        members = this.serializePropertiesWithTruncation(filteredProps, members);
        var heritageClauses = this.hoverHeritageClauses(interfaceDeclarations);
        return this.f.NewInterfaceDeclaration(null, this.f.NewIdentifierWithTextValue(name), this.f.NewNodeList(typeParamDecls), this.f.NewNodeList(heritageClauses), this.f.NewNodeList(members));
    }

    // Upstream internal/checker/nodebuilder_hover.go:275:1.
    private Slice<Node> hoverHeritageClauses(Slice<Node> declarations)
    {
        Slice<Node> extendsTypes = default;
        Slice<Node> implementsTypes = default;
        {
            foreach (var declaration in declarations)
            {
                {
                    var range7Items = Ast.GetExtendsHeritageClauseElements(declaration);
                    for (int range7Index = 0; range7Index < range7Items.Count; range7Index++)
                    {
                        var _2 = range7Index;
                        var heritageElement = range7Items[range7Index];
                        extendsTypes = extendsTypes.Append(this.f.DeepCloneNode(heritageElement.AsNode()));
                    }
                }

                {
                    var range8Items = Ast.GetImplementsHeritageClauseElements(declaration);
                    for (int range8Index = 0; range8Index < range8Items.Count; range8Index++)
                    {
                        var _3 = range8Index;
                        var heritageElement2 = range8Items[range8Index];
                        implementsTypes = implementsTypes.Append(this.f.DeepCloneNode(heritageElement2.AsNode()));
                    }
                }
            }
        }

        Slice<Node> heritageClauses = default;
        if ((GoRuntime.Len(extendsTypes) > 0))
        {
            heritageClauses = heritageClauses.Append(this.f.NewHeritageClause(Kind.ExtendsKeyword, this.f.NewNodeList(extendsTypes)));
        }

        if ((GoRuntime.Len(implementsTypes) > 0))
        {
            heritageClauses = heritageClauses.Append(this.f.NewHeritageClause(Kind.ImplementsKeyword, this.f.NewNodeList(implementsTypes)));
        }

        return heritageClauses;
    }

    // serializePropertiesWithTruncation iterates properties using addPropertyToElementList,
    // with truncation checks matching Strada's createTypeNodesFromResolvedType behavior.
    // Upstream internal/checker/nodebuilder_hover.go:299:1.
    private Slice<Node> serializePropertiesWithTruncation(Slice<Symbol> properties, Slice<Node> elements)
    {
        properties = Core.Filter(properties, (p) =>
        {
            return ((p.Flags & SymbolFlags.Prototype) == 0);
        });
        {
            var range9Items = properties;
            for (int range9Index = 0; range9Index < range9Items.Count; range9Index++)
            {
                var i = range9Index;
                var p2 = range9Items[range9Index];
                if ((this.checkTruncationLengthIfExpanding() && (((i + 3) < (GoRuntime.Len(properties) - 1)))))
                {
                    this.ctx.expansionTruncated = true;
                    var text = $"... {properties.Count - i - 1} more ...";
                    elements = elements.Append(this.f.NewPropertySignatureDeclaration(null, this.f.NewIdentifier(text), null, null, null));
                    elements = this.addPropertyToElementList(properties[(GoRuntime.Len(properties) - 1)], elements);
                    break;
                }

                elements = this.addPropertyToElementList(p2, elements);
            }
        }

        return elements;
    }

    // serializeConstructors builds constructor signature(s) for a class, with base type filtering.
    // Upstream internal/checker/nodebuilder_hover.go:317:1.
    private Slice<Node> serializeConstructors(Type staticType, Type staticBaseType, bool isClass, Symbol symbol)
    {
        var isNonConstructable = (((!isClass && (symbol.ValueDeclaration != null)) && Ast.IsInJSFile(symbol.ValueDeclaration)) && (GoRuntime.Len(this.ch.getSignaturesOfType(staticType, SignatureKind.Construct)) == 0));
        if (isNonConstructable)
        {
            this.ctx.approximateLength += 21;
            var modifiers = Ast.CreateModifiersFromModifierFlags(ModifierFlags.Private, this.f.NewModifier);
            return Slice<Node>.FromArray([this.f.NewConstructorDeclaration(this.f.NewModifierList(modifiers), null, this.f.NewNodeList(default(Slice<Node>)), null, null, null)]);
        }

        var signatures = this.ch.getSignaturesOfType(staticType, SignatureKind.Construct);
        if ((staticBaseType != null))
        {
            var baseSigs = this.ch.getSignaturesOfType(staticBaseType, SignatureKind.Construct);
            if (((GoRuntime.Len(baseSigs) == 0) && Core.Every(signatures, (sig) =>
            {
                return (GoRuntime.Len(sig.parameters) == 0);
            })))
            {
                return default;
            }

            if ((GoRuntime.Len(baseSigs) == GoRuntime.Len(signatures)))
            {
                var allMatch = true;
                {
                    for (int i = 0; i < baseSigs.Count; i++)
                    {
                        if ((this.ch.compareSignaturesIdentical(signatures[i], baseSigs[i], false, false, true, this.ch.compareTypesIdentical) != Ternary.True))
                        {
                            allMatch = false;
                            break;
                        }
                    }
                }

                if (allMatch)
                {
                    return default;
                }
            }

            ModifierFlags privateProtected = default;
            {
                foreach (var sig2 in signatures)
                {
                    if ((sig2.declaration != null))
                    {
                        privateProtected |= (sig2.declaration.ModifierFlags() & ((ModifierFlags.Private | ModifierFlags.Protected)));
                    }
                }
            }

            if ((privateProtected != 0))
            {
                return Slice<Node>.FromArray([this.f.NewConstructorDeclaration(this.f.NewModifierList(Ast.CreateModifiersFromModifierFlags(privateProtected, this.f.NewModifier)), null, this.f.NewNodeList(default(Slice<Node>)), null, null, null)]);
            }
        }
        else
        {
            if (Core.Every(signatures, (sig3) =>
            {
                return (GoRuntime.Len(sig3.parameters) == 0);
            }))
            {
                return default;
            }
        }

        Slice<Node> result = default;
        {
            var range12Items = signatures;
            for (int range12Index = 0; range12Index < range12Items.Count; range12Index++)
            {
                var _2 = range12Index;
                var sig4 = range12Items[range12Index];
                this.ctx.approximateLength++;
                result = result.Append(this.signatureToSignatureDeclarationHelper(sig4, Kind.Constructor, null));
            }
        }

        return result;
    }

    // serializeIndexSignaturesOfType builds index signature declarations, filtering those identical to baseType.
    // Upstream internal/checker/nodebuilder_hover.go:369:1.
    private Slice<Node> serializeIndexSignaturesOfType(Type input, Type baseType)
    {
        Slice<Node> result = default;
        {
            foreach (var info in this.ch.getIndexInfosOfType(input))
            {
                if ((baseType != null))
                {
                    var baseInfo = this.ch.getIndexInfoOfType(baseType, info.keyType);
                    if (((baseInfo != null) && this.ch.isTypeIdenticalTo(info.valueType, baseInfo.valueType)))
                    {
                        continue;
                    }
                }

                result = result.Append(this.indexInfoToIndexSignatureDeclarationHelper(info, null));
            }
        }

        return result;
    }

    // serializeNamespaceMember produces the appropriate declaration node for a namespace member
    // based on its symbol flags (type alias, enum, class, interface, nested namespace, or variable).
    // Upstream internal/checker/nodebuilder_hover.go:385:1.
    private Node serializeNamespaceMember(Symbol resolved, ByteStringValue name)
    {
        switch (true)
        {
            case true when ((resolved.Flags & SymbolFlags.TypeAlias) != 0):
            {
                return this.serializeTypeAliasForNamespace(resolved, name);
            }

            case true when ((resolved.Flags & SymbolFlags.Enum) != 0):
            {
                return this.expandEnumDecl(resolved);
            }

            case true when ((resolved.Flags & SymbolFlags.Class) != 0):
            {
                return this.expandClassDecl(resolved);
            }

            case true when ((resolved.Flags & SymbolFlags.Interface) != 0):
            {
                return this.expandInterfaceDecl(resolved);
            }

            case true when ((resolved.Flags & ((SymbolFlags.ValueModule | SymbolFlags.NamespaceModule))) != 0):
            {
                return this.expandModuleDecl(resolved);
            }

            default:
            {
                var t = this.ch.getWidenedType(this.ch.getTypeOfSymbol(resolved));
                this.ctx.approximateLength += (GoRuntime.Len(name) + 5);
                return this.f.NewVariableStatement(null, this.f.NewVariableDeclarationList(this.f.NewNodeList(Slice<Node>.FromArray([this.f.NewVariableDeclaration(this.f.NewIdentifierWithTextValue(name), null, this.serializeTypeForDeclaration(null, t, resolved, true), null)])), NodeFlags.Let));
            }
        }
    }

    // expandModuleDecl produces a ModuleDeclaration with exported members.
    // Upstream internal/checker/nodebuilder_hover.go:413:1.
    private Node expandModuleDecl(Symbol symbol)
    {
        var exports = this.ch.getExportsOfSymbol(symbol);
        Slice<Symbol> members = default;
        {
            if (exports != null)
                foreach (var sym in exports.Values)
                {
                    if (!this.isNamespaceMember(sym))
                    {
                        continue;
                    }

                    if (!TypeScript.Scanner.Scanner.IsIdentifierTextByteString(sym.NameValue, LanguageVariant.Standard))
                    {
                        continue;
                    }

                    members = members.Append(sym);
                }
        }

        this.ch.sortSymbols(members);
        this.ctx.approximateLength += 14;
        var oldFlags = this.ctx.flags;
        try
        {
            this.ctx.flags |= (NodeBuilderFlags.WriteTypeParametersInQualifiedName | (NodeBuilderFlags)SymbolFormatFlags.UseOnlyExternalAliasing);
            var localName = this.symbolToNode(symbol, SymbolFlags.All);
            this.ctx.flags = oldFlags;
            Slice<HoverStatement> bodyStmts = default;
            HashSet<Symbol> emittedLocals = default;
            {
                var i = 0;
                for (; (i < GoRuntime.Len(members)); i++)
                {
                    var m = members[i];
                    if ((this.checkTruncationLengthIfExpanding() && ((i + 3) < (GoRuntime.Len(members) - 1))))
                    {
                        this.ctx.expansionTruncated = true;
                        bodyStmts = bodyStmts.Append(new HoverStatement { node = this.f.NewExpressionStatement(this.f.NewIdentifier($"... ({members.Count - i - 1} more) ...")) });
                        i = (GoRuntime.Len(members) - 2);
                        continue;
                    }

                    if (((m.Flags & SymbolFlags.Alias) != 0))
                    {
                        var aliasDecl = this.ch.getDeclarationOfAliasSymbol(m);
                        var target = this.ch.getMergedSymbol(this.ch.getTargetOfAliasDeclaration(aliasDecl));
                        if ((target != null))
                        {
                            if (((target.Flags & (((SymbolFlags.BlockScopedVariable | SymbolFlags.FunctionScopedVariable) | SymbolFlags.Property))) != 0))
                            {
                                if ((emittedLocals ??= new HashSet<Symbol>()).Add(target))
                                {
                                    var localType = this.ch.getWidenedType(this.ch.getTypeOfSymbol(target));
                                    this.ctx.approximateLength += (target.NameValue.Length + 5);
                                    var localStmt = this.f.NewVariableStatement(null, this.f.NewVariableDeclarationList(this.f.NewNodeList(Slice<Node>.FromArray([this.f.NewVariableDeclaration(this.f.NewIdentifierWithTextValue(target.NameValue), null, this.serializeTypeForDeclaration(null, localType, target, true), null)])), NodeFlags.Let));
                                    bodyStmts = bodyStmts.Append(new HoverStatement { node = localStmt, isLocal = true });
                                }
                            }

                            var targetName = target.NameValue;
                            this.ctx.approximateLength += (16 + m.NameValue.Length);
                            Node propertyName = default;
                            if ((!m.NameValue.EqualsValue(targetName)))
                            {
                                propertyName = this.f.NewIdentifierWithTextValue(targetName);
                            }

                            var stmt = this.f.NewExportDeclaration(null, false, this.f.NewNamedExports(this.f.NewNodeList(Slice<Node>.FromArray([this.f.NewExportSpecifier(false, propertyName, this.f.NewIdentifierWithTextValue(m.NameValue))]))), null, null);
                            bodyStmts = bodyStmts.Append(new HoverStatement { node = stmt });
                            continue;
                        }
                    }

                    var resolved = this.ch.resolveSymbol(m);
                    if (((resolved.Flags & ((SymbolFlags.Function | SymbolFlags.Method))) != 0))
                    {
                        var t = this.ch.getTypeOfSymbol(resolved);
                        var sigs = this.ch.getSignaturesOfType(t, SignatureKind.Call);
                        {
                            var range15Items = sigs;
                            for (int range15Index = 0; range15Index < range15Items.Count; range15Index++)
                            {
                                var _2 = range15Index;
                                var sig = range15Items[range15Index];
                                this.ctx.approximateLength++;
                                var decl = this.signatureToSignatureDeclarationHelper(sig, Kind.FunctionDeclaration, new SignatureToSignatureDeclarationOptions { name = this.f.NewIdentifierWithTextValue(m.NameValue) });
                                bodyStmts = bodyStmts.Append(new HoverStatement { node = decl });
                            }
                        }

                        var merged = this.ch.getMergedSymbol(resolved);
                        var hasModuleExports = ((((merged.Flags & ((SymbolFlags.ValueModule | SymbolFlags.NamespaceModule))) != 0) && (merged.Exports != null)) && (GoRuntime.Len(merged.Exports) != 0));
                        if (!hasModuleExports)
                        {
                            bodyStmts = bodyStmts.Append(new HoverStatement { node = this.f.NewModuleDeclaration(null, Kind.NamespaceKeyword, this.f.NewIdentifierWithTextValue(m.NameValue), null, this.f.NewModuleBlock(this.f.NewNodeList(default(Slice<Node>)))) });
                        }

                        continue;
                    }

                    {
                        var node = this.serializeNamespaceMember(resolved, m.NameValue);
                        if ((node != null))
                        {
                            bodyStmts = bodyStmts.Append(new HoverStatement { node = node });
                        }
                    }
                }
            }

            {
                for (int i2 = 0; i2 < bodyStmts.Count; i2++)
                {
                    ref var s = ref bodyStmts.AsSpan()[i2];
                    if ((s.isLocal || Ast.IsExportDeclaration(s.node)))
                    {
                        continue;
                    }

                    if (Ast.CanHaveModifiers(s.node))
                    {
                        var mf = (s.node.ModifierFlags() | ModifierFlags.Export);
                        s.node = Ast.ReplaceModifiers(this.f, s.node, this.f.NewModifierList(Ast.CreateModifiersFromModifierFlags(mf, this.f.NewModifier)));
                    }
                }
            }

            var bodyStatements = Slice<Node>.Make(bodyStmts.Count);
            {
                for (int i3 = 0; i3 < bodyStmts.Count; i3++)
                {
                    bodyStatements[i3] = bodyStmts[i3].node;
                }
            }

            var allExported = ((GoRuntime.Len(bodyStatements) > 0) && Core.Every(bodyStatements, (d) =>
            {
                return Ast.HasSyntacticModifier(d, ModifierFlags.Export);
            }));
            if (allExported)
            {
                {
                    var range18Items = bodyStatements;
                    for (int range18Index = 0; range18Index < range18Items.Count; range18Index++)
                    {
                        var i4 = range18Index;
                        var stmt2 = range18Items[range18Index];
                        if (Ast.CanHaveModifiers(stmt2))
                        {
                            var mf2 = (stmt2.ModifierFlags() & ~ModifierFlags.Export);
                            bodyStatements[i4] = Ast.ReplaceModifiers(this.f, stmt2, this.f.NewModifierList(Ast.CreateModifiersFromModifierFlags(mf2, this.f.NewModifier)));
                        }
                    }
                }
            }

            var keyword = Kind.NamespaceKeyword;
            if (!Ast.IsIdentifier(localName))
            {
                keyword = Kind.ModuleKeyword;
            }

            Node attributes = default;
            {
                var declaration = Core.Find(symbol.Declarations, (declaration2) =>
                {
                    return (Ast.IsModuleDeclaration(declaration2) && (declaration2.AsModuleDeclaration().Attributes != null));
                });
                if ((declaration != null))
                {
                    attributes = this.f.DeepCloneNode(declaration.AsModuleDeclaration().Attributes);
                    this.e.SetEmitFlags(attributes, TypeScript.DiagnosticPrinting.EmitFlags.SingleLine);
                }
            }

            return this.f.NewModuleDeclaration(null, keyword, localName, attributes, this.f.NewModuleBlock(this.f.NewNodeList(bodyStatements)));
        }
        finally
        {
            this.ctx.flags = oldFlags;
        }
    }

    // serializeTypeAliasForNamespace produces a TypeAliasDeclaration for a type alias inside a namespace body.
    // Upstream internal/checker/nodebuilder_hover.go:558:1.
    private Node serializeTypeAliasForNamespace(Symbol symbol, ByteStringValue name)
    {
        var aliasType = this.ch.getDeclaredTypeOfTypeAlias(symbol);
        var typeParams = this.ch.getLocalTypeParametersOfClassOrInterfaceOrTypeAlias(symbol);
        var typeParamDecls = Core.Map(typeParams, (p) =>
        {
            return this.typeParameterToDeclaration(p);
        });
        var restoreFlags = this.saveRestoreFlags();
        this.ctx.flags |= NodeBuilderFlags.InTypeAlias;
        var typeNode = this.typeToTypeNode(aliasType);
        restoreFlags();
        this.ctx.approximateLength += (8 + GoRuntime.Len(name));
        return this.f.NewTypeAliasDeclaration(null, this.f.NewIdentifierWithTextValue(name), this.f.NewNodeList(typeParamDecls), typeNode);
    }

    // filterInheritedProperties removes properties already present in base types.
    // Upstream internal/checker/nodebuilder_hover.go:571:1.
    private Slice<Symbol> filterInheritedProperties(Type t, Slice<Type> baseTypes, Slice<Symbol> properties)
    {
        if ((GoRuntime.Len(baseTypes) == 0))
        {
            return properties;
        }

        var propsByName = new Dictionary<ByteStringKey, Symbol>(GoRuntime.Len(properties));
        {
            foreach (var p in properties)
            {
                propsByName[ByteStringKey.FromValue(p.NameValue)] = p;
            }
        }

        HashSet<ByteStringKey> inherited = default;
        {
            foreach (var range20 in GoRuntime.Range(baseTypes))
            {
                var _2 = range20.Key;
                var @base = range20.Value;
                var baseWithThis = this.ch.getTypeWithThisArgument(@base, Checker.getTargetType(t).AsInterfaceType().thisType, false);
                {
                    var range21Items = this.ch.getPropertiesOfType(baseWithThis);
                    for (int range21Index = 0; range21Index < range21Items.Count; range21Index++)
                    {
                        var _3 = range21Index;
                        var prop = range21Items[range21Index];
                        {
                            var ok = propsByName.TryGetValue(ByteStringKey.FromValue(prop.NameValue), out var existing);
                            if ((ok && (prop.Parent == existing.Parent)))
                            {
                                (inherited ??= new HashSet<ByteStringKey>()).Add(ByteStringKey.FromValue(prop.NameValue));
                            }
                        }
                    }
                }
            }
        }

        if ((inherited == null || inherited.Count == 0))
        {
            return properties;
        }

        return Core.Filter(properties, (p2) =>
        {
            return !inherited.Contains(ByteStringKey.FromValue(p2.NameValue));
        });
    }

    // Upstream internal/checker/nodebuilder_hover.go:598:1.
    private bool isNamespaceMember(Symbol p)
    {
        return (((p.Flags & (((SymbolFlags.Type | SymbolFlags.Namespace) | SymbolFlags.Alias))) != 0) || !(((((p.Flags & SymbolFlags.Prototype) != 0) || (p.NameValue.EqualsString("prototype"))) || ((((p.ValueDeclaration != null) && Ast.HasStaticModifier(p.ValueDeclaration)) && Ast.IsClassLike(p.ValueDeclaration.Parent))))));
    }

    // Upstream internal/checker/nodebuilder_hover.go:603:1.
    private static bool isHashPrivate(Symbol s)
    {
        return (((s.ValueDeclaration != null) && (s.ValueDeclaration.Name() != null)) && Ast.IsPrivateIdentifier(s.ValueDeclaration.Name()));
    }

    // Upstream internal/checker/nodebuilder_hover.go:92. Go dynamic constant values retain their runtime kind.
    private Node enumMemberInitializer(Symbol symbol)
    {
        var declaration = Core.Find(symbol.Declarations, Ast.IsEnumMember);
        if (declaration is null)
            return default;
        return ch.GetConstantValue(declaration) switch
        {
            string text => f.NewStringLiteral(text, 0),
            ByteStringValue text => f.NewStringLiteralWithTextValue(text, 0),
            JsNum.Number number => f.NewNumericLiteral(number.String(), 0),
            _ => null
        };
    }

    // Upstream internal/checker/nodebuilderimpl.go:165/174/198. Configured expansion request helpers.
    private bool checkTruncationLengthIfExpanding()
    {
        if (ctx.maxExpansionDepth >= 0 && checkTruncationLength())
        {
            ctx.expansionTruncated = true;
            return true;
        }

        return false;
    }

    private bool isExpandableType(Type type, bool isAlias)
    {
        if (isAlias)
            return !ch.IsLibSymbolForHoverVerbosity(type.alias.Symbol());
        if (ch.IsLibTypeForHoverVerbosity(type))
            return false;
        if ((type.flags & TypeFlags.EnumLike) != 0 || (type.objectFlags & (ObjectFlags.Reference | ObjectFlags.ClassOrInterface)) != 0)
            return true;
        return (type.objectFlags & ObjectFlags.Anonymous) != 0 && type.symbol is not null && (type.symbol.Flags & (SymbolFlags.Class | SymbolFlags.Enum | SymbolFlags.ValueModule | SymbolFlags.Function | SymbolFlags.Method)) != 0;
    }

    private bool isTypeOnStack(Type type)
    {
        for (int i = 0; i < ctx.typeStack.Count - 1; i++)
            if (ctx.typeStack[i] == type)
                return true;
        return false;
    }
}

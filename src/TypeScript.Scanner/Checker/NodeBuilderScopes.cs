// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/nodebuilderscopes.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Diagnostic-only finite data-property, scalar and non-generic callable context reuse; general emit excluded.
#nullable disable
using TypeScript.Scanner;
using TypeScript.PseudoChecking;

namespace TypeScript.Checking;
internal sealed partial class NodeBuilderImpl
{
    // nodebuilderscopes.go:40. Preserve both old-value and missing-entry cases.
    private Action addSymbolTypeToContext(Symbol symbol, Type t)
    {
        var id = Ast.GetSymbolId(symbol);
        bool exists = ctx.enclosingSymbolTypes.TryGetValue(id, out var old);
        ctx.enclosingSymbolTypes[id] = t;
        return () =>
        {
            if (exists)
                ctx.enclosingSymbolTypes[id] = old;
            else
                ctx.enclosingSymbolTypes.Remove(id);
        };
    }
}

// The retained maps represent upstream copy-on-write scope cells. Managed copying
// occurs at scope entry rather than at first mutation; identity is private.
internal sealed partial class NodeBuilderImpl
{
    // nodebuilderscopes.go:9. Preserve all four per-signature name scopes.
    private static Action cloneNodeBuilderContext(NodeBuilderContext context)
    {
        var names = context.typeParameterNames;
        var namesByText = context.typeParameterNamesByText;
        var nextCounts = context.typeParameterNamesByTextNextNameCount;
        var symbolList = context.typeParameterSymbolList;
        context.typeParameterNames = new(names);
        context.typeParameterNamesByText = new(namesByText, StringComparer.Ordinal);
        context.typeParameterNamesByTextNextNameCount = new(nextCounts, StringComparer.Ordinal);
        context.typeParameterSymbolList = new(symbolList);
        return () =>
        {
            context.typeParameterNames = names;
            context.typeParameterNamesByText = namesByText;
            context.typeParameterNamesByTextNextNameCount = nextCounts;
            context.typeParameterSymbolList = symbolList;
        };
    }

    // nodebuilderscopes.go:53. The ordinary nonrest expansion preserves the actual
    // signature parameter slice; tuple/rest expansion is outside admission.
    private (Slice<Symbol>, Action) enterSignatureScope(Signature signature)
    {
        var expandedParams = ch.getExpandedParameters(signature, true)[0];
        return (expandedParams, enterNewScope(signature.declaration, expandedParams, signature.typeParameters, signature.parameters, signature.mapper));
    }

    // nodebuilderscopes.go:59. Fake parameter scopes preserve lexical shadowing in
    // diagnostic reuse; they are not inserted into the parsed source tree.
    private Action enterNewScope(Node declaration, Slice<Symbol> expandedParams, Slice<Type> typeParameters, Slice<Symbol> originalParameters, TypeMapper mapper)
    {
        var cleanupContext = cloneNodeBuilderContext(ctx);
        Action cleanupParams = null;
        Action cleanupTypeParams = null;
        var oldEnclosingDecl = ctx.enclosingDeclaration;
        var oldMapper = ctx.mapper;
        if (mapper != null)
            ctx.mapper = mapper;
        if (ctx.enclosingDeclaration != null && declaration != null)
        {
            Action pushFakeScope(string kind, Action<Action<string, Symbol>> addAll)
            {
                Debug.Assert(ctx.enclosingDeclaration != null);
                Node existingFakeScope = null;
                if (links.TryGetValue(ctx.enclosingDeclaration, out var ownLinks) && ownLinks.fakeScopeForSignatureDeclaration == kind)
                    existingFakeScope = ctx.enclosingDeclaration;
                if (existingFakeScope == null && ctx.enclosingDeclaration.Parent != null && links.TryGetValue(ctx.enclosingDeclaration.Parent, out var parentLinks) && parentLinks.fakeScopeForSignatureDeclaration == kind)
                    existingFakeScope = ctx.enclosingDeclaration.Parent;
                Debug.Assert(existingFakeScope == null || Ast.IsBlock(existingFakeScope));
                var locals = existingFakeScope?.Locals() ?? new SymbolTable();
                var newLocals = new List<string>();
                var oldLocals = new List<(string name, Symbol symbol)>();
                addAll((name, symbol) =>
                {
                    if (existingFakeScope != null)
                    {
                        var oldSymbol = locals[name];
                        if (oldSymbol == null)
                            newLocals.Add(name);
                        else
                            oldLocals.Add((name, oldSymbol));
                    }

                    locals[name] = symbol;
                });
                if (existingFakeScope == null)
                {
                    var fakeScope = f.NewBlock(f.NewNodeList(Slice<Node>.Make(0)), false);
                    getLinks(fakeScope).fakeScopeForSignatureDeclaration = kind;
                    fakeScope.LocalsContainerData().Locals = locals;
                    fakeScope.Parent = ctx.enclosingDeclaration;
                    ctx.enclosingDeclaration = fakeScope;
                    return null;
                }

                return () =>
                {
                    foreach (var name in newLocals)
                        locals.Remove(name);
                    foreach (var old in oldLocals)
                        locals[old.name] = old.symbol;
                };
            }

            if (!expandedParams.IsNil && Core.Some(expandedParams, p => p != null))
            {
                cleanupParams = pushFakeScope("params", add =>
                {
                    for (int i = 0; i < expandedParams.Count; i++)
                    {
                        var parameter = expandedParams[i];
                        var original = i < GoRuntime.Len(originalParameters) ? originalParameters[i] : null;
                        if (!originalParameters.IsNil && original != parameter)
                        {
                            if (original != null)
                                add(original.Name, original);
                        }
                        else
                        {
                            bool BindDeclaration(Node declaration)
                            {
                                void BindPattern(Node pattern)
                                {
                                    foreach (var element in pattern.AsBindingPattern().Elements.Nodes)
                                    {
                                        switch (element.Kind)
                                        {
                                            case Kind.OmittedExpression:
                                                return;
                                            case Kind.BindingElement:
                                                BindElement(element);
                                                return;
                                            default:
                                                throw new InvalidOperationException("Unhandled binding element kind");
                                        }
                                    }
                                }

                                void BindElement(Node element)
                                {
                                    if (element.Name() != null && Ast.IsBindingPattern(element.Name()))
                                    {
                                        BindPattern(element.Name());
                                        return;
                                    }

                                    var symbol = ch.getSymbolOfDeclaration(element);
                                    if (symbol != null)
                                        add(symbol.Name, symbol);
                                }

                                if (Ast.IsParameterDeclaration(declaration) && declaration.Name() != null && Ast.IsBindingPattern(declaration.Name()))
                                {
                                    BindPattern(declaration.Name());
                                    return true;
                                }

                                return false;
                            }

                            if (!Core.Some(parameter.Declarations, BindDeclaration))
                                add(parameter.Name, parameter);
                        }
                    }
                });
            }

            if ((ctx.flags & TypeScript.NodeBuilding.Flags.GenerateNamesForShadowedTypeParams) != 0 && !typeParameters.IsNil && Core.Some(typeParameters, p => p != null))
                cleanupTypeParams = pushFakeScope("typeParams", add =>
                {
                    foreach (var typeParameter in typeParameters)
                        if (typeParameter != null)
                            add(typeParameterToName(typeParameter).Text(), typeParameter.symbol);
                });
        }

        return () =>
        {
            cleanupParams?.Invoke();
            cleanupTypeParams?.Invoke();
            cleanupContext();
            ctx.enclosingDeclaration = oldEnclosingDecl;
            ctx.mapper = oldMapper;
        };
    }
}

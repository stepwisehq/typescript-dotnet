// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of internal/checker/nodebuilder.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using Flags = TypeScript.NodeBuilding.Flags;
using InternalFlags = TypeScript.NodeBuilding.InternalFlags;

namespace TypeScript.Checking;
// nodebuilder.go:21. Shared request owner; outputs accumulate across serializations.
internal sealed class VerbosityContext
{
    internal int Level;
    internal long MaxTruncationLength;
    internal bool CanIncreaseVerbosity;
    internal bool Truncated;
}

internal sealed partial class NodeBuilder
{
    // nodebuilder.go:62.
    private void propagateVerbosityOut()
    {
        if (verbosity == null)
            return;
        if (impl.ctx.canIncreaseExpansionDepth)
            verbosity.CanIncreaseVerbosity = true;
        if (impl.ctx.expansionTruncated)
            verbosity.Truncated = true;
    }

    // nodebuilder.go:152.
    internal Slice<Node> ExpandSymbolForHover(Symbol symbol, SymbolFlags meaning)
    {
        enterContext(null, Flags.IgnoreErrors | Flags.MultilineObjectLiterals | Flags.UseAliasDefinedOutsideCurrentScope, InternalFlags.None, null);
        var declaredType = impl.ch.getDeclaredTypeOfSymbol(symbol);
        impl.ctx.typeStack = impl.ctx.typeStack.Append(declaredType).Append((Type)null);
        var nodes = impl.expandSymbolForHover(symbol);
        impl.ctx.typeStack = impl.ctx.typeStack.Slice2(0, impl.ctx.typeStack.Count - 2);
        propagateVerbosityOut();
        var result = Slice<Node>.Make(0, nodes.Count);
        foreach (var node in nodes)
        {
            switch (node.Kind)
            {
                case Kind.ClassDeclaration:
                    result = result.Append(simplifyClassDeclaration(impl.f, node, symbol));
                    break;
                case Kind.EnumDeclaration:
                    result = result.Append(simplifyModifiers(impl.f, node, Ast.IsEnumDeclaration, symbol));
                    break;
                case Kind.InterfaceDeclaration:
                    if ((meaning & SymbolFlags.Interface) != 0)
                        result = result.Append(simplifyModifiers(impl.f, node, Ast.IsInterfaceDeclaration, symbol));
                    break;
                case Kind.ModuleDeclaration:
                    result = result.Append(simplifyModifiers(impl.f, node, Ast.IsModuleDeclaration, symbol));
                    break;
            }
        }

        return exitContextSlice(result);
    }

    // nodebuilder.go:189.
    private static Node simplifyClassDeclaration(NodeFactory factory, Node classDeclaration, Symbol symbol)
    {
        var declarations = Core.Filter(symbol.Declarations, Ast.IsClassLike);
        var original = declarations.Count != 0 ? declarations[0] : classDeclaration;
        var modifiers = original.ModifierFlags() & ~(ModifierFlags.Export | ModifierFlags.Ambient);
        if (Ast.IsClassExpression(original))
        {
            var declaration = classDeclaration.AsClassDeclaration();
            classDeclaration = factory.UpdateClassDeclaration(declaration, classDeclaration.Modifiers(), null, declaration.TypeParameters, declaration.HeritageClauses, declaration.Members);
        }

        return Ast.ReplaceModifiers(factory, classDeclaration, factory.NewModifierList(Ast.CreateModifiersFromModifierFlags(modifiers, factory.NewModifier)));
    }

    // nodebuilder.go:216.
    private static Node simplifyModifiers(NodeFactory factory, Node declaration, Func<Node, bool> isDeclarationKind, Symbol symbol)
    {
        var declarations = Core.Filter(symbol.Declarations, isDeclarationKind);
        var original = declarations.Count != 0 ? declarations[0] : declaration;
        var modifiers = original.ModifierFlags() & ~(ModifierFlags.Export | ModifierFlags.Ambient);
        return Ast.ReplaceModifiers(factory, declaration, factory.NewModifierList(Ast.CreateModifiersFromModifierFlags(modifiers, factory.NewModifier)));
    }
}

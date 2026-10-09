// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/exports.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Selected data/callable forwarding APIs; general checker exports remain excluded.
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    public bool IsTypeAssignableTo(Type source, Type target)
    {
        if (source.checker != this || target.checker != this)
            throw new ArgumentException("Types belong to a different checker");
        return isTypeAssignableTo(source, target);
    }
}

public sealed partial class Checker
{
    // exports.go selected forwarding APIs. Ownership validation is an authored
    // public boundary; underlying semantic helpers retain upstream behavior.
    public static ModifierFlags GetDeclarationModifierFlagsFromSymbol(Symbol symbol) => getDeclarationModifierFlagsFromSymbol(symbol);
    public Type GetWidenedType(Type t)
    {
        validateOwnedType(t);
        return getWidenedType(t);
    }

    public Type GetDeclaredTypeOfSymbol(Symbol symbol)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));
        foreach (var declaration in symbol.Declarations ?? [])
            validateOwnedDeclaration(declaration);
        return getDeclaredTypeOfSymbol(symbol);
    }

    public List<Symbol> GetPropertiesOfType(Type t)
    {
        validateOwnedType(t);
        return getPropertiesOfType(t);
    }

    public Symbol GetPropertyOfType(Type t, string name)
    {
        validateOwnedType(t);
        return getPropertyOfType(t, GoString.ToUtf8(name));
    }

    public Type GetTypeOfSymbol(Symbol symbol)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));
        if ((symbol.Flags & SymbolFlags.Transient) != 0 && !authoredOwnedSymbols.Contains(symbol))
            throw new ArgumentException("Symbol belongs to a different checker", nameof(symbol));
        if (!authoredOwnedSymbols.Contains(symbol))
        {
            var declarations = symbol.Declarations;
            if (declarations == null || declarations.Count == 0)
                throw new ArgumentException("Symbol has no owned declaration", nameof(symbol));
            foreach (var declaration in declarations)
                validateOwnedDeclaration(declaration);
        }

        return getTypeOfSymbol(symbol);
    }

    private void validateOwnedType(Type t)
    {
        if (t == null)
            throw new ArgumentNullException(nameof(t));
        if (t.checker != this)
            throw new ArgumentException("Type belongs to a different checker", nameof(t));
    }

    // Authored ownership boundary: binder declarations may belong to any actual
    // source in this checker's fixed environment; node queries still use the user AST.
    private void validateOwnedDeclaration(Node declaration)
    {
        var root = Ast.GetSourceFileOfNode(declaration);
        if (root == null || !fileIndexMap.ContainsKey(root))
            throw new ArgumentException("Declaration belongs to a different checker environment", nameof(declaration));
    }
}

public sealed partial class Checker
{
    public List<Signature> GetSignaturesOfType(Type t, SignatureKind kind)
    {
        validateOwnedType(t);
        return getSignaturesOfType(t, kind);
    }

    public Type GetReturnTypeOfSignature(Signature signature)
    {
        validateOwnedSignature(signature);
        return getReturnTypeOfSignature(signature);
    }

    public TypePredicate GetTypePredicateOfSignature(Signature signature)
    {
        validateOwnedSignature(signature);
        return getTypePredicateOfSignature(signature);
    }

    public Signature GetResolvedSignature(Node node)
    {
        validateOwnedUserNode(node);
        return getResolvedSignature(node, null, CheckMode.Normal);
    }

    private void validateOwnedSignature(Signature signature)
    {
        ArgumentNullException.ThrowIfNull(signature);
        if (!authoredOwnedSignatures.Contains(signature))
            throw new ArgumentException("Signature belongs to a different checker", nameof(signature));
    }
}

public sealed partial class Checker
{
    // exports.go:410.
    public static bool IsDistributedTypeParameter(Type t) => (t.flags & TypeFlags.TypeParameter) != 0 && t.AsTypeParameter().isDistributed;
}

public sealed partial class Checker
{
    // exports.go:123/127/131. Public ownership checks are authored API guards.
    public Symbol GetImmediateAliasedSymbol(Symbol symbol)
    {
        validateOwnedProjectSymbol(symbol);
        return getImmediateAliasedSymbol(symbol);
    }

    public Symbol GetTargetSymbol(Symbol symbol)
    {
        validateOwnedProjectSymbol(symbol);
        return getTargetSymbol(symbol);
    }

    public Node GetTypeOnlyAliasDeclaration(Symbol symbol)
    {
        validateOwnedProjectSymbol(symbol);
        return getTypeOnlyAliasDeclaration(symbol);
    }

    private void validateOwnedProjectSymbol(Symbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        if (authoredOwnedSymbols.Contains(symbol))
            return;
        if ((symbol.Flags & SymbolFlags.Transient) != 0 || symbol.Declarations == null || symbol.Declarations.Count == 0)
            throw new ArgumentException("Symbol belongs to a different checker", nameof(symbol));
        foreach (var declaration in symbol.Declarations)
            validateOwnedDeclaration(declaration);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/exports.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Scalar IsTypeAssignableTo export only; general checker exports excluded.
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
            SemanticAdmission.ValidateOwnedNode(sourceFile, declaration);
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
        if (symbol != undefinedSymbol && symbol != argumentsSymbol && symbol != unknownSymbol && symbol != globalThisSymbol)
        {
            var declarations = symbol.Declarations;
            if (declarations == null || declarations.Count == 0)
                throw new ArgumentException("Symbol has no owned declaration", nameof(symbol));
            foreach (var declaration in declarations)
                SemanticAdmission.ValidateOwnedNode(sourceFile, declaration);
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
}

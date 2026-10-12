// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Focused exports.go:117 borrowed owner join; accepted List snapshot API remains unchanged.
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal Slice<Signature> GetSignaturesOfTypeSlice(Type type, SignatureKind kind)
    {
        validateOwnedType(type);
        return getSignaturesOfType(type, kind);
    }

    // checker.go:16641. Preserve nil admission and unknown-symbol result flag.
    internal (Symbol? Symbol, bool Resolved) ResolveAlias(Symbol? symbol)
    {
        if (symbol == null)
            return (null, false);
        var resolved = resolveAlias(symbol);
        return (resolved, resolved != unknownSymbol);
    }

    // checker.go:1215. Return the retained alias parameter header after resolving it.
    internal Slice<Type> GetTypeAliasTypeParameters(Symbol symbol)
    {
        if ((symbol.Flags & SymbolFlags.TypeAlias) == 0)
            throw new InvalidOperationException("Attempted to fetch type alias parameters for non-type-alias symbol");
        getDeclaredTypeOfSymbol(symbol);
        return typeAliasLinks.Get(symbol).typeParameters;
    }

    // exports.go:155. Retain the borrowed byte name into the existing property worker.
    internal Symbol? GetPropertyOfType(Type type, ByteStringValue name)
    {
        validateOwnedType(type);
        return getPropertyOfType(type, name);
    }

    // exports.go:231.
    internal static bool IsTupleType(Type type) => isTupleType(type);
}

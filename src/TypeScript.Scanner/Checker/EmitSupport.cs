// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/emitsupport.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Diagnostic-only finite data-property/scalar context reuse; general emit excluded.
#nullable disable
using TypeScript.Scanner;
using TypeScript.PseudoChecking;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    // emitsupport.go:285. The anonymous property-signature branch can add undefined
    // only for a reverse-mapped symbol; mapped types and parameters are unsupported.
    internal bool requiresAddingImplicitUndefined(Node declaration, Symbol symbol, Node enclosingDeclaration)
    {
        if (!Ast.IsParseTreeNode(declaration))
            return false;
        switch (declaration.Kind)
        {
            case Kind.PropertySignature:
                symbol ??= getSymbolOfDeclaration(declaration);
                var t = getTypeOfSymbol(symbol);
                if ((symbol.CheckFlags & CheckFlags.ReverseMapped) != 0 || (t.objectFlags & ObjectFlags.ReverseMapped) != 0)
                    throw unsupported("requiresAddingImplicitUndefined: reverse mapping");
                return false; // Exact final conjunct (ReverseMappedSymbolLinks.Has) is false in this guarded slice.
            case Kind.PropertyDeclaration:
            case Kind.JSDocPropertyTag:
            case Kind.Parameter:
            case Kind.JSDocParameterTag:
                throw unsupported("requiresAddingImplicitUndefined: class/JS/parameter");
            default:
                throw new InvalidOperationException("Node cannot possibly require adding undefined");
        }
    }
}

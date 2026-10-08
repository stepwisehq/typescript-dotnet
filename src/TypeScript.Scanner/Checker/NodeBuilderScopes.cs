// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/nodebuilderscopes.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Diagnostic-only finite data-property/scalar context reuse; general emit excluded.
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

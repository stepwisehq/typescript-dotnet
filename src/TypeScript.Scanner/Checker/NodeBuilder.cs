// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/nodebuilder.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Diagnostic-only scalar type serialization; general declaration serialization excluded.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
internal sealed class NodeBuilder
{
    private readonly NodeBuilderImpl impl;
    internal NodeBuilder(Checker checker) => impl = new(checker);
    // TypeToTypeNode (267); enclosing declarations/tracker/verbosity excluded by API.
    internal Node TypeToTypeNode(Type type)
    {
        enterContext();
        return exitContext(impl.typeToTypeNode(type));
    }

    // nodebuilder.go:238. Default IgnoreErrors; computed-property writing disabled.
    internal Node SymbolToNode(Symbol symbol, SymbolFlags meaning)
    {
        enterContext();
        return exitContext(impl.symbolToNode(symbol, meaning));
    }

    private void enterContext() => impl.approximateLength = 0;
    private Node exitContext(Node result)
    {
        if (result == null)
            throw new InvalidOperationException("should always get typenode");
        return result;
    }
}

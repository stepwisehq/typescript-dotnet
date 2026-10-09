// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/emitresolver.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Declaration visibility links only, for diagnostic symbol accessibility; no emit resolver API.
using TypeScript.Scanner;

namespace TypeScript.Checking;
internal sealed class DeclarationLinks
{
    internal Tristate isVisible;
}

internal sealed class EmitResolverLinks
{
    // Managed identity store replaces core.LinkStore's node pointer storage.
    internal readonly Dictionary<Node, DeclarationLinks> declarationLinks = new();
    internal DeclarationLinks Get(Node node)
    {
        if (!declarationLinks.TryGetValue(node, out var links))
            declarationLinks[node] = links = new();
        return links;
    }
}

public sealed partial class Checker
{
    // emitresolver.go:383. Pure symbol classification used by actual alias tracking.
    private static bool isConstEnumOrConstEnumOnlyModule(Symbol s) => isConstEnumSymbol(s) || (s.Flags & SymbolFlags.ConstEnumOnlyModule) != 0;
}

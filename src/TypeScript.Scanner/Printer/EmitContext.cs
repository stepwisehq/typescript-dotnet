// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/printer/emitcontext.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Selected original links and per-node diagnostic flags only; transformations/source maps excluded.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.DiagnosticPrinting;
internal sealed class EmitContext
{
    private readonly Dictionary<Node, Node> original = new();
    private readonly Dictionary<Node, EmitFlags> emitFlags = new();
    internal EmitFlags EmitFlags(Node node) => emitFlags.TryGetValue(node, out var flags) ? flags : TypeScript.DiagnosticPrinting.EmitFlags.None;
    internal void SetEmitFlags(Node node, EmitFlags flags) => emitFlags[node] = flags;
    internal void AddEmitFlags(Node node, EmitFlags flags) => emitFlags[node] = EmitFlags(node) | flags;
    internal void SetOriginal(Node node, Node source) => SetOriginalEx(node, source, false);
    internal void SetOriginalEx(Node node, Node source, bool allowOverwrite)
    {
        if (source == null)
            throw new InvalidOperationException("Original cannot be nil.");
        if (!original.TryGetValue(node, out var prior))
        {
            original[node] = source;
            if (emitFlags.TryGetValue(source, out var flags))
                emitFlags[node] = flags;
        }
        else if (!allowOverwrite && prior != source)
            throw new InvalidOperationException("Original node already set.");
        else if (allowOverwrite)
            original[node] = source;
    }

    internal Node Original(Node node) => original.TryGetValue(node, out var source) ? source : null;
    internal Node MostOriginal(Node node)
    {
        if (node != null)
            for (var source = Original(node); source != null; source = Original(source))
                node = source;
        return node;
    }
}

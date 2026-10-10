// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/printer/emitcontext.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Selected original links, diagnostic flags and synthesized unresolved comments; transformations/source maps excluded.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.DiagnosticPrinting;
// emitcontext.go:518. The diagnostic slice constructs single-line block comments.
internal readonly record struct SynthesizedComment(Kind Kind, TextRange Loc, bool HasLeadingNewLine, bool HasTrailingNewLine, string Text);
internal sealed class EmitContext
{
    private readonly Dictionary<Node, Node> original = new();
    private readonly Dictionary<Node, EmitFlags> emitFlags = new();
    private readonly Dictionary<Node, List<SynthesizedComment>> leadingComments = new();
    private readonly Dictionary<Node, List<SynthesizedComment>> trailingComments = new();
    // emitcontext.go:997/1002. Managed prefix copies preserve Go slice lengths.
    internal Node AddSyntheticLeadingComment(Node node, Kind kind, string text, bool hasTrailingNewLine)
    {
        if (!leadingComments.TryGetValue(node, out var comments))
            leadingComments[node] = comments = new();
        comments.Add(new(kind, new(-1, -1), false, hasTrailingNewLine, text));
        return node;
    }

    internal List<SynthesizedComment> GetSyntheticLeadingComments(Node node) => leadingComments.TryGetValue(node, out var comments) ? comments : null;
    // emitcontext.go:1014/1019. Diagnostic elision retains source trailing data.
    internal Node AddSyntheticTrailingComment(Node node, Kind kind, string text, bool hasTrailingNewLine)
    {
        if (!trailingComments.TryGetValue(node, out var comments))
            trailingComments[node] = comments = new();
        comments.Add(new(kind, new(-1, -1), false, hasTrailingNewLine, text));
        return node;
    }

    internal List<SynthesizedComment> GetSyntheticTrailingComments(Node node) => trailingComments.TryGetValue(node, out var comments) ? comments : null;
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
            if (leadingComments.TryGetValue(source, out var comments))
                leadingComments[node] = new(comments);
            if (trailingComments.TryGetValue(source, out var trailing))
                trailingComments[node] = new(trailing);
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/checker/links.go; managed identity-preserving page/arena adapters.
using TypeScript.Scanner;

namespace TypeScript.Checking;
internal sealed class nodeLinkStore<V>
    where V : class, new()
{
    private readonly PagedLinkStore<V> store = new();
    public V Get(Node node)
    {
        ref var slot = ref store.Get(Ast.GetNodeId(node));
        return slot ??= new();
    }
    public bool Has(Node node) => store.Has(Ast.GetNodeId(node));
    public V? TryGet(Node node)
    {
        var id = Ast.GetNodeId(node);
        if (!store.TryGet(id, out var value))
            return null;
        // checker/links.go:22: an existing page includes untouched zero structs.
        // Preserve the managed V identity when materializing that zero value.
        if (value is not null)
            return value;
        ref var slot = ref store.Get(id);
        return slot ??= new();
    }
}

internal sealed class symbolArenaLinkStore<V>
    where V : class, new()
{
    private readonly PagedLinkStore<V> store = new();
    public V Get(Symbol symbol)
    {
        // checker/links.go:34: populate the pointer slot once, then return V.
        ref var slot = ref store.Get(Ast.GetSymbolId(symbol));
        return slot ??= new();
    }
    public bool Has(Symbol symbol) => TryGet(symbol)is not null;
    public V? TryGet(Symbol symbol)
    {
        // checker/links.go:45: a present page may still contain a nil pointer.
        store.TryGet(Ast.GetSymbolId(symbol), out var value);
        return value;
    }
}

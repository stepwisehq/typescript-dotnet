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
    public V Get(Node node) => store.Get(Ast.GetNodeId(node)).Value ??= new();
    public bool Has(Node node) => store.Has(Ast.GetNodeId(node));
    public V? TryGet(Node node)
    {
        var slot = store.TryGet(Ast.GetNodeId(node));
        // Allocated pages contain all Go struct slots, even untouched zero values.
        return slot is not null ? slot.Value ??= new() : null;
    }
}

internal sealed class symbolArenaLinkStore<V>
    where V : class, new()
{
    private readonly PagedLinkStore<V> store = new();
    public V Get(Symbol symbol) => store.Get(Ast.GetSymbolId(symbol)).Value ??= new();
    public bool Has(Symbol symbol) => TryGet(symbol)is not null;
    public V? TryGet(Symbol symbol) => store.TryGet(Ast.GetSymbolId(symbol))?.Value;
}

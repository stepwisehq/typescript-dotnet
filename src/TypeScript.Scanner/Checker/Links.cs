// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/checker/links.go; managed identity-preserving page/arena adapters.
using TypeScript.Scanner;

namespace TypeScript.Checking;
internal sealed class nodeLinkStore<V>
    where V : struct
{
    private readonly PagedLinkStore<V> store = new();
    // checker/links.go:10–24. Small records are inline zeroed page slots.
    public ref V Get(Node node) => ref store.Get(Ast.GetNodeId(node));
    public bool Has(Node node) => store.Has(Ast.GetNodeId(node));
    public Memory<V> TryGet(Node node) => store.TryGetLocation(Ast.GetNodeId(node));
}

// checker/links.go:26–49. Large value payloads live in arena chunks; sparse
// pages retain private pointer-sized ordinals rather than the payload itself.
// Managed Memory is created only at the retained-location escape boundary.
internal sealed class symbolArenaLinkStore<V>
    where V : struct
{
    private readonly PagedLinkStore<ulong> store = new();
    private readonly LinkStoreArena<V> arena = new();

    public ref V Get(Symbol symbol)
    {
        ref var location = ref store.Get(Ast.GetSymbolId(symbol));
        if (location == 0)
            location = arena.New();
        return ref arena.Get(location);
    }

    public bool Has(Symbol symbol)
    {
        store.TryGet(Ast.GetSymbolId(symbol), out var location);
        return location != 0;
    }

    // A present sparse page may still contain an uninitialized location. The
    // zero ordinal is the nil pointer; escaped Memory keeps old chunks alive.
    public Memory<V> TryGet(Symbol symbol)
    {
        store.TryGet(Ast.GetSymbolId(symbol), out var location);
        return arena.TryGet(location);
    }
}

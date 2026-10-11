// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/core/linkstore.go; fixed arrays retain page slot addresses.
namespace TypeScript.Scanner;
internal sealed class LinkStore<K, V>
    where K : notnull where V : struct
{
    private Dictionary<K, Memory<V>>? entries;
    private readonly GoArena<V> arena = new();

    // core/linkstore.go:10–24. Entries retain pointers into zeroed arena chunks;
    // array-backed Memory locations preserve those pointers across map growth.
    public ref V Get(K key)
    {
        var location = TryGet(key);
        if (!location.IsEmpty)
            return ref location.Span[0];
        entries ??= new();
        location = arena.NewSliceHeader(1).AsWritableMemory();
        entries[key] = location;
        return ref location.Span[0];
    }

    public bool Has(K key) => entries?.ContainsKey(key) == true;
    public Memory<V> TryGet(K key) => entries is not null && entries.TryGetValue(key, out var location) ? location : default;

    // Authored read-only validation support: actual key snapshot, preserving
    // absent versus allocated-empty storage without Get or entity ID effects.
    internal List<K>? VerificationKeys() => entries?.Keys.ToList();
}

// Managed optional-pointer metadata cell, retained for containing-symbol links.
// PagedLinkStore uses fixed array slots directly rather than allocating cells.
internal sealed class LinkCell<V>
    where V : class
{
    public V? Value;
}

internal sealed class PagedLinkStore<V>
{
    private const int pageShift = 8, pageSize = 1 << pageShift, pageMask = pageSize - 1;
    private const ulong maxPageCount = 65536;
    private Dictionary<ulong, V?[]>? pageMap;
    private readonly List<V?[]?> pageList = [];
    // core/linkstore.go:49/72: fixed pages keep slot addresses stable even when
    // the page table grows. Managed ref returns retain the array owner safely.
    private static V?[] NewPage() => new V?[pageSize];

    public ref V? Get(ulong key)
    {
        V?[]? page;
        var pageIndex = key >> pageShift;
        if (pageIndex < maxPageCount)
        {
            while ((ulong)pageList.Count <= pageIndex)
                pageList.Add(null);
            page = pageList[(int)pageIndex];
            if (page is null)
                pageList[(int)pageIndex] = page = NewPage();
        }
        else
        {
            pageMap ??= new();
            if (!pageMap.TryGetValue(pageIndex, out page))
                pageMap[pageIndex] = page = NewPage();
        }

        return ref page[(int)(key & pageMask)];
    }

    // Every slot exists once its page exists, including unexposed zero slots.
    public bool Has(ulong key) => FindPage(key) is not null;
    // core/linkstore.go:79: distinguish absent pages from present nil slots
    // without returning an unsafe null reference. No slot is initialized here.
    public bool TryGet(ulong key, out V? value)
    {
        var page = FindPage(key);
        value = page is not null ? page[(int)(key & pageMask)] : default;
        return page is not null;
    }

    // A location denotes page presence even when its value is the zero record.
    // No page or slot is initialized while looking up this managed pointer.
    public Memory<V?> TryGetLocation(ulong key)
    {
        var page = FindPage(key);
        return page is null ? default : page.AsMemory((int)(key & pageMask), 1);
    }

    private V?[]? FindPage(ulong key)
    {
        V?[]? page = null;
        var pageIndex = key >> pageShift;
        if (pageIndex < maxPageCount)
        {
            if (pageIndex < (ulong)pageList.Count)
                page = pageList[(int)pageIndex];
        }
        else if (pageMap is not null)
            pageMap.TryGetValue(pageIndex, out page);
        return page;
    }
}

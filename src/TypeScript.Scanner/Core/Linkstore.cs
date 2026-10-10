// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/core/linkstore.go; managed objects replace arenas and slot addresses.
namespace TypeScript.Scanner;
internal sealed class LinkStore<K, V>
    where K : notnull where V : class, new()
{
    private Dictionary<K, V>? entries;
    public V Get(K key)
    {
        var value = TryGet(key);
        if (value is not null)
            return value;
        entries ??= new();
        value = new();
        entries[key] = value;
        return value;
    }

    public bool Has(K key) => entries?.ContainsKey(key) == true;
    public V? TryGet(K key) => entries is not null && entries.TryGetValue(key, out var value) ? value : null;

    // Authored read-only validation support: actual key snapshot, preserving
    // absent versus allocated-empty storage without Get or entity ID effects.
    internal List<K>? VerificationKeys() => entries?.Keys.ToList();
}

// One managed cell represents the stable address of one Go page slot. Value may
// itself be a nil pointer (symbolArenaLinkStore) or a lazily materialized zero struct.
internal sealed class LinkCell<V>
    where V : class
{
    public V? Value;
}

internal sealed class PagedLinkStore<V>
    where V : class
{
    private const int pageShift = 8, pageSize = 1 << pageShift, pageMask = pageSize - 1;
    private const ulong maxPageCount = 65536;
    private Dictionary<ulong, LinkCell<V>[]>? pageMap;
    private readonly List<LinkCell<V>[]?> pageList = [];
    // Go exposes inline zero slots in an allocated page. Managed address cells
    // are needed only when a caller requests a slot, and remain stable afterward.
    private static LinkCell<V>[] NewPage() => new LinkCell<V>[pageSize];

    public LinkCell<V> Get(ulong key)
    {
        LinkCell<V>[]? page;
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

        return page[(int)(key & pageMask)] ??= new();
    }

    // Every slot exists once its page exists, including unexposed zero slots.
    public bool Has(ulong key) => FindPage(key) is not null;
    public LinkCell<V>? TryGet(ulong key)
    {
        var page = FindPage(key);
        return page is not null ? page[(int)(key & pageMask)] ??= new() : null;
    }

    private LinkCell<V>[]? FindPage(ulong key)
    {
        LinkCell<V>[]? page = null;
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

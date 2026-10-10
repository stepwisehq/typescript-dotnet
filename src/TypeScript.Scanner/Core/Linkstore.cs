// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/core/linkstore.go; managed objects replace arenas, fixed arrays retain page slots.
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

// Managed optional-pointer metadata cell, retained for containing-symbol links.
// PagedLinkStore uses fixed array slots directly rather than allocating cells.
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
        value = page is not null ? page[(int)(key & pageMask)] : null;
        return page is not null;
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

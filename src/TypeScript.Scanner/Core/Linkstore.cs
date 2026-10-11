// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/core/linkstore.go; fixed arrays retain page slot addresses.
using System.Numerics;

namespace TypeScript.Scanner;
internal sealed class LinkStore<K, V>
    where K : notnull where V : struct
{
    private Dictionary<K, ulong>? entries;
    private readonly LinkStoreArena<V> arena = new();

    // core/linkstore.go:10–24. Entries retain pointers into zeroed arena chunks;
    // private one-based ordinals keep retained slots pointer-sized. Actual managed
    // refs and Memory at the public store boundary retain the stable array owner.
    public ref V Get(K key)
    {
        if (entries is not null && entries.TryGetValue(key, out var location))
            return ref arena.Get(location);
        entries ??= new();
        location = arena.New();
        entries[key] = location;
        return ref arena.Get(location);
    }

    public bool Has(K key) => entries?.ContainsKey(key) == true;
    public Memory<V> TryGet(K key) => entries is not null && entries.TryGetValue(key, out var location) ? arena.TryGet(location) : default;

    // Authored read-only validation support: actual key snapshot, preserving
    // absent versus allocated-empty storage without Get or entity ID effects.
    internal List<K>? VerificationKeys() => entries?.Keys.ToList();
}

// Bounded adaptation of core/arena.go:New for internal link pointers. The ordinal
// never leaves the owning stores: callers receive a managed ref or a one-slot
// Memory with the actual array owner. Chunks are not reset, reused or relocated.
// A directory load replaces Memory's stored owner/index/length; its extra lookup
// and retained directory are explicit runtime tradeoffs, not a measured gain.
internal sealed class LinkStoreArena<V>
    where V : struct
{
    // Initial chunks have 2,4,8,16,32,64,128 slots (254 total); all later chunks
    // have 256. This is the same growth as GoArena's source NewSliceHeader(1).
    private const ulong initialRecordCount = 254;
    private const int initialChunkCount = 7;
    private const int chunkShift = 8;
    private const ulong chunkMask = (1UL << chunkShift) - 1;
    private readonly List<V[]> chunks = [];
    private V[]? currentChunk;
    private int currentLength;
    private ulong recordCount;

    public ulong New()
    {
        ulong location = checked(recordCount + 1);
        if (currentChunk is null || currentLength == currentChunk.Length)
        {
            var chunk = new V[GoArena<V>.NextSize(currentLength)];
            chunks.Add(chunk);
            currentChunk = chunk;
            currentLength = 0;
        }
        currentLength++;
        recordCount = location;
        return location;
    }

    public ref V Get(ulong location)
    {
        findLocation(location, out var chunk, out int index);
        return ref chunk[index];
    }

    public Memory<V> TryGet(ulong location)
    {
        if (location == 0)
            return default;
        findLocation(location, out var chunk, out int index);
        return chunk.AsMemory(index, 1);
    }

    private void findLocation(ulong location, out V[] chunk, out int index)
    {
        if (location == 0 || location > recordCount)
            throw new ArgumentOutOfRangeException(nameof(location));
        ulong position = location - 1;
        int chunkIndex;
        if (position < initialRecordCount)
        {
            chunkIndex = BitOperations.Log2((uint)(position + 2)) - 1;
            ulong chunkStart = (2UL << chunkIndex) - 2;
            index = (int)(position - chunkStart);
        }
        else
        {
            ulong fullChunkPosition = position - initialRecordCount;
            chunkIndex = checked(initialChunkCount + (int)(fullChunkPosition >> chunkShift));
            index = (int)(fullChunkPosition & chunkMask);
        }
        chunk = chunks[chunkIndex];
    }
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/ownercache.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.ProjectSystem;
internal sealed class OwnerCache<K, V, LoadArgs>(Func<K, LoadArgs, V> parse, Func<K, V, LoadArgs, bool>? isExpired = null)
    where K : notnull
{
    private sealed class Entry
    {
        internal readonly SemaphoreSlim Mu = new(1, 1);
        internal V Value = default!;
        internal readonly HashSet<ulong> Owners = [];
    }

    private readonly TypeScript.Collections.SyncMap<K, Entry> entries = new();
    // ownercache.go:38 LoadAndAcquire.
    public V LoadAndAcquire(K identity, ulong owner, LoadArgs args)
    {
        var(entry, loaded) = LoadOrStoreLockedEntry(identity);
        try
        {
            if (!loaded || isExpired?.Invoke(identity, entry.Value, args) == true)
                entry.Value = parse(identity, args);
            entry.Owners.Add(owner);
            return entry.Value;
        }
        finally
        {
            entry.Mu.Release();
        }
    }

    // ownercache.go:48 Acquire.
    public void Acquire(K identity, ulong owner, V value)
    {
        var(entry, loaded) = LoadOrStoreLockedEntry(identity);
        try
        {
            if (!loaded)
                entry.Value = value;
            entry.Owners.Add(owner);
        }
        finally
        {
            entry.Mu.Release();
        }
    }

    // ownercache.go:60 AddOwner.
    public void AddOwner(K identity, ulong owner)
    {
        var(entry, ok) = entries.Load(identity);
        if (!ok)
            throw new InvalidOperationException("OwnerCache.AddOwner: entry not found");
        entry.Mu.Wait();
        try
        {
            if (entry.Owners.Count == 0)
                throw new InvalidOperationException("OwnerCache.AddOwner: entry has no owners");
            entry.Owners.Add(owner);
        }
        finally
        {
            entry.Mu.Release();
        }
    }

    // ownercache.go:73 Has.
    public bool Has(K identity) => entries.Load(identity).Ok;
    // ownercache.go:78 Release.
    public void Release(K identity, ulong owner)
    {
        var(entry, ok) = entries.Load(identity);
        if (!ok)
            return;
        entry.Mu.Wait();
        try
        {
            entry.Owners.Remove(owner);
            if (entry.Owners.Count == 0)
                entries.Delete(identity);
        }
        finally
        {
            entry.Mu.Release();
        }
    }

    // ownercache.go:91 LoadOrStoreLockedEntry.
    private (Entry Entry, bool Loaded) LoadOrStoreLockedEntry(K key)
    {
        var entry = new Entry();
        entry.Mu.Wait();
        var(existing, loaded) = entries.LoadOrStore(key, entry);
        if (loaded)
        {
            entry.Mu.Release();
            existing.Mu.Wait();
            if (existing.Owners.Count == 0)
            {
                existing.Mu.Release();
                return LoadOrStoreLockedEntry(key);
            }

            return (existing, true);
        }

        return (entry, false);
    }
}

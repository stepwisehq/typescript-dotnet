// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/refcountcache.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.ProjectSystem;
internal readonly record struct RefCountCacheOptions(bool DisableDeletion = false);
internal sealed class RefCountCache<K, V, AcquireArgs>(RefCountCacheOptions options, Func<K, AcquireArgs, V> parse)
    where K : notnull
{
    private sealed class Entry
    {
        internal readonly SemaphoreSlim Mu = new(1, 1);
        internal V Value = default!;
        internal int RefCount;
    }

    public RefCountCacheOptions Options { get; } = options;

    private readonly TypeScript.Collections.SyncMap<K, Entry> entries = new();
    // session.go logging observes the original Range, rather than inventing a
    // private-state count contract for concurrent cache mutation.
    internal int EntryCount()
    {
        int count = 0;
        entries.Range((_, _) =>
        {
            count++;
            return true;
        });
        return count;
    }

    // refcountcache.go:44 Acquire.
    public V Acquire(K identity, AcquireArgs args)
    {
        var(entry, loaded) = LoadOrStoreNewLockedEntry(identity);
        try
        {
            if (!loaded)
                entry.Value = parse(identity, args);
            return entry.Value;
        }
        finally
        {
            entry.Mu.Release();
        }
    }

    // refcountcache.go:55 Has.
    public bool Has(K identity) => entries.Load(identity).Ok;
    // refcountcache.go:64 AcquireExisting.
    public (V Value, bool Ok) AcquireExisting(K identity)
    {
        var(entry, ok) = entries.Load(identity);
        if (!ok)
            return (default!, false);
        entry.Mu.Wait();
        try
        {
            if (entry.RefCount <= 0 && !Options.DisableDeletion)
                return (default!, false);
            entry.RefCount++;
            return (entry.Value, true);
        }
        finally
        {
            entry.Mu.Release();
        }
    }

    // refcountcache.go:86 AcquireOrError.
    public (V Value, Exception? Error) AcquireOrError(K identity, Func<(V Value, Exception? Error)> produce)
    {
        var(entry, loaded) = LoadOrStoreNewLockedEntry(identity);
        try
        {
            if (loaded)
                return (entry.Value, null);
            var(value, error) = produce();
            if (error is not null)
            {
                entry.RefCount = 0;
                entries.Delete(identity);
                return (value, error);
            }

            entry.Value = value;
            return (value, null);
        }
        finally
        {
            entry.Mu.Release();
        }
    }

    // refcountcache.go:105 Ref.
    public void Ref(K identity)
    {
        var(entry, ok) = entries.Load(identity);
        if (!ok)
            throw new InvalidOperationException("cache entry not found");
        entry.Mu.Wait();
        try
        {
            if (entry.RefCount <= 0 && !Options.DisableDeletion)
            {
                var(newEntry, _) = LoadOrStoreNewLockedEntry(identity);
                try
                {
                    newEntry.Value = entry.Value;
                }
                finally
                {
                    newEntry.Mu.Release();
                }

                return;
            }

            entry.RefCount++;
        }
        finally
        {
            entry.Mu.Release();
        }
    }

    // refcountcache.go:125 Deref.
    public void Deref(K identity)
    {
        var(entry, ok) = entries.Load(identity);
        if (!ok)
            return;
        entry.Mu.Wait();
        try
        {
            entry.RefCount--;
            if (entry.RefCount <= 0 && !Options.DisableDeletion)
                entries.Delete(identity);
        }
        finally
        {
            entry.Mu.Release();
        }
    }

    // refcountcache.go:141 LoadOrStoreNewLockedEntry.
    private (Entry Entry, bool Loaded) LoadOrStoreNewLockedEntry(K key)
    {
        var entry = new Entry
        {
            RefCount = 1
        };
        entry.Mu.Wait();
        var(existing, loaded) = entries.LoadOrStore(key, entry);
        if (loaded)
        {
            entry.Mu.Release();
            existing.Mu.Wait();
            if (existing.RefCount <= 0 && !Options.DisableDeletion)
            {
                existing.Mu.Release();
                return LoadOrStoreNewLockedEntry(key);
            }

            existing.RefCount++;
            return (existing, true);
        }

        return (entry, false);
    }
}

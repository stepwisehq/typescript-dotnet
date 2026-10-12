// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/collections/syncmap.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Managed locking/storage adaptation; no sync.Map backing topology claim.
namespace TypeScript.Collections;
public sealed class SyncMap<K, V>
    where K : notnull
{
    private readonly Dictionary<K, V> values;
    public SyncMap(IEqualityComparer<K>? comparer = null) => values = new(comparer);
    private readonly object gate = new();
    private static V Zero => typeof(V) == typeof(string) ? (V)(object)"" : default!;

    public (V Value, bool Ok) Load(K key)
    {
        lock (gate)
            return values.TryGetValue(key, out var value) ? (value, true) : (Zero, false);
    }

    public void Store(K key, V value)
    {
        lock (gate)
            values[key] = value;
    }

    public (V Actual, bool Loaded) LoadOrStore(K key, V value)
    {
        lock (gate)
        {
            if (values.TryGetValue(key, out var previous))
                return (previous, true);
            values.Add(key, value);
            return (value, false);
        }
    }

    public void Delete(K key)
    {
        lock (gate)
            values.Remove(key);
    }

    public void Clear()
    {
        lock (gate)
            values.Clear();
    }

    // Range allows callbacks to mutate the map. A captured entry list is one
    // valid traversal under Go's non-snapshot Range contract; no lock crosses a callback.
    public void Range(Func<K, V, bool> callback)
    {
        KeyValuePair<K, V>[] entries;
        lock (gate)
            entries = values.ToArray();
        foreach (var entry in entries)
            if (!callback(entry.Key, entry.Value))
                break;
    }

    // State is borrowed synchronously; new snapshot owners can avoid captured delegates.
    public void Range<TState>(TState state, Func<K, V, TState, bool> callback)
    {
        KeyValuePair<K, V>[] entries;
        lock (gate)
            entries = values.ToArray();
        foreach (var entry in entries)
            if (!callback(entry.Key, entry.Value, state))
                break;
    }

    public int Size()
    {
        lock (gate)
            return values.Count;
    }

    public Dictionary<K, V> ToMap()
    {
        var result = new Dictionary<K, V>(values.Comparer);
        Range((key, value) =>
        {
            result[key] = value;
            return true;
        });
        return result;
    }

    public IEnumerable<K> Keys()
    {
        foreach (var key in ToMap().Keys)
            yield return key;
    }

    public SyncMap<K, V> Clone()
    {
        var result = new SyncMap<K, V>(values.Comparer);
        Range((key, value) =>
        {
            result.Store(key, value);
            return true;
        });
        return result;
    }
}

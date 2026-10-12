// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/collections/multimap.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
namespace TypeScript.Collections;

internal static partial class Collections
{
    // multimap.go:13. A supplied hint owns an allocated map even when empty.
    internal static MultiMap<K, V> NewMultiMapWithSizeHint<K, V>(int hint) where K : notnull
        => new() { M = new Dictionary<K, Slice<V>>(Math.Max(0, hint)) };

    // multimap.go:19. Group order within each retained slice follows input order.
    internal static MultiMap<K, V> GroupBy<K, V>(Slice<V> items, Func<V, K> groupId) where K : notnull
    {
        var map = new MultiMap<K, V>();
        foreach (var item in items)
            map.Add(groupId(item), item);
        return map;
    }
}

internal sealed class MultiMap<K, V> where K : notnull
{
    // multimap.go:9. The zero owner has no dictionary until the first Add.
    internal Dictionary<K, Slice<V>>? M;

    // multimap.go:27.
    internal bool Has(K key) => M?.ContainsKey(key) == true;

    // multimap.go:32. Absent groups return the nil slice header.
    internal Slice<V> Get(K key) => M?.GetValueOrDefault(key) ?? default;

    // multimap.go:36.
    internal void Add(K key, V value)
    {
        M ??= new();
        M[key] = M.GetValueOrDefault(key).Append(value);
    }

    // multimap.go:43. The overlapping append preserves capacity and tail ownership.
    internal void Remove(K key, V value)
    {
        if (M is null || !M.TryGetValue(key, out var values))
            return;
        int index = -1;
        var comparer = EqualityComparer<V>.Default;
        for (int i = 0; i < values.Length; i++)
        {
            if (!comparer.Equals(values[i], value))
                continue;
            index = i;
            break;
        }
        if (index < 0)
            return;
        if (values.Length == 1)
            M.Remove(key);
        else
            M[key] = values.Slice2(0, index).Append(values.Slice2(index + 1, values.Length));
    }

    // multimap.go:57.
    internal void RemoveAll(K key) => M?.Remove(key);

    // multimap.go:61.
    internal int Len() => M?.Count ?? 0;

    // multimap.go:65/69. Value sequences keep dictionary iteration live and use
    // standard struct enumerators; no yield closure or materialized key/value list.
    internal KeysSequence Keys() => new(M);
    internal ValuesSequence Values() => new(M);

    // multimap.go:73. Clear retains the allocated backing owner.
    internal void Clear() => M?.Clear();

    internal readonly struct KeysSequence(Dictionary<K, Slice<V>>? map)
    {
        public Enumerator GetEnumerator() => new(map);
        internal struct Enumerator
        {
            private readonly bool present;
            private Dictionary<K, Slice<V>>.Enumerator entries;
            internal Enumerator(Dictionary<K, Slice<V>>? map)
            {
                present = map is not null;
                entries = map?.GetEnumerator() ?? default;
            }
            public K Current => entries.Current.Key;
            public bool MoveNext() => present && entries.MoveNext();
            public void Dispose() => entries.Dispose();
        }
    }

    internal readonly struct ValuesSequence(Dictionary<K, Slice<V>>? map)
    {
        public Enumerator GetEnumerator() => new(map);
        internal struct Enumerator
        {
            private readonly bool present;
            private Dictionary<K, Slice<V>>.Enumerator entries;
            internal Enumerator(Dictionary<K, Slice<V>>? map)
            {
                present = map is not null;
                entries = map?.GetEnumerator() ?? default;
            }
            public Slice<V> Current => entries.Current.Value;
            public bool MoveNext() => present && entries.MoveNext();
            public void Dispose() => entries.Dispose();
        }
    }
}

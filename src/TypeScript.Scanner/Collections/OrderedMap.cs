// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/collections/ordered_map.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Configuration-required ordered storage. JSON codec adapters are supplied by TsOptions;
// the upstream reflection-based arbitrary-key codec is not a general serialization API here.
namespace TypeScript.Collections;
public readonly record struct MapEntry<K, V>(K Key, V Value);
public sealed class OrderedMap<K, V>
    where K : notnull
{
    private List<K>? keys;
    private Dictionary<K, V>? mp;
    public OrderedMap()
    {
    }

    public OrderedMap(int hint)
    {
        keys = new(hint);
        mp = new(hint);
    }

    private static T Zero<T>() => typeof(T) == typeof(string) ? (T)(object)"" : default!;
    public void Set(K key, V value)
    {
        mp ??= new();
        if (!mp.ContainsKey(key))
            (keys ??= new()).Add(key);
        mp[key] = value;
    }

    public (V Value, bool Ok) Get(K key) => mp != null && mp.TryGetValue(key, out var value) ? (value, true) : (Zero<V>(), false);
    // Existing ordered JSON string keys remain owned strings. The resolver's
    // alternate lookup borrows its byte/raw-character input and the standard
    // Dictionary comparer without constructing another string key.
    internal (V Value, bool Ok) Get(TypeScript.Scanner.ByteStringValue key)
    {
        if (mp is null) return (Zero<V>(), false);
        if (typeof(K) != typeof(string)) throw new InvalidOperationException("Borrowed text lookup requires string keys");
        var strings = (Dictionary<string, V>)(object)mp;
        return key.TryGetValue(strings, out var value) ? (value, true) : (Zero<V>(), false);
    }
    public V GetOrZero(K key) => Get(key).Value;
    public (K Key, V Value, bool Ok) EntryAt(int index)
    {
        if (index < 0 || index >= (keys?.Count ?? 0))
            return (Zero<K>(), Zero<V>(), false);
        var key = keys![index];
        return (key, mp![key], true);
    }

    public bool Has(K key) => mp?.ContainsKey(key) == true;
    public (V Value, bool Ok) Delete(K key)
    {
        if (mp == null || !mp.Remove(key, out var value))
            return (Zero<V>(), false);
        // List removal represents the original Go slice-header change; remaining order is retained.
        keys!.RemoveAt(keys.IndexOf(key));
        return (value, true);
    }

    // The live count deliberately includes entries appended during enumeration, as upstream does.
    public IEnumerable<K> Keys()
    {
        for (int i = 0; i < (keys?.Count ?? 0); i++)
            yield return keys![i];
    }

    public IEnumerable<V> Values()
    {
        for (int i = 0; i < (keys?.Count ?? 0); i++)
            yield return mp![keys![i]];
    }

    public IEnumerable<(K Key, V Value)> Entries()
    {
        for (int i = 0; i < (keys?.Count ?? 0); i++)
        {
            var key = keys![i];
            yield return (key, mp![key]);
        }
    }

    public void Clear()
    {
        keys?.Clear();
        mp?.Clear();
    }

    public int Size() => keys?.Count ?? 0;
    public OrderedMap<K, V> Clone() => new()
    {
        keys = keys == null ? null : new(keys),
        mp = mp == null ? null : new(mp)
    };
    public bool EqualFunc(OrderedMap<K, V>? other, Func<V, V, bool> equal)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other == null || Size() != other.Size())
            return false;
        if (!Keys().SequenceEqual(other.Keys()))
            return false;
        foreach (var(key, value)in Entries())
            if (!other.Has(key) || !equal(value, other.GetOrZero(key)))
                return false;
        return true;
    }
}

public static class OrderedMaps
{
    public static OrderedMap<K, V> NewOrderedMapWithSizeHint<K, V>(int hint)
        where K : notnull => new(hint);
    public static OrderedMap<K, V> NewOrderedMapFromList<K, V>(IEnumerable<MapEntry<K, V>>? items)
        where K : notnull
    {
        var result = new OrderedMap<K, V>();
        if (items != null)
            foreach (var item in items)
                result.Set(item.Key, item.Value);
        return result;
    }

    public static bool EqualFunc<K, V>(OrderedMap<K, V>? a, OrderedMap<K, V>? b, Func<V, V, bool> equal)
        where K : notnull => ReferenceEquals(a, b) || a != null && a.EqualFunc(b, equal);
    public static void DiffOrderedMaps<K, V>(OrderedMap<K, V>? a, OrderedMap<K, V>? b, Action<K, V> added, Action<K, V> removed, Action<K, V, V> modified)
        where K : notnull => DiffOrderedMapsFunc(a, b, EqualityComparer<V>.Default.Equals, added, removed, modified);
    public static void DiffOrderedMapsFunc<K, V>(OrderedMap<K, V>? a, OrderedMap<K, V>? b, Func<V, V, bool> equal, Action<K, V> added, Action<K, V> removed, Action<K, V, V> modified)
        where K : notnull
    {
        if (b != null)
            foreach (var(key, value)in b.Entries())
                if (a?.Has(key) != true)
                    added(key, value);
        if (a != null)
            foreach (var(key, value)in a.Entries())
            {
                if (b?.Has(key) == true)
                {
                    var newValue = b.GetOrZero(key);
                    if (!equal(value, newValue))
                        modified(key, value, newValue);
                }
                else
                    removed(key, value);
            }
    }
}

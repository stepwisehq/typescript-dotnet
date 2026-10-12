// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/dirty/map.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.ProjectSystem.Dirty;
internal sealed class MapEntry<K, V> : Entry<K, V>, IValue<V> where K : notnull where V : ICloneable<V>
{
    internal Map<K, V> map = null!;
    // map.go:10 Change.
    public void Change(Action<V> apply)
    {
        if (deleted)
            throw new InvalidOperationException("tried to change a deleted entry");
        if (!dirty)
        {
            value = value.Clone();
            dirty = true;
            map.dirty[key] = this;
        }

        apply(value);
    }

    // map.go:22 Replace.
    public void Replace(V newValue)
    {
        if (deleted)
            throw new InvalidOperationException("tried to change a deleted entry");
        if (!dirty)
        {
            dirty = true;
            map.dirty[key] = this;
        }

        value = newValue;
    }

    // map.go:33 ChangeIf.
    public bool ChangeIf(Func<V, bool> cond, Action<V> apply)
    {
        if (!cond(Value()))
            return false;
        Change(apply);
        return true;
    }

    // map.go:41 Delete.
    public void Delete()
    {
        if (!dirty)
            map.dirty[key] = this;
        deleted = true;
    }

    // map.go:48 Locked.
    public void Locked(Action<IValue<V>> fn) => fn(this);
}

internal sealed class Map<K, V>
    where K : notnull where V : ICloneable<V>
{
    private Dictionary<K, V>? original;
    internal Dictionary<K, MapEntry<K, V>> dirty;
    private readonly IEqualityComparer<K> comparer;
    public Map(Dictionary<K, V>? original, IEqualityComparer<K>? comparer = null)
    {
        this.original = original;
        this.comparer = original?.Comparer ?? comparer ?? EqualityComparer<K>.Default;
        dirty = new(this.comparer);
    }

    // map.go:64 Get.
    public (MapEntry<K, V>? Entry, bool Ok) Get(K key)
    {
        if (dirty.TryGetValue(key, out var entry))
            return entry.deleted ? (null, false) : (entry, true);
        if (original is null || !original.TryGetValue(key, out var value))
            return (null, false);
        return (new() { map = this, key = key, original = value, value = value }, true);
    }

    // map.go:89 Add.
    public void Add(K key, V value) => dirty[key] = new()
    {
        map = this,
        key = key,
        value = value,
        dirty = true
    };
    // map.go:10 Change.
    public void Change(K key, Action<V> apply)
    {
        var(entry, ok) = Get(key);
        if (!ok)
            throw new InvalidOperationException("tried to change a non-existent entry");
        entry!.Change(apply);
    }

    // map.go:106 TryDelete.
    public bool TryDelete(K key)
    {
        var(entry, ok) = Get(key);
        if (!ok)
            return false;
        entry!.Delete();
        return true;
    }

    // map.go:41 Delete.
    public void Delete(K key)
    {
        if (!TryDelete(key))
            throw new InvalidOperationException("tried to delete a non-existent entry");
    }

    // map.go:120 Range.
    public void Range(Func<MapEntry<K, V>, bool> fn)
    {
        var seen = new HashSet<K>(comparer);
        // Go permits callback mutation during map traversal. Capture entries so CLR
        // dictionary version checks do not strengthen the upstream contract.
        foreach (var entry in dirty.Values.ToArray())
        {
            seen.Add(entry.key);
            if (!entry.deleted && !fn(entry))
                break;
        }

        if (original is null)
            return;
        foreach (var(key, value)in original)
        {
            if (seen.Contains(key))
                continue;
            if (!fn(new() { map = this, key = key, original = value, value = value }))
                break;
        }
    }

    // map.go:144 Clear.
    public void Clear()
    {
        dirty = new(comparer);
        original = new(comparer);
    }

    // map.go:149 FinalizeValue.
    public (Dictionary<K, V>? Value, bool Changed) FinalizeValue()
    {
        if (dirty.Count == 0)
            return (original, false);
        var result = original is null ? new Dictionary<K, V>(dirty.Count, comparer) : new(original, comparer);
        foreach (var(key, entry)in dirty)
        {
            if (entry.deleted)
                result.Remove(key);
            else
                result[key] = entry.value;
        }

        return (result, true);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/collections/cow.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
// A stable managed owner represents the Go receiver pointer retained by scope cleanup.
// Backing storage stays absent on reads and clones only at the first scoped write.
internal sealed class CopyOnWriteMap<K, V> where K : notnull
{
    private Dictionary<K, V>? map;
    private bool owned;
    private readonly IEqualityComparer<K> comparer;
    internal CopyOnWriteMap(IEqualityComparer<K>? comparer = null) => this.comparer = comparer ?? EqualityComparer<K>.Default;
    internal bool TryGetValue(K key, out V value)
    {
        if (map is not null) return map.TryGetValue(key, out value!);
        value = default!;
        return false;
    }
    internal bool Has(K key) => map?.ContainsKey(key) == true;
    internal void Set(K key, V value) { ensureOwned(); map![key] = value; }
    private void ensureOwned()
    {
        if (owned) return;
        map = map is null ? new(comparer) : new(map, comparer);
        owned = true;
    }
    internal Action EnterScope()
    {
        var savedMap = map;
        bool savedOwned = owned;
        owned = false;
        return () => { map = savedMap; owned = savedOwned; };
    }
}
internal sealed class CopyOnWriteSet<K> where K : notnull
{
    private readonly CopyOnWriteMap<K, bool> map;
    internal CopyOnWriteSet(IEqualityComparer<K>? comparer = null) => map = new(comparer);
    internal bool Has(K key) => map.Has(key);
    internal void Add(K key) => map.Set(key, true);
    internal Action EnterScope() => map.EnterScope();
}

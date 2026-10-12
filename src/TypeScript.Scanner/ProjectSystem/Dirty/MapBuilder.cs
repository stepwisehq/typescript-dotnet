// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/dirty/mapbuilder.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.ProjectSystem.Dirty;
internal sealed class MapBuilder<K, VBase, VBuilder>
    where K : notnull
{
    private readonly Dictionary<K, VBase>? original;
    private Dictionary<K, VBuilder> dirty;
    private HashSet<K>? deleted;
    private readonly Func<VBuilder, VBase> build;
    private readonly IEqualityComparer<K> comparer;
    // toBuilder is retained in the original constructor contract, though pinned
    // mapbuilder.go never invokes it; builders are supplied through Set.
    public MapBuilder(Dictionary<K, VBase>? original, Func<VBase, VBuilder> toBuilder, Func<VBuilder, VBase> build, IEqualityComparer<K>? comparer = null)
    {
        this.original = original;
        this.build = build;
        this.comparer = original?.Comparer ?? comparer ?? EqualityComparer<K>.Default;
        dirty = new(this.comparer);
    }

    // mapbuilder.go:27 Set.
    public void Set(K key, VBuilder value)
    {
        dirty[key] = value;
        deleted?.Remove(key);
    }

    // mapbuilder.go:32 Delete.
    public void Delete(K key)
    {
        (deleted ??= new(comparer)).Add(key);
        dirty.Remove(key);
    }

    // mapbuilder.go:40 Clear.
    public void Clear()
    {
        dirty = new(comparer);
        deleted = new(original?.Count ?? 0, comparer);
        if (original is not null)
            foreach (var key in original.Keys)
                deleted.Add(key);
    }

    // mapbuilder.go:48 Has.
    public bool Has(K key) => deleted?.Contains(key) != true && (dirty.ContainsKey(key) || original?.ContainsKey(key) == true);
    // mapbuilder.go:59 Build.
    public Dictionary<K, VBase>? Build()
    {
        if (dirty.Count == 0 && (deleted?.Count ?? 0) == 0)
            return original;
        var result = original is null ? new Dictionary<K, VBase>(comparer) : new(original, comparer);
        if (deleted is not null)
            foreach (var key in deleted)
                result.Remove(key);
        foreach (var(key, value)in dirty)
            result[key] = build(value);
        return result;
    }
}

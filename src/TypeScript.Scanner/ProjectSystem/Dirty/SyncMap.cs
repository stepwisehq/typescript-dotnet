// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/dirty/syncmap.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.ProjectSystem.Dirty;
internal sealed class LockedEntry<K, V>(SyncMapEntry<K, V> entry) : IValue<V> where K : notnull where V : ICloneable<V>
{
    // syncmap.go:14 Value.
    public V Value() => entry.ValueLocked();
    // syncmap.go:18 Original.
    public V Original() => entry.original;
    // syncmap.go:22 Dirty.
    public bool Dirty() => entry.dirty;
    // syncmap.go:26 Change.
    public void Change(Action<V> apply) => entry.ChangeLocked(apply);
    // syncmap.go:30 ChangeIf.
    public bool ChangeIf(Func<V, bool> cond, Action<V> apply)
    {
        if (!cond(entry.ValueLocked()))
            return false;
        entry.ChangeLocked(apply);
        return true;
    }

    // syncmap.go:38 Delete.
    public void Delete() => entry.DeleteLocked();
    // syncmap.go:42 Locked.
    public void Locked(Action<IValue<V>> fn) => fn(this);
}

internal sealed class SyncMapEntry<K, V> : Entry<K, V>, IValue<V> where K : notnull where V : ICloneable<V>
{
    internal SyncMap<K, V> map = null!;
    // Private synchronous non-reentrant mutex preserves Go callback lock boundaries.
    internal readonly SemaphoreSlim mu = new(1, 1);
    internal SyncMapEntry<K, V>? proxyFor;
    // syncmap.go:14 Value.
    public override V Value()
    {
        mu.Wait();
        try
        {
            return proxyFor is not null ? proxyFor.Value() : ValueLocked();
        }
        finally
        {
            mu.Release();
        }
    }

    // syncmap.go:66 ValueLocked.
    internal V ValueLocked() => deleted ? default! : value;
    // syncmap.go:22 Dirty.
    public override bool Dirty()
    {
        mu.Wait();
        try
        {
            return proxyFor is not null ? proxyFor.Dirty() : dirty;
        }
        finally
        {
            mu.Release();
        }
    }

    // syncmap.go:42 Locked.
    public void Locked(Action<IValue<V>> fn)
    {
        mu.Wait();
        try
        {
            if (proxyFor is not null)
                proxyFor.Locked(fn);
            else
                fn(new LockedEntry<K, V>(this));
        }
        finally
        {
            mu.Release();
        }
    }

    // syncmap.go:26 Change.
    public void Change(Action<V> apply)
    {
        mu.Wait();
        try
        {
            if (proxyFor is not null)
                proxyFor.Change(apply);
            else
                ChangeLocked(apply);
        }
        finally
        {
            mu.Release();
        }
    }

    // syncmap.go:103 ChangeLocked.
    internal void ChangeLocked(Action<V> apply)
    {
        if (dirty)
        {
            apply(value);
            return;
        }

        var(entry, loaded) = map.dirty.LoadOrStore(key, this);
        if (loaded)
            entry.mu.Wait();
        try
        {
            if (!entry.dirty)
            {
                entry.value = entry.value.Clone();
                entry.dirty = true;
            }

            if (loaded)
            {
                proxyFor = entry;
                value = entry.value;
                dirty = true;
                deleted = entry.deleted;
            }

            apply(entry.value);
        }
        finally
        {
            if (loaded)
                entry.mu.Release();
        }
    }

    // syncmap.go:30 ChangeIf.
    public bool ChangeIf(Func<V, bool> cond, Action<V> apply)
    {
        mu.Wait();
        try
        {
            if (proxyFor is not null)
                return proxyFor.ChangeIf(cond, apply);
            if (!cond(value))
                return false;
            ChangeLocked(apply);
            return true;
        }
        finally
        {
            mu.Release();
        }
    }

    // syncmap.go:38 Delete.
    public void Delete()
    {
        mu.Wait();
        try
        {
            if (proxyFor is not null)
            {
                proxyFor.Delete();
                return;
            }

            if (dirty)
            {
                deleted = true;
                return;
            }

            var(entry, loaded) = map.dirty.LoadOrStore(key, this);
            if (loaded)
                entry.mu.Wait();
            try
            {
                // Pinned Delete differs from deleteLocked: a losing entry marks
                // itself, without installing a proxy or changing the winning entry.
                if (loaded)
                    deleted = true;
                else
                    entry.deleted = true;
            }
            finally
            {
                if (loaded)
                    entry.mu.Release();
            }
        }
        finally
        {
            mu.Release();
        }
    }

    // syncmap.go:163 DeleteLocked.
    internal void DeleteLocked()
    {
        if (dirty)
        {
            deleted = true;
            return;
        }

        var(entry, loaded) = map.dirty.LoadOrStore(key, this);
        if (loaded)
            entry.mu.Wait();
        try
        {
            if (loaded)
            {
                proxyFor = entry;
                value = entry.value;
                deleted = true;
                dirty = entry.dirty;
            }

            entry.deleted = true;
        }
        finally
        {
            if (loaded)
                entry.mu.Release();
        }
    }

    // syncmap.go:180 DeleteIf.
    public void DeleteIf(Func<V, bool> cond)
    {
        mu.Wait();
        try
        {
            if (proxyFor is not null)
                proxyFor.DeleteIf(cond);
            else if (cond(value))
                DeleteLocked();
        }
        finally
        {
            mu.Release();
        }
    }
}

internal readonly record struct FinalizationHooks<K, V>(Action<K, V>? OnDelete = null, Action<K, V, V>? OnChange = null, Action<K, V>? OnAdd = null);
internal sealed class SyncMap<K, V>
    where K : notnull where V : ICloneable<V>
{
    private readonly Dictionary<K, V>? original;
    internal readonly TypeScript.Collections.SyncMap<K, SyncMapEntry<K, V>> dirty;
    private readonly IEqualityComparer<K> comparer;
    public SyncMap(Dictionary<K, V>? original, IEqualityComparer<K>? comparer = null)
    {
        this.original = original;
        this.comparer = original?.Comparer ?? comparer ?? EqualityComparer<K>.Default;
        dirty = new(this.comparer);
    }

    // syncmap.go:204 Load.
    public (SyncMapEntry<K, V>? Entry, bool Ok) Load(K key)
    {
        var(entry, ok) = dirty.Load(key);
        if (ok)
        {
            entry.mu.Wait();
            try
            {
                return entry.deleted ? (null, false) : (entry, true);
            }
            finally
            {
                entry.mu.Release();
            }
        }

        if (original is null || !original.TryGetValue(key, out var value))
            return (null, false);
        return (new() { map = this, key = key, original = value, value = value }, true);
    }

    // syncmap.go:226 LoadOrStore.
    public (SyncMapEntry<K, V>? Entry, bool Loaded) LoadOrStore(K key, V value)
    {
        if (original is not null && original.TryGetValue(key, out var baseValue))
        {
            var(existing, ok) = dirty.Load(key);
            if (ok)
            {
                existing.mu.Wait();
                try
                {
                    return existing.deleted ? (null, false) : (existing, true);
                }
                finally
                {
                    existing.mu.Release();
                }
            }

            return (new() { map = this, key = key, original = baseValue, value = baseValue }, true);
        }

        var(entry, loaded) = dirty.LoadOrStore(key, new() { map = this, key = key, value = value, dirty = true });
        if (loaded)
        {
            entry.mu.Wait();
            try
            {
                if (entry.deleted)
                    return (null, false);
            }
            finally
            {
                entry.mu.Release();
            }
        }

        return (entry, loaded);
    }

    // syncmap.go:38 Delete.
    public void Delete(K key)
    {
        var(entry, loaded) = dirty.LoadOrStore(key, new() { map = this, key = key, original = original is null ? default! : original.GetValueOrDefault(key)!, deleted = true });
        if (loaded)
            entry.Delete();
    }

    // syncmap.go:274 Range.
    public void Range(Func<SyncMapEntry<K, V>, bool> fn)
    {
        var seen = new HashSet<K>(comparer);
        dirty.Range((Seen: seen, Callback: fn), static (key, entry, state) =>
        {
            state.Seen.Add(key);
            entry.mu.Wait();
            bool deleted;
            try
            {
                deleted = entry.deleted;
            }
            finally
            {
                entry.mu.Release();
            }

            return deleted || state.Callback(entry);
        });
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

    private sealed class FinalizerState(SyncMap<K, V> owner, FinalizationHooks<K, V> hooks)
    {
        internal Dictionary<K, V>? Result = owner.original;
        internal bool Changed;
        internal readonly SyncMap<K, V> Owner = owner;
        internal readonly FinalizationHooks<K, V> Hooks = hooks;
    }

    // syncmap.go:308 FinalizeCore.
    private (Dictionary<K, V>? Value, bool Changed) FinalizeCore(FinalizationHooks<K, V> hooks)
    {
        var state = new FinalizerState(this, hooks);
        dirty.Range(state, static (key, entry, state) =>
        {
            entry.mu.Wait();
            try
            {
                if (!entry.deleted && !entry.dirty)
                    return true;
                if (!state.Changed)
                {
                    state.Result = state.Owner.original is null ? new(state.Owner.comparer) : new(state.Owner.original, state.Owner.comparer);
                    state.Changed = true;
                }

                if (entry.deleted)
                {
                    state.Hooks.OnDelete?.Invoke(key, entry.value);
                    state.Result!.Remove(key);
                }
                else
                {
                    if (state.Owner.original?.ContainsKey(key) == true)
                        state.Hooks.OnChange?.Invoke(key, entry.original, entry.value);
                    else
                        state.Hooks.OnAdd?.Invoke(key, entry.value);
                    state.Result![key] = entry.value;
                }

                return true;
            }
            finally
            {
                entry.mu.Release();
            }
        });
        return (state.Result, state.Changed);
    }

    // syncmap.go:308 FinalizeValue.
    public (Dictionary<K, V>? Value, bool Changed) FinalizeValue() => FinalizeCore(default);
    // syncmap.go:353 FinalizeWith.
    public (Dictionary<K, V>? Value, bool Changed) FinalizeWith(FinalizationHooks<K, V> hooks) => FinalizeCore(hooks);
}

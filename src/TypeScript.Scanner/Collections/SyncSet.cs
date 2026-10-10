// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/collections/syncset.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Collections;
public sealed class SyncSet<T> where T : notnull
{
    private readonly SyncMap<T, bool> map = new();
    public bool Has(T key) => map.Load(key).Ok;
    public void Add(T key) => AddIfAbsent(key);
    public bool AddIfAbsent(T key) => !map.LoadOrStore(key, false).Loaded;
    public void Delete(T key) => map.Delete(key);
    public void Range(Func<T, bool> callback) => map.Range((key, _) => callback(key));
    public int Size() => map.Size();
    public bool IsEmpty()
    {
        bool empty = true;
        map.Range((_, _) => { empty = false; return false; });
        return empty;
    }
    public List<T> ToSlice()
    {
        var result = new List<T>(map.Size());
        map.Range((key, _) => { result.Add(key); return true; });
        return result;
    }
    public IEnumerable<T> Keys() => map.Keys();
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/dirty/entry.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.ProjectSystem.Dirty;
internal class Entry<K, V>
    where K : notnull
{
    internal K key = default!;
    internal V original = default!;
    internal V value = default!;
    internal bool dirty, deleted;
    // entry.go:11 Key.
    public K Key() => key;
    // entry.go:15 Original.
    public V Original() => original;
    // entry.go:19 Value.
    public virtual V Value() => deleted ? default! : value;
    // entry.go:27 Dirty.
    public virtual bool Dirty() => dirty;
}

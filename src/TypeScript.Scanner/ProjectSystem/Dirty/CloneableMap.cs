// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/dirty/cloneablemap.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.ProjectSystem.Dirty;
// Value header preserves a nil map separately from an allocated empty map.
internal readonly struct CloneableMap<K, V>(Dictionary<K, V>? map) : ICloneable<CloneableMap<K, V>> where K : notnull
{
    public Dictionary<K, V>? Map { get; } = map;

    // cloneablemap.go:7 Clone.
    public CloneableMap<K, V> Clone() => new(Map is null ? null : new Dictionary<K, V>(Map, Map.Comparer));
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of collections/ordered_set.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Collections;
internal sealed class OrderedSet<T> where T : notnull
{
    // Go embeds OrderedMap; this owner retains the same map for its lifetime.
    private readonly OrderedMap<T, byte> m;
    internal OrderedSet() => m = new();
    private OrderedSet(OrderedMap<T, byte> map) => m = map;
    // ordered_set.go:11. Negative hint still fails through slice capacity, as Go.
    internal static OrderedSet<T> NewOrderedSetWithSizeHint(int hint) => new(new OrderedMap<T, byte>(hint));
    // ordered_set.go:18.
    internal void Add(T value) => m.Set(value, 0);
    // ordered_set.go:23.
    internal bool Has(T value) => m.Has(value);
    // ordered_set.go:28.
    internal bool Delete(T value) => m.Delete(value).Ok;
    // ordered_set.go:34.
    internal OrderedMap<T, byte>.KeySequence Values() => m.KeyValues();
    // ordered_set.go:40.
    internal void Clear() => m.Clear();
    // ordered_set.go:45.
    internal int Size() => m.Size();
    // ordered_set.go:50. Both map and ordered keys are copied by genuine clone.
    internal OrderedSet<T> Clone() => new(m.Clone());
}

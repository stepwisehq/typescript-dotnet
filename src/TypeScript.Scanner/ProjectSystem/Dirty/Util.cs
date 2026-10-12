// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/dirty/util.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.ProjectSystem.Dirty;
internal static class DirtyUtilities
{
    // util.go:5 CloneMapIfNil.
    public static Dictionary<K, V> CloneMapIfNil<K, V, T>(T dirty, T? original, Func<T, Dictionary<K, V>?> getMap, IEqualityComparer<K>? comparer = null)
        where K : notnull where T : class
    {
        var dirtyMap = getMap(dirty);
        if (dirtyMap is not null)
            return dirtyMap;
        var originalMap = original is null ? null : getMap(original);
        return originalMap is null ? new(comparer) : new(originalMap, originalMap.Comparer);
    }
}

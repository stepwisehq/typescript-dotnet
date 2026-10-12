// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;

namespace TypeScript.Scanner;
internal static partial class Core
{
    // internal/core/core.go:778. Nil dictionaries retain Go nil-range semantics.
    internal static void DiffMapsFunc<K, V1, V2>(Dictionary<K, V1>? first, Dictionary<K, V2>? second, Func<V1, V2, bool> equalValues, Action<K, V2>? onAdded, Action<K, V1>? onRemoved, Action<K, V1, V2>? onChanged)
        where K : notnull
    {
        if (onAdded != null && second != null)
            foreach (var(key, value)in second)
                if (first == null || !first.ContainsKey(key))
                    onAdded(key, value);
        if (onChanged == null && onRemoved == null)
            return;
        if (first == null)
            return;
        foreach (var(key, value)in first)
        {
            if (second != null && second.TryGetValue(key, out var other))
            {
                if (onChanged != null && !equalValues(value, other))
                    onChanged(key, value, other);
            }
            else
                onRemoved!(key, value);
        }
    }
}

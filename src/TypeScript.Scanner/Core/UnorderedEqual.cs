// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;

namespace TypeScript.Scanner;
internal static partial class Core
{
    // internal/core/core.go:811. Count duplicates rather than substituting set equality.
    internal static bool UnorderedEqual<T>(Slice<T> first, Slice<T> second)
        where T : notnull
    {
        if (first.Count != second.Count)
            return false;
        var counts = new Dictionary<T, int>();
        foreach (var value in first)
        {
            counts.TryGetValue(value, out var count);
            counts[value] = count + 1;
        }

        foreach (var value in second)
        {
            counts.TryGetValue(value, out var count);
            counts[value] = count - 1;
            if (counts[value] < 0)
                return false;
        }

        return true;
    }
}

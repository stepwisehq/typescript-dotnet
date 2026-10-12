// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
namespace TypeScript.Scanner;
internal static partial class Core
{
    // Upstream internal/core/binarysearch.go:8. Only one element can match;
    // comparator receives its current index, not a separately stored target.
    public static (int Index, bool Found) BinarySearchUniqueFunc<T>(Slice<T> values, Func<int, T, int> compare)
    {
        int n = values.Count;
        if (n == 0)
            return (0, false);
        int low = 0, high = n - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) >> 1);
            int value = compare(middle, values[middle]);
            if (value < 0)
                low = middle + 1;
            else if (value > 0)
                high = middle - 1;
            else
                return (middle, true);
        }

        return (low, false);
    }
}

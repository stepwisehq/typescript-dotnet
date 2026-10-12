// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/core/core.go:357; explicit state callback is an allocation-preserving managed adaptation.
namespace TypeScript.Scanner;
internal static partial class Core
{
    internal static Slice<T> MinAllFunc<T>(Slice<T> values, Func<T, T, int> compare) => MinAllFunc(values, compare, static (left, right, callback) => callback(left, right));
    internal static Slice<T> MinAllFunc<T, TState>(Slice<T> values, TState state, Func<T, T, TState, int> compare)
    {
        if (values.Count == 0)
            return default;
        var minimum = values[0];
        var minima = Slice<T>.Make(1);
        minima[0] = minimum;
        foreach (var value in values.Slice2(1))
        {
            var comparison = compare(value, minimum, state);
            if (comparison < 0)
            {
                minimum = value;
                minima = minima.Slice2(0, 0).Append(value);
            }
            else if (comparison == 0)
                minima = minima.Append(value);
        }

        return minima;
    }
}

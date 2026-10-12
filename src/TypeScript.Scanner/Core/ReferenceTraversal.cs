// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Focused core/core.go:117 MapNonNil explicit-state callback adaptation for the
// reference traversal callers. Required output header/growth remains unchanged.
namespace TypeScript.Scanner;
internal static partial class Core
{
    internal static Slice<U> MapNonNilSlice<T, U, TState>(Slice<T> values, TState state, Func<T, TState, U?> map)
        where U : class
    {
        Slice<U> result = default;
        U zero = typeof(U) == typeof(string) ? (U)(object)string.Empty : default!;
        foreach (var value in values)
        {
            var mapped = map(value, state);
            if (!EqualityComparer<U>.Default.Equals(mapped, zero))
                result = result.Append(mapped!);
        }

        return result;
    }
}

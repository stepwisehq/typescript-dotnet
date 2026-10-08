// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Managed object allocation adapter for binder uses of core.Arena.New; no reflection.
namespace TypeScript.Scanner;
internal sealed class GoObjectArena<T>
    where T : class, new()
{
    public T New() => new();
}

internal static partial class Strconv
{
    public static string Itoa(long value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public static string FormatUint(ulong value, int numberBase)
    {
        if (numberBase != 10)
            throw new ArgumentOutOfRangeException(nameof(numberBase), "Binder uses decimal identity formatting only.");
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

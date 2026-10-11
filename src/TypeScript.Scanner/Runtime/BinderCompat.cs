// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Managed identity formatting adapters for binder uses of strconv.
namespace TypeScript.Scanner;
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

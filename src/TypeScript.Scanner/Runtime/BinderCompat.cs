// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Managed adapters for required Go strconv integer formatting.
namespace TypeScript.Scanner;
internal static partial class Strconv
{
    public static string Itoa(long value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public static string FormatUint(ulong value, int numberBase)
    {
        // Go strconv.FormatUint accepts every radix from 2 through 36.
        if (numberBase < 2 || numberBase > 36)
            throw new ArgumentOutOfRangeException(nameof(numberBase), "strconv: illegal AppendInt/FormatInt base");
        if (numberBase == 10)
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (numberBase == 16)
            return value.ToString("x", System.Globalization.CultureInfo.InvariantCulture);
        // A uint64 needs at most 64 digits (base 2). Fill backwards, then own
        // exactly the resulting text; no retained scratch owner or intermediate string.
        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        Span<char> buffer = stackalloc char[64];
        int position = buffer.Length;
        ulong radix = (ulong)numberBase;
        do
        {
            ulong quotient = value / radix;
            buffer[--position] = digits[(int)(value - quotient * radix)];
            value = quotient;
        } while (value != 0);
        return new string(buffer[position..]);
    }
}

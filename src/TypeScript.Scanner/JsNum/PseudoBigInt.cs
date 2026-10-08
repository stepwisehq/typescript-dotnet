// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/jsnum/pseudobigint.go, scanner-only ParsePseudoBigInt slice.
using System.Numerics;
using System.Globalization;

namespace TypeScript.Scanner;
internal static partial class JsNum
{
    public static string ParsePseudoBigInt(string stringValue)
    {
        if (stringValue.EndsWith('n'))
            stringValue = stringValue[..^1];
        char b1 = stringValue.Length > 1 ? stringValue[1] : '\0';
        int radix = b1 is 'b' or 'B' ? 2 : b1 is 'o' or 'O' ? 8 : b1 is 'x' or 'X' ? 16 : 10;
        if (radix == 10)
        {
            stringValue = stringValue.TrimStart('0');
            return stringValue == "" ? "0" : stringValue;
        }

        BigInteger bi = 0;
        foreach (char ch in stringValue[2..])
        {
            int digit = ch is >= '0' and <= '9' ? ch - '0' : ch is >= 'a' and <= 'f' ? ch - 'a' + 10 : ch is >= 'A' and <= 'F' ? ch - 'A' + 10 : -1;
            if (digit < 0 || digit >= radix)
                throw new InvalidOperationException("Failed to parse big int: " + stringValue);
            bi = bi * radix + digit;
        }

        return bi.ToString(CultureInfo.InvariantCulture);
    }
}

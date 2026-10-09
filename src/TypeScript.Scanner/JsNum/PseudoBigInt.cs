// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/jsnum/pseudobigint.go, parsing plus original scalar pseudo-bigint value model.
using System.Numerics;
using System.Globalization;

namespace TypeScript.Scanner;
internal static partial class JsNum
{
    public readonly record struct PseudoBigInt(bool Negative, string Base10Value)
    {
        public string String() => string.IsNullOrEmpty(Base10Value) ? "0" : Negative ? "-" + Base10Value : Base10Value;
        public int Sign() => string.IsNullOrEmpty(Base10Value) ? 0 : Negative ? -1 : 1;
        public int Compare(PseudoBigInt other)
        {
            int c = Sign().CompareTo(other.Sign());
            if (c != 0)
                return c;
            c = (Base10Value?.Length ?? 0).CompareTo(other.Base10Value?.Length ?? 0);
            if (c == 0)
                c = string.CompareOrdinal(Base10Value ?? "", other.Base10Value ?? "");
            return Negative ? -c : c;
        }

        // The managed default string is null; Go's zero string is empty. Treat
        // those backing representations identically in the value's equality.
        public bool Equals(PseudoBigInt other) => Negative == other.Negative && (Base10Value ?? "") == (other.Base10Value ?? "");
        public override int GetHashCode() => HashCode.Combine(Negative, Base10Value ?? "");
        public override string ToString() => String();
    }

    public static PseudoBigInt NewPseudoBigInt(string value, bool negative)
    {
        value = value.TrimStart('0');
        return new(negative && value.Length != 0, value);
    }

    public static PseudoBigInt ParseValidBigInt(string text)
    {
        bool negative = text.StartsWith('-');
        if (negative)
            text = text[1..];
        return NewPseudoBigInt(ParsePseudoBigInt(text), negative);
    }

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

        // Go big.Int.SetString(value, 0) permits underscores between digits and
        // immediately after the base prefix, but never consecutive or trailing.
        if (stringValue[0] != '0' || stringValue.Length <= 2)
            throw new InvalidOperationException("Failed to parse big int: " + stringValue);
        BigInteger bi = 0;
        bool previousUnderscore = false;
        bool hasDigit = false;
        for (int i = 2; i < stringValue.Length; i++)
        {
            char ch = stringValue[i];
            if (ch == '_')
            {
                if (previousUnderscore || i == stringValue.Length - 1)
                    throw new InvalidOperationException("Failed to parse big int: " + stringValue);
                previousUnderscore = true;
                continue;
            }

            int digit = ch is >= '0' and <= '9' ? ch - '0' : ch is >= 'a' and <= 'f' ? ch - 'a' + 10 : ch is >= 'A' and <= 'F' ? ch - 'A' + 10 : -1;
            if (digit < 0 || digit >= radix)
                throw new InvalidOperationException("Failed to parse big int: " + stringValue);
            bi = bi * radix + digit;
            previousUnderscore = false;
            hasDigit = true;
        }

        if (!hasDigit)
            throw new InvalidOperationException("Failed to parse big int: " + stringValue);
        return bi.ToString(CultureInfo.InvariantCulture);
    }
}

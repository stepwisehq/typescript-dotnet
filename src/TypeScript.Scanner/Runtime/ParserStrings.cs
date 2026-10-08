// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of Go string/unicode operations used by pinned tsc/parser.
namespace TypeScript.Scanner;
internal static partial class GoStrings
{
    public static int LastIndex(string value, string text) => value.LastIndexOf(text, StringComparison.Ordinal);
    public static string TrimSuffix(string value, string suffix) => HasSuffix(value, suffix) ? value[..^suffix.Length] : value;
    public static string TrimRightFunc(string value, Func<int, bool> predicate)
    {
        int end = value.Length;
        while (end > 0)
        {
            var(rune, size) = Utf8.DecodeLastRuneInString(value.AsSpan(0, end));
            if (!predicate(rune))
                break;
            end -= size;
        }

        return value[..end];
    }

    public static string TrimLeftFunc(string value, Func<int, bool> predicate)
    {
        int start = 0;
        while (start < value.Length)
        {
            var(rune, size) = Utf8.DecodeRuneInString(value.AsSpan(start));
            if (!predicate(rune))
                break;
            start += size;
        }

        return value[start..];
    }
}

internal static class GoUnicode
{
    // Go unicode.IsSpace's Latin1 fast path and White_Space table.
    public static bool IsSpace(int rune) => rune is 9 or 10 or 11 or 12 or 13 or 32 or 0x85 or 0xa0 or 0x1680 or >= 0x2000 and <= 0x200a or 0x2028 or 0x2029 or 0x202f or 0x205f or 0x3000;
}

internal static partial class Strconv
{
    public static string Itoa(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

internal static partial class Debug
{
    public static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Runtime adaptations for the C# translation of tsc/internal/scanner/scanner.go.
using System.Globalization;
using System.Numerics;

namespace TypeScript.Scanner;
internal static partial class GoStrings
{
    public static int Index(string value, string text) => value.IndexOf(text, StringComparison.Ordinal);
    public static int IndexByte(ReadOnlySpan<char> value, int ch) => value.IndexOf((char)ch);
    public static bool HasPrefix(ReadOnlySpan<char> value, string prefix) => value.StartsWith(prefix, StringComparison.Ordinal);
    public static int IndexByte(string value, int ch) => value.IndexOf((char)ch);
    public static bool HasPrefix(string value, string prefix) => value.StartsWith(prefix, StringComparison.Ordinal);
    public static bool HasSuffix(string value, string suffix) => value.EndsWith(suffix, StringComparison.Ordinal);
    public static bool Contains(string value, string text) => value.Contains(text, StringComparison.Ordinal);
    public static string ReplaceAll(string value, string oldValue, string newValue) => value.Replace(oldValue, newValue, StringComparison.Ordinal);
    public static string ToLower(string value) => value.ToLowerInvariant();
    public static string Join(IEnumerable<string> values, string separator) => string.Join(separator, values);
    public static int Compare(string left, string right) => string.CompareOrdinal(left, right);
    public static string TrimLeft(string value, string cutset) => value.TrimStart(cutset.ToCharArray());
}

internal static partial class Strconv
{
    public static (long Value, bool Error) ParseInt(string value, int radix, int bitSize)
    {
        if (value.Length == 0)
            return (0, true);
        BigInteger result = 0;
        foreach (char c in value)
        {
            int digit = c is >= '0' and <= '9' ? c - '0' : c is >= 'a' and <= 'z' ? c - 'a' + 10 : c is >= 'A' and <= 'Z' ? c - 'A' + 10 : -1;
            if (digit < 0 || digit >= radix)
                return (0, true);
            result = result * radix + digit;
        }

        BigInteger maximum = (BigInteger.One << bitSize - 1) - 1;
        return result > maximum ? ((long)maximum, true) : ((long)result, false);
    }

    public static string FormatInt(long value, int radix) => radix == 10 ? value.ToString(CultureInfo.InvariantCulture) : Convert.ToString(value, radix);
}

internal static class Fmt
{
    public static string Sprintf(string pattern, params object[] args)
    {
        var result = new System.Text.StringBuilder();
        int argument = 0;
        for (int i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] != '%' || i + 1 == pattern.Length)
            {
                result.Append(pattern[i]);
                continue;
            }

            if (pattern.AsSpan(i).StartsWith("%02x"))
            {
                result.Append(Convert.ToInt64(args[argument++], CultureInfo.InvariantCulture).ToString("x2", CultureInfo.InvariantCulture));
                i += 3;
            }
            else if (pattern[i + 1] is 'd' or 's')
            {
                result.Append(Convert.ToString(args[argument++], CultureInfo.InvariantCulture));
                i++;
            }
            else
            {
                result.Append(pattern[i]);
            }
        }

        return result.ToString();
    }
}

internal static partial class Debug
{
    public static void Assert(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("Assertion failed");
    }
}

internal static class Utf16
{
    public static int RuneLen(int rune) => rune < 0 || rune > 0x10FFFF ? -1 : rune >= 0x10000 ? 2 : 1;
}

internal static class ScannerString
{
    public static string ToUtf8String(string bytes)
    {
        var value = new System.Text.StringBuilder();
        for (int i = 0; i < bytes.Length;)
        {
            var(rune, size) = Utf8.DecodeRuneInString(bytes.AsSpan(i));
            value.Append(char.ConvertFromUtf32(rune));
            i += size;
        }

        return value.ToString();
    }

    // Go stores isolated JavaScript surrogates in CESU-8; preserve them in .NET UTF-16.
    public static string ToJsString(string bytes)
    {
        var value = new System.Text.StringBuilder();
        for (int i = 0; i < bytes.Length;)
        {
            var(rune, size) = StringUtil.DecodeJSStringRune(bytes.AsSpan(i));
            if (rune <= 0xFFFF)
                value.Append((char)rune);
            else
                value.Append(char.ConvertFromUtf32(rune));
            i += size;
        }

        return value.ToString();
    }
}

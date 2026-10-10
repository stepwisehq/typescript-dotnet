// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of Go string/unicode operations used by pinned tsc/parser.
namespace TypeScript.Scanner;
internal static partial class GoStrings
{
    // Go strings operations for module naming. Managed characters represent Go bytes.
    public static (string Before, string After, bool Found) Cut(string s, string separator)
    {
        int index = s.IndexOf(separator, StringComparison.Ordinal);
        return index < 0 ? (s, "", false) : (s[..index], s[(index + separator.Length)..], true);
    }

    public static (string Before, string After, bool Found) CutLast(string s, string separator)
    {
        int index = s.LastIndexOf(separator, StringComparison.Ordinal);
        return index < 0 ? (s, "", false) : (s[..index], s[(index + separator.Length)..], true);
    }

    public static int Count(string s, string separator)
    {
        if (separator.Length == 0)
        {
            int count = 1;
            for (int index = 0; index < s.Length; count++)
                index += Utf8.DecodeRuneInString(s.AsSpan(index)).Size;
            return count;
        }
        int matches = 0;
        for (int start = 0; ;)
        {
            int index = s.IndexOf(separator, start, StringComparison.Ordinal);
            if (index < 0)
                return matches;
            matches++;
            start = index + separator.Length;
        }
    }

    public static List<string> Split(string s, string separator)
    {
        if (separator.Length != 0)
            return s.Split(separator, StringSplitOptions.None).ToList();
        var result = new List<string>();
        for (int index = 0; index < s.Length;)
        {
            int size = Utf8.DecodeRuneInString(s.AsSpan(index)).Size;
            result.Add(s.Substring(index, size));
            index += size;
        }
        return result;
    }

    public static string Replace(string s, string oldValue, string newValue, int n)
    {
        if (oldValue == newValue || n == 0)
            return s;
        int count = Count(s, oldValue);
        if (count == 0)
            return s;
        if (n < 0 || count < n)
            n = count;
        var builder = new GoBuilder();
        int start = 0;
        for (int i = 0; i < n; i++)
        {
            int index = start;
            if (oldValue.Length == 0)
            {
                if (i > 0)
                    index += Utf8.DecodeRuneInString(s.AsSpan(start)).Size;
            }
            else
                index += s[start..].IndexOf(oldValue, StringComparison.Ordinal);
            builder.WriteString(s[start..index]);
            builder.WriteString(newValue);
            start = index + oldValue.Length;
        }
        builder.WriteString(s[start..]);
        return builder.String();
    }

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

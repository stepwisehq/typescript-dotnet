// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of Go string/unicode operations used by pinned tsc/parser.
namespace TypeScript.Scanner;
internal static partial class GoStrings
{
    // Narrow view counterparts of Go's substring-returning text operations.
    public static ByteStringView TrimSuffix(ByteStringView value, string suffix)
        => HasSuffix(value.AsSpan(), suffix)
            ? value.Slice(0, value.Length - suffix.Length) : value;
    public static ByteStringView TrimSuffix(ByteStringView value, ReadOnlySpan<byte> suffix)
        => HasSuffix(value.AsSpan(), suffix)
            ? value.Slice(0, value.Length - suffix.Length) : value;
    public static ByteStringValue TrimRightFunc(ByteStringValue value, Func<int, bool> predicate)
    {
        if (value.IsNull) throw new NullReferenceException();
        int end = value.Length;
        while (end > 0)
        {
            var (rune, size) = value.IsBytes ? Utf8.DecodeLastRuneInString(value.ByteSpan[..end])
                : Utf8.DecodeLastRuneInString(value.CharSpan[..end]);
            if (!predicate(rune)) break;
            end -= size;
        }
        return value.Slice(0, end);
    }
    public static ByteStringView TrimRightFunc(ByteStringView value, Func<int, bool> predicate)
    {
        int end = value.Length;
        while (end > 0)
        {
            var (rune, size) = Utf8.DecodeLastRuneInString(value.AsSpan(0, end));
            if (!predicate(rune)) break;
            end -= size;
        }
        return value.Slice(0, end);
    }
    public static ByteStringView TrimLeftFunc(ByteStringView value, Func<int, bool> predicate)
    {
        int start = 0;
        while (start < value.Length)
        {
            var (rune, size) = Utf8.DecodeRuneInString(value.AsSpan(start));
            if (!predicate(rune)) break;
            start += size;
        }
        return value.Slice(start);
    }
    public static string Join(List<ByteStringView> values, string separator)
    {
        if (values == null || values.Count == 0) return "";
        if (values.Count == 1) return values[0].Materialize();
        int length = checked(separator.Length * (values.Count - 1));
        foreach (var value in values) length = checked(length + value.Length);
        return string.Create(length, (values, separator), static (destination, state) =>
        {
            int pos = 0;
            for (int i = 0; i < state.values.Count; i++)
            {
                if (i > 0)
                {
                    state.separator.AsSpan().CopyTo(destination[pos..]);
                    pos += state.separator.Length;
                }
                var source = state.values[i].AsSpan();
                System.Text.Encoding.Latin1.GetChars(source, destination.Slice(pos, source.Length));
                pos += source.Length;
            }
        });
    }
    public static string Join(Slice<ByteStringView> values, string separator)
    {
        if (values.Length == 0) return "";
        if (values.Length == 1) return values[0].Materialize();
        int length = checked(separator.Length * (values.Length - 1));
        foreach (var value in values) length = checked(length + value.Length);
        return string.Create(length, (values, separator), static (destination, state) =>
        {
            int pos = 0;
            for (int i = 0; i < state.values.Length; i++)
            {
                if (i > 0)
                {
                    state.separator.AsSpan().CopyTo(destination[pos..]);
                    pos += state.separator.Length;
                }
                var source = state.values[i].AsSpan();
                System.Text.Encoding.Latin1.GetChars(source, destination.Slice(pos, source.Length));
                pos += source.Length;
            }
        });
    }

    // strings.Join constructs one owned byte result. Internal indentation text
    // can retain that result without a full raw-string encode/decode roundtrip.
    public static ByteStringView JoinViews(Slice<ByteStringView> values, string separator)
    {
        if (values.Length == 0) return "";
        if (values.Length == 1) return values[0];
        int length = checked(separator.Length * (values.Length - 1));
        foreach (var value in values) length = checked(length + value.Length);
        var result = new byte[length];
        int pos = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                for (int j = 0; j < separator.Length; j++) result[pos++] = checked((byte)separator[j]);
            }
            var source = values[i].AsSpan();
            source.CopyTo(result.AsSpan(pos));
            pos += source.Length;
        }
        return ByteStringView.FromOwnedBytes(result);
    }

    // Go strings operations for module naming. Managed characters represent Go bytes.
    public static (string Before, string After, bool Found) Cut(string s, string separator)
    {
        int index = s.IndexOf(separator, StringComparison.Ordinal);
        return index < 0 ? (s, "", false) : (s[..index], s[(index + separator.Length)..], true);
    }
    public static (ByteStringValue Before, ByteStringValue After, bool Found) Cut(ByteStringValue value, ByteStringValue separator)
    {
        int index = value.IndexOf(separator);
        return index < 0 ? (value, default, false) : (value.Slice(0, index), value.Slice(index + separator.Length), true);
    }
    public static (string Before, string After, bool Found) CutLast(string s, string separator)
    {
        int index = s.LastIndexOf(separator, StringComparison.Ordinal);
        return index < 0 ? (s, "", false) : (s[..index], s[(index + separator.Length)..], true);
    }
    public static (ByteStringValue Before, ByteStringValue After, bool Found) CutLast(ByteStringValue value, ByteStringValue separator)
    {
        int index = value.LastIndexOf(separator);
        return index < 0 ? (value, default, false) : (value.Slice(0, index), value.Slice(index + separator.Length), true);
    }
    public static int Count(string s, string separator) => Count(ByteStringValue.FromString(s), ByteStringValue.FromString(separator));
    public static int Count(ByteStringValue value, string separator) => Count(value, ByteStringValue.FromString(separator));
    public static int Count(ByteStringValue value, ByteStringValue separator)
    {
        if (separator.IsNull || value.IsNull) throw new NullReferenceException();
        if (separator.Length == 0)
        {
            int count = 1;
            for (int index = 0; index < value.Length; count++)
                index += decodeStringRune(value, index).Size;
            return count;
        }
        int matches = 0;
        for (int start = 0; ;)
        {
            int index = value.IndexOf(separator, start);
            if (index < 0)
                return matches;
            matches++;
            start = index + separator.Length;
        }
    }
    private static (int Rune, int Size) decodeStringRune(ByteStringValue value, int index)
        => value.IsBytes ? Utf8.DecodeRuneInString(value.ByteSpan[index..]) : Utf8.DecodeRuneInString(value.CharSpan[index..]);
    public static Slice<ByteStringValue> Split(ByteStringValue value, string separator)
    {
        var separatorValue = ByteStringValue.FromString(separator);
        if (separatorValue.IsNull || value.IsNull) throw new NullReferenceException();
        // strings.Split's empty separator explodes by decoded UTF8 runes.
        if (separatorValue.Length == 0)
        {
            var result = Slice<ByteStringValue>.Make(Count(value, separatorValue) - 1);
            int index = 0;
            for (int i = 0; i < result.Length; i++)
            {
                int size = decodeStringRune(value, index).Size;
                result[i] = value.Slice(index, index + size);
                index += size;
            }
            return result;
        }
        var parts = Slice<ByteStringValue>.Make(Count(value, separatorValue) + 1);
        int start = 0;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            int index = value.IndexOf(separatorValue, start);
            parts[i] = value.Slice(start, index);
            start = index + separatorValue.Length;
        }
        parts[parts.Length - 1] = value.Slice(start);
        return parts;
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
        => Replace(ByteStringValue.FromString(s), ByteStringValue.FromString(oldValue), ByteStringValue.FromString(newValue), n).Materialize()!;
    public static ByteStringValue Replace(ByteStringValue value, string oldValue, ByteStringValue newValue, int n)
        => Replace(value, ByteStringValue.FromString(oldValue), newValue, n);
    public static ByteStringValue Replace(ByteStringValue value, ByteStringValue oldValue, ByteStringValue newValue, int n)
    {
        if (oldValue.EqualsValue(newValue) || n == 0)
            return value;
        int count = Count(value, oldValue);
        if (count == 0)
            return value;
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
                    index += decodeStringRune(value, start).Size;
            }
            else
                index = value.IndexOf(oldValue, start);
            builder.WriteString(value.Slice(start, index));
            builder.WriteString(newValue);
            start = index + oldValue.Length;
        }
        builder.WriteString(value.Slice(start));
        return builder.Value();
    }

    public static int LastIndex(string value, string text) => value.LastIndexOf(text, StringComparison.Ordinal);
    public static string TrimSuffix(string value, string suffix) => HasSuffix(value, suffix) ? value[..^suffix.Length] : value;
    public static ByteStringValue TrimSuffix(ByteStringValue value, ByteStringValue suffix)
        => value.EndsWith(suffix) ? value.Slice(0, value.Length - suffix.Length) : value;
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Runtime adaptations for the C# translation of tsc/internal/scanner/scanner.go.
using System.Globalization;
using System.Numerics;

namespace TypeScript.Scanner;
internal static partial class GoStrings
{
    public static int Index(string value, string text) => value.IndexOf(text, StringComparison.Ordinal);
    public static int IndexByte(ReadOnlySpan<byte> value, int ch) => value.IndexOf((byte)ch);
    // Go strings byte comparisons: static patterns borrow u8 literals directly.
    public static bool HasPrefix(ReadOnlySpan<byte> value, ReadOnlySpan<byte> prefix) => value.StartsWith(prefix);
    public static bool HasSuffix(ReadOnlySpan<byte> value, ReadOnlySpan<byte> suffix) => value.EndsWith(suffix);
    public static int Index(ReadOnlySpan<byte> value, ReadOnlySpan<byte> part) => value.IndexOf(part);
    public static bool Equal(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) => left.SequenceEqual(right);
    // Dynamic raw-string compatibility keeps char-per-byte comparisons. Do not
    // UTF8-encode these strings: their non-ASCII characters already denote bytes.
    public static bool HasPrefix(ReadOnlySpan<byte> value, string prefix)
    {
        if (value.Length < prefix.Length) return false;
        for (int i = 0; i < prefix.Length; i++) if (value[i] != prefix[i]) return false;
        return true;
    }
    public static bool HasSuffix(ReadOnlySpan<byte> value, string suffix)
        => value.Length >= suffix.Length && HasPrefix(value[(value.Length - suffix.Length)..], suffix);
    public static int Index(ReadOnlySpan<byte> value, string part)
    {
        for (int i = 0; i <= value.Length - part.Length; i++)
            if (HasPrefix(value[i..], part)) return i;
        return -1;
    }
    // Go strings.ContainsFunc iterates decoded UTF8 runes, not byte values.
    public static bool ContainsFunc(ReadOnlySpan<byte> value, Func<int, bool> predicate)
    {
        for (int pos = 0; pos < value.Length;)
        {
            var (rune, size) = Utf8.DecodeRuneInString(value[pos..]);
            if (predicate(rune)) return true;
            pos += size;
        }
        return false;
    }
    public static int IndexByte(ReadOnlySpan<char> value, int ch) => value.IndexOf((char)ch);
    public static bool HasPrefix(ReadOnlySpan<char> value, string prefix) => value.StartsWith(prefix, StringComparison.Ordinal);
    public static int IndexByte(string value, int ch) => value.IndexOf((char)ch);
    public static bool HasPrefix(string value, string prefix) => value.StartsWith(prefix, StringComparison.Ordinal);
    public static bool HasSuffix(string value, string suffix) => value.EndsWith(suffix, StringComparison.Ordinal);
    public static bool Contains(string value, string text) => value.Contains(text, StringComparison.Ordinal);
    public static string ReplaceAll(string value, string oldValue, string newValue) => value.Replace(oldValue, newValue, StringComparison.Ordinal);
    public static string ToLower(string value) => value.ToLowerInvariant();
    // Go strings.go:708 ToLower and Map: share unchanged input; allocate only
    // at the first mapped rune/invalid UTF8 byte, preserving consume-one errors.
    public static ByteStringValue ToLower(ByteStringValue value) => toLower(value, false);
    // tspath preserves U+0130, unlike ordinary Go strings.ToLower. Both use
    // the same first-change map/invalid-rune algorithm and unchanged header.
    internal static ByteStringValue ToFileNameLowerCase(ByteStringValue value) => toLower(value, true);
    private static ByteStringValue toLower(ByteStringValue value, bool preserveIWithDot)
    {
        if (value.IsNull) throw new NullReferenceException();
        bool isAscii = true, hasUpper = false;
        for (int i = 0; i < value.Length; i++)
        {
            int c = value[i];
            if (c >= Utf8.RuneSelf) { isAscii = false; break; }
            hasUpper |= c is >= 'A' and <= 'Z';
        }
        if (isAscii)
        {
            if (!hasUpper) return value;
            var builder = new GoBuilder();
            builder.Grow(value.Length);
            int pos = 0;
            for (int i = 0; i < value.Length; i++)
            {
                int c = value[i];
                if (c is >= 'A' and <= 'Z')
                {
                    if (pos < i) builder.WriteString(value.Slice(pos, i));
                    builder.WriteByte(c + 'a' - 'A');
                    pos = i + 1;
                }
            }
            if (pos < value.Length) builder.WriteString(value.Slice(pos));
            return builder.Value();
        }
        GoBuilder? output = null;
        for (int i = 0; i < value.Length;)
        {
            var (rune, width) = value.IsBytes ? Utf8.DecodeRuneInString(value.ByteSpan[i..])
                : Utf8.DecodeRuneInString(value.CharSpan[i..]);
            int mapped = preserveIWithDot && rune == 0x130 ? rune : UnicodeCase.ToLower(rune);
            if (output is null && (mapped != rune || rune == Utf8.RuneError && width == 1))
            {
                output = new GoBuilder();
                output.Grow(checked(value.Length + 4));
                output.WriteString(value.Slice(0, i));
            }
            if (output is not null)
            {
                if (mapped < Utf8.RuneSelf) output.WriteByte(mapped);
                else output.WriteRune(mapped);
            }
            i += width;
        }
        return output is null ? value : output.Value();
    }
    public static string Join(IEnumerable<string> values, string separator) => string.Join(separator, values);
    public static int Compare(string left, string right) => string.CompareOrdinal(left, right);
    public static string TrimLeft(string value, string cutset) => value.TrimStart(cutset.ToCharArray());
}

internal static partial class Strconv
{
    public static (long Value, bool Error) ParseInt(string value, int radix, int bitSize)
    {
        _ = value.Length; // Preserve the public raw-string null failure.
        return ParseInt(value.AsSpan(), radix, bitSize);
    }
    public static (long Value, bool Error) ParseInt(ReadOnlySpan<char> value, int radix, int bitSize) =>
        parseInt(value.Length, value, default, false, radix, bitSize);
    public static (long Value, bool Error) ParseInt(ReadOnlySpan<byte> value, int radix, int bitSize) =>
        parseInt(value.Length, default, value, true, radix, bitSize);
    public static (long Value, bool Error) ParseInt(ByteStringValue value, int radix, int bitSize) =>
        value.IsBytes ? ParseInt(value.ByteSpan, radix, bitSize) : ParseInt(value.CharSpan, radix, bitSize);
    private static (long Value, bool Error) parseInt(int length, ReadOnlySpan<char> chars,
        ReadOnlySpan<byte> bytes, bool isBytes, int radix, int bitSize)
    {
        if (length == 0)
            return (0, true);
        BigInteger result = 0;
        for (int i = 0; i < length; i++)
        {
            int c = isBytes ? bytes[i] : chars[i];
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
    public static string ToUtf8String(string bytes) => ToUtf8String(ByteStringValue.FromString(bytes));
    public static string ToUtf8String(ByteStringValue bytes)
    {
        if (bytes.IsNull) throw new NullReferenceException();
        var value = new System.Text.StringBuilder();
        for (int i = 0; i < bytes.Length;)
        {
            var(rune, size) = bytes.IsBytes ? Utf8.DecodeRuneInString(bytes.ByteSpan.Slice(i))
                : Utf8.DecodeRuneInString(bytes.CharSpan.Slice(i));
            value.Append(char.ConvertFromUtf32(rune));
            i += size;
        }

        return value.ToString();
    }

    // Go stores isolated JavaScript surrogates in CESU-8; preserve them in .NET UTF-16.
    public static string ToJsString(string bytes)
    {
        _ = bytes.Length;
        return ToJsString(ByteStringValue.FromString(bytes));
    }
    public static string ToJsString(ByteStringValue bytes)
    {
        if (bytes.IsNull) throw new NullReferenceException();
        var value = new System.Text.StringBuilder();
        for (int i = 0; i < bytes.Length;)
        {
            var (rune, size) = bytes.IsBytes ? StringUtil.DecodeJSStringRune(bytes.ByteSpan[i..])
                : StringUtil.DecodeJSStringRune(bytes.CharSpan[i..]);
            if (rune <= 0xFFFF) value.Append((char)rune);
            else value.Append(char.ConvertFromUtf32(rune));
            i += size;
        }
        return value.ToString();
    }

}

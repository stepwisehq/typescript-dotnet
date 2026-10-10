// New .NET compatibility support; not an upstream source port.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// .NET adaptation for Go byte strings. Internal strings contain one char per byte.
using System.Text;

namespace TypeScript.Scanner;
internal static class GoString
{
    public static string FromRune(int rune)
    {
        if (rune < 0 || rune > 0x10FFFF || rune is >= 0xD800 and <= 0xDFFF)
            rune = 0xFFFD;
        Span<byte> bytes = stackalloc byte[4];
        int size = new Rune(rune).EncodeToUtf8(bytes);
        return Encoding.Latin1.GetString(bytes[..size]);
    }

    public static string FromBytes(ReadOnlySpan<byte> bytes) => Encoding.Latin1.GetString(bytes);
    public static string ToUtf8(string text) => Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(text));
    public static string ToString(string bytes) => Encoding.UTF8.GetString(Bytes(bytes));
    public static byte[] Bytes(string bytes) => Encoding.Latin1.GetBytes(bytes);
    public static int[] Runes(string bytes)
    {
        var runes = new List<int>();
        for (int i = 0; i < bytes.Length;)
        {
            var(rune, size) = Utf8.DecodeRuneInString(bytes.AsSpan(i));
            runes.Add(rune);
            i += size;
        }

        return runes.ToArray();
    }
}

// unicode/utf8.DecodeRuneInString semantics: an invalid sequence consumes one byte;
// an empty sequence returns (RuneError,0); surrogates and overlong encodings fail.
internal static class Utf8
{
    public const int RuneError = 0xFFFD;
    public const int RuneSelf = 0x80;
    public static (int Rune, int Size) DecodeRuneInString(string text) => DecodeRuneInString(text.AsSpan());
    public static (int Rune, int Size) DecodeRuneInString(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
            return (RuneError, 0);
        int first = text[0];
        if (first < 0x80)
            return (first, 1);
        int size, value, secondLow = 0x80, secondHigh = 0xBF;
        if (first is >= 0xC2 and <= 0xDF)
        {
            size = 2;
            value = first & 0x1F;
        }
        else if (first is >= 0xE0 and <= 0xEF)
        {
            size = 3;
            value = first & 0xF;
            if (first == 0xE0)
                secondLow = 0xA0;
            if (first == 0xED)
                secondHigh = 0x9F;
        }
        else if (first is >= 0xF0 and <= 0xF4)
        {
            size = 4;
            value = first & 7;
            if (first == 0xF0)
                secondLow = 0x90;
            if (first == 0xF4)
                secondHigh = 0x8F;
        }
        else
            return (RuneError, 1);
        if (text.Length < size || text[1] < secondLow || text[1] > secondHigh)
            return (RuneError, 1);
        value = (value << 6) | (text[1] & 0x3F);
        for (int i = 2; i < size; i++)
        {
            if (text[i] < 0x80 || text[i] > 0xBF)
                return (RuneError, 1);
            value = (value << 6) | (text[i] & 0x3F);
        }

        return (value, size);
    }

    public static (int Rune, int Size) DecodeLastRuneInString(string text) => DecodeLastRuneInString(text.AsSpan());
    public static (int Rune, int Size) DecodeLastRuneInString(ReadOnlySpan<char> text)
    {
        if (text.Length == 0)
            return (RuneError, 0);
        int start = text.Length - 1;
        int lower = Math.Max(0, text.Length - 4);
        while (start > lower && text[start] is >= (char)0x80 and <= (char)0xBF)
            start--;
        var result = DecodeRuneInString(text[start..]);
        return start + result.Size == text.Length ? result : (RuneError, 1);
    }

    public static (int Rune, int Size) DecodeRuneInString(ReadOnlySpan<byte> text)
    {
        if (text.IsEmpty)
            return (RuneError, 0);
        int first = text[0];
        if (first < 0x80)
            return (first, 1);
        int size, value, secondLow = 0x80, secondHigh = 0xBF;
        if (first is >= 0xC2 and <= 0xDF)
        {
            size = 2;
            value = first & 0x1F;
        }
        else if (first is >= 0xE0 and <= 0xEF)
        {
            size = 3;
            value = first & 0xF;
            if (first == 0xE0)
                secondLow = 0xA0;
            if (first == 0xED)
                secondHigh = 0x9F;
        }
        else if (first is >= 0xF0 and <= 0xF4)
        {
            size = 4;
            value = first & 7;
            if (first == 0xF0)
                secondLow = 0x90;
            if (first == 0xF4)
                secondHigh = 0x8F;
        }
        else
            return (RuneError, 1);
        if (text.Length < size || text[1] < secondLow || text[1] > secondHigh)
            return (RuneError, 1);
        value = (value << 6) | (text[1] & 0x3F);
        for (int i = 2; i < size; i++)
        {
            if (text[i] < 0x80 || text[i] > 0xBF)
                return (RuneError, 1);
            value = (value << 6) | (text[i] & 0x3F);
        }

        return (value, size);
    }

    public static (int Rune, int Size) DecodeLastRuneInString(ReadOnlySpan<byte> text)
    {
        if (text.Length == 0)
            return (RuneError, 0);
        int start = text.Length - 1;
        int lower = Math.Max(0, text.Length - 4);
        while (start > lower && text[start] is >= (byte)0x80 and <= (byte)0xBF)
            start--;
        var result = DecodeRuneInString(text[start..]);
        return start + result.Size == text.Length ? result : (RuneError, 1);
    }

    public static int RuneLen(int rune) => rune < 0 || rune > 0x10FFFF || rune is >= 0xD800 and <= 0xDFFF ? -1 : rune < 0x80 ? 1 : rune < 0x800 ? 2 : rune < 0x10000 ? 3 : 4;
}

internal sealed class GoBuilder
{
    private readonly StringBuilder value = new();
    public void Grow(int capacity) => value.EnsureCapacity(value.Length + capacity);
    public void WriteString(string bytes) => value.Append(bytes);
    // Go strings.Builder.WriteString accepts a shared substring without copying
    // an intermediate string. The builder still owns its constructed output.
    public void WriteString(ReadOnlySpan<char> bytes) => value.Append(bytes);
    public void WriteString(ReadOnlySpan<byte> bytes)
    {
        // Existing builder output is a raw Go string, one char per byte. Only
        // this constructed output crosses that compatibility boundary.
        foreach (byte b in bytes) value.Append((char)b);
    }
    // strings.Builder.WriteRune owns only the encoded output; preserve FromRune's
    // invalid-scalar replacement without materializing an intermediate raw string.
    public void WriteRune(int rune)
    {
        if (rune < 0 || rune > 0x10FFFF || rune is >= 0xD800 and <= 0xDFFF)
            rune = 0xFFFD;
        Span<byte> bytes = stackalloc byte[4];
        int size = new Rune(rune).EncodeToUtf8(bytes);
        WriteString(bytes[..size]);
    }
    public void WriteByte(int b) => value.Append((char)b);
    public string String() => value.ToString();
    public int Len() => value.Length;
}

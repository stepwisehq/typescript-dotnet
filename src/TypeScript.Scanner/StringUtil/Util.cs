// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/stringutil/util.go, scanner-used character and JS string slice.
namespace TypeScript.Scanner;
internal static partial class StringUtil
{
    public static bool IsWhiteSpaceLike(int ch) => IsWhiteSpaceSingleLine(ch) || IsLineBreak(ch);
    public static bool IsWhiteSpaceSingleLine(int ch) => ch is ' ' or '\t' or '\v' or '\f' or 0x0085 or 0x00A0 or 0x1680 or >= 0x2000 and <= 0x200B or 0x202F or 0x205F or 0x3000 or 0xFEFF;
    public static bool IsLineBreak(int ch) => ch is '\n' or '\r' or 0x2028 or 0x2029;
    public static bool IsDigit(int ch) => ch is >= '0' and <= '9';
    public static bool IsOctalDigit(int ch) => ch is >= '0' and <= '7';
    public static bool IsHexDigit(int ch) => IsDigit(ch) || ch is >= 'A' and <= 'F' or >= 'a' and <= 'f';
    public static bool IsASCIILetter(int ch) => ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    public static bool IsSurrogate(int ch) => ch is >= 0xD800 and <= 0xDFFF;
    public static bool IsHighSurrogate(int ch) => ch is >= 0xD800 and < 0xDC00;
    public static bool IsLowSurrogate(int ch) => ch is >= 0xDC00 and <= 0xDFFF;
    public static int SurrogatePairToCodePoint(int high, int low) => IsHighSurrogate(high) && IsLowSurrogate(low) ? ((high - 0xD800) << 10) + low - 0xDC00 + 0x10000 : Utf8.RuneError;
    public static (int High, int Low) CodePointToSurrogatePair(int ch) => ch is >= 0x10000 and <= 0x10FFFF ? (0xD800 + ((ch - 0x10000) >> 10), 0xDC00 + ((ch - 0x10000) & 0x3FF)) : (Utf8.RuneError, Utf8.RuneError);
    public static string EncodeJSStringRune(int ch) => IsSurrogate(ch) ? new string ([(char)0xED, (char)(0x80 | ((ch >> 6) & 0x3F)), (char)(0x80 | (ch & 0x3F))]) : GoString.FromRune(ch);
    public static (int Rune, int Size) DecodeJSStringRune(string s) => DecodeJSStringRune(s.AsSpan());
    public static (int Rune, int Size) DecodeJSStringRune(ReadOnlySpan<char> s)
    {
        if (s.Length >= 3 && s[0] == 0xED && s[1] is >= (char)0xA0 and <= (char)0xBF && s[2] is >= (char)0x80 and <= (char)0xBF)
            return (0xD000 | ((s[1] & 0x3F) << 6) | (s[2] & 0x3F), 3);
        return Utf8.DecodeRuneInString(s);
    }

    public static (int Rune, int Size) DecodeJSStringRune(ReadOnlySpan<byte> s)
    {
        if (s.Length >= 3 && s[0] == 0xED && s[1] is >= 0xA0 and <= 0xBF && s[2] is >= 0x80 and <= 0xBF)
            return (0xD000 | ((s[1] & 0x3F) << 6) | (s[2] & 0x3F), 3);
        return Utf8.DecodeRuneInString(s);
    }

    public static string CombineSurrogatePairs(string s)
    {
        if (!s.Contains((char)0xED))
            return s;
        var b = new GoBuilder();
        b.Grow(s.Length);
        for (int i = 0; i < s.Length;)
        {
            var(r, size) = DecodeJSStringRune(s.AsSpan(i));
            if (IsHighSurrogate(r))
            {
                var(low, lowSize) = DecodeJSStringRune(s.AsSpan(i + size));
                if (IsLowSurrogate(low))
                {
                    b.WriteRune(SurrogatePairToCodePoint(r, low));
                    i += size + lowSize;
                    continue;
                }
            }

            b.WriteString(s[i..(i + size)]);
            i += size;
        }

        return b.String();
    }
}

internal static partial class StringUtil
{
    // stringutil/util.go:240. Original strips quote delimiters and removes a
    // backslash before any non-newline rune; it is not a JS escape decoder.
    internal static string UnquoteString(string text)
    {
        if (text.Length >= 2 && ((text[0] == '\"' && text[^1] == '\"') || (text[0] == '\'' && text[^1] == '\'') || (text[0] == '`' && text[^1] == '`')))
            text = text[1..^1];
        var result = new GoBuilder();
        for (var index = 0; index < text.Length;)
        {
            if (text[index] == '\\' && index + 1 < text.Length && text[index + 1] != '\n')
                index++;
            var(_, size) = Utf8.DecodeRuneInString(text.AsSpan(index));
            result.WriteString(text[index..(index + size)]);
            index += size;
        }

        return result.String();
    }
}

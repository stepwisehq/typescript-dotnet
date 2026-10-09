// Copyright 2023 The Go Authors. All rights reserved.
// Licensed under the BSD-style license in licenses/Go-LICENSE.txt.
// Selected whole-buffer counterparts of Go1.27.1 encoding/json/internal/jsonwire
// decode.go and wire.go. Resumable I/O and reformat/canonicalization are external.
using System.Globalization;
using TypeScript.Scanner;

namespace TypeScript.Json.Internal;
internal readonly record struct WireResult(int Consumed, string? Error = null, bool UnexpectedEof = false);
internal static class JsonWire
{
    internal static int ConsumeWhitespace(ReadOnlySpan<char> b)
    {
        int n = 0;
        while (b.Length > n && b[n] is ' ' or '\t' or '\r' or '\n')
            n++;
        return n;
    }

    internal static WireResult ConsumeLiteral(ReadOnlySpan<char> b, string literal)
    {
        for (int i = 0; i < b.Length && i < literal.Length; i++)
            if (b[i] != literal[i])
                return new(i, NewInvalidCharacterError(b[i..], "in literal " + literal + " (expecting " + QuoteRune(literal.AsSpan(i)) + ")"));
        return b.Length < literal.Length ? new(b.Length, UnexpectedEof: true) : new(literal.Length);
    }

    internal static WireResult ConsumeString(ReadOnlySpan<char> b, bool validateUtf8 = true)
    {
        int n = 0;
        if (b.IsEmpty)
            return new(0, UnexpectedEof: true);
        if (b[0] != '"')
            return new(0, NewInvalidCharacterError(b, "at start of string (expecting '\"')"));
        n++;
        while (b.Length > n)
        {
            while (b.Length > n && b[n] < 0x80 && b[n] >= ' ' && b[n] is not ('\\' or '"'))
                n++;
            if (b.Length <= n)
                return new(n, UnexpectedEof: true);
            if (b[n] == '"')
                return new(n + 1);
            var(rune, size) = Utf8.DecodeRuneInString(b[n..]);
            if (size > 1)
            {
                n += size;
                continue;
            }

            if (rune == '\\')
            {
                int resumeOffset = n;
                if (b.Length < n + 2)
                    return new(resumeOffset, UnexpectedEof: true);
                switch (b[n + 1])
                {
                    case '/':
                    case '"':
                    case '\\':
                    case 'b':
                    case 'f':
                    case 'n':
                    case 'r':
                    case 't':
                        n += 2;
                        break;
                    case 'u':
                        if (b.Length < n + 6)
                            return HasEscapedUTF16Prefix(b[n..], false) ? new(resumeOffset, UnexpectedEof: true) : new(n, NewInvalidEscapeSequenceError(b[n..]));
                        var(v1, ok) = parseHexUint16(b.Slice(n + 2, 4));
                        if (!ok)
                            return new(n, NewInvalidEscapeSequenceError(b.Slice(n, 6)));
                        n += 6;
                        if (validateUtf8 && v1 is >= 0xD800 and <= 0xDFFF)
                        {
                            if (b.Length < n + 6)
                                return HasEscapedUTF16Prefix(b[n..], true) ? new(resumeOffset, UnexpectedEof: true) : new(n - 6, NewInvalidEscapeSequenceError(b[(n - 6)..]));
                            var(v2, ok2) = parseHexUint16(b.Slice(n + 2, 4));
                            if (b[n] != '\\' || b[n + 1] != 'u' || !ok2 || v1 is not (>= 0xD800 and <= 0xDBFF) || v2 is not (>= 0xDC00 and <= 0xDFFF))
                                return new(n - 6, NewInvalidEscapeSequenceError(b.Slice(n - 6, 12)));
                            n += 6;
                        }

                        break;
                    default:
                        return new(n, NewInvalidEscapeSequenceError(b.Slice(n, 2)));
                }

                continue;
            }

            if (rune == Utf8.RuneError && !validateUtf8)
            {
                n++;
                continue;
            }

            if (rune == Utf8.RuneError)
                return FullRune(b[n..]) ? new(n, "invalid UTF-8") : new(n, UnexpectedEof: true);
            if (rune < ' ')
                return new(n, NewInvalidCharacterError(b[n..], "in string (expecting non-control character)"));
            throw new InvalidOperationException("BUG: unhandled character " + QuoteRune(b[n..]));
        }

        return new(n, UnexpectedEof: true);
    }

    internal static string Unquote(string quoted)
    {
        var output = new GoBuilder();
        for (int i = 1; i < quoted.Length - 1;)
        {
            if (quoted[i] != '\\')
            {
                var(rune, size) = Utf8.DecodeRuneInString(quoted.AsSpan(i));
                output.WriteRune(rune);
                i += size;
                continue;
            }

            char escape = quoted[i + 1];
            i += 2;
            if (escape == 'u')
            {
                int rune = parseHexUint16(quoted.AsSpan(i, 4)).Value;
                i += 4;
                if (rune is >= 0xD800 and <= 0xDFFF)
                {
                    if (rune <= 0xDBFF && i + 6 <= quoted.Length - 1 && quoted[i] == '\\' && quoted[i + 1] == 'u' && parseHexUint16(quoted.AsSpan(i + 2, 4)).Value is >= 0xDC00 and <= 0xDFFF)
                    {
                        int low = parseHexUint16(quoted.AsSpan(i + 2, 4)).Value;
                        rune = 0x10000 + ((rune - 0xD800) << 10) + low - 0xDC00;
                        i += 6;
                    }
                    else
                        rune = Utf8.RuneError;
                }

                output.WriteRune(rune);
            }
            else
                output.WriteRune(escape switch
                {
                    'b' => '\b',
                    'f' => '\f',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => escape
                });
        }

        return output.String();
    }

    // Whole-buffer initial state of ConsumeNumberResumable; offsets deliberately
    // identify the same resume point when a fraction/exponent is truncated.
    internal static WireResult ConsumeNumber(ReadOnlySpan<char> b)
    {
        int n = 0;
        if (b.Length > 0 && b[0] == '-')
            n++;
        if (b.Length <= n)
            return new(0, UnexpectedEof: true);
        if (b[n] == '0')
            n++;
        else if (b[n] is >= '1' and <= '9')
        {
            do
            {
                n++;
            }
            while (b.Length > n && b[n] is >= '0' and <= '9');
        }
        else
            return new(n, NewInvalidCharacterError(b[n..], "in number (expecting digit)"));
        if (b.Length > n && b[n] == '.')
        {
            int resume = n;
            n++;
            if (b.Length <= n)
                return new(resume, UnexpectedEof: true);
            if (b[n] is not (>= '0' and <= '9'))
                return new(n, NewInvalidCharacterError(b[n..], "in number (expecting digit)"));
            do
            {
                n++;
            }
            while (b.Length > n && b[n] is >= '0' and <= '9');
        }

        if (b.Length > n && b[n] is 'e' or 'E')
        {
            int resume = n;
            n++;
            if (b.Length > n && b[n] is '-' or '+')
                n++;
            if (b.Length <= n)
                return new(resume, UnexpectedEof: true);
            if (b[n] is not (>= '0' and <= '9'))
                return new(n, NewInvalidCharacterError(b[n..], "in number (expecting digit)"));
            do
            {
                n++;
            }
            while (b.Length > n && b[n] is >= '0' and <= '9');
        }

        return new(n);
    }

    internal static (int Value, bool Ok) parseHexUint16(ReadOnlySpan<char> b)
    {
        if (b.Length != 4)
            return (0, false);
        int value = 0;
        foreach (char c in b)
        {
            int digit = c is >= '0' and <= '9' ? c - '0' : c is >= 'a' and <= 'f' ? c - 'a' + 10 : c is >= 'A' and <= 'F' ? c - 'A' + 10 : -1;
            if (digit < 0)
                return (0, false);
            value = value * 16 + digit;
        }

        return (value, true);
    }

    private static bool HasEscapedUTF16Prefix(ReadOnlySpan<char> b, bool surrogate)
    {
        // decode.go hasEscapedUTF16Prefix accepts only a valid (possibly empty)
        // prefix of an escape, and the low-surrogate range when requested.
        if (b.Length == 0)
            return true;
        if (b[0] != '\\')
            return false;
        if (b.Length == 1)
            return true;
        if (b[1] != 'u')
            return false;
        for (int i = 2; i < b.Length; i++)
        {
            char c = b[i];
            if (i >= 6 || !(c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
                return false;
            if (surrogate && i == 2 && c is not ('d' or 'D'))
                return false;
            if (surrogate && i == 3 && c is not (>= 'c' and <= 'f' or >= 'C' and <= 'F'))
                return false;
        }

        return true;
    }

    private static bool FullRune(ReadOnlySpan<char> b)
    {
        if (b.IsEmpty)
            return false;
        int first = b[0], size = first < 0x80 ? 1 : first is >= 0xC2 and <= 0xDF ? 2 : first is >= 0xE0 and <= 0xEF ? 3 : first is >= 0xF0 and <= 0xF4 ? 4 : 1;
        if (b.Length >= size)
            return true;
        int low = first == 0xE0 ? 0xA0 : first == 0xF0 ? 0x90 : 0x80;
        int high = first == 0xED ? 0x9F : first == 0xF4 ? 0x8F : 0xBF;
        if (b.Length > 1 && (b[1] < low || b[1] > high))
            return true;
        for (int i = 2; i < b.Length; i++)
            if (b[i] is < (char)0x80 or > (char)0xBF)
                return true;
        return false;
    }

    internal static string QuoteRune(ReadOnlySpan<char> b)
    {
        var(rune, size) = Utf8.DecodeRuneInString(b);
        if (rune == Utf8.RuneError && size == 1)
            return "'\\x" + ((int)b[0]).ToString("x", CultureInfo.InvariantCulture) + "'";
        string quoted = GoQuote.Quote(GoString.FromRune(rune));
        return "'" + quoted[1..^1].Replace("\\\"", "\"", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal) + "'";
    }

    internal static string NewInvalidCharacterError(ReadOnlySpan<char> b, string where)
    {
        var(_, size) = Utf8.DecodeRuneInString(b);
        return InvalidTextError("character", b[..size].ToString(), where);
    }

    internal static string NewInvalidEscapeSequenceError(ReadOnlySpan<char> what) => InvalidTextError(what.Length > 6 ? "surrogate pair" : "escape sequence", what.ToString(), "in string");
    private static string InvalidTextError(string label, string what, string where)
    {
        var runes = GoString.Runes(what).ToList();
        bool escape = runes.Any(rune => rune is '`' or Utf8.RuneError || GoUnicode.IsSpace(rune) || !GoQuote.IsPrint(rune));
        string quoted = runes.Count == 1 ? QuoteRune(what.AsSpan()) : escape ? GoQuote.Quote(what) : "`" + what + "`";
        return ("invalid " + label + " " + quoted + " " + where).TrimEnd(' ');
    }

    internal static string TruncatePointer(string s, int n)
    {
        if (s.Length <= n)
            return s;
        int i = n / 2, j = s.Length - n / 2;
        int k = s[..i].LastIndexOf('/');
        if (k > 0)
            i = k;
        k = s[j..].IndexOf('/');
        if (k >= 0)
            j += k + 1;
        while (i > 0 && Utf8.DecodeLastRuneInString(s.AsSpan(0, i))is (Utf8.RuneError, 1))
            i--;
        while (j < s.Length && Utf8.DecodeRuneInString(s.AsSpan(j))is (Utf8.RuneError, 1))
            j++;
        int slashCount = s[i..j].Count(c => c == '/');
        string middle = slashCount == 0 ? GoString.ToUtf8("…") : slashCount == 1 ? GoString.ToUtf8("…/…") : GoString.ToUtf8("…/…/…");
        if (s[i] == '/' && slashCount != 0)
            middle = middle[3..];
        if (s[j - 1] == '/' && slashCount != 0)
            middle = middle[..^3];
        return s[..i] + middle + s[j..];
    }
}

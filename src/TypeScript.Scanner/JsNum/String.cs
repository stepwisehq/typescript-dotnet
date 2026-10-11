// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/jsnum/string.go, scanner-only Number.String and FromString slice.
using System.Globalization;
using System.Numerics;
using System.Text;

namespace TypeScript.Scanner;
internal static partial class JsNum
{
    public readonly partial record struct Number(double Value)
    {
        public string String()
        {
            if (double.IsNaN(Value))
                return "NaN";
            if (double.IsInfinity(Value))
                return Value < 0 ? "-Infinity" : "Infinity";
            if (Value == 0)
                return "0";
            if (Value is >= -9007199254740991 and <= 9007199254740991 && Math.Truncate(Value) == Value)
                return ((long)Value).ToString(CultureInfo.InvariantCulture);
            // .NET supplies a shortest round-trip decimal; adjust the presentation to
            // the ECMAScript/Go JSON exponent and fixed notation boundaries.
            double absolute = Math.Abs(Value);
            string text = absolute.ToString("R", CultureInfo.InvariantCulture);
            // The runtime's shortest formatter can choose a decimal outside the
            // asymmetric rounding interval at a binary exponent boundary. Check
            // its round-trip contract before using it; G17 retains all binary64
            // digits when that contract fails. This is a library adaptation, not
            // a special case for a particular literal (see the original Ryu rows).
            if (double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture) != absolute)
                text = absolute.ToString("G17", CultureInfo.InvariantCulture);
            int e = text.IndexOf('E');
            int exponent = e < 0 ? 0 : int.Parse(text[(e + 1)..], CultureInfo.InvariantCulture);
            string mantissa = e < 0 ? text : text[..e];
            int point = mantissa.IndexOf('.');
            int decimalPosition = (point < 0 ? mantissa.Length : point) + exponent;
            string digits = mantissa.Replace(".", "");
            if (e < 0)
            {
                int leading = 0;
                while (leading < digits.Length - 1 && digits[leading] == '0')
                    leading++;
                digits = digits[leading..];
                decimalPosition -= leading;
            }

            digits = digits.TrimEnd('0');
            string value;
            if (Math.Abs(Value) >= 1e-6 && Math.Abs(Value) < 1e21)
                value = decimalPosition <= 0 ? "0." + new string ('0', -decimalPosition) + digits : decimalPosition >= digits.Length ? digits + new string ('0', decimalPosition - digits.Length) : digits[..decimalPosition] + "." + digits[decimalPosition..];
            else
            {
                int scientificExponent = decimalPosition - 1;
                value = digits[..1] + (digits.Length > 1 ? "." + digits[1..] : "") + "e" + (scientificExponent >= 0 ? "+" : "") + scientificExponent.ToString(CultureInfo.InvariantCulture);
            }

            return Value < 0 ? "-" + value : value;
        }
    }

    public static Number FromString(string s) => FromString(s.AsSpan(0, s.Length));

    internal static Number FromString(ByteStringValue s) => s.IsNull ? FromString((string)null!) :
        s.IsBytes ? fromBytes(s.ByteSpan) : FromString(s.CharSpan);

    private static Number fromBytes(ReadOnlySpan<byte> bytes)
    {
        char[]? rented = null;
        Span<char> chars = bytes.Length <= 256 ? stackalloc char[bytes.Length]
            : (rented = System.Buffers.ArrayPool<char>.Shared.Rent(bytes.Length)).AsSpan(0, bytes.Length);
        try
        {
            // Raw Go bytes correspond to Latin1 byte-characters in the existing
            // numeric library adapter. The shared decoder below still validates
            // UTF8 and consumes malformed bytes exactly as before.
            Encoding.Latin1.GetChars(bytes, chars);
            return FromString(chars);
        }
        finally
        {
            if (rented is not null) System.Buffers.ArrayPool<char>.Shared.Return(rented);
        }
    }

    private static Number FromString(ReadOnlySpan<char> s)
    {
        int start = 0, end = s.Length;
        while (start < end)
        {
            var(r, size) = Utf8.DecodeRuneInString(s.Slice(start, end - start));
            if (!isStrWhiteSpace(r))
                break;
            start += size;
        }

        while (start < end)
        {
            var(r, size) = Utf8.DecodeLastRuneInString(s.Slice(start, end - start));
            if (!isStrWhiteSpace(r))
                break;
            end -= size;
        }

        ReadOnlySpan<char> text = s.Slice(start, end - start);
        if (text.IsEmpty) return new(0);
        if (text.SequenceEqual("Infinity") || text.SequenceEqual("+Infinity")) return new(double.PositiveInfinity);
        if (text.SequenceEqual("-Infinity")) return new(double.NegativeInfinity);

        // jsnum/string.go:66. Range validates borrowed raw UTF-8, consuming
        // malformed bytes exactly as the standard Go decoder.
        for (int offset = 0; offset < text.Length;)
        {
            var (r, size) = Utf8.DecodeRuneInString(text[offset..]);
            if (!isNumberRune(r)) return new(double.NaN);
            offset += size;
        }
        var (n, ok) = tryParseInt(text);
        if (ok) return n;
        bool negative = text[0] == '-';
        if (negative || text[0] == '+') text = text[1..];
        var (first, _) = Utf8.DecodeRuneInString(text);
        if (!StringUtil.IsDigit(first) && first != '.') return new(double.NaN);
        double f = parseFloatString(text);
        if (double.IsNaN(f)) return new(double.NaN);
        return new(Math.CopySign(f, negative ? -1 : 1));
    }

    private static bool isStrWhiteSpace(int r) => r is '\n' or '\r' or 0x2028 or 0x2029 or '\t' or '\v' or '\f' or 0xFEFF or 0x20 or 0xA0 or 0x1680 or >= 0x2000 and <= 0x200A or 0x202F or 0x205F or 0x3000;
    // jsnum/string.go:117. Validate the selected base before attempting the
    // signed 64-bit source fast path; only overflow needs the big.Int adapter.
    private static (Number Number, bool Ok) tryParseInt(ReadOnlySpan<char> s)
    {
        int radix = 10;
        bool prefixed = false;
        ReadOnlySpan<char> digits = s;
        if (s.Length > 2 && s[0] == '0')
        {
            switch (s[1])
            {
                case 'b': case 'B': radix = 2; prefixed = true; break;
                case 'o': case 'O': radix = 8; prefixed = true; break;
                case 'x': case 'X': radix = 16; prefixed = true; break;
            }
            if (prefixed) digits = s[2..];
        }
        if (!prefixed)
        {
            digits = trimLeadingZeros(s);
            if (!isAllDigits(digits)) return (new(0), false);
        }

        foreach (char ch in digits)
        {
            int digit = integerDigit(ch);
            if (digit < 0 || digit >= radix) return (new(double.NaN), true);
        }

        // Decimal uses the signed source bound directly. Hex/binary library
        // parsing is unsigned so bit63 is never reinterpreted as a negative;
        // values above signed64 take the original big.Int overflow branch.
        if (radix == 10)
        {
            if (long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out long narrow))
                return (new((double)narrow), true);
        }
        else if (radix is 2 or 16)
        {
            var style = radix == 2 ? NumberStyles.AllowBinarySpecifier : NumberStyles.AllowHexSpecifier;
            if (ulong.TryParse(digits, style, CultureInfo.InvariantCulture, out ulong narrow) && narrow <= long.MaxValue)
                return (new((double)(long)narrow), true);
        }
        else
        {
            // .NET has no octal span parser; this adapter preserves ParseInt's
            // positive signed64 bound without creating another input spelling.
            long narrow = 0;
            bool overflow = false;
            foreach (char ch in digits)
            {
                int digit = ch - '0';
                if (narrow > (long.MaxValue - digit) / 8) { overflow = true; break; }
                narrow = narrow * 8 + digit;
            }
            if (!overflow) return (new((double)narrow), true);
        }

        BigInteger value = 0;
        foreach (char ch in digits) value = value * radix + integerDigit(ch);
        // Keep the existing big.Int.Float64 round-to-nearest-even adapter.
        return (new(ToDouble(value)), true);
    }

    private static int integerDigit(char ch) => ch is >= '0' and <= '9' ? ch - '0' :
        ch is >= 'a' and <= 'f' ? ch - 'a' + 10 : ch is >= 'A' and <= 'F' ? ch - 'A' + 10 : -1;

    private static double ToDouble(BigInteger value)
    {
        if (value.IsZero)
            return 0;
        long bits = value.GetBitLength();
        if (bits <= 53)
            return (double)value;
        if (bits > 1024)
            return double.PositiveInfinity;
        int shift = (int)bits - 53;
        BigInteger significant = value >> shift;
        BigInteger discarded = value - (significant << shift), half = BigInteger.One << (shift - 1);
        if (discarded > half || (discarded == half && !significant.IsEven))
            significant++;
        return Math.ScaleB((double)significant, shift);
    }

    private static double parseFloatString(ReadOnlySpan<char> s)
    {
        int dot = s.IndexOf('.');
        bool hasDot = dot >= 0;
        ReadOnlySpan<char> a = hasDot ? s[..dot] : s;
        ReadOnlySpan<char> rest = hasDot ? s[(dot + 1)..] : s;
        int exponent = rest.IndexOfAny('e', 'E');
        bool hasExp = exponent >= 0;
        ReadOnlySpan<char> b = default;
        ReadOnlySpan<char> c = hasExp ? rest[(exponent + 1)..] : default;
        if (hasDot) b = hasExp ? rest[..exponent] : rest;
        else if (hasExp) a = rest[..exponent];

        // Source's normalized floating spelling is the final owned parse input;
        // trimmed and split substrings above remain borrowed headers.
        var sb = new StringBuilder(a.Length + b.Length + c.Length + 3);
        if (a.IsEmpty)
        {
            if (hasDot && b.IsEmpty || hasExp && c.IsEmpty) return double.NaN;
            sb.Append('0');
        }
        else
        {
            a = trimLeadingZeros(a);
            if (!isAllDigits(a)) return double.NaN;
            sb.Append(a);
        }
        if (hasDot)
        {
            sb.Append('.');
            if (b.IsEmpty) sb.Append('0');
            else
            {
                b = trimTrailingZeros(b);
                if (!isAllDigits(b)) return double.NaN;
                sb.Append(b);
            }
        }
        if (hasExp)
        {
            sb.Append('e');
            if (!c.IsEmpty && c[0] == '-') { sb.Append('-'); c = c[1..]; }
            else if (!c.IsEmpty && c[0] == '+') c = c[1..];
            c = trimLeadingZeros(c);
            if (!isAllDigits(c)) return double.NaN;
            sb.Append(c);
        }
        return double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double f) ? f : double.NaN;
    }

    private static ReadOnlySpan<char> trimLeadingZeros(ReadOnlySpan<char> s)
    {
        if (!s.IsEmpty && s[0] == '0')
        {
            int start = 0;
            while (start < s.Length && s[start] == '0') start++;
            return start == s.Length ? "0" : s[start..];
        }
        return s;
    }

    private static ReadOnlySpan<char> trimTrailingZeros(ReadOnlySpan<char> s)
    {
        if (!s.IsEmpty && s[^1] == '0')
        {
            int end = s.Length;
            while (end > 0 && s[end - 1] == '0') end--;
            return end == 0 ? "0" : s[..end];
        }
        return s;
    }

    private static bool isAllDigits(ReadOnlySpan<char> s)
    {
        foreach (char r in s) if (!StringUtil.IsDigit(r)) return false;
        return true;
    }

    private static bool isNumberRune(int r) => StringUtil.IsDigit(r) || r is >= 'a' and <= 'f' or >= 'A' and <= 'F' or '.' or '-' or '+' or 'x' or 'X' or 'o' or 'O';
}

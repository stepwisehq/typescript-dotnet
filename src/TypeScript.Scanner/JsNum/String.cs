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
    public readonly record struct Number(double Value)
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
            string text = Math.Abs(Value).ToString("R", CultureInfo.InvariantCulture);
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

    public static Number FromString(string s)
    {
        int start = 0, end = s.Length;
        while (start < end)
        {
            var(r, size) = Utf8.DecodeRuneInString(s.AsSpan(start, end - start));
            if (!isStrWhiteSpace(r))
                break;
            start += size;
        }

        while (start < end)
        {
            var(r, size) = Utf8.DecodeLastRuneInString(s.AsSpan(start, end - start));
            if (!isStrWhiteSpace(r))
                break;
            end -= size;
        }

        s = s[start..end];
        switch (s)
        {
            case "":
                return new(0);
            case "Infinity":
            case "+Infinity":
                return new(double.PositiveInfinity);
            case "-Infinity":
                return new(double.NegativeInfinity);
        }

        foreach (int r in GoString.Runes(s))
            if (!isNumberRune(r))
                return new(double.NaN);
        var(n, ok) = tryParseInt(s);
        if (ok)
            return n;
        bool negative = s.StartsWith('-');
        if (negative || s.StartsWith('+'))
            s = s[1..];
        var(first, _) = Utf8.DecodeRuneInString(s);
        if (!StringUtil.IsDigit(first) && first != '.')
            return new(double.NaN);
        double f = parseFloatString(s);
        if (double.IsNaN(f))
            return new(double.NaN);
        return new(Math.CopySign(f, negative ? -1 : 1));
    }

    private static bool isStrWhiteSpace(int r) => r is '\n' or '\r' or 0x2028 or 0x2029 or '\t' or '\v' or '\f' or 0xFEFF or 0x20 or 0xA0 or 0x1680 or >= 0x2000 and <= 0x200A or 0x202F or 0x205F or 0x3000;
    private static (Number Number, bool Ok) tryParseInt(string s)
    {
        int radix = 10;
        bool hasIntResult = false;
        string digits = s;
        if (s.Length > 2)
        {
            switch (s[..2])
            {
                case "0b":
                case "0B":
                    radix = 2;
                    hasIntResult = true;
                    break;
                case "0o":
                case "0O":
                    radix = 8;
                    hasIntResult = true;
                    break;
                case "0x":
                case "0X":
                    radix = 16;
                    hasIntResult = true;
                    break;
            }

            if (hasIntResult)
                digits = s[2..];
        }

        if (!hasIntResult)
        {
            s = trimLeadingZeros(s);
            digits = s;
            if (!isAllDigits(s))
                return (new(0), false);
        }

        BigInteger value = 0;
        foreach (char ch in digits)
        {
            int digit = ch is >= '0' and <= '9' ? ch - '0' : ch is >= 'a' and <= 'f' ? ch - 'a' + 10 : ch is >= 'A' and <= 'F' ? ch - 'A' + 10 : -1;
            if (digit < 0 || digit >= radix)
                return (new(double.NaN), true);
            value = value * radix + digit;
        }

        // Go's big.Int.Float64 uses round-to-nearest-even. BigInteger's explicit
        // double conversion truncates, so perform the rounding before conversion.
        return (new(ToDouble(value)), true);
    }

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

    private static double parseFloatString(string s)
    {
        string a, b = "", c = "";
        bool hasDot, hasExp;
        string rest;
        (a, rest, hasDot) = cut(s, '.');
        if (hasDot)
            (b, c, hasExp) = cutAny(rest, "eE");
        else
            (a, c, hasExp) = cutAny(s, "eE");
        var sb = new StringBuilder(s.Length + 3);
        if (a == "")
        {
            if (hasDot && b == "" || hasExp && c == "")
                return double.NaN;
            sb.Append('0');
        }
        else
        {
            a = trimLeadingZeros(a);
            if (!isAllDigits(a))
                return double.NaN;
            sb.Append(a);
        }

        if (hasDot)
        {
            sb.Append('.');
            if (b == "")
                sb.Append('0');
            else
            {
                b = trimTrailingZeros(b);
                if (!isAllDigits(b))
                    return double.NaN;
                sb.Append(b);
            }
        }

        if (hasExp)
        {
            sb.Append('e');
            if (c.StartsWith('-'))
            {
                sb.Append('-');
                c = c[1..];
            }
            else if (c.StartsWith('+'))
                c = c[1..];
            c = trimLeadingZeros(c);
            if (!isAllDigits(c))
                return double.NaN;
            sb.Append(c);
        }

        return double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double f) ? f : double.NaN;
    }

    private static (string Before, string After, bool Found) cut(string s, char separator)
    {
        int i = s.IndexOf(separator);
        return i >= 0 ? (s[..i], s[(i + 1)..], true) : (s, "", false);
    }

    private static (string Before, string After, bool Found) cutAny(string s, string separators)
    {
        int i = s.IndexOfAny(separators.ToCharArray());
        return i >= 0 ? (s[..i], s[(i + 1)..], true) : (s, "", false);
    }

    private static string trimLeadingZeros(string s)
    {
        if (s.StartsWith('0'))
        {
            s = s.TrimStart('0');
            if (s == "")
                return "0";
        }

        return s;
    }

    private static string trimTrailingZeros(string s)
    {
        if (s.EndsWith('0'))
        {
            s = s.TrimEnd('0');
            if (s == "")
                return "0";
        }

        return s;
    }

    private static bool isAllDigits(string s)
    {
        foreach (char r in s)
            if (!StringUtil.IsDigit(r))
                return false;
        return true;
    }

    private static bool isNumberRune(int r) => StringUtil.IsDigit(r) || r is >= 'a' and <= 'f' or >= 'A' and <= 'F' or '.' or '-' or '+' or 'x' or 'X' or 'o' or 'O';
}

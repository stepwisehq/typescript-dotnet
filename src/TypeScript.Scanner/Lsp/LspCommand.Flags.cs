// Copyright 2009 The Go Authors. All rights reserved.
// Modified C# port of Go1.27.1 internal/strconv/atoi.go ParseInt/ParseUint/underscoreOK,
// restricted to flag.Int's actual base0 and Linux64 bit-size admission.
// Licensed under the BSD license in licenses/Go-LICENSE.txt.
namespace TypeScript.Lsp;
public static partial class LspCommand
{
    internal static bool TryParseProcessID(ReadOnlySpan<char> text, out long result)
    {
        result = 0;
        if (text.IsEmpty)
            return false;
        bool negative = false;
        if (text[0] is '+' or '-')
        {
            negative = text[0] == '-';
            text = text[1..];
        }

        if (text.IsEmpty)
            return false;
        var original = text;
        int numberBase = 10;
        if (text[0] == '0')
        {
            if (text.Length >= 3 && Lower(text[1])is 'b' or 'o' or 'x')
            {
                numberBase = Lower(text[1]) switch
                {
                    'b' => 2,
                    'o' => 8,
                    _ => 16
                };
                text = text[2..];
            }
            else
            {
                numberBase = 8;
                text = text[1..];
            }
        }

        ulong cutoff = ulong.MaxValue / (uint)numberBase + 1;
        ulong value = 0;
        bool underscores = false;
        foreach (char c in text)
        {
            if (c == '_')
            {
                underscores = true;
                continue;
            }

            int digit;
            if (c is >= '0' and <= '9')
                digit = c - '0';
            else if (Lower(c)is >= 'a' and <= 'z')
                digit = Lower(c) - 'a' + 10;
            else
                return false;
            if (digit >= numberBase || value >= cutoff)
                return false;
            value *= (uint)numberBase;
            ulong next = unchecked(value + (uint)digit);
            if (next < value)
                return false;
            value = next;
        }

        if (underscores && !UnderscoreOK(original))
            return false;
        const ulong signedCutoff = 1UL << 63;
        if (!negative && value >= signedCutoff || negative && value > signedCutoff)
            return false;
        result = negative ? unchecked(-(long)value) : (long)value;
        return true;
    }

    private static char Lower(char c) => c is >= 'A' and <= 'Z' ? (char)(c + ('a' - 'A')) : c;
    private static bool UnderscoreOK(ReadOnlySpan<char> text)
    {
        char saw = '^';
        int i = 0;
        if (!text.IsEmpty && text[0] is '+' or '-')
            text = text[1..];
        bool hex = false;
        if (text.Length >= 2 && text[0] == '0' && Lower(text[1])is 'b' or 'o' or 'x')
        {
            i = 2;
            saw = '0';
            hex = Lower(text[1]) == 'x';
        }

        for (; i < text.Length; i++)
        {
            char c = text[i];
            if (c is >= '0' and <= '9' || hex && Lower(c)is >= 'a' and <= 'f')
            {
                saw = '0';
                continue;
            }

            if (c == '_')
            {
                if (saw != '0')
                    return false;
                saw = '_';
                continue;
            }

            if (saw == '_')
                return false;
            saw = '!';
        }

        return saw != '_';
    }
}

// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by the BSD-style license in licenses/Go-LICENSE.txt.
// Modified narrow port of the pinned Go toolchain strconv.Quote/IsPrint with
// the separately generated print tables. Inputs and outputs are original Go byte strings.
namespace TypeScript.Scanner;
internal static partial class GoQuote
{
    internal static string Quote(string bytes)
    {
        var writer = new GoBuilder();
        writer.WriteString("\"");
        for (int offset = 0; offset < bytes.Length;)
        {
            var(rune, size) = Utf8.DecodeRuneInString(bytes.AsSpan(offset));
            if (size == 1 && rune == 0xFFFD)
            {
                writer.WriteString("\\x" + ((byte)bytes[offset]).ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                offset++;
                continue;
            }

            offset += size;
            if (rune is '"' or '\\')
            {
                writer.WriteString("\\");
                writer.WriteRune(rune);
            }
            else if (IsPrint(rune))
                writer.WriteRune(rune);
            else
            {
                string? escape = rune switch
                {
                    7 => "\\a",
                    8 => "\\b",
                    12 => "\\f",
                    10 => "\\n",
                    13 => "\\r",
                    9 => "\\t",
                    11 => "\\v",
                    _ => null
                };
                if (escape != null)
                    writer.WriteString(escape);
                else if (rune < 32 || rune == 127)
                    writer.WriteString("\\x" + rune.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                else if (rune < 0x10000)
                    writer.WriteString("\\u" + rune.ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                else
                    writer.WriteString("\\U" + rune.ToString("x8", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        writer.WriteString("\"");
        return writer.String();
    }

    private static int Search(int[] values, int rune)
    {
        int low = 0, high = values.Length;
        while (low < high)
        {
            int mid = low + (high - low) / 2;
            if (values[mid] < rune)
                low = mid + 1;
            else
                high = mid;
        }

        return low;
    }

    internal static bool IsPrint(int rune)
    {
        if (rune <= 0xFF)
            return rune >= 0x20 && rune <= 0x7E || rune >= 0xA1 && rune <= 0xFF && rune != 0xAD;
        var ranges = rune < 0x10000 ? isPrint16 : isPrint32;
        var exceptions = rune < 0x10000 ? isNotPrint16 : isNotPrint32;
        int index = Search(ranges, rune);
        if (index >= ranges.Length || rune < ranges[index & ~1] || ranges[index | 1] < rune)
            return false;
        if (rune >= 0x20000)
            return true;
        if (rune >= 0x10000)
            rune -= 0x10000;
        index = Search(exceptions, rune);
        return index == exceptions.Length || exceptions[index] != rune;
    }
}

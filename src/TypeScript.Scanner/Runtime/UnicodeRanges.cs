// Copyright 2009 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go 1.27.1 src/unicode/letter.go range lookup workers required by LSUtil and normalization.
namespace TypeScript.Scanner;
internal static partial class GoUnicode
{
    private const int linearMax = 18;
    private static bool is16(ReadOnlySpan<Range> ranges, int rune)
    {
        if (ranges.Length <= linearMax || rune <= 255)
        {
            foreach (var range in ranges)
            {
                if (rune < range.Lo) return false;
                if (rune <= range.Hi) return range.Stride == 1 || (rune - range.Lo) % range.Stride == 0;
            }
            return false;
        }
        int lo = 0, hi = ranges.Length;
        while (lo < hi)
        {
            int mid = (int)((uint)(lo + hi) >> 1);
            var range = ranges[mid];
            if (range.Lo <= rune && rune <= range.Hi) return range.Stride == 1 || (rune - range.Lo) % range.Stride == 0;
            if (rune < range.Lo) hi = mid;
            else lo = mid + 1;
        }
        return false;
    }
    private static bool is32(ReadOnlySpan<Range> ranges, int rune)
    {
        if (ranges.Length <= linearMax)
        {
            foreach (var range in ranges)
            {
                if (rune < range.Lo) return false;
                if (rune <= range.Hi) return range.Stride == 1 || (rune - range.Lo) % range.Stride == 0;
            }
            return false;
        }
        int lo = 0, hi = ranges.Length;
        while (lo < hi)
        {
            int mid = (int)((uint)(lo + hi) >> 1);
            var range = ranges[mid];
            if (range.Lo <= rune && rune <= range.Hi) return range.Stride == 1 || (rune - range.Lo) % range.Stride == 0;
            if (rune < range.Lo) hi = mid;
            else lo = mid + 1;
        }
        return false;
    }
    internal static bool Is(RangeTable table, int rune)
    {
        var r16 = table.R16;
        if (r16.Length > 0 && (uint)rune <= (uint)r16[^1].Hi) return is16(r16, rune);
        var r32 = table.R32;
        if (r32.Length > 0 && rune >= r32[0].Lo) return is32(r32, rune);
        return false;
    }
    private static bool isExcludingLatin(RangeTable table, int rune)
    {
        var r16 = table.R16;
        int offset = table.LatinOffset;
        if (r16.Length > offset && (uint)rune <= (uint)r16[^1].Hi) return is16(r16.AsSpan(offset), rune);
        var r32 = table.R32;
        if (r32.Length > 0 && rune >= r32[0].Lo) return is32(r32, rune);
        return false;
    }
    internal static bool IsUpper(int rune)
    {
        // Original Latin-1 upper-category bit test, projected from the same generated Lu ranges.
        if ((uint)rune <= 255) return Is(Lu, rune);
        return isExcludingLatin(Lu, rune);
    }
}

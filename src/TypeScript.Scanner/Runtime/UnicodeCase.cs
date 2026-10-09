// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license in licenses/Go-LICENSE.txt.
// Modified C# scanner-only port of Go 1.27.1 src/unicode/letter.go: ToLower/SimpleFold.
namespace TypeScript.Scanner;
internal static partial class UnicodeCase
{
    public static int ToLower(int rune) => ConvertCase(rune, true);
    public static int ToUpper(int rune) => ConvertCase(rune, false);
    private static int ConvertCase(int rune, bool lower)
    {
        int lo = 0, hi = CaseRanges.Length;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            var range = CaseRanges[mid];
            if (rune < range.Low)
                hi = mid;
            else if (rune > range.High)
                lo = mid + 1;
            else
            {
                int delta = lower ? range.Lower : range.Upper;
                return delta > 0x10FFFF ? range.Low + (((rune - range.Low) & ~1) | (lower ? 1 : 0)) : rune + delta;
            }
        }

        return rune;
    }

    public static int SimpleFold(int rune)
    {
        if (rune < 0 || rune > 0x10FFFF)
            return rune;
        int lo = 0, hi = CaseOrbit.Length;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (CaseOrbit[mid].From < rune)
                lo = mid + 1;
            else
                hi = mid;
        }

        if (lo < CaseOrbit.Length && CaseOrbit[lo].From == rune)
            return CaseOrbit[lo].To;
        int lower = ConvertCase(rune, true);
        return lower != rune ? lower : ConvertCase(rune, false);
    }

    // Byte strings are equivalent when each decoded rune belongs to the same
    // pinned simple-fold orbit. Invalid bytes decode to RuneError as in Go.
    public static bool EqualFold(string a, string b)
    {
        int ai = 0, bi = 0;
        while (ai < a.Length && bi < b.Length)
        {
            var(ar, aSize) = Utf8.DecodeRuneInString(a.AsSpan(ai));
            var(br, bSize) = Utf8.DecodeRuneInString(b.AsSpan(bi));
            ai += aSize;
            bi += bSize;
            if (ar == br)
                continue;
            if (ar > br)
                (ar, br) = (br, ar);
            int folded = SimpleFold(ar);
            while (folded != ar && folded < br)
                folded = SimpleFold(folded);
            if (folded != br)
                return false;
        }

        return ai == a.Length && bi == b.Length;
    }
}

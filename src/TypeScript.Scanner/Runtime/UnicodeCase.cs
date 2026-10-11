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
        // Preserve the original raw-string null failure and argument order.
        _ = a.Length;
        return EqualFold(a.AsSpan(), b is null ? throw new NullReferenceException() : b.AsSpan());
    }
    public static bool EqualFold(ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
        equalFold(a, default, false, b, default, false);
    public static bool EqualFold(ByteStringValue a, ByteStringValue b)
    {
        if (a.IsNull || b.IsNull) throw new NullReferenceException();
        return equalFold(a.IsBytes ? default : a.CharSpan, a.IsBytes ? a.ByteSpan : default, a.IsBytes,
            b.IsBytes ? default : b.CharSpan, b.IsBytes ? b.ByteSpan : default, b.IsBytes);
    }
    private static bool equalFold(ReadOnlySpan<char> ac, ReadOnlySpan<byte> ab, bool aIsBytes,
        ReadOnlySpan<char> bc, ReadOnlySpan<byte> bb, bool bIsBytes)
    {
        int aLength = aIsBytes ? ab.Length : ac.Length, bLength = bIsBytes ? bb.Length : bc.Length;
        int ai = 0, bi = 0;
        while (ai < aLength && bi < bLength)
        {
            var(ar, aSize) = aIsBytes ? Utf8.DecodeRuneInString(ab[ai..]) : Utf8.DecodeRuneInString(ac[ai..]);
            var(br, bSize) = bIsBytes ? Utf8.DecodeRuneInString(bb[bi..]) : Utf8.DecodeRuneInString(bc[bi..]);
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

        return ai == aLength && bi == bLength;
    }
}

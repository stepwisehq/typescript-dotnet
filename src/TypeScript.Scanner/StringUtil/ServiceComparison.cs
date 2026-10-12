// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Remaining pinned internal/stringutil/compare.go query/comparison workers; prefix/suffix owners remain in Compare.cs.
namespace TypeScript.Scanner;
internal static partial class StringUtil
{
    internal static bool EquateStringCaseInsensitive(string a, string b) => UnicodeCase.EqualFold(a, b);
    internal static bool EquateStringCaseSensitive(string a, string b) => a == b;
    internal static Func<string, string, bool> GetStringEqualityComparer(bool ignoreCase) => ignoreCase ? EquateStringCaseInsensitive : EquateStringCaseSensitive;
    // compare.go:34. Decode and simple-lowercase one Go rune at a time, retaining malformed byte advancement.
    internal static int CompareStringsCaseInsensitive(string a, string b) => CompareStringsCaseInsensitive(ByteStringValue.FromString(a), ByteStringValue.FromString(b));
    internal static int CompareStringsCaseInsensitive(ByteStringValue a, ByteStringValue b)
    {
        if (a.EqualsValue(b))
            return 0;
        int aPos = 0, bPos = 0;
        while (true)
        {
            var(aRune, aSize) = aPos < a.Length ? (a.IsBytes ? Utf8.DecodeRuneInString(a.ByteSpan[aPos..]) : Utf8.DecodeRuneInString(a.CharSpan[aPos..])) : (0, 0);
            var(bRune, bSize) = bPos < b.Length ? (b.IsBytes ? Utf8.DecodeRuneInString(b.ByteSpan[bPos..]) : Utf8.DecodeRuneInString(b.CharSpan[bPos..])) : (0, 0);
            if (aSize == 0)
                return bSize == 0 ? 0 : -1;
            if (bSize == 0)
                return 1;
            var lowerA = UnicodeCase.ToLower(aRune);
            var lowerB = UnicodeCase.ToLower(bRune);
            if (lowerA != lowerB)
                return lowerA < lowerB ? -1 : 1;
            aPos += aSize;
            bPos += bSize;
        }
    }

    internal static int CompareStringsCaseSensitive(string a, string b) => Math.Sign(string.CompareOrdinal(a, b));
    internal static int CompareStringsCaseSensitive(ByteStringValue a, ByteStringValue b) => a.Length == 0 ? (b.Length == 0 ? 0 : -1) : b.Length == 0 ? 1 : Math.Sign(ByteStringKey.FromValue(a).CompareTo(ByteStringKey.FromValue(b)));
    // compare.go:102. Preserve rune case comparison and exact byte-sensitive tie-break.
    internal static int CompareStringsCaseInsensitiveThenSensitive(ByteStringValue a, ByteStringValue b)
    {
        int comparison = CompareStringsCaseInsensitive(a, b);
        return comparison != 0 ? comparison : CompareStringsCaseSensitive(a, b);
    }

    internal static int CompareStringsCaseInsensitiveThenSensitive(string a, string b) => CompareStringsCaseInsensitiveThenSensitive(ByteStringValue.FromString(a), ByteStringValue.FromString(b));
    internal static Func<string, string, int> GetStringComparer(bool ignoreCase) => ignoreCase ? CompareStringsCaseInsensitive : CompareStringsCaseSensitive;
    // compare.go:121. ESLint-compatible sort deliberately uses full string ToLower rather than the rune comparer.
    internal static int CompareStringsCaseInsensitiveEslintCompatible(string a, string b)
    {
        if (a == b)
            return 0;
        var lowerA = GoStrings.ToLower(ByteStringValue.FromString(a));
        var lowerB = GoStrings.ToLower(ByteStringValue.FromString(b));
        return Math.Sign(ByteStringKey.FromValue(lowerA).CompareTo(ByteStringKey.FromValue(lowerB)));
    }
}

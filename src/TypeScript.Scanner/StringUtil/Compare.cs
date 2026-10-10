// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/stringutil/compare.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
internal static partial class StringUtil
{
    // compare.go:74/84/94. Lengths and slices refer to original Go bytes.
    public static bool HasPrefix(string s, string prefix, bool caseSensitive)
    {
        if (caseSensitive)
            return s.StartsWith(prefix, StringComparison.Ordinal);
        if (prefix.Length > s.Length)
            return false;
        return UnicodeCase.EqualFold(s[..prefix.Length], prefix);
    }

    public static bool HasSuffix(string s, string suffix, bool caseSensitive)
    {
        if (caseSensitive)
            return s.EndsWith(suffix, StringComparison.Ordinal);
        if (suffix.Length > s.Length)
            return false;
        return UnicodeCase.EqualFold(s[(s.Length - suffix.Length)..], suffix);
    }

    public static bool HasPrefixAndSuffixWithoutOverlap(string s, string prefix, string suffix, bool caseSensitive) =>
        prefix.Length + suffix.Length <= s.Length && HasPrefix(s, prefix, caseSensitive) && HasSuffix(s, suffix, caseSensitive);
}

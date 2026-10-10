// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// C# port of tsc/internal/stringutil/js_case.go, upstream
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Strings carry Go UTF-8/WTF-8 bytes.
namespace TypeScript.Scanner;

internal static partial class StringUtil
{
    public static string ToLowerJS(string str)
    {
        var (ascii, ok) = toLowerASCII(str);
        if (ok)
            return ascii;

        var builder = new GoBuilder();
        builder.Grow(str.Length);
        // Backward Final_Sigma context is accumulated without decoding backwards.
        var casedBefore = false;
        for (var i = 0; i < str.Length;)
        {
            var (r, size) = DecodeJSStringRune(str.AsSpan(i));
            i += size;
            if (IsSurrogate(r))
            {
                // WriteRune would replace a lone surrogate; restore its sentinel.
                builder.WriteString(EncodeJSStringRune(r));
            }
            else if (specialCasingMappings.TryGetValue(r, out var mapping))
            {
                if (mapping.Condition == SpecialCasingCondition.FinalSigma && isFinalSigmaContext(casedBefore, str, i))
                    builder.WriteString(mapping.ConditionalLower);
                else
                    builder.WriteString(mapping.Lower);
            }
            else
            {
                builder.WriteRune(r);
            }
            if (!isUnicodeCaseIgnorable(r))
                casedBefore = isSigmaCased(r);
        }
        return builder.String();
    }

    public static string ToUpperJS(string str)
    {
        var (ascii, ok) = toUpperASCII(str);
        if (ok)
            return ascii;

        var builder = new GoBuilder();
        builder.Grow(str.Length);
        for (var i = 0; i < str.Length;)
        {
            var (r, size) = DecodeJSStringRune(str.AsSpan(i));
            if (IsSurrogate(r))
                builder.WriteString(str[i..(i + size)]);
            else if (specialCasingMappings.TryGetValue(r, out var mapping))
                builder.WriteString(mapping.Upper);
            else
                builder.WriteRune(r);
            i += size;
        }
        return builder.String();
    }

    private static (string Value, bool Ok) toLowerASCII(string str)
    {
        var needsMapping = false;
        for (var i = 0; i < str.Length; i++)
        {
            var ch = str[i];
            if (ch >= 0x80)
                return ("", false);
            needsMapping |= 'A' <= ch && ch <= 'Z';
        }
        if (!needsMapping)
            return (str, true);

        var buffer = str.ToCharArray();
        for (var i = 0; i < buffer.Length; i++)
            if ('A' <= buffer[i] && buffer[i] <= 'Z')
                buffer[i] = (char)(buffer[i] + ('a' - 'A'));
        return (new string(buffer), true);
    }

    private static (string Value, bool Ok) toUpperASCII(string str)
    {
        var needsMapping = false;
        for (var i = 0; i < str.Length; i++)
        {
            var ch = str[i];
            if (ch >= 0x80)
                return ("", false);
            needsMapping |= 'a' <= ch && ch <= 'z';
        }
        if (!needsMapping)
            return (str, true);

        var buffer = str.ToCharArray();
        for (var i = 0; i < buffer.Length; i++)
            if ('a' <= buffer[i] && buffer[i] <= 'z')
                buffer[i] = (char)(buffer[i] - ('a' - 'A'));
        return (new string(buffer), true);
    }

    private static bool isFinalSigmaContext(bool casedBefore, string str, int afterOffset)
        => casedBefore && !hasSigmaCasedAfter(str, afterOffset);

    private static bool hasSigmaCasedAfter(string str, int start)
    {
        for (var i = start; i < str.Length;)
        {
            var (r, size) = DecodeJSStringRune(str.AsSpan(i));
            i += size;
            if (isUnicodeCaseIgnorable(r))
                continue;
            return isSigmaCased(r);
        }
        return false;
    }

    // The pinned R16/R32 range slices are concatenated in increasing order;
    // existing integer range lookup preserves membership and stride semantics.
    private static bool isSigmaCased(int r) => IsInRanges(unicodeCasedRanges, r);
    private static bool isUnicodeCaseIgnorable(int r) => IsInRanges(unicodeCaseIgnorableRanges, r);

    private enum SpecialCasingCondition : byte { None, FinalSigma }
    private readonly record struct SpecialCasingMapping(string Lower, string Upper, string ConditionalLower, SpecialCasingCondition Condition);
}

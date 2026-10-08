// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/stringutil/identifier.go.
namespace TypeScript.Scanner;

internal static partial class StringUtil
{
    public static bool IsUnicodeIdentifierStart(int ch) => IsInRanges(unicodeESNextIdentifierStart, ch);
    public static bool IsUnicodeIdentifierPart(int ch) => IsInRanges(unicodeESNextIdentifierPart, ch);
    private static bool IsInRanges((int Low,int High,int Stride)[] ranges, int ch)
    {
        int low = 0, high = ranges.Length - 1;
        while (low <= high)
        {
            int mid = low + (high-low)/2;
            var range = ranges[mid];
            if (ch < range.Low) high = mid-1;
            else if (ch > range.High) low = mid+1;
            else return (ch-range.Low)%range.Stride == 0;
        }
        return false;
    }
}

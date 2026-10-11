// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/ast/positionmap.go,
// upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
/// <summary>Bidirectional mapping between source byte offsets and UTF-16 code unit offsets.</summary>
public sealed class PositionMap
{
    internal bool asciiOnly;
    internal Slice<(int utf8Pos, int delta)> entries;
    public bool IsAsciiOnly() => asciiOnly;
    public int UTF8ToUTF16(int utf8Offset)
    {
        if (asciiOnly)
            return utf8Offset;
        int lo = 0, hi = entries.Length;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (entries[mid].utf8Pos <= utf8Offset)
                lo = mid + 1;
            else
                hi = mid;
        }

        return lo == 0 ? utf8Offset : utf8Offset - entries[lo - 1].delta;
    }

    public int UTF16ToUTF8(int utf16Offset)
    {
        if (asciiOnly)
            return utf16Offset;
        int lo = 0, hi = entries.Length;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            int utf16Pos = entries[mid].utf8Pos - entries[mid].delta;
            if (utf16Pos <= utf16Offset)
                lo = mid + 1;
            else
                hi = mid;
        }

        return lo == 0 ? utf16Offset : utf16Offset + entries[lo - 1].delta;
    }
}

public static partial class Ast
{
    public static PositionMap ComputePositionMap(string text) => ComputePositionMapByteString(GoString.ToUtf8(text));
    public static PositionMap ComputePositionMap(byte[] text) => ComputePositionMapByteString(ByteStringView.CopyFrom(text));
    internal static PositionMap ComputePositionMapByteString(string text) => ComputePositionMapByteString(ByteStringView.FromGoBytes(text));
    internal static PositionMap ComputePositionMapByteString(ByteStringView text)
    {
        var pm = new PositionMap();
        int delta = 0;
        for (int i = 0; i < text.Length;)
        {
            if (text[i] < 128)
            {
                i++;
                continue;
            }

            var(r, size) = StringUtil.DecodeJSStringRune(text.AsSpan(i));
            int utf16Size = r >= 0x10000 ? 2 : 1;
            delta += size - utf16Size;
            pm.entries = pm.entries.Append((i + size, delta));
            i += size;
        }

        pm.asciiOnly = pm.entries.Length == 0;
        return pm;
    }
}

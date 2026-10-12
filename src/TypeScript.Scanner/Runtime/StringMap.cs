// Copyright 2009 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go 1.27.1 src/strings/strings.go:513 Map; UTF8 headers remain borrowed until the first change.
namespace TypeScript.Scanner;
internal static partial class GoStrings
{
    internal static string Map(Func<int, int> mapping, string value) => Map(mapping, ByteStringValue.FromString(value)).Materialize()!;
    internal static ByteStringValue Map(Func<int, int> mapping, ByteStringValue value)
    {
        GoBuilder? builder = null;
        int position = 0;
        while (position < value.Length)
        {
            var (rune, width) = value.IsBytes ? Utf8.DecodeRuneInString(value.ByteSpan[position..]) : Utf8.DecodeRuneInString(value.CharSpan[position..]);
            int mapped = mapping(rune);
            if (mapped != rune || rune == Utf8.RuneError && width == 1)
            {
                builder = new GoBuilder();
                builder.Grow(checked(value.Length + 4 /* utf8.UTFMax */));
                builder.WriteString(value.Slice(0, position));
                if (mapped >= 0) builder.WriteRune(mapped);
                position += width;
                break;
            }
            position += width;
        }
        if (builder == null) return value;
        while (position < value.Length)
        {
            var (rune, width) = value.IsBytes ? Utf8.DecodeRuneInString(value.ByteSpan[position..]) : Utf8.DecodeRuneInString(value.CharSpan[position..]);
            int mapped = mapping(rune);
            if (mapped >= 0)
            {
                if (mapped < Utf8.RuneSelf) builder.WriteByte(mapped);
                else builder.WriteRune(mapped);
            }
            position += width;
        }
        return builder.Value();
    }
}

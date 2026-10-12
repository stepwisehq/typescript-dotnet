// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/stringutil/util.go:87/115, printer dependencies.
namespace TypeScript.Scanner;
internal static partial class StringUtil
{
    internal static Slice<ByteStringValue> SplitLines(ByteStringValue text)
    {
        int lineCount = text.IsBytes ? text.ByteSpan.Count((byte)'\n') : text.CharSpan.Count('\n');
        var lines = Slice<ByteStringValue>.Make(0, lineCount + 1);
        int start = 0, pos = 0;
        while (pos < text.Length)
        {
            switch (text[pos])
            {
                case '\r':
                    if (pos + 1 < text.Length && text[pos + 1] == '\n')
                    {
                        lines = lines.Append(text.Slice(start, pos));
                        pos += 2;
                        start = pos;
                        continue;
                    }
                    goto case '\n';
                case '\n':
                    lines = lines.Append(text.Slice(start, pos));
                    pos++;
                    start = pos;
                    continue;
            }
            pos++;
        }
        if (start < text.Length) lines = lines.Append(text.Slice(start));
        return lines;
    }
    internal static int GuessIndentation(Slice<ByteStringValue> lines)
    {
        const int MAX_SMI_X86 = 0x3fff_ffff;
        int indentation = MAX_SMI_X86;
        foreach (var line in lines)
        {
            if (line.Length == 0) continue;
            int i = 0;
            while (i < line.Length && i < indentation)
            {
                var (ch, size) = line.IsBytes ? Utf8.DecodeRuneInString(line.ByteSpan[i..]) : Utf8.DecodeRuneInString(line.CharSpan[i..]);
                if (!IsWhiteSpaceLike(ch)) break;
                i += size;
            }
            if (i < indentation) indentation = i;
            if (indentation == 0) return 0;
        }
        return indentation == MAX_SMI_X86 ? 0 : indentation;
    }
}

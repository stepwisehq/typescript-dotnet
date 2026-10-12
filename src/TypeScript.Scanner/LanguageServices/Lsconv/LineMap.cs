// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ls/lsconv/linemap.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.LanguageServices.Lsconv;
internal sealed class LSPLineMap
{
    internal Slice<int> LineStarts;
    internal bool AsciiOnly;

    // linemap.go:59: binary-search insertion position, then preceding line.
    internal int ComputeIndexOfLineStart(int targetPos)
    {
        var (lineNumber, found) = GoSlices.BinarySearchFunc(LineStarts, targetPos, static (position, target) => position.CompareTo(target));
        if (!found && lineNumber > 0)
            lineNumber--;
        return lineNumber;
    }
}

internal static partial class Lsconv
{
    // linemap.go:19: LSP recognizes CR/LF only; all positions remain byte offsets.
    internal static LSPLineMap ComputeLSPLineStarts(ByteStringValue text)
    {
        int newlineCount = text.IsBytes ? text.ByteSpan.Count((byte)'\n') : text.CharSpan.Count('\n');
        var lineStarts = Slice<int>.Make(0, checked(newlineCount + 1));
        bool asciiOnly = true;
        int textLen = text.Length;
        int pos = 0;
        int lineStart = 0;
        while (pos < textLen)
        {
            int b = text[pos];
            if (b < Utf8.RuneSelf)
            {
                pos++;
                switch (b)
                {
                    case '\r':
                        if (pos < textLen && text[pos] == '\n')
                            pos++;
                        goto case '\n';
                    case '\n':
                        lineStarts = lineStarts.Append(lineStart);
                        lineStart = pos;
                        break;
                }
            }
            else
            {
                var (_, size) = text.IsBytes ? Utf8.DecodeRuneInString(text.ByteSpan[pos..]) : Utf8.DecodeRuneInString(text.CharSpan[pos..]);
                pos += size;
                asciiOnly = false;
            }
        }
        lineStarts = lineStarts.Append(lineStart);
        return new LSPLineMap { LineStarts = lineStarts, AsciiOnly = asciiOnly };
    }
}

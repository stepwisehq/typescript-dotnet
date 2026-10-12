// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/sourcemap/util.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
namespace TypeScript.SourceMap;
internal static partial class SourceMaps
{
    internal static ByteStringValue TryGetSourceMappingURL(ECMALineInfo? lineInfo)
    {
        if (lineInfo != null)
        {
            for (int index = lineInfo.LineCount()-1; index >= 0; index--)
            {
                var line = GoStrings.TrimLeftFunc(lineInfo.LineText(index), GoUnicode.IsSpace);
                line = GoStrings.TrimRightFunc(line, StringUtil.IsLineBreak);
                if (line.Length == 0) continue;
                if (line.Length < 4 || !line.AsSpan().StartsWith("//"u8) || line[2] != '#' && line[2] != '@' || line[3] != ' ') break;
                if (line.AsSpan(4).StartsWith("sourceMappingURL="u8))
                    return ByteStringValue.FromView(GoStrings.TrimRightFunc(line.Slice(4+17), GoUnicode.IsSpace));
            }
        }
        return default;
    }
}

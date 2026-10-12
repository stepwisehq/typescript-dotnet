// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/sourcemap/lineinfo.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
namespace TypeScript.SourceMap;
internal sealed class ECMALineInfo
{
    internal readonly ByteStringView text;
    internal readonly Slice<int> lineStarts;
    private ECMALineInfo(ByteStringView text, Slice<int> lineStarts) { this.text = text; this.lineStarts = lineStarts; }
    internal static ECMALineInfo CreateECMALineInfo(ByteStringView text, Slice<int> lineStarts) => new(text, lineStarts);
    internal static ECMALineInfo CreateECMALineInfo(string goBytes, Slice<int> lineStarts) => new(ByteStringView.CopyFrom(GoString.Bytes(goBytes)), lineStarts);
    internal int LineCount() => lineStarts.Length;
    internal ByteStringView LineText(int line) => text.Slice(lineStarts[line], line+1 < lineStarts.Length ? lineStarts[line+1] : text.Length);
}

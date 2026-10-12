// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/core/textchange.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
internal readonly record struct TextChange(TextRange TextRange, ByteStringValue NewText)
{
    // textchange.go:10. The concatenation creates the new owned text; unchanged
    // prefix/suffix remain borrowed headers until that required copy.
    internal ByteStringValue ApplyTo(ByteStringValue text) => ByteStringValue.Concat(text.Slice(0, TextRange.Pos()), NewText, text.Slice(TextRange.End()));
}

internal static partial class Core
{
    // textchange.go:14. Caller supplies ordered nonoverlapping edits, as upstream;
    // do not sort or silently repair invalid ranges.
    internal static ByteStringValue ApplyBulkEdits(ByteStringValue text, Slice<TextChange> edits)
    {
        var builder = new GoBuilder();
        builder.Grow(text.Length);
        int lastEnd = 0;
        foreach (var edit in edits)
        {
            int start = edit.TextRange.Pos();
            if (start != lastEnd)
                builder.WriteString(text.Slice(lastEnd, start));
            builder.WriteString(edit.NewText);
            lastEnd = edit.TextRange.End();
        }

        builder.WriteString(text.Slice(lastEnd));
        return builder.Value();
    }
}

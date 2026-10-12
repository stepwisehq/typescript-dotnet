// Copyright 2009 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 strings/strings.go Join borrowed-header counterpart required by ls/jsdoc.go.
namespace TypeScript.Scanner;
internal static partial class GoStrings
{
    internal static ByteStringValue Join(Slice<ByteStringValue> values, ByteStringValue separator)
    {
        if (values.Count == 0)
            return default;
        if (values.Count == 1)
            return values[0];
        long length = (long)separator.Length * (values.Count - 1);
        foreach (var value in values)
            length += value.Length;
        if (length > int.MaxValue)
            throw new InvalidOperationException("strings: Join output length overflow");
        var builder = new GoBuilder();
        builder.Grow((int)length);
        builder.WriteString(values[0]);
        for (int i = 1; i < values.Count; i++)
        {
            builder.WriteString(separator);
            builder.WriteString(values[i]);
        }

        return builder.Value();
    }
}

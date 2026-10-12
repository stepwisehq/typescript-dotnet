// Copyright (c) The Go Authors. All rights reserved.
// Licensed under the BSD-style license in licenses/Go-LICENSE.txt.
// jsontext/options.go WithIndentPrefix/WithIndent and encode.go AppendIndent.
using TypeScript.Scanner;
using TypeScript.Json.Internal;

namespace TypeScript.Json;
internal readonly struct JsonIndentation
{
    internal readonly ByteStringValue Prefix, Indent;
    internal readonly bool Multiline;
    internal JsonIndentation(ByteStringValue prefix, ByteStringValue indent)
    {
        Prefix = prefix;
        Indent = indent;
        Multiline = prefix.Length != 0 || indent.Length != 0;
        if (!Multiline)
            return;
        Validate(prefix, "indent prefix");
        Validate(indent, "indent");
    }

    private static void Validate(ByteStringValue value, string role)
    {
        int start = 0, end = value.Length;
        while (start < end && value[start] is ' ' or '\t')
            start++;
        while (end > start && value[end - 1] is ' ' or '\t')
            end--;
        if (start != end)
            throw new InvalidOperationException("json: invalid character " + JsonWire.QuoteRune(value.Slice(start, end).Materialize()!.AsSpan()) + " in " + role);
    }

    internal int Length(int n) => !Multiline || n == 0 ? 0 : checked(1 + Prefix.Length + (n - 1) * Indent.Length);
    // n has the original NeedIndent convention: n=1 is newline+prefix,
    // n=2 adds one indent body; n=0 emits no bytes.
    internal void AppendIndent(GoBuilder output, int n)
    {
        if (!Multiline || n == 0)
            return;
        output.WriteByte('\n');
        output.WriteString(Prefix);
        for (; n > 1; n--)
            output.WriteString(Indent);
    }
}

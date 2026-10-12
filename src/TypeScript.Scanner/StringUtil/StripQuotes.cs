// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Focused internal/stringutil/util.go:222 at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
internal static partial class StringUtil
{
    internal static ByteStringValue StripQuotesValue(ByteStringValue name)
    {
        if (name.Length < 2)
            return name;
        var(first, _) = name.IsBytes ? Utf8.DecodeRuneInString(name.ByteSpan) : Utf8.DecodeRuneInString(name.CharSpan);
        var(last, _) = name.IsBytes ? Utf8.DecodeLastRuneInString(name.ByteSpan) : Utf8.DecodeLastRuneInString(name.CharSpan);
        return first == last && first is '\'' or '"' or '`' ? name.Slice(1, name.Length - 1) : name;
    }

    internal static string StripQuotes(string name) => StripQuotesValue(ByteStringValue.FromString(name)).Materialize()!;
}

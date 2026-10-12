// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Demanded compiler-output counterparts from tsc/internal/stringutil/util.go.
namespace TypeScript.Scanner;
internal static partial class StringUtil
{
    // util.go:144. URI encoding operates on bytes, including arbitrary source-map URL bytes.
    internal static string EncodeURI(string text) => EncodeURI(ByteStringValue.FromString(text)).Materialize()!;
    internal static ByteStringValue EncodeURI(ByteStringValue text)
    {
        var builder = new GoBuilder();
        const string upperhex = "0123456789ABCDEF";
        for (int i = 0; i < text.Length; i++)
        {
            int b = text[i];
            if (!shouldEscapeForEncodeURI(b))
            {
                builder.WriteByte((byte)b);
                continue;
            }

            builder.WriteByte((byte)'%');
            builder.WriteByte((byte)upperhex[b >> 4]);
            builder.WriteByte((byte)upperhex[b & 15]);
        }

        return builder.Value();
    }

    // util.go:164.
    private static bool shouldEscapeForEncodeURI(int b)
    {
        if (b is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9')
            return false;
        return b is not (';' or '/' or '?' or ':' or '@' or '&' or '=' or '+' or '$' or ',' or '#' or '-' or '_' or '.' or '!' or '~' or '*' or '\'' or '(' or ')');
    }

    // util.go:182. All three accepted BOMs suppress adding a UTF8 BOM, as upstream does.
    private static int getByteOrderMarkLength(ByteStringValue text)
    {
        if (text.Length == 0)
            return 0;
        switch (text[0])
        {
            case 0xfe:
                return text.Length >= 2 && text[1] == 0xff ? 2 : 0;
            case 0xff:
                return text.Length >= 2 && text[1] == 0xfe ? 2 : 0;
            case 0xef:
                return text.Length >= 3 && text[1] == 0xbb && text[2] == 0xbf ? 3 : 0;
            default:
                return 0;
        }
    }

    // util.go:215.
    internal static string AddUTF8ByteOrderMark(string text) => AddUTF8ByteOrderMark(ByteStringValue.FromString(text)).Materialize()!;
    internal static ByteStringValue AddUTF8ByteOrderMark(ByteStringValue text) => getByteOrderMarkLength(text) == 0 ? ByteStringValue.Concat(ByteStringValue.FromString("\u00ef\u00bb\u00bf"), text) : text;
}

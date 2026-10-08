// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/printer/utilities.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using System.Text;
using TypeScript.Scanner;

namespace TypeScript.DiagnosticPrinting;
internal sealed partial class Printer
{
    private static string getLiteralText(Node node)
    {
        if (node.Kind == Kind.NumericLiteral)
            return node.Text();
        if (node.Kind != Kind.StringLiteral)
            throw TypeScript.Checking.Checker.unsupported("getLiteralText: nonscalar literal");
        char quote = (node.AsStringLiteral().TokenFlags & TokenFlags.SingleQuote) != 0 ? '\'' : '"';
        var b = new StringBuilder(node.Text().Length + 2);
        b.Append(quote);
        escapeStringWorker(node.Text(), quote, b);
        b.Append(quote);
        return b.ToString();
    }

    // escapeStringWorker (77), NeverAsciiEscape; JSX/backtick flags excluded.
    private static void escapeStringWorker(string s, char quote, StringBuilder b)
    {
        int pos = 0, i = 0;
        while (i < s.Length)
        {
            var(ch, size) = StringUtil.DecodeJSStringRune(s.AsSpan(i));
            bool escape = ch is >= 0xD800 and <= 0xDFFF || ch == 0xFFFD && size == 1;
            switch (ch)
            {
                case '\\':
                    escape = true;
                    break;
                case 0x2028:
                case 0x2029:
                case 0x85:
                case '\r':
                case '\n':
                    escape = true;
                    break;
                default:
                    if (ch == quote || ch <= 0x1F)
                        escape = true;
                    break;
            }

            if (escape)
            {
                if (pos < i)
                    b.Append(s.AsSpan(pos, i - pos));
                if (ch > 0xFFFF)
                {
                    ch -= 0x10000;
                    encodeUtf16EscapeSequence(b, (ch >> 10) + 0xD800);
                    encodeUtf16EscapeSequence(b, (ch & 0x3FF) + 0xDC00);
                }
                else if (ch is >= 0xD800 and <= 0xDFFF)
                    encodeUtf16EscapeSequence(b, ch);
                else if (ch == 0)
                    b.Append(i + 1 < s.Length && s[i + 1] is >= '0' and <= '9' ? "\\x00" : "\\0");
                else
                {
                    string match = ch switch
                    {
                        '\t' => "\\t",
                        '\v' => "\\v",
                        '\f' => "\\f",
                        '\b' => "\\b",
                        '\r' => "\\r",
                        '\n' => "\\n",
                        '\\' => "\\\\",
                        '"' => "\\\"",
                        '\'' => "\\'",
                        0x2028 => "\\u2028",
                        0x2029 => "\\u2029",
                        0x85 => "\\u0085",
                        _ => null
                    };
                    if (match != null)
                        b.Append(match);
                    else
                        encodeUtf16EscapeSequence(b, ch);
                }

                pos = i + size;
            }

            i += size;
        }

        if (pos < i)
            b.Append(s.AsSpan(pos));
    }

    private static void encodeUtf16EscapeSequence(StringBuilder b, int ch)
    {
        string hex = ch.ToString("X", System.Globalization.CultureInfo.InvariantCulture);
        b.Append("\\u");
        for (int i = hex.Length; i < 4; i++)
            b.Append('0');
        b.Append(hex);
    }
}

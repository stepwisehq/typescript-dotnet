// Copyright (c) The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Whole Go1.27.1 strings/strings_test.go TestTrimFunc: all9rows ×3trimmers.
using System.Text;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class TrimFuncChecks
{
    private readonly record struct Row(Func<int, bool> Predicate, ByteStringValue Input, ByteStringValue Trim, ByteStringValue Left, ByteStringValue Right);
    private static ByteStringValue Utf8Text(string value) => ByteStringValue.FromView(ByteStringView.FromOwnedBytes(Encoding.UTF8.GetBytes(value)));
    private static ByteStringValue Raw(string value) => ByteStringValue.FromString(value);
    public static int RunAll()
    {
        const string space = "\t\v\r\f\n\u0085\u00a0\u2000\u3000";
        var rows = new Row[]
        {
            new(GoUnicode.IsSpace, Utf8Text(space + " hello " + space), Utf8Text("hello"), Utf8Text("hello " + space), Utf8Text(space + " hello")),
            new(GoUnicode.IsDigit, Utf8Text("\u0e50\u0e5212hello34\u0e50\u0e51"), Utf8Text("hello"), Utf8Text("hello34\u0e50\u0e51"), Utf8Text("\u0e50\u0e5212hello")),
            new(GoUnicode.IsUpper, Utf8Text("\u2C6F\u2C6F\u2C6F\u2C6FABCDhelloEF\u2C6F\u2C6FGH\u2C6F\u2C6F"), Utf8Text("hello"), Utf8Text("helloEF\u2C6F\u2C6FGH\u2C6F\u2C6F"), Utf8Text("\u2C6F\u2C6F\u2C6F\u2C6FABCDhello")),
            new(static rune => !GoUnicode.IsSpace(rune), Utf8Text("hello" + space + "hello"), Utf8Text(space), Utf8Text(space + "hello"), Utf8Text("hello" + space)),
            new(static rune => !GoUnicode.IsDigit(rune), Utf8Text("hello\u0e50\u0e521234\u0e50\u0e51helo"), Utf8Text("\u0e50\u0e521234\u0e50\u0e51"), Utf8Text("\u0e50\u0e521234\u0e50\u0e51helo"), Utf8Text("hello\u0e50\u0e521234\u0e50\u0e51")),
            new(static rune => rune != Utf8.RuneError, Raw("ab\u00c0a\u00c0cd"), Raw("\u00c0a\u00c0"), Raw("\u00c0a\u00c0cd"), Raw("ab\u00c0a\u00c0")),
            new(static rune => rune == Utf8.RuneError, Raw("\u00c0a\u00c0"), Raw("a"), Raw("a\u00c0"), Raw("\u00c0a")),
            new(GoUnicode.IsSpace, Raw(""), Raw(""), Raw(""), Raw("")),
            new(GoUnicode.IsSpace, Raw(" "), Raw(""), Raw(""), Raw("")),
        };
        foreach (var row in rows)
        {
            // The original comparisons remain byte-content equality, not display text.
            Equal(GoStrings.TrimFunc(row.Input, row.Predicate), row.Trim, "TrimFunc");
            Equal(GoStrings.TrimLeftFunc(row.Input, row.Predicate), row.Left, "TrimLeftFunc");
            Equal(GoStrings.TrimRightFunc(row.Input, row.Predicate), row.Right, "TrimRightFunc");
        }

        return 1;
    }

    private static void Equal(ByteStringValue actual, ByteStringValue expected, string name)
    {
        if (!actual.EqualsValue(expected))
            throw new InvalidOperationException(name + " differs from original Go expectation");
    }
}

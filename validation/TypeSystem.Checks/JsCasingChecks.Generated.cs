// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# original assertion port; upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Generated from the complete internal/stringutil/js_case_test.go table.
using TypeScript.Scanner;

namespace TypeSystem.Checks;

public static class JsCasingChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestJSCasing"];
    public const int OriginalTableAssertions = 26;
    private static string B(string value) => GoString.ToUtf8(value);

    public static void Run(string name)
    {
        if (name != "TestJSCasing") throw new ArgumentException("Unknown original casing test.");
        var rows = new (string Name, string Got, string Want)[]
        {
            ("ascii lowercase", StringUtil.ToLowerJS(B("HELLO")), B("hello")),
            ("ascii uppercase", StringUtil.ToUpperJS(B("hello")), B("HELLO")),
            ("lowercase dotted i", StringUtil.ToLowerJS(B("İSPANYOL")), B("i̇spanyol")),
            ("lowercase lone sigma", StringUtil.ToLowerJS(B("Σ")), B("σ")),
            ("lowercase final sigma", StringUtil.ToLowerJS(B("ΟΣ")), B("ος")),
            ("lowercase non-sigma greek", StringUtil.ToLowerJS(B("Ω")), B("ω")),
            ("uppercase sharp s", StringUtil.ToUpperJS(B("ßfoo")), B("SSFOO")),
            ("uppercase non-ascii simple mapping", StringUtil.ToUpperJS(B("ω")), B("Ω")),
            ("uppercase ligature", StringUtil.ToUpperJS(B("ﬁoo")), B("FIOO")),
            ("capitalize-style uppercase", StringUtil.ToUpperJS(B("ß")) + B("foo"), B("SSfoo")),
            ("uncapitalize-style lowercase", StringUtil.ToLowerJS(B("İ")) + B("foo"), B("i̇foo")),
            ("lowercase final sigma after lowercase letter without uppercase mapping", StringUtil.ToLowerJS(B("ʕΣ")), B("ʕς")),
            ("lowercase sigma after modifier letter", StringUtil.ToLowerJS(B("ʰΣ")), B("ʰσ")),
            ("lowercase sigma after case ignorable ypogegrammeni", StringUtil.ToLowerJS(B("ͅΣ")), B("ͅσ")),
            ("lowercase final sigma after feminine ordinal indicator", StringUtil.ToLowerJS(B("ªΣ")), B("ªς")),
            ("lowercase final sigma after masculine ordinal indicator", StringUtil.ToLowerJS(B("ºΣ")), B("ºς")),
            ("lowercase final sigma after roman numeral", StringUtil.ToLowerJS(B("ⅠΣ")), B("ⅰς")),
            ("lowercase sigma after uppercase property added after unicode 15", StringUtil.ToLowerJS(B("\u1C89Σ")), B("\u1C89σ")),
            ("lowercase sigma after uppercase property skewed from local v8 unicode data", StringUtil.ToLowerJS(B("\uA7CBΣ")), B("\uA7CBσ")),
            ("lowercase sigma before immediate latin letter", StringUtil.ToLowerJS(B("ΣA")), B("σa")),
            ("lowercase sigma before immediate roman numeral letter", StringUtil.ToLowerJS(B("ΣⅠ")), B("σⅰ")),
            ("lowercase sigma before case ignorable then latin letter", StringUtil.ToLowerJS(B("ΣͅA")), B("σͅa")),
            ("uppercase lone surrogate", StringUtil.ToUpperJS(StringUtil.EncodeJSStringRune(0xD800)), StringUtil.EncodeJSStringRune(0xD800)),
            ("lowercase lone surrogate", StringUtil.ToLowerJS(B("A") + StringUtil.EncodeJSStringRune(0xD800) + B("B")), B("a") + StringUtil.EncodeJSStringRune(0xD800) + B("b")),
            ("uppercase lone low surrogate with text", StringUtil.ToUpperJS(StringUtil.EncodeJSStringRune(0xDC00) + B("x")), StringUtil.EncodeJSStringRune(0xDC00) + B("X")),
            ("lowercase lone surrogate before sigma", StringUtil.ToLowerJS(StringUtil.EncodeJSStringRune(0xD800) + B("Σ")), StringUtil.EncodeJSStringRune(0xD800) + B("σ")),
        };
        foreach (var row in rows)
            if (!StringComparer.Ordinal.Equals(row.Got, row.Want))
                throw new InvalidOperationException("TestJSCasing: " + row.Name);
    }
}

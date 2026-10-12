// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// x/text v0.42.0 unicode/norm/composition_test.go:85–117.
// Original output and byte-length assertions with NFD.String as the adapted setup,
// replacing the test-only per-rune insertion driver. These are assertion channels,
// not unchanged whole-function ports; the NFKD rows are outside the selected API.
using TypeScript.Scanner;
using TypeScript.UnicodeNormalization;

namespace LanguageServices.Checks;
public static class NormalizationStringChannels
{
    public static readonly string[] Names = ["TestInsert.NfdStringSetup", "TestDecomposition.NfdStringSetup"];
    private static string Raw(string text) => ByteStringView.FromUnicode(text).Materialize();
    public static int RunAll()
    {
        foreach (string name in Names)
            Run(name);
        return Names.Length;
    }
    public static void Run(string name)
    {
        (string Input, string Output)[] rows = name switch
        {
            "TestInsert.NfdStringSetup" => [
                ("a", "a"),
                (Raw("\u0300"), Raw("\u0300")),
                (Raw("\u0300\u0316"), Raw("\u0316\u0300")),
                (Raw("\u0316\u0300"), Raw("\u0316\u0300")),
                (Raw("A\u0316\u0300"), Raw("A\u0316\u0300")),
                (Raw("A\u0300\u0316"), Raw("A\u0316\u0300")),
                (Raw("\u0300\u0316A"), Raw("\u0316\u0300A")),
                (Raw("A\u0300@\u0316"), Raw("A\u0300@\u0316"))],
            "TestDecomposition.NfdStringSetup" => [
                (Raw("\u00C0"), Raw("A\u0300")),
                (Raw("\uAC00"), Raw("\u1100\u1161")),
                (Raw("\u01C4"), Raw("\u01C4")),
                (Raw("\u320E"), Raw("\u320E")),
                (Raw("음ẻ과"), Raw("\u110B\u1173\u11B7e\u0309\u1100\u116A"))],
            _ => throw new ArgumentException(name)
        };
        for (int index = 0; index < rows.Length; index++)
        {
            string actual = Norm.NFD.String(rows[index].Input);
            if (actual.Length != rows[index].Output.Length || actual != rows[index].Output)
                throw new InvalidOperationException(name + " original output/length row " + index);
        }
    }
}

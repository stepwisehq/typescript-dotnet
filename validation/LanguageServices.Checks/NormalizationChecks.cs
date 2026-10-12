// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Original x/text v0.42.0 unicode/norm/normalize_test.go TestDecomposeSegment: all20 rows.
using TypeScript.Scanner;
using TypeScript.UnicodeNormalization;

namespace LanguageServices.Checks;
public static class NormalizationChecks
{
    public static readonly string[] Names = ["TestDecomposeSegment"];
    private static string Raw(string text) => ByteStringView.FromUnicode(text).Materialize();
    private static string Repeat(string value, int count) => string.Concat(Enumerable.Repeat(value, count));
    private static string Grave(int count) => Repeat(Raw("\u0300"), count);
    private static readonly string Joiner = Raw("\u034F");
    public static int RunAll()
    {
        Run(Names[0]);
        return 1;
    }

    public static void Run(string name)
    {
        if (name != Names[0])
            throw new ArgumentException(name);
        (string Input, int Position, string Buffer)[] rows = [("\xC2", 1, ""), ("\xC0", 1, "\xC0"), (Raw("\u00E0") + "\x80", 2, Raw("\u0061\u0300")), ("a", 1, "a"), ("ab", 1, "a"), (Raw("a\u0300"), 3, Raw("a\u0300")), (Raw("a\u0300b"), 3, Raw("a\u0300")), (Raw("\u00C0"), 2, Raw("A\u0300")), (Raw("\u00C0b"), 2, Raw("A\u0300")), (Grave(31), 60, Grave(30) + Joiner), ("a" + Grave(31), 61, "a" + Grave(30) + Joiner), (Raw("\u0B47\u0300\u0B3E"), 8, Raw("\u0B47\u0300\u0B3E")), (Raw("\u1100\u0300\u1161"), 8, Raw("\u1100\u0300\u1161")), (Raw("\u0B47\u0B3E"), 6, Raw("\u0B47\u0B3E")), (Raw("\u1100\u1161"), 6, Raw("\u1100\u1161")), (Raw("\u0D4A") + Repeat(Raw("\u0D3E"), 31), 90, Raw("\u0D46") + Repeat(Raw("\u0D3E"), 30) + Joiner), (Grave(30), 60, Grave(30)), (Grave(30) + Raw("\uFF9E"), 60, Grave(30) + Joiner), ("\xCC", 1, ""), (Raw("\u0300") + "\xCC", 2, Raw("\u0300"))];
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var buffer = new ReorderBuffer
            {
                Source = ByteStringValue.FromString(row.Input),
                SourceLength = row.Input.Length
            };
            int position = Norm.DecomposeSegment(ref buffer, 0, true);
            string output = GoString.FromBytes(buffer.Output.AsSpan());
            if (position != row.Position || output != row.Buffer)
                throw new InvalidOperationException("Original NFD segment row " + index);
        }
    }

    // Authored actual NFD.String public observations, distinct from original multi-form TestString.
    public static int RunAuthoredControls()
    {
        (string Input, string Output)[] rows = [("plain ASCII", "plain ASCII"), (Raw("\u00C0"), Raw("A\u0300")), (Raw("\uAC01"), Raw("\u1100\u1161\u11A8")), ("\x80\xC2", "\x80\xC2"), ("a" + Grave(31), "a" + Grave(30) + Joiner + Grave(1)), // Both musical combining marks are Mc: stream-safe CGJ still separates their reorder segments.
 (Repeat(Raw("\U0001D16D"), 30) + Raw("\U0001D165"), Repeat(Raw("\U0001D16D"), 30) + Joiner + Raw("\U0001D165")), (Raw("prefix\u00C0middle\u00E9"), Raw("prefixA\u0300middlee\u0301")), (Raw("a\u0315\u0300"), Raw("a\u0300\u0315"))];
        foreach (var row in rows)
            if (Norm.NFD.String(row.Input) != row.Output)
                throw new InvalidOperationException("Authored NFD.String byte output differs");
        return rows.Length;
    }
}

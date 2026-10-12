// Copyright 2011 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// normalize_test.go TestSpan: selected NFD rows through its actual quickSpan worker.
// The test-only error projection copies normalize.go:288 SpanString; public byte
// and non-NFD form APIs are not implemented or credited by this channel.
using TypeScript.Scanner;
using TypeScript.UnicodeNormalization;
namespace LanguageServices.Checks;
public static class NormalizationSpanChecks
{
    private static string Raw(string text) => ByteStringView.FromUnicode(text).Materialize();
    public static int RunAll()
    {
        (string Input, bool AtEof, int Length, int Error)[] rows = [
            ("", true, 0, 0),
            ("a", true, 1, 0),
            ("abc", true, 3, 0),
            (Raw("оb"), true, 3, 0),
            ("\u00cc", true, 1, 0),
            ("\u00cc", false, 0, 2),
            ("a\u00cc", true, 2, 0),
            ("a\u00cc", false, 0, 2),
            (Raw("̖̀"), true, 0, 1),
            (Raw("̖̀"), false, 0, 1),
            (Raw("̖̀cd"), true, 0, 1),
            (Raw("̖̀cd"), false, 0, 1),
            (Raw("͛͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝"), true, 0, 1),
            (Raw("a͛͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝"), true, 0, 1),
            (Raw("Ɵ͛͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝"), true, 0, 1),
            (Raw("aa͛͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝"), true, 1, 1),
            (Raw("͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͏͛"), true, 64, 0),
            (Raw("a͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͏͛"), true, 65, 0),
            (Raw("Ɵ͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͏͛"), true, 66, 0),
            (Raw("aa͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͏͛"), true, 66, 0),
            (Raw("a͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͏͛"), false, 61, 2),
            (Raw("Ɵ͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͏͛"), false, 62, 2),
            (Raw("aa͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͝͏͛"), false, 62, 2),
            (Raw("À"), true, 0, 1),
            (Raw("abcÀ"), true, 3, 1),
            (Raw("̀"), true, 2, 0),
            (Raw("ab̀"), true, 4, 0),
            (Raw("ab̀cd"), true, 6, 0),
            (Raw("̀cd"), true, 4, 0),
            (Raw("̖̀"), true, 4, 0),
            (Raw("ab̖̀"), true, 6, 0),
            (Raw("ab̖̀cd"), true, 8, 0),
            (Raw("ab̖̀À"), true, 6, 1),
            (Raw("̖̀cd"), true, 6, 0),
            (Raw("ӧb"), true, 5, 0),
            (Raw("ab̖̀"), true, 1, 1),
            (Raw("ab̖̀cd"), true, 1, 1),
            (Raw("같은"), true, 0, 1),
        ];
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var (length, complete) = Norm.QuickSpan(ByteStringValue.FromString(row.Input), 0, row.Input.Length, row.AtEof);
            int error = length < row.Input.Length ? (complete ? 2 : 1) : 0;
            if (length != row.Length || error != row.Error)
                throw new InvalidOperationException("Original NFD span channel row " + index);
        }
        return 1;
    }
}

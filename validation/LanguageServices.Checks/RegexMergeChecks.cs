// Copyright 2014 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 regexp/onepass_test.go TestMergeRuneSet: complete original arrays/assertions.
using TypeScript.Scanner;
using TypeScript.RegularExpressions;
using TypeScript.RegularExpressions.Syntax;
namespace LanguageServices.Checks;
public static class RegexMergeChecks
{
    public static int RunAll()
    {
        foreach (var row in Rows)
        {
            var (merged, next) = GoRegexp.MergeRuneSets(RegexSlice<int>.FromArray(row.Left), RegexSlice<int>.FromArray(row.Right), row.LeftPc, row.RightPc);
            if (!merged.AsSpan().SequenceEqual(row.Merged) || !next.AsSpan().SequenceEqual(row.Next))
                throw new InvalidOperationException("wrong merged rune set");
        }
        return 1;
    }
    private static readonly (int[] Left, int[] Right, int[] Merged, uint[] Next, uint LeftPc, uint RightPc)[] Rows = [
        ([69, 69], [], [69, 69], [1], 1, 2),
        ([69, 69], [69, 69], [], [uint.MaxValue], 1, 1),
        ([69, 69], [69, 69], [], [uint.MaxValue], 1, 2),
        ([69, 69], [71, 71], [69, 69, 71, 71], [1, 2], 1, 2),
        ([71, 71], [69, 69], [69, 69, 71, 71], [2, 1], 1, 2),
        ([60, 60, 71, 71, 101, 101], [69, 69, 88, 88], [60, 60, 69, 69, 71, 71, 88, 88, 101, 101], [1, 2, 1, 2, 1], 1, 2),
        ([69, 74], [71, 71], [], [uint.MaxValue], 1, 2),
        ([69, 74], [68, 75], [], [uint.MaxValue], 1, 2),
        ([69, 74], [74, 75], [], [uint.MaxValue], 1, 2),
        ([69, 74], [65, 69], [], [uint.MaxValue], 1, 2),
        ([69, 74], [71, 74], [], [uint.MaxValue], 1, 2),
        ([69, 74], [65, 71], [], [uint.MaxValue], 1, 2),
        ([69, 74, 60, 65], [66, 67], [], [uint.MaxValue], 1, 2)
    ];
}

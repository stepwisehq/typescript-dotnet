// Copyright (c) The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Whole Go1.27.1 slices/slices_test.go TestIndexFunc and TestContainsFunc.
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class SlicePredicateChecks
{
    private readonly record struct Row(Slice<int> Values, int Target, int Want);
    private static readonly Row[] rows = [new(default, 0, -1), new(Slice<int>.Make(0), 0, -1), new(Slice<int>.FromArray([1, 2, 3]), 2, 1), new(Slice<int>.FromArray([1, 2, 2, 3]), 2, 1), new(Slice<int>.FromArray([1, 2, 3, 2]), 2, 1), ];
    public static int RunAll()
    {
        TestIndexFunc();
        TestContainsFunc();
        return 2;
    }

    public static void TestIndexFunc()
    {
        foreach (var row in rows)
            Require(GoSlices.IndexFunc(row.Values, value => value == row.Target) == row.Want);
        var values = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("hi"), ByteStringValue.FromString("HI")]);
        Require(GoSlices.IndexFunc(values, value => value.EqualsString("HI")) == 1);
        Require(GoSlices.IndexFunc(values, value => UnicodeCase.EqualFold(value, ByteStringValue.FromString("HI"))) == 0);
    }

    public static void TestContainsFunc()
    {
        foreach (var row in rows)
            Require(GoSlices.ContainsFunc(row.Values, value => value == row.Target) == (row.Want != -1));
        var values = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("hi"), ByteStringValue.FromString("HI")]);
        Require(GoSlices.ContainsFunc(values, value => value.EqualsString("HI")));
        Require(!GoSlices.ContainsFunc(values, value => value.EqualsString("hI")));
        Require(GoSlices.ContainsFunc(values, value => UnicodeCase.EqualFold(value, ByteStringValue.FromString("hI"))));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("Original slice predicate assertion failed");
    }
}

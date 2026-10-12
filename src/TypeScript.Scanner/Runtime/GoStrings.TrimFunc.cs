// Copyright (c) The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license in licenses/Go-LICENSE.txt.
// Go1.27.1 strings/strings.go868–875,891–893,910–917: demanded raw closure.
// TrimRightFunc reuses the accepted raw worker in ParserStrings.cs.
namespace TypeScript.Scanner;
internal static partial class GoStrings
{
    internal static ByteStringValue TrimLeftFunc(ByteStringValue text, Func<int, bool> predicate)
    {
        int index = indexFuncValue(text, predicate, false);
        return index == -1 ? ByteStringValue.FromString("") : text.Slice(index);
    }

    internal static ByteStringValue TrimFunc(ByteStringValue text, Func<int, bool> predicate) => TrimRightFunc(TrimLeftFunc(text, predicate), predicate);
    private static int indexFuncValue(ByteStringValue text, Func<int, bool> predicate, bool truth)
    {
        foreach (var item in new GoRuneIndexRange(text))
            if (predicate(item.Value) == truth)
                return item.Key;
        return -1;
    }
}

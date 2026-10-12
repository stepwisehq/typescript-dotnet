// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete stringutil/util.go:256 TruncateByRunes raw owner required by symbols.
namespace TypeScript.Scanner;
internal static partial class StringUtil
{
    internal static ByteStringValue TruncateByRunesValue(ByteStringValue text, int maxLength)
    {
        if (text.Length < maxLength)
            return text;
        if (maxLength <= 0)
            return ByteStringValue.FromString("");
        int runeCount = 0;
        foreach (var pair in new GoRuneIndexRange(text))
        {
            runeCount++;
            if (runeCount > maxLength)
                return text.Slice(0, pair.Key);
        }

        return text;
    }
}

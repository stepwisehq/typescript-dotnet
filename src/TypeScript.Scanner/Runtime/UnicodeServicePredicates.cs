// Copyright 2009 The Go Authors. All rights reserved.
// BSD-style license: licenses/Go-LICENSE.txt.
// Go1.27.1 unicode/letter.go and digit.go: original category predicates.
namespace TypeScript.Scanner;
internal static partial class GoUnicode
{
    internal static bool IsLower(int rune)
    {
        // Same pinned category bit membership for Latin-1; do not consult CLR Unicode tables.
        if ((uint)rune <= 255)
            return Is(Ll, rune);
        return isExcludingLatin(Ll, rune);
    }

    internal static bool IsLetter(int rune)
    {
        if ((uint)rune <= 255)
            return Is(L, rune);
        return isExcludingLatin(L, rune);
    }

    internal static bool IsDigit(int rune)
    {
        if ((uint)rune <= 255)
            return '0' <= rune && rune <= '9';
        return isExcludingLatin(Nd, rune);
    }
}

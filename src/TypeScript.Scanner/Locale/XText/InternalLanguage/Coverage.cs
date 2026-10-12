// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by licenses/XText-LICENSE.txt.
// Required original canonicalization assertion dependency: internal/language/coverage.go.
using TypeScript.Scanner;

namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
    internal static Slice<int> BaseLanguages()
    {
        var languages = Slice<int>.Make(0, 8798); // Generated NumLanguages.
        for (int i = 0; i < langNoIndexOffset; i++)
            if (i != nonCanonicalUnd)
                languages = languages.Append(i);
        int id = langNoIndexOffset;
        foreach (int value in langNoIndex)
        {
            int bits = value;
            for (int k = 0; k < 8; k++)
            {
                if ((bits & 1) == 1)
                    languages = languages.Append(id);
                bits >>= 1;
                id++;
            }
        }

        return languages;
    }
}

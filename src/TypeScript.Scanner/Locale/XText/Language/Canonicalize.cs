// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by licenses/XText-LICENSE.txt.
// Direct matching dependency of x/text v0.42.0 language/language.go:115.
namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
    [Flags]
    internal enum CanonType
    {
        Raw = 0,
        DeprecatedBase = 1,
        DeprecatedScript = 2,
        DeprecatedRegion = 4,
        SuppressScript = 8,
        Legacy = 16,
        Macro = 32,
        CLDR = 64,
        Deprecated = DeprecatedBase | DeprecatedScript | DeprecatedRegion,
        All = Deprecated | Legacy | Macro | SuppressScript,
    }

    internal static (Tag Tag, bool Changed) Canonicalize(CanonType c, Tag source)
    {
        Tag t = CopyTag(source);
        if (c == CanonType.Raw)
            return (t, false);
        bool changed = false;
        if ((c & CanonType.SuppressScript) != 0)
        {
            int suppressed = t.LangID < suppressScript.Length ? suppressScript[t.LangID] : 0;
            if (suppressed == t.ScriptID)
            {
                t.ScriptID = 0;
                changed = true;
            }
        }

        if ((c & (CanonType.DeprecatedBase | CanonType.Legacy | CanonType.Macro)) != 0)
        {
            while (true)
            {
                var(language, kind) = NormLang(t.LangID);
                if (language != t.LangID)
                {
                    switch (kind)
                    {
                        case 2: // internal/language.Legacy
                            if ((c & CanonType.Legacy) != 0)
                            {
                                if (t.LangID == 1031 && t.ScriptID == 0)
                                    t.ScriptID = 91;
                                t.LangID = language;
                                changed = true;
                            }

                            break;
                        case 1: // internal/language.Macro
                            if ((c & CanonType.Macro) != 0 && ((c & CanonType.CLDR) == 0 || t.LangID != 839))
                            {
                                changed = true;
                                t.LangID = language;
                            }

                            break;
                        case 0: // internal/language.Deprecated
                            if ((c & CanonType.DeprecatedBase) != 0)
                            {
                                if (t.LangID == 784 && t.RegionID == 0)
                                    t.RegionID = 189;
                                t.LangID = language;
                                changed = true;
                                continue;
                            }

                            break;
                    }
                }
                else if ((c & CanonType.Legacy) != 0 && t.LangID == 879 && (c & CanonType.CLDR) != 0)
                {
                    t.LangID = 839;
                    changed = true;
                }

                break;
            }
        }

        if ((c & CanonType.DeprecatedScript) != 0 && t.ScriptID == 157)
        {
            changed = true;
            t.ScriptID = 255;
        }

        if ((c & CanonType.DeprecatedRegion) != 0)
        {
            int region = CanonicalRegion(t.RegionID);
            if (region != t.RegionID)
            {
                changed = true;
                t.RegionID = region;
            }
        }

        return (t, changed);
    }
}

// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by the BSD-style license in licenses/XText-LICENSE.txt.
// Modified Default canonicalize selected port of x/text v0.42.0 language/language.go.
namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
    internal static bool CanonicalizeDefault(Tag t)
    {
        bool changed = false;
        while (true)
        {
            var(language, kind) = NormLang(t.LangID);
            if (language == t.LangID)
                break;
            if (kind == 2) // Legacy
            {
                if (LanguageString(t.LangID) == "sh" && t.ScriptID == 0)
                    t.ScriptID = Index(script, "Latn");
                t.LangID = language;
                changed = true;
            }
            else if (kind == 0) // DeprecatedBase; source may continue through another alias.
            {
                if (LanguageString(t.LangID) == "mo" && t.RegionID == 0)
                    t.RegionID = Index(regionISO, "MD") + isoRegionOffset;
                t.LangID = language;
                changed = true;
                continue;
            }

            // Macro and SuppressScript are not included in Default.
            break;
        }

        if (t.ScriptID == Index(script, "Qaai"))
        {
            t.ScriptID = Index(script, "Zinh");
            changed = true;
        }

        int region = CanonicalRegion(t.RegionID);
        if (region != t.RegionID)
        {
            t.RegionID = region;
            changed = true;
        }

        return changed;
    }
}

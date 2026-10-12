// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by licenses/XText-LICENSE.txt.
// Direct counterpart of x/text v0.42.0 language/match.go:524–735.
namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
    private sealed class BestMatch
    {
        internal HaveTag? have;
        internal Tag want = new();
        internal Confidence conf;
        private int pinnedRegion, regGroupDist;
        private bool pinLanguage, sameRegionGroup, origLang, origReg, paradigmReg, origScript;
        internal void Update(HaveTag candidate, Tag tag, int maxScript, int maxRegion, bool pin)
        {
            Confidence c = candidate.conf;
            if (c < conf)
                return;
            if (pinLanguage && tag.LangID != want.LangID)
                return;
            if (tag.LangID == want.LangID && sameRegionGroup)
            {
                var(_, same) = RegionGroupDistance(pinnedRegion, candidate.maxRegion, candidate.maxScript, want.LangID);
                if (!same)
                    return;
            }

            if (c == Confidence.Exact && candidate.maxScript == maxScript)
                pinLanguage = pin;
            if (EqualsRest(candidate.tag, tag))
            {
            }
            else if (candidate.maxScript != maxScript)
            {
                if (Confidence.Low < conf || candidate.altScript != maxScript)
                    return;
                c = Confidence.Low;
            }
            else if (candidate.maxRegion != maxRegion && Confidence.High < c)
                c = Confidence.High;
            bool beaten = false;
            if (c != conf)
            {
                if (c < conf)
                    return;
                beaten = true;
            }

            bool newOrigLang = candidate.tag.LangID == tag.LangID && tag.LangID != 0;
            if (!beaten && origLang != newOrigLang)
            {
                if (origLang)
                    return;
                beaten = true;
            }

            bool newOrigReg = candidate.tag.RegionID == tag.RegionID && tag.RegionID != 0;
            if (!beaten && origReg != newOrigReg)
            {
                if (origReg)
                    return;
                beaten = true;
            }

            var(distance, sameGroup) = RegionGroupDistance(candidate.maxRegion, maxRegion, maxScript, tag.LangID);
            if (!beaten && regGroupDist != distance)
            {
                if (distance > regGroupDist)
                    return;
                beaten = true;
            }

            bool newParadigm = IsParadigmLocale(tag.LangID, candidate.maxRegion);
            if (!beaten && paradigmReg != newParadigm)
            {
                if (!newParadigm)
                    return;
                beaten = true;
            }

            bool newOrigScript = candidate.tag.ScriptID == tag.ScriptID && tag.ScriptID != 0;
            if (!beaten && origScript != newOrigScript)
            {
                if (origScript)
                    return;
                beaten = true;
            }

            if (beaten)
            {
                have = candidate;
                want = CopyTag(tag);
                conf = c;
                pinnedRegion = maxRegion;
                sameRegionGroup = sameGroup;
                origLang = newOrigLang;
                origReg = newOrigReg;
                paradigmReg = newParadigm;
                origScript = newOrigScript;
                regGroupDist = distance;
            }
        }
    }

    internal static bool IsParadigmLocale(int language, int region)
    {
        foreach (int[] entry in MatcherData.Paradigms)
            if (entry[0] == language && (entry[1] == region || entry[2] == region))
                return true;
        return false;
    }

    internal static (int Distance, bool Same) RegionGroupDistance(int a, int b, int script, int language)
    {
        const int defaultDistance = 4;
        uint aGroup = (uint)regionToGroups[a] << 1;
        uint bGroup = (uint)regionToGroups[b] << 1;
        foreach (var entry in matchRegion)
        {
            if (entry.lang != language || (entry.script != 0 && entry.script != script))
                continue;
            uint group = 1u << (entry.group & ~0x80);
            if ((entry.group & 0x80) == 0)
            {
                if ((aGroup & bGroup & group) != 0)
                    return (entry.distance, entry.distance == defaultDistance);
            }
            else if (((aGroup | bGroup) & group) == 0)
                return (entry.distance, entry.distance == defaultDistance);
        }

        return (defaultDistance, true);
    }

    private static bool EqualsRest(Tag a, Tag b) => a.ScriptID == b.ScriptID && a.RegionID == b.RegionID && VariantOrPrivateUseTags(a) == VariantOrPrivateUseTags(b);
    private static bool IsExactEquivalent(int language) => !MatcherData.NotEquivalent.Contains(language);
    // Lazy CLR initialization preserves Go's package init dependency order without
    // eagerly creating matching metadata during ordinary compiler invocations.
    private static class MatcherData
    {
        internal static readonly List<int> NotEquivalent = [];
        internal static readonly int[][] Paradigms;
        static MatcherData()
        {
            foreach (var alias in AliasMap)
            {
                Tag tag = Canonicalize(CanonType.All, new Tag { LangID = alias.From }).Tag;
                if (tag.ScriptID != 0 || tag.RegionID != 0)
                    NotEquivalent.Add(alias.From);
            }

            Paradigms = paradigmLocales;
            for (int i = 0; i < paradigmLocales.Length; i++)
            {
                int[] entry = Paradigms[i];
                Tag max = AddTags(new Tag { LangID = entry[0] }).Tag;
                if (entry[1] == 0)
                    entry[1] = max.RegionID;
                if (entry[2] == 0)
                    entry[2] = max.RegionID;
            }
        }
    }
}

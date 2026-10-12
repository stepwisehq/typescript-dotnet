// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by licenses/XText-LICENSE.txt.
// Direct counterpart of x/text v0.42.0 language/match.go.
namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
    internal enum Confidence
    {
        No,
        Low,
        High,
        Exact
    }

    private sealed class HaveTag
    {
        internal Tag tag = new();
        internal int index, maxRegion, maxScript, altScript;
        internal Confidence conf;
        internal ushort nextMax;
        internal HaveTag Copy() => new()
        {
            tag = CopyTag(tag),
            index = index,
            maxRegion = maxRegion,
            maxScript = maxScript,
            altScript = altScript,
            conf = conf,
            nextMax = nextMax
        };
    }

    private sealed class MatchHeader
    {
        internal readonly List<HaveTag> haveTags = [];
        internal bool original;
        internal void AddIfNew(HaveTag n, bool exact)
        {
            original |= exact;
            foreach (HaveTag v in haveTags)
                if (EqualsRest(v.tag, n.tag))
                    return;
            for (int i = 0; i < haveTags.Count; i++)
            {
                HaveTag v = haveTags[i];
                if (v.maxScript == n.maxScript && v.maxRegion == n.maxRegion && VariantOrPrivateUseTags(v.tag) == VariantOrPrivateUseTags(n.tag))
                {
                    while (haveTags[i].nextMax != 0)
                        i = haveTags[i].nextMax;
                    haveTags[i].nextMax = unchecked((ushort)haveTags.Count);
                    break;
                }
            }

            haveTags.Add(n.Copy());
        }
    }

    private static (HaveTag Have, int Language) MakeHaveTag(Tag tag, int index)
    {
        Tag max = CopyTag(tag);
        if (tag.LangID != 0 || tag.RegionID != 0 || tag.ScriptID != 0)
        {
            max = Canonicalize(CanonType.All, max).Tag;
            max = AddTags(max).Tag;
            max.RemakeString();
        }

        return (new HaveTag { tag = CopyTag(tag), index = index, conf = Confidence.Exact, maxRegion = max.RegionID, maxScript = max.ScriptID, altScript = AltScript(max.LangID, max.ScriptID) }, max.LangID);
    }

    private static int AltScript(int language, int script)
    {
        foreach (var alt in matchScript)
            if ((alt.wantLang == language || alt.haveLang == language) && alt.haveScript == script)
                return alt.wantScript;
        return 0;
    }

    private static Confidence ToConfidence(int distance) => distance <= 10 ? Confidence.High : distance < 30 ? Confidence.Low : Confidence.No;
    internal sealed class Matcher
    {
        private HaveTag default_ = new();
        private readonly List<HaveTag> supported = [];
        private readonly Dictionary<int, MatchHeader> index = [];
        private readonly bool preferSameScript;
        internal Matcher(IReadOnlyList<Tag> tags, bool preferSameScript = true)
        {
            this.preferSameScript = preferSameScript;
            if (tags.Count == 0)
                return;
            for (int i = 0; i < tags.Count; i++)
            {
                HaveTag pair = MakeHaveTag(tags[i], i).Have;
                Header(tags[i].LangID).AddIfNew(pair, true);
                supported.Add(pair);
            }

            default_ = Header(tags[0].LangID).haveTags[0];
            for (int i = 0; i < tags.Count; i++)
            {
                var(pair, max) = MakeHaveTag(tags[i], i);
                if (max != tags[i].LangID)
                    Header(max).AddIfNew(pair, true);
            }

            foreach (var match in matchLang)
            {
                UpdateIndex(match.want, match.have, ToConfidence(match.distance));
                if (!match.oneway)
                    UpdateIndex(match.have, match.want, ToConfidence(match.distance));
            }

            for (int i = 0; i < AliasMap.Length; i++)
            {
                var alias = AliasMap[i];
                Confidence conf = Confidence.Exact;
                if (AliasTypes[i] != 1)
                {
                    if (!IsExactEquivalent(alias.From))
                        conf = Confidence.High;
                    UpdateIndex(alias.To, alias.From, conf);
                }

                UpdateIndex(alias.From, alias.To, conf);
            }
        }

        private MatchHeader Header(int language)
        {
            if (index.TryGetValue(language, out var header))
                return header;
            header = new MatchHeader();
            index.Add(language, header);
            return header;
        }

        private void UpdateIndex(int want, int have, Confidence confidence)
        {
            if (!index.TryGetValue(have, out var hh) || !hh.original)
                return;
            MatchHeader hw = Header(want);
            // Go ranges the original slice length even if the destination aliases
            // the same header. AddIfNew does not append equivalent existing entries.
            int count = hh.haveTags.Count;
            for (int i = 0; i < count; i++)
            {
                HaveTag v = hh.haveTags[i].Copy();
                if (confidence < v.conf)
                    v.conf = confidence;
                v.nextMax = 0;
                if (v.altScript != 0)
                    v.altScript = AltScript(want, v.maxScript);
                hw.AddIfNew(v, confidence == Confidence.Exact && hh.original);
            }
        }

        private (HaveTag? Have, Tag Want, Confidence Confidence) GetBest(IReadOnlyList<Tag> want)
        {
            BestMatch best = new();
            for (int i = 0; i < want.Count; i++)
            {
                Tag w = CopyTag(want[i]);
                Tag max;
                index.TryGetValue(w.LangID, out MatchHeader? h);
                if (w.LangID != 0)
                {
                    if (h == null)
                        continue;
                    max = Canonicalize(CanonType.Legacy | CanonType.Deprecated | CanonType.Macro, w).Tag;
                    if (w.RegionID != max.RegionID)
                        w.RegionID = max.RegionID;
                    max = AddTags(max).Tag;
                }
                else
                {
                    if (h != null)
                        foreach (HaveTag have in h.haveTags)
                            if (EqualsRest(have.tag, w))
                                return (have, w, Confidence.Exact);
                    if (w.ScriptID == 0 && w.RegionID == 0)
                        continue;
                    max = AddTags(w).Tag;
                    if (!index.TryGetValue(max.LangID, out h))
                        continue;
                }

                bool pin = true;
                for (int j = i + 1; j < want.Count; j++)
                    if (w.LangID == want[j].LangID)
                    {
                        pin = false;
                        break;
                    }

                foreach (HaveTag entry in h.haveTags)
                {
                    HaveTag have = entry;
                    best.Update(have, w, max.ScriptID, max.RegionID, pin);
                    if (best.conf == Confidence.Exact)
                    {
                        while (have.nextMax != 0)
                        {
                            have = h.haveTags[have.nextMax];
                            best.Update(have, w, max.ScriptID, max.RegionID, pin);
                        }

                        return (best.have, best.want, best.conf);
                    }
                }
            }

            if (best.conf <= Confidence.No)
                return (null, want.Count > 0 ? CopyTag(want[0]) : new Tag(), Confidence.No);
            return (best.have, best.want, best.conf);
        }

        internal (Tag Tag, int Index, Confidence Confidence) Match(params Tag[] want)
        {
            var(match, w, confidence) = GetBest(want);
            Tag tt;
            int resultIndex = 0;
            if (match != null)
            {
                tt = CopyTag(match.tag);
                resultIndex = match.index;
            }
            else
            {
                tt = CopyTag(default_.tag);
                if (preferSameScript)
                {
                    bool found = false;
                    foreach (Tag desired in want)
                    {
                        int script = ScriptOf(desired);
                        if (script == 0)
                            continue;
                        for (int i = 0; i < supported.Count; i++)
                            if (script == supported[i].maxScript)
                            {
                                tt = CopyTag(supported[i].tag);
                                resultIndex = i;
                                found = true;
                                break;
                            }

                        if (found)
                            break;
                    }
                }
            }

            if (w.RegionID != tt.RegionID && w.RegionID != 0)
            {
                if (tt.RegionID != 0 && RegionContains(tt.RegionID, w.RegionID))
                {
                    tt.RegionID = w.RegionID;
                    tt.RemakeString();
                }
                else
                {
                    string region = RegionString(w.RegionID);
                    if (region.Length == 2)
                        tt = SetTypeForKey(tt, "rg", region.ToLowerInvariant() + "zzzz").Tag;
                }
            }

            var extensions = Extensions(w);
            if (extensions.Count > 0)
            {
                TagBuilder builder = new();
                builder.SetTag(tt);
                foreach (string extension in extensions)
                    builder.AddExt(extension);
                tt = builder.Make();
            }

            return (tt, resultIndex, confidence);
        }
    }
}

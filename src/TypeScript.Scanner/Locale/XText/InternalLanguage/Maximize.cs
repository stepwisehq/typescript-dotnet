// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by licenses/XText-LICENSE.txt.
// Direct counterpart of x/text v0.42.0 internal/language/match.go.
namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
    private const int IsList = 1, ScriptInFrom = 2, RegionInFrom = 4;
    private static readonly Exception ErrMissingLikelyTagsData = new InvalidOperationException("missing likely tags data");
    // Go Tag is a copied value. The managed parser retains reference owners, so
    // each value-taking matching worker explicitly copies the complete header.
    private static Tag CopyTag(Tag t) => new()
    {
        LangID = t.LangID,
        ScriptID = t.ScriptID,
        RegionID = t.RegionID,
        pVariant = t.pVariant,
        pExt = t.pExt,
        str = t.str,
    };
    private static bool EqualTags(Tag a, Tag b) => a.LangID == b.LangID && a.ScriptID == b.ScriptID && a.RegionID == b.RegionID;
    private static bool IsPrivateUse(Tag t) => t.LangID == 0 && t.ScriptID == 0 && t.RegionID == 0 && t.str.StartsWith("x-", StringComparison.Ordinal);
    private static void SetUndefinedLang(Tag t, int id)
    {
        if (t.LangID == 0)
            t.LangID = id;
    }

    private static void SetUndefinedScript(Tag t, int id)
    {
        if (t.ScriptID == 0)
            t.ScriptID = id;
    }

    private static void SetUndefinedRegion(Tag t, int id)
    {
        if (t.RegionID == 0 || RegionContains(t.RegionID, id))
            t.RegionID = id;
    }

    internal static bool RegionContains(int region, int contained)
    {
        if (region == contained)
            return true;
        int group = regionInclusion[region];
        if (group >= nRegionGroups)
            return false;
        ulong mask = regionContainment[group];
        int child = regionInclusion[contained];
        ulong bits = regionInclusionBits[child];
        return child >= nRegionGroups ? (bits & mask) != 0 : (bits & ~mask) == 0;
    }

    private static bool SpecializeRegion(Tag t)
    {
        int i = regionInclusion[t.RegionID];
        if (i >= nRegionGroups)
            return false;
        var x = likelyRegionGroup[i];
        if (x.lang == t.LangID && x.script == t.ScriptID)
            t.RegionID = x.region;
        return true;
    }

    internal static (Tag Tag, Exception? Error) AddLikelySubtags(Tag source)
    {
        var(id, error) = AddTags(source);
        if (error != null || EqualTags(id, source))
            return (CopyTag(source), error);
        id.RemakeString();
        return (id, null);
    }

    private static (Tag Tag, Exception? Error) Maximize(Tag source) => AddTags(source);
    internal static (Tag Tag, Exception? Error) Minimize(Tag source)
    {
        var(tag, error) = MinimizeTags(source);
        if (error != null)
            return (tag, error);
        tag.RemakeString();
        return (tag, null);
    }

    private static (Tag Tag, Exception? Error) AddTags(Tag source)
    {
        Tag t = CopyTag(source);
        if (IsPrivateUse(t))
            return (t, null);
        if (t.ScriptID != 0 && t.RegionID != 0)
        {
            if (t.LangID != 0)
            {
                SpecializeRegion(t);
                return (t, null);
            }

            var x = likelyRegion[t.RegionID];
            ReadOnlySpan<likelyLangScript> list = likelyRegion.AsSpan(t.RegionID, 1);
            if ((x.flags & IsList) != 0)
                list = likelyRegionList.AsSpan(x.lang, x.script);
            foreach (var entry in list)
                if (entry.script == t.ScriptID)
                {
                    SetUndefinedLang(t, entry.lang);
                    return (t, null);
                }
        }

        if (t.LangID != 0)
        {
            if (t.LangID < langNoIndexOffset)
            {
                var x = likelyLang[t.LangID];
                if ((x.flags & IsList) != 0)
                {
                    var list = likelyLangList.AsSpan(x.region, x.script);
                    if (t.ScriptID != 0)
                    {
                        foreach (var entry in list)
                            if (entry.script == t.ScriptID && (entry.flags & ScriptInFrom) != 0)
                            {
                                SetUndefinedRegion(t, entry.region);
                                return (t, null);
                            }
                    }
                    else if (t.RegionID != 0)
                    {
                        int count = 0;
                        bool goodScript = true;
                        Tag tt = CopyTag(t);
                        foreach (var entry in list)
                            if ((entry.flags & ScriptInFrom) == 0 && RegionContains(t.RegionID, entry.region))
                            {
                                tt.RegionID = entry.region;
                                SetUndefinedScript(tt, entry.script);
                                goodScript = goodScript && tt.ScriptID == entry.script;
                                count++;
                            }

                        if (count == 1)
                            return (tt, null);
                        if (goodScript)
                            t.ScriptID = tt.ScriptID;
                    }
                }
            }
        }
        else
        {
            if (t.ScriptID != 0)
            {
                var x = likelyScript[t.ScriptID];
                if (x.region != 0)
                {
                    SetUndefinedRegion(t, x.region);
                    SetUndefinedLang(t, x.lang);
                    return (t, null);
                }
            }

            if (t.RegionID != 0)
            {
                int i = regionInclusion[t.RegionID];
                if (i < nRegionGroups)
                {
                    var x = likelyRegionGroup[i];
                    if (x.region != 0)
                    {
                        SetUndefinedLang(t, x.lang);
                        SetUndefinedScript(t, x.script);
                        t.RegionID = x.region;
                    }
                }
                else
                {
                    var x = likelyRegion[t.RegionID];
                    if ((x.flags & IsList) != 0)
                        x = likelyRegionList[x.lang];
                    if (x.script != 0 && x.flags != ScriptInFrom)
                    {
                        SetUndefinedLang(t, x.lang);
                        SetUndefinedScript(t, x.script);
                        return (t, null);
                    }
                }
            }
        }

        if (t.LangID < langNoIndexOffset)
        {
            var x = likelyLang[t.LangID];
            if ((x.flags & IsList) != 0)
                x = likelyLangList[x.region];
            if (x.region != 0)
            {
                SetUndefinedScript(t, x.script);
                SetUndefinedRegion(t, x.region);
            }

            SpecializeRegion(t);
            if (t.LangID == 0)
                t.LangID = 313; // x/text generated _en.
            return (t, null);
        }

        return (t, ErrMissingLikelyTagsData);
    }

    private static void SetTagsFrom(Tag t, Tag id)
    {
        t.LangID = id.LangID;
        t.ScriptID = id.ScriptID;
        t.RegionID = id.RegionID;
    }

    private static (Tag Tag, Exception? Error) MinimizeTags(Tag source)
    {
        Tag t = CopyTag(source);
        if (EqualTags(t, new Tag()))
            return (t, null);
        var(max, error) = AddTags(t);
        if (error != null)
            return (t, error);
        Tag[] candidates = [new()
        {
            LangID = t.LangID
        }, new()
        {
            LangID = t.LangID,
            RegionID = t.RegionID
        }, new()
        {
            LangID = t.LangID,
            ScriptID = t.ScriptID
        }

        ];
        foreach (Tag id in candidates)
        {
            var(x, candidateError) = AddTags(id);
            if (candidateError == null && EqualTags(max, x))
            {
                SetTagsFrom(t, id);
                break;
            }
        }

        return (t, null);
    }
}

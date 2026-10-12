// Copyright 2013, 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by the BSD-style license in licenses/XText-LICENSE.txt.
// Parse/String-reachable port of x/text v0.42.0 internal/language/compact/{language,compact}.go.
// Compact IDs/private parent matching omitted: when FromTag is inexact source retains full Tag.
// Exact core/special registries and rg key reconstruction are preserved for String semantics.
namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
    internal static string CompactString(Tag tag)
    {
        string region = TypeForKey(tag, "rg");
        if (region.Length == 6 && region[2..] == "zzzz")
        {
            var(id, error) = GetRegionID(region[..2].ToCharArray());
            if (error == null)
            {
                Tag stripped = RemoveTypeForKey(tag, "rg");
                bool exact1 = FromTagExact(stripped);
                Tag regional = new()
                {
                    LangID = stripped.LangID,
                    ScriptID = stripped.ScriptID,
                    RegionID = id,
                    str = stripped.str,
                    pExt = stripped.pExt,
                    pVariant = stripped.pVariant
                };
                bool exact2 = FromTagExact(regional);
                if (exact1 && exact2)
                {
                    // Source Make/Tag emits the stripped language tag when both compact IDs equal.
                    if (stripped.RegionID == regional.RegionID)
                        return stripped.String();
                    // The only value normalized by reconstruction is the two-letter region case.
                    return ReplaceTypeForKey(stripped, "rg", RegionString(id).ToLowerInvariant() + "zzzz");
                }
            }
        }

        return tag.String();
    }

    private static bool FromTagExact(Tag t)
    {
        if (t.str.Length > 0)
        {
            // The compact special registry contains exactly these two tags in v0.42.0.
            if (t.String() == "ca-ES-valencia" && t.CoreString() == "ca-ES")
                return true;
            if (t.String() == "en-US-u-va-posix" && t.CoreString() == "en-US")
                return true;
            return false;
        }

        if (t.LangID > langNoIndexOffset)
            return false;
        uint key = ((uint)t.LangID << 20) | ((uint)t.ScriptID << 12) | (uint)t.RegionID;
        return Array.BinarySearch(coreTags, key) >= 0;
    }

    internal static int NextExtension(string s, int p)
    {
        for (int n = s.Length - 3; p < n;)
        {
            if (s[p] == '-')
            {
                if (s[p + 2] == '-')
                    return p;
                p += 3;
            }
            else
                p++;
        }

        return s.Length;
    }

    private static (int Start, int Separator, int End, bool HasExtension) FindTypeForKey(Tag t, string key)
    {
        int p = t.pExt;
        if (key.Length != 2 || p == t.str.Length || p == 0)
            return (p, p, p, false);
        string s = t.str;
        for (p++; s[p] != 'u'; p++)
        {
            if (s[p] > 'u')
            {
                p--;
                return (p, p, p, false);
            }

            p = NextExtension(s, p);
            if (p == s.Length)
                return (p, p, p, false);
        }

        p++;
        string currentKey = "";
        int start = 0, sep = 0;
        while (true)
        {
            int end = p;
            for (p++; p < s.Length && s[p] != '-'; p++)
            {
            }

            int n = p - end - 1;
            if (n <= 2 && currentKey == key)
            {
                if (sep < end)
                    sep++;
                return (start, sep, end, true);
            }

            if (n <= 1)
                return (end, end, end, true);
            if (n == 2)
            {
                currentKey = s[(end + 1)..p];
                if (string.CompareOrdinal(currentKey, key) > 0)
                    return (end, end, end, true);
                start = end;
                sep = p;
            }
        }
    }

    internal static string TypeForKey(Tag t, string key)
    {
        var(_, start, end, _) = FindTypeForKey(t, key);
        if (end == start)
            return "";
        string text = t.str[start..end];
        int dash = text.IndexOf('-');
        return dash < 0 ? text : text[..dash];
    }

    private static Tag RemoveTypeForKey(Tag t, string key)
    {
        Tag result = new()
        {
            LangID = t.LangID,
            ScriptID = t.ScriptID,
            RegionID = t.RegionID,
            str = t.str,
            pVariant = t.pVariant,
            pExt = t.pExt
        };
        var(start, sep, end, _) = FindTypeForKey(t, key);
        if (start != sep)
        {
            if (t.str[start - 2] == '-' && (end == t.str.Length || (end + 2 < t.str.Length && t.str[end + 2] == '-')))
                start -= 2;
            if (start == t.pVariant && end == t.str.Length)
            {
                result.str = "";
                result.pVariant = 0;
                result.pExt = 0;
            }
            else
                result.str = t.str[..start] + t.str[end..];
        }

        return result;
    }

    private static string ReplaceTypeForKey(Tag t, string key, string value)
    {
        if (t.str.Length == 0)
            return t.CoreString() + "-u-" + key + "-" + value;
        var(_, sep, end, hasExt) = FindTypeForKey(t, key);
        return t.str[..sep] + "-" + (hasExt ? "" : "u-") + key + "-" + value + t.str[end..];
    }
}

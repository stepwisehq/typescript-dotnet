// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by the BSD-style license in licenses/XText-LICENSE.txt.
// Modified Parse-reachable port of x/text v0.42.0 internal/language/lookup.go.
// Index/Next/FixCase retain internal/tag/tag.go byte semantics; table IDs remain original.
namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
    internal static bool FixCase(string form, char[] b)
    {
        if (form.Length != b.Length)
            return false;
        for (int i = 0; i < b.Length; i++)
        {
            int c = b[i];
            if (form[i] <= 'Z')
            {
                if (c >= 'a')
                    c -= 'z' - 'Z';
                if (c < 'A' || c > 'Z')
                    return false;
            }
            else
            {
                if (c <= 'Z')
                    c += 'z' - 'Z';
                if (c < 'a' || c > 'z')
                    return false;
            }

            b[i] = (char)c;
        }

        return true;
    }

    internal static int Index(string table, string key)
    {
        int low = 0, high = table.Length / 4;
        while (low < high)
        {
            int mid = low + (high - low) / 2;
            if (string.CompareOrdinal(table, mid * 4, key, 0, key.Length) < 0)
                low = mid + 1;
            else
                high = mid;
        }

        return low * 4 + key.Length <= table.Length && string.CompareOrdinal(table, low * 4, key, 0, key.Length) == 0 ? low : -1;
    }

    private static int Next(string table, string key, int index) => ++index * 4 + key.Length <= table.Length && string.CompareOrdinal(table, index * 4, key, 0, key.Length) == 0 ? index : -1;
    internal static (int ID, Exception? Error) FindIndex(string table, char[] key, string form)
    {
        if (!FixCase(form, key))
            return (0, ErrSyntax);
        int i = Index(table, new string (key));
        return i < 0 ? (0, NewValueError(key)) : (i, null);
    }

    internal static (int ID, Exception? Error) GetLangID(char[] s) => s.Length == 2 ? GetLangISO2(s) : GetLangISO3(s);
    internal static (int ID, Exception? Error) GetLangISO2(char[] s)
    {
        if (!FixCase("zz", s))
            return (0, ErrSyntax);
        int i = Index(lang, new string (s));
        return i >= 0 && lang[i * 4 + 3] != 0 ? (i, null) : (0, NewValueError(s));
    }

    internal static (int ID, Exception? Error) GetLangISO3(char[] s)
    {
        if (!FixCase("und", s))
            return (0, ErrSyntax);
        string text = new(s), prefix = text[..2];
        for (int i = Index(lang, prefix); i >= 0; i = Next(lang, prefix, i))
            if (lang[i * 4 + 3] == 0 && lang[i * 4 + 2] == s[2])
                return (i == nonCanonicalUnd ? 0 : i, null);
        int alternative = Index(altLangISO3, text);
        if (alternative >= 0)
            return (altLangIndex[altLangISO3[alternative * 4 + 3]], null);
        int n = 0;
        foreach (char c in s)
            n = n * 26 + c - 'a';
        if ((langNoIndex[n / 8] & (1 << (n % 8))) != 0)
            return (n + langNoIndexOffset, null);
        prefix = text[..1];
        for (int i = Index(lang, prefix); i >= 0; i = Next(lang, prefix, i))
            if (lang[i * 4 + 2] == s[1] && lang[i * 4 + 3] == s[2])
                return (i, null);
        return (0, NewValueError(s));
    }

    internal static string LanguageString(int id)
    {
        if (id == 0)
            return "und";
        if (id >= langNoIndexOffset)
        {
            int n = id - langNoIndexOffset;
            char[] s = new char[3];
            for (int i = 2; i >= 0; i--)
            {
                s[i] = (char)('a' + n % 26);
                n /= 26;
            }

            return new(s);
        }

        return lang.Substring(id * 4, lang[id * 4 + 3] == 0 ? 3 : 2);
    }

    internal static (int ID, Exception? Error) GetScriptID(char[] s) => FindIndex(script, s, "Zzzz");
    internal static (int ID, Exception? Error) GetRegionID(char[] s)
    {
        if (s.Length == 3)
        {
            if (s[0] > '9')
                return GetRegionISO3(s);
            if (s.All(c => c is >= '0' and <= '9'))
                return GetRegionM49(int.Parse(new string (s), System.Globalization.CultureInfo.InvariantCulture));
        }

        var(i, error) = FindIndex(regionISO, s, "ZZ");
        return error == null ? (i + isoRegionOffset, null) : (0, error);
    }

    private static (int ID, Exception? Error) GetRegionISO3(char[] s)
    {
        if (!FixCase("ZZZ", s))
            return (0, ErrSyntax);
        string prefix = new(s, 0, 1);
        for (int i = Index(regionISO, prefix); i >= 0; i = Next(regionISO, prefix, i))
            if (regionISO[i * 4 + 2] == s[1] && regionISO[i * 4 + 3] == s[2])
                return (i + isoRegionOffset, null);
        for (int i = 0; i < altRegionISO3.Length; i += 3)
            if (altRegionISO3.AsSpan(i, 3).SequenceEqual(s))
                return (altRegionIDs[i / 3], null);
        return (0, NewValueError(s));
    }

    internal static (int ID, Exception? Error) GetRegionM49(int n)
    {
        if (n is> 0 and <= 999)
        {
            int index = n >> 7, low = m49Index[index], high = m49Index[index + 1];
            int value = unchecked((ushort)(n << 9));
            while (low < high)
            {
                int mid = low + (high - low) / 2;
                if (fromM49[mid] < value)
                    low = mid + 1;
                else
                    high = mid;
            }

            if (low < fromM49.Length && (fromM49[low] & ~511) == value)
                return (fromM49[low] & 511, null);
        }

        // Source fmt.Fprint appends after a pre-sized eight-zero-byte buffer;
        // the returned ValueError therefore has an empty subtag, not the number.
        return (0, NewValueError([]));
    }

    internal static string RegionString(int region) => region < isoRegionOffset ? (region == 0 ? "ZZ" : m49[region].ToString("D3", System.Globalization.CultureInfo.InvariantCulture)) : regionISO.Substring((region - isoRegionOffset) * 4, 2);
    internal static (int ID, int Kind) NormLang(int id)
    {
        for (int i = 0; i < AliasMap.Length; i++)
            if (AliasMap[i].From == id)
                return (AliasMap[i].To, AliasTypes[i]);
        return (id, -1);
    }

    private static readonly Dictionary<string, string> grandfatheredMap = new(StringComparer.Ordinal)
    {
        ["art-lojban"] = "jbo",
        ["i-ami"] = "ami",
        ["i-bnn"] = "bnn",
        ["i-hak"] = "hak",
        ["i-klingon"] = "tlh",
        ["i-lux"] = "lb",
        ["i-navajo"] = "nv",
        ["i-pwn"] = "pwn",
        ["i-tao"] = "tao",
        ["i-tay"] = "tay",
        ["i-tsu"] = "tsu",
        ["no-bok"] = "nb",
        ["no-nyn"] = "nn",
        ["sgn-be-fr"] = "sfb",
        ["sgn-be-nl"] = "vgt",
        ["sgn-ch-de"] = "sgg",
        ["zh-guoyu"] = "cmn",
        ["zh-hakka"] = "hak",
        ["zh-min-nan"] = "nan",
        ["zh-xiang"] = "hsn",
        ["cel-gaulish"] = "xtg-x-cel-gaulish",
        ["en-gb-oed"] = "en-GB-oxendict",
        ["i-default"] = "en-x-i-default",
        ["i-enochian"] = "und-x-i-enochian",
        ["i-mingo"] = "see-x-i-mingo",
        ["zh-min"] = "nan-x-zh-min",
        ["root"] = "und",
        ["en-us-posix"] = "en-US-u-va-posix"
    };
}

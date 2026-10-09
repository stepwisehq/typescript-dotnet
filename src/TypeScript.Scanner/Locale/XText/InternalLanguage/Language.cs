// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by the BSD-style license in licenses/XText-LICENSE.txt.
// Modified selected port of x/text v0.42.0 internal/language/language.go.
namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
    internal sealed class Tag
    {
        internal int LangID, RegionID, ScriptID;
        internal byte pVariant;
        internal ushort pExt;
        internal string str = "";
        internal string CoreString() => LanguageString(LangID) + (ScriptID == 0 ? "" : "-" + script.Substring(ScriptID * 4, 4)) + (RegionID == 0 ? "" : "-" + RegionString(RegionID));
        internal string String() => str.Length == 0 ? CoreString() : str;
        internal void RemakeString()
        {
            if (str.Length == 0)
                return;
            string extra = str[pVariant..];
            if (pVariant > 0)
                extra = extra[1..];
            if (LangID == 0 && ScriptID == 0 && RegionID == 0 && extra.StartsWith("x-", StringComparison.Ordinal))
            {
                str = extra;
                pVariant = 0;
                pExt = 0;
                return;
            }

            string core = CoreString();
            if (extra.Length > 0)
            {
                int diff = core.Length - pVariant;
                str = core + "-" + extra;
                pVariant = unchecked((byte)(pVariant + diff));
                pExt = unchecked((ushort)(pExt + diff));
            }
            else
            {
                str = core;
                pVariant = unchecked((byte)core.Length);
                pExt = unchecked((ushort)core.Length);
            }
        }
    }

    internal static int CanonicalRegion(int region)
    {
        foreach (var(from, to)in regionOldMap)
            if (from == region)
                return to;
        return region;
    }
}

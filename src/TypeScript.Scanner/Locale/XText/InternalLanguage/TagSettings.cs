// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by licenses/XText-LICENSE.txt.
// Matching closure from internal/language/language.go and language/language.go.
namespace TypeScript.Locale.XText;
internal static partial class LanguageParser
{
    internal static int ScriptOf(Tag t) => ScriptWithConfidence(t).Script;
    // Direct language/language.go Tag.Script owner; matcher consumes only Script.
    internal static (int Script, Confidence Confidence) ScriptWithConfidence(Tag t)
    {
        if (t.ScriptID != 0)
            return (t.ScriptID, Confidence.Exact);
        int script = 261; // generated _Zzzz
        Confidence confidence = Confidence.No;
        int suppressed = t.LangID < suppressScript.Length ? suppressScript[t.LangID] : 0;
        if (suppressed != 0)
        {
            if (t.RegionID == 0)
                return (suppressed, Confidence.High);
            script = suppressed;
            confidence = Confidence.High;
        }

        var(max, error) = AddTags(t);
        if (error == null)
        {
            if (max.ScriptID != script)
            {
                script = max.ScriptID;
                confidence = Confidence.Low;
            }
        }
        else
        {
            Tag canonical = Canonicalize(CanonType.Deprecated | CanonType.Macro, t).Tag;
            (max, error) = AddTags(canonical);
            if (error == null && max.ScriptID != script)
            {
                script = max.ScriptID;
                confidence = Confidence.Low;
            }
        }

        return (script, confidence);
    }

    private static readonly Exception ErrPrivateUse = new InvalidOperationException("cannot set a key on a private use tag");
    private static readonly Exception ErrInvalidArguments = new InvalidOperationException("invalid key or type");
    internal static (Tag Tag, Exception? Error) SetTypeForKey(Tag source, string key, string value)
    {
        Tag t = CopyTag(source);
        if (IsPrivateUse(t))
            return (t, ErrPrivateUse);
        if (key.Length != 2)
            return (t, ErrInvalidArguments);
        if (value == "")
        {
            var(start, separator, end, _) = FindTypeForKey(t, key);
            if (start != separator)
            {
                if (t.str[start - 2] != '-')
                {
                }
                else if (end == t.str.Length || (end + 2 < t.str.Length && t.str[end + 2] == '-'))
                    start -= 2;
                if (start == t.pVariant && end == t.str.Length)
                {
                    t.str = "";
                    t.pVariant = 0;
                    t.pExt = 0;
                }
                else
                    t.str = t.str[..start] + t.str[end..];
            }

            return (t, null);
        }

        if (value.Length < 3 || value.Length > 8)
            return (t, ErrInvalidArguments);
        string pair = "u-" + key + "-" + value;
        Scanner scanner = new(pair);
        ParseExtensions(scanner);
        if (scanner.err != null)
            return (t, scanner.err);
        // The scanner canonicalizes the same temporary ASCII byte owner in Go.
        pair = scanner.b;
        if (t.str == "")
        {
            string core = t.CoreString();
            t.pVariant = unchecked((byte)core.Length);
            t.pExt = unchecked((ushort)core.Length);
            t.str = core + "-" + pair;
        }
        else
        {
            string s = t.str;
            var(start, separator, end, hasExtension) = FindTypeForKey(t, key);
            if (start == separator)
            {
                if (hasExtension)
                    pair = pair[2..];
                t.str = s[..separator] + "-" + pair + s[end..];
            }
            else
                t.str = s[..(start + 3)] + "-" + value + s[end..];
        }

        return (t, null);
    }
}

// Copyright 2013, 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by licenses/XText-LICENSE.txt.
// Original x/text matching/likely/builder assertions; no alternate expected oracle.
using TypeScript.Locale.XText;

namespace LanguageServices.Checks;
public static partial class LocaleMatcherChecks
{
    private readonly record struct ParseParts(string Input, string Language, string Script, string Region, string Variants, string Extensions);
    private readonly record struct ComplianceCase(string Name, string Supported, string Desired, string Expected, string Combined);
    public static string[] Names => ["TestAddLikelySubtags", "TestMinimize", "TestBuilder", "TestSetTag", "TestCompliance", "TestRegionGroups", "TestIsParadigmLocale", "TestCanonicalize", "TestSetTypeForKey", "TestContains", "TestScript"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestAddLikelySubtags":
                TestAddLikelySubtags();
                break;
            case "TestMinimize":
                TestMinimize();
                break;
            case "TestBuilder":
                TestBuilder(false);
                break;
            case "TestSetTag":
                TestBuilder(true);
                break;
            case "TestCompliance":
                TestCompliance();
                break;
            case "TestRegionGroups":
                TestRegionGroups();
                break;
            case "TestIsParadigmLocale":
                TestIsParadigmLocale();
                break;
            case "TestCanonicalize":
                TestCanonicalize();
                break;
            case "TestSetTypeForKey":
                TestSetTypeForKey();
                break;
            case "TestContains":
                TestContains();
                break;
            case "TestScript":
                TestScript();
                break;
            default:
                throw new ArgumentException("Unknown original x/text locale assertion", nameof(name));
        }
    }

    public static int RunAll()
    {
        foreach (string name in Names)
            Run(name);
        return Names.Length;
    }

    private static void Equal<T>(T actual, T expected, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
            throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual);
    }

    private static LanguageParser.Tag Parse(string text) => LanguageParser.Parse(text).Tag;
    private static void EqualTag(LanguageParser.Tag a, LanguageParser.Tag b, string label)
    {
        Equal(a.LangID, b.LangID, label + " language");
        Equal(a.ScriptID, b.ScriptID, label + " script");
        Equal(a.RegionID, b.RegionID, label + " region");
        Equal(a.pVariant, b.pVariant, label + " variant offset");
        Equal(a.pExt, b.pExt, label + " extension offset");
        Equal(a.str, b.str, label + " retained bytes");
    }

    private static void TestSetTypeForKey()
    {
        foreach (var item in SetTypeCases)
        {
            LanguageParser.Tag tag = Parse(item.Input);
            var(value, error) = LanguageParser.SetTypeForKey(tag, item.Key, item.Value);
            Equal(value.String(), item.Expected, item.Input + " set key");
            Equal(error != null, item.Error, item.Input + " set key error");
            if (error == null)
                Equal(LanguageParser.TypeForKey(value, item.Key), item.Value, item.Input + " resulting type");
            if (tag.String().Length <= 3)
            {
                tag.str = "";
                tag.pExt = 0;
                tag.pVariant = 0;
                (value, error) = LanguageParser.SetTypeForKey(tag, item.Key, item.Value);
                if (error == null)
                    Equal(LanguageParser.TypeForKey(value, item.Key), item.Value, item.Input + " unset string type");
            }
        }
    }

    private static void TestContains()
    {
        foreach (var item in ContainsCases)
        {
            int enclosing = LanguageParser.GetRegionID(item.Enclosing.ToCharArray()).ID;
            int contained = LanguageParser.GetRegionID(item.Contained.ToCharArray()).ID;
            Equal(LanguageParser.RegionContains(enclosing, contained), item.Expected, item.Enclosing + " contains " + item.Contained);
        }
    }

    private static void TestScript()
    {
        foreach (var item in ScriptCases)
        {
            // Public language.Parse applies Default canonicalization before Script.
            LanguageParser.Tag tag = LanguageParser.Canonicalize(LanguageParser.CanonType.Deprecated | LanguageParser.CanonType.Legacy, Parse(item.Input)).Tag;
            var(script, confidence) = LanguageParser.ScriptWithConfidence(tag);
            Equal(LanguageParser.ScriptString(script), item.Script, item.Input + " script");
            Equal(confidence, item.Confidence, item.Input + " script confidence");
        }
    }

    private static void TestAddLikelySubtags()
    {
        foreach (var(input, expected)in TestAddLikelySubtagsCases)
        {
            var result = LanguageParser.AddLikelySubtags(Parse(input)).Tag;
            Equal(result.String(), Parse(expected).String(), "add(" + input + ")");
        }
    }

    private static void TestMinimize()
    {
        foreach (var(input, expected)in TestMinimizeCases)
        {
            var source = Parse(input);
            var min = LanguageParser.Minimize(source).Tag;
            Equal(min.String(), Parse(expected).String(), "min(" + input + ")");
            var max = LanguageParser.AddLikelySubtags(min).Tag;
            var originalMax = LanguageParser.AddLikelySubtags(source).Tag;
            Equal(originalMax.String(), max.String(), "max(min(" + input + "))");
        }
    }

    private static void TestBuilder(bool setTag)
    {
        foreach (var item in BuilderCases)
        {
            var tag = Parse(item.Input);
            var builder = new LanguageParser.TagBuilder();
            if (setTag)
                builder.SetTag(tag);
            else
            {
                builder.SetTag(new LanguageParser.Tag { LangID = item.Language == "" ? 0 : LanguageParser.GetLangID(item.Language.ToCharArray()).ID, ScriptID = item.Script == "" ? 0 : LanguageParser.GetScriptID(item.Script.ToCharArray()).ID, RegionID = item.Region == "" ? 0 : LanguageParser.GetRegionID(item.Region.ToCharArray()).ID, });
                if (item.Variants != "")
                    builder.AddVariant(item.Variants.Split('-'));
                // Original input tag's actual extension list, not the expected fields.
                foreach (string extension in LanguageParser.Extensions(tag))
                    builder.AddExt(extension);
            }

            var got = builder.Make();
            EqualTag(got, tag, item.Input);
            Equal(LanguageParser.GetLangID(item.Language.ToCharArray()).ID, got.LangID, item.Input + " part language");
            Equal(LanguageParser.GetScriptID(item.Script.ToCharArray()).ID, got.ScriptID, item.Input + " part script");
            Equal(LanguageParser.GetRegionID(item.Region.ToCharArray()).ID, got.RegionID, item.Input + " part region");
            if (got.str == "")
                continue;
            int p = got.pVariant;
            if (p < got.pExt)
                p++;
            Equal(got.str[p..got.pExt], item.Variants, item.Input + " part variants");
            p = got.pExt;
            if (p > 0 && p < got.str.Length)
                p++;
            Equal(got.str[p..], item.Extensions, item.Input + " part extensions");
        }
    }

    private static LanguageParser.Tag[] TagList(string text) => text.Split(',').Select(static item => Parse(item.Trim())).ToArray();
    private static void TestCompliance()
    {
        foreach (var item in ComplianceCases)
        {
            var supported = TagList(item.Supported);
            var desired = TagList(item.Desired);
            var(combined, index, _) = new LanguageParser.Matcher(supported).Match(desired);
            EqualTag(supported[index], Parse(item.Expected), item.Name + " match");
            if (item.Combined != "")
                Equal(LanguageParser.CompactString(combined), LanguageParser.CompactString(Parse(item.Combined)), item.Name + " combined");
        }
    }

    private static void TestRegionGroups()
    {
        (string A, string B, int Distance)[] cases = [("zh-TW", "zh-HK", 5), ("zh-MO", "zh-HK", 4), ("es-ES", "es-AR", 5), ("es-ES", "es", 4), ("es-419", "es-MX", 4), ("es-AR", "es-MX", 4), ("es-ES", "es-MX", 5), ("es-PT", "es-MX", 5)];
        foreach (var item in cases)
        {
            var a = Parse(item.A);
            LanguageParser.CanonicalizeDefault(a);
            var b = Parse(item.B);
            LanguageParser.CanonicalizeDefault(b);
            int aScript = LanguageParser.ScriptOf(a), bScript = LanguageParser.ScriptOf(b);
            Equal(aScript, bScript, item.A + " vs " + item.B + " scripts");
            Equal(LanguageParser.RegionGroupDistance(a.RegionID, b.RegionID, aScript, a.LangID).Distance, item.Distance, item.A + " vs " + item.B + " distance");
        }
    }

    private static void TestIsParadigmLocale()
    {
        (string Input, bool Expected)[] cases = [("en-US", true), ("en-GB", true), ("en-VI", false), ("es-GB", false), ("es-ES", true), ("es-419", true)];
        foreach (var item in cases)
        {
            var tag = Parse(item.Input);
            LanguageParser.CanonicalizeDefault(tag);
            Equal(LanguageParser.IsParadigmLocale(tag.LangID, tag.RegionID), item.Expected, "isPL(" + item.Input + ")");
        }
    }

    private static LanguageParser.Tag Canonicalize(LanguageParser.CanonType option, LanguageParser.Tag tag)
    {
        var(result, changed) = LanguageParser.Canonicalize(option, tag);
        if (changed)
            result.RemakeString();
        return result;
    }

    private static void TestCanonicalize()
    {
        foreach (var item in CanonicalizeCases)
        {
            var tag = Canonicalize(item.Option, Parse(item.Input));
            Equal(LanguageParser.CompactString(tag), item.Expected, "canonicalize(" + item.Input + ")");
        }

        foreach (int language in LanguageParser.BaseLanguages())
        {
            var tag = new LanguageParser.Tag
            {
                LangID = language
            };
            var got = Canonicalize(LanguageParser.CanonType.All, tag);
            var want = Canonicalize(LanguageParser.CanonType.All, got);
            EqualTag(got, want, "canonicalize idempotence " + tag.String());
        }
    }
}

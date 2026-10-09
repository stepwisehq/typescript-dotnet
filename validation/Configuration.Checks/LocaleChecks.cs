// Copyright 2013, 2015 The Go Authors. All rights reserved.
// BSD license: licenses/XText-LICENSE.txt.
// Modified assertion-preserving selected x/text v0.42.0 original test bodies.
// Buffer/error/Make representation adaptations are recorded in locale-test-provenance.json.
using Parser = TypeScript.Locale.XText.LanguageParser;
using TypeScript.Locale.XText;

namespace Configuration.Checks;
public static partial class LocaleChecks
{
    private sealed record ScanRow(bool Ok, string Input, string[] Tokens);
    private sealed record ParseRow(string Input, string Language, string Script, string Region, string Variants, string Extensions, bool Invalid, bool Rewrite, bool Changed);
    public static IReadOnlyList<string> Cases { get; } = ["InternalLanguage.TestScan", "InternalLanguage.TestAcceptMinSize", "InternalLanguage.TestParseExtensions", "InternalLanguage.TestParseTag", "InternalLanguage.TestParse", "InternalLanguage.TestErrors", "InternalTag.TestIndex", "InternalTag.TestFixCase", "Language.TestString", "InternalLanguage.TestEqualityAdapted", "Compact.TestEqualityAdapted", "Language.TestEqualityAdapted", "InternalLanguage.TestGrandfatheredAdapted", "InternalLanguage.TestMakeStringAdapted", "InternalLanguage.TestGetScriptIDAdapted", "InternalLanguage.TestLangIDSubset", "InternalLanguage.TestRegionIDSubset", "InternalLanguage.TestRegionM49Subset", "InternalLanguage.TestRegionDeprecationAdapted", "InternalLanguage.TestRegionCanonicalizeAdapted", "InternalLanguage.TestParseBaseAdapted", "InternalLanguage.TestParseScriptAdapted", "InternalLanguage.TestParseRegionAdapted", "Language.TestCanonicalizeSubset", "Language.TestParseAdapted", "Language.TestErrorsAdapted", "Language.ExampleParseErrorsAdapted"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "InternalLanguage.TestScan":
                TestScan();
                break;
            case "InternalLanguage.TestAcceptMinSize":
                TestAcceptMinSize();
                break;
            case "InternalLanguage.TestParseExtensions":
                TestParseExtensions();
                break;
            case "InternalLanguage.TestParseTag":
                TestParseTag();
                break;
            case "InternalLanguage.TestParse":
                TestParse();
                break;
            case "InternalLanguage.TestErrors":
                TestErrors();
                break;
            case "InternalTag.TestIndex":
                TestIndex();
                break;
            case "InternalTag.TestFixCase":
                TestFixCase();
                break;
            case "Language.TestString":
                TestString();
                break;
            case "InternalLanguage.TestEqualityAdapted":
                TestInternalEquality();
                break;
            case "Compact.TestEqualityAdapted":
                TestCompactEquality();
                break;
            case "Language.TestEqualityAdapted":
                TestPublicEquality();
                break;
            case "InternalLanguage.TestGrandfatheredAdapted":
                TestGrandfathered();
                break;
            case "InternalLanguage.TestMakeStringAdapted":
                TestMakeString();
                break;
            case "InternalLanguage.TestGetScriptIDAdapted":
                TestGetScriptID();
                break;
            case "InternalLanguage.TestLangIDSubset":
                TestLangIDSubset();
                break;
            case "InternalLanguage.TestRegionIDSubset":
                TestRegionIDSubset();
                break;
            case "InternalLanguage.TestRegionM49Subset":
                TestRegionM49Subset();
                break;
            case "InternalLanguage.TestRegionDeprecationAdapted":
                TestRegionDeprecation();
                break;
            case "InternalLanguage.TestRegionCanonicalizeAdapted":
                TestRegionCanonicalize();
                break;
            case "InternalLanguage.TestParseBaseAdapted":
                TestParseBase();
                break;
            case "InternalLanguage.TestParseScriptAdapted":
                TestParseScript();
                break;
            case "InternalLanguage.TestParseRegionAdapted":
                TestParseRegion();
                break;
            case "Language.TestCanonicalizeSubset":
                TestCanonicalizeSubset();
                break;
            case "Language.TestParseAdapted":
                TestPublicRawParts();
                break;
            case "Language.TestErrorsAdapted":
                TestPublicErrors();
                break;
            case "Language.ExampleParseErrorsAdapted":
                TestPublicErrorExample();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown selected original locale test.");
        }
    }

    private static void TestScan()
    {
        foreach (var row in scanRows)
        {
            var scan = new Parser.Scanner(row.Input);
            int count = 0;
            while (!scan.done)
            {
                Check.True(count < row.Tokens.Length, row.Input + " extra token");
                Check.Equal(new string (scan.token), row.Tokens[count], row.Input + " token");
                count++;
                scan.Scan();
            }

            Check.Equal(scan.b.Replace('_', '-'), string.Join('-', row.Tokens), row.Input + " rewritten buffer");
            Check.Equal(scan.err == null, row.Ok, row.Input + " scan error");
        }
    }

    private static void TestAcceptMinSize()
    {
        foreach (var row in scanRows)
            for (int size = 1; size <= 8; size++)
            {
                var scan = new Parser.Scanner(row.Input);
                scan.end = scan.next = 0;
                int actual = scan.AcceptMinSize(size), expected = 0;
                for (int i = 0; i < row.Tokens.Length && row.Tokens[i].Length >= size; i++)
                    expected += row.Tokens[i].Length + (i > 0 ? 1 : 0);
                Check.Equal(actual, expected, row.Input + " minimum size " + size);
            }
    }

    private static void TestParseExtensions()
    {
        foreach (var row in parseRows)
        {
            if (row.Extensions == "" || row.Rewrite)
                continue;
            var scan = new Parser.Scanner(row.Input);
            if (scan.b.Length > 1 && scan.b[1] != '-')
            {
                scan.end = Parser.NextExtension(scan.b, 0);
                scan.next = scan.end + 1;
                scan.Scan();
            }

            int start = scan.start;
            scan.ToLower(start, scan.b.Length);
            Parser.ParseExtensions(scan);
            string extension = scan.b[start..];
            Check.Equal(extension, row.Extensions, row.Input + " extensions");
            Check.Equal(!row.Input[start..].StartsWith(extension, StringComparison.Ordinal), row.Changed, row.Input + " changed extensions");
        }
    }

    private static void AssertParts(Parser.Tag tag, ParseRow row, bool includeExtensions)
    {
        Check.Equal(tag.LangID, Parser.GetLangID(row.Language.ToCharArray()).ID, row.Input + " language");
        Check.Equal(tag.ScriptID, Parser.GetScriptID(row.Script.ToCharArray()).ID, row.Input + " script");
        Check.Equal(tag.RegionID, Parser.GetRegionID(row.Region.ToCharArray()).ID, row.Input + " region");
        if (tag.str == "")
            return;
        int position = tag.pVariant;
        if (position < tag.pExt)
            position++;
        Check.Equal(tag.str[position..tag.pExt], row.Variants, row.Input + " variants");
        position = tag.pExt;
        if (position > 0 && position < tag.str.Length)
            position++;
        Check.Equal(tag.str[position..], includeExtensions ? row.Extensions : "", row.Input + " part extensions");
    }

    private static void TestParseTag()
    {
        foreach (var row in parseRows)
        {
            if (row.Input.StartsWith("x-", StringComparison.Ordinal) || row.Rewrite)
                continue;
            var scan = new Parser.Scanner(row.Input);
            var(tag, end) = Parser.ParseTag(scan, true);
            tag.str = scan.b[..end];
            AssertParts(tag, row, false);
        }
    }

    private static void TestParse()
    {
        foreach (var row in parseRows)
        {
            var(tag, error) = Parser.Parse(row.Input);
            string extension = "";
            if (tag.str != "")
            {
                if (tag.str.StartsWith("x-", StringComparison.Ordinal))
                    extension = tag.str;
                else if (tag.pExt < tag.str.Length && tag.pExt > 0)
                    extension = tag.str[(tag.pExt + 1)..];
            }

            Check.Equal(Parser.Parse(tag.String()).Tag.String(), tag.String(), row.Input + " reparse");
            Check.Equal(extension, row.Extensions, row.Input + " extensions");
            Check.Equal(tag.str != "" && !row.Input.StartsWith(tag.str, StringComparison.Ordinal), row.Changed, row.Input + " changed");
            Check.Equal(error != null, row.Invalid, row.Input + " invalid");
            AssertParts(tag, row, true);
        }
    }

    private static void TestErrors()
    {
        (string Input, string? Subtag)[] rows = [("ac", "ac"), ("AC", "ac"), ("aa-Uuuu", "Uuuu"), ("aa-AB", "AB"), ("ac-u", null), ("ac-u-ca", "ac"), ("ac-u-ca-co-pinyin", "ac"), ("noob", null)];
        foreach (var row in rows)
        {
            var(_, error) = Parser.Parse(row.Input);
            if (row.Subtag == null)
                Check.True(ReferenceEquals(error, Parser.ErrSyntax), row.Input + " syntax sentinel");
            else
            {
                // Go ValueError is a comparable value in the error interface.
                // Its managed exception representation retains the same type/subtag.
                Check.True(error is Parser.ValueError, row.Input + " error dynamic type");
                var expected = (Parser.ValueError)Parser.NewValueError(row.Subtag.ToCharArray());
                Check.Equal(((Parser.ValueError)error!).Subtag(), expected.Subtag(), row.Input + " value-error subtag");
                Check.Equal(error!.Message, expected.Message, row.Input + " value-error text");
            }
        }
    }

    private static void TestIndex()
    {
        foreach (var row in indexRows)
            Check.Equal(Parser.Index(indexTable, row.Key), row.Expected, row.Key + " index");
    }

    private static void TestFixCase()
    {
        foreach (var row in fixRows)
        {
            char[] buffer = row.Input.ToCharArray();
            string actual = Parser.FixCase(row.Form, buffer) ? new string (buffer) : "";
            Check.Equal(actual, row.Expected, row.Form + " fix case " + row.Input);
        }
    }

    private static void TestString()
    {
        // Make delegates to the selected Default.Parse path and ignores its error.
        // This original input is valid; no unselected public Make API is introduced.
        foreach (string input in publicStringRows)
            Check.Equal(PublicLanguage.Parse(input).Tag, input, input + " public String");
    }

    private static void TestInternalEquality()
    {
        foreach (var row in parseRows)
        {
            var first = Parser.Parse(row.Input).Tag;
            var second = Parser.Parse(first.String()).Tag;
            // Original internal Tag is a Go comparable value, while this
            // managed retained payload is a class. Assert every source slot.
            Check.Equal(second.LangID, first.LangID, row.Input + " equal language");
            Check.Equal(second.ScriptID, first.ScriptID, row.Input + " equal script");
            Check.Equal(second.RegionID, first.RegionID, row.Input + " equal region");
            Check.Equal(second.pVariant, first.pVariant, row.Input + " equal variant offset");
            Check.Equal(second.pExt, first.pExt, row.Input + " equal extension offset");
            Check.Equal(second.str, first.str, row.Input + " equal stored string");
        }
    }

    private static void TestCompactEquality()
    {
        foreach (string input in compactEqualityInputs)
        {
            var first = new TypeScript.Locale.Locale(Parser.CompactString(Parser.Parse(input).Tag));
            var second = new TypeScript.Locale.Locale(Parser.CompactString(Parser.Parse(first.String()).Tag));
            // Selected comparable Locale state preserves the canonical value;
            // private compact language/regional numeric IDs remain unclaimed.
            Check.Equal(second, first, input + " compact roundtrip comparable state");
        }
    }

    private static void TestPublicEquality()
    {
        foreach (string input in publicEqualityInputs)
        {
            var first = TypeScript.Locale.Locales.Parse(input).Locale;
            var second = TypeScript.Locale.Locales.Parse(first.String()).Locale;
            Check.Equal(second, first, input + " public roundtrip comparable state");
        // The original's separate Compose assertion is outside the selected API.
        }
    }

    private static void TestGrandfathered()
    {
        foreach (var row in grandfatheredRows)
        {
            var first = Parser.Parse(row.Input).Tag;
            var(expected, error) = Parser.Parse(row.Expected);
            Check.True(error == null, row.Expected + " original MustParse setup");
            Check.Equal(first.LangID, expected.LangID, row.Input + " grandfathered language");
            Check.Equal(first.ScriptID, expected.ScriptID, row.Input + " grandfathered script");
            Check.Equal(first.RegionID, expected.RegionID, row.Input + " grandfathered region");
            Check.Equal(first.pVariant, expected.pVariant, row.Input + " grandfathered variant offset");
            Check.Equal(first.pExt, expected.pExt, row.Input + " grandfathered extension offset");
            Check.Equal(first.str, expected.str, row.Input + " grandfathered stored string");
        }
    }

    private static void TestMakeString()
    {
        foreach (var row in makeStringRows)
        {
            var value = Parser.Parse(row.Input).Tag;
            var modified = Parser.Parse(row.Expected).Tag;
            // Original setTagsFrom test setup changes exactly these three fields.
            value.LangID = modified.LangID;
            value.ScriptID = modified.ScriptID;
            value.RegionID = modified.RegionID;
            for (int i = 0; i < 2; i++)
            {
                value.RemakeString();
                Check.Equal(value.String(), row.Expected, row.Input + " remake " + i);
            }
        // Original Go AllocsPerRun <=1 is outside the managed representation.
        }
    }

    private static void TestGetScriptID()
    {
        foreach (var row in scriptIDRows)
        {
            // GetScriptID delegates to this selected general helper in Go.
            // The managed registry-specific wrapper is not expanded for testing.
            var(id, error) = Parser.FindIndex(scriptTestTable, row.Input.ToCharArray(), "Zzzz");
            Check.Equal(id, row.Expected, row.Input + " custom script index");
            if (id == 0)
                Check.True(error != null, row.Input + " missing script error");
        }
    }

    private static void AssertNamedError(Exception? error, string expected, string context)
    {
        if (expected == "")
            Check.True(error is null, context + " no error");
        else if (expected == "syntax")
            Check.True(ReferenceEquals(error, Parser.ErrSyntax), context + " syntax sentinel");
        else
        {
            Check.True(error is Parser.ValueError, context + " value-error type");
            Check.Equal(((Parser.ValueError)error!).Subtag(), expected[6..], context + " value-error subtag");
        }
    }

    private static void TestLangIDSubset()
    {
        foreach (var row in langIDRows)
        {
            var(id, error) = Parser.GetLangID(row.Input.ToCharArray());
            AssertNamedError(error, row.Error, row.Input);
            if (error != null)
                continue;
            if (row.BCP47.Length == 2)
                Check.Equal(Parser.GetLangISO2(row.BCP47.ToCharArray()).ID, id, row.Input + " ISO2 input");
            if (row.ISO3.Length == 3)
            {
                Check.Equal(Parser.GetLangISO3(row.ISO3.ToCharArray()).ID, id, row.Input + " ISO3 input");
                Check.Equal(Parser.GetLangID(row.ISO3.ToCharArray()).ID, id, row.Input + " ID3 input");
            }

            int normalized = row.Normalized == "" ? id : Parser.GetLangID(row.Normalized.ToCharArray()).ID;
            Check.Equal(Parser.NormLang(id).ID, normalized, row.Input + " normalized ID");
            Check.Equal(Parser.LanguageString(id), row.BCP47, row.Input + " BCP47 String");
        // Original ISO3 output method is outside the selected closure.
        }
    }

    private static void TestRegionIDSubset()
    {
        foreach (var row in regionIDRows)
        {
            if (row.Input.StartsWith('_'))
            {
                Check.True(Parser.GetRegionID(row.Input[1..].ToCharArray()).Error != null, row.Input + " invalid region");
                continue;
            }

            var(id, _) = Parser.GetRegionID(row.Input.ToCharArray());
            Check.Equal(Parser.RegionString(id), row.Expected, row.Input + " region String");
            // GetRegionID's two-byte arm is the selected inlined ISO2 lookup;
            // retain the original second ISO2 assertion through that branch.
            if (row.Input.Length == 2)
                Check.Equal(Parser.RegionString(Parser.GetRegionID(row.Input.ToCharArray()).ID), row.Expected, row.Input + " ISO2 String");
        }
    }

    private static void TestRegionM49Subset()
    {
        foreach (var row in m49Rows)
        {
            var(id, error) = Parser.GetRegionM49(row.Input);
            Check.Equal(error != null, row.Expected == "", row.Input + " M49 lookup error");
            Check.Equal(id, Parser.GetRegionID(row.Expected.ToCharArray()).ID, row.Input + " M49 lookup ID");
        }
    // Original inverse Region.M49 output assertions are not selected.
    }

    private static void TestRegionDeprecation()
    {
        foreach (var row in regionDeprecationRows)
        {
            int input = Parser.GetRegionID(row.Input.ToCharArray()).ID;
            int expected = Parser.GetRegionID(row.Expected.ToCharArray()).ID;
            int output = Parser.CanonicalRegion(input);
            // Original normRegion returns 0 when unchanged; the existing
            // selected canonicalization helper retains the input in that case.
            Check.Equal(output == input ? 0 : output, expected == input ? 0 : expected, row.Input + " deprecated region decision");
        }
    }

    private static void TestRegionCanonicalize()
    {
        foreach (var row in regionCanonicalRows)
            Check.Equal(Parser.CanonicalRegion(Parser.GetRegionID(row.Input.ToCharArray()).ID), Parser.GetRegionID(row.Expected.ToCharArray()).ID, row.Input + " canonical region");
    }

    private static void TestParseBase()
    {
        foreach (var row in baseRows)
        {
            var(id, error) = Parser.GetLangID(row.Input.ToCharArray());
            Check.Equal(Parser.LanguageString(id), row.Expected, row.Input + " base String");
            Check.Equal(error == null, row.Ok, row.Input + " base error");
            Check.Equal(id, Parser.Parse(row.Expected).Tag.LangID, row.Input + " raw base field");
        }
    }

    private static void TestParseScript()
    {
        foreach (var row in scriptRows)
        {
            var(id, error) = Parser.GetScriptID(row.Input.ToCharArray());
            Check.Equal(error == null, row.Ok, row.Input + " script error");
            if (error == null)
            {
                Check.Equal(id, Parser.GetScriptID(row.Expected.ToCharArray()).ID, row.Input + " expected script ID");
                Check.Equal(id, Parser.Parse("und-" + row.Expected).Tag.ScriptID, row.Input + " raw script field");
            }
            else
            {
                // Source lookup returns zero on failure. Unspecified Script's
                // String is Zzzz, distinct from the valid registry Zzzz ID.
                Check.Equal(id, 0, row.Input + " failed script lookup zero");
            }
        }
    }

    private static void TestParseRegion()
    {
        foreach (var row in regionRows)
        {
            var(id, error) = Parser.GetRegionID(row.Input.ToCharArray());
            Check.Equal(Parser.RegionString(id), row.Expected, row.Input + " region String");
            Check.Equal(error == null, row.Ok, row.Input + " region error");
            if (error == null)
                Check.Equal(id, Parser.Parse("und-" + row.Expected).Tag.RegionID, row.Input + " raw region field");
        }
    }

    private static void TestCanonicalizeSubset()
    {
        foreach (var row in canonicalRows)
        {
            var tag = Parser.Parse(row.Input).Tag;
            if (!row.Raw && Parser.CanonicalizeDefault(tag))
                tag.RemakeString();
            Check.Equal(tag.String(), row.Expected, row.Input + " original compatible canonicalization");
        }
    // SuppressScript/Macro/CLDR/All and Supported/Compose idempotence are
    // excluded; exact selected original rows are recorded by the generator.
    }

    private static void TestPublicRawParts()
    {
        foreach (var row in publicRawRows)
        {
            var tag = Parser.Parse(row.Input).Tag;
            AssertParts(tag, row, true);
            if (tag.str == "")
            {
                Check.Equal("", row.Variants, row.Input + " absent variants");
                Check.Equal("", row.Extensions, row.Input + " absent extensions");
            }
        }
    }

    private static void TestPublicErrors()
    {
        (string Input, string Error)[] rows = [("ac", "value:ac"), ("AC", "value:ac"), ("aa-Uuuu", "value:Uuuu"), ("aa-AB", "value:AB"), ("ac-u", "syntax"), ("ac-u-ca", "value:ac"), ("ac-u-ca-co-pinyin", "value:ac"), ("noob", "syntax")];
        foreach (var row in rows)
            AssertNamedError(PublicLanguage.Parse(row.Input).Error, row.Error, row.Input);
    }

    private static void TestPublicErrorExample()
    {
        var output = new System.Text.StringBuilder();
        foreach (string input in new[]
        {
            "Foo",
            "Bar",
            "Foobar"
        }

        )
        {
            var error = PublicLanguage.Parse(input).Error;
            if (error is Parser.ValueError value)
                output.Append(value.Subtag()).Append('\n');
            else if (error != null)
                output.Append(input).Append('\n');
        }

        foreach (string input in new[]
        {
            "en",
            "aa-Uuuu",
            "AC",
            "ac-u"
        }

        )
        {
            var error = PublicLanguage.Parse(input).Error;
            if (error is Parser.ValueError value)
                output.Append(input).Append(": culprit ").Append(TypeScript.Scanner.GoQuote.Quote(value.Subtag())).Append('\n');
            else if (error != null)
                output.Append(input).Append(": ill-formed\n");
        }

        Check.Equal(output.ToString(), "foo\nFoobar\naa-Uuuu: culprit \"Uuuu\"\nAC: culprit \"ac\"\nac-u: ill-formed\n", "Original ExampleParse_errors output with selected parser");
    }
}

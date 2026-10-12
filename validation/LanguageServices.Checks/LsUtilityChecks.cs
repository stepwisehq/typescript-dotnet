// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete original internal/ls/lsutil/utilities_test.go assertions.
using TypeScript.Scanner;
using TypeScript.LanguageServices.Utilities;

namespace LanguageServices.Checks;
public static class LsUtilityChecks
{
    public static readonly string[] Names = ["TestProbablyUsesSemicolons", "TestResolveOrganizeImportsSort", "TestCompareOrganizeImportsNaturalStrings"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestProbablyUsesSemicolons":
                TestProbablyUsesSemicolons();
                break;
            case "TestResolveOrganizeImportsSort":
                TestResolveOrganizeImportsSort();
                break;
            case "TestCompareOrganizeImportsNaturalStrings":
                TestCompareOrganizeImportsNaturalStrings();
                break;
            default:
                throw new ArgumentException(name);
        }
    }

    // utilities_test.go:19. Original first-five observations and all three rows.
    private static void TestProbablyUsesSemicolons()
    {
        (string Name, string Source, bool Want)[] cases = [("mixed semicolons and ASI favors semicolons when ratio exceeds one fifth", "let a = 1;\nlet b = 2;\nlet c = 3\nlet d = 4\nlet e = 5\n", true), ("consistent ASI with no semicolons", "let a = 1\nlet b = 2\nlet c = 3\n", false), ("consistent semicolons", "let a = 1;\nlet b = 2;\nlet c = 3;\n", true)];
        foreach (var item in cases)
        {
            var file = Parser.ParseSourceFile(new SourceFileParseOptions("/test.ts", "/test.ts"), item.Source, ScriptKind.TS);
            if (LsUtil.ProbablyUsesSemicolons(file) != item.Want)
                throw new InvalidOperationException(item.Name);
        }
    }

    // utilities_test.go:69. Complete seven-row original preference-to-sort table.
    private static void TestResolveOrganizeImportsSort()
    {
        (string Name, UserPreferences Preferences, OrganizeImportsSort Want)[] cases = [("explicit sort wins", new() { OrganizeImportsSort = OrganizeImportsSort.Ordinal, OrganizeImportsCollation = OrganizeImportsCollation.Unicode, OrganizeImportsIgnoreCase = Tristate.True }, OrganizeImportsSort.Ordinal), ("unicode case-sensitive maps to natural", new() { OrganizeImportsCollation = OrganizeImportsCollation.Unicode, OrganizeImportsIgnoreCase = Tristate.False }, OrganizeImportsSort.Natural), ("unicode ignore case maps to natural ignore case", new() { OrganizeImportsCollation = OrganizeImportsCollation.Unicode, OrganizeImportsIgnoreCase = Tristate.True }, OrganizeImportsSort.NaturalIgnoreCase), ("unicode unknown case sensitivity stays auto for detection", new() { OrganizeImportsCollation = OrganizeImportsCollation.Unicode }, OrganizeImportsSort.Auto), ("ordinal ignore case maps to ordinal ignore case", new() { OrganizeImportsIgnoreCase = Tristate.True }, OrganizeImportsSort.OrdinalIgnoreCase), ("ordinal case sensitive maps to ordinal", new() { OrganizeImportsIgnoreCase = Tristate.False }, OrganizeImportsSort.Ordinal), ("unknown ordinal stays auto", default, OrganizeImportsSort.Auto)];
        foreach (var item in cases)
            if (LsUtil.ResolveOrganizeImportsSort(item.Preferences) != item.Want)
                throw new InvalidOperationException(item.Name);
    }

    // utilities_test.go:139. Original comparer and cmpSign semantics, all five byte-preserved rows.
    private static void TestCompareOrganizeImportsNaturalStrings()
    {
        var compare = LsUtil.getOrganizeImportsPresetStringComparer(OrganizeImportsSort.NaturalIgnoreCase);
        (string Name, string A, string B, int Want)[] cases = [("numeric runs sort by numeric value", "a2", "a100", -1), ("numeric runs with equal value use raw tie break", "a02", "a2", -1), ("accents are folded for primary comparison", "À", "B", -1), ("raw comparison breaks accent ties", "A", "À", -1), ("hyphen sorts before slash like Intl.Collator fallback", "app-init", "app/app", -1)];
        foreach (var item in cases)
        {
            var a = ByteStringView.FromUnicode(item.A).Materialize();
            var b = ByteStringView.FromUnicode(item.B).Materialize();
            if (Math.Sign(compare(a, b)) != item.Want)
                throw new InvalidOperationException(item.Name);
        }
    }
}

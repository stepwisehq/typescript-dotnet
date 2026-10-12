// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original preference assertions execute synchronously, including original parallel subtests.
// Go t.Parallel scheduling is not reproduced; these tests assert preference values, not scheduling.
#nullable disable
using TypeScript.Scanner;
using TypeScript.LanguageServices.Utilities;
using TypeScript.ModuleSpecifiers;
using UserPreferences = TypeScript.LanguageServices.Utilities.UserPreferences;

namespace LanguageServices.Checks;
public static partial class UserPreferenceChecks
{
    // userpreferences_test.go:177.
    private static void TestUserPreferencesSerialize(T t)
    {
        t.Parallel();
        t.Run("config path field serializes to nested path", (t2) =>
        {
            t2.Parallel();
            var prefs = new UserPreferences
            {
                QuotePreference = QuotePreference.Single
            };
            var tuple1 = Marshal(prefs);
            var jsonBytes = tuple1.Item1;
            var err = tuple1.Item2;
            Assertions.NilError(t2, err);
            Dictionary<string, object> actual = default;
            err = Unmarshal(jsonBytes, out actual);
            Assertions.NilError(t2, err);
            var preferences = ((Dictionary<string, object>)actual["preferences"]);
            Assertions.Equal(t2, "single", preferences["quoteStyle"]);
        });
        t.Run("raw-only field serializes to unstable section", (t3) =>
        {
            t3.Parallel();
            var prefs2 = new UserPreferences
            {
                DisableSuggestions = Tristate.True
            };
            var tuple2 = Marshal(prefs2);
            var jsonBytes2 = tuple2.Item1;
            var err2 = tuple2.Item2;
            Assertions.NilError(t3, err2);
            Dictionary<string, object> actual2 = default;
            err2 = Unmarshal(jsonBytes2, out actual2);
            Assertions.NilError(t3, err2);
            var unstable = ((Dictionary<string, object>)actual2["unstable"]);
            Assertions.Equal(t3, true, unstable["disableSuggestions"]);
        });
        t.Run("inlay hint inversion on serialize", (t4) =>
        {
            t4.Parallel();
            var prefs3 = new UserPreferences
            {
                IncludeInlayParameterNameHints = IncludeInlayParameterNameHints.All,
                IncludeInlayParameterNameHintsWhenArgumentMatchesName = Tristate.True
            };
            var tuple3 = Marshal(prefs3);
            var jsonBytes3 = tuple3.Item1;
            var err3 = tuple3.Item2;
            Assertions.NilError(t4, err3);
            Dictionary<string, object> actual3 = default;
            err3 = Unmarshal(jsonBytes3, out actual3);
            Assertions.NilError(t4, err3);
            var inlayHints = ((Dictionary<string, object>)actual3["inlayHints"]);
            var parameterNames = ((Dictionary<string, object>)inlayHints["parameterNames"]);
            Assertions.Equal(t4, "all", parameterNames["enabled"]);
            Assertions.Equal(t4, false, parameterNames["suppressWhenArgumentMatchesName"]);
        });
        t.Run("mixed config and unstable fields", (t5) =>
        {
            t5.Parallel();
            var prefs4 = new UserPreferences
            {
                QuotePreference = QuotePreference.Single,
                DisableSuggestions = Tristate.True,
                DisplayPartsForJSDoc = Tristate.True
            };
            var tuple4 = Marshal(prefs4);
            var jsonBytes4 = tuple4.Item1;
            var err4 = tuple4.Item2;
            Assertions.NilError(t5, err4);
            Dictionary<string, object> actual4 = default;
            err4 = Unmarshal(jsonBytes4, out actual4);
            Assertions.NilError(t5, err4);
            var preferences2 = ((Dictionary<string, object>)actual4["preferences"]);
            Assertions.Equal(t5, "single", preferences2["quoteStyle"]);
            var unstable2 = ((Dictionary<string, object>)actual4["unstable"]);
            Assertions.Equal(t5, true, unstable2["disableSuggestions"]);
            Assertions.Equal(t5, true, unstable2["displayPartsForJSDoc"]);
        });
    }

    // userpreferences_test.go:254.
    private static void TestUserPreferencesParseUnstable(T t)
    {
        t.Parallel();
        var tests = new PreferenceJsonCase[]
        {
            new PreferenceJsonCase
            {
                name = "unstable fields with correct casing",
                json = "{\n\t\t\t\t\"unstable\": {\n\t\t\t\t\t\"disableSuggestions\": true,\n\t\t\t\t\t\"maximumHoverLength\": 100,\n\t\t\t\t\t\"allowRenameOfImportPath\": true\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    DisableSuggestions = Tristate.True,
                    MaximumHoverLength = 100,
                    AllowRenameOfImportPath = Tristate.True
                }
            },
            new PreferenceJsonCase
            {
                name = "nested preferences path",
                json = "{\n\t\t\t\t\"preferences\": {\n\t\t\t\t\t\"quoteStyle\": \"single\",\n\t\t\t\t\t\"useAliasesForRenames\": true\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    QuotePreference = QuotePreference.Single,
                    ProvidePrefixAndSuffixTextForRename = Tristate.True
                }
            },
            new PreferenceJsonCase
            {
                name = "suggest section",
                json = "{\n\t\t\t\t\"suggest\": {\n\t\t\t\t\t\"autoImports\": false,\n\t\t\t\t\t\"includeCompletionsForImportStatements\": true\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    IncludeCompletionsForModuleExports = Tristate.False,
                    IncludeCompletionsForImportStatements = Tristate.True
                }
            },
            new PreferenceJsonCase
            {
                name = "inlayHints with invert",
                json = "{\n\t\t\t\t\"inlayHints\": {\n\t\t\t\t\t\"parameterNames\": {\n\t\t\t\t\t\t\"enabled\": \"all\",\n\t\t\t\t\t\t\"suppressWhenArgumentMatchesName\": true\n\t\t\t\t\t}\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    InlayHintsPreferences = new InlayHintsPreferences
                    {
                        IncludeInlayParameterNameHints = IncludeInlayParameterNameHints.All,
                        IncludeInlayParameterNameHintsWhenArgumentMatchesName = Tristate.False
                    }
                }
            },
            new PreferenceJsonCase
            {
                name = "mixed config",
                json = "{\n\t\t\t\t\"unstable\": {\n\t\t\t\t\t\"displayPartsForJSDoc\": true\n\t\t\t\t},\n\t\t\t\t\"preferences\": {\n\t\t\t\t\t\"importModuleSpecifier\": \"relative\"\n\t\t\t\t},\n\t\t\t\t\"workspaceSymbols\": {\n\t\t\t\t\t\"excludeLibrarySymbols\": true,\n\t\t\t\t\t\"scope\": \"currentProject\"\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    DisplayPartsForJSDoc = Tristate.True,
                    ImportModuleSpecifierPreference = ImportModuleSpecifierPreference.Relative,
                    ExcludeLibrarySymbolsInNavTo = Tristate.True,
                    WorkspaceSymbolsScope = WorkspaceSymbolsScope.CurrentProject
                }
            },
            new PreferenceJsonCase
            {
                name = "stable config overrides unstable",
                json = "{\n\t\t\t\t\"unstable\": {\n\t\t\t\t\t\"quotePreference\": \"double\"\n\t\t\t\t},\n\t\t\t\t\"preferences\": {\n\t\t\t\t\t\"quoteStyle\": \"single\"\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    QuotePreference = QuotePreference.Single
                }
            },
            new PreferenceJsonCase
            {
                name = "unstable sets value when no stable config",
                json = "{\n\t\t\t\t\"unstable\": {\n\t\t\t\t\t\"includeAutomaticOptionalChainCompletions\": false\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    IncludeAutomaticOptionalChainCompletions = Tristate.False
                }
            },
            new PreferenceJsonCase
            {
                name = "any field can be passed via unstable by its raw name",
                json = "{\n\t\t\t\t\"unstable\": {\n\t\t\t\t\t\"quotePreference\": \"double\",\n\t\t\t\t\t\"includeCompletionsForModuleExports\": true,\n\t\t\t\t\t\"excludeLibrarySymbolsInNavTo\": true\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    QuotePreference = QuotePreference.Double,
                    IncludeCompletionsForModuleExports = Tristate.True,
                    ExcludeLibrarySymbolsInNavTo = Tristate.True
                }
            },
            new PreferenceJsonCase
            {
                name = "TypeScript raw names work in unstable section",
                json = "{\n\t\t\t\t\"unstable\": {\n\t\t\t\t\t\"includeCompletionsForModuleExports\": true,\n\t\t\t\t\t\"quotePreference\": \"single\",\n\t\t\t\t\t\"providePrefixAndSuffixTextForRename\": true,\n\t\t\t\t\t\"includeInlayParameterNameHints\": \"all\",\n\t\t\t\t\t\"organizeImportsLocale\": \"en\"\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    IncludeCompletionsForModuleExports = Tristate.True,
                    QuotePreference = QuotePreference.Single,
                    ProvidePrefixAndSuffixTextForRename = Tristate.True,
                    OrganizeImportsLocale = "en",
                    InlayHintsPreferences = new InlayHintsPreferences
                    {
                        IncludeInlayParameterNameHints = IncludeInlayParameterNameHints.All
                    }
                }
            },
            new PreferenceJsonCase
            {
                name = "old raw organize imports unicode preferences load as raw state",
                json = "{\n\t\t\t\t\"unstable\": {\n\t\t\t\t\t\"organizeImportsCollation\": \"unicode\",\n\t\t\t\t\t\"organizeImportsCaseFirst\": \"upper\",\n\t\t\t\t\t\"organizeImportsIgnoreCase\": false,\n\t\t\t\t\t\"organizeImportsNumericCollation\": true\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    OrganizeImportsCollation = OrganizeImportsCollation.Unicode,
                    OrganizeImportsCaseFirst = OrganizeImportsCaseFirst.Upper,
                    OrganizeImportsIgnoreCase = Tristate.False,
                    OrganizeImportsNumericCollation = Tristate.True
                }
            },
            new PreferenceJsonCase
            {
                name = "old top-level raw organize imports unicode preferences load as raw state",
                json = "{\n\t\t\t\t\"organizeImportsCollation\": \"unicode\",\n\t\t\t\t\"organizeImportsIgnoreCase\": true\n\t\t\t}",
                expected = new UserPreferences
                {
                    OrganizeImportsCollation = OrganizeImportsCollation.Unicode,
                    OrganizeImportsIgnoreCase = Tristate.True
                }
            },
            new PreferenceJsonCase
            {
                name = "new top-level raw organize imports sort is accepted",
                json = "{\n\t\t\t\t\"organizeImportsSort\": \"natural\"\n\t\t\t}",
                expected = new UserPreferences
                {
                    OrganizeImportsSort = OrganizeImportsSort.Natural
                }
            },
            new PreferenceJsonCase
            {
                name = "old raw organize imports ignore case loads as raw state",
                json = "{\n\t\t\t\t\"unstable\": {\n\t\t\t\t\t\"organizeImportsIgnoreCase\": true\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    OrganizeImportsIgnoreCase = Tristate.True
                }
            },
            new PreferenceJsonCase
            {
                name = "new raw organize imports sort loads alongside old raw preferences",
                json = "{\n\t\t\t\t\"unstable\": {\n\t\t\t\t\t\"organizeImportsSort\": \"ordinal\",\n\t\t\t\t\t\"organizeImportsCollation\": \"unicode\",\n\t\t\t\t\t\"organizeImportsIgnoreCase\": true\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    OrganizeImportsSort = OrganizeImportsSort.Ordinal,
                    OrganizeImportsCollation = OrganizeImportsCollation.Unicode,
                    OrganizeImportsIgnoreCase = Tristate.True
                }
            },
            new PreferenceJsonCase
            {
                name = "old nested organize imports unicode preferences load as raw state",
                json = "{\n\t\t\t\t\"preferences\": {\n\t\t\t\t\t\"organizeImports\": {\n\t\t\t\t\t\t\"unicodeCollation\": \"unicode\",\n\t\t\t\t\t\t\"caseSensitivity\": \"caseSensitive\",\n\t\t\t\t\t\t\"numericCollation\": true,\n\t\t\t\t\t\t\"caseFirst\": \"upper\"\n\t\t\t\t\t}\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    OrganizeImportsCollation = OrganizeImportsCollation.Unicode,
                    OrganizeImportsIgnoreCase = Tristate.False,
                    OrganizeImportsNumericCollation = Tristate.True,
                    OrganizeImportsCaseFirst = OrganizeImportsCaseFirst.Upper
                }
            },
            new PreferenceJsonCase
            {
                name = "new nested organize imports sort loads alongside old nested preferences",
                json = "{\n\t\t\t\t\"preferences\": {\n\t\t\t\t\t\"organizeImports\": {\n\t\t\t\t\t\t\"sort\": \"ordinalIgnoreCase\",\n\t\t\t\t\t\t\"unicodeCollation\": \"unicode\",\n\t\t\t\t\t\t\"caseSensitivity\": \"caseSensitive\"\n\t\t\t\t\t}\n\t\t\t\t}\n\t\t\t}",
                expected = new UserPreferences
                {
                    OrganizeImportsSort = OrganizeImportsSort.OrdinalIgnoreCase,
                    OrganizeImportsCollation = OrganizeImportsCollation.Unicode,
                    OrganizeImportsIgnoreCase = Tristate.False
                }
            }
        };
        {
            foreach (var tt in tests)
            {
                t.Run(tt.name, (t2) =>
                {
                    t2.Parallel();
                    Dictionary<string, object> config = default;
                    var err = Unmarshal(tt.json, out config);
                    Assertions.NilError(t2, err);
                    var parsed = new UserPreferences
                    {
                    }.WithConfig(config);
                    Assertions.DeepEqual(t2, tt.expected, parsed);
                });
            }
        }
    }

    // userpreferences_test.go:517.
    private static void TestUserPreferencesLocale(T t)
    {
        t.Parallel();
        var prefs = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["typescript"] = new Dictionary<string, object> { ["locale"] = "de" }, ["js/ts"] = new Dictionary<string, object> { ["locale"] = "fr" } });
        Assertions.Equal(t, prefs.Locale, "fr");
    }

    // userpreferences_test.go:532.
    private static void TestUserPreferencesReportStyleChecksAsWarnings(T t)
    {
        t.Parallel();
        t.Run("reportStyleChecksAsWarnings via config path", (t2) =>
        {
            t2.Parallel();
            var prefs = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["js/ts"] = new Dictionary<string, object> { ["reportStyleChecksAsWarnings"] = false } });
            Assertions.Equal(t2, prefs.ReportStyleChecksAsWarnings, Tristate.False);
        });
        t.Run("reportStyleChecksAsWarnings defaults to true", (t3) =>
        {
            t3.Parallel();
            var prefs2 = LsUtil.NewDefaultUserPreferences();
            Assertions.Equal(t3, prefs2.ReportStyleChecksAsWarnings, Tristate.True);
        });
        t.Run("reportStyleChecksAsWarnings via unstable section", (t4) =>
        {
            t4.Parallel();
            var prefs3 = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["js/ts"] = new Dictionary<string, object> { ["unstable"] = new Dictionary<string, object> { ["reportStyleChecksAsWarnings"] = false } } });
            Assertions.Equal(t4, prefs3.ReportStyleChecksAsWarnings, Tristate.False);
        });
    }

    // userpreferences_test.go:564.
    private static void TestUserPreferencesParseServerFeaturePreferences(T t)
    {
        t.Parallel();
        t.Run("preferred server feature settings", (t2) =>
        {
            t2.Parallel();
            var prefs = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["js/ts"] = new Dictionary<string, object> { ["validate"] = new Dictionary<string, object> { ["enabled"] = false }, ["format"] = new Dictionary<string, object> { ["enabled"] = false }, ["autoClosingTags"] = new Dictionary<string, object> { ["enabled"] = false } } });
            Assertions.Equal(t2, prefs.ValidateEnabled, Tristate.False);
            Assertions.Equal(t2, prefs.FormatEnabled, Tristate.False);
            Assertions.Equal(t2, prefs.AutoClosingTags, Tristate.False);
        });
        t.Run("legacy server feature fallbacks", (t3) =>
        {
            t3.Parallel();
            var prefs2 = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["typescript"] = new Dictionary<string, object> { ["validate"] = new Dictionary<string, object> { ["enable"] = false }, ["format"] = new Dictionary<string, object> { ["enable"] = false }, ["autoClosingTags"] = false } });
            Assertions.Equal(t3, prefs2.ValidateEnabled, Tristate.False);
            Assertions.Equal(t3, prefs2.FormatEnabled, Tristate.False);
            Assertions.Equal(t3, prefs2.AutoClosingTags, Tristate.False);
        });
        t.Run("preferred settings take precedence over fallbacks", (t4) =>
        {
            t4.Parallel();
            var prefs3 = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["typescript"] = new Dictionary<string, object> { ["validate"] = new Dictionary<string, object> { ["enable"] = false }, ["format"] = new Dictionary<string, object> { ["enable"] = false }, ["autoClosingTags"] = false }, ["js/ts"] = new Dictionary<string, object> { ["validate"] = new Dictionary<string, object> { ["enabled"] = true }, ["format"] = new Dictionary<string, object> { ["enabled"] = true }, ["autoClosingTags"] = new Dictionary<string, object> { ["enabled"] = true } } });
            Assertions.Equal(t4, prefs3.ValidateEnabled, Tristate.True);
            Assertions.Equal(t4, prefs3.FormatEnabled, Tristate.True);
            Assertions.Equal(t4, prefs3.AutoClosingTags, Tristate.True);
        });
    }

    // userpreferences_test.go:619.
    private static void TestParseUserPreferencesEditorFormatting(T t)
    {
        t.Parallel();
        var prefs = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["editor"] = new Dictionary<string, object> { ["tabSize"] = 2, ["insertSpaces"] = false } });
        Assertions.Equal(t, prefs.FormatCodeSettings.TabSize, 2);
        Assertions.Equal(t, prefs.FormatCodeSettings.IndentSize, 2);
        Assertions.Equal(t, prefs.FormatCodeSettings.ConvertTabsToSpaces, Tristate.False);
    }

    // userpreferences_test.go:634.
    private static void TestUserPreferencesParseJSDocCompletionPreferences(T t)
    {
        t.Parallel();
        t.Run("unified jsdoc enabled setting", (t2) =>
        {
            t2.Parallel();
            var prefs = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["js/ts"] = new Dictionary<string, object> { ["suggest"] = new Dictionary<string, object> { ["jsdoc"] = new Dictionary<string, object> { ["enabled"] = false } } } });
            Assertions.Equal(t2, prefs.CompleteJSDocs, Tristate.False);
        });
        t.Run("language fallback completeJSDocs setting", (t3) =>
        {
            t3.Parallel();
            var prefs2 = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["typescript"] = new Dictionary<string, object> { ["suggest"] = new Dictionary<string, object> { ["completeJSDocs"] = false } } });
            Assertions.Equal(t3, prefs2.CompleteJSDocs, Tristate.False);
        });
        t.Run("unified jsdoc enabled takes precedence over language fallback", (t4) =>
        {
            t4.Parallel();
            var prefs3 = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["typescript"] = new Dictionary<string, object> { ["suggest"] = new Dictionary<string, object> { ["completeJSDocs"] = false } }, ["js/ts"] = new Dictionary<string, object> { ["suggest"] = new Dictionary<string, object> { ["jsdoc"] = new Dictionary<string, object> { ["enabled"] = true } } } });
            Assertions.Equal(t4, prefs3.CompleteJSDocs, Tristate.True);
        });
        t.Run("unified jsdoc generateReturns setting", (t5) =>
        {
            t5.Parallel();
            var prefs4 = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["js/ts"] = new Dictionary<string, object> { ["suggest"] = new Dictionary<string, object> { ["jsdoc"] = new Dictionary<string, object> { ["generateReturns"] = false } } } });
            Assertions.Equal(t5, prefs4.GenerateReturnInDocTemplate, Tristate.False);
        });
        t.Run("language jsdoc generateReturns setting", (t6) =>
        {
            t6.Parallel();
            var prefs5 = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["typescript"] = new Dictionary<string, object> { ["suggest"] = new Dictionary<string, object> { ["jsdoc"] = new Dictionary<string, object> { ["generateReturns"] = false } } } });
            Assertions.Equal(t6, prefs5.GenerateReturnInDocTemplate, Tristate.False);
        });
    }

    // userpreferences_test.go:711.
    private static void TestUserPreferencesParseATA(T t)
    {
        t.Parallel();
        t.Run("ParseUserPreferences with unified ATA setting in js/ts section", (t2) =>
        {
            t2.Parallel();
            var prefs = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["js/ts"] = new Dictionary<string, object> { ["tsserver"] = new Dictionary<string, object> { ["automaticTypeAcquisition"] = new Dictionary<string, object> { ["enabled"] = false } } } });
            Assertions.Assert(t2, prefs.IsATADisabled());
            Assertions.Equal(t2, prefs.AutomaticTypeAcquisitionEnabled, Tristate.False);
        });
        t.Run("ParseUserPreferences with deprecated disableAutomaticTypeAcquisition in typescript section", (t3) =>
        {
            t3.Parallel();
            var prefs2 = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["typescript"] = new Dictionary<string, object> { ["disableAutomaticTypeAcquisition"] = true } });
            Assertions.Assert(t3, prefs2.IsATADisabled());
            Assertions.Equal(t3, prefs2.DisableAutomaticTypeAcquisition, Tristate.True);
        });
        t.Run("unified setting takes precedence over deprecated setting", (t4) =>
        {
            t4.Parallel();
            var prefs3 = LsUtil.ParseUserPreferences(new Dictionary<string, object> { ["typescript"] = new Dictionary<string, object> { ["disableAutomaticTypeAcquisition"] = true }, ["js/ts"] = new Dictionary<string, object> { ["tsserver"] = new Dictionary<string, object> { ["automaticTypeAcquisition"] = new Dictionary<string, object> { ["enabled"] = true } } } });
            Assertions.Assert(t4, !prefs3.IsATADisabled());
            Assertions.Equal(t4, prefs3.AutomaticTypeAcquisitionEnabled, Tristate.True);
        });
        t.Run("IsATADisabled returns false when neither setting is configured", (t5) =>
        {
            t5.Parallel();
            var prefs4 = LsUtil.NewDefaultUserPreferences();
            Assertions.Assert(t5, !prefs4.IsATADisabled());
        });
    }

    private sealed class PreferenceJsonCase
    {
        internal string name;
        internal string json;
        internal UserPreferences expected;
    }

    private sealed class T
    {
        internal readonly string Name;
        internal T(string name) => Name = name;
        internal void Parallel()
        {
        }

        internal void Run(string name, Action<T> action) => action(new T(Name + "/" + name));
    }

    private static (string, Exception) Marshal(UserPreferences value) => (value.MarshalJSONTo(), null);
    private static Exception Unmarshal(string value, out Dictionary<string, object> result)
    {
        var parsed = TypeScript.Json.JsonCodec.ParseDictionary(value);
        result = (Dictionary<string, object>)parsed.Item1;
        return parsed.Item2;
    }

    private static class Assertions
    {
        internal static void NilError(T t, Exception error)
        {
            if (error != null)
                throw new InvalidOperationException(t.Name, error);
        }

        internal static void Equal<TValue>(T t, TValue expected, TValue actual)
        {
            if (!EqualityComparer<TValue>.Default.Equals(expected, actual))
                throw new InvalidOperationException(t.Name + ": values differ");
        }

        internal static void Assert(T t, bool value)
        {
            if (!value)
                throw new InvalidOperationException(t.Name);
        }

        internal static void DeepEqual(T t, UserPreferences expected, UserPreferences actual) => CheckPreferences(t.Name, expected, actual);
    }
}

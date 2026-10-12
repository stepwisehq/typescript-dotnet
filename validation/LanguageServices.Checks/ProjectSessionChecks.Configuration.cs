// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestSession preference/locale/configuration assertion groups.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.Utilities;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class ProjectSessionChecks
{
    private static readonly BaselineFS.Input[] SimpleConfigurationFiles = [new("/src/tsconfig.json", "{}"), new("/src/index.ts", "export const x = 1;")];
    private static void ConfigureRefreshOriginalAssertions()
    {
        using var fixture = new ProjectSessionFixture(SimpleConfigurationFiles);
        var session = fixture.Session;
        Open(session, "file:///src/index.ts", "export const x = 1;");
        _ = Service(session, "file:///src/index.ts");
        session.Configure(LsUtil.NewDefaultUserPreferences());
        var prefs = session.Config();
        prefs.CodeLensUserPreferences.ReferencesCodeLensEnabled = Tristate.True;
        prefs.InlayHintsPreferences.IncludeInlayFunctionLikeReturnTypeHints = Tristate.True;
        session.Configure(prefs);
        Require(fixture.Client.CodeLensRefreshCalls().Length == 1, "code-lens preference change must refresh once");
        Require(fixture.Client.InlayHintsRefreshCalls().Length == 1, "inlay-hint preference change must refresh once");
    }

    private static void ConfigureLocaleOriginalAssertions()
    {
        using (var fixture = new ProjectSessionFixture([]))
        {
            var prefs = LsUtil.NewDefaultUserPreferences();
            prefs.Locale = "fr";
            fixture.Session.Configure(prefs);
            var calls = fixture.Client.LocaleCalls();
            Require(calls.Length == 1 && calls[0].EqualsBytes("fr"u8), "locale configuration must call SetLocale with fr once");
        }

        using (var fixture = new ProjectSessionFixture(SimpleConfigurationFiles))
        {
            var session = fixture.Session;
            Open(session, "file:///src/index.ts", "export const x = 1;");
            _ = Service(session, "file:///src/index.ts");
            var initial = session.Snapshot().ProjectCollection.ConfiguredProject(new PathKey("/src/tsconfig.json"))!.Program;
            var prefs = session.Config();
            prefs.CodeLensUserPreferences.ReferencesCodeLensEnabled = Tristate.True;
            session.Configure(prefs);
            _ = Service(session, "file:///src/index.ts");
            Require(ReferenceEquals(initial, session.Snapshot().ProjectCollection.ConfiguredProject(new PathKey("/src/tsconfig.json"))!.Program), "code-lens change must preserve program");
            prefs.Locale = "fr";
            session.Configure(prefs);
            _ = Service(session, "file:///src/index.ts");
            Require(!ReferenceEquals(initial, session.Snapshot().ProjectCollection.ConfiguredProject(new PathKey("/src/tsconfig.json"))!.Program), "locale change must invalidate program");
        }

        using (var fixture = new ProjectSessionFixture([]))
        {
            var(fr, ok) = TypeScript.Locale.Locales.Parse("fr");
            Require(ok, "original French locale must parse");
            fixture.Client.GetLocaleFunc = () => fr;
            fixture.Client.RefreshCodeLensFunc = context =>
            {
                Require(context.Locale.Equals(fr), "background code-lens callback must retain French locale");
                return null;
            };
            var prefs = LsUtil.NewDefaultUserPreferences();
            prefs.CodeLensUserPreferences.ReferencesCodeLensEnabled = Tristate.True;
            fixture.Session.Configure(prefs);
            Require(fixture.Client.CodeLensRefreshCalls().Length == 1, "background code-lens refresh must occur once");
        }
    }

    private static void ConfigureWarningsRefreshOriginalAssertions()
    {
        using var fixture = new ProjectSessionFixture(SimpleConfigurationFiles);
        var session = fixture.Session;
        Open(session, "file:///src/index.ts", "export const x = 1;");
        _ = Service(session, "file:///src/index.ts");
        session.WaitForBackgroundTasks();
        int baseline = fixture.Client.RefreshDiagnosticsCallCount();
        var prefs = LsUtil.NewDefaultUserPreferences();
        prefs.ReportStyleChecksAsWarnings = Tristate.False;
        session.Configure(prefs);
        session.WaitForBackgroundTasks();
        Require(fixture.Client.RefreshDiagnosticsCallCount() > baseline, "warning preference change must schedule diagnostics refresh");
    }

    private static void ConfigurationParsingOriginalAssertions()
    {
        using var fixture = new ProjectSessionFixture(SimpleConfigurationFiles);
        var session = fixture.Session;
        Open(session, "file:///src/index.ts", "export const x = 1;");
        _ = Service(session, "file:///src/index.ts");
        foreach (bool aliases in new[]
        {
            true,
            false
        }

        )
        {
            var configuration = new Dictionary<string, object>
            {
                ["js/ts"] = new Dictionary<string, object>
                {
                    ["preferences"] = new Dictionary<string, object>
                    {
                        ["useAliasesForRenames"] = aliases,
                        ["quoteStyle"] = aliases ? "single" : "double"
                    },
                    ["unstable"] = new Dictionary<string, object>
                    {
                        ["organizeImportsSort"] = aliases ? "ordinalIgnoreCase" : "ordinal"
                    }
                }
            };
            session.Configure(LsUtil.ParseUserPreferences(configuration));
            var expected = LsUtil.NewDefaultUserPreferences();
            expected.ProvidePrefixAndSuffixTextForRename = aliases ? Tristate.True : Tristate.False;
            expected.QuotePreference = aliases ? QuotePreference.Single : QuotePreference.Double;
            expected.OrganizeImportsSort = aliases ? OrganizeImportsSort.OrdinalIgnoreCase : OrganizeImportsSort.Ordinal;
            UserPreferenceChecks.AssertOriginalPreferences("TestSession/config parsing", expected, session.Config());
        }
    }
}

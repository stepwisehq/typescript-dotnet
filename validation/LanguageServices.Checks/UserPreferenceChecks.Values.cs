// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Explicit AOT counterpart of original reflect-based field filling and DeepEqual; every source field participates.
#nullable disable
using TypeScript.Scanner;
using TypeScript.LanguageServices.Utilities;
using TypeScript.ModuleSpecifiers;
using UserPreferences = TypeScript.LanguageServices.Utilities.UserPreferences;

namespace LanguageServices.Checks;
public static partial class UserPreferenceChecks
{
    public static readonly string[] Names = ["TestUserPreferencesParsingEdgeCases", "TestUserPreferencesRoundtrip", "TestUserPreferencesSerialize", "TestUserPreferencesParseUnstable", "TestUserPreferencesLocale", "TestUserPreferencesReportStyleChecksAsWarnings", "TestUserPreferencesParseServerFeaturePreferences", "TestParseUserPreferencesEditorFormatting", "TestUserPreferencesParseJSDocCompletionPreferences", "TestUserPreferencesParseATA"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        var t = new T(name);
        switch (name)
        {
            case "TestUserPreferencesParsingEdgeCases":
                TestUserPreferencesParsingEdgeCases(t);
                break;
            case "TestUserPreferencesRoundtrip":
                TestUserPreferencesRoundtrip(t);
                break;
            case "TestUserPreferencesSerialize":
                TestUserPreferencesSerialize(t);
                break;
            case "TestUserPreferencesParseUnstable":
                TestUserPreferencesParseUnstable(t);
                break;
            case "TestUserPreferencesLocale":
                TestUserPreferencesLocale(t);
                break;
            case "TestUserPreferencesReportStyleChecksAsWarnings":
                TestUserPreferencesReportStyleChecksAsWarnings(t);
                break;
            case "TestUserPreferencesParseServerFeaturePreferences":
                TestUserPreferencesParseServerFeaturePreferences(t);
                break;
            case "TestParseUserPreferencesEditorFormatting":
                TestParseUserPreferencesEditorFormatting(t);
                break;
            case "TestUserPreferencesParseJSDocCompletionPreferences":
                TestUserPreferencesParseJSDocCompletionPreferences(t);
                break;
            case "TestUserPreferencesParseATA":
                TestUserPreferencesParseATA(t);
                break;
            default:
                throw new ArgumentException(name);
        }
    }

    // Authored public owner-width witness; distinct from the ten original functions.
    public static int RunAuthoredControls()
    {
        const long large = 4294967297;
        var preferences = default(UserPreferences).WithConfig(new Dictionary<string, object> { ["maximumHoverLength"] = (double)large, ["indentStyle"] = (double)large, ["tabSize"] = large });
        if (preferences.MaximumHoverLength != large || (long)preferences.IndentStyle != large || preferences.TabSize != large)
            throw new InvalidOperationException("Go64 preference values were narrowed at the storage boundary");
        var parsed = default(UserPreferences);
        parsed.UnmarshalJSONFrom(preferences.MarshalJSONTo());
        if (parsed.MaximumHoverLength != large || (long)parsed.IndentStyle != large || parsed.TabSize != large)
            throw new InvalidOperationException("Go64 preference values were narrowed at the JSON boundary");
        return 1;
    }

    private static void CheckPreferences(string name, UserPreferences expected, UserPreferences actual)
    {
        if (!EqualityComparer<long>.Default.Equals(expected.BaseIndentSize, actual.BaseIndentSize))
            throw new InvalidOperationException(name + ": BaseIndentSize");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.ConvertTabsToSpaces, actual.ConvertTabsToSpaces))
            throw new InvalidOperationException(name + ": ConvertTabsToSpaces");
        if (!EqualityComparer<long>.Default.Equals(expected.IndentSize, actual.IndentSize))
            throw new InvalidOperationException(name + ": IndentSize");
        if (!EqualityComparer<IndentStyle>.Default.Equals(expected.IndentStyle, actual.IndentStyle))
            throw new InvalidOperationException(name + ": IndentStyle");
        if (!EqualityComparer<string>.Default.Equals(expected.NewLineCharacter, actual.NewLineCharacter))
            throw new InvalidOperationException(name + ": NewLineCharacter");
        if (!EqualityComparer<long>.Default.Equals(expected.TabSize, actual.TabSize))
            throw new InvalidOperationException(name + ": TabSize");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.TrimTrailingWhitespace, actual.TrimTrailingWhitespace))
            throw new InvalidOperationException(name + ": TrimTrailingWhitespace");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IndentMultiLineObjectLiteralBeginningOnBlankLine, actual.IndentMultiLineObjectLiteralBeginningOnBlankLine))
            throw new InvalidOperationException(name + ": IndentMultiLineObjectLiteralBeginningOnBlankLine");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IndentSwitchCase, actual.IndentSwitchCase))
            throw new InvalidOperationException(name + ": IndentSwitchCase");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceAfterCommaDelimiter, actual.InsertSpaceAfterCommaDelimiter))
            throw new InvalidOperationException(name + ": InsertSpaceAfterCommaDelimiter");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceAfterConstructor, actual.InsertSpaceAfterConstructor))
            throw new InvalidOperationException(name + ": InsertSpaceAfterConstructor");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceAfterFunctionKeywordForAnonymousFunctions, actual.InsertSpaceAfterFunctionKeywordForAnonymousFunctions))
            throw new InvalidOperationException(name + ": InsertSpaceAfterFunctionKeywordForAnonymousFunctions");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceAfterKeywordsInControlFlowStatements, actual.InsertSpaceAfterKeywordsInControlFlowStatements))
            throw new InvalidOperationException(name + ": InsertSpaceAfterKeywordsInControlFlowStatements");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces, actual.InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces))
            throw new InvalidOperationException(name + ": InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces, actual.InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces))
            throw new InvalidOperationException(name + ": InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces, actual.InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces))
            throw new InvalidOperationException(name + ": InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets, actual.InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets))
            throw new InvalidOperationException(name + ": InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis, actual.InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis))
            throw new InvalidOperationException(name + ": InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces, actual.InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces))
            throw new InvalidOperationException(name + ": InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceAfterSemicolonInForStatements, actual.InsertSpaceAfterSemicolonInForStatements))
            throw new InvalidOperationException(name + ": InsertSpaceAfterSemicolonInForStatements");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceAfterTypeAssertion, actual.InsertSpaceAfterTypeAssertion))
            throw new InvalidOperationException(name + ": InsertSpaceAfterTypeAssertion");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceBeforeAndAfterBinaryOperators, actual.InsertSpaceBeforeAndAfterBinaryOperators))
            throw new InvalidOperationException(name + ": InsertSpaceBeforeAndAfterBinaryOperators");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceBeforeFunctionParenthesis, actual.InsertSpaceBeforeFunctionParenthesis))
            throw new InvalidOperationException(name + ": InsertSpaceBeforeFunctionParenthesis");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.InsertSpaceBeforeTypeAnnotation, actual.InsertSpaceBeforeTypeAnnotation))
            throw new InvalidOperationException(name + ": InsertSpaceBeforeTypeAnnotation");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.PlaceOpenBraceOnNewLineForControlBlocks, actual.PlaceOpenBraceOnNewLineForControlBlocks))
            throw new InvalidOperationException(name + ": PlaceOpenBraceOnNewLineForControlBlocks");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.PlaceOpenBraceOnNewLineForFunctions, actual.PlaceOpenBraceOnNewLineForFunctions))
            throw new InvalidOperationException(name + ": PlaceOpenBraceOnNewLineForFunctions");
        if (!EqualityComparer<SemicolonPreference>.Default.Equals(expected.Semicolons, actual.Semicolons))
            throw new InvalidOperationException(name + ": Semicolons");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IncludeAutomaticOptionalChainCompletions, actual.IncludeAutomaticOptionalChainCompletions))
            throw new InvalidOperationException(name + ": IncludeAutomaticOptionalChainCompletions");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IncludeCompletionsForImportStatements, actual.IncludeCompletionsForImportStatements))
            throw new InvalidOperationException(name + ": IncludeCompletionsForImportStatements");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IncludeCompletionsForModuleExports, actual.IncludeCompletionsForModuleExports))
            throw new InvalidOperationException(name + ": IncludeCompletionsForModuleExports");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IncludeCompletionsWithClassMemberSnippets, actual.IncludeCompletionsWithClassMemberSnippets))
            throw new InvalidOperationException(name + ": IncludeCompletionsWithClassMemberSnippets");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IncludeCompletionsWithObjectLiteralMethodSnippets, actual.IncludeCompletionsWithObjectLiteralMethodSnippets))
            throw new InvalidOperationException(name + ": IncludeCompletionsWithObjectLiteralMethodSnippets");
        if (!EqualityComparer<JsxAttributeCompletionStyle>.Default.Equals(expected.JsxAttributeCompletionStyle, actual.JsxAttributeCompletionStyle))
            throw new InvalidOperationException(name + ": JsxAttributeCompletionStyle");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IncludeInlayEnumMemberValueHints, actual.IncludeInlayEnumMemberValueHints))
            throw new InvalidOperationException(name + ": IncludeInlayEnumMemberValueHints");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IncludeInlayFunctionLikeReturnTypeHints, actual.IncludeInlayFunctionLikeReturnTypeHints))
            throw new InvalidOperationException(name + ": IncludeInlayFunctionLikeReturnTypeHints");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IncludeInlayFunctionParameterTypeHints, actual.IncludeInlayFunctionParameterTypeHints))
            throw new InvalidOperationException(name + ": IncludeInlayFunctionParameterTypeHints");
        if (!EqualityComparer<IncludeInlayParameterNameHints>.Default.Equals(expected.IncludeInlayParameterNameHints, actual.IncludeInlayParameterNameHints))
            throw new InvalidOperationException(name + ": IncludeInlayParameterNameHints");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IncludeInlayParameterNameHintsWhenArgumentMatchesName, actual.IncludeInlayParameterNameHintsWhenArgumentMatchesName))
            throw new InvalidOperationException(name + ": IncludeInlayParameterNameHintsWhenArgumentMatchesName");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IncludeInlayPropertyDeclarationTypeHints, actual.IncludeInlayPropertyDeclarationTypeHints))
            throw new InvalidOperationException(name + ": IncludeInlayPropertyDeclarationTypeHints");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IncludeInlayVariableTypeHints, actual.IncludeInlayVariableTypeHints))
            throw new InvalidOperationException(name + ": IncludeInlayVariableTypeHints");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.IncludeInlayVariableTypeHintsWhenTypeMatchesName, actual.IncludeInlayVariableTypeHintsWhenTypeMatchesName))
            throw new InvalidOperationException(name + ": IncludeInlayVariableTypeHintsWhenTypeMatchesName");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.ImplementationsCodeLensEnabled, actual.ImplementationsCodeLensEnabled))
            throw new InvalidOperationException(name + ": ImplementationsCodeLensEnabled");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.ImplementationsCodeLensShowOnAllClassMethods, actual.ImplementationsCodeLensShowOnAllClassMethods))
            throw new InvalidOperationException(name + ": ImplementationsCodeLensShowOnAllClassMethods");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.ImplementationsCodeLensShowOnInterfaceMethods, actual.ImplementationsCodeLensShowOnInterfaceMethods))
            throw new InvalidOperationException(name + ": ImplementationsCodeLensShowOnInterfaceMethods");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.ReferencesCodeLensEnabled, actual.ReferencesCodeLensEnabled))
            throw new InvalidOperationException(name + ": ReferencesCodeLensEnabled");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.ReferencesCodeLensShowOnAllFunctions, actual.ReferencesCodeLensShowOnAllFunctions))
            throw new InvalidOperationException(name + ": ReferencesCodeLensShowOnAllFunctions");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.AllowRenameOfImportPath, actual.AllowRenameOfImportPath))
            throw new InvalidOperationException(name + ": AllowRenameOfImportPath");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.AllowTextChangesInNewFiles, actual.AllowTextChangesInNewFiles))
            throw new InvalidOperationException(name + ": AllowTextChangesInNewFiles");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.AutoClosingTags, actual.AutoClosingTags))
            throw new InvalidOperationException(name + ": AutoClosingTags");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.AutoImportEntrypointDirectorySearch, actual.AutoImportEntrypointDirectorySearch))
            throw new InvalidOperationException(name + ": AutoImportEntrypointDirectorySearch");
        if (expected.AutoImportFileExcludePatterns.IsNil != actual.AutoImportFileExcludePatterns.IsNil || !expected.AutoImportFileExcludePatterns.AsSpan().SequenceEqual(actual.AutoImportFileExcludePatterns.AsSpan()))
            throw new InvalidOperationException(name + ": AutoImportFileExcludePatterns");
        if (expected.AutoImportSpecifierExcludeRegexes.IsNil != actual.AutoImportSpecifierExcludeRegexes.IsNil || !expected.AutoImportSpecifierExcludeRegexes.AsSpan().SequenceEqual(actual.AutoImportSpecifierExcludeRegexes.AsSpan()))
            throw new InvalidOperationException(name + ": AutoImportSpecifierExcludeRegexes");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.AutomaticTypeAcquisitionEnabled, actual.AutomaticTypeAcquisitionEnabled))
            throw new InvalidOperationException(name + ": AutomaticTypeAcquisitionEnabled");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.CompleteJSDocs, actual.CompleteJSDocs))
            throw new InvalidOperationException(name + ": CompleteJSDocs");
        if (!EqualityComparer<string>.Default.Equals(expected.CustomConfigFileName, actual.CustomConfigFileName))
            throw new InvalidOperationException(name + ": CustomConfigFileName");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.DisableAutomaticTypeAcquisition, actual.DisableAutomaticTypeAcquisition))
            throw new InvalidOperationException(name + ": DisableAutomaticTypeAcquisition");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.DisableLineTextInReferences, actual.DisableLineTextInReferences))
            throw new InvalidOperationException(name + ": DisableLineTextInReferences");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.DisableSuggestions, actual.DisableSuggestions))
            throw new InvalidOperationException(name + ": DisableSuggestions");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.DisplayPartsForJSDoc, actual.DisplayPartsForJSDoc))
            throw new InvalidOperationException(name + ": DisplayPartsForJSDoc");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.ExcludeLibrarySymbolsInNavTo, actual.ExcludeLibrarySymbolsInNavTo))
            throw new InvalidOperationException(name + ": ExcludeLibrarySymbolsInNavTo");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.FormatEnabled, actual.FormatEnabled))
            throw new InvalidOperationException(name + ": FormatEnabled");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.GenerateReturnInDocTemplate, actual.GenerateReturnInDocTemplate))
            throw new InvalidOperationException(name + ": GenerateReturnInDocTemplate");
        if (!EqualityComparer<ImportModuleSpecifierEndingPreference>.Default.Equals(expected.ImportModuleSpecifierEnding, actual.ImportModuleSpecifierEnding))
            throw new InvalidOperationException(name + ": ImportModuleSpecifierEnding");
        if (!EqualityComparer<ImportModuleSpecifierPreference>.Default.Equals(expected.ImportModuleSpecifierPreference, actual.ImportModuleSpecifierPreference))
            throw new InvalidOperationException(name + ": ImportModuleSpecifierPreference");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.LazyConfiguredProjectsFromExternalProject, actual.LazyConfiguredProjectsFromExternalProject))
            throw new InvalidOperationException(name + ": LazyConfiguredProjectsFromExternalProject");
        if (!EqualityComparer<string>.Default.Equals(expected.Locale, actual.Locale))
            throw new InvalidOperationException(name + ": Locale");
        if (!EqualityComparer<long>.Default.Equals(expected.MaximumHoverLength, actual.MaximumHoverLength))
            throw new InvalidOperationException(name + ": MaximumHoverLength");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.OrganizeImportsAccentCollation, actual.OrganizeImportsAccentCollation))
            throw new InvalidOperationException(name + ": OrganizeImportsAccentCollation");
        if (!EqualityComparer<OrganizeImportsCaseFirst>.Default.Equals(expected.OrganizeImportsCaseFirst, actual.OrganizeImportsCaseFirst))
            throw new InvalidOperationException(name + ": OrganizeImportsCaseFirst");
        if (!EqualityComparer<OrganizeImportsCollation>.Default.Equals(expected.OrganizeImportsCollation, actual.OrganizeImportsCollation))
            throw new InvalidOperationException(name + ": OrganizeImportsCollation");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.OrganizeImportsIgnoreCase, actual.OrganizeImportsIgnoreCase))
            throw new InvalidOperationException(name + ": OrganizeImportsIgnoreCase");
        if (!EqualityComparer<string>.Default.Equals(expected.OrganizeImportsLocale, actual.OrganizeImportsLocale))
            throw new InvalidOperationException(name + ": OrganizeImportsLocale");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.OrganizeImportsNumericCollation, actual.OrganizeImportsNumericCollation))
            throw new InvalidOperationException(name + ": OrganizeImportsNumericCollation");
        if (!EqualityComparer<OrganizeImportsSort>.Default.Equals(expected.OrganizeImportsSort, actual.OrganizeImportsSort))
            throw new InvalidOperationException(name + ": OrganizeImportsSort");
        if (!EqualityComparer<OrganizeImportsTypeOrder>.Default.Equals(expected.OrganizeImportsTypeOrder, actual.OrganizeImportsTypeOrder))
            throw new InvalidOperationException(name + ": OrganizeImportsTypeOrder");
        if (!EqualityComparer<bool>.Default.Equals(expected.PreferGoToSourceDefinition, actual.PreferGoToSourceDefinition))
            throw new InvalidOperationException(name + ": PreferGoToSourceDefinition");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.PreferTypeOnlyAutoImports, actual.PreferTypeOnlyAutoImports))
            throw new InvalidOperationException(name + ": PreferTypeOnlyAutoImports");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.ProvidePrefixAndSuffixTextForRename, actual.ProvidePrefixAndSuffixTextForRename))
            throw new InvalidOperationException(name + ": ProvidePrefixAndSuffixTextForRename");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.ProvideRefactorNotApplicableReason, actual.ProvideRefactorNotApplicableReason))
            throw new InvalidOperationException(name + ": ProvideRefactorNotApplicableReason");
        if (!EqualityComparer<QuotePreference>.Default.Equals(expected.QuotePreference, actual.QuotePreference))
            throw new InvalidOperationException(name + ": QuotePreference");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.ReportStyleChecksAsWarnings, actual.ReportStyleChecksAsWarnings))
            throw new InvalidOperationException(name + ": ReportStyleChecksAsWarnings");
        if (!EqualityComparer<Tristate>.Default.Equals(expected.ValidateEnabled, actual.ValidateEnabled))
            throw new InvalidOperationException(name + ": ValidateEnabled");
        if (!EqualityComparer<WorkspaceSymbolsScope>.Default.Equals(expected.WorkspaceSymbolsScope, actual.WorkspaceSymbolsScope))
            throw new InvalidOperationException(name + ": WorkspaceSymbolsScope");
    }

    // userpreferences_test.go:150; fillNonZeroValues/getValidStringValue use explicit source-field assignments under AOT.
    private static void TestUserPreferencesRoundtrip(T t)
    {
        var original = new UserPreferences
        {
            BaseIndentSize = (int)1,
            ConvertTabsToSpaces = (Tristate)1,
            IndentSize = (int)1,
            IndentStyle = (IndentStyle)1,
            NewLineCharacter = "test",
            TabSize = (int)1,
            TrimTrailingWhitespace = (Tristate)1,
            IndentMultiLineObjectLiteralBeginningOnBlankLine = (Tristate)1,
            IndentSwitchCase = (Tristate)1,
            InsertSpaceAfterCommaDelimiter = (Tristate)1,
            InsertSpaceAfterConstructor = (Tristate)1,
            InsertSpaceAfterFunctionKeywordForAnonymousFunctions = (Tristate)1,
            InsertSpaceAfterKeywordsInControlFlowStatements = (Tristate)1,
            InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces = (Tristate)1,
            InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces = (Tristate)1,
            InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces = (Tristate)1,
            InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets = (Tristate)1,
            InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis = (Tristate)1,
            InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces = (Tristate)1,
            InsertSpaceAfterSemicolonInForStatements = (Tristate)1,
            InsertSpaceAfterTypeAssertion = (Tristate)1,
            InsertSpaceBeforeAndAfterBinaryOperators = (Tristate)1,
            InsertSpaceBeforeFunctionParenthesis = (Tristate)1,
            InsertSpaceBeforeTypeAnnotation = (Tristate)1,
            PlaceOpenBraceOnNewLineForControlBlocks = (Tristate)1,
            PlaceOpenBraceOnNewLineForFunctions = (Tristate)1,
            Semicolons = SemicolonPreference.Insert,
            IncludeAutomaticOptionalChainCompletions = (Tristate)1,
            IncludeCompletionsForImportStatements = (Tristate)1,
            IncludeCompletionsForModuleExports = (Tristate)1,
            IncludeCompletionsWithClassMemberSnippets = (Tristate)1,
            IncludeCompletionsWithObjectLiteralMethodSnippets = (Tristate)1,
            JsxAttributeCompletionStyle = JsxAttributeCompletionStyle.Braces,
            IncludeInlayEnumMemberValueHints = (Tristate)1,
            IncludeInlayFunctionLikeReturnTypeHints = (Tristate)1,
            IncludeInlayFunctionParameterTypeHints = (Tristate)1,
            IncludeInlayParameterNameHints = IncludeInlayParameterNameHints.All,
            IncludeInlayParameterNameHintsWhenArgumentMatchesName = (Tristate)1,
            IncludeInlayPropertyDeclarationTypeHints = (Tristate)1,
            IncludeInlayVariableTypeHints = (Tristate)1,
            IncludeInlayVariableTypeHintsWhenTypeMatchesName = (Tristate)1,
            ImplementationsCodeLensEnabled = (Tristate)1,
            ImplementationsCodeLensShowOnAllClassMethods = (Tristate)1,
            ImplementationsCodeLensShowOnInterfaceMethods = (Tristate)1,
            ReferencesCodeLensEnabled = (Tristate)1,
            ReferencesCodeLensShowOnAllFunctions = (Tristate)1,
            AllowRenameOfImportPath = (Tristate)1,
            AllowTextChangesInNewFiles = (Tristate)1,
            AutoClosingTags = (Tristate)1,
            AutoImportEntrypointDirectorySearch = (Tristate)1,
            AutoImportFileExcludePatterns = Slice<string>.FromArray(["test"]),
            AutoImportSpecifierExcludeRegexes = Slice<string>.FromArray(["test"]),
            AutomaticTypeAcquisitionEnabled = (Tristate)1,
            CompleteJSDocs = (Tristate)1,
            CustomConfigFileName = "test",
            DisableAutomaticTypeAcquisition = (Tristate)1,
            DisableLineTextInReferences = (Tristate)1,
            DisableSuggestions = (Tristate)1,
            DisplayPartsForJSDoc = (Tristate)1,
            ExcludeLibrarySymbolsInNavTo = (Tristate)1,
            FormatEnabled = (Tristate)1,
            GenerateReturnInDocTemplate = (Tristate)1,
            ImportModuleSpecifierEnding = ImportModuleSpecifierEndingPreference.Js,
            ImportModuleSpecifierPreference = ImportModuleSpecifierPreference.Relative,
            LazyConfiguredProjectsFromExternalProject = (Tristate)1,
            Locale = "test",
            MaximumHoverLength = (int)1,
            OrganizeImportsAccentCollation = (Tristate)1,
            OrganizeImportsCaseFirst = (OrganizeImportsCaseFirst)1,
            OrganizeImportsCollation = (OrganizeImportsCollation)1,
            OrganizeImportsIgnoreCase = (Tristate)1,
            OrganizeImportsLocale = "test",
            OrganizeImportsNumericCollation = (Tristate)1,
            OrganizeImportsSort = (OrganizeImportsSort)1,
            OrganizeImportsTypeOrder = (OrganizeImportsTypeOrder)1,
            PreferGoToSourceDefinition = true,
            PreferTypeOnlyAutoImports = (Tristate)1,
            ProvidePrefixAndSuffixTextForRename = (Tristate)1,
            ProvideRefactorNotApplicableReason = (Tristate)1,
            QuotePreference = QuotePreference.Single,
            ReportStyleChecksAsWarnings = (Tristate)1,
            ValidateEnabled = (Tristate)1,
            WorkspaceSymbolsScope = WorkspaceSymbolsScope.AllOpenProjects,
        };
        var encoded = original.MarshalJSONTo();
        t.Run("UnmarshalJSONFrom", child =>
        {
            UserPreferences parsed = default;
            parsed.UnmarshalJSONFrom(encoded);
            CheckPreferences(child.Name, original, parsed);
        });
        t.Run("withConfig", child =>
        {
            var(config, error) = TypeScript.Json.JsonCodec.ParseDictionary(encoded);
            Assertions.NilError(child, error);
            var parsed = default(UserPreferences).WithConfig((Dictionary<string, object>)config);
            CheckPreferences(child.Name, original, parsed);
        });
    }

    // userpreferences_test.go:13. All eight original table rows and full preference equality.
    private static void TestUserPreferencesParsingEdgeCases(T t)
    {
        var baseline = LsUtil.NewDefaultUserPreferences();
        baseline.QuotePreference = QuotePreference.Double;
        baseline.MaximumHoverLength = 17;
        baseline.OrganizeImportsIgnoreCase = Tristate.True;
        void Row(string name, Dictionary<string, object> config, UserPreferences expected) => CheckPreferences(t.Name + "/" + name, expected, baseline.WithConfig(config));
        Row("null raw values leave preferences unchanged", new() { ["quotePreference"] = null, ["maximumHoverLength"] = null, ["includeCompletionsForModuleExports"] = null }, baseline);
        var expected = baseline;
        expected.IncludeCompletionsForModuleExports = Tristate.Unknown;
        Row("invalid boolean becomes unknown", new() { ["includeCompletionsForModuleExports"] = "invalid" }, expected);
        expected = baseline;
        expected.QuotePreference = QuotePreference.Single;
        expected.JsxAttributeCompletionStyle = JsxAttributeCompletionStyle.Braces;
        expected.OrganizeImportsCaseFirst = OrganizeImportsCaseFirst.Lower;
        expected.IncludeInlayParameterNameHints = IncludeInlayParameterNameHints.All;
        expected.OrganizeImportsTypeOrder = OrganizeImportsTypeOrder.First;
        expected.WorkspaceSymbolsScope = WorkspaceSymbolsScope.CurrentProject;
        Row("case-insensitive enums", new() { ["quotePreference"] = "SINGLE", ["jsxAttributeCompletionStyle"] = "BRACES", ["organizeImportsCaseFirst"] = "LOWER", ["includeInlayParameterNameHints"] = "ALL", ["organizeImportsTypeOrder"] = "FIRST", ["workspaceSymbolsScope"] = "CURRENTPROJECT" }, expected);
        Row("present null primary path prevents fallback", new() { ["suggest"] = new Dictionary<string, object> { ["jsdoc"] = new Dictionary<string, object> { ["enabled"] = null }, ["completeJSDocs"] = false } }, baseline);
        expected = baseline;
        expected.OrganizeImportsIgnoreCase = Tristate.Unknown;
        Row("null case sensitivity becomes unknown", new() { ["preferences"] = new Dictionary<string, object> { ["organizeImports"] = new Dictionary<string, object> { ["caseSensitivity"] = null } } }, expected);
        expected = baseline;
        expected.MaximumHoverLength = 9;
        expected.IndentStyle = IndentStyle.Block;
        expected.AutoImportFileExcludePatterns = Slice<string>.FromArray(["first", "second"]);
        Row("numeric conversion and array filtering", new() { ["maximumHoverLength"] = 9.8, ["indentStyle"] = 1.7, ["autoImportFileExcludePatterns"] = new List<object> { "first", false, 3, "second" } }, expected);
        expected = baseline;
        expected.AutoImportFileExcludePatterns = Slice<string>.FromArray([]);
        Row("empty array is not nil", new() { ["autoImportFileExcludePatterns"] = new List<object>() }, expected);
        expected = baseline;
        expected.ImportModuleSpecifierPreference = ImportModuleSpecifierPreference.Shortest;
        Row("invalid module specifier preference uses its default", new() { ["importModuleSpecifierPreference"] = true }, expected);
    }
}

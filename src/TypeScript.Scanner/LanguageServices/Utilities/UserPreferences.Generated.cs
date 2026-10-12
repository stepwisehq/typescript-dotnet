// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned internal/ls/lsutil/userpreferences_generated.go configuration dispatch.
#nullable disable
using TypeScript.Scanner;
using TypeScript.ModuleSpecifiers;

namespace TypeScript.LanguageServices.Utilities;
internal static partial class LsUtil
{
    // userpreferences_generated.go:179.
    internal static IncludeInlayParameterNameHints parsePreferenceIncludeInlayParameterNameHints(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "none":
                    return IncludeInlayParameterNameHints.None;
                case "literals":
                    return IncludeInlayParameterNameHints.Literals;
                case "all":
                    return IncludeInlayParameterNameHints.All;
            }
        }

        return IncludeInlayParameterNameHints.None;
    }

    // userpreferences_generated.go:193.
    internal static IndentStyle parsePreferenceIndentStyle(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "none":
                    return IndentStyle.None;
                case "block":
                    return IndentStyle.Block;
                case "smart":
                    return IndentStyle.Smart;
            }
        }

        if (value is long integer)
            return (IndentStyle)integer;
        if (value is int smallInteger)
            return (IndentStyle)smallInteger;
        if (value is double number)
            return (IndentStyle)unchecked((long)number);
        return IndentStyle.Smart;
    }

    // userpreferences_generated.go:213.
    internal static JsxAttributeCompletionStyle parsePreferenceJsxAttributeCompletionStyle(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "auto":
                    return JsxAttributeCompletionStyle.Auto;
                case "braces":
                    return JsxAttributeCompletionStyle.Braces;
                case "none":
                    return JsxAttributeCompletionStyle.None;
            }
        }

        return JsxAttributeCompletionStyle.Auto;
    }

    // userpreferences_generated.go:227.
    internal static OrganizeImportsCaseFirst parsePreferenceOrganizeImportsCaseFirst(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "default":
                    return OrganizeImportsCaseFirst.Default;
                case "lower":
                    return OrganizeImportsCaseFirst.Lower;
                case "upper":
                    return OrganizeImportsCaseFirst.Upper;
            }
        }

        return OrganizeImportsCaseFirst.Default;
    }

    // userpreferences_generated.go:254.
    internal static OrganizeImportsCollation parsePreferenceOrganizeImportsCollation(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "ordinal":
                    return OrganizeImportsCollation.Ordinal;
                case "unicode":
                    return OrganizeImportsCollation.Unicode;
            }
        }

        return OrganizeImportsCollation.Ordinal;
    }

    // userpreferences_generated.go:277.
    internal static OrganizeImportsSort parsePreferenceOrganizeImportsSort(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "auto":
                    return OrganizeImportsSort.Auto;
                case "ordinal":
                    return OrganizeImportsSort.Ordinal;
                case "ordinalignorecase":
                    return OrganizeImportsSort.OrdinalIgnoreCase;
                case "natural":
                    return OrganizeImportsSort.Natural;
                case "naturalignorecase":
                    return OrganizeImportsSort.NaturalIgnoreCase;
            }
        }

        return OrganizeImportsSort.Auto;
    }

    // userpreferences_generated.go:312.
    internal static OrganizeImportsTypeOrder parsePreferenceOrganizeImportsTypeOrder(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "auto":
                    return OrganizeImportsTypeOrder.Auto;
                case "last":
                    return OrganizeImportsTypeOrder.Last;
                case "inline":
                    return OrganizeImportsTypeOrder.Inline;
                case "first":
                    return OrganizeImportsTypeOrder.First;
            }
        }

        return OrganizeImportsTypeOrder.Auto;
    }

    // userpreferences_generated.go:343.
    internal static QuotePreference parsePreferenceQuotePreference(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "auto":
                    return QuotePreference.Auto;
                case "double":
                    return QuotePreference.Double;
                case "single":
                    return QuotePreference.Single;
            }
        }

        return default;
    }

    // userpreferences_generated.go:357.
    internal static SemicolonPreference parsePreferenceSemicolonPreference(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "ignore":
                    return SemicolonPreference.Ignore;
                case "insert":
                    return SemicolonPreference.Insert;
                case "remove":
                    return SemicolonPreference.Remove;
            }
        }

        return SemicolonPreference.Ignore;
    }

    // userpreferences_generated.go:371.
    internal static WorkspaceSymbolsScope parsePreferenceWorkspaceSymbolsScope(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "allopenprojects":
                    return WorkspaceSymbolsScope.AllOpenProjects;
                case "currentproject":
                    return WorkspaceSymbolsScope.CurrentProject;
            }
        }

        return default;
    }

    // userpreferences_generated.go:383.
    internal static ImportModuleSpecifierEndingPreference parsePreferenceImportModuleSpecifierEndingPreference(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "auto":
                    return ImportModuleSpecifierEndingPreference.Auto;
                case "minimal":
                    return ImportModuleSpecifierEndingPreference.Minimal;
                case "index":
                    return ImportModuleSpecifierEndingPreference.Index;
                case "js":
                    return ImportModuleSpecifierEndingPreference.Js;
            }
        }

        return ImportModuleSpecifierEndingPreference.Auto;
    }

    // userpreferences_generated.go:399.
    internal static ImportModuleSpecifierPreference parsePreferenceImportModuleSpecifierPreference(object value)
    {
        if (value is string text)
        {
            switch (GoStrings.ToLower(ByteStringValue.FromString(text)).Materialize())
            {
                case "shortest":
                    return ImportModuleSpecifierPreference.Shortest;
                case "project-relative":
                    return ImportModuleSpecifierPreference.ProjectRelative;
                case "relative":
                    return ImportModuleSpecifierPreference.Relative;
                case "non-relative":
                    return ImportModuleSpecifierPreference.NonRelative;
            }
        }

        return ImportModuleSpecifierPreference.Shortest;
    }

    // userpreferences_generated.go:241.
    internal static string serializePreferenceOrganizeImportsCaseFirst(OrganizeImportsCaseFirst value) => value switch
    {
        OrganizeImportsCaseFirst.Default => "default",
        OrganizeImportsCaseFirst.Lower => "lower",
        OrganizeImportsCaseFirst.Upper => "upper",
        _ => "default"
    };
    // userpreferences_generated.go:266.
    internal static string serializePreferenceOrganizeImportsCollation(OrganizeImportsCollation value) => value switch
    {
        OrganizeImportsCollation.Ordinal => "ordinal",
        OrganizeImportsCollation.Unicode => "unicode",
        _ => "ordinal"
    };
    // userpreferences_generated.go:295.
    internal static string serializePreferenceOrganizeImportsSort(OrganizeImportsSort value) => value switch
    {
        OrganizeImportsSort.Auto => "auto",
        OrganizeImportsSort.Ordinal => "ordinal",
        OrganizeImportsSort.OrdinalIgnoreCase => "ordinalIgnoreCase",
        OrganizeImportsSort.Natural => "natural",
        OrganizeImportsSort.NaturalIgnoreCase => "naturalIgnoreCase",
        _ => "auto"
    };
    // userpreferences_generated.go:328.
    internal static string serializePreferenceOrganizeImportsTypeOrder(OrganizeImportsTypeOrder value) => value switch
    {
        OrganizeImportsTypeOrder.Auto => "auto",
        OrganizeImportsTypeOrder.Last => "last",
        OrganizeImportsTypeOrder.Inline => "inline",
        OrganizeImportsTypeOrder.First => "first",
        _ => "auto"
    };
}

internal partial struct UserPreferences
{
    // userpreferences_generated.go:415. Original raw-name dispatch and nil-value admission.
    internal void ApplyRawPreferences(Dictionary<string, object> settings)
    {
        if (settings == null)
            return;
        foreach (var(name, value)in settings)
        {
            if (value == null)
                continue;
            switch (name)
            {
                case "baseIndentSize":
                {
                    if (value is long integer)
                        BaseIndentSize = integer;
                    else if (value is int smallInteger)
                        BaseIndentSize = smallInteger;
                    else if (value is double number)
                        BaseIndentSize = unchecked((long)number);
                    break;
                }

                case "convertTabsToSpaces":
                {
                    ConvertTabsToSpaces = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "indentSize":
                {
                    if (value is long integer)
                        IndentSize = integer;
                    else if (value is int smallInteger)
                        IndentSize = smallInteger;
                    else if (value is double number)
                        IndentSize = unchecked((long)number);
                    break;
                }

                case "indentStyle":
                {
                    IndentStyle = LsUtil.parsePreferenceIndentStyle(value);
                    break;
                }

                case "newLineCharacter":
                {
                    if (value is string text)
                        NewLineCharacter = text;
                    break;
                }

                case "tabSize":
                {
                    if (value is long integer)
                        TabSize = integer;
                    else if (value is int smallInteger)
                        TabSize = smallInteger;
                    else if (value is double number)
                        TabSize = unchecked((long)number);
                    break;
                }

                case "trimTrailingWhitespace":
                {
                    TrimTrailingWhitespace = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "indentMultiLineObjectLiteralBeginningOnBlankLine":
                {
                    IndentMultiLineObjectLiteralBeginningOnBlankLine = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "indentSwitchCase":
                {
                    IndentSwitchCase = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceAfterCommaDelimiter":
                {
                    InsertSpaceAfterCommaDelimiter = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceAfterConstructor":
                {
                    InsertSpaceAfterConstructor = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceAfterFunctionKeywordForAnonymousFunctions":
                {
                    InsertSpaceAfterFunctionKeywordForAnonymousFunctions = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceAfterKeywordsInControlFlowStatements":
                {
                    InsertSpaceAfterKeywordsInControlFlowStatements = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceAfterOpeningAndBeforeClosingEmptyBraces":
                {
                    InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces":
                {
                    InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceAfterOpeningAndBeforeClosingNonemptyBraces":
                {
                    InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets":
                {
                    InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis":
                {
                    InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces":
                {
                    InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceAfterSemicolonInForStatements":
                {
                    InsertSpaceAfterSemicolonInForStatements = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceAfterTypeAssertion":
                {
                    InsertSpaceAfterTypeAssertion = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceBeforeAndAfterBinaryOperators":
                {
                    InsertSpaceBeforeAndAfterBinaryOperators = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceBeforeFunctionParenthesis":
                {
                    InsertSpaceBeforeFunctionParenthesis = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "insertSpaceBeforeTypeAnnotation":
                {
                    InsertSpaceBeforeTypeAnnotation = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "placeOpenBraceOnNewLineForControlBlocks":
                {
                    PlaceOpenBraceOnNewLineForControlBlocks = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "placeOpenBraceOnNewLineForFunctions":
                {
                    PlaceOpenBraceOnNewLineForFunctions = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "semicolons":
                {
                    Semicolons = LsUtil.parsePreferenceSemicolonPreference(value);
                    break;
                }

                case "includeAutomaticOptionalChainCompletions":
                {
                    IncludeAutomaticOptionalChainCompletions = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "includeCompletionsForImportStatements":
                {
                    IncludeCompletionsForImportStatements = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "includeCompletionsForModuleExports":
                {
                    IncludeCompletionsForModuleExports = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "includeCompletionsWithClassMemberSnippets":
                {
                    IncludeCompletionsWithClassMemberSnippets = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "includeCompletionsWithObjectLiteralMethodSnippets":
                {
                    IncludeCompletionsWithObjectLiteralMethodSnippets = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "jsxAttributeCompletionStyle":
                {
                    JsxAttributeCompletionStyle = LsUtil.parsePreferenceJsxAttributeCompletionStyle(value);
                    break;
                }

                case "includeInlayEnumMemberValueHints":
                {
                    IncludeInlayEnumMemberValueHints = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "includeInlayFunctionLikeReturnTypeHints":
                {
                    IncludeInlayFunctionLikeReturnTypeHints = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "includeInlayFunctionParameterTypeHints":
                {
                    IncludeInlayFunctionParameterTypeHints = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "includeInlayParameterNameHints":
                {
                    IncludeInlayParameterNameHints = LsUtil.parsePreferenceIncludeInlayParameterNameHints(value);
                    break;
                }

                case "includeInlayParameterNameHintsWhenArgumentMatchesName":
                {
                    IncludeInlayParameterNameHintsWhenArgumentMatchesName = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "includeInlayPropertyDeclarationTypeHints":
                {
                    IncludeInlayPropertyDeclarationTypeHints = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "includeInlayVariableTypeHints":
                {
                    IncludeInlayVariableTypeHints = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "includeInlayVariableTypeHintsWhenTypeMatchesName":
                {
                    IncludeInlayVariableTypeHintsWhenTypeMatchesName = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "implementationsCodeLensEnabled":
                {
                    ImplementationsCodeLensEnabled = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "implementationsCodeLensShowOnAllClassMethods":
                {
                    ImplementationsCodeLensShowOnAllClassMethods = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "implementationsCodeLensShowOnInterfaceMethods":
                {
                    ImplementationsCodeLensShowOnInterfaceMethods = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "referencesCodeLensEnabled":
                {
                    ReferencesCodeLensEnabled = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "referencesCodeLensShowOnAllFunctions":
                {
                    ReferencesCodeLensShowOnAllFunctions = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "allowRenameOfImportPath":
                {
                    AllowRenameOfImportPath = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "allowTextChangesInNewFiles":
                {
                    AllowTextChangesInNewFiles = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "autoClosingTags":
                {
                    AutoClosingTags = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "autoImportEntrypointDirectorySearch":
                {
                    AutoImportEntrypointDirectorySearch = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "autoImportFileExcludePatterns":
                {
                    if (value is List<object> values)
                    {
                        var result = Slice<string>.Make(0, values.Count);
                        foreach (var item in values)
                            if (item is string text)
                                result = result.Append(text);
                        AutoImportFileExcludePatterns = result;
                    }

                    break;
                }

                case "autoImportSpecifierExcludeRegexes":
                {
                    if (value is List<object> values)
                    {
                        var result = Slice<string>.Make(0, values.Count);
                        foreach (var item in values)
                            if (item is string text)
                                result = result.Append(text);
                        AutoImportSpecifierExcludeRegexes = result;
                    }

                    break;
                }

                case "automaticTypeAcquisitionEnabled":
                {
                    AutomaticTypeAcquisitionEnabled = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "completeJSDocs":
                {
                    CompleteJSDocs = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "customConfigFileName":
                {
                    if (value is string text)
                        CustomConfigFileName = text;
                    break;
                }

                case "disableAutomaticTypeAcquisition":
                {
                    DisableAutomaticTypeAcquisition = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "disableLineTextInReferences":
                {
                    DisableLineTextInReferences = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "disableSuggestions":
                {
                    DisableSuggestions = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "displayPartsForJSDoc":
                {
                    DisplayPartsForJSDoc = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "excludeLibrarySymbolsInNavTo":
                {
                    ExcludeLibrarySymbolsInNavTo = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "formatEnabled":
                {
                    FormatEnabled = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "generateReturnInDocTemplate":
                {
                    GenerateReturnInDocTemplate = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "importModuleSpecifierEnding":
                {
                    ImportModuleSpecifierEnding = LsUtil.parsePreferenceImportModuleSpecifierEndingPreference(value);
                    break;
                }

                case "importModuleSpecifierPreference":
                {
                    ImportModuleSpecifierPreference = LsUtil.parsePreferenceImportModuleSpecifierPreference(value);
                    break;
                }

                case "lazyConfiguredProjectsFromExternalProject":
                {
                    LazyConfiguredProjectsFromExternalProject = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "locale":
                {
                    if (value is string text)
                        Locale = text;
                    break;
                }

                case "maximumHoverLength":
                {
                    if (value is long integer)
                        MaximumHoverLength = integer;
                    else if (value is int smallInteger)
                        MaximumHoverLength = smallInteger;
                    else if (value is double number)
                        MaximumHoverLength = unchecked((long)number);
                    break;
                }

                case "organizeImportsAccentCollation":
                {
                    OrganizeImportsAccentCollation = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "organizeImportsCaseFirst":
                {
                    OrganizeImportsCaseFirst = LsUtil.parsePreferenceOrganizeImportsCaseFirst(value);
                    break;
                }

                case "organizeImportsCollation":
                {
                    OrganizeImportsCollation = LsUtil.parsePreferenceOrganizeImportsCollation(value);
                    break;
                }

                case "organizeImportsIgnoreCase":
                {
                    OrganizeImportsIgnoreCase = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "organizeImportsLocale":
                {
                    if (value is string text)
                        OrganizeImportsLocale = text;
                    break;
                }

                case "organizeImportsNumericCollation":
                {
                    OrganizeImportsNumericCollation = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "organizeImportsSort":
                {
                    OrganizeImportsSort = LsUtil.parsePreferenceOrganizeImportsSort(value);
                    break;
                }

                case "organizeImportsTypeOrder":
                {
                    OrganizeImportsTypeOrder = LsUtil.parsePreferenceOrganizeImportsTypeOrder(value);
                    break;
                }

                case "preferGoToSourceDefinition":
                {
                    if (value is bool boolean)
                        PreferGoToSourceDefinition = boolean;
                    break;
                }

                case "preferTypeOnlyAutoImports":
                {
                    PreferTypeOnlyAutoImports = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "providePrefixAndSuffixTextForRename":
                {
                    ProvidePrefixAndSuffixTextForRename = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "provideRefactorNotApplicableReason":
                {
                    ProvideRefactorNotApplicableReason = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "quotePreference":
                {
                    QuotePreference = LsUtil.parsePreferenceQuotePreference(value);
                    break;
                }

                case "reportStyleChecksAsWarnings":
                {
                    ReportStyleChecksAsWarnings = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "validateEnabled":
                {
                    ValidateEnabled = LsUtil.parsePreferenceTristate(value);
                    break;
                }

                case "workspaceSymbolsScope":
                {
                    WorkspaceSymbolsScope = LsUtil.parsePreferenceWorkspaceSymbolsScope(value);
                    break;
                }
            }
        }
    }

    // userpreferences_generated.go:637. Each call copies embedded settings before applying precedence.
    internal UserPreferences WithConfig(Dictionary<string, object> config)
    {
        var p = this;
        p.ApplyRawPreferences(config);
        if (config != null && config.TryGetValue("unstable", out var unstable) && unstable is Dictionary<string, object> raw)
            p.ApplyRawPreferences(raw);
        if (LsUtil.GetPreferenceConfigValue(config, out var value0, "format.baseIndentSize"))
        {
            var value = value0;
            if (value != null)
            {
                if (value is long integer)
                    p.BaseIndentSize = integer;
                else if (value is int smallInteger)
                    p.BaseIndentSize = smallInteger;
                else if (value is double number)
                    p.BaseIndentSize = unchecked((long)number);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value1, "format.convertTabsToSpaces"))
        {
            var value = value1;
            if (value != null)
            {
                p.ConvertTabsToSpaces = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value2, "format.indentSize"))
        {
            var value = value2;
            if (value != null)
            {
                if (value is long integer)
                    p.IndentSize = integer;
                else if (value is int smallInteger)
                    p.IndentSize = smallInteger;
                else if (value is double number)
                    p.IndentSize = unchecked((long)number);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value3, "format.indentStyle"))
        {
            var value = value3;
            if (value != null)
            {
                p.IndentStyle = LsUtil.parsePreferenceIndentStyle(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value4, "format.newLineCharacter"))
        {
            var value = value4;
            if (value != null)
            {
                if (value is string text)
                    p.NewLineCharacter = text;
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value5, "format.tabSize"))
        {
            var value = value5;
            if (value != null)
            {
                if (value is long integer)
                    p.TabSize = integer;
                else if (value is int smallInteger)
                    p.TabSize = smallInteger;
                else if (value is double number)
                    p.TabSize = unchecked((long)number);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value6, "format.trimTrailingWhitespace"))
        {
            var value = value6;
            if (value != null)
            {
                p.TrimTrailingWhitespace = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value7, "format.indentMultiLineObjectLiteralBeginningOnBlankLine"))
        {
            var value = value7;
            if (value != null)
            {
                p.IndentMultiLineObjectLiteralBeginningOnBlankLine = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value8, "format.indentSwitchCase"))
        {
            var value = value8;
            if (value != null)
            {
                p.IndentSwitchCase = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value9, "format.insertSpaceAfterCommaDelimiter"))
        {
            var value = value9;
            if (value != null)
            {
                p.InsertSpaceAfterCommaDelimiter = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value10, "format.insertSpaceAfterConstructor"))
        {
            var value = value10;
            if (value != null)
            {
                p.InsertSpaceAfterConstructor = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value11, "format.insertSpaceAfterFunctionKeywordForAnonymousFunctions"))
        {
            var value = value11;
            if (value != null)
            {
                p.InsertSpaceAfterFunctionKeywordForAnonymousFunctions = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value12, "format.insertSpaceAfterKeywordsInControlFlowStatements"))
        {
            var value = value12;
            if (value != null)
            {
                p.InsertSpaceAfterKeywordsInControlFlowStatements = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value13, "format.insertSpaceAfterOpeningAndBeforeClosingEmptyBraces"))
        {
            var value = value13;
            if (value != null)
            {
                p.InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value14, "format.insertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces"))
        {
            var value = value14;
            if (value != null)
            {
                p.InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value15, "format.insertSpaceAfterOpeningAndBeforeClosingNonemptyBraces"))
        {
            var value = value15;
            if (value != null)
            {
                p.InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value16, "format.insertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets"))
        {
            var value = value16;
            if (value != null)
            {
                p.InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value17, "format.insertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis"))
        {
            var value = value17;
            if (value != null)
            {
                p.InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value18, "format.insertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces"))
        {
            var value = value18;
            if (value != null)
            {
                p.InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value19, "format.insertSpaceAfterSemicolonInForStatements"))
        {
            var value = value19;
            if (value != null)
            {
                p.InsertSpaceAfterSemicolonInForStatements = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value20, "format.insertSpaceAfterTypeAssertion"))
        {
            var value = value20;
            if (value != null)
            {
                p.InsertSpaceAfterTypeAssertion = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value21, "format.insertSpaceBeforeAndAfterBinaryOperators"))
        {
            var value = value21;
            if (value != null)
            {
                p.InsertSpaceBeforeAndAfterBinaryOperators = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value22, "format.insertSpaceBeforeFunctionParenthesis"))
        {
            var value = value22;
            if (value != null)
            {
                p.InsertSpaceBeforeFunctionParenthesis = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value23, "format.insertSpaceBeforeTypeAnnotation"))
        {
            var value = value23;
            if (value != null)
            {
                p.InsertSpaceBeforeTypeAnnotation = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value24, "format.placeOpenBraceOnNewLineForControlBlocks"))
        {
            var value = value24;
            if (value != null)
            {
                p.PlaceOpenBraceOnNewLineForControlBlocks = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value25, "format.placeOpenBraceOnNewLineForFunctions"))
        {
            var value = value25;
            if (value != null)
            {
                p.PlaceOpenBraceOnNewLineForFunctions = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value26, "format.semicolons"))
        {
            var value = value26;
            if (value != null)
            {
                p.Semicolons = LsUtil.parsePreferenceSemicolonPreference(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value27, "suggest.includeAutomaticOptionalChainCompletions"))
        {
            var value = value27;
            if (value != null)
            {
                p.IncludeAutomaticOptionalChainCompletions = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value28, "suggest.includeCompletionsForImportStatements"))
        {
            var value = value28;
            if (value != null)
            {
                p.IncludeCompletionsForImportStatements = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value29, "suggest.autoImports"))
        {
            var value = value29;
            if (value != null)
            {
                p.IncludeCompletionsForModuleExports = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value30, "suggest.classMemberSnippets.enabled"))
        {
            var value = value30;
            if (value != null)
            {
                p.IncludeCompletionsWithClassMemberSnippets = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value31, "suggest.objectLiteralMethodSnippets.enabled"))
        {
            var value = value31;
            if (value != null)
            {
                p.IncludeCompletionsWithObjectLiteralMethodSnippets = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value32, "preferences.jsxAttributeCompletionStyle"))
        {
            var value = value32;
            if (value != null)
            {
                p.JsxAttributeCompletionStyle = LsUtil.parsePreferenceJsxAttributeCompletionStyle(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value33, "inlayHints.enumMemberValues.enabled"))
        {
            var value = value33;
            if (value != null)
            {
                p.IncludeInlayEnumMemberValueHints = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value34, "inlayHints.functionLikeReturnTypes.enabled"))
        {
            var value = value34;
            if (value != null)
            {
                p.IncludeInlayFunctionLikeReturnTypeHints = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value35, "inlayHints.parameterTypes.enabled"))
        {
            var value = value35;
            if (value != null)
            {
                p.IncludeInlayFunctionParameterTypeHints = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value36, "inlayHints.parameterNames.enabled"))
        {
            var value = value36;
            if (value != null)
            {
                p.IncludeInlayParameterNameHints = LsUtil.parsePreferenceIncludeInlayParameterNameHints(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value37, "inlayHints.parameterNames.suppressWhenArgumentMatchesName,invert"))
        {
            var value = value37;
            if (value != null)
            {
                p.IncludeInlayParameterNameHintsWhenArgumentMatchesName = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value38, "inlayHints.propertyDeclarationTypes.enabled"))
        {
            var value = value38;
            if (value != null)
            {
                p.IncludeInlayPropertyDeclarationTypeHints = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value39, "inlayHints.variableTypes.enabled"))
        {
            var value = value39;
            if (value != null)
            {
                p.IncludeInlayVariableTypeHints = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value40, "inlayHints.variableTypes.suppressWhenTypeMatchesName,invert"))
        {
            var value = value40;
            if (value != null)
            {
                p.IncludeInlayVariableTypeHintsWhenTypeMatchesName = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value41, "implementationsCodeLens.enabled"))
        {
            var value = value41;
            if (value != null)
            {
                p.ImplementationsCodeLensEnabled = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value42, "implementationsCodeLens.showOnAllClassMethods"))
        {
            var value = value42;
            if (value != null)
            {
                p.ImplementationsCodeLensShowOnAllClassMethods = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value43, "implementationsCodeLens.showOnInterfaceMethods"))
        {
            var value = value43;
            if (value != null)
            {
                p.ImplementationsCodeLensShowOnInterfaceMethods = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value44, "referencesCodeLens.enabled"))
        {
            var value = value44;
            if (value != null)
            {
                p.ReferencesCodeLensEnabled = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value45, "referencesCodeLens.showOnAllFunctions"))
        {
            var value = value45;
            if (value != null)
            {
                p.ReferencesCodeLensShowOnAllFunctions = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value46, "autoClosingTags.enabled", "autoClosingTags"))
        {
            var value = value46;
            if (value != null)
            {
                p.AutoClosingTags = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value47, "preferences.autoImportEntrypointDirectorySearch"))
        {
            var value = value47;
            if (value != null)
            {
                p.AutoImportEntrypointDirectorySearch = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value48, "preferences.autoImportFileExcludePatterns"))
        {
            var value = value48;
            if (value != null)
            {
                if (value is List<object> values)
                {
                    var result = Slice<string>.Make(0, values.Count);
                    foreach (var item in values)
                        if (item is string text)
                            result = result.Append(text);
                    p.AutoImportFileExcludePatterns = result;
                }
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value49, "preferences.autoImportSpecifierExcludeRegexes"))
        {
            var value = value49;
            if (value != null)
            {
                if (value is List<object> values)
                {
                    var result = Slice<string>.Make(0, values.Count);
                    foreach (var item in values)
                        if (item is string text)
                            result = result.Append(text);
                    p.AutoImportSpecifierExcludeRegexes = result;
                }
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value50, "tsserver.automaticTypeAcquisition.enabled"))
        {
            var value = value50;
            if (value != null)
            {
                p.AutomaticTypeAcquisitionEnabled = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value51, "suggest.jsdoc.enabled", "suggest.completeJSDocs"))
        {
            var value = value51;
            if (value != null)
            {
                p.CompleteJSDocs = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value52, "customConfigFileName"))
        {
            var value = value52;
            if (value != null)
            {
                if (value is string text)
                    p.CustomConfigFileName = text;
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value53, "disableAutomaticTypeAcquisition"))
        {
            var value = value53;
            if (value != null)
            {
                p.DisableAutomaticTypeAcquisition = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value54, "workspaceSymbols.excludeLibrarySymbols"))
        {
            var value = value54;
            if (value != null)
            {
                p.ExcludeLibrarySymbolsInNavTo = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value55, "format.enabled", "format.enable"))
        {
            var value = value55;
            if (value != null)
            {
                p.FormatEnabled = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value56, "suggest.jsdoc.generateReturns"))
        {
            var value = value56;
            if (value != null)
            {
                p.GenerateReturnInDocTemplate = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value57, "preferences.importModuleSpecifierEnding"))
        {
            var value = value57;
            if (value != null)
            {
                p.ImportModuleSpecifierEnding = LsUtil.parsePreferenceImportModuleSpecifierEndingPreference(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value58, "preferences.importModuleSpecifier"))
        {
            var value = value58;
            if (value != null)
            {
                p.ImportModuleSpecifierPreference = LsUtil.parsePreferenceImportModuleSpecifierPreference(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value59, "locale"))
        {
            var value = value59;
            if (value != null)
            {
                if (value is string text)
                    p.Locale = text;
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value60, "preferences.organizeImports.accentCollation"))
        {
            var value = value60;
            if (value != null)
            {
                p.OrganizeImportsAccentCollation = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value61, "preferences.organizeImports.caseFirst"))
        {
            var value = value61;
            if (value != null)
            {
                p.OrganizeImportsCaseFirst = LsUtil.parsePreferenceOrganizeImportsCaseFirst(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value62, "preferences.organizeImports.unicodeCollation"))
        {
            var value = value62;
            if (value != null)
            {
                p.OrganizeImportsCollation = LsUtil.parsePreferenceOrganizeImportsCollation(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value63, "preferences.organizeImports.caseSensitivity"))
        {
            var value = value63;
            p.OrganizeImportsIgnoreCase = LsUtil.parseOrganizeImportsCaseSensitivity(value);
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value64, "preferences.organizeImports.locale"))
        {
            var value = value64;
            if (value != null)
            {
                if (value is string text)
                    p.OrganizeImportsLocale = text;
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value65, "preferences.organizeImports.numericCollation"))
        {
            var value = value65;
            if (value != null)
            {
                p.OrganizeImportsNumericCollation = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value66, "preferences.organizeImports.sort"))
        {
            var value = value66;
            if (value != null)
            {
                p.OrganizeImportsSort = LsUtil.parsePreferenceOrganizeImportsSort(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value67, "preferences.organizeImports.typeOrder"))
        {
            var value = value67;
            if (value != null)
            {
                p.OrganizeImportsTypeOrder = LsUtil.parsePreferenceOrganizeImportsTypeOrder(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value68, "preferences.preferTypeOnlyAutoImports"))
        {
            var value = value68;
            if (value != null)
            {
                p.PreferTypeOnlyAutoImports = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value69, "preferences.useAliasesForRenames"))
        {
            var value = value69;
            if (value != null)
            {
                p.ProvidePrefixAndSuffixTextForRename = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value70, "preferences.quoteStyle"))
        {
            var value = value70;
            if (value != null)
            {
                p.QuotePreference = LsUtil.parsePreferenceQuotePreference(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value71, "reportStyleChecksAsWarnings"))
        {
            var value = value71;
            if (value != null)
            {
                p.ReportStyleChecksAsWarnings = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value72, "validate.enabled", "validate.enable"))
        {
            var value = value72;
            if (value != null)
            {
                p.ValidateEnabled = LsUtil.parsePreferenceTristate(value);
            }
        }

        if (LsUtil.GetPreferenceConfigValue(config, out var value73, "workspaceSymbols.scope"))
        {
            var value = value73;
            if (value != null)
            {
                p.WorkspaceSymbolsScope = LsUtil.parsePreferenceWorkspaceSymbolsScope(value);
            }
        }

        p.CustomConfigFileName = LsUtil.NormalizeCustomConfigFileName(p.CustomConfigFileName);
        return p;
    }
}

internal partial struct UserPreferences
{
    // userpreferences_generated.go:1053. Deterministic JSON is emitted from these original nested paths.
    internal readonly Dictionary<string, object> ToConfiguration()
    {
        var config = new Dictionary<string, object>(StringComparer.Ordinal);
        if (BaseIndentSize != 0)
            LsUtil.SetNestedValue(config, "format.baseIndentSize", (long)BaseIndentSize);
        if (ConvertTabsToSpaces == Tristate.True || ConvertTabsToSpaces == Tristate.False)
            LsUtil.SetNestedValue(config, "format.convertTabsToSpaces", ConvertTabsToSpaces == Tristate.True);
        if (IndentSize != 0)
            LsUtil.SetNestedValue(config, "format.indentSize", (long)IndentSize);
        if (IndentStyle != 0)
            LsUtil.SetNestedValue(config, "format.indentStyle", (long)IndentStyle);
        if (NewLineCharacter.Length != 0)
            LsUtil.SetNestedValue(config, "format.newLineCharacter", NewLineCharacter);
        if (TabSize != 0)
            LsUtil.SetNestedValue(config, "format.tabSize", (long)TabSize);
        if (TrimTrailingWhitespace == Tristate.True || TrimTrailingWhitespace == Tristate.False)
            LsUtil.SetNestedValue(config, "format.trimTrailingWhitespace", TrimTrailingWhitespace == Tristate.True);
        if (IndentMultiLineObjectLiteralBeginningOnBlankLine == Tristate.True || IndentMultiLineObjectLiteralBeginningOnBlankLine == Tristate.False)
            LsUtil.SetNestedValue(config, "format.indentMultiLineObjectLiteralBeginningOnBlankLine", IndentMultiLineObjectLiteralBeginningOnBlankLine == Tristate.True);
        if (IndentSwitchCase == Tristate.True || IndentSwitchCase == Tristate.False)
            LsUtil.SetNestedValue(config, "format.indentSwitchCase", IndentSwitchCase == Tristate.True);
        if (InsertSpaceAfterCommaDelimiter == Tristate.True || InsertSpaceAfterCommaDelimiter == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceAfterCommaDelimiter", InsertSpaceAfterCommaDelimiter == Tristate.True);
        if (InsertSpaceAfterConstructor == Tristate.True || InsertSpaceAfterConstructor == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceAfterConstructor", InsertSpaceAfterConstructor == Tristate.True);
        if (InsertSpaceAfterFunctionKeywordForAnonymousFunctions == Tristate.True || InsertSpaceAfterFunctionKeywordForAnonymousFunctions == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceAfterFunctionKeywordForAnonymousFunctions", InsertSpaceAfterFunctionKeywordForAnonymousFunctions == Tristate.True);
        if (InsertSpaceAfterKeywordsInControlFlowStatements == Tristate.True || InsertSpaceAfterKeywordsInControlFlowStatements == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceAfterKeywordsInControlFlowStatements", InsertSpaceAfterKeywordsInControlFlowStatements == Tristate.True);
        if (InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces == Tristate.True || InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceAfterOpeningAndBeforeClosingEmptyBraces", InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces == Tristate.True);
        if (InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces == Tristate.True || InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces", InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces == Tristate.True);
        if (InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces == Tristate.True || InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceAfterOpeningAndBeforeClosingNonemptyBraces", InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces == Tristate.True);
        if (InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets == Tristate.True || InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets", InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets == Tristate.True);
        if (InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis == Tristate.True || InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis", InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis == Tristate.True);
        if (InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces == Tristate.True || InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces", InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces == Tristate.True);
        if (InsertSpaceAfterSemicolonInForStatements == Tristate.True || InsertSpaceAfterSemicolonInForStatements == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceAfterSemicolonInForStatements", InsertSpaceAfterSemicolonInForStatements == Tristate.True);
        if (InsertSpaceAfterTypeAssertion == Tristate.True || InsertSpaceAfterTypeAssertion == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceAfterTypeAssertion", InsertSpaceAfterTypeAssertion == Tristate.True);
        if (InsertSpaceBeforeAndAfterBinaryOperators == Tristate.True || InsertSpaceBeforeAndAfterBinaryOperators == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceBeforeAndAfterBinaryOperators", InsertSpaceBeforeAndAfterBinaryOperators == Tristate.True);
        if (InsertSpaceBeforeFunctionParenthesis == Tristate.True || InsertSpaceBeforeFunctionParenthesis == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceBeforeFunctionParenthesis", InsertSpaceBeforeFunctionParenthesis == Tristate.True);
        if (InsertSpaceBeforeTypeAnnotation == Tristate.True || InsertSpaceBeforeTypeAnnotation == Tristate.False)
            LsUtil.SetNestedValue(config, "format.insertSpaceBeforeTypeAnnotation", InsertSpaceBeforeTypeAnnotation == Tristate.True);
        if (PlaceOpenBraceOnNewLineForControlBlocks == Tristate.True || PlaceOpenBraceOnNewLineForControlBlocks == Tristate.False)
            LsUtil.SetNestedValue(config, "format.placeOpenBraceOnNewLineForControlBlocks", PlaceOpenBraceOnNewLineForControlBlocks == Tristate.True);
        if (PlaceOpenBraceOnNewLineForFunctions == Tristate.True || PlaceOpenBraceOnNewLineForFunctions == Tristate.False)
            LsUtil.SetNestedValue(config, "format.placeOpenBraceOnNewLineForFunctions", PlaceOpenBraceOnNewLineForFunctions == Tristate.True);
        if (Semicolons.Value.Length != 0)
            LsUtil.SetNestedValue(config, "format.semicolons", Semicolons.Value);
        if (IncludeAutomaticOptionalChainCompletions == Tristate.True || IncludeAutomaticOptionalChainCompletions == Tristate.False)
            LsUtil.SetNestedValue(config, "suggest.includeAutomaticOptionalChainCompletions", IncludeAutomaticOptionalChainCompletions == Tristate.True);
        if (IncludeCompletionsForImportStatements == Tristate.True || IncludeCompletionsForImportStatements == Tristate.False)
            LsUtil.SetNestedValue(config, "suggest.includeCompletionsForImportStatements", IncludeCompletionsForImportStatements == Tristate.True);
        if (IncludeCompletionsForModuleExports == Tristate.True || IncludeCompletionsForModuleExports == Tristate.False)
            LsUtil.SetNestedValue(config, "suggest.autoImports", IncludeCompletionsForModuleExports == Tristate.True);
        if (IncludeCompletionsWithClassMemberSnippets == Tristate.True || IncludeCompletionsWithClassMemberSnippets == Tristate.False)
            LsUtil.SetNestedValue(config, "suggest.classMemberSnippets.enabled", IncludeCompletionsWithClassMemberSnippets == Tristate.True);
        if (IncludeCompletionsWithObjectLiteralMethodSnippets == Tristate.True || IncludeCompletionsWithObjectLiteralMethodSnippets == Tristate.False)
            LsUtil.SetNestedValue(config, "suggest.objectLiteralMethodSnippets.enabled", IncludeCompletionsWithObjectLiteralMethodSnippets == Tristate.True);
        if (JsxAttributeCompletionStyle.Value.Length != 0)
            LsUtil.SetNestedValue(config, "preferences.jsxAttributeCompletionStyle", JsxAttributeCompletionStyle.Value);
        if (IncludeInlayEnumMemberValueHints == Tristate.True || IncludeInlayEnumMemberValueHints == Tristate.False)
            LsUtil.SetNestedValue(config, "inlayHints.enumMemberValues.enabled", IncludeInlayEnumMemberValueHints == Tristate.True);
        if (IncludeInlayFunctionLikeReturnTypeHints == Tristate.True || IncludeInlayFunctionLikeReturnTypeHints == Tristate.False)
            LsUtil.SetNestedValue(config, "inlayHints.functionLikeReturnTypes.enabled", IncludeInlayFunctionLikeReturnTypeHints == Tristate.True);
        if (IncludeInlayFunctionParameterTypeHints == Tristate.True || IncludeInlayFunctionParameterTypeHints == Tristate.False)
            LsUtil.SetNestedValue(config, "inlayHints.parameterTypes.enabled", IncludeInlayFunctionParameterTypeHints == Tristate.True);
        if (IncludeInlayParameterNameHints.Value.Length != 0)
            LsUtil.SetNestedValue(config, "inlayHints.parameterNames.enabled", IncludeInlayParameterNameHints.Value);
        if (IncludeInlayParameterNameHintsWhenArgumentMatchesName == Tristate.True || IncludeInlayParameterNameHintsWhenArgumentMatchesName == Tristate.False)
            LsUtil.SetNestedValue(config, "inlayHints.parameterNames.suppressWhenArgumentMatchesName", IncludeInlayParameterNameHintsWhenArgumentMatchesName != Tristate.True);
        if (IncludeInlayPropertyDeclarationTypeHints == Tristate.True || IncludeInlayPropertyDeclarationTypeHints == Tristate.False)
            LsUtil.SetNestedValue(config, "inlayHints.propertyDeclarationTypes.enabled", IncludeInlayPropertyDeclarationTypeHints == Tristate.True);
        if (IncludeInlayVariableTypeHints == Tristate.True || IncludeInlayVariableTypeHints == Tristate.False)
            LsUtil.SetNestedValue(config, "inlayHints.variableTypes.enabled", IncludeInlayVariableTypeHints == Tristate.True);
        if (IncludeInlayVariableTypeHintsWhenTypeMatchesName == Tristate.True || IncludeInlayVariableTypeHintsWhenTypeMatchesName == Tristate.False)
            LsUtil.SetNestedValue(config, "inlayHints.variableTypes.suppressWhenTypeMatchesName", IncludeInlayVariableTypeHintsWhenTypeMatchesName != Tristate.True);
        if (ImplementationsCodeLensEnabled == Tristate.True || ImplementationsCodeLensEnabled == Tristate.False)
            LsUtil.SetNestedValue(config, "implementationsCodeLens.enabled", ImplementationsCodeLensEnabled == Tristate.True);
        if (ImplementationsCodeLensShowOnAllClassMethods == Tristate.True || ImplementationsCodeLensShowOnAllClassMethods == Tristate.False)
            LsUtil.SetNestedValue(config, "implementationsCodeLens.showOnAllClassMethods", ImplementationsCodeLensShowOnAllClassMethods == Tristate.True);
        if (ImplementationsCodeLensShowOnInterfaceMethods == Tristate.True || ImplementationsCodeLensShowOnInterfaceMethods == Tristate.False)
            LsUtil.SetNestedValue(config, "implementationsCodeLens.showOnInterfaceMethods", ImplementationsCodeLensShowOnInterfaceMethods == Tristate.True);
        if (ReferencesCodeLensEnabled == Tristate.True || ReferencesCodeLensEnabled == Tristate.False)
            LsUtil.SetNestedValue(config, "referencesCodeLens.enabled", ReferencesCodeLensEnabled == Tristate.True);
        if (ReferencesCodeLensShowOnAllFunctions == Tristate.True || ReferencesCodeLensShowOnAllFunctions == Tristate.False)
            LsUtil.SetNestedValue(config, "referencesCodeLens.showOnAllFunctions", ReferencesCodeLensShowOnAllFunctions == Tristate.True);
        if (AllowRenameOfImportPath == Tristate.True || AllowRenameOfImportPath == Tristate.False)
            LsUtil.SetNestedValue(config, "unstable.allowRenameOfImportPath", AllowRenameOfImportPath == Tristate.True);
        if (AllowTextChangesInNewFiles == Tristate.True || AllowTextChangesInNewFiles == Tristate.False)
            LsUtil.SetNestedValue(config, "unstable.allowTextChangesInNewFiles", AllowTextChangesInNewFiles == Tristate.True);
        if (AutoClosingTags == Tristate.True || AutoClosingTags == Tristate.False)
            LsUtil.SetNestedValue(config, "autoClosingTags.enabled", AutoClosingTags == Tristate.True);
        if (AutoImportEntrypointDirectorySearch == Tristate.True || AutoImportEntrypointDirectorySearch == Tristate.False)
            LsUtil.SetNestedValue(config, "preferences.autoImportEntrypointDirectorySearch", AutoImportEntrypointDirectorySearch == Tristate.True);
        if (AutoImportFileExcludePatterns.Count != 0)
            LsUtil.SetNestedValue(config, "preferences.autoImportFileExcludePatterns", LsUtil.StringSliceToArray(AutoImportFileExcludePatterns));
        if (AutoImportSpecifierExcludeRegexes.Count != 0)
            LsUtil.SetNestedValue(config, "preferences.autoImportSpecifierExcludeRegexes", LsUtil.StringSliceToArray(AutoImportSpecifierExcludeRegexes));
        if (AutomaticTypeAcquisitionEnabled == Tristate.True || AutomaticTypeAcquisitionEnabled == Tristate.False)
            LsUtil.SetNestedValue(config, "tsserver.automaticTypeAcquisition.enabled", AutomaticTypeAcquisitionEnabled == Tristate.True);
        if (CompleteJSDocs == Tristate.True || CompleteJSDocs == Tristate.False)
            LsUtil.SetNestedValue(config, "suggest.jsdoc.enabled", CompleteJSDocs == Tristate.True);
        if (CustomConfigFileName.Length != 0)
            LsUtil.SetNestedValue(config, "customConfigFileName", CustomConfigFileName);
        if (DisableAutomaticTypeAcquisition == Tristate.True || DisableAutomaticTypeAcquisition == Tristate.False)
            LsUtil.SetNestedValue(config, "disableAutomaticTypeAcquisition", DisableAutomaticTypeAcquisition == Tristate.True);
        if (DisableLineTextInReferences == Tristate.True || DisableLineTextInReferences == Tristate.False)
            LsUtil.SetNestedValue(config, "unstable.disableLineTextInReferences", DisableLineTextInReferences == Tristate.True);
        if (DisableSuggestions == Tristate.True || DisableSuggestions == Tristate.False)
            LsUtil.SetNestedValue(config, "unstable.disableSuggestions", DisableSuggestions == Tristate.True);
        if (DisplayPartsForJSDoc == Tristate.True || DisplayPartsForJSDoc == Tristate.False)
            LsUtil.SetNestedValue(config, "unstable.displayPartsForJSDoc", DisplayPartsForJSDoc == Tristate.True);
        if (ExcludeLibrarySymbolsInNavTo == Tristate.True || ExcludeLibrarySymbolsInNavTo == Tristate.False)
            LsUtil.SetNestedValue(config, "workspaceSymbols.excludeLibrarySymbols", ExcludeLibrarySymbolsInNavTo == Tristate.True);
        if (FormatEnabled == Tristate.True || FormatEnabled == Tristate.False)
            LsUtil.SetNestedValue(config, "format.enabled", FormatEnabled == Tristate.True);
        if (GenerateReturnInDocTemplate == Tristate.True || GenerateReturnInDocTemplate == Tristate.False)
            LsUtil.SetNestedValue(config, "suggest.jsdoc.generateReturns", GenerateReturnInDocTemplate == Tristate.True);
        if (ImportModuleSpecifierEnding.Value.Length != 0)
            LsUtil.SetNestedValue(config, "preferences.importModuleSpecifierEnding", LsUtil.ModulePreferenceText(ImportModuleSpecifierEnding));
        if (ImportModuleSpecifierPreference.Value.Length != 0)
            LsUtil.SetNestedValue(config, "preferences.importModuleSpecifier", LsUtil.ModulePreferenceText(ImportModuleSpecifierPreference));
        if (LazyConfiguredProjectsFromExternalProject == Tristate.True || LazyConfiguredProjectsFromExternalProject == Tristate.False)
            LsUtil.SetNestedValue(config, "unstable.lazyConfiguredProjectsFromExternalProject", LazyConfiguredProjectsFromExternalProject == Tristate.True);
        if (Locale.Length != 0)
            LsUtil.SetNestedValue(config, "locale", Locale);
        if (MaximumHoverLength != 0)
            LsUtil.SetNestedValue(config, "unstable.maximumHoverLength", (long)MaximumHoverLength);
        if (OrganizeImportsAccentCollation == Tristate.True || OrganizeImportsAccentCollation == Tristate.False)
            LsUtil.SetNestedValue(config, "preferences.organizeImports.accentCollation", OrganizeImportsAccentCollation == Tristate.True);
        LsUtil.SetNestedValue(config, "preferences.organizeImports.caseFirst", LsUtil.serializePreferenceOrganizeImportsCaseFirst(OrganizeImportsCaseFirst));
        LsUtil.SetNestedValue(config, "preferences.organizeImports.unicodeCollation", LsUtil.serializePreferenceOrganizeImportsCollation(OrganizeImportsCollation));
        if (OrganizeImportsIgnoreCase == Tristate.True || OrganizeImportsIgnoreCase == Tristate.False)
            LsUtil.SetNestedValue(config, "preferences.organizeImports.caseSensitivity", OrganizeImportsIgnoreCase == Tristate.True);
        if (OrganizeImportsLocale.Length != 0)
            LsUtil.SetNestedValue(config, "preferences.organizeImports.locale", OrganizeImportsLocale);
        if (OrganizeImportsNumericCollation == Tristate.True || OrganizeImportsNumericCollation == Tristate.False)
            LsUtil.SetNestedValue(config, "preferences.organizeImports.numericCollation", OrganizeImportsNumericCollation == Tristate.True);
        LsUtil.SetNestedValue(config, "preferences.organizeImports.sort", LsUtil.serializePreferenceOrganizeImportsSort(OrganizeImportsSort));
        LsUtil.SetNestedValue(config, "preferences.organizeImports.typeOrder", LsUtil.serializePreferenceOrganizeImportsTypeOrder(OrganizeImportsTypeOrder));
        LsUtil.SetNestedValue(config, "unstable.preferGoToSourceDefinition", PreferGoToSourceDefinition);
        if (PreferTypeOnlyAutoImports == Tristate.True || PreferTypeOnlyAutoImports == Tristate.False)
            LsUtil.SetNestedValue(config, "preferences.preferTypeOnlyAutoImports", PreferTypeOnlyAutoImports == Tristate.True);
        if (ProvidePrefixAndSuffixTextForRename == Tristate.True || ProvidePrefixAndSuffixTextForRename == Tristate.False)
            LsUtil.SetNestedValue(config, "preferences.useAliasesForRenames", ProvidePrefixAndSuffixTextForRename == Tristate.True);
        if (ProvideRefactorNotApplicableReason == Tristate.True || ProvideRefactorNotApplicableReason == Tristate.False)
            LsUtil.SetNestedValue(config, "unstable.provideRefactorNotApplicableReason", ProvideRefactorNotApplicableReason == Tristate.True);
        if (QuotePreference.Value.Length != 0)
            LsUtil.SetNestedValue(config, "preferences.quoteStyle", QuotePreference.Value);
        if (ReportStyleChecksAsWarnings == Tristate.True || ReportStyleChecksAsWarnings == Tristate.False)
            LsUtil.SetNestedValue(config, "reportStyleChecksAsWarnings", ReportStyleChecksAsWarnings == Tristate.True);
        if (ValidateEnabled == Tristate.True || ValidateEnabled == Tristate.False)
            LsUtil.SetNestedValue(config, "validate.enabled", ValidateEnabled == Tristate.True);
        if (WorkspaceSymbolsScope.Value.Length != 0)
            LsUtil.SetNestedValue(config, "workspaceSymbols.scope", WorkspaceSymbolsScope.Value);
        return config;
    }
}

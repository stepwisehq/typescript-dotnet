// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
#nullable disable
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;

namespace TypeScript.LanguageServices.Utilities;
internal static partial class LsUtil
{
    // Pinned internal/ls/lsutil/formatcodeoptions.go:44.
    internal static FormatCodeSettings GetDefaultFormatCodeSettings() => new()
    {
        IndentSize = TypeScript.DiagnosticPrinting.TextWriter.GetDefaultIndentSize(),
        TabSize = TypeScript.DiagnosticPrinting.TextWriter.GetDefaultIndentSize(),
        NewLineCharacter = "\n",
        ConvertTabsToSpaces = Tristate.True,
        IndentStyle = IndentStyle.Smart,
        TrimTrailingWhitespace = Tristate.True,
        InsertSpaceAfterConstructor = Tristate.False,
        InsertSpaceAfterCommaDelimiter = Tristate.True,
        InsertSpaceAfterSemicolonInForStatements = Tristate.True,
        InsertSpaceBeforeAndAfterBinaryOperators = Tristate.True,
        InsertSpaceAfterKeywordsInControlFlowStatements = Tristate.True,
        InsertSpaceAfterFunctionKeywordForAnonymousFunctions = Tristate.False,
        InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis = Tristate.False,
        InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets = Tristate.False,
        InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces = Tristate.True,
        InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces = Tristate.False,
        InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces = Tristate.False,
        InsertSpaceBeforeFunctionParenthesis = Tristate.False,
        PlaceOpenBraceOnNewLineForFunctions = Tristate.False,
        PlaceOpenBraceOnNewLineForControlBlocks = Tristate.False,
        Semicolons = SemicolonPreference.Ignore,
        IndentSwitchCase = Tristate.True,
    };
    // Pinned internal/ls/lsutil/userpreferences.go:16.
    internal static UserPreferences NewDefaultUserPreferences() => new()
    {
        FormatCodeSettings = GetDefaultFormatCodeSettings(),
        AutoClosingTags = Tristate.True,
        CompleteJSDocs = Tristate.True,
        GenerateReturnInDocTemplate = Tristate.True,
        AllowRenameOfImportPath = Tristate.True,
        ProvideRefactorNotApplicableReason = Tristate.True,
        FormatEnabled = Tristate.True,
        ValidateEnabled = Tristate.True,
        DisplayPartsForJSDoc = Tristate.True,
        DisableLineTextInReferences = Tristate.True,
        ReportStyleChecksAsWarnings = Tristate.True,
        ExcludeLibrarySymbolsInNavTo = Tristate.True,
        WorkspaceSymbolsScope = WorkspaceSymbolsScope.AllOpenProjects,
        IncludeCompletionsForModuleExports = Tristate.True,
        IncludeCompletionsForImportStatements = Tristate.True,
    };
    // formatcodeoptions.go:27. Copy the complete settings value, not a shared settings object.
    internal static FormatCodeSettings FromLSFormatOptions(FormatCodeSettings settings, FormattingOptions options)
    {
        var updated = settings;
        updated.TabSize = unchecked((long)options.TabSize);
        updated.IndentSize = unchecked((long)options.TabSize);
        updated.ConvertTabsToSpaces = options.InsertSpaces ? Tristate.True : Tristate.False;
        if (options.TrimTrailingWhitespace.HasValue)
            updated.TrimTrailingWhitespace = options.TrimTrailingWhitespace.Value ? Tristate.True : Tristate.False;
        return updated;
    }
}

internal partial struct FormatCodeSettings
{
    // formatcodeoptions.go:38. TrimTrailingWhitespace pointer is present even for unknown settings.
    internal readonly FormattingOptions ToLSFormatOptions() => new()
    {
        TabSize = unchecked((uint)TabSize),
        InsertSpaces = ConvertTabsToSpaces == Tristate.True,
        TrimTrailingWhitespace = TrimTrailingWhitespace == Tristate.True
    };
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/ls/lsutil/userpreferences_generated.go models; embedded structs remain value owners.
#nullable disable
using TypeScript.Scanner;
using TypeScript.ModuleSpecifiers;

namespace TypeScript.LanguageServices.Utilities;
internal readonly record struct QuotePreference
{
    private readonly string value;
    internal QuotePreference(string value) => this.value = value;
    internal string Value => value ?? "";

    public override string ToString() => Value;
    public bool Equals(QuotePreference other) => Value == other.Value;
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    internal static readonly QuotePreference Unknown = new("");
    internal static readonly QuotePreference Auto = new("auto");
    internal static readonly QuotePreference Double = new("double");
    internal static readonly QuotePreference Single = new("single");
}

internal readonly record struct WorkspaceSymbolsScope
{
    private readonly string value;
    internal WorkspaceSymbolsScope(string value) => this.value = value;
    internal string Value => value ?? "";

    public override string ToString() => Value;
    public bool Equals(WorkspaceSymbolsScope other) => Value == other.Value;
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    internal static readonly WorkspaceSymbolsScope AllOpenProjects = new("allOpenProjects");
    internal static readonly WorkspaceSymbolsScope CurrentProject = new("currentProject");
}

internal readonly record struct JsxAttributeCompletionStyle
{
    private readonly string value;
    internal JsxAttributeCompletionStyle(string value) => this.value = value;
    internal string Value => value ?? "";

    public override string ToString() => Value;
    public bool Equals(JsxAttributeCompletionStyle other) => Value == other.Value;
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    internal static readonly JsxAttributeCompletionStyle Unknown = new("");
    internal static readonly JsxAttributeCompletionStyle Auto = new("auto");
    internal static readonly JsxAttributeCompletionStyle Braces = new("braces");
    internal static readonly JsxAttributeCompletionStyle None = new("none");
}

internal readonly record struct IncludeInlayParameterNameHints
{
    private readonly string value;
    internal IncludeInlayParameterNameHints(string value) => this.value = value;
    internal string Value => value ?? "";

    public override string ToString() => Value;
    public bool Equals(IncludeInlayParameterNameHints other) => Value == other.Value;
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    internal static readonly IncludeInlayParameterNameHints None = new("");
    internal static readonly IncludeInlayParameterNameHints All = new("all");
    internal static readonly IncludeInlayParameterNameHints Literals = new("literals");
}

internal enum OrganizeImportsSort : long
{
    Auto = 0,
    Ordinal = 1,
    OrdinalIgnoreCase = 2,
    Natural = 3,
    NaturalIgnoreCase = 4,
}

internal enum OrganizeImportsCollation
{
    Ordinal = 0,
    Unicode = 1,
}

internal enum OrganizeImportsCaseFirst : long
{
    Default = 0,
    Lower = 1,
    Upper = 2,
}

internal enum OrganizeImportsTypeOrder : long
{
    Auto = 0,
    Last = 1,
    Inline = 2,
    First = 3,
}

internal enum IndentStyle : long
{
    None = 0,
    Block = 1,
    Smart = 2,
}

internal readonly record struct SemicolonPreference
{
    private readonly string value;
    internal SemicolonPreference(string value) => this.value = value;
    internal string Value => value ?? "";

    public override string ToString() => Value;
    public bool Equals(SemicolonPreference other) => Value == other.Value;
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    internal static readonly SemicolonPreference Ignore = new("ignore");
    internal static readonly SemicolonPreference Insert = new("insert");
    internal static readonly SemicolonPreference Remove = new("remove");
}

internal partial struct CodeLensUserPreferences
{
    internal Tristate ImplementationsCodeLensEnabled;
    internal Tristate ImplementationsCodeLensShowOnAllClassMethods;
    internal Tristate ImplementationsCodeLensShowOnInterfaceMethods;
    internal Tristate ReferencesCodeLensEnabled;
    internal Tristate ReferencesCodeLensShowOnAllFunctions;
}

internal partial struct CompletionPreferences
{
    internal Tristate IncludeAutomaticOptionalChainCompletions;
    internal Tristate IncludeCompletionsForImportStatements;
    internal Tristate IncludeCompletionsForModuleExports;
    internal Tristate IncludeCompletionsWithClassMemberSnippets;
    internal Tristate IncludeCompletionsWithObjectLiteralMethodSnippets;
    internal JsxAttributeCompletionStyle JsxAttributeCompletionStyle;
}

internal partial struct EditorSettings
{
    internal long BaseIndentSize;
    internal Tristate ConvertTabsToSpaces;
    internal long IndentSize;
    internal IndentStyle IndentStyle;
    private string NewLineCharacterValue;
    internal string NewLineCharacter { readonly get => NewLineCharacterValue ?? ""; set => NewLineCharacterValue = value; }

    internal long TabSize;
    internal Tristate TrimTrailingWhitespace;
}

internal partial struct FormatCodeSettings
{
    internal EditorSettings EditorSettings;
    internal Tristate IndentMultiLineObjectLiteralBeginningOnBlankLine;
    internal Tristate IndentSwitchCase;
    internal Tristate InsertSpaceAfterCommaDelimiter;
    internal Tristate InsertSpaceAfterConstructor;
    internal Tristate InsertSpaceAfterFunctionKeywordForAnonymousFunctions;
    internal Tristate InsertSpaceAfterKeywordsInControlFlowStatements;
    internal Tristate InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces;
    internal Tristate InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces;
    internal Tristate InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces;
    internal Tristate InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets;
    internal Tristate InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis;
    internal Tristate InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces;
    internal Tristate InsertSpaceAfterSemicolonInForStatements;
    internal Tristate InsertSpaceAfterTypeAssertion;
    internal Tristate InsertSpaceBeforeAndAfterBinaryOperators;
    internal Tristate InsertSpaceBeforeFunctionParenthesis;
    internal Tristate InsertSpaceBeforeTypeAnnotation;
    internal Tristate PlaceOpenBraceOnNewLineForControlBlocks;
    internal Tristate PlaceOpenBraceOnNewLineForFunctions;
    internal SemicolonPreference Semicolons;
    internal long BaseIndentSize { readonly get => EditorSettings.BaseIndentSize; set => EditorSettings.BaseIndentSize = value; }
    internal Tristate ConvertTabsToSpaces { readonly get => EditorSettings.ConvertTabsToSpaces; set => EditorSettings.ConvertTabsToSpaces = value; }
    internal long IndentSize { readonly get => EditorSettings.IndentSize; set => EditorSettings.IndentSize = value; }
    internal IndentStyle IndentStyle { readonly get => EditorSettings.IndentStyle; set => EditorSettings.IndentStyle = value; }
    internal string NewLineCharacter { readonly get => EditorSettings.NewLineCharacter; set => EditorSettings.NewLineCharacter = value; }
    internal long TabSize { readonly get => EditorSettings.TabSize; set => EditorSettings.TabSize = value; }
    internal Tristate TrimTrailingWhitespace { readonly get => EditorSettings.TrimTrailingWhitespace; set => EditorSettings.TrimTrailingWhitespace = value; }
}

internal partial struct InlayHintsPreferences
{
    internal Tristate IncludeInlayEnumMemberValueHints;
    internal Tristate IncludeInlayFunctionLikeReturnTypeHints;
    internal Tristate IncludeInlayFunctionParameterTypeHints;
    internal IncludeInlayParameterNameHints IncludeInlayParameterNameHints;
    internal Tristate IncludeInlayParameterNameHintsWhenArgumentMatchesName;
    internal Tristate IncludeInlayPropertyDeclarationTypeHints;
    internal Tristate IncludeInlayVariableTypeHints;
    internal Tristate IncludeInlayVariableTypeHintsWhenTypeMatchesName;
}

internal partial struct UserPreferences
{
    internal FormatCodeSettings FormatCodeSettings;
    internal CompletionPreferences CompletionPreferences;
    internal InlayHintsPreferences InlayHintsPreferences;
    internal CodeLensUserPreferences CodeLensUserPreferences;
    internal Tristate AllowRenameOfImportPath;
    internal Tristate AllowTextChangesInNewFiles;
    internal Tristate AutoClosingTags;
    internal Tristate AutoImportEntrypointDirectorySearch;
    internal Slice<string> AutoImportFileExcludePatterns;
    internal Slice<string> AutoImportSpecifierExcludeRegexes;
    internal Tristate AutomaticTypeAcquisitionEnabled;
    internal Tristate CompleteJSDocs;
    private string CustomConfigFileNameValue;
    internal string CustomConfigFileName { readonly get => CustomConfigFileNameValue ?? ""; set => CustomConfigFileNameValue = value; }

    internal Tristate DisableAutomaticTypeAcquisition;
    internal Tristate DisableLineTextInReferences;
    internal Tristate DisableSuggestions;
    internal Tristate DisplayPartsForJSDoc;
    internal Tristate ExcludeLibrarySymbolsInNavTo;
    internal Tristate FormatEnabled;
    internal Tristate GenerateReturnInDocTemplate;
    internal ImportModuleSpecifierEndingPreference ImportModuleSpecifierEnding;
    internal ImportModuleSpecifierPreference ImportModuleSpecifierPreference;
    internal Tristate LazyConfiguredProjectsFromExternalProject;
    private string LocaleValue;
    internal string Locale { readonly get => LocaleValue ?? ""; set => LocaleValue = value; }

    internal long MaximumHoverLength;
    internal Tristate OrganizeImportsAccentCollation;
    internal OrganizeImportsCaseFirst OrganizeImportsCaseFirst;
    internal OrganizeImportsCollation OrganizeImportsCollation;
    internal Tristate OrganizeImportsIgnoreCase;
    private string OrganizeImportsLocaleValue;
    internal string OrganizeImportsLocale { readonly get => OrganizeImportsLocaleValue ?? ""; set => OrganizeImportsLocaleValue = value; }

    internal Tristate OrganizeImportsNumericCollation;
    internal OrganizeImportsSort OrganizeImportsSort;
    internal OrganizeImportsTypeOrder OrganizeImportsTypeOrder;
    internal bool PreferGoToSourceDefinition;
    internal Tristate PreferTypeOnlyAutoImports;
    internal Tristate ProvidePrefixAndSuffixTextForRename;
    internal Tristate ProvideRefactorNotApplicableReason;
    internal QuotePreference QuotePreference;
    internal Tristate ReportStyleChecksAsWarnings;
    internal Tristate ValidateEnabled;
    internal WorkspaceSymbolsScope WorkspaceSymbolsScope;
    internal Tristate IndentMultiLineObjectLiteralBeginningOnBlankLine { readonly get => FormatCodeSettings.IndentMultiLineObjectLiteralBeginningOnBlankLine; set => FormatCodeSettings.IndentMultiLineObjectLiteralBeginningOnBlankLine = value; }
    internal Tristate IndentSwitchCase { readonly get => FormatCodeSettings.IndentSwitchCase; set => FormatCodeSettings.IndentSwitchCase = value; }
    internal Tristate InsertSpaceAfterCommaDelimiter { readonly get => FormatCodeSettings.InsertSpaceAfterCommaDelimiter; set => FormatCodeSettings.InsertSpaceAfterCommaDelimiter = value; }
    internal Tristate InsertSpaceAfterConstructor { readonly get => FormatCodeSettings.InsertSpaceAfterConstructor; set => FormatCodeSettings.InsertSpaceAfterConstructor = value; }
    internal Tristate InsertSpaceAfterFunctionKeywordForAnonymousFunctions { readonly get => FormatCodeSettings.InsertSpaceAfterFunctionKeywordForAnonymousFunctions; set => FormatCodeSettings.InsertSpaceAfterFunctionKeywordForAnonymousFunctions = value; }
    internal Tristate InsertSpaceAfterKeywordsInControlFlowStatements { readonly get => FormatCodeSettings.InsertSpaceAfterKeywordsInControlFlowStatements; set => FormatCodeSettings.InsertSpaceAfterKeywordsInControlFlowStatements = value; }
    internal Tristate InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces { readonly get => FormatCodeSettings.InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces; set => FormatCodeSettings.InsertSpaceAfterOpeningAndBeforeClosingEmptyBraces = value; }
    internal Tristate InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces { readonly get => FormatCodeSettings.InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces; set => FormatCodeSettings.InsertSpaceAfterOpeningAndBeforeClosingJsxExpressionBraces = value; }
    internal Tristate InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces { readonly get => FormatCodeSettings.InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces; set => FormatCodeSettings.InsertSpaceAfterOpeningAndBeforeClosingNonemptyBraces = value; }
    internal Tristate InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets { readonly get => FormatCodeSettings.InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets; set => FormatCodeSettings.InsertSpaceAfterOpeningAndBeforeClosingNonemptyBrackets = value; }
    internal Tristate InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis { readonly get => FormatCodeSettings.InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis; set => FormatCodeSettings.InsertSpaceAfterOpeningAndBeforeClosingNonemptyParenthesis = value; }
    internal Tristate InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces { readonly get => FormatCodeSettings.InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces; set => FormatCodeSettings.InsertSpaceAfterOpeningAndBeforeClosingTemplateStringBraces = value; }
    internal Tristate InsertSpaceAfterSemicolonInForStatements { readonly get => FormatCodeSettings.InsertSpaceAfterSemicolonInForStatements; set => FormatCodeSettings.InsertSpaceAfterSemicolonInForStatements = value; }
    internal Tristate InsertSpaceAfterTypeAssertion { readonly get => FormatCodeSettings.InsertSpaceAfterTypeAssertion; set => FormatCodeSettings.InsertSpaceAfterTypeAssertion = value; }
    internal Tristate InsertSpaceBeforeAndAfterBinaryOperators { readonly get => FormatCodeSettings.InsertSpaceBeforeAndAfterBinaryOperators; set => FormatCodeSettings.InsertSpaceBeforeAndAfterBinaryOperators = value; }
    internal Tristate InsertSpaceBeforeFunctionParenthesis { readonly get => FormatCodeSettings.InsertSpaceBeforeFunctionParenthesis; set => FormatCodeSettings.InsertSpaceBeforeFunctionParenthesis = value; }
    internal Tristate InsertSpaceBeforeTypeAnnotation { readonly get => FormatCodeSettings.InsertSpaceBeforeTypeAnnotation; set => FormatCodeSettings.InsertSpaceBeforeTypeAnnotation = value; }
    internal Tristate PlaceOpenBraceOnNewLineForControlBlocks { readonly get => FormatCodeSettings.PlaceOpenBraceOnNewLineForControlBlocks; set => FormatCodeSettings.PlaceOpenBraceOnNewLineForControlBlocks = value; }
    internal Tristate PlaceOpenBraceOnNewLineForFunctions { readonly get => FormatCodeSettings.PlaceOpenBraceOnNewLineForFunctions; set => FormatCodeSettings.PlaceOpenBraceOnNewLineForFunctions = value; }
    internal SemicolonPreference Semicolons { readonly get => FormatCodeSettings.Semicolons; set => FormatCodeSettings.Semicolons = value; }
    internal long BaseIndentSize { readonly get => FormatCodeSettings.EditorSettings.BaseIndentSize; set => FormatCodeSettings.EditorSettings.BaseIndentSize = value; }
    internal Tristate ConvertTabsToSpaces { readonly get => FormatCodeSettings.EditorSettings.ConvertTabsToSpaces; set => FormatCodeSettings.EditorSettings.ConvertTabsToSpaces = value; }
    internal long IndentSize { readonly get => FormatCodeSettings.EditorSettings.IndentSize; set => FormatCodeSettings.EditorSettings.IndentSize = value; }
    internal IndentStyle IndentStyle { readonly get => FormatCodeSettings.EditorSettings.IndentStyle; set => FormatCodeSettings.EditorSettings.IndentStyle = value; }
    internal string NewLineCharacter { readonly get => FormatCodeSettings.EditorSettings.NewLineCharacter; set => FormatCodeSettings.EditorSettings.NewLineCharacter = value; }
    internal long TabSize { readonly get => FormatCodeSettings.EditorSettings.TabSize; set => FormatCodeSettings.EditorSettings.TabSize = value; }
    internal Tristate TrimTrailingWhitespace { readonly get => FormatCodeSettings.EditorSettings.TrimTrailingWhitespace; set => FormatCodeSettings.EditorSettings.TrimTrailingWhitespace = value; }
    internal Tristate IncludeAutomaticOptionalChainCompletions { readonly get => CompletionPreferences.IncludeAutomaticOptionalChainCompletions; set => CompletionPreferences.IncludeAutomaticOptionalChainCompletions = value; }
    internal Tristate IncludeCompletionsForImportStatements { readonly get => CompletionPreferences.IncludeCompletionsForImportStatements; set => CompletionPreferences.IncludeCompletionsForImportStatements = value; }
    internal Tristate IncludeCompletionsForModuleExports { readonly get => CompletionPreferences.IncludeCompletionsForModuleExports; set => CompletionPreferences.IncludeCompletionsForModuleExports = value; }
    internal Tristate IncludeCompletionsWithClassMemberSnippets { readonly get => CompletionPreferences.IncludeCompletionsWithClassMemberSnippets; set => CompletionPreferences.IncludeCompletionsWithClassMemberSnippets = value; }
    internal Tristate IncludeCompletionsWithObjectLiteralMethodSnippets { readonly get => CompletionPreferences.IncludeCompletionsWithObjectLiteralMethodSnippets; set => CompletionPreferences.IncludeCompletionsWithObjectLiteralMethodSnippets = value; }
    internal JsxAttributeCompletionStyle JsxAttributeCompletionStyle { readonly get => CompletionPreferences.JsxAttributeCompletionStyle; set => CompletionPreferences.JsxAttributeCompletionStyle = value; }
    internal Tristate IncludeInlayEnumMemberValueHints { readonly get => InlayHintsPreferences.IncludeInlayEnumMemberValueHints; set => InlayHintsPreferences.IncludeInlayEnumMemberValueHints = value; }
    internal Tristate IncludeInlayFunctionLikeReturnTypeHints { readonly get => InlayHintsPreferences.IncludeInlayFunctionLikeReturnTypeHints; set => InlayHintsPreferences.IncludeInlayFunctionLikeReturnTypeHints = value; }
    internal Tristate IncludeInlayFunctionParameterTypeHints { readonly get => InlayHintsPreferences.IncludeInlayFunctionParameterTypeHints; set => InlayHintsPreferences.IncludeInlayFunctionParameterTypeHints = value; }
    internal IncludeInlayParameterNameHints IncludeInlayParameterNameHints { readonly get => InlayHintsPreferences.IncludeInlayParameterNameHints; set => InlayHintsPreferences.IncludeInlayParameterNameHints = value; }
    internal Tristate IncludeInlayParameterNameHintsWhenArgumentMatchesName { readonly get => InlayHintsPreferences.IncludeInlayParameterNameHintsWhenArgumentMatchesName; set => InlayHintsPreferences.IncludeInlayParameterNameHintsWhenArgumentMatchesName = value; }
    internal Tristate IncludeInlayPropertyDeclarationTypeHints { readonly get => InlayHintsPreferences.IncludeInlayPropertyDeclarationTypeHints; set => InlayHintsPreferences.IncludeInlayPropertyDeclarationTypeHints = value; }
    internal Tristate IncludeInlayVariableTypeHints { readonly get => InlayHintsPreferences.IncludeInlayVariableTypeHints; set => InlayHintsPreferences.IncludeInlayVariableTypeHints = value; }
    internal Tristate IncludeInlayVariableTypeHintsWhenTypeMatchesName { readonly get => InlayHintsPreferences.IncludeInlayVariableTypeHintsWhenTypeMatchesName; set => InlayHintsPreferences.IncludeInlayVariableTypeHintsWhenTypeMatchesName = value; }
    internal Tristate ImplementationsCodeLensEnabled { readonly get => CodeLensUserPreferences.ImplementationsCodeLensEnabled; set => CodeLensUserPreferences.ImplementationsCodeLensEnabled = value; }
    internal Tristate ImplementationsCodeLensShowOnAllClassMethods { readonly get => CodeLensUserPreferences.ImplementationsCodeLensShowOnAllClassMethods; set => CodeLensUserPreferences.ImplementationsCodeLensShowOnAllClassMethods = value; }
    internal Tristate ImplementationsCodeLensShowOnInterfaceMethods { readonly get => CodeLensUserPreferences.ImplementationsCodeLensShowOnInterfaceMethods; set => CodeLensUserPreferences.ImplementationsCodeLensShowOnInterfaceMethods = value; }
    internal Tristate ReferencesCodeLensEnabled { readonly get => CodeLensUserPreferences.ReferencesCodeLensEnabled; set => CodeLensUserPreferences.ReferencesCodeLensEnabled = value; }
    internal Tristate ReferencesCodeLensShowOnAllFunctions { readonly get => CodeLensUserPreferences.ReferencesCodeLensShowOnAllFunctions; set => CodeLensUserPreferences.ReferencesCodeLensShowOnAllFunctions = value; }
}

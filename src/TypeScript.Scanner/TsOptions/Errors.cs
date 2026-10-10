// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/errors.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.TsOptions;
public static partial class TsOptions
{
    internal static Diagnostic createDiagnosticForInvalidEnumType(CommandLineOption option, SourceFile? sourceFile, Node? node) => CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(sourceFile, node, Diagnostics.Argument_for_0_option_must_be_Colon_1, "--" + option.Name, formatEnumTypeKeys(option, option.EnumMap()?.Keys().ToList() ?? new List<string>()));
    internal static string formatEnumTypeKeys(CommandLineOption option, List<string> keys)
    {
        var deprecated = option.DeprecatedKeys();
        if (deprecated != null)
            keys = keys.Where(key => !deprecated.Contains(key)).ToList();
        return "'" + string.Join("', '", keys) + "'";
    }

    internal static string getCompilerOptionValueTypeString(CommandLineOption option) => option.Kind switch
    {
        CommandLineOptionKind.ListOrElement => getCompilerOptionValueTypeString(option.Elements()!) + " or Array",
        CommandLineOptionKind.List => "Array",
        _ => option.Kind.String(),
    };
    internal static Diagnostic createUnknownOptionError(string unknownOption, Message unknownDiagnostic, string errorText, Node? node, SourceFile? sourceFile, AlternateModeDiagnostics? alternateMode, Message? didYouMean, CommandLineOptionNameMap? optionMap)
    {
        var otherOption = alternateMode?.optionsNameMap?.Get(goLower(unknownOption));
        if (otherOption != null)
            return CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(sourceFile, node, otherOption.Name == "build" ? Diagnostics.Option_build_must_be_the_first_command_line_argument : alternateMode!.diagnostic!, unknownOption);
        if (errorText == "")
            errorText = unknownOption;
        var possibleOption = didYouMean != null ? optionMap?.GetSpellingSuggestion(unknownOption) : null;
        return possibleOption != null ? CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(sourceFile, node, didYouMean!, errorText, possibleOption.Name) : CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(sourceFile, node, unknownDiagnostic, errorText);
    }

    public static Diagnostic CreateDiagnosticForNodeInSourceFile(SourceFile sourceFile, Node node, Message message, params object[] args) => Ast.NewDiagnostic(sourceFile, new TextRange(global::TypeScript.Scanner.Scanner.SkipTriviaByteString(sourceFile.TextView(), node.Pos()), node.End()), message, args);
    public static Diagnostic CreateDiagnosticForNodeInSourceFileOrCompilerDiagnostic(SourceFile? sourceFile, Node? node, Message message, params object[] args) => sourceFile != null && node != null ? CreateDiagnosticForNodeInSourceFile(sourceFile, node, message, args) : Ast.NewCompilerDiagnostic(message, args);
    internal static Message? extraKeyDiagnostics(string key) => key switch
    {
        "compilerOptions" => Diagnostics.Unknown_compiler_option_0,
        "typeAcquisition" => Diagnostics.Unknown_type_acquisition_option_0,
        "buildOptions" => Diagnostics.Unknown_build_option_0,
        _ => null,
    };
    internal static Message? extraKeyDidYouMeanDiagnostics(string key) => key switch
    {
        "compilerOptions" => Diagnostics.Unknown_compiler_option_0_Did_you_mean_1,
        "typeAcquisition" => Diagnostics.Unknown_type_acquisition_option_0_Did_you_mean_1,
        "buildOptions" => Diagnostics.Unknown_build_option_0_Did_you_mean_1,
        _ => null,
    };
}

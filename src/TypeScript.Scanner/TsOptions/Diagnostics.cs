// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/diagnostics.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.TsOptions;
public sealed class DidYouMeanOptionsDiagnostics
{
    internal AlternateModeDiagnostics? alternateMode;
    public List<CommandLineOption> OptionDeclarations = new();
    public Message? UnknownOptionDiagnostic;
    public Message? UnknownDidYouMeanDiagnostic;
}

public sealed class AlternateModeDiagnostics
{
    internal Message? diagnostic;
    internal NameMap? optionsNameMap;
}

public sealed class ParseCommandLineWorkerDiagnostics
{
    internal DidYouMeanOptionsDiagnostics didYouMean = new();
    internal NameMap? optionsNameMap;
    internal readonly object optionsNameMapLock = new();
    public Message? OptionTypeMismatchDiagnostic;
}

public static partial class TsOptions
{
    // Lazy construction preserves Go dependency initialization across managed partial files.
    private static readonly Lazy<ParseCommandLineWorkerDiagnostics> compilerDiagnostics = new(() => getParseCommandLineWorkerDiagnostics(OptionsDeclarations!));
    public static ParseCommandLineWorkerDiagnostics CompilerOptionsDidYouMeanDiagnostics => compilerDiagnostics.Value;

    private static readonly Lazy<ParseCommandLineWorkerDiagnostics> buildDiagnostics = new(() => new() { didYouMean = new() { alternateMode = new() { diagnostic = Diagnostics.Compiler_option_0_may_not_be_used_with_build, optionsNameMap = CompilerNameMap }, OptionDeclarations = BuildOpts!, UnknownOptionDiagnostic = Diagnostics.Unknown_build_option_0, UnknownDidYouMeanDiagnostic = Diagnostics.Unknown_build_option_0_Did_you_mean_1, }, OptionTypeMismatchDiagnostic = Diagnostics.Build_option_0_requires_a_value_of_type_1, });
    internal static ParseCommandLineWorkerDiagnostics buildOptionsDidYouMeanDiagnostics => buildDiagnostics.Value;

    internal static ParseCommandLineWorkerDiagnostics getParseCommandLineWorkerDiagnostics(List<CommandLineOption> declarations) => new()
    {
        didYouMean = new()
        {
            alternateMode = new()
            {
                diagnostic = Diagnostics.Compiler_option_0_may_only_be_used_with_build,
                optionsNameMap = BuildNameMap
            },
            OptionDeclarations = declarations,
            UnknownOptionDiagnostic = Diagnostics.Unknown_compiler_option_0,
            UnknownDidYouMeanDiagnostic = Diagnostics.Unknown_compiler_option_0_Did_you_mean_1,
        },
        OptionTypeMismatchDiagnostic = Diagnostics.Compiler_option_0_expects_an_argument,
    };
}

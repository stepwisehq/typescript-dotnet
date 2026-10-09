// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/commandlineparser.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Globalization;
using TypeScript.Collections;
using TypeScript.Scanner;
using TypeScript.Vfs;

namespace TypeScript.TsOptions;
internal sealed class commandLineParser
{
    internal ParseCommandLineWorkerDiagnostics workerDiagnostics = null!;
    internal NameMap optionsMap = null!;
    internal IFS fs = null!;
    internal RootedDirectoryPath currentDirectory;
    internal OrderedMap<string, object?> options = new();
    internal List<string> fileNames = new();
    internal List<Diagnostic> errors = new();
    internal HashSet<PathKey> responseFileStack = new();
    internal AlternateModeDiagnostics? AlternateMode() => workerDiagnostics.didYouMean.alternateMode;
    internal List<CommandLineOption> OptionsDeclarations() => workerDiagnostics.didYouMean.OptionDeclarations;
    internal Message UnknownOptionDiagnostic() => workerDiagnostics.didYouMean.UnknownOptionDiagnostic!;
    internal Message UnknownDidYouMeanDiagnostic() => workerDiagnostics.didYouMean.UnknownDidYouMeanDiagnostic!;
    internal Diagnostic createUnknownOptionError(string unknownOption, string errorText, Node? node, SourceFile? sourceFile) => TsOptions.createUnknownOptionError(unknownOption, UnknownOptionDiagnostic(), errorText, node, sourceFile, AlternateMode(), UnknownDidYouMeanDiagnostic(), TsOptions.commandLineOptionsToMap(OptionsDeclarations()));
    internal void parseStrings(List<string>? args)
    {
        int index = 0;
        while (index < (args?.Count ?? 0))
        {
            string text = args![index++];
            if (text == "")
                continue;
            switch (text[0])
            {
                case '@':
                    parseResponseFile(text[1..]);
                    break;
                case '-':
                    var name = TsOptions.getInputOptionName(text);
                    var option = optionsMap.GetOptionDeclarationFromName(name, true);
                    if (option != null)
                        index = parseOptionValue(args, index, option, workerDiagnostics.OptionTypeMismatchDiagnostic!);
                    else
                        errors.Add(createUnknownOptionError(name, text, null, null));
                    break;
                default:
                    fileNames.Add(text);
                    break;
            }
        }
    }

    internal void parseResponseFile(string fileName)
    {
        var typedFileName = TsPath.RootedFilePathFromPath(currentDirectory.AsPath());
        if (fileName != "")
            typedFileName = TsPath.ToRootedFilePath(fileName, currentDirectory);
        var path = fs.CaseSensitivity().PathKey(typedFileName.AsPath());
        if (!responseFileStack.Add(path))
            return;
        try
        {
            var(contents, readErrors) = TsOptions.tryReadFile(typedFileName, name => fs == null ? ("", false) : fs.ReadFile(name), errors);
            errors = readErrors!;
            if (contents == "")
                return;
            List<string>? args = null;
            var text = GoString.Runes(contents).ToList();
            int position = 0;
            while (position < text.Count)
            {
                while (position < text.Count && text[position] <= ' ')
                    position++;
                if (position >= text.Count)
                    break;
                int start = position;
                if (text[position] == '"')
                {
                    position++;
                    while (position < text.Count && text[position] != '"')
                        position++;
                    if (position < text.Count)
                    {
                        (args ??= new()).Add(TsOptions.runesToGoString(text, start + 1, position));
                        position++;
                    }
                    else
                        errors.Add(Ast.NewCompilerDiagnostic(Diagnostics.Unterminated_quoted_string_in_response_file_0, fileName));
                }
                else
                {
                    while (position < text.Count && text[position] > ' ')
                        position++;
                    (args ??= new()).Add(TsOptions.runesToGoString(text, start, position));
                }
            }

            parseStrings(args);
        }
        finally
        {
            responseFileStack.Remove(path);
        }
    }

    internal int parseOptionValue(List<string> args, int index, CommandLineOption option, Message diagnostic)
    {
        if (option.IsTSConfigOnly && index <= args.Count)
        {
            string value = index < args.Count ? args[index] : "";
            if (value == "null")
            {
                options.Set(option.Name, null);
                index++;
            }
            else if (option.Kind == CommandLineOptionKind.Boolean)
            {
                if (value == "false")
                {
                    options.Set(option.Name, false);
                    index++;
                }
                else
                {
                    if (value == "true")
                        index++;
                    errors.Add(Ast.NewCompilerDiagnostic(Diagnostics.Option_0_can_only_be_specified_in_tsconfig_json_file_or_set_to_false_or_null_on_command_line, option.Name));
                }
            }
            else
            {
                errors.Add(Ast.NewCompilerDiagnostic(Diagnostics.Option_0_can_only_be_specified_in_tsconfig_json_file_or_set_to_null_on_command_line, option.Name));
                if (value.Length != 0 && !value.StartsWith('-'))
                    index++;
            }
        }
        else
        {
            if (index >= args.Count)
            {
                if (option.Kind != CommandLineOptionKind.Boolean)
                {
                    errors.Add(Ast.NewCompilerDiagnostic(diagnostic, option.Name, TsOptions.getCompilerOptionValueTypeString(option)));
                    if (option.Kind == CommandLineOptionKind.List)
                        options.Set(option.Name, new List<string>());
                    else if (option.Kind == CommandLineOptionKind.Enum)
                        errors.Add(TsOptions.createDiagnosticForInvalidEnumType(option, null, null));
                }
                else
                    options.Set(option.Name, true);
                return index;
            }

            if (args[index] != "null")
            {
                switch (option.Kind)
                {
                    case CommandLineOptionKind.Number:
                        if (long.TryParse(args[index], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long number))
                        {
                            if (number >= option.minValue)
                                options.Set(option.Name, number);
                            else
                                errors.Add(Ast.NewCompilerDiagnostic(Diagnostics.Option_0_requires_value_to_be_greater_than_1, option.Name, option.minValue.ToString(CultureInfo.InvariantCulture)));
                        }
                        else
                            errors.Add(Ast.NewCompilerDiagnostic(diagnostic, option.Name, "number"));
                        index++;
                        break;
                    case CommandLineOptionKind.Boolean:
                        options.Set(option.Name, args[index] != "false");
                        if (args[index] is "false" or "true")
                            index++;
                        break;
                    case CommandLineOptionKind.String:
                        var(stringValue, stringErrors) = TsOptions.validateJsonOptionValue(option, args[index], null, null);
                        if (stringErrors == null)
                            options.Set(option.Name, stringValue);
                        else
                            errors.AddRange(stringErrors);
                        index++;
                        break;
                    case CommandLineOptionKind.List:
                        var(list, listErrors) = parseListTypeOption(option, args[index]);
                        options.Set(option.Name, list);
                        if (listErrors != null)
                            errors.AddRange(listErrors);
                        if ((list?.Count ?? 0) > 0 || (listErrors?.Count ?? 0) > 0)
                            index++;
                        break;
                    case CommandLineOptionKind.ListOrElement:
                        throw new InvalidOperationException("listOrElement not supported here");
                    default:
                        var(enumValue, enumErrors) = TsOptions.convertJsonOptionOfEnumType(option, TsOptions.trimGoRunes(args[index], StringUtil.IsWhiteSpaceLike), null, null);
                        options.Set(option.Name, enumValue);
                        if (enumErrors != null)
                            errors.AddRange(enumErrors);
                        index++;
                        break;
                }
            }
            else
            {
                options.Set(option.Name, null);
                index++;
            }
        }

        return index;
    }

    internal (List<object?>? Values, List<Diagnostic>? Errors) parseListTypeOption(CommandLineOption option, string value) => TsOptions.ParseListTypeOption(option, value);
}

public static partial class TsOptions
{
    public static ParsedCommandLine ParseCommandLine(List<string>? args, IFS fs, RootedDirectoryPath currentDirectory)
    {
        args ??= new();
        var parser = parseCommandLineWorker(CompilerOptionsDidYouMeanDiagnostics, args, fs, currentDirectory);
        var options = convertToOptionsWithAbsolutePaths(parser.options.Clone(), CommandLineCompilerOptionsMap, currentDirectory);
        var compilerOptions = convertMapToOptions(options, new compilerOptionsParser()).CompilerOptions;
        var result = NewParsedCommandLine(compilerOptions, Core.Map(parser.fileNames, fileName => TsPath.ToRootedFilePath(fileName, currentDirectory)), null, currentDirectory, fs.CaseSensitivity());
        result.rootFileNamesForDiagnostics = parser.fileNames;
        result.Errors = parser.errors;
        result.Raw = parser.options;
        return result;
    }

    public static ParsedBuildCommandLine ParseBuildCommandLine(List<string>? args, IFS fs, RootedDirectoryPath currentDirectory)
    {
        args ??= new();
        var parser = parseCommandLineWorker(buildOptionsDidYouMeanDiagnostics, args, fs, currentDirectory);
        var compilerOptions = new CompilerOptions();
        foreach (var(key, value)in parser.options.Entries())
        {
            var buildOption = BuildNameMap.Get(key);
            if (ReferenceEquals(buildOption, TscBuildOption) || ReferenceEquals(buildOption, CompilerNameMap.Get(key)))
                ParseCompilerOptions(key, value, compilerOptions);
        }

        var result = new ParsedBuildCommandLine
        {
            BuildOptions = convertMapToOptions(parser.options, new buildOptionsParser()).BuildOptions,
            CompilerOptions = compilerOptions,
            Projects = parser.fileNames,
            Errors = parser.errors,
            Raw = parser.options,
            currentDirectory = currentDirectory,
        };
        if (result.Projects.Count == 0)
            result.Projects.Add(".");
        if (result.BuildOptions.Clean == Tristate.True && result.BuildOptions.Force == Tristate.True)
            result.Errors.Add(Ast.NewCompilerDiagnostic(Diagnostics.Options_0_and_1_cannot_be_combined, "clean", "force"));
        if (result.BuildOptions.Clean == Tristate.True && result.BuildOptions.Verbose == Tristate.True)
            result.Errors.Add(Ast.NewCompilerDiagnostic(Diagnostics.Options_0_and_1_cannot_be_combined, "clean", "verbose"));
        if (result.BuildOptions.Clean == Tristate.True && result.CompilerOptions.Watch == Tristate.True)
            result.Errors.Add(Ast.NewCompilerDiagnostic(Diagnostics.Options_0_and_1_cannot_be_combined, "clean", "watch"));
        if (result.CompilerOptions.Watch == Tristate.True && result.BuildOptions.Dry == Tristate.True)
            result.Errors.Add(Ast.NewCompilerDiagnostic(Diagnostics.Options_0_and_1_cannot_be_combined, "watch", "dry"));
        return result;
    }

    internal static commandLineParser parseCommandLineWorker(ParseCommandLineWorkerDiagnostics diagnostics, List<string>? args, IFS fs, RootedDirectoryPath currentDirectory)
    {
        var parser = new commandLineParser
        {
            fs = fs,
            currentDirectory = currentDirectory,
            workerDiagnostics = diagnostics
        };
        parser.optionsMap = GetNameMapFromList(parser.OptionsDeclarations());
        parser.parseStrings(args);
        return parser;
    }

    internal static string getInputOptionName(string input)
    {
        if (input.StartsWith('-'))
            input = input[1..];
        if (input.StartsWith('-'))
            input = input[1..];
        return input;
    }

    internal static (string Text, List<Diagnostic>? Errors) tryReadFile(RootedFilePath fileName, Func<RootedFilePath, (string Text, bool Ok)> readFile, List<Diagnostic>? errors)
    {
        var(text, ok) = readFile(fileName);
        if (!ok)
        {
            text = "";
            (errors ??= new()).Add(Ast.NewCompilerDiagnostic(Diagnostics.Cannot_read_file_0, fileName.AsString()));
        }

        return (text, errors);
    }

    public static (List<object?>? Values, List<Diagnostic>? Errors) ParseListTypeOption(CommandLineOption option, string value)
    {
        value = trimGoRunes(value, isGoWhiteSpace);
        List<Diagnostic>? errors = null;
        if (value.StartsWith('-'))
            return (new(), errors);
        if (option.Kind == CommandLineOptionKind.ListOrElement && !value.Contains(','))
        {
            var(converted, conversionErrors) = validateJsonOptionValue(option, value, null, null);
            return conversionErrors != null ? (new(), conversionErrors) : (new() { (string)converted! }, errors);
        }

        if (value == "")
            return (new(), errors);
        var element = option.Elements()!;
        if (element.Kind is CommandLineOptionKind.Boolean or CommandLineOptionKind.Object or CommandLineOptionKind.Number)
            throw new InvalidOperationException("List of " + element.Kind.String() + " is not yet supported.");
        List<object?>? result = null;
        foreach (var item in value.Split(','))
        {
            var(converted, conversionErrors) = element.Kind == CommandLineOptionKind.String ? validateJsonOptionValue(element, item, null, null) : convertJsonOptionOfEnumType(element, trimGoRunes(item, StringUtil.IsWhiteSpaceLike), null, null);
            if (converted is string text && (conversionErrors?.Count ?? 0) == 0 && text.Length != 0)
                (result ??= new()).Add(text);
            else if (conversionErrors != null)
                (errors ??= new()).AddRange(conversionErrors);
        }

        return (result, errors);
    }

    internal static (object? Value, List<Diagnostic>? Errors) convertJsonOptionOfEnumType(CommandLineOption option, string value, Node? valueExpression, SourceFile? sourceFile)
    {
        if (value == "")
            return (null, null);
        var map = option.EnumMap();
        if (map == null)
            return (null, null);
        var(converted, ok) = map.Get(goLower(value));
        return ok ? validateJsonOptionValue(option, converted, valueExpression, sourceFile) : (null, new() { createDiagnosticForInvalidEnumType(option, sourceFile, valueExpression) });
    }

    internal static string runesToGoString(List<int> runes, int start, int end)
    {
        var builder = new GoBuilder();
        for (int i = start; i < end; i++)
            builder.WriteRune(runes[i]);
        return builder.String();
    }

    internal static string trimGoRunes(string bytes, Func<int, bool> predicate)
    {
        // strings.TrimFunc removes only the matching edge rune byte ranges, retaining all interior bytes.
        int start = 0, end = bytes.Length;
        while (start < end)
        {
            var(rune, width) = Utf8.DecodeRuneInString(bytes[start..end]);
            if (!predicate(rune))
                break;
            start += width;
        }

        while (start < end)
        {
            var(rune, width) = Utf8.DecodeLastRuneInString(bytes[start..end]);
            if (!predicate(rune))
                break;
            end -= width;
        }

        return bytes[start..end];
    }

    internal static bool isGoWhiteSpace(int rune) => rune is >= 0x09 and <= 0x0D or 0x20 or 0x85 or 0xA0 or 0x1680 or >= 0x2000 and <= 0x200A or 0x2028 or 0x2029 or 0x202F or 0x205F or 0x3000;
}

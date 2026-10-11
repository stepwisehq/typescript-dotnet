// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/parsinghelpers.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Collections;
using System.Runtime.CompilerServices;
using TypeScript.Scanner;

namespace TypeScript.TsOptions;
public static partial class TsOptions
{
    public static Tristate ParseTristate(object? value) => value == null ? Tristate.Unknown : value is Tristate state ? state : value is true ? Tristate.True : Tristate.False;
    public static List<string>? ParseStringArray(object? value) => value is List<object?> values ? values.OfType<string>().ToList() : null;
    public static string ParseString(object? value) => value as string ?? "";
    internal static OrderedMap<string, List<string>?>? parseStringMap(object? value)
    {
        if (value is not OrderedMap<string, object?> map)
            return null;
        var result = new OrderedMap<string, List<string>?>(map.Size());
        foreach (var(key, item)in map.Entries())
            result.Set(key, ParseStringArray(item));
        return result;
    }

    internal static RootedFilePath parseFileName(object? value)
    {
        var path = parsePathText(value);
        return path.Length == 0 ? default : TsPath.RootedFilePathFromNormalized(path);
    }

    internal static RootedDirectoryPath parseDirectoryName(object? value)
    {
        var path = parsePathText(value);
        return path.Length == 0 ? default : TsPath.RootedDirectoryPathFromNormalized(path);
    }

    internal static RootedPath parseFileOrDirectoryName(object? value)
    {
        var path = parsePathText(value);
        return path.Length == 0 ? default : TsPath.RootedPathFromNormalized(path);
    }

    internal static ByteStringValue parsePathText(object? value) => value is ByteStringValue bytes ? bytes : ByteStringValue.FromString(value as string ?? "");
    internal static List<ByteStringValue>? parsePathTexts(object? value) => value is List<object?> values ? values.Where(static item => item is string or ByteStringValue).Select(parsePathText).ToList() : null;
    internal static List<RootedDirectoryPath>? parseDirectoryNames(object? value) => parsePathTexts(value)?.Select(static path => path.Length == 0 ? default : TsPath.RootedDirectoryPathFromNormalized(path)).ToList();
    internal static StrongBox<long>? parseNumber(object? value) => value switch
    {
        long integer => new(integer),
        int integer => new(integer), // Existing managed callers' small Go int values.
        double number => new(goFloatToInt64(number)),
        _ => null,
    };
    // The pinned amd64 Go conversion produces MinInt64 for nonfinite/out-of-range values.
    // JSON option parsing intentionally truncates fractional values; CLI uses integer parsing separately.
    internal static long goFloatToInt64(double number) => double.IsNaN(number) || number >= 9223372036854775808d || number < -9223372036854775808d ? long.MinValue : (long)number;
    internal static int goFloatToInt32(double number) => double.IsNaN(number) || number >= 2147483648d || number < -2147483648d ? int.MinValue : (int)number;
    // A static converter expresses the Go ~int32 cast without reflection/Enum.ToObject under Native AOT.
    internal static T floatOrInt32ToFlag<T>(object? value, Func<int, T> cast) => value is T flag ? flag : cast(goFloatToInt32((double)value!));
    public static void ParseCompilerOptions(string key, object? value, CompilerOptions? options)
    {
        if (value == null || options == null)
            return;
        parseCompilerOptions(key, value, options);
    }

    internal static (List<string>? Values, bool Ok) parseStringArrayStrict(object? value)
    {
        if (value is not List<object?> values)
            return (null, false);
        var result = new List<string>();
        foreach (var item in values)
        {
            if (item is not string text)
                return (null, false);
            result.Add(text);
        }

        return (result, true);
    }

    internal static OrderedMap<string, object?> parseJsonToStringKey(object? value)
    {
        var result = new OrderedMap<string, object?>(6);
        if (value is OrderedMap<string, object?> map)
        {
            foreach (var key in new[]
            {
                "include",
                "exclude",
                "files",
                "references",
                "contentMappers",
                "extends",
                "compilerOptions",
                "excludes",
                "typeAcquisition"
            }

            )
            {
                var(item, ok) = map.Get(key);
                if (!ok)
                    continue;
                if (key == "extends" && item is string text)
                    result.Set(key, new List<object?> { text });
                result.Set(key, item);
            }
        }

        return result;
    }

    internal static OrderedMap<string, object?>? convertToOptionsWithAbsolutePaths(OrderedMap<string, object?>? options, CommandLineOptionNameMap optionMap, RootedDirectoryPath directory)
    {
        if (options == null)
            return null;
        foreach (var(key, value)in options.Entries())
        {
            var(converted, ok) = convertOptionToAbsolutePath(key, value, optionMap, directory);
            if (ok)
                options.Set(key, converted);
        }

        return options;
    }

    public static (object? Value, bool Ok) ConvertOptionToAbsolutePath(string key, object? value, CommandLineOptionNameMap optionMap, RootedDirectoryPath directory)
    {
        var (converted, ok) = convertOptionToAbsolutePath(key, value, optionMap, directory);
        return (converted switch
        {
            ByteStringValue text => text.Materialize(),
            List<ByteStringValue> texts => texts.Select(static text => text.Materialize()!).ToList(),
            List<object?> values => values.Select(static item => item is ByteStringValue text ? (object?)text.Materialize() : item).ToList(),
            _ => converted
        }, ok);
    }
    private static (object? Value, bool Ok) convertOptionToAbsolutePath(string key, object? value, CommandLineOptionNameMap optionMap, RootedDirectoryPath directory)
    {
        var option = optionMap.Get(key);
        if (option == null)
            return (null, false);
        if (option.Kind == CommandLineOptionKind.List)
        {
            if (option.Elements()!.IsFilePath)
            {
                if (value is List<string> strings)
                    return (strings.Select(item => TsPath.GetNormalizedAbsolutePath(ByteStringValue.FromString(item), directory.AsValue())).ToList(), true);
                if (value is List<object?> values)
                    return (values.Select(item => item is string or ByteStringValue ? TsPath.GetNormalizedAbsolutePath(parsePathText(item), directory.AsValue()) : item).ToList(), true);
            }
        }
        else if (option.IsFilePath && value is string or ByteStringValue)
            return (TsPath.GetNormalizedAbsolutePath(parsePathText(value), directory.AsValue()), true);
        return (null, false);
    }

    internal static projectReferenceParseResult? parseProjectReference(object? value)
    {
        if (value is not OrderedMap<string, object?> map)
            return null;
        var result = new projectReferenceParseResult();
        var(path, hasPath) = map.Get("path");
        if (hasPath)
        {
            result.hasPath = true;
            if (path is string pathString)
            {
                result.path = pathString;
                result.pathValid = true;
            }
        }

        var(circular, hasCircular) = map.Get("circular");
        if (hasCircular)
        {
            result.hasCircular = true;
            if (circular is bool flag)
            {
                result.reference.Circular = flag;
                result.circularValid = true;
            }
        }

        return result;
    }

    internal static (TypeScript.ContentMapper.Mapper? Mapper, Slice<Diagnostic> Errors) parseContentMapper(object? value)
    {
        if (value is not OrderedMap<string, object?> map)
            return (null, default);
        Slice<Diagnostic> errors = default;
        var mapper = new TypeScript.ContentMapper.Mapper();
        var(package, hasPackage) = map.Get("package");
        if (hasPackage && package is string text && text != "")
            mapper.Package = text;
        else
            errors = errors.Append(Ast.NewCompilerDiagnostic(Diagnostics.Compiler_option_0_requires_a_value_of_type_1, "contentMapper.package", "string"));
        var(extensions, hasExtensions) = map.Get("extensions");
        var(strings, isStringArray) = parseStringArrayStrict(extensions);
        if (hasExtensions && isStringArray)
            mapper.Extensions = strings;
        else
            errors = errors.Append(Ast.NewCompilerDiagnostic(Diagnostics.Compiler_option_0_requires_a_value_of_type_1, "contentMapper.extensions", "string[]"));
        var(options, hasOptions) = map.Get("options");
        if (hasOptions)
        {
            if (options is not OrderedMap<string, object?>)
                errors = errors.Append(Ast.NewCompilerDiagnostic(Diagnostics.Compiler_option_0_requires_a_value_of_type_1, "contentMapper.options", "object"));
            else
            {
                var(encoded, _) = TypeScript.Json.JsonCodec.TryMarshal(options);
                if (encoded != null)
                    mapper.Options = new TypeScript.Json.JsonValue(encoded);
            }
        }

        return errors is { Length: > 0 } ? (null, errors) : (mapper, errors);
    }

    internal static bool compilerOptionContainsConfigDirTemplate(string key, object? value)
    {
        var option = CommandLineCompilerOptionsMap.Get(key);
        if (option == null)
            return false;
        var pathKind = option.PathKind;
        if (option.Kind == CommandLineOptionKind.List)
        {
            var element = option.Elements();
            if (element == null)
                return false;
            pathKind = element.PathKind;
        }

        if (!pathKind.IsRooted())
            return false;
        return option.Kind == CommandLineOptionKind.List ? parsePathTexts(value)?.Any(startsWithConfigDirTemplate) == true : value is string or ByteStringValue && startsWithConfigDirTemplate(parsePathText(value));
    }

    internal static CompilerOptions? mergeCompilerOptions(CompilerOptions? targetOptions, CompilerOptions? sourceOptions, object? rawSource)
    {
        if (sourceOptions == null)
            return targetOptions;
        HashSet<string>? explicitNullFields = null;
        if (rawSource is OrderedMap<string, object?> rawMap && rawMap.GetOrZero("compilerOptions")is OrderedMap<string, object?> compilerOptionsMap)
        {
            foreach (var(key, value)in compilerOptionsMap.Entries())
                if (value == null)
                    (explicitNullFields ??= new()).Add(key);
        }

        mergeCompilerOptionFields(targetOptions!, sourceOptions, explicitNullFields);
        return targetOptions;
    }

    internal static parsedCompilerOptions? mergeParsedCompilerOptions(parsedCompilerOptions? targetOptions, parsedCompilerOptions? sourceOptions, object? rawSource)
    {
        if (sourceOptions == null)
            return targetOptions;
        targetOptions ??= new()
        {
            CompilerOptions = new(),
            unresolvedPaths = new()
        };
        mergeCompilerOptions(targetOptions.CompilerOptions, sourceOptions.CompilerOptions, rawSource);
        if (rawSource is OrderedMap<string, object?> rawMap && rawMap.GetOrZero("compilerOptions")is OrderedMap<string, object?> compilerOptionsMap)
            foreach (var key in compilerOptionsMap.Keys())
                targetOptions.unresolvedPaths?.Remove(key);
        if (sourceOptions.unresolvedPaths != null)
        {
            // Original maps.Copy requires a nonnil target when there are entries.
            foreach (var(key, value)in sourceOptions.unresolvedPaths)
                targetOptions.unresolvedPaths![key] = value;
        }

        return targetOptions;
    }
}

internal interface IOptionParser
{
    void ParseOption(string key, object? value);
    Message UnknownOptionDiagnostic();
    Message UnknownDidYouMeanDiagnostic();
}

internal sealed class compilerOptionsParser : IOptionParser
{
    internal CompilerOptions CompilerOptions = new();
    internal Dictionary<string, object?>? unresolvedPaths;
    public void ParseOption(string key, object? value)
    {
        if (unresolvedPaths != null && TsOptions.compilerOptionContainsConfigDirTemplate(key, value))
        {
            unresolvedPaths[key] = value;
            return;
        }

        TsOptions.ParseCompilerOptions(key, value, CompilerOptions);
    }

    public Message UnknownOptionDiagnostic() => TsOptions.extraKeyDiagnostics("compilerOptions")!;
    public Message UnknownDidYouMeanDiagnostic() => TsOptions.extraKeyDidYouMeanDiagnostics("compilerOptions")!;
}

internal sealed class typeAcquisitionParser : IOptionParser
{
    internal TypeAcquisition TypeAcquisition = new();
    public void ParseOption(string key, object? value) => TsOptions.ParseTypeAcquisition(key, value, TypeAcquisition);
    public Message UnknownOptionDiagnostic() => TsOptions.extraKeyDiagnostics("typeAcquisition")!;
    public Message UnknownDidYouMeanDiagnostic() => TsOptions.extraKeyDidYouMeanDiagnostics("typeAcquisition")!;
}

internal sealed class buildOptionsParser : IOptionParser
{
    internal BuildOptions BuildOptions = new();
    public void ParseOption(string key, object? value) => TsOptions.ParseBuildOptions(key, value, BuildOptions);
    public Message UnknownOptionDiagnostic() => TsOptions.extraKeyDiagnostics("buildOptions")!;
    public Message UnknownDidYouMeanDiagnostic() => TsOptions.extraKeyDidYouMeanDiagnostics("buildOptions")!;
}

internal sealed class projectReferenceParseResult
{
    internal ProjectReference reference = new();
    internal string path = "";
    internal bool hasPath, pathValid, hasCircular, circularValid;
}

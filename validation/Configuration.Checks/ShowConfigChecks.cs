// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete showconfig_test.go reflection domains translated to the independently
// source-generated 132-field test table. No reflection or production serializer
// output supplies the reference expectation.
using System.Collections;
using System.Runtime.CompilerServices;
using TypeScript.Collections;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using Options = TypeScript.TsOptions.TsOptions;

namespace Configuration.Checks;
public static class ShowConfigChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestShowConfigSerialization", "TestShowConfigEnumSerialization"];

    public static void Run(string name)
    {
        if (name == "TestShowConfigSerialization")
            CompilerOptionChecks.ShowSerialization();
        else if (name == "TestShowConfigEnumSerialization")
            CompilerOptionChecks.ShowEnums();
        else
            throw new InvalidOperationException("Unknown original showconfig test: " + name);
    }
}

public static partial class CompilerOptionChecks
{
    private static object Numeric(string type, int value) => type switch
    {
        "ScriptTarget" => (ScriptTarget)value,
        "JsxEmit" => (JsxEmit)value,
        "ModuleKind" => (ModuleKind)value,
        "ModuleResolutionKind" => (ModuleResolutionKind)value,
        "ModuleDetectionKind" => (ModuleDetectionKind)value,
        "NewLineKind" => (NewLineKind)value,
        "Tristate" => (Tristate)value,
        _ => throw new InvalidOperationException("Unknown original numeric option domain: " + type)};
    private static object StringValue(string type, string value) => type switch
    {
        "string" => value,
        "tspath.RootedFilePath" => RootedFilePath.FromGoString(value),
        "tspath.RootedDirectoryPath" => RootedDirectoryPath.FromGoString(value),
        "tspath.RootedPath" => RootedPath.FromGoString(value),
        "tspath.SourceMapLocation" => SourceMapLocation.FromGoString(value),
        _ => throw new InvalidOperationException("Unknown original string option domain: " + type)};
    internal static void ShowSerialization()
    {
        CheckShow(new());
        CompilerOptions all = new();
        foreach (var field in originalFields)
        {
            // Exact compilerOptionTestValues source domain: zero, 1, 2; string
            // aliases b/c; two separately allocated zero pointers; nil/empty/
            // one-zero-element slices. Equality tables provide these same values.
            List<object?> values = field.EqualityValues().Take(3).ToList();
            var option = Options.CommandLineCompilerOptionsMap.Get(field.Name);
            if (option?.EnumMap()is { } map)
                values.AddRange(map.Values());
            if (field.NumericEnum)
                values.Add(Numeric(field.GoType, -1));
            else if (field.GoType == "Tristate")
                values.Add((Tristate)255);
            else if (field.GoType == "string" || field.GoType.StartsWith("tspath.", StringComparison.Ordinal))
                foreach (string value in new[]
                {
                    "/project/src",
                    "../other",
                    "./local"
                }

                )
                    values.Add(StringValue(field.GoType, value));
            if (field.GoType is "[]string" or "[]tspath.RootedDirectoryPath")
            {
                string[] items = ["lib.es2015.d.ts", "lib.es2016.d.ts", "unknown", "/project/types"];
                values.Add(field.GoType == "[]string" ? items.ToList() : items.Select(RootedDirectoryPath.FromGoString).ToList());
            }

            field.Write(all, values[^1]);
            foreach (var value in values)
            {
                var options = new CompilerOptions();
                field.Write(options, value);
                CheckShow(options);
            }
        }

        CheckShow(all);
        OrderedMap<string, List<string>?> paths = new();
        paths.Set("@/*", ["./src/*", "../shared/*"]);
        paths.Set("empty", []);
        CheckShow(new() { Paths = paths, Plugins = [new() { Name = "plugin" }], MaxNodeModuleJsDepth = new(0) });
    }

    private static void CheckShow(CompilerOptions options)
    {
        var file = RootedFilePath.FromGoString("/project/tsconfig.json");
        var got = Options.serializeCompilerOptions(options, file, CaseSensitivity.CaseSensitive);
        var want = StaticReflectedSerialize(options, file);
        want.Delete("listFiles");
        want.Delete("listEmittedFiles");
        Check.Sequence(got.Keys().ToList(), want.Keys().ToList(), "Original showconfig complete ordered keys");
        foreach (var(name, expected)in want.Entries())
            Check.True(deepValue(got.GetOrZero(name), expected), "Original showconfig independent value " + name);
    }

    private static bool Zero(object? value, string type) => value is null || type switch
    {
        "Tristate" => (Tristate)value == Tristate.Unknown,
        "ScriptTarget" or "JsxEmit" or "ModuleKind" or "ModuleResolutionKind" or "ModuleDetectionKind" or "NewLineKind" => Convert.ToInt32(value) == 0,
        "string" => (string)value == "",
        "tspath.RootedFilePath" => ((RootedFilePath)value).AsString() == "",
        "tspath.RootedDirectoryPath" => ((RootedDirectoryPath)value).AsString() == "",
        "tspath.RootedPath" => ((RootedPath)value).AsString() == "",
        "tspath.SourceMapLocation" => ((SourceMapLocation)value).AsString() == "",
        _ => false // allocated-empty slice/map or a pointer to zero is nonzero
    };
    private static OrderedMap<string, object?> StaticReflectedSerialize(CompilerOptions options, RootedFilePath file)
    {
        OrderedMap<string, object?> result = new(32);
        var configDir = file.Directory();
        foreach (var field in originalFields)
        {
            var option = Options.CommandLineCompilerOptionsMap.Get(field.Name);
            if (option is null || option.Category == Diagnostics.Command_line_Options || option.Category == Diagnostics.Output_Formatting)
                continue;
            object? value = field.Read(options);
            if (Zero(value, field.GoType))
                continue;
            if (option.EnumMap()is { } map)
            {
                foreach (var(key, item)in map.Entries())
                    if (Convert.ToInt64(item) == Convert.ToInt64(value))
                    {
                        result.Set(option.Name, key);
                        break;
                    }

                continue;
            }

            switch (option.Kind)
            {
                case CommandLineOptionKind.ListOrElement:
                    throw new InvalidOperationException("Original listOrElement should not reach serialization assertion");
                case CommandLineOptionKind.List:
                    if (option.Elements()is { } element)
                    {
                        var(strings, paths) = Options.PathValuesAsStrings(value);
                        if (element.PathKind.IsRooted() && paths)
                        {
                            result.Set(option.Name, strings!.Select(path => path == "" ? "" : Options.serializeCompilerOptionPath(RootedPath.FromGoString(TsPath.GetNormalizedAbsolutePath(path, configDir.AsString())), file, CaseSensitivity.CaseSensitive)).ToList());
                            continue;
                        }

                        if (element.EnumMap()is { } enumMap && value is List<string> items)
                        {
                            result.Set(option.Name, items.Select(item =>
                            {
                                foreach (var(key, enumValue)in enumMap.Entries())
                                    if (Equals(enumValue, item))
                                        return key;
                                return item;
                            }).ToList());
                            continue;
                        }
                    }

                    result.Set(option.Name, value);
                    break;
                case CommandLineOptionKind.String:
                    var(pathValue, ok) = Options.PathValueAsString(value);
                    if (option.PathKind.IsRooted() && ok)
                        value = Options.serializeCompilerOptionPath(RootedPath.FromGoString(TsPath.GetNormalizedAbsolutePath(pathValue, configDir.AsString())), file, CaseSensitivity.CaseSensitive);
                    result.Set(option.Name, value);
                    break;
                case CommandLineOptionKind.Boolean:
                    if ((Tristate)value! == Tristate.True)
                        result.Set(option.Name, true);
                    else if ((Tristate)value! == Tristate.False)
                        result.Set(option.Name, false);
                    break;
                default:
                    result.Set(option.Name, value);
                    break;
            }
        }

        return result;
    }

    internal static void ShowEnums()
    {
        foreach (var option in Options.OptionsDeclarations)
        {
            var map = option.EnumMap();
            if (map is null)
                continue;
            Dictionary<object, string> seen = [];
            foreach (var(name, value)in map.Entries())
            {
                Check.True(value is not null, "Original enum declaration value nonnull");
                if (!seen.ContainsKey(value!))
                    seen[value!] = name;
                Check.Equal(Options.serializeCompilerOptionEnum(value), seen[value!], "Original first alias enum serialization " + name);
            }
        }
    }
}

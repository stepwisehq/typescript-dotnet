// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/showconfig.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Collections;
using TypeScript.Scanner;

namespace TypeScript.TsOptions;
public static partial class TsOptions
{
    internal static Func<CompilerOptions, object?> computeFn<T>(Func<CompilerOptions, T> compute) => options => compute(options);
    private sealed record impliedOption(string name, List<string> dependencies, Func<CompilerOptions, object?> compute);
    private static readonly List<impliedOption> impliedOptions = new()
    {
        new("Module", new() { "Target" }, computeFn(options => options.GetEmitModuleKind())),
        new("ModuleResolution", new() { "Module", "Target" }, computeFn(options => options.GetModuleResolutionKind())),
        new("ModuleDetection", new() { "Module", "Target" }, computeFn(options => options.GetEmitModuleDetectionKind())),
        new("IsolatedModules", new() { "VerbatimModuleSyntax" }, computeFn(options => options.GetIsolatedModules())),
        new("PreserveConstEnums", new() { "IsolatedModules", "VerbatimModuleSyntax" }, computeFn(options => options.ShouldPreserveConstEnums())),
        new("Declaration", new() { "Composite" }, computeFn(options => options.GetEmitDeclarations())),
        new("DeclarationMap", new() { "Declaration", "Composite" }, computeFn(options => options.GetAreDeclarationMapsEnabled())),
        new("Incremental", new() { "Composite" }, computeFn(options => options.IsIncremental())),
        new("UseDefineForClassFields", new() { "Target", "Module" }, computeFn(options => options.GetUseDefineForClassFields())),
        new("ResolvePackageJsonExports", new() { "ModuleResolution", "Module", "Target" }, computeFn(options => options.GetResolvePackageJsonExports())),
        new("ResolvePackageJsonImports", new() { "ModuleResolution", "ResolvePackageJsonExports", "Module", "Target" }, computeFn(options => options.GetResolvePackageJsonImports())),
        new("ResolveJsonModule", new() { "ModuleResolution", "Module", "Target" }, computeFn(options => options.GetResolveJsonModule())),
        new("AllowJs", new() { "CheckJs" }, computeFn(options => options.GetAllowJS())),
        new("AllowImportingTsExtensions", new() { "RewriteRelativeImportExtensions" }, computeFn(options => options.GetAllowImportingTsExtensions())),
    };
    public static TSConfig ConvertToTSConfig(ParsedCommandLine parsed, RootedFilePath fileName = default)
    {
        if (fileName.AsValue().Length == 0)
            fileName = parsed.BaseDirectory().ResolveFile("tsconfig.json");
        var sensitivity = parsed.CaseSensitivity();
        List<string>? files = null;
        if (parsed.FileNames() != null)
            foreach (var file in parsed.FileNames()!)
            {
                var(relative, ok) = sensitivity.RelativePathFromFile(fileName, file);
                (files ??= new()).Add(ok ? relative.AsModuleSpecifier().AsString() : file.AsString());
            }

        var optionMap = serializeCompilerOptions(parsed.CompilerOptions(), fileName, sensitivity);
        foreach (var name in new[]
        {
            "showConfig",
            "configFile",
            "configFilePath",
            "help",
            "init",
            "listFilesOnly",
            "listEmittedFiles",
            "project",
            "build",
            "version"
        }

        )
            optionMap.Delete(name);
        addImpliedOptions(optionMap, parsed.CompilerOptions());
        var result = new TSConfig
        {
            CompilerOptions = optionMap
        };
        if (parsed.ProjectReferences()is { Count: > 0 } references)
        {
            foreach (var reference in references)
            {
                var value = new OrderedMap<string, object?>();
                value.Set("path", reference.OriginalPath);
                if (reference.Circular)
                    value.Set("circular", true);
                (result.References ??= new()).Add(value);
            }
        }

        if ((files?.Count ?? 0) > 0)
            result.Files = files;
        if (parsed.getConfigFileSpecs()is { } specs)
        {
            var include = filterSameAsDefaultInclude(Core.Map(specs.validatedIncludeSpecs, value => value.AsString()));
            if ((include?.Count ?? 0) > 0)
                result.Include = include;
            result.Exclude = Core.Map(specs.validatedExcludeSpecs, value => value.AsString());
        }

        if (parsed.CompileOnSave?.Value == true)
            result.CompileOnSave = new(true);
        return result;
    }

    internal static List<string>? filterSameAsDefaultInclude(List<string>? specs) => (specs?.Count ?? 0) == 0 || specs is { Count: 1 } && specs[0] == defaultIncludeSpec ? null : specs;
    internal static void addImpliedOptions(OrderedMap<string, object?> optionMap, CompilerOptions options)
    {
        var provided = new HashSet<string>(optionMap.Keys(), StringComparer.Ordinal);
        var defaultOptions = new CompilerOptions();
        foreach (var entry in impliedOptions)
        {
            var declaration = CommandLineCompilerOptionsMap.Get(entry.name);
            if (declaration == null || provided.Contains(declaration.Name) || !anyDependencyProvided(entry.dependencies, provided))
                continue;
            var implied = entry.compute(options);
            if (Equals(implied, entry.compute(defaultOptions)))
                continue;
            var serialized = serializeImpliedOptionValue(declaration, implied);
            if (serialized != null)
                optionMap.Set(declaration.Name, serialized);
        }
    }

    internal static bool anyDependencyProvided(List<string> dependencies, HashSet<string> provided)
    {
        foreach (var dependency in dependencies)
        {
            var declaration = CommandLineCompilerOptionsMap.Get(dependency);
            if (declaration != null && provided.Contains(declaration.Name))
                return true;
        }

        return false;
    }

    internal static object? serializeImpliedOptionValue(CommandLineOption declaration, object? value)
    {
        if (value == null)
            return null;
        if (declaration.EnumMap() != null)
        {
            string serialized = serializeCompilerOptionEnum(value);
            return serialized != "" ? serialized : null;
        }

        return value is Tristate state ? state == Tristate.True ? true : state == Tristate.False ? false : null : value;
    }

    internal static string getNameOfCompilerOptionValue(object? value, OrderedMap<string, object?>? enumMap)
    {
        if (enumMap != null)
            foreach (var(key, item)in enumMap.Entries())
                if (Equals(item, value))
                    return key;
        return "";
    }

    internal static string serializeCompilerOptionPath(RootedPath value, RootedFilePath configFilePath, CaseSensitivity caseSensitivity)
    {
        var(relative, ok) = caseSensitivity.RelativePathFromFileToPath(configFilePath, value);
        return ok ? relative.AsModuleSpecifier().AsString() : value.AsString();
    }

    internal static List<string> serializeCompilerOptionEnumList(List<string> values, OrderedMap<string, object?> enumMap) => values.Select(value =>
    {
        var name = getNameOfCompilerOptionValue(value, enumMap);
        return name == "" ? value : name;
    }).ToList();
}

public sealed class TSConfig
{
    public OrderedMap<string, object?> CompilerOptions = new();
    public List<object?>? References;
    public List<string>? Files, Include, Exclude;
    public System.Runtime.CompilerServices.StrongBox<bool>? CompileOnSave;
}

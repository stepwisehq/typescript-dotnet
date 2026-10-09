// Authored AOT-safe configuration verification visibility; not an upstream algorithm.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using System.Runtime.CompilerServices;
using TypeScript.Scanner;
using TypeScript.ContentMapper;

[assembly: InternalsVisibleTo("Configuration.Checks")]
[assembly: InternalsVisibleTo("Configuration.Validation")]
namespace TypeScript.TsOptions;
public readonly record struct ConfigurationStoredField(string Name, object? Value);
public sealed class ConfigurationExtendedEntry
{
    public SourceFile? Source;
    public List<ConfigurationStoredField>? Specs;
    public List<RootedFilePath>? ExtendedSourceFiles;
    public bool HasParsedConfig;
    public object? Raw;
    public CompilerOptions? Options;
    public Dictionary<string, object?>? UnresolvedPaths;
    public TypeAcquisition? TypeAcquisition;
    public List<RootedFilePath>? ExtendedConfigPaths;
    public List<Diagnostic>? Errors;
}

// Authored stored-field selection: no lazy config queries, resolver calls or identity allocation.
public static partial class ConfigurationObserve
{
    private static List<ConfigurationStoredField>? storedSpecs(configFileSpecs? specs) => specs == null ? null : new()
    {
        new("filesSpecs", specs.filesSpecs),
        new("includeSpecs", specs.includeSpecs),
        new("excludeSpecs", specs.excludeSpecs),
        new("validatedFilesSpec", specs.validatedFilesSpec),
        new("validatedFileNames", specs.validatedFileNames),
        new("fileSpecByPath", specs.fileSpecByPath),
        new("validatedIncludeSpecs", specs.validatedIncludeSpecs),
        new("validatedExcludeSpecs", specs.validatedExcludeSpecs),
        new("validatedFilesSpecBeforeSubstitution", specs.validatedFilesSpecBeforeSubstitution),
        new("validatedIncludeSpecsBeforeSubstitution", specs.validatedIncludeSpecsBeforeSubstitution),
        new("isDefaultIncludeSpec", specs.isDefaultIncludeSpec),
    };
    public static List<ConfigurationStoredField>? Specs(ParsedCommandLine parsed) => storedSpecs(parsed.configFileSpecs ?? parsed.ConfigFile?.configFileSpecs);
    public static List<ConfigurationStoredField>? Specs(TsConfigSourceFile source) => storedSpecs(source.configFileSpecs);
    public static ConfigurationExtendedEntry ExtendedConfig(ExtendedConfigCacheEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var result = new ConfigurationExtendedEntry
        {
            Errors = entry.errors
        };
        if (entry.extendedResult is { } source)
        {
            result.Source = source.SourceFile;
            result.Specs = storedSpecs(source.configFileSpecs);
            result.ExtendedSourceFiles = source.ExtendedSourceFiles;
        }

        if (entry.extendedConfig is { } config)
        {
            result.HasParsedConfig = true;
            result.Raw = config.raw;
            result.TypeAcquisition = config.typeAcquisition;
            result.ExtendedConfigPaths = config.extendedConfigPaths;
            result.Options = config.options?.CompilerOptions;
            result.UnresolvedPaths = config.options?.unresolvedPaths;
        }

        return result;
    }

    public static IReadOnlyList<OptionFieldDescriptor<TSConfig>> TSConfigFields { get; } = new OptionFieldDescriptor<TSConfig>[]
    {
        new("CompilerOptions", "compilerOptions", "*collections.OrderedMap[string,interface {}]", value => value.CompilerOptions),
        new("References", "references", "[]interface {}", value => value.References),
        new("Files", "files", "[]string", value => value.Files),
        new("Include", "include", "[]string", value => value.Include),
        new("Exclude", "exclude", "[]string", value => value.Exclude),
        new("CompileOnSave", "compileOnSave", "*bool", value => value.CompileOnSave),
    };
    public static IReadOnlyList<OptionFieldDescriptor<ProjectReference>> ProjectReferenceFields { get; } = new OptionFieldDescriptor<ProjectReference>[]
    {
        new("Path", "path", "tspath.RootedPath", value => value.Path),
        new("OriginalPath", "originalPath", "string", value => value.OriginalPath),
        new("Circular", "circular", "bool", value => value.Circular),
    };
    public static IReadOnlyList<OptionFieldDescriptor<PluginImport>> PluginImportFields { get; } = new OptionFieldDescriptor<PluginImport>[]
    {
        new("Name", "name", "string", value => value.Name)
    };
    public static IReadOnlyList<OptionFieldDescriptor<Definition>> DefinitionFields { get; } = new OptionFieldDescriptor<Definition>[]
    {
        new("Package", "package", "string", value => value.Package),
        new("Extensions", "extensions", "[]string", value => value.Extensions),
        new("Options", "options", "jsontext.Value", value => value.Options),
    };
    public static IReadOnlyList<OptionFieldDescriptor<Manifest>> ManifestFields { get; } = new OptionFieldDescriptor<Manifest>[]
    {
        new("Name", "", "string", value => value.Name),
        new("Version", "", "string", value => value.Version),
        new("Exec", "", "[]string", value => value.Exec),
        new("CompilerOptions", "", "[]string", value => value.CompilerOptions),
        new("DynamicConfig", "", "bool", value => value.DynamicConfig),
    };
    public static IReadOnlyList<OptionFieldDescriptor<Mapper>> MapperFields { get; } = new OptionFieldDescriptor<Mapper>[]
    {
        new("Definition", "", "contentmapper.Definition", value => (Definition)value),
        new("Manifest", "-", "contentmapper.Manifest", value => value.Manifest),
        new("PackageDirectory", "-", "tspath.RootedDirectoryPath", value => value.PackageDirectory),
        new("ContributionID", "-", "string", value => value.ContributionID),
    };
}

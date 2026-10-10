// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/parsedcommandline.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Runtime.CompilerServices;
using TypeScript.Scanner;
using TypeScript.ContentMapper;
using TypeScript.Vfs;
using TypeScript.Vfs.VfsMatch;
using Output = TypeScript.OutputPaths.OutputPaths;
using GlobPattern = TypeScript.Glob.Glob;
using LocaleValue = TypeScript.Locale.Locale;
using ProjectReferencesHelper = TypeScript.Scanner.ProjectReferences;

namespace TypeScript.TsOptions;
// Minimal managed sync.Once adaptation: publication occurs once, including a throwing callback.
internal sealed class ConfigOnce
{
    private readonly object gate = new();
    private bool done;
    internal void Do(Action action)
    {
        lock (gate)
        {
            if (done)
                return;
            try
            {
                action();
            }
            finally
            {
                done = true;
            }
        }
    }
}

public sealed class SourceOutputAndProjectReference
{
    public RootedFilePath Source, OutputDts;
    public PathKey SourcePath, OutputDtsPath;
    public ParsedCommandLine Resolved = null!;
}

public sealed class ParsedCommandLine : TypeScript.OutputPaths.IOutputPathsHost, TypeScript.Modules.IResolvedProjectReference
{
    public ParsedOptions ParsedConfig = null!;
    public TsConfigSourceFile? ConfigFile;
    public List<Diagnostic>? Errors;
    public object? Raw;
    public StrongBox<bool>? CompileOnSave;
    internal configFileSpecs? configFileSpecs;
    internal RootedDirectoryPath baseDirectory;
    internal CaseSensitivity caseSensitivity;
    private readonly ConfigOnce wildcardDirectoriesOnce = new(), includeGlobsOnce = new(), sourceAndOutputMapsOnce = new(), commonSourceDirectoryOnce = new(), resolvedProjectReferencePathsOnce = new(), filePathsOnce = new(), localeOnce = new();
    internal Dictionary<RootedDirectoryPath, bool>? wildcardDirectories;
    internal List<GlobPattern>? includeGlobs;
    private Dictionary<PathKey, SourceOutputAndProjectReference>? sourceToProjectReference, outputDtsToProjectReference;
    private RootedDirectoryPath commonSourceDirectory;
    private List<RootedFilePath>? resolvedProjectReferencePaths;
    internal int literalFileNamesLen;
    internal List<string>? rootFileNamesForDiagnostics;
    private HashSet<PathKey>? filePaths;
    private LocaleValue locale;
    private const string fileGlobPattern = "*.{js,jsx,mjs,cjs,ts,tsx,mts,cts,json}", recursiveFileGlobPattern = "**/*.{js,jsx,mjs,cjs,ts,tsx,mts,cts,json}";
    internal (string FileGlob, string RecursiveFileGlob) fileGlobPatterns()
    {
        var mapperExtensions = ContentMapperExtensions();
        if ((mapperExtensions?.Count ?? 0) == 0)
            return (fileGlobPattern, recursiveFileGlobPattern);
        var extensions = new List<string>
        {
            "js",
            "jsx",
            "mjs",
            "cjs",
            "ts",
            "tsx",
            "mts",
            "cts",
            "json"
        };
        foreach (var extension in mapperExtensions!)
            extensions.Add(extension.StartsWith('.') ? extension[1..] : extension);
        string pattern = "*.{" + string.Join(',', extensions) + "}";
        return (pattern, "**/" + pattern);
    }

    private ParsedCommandLine copyWithParsedOptions(ParsedOptions config, int literalLength) => new()
    {
        ParsedConfig = config,
        ConfigFile = ConfigFile,
        Errors = Errors,
        Raw = Raw,
        CompileOnSave = CompileOnSave,
        configFileSpecs = configFileSpecs,
        baseDirectory = baseDirectory,
        caseSensitivity = caseSensitivity,
        wildcardDirectories = wildcardDirectories,
        includeGlobs = includeGlobs,
        literalFileNamesLen = literalLength,
        rootFileNamesForDiagnostics = rootFileNamesForDiagnostics,
    };
    public ParsedCommandLine WithFileNames(List<RootedFilePath>? fileNames)
    {
        var config = copyParsedConfig();
        config.FileNames = fileNames;
        return copyWithParsedOptions(config, literalFileNamesLen);
    }

    private ParsedOptions copyParsedConfig() => new()
    {
        CompilerOptions = ParsedConfig.CompilerOptions,
        TypeAcquisition = ParsedConfig.TypeAcquisition,
        FileNames = ParsedConfig.FileNames,
        ProjectReferences = ParsedConfig.ProjectReferences,
        ContentMappers = ParsedConfig.ContentMappers,
    };
    internal configFileSpecs? getConfigFileSpecs() => configFileSpecs ?? ConfigFile?.configFileSpecs;
    public RootedFilePath ConfigName() => ConfigFile?.FileName() ?? default;
    public RootedFilePath ConfigFileName() => ConfigName().AsString() != "" ? ConfigName() : CompilerOptions().ConfigFilePath;
    public Dictionary<PathKey, SourceOutputAndProjectReference>? SourceToProjectReference() => sourceToProjectReference;
    public Dictionary<PathKey, SourceOutputAndProjectReference>? OutputDtsToProjectReference() => outputDtsToProjectReference;
    public void ParseInputOutputNames() => sourceAndOutputMapsOnce.Do(() =>
    {
        var sourceToOutput = new Dictionary<PathKey, SourceOutputAndProjectReference>();
        var outputToSource = new Dictionary<PathKey, SourceOutputAndProjectReference>();
        foreach (var(output, source)in getOutputDeclarationAndSourceFileNames())
        {
            var reference = new SourceOutputAndProjectReference
            {
                Source = source,
                SourcePath = caseSensitivity.PathKey(source.AsPath()),
                OutputDts = output,
                Resolved = this
            };
            if (output.AsString() != "")
            {
                reference.OutputDtsPath = caseSensitivity.PathKey(output.AsPath());
                outputToSource[reference.OutputDtsPath] = reference;
            }

            sourceToOutput[reference.SourcePath] = reference;
        }

        outputDtsToProjectReference = outputToSource;
        sourceToProjectReference = sourceToOutput;
    });
    public RootedDirectoryPath CommonSourceDirectory()
    {
        commonSourceDirectoryOnce.Do(() => commonSourceDirectory = Output.GetCommonSourceDirectory(CompilerOptions(), () => Core.Filter(FileNames(), file => !(CompilerOptions().NoEmitForJsFiles == Tristate.True && file.HasJSFileExtension()) && !file.IsDeclarationFile()), BaseDirectory(), CaseSensitivity(), checkSourceFilesBelongToPath));
        return commonSourceDirectory;
    }

    private bool checkSourceFilesBelongToPath(List<RootedFilePath>? files, RootedDirectoryPath rootDirectory)
    {
        bool allBelong = true;
        if (files != null)
            foreach (var file in files)
            {
                if (caseSensitivity.ContainsFilePath(rootDirectory, file))
                    continue;
                (Errors ??= new()).Add(Ast.NewCompilerDiagnostic(Diagnostics.File_0_is_not_under_rootDir_1_rootDir_is_expected_to_contain_all_source_files, caseSensitivity.PathKey(file.AsPath()), rootDirectory));
                allBelong = false;
            }

        return allBelong;
    }

    public RootedDirectoryPath BaseDirectory() => baseDirectory;
    public CaseSensitivity CaseSensitivity() => caseSensitivity;
    private IEnumerable<(RootedFilePath Output, RootedFilePath Source)> getOutputDeclarationAndSourceFileNames()
    {
        if (FileNames() != null)
            foreach (var file in FileNames()!)
                yield return (!file.IsDeclarationFile() && !file.ExtensionIs(TsPath.ExtensionJson) ? Output.GetOutputDeclarationFileNameWorker(file, CompilerOptions(), this) : default, file);
    }

    public IEnumerable<RootedFilePath> GetOutputFileNames()
    {
        if (FileNames() != null)
            foreach (var file in FileNames()!)
            {
                if (file.IsDeclarationFile())
                    continue;
                var js = Output.GetOutputJSFileName(file, CompilerOptions(), this);
                bool isJson = file.ExtensionIs(TsPath.ExtensionJson);
                if (js.AsString() != "")
                {
                    yield return js;
                    if (!isJson)
                    {
                        var sourceMap = Output.GetSourceMapFilePath(js, CompilerOptions());
                        if (sourceMap.AsString() != "")
                            yield return sourceMap;
                    }
                }

                if (isJson)
                    continue;
                if (CompilerOptions().GetEmitDeclarations())
                {
                    var dts = Output.GetOutputDeclarationFileNameWorker(file, CompilerOptions(), this);
                    yield return dts;
                    if (GetContentMapperForFileName(file) == null && CompilerOptions().GetAreDeclarationMapsEnabled())
                        yield return dts.AppendSuffix(".map");
                }
            }
    }

    public RootedFilePath GetBuildInfoFileName() => Output.GetBuildInfoFileName(CompilerOptions(), caseSensitivity);
    public Dictionary<RootedDirectoryPath, bool>? WildcardDirectories()
    {
        wildcardDirectoriesOnce.Do(() =>
        {
            if (wildcardDirectories != null)
                return;
            var specs = getConfigFileSpecs()!;
            wildcardDirectories = TsOptions.getWildcardDirectories(specs.validatedIncludeSpecs, specs.validatedExcludeSpecs, baseDirectory, caseSensitivity);
        });
        return wildcardDirectories;
    }

    public List<GlobPattern>? WildcardDirectoryGlobs()
    {
        var directories = WildcardDirectories();
        if (directories == null)
            return null;
        includeGlobsOnce.Do(() =>
        {
            if (includeGlobs != null)
                return;
            var(pattern, recursive) = fileGlobPatterns();
            var globs = new List<GlobPattern>(directories.Count);
            foreach (var(directory, isRecursive)in directories)
            {
                var(parsed, error) = GlobPattern.Parse(TsPath.CombinePaths(directory.AsString(), isRecursive ? recursive : pattern));
                if (error == null)
                    globs.Add(parsed!);
            }

            includeGlobs = globs;
        });
        return includeGlobs;
    }

    public List<RootedFilePath>? LiteralFileNames() => ConfigFile != null ? GoRuntime.Slice(FileNames(), 0, literalFileNamesLen) : null;
    public void SetParsedOptions(ParsedOptions options) => ParsedConfig = options;
    public void SetCompilerOptions(CompilerOptions options) => ParsedConfig.CompilerOptions = options;
    public CompilerOptions CompilerOptions() => ParsedConfig.CompilerOptions!;
    public void SetTypeAcquisition(TypeAcquisition options) => ParsedConfig.TypeAcquisition = options;
    public TypeAcquisition? TypeAcquisition() => ParsedConfig.TypeAcquisition;
    public List<RootedFilePath>? FileNames() => ParsedConfig.FileNames;
    public string RootFileNameForDiagnostic(int index) => index < (rootFileNamesForDiagnostics?.Count ?? 0) ? rootFileNamesForDiagnostics![index] : FileNames()![index].AsString();
    public HashSet<PathKey> FilePaths()
    {
        filePathsOnce.Do(() =>
        {
            filePaths = new HashSet<PathKey>(FileNames()?.Count ?? 0);
            if (FileNames() != null)
                foreach (var file in FileNames()!)
                    filePaths.Add(caseSensitivity.PathKey(file.AsPath()));
        });
        return filePaths!;
    }

    public List<ProjectReference>? ProjectReferences() => ParsedConfig.ProjectReferences;
    public List<Mapper>? ContentMappers() => ParsedConfig.ContentMappers;
    public List<string>? ContentMapperExtensions()
    {
        List<string>? extensions = null;
        if (ContentMappers() != null)
            foreach (var mapper in ContentMappers()!)
                if (mapper.Extensions != null)
                    (extensions ??= new()).AddRange(mapper.Extensions);
        return extensions;
    }

    public Mapper? GetContentMapperForFileName(RootedFilePath file)
    {
        var extension = file.LongestExtension(ContentMapperExtensions(), caseSensitivity);
        if (ContentMappers() != null)
            foreach (var mapper in ContentMappers()!)
                if (mapper.Extensions?.Any(item => extension == item || caseSensitivity.IsCaseInsensitive() && UnicodeCase.EqualFold(extension, item)) == true)
                    return mapper;
        return null;
    }

    public List<RootedFilePath>? ResolvedProjectReferencePaths()
    {
        resolvedProjectReferencePathsOnce.Do(() => resolvedProjectReferencePaths = Core.Map(ProjectReferences(), ProjectReferencesHelper.ResolveProjectReferencePath));
        return resolvedProjectReferencePaths;
    }

    public List<RootedFilePath>? ExtendedSourceFiles() => ConfigFile?.ExtendedSourceFiles;
    public List<Diagnostic>? GetConfigFileParsingDiagnostics() => ConfigFile != null ? Core.Concatenate(ConfigFile.SourceFile.Diagnostics(), Errors) : Errors;
    public bool PossiblyMatchesFileName(RootedFilePath file)
    {
        var path = caseSensitivity.PathKey(file.AsPath());
        if (FilePaths().Contains(path))
            return true;
        var specs = getConfigFileSpecs()!;
        if (specs.validatedIncludeSpecs != null)
            foreach (var include in specs.validatedIncludeSpecs)
            {
                string text = include.AsString();
                if (text.IndexOfAny(['*', '?']) < 0 && !VfsMatch.IsImplicitGlob(text) && CaseSensitivity().PathKey(TsPath.ToRootedPath(text, BaseDirectory())) == path)
                    return true;
            }

        if (GetContentMapperForFileName(file) != null && PossiblyMatchesDirectoryName(path.Parent()))
            return true;
        return WildcardDirectoryGlobs()?.Any(glob => glob.Match(file.AsString())) == true;
    }

    public bool PossiblyMatchesDirectoryName(PathKey directory)
    {
        var wildcards = WildcardDirectories();
        if (wildcards != null)
            foreach (var(path, recursive)in wildcards)
            {
                var key = caseSensitivity.PathKey(path.AsPath());
                if (recursive ? key.ContainsPath(directory) : key == directory)
                    return true;
            }

        return false;
    }

    public string GetMatchedFileSpec(RootedFilePath file) => getConfigFileSpecs()!.getMatchedFileSpec(caseSensitivity.PathKey(file.AsPath())).AsString();
    public (string Spec, bool IsDefault) GetMatchedIncludeSpec(RootedFilePath file)
    {
        var specs = getConfigFileSpecs()!;
        if ((specs.validatedIncludeSpecs?.Count ?? 0) == 0)
            return ("", false);
        return specs.isDefaultIncludeSpec ? (specs.validatedIncludeSpecs![0].AsString(), true) : (specs.getMatchedIncludeSpec(file, baseDirectory, caseSensitivity).AsString(), false);
    }

    public ParsedCommandLine ReloadFileNamesOfParsedCommandLine(IFS fs)
    {
        var config = copyParsedConfig();
        var(files, literalLength) = TsOptions.getFileNamesFromConfigSpecs(getConfigFileSpecs()!, BaseDirectory(), CompilerOptions(), fs, ContentMapperExtensions());
        config.FileNames = files;
        return copyWithParsedOptions(config, literalLength);
    }

    public LocaleValue Locale()
    {
        localeOnce.Do(() => (locale, _) = TypeScript.Locale.Locales.Parse(CompilerOptions().Locale));
        return locale;
    }
}

public static partial class TsOptions
{
    public static ParsedCommandLine NewParsedCommandLine(CompilerOptions options, List<RootedFilePath>? rootFileNames, List<ProjectReference>? references, RootedDirectoryPath baseDirectory, CaseSensitivity caseSensitivity) => new()
    {
        ParsedConfig = new()
        {
            CompilerOptions = options,
            FileNames = rootFileNames,
            ProjectReferences = references
        },
        baseDirectory = baseDirectory,
        caseSensitivity = caseSensitivity,
    };
}

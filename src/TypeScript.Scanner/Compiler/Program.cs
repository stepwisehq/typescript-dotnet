// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of selected construction, lifecycle and diagnostic APIs in
// tsc/internal/compiler/program.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Actual emission and incremental builder/watch execution remain external.
// Semantic acquisition uses the real checker with its explicit capability admission.
// Retained static diagnostic helpers preserve the named legacy host policy.
using TypeScript.Scanner;
using TypeScript.Modules;
using TypeScript.TsOptions;
using Kind = TypeScript.Scanner.Kind;
using TypeScript.SpanMap;
using TsScanner = TypeScript.Scanner.Scanner;
using TypeScript.PackageJson;

namespace TypeScript.Compiler;
// program.go:34–64. Source option groups remain separate: only ProgramConfig is
// retained shared data; construction hosts and factories are not copied into it.
public struct ProgramConfig
{
    public ParsedCommandLine Config;
    public bool UseSourceOfProjectReference;
    public Tristate SingleThreaded;
    public RootedDirectoryPath TypingsLocation;
    private string? projectName;
    public string ProjectName { readonly get => projectName ?? ""; set => projectName = value; }

    public bool SkipModuleResolution;
    internal readonly bool canUseProjectReferenceSource() => UseSourceOfProjectReference && Config.CompilerOptions().DisableSourceOfProjectReferenceRedirect != Tristate.True;
}

public struct ProgramHosts
{
    public ICompilerHost Host;
    public TypeScript.Tracing.Tracing? Tracing;
}

public struct ProgramFactories
{
    public Func<Program, ICheckerPool>? CreateCheckerPool;
    public Func<ResolverOptions, IResolver>? CreateModuleResolver;
}

public sealed class ProgramOptions
{
    public ProgramConfig ProgramConfig;
    public ProgramHosts ProgramHosts;
    public ProgramFactories ProgramFactories;
    public ParsedCommandLine Config { get => ProgramConfig.Config; set => ProgramConfig.Config = value; }
    public bool UseSourceOfProjectReference { get => ProgramConfig.UseSourceOfProjectReference; set => ProgramConfig.UseSourceOfProjectReference = value; }
    public Tristate SingleThreaded { get => ProgramConfig.SingleThreaded; set => ProgramConfig.SingleThreaded = value; }
    public RootedDirectoryPath TypingsLocation { get => ProgramConfig.TypingsLocation; set => ProgramConfig.TypingsLocation = value; }
    public string ProjectName { get => ProgramConfig.ProjectName; set => ProgramConfig.ProjectName = value; }
    public bool SkipModuleResolution { get => ProgramConfig.SkipModuleResolution; set => ProgramConfig.SkipModuleResolution = value; }
    public ICompilerHost Host { get => ProgramHosts.Host; set => ProgramHosts.Host = value; }
    public TypeScript.Tracing.Tracing? Tracing { get => ProgramHosts.Tracing; set => ProgramHosts.Tracing = value; }
    public Func<Program, ICheckerPool>? CreateCheckerPool { get => ProgramFactories.CreateCheckerPool; set => ProgramFactories.CreateCheckerPool = value; }
    public Func<ResolverOptions, IResolver>? CreateModuleResolver { get => ProgramFactories.CreateModuleResolver; set => ProgramFactories.CreateModuleResolver = value; }
}

public sealed partial class Program : ICheckerProgramHost, TypeScript.OutputPaths.IOutputPathsHost
{
    internal ProgramConfig opts;
    internal ProgramHosts hosts;
    internal ResolutionData resolutionData = null!;
    internal ProcessedFiles processedFiles = new();
    private IncludeProcessor? includeProcessorValue;
    internal IncludeProcessor includeProcessor { get => includeProcessorValue ??= new(this); set => includeProcessorValue = value; }

    internal Exception? moduleResolutionError;
    internal ICheckerPool checkerPool = null!;
    internal CheckerPool? compilerCheckerPool;
    internal CaseSensitivity caseSensitivity;
    internal Tristate usesUriStyleNodeCoreModules;
    internal List<Diagnostic>? programDiagnostics;
    internal HashSet<PathKey> hasEmitBlockingDiagnostics = [];
    internal List<Diagnostic>? contentMapperOptionDiagnostics;
    private readonly object commonSourceDirectoryGate = new(), sourceFilesToEmitGate = new(), packagesMapGate = new(), hasTSFileGate = new();
    private bool commonSourceDirectoryInitialized, sourceFilesToEmitInitialized, packagesMapInitialized, hasTSFileInitialized;
    private RootedDirectoryPath commonSourceDirectory;
    private List<SourceFile>? sourceFilesToEmit;
    private Dictionary<string, bool>? packagesMap;
    private bool hasTSFile;
    private readonly ProgramLazyValue<HashSet<string>> unresolvedImports = new();
    private readonly ProgramLazyValue<TypeScript.Symlinks.KnownSymlinks> knownSymlinks = new();
    private readonly ProgramLazyValue<PackageNamesInfo> packageNames = new();
    internal List<SourceFile> files { get => processedFiles.files; set => processedFiles.files = value; }
    internal Dictionary<string, SourceFile> filesByPath { get => processedFiles.filesByPath; set => processedFiles.filesByPath = value; }
    internal Dictionary<string, ModeAwareCache<ResolvedModule?>> resolvedModules => processedFiles.resolvedModules;
    internal Dictionary<string, ModeAwareCache<ResolvedTypeReferenceDirective?>> typeResolutionsInFile => processedFiles.typeResolutionsInFile;
    internal ProjectReferenceFileMapper projectReferenceFileMapper => processedFiles.projectReferenceFileMapper;

    public static Program NewProgram(ProgramOptions options)
    {
        var p = new Program
        {
            opts = options.ProgramConfig,
            hosts = options.ProgramHosts,
            caseSensitivity = options.Host.FS().CaseSensitivity()
        };
        Action? done = options.Tracing?.Push(TypeScript.Tracing.Phase.Program, "createProgram", new() { ["configFilePath"] = options.Config.CompilerOptions().ConfigFilePath }, true);
        try
        {
            (p.processedFiles, p.resolutionData, p.moduleResolutionError) = FileLoader.ProcessAllProgramFiles(options, p.SingleThreaded());
            p.includeProcessor = new(p);
            p.initCheckerPool(options.CreateCheckerPool);
            p.verifyCompilerOptions();
            p.collectContentMapperOptionDiagnostics();
            return p;
        }
        finally
        {
            done?.Invoke();
        }
    }

    internal void initCheckerPool(Func<Program, ICheckerPool>? create)
    {
        if (!processedFiles.finishedProcessing)
            throw new InvalidOperationException("Program must finish processing files before initializing checker pool");
        if (create is not null)
            checkerPool = create(this);
        else
            checkerPool = compilerCheckerPool = CheckerPool.newCheckerPoolWithTracing(this, hosts.Tracing);
    }

    public ICheckerPool GetCheckerPool() => checkerPool;
    public CompilerOptions Options() => opts.Config.CompilerOptions();
    public ParsedCommandLine CommandLine() => opts.Config;
    public ICompilerHost Host() => hosts.Host;
    public TypeScript.Tracing.Tracing? Tracing() => hosts.Tracing;
    public RootedDirectoryPath BaseDirectory() => opts.Config.BaseDirectory();
    public RootedDirectoryPath GetCurrentDirectory() => BaseDirectory();
    public RootedDirectoryPath GetGlobalTypingsCacheLocation() => opts.TypingsLocation;
    public TypeScript.ContentMapper.IProject? ContentMapperProject() => Host().ContentMapperProject();
    public bool FileExists(RootedFilePath path) => Host().FS().FileExists(path);
    public CaseSensitivity CaseSensitivity() => caseSensitivity;
    public bool UseCaseSensitiveFileNames() => caseSensitivity == TypeScript.Scanner.CaseSensitivity.CaseSensitive;
    public bool SingleThreaded() => (opts.SingleThreaded == Tristate.Unknown ? Options().SingleThreaded : opts.SingleThreaded) == Tristate.True;
    public Tristate UsesUriStyleNodeCoreModules() => usesUriStyleNodeCoreModules;
    public List<SourceFile> SourceFiles() => files;
    public List<SourceFile> GetSourceFiles() => files;
    public List<DuplicateSourceFile>? DuplicateSourceFiles() => processedFiles.duplicateSourceFiles;
    public List<string>? ContentMapperExtensions() => opts.Config.ContentMapperExtensions();
    public List<Diagnostic>? GetConfigFileParsingDiagnostics() => opts.Config.GetConfigFileParsingDiagnostics();
    public Exception? ModuleResolutionError() => moduleResolutionError;
    public Dictionary<string, SourceFile> FilesByPath() => filesByPath;
    public PathKey PathKeyForFileName(RootedFilePath file) => caseSensitivity.PathKey(file.AsPath());
    public SourceFile? GetSourceFile(RootedFilePath file) => GetSourceFileByPath(PathKeyForFileName(file));
    public SourceFile? GetSourceFileByPath(PathKey path) => filesByPath.GetValueOrDefault(path.AsString());
    public SourceFile? GetSourceFileForResolvedModule(ResolvedModule resolved) => getSourceFileWithRedirect(resolved.ResolvedFileName, resolved.ResolvedPath);
    internal SourceFile? getSourceFileWithRedirect(RootedFilePath file, PathKey path)
    {
        var source = GetSourceFileByPath(path);
        if (source is null)
        {
            var redirect = getParseFileRedirectByFileName(file);
            if (redirect != default)
                return GetSourceFile(redirect);
        }

        return source;
    }

    public Program GetProgram() => this; // Go Program.Program; C# constructor-name adaptation.
    public bool HasSameFileNames(Program other)
    {
        var redirects = processedFiles.redirectFilesByPath;
        var otherRedirects = other.processedFiles.redirectFilesByPath;
        return filesByPath.Count == other.filesByPath.Count &&
            filesByPath.All(pair => other.filesByPath.TryGetValue(pair.Key, out var file) &&
                pair.Value.FileNameGoBytes() == file.FileNameGoBytes()) &&
            (redirects?.Count ?? 0) == (otherRedirects?.Count ?? 0) &&
            (redirects is null || redirects.All(pair =>
                otherRedirects is not null && otherRedirects.TryGetValue(pair.Key, out var file) &&
                pair.Value.FileName() == file.FileName()));
    }

    public Dictionary<string, List<FileIncludeReason>> GetIncludeReasons() => processedFiles.fileIncludeData.fileIncludeReasons;
    public bool IsMissingPath(PathKey path) => processedFiles.missingFiles.Contains(path.AsString());
    public SourceFileMetaData GetSourceFileMetaData(PathKey path) => processedFiles.sourceFileMetaDatas.GetValueOrDefault(path.AsString());
    public bool IsSourceFileDefaultLibrary(PathKey path) => processedFiles.libFiles.ContainsKey(path.AsString());
    public bool IsLibFile(SourceFile source) => IsSourceFileDefaultLibrary(PathKey.FromGoString(source.PathKey()));
    public LibFile? GetDefaultLibFile(PathKey path) => processedFiles.libFiles.GetValueOrDefault(path.AsString());
    public bool IsGlobalTypingsFile(RootedFilePath file) => file.IsDeclarationFile() && caseSensitivity.ContainsPath(GetGlobalTypingsCacheLocation(), file.AsPath());
    public bool IsSourceFileFromExternalLibrary(SourceFile file) => processedFiles.sourceFilesFoundSearchingNodeModules.Contains(file.PathKey());
    public List<RootedFilePath>? GetRedirectTargets(PathKey path) => processedFiles.redirectTargetsMap.GetValueOrDefault(path.AsString())?.Select(RootedFilePath.FromGoString).ToList();
    public RootedFilePath GetSourceOfProjectReferenceIfOutputIncluded(SourceFile file) => processedFiles.outputFileToProjectReferenceSource.GetValueOrDefault(file.PathKey(), RootedFilePath.FromGoString(file.FileNameGoBytes()));
    public SourceOutputAndProjectReference? GetProjectReferenceFromSource(PathKey path) => projectReferenceFileMapper.GetProjectReferenceFromSource(path);
    public bool IsSourceFromProjectReference(PathKey path) => projectReferenceFileMapper.IsSourceFromProjectReference(path);
    public SourceOutputAndProjectReference? GetProjectReferenceFromOutputDts(PathKey path) => projectReferenceFileMapper.GetProjectReferenceFromOutputDts(path);
    public (ParsedCommandLine? Config, bool Ok) GetResolvedProjectReferenceFor(PathKey path) => projectReferenceFileMapper.GetResolvedReferenceFor(path);
    public ParsedCommandLine? GetRedirectForResolution(SourceFile file) => projectReferenceFileMapper.GetRedirectForResolution(file).Config;
    public RootedFilePath GetParseFileRedirect(RootedFilePath file) => getParseFileRedirectByFileName(file);
    internal RootedFilePath getParseFileRedirectByFileName(RootedFilePath file) => projectReferenceFileMapper.GetParseFileRedirect(file, PathKeyForFileName(file)).File;
    public List<ParsedCommandLine?>? GetResolvedProjectReferences() => projectReferenceFileMapper.GetResolvedProjectReferences();
    public bool RangeResolvedProjectReference(Func<PathKey, ParsedCommandLine?, ParsedCommandLine, int, bool> callback) => projectReferenceFileMapper.RangeResolvedProjectReference(callback);
    public bool RangeResolvedProjectReferenceInChildConfig(ParsedCommandLine child, Func<PathKey, ParsedCommandLine?, ParsedCommandLine, int, bool> callback) => projectReferenceFileMapper.RangeResolvedProjectReferenceInChildConfig(child, callback);
    public ResolvedModule? GetResolvedModule(SourceFile file, string moduleReference, ResolutionMode mode) => resolvedModules.GetValueOrDefault(file.PathKey())?.GetValueOrDefault(new(moduleReference, mode));
    public ResolvedModule? GetResolvedModuleFromModuleSpecifier(SourceFile file, Node specifier)
    {
        if (!Ast.IsStringLiteralLike(specifier))
            throw new InvalidOperationException("moduleSpecifier must be a StringLiteralLike");
        if (Ast.IsSourcePhaseImport(specifier.Parent))
            return null;
        return GetResolvedModule(file, specifier.Text(), GetModeForUsageLocation(file, specifier));
    }

    public Dictionary<string, ModeAwareCache<ResolvedModule?>> GetResolvedModules() => resolvedModules;
    public ResolvedTypeReferenceDirective? GetResolvedTypeReferenceDirective(SourceFile file, string name, ResolutionMode mode) => typeResolutionsInFile.GetValueOrDefault(file.PathKey())?.GetValueOrDefault(new(name, mode));
    public ResolvedTypeReferenceDirective? GetResolvedTypeReferenceDirectiveFromTypeReferenceDirective(FileReference reference, SourceFile file) => GetResolvedTypeReferenceDirective(file, reference.FileName, getModeForTypeReferenceDirectiveInFile(reference, file));
    internal ResolutionMode getModeForTypeReferenceDirectiveInFile(FileReference reference, SourceFile file) => reference.ResolutionMode != ResolutionMode.None ? reference.ResolutionMode : GetDefaultResolutionModeForFile(file);
    public Dictionary<string, ModeAwareCache<ResolvedTypeReferenceDirective?>> GetResolvedTypeReferenceDirectives() => typeResolutionsInFile;
    public ModuleKind GetEmitModuleFormatOfFile(SourceFile file) => Ast.GetEmitModuleFormatOfFileWorker(RootedFilePath.FromGoString(file.FileNameGoBytes()), projectReferenceFileMapper.GetCompilerOptionsForFile(file), GetSourceFileMetaData(PathKey.FromGoString(file.PathKey())));
    public ResolutionMode GetEmitSyntaxForUsageLocation(SourceFile file, Node usage) => FileLoader.GetEmitSyntaxForUsageLocationWorker(RootedFilePath.FromGoString(file.FileNameGoBytes()), GetSourceFileMetaData(PathKey.FromGoString(file.PathKey())), usage, projectReferenceFileMapper.GetCompilerOptionsForFile(file));
    public ResolutionMode GetImpliedNodeFormatForEmit(SourceFile file) => Ast.GetImpliedNodeFormatForEmitWorker(RootedFilePath.FromGoString(file.FileNameGoBytes()), projectReferenceFileMapper.GetCompilerOptionsForFile(file).GetEmitModuleKind(), GetSourceFileMetaData(PathKey.FromGoString(file.PathKey())));
    public ResolutionMode GetModeForUsageLocation(SourceFile file, Node usage) => FileLoader.GetModeForUsageLocation(RootedFilePath.FromGoString(file.FileNameGoBytes()), GetSourceFileMetaData(PathKey.FromGoString(file.PathKey())), usage, projectReferenceFileMapper.GetCompilerOptionsForFile(file));
    public ResolutionMode GetDefaultResolutionModeForFile(SourceFile file) => FileLoader.GetDefaultResolutionModeForFile(RootedFilePath.FromGoString(file.FileNameGoBytes()), GetSourceFileMetaData(PathKey.FromGoString(file.PathKey())), projectReferenceFileMapper.GetCompilerOptionsForFile(file));
    public ResolutionMode GetModeForResolutionAtIndex(SourceFile file, int index)
    {
        var imports = file.Imports();
        if (index < (imports?.Count ?? 0))
            return GetModeForUsageLocation(file, imports![index]);
        index -= imports?.Count ?? 0;
        foreach (var augmentation in file.ModuleAugmentations ?? [])
            if (augmentation.Kind == Kind.StringLiteral)
            {
                if (index == 0)
                    return GetModeForUsageLocation(file, augmentation);
                index--;
            }

        throw new InvalidOperationException("resolution index out of range");
    }

    public void BindSourceFiles()
    {
        var work = Core.NewWorkGroup(SingleThreaded());
        foreach (SourceFile file in files)
            if (!file.IsBound())
                work.Queue(() =>
                {
                    Action? done = hosts.Tracing?.Push(TypeScript.Tracing.Phase.Bind, "bindSourceFile", new() { ["path"] = file.PathKey() }, true);
                    try
                    {
                        Binder.BindSourceFile(file);
                    }
                    finally
                    {
                        done?.Invoke();
                    }
                });
        work.RunAndWait();
    }

    public (TypeScript.Checking.Checker Checker, Action Done) GetTypeChecker(CancellationToken context = default) => compilerCheckerPool is not null ? compilerCheckerPool.getCheckerNonExclusive() : checkerPool.GetChecker(context, null);
    public (TypeScript.Checking.Checker Checker, Action Done) GetTypeCheckerForFile(CancellationToken context, SourceFile file) => compilerCheckerPool is not null ? compilerCheckerPool.getCheckerForFileNonExclusive(file) : checkerPool.GetChecker(context, file);
    public (TypeScript.Checking.Checker Checker, Action Done) GetTypeCheckerForFileExclusive(CancellationToken context, SourceFile file) => compilerCheckerPool is not null ? compilerCheckerPool.getCheckerForFileExclusive(context, file) : checkerPool.GetChecker(context, file);
    public void ForEachCheckerParallel(Action<int, TypeScript.Checking.Checker> callback) => compilerCheckerPool?.forEachCheckerParallel(callback);
    internal TypeScript.Checking.Checker CreateActualChecker() =>
        TypeScript.Checking.Checker.NewChecker(this).Checker;

    public List<Diagnostic>? GetGlobalDiagnostics(CancellationToken context = default) => files.Count == 0 ? null : compilerCheckerPool?.GetGlobalDiagnostics();
    public Dictionary<string, bool> GetPackagesMap()
    {
        lock (packagesMapGate)
        {
            if (!packagesMapInitialized)
            {
                packagesMap = new(StringComparer.Ordinal);
                foreach (var modules in resolvedModules.Values)
                    foreach (var module in modules?.Values.AsEnumerable() ?? [])
                        if (module is not null && module.PackageId.Name != "")
                            packagesMap[module.PackageId.Name] = packagesMap.GetValueOrDefault(module.PackageId.Name) || module.Extension == TsPath.ExtensionDts;
                packagesMapInitialized = true;
            }

            return packagesMap!;
        }
    }

    public bool HasTSFile()
    {
        lock (hasTSFileGate)
        {
            if (!hasTSFileInitialized)
            {
                hasTSFile = files.Any(file => RootedFilePath.FromGoString(file.FileNameGoBytes()).HasImplementationTSFileExtension());
                hasTSFileInitialized = true;
            }

            return hasTSFile;
        }
    }

    public HashSet<string> GetUnresolvedImports() => unresolvedImports.getValue(extractUnresolvedImports);
    internal HashSet<string> extractUnresolvedImports()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
            foreach (var name in extractUnresolvedImportsFromSourceFile(file) ?? [])
                set.Add(name);
        return set;
    }

    internal List<string>? extractUnresolvedImportsFromSourceFile(SourceFile file)
    {
        List<string>? result = null;
        foreach (var(key, resolution)in resolvedModules.GetValueOrDefault(file.PathKey()) ?? [])
            if ((!resolution.IsResolved() || !TsPath.ExtensionIsOneOf(resolution!.Extension, TsPath.SupportedTSExtensionsWithJsonFlat)) && !TsPath.IsExternalModuleNameRelative(key.Name))
                (result ??= []).Add(key.Name);
        return result;
    }

    public void ForEachResolvedModule(Action<ResolvedModule, string, ResolutionMode, PathKey> callback, SourceFile? file = null) => forEachResolution(resolvedModules, (value, name, mode, path) => callback(value!, name, mode, path), file);
    public void ForEachResolvedTypeReferenceDirective(Action<ResolvedTypeReferenceDirective, string, ResolutionMode, PathKey> callback, SourceFile? file = null) => forEachResolution(typeResolutionsInFile, (value, name, mode, path) => callback(value!, name, mode, path), file);
    internal static void forEachResolution<T>(Dictionary<string, ModeAwareCache<T>> cache, Action<T, string, ResolutionMode, PathKey> callback, SourceFile? file)
    {
        if (file is not null)
        {
            foreach (var(key, value)in cache.GetValueOrDefault(file.PathKey()) ?? [])
                callback(value, key.Name, key.Mode, PathKey.FromGoString(file.PathKey()));
        }
        else
            foreach (var(path, resolutions)in cache)
                foreach (var(key, value)in resolutions ?? [])
                    callback(value, key.Name, key.Mode, PathKey.FromGoString(path));
    }

    public (Program? Program, SourceFile? NewFile, bool Reused) ReuseProgram(PathKey changedFilePath, ICompilerHost newHost, Func<Program, ICheckerPool>? createCheckerPool = null, Func<ResolverOptions, IResolver>? createModuleResolver = null)
    {
        var oldFile = filesByPath.GetValueOrDefault(changedFilePath.AsString())!;
        SourceFile? newFile;
        List<SourceFile>? oldSupplemental = null, newSupplemental = null;
        if (oldFile.ContentMapper() != "")
        {
            var mapper = opts.Config.GetContentMapperForFileName(RootedFilePath.FromGoString(oldFile.FileNameGoBytes()));
            var(sourceFiles, error) = newHost.GetContentMappedSourceFiles(oldFile.ParseOptions(), mapper!);
            newFile = sourceFiles.Canonical;
            if (error is not null)
                return (null, null, false);
            oldSupplemental = oldFile.SupplementalSourceFiles();
            newSupplemental = sourceFiles.Supplemental;
        }
        else
            newFile = newHost.GetSourceFile(oldFile.ParseOptions());
        if (processedFiles.redirectFilesByPath?.ContainsKey(changedFilePath.AsString()) == true || processedFiles.redirectTargetsMap.ContainsKey(changedFilePath.AsString()))
            return (null, newFile, false);
        if (moduleResolutionError is not null || !canReplaceFileInProgram(oldFile, newFile))
            return (null, newFile, false);
        if (processedFiles.importHelpersImportSpecifiers?.GetValueOrDefault(oldFile.PathKey())is not null || needsImportHelpersImportSpecifier(newFile!))
            return (null, newFile, false);
        if (processedFiles.jsxRuntimeImportSpecifiers?.GetValueOrDefault(oldFile.PathKey())is not null || jsxRuntimeImportSpecifier(newFile!) != "")
            return (null, newFile, false);
        if ((oldSupplemental?.Count ?? 0) != (newSupplemental?.Count ?? 0))
            return (null, newFile, false);
        for (int i = 0; i < (oldSupplemental?.Count ?? 0); i++)
        {
            var old = oldSupplemental![i];
            var updated = newSupplemental![i];
            if (old.PathKey() != updated.PathKey() || !canReplaceFileInProgram(old, updated))
                return (null, newFile, false);
            if (processedFiles.importHelpersImportSpecifiers?.GetValueOrDefault(old.PathKey())is not null || needsImportHelpersImportSpecifier(updated))
                return (null, newFile, false);
            if (processedFiles.jsxRuntimeImportSpecifiers?.GetValueOrDefault(old.PathKey())is not null || jsxRuntimeImportSpecifier(updated) != "")
                return (null, newFile, false);
        }

        var result = new Program
        {
            opts = opts,
            hosts = new()
            {
                Host = newHost
            },
            resolutionData = resolutionData.Clone(),
            caseSensitivity = caseSensitivity,
            processedFiles = processedFiles.ShallowClone(),
            usesUriStyleNodeCoreModules = usesUriStyleNodeCoreModules,
            programDiagnostics = programDiagnostics,
            hasEmitBlockingDiagnostics = hasEmitBlockingDiagnostics,
            contentMapperOptionDiagnostics = contentMapperOptionDiagnostics?.ToList()
        };
        result.unresolvedImports.tryReuse(unresolvedImports);
        result.knownSymlinks.tryReuse(knownSymlinks);
        result.packageNames.tryReuse(packageNames);
        int index = result.files.FindIndex(file => file.PathKey() == newFile!.PathKey());
        result.files = result.files.ToList();
        result.files[index] = newFile!;
        result.filesByPath = new(result.filesByPath, StringComparer.Ordinal);
        result.filesByPath[newFile!.PathKey()] = newFile;
        for (int i = 0; i < (oldSupplemental?.Count ?? 0); i++)
        {
            var old = oldSupplemental![i];
            var updated = newSupplemental![i];
            int supplementalIndex = result.files.FindIndex(file => ReferenceEquals(file, old));
            result.files[supplementalIndex] = updated;
            result.filesByPath[updated.PathKey()] = updated;
        }

        result.includeProcessor = new(result);
        result.initCheckerPool(createCheckerPool);
        return (result, newFile, true);
    }

    public (Program Program, SourceFile? NewFile, bool Reused) UpdateProgram(PathKey changedFilePath, ICompilerHost newHost, Func<Program, ICheckerPool>? createCheckerPool = null, Func<ResolverOptions, IResolver>? createModuleResolver = null)
    {
        var(result, newFile, reused) = ReuseProgram(changedFilePath, newHost, createCheckerPool, createModuleResolver);
        return reused ? (result!, newFile, true) : (NewProgram(new() { ProgramConfig = opts, Host = newHost, CreateCheckerPool = createCheckerPool, CreateModuleResolver = createModuleResolver }), newFile, false);
    }

    internal bool canReplaceFileInProgram(SourceFile first, SourceFile? second) =>
        second is not null &&
        first.ParseOptions() == second.ParseOptions() &&
        first.ScriptKind == second.ScriptKind &&
        Ast.IsExternalOrCommonJSModule(first) == Ast.IsExternalOrCommonJSModule(second) &&
        first.UsesUriStyleNodeCoreModules == second.UsesUriStyleNodeCoreModules &&
        equalSlices(first.Imports(), second.Imports(), (a, b) =>
            equalModuleSpecifiers(a, b) &&
            GetModeForUsageLocation(first, a) == GetModeForUsageLocation(second, b) &&
            Ast.IsSourcePhaseImport(a.Parent) == Ast.IsSourcePhaseImport(b.Parent)) &&
        equalSlices(first.ModuleAugmentations, second.ModuleAugmentations, equalModuleAugmentationNames) &&
        equalSlices(first.AmbientModuleNames, second.AmbientModuleNames, (a, b) => a == b) &&
        equalSlices(first.ReferencedFiles, second.ReferencedFiles, equalFileReferences) &&
        equalSlices(first.TypeReferenceDirectives, second.TypeReferenceDirectives, equalFileReferences) &&
        equalSlices(first.LibReferenceDirectives, second.LibReferenceDirectives, equalFileReferences) &&
        equalCheckJSDirectives(first.CheckJsDirective, second.CheckJsDirective);

    private static bool equalSlices<T>(List<T>? first, List<T>? second, Func<T, T, bool> equal)
    {
        if ((first?.Count ?? 0) != (second?.Count ?? 0))
            return false;
        for (int i = 0; i < (first?.Count ?? 0); i++)
            if (!equal(first![i], second![i]))
                return false;
        return true;
    }

    internal static bool equalModuleSpecifiers(Node first, Node second) => first.Kind == second.Kind && (!Ast.IsStringLiteral(first) || first.Text() == second.Text());
    internal static bool equalModuleAugmentationNames(Node first, Node second) => first.Kind == second.Kind && first.Text() == second.Text();
    internal static bool equalFileReferences(FileReference first, FileReference second) => first.FileName == second.FileName && first.ResolutionMode == second.ResolutionMode && first.Preserve == second.Preserve;
    internal static bool equalCheckJSDirectives(CheckJsDirective? first, CheckJsDirective? second) => first is null && second is null || first is not null && second is not null && first.Enabled == second.Enabled;
    internal bool needsImportHelpersImportSpecifier(SourceFile file)
    {
        var options = DefaultResolver.GetCompilerOptionsWithRedirect(Options(), GetRedirectForResolution(file));
        if (options.ImportHelpers != Tristate.True)
            return false;
        if (!Ast.IsSourceFileJS(file) && (file.IsDeclarationFile || !options.GetIsolatedModules() && !Ast.IsExternalModule(file)))
            return false;
        return true;
    }

    internal string jsxRuntimeImportSpecifier(SourceFile file)
    {
        if (!Ast.IsSourceFileJS(file) && file.ScriptKind != ScriptKind.TSX)
            return "";
        var options = DefaultResolver.GetCompilerOptionsWithRedirect(Options(), GetRedirectForResolution(file));
        return Ast.GetJSXRuntimeImport(Ast.GetJSXImplicitImportBase(options, file), options);
    }

    public TypeScript.ContentMapper.Mapper? GetContentMapper(SourceFile file)
    {
        if (file.ContentMapper() == "")
            return null;
        var mapper = opts.Config.GetContentMapperForFileName(RootedFilePath.FromGoString(file.FileNameGoBytes()));
        return mapper is not null && mapper.Identity() == file.ContentMapper() ? mapper : null;
    }

    public SourceFile? GetSourceFileFromReference(SourceFile origin, FileReference reference)
    {
        var fileName = RootedFilePath.FromGoString(origin.FileNameGoBytes()).Directory().ResolveFile(reference.FileName);
        var extensions = TypeScript.TsOptions.TsOptions.GetSupportedExtensions(Options(), CommandLine().ContentMapperExtensions());
        var supported = TypeScript.TsOptions.TsOptions.GetSupportedExtensionsWithJsonIfResolveJsonModule(Options(), extensions);
        bool allowNonTs = Options().AllowNonTsExtensions == Tristate.True;
        if (fileName.HasExtension())
        {
            if (!allowNonTs && !supported.Any(group => PathKeyForFileName(fileName).ExtensionIsOneOf(group)))
                return null;
            return getSourceFileWithRedirect(fileName, PathKeyForFileName(fileName));
        }

        if (allowNonTs && getSourceFileWithRedirect(fileName, PathKeyForFileName(fileName))is { } extensionless)
            return extensionless;
        foreach (var extension in supported[0])
        {
            var candidate = fileName.AppendSuffix(extension);
            if (getSourceFileWithRedirect(candidate, PathKeyForFileName(candidate))is { } source)
                return source;
        }

        return null;
    }

    IReadOnlyList<SourceFile> ICheckerProgramHost.SourceFiles => files;

    CompilerOptions ICheckerProgramHost.Options => Options();

    bool ICheckerProgramHost.AllowsNonRelativeModuleSpecifiers => true;

    string ICheckerProgramHost.GetSourceFileNameGoBytes(SourceFile file) => file.FileNameGoBytes();
    ModuleKind ICheckerProgramHost.GetImpliedNodeFormatForEmit(SourceFile file) => (ModuleKind)(int)GetImpliedNodeFormatForEmit(file);
    bool ICheckerProgramHost.IsSourceFileDefaultLibrary(SourceFile file) => IsSourceFileDefaultLibrary(PathKey.FromGoString(file.PathKey()));

    // Selected emitter.go:476/538 producers: eligibility and output enumeration only.
    internal static bool sourceFileMayBeEmitted(SourceFile file, Program host, bool forceDtsEmit, bool forceJsEmit)
    {
        var options = host.Options();
        if (!forceJsEmit && options.NoEmitForJsFiles == Tristate.True && Ast.IsSourceFileJS(file))
            return false;
        if (file.IsDeclarationFile)
            return false;
        if (file.ContentMapper() != "" && !forceDtsEmit && !options.GetEmitDeclarations())
            return false;
        if (host.IsSourceFileFromExternalLibrary(file))
            return false;
        if (forceDtsEmit || forceJsEmit)
            return true;
        if (host.GetProjectReferenceFromSource(PathKey.FromGoString(file.PathKey()))is not null)
            return false;
        if (!Ast.IsJsonSourceFile(file))
            return true;
        if (options.OutDir == default)
            return false;
        if (options.RootDir != default || options.ConfigFilePath != default)
        {
            var directory = TypeScript.OutputPaths.OutputPaths.GetCommonSourceDirectory(options, () => null, host.BaseDirectory(), host.CaseSensitivity(), null);
            var filename = RootedFilePath.FromGoString(file.FileNameGoBytes());
            var output = TypeScript.OutputPaths.OutputPaths.GetSourceFileNameInNewDir(filename, options.OutDir, directory, host.CaseSensitivity());
            if (host.CaseSensitivity().CompareFilePaths(filename, output) == 0)
                return false;
        }

        return true;
    }

    public bool SourceFileMayBeEmitted(SourceFile file, bool forceDtsEmit) => sourceFileMayBeEmitted(file, this, forceDtsEmit, false);
    internal static List<SourceFile>? getSourceFilesToEmit(Program host, List<SourceFile>? target, bool forceDtsEmit, bool forceJsEmit) => Core.Filter(target ?? host.SourceFiles(), file => sourceFileMayBeEmitted(file, host, forceDtsEmit, forceJsEmit));
    internal List<SourceFile>? getSourceFilesToEmit(List<SourceFile>? target, bool forceDtsEmit, bool forceJsEmit)
    {
        if (target is null && !forceDtsEmit && !forceJsEmit)
        {
            lock (sourceFilesToEmitGate)
            {
                if (!sourceFilesToEmitInitialized)
                {
                    sourceFilesToEmit = getSourceFilesToEmit(this, null, false, false);
                    sourceFilesToEmitInitialized = true;
                }

                return sourceFilesToEmit;
            }
        }

        return getSourceFilesToEmit(this, target, forceDtsEmit, forceJsEmit);
    }

    public RootedDirectoryPath CommonSourceDirectory()
    {
        lock (commonSourceDirectoryGate)
        {
            if (!commonSourceDirectoryInitialized)
            {
                commonSourceDirectory = TypeScript.OutputPaths.OutputPaths.GetCommonSourceDirectory(Options(), () => files.Where(file => !file.IsDeclarationFile && sourceFileMayBeEmitted(file, this, false, false)).Select(file => RootedFilePath.FromGoString(file.FileNameGoBytes())).ToList(), BaseDirectory(), CaseSensitivity(), checkSourceFilesBelongToPath);
                commonSourceDirectoryInitialized = true;
            }

            return commonSourceDirectory;
        }
    }

    internal bool checkSourceFilesBelongToPath(List<RootedFilePath>? sourceFiles, RootedDirectoryPath rootDirectory)
    {
        bool all = true;
        foreach (var file in sourceFiles ?? [])
            if (!caseSensitivity.ContainsFilePath(rootDirectory, file))
            {
                addProcessingDiagnostic(new() { file = caseSensitivity.PathKey(file.AsPath()).AsString(), message = Diagnostics.File_0_is_not_under_rootDir_1_rootDir_is_expected_to_contain_all_source_files, args = [file.AsString(), rootDirectory.AsString()] });
                all = false;
            }

        return all;
    }

    internal void addProcessingDiagnostic(ProcessingDiagnostic diagnostic) => includeProcessor.processingDiagnostics.Add(diagnostic);
    public bool IsEmitBlocked(RootedFilePath file) => hasEmitBlockingDiagnostics.Contains(PathKeyForFileName(file));
    internal void blockEmittingOfFile(RootedFilePath file, Diagnostic diagnostic)
    {
        hasEmitBlockingDiagnostics.Add(PathKeyForFileName(file));
        (programDiagnostics ??= []).Add(diagnostic);
    }

    internal static bool hasZeroOrOneAsteriskCharacter(string text)
    {
        bool seen = false;
        foreach (char ch in text)
            if (ch == '*')
            {
                if (seen)
                    return false;
                seen = true;
            }

        return true;
    }

    internal static bool moduleResolutionSupportsPackageJsonExportsAndImports(ModuleResolutionKind kind) => kind >= ModuleResolutionKind.Node16 && kind <= ModuleResolutionKind.NodeNext || kind == ModuleResolutionKind.Bundler;
    internal static bool emitModuleKindIsNonNodeESM(ModuleKind kind) => kind >= ModuleKind.ES2015 && kind <= ModuleKind.ESNext;
    internal void verifyProjectReferences()
    {
        var buildInfoFile = Options().SuppressOutputPathCheck != Tristate.True ? opts.Config.GetBuildInfoFileName() : default;
        void Add(ParsedCommandLine config, int index, Message message, params object[] args) => (programDiagnostics ??= []).Add(TypeScript.TsOptions.TsOptions.CreateDiagnosticAtReferenceSyntax(config, index, message, args) ?? Ast.NewCompilerDiagnostic(message, args));
        RangeResolvedProjectReference((path, config, parent, index) =>
        {
            var reference = parent.ProjectReferences()![index];
            if (config is null)
            {
                Add(parent, index, Diagnostics.File_0_not_found, reference.Path);
                return true;
            }

            var options = config.CompilerOptions();
            if ((options.Composite != Tristate.True || options.NoEmit == Tristate.True) && (parent.FileNames()?.Count ?? 0) > 0)
            {
                if (options.Composite != Tristate.True)
                    Add(parent, index, Diagnostics.Referenced_project_0_must_have_setting_composite_Colon_true, reference.Path);
                if (options.NoEmit == Tristate.True)
                    Add(parent, index, Diagnostics.Referenced_project_0_may_not_disable_emit, reference.Path);
            }

            if (buildInfoFile != default && buildInfoFile == config.GetBuildInfoFileName())
            {
                Add(parent, index, Diagnostics.Cannot_write_file_0_because_it_will_overwrite_tsbuildinfo_file_generated_by_referenced_project_1, buildInfoFile, reference.Path);
                hasEmitBlockingDiagnostics.Add(PathKeyForFileName(buildInfoFile));
            }

            return true;
        });
    }

    internal void verifyCompilerOptions()
    {
        var options = Options();
        var sourceFile = opts.Config.ConfigFile?.SourceFile;
        var configFilePath = sourceFile is not null ? RootedFilePath.FromGoString(sourceFile.FileNameGoBytes()) : default;
        var compilerOptionsProperty = TypeScript.TsOptions.TsOptions.ForEachTsConfigPropArray(sourceFile, "compilerOptions", property => property);
        var compilerOptionsObject = compilerOptionsProperty is not null && Ast.IsObjectLiteralExpression(compilerOptionsProperty.Initializer) ? compilerOptionsProperty.Initializer.AsObjectLiteralExpression() : null;
        Diagnostic? InObject(ObjectLiteralExpression? syntax, bool onKey, string key, string key2, Message message, params object[] args)
        {
            var diagnostic = TypeScript.TsOptions.TsOptions.ForEachPropertyAssignment(syntax, key, property => TypeScript.TsOptions.TsOptions.CreateDiagnosticForNodeInSourceFile(sourceFile!, onKey ? property.Name() : property.Initializer, message, args), key2);
            if (diagnostic is not null)
                (programDiagnostics ??= []).Add(diagnostic);
            return diagnostic;
        }

        Diagnostic Compiler(Message message, params object[] args)
        {
            var diagnostic = compilerOptionsProperty is not null ? TypeScript.TsOptions.TsOptions.CreateDiagnosticForNodeInSourceFile(sourceFile!, compilerOptionsProperty.Name(), message, args) : Ast.NewCompilerDiagnostic(message, args);
            (programDiagnostics ??= []).Add(diagnostic);
            return diagnostic;
        }

        Diagnostic Option(bool onKey, string name, string name2, Message message, params object[] args) => InObject(compilerOptionsObject, onKey, name, name2, message, args) ?? Compiler(message, args);
        void Name(Message message, string name, string name2, params object[] args) => Option(true, name, name2, message, new object[] { name, name2 }.Concat(args).ToArray());
        void Value(string name, Message message, params object[] args) => Option(false, name, "", message, args);
        void Removed(string name, string value, string instead)
        {
            var diagnostic = value == "" ? Option(true, name, "", Diagnostics.Option_0_has_been_removed_Please_remove_it_from_your_configuration, name) : Option(false, name, "", Diagnostics.Option_0_1_has_been_removed_Please_remove_it_from_your_configuration, name, value);
            if (instead != "")
                diagnostic.AddMessageChain(Ast.NewCompilerDiagnostic(Diagnostics.Use_0_instead, instead));
        }

        if (options.BaseUrl != default)
        {
            string instead = "";
            if (configFilePath != default)
            {
                var (relativePath, relativeOk) = caseSensitivity.RelativePathFromFileToPath(configFilePath, options.BaseUrl.AsPath());
                string relative = relativeOk ? relativePath.AsString() : options.BaseUrl.AsString();
                if (!relative.StartsWith("./", StringComparison.Ordinal) && !relative.StartsWith("../", StringComparison.Ordinal))
                    relative = "./" + relative;
                string suggestion = TsPath.CombinePaths(relative, "*");
                instead = "\"paths\": {\"*\": [" + TypeScript.Json.JsonCodec.MarshalString(suggestion) + "]}";
            }

            Removed("baseUrl", "", instead);
        }

        if (options.OutFile != default)
            Removed("outFile", "", "");
        if (options.Target == ScriptTarget.ES5)
            Removed("target", "ES5", "");
        if (options.Module == ModuleKind.AMD)
            Removed("module", "AMD", "");
        if (options.Module == ModuleKind.System)
            Removed("module", "System", "");
        if (options.Module == ModuleKind.UMD)
            Removed("module", "UMD", "");
        if (options.ModuleResolution == ModuleResolutionKind.Classic)
            Removed("moduleResolution", "Classic", "");
        if (options.AlwaysStrict == Tristate.False)
            Removed("alwaysStrict", "false", "");
        if (options.ESModuleInterop == Tristate.False)
            Removed("esModuleInterop", "false", "");
        if (options.AllowSyntheticDefaultImports == Tristate.False)
            Removed("allowSyntheticDefaultImports", "false", "");
        if (options.ModuleResolution == ModuleResolutionKind.Node10)
            Removed("moduleResolution", "node10", "");
        if (options.DownlevelIteration != Tristate.Unknown)
            Removed("downlevelIteration", "", "");
        if (options.StrictPropertyInitialization == Tristate.True && !options.GetStrictOptionValue(options.StrictNullChecks))
            Name(Diagnostics.Option_0_cannot_be_specified_without_specifying_option_1, "strictPropertyInitialization", "strictNullChecks");
        if (options.ExactOptionalPropertyTypes == Tristate.True && !options.GetStrictOptionValue(options.StrictNullChecks))
            Name(Diagnostics.Option_0_cannot_be_specified_without_specifying_option_1, "exactOptionalPropertyTypes", "strictNullChecks");
        if (options.IsolatedDeclarations == Tristate.True)
        {
            if (options.GetAllowJS())
                Name(Diagnostics.Option_0_cannot_be_specified_with_option_1, "allowJs", "isolatedDeclarations");
            if (!options.GetEmitDeclarations())
                Name(Diagnostics.Option_0_cannot_be_specified_without_specifying_option_1_or_option_2, "isolatedDeclarations", "declaration", "composite");
        }

        if (options.InlineSourceMap == Tristate.True)
        {
            if (options.SourceMap == Tristate.True)
                Name(Diagnostics.Option_0_cannot_be_specified_with_option_1, "sourceMap", "inlineSourceMap");
            if (options.MapRoot != default)
                Name(Diagnostics.Option_0_cannot_be_specified_with_option_1, "mapRoot", "inlineSourceMap");
        }

        if (options.Composite == Tristate.True)
        {
            if (options.Declaration == Tristate.False)
                Name(Diagnostics.Composite_projects_may_not_disable_declaration_emit, "declaration", "");
            if (options.Incremental == Tristate.False)
                Name(Diagnostics.Composite_projects_may_not_disable_incremental_compilation, "declaration", "");
        }

        if (options.TsBuildInfoFile == default && options.Incremental == Tristate.True && options.ConfigFilePath == default)
            Compiler(Diagnostics.Option_incremental_is_only_valid_with_a_known_configuration_file_like_tsconfig_json_or_when_tsBuildInfoFile_is_explicitly_provided);
        verifyProjectReferences();
        if (options.Composite == Tristate.True)
        {
            var roots = new HashSet<PathKey>((opts.Config.FileNames() ?? []).Select(file => caseSensitivity.PathKey(file.AsPath())));
            foreach (var file in files)
            {
                string rootPath = file.CanonicalSourceFile()?.PathKey() ?? file.PathKey();
                if (sourceFileMayBeEmitted(file, this, false, false) && !roots.Contains(PathKey.FromGoString(rootPath)))
                    addProcessingDiagnostic(new() { file = file.PathKey(), message = Diagnostics.File_0_is_not_listed_within_the_file_list_of_project_1_Projects_must_list_all_files_or_use_an_include_pattern, args = [file.FileNameGoBytes(), configFilePath.AsString()] });
            }
        }

        Diagnostic? PathSyntax(Func<PropertyAssignment, Diagnostic?> callback) => TypeScript.TsOptions.TsOptions.ForEachPropertyAssignment(compilerOptionsObject, "paths", callback);
        Diagnostic PathDiagnostic(bool onKey, string key, Message message, params object[] args) => PathSyntax(property => Ast.IsObjectLiteralExpression(property.Initializer) ? InObject(property.Initializer.AsObjectLiteralExpression(), onKey, key, "", message, args) : null) ?? Compiler(message, args);
        Diagnostic PathValueDiagnostic(string key, int index, Message message, params object[] args) => PathSyntax(property => Ast.IsObjectLiteralExpression(property.Initializer) ? TypeScript.TsOptions.TsOptions.ForEachPropertyAssignment(property.Initializer.AsObjectLiteralExpression(), key, keyProperty =>
        {
            var initializer = keyProperty.Initializer;
            if (Ast.IsArrayLiteralExpression(initializer) && initializer.ElementList()?.Nodes is { } elements && elements.Count > index)
            {
                var diagnostic = TypeScript.TsOptions.TsOptions.CreateDiagnosticForNodeInSourceFile(sourceFile!, elements[index], message, args);
                (programDiagnostics ??= []).Add(diagnostic);
                return diagnostic;
            }

            return null;
        }) : null) ?? Compiler(message, args);
        foreach (var(key, substitutions)in options.Paths?.Entries() ?? [])
        {
            if (!hasZeroOrOneAsteriskCharacter(key))
                PathDiagnostic(true, key, Diagnostics.Pattern_0_can_have_at_most_one_Asterisk_character, key);
            if (substitutions is null)
                PathDiagnostic(false, key, Diagnostics.Substitutions_for_pattern_0_should_be_an_array, key);
            else if (substitutions.Count == 0)
                PathDiagnostic(false, key, Diagnostics.Substitutions_for_pattern_0_shouldn_t_be_an_empty_array, key);
            for (int i = 0; i < (substitutions?.Count ?? 0); i++)
            {
                string substitution = substitutions![i];
                if (!hasZeroOrOneAsteriskCharacter(substitution))
                    PathValueDiagnostic(key, i, Diagnostics.Substitution_0_in_pattern_1_can_have_at_most_one_Asterisk_character, substitution, key);
                if (!TsPath.PathIsRelative(substitution) && !TsPath.PathIsAbsolute(substitution))
                    PathValueDiagnostic(key, i, Diagnostics.Non_relative_paths_are_not_allowed_Did_you_forget_a_leading_Slash);
            }
        }

        if (options.SourceMap != Tristate.True && options.InlineSourceMap != Tristate.True)
        {
            if (options.InlineSources == Tristate.True)
                Name(Diagnostics.Option_0_can_only_be_used_when_either_option_inlineSourceMap_or_option_sourceMap_is_provided, "inlineSources", "");
            if (options.SourceRoot != default)
                Name(Diagnostics.Option_0_can_only_be_used_when_either_option_inlineSourceMap_or_option_sourceMap_is_provided, "sourceRoot", "");
        }

        if (options.MapRoot != default && !(options.SourceMap == Tristate.True || options.DeclarationMap == Tristate.True))
            Name(Diagnostics.Option_0_cannot_be_specified_without_specifying_option_1_or_option_2, "mapRoot", "sourceMap", "declarationMap");
        if (options.DeclarationDir != default && !options.GetEmitDeclarations())
            Name(Diagnostics.Option_0_cannot_be_specified_without_specifying_option_1_or_option_2, "declarationDir", "declaration", "composite");
        if (options.DeclarationMap == Tristate.True && !options.GetEmitDeclarations())
            Name(Diagnostics.Option_0_cannot_be_specified_without_specifying_option_1_or_option_2, "declarationMap", "declaration", "composite");
        if (options.Lib is not null && options.NoLib == Tristate.True)
            Name(Diagnostics.Option_0_cannot_be_specified_with_option_1, "lib", "noLib");
        if ((options.IsolatedModules == Tristate.True || options.VerbatimModuleSyntax == Tristate.True) && options.PreserveConstEnums == Tristate.False)
            Name(Diagnostics.Option_preserveConstEnums_cannot_be_disabled_when_0_is_enabled, options.VerbatimModuleSyntax == Tristate.True ? "verbatimModuleSyntax" : "isolatedModules", "preserveConstEnums");
        if (options.OutDir != default || options.RootDir != default || options.SourceRoot != default || options.MapRoot != default || options.GetEmitDeclarations() && options.DeclarationDir != default)
        {
            var directory = CommonSourceDirectory();
            if (options.OutDir != default && directory == default && files.Any(file => RootedFilePath.FromGoString(file.FileNameGoBytes()).RootLength() > 1))
                Name(Diagnostics.Cannot_find_the_common_subdirectory_path_for_the_input_files, "outDir", "");
        }

        if (options.NoEmit != Tristate.True && options.Composite != Tristate.True && options.RootDir == default && options.ConfigFilePath != default && (options.OutDir != default || options.GetEmitDeclarations() && options.DeclarationDir != default || options.OutFile != default))
        {
            var directory = CommonSourceDirectory();
            var emitted = files.Where(file => !file.IsDeclarationFile && sourceFileMayBeEmitted(file, this, false, false)).Select(file => RootedFilePath.FromGoString(file.FileNameGoBytes())).ToList();
            var directory59 = TypeScript.OutputPaths.OutputPaths.GetComputedCommonSourceDirectory(emitted, BaseDirectory(), CaseSensitivity());
            if (directory59 != default && caseSensitivity.ComparePaths(directory.AsPath(), directory59.AsPath()) != 0)
            {
                string name = options.OutFile != default ? "outFile" : options.OutDir != default ? "outDir" : "declarationDir";
                string name2 = options.OutFile == default && options.OutDir != default ? "declarationDir" : "";
                string common = directory59.AsString();
                var(relative, ok) = caseSensitivity.RelativePathFromFileToPath(options.ConfigFilePath, directory59.AsPath());
                if (ok)
                    common = relative.AsModuleSpecifier().AsString();
                var diagnostic = Option(true, name, name2, Diagnostics.The_common_source_directory_of_0_is_1_The_rootDir_setting_must_be_explicitly_set_to_this_or_another_path_to_adjust_your_output_s_file_layout, options.ConfigFilePath.BaseName(), common);
                diagnostic.AddMessageChain(Ast.NewCompilerDiagnostic(Diagnostics.Visit_https_Colon_Slash_Slashaka_ms_Slashts6_for_migration_information));
            }
        }

        if (options.CheckJs == Tristate.True && !options.GetAllowJS())
            Name(Diagnostics.Option_0_cannot_be_specified_without_specifying_option_1, "checkJs", "allowJs");
        if (options.EmitDeclarationOnly == Tristate.True && !options.GetEmitDeclarations())
            Name(Diagnostics.Option_0_cannot_be_specified_without_specifying_option_1_or_option_2, "emitDeclarationOnly", "declaration", "composite");
        if (options.EmitDecoratorMetadata == Tristate.True && options.ExperimentalDecorators != Tristate.True)
            Name(Diagnostics.Option_0_cannot_be_specified_without_specifying_option_1, "emitDecoratorMetadata", "experimentalDecorators");
        if (options.JsxFactory != "")
        {
            if (options.ReactNamespace != "")
                Name(Diagnostics.Option_0_cannot_be_specified_with_option_1, "reactNamespace", "jsxFactory");
            if (options.Jsx is JsxEmit.ReactJSX or JsxEmit.ReactJSXDev)
                Name(Diagnostics.Option_0_cannot_be_specified_when_option_jsx_is_1, "jsxFactory", GeneratedOptionEnums.String(options.Jsx));
            if (Parser.ParseIsolatedEntityName(GoString.Bytes(options.JsxFactory))is null)
                Value("jsxFactory", Diagnostics.Invalid_value_for_jsxFactory_0_is_not_a_valid_identifier_or_qualified_name, options.JsxFactory);
        }
        else if (options.ReactNamespace != "" && !TsScanner.IsIdentifierTextByteString(options.ReactNamespace, LanguageVariant.Standard))
            Value("reactNamespace", Diagnostics.Invalid_value_for_reactNamespace_0_is_not_a_valid_identifier, options.ReactNamespace);
        if (options.JsxFragmentFactory != "")
        {
            if (options.JsxFactory == "")
                Name(Diagnostics.Option_0_cannot_be_specified_without_specifying_option_1, "jsxFragmentFactory", "jsxFactory");
            if (options.Jsx is JsxEmit.ReactJSX or JsxEmit.ReactJSXDev)
                Name(Diagnostics.Option_0_cannot_be_specified_when_option_jsx_is_1, "jsxFragmentFactory", GeneratedOptionEnums.String(options.Jsx));
            if (Parser.ParseIsolatedEntityName(GoString.Bytes(options.JsxFragmentFactory))is null)
                Value("jsxFragmentFactory", Diagnostics.Invalid_value_for_jsxFragmentFactory_0_is_not_a_valid_identifier_or_qualified_name, options.JsxFragmentFactory);
        }

        if (options.ReactNamespace != "" && options.Jsx is JsxEmit.ReactJSX or JsxEmit.ReactJSXDev)
            Name(Diagnostics.Option_0_cannot_be_specified_when_option_jsx_is_1, "reactNamespace", GeneratedOptionEnums.String(options.Jsx));
        if (options.JsxImportSource != "" && options.Jsx == JsxEmit.React)
            Name(Diagnostics.Option_0_cannot_be_specified_when_option_jsx_is_1, "jsxImportSource", GeneratedOptionEnums.String(options.Jsx));
        var moduleKind = options.GetEmitModuleKind();
        var resolution = options.GetModuleResolutionKind();
        if (options.AllowImportingTsExtensions == Tristate.True && !(options.NoEmit == Tristate.True || options.EmitDeclarationOnly == Tristate.True || options.RewriteRelativeImportExtensions == Tristate.True))
            Value("allowImportingTsExtensions", Diagnostics.Option_allowImportingTsExtensions_can_only_be_used_when_one_of_noEmit_emitDeclarationOnly_or_rewriteRelativeImportExtensions_is_set);
        if (options.ResolvePackageJsonExports == Tristate.True && !moduleResolutionSupportsPackageJsonExportsAndImports(resolution))
            Name(Diagnostics.Option_0_can_only_be_used_when_moduleResolution_is_set_to_node16_nodenext_or_bundler, "resolvePackageJsonExports", "");
        if (options.ResolvePackageJsonImports == Tristate.True && !moduleResolutionSupportsPackageJsonExportsAndImports(resolution))
            Name(Diagnostics.Option_0_can_only_be_used_when_moduleResolution_is_set_to_node16_nodenext_or_bundler, "resolvePackageJsonImports", "");
        if (options.CustomConditions is not null && !moduleResolutionSupportsPackageJsonExportsAndImports(resolution))
            Name(Diagnostics.Option_0_can_only_be_used_when_moduleResolution_is_set_to_node16_nodenext_or_bundler, "customConditions", "");
        if (resolution == ModuleResolutionKind.Bundler && !emitModuleKindIsNonNodeESM(moduleKind) && moduleKind != ModuleKind.Preserve && moduleKind != ModuleKind.CommonJS)
            Value("moduleResolution", Diagnostics.Option_0_can_only_be_used_when_module_is_set_to_preserve_commonjs_or_es2015_or_later, "bundler");
        if (moduleKind >= ModuleKind.Node16 && moduleKind <= ModuleKind.NodeNext && !(resolution >= ModuleResolutionKind.Node16 && resolution <= ModuleResolutionKind.NodeNext))
        {
            string resolutionName = GeneratedOptionEnums.ModuleKindToModuleResolutionKind.TryGetValue(moduleKind, out var equivalent) ? GeneratedOptionEnums.String(equivalent) : "Node16";
            Value("moduleResolution", Diagnostics.Option_moduleResolution_must_be_set_to_0_or_left_unspecified_when_option_module_is_set_to_1, resolutionName, ModuleKindStringer.String(moduleKind));
        }
        else if (resolution >= ModuleResolutionKind.Node16 && resolution <= ModuleResolutionKind.NodeNext && !(moduleKind >= ModuleKind.Node16 && moduleKind <= ModuleKind.NodeNext))
            Value("module", Diagnostics.Option_module_must_be_set_to_0_when_option_moduleResolution_is_set_to_1, GeneratedOptionEnums.String(resolution), GeneratedOptionEnums.String(resolution));
        if (options.NoEmit != Tristate.True && options.SuppressOutputPathCheck != Tristate.True)
        {
            var seen = new HashSet<PathKey>();
            void Verify(RootedFilePath file)
            {
                if (file == default)
                    return;
                var path = PathKeyForFileName(file);
                if (filesByPath.ContainsKey(path.AsString()))
                {
                    var diagnostic = Ast.NewCompilerDiagnostic(Diagnostics.Cannot_write_file_0_because_it_would_overwrite_input_file, file);
                    if (configFilePath == default)
                        diagnostic.AddMessageChain(Ast.NewCompilerDiagnostic(Diagnostics.Adding_a_tsconfig_json_file_will_help_organize_projects_that_contain_both_TypeScript_and_JavaScript_files_Learn_more_at_https_Colon_Slash_Slashaka_ms_Slashtsconfig));
                    blockEmittingOfFile(file, diagnostic);
                }

                if (!seen.Add(path))
                    blockEmittingOfFile(file, Ast.NewCompilerDiagnostic(Diagnostics.Cannot_write_file_0_because_it_would_be_overwritten_by_multiple_input_files, file));
            }

            TypeScript.OutputPaths.OutputPaths.ForEachEmittedFile(this, options, (paths, file) =>
            {
                Verify(paths.JsFilePath());
                Verify(paths.SourceMapFilePath());
                Verify(paths.DeclarationFilePath());
                Verify(paths.DeclarationMapPath());
                return false;
            }, getSourceFilesToEmit(null, false, false), false);
            Verify(opts.Config.GetBuildInfoFileName());
        }
    }

    internal List<Diagnostic>? collectDiagnostics(CancellationToken context, SourceFile? source, bool concurrent, Func<CancellationToken, SourceFile, List<Diagnostic>?> collect)
    {
        if (source is not null)
            return filterAndSortDiagnostics(collect(context, source));
        return filterAndSortDiagnostics(concatDiagnostics(collectDiagnosticsFromFiles(context, files, concurrent, collect)));
    }

    internal List<List<Diagnostic>?> collectDiagnosticsFromFiles(CancellationToken context, List<SourceFile> sourceFiles, bool concurrent, Func<CancellationToken, SourceFile, List<Diagnostic>?> collect)
    {
        var diagnostics = Enumerable.Repeat<List<Diagnostic>?>(null, sourceFiles.Count).ToList();
        var work = Core.NewWorkGroup(!concurrent || SingleThreaded());
        for (int i = 0; i < sourceFiles.Count; i++)
        {
            int index = i;
            work.Queue(() => diagnostics[index] = collect(context, sourceFiles[index]));
        }

        work.RunAndWait();
        return diagnostics;
    }

    private static List<Diagnostic>? concatDiagnostics(IEnumerable<List<Diagnostic>?> diagnostics)
    {
        List<Diagnostic>? result = null;
        foreach (var list in diagnostics)
            if ((list?.Count ?? 0) > 0)
                (result ??= []).AddRange(list!);
        return result;
    }

    internal static List<Diagnostic>? filterAndSortDiagnostics(List<Diagnostic>? diagnostics) => SortAndDeduplicateDiagnostics(Core.Filter(diagnostics, diagnostic =>
    {
        var file = diagnostic.File();
        if (!diagnostic.ReportsUnnecessary() || file is null || file.SpanMap()is null || diagnostic.Source() != "")
            return true;
        return file.SpanMap().VirtualToOriginalSpan(diagnostic.Loc()).Fidelity != TypeScript.SpanMap.Fidelity.None;
    }));
    public List<Diagnostic>? GetSyntacticDiagnostics(CancellationToken context = default, SourceFile? source = null) => collectDiagnostics(context, source, false, (_, file) =>
    {
        var diagnostics = Core.Concatenate(file.Diagnostics(), file.JSDiagnostics());
        if (Ast.IsSourceFileJS(file) && !Ast.IsCheckJSEnabledForFile(file, Options()))
            diagnostics = Core.Concatenate(diagnostics, getAdditionalJSSyntacticDiagnostics(file, Options()));
        return diagnostics;
    });
    internal static List<Diagnostic>? getAdditionalJSSyntacticDiagnostics(SourceFile file, CompilerOptions options)
    {
        if (options.ExperimentalDecorators == Tristate.True)
            return null;
        List<Diagnostic>? diagnostics = null;
        bool Walk(Node node)
        {
            if ((node.SubtreeFacts() & SubtreeFacts.ContainsDecorators) == 0)
                return false;
            if (node.Kind == Kind.Parameter && Ast.HasDecorators(node))
            {
                var decorator = node.ModifierNodes()?.Find(Ast.IsDecorator);
                if (decorator is not null)
                    (diagnostics ??= []).Add(Ast.NewDiagnostic(file, decorator.Loc, Diagnostics.Decorators_are_not_valid_here));
            }

            node.ForEachChild(Walk);
            return false;
        }

        file.AsNode().ForEachChild(Walk);
        return diagnostics;
    }

    public List<Diagnostic>? GetBindDiagnostics(CancellationToken context = default, SourceFile? source = null)
    {
        if (source is not null)
            Binder.BindSourceFile(source);
        else
            BindSourceFiles();
        return collectDiagnostics(context, source, false, (_, file) => file.BindDiagnostics());
    }

    public List<Diagnostic>? GetProgramDiagnostics() => SortAndDeduplicateDiagnostics(concatDiagnostics([programDiagnostics, processedFiles.contentMapperDiagnostics, contentMapperOptionDiagnostics, includeProcessor.GetDiagnostics(this).GetGlobalDiagnostics()]));
    internal void collectContentMapperOptionDiagnostics()
    {
        var project = ContentMapperProject();
        if (project is null)
            return;
        var diagnostics = project.Diagnostics();
        contentMapperOptionDiagnostics = diagnostics?.Select(diagnostic =>
        {
            var(file, location) = TypeScript.TsOptions.TsOptions.GetContentMapperOptionDiagnosticLocation(opts.Config, diagnostic.Mapper!, diagnostic.Path);
            return Ast.NewExternalDiagnostic(file, location, diagnostic.Source, DiagnosticCategory.Error, diagnostic.Code, diagnostic.MessageText);
        }).ToList();
    }

    // Authored inspection accessor for the actual include collection. It intentionally
    // does not apply semantic SkipTypeChecking or acquire a checker.
    public List<Diagnostic>? GetFileInclusionDiagnostics(SourceFile? file = null) =>
        file is null ? includeProcessor.GetDiagnostics(this).GetGlobalDiagnostics() :
            includeProcessor.GetDiagnostics(this).GetDiagnosticsForFile(file);

    public List<Diagnostic>? GetIncludeProcessorDiagnostics(SourceFile file)
    {
        if (SkipTypeChecking(file, false))
            return null;
        return getDiagnosticsWithPrecedingDirectives(file, includeProcessor.GetDiagnostics(this).GetDiagnosticsForFile(file)).Diagnostics;
    }

    public bool SkipTypeChecking(SourceFile file, bool ignoreNoCheck = false) => !ignoreNoCheck && Options().NoCheck == Tristate.True || Options().SkipLibCheck == Tristate.True && file.IsDeclarationFile || Options().SkipDefaultLibCheck == Tristate.True && IsSourceFileDefaultLibrary(PathKey.FromGoString(file.PathKey())) || IsSourceFromProjectReference(PathKey.FromGoString(file.PathKey())) || !canIncludeBindAndCheckDiagnostics(file);
    internal bool canIncludeBindAndCheckDiagnostics(SourceFile file)
    {
        if (file.CheckJsDirective is not null && !file.CheckJsDirective.Enabled)
            return false;
        if (file.ScriptKind is ScriptKind.TS or ScriptKind.TSX)
            return true;
        bool js = file.ScriptKind is ScriptKind.JS or ScriptKind.JSX;
        bool checkedJS = js && Ast.IsCheckJSEnabledForFile(file, Options());
        return Ast.IsPlainJSFile(file, Options().CheckJs) || checkedJS;
    }

    internal List<Diagnostic>? collectCheckerDiagnostics(CancellationToken context, SourceFile? source, Func<CancellationToken, TypeScript.Checking.Checker, SourceFile, List<Diagnostic>?> collect)
    {
        if (source is not null)
        {
            if (SkipTypeChecking(source, false))
                return null;
            var(checker, done) = GetTypeCheckerForFileExclusive(context, source);
            try
            {
                return filterAndSortDiagnostics(collect(context, checker, source));
            }
            finally
            {
                done();
            }
        }

        return filterAndSortDiagnostics(concatDiagnostics(collectCheckerDiagnosticsFromFiles(context, files, collect)));
    }

    internal List<List<Diagnostic>?> collectCheckerDiagnosticsFromFiles(CancellationToken context, List<SourceFile> sourceFiles, Func<CancellationToken, TypeScript.Checking.Checker, SourceFile, List<Diagnostic>?> collect)
    {
        var diagnostics = Enumerable.Repeat<List<Diagnostic>?>(null, sourceFiles.Count).ToList();
        if (compilerCheckerPool is not null)
            compilerCheckerPool.forEachCheckerGroupDo(context, sourceFiles, SingleThreaded(), (checker, index, file) => diagnostics[index] = collect(context, checker, file));
        else
        {
            var work = Core.NewWorkGroup(SingleThreaded());
            for (int i = 0; i < sourceFiles.Count; i++)
            {
                int index = i;
                SourceFile file = sourceFiles[i];
                if (SkipTypeChecking(file, false))
                    continue;
                work.Queue(() =>
                {
                    var(checker, done) = checkerPool.GetChecker(context, file);
                    try
                    {
                        diagnostics[index] = collect(context, checker, file);
                    }
                    finally
                    {
                        done();
                    }
                });
            }

            work.RunAndWait();
        }

        return diagnostics;
    }

    public List<Diagnostic>? GetSemanticDiagnostics(CancellationToken context = default, SourceFile? source = null) => collectCheckerDiagnostics(context, source, getSemanticDiagnosticsWithChecker);
    // program.go:828. Newly discovered globals belong to each requested file;
    // noEmit filtering remains the builder's responsibility.
    public Dictionary<SourceFile, List<Diagnostic>?> GetSemanticDiagnosticsForIncremental(CancellationToken context, List<SourceFile> sourceFiles)
    {
        var diagnostics = collectCheckerDiagnosticsFromFiles(context, sourceFiles,
            (token, checker, file) => getBindAndCheckDiagnosticsWithChecker(token, checker, file, true));
        var result = new Dictionary<SourceFile, List<Diagnostic>?>(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < diagnostics.Count; i++)
            result[sourceFiles[i]] = filterAndSortDiagnostics(diagnostics[i]);
        return result;
    }
    internal List<Diagnostic>? getSemanticDiagnosticsWithChecker(CancellationToken context, TypeScript.Checking.Checker checker, SourceFile file) => Core.Concatenate(FilterNoEmitSemanticDiagnostics(getBindAndCheckDiagnosticsWithChecker(context, checker, file, false), Options()), GetIncludeProcessorDiagnostics(file));
    internal List<Diagnostic>? getBindAndCheckDiagnosticsWithChecker(CancellationToken context, TypeScript.Checking.Checker checker, SourceFile file, bool includeDeferredGlobals)
    {
        if (SkipTypeChecking(file, false))
            return null;
        var previous = includeDeferredGlobals ? checker.GetGlobalDiagnostics() : null;
        var diagnostics = file.BindDiagnostics()?.ToList();
        diagnostics = Core.Concatenate(diagnostics, checker.GetDiagnostics(context, file));
        if (includeDeferredGlobals)
        {
            if (checker.WasCanceled())
                return null;
            var globals = checker.GetGlobalDiagnostics();
            if ((globals?.Count ?? 0) > (previous?.Count ?? 0))
                foreach (var diagnostic in globals!)
                    if (!(previous?.Any(old => Ast.CompareDiagnostics(old, diagnostic) == 0) ?? false))
                        (diagnostics ??= []).Add(diagnostic);
        }

        if (Ast.IsPlainJSFile(file, Options().CheckJs))
            return Core.Filter(diagnostics, diagnostic => plainJSErrors.Contains(diagnostic.Code()));
        if ((file.ScriptKind is ScriptKind.JS or ScriptKind.JSX) && Ast.IsCheckJSEnabledForFile(file, Options()))
            diagnostics = Core.Concatenate(diagnostics, file.JSDocDiagnostics());
        var(filtered, directives) = getDiagnosticsWithPrecedingDirectives(file, diagnostics);
        foreach (var directive in directives?.Values.AsEnumerable() ?? [])
            if (directive.Kind == CommentDirectiveKind.ExpectError)
                (filtered ??= []).Add(Ast.NewDiagnostic(file, directive.Loc, Diagnostics.Unused_ts_expect_error_directive));
        return applyContentMapperDiagnosticDirectives(file, filtered);
    }

    internal static List<Diagnostic>? applyContentMapperDiagnosticDirectives(SourceFile file, List<Diagnostic>? diagnostics)
    {
        var directives = file.DiagnosticDirectives();
        if ((directives?.Count ?? 0) == 0)
            return diagnostics;
        var used = new bool[directives!.Count];
        bool MarkUsed(Diagnostic diagnostic)
        {
            if (!ReferenceEquals(diagnostic.File(), file) || diagnostic.Source() != "")
                return false;
            for (int i = 0; i < directives.Count; i++)
                if (diagnostic.Pos() >= directives[i].VirtualRange.Pos() && diagnostic.Pos() < directives[i].VirtualRange.End())
                {
                    used[i] = true;
                    return true;
                }

            return false;
        }

        var filtered = Core.Filter(diagnostics, diagnostic => !MarkUsed(diagnostic));
        for (int i = 0; i < directives.Count; i++)
            if (directives[i].Policy == MappedDiagnosticDirectivePolicy.Expect && !used[i])
                (filtered ??= []).Add(Ast.NewExternalDiagnostic(file, directives[i].OriginalRange, directives[i].Source, DiagnosticCategory.Error, directives[i].UnusedCode, directives[i].UnusedMessageText));
        return filtered;
    }

    internal (List<Diagnostic>? Diagnostics, Dictionary<int, CommentDirective>? Directives) getDiagnosticsWithPrecedingDirectives(SourceFile file, List<Diagnostic>? diagnostics)
    {
        if ((file.CommentDirectives?.Count ?? 0) == 0)
            return (diagnostics, null);
        var directives = new Dictionary<int, CommentDirective>();
        foreach (var directive in file.CommentDirectives!)
            directives[TsScanner.GetECMALineOfPosition(file, directive.Loc.Pos())] = directive;
        var lineStarts = TsScanner.GetECMALineStarts(file);
        var filtered = new List<Diagnostic>();
        foreach (var diagnostic in diagnostics ?? [])
        {
            bool ignore = false;
            if (!ReferenceEquals(diagnostic.File(), file))
            {
                filtered.Add(diagnostic);
                continue;
            }

            for (int line = TsScanner.ComputeLineOfPosition(lineStarts.ToArray(), diagnostic.Pos()) - 1; line >= 0; line--)
            {
                if (directives.TryGetValue(line, out var directive))
                {
                    ignore = true;
                    directives[line] = directive with
                    {
                        Kind = CommentDirectiveKind.Ignore
                    };
                    break;
                }

                if (!isCommentOrBlankLine(file.Text(), lineStarts[line]))
                    break;
            }

            if (!ignore)
                filtered.Add(diagnostic);
        }

        return (filtered, directives);
    }

    internal static bool isCommentOrBlankLine(string text, int position)
    {
        while (position < text.Length && text[position] is ' ' or '\t')
            position++;
        return position == text.Length || position < text.Length && text[position] is '\r' or '\n' || position + 1 < text.Length && text[position] == '/' && text[position + 1] == '/';
    }

    public List<Diagnostic>? GetSuggestionDiagnostics(CancellationToken context = default, SourceFile? source = null) => collectCheckerDiagnostics(context, source, getSuggestionDiagnosticsWithChecker);
    internal List<Diagnostic>? getSuggestionDiagnosticsWithChecker(CancellationToken context, TypeScript.Checking.Checker checker, SourceFile file)
    {
        if (SkipTypeChecking(file, false))
            return null;
        return checker.GetSuggestionDiagnostics(context, file);
    }

    public List<Diagnostic>? GetDeclarationDiagnostics(CancellationToken context = default, SourceFile? source = null) => collectDiagnostics(context, source, true, getDeclarationDiagnosticsForFile);
    internal List<Diagnostic>? getDeclarationDiagnosticsForFile(CancellationToken context, SourceFile file)
    {
        if (file.IsDeclarationFile)
            return[];
        throw new TypeScript.Checking.UnsupportedSemanticFeatureException("declaration transform diagnostics are outside Program construction");
    }

    public long LineCount()
    {
        long count = 0;
        foreach (var file in files)
            count = unchecked(count + file.ECMALineMap().Count);
        return count;
    }

    public long IdentifierCount()
    {
        long count = 0;
        foreach (var file in files)
            count = unchecked(count + file.IdentifierCount);
        return count;
    }

    public long SymbolCount()
    {
        long count = 0;
        foreach (var file in files)
            count += file.SymbolCount;
        int value = unchecked((int)(uint)count);
        ForEachCheckerParallel((_, checker) => Interlocked.Add(ref value, checker.SymbolCount));
        return unchecked((uint)value);
    }

    public long TypeCount()
    {
        int value = 0;
        ForEachCheckerParallel((_, checker) => Interlocked.Add(ref value, checker.TypeCount));
        return unchecked((uint)value);
    }

    public long InstantiationCount()
    {
        int value = 0;
        ForEachCheckerParallel((_, checker) => Interlocked.Add(ref value, checker.TotalInstantiationCount));
        return unchecked((uint)value);
    }

    private static readonly HashSet<int> plainJSErrors = [Diagnostics.Cannot_redeclare_block_scoped_variable_0.Code(), Diagnostics.A_module_cannot_have_multiple_default_exports.Code(), Diagnostics.Another_export_default_is_here.Code(), Diagnostics.The_first_export_default_is_here.Code(), Diagnostics.Identifier_expected_0_is_a_reserved_word_at_the_top_level_of_a_module.Code(), Diagnostics.Identifier_expected_0_is_a_reserved_word_in_strict_mode_Modules_are_automatically_in_strict_mode.Code(), Diagnostics.Identifier_expected_0_is_a_reserved_word_that_cannot_be_used_here.Code(), Diagnostics.X_constructor_is_a_reserved_word.Code(), Diagnostics.X_delete_cannot_be_called_on_an_identifier_in_strict_mode.Code(), Diagnostics.Code_contained_in_a_class_is_evaluated_in_JavaScript_s_strict_mode_which_does_not_allow_this_use_of_0_For_more_information_see_https_Colon_Slash_Slashdeveloper_mozilla_org_Slashen_US_Slashdocs_SlashWeb_SlashJavaScript_SlashReference_SlashStrict_mode.Code(), Diagnostics.Invalid_use_of_0_Modules_are_automatically_in_strict_mode.Code(), Diagnostics.Invalid_use_of_0_in_strict_mode.Code(), Diagnostics.A_label_is_not_allowed_here.Code(), Diagnostics.X_with_statements_are_not_allowed_in_strict_mode.Code(), Diagnostics.A_break_statement_can_only_be_used_within_an_enclosing_iteration_or_switch_statement.Code(), Diagnostics.A_break_statement_can_only_jump_to_a_label_of_an_enclosing_statement.Code(), Diagnostics.A_class_declaration_without_the_default_modifier_must_have_a_name.Code(), Diagnostics.A_class_member_cannot_have_the_0_keyword.Code(), Diagnostics.A_comma_expression_is_not_allowed_in_a_computed_property_name.Code(), Diagnostics.A_continue_statement_can_only_be_used_within_an_enclosing_iteration_statement.Code(), Diagnostics.A_continue_statement_can_only_jump_to_a_label_of_an_enclosing_iteration_statement.Code(), Diagnostics.A_default_clause_cannot_appear_more_than_once_in_a_switch_statement.Code(), Diagnostics.A_default_export_must_be_at_the_top_level_of_a_file_or_module_declaration.Code(), Diagnostics.A_deferred_import_must_specify_a_namespace_binding.Code(), Diagnostics.A_definite_assignment_assertion_is_not_permitted_in_this_context.Code(), Diagnostics.A_destructuring_declaration_must_have_an_initializer.Code(), Diagnostics.A_get_accessor_cannot_have_parameters.Code(), Diagnostics.A_rest_element_cannot_contain_a_binding_pattern.Code(), Diagnostics.A_rest_element_cannot_have_a_property_name.Code(), Diagnostics.A_rest_element_cannot_have_an_initializer.Code(), Diagnostics.A_rest_element_must_be_last_in_a_destructuring_pattern.Code(), Diagnostics.A_rest_parameter_cannot_have_an_initializer.Code(), Diagnostics.A_rest_parameter_must_be_last_in_a_parameter_list.Code(), Diagnostics.A_rest_parameter_or_binding_pattern_may_not_have_a_trailing_comma.Code(), Diagnostics.A_return_statement_cannot_be_used_inside_a_class_static_block.Code(), Diagnostics.A_set_accessor_cannot_have_rest_parameter.Code(), Diagnostics.A_set_accessor_must_have_exactly_one_parameter.Code(), Diagnostics.A_source_phase_import_must_specify_a_local_binding.Code(), Diagnostics.An_export_declaration_can_only_be_used_at_the_top_level_of_a_module.Code(), Diagnostics.An_export_declaration_cannot_have_modifiers.Code(), Diagnostics.An_import_declaration_can_only_be_used_at_the_top_level_of_a_module.Code(), Diagnostics.An_import_declaration_cannot_have_modifiers.Code(), Diagnostics.An_object_member_cannot_be_declared_optional.Code(), Diagnostics.Argument_of_dynamic_import_cannot_be_spread_element.Code(), Diagnostics.Cannot_assign_to_private_method_0_Private_methods_are_not_writable.Code(), Diagnostics.Cannot_redeclare_identifier_0_in_catch_clause.Code(), Diagnostics.Catch_clause_variable_cannot_have_an_initializer.Code(), Diagnostics.Class_decorators_can_t_be_used_with_static_private_identifier_Consider_removing_the_experimental_decorator.Code(), Diagnostics.Classes_can_only_extend_a_single_class.Code(), Diagnostics.Classes_may_not_have_a_field_named_constructor.Code(), Diagnostics.Did_you_mean_to_use_a_Colon_An_can_only_follow_a_property_name_when_the_containing_object_literal_is_part_of_a_destructuring_pattern.Code(), Diagnostics.Duplicate_label_0.Code(), Diagnostics.Dynamic_imports_can_only_accept_a_module_specifier_and_an_optional_set_of_attributes_as_arguments.Code(), Diagnostics.X_for_await_loops_cannot_be_used_inside_a_class_static_block.Code(), Diagnostics.JSX_attributes_must_only_be_assigned_a_non_empty_expression.Code(), Diagnostics.JSX_elements_cannot_have_multiple_attributes_with_the_same_name.Code(), Diagnostics.JSX_expressions_may_not_use_the_comma_operator_Did_you_mean_to_write_an_array.Code(), Diagnostics.JSX_property_access_expressions_cannot_include_JSX_namespace_names.Code(), Diagnostics.Jump_target_cannot_cross_function_boundary.Code(), Diagnostics.Line_terminator_not_permitted_before_arrow.Code(), Diagnostics.Modifiers_cannot_appear_here.Code(), Diagnostics.Named_and_namespace_imports_are_not_allowed_in_a_source_phase_import.Code(), Diagnostics.Only_a_single_variable_declaration_is_allowed_in_a_for_in_statement.Code(), Diagnostics.Only_a_single_variable_declaration_is_allowed_in_a_for_of_statement.Code(), Diagnostics.Optional_chaining_cannot_be_used_with_import_source.Code(), Diagnostics.Private_identifiers_are_not_allowed_outside_class_bodies.Code(), Diagnostics.Private_identifiers_are_only_allowed_in_class_bodies_and_may_only_be_used_as_part_of_a_class_member_declaration_property_access_or_on_the_left_hand_side_of_an_in_expression.Code(), Diagnostics.Property_0_is_not_accessible_outside_class_1_because_it_has_a_private_identifier.Code(), Diagnostics.Source_phase_imports_are_not_allowed_on_statements_that_compile_to_CommonJS_require_calls.Code(), Diagnostics.Source_phase_imports_are_only_supported_when_the_module_option_is_set_to_esnext_nodenext_or_preserve.Code(), Diagnostics.Tagged_template_expressions_are_not_permitted_in_an_optional_chain.Code(), Diagnostics.The_left_hand_side_of_a_for_of_statement_may_not_be_async.Code(), Diagnostics.The_variable_declaration_of_a_for_in_statement_cannot_have_an_initializer.Code(), Diagnostics.The_variable_declaration_of_a_for_of_statement_cannot_have_an_initializer.Code(), Diagnostics.Trailing_comma_not_allowed.Code(), Diagnostics.Variable_declaration_list_cannot_be_empty.Code(), Diagnostics.X_0_and_1_operations_cannot_be_mixed_without_parentheses.Code(), Diagnostics.X_0_expected.Code(), Diagnostics.X_0_is_not_a_valid_meta_property_for_keyword_import_Did_you_mean_meta_defer_or_source.Code(), Diagnostics.X_0_is_not_a_valid_meta_property_for_keyword_1_Did_you_mean_2.Code(), Diagnostics.X_0_list_cannot_be_empty.Code(), Diagnostics.X_0_modifier_already_seen.Code(), Diagnostics.X_0_modifier_cannot_appear_on_a_constructor_declaration.Code(), Diagnostics.X_0_modifier_cannot_appear_on_a_module_or_namespace_element.Code(), Diagnostics.X_0_modifier_cannot_appear_on_a_parameter.Code(), Diagnostics.X_0_modifier_cannot_appear_on_class_elements_of_this_kind.Code(), Diagnostics.X_0_modifier_cannot_be_used_here.Code(), Diagnostics.X_0_modifier_must_precede_1_modifier.Code(), Diagnostics.X_0_declarations_can_only_be_declared_inside_a_block.Code(), Diagnostics.X_0_declarations_must_be_initialized.Code(), Diagnostics.X_extends_clause_already_seen.Code(), Diagnostics.X_let_is_not_allowed_to_be_used_as_a_name_in_let_or_const_declarations.Code(), Diagnostics.Class_constructor_may_not_be_a_generator.Code(), Diagnostics.Class_constructor_may_not_be_an_accessor.Code(), Diagnostics.X_await_expressions_are_only_allowed_within_async_functions_and_at_the_top_levels_of_modules.Code(), Diagnostics.X_await_using_statements_are_only_allowed_within_async_functions_and_at_the_top_levels_of_modules.Code(), Diagnostics.Private_field_0_must_be_declared_in_an_enclosing_class.Code(), Diagnostics.This_condition_will_always_return_0_since_JavaScript_compares_objects_by_reference_not_value.Code(), ];
    internal DefaultResolver newResolver() => resolutionData.NewResolver(projectReferenceFileMapper.ResolutionHost(new CompilerResolutionHost(Host(), BaseDirectory())));
    public RootedDirectoryPath GetNearestAncestorDirectoryWithPackageJson(RootedDirectoryPath directory)
    {
        var scope = newResolver().GetPackageScopeForPath(directory);
        return scope.Exists() ? scope!.PackageDirectory.AsDirectoryPath() : default;
    }

    public TypeScript.PackageJson.InfoCacheEntry? GetPackageJsonInfo(RootedFilePath filename)
    {
        var directory = filename.Directory();
        var scope = newResolver().GetPackageScopeForPath(directory);
        return scope.Exists() && scope!.PackageDirectory.AsDirectoryPath() == directory ? scope : null;
    }

    public void PackageJsonCacheEntries(Func<PathKey, TypeScript.PackageJson.InfoCacheEntry, bool> callback) => resolutionData.PackageJsonCacheEntries(callback);
    public (string ModuleReference, Node? Specifier) GetJSXRuntimeImportSpecifier(PathKey path)
    {
        var result = processedFiles.jsxRuntimeImportSpecifiers?.GetValueOrDefault(path.AsString());
        return result is not null ? (result.moduleReference, result.specifier) : ("", null);
    }

    public Node? GetImportHelpersImportSpecifier(PathKey path) => processedFiles.importHelpersImportSpecifiers?.GetValueOrDefault(path.AsString());
    public HashSet<string> ResolvedPackageNames() => collectPackageNames().resolved;
    public HashSet<string> UnresolvedPackageNames() => collectPackageNames().unresolved;
    public HashSet<string> DeepImportPackageNames() => collectPackageNames().deepImportPackages;
    internal PackageNamesInfo collectPackageNames() => packageNames.getValue(() =>
    {
        var resolver = newResolver();
        var names = new PackageNamesInfo();
        foreach (var file in files)
        {
            var filename = RootedFilePath.FromGoString(file.FileNameGoBytes());
            if (IsSourceFileDefaultLibrary(PathKey.FromGoString(file.PathKey())) || IsSourceFileFromExternalLibrary(file) || filename.ContainsLowercaseDirectorySequence("/node_modules/"))
                continue;
            foreach (var import in file.Imports() ?? [])
            {
                if (Ast.IsSourcePhaseImport(import.Parent) || TsPath.IsExternalModuleNameRelative(import.Text()))
                    continue;
                var key = new ModeAwareCacheKey(import.Text(), GetModeForUsageLocation(file, import));
                if (resolvedModules.GetValueOrDefault(file.PathKey())?.GetValueOrDefault(key)is { } module && module.IsResolved())
                {
                    if (!module.IsExternalLibraryImport)
                        continue;
                    string name = module.PackageId.Name;
                    if (name == "")
                    {
                        var scope = resolver.GetPackageScopeForPath(module.ResolvedFileName.Directory());
                        if (scope.Exists())
                        {
                            var(value, ok) = scope!.Contents!.Fields.Name.GetValue();
                            if (ok)
                                name = value;
                        }
                    }

                    if (name == "")
                        name = TypeScript.ModuleSpecifiers.ModuleSpecifiers.GetPackageNameFromDirectory(module.ResolvedFileName.AsPath());
                    if (name != "")
                    {
                        names.resolved.Add(name);
                        var(_, rest) = ModuleUtilities.ParsePackageName(import.Text());
                        if (rest != "")
                        {
                            var scope = resolver.GetPackageScopeForPath(module.ResolvedFileName.Directory());
                            if (scope.Exists() && !scope!.Contents!.Exports.IsPresent())
                                names.deepImportPackages.Add(ModuleUtilities.GetPackageNameFromTypesPackageName(name));
                        }
                    }

                    continue;
                }

                names.unresolved.Add(import.Text());
            }
        }

        return names;
    });
    public TypeScript.Symlinks.KnownSymlinks GetSymlinkCache() => knownSymlinks.getValue(() =>
    {
        var resolver = newResolver();
        var symlinks = TypeScript.Symlinks.KnownSymlinks.NewKnownSymlinks(CaseSensitivity());
        if (resolvedModules.Count > 0 || typeResolutionsInFile.Count > 0)
            symlinks.SetSymlinksFromResolutions(ForEachResolvedModule, ForEachResolvedTypeReferenceDirective);
        var seen = new HashSet<PathKey>();
        foreach (var(path, metadata)in processedFiles.sourceFileMetaDatas)
        {
            if (metadata.PackageJsonDirectory == default || !SourceFileMayBeEmitted(GetSourceFileByPath(PathKey.FromGoString(path))!, false) || !seen.Add(caseSensitivity.PathKey(metadata.PackageJsonDirectory.AsPath())))
                continue;
            var packageJsonFile = metadata.PackageJsonDirectory.ResolveFile("package.json");
            var info = GetPackageJsonInfo(packageJsonFile);
            if (info.GetContents()is not { } contents)
                continue;
            foreach (string dependency in contents.Fields.GetRuntimeDependencyNames())
            {
                var adjacent = caseSensitivity.PathKey(metadata.PackageJsonDirectory.ResolveDirectory(TsPath.CombinePaths("node_modules", dependency)).AsPath());
                if (symlinks.HasDirectory(adjacent))
                    continue;
                if (!dependency.StartsWith("@types", StringComparison.Ordinal))
                {
                    var types = caseSensitivity.PathKey(metadata.PackageJsonDirectory.ResolveDirectory(TsPath.CombinePaths("node_modules", ModuleUtilities.GetTypesPackageName(dependency))).AsPath());
                    if (symlinks.HasDirectory(types))
                        continue;
                }

                var resolution = resolver.ResolvePackageDirectory(dependency, packageJsonFile, ResolutionMode.CommonJS, null);
                if (resolution.IsResolved() && resolution!.OriginalPath != default)
                    symlinks.ProcessResolution(TsPath.RootedDirectoryPathFromPath(resolution.OriginalPath.AsPath()).ResolveFile("package.json"), TsPath.RootedDirectoryPathFromPath(resolution.ResolvedFileName.AsPath()).ResolveFile("package.json"));
            }
        }

        return symlinks;
    });
    public SourceFile? GetLibFileFromReference(FileReference reference)
    {
        var(name, ok) = TypeScript.TsOptions.TsOptions.GetLibFileName(reference.FileName);
        if (!ok)
            return null;
        foreach (var(path, lib)in processedFiles.libFiles)
            if (lib.Name == name)
                return filesByPath.GetValueOrDefault(path);
        return null;
    }

    public void ExplainFiles(Stream writer, TypeScript.Locale.Locale locale, RootedDirectoryPath currentDirectory)
    {
        string Relative(RootedFilePath filename)
        {
            var(path, ok) = caseSensitivity.RelativePathFromDirectory(currentDirectory, filename);
            return ok ? path.AsString() : filename.AsString();
        }

        void WriteLine(string bytes)
        {
            writer.Write(GoString.Bytes(bytes));
            writer.WriteByte((byte)'\n');
        }

        int explained = 0;
        void Explain(RootedFilePath filename, PathKey path)
        {
            WriteLine(Relative(filename));
            foreach (var reason in processedFiles.fileIncludeData.fileIncludeReasons.GetValueOrDefault(path.AsString()) ?? [])
                WriteLine("   " + TypeScript.DiagnosticWriter.DiagnosticWriter.WrapASTDiagnostic(reason.ToDiagnostic(this, true, currentDirectory), true).Localize(locale));
            foreach (var diagnostic in includeProcessor.ExplainRedirectAndImpliedFormat(this, path, Relative) ?? [])
                WriteLine("   " + TypeScript.DiagnosticWriter.DiagnosticWriter.WrapASTDiagnostic(diagnostic, true).Localize(locale));
            explained++;
        }

        var redirects = processedFiles.redirectFilesByPath?.Values.ToList() ?? [];
        redirects.Sort((first, second) => first.index - second.index);
        int sourceIndex = 0;
        void Sources(int end)
        {
            while (explained < end)
            {
                var file = files[sourceIndex++];
                Explain(RootedFilePath.FromGoString(file.FileNameGoBytes()), PathKey.FromGoString(file.PathKey()));
            }
        }

        foreach (var redirect in redirects)
        {
            Sources(redirect.index);
            Explain(redirect.FileName(), redirect.PathKey());
        }

        Sources(files.Count + redirects.Count);
    }

    // program.go:1505,1514,1675. Fixed TS host counterpart: noEmit follows options,
    // no project references/content mapper or checking directives are admitted.
    // Project aggregates actual include-processor diagnostics separately; the
    // historical Collections host has none. Module checks use this same checker.
    internal static List<Diagnostic>? GetSemanticDiagnosticsWithChecker(SourceFile sourceFile, CompilerOptions options, TypeScript.Checking.Checker checker)
    {
        if (SkipTypeChecking(sourceFile, options))
            return null;
        if (sourceFile.ScriptKind != ScriptKind.TS || sourceFile.CommentDirectives is { Count: > 0 } || sourceFile.CheckJsDirective is not null)
            throw new TypeScript.Checking.UnsupportedSemanticFeatureException("Program.getSemanticDiagnosticsWithChecker: fixed TS host policy");
        var diags = sourceFile.BindDiagnostics()?.ToList();
        var checkedDiags = checker.GetDiagnostics(sourceFile);
        if (checkedDiags is { Count: > 0 })
        {
            diags ??= [];
            diags.AddRange(checkedDiags);
        }

        return SortAndDeduplicateDiagnostics(FilterNoEmitSemanticDiagnostics(diags, options));
    }

    // program.go:1495. Retain the original marker, rather than filter diagnostic codes.
    internal static List<Diagnostic>? FilterNoEmitSemanticDiagnostics(List<Diagnostic>? diagnostics, CompilerOptions options) => options.NoEmit == Tristate.True ? Core.Filter(diagnostics, d => !d.SkippedOnNoEmit()) : diagnostics;
    internal static List<Diagnostic>? SortAndDeduplicateDiagnostics(List<Diagnostic>? diagnostics)
    {
        if (diagnostics is null)
            return null;
        diagnostics = diagnostics.ToList();
        GoSlices.SortFunc(diagnostics, Ast.CompareDiagnostics);
        return compactAndMergeRelatedInfos(diagnostics);
    }

    // program.go:1683. The private clone's unused tail is removed in managed
    // storage, matching clear plus the shorter Go result slice.
    private static List<Diagnostic> compactAndMergeRelatedInfos(List<Diagnostic> diagnostics)
    {
        if (diagnostics.Count < 2)
            return diagnostics;
        int i = 0, j = 0;
        while (i < diagnostics.Count)
        {
            var diagnostic = diagnostics[i];
            int n = 1;
            while (i + n < diagnostics.Count && Ast.EqualDiagnosticsNoRelatedInfo(diagnostic, diagnostics[i + n]))
                n++;
            if (n > 1)
            {
                List<Diagnostic>? related = null;
                for (int k = 0; k < n; k++)
                    if (diagnostics[i + k].RelatedInformation()is { Count: > 0 } infos)
                    {
                        related ??= [];
                        related.AddRange(infos);
                    }

                if (related is not null)
                {
                    GoSlices.SortFunc(related, Ast.CompareDiagnostics);
                    int write = 0;
                    foreach (var value in related.ToArray())
                        if (write == 0 || !Ast.EqualDiagnostics(related[write - 1], value))
                            related[write++] = value;
                    related.RemoveRange(write, related.Count - write);
                    diagnostic = diagnostic.Clone().SetRelatedInfo(related);
                }
            }

            diagnostics[j++] = diagnostic;
            i += n;
        }

        diagnostics.RemoveRange(j, diagnostics.Count - j);
        return diagnostics;
    }

    internal static bool SkipTypeChecking(SourceFile sourceFile, CompilerOptions options) => options.SkipLibCheck == Tristate.True && sourceFile.IsDeclarationFile || !canIncludeLegacyBindAndCheckDiagnostics(sourceFile);
    private static bool canIncludeLegacyBindAndCheckDiagnostics(SourceFile sourceFile)
    {
        if (sourceFile.CheckJsDirective is { Enabled: false })
            return false;
        if (sourceFile.ScriptKind is ScriptKind.TS or ScriptKind.TSX)
            return true;
        throw new TypeScript.Checking.UnsupportedSemanticFeatureException("Program.canIncludeBindAndCheckDiagnostics: fixed TS environment");
    }
}

// program.go:68–89. Retains a computed value for reuse without retaining its factory.
internal sealed class ProgramLazyValue<T>
    where T : class
{
    private T? value;
    private readonly object gate = new();
    private bool once;
    private int initialized;
    internal T getValue(Func<T> compute)
    {
        lock (gate)
        {
            if (!once)
            {
                try
                {
                    if (value is null)
                        value = compute();
                    Volatile.Write(ref initialized, 1);
                }
                finally
                {
                    once = true;
                }
            }

            return value!;
        }
    }

    internal void tryReuse(ProgramLazyValue<T> from)
    {
        if (Volatile.Read(ref from.initialized) != 0)
        {
            value = from.value;
            Volatile.Write(ref initialized, 1);
        }
    }
}

internal sealed class PackageNamesInfo
{
    internal HashSet<string> resolved = new(StringComparer.Ordinal), unresolved = new(StringComparer.Ordinal), deepImportPackages = new(StringComparer.Ordinal);
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/module/resolver.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Collections;
using TypeScript.PackageJson;
using TypeScript.Scanner;
using TypeScript.Vfs;
using TypeScript.Vfs.VfsMatch;

namespace TypeScript.Modules;
public sealed class ResolverOptions
{
    public IResolutionHost Host = null!;
    public CompilerOptions CompilerOptions = null!;
    public RootedDirectoryPath TypingsLocation;
    public string ProjectName = "";
    public List<string>? ExtraExtensions;
    public InfoCache? PackageJsonCache;
}

public struct DiagAndArgs
{
    public Message Message;
    public Slice<object?> Args;
}

public sealed class ParsedPatterns
{
    internal HashSet<ByteStringKey>? matchableStringSet;
    internal Slice<Pattern> patterns;
}

public enum Ending
{
    Fixed,
    ExtensionChangeable,
    Changeable
}

public sealed class ResolvedEntrypoint
{
    public RootedFilePath OriginalFileName, ResolvedFileName;
    public PathKey ResolvedPath;
    public ModuleSpecifier ModuleSpecifier;
    public Ending Ending;
    public HashSet<string>? IncludeConditions, ExcludeConditions;
    public RootedFilePath SymlinkOrRealpath() => OriginalFileName != default ? OriginalFileName : ResolvedFileName;
}

public sealed partial class DefaultResolver : IResolver, ITextResolver
{
    internal ResolutionData resolutionData = null!;
    internal IFS fs = null!;
    internal RootedDirectoryPath baseDirectory;
    internal CompilerOptions compilerOptions => resolutionData.compilerOptions;
    internal RootedDirectoryPath typingsLocation => resolutionData.typingsLocation;
    internal string projectName => resolutionData.projectName;
    internal List<string>? extraExtensions => resolutionData.extraExtensions;
    internal InfoCache packageJsonInfoCache => resolutionData.packageJsonInfoCache;

    internal readonly ModuleResolutionCache moduleResolutionCache = new();
    internal readonly TypeRefDirectiveResolutionCache typeRefDirectiveResolutionCache = new();
    internal readonly ParsedPatternsCache parsedPatternsForPaths = new();
    internal DefaultResolver()
    {
    }

    internal DefaultResolver(ResolutionData data, IResolutionHost host)
    {
        fs = host.FS();
        baseDirectory = host.GetCurrentDirectory();
        if (baseDirectory == default)
            throw new InvalidOperationException("resolver must have a rooted base directory");
        if (data.packageJsonInfoCache.CaseSensitivity() != fs.CaseSensitivity())
            throw new InvalidOperationException("package JSON cache and resolver must use the same case sensitivity");
        resolutionData = data;
    }

    // Existing configuration API convenience: the same actual default resolver.
    public DefaultResolver(IFS fs, RootedDirectoryPath baseDirectory, CompilerOptions options) : this(ResolutionData.New(new ResolverOptions { Host = new FileSystemResolutionHost(fs, baseDirectory), CompilerOptions = options }), new FileSystemResolutionHost(fs, baseDirectory))
    {
    }

    public static DefaultResolver NewResolver(ResolverOptions options) => ResolutionData.New(options).NewResolver(options.Host);
    public static DefaultResolver NewResolverWithOptions(IResolutionHost host, CompilerOptions options, RootedDirectoryPath typingsLocation, string projectName, ResolverOptions resolverOptions)
    {
        // Original ResolverOptions is passed by value; preserve the caller's fields.
        return NewResolver(new ResolverOptions { Host = host, CompilerOptions = options, TypingsLocation = typingsLocation, ProjectName = projectName, ExtraExtensions = resolverOptions.ExtraExtensions, PackageJsonCache = resolverOptions.PackageJsonCache });
    }

    public RootedDirectoryPath BaseDirectory() => baseDirectory;
    public ResolutionData GetResolutionData() => resolutionData;
    public InfoCacheEntry? GetPackageScopeForPath(RootedDirectoryPath directory) => new resolutionState
    {
        resolver = this,
        compilerOptions = compilerOptions
    }.getPackageScopeForPath(directory);
    public void PackageJsonCacheEntries(Func<PathKey, InfoCacheEntry, bool> callback) => resolutionData.PackageJsonCacheEntries(callback);
    public static CompilerOptions GetCompilerOptionsWithRedirect(CompilerOptions options, IResolvedProjectReference? reference) => reference?.CompilerOptions() ?? options;
    private Tracer? newTraceBuilder() => compilerOptions.TraceResolution == Tristate.True ? new() : null;
    internal sealed class ResolvedData
    {
        internal RootedFilePath path;
        internal string extension = "";
        internal PackageId packageId;
        internal RootedFilePath originalPath;
        internal bool resolvedUsingTsExtension, resolvedUsingExtraExtensions;
    }

    private static bool isResolved(ResolvedData? value) => value is not null && value.path != default;
    private static bool shouldContinueSearching(ResolvedData? value) => value is null;
    private static ResolvedData? continueSearching() => null;
    private static ResolvedData unresolved() => new();
    internal readonly record struct resolutionCandidate(RootedPath path, RootedDirectoryPath directoryPath = default, bool directoryOnly = false)
    {
        internal string AsString() => directoryOnly ? TsPath.EnsureTrailingDirectorySeparator(path.AsString()) : path.AsString();
        public override string ToString() => AsString();
        internal RootedDirectoryPath AsDirectoryPath() => directoryPath != default ? directoryPath : TsPath.RootedDirectoryPathFromPath(path);
        internal RootedDirectoryPath Directory() => path.Directory();
        internal bool HasTrailingDirectorySeparator() => directoryOnly;
        internal FileNameStem RemoveFileExtension() => TsPath.RootedFilePathFromPath(path).RemoveFileExtension();
        internal FileNameStem RemoveExtension(string extension) => TsPath.RootedFilePathFromPath(path).RemoveExtension(extension);
        internal resolutionCandidate Resolve(string text) => resolutionCandidateFromDirectoryPath(TsPath.RootedDirectoryPathFromPath(path), text);
        internal bool HasDirectoryPrefix(RootedDirectoryPath directory)
        {
            var(relative, ok) = path.RelativeTo(directory);
            return ok && (relative != default || directoryOnly);
        }

        internal RelativePath RelativeToDirectory(RootedDirectoryPath directory)
        {
            var(relative, ok) = path.RelativeTo(directory);
            if (!ok)
                throw new InvalidOperationException("resolution candidate must be within prefix");
            return directoryOnly ? relative.WithTrailingDirectorySeparator() : relative;
        }

        internal (FileNameStem Stem, string Extension) SplitExtension(List<string>? extraExtensions)
        {
            var fileName = TsPath.RootedFilePathFromPath(path);
            var extension = fileName.Extension();
            var extensionless = RemoveFileExtension();
            if (extensionless.AsString() == path.AsString())
            {
                extension = fileName.LongestExtension(extraExtensions, CaseSensitivity.CaseSensitive);
                if (extension == "")
                    extension = path.AsString()[path.AsString().LastIndexOf('.')..];
                extensionless = RemoveExtension(extension);
            }

            return (extensionless, extension);
        }
    }

    internal static resolutionCandidate resolutionCandidateFromNormalized(string path)
    {
        bool directoryOnly = TsPath.HasTrailingDirectorySeparator(path);
        if (directoryOnly && path.Length > TsPath.GetRootLength(path))
            path = TsPath.RemoveTrailingDirectorySeparator(path);
        return new(TsPath.RootedPathFromNormalized(path), directoryOnly: directoryOnly);
    }

    internal static resolutionCandidate resolveResolutionCandidate(string path, params string[] paths) => resolutionCandidateFromNormalized(TsPath.ResolvePath(path, paths));
    internal static resolutionCandidate resolutionCandidateFromDirectory(RootedDirectoryPath directory) => new(directory.AsPath());
    internal static resolutionCandidate resolutionCandidateFromFileName(RootedFilePath fileName) => new(fileName.AsPath());
    internal static resolutionCandidate resolutionCandidateFromDirectoryPath(RootedDirectoryPath directory, string path) => resolutionCandidateFromEncodedPaths(directory, pathForDynamicResolution(directory, path, false), pathForDynamicResolution(directory, path, true), TsPath.HasTrailingDirectorySeparator(path));
    internal static resolutionCandidate resolutionCandidateFromDirectoryPath(RootedDirectoryPath directory, ByteStringValue path)
    {
        if (directory == default) throw new ArgumentException("cannot resolve from an empty directory name");
        if (path.Length == 0) return new(directory.AsPath());
        if (TsPath.IsEncodedDynamicFileName(directory.AsString()) && TsPath.GetEncodedRootLength(path) == 0)
        {
            var file = TsPath.ToRootedPath(TsPath.EncodeDynamicModuleSpecifier(path), directory);
            var folder = TsPath.ToRootedPath(TsPath.EncodeDynamicDirectorySpecifier(path), directory);
            if (TsPath.HasTrailingDirectorySeparator(path)) return new(folder, directoryOnly: true);
            return new(file, folder != file ? TsPath.RootedDirectoryPathFromPath(folder) : default);
        }
        var resolved = TsPath.ToRootedPath(path, directory);
        return new(resolved, directoryOnly: TsPath.HasTrailingDirectorySeparator(path));
    }
    internal static resolutionCandidate resolutionCandidateFromDynamicLogicalPath(RootedDirectoryPath directory, string path)
    {
        bool empty = path == "";
        bool directoryOnly = empty || TsPath.HasTrailingDirectorySeparator(path);
        if (path != "" && directoryOnly)
            path = TsPath.RemoveTrailingDirectorySeparator(path);
        return empty ? new(directory.AsPath(), directoryOnly: true) : resolutionCandidateFromEncodedPaths(directory, TsPath.EncodeDynamicRelativeURIPath(path), TsPath.EncodeDynamicRelativeURIDirectoryPath(path), directoryOnly);
    }

    internal static resolutionCandidate resolutionCandidateFromDiskLogicalPath(RootedDirectoryPath directory, string path, bool directoryOnly)
    {
        if (path != "" && directoryOnly)
            path = TsPath.RemoveTrailingDirectorySeparator(path);
        return path == "" ? new(directory.AsPath(), directoryOnly: directoryOnly) : new(directory.ResolveFileFromNormalizedRelative(path).AsPath(), directoryOnly: directoryOnly);
    }

    internal static resolutionCandidate resolutionCandidateFromEncodedPaths(RootedDirectoryPath directory, string filePath, string directoryPath, bool directoryOnly)
    {
        var resolvedDirectoryPath = directory.ResolveDirectory(directoryPath);
        if (directoryOnly)
            return new(resolvedDirectoryPath.AsPath(), directoryOnly: true);
        var path = directory.ResolveFile(filePath).AsPath();
        return new(path, resolvedDirectoryPath.AsPath() != path ? resolvedDirectoryPath : default);
    }

    internal static string pathForDynamicResolution(RootedDirectoryPath directory, string path, bool directoryOnly) => TsPath.IsEncodedDynamicFileName(directory.AsString()) && !TsPath.PathIsAbsolute(path) ? directoryOnly ? TsPath.EncodeDynamicDirectorySpecifier(path) : TsPath.EncodeDynamicModuleSpecifier(path) : path;

    internal static resolutionCandidate resolutionCandidateFromRelativePath(RootedDirectoryPath directory, RelativePath path)
    {
        bool directoryOnly = path == default || path.HasTrailingDirectorySeparator();
        if (path != default && directoryOnly)
            path = path.WithoutTrailingDirectorySeparator();
        return path == default ? new(directory.AsPath(), directoryOnly: true) : new(directory.ResolveRelativeFile(path).AsPath(), directoryOnly: directoryOnly);
    }

    internal sealed class Tracer
    {
        private Slice<DiagAndArgs> traces;
        internal void write(Message message, params object? []? args)
        {
            if (args is not null)
                for (int i = 0; i < args.Length; i++)
                    if (args[i] is ByteStringValue value) args[i] = value.Materialize();
            traces = traces.Append(new DiagAndArgs() { Message = message, Args = args is null || args.Length == 0 ? default : Slice<object?>.FromArray(args) });
        }
        internal Slice<DiagAndArgs> getTraces() => traces;
        internal void traceResolutionUsingProjectReference(IResolvedProjectReference? reference)
        {
            if (reference?.CompilerOptions()is not null)
                write(Diagnostics.Using_compiler_options_of_project_reference_redirect_0, reference.ConfigName());
        }

        internal void traceTypeReferenceDirectiveResult(ByteStringValue name, ResolvedTypeReferenceDirective result)
        {
            if (!result.IsResolved())
                write(Diagnostics.Type_reference_directive_0_was_not_resolved, name);
            else if (result.PackageId.Name != "")
                write(Diagnostics.Type_reference_directive_0_was_successfully_resolved_to_1_with_Package_ID_2_primary_Colon_3, name, result.ResolvedFileName, result.PackageId.ToString(), result.Primary);
            else
                write(Diagnostics.Type_reference_directive_0_was_successfully_resolved_to_1_primary_Colon_2, name, result.ResolvedFileName, result.Primary);
        }
    }

    public (ResolvedTypeReferenceDirective? Result, Slice<DiagAndArgs> Traces) ResolveTypeReferenceDirective(string name, RootedFilePath containingFile, ResolutionMode resolutionMode, IResolvedProjectReference? reference = null) => ResolveTypeReferenceDirective(ByteStringValue.FromString(name), containingFile, resolutionMode, reference);
    (ResolvedTypeReferenceDirective? Result, Slice<DiagAndArgs> Traces) ITextResolver.ResolveTypeReferenceDirective(ByteStringValue name, RootedFilePath containingFile, ResolutionMode resolutionMode, IResolvedProjectReference? reference) => ResolveTypeReferenceDirective(name, containingFile, resolutionMode, reference);
    internal (ResolvedTypeReferenceDirective? Result, Slice<DiagAndArgs> Traces) ResolveTypeReferenceDirective(ByteStringValue name, RootedFilePath containingFile, ResolutionMode resolutionMode, IResolvedProjectReference? reference = null)
    {
        var directory = containingFile.Directory();
        var trace = newTraceBuilder();
        bool inferred = containingFile.BaseName() == ModuleUtilities.InferredTypesContainingFile;
        var key = new TypeRefDirectiveResolutionCacheKey(directory, new ModeAwareCacheKey(name, resolutionMode), ResolutionData.GetRedirectConfigName(reference), inferred);
        if (trace is null)
        {
            var(cached, found) = typeRefDirectiveResolutionCache.Get(key);
            if (found)
                return (cached, default);
        }

        var options = GetCompilerOptionsWithRedirect(compilerOptions, reference);
        var(roots, fromConfig) = options.GetEffectiveTypeRoots(baseDirectory);
        trace?.write(Diagnostics.Resolving_type_reference_directive_0_containing_file_1_root_directory_2, name, containingFile.AsString(), string.Join(",", roots.Select(root => root.AsString())));
        trace?.traceResolutionUsingProjectReference(reference);
        var state = new resolutionState(name, directory, true, resolutionMode, options, reference, this, trace);
        var result = state.resolveTypeReferenceDirective(roots, fromConfig, inferred);
        trace?.traceTypeReferenceDirectiveResult(name, result);
        typeRefDirectiveResolutionCache.Set(key, result);
        return (result, trace?.getTraces() ?? default);
    }

    public (ResolvedModule? Result, Slice<DiagAndArgs> Traces, Exception? Error) ResolveModuleName(string moduleName, RootedFilePath containingFile, ResolutionMode resolutionMode, IResolvedProjectReference? redirectedReference = null) => ResolveModuleName(ByteStringValue.FromString(moduleName), containingFile, resolutionMode, redirectedReference);
    (ResolvedModule? Result, Slice<DiagAndArgs> Traces, Exception? Error) ITextResolver.ResolveModuleName(ByteStringValue moduleName, RootedFilePath containingFile, ResolutionMode resolutionMode, IResolvedProjectReference? redirectedReference) => ResolveModuleName(moduleName, containingFile, resolutionMode, redirectedReference);
    internal (ResolvedModule? Result, Slice<DiagAndArgs> Traces, Exception? Error) ResolveModuleName(ByteStringValue moduleName, RootedFilePath containingFile, ResolutionMode resolutionMode, IResolvedProjectReference? redirectedReference = null)
    {
        var(result, traces) = resolveModuleName(moduleName, containingFile.AsPath(), containingFile.Directory(), resolutionMode, redirectedReference);
        return (result, traces, null);
    }

    public (ResolvedModule? Result, Slice<DiagAndArgs> Traces, Exception? Error) ResolveModuleNameFromDirectory(string moduleName, RootedDirectoryPath containingDirectory, ResolutionMode resolutionMode) => ResolveModuleNameFromDirectory(ByteStringValue.FromString(moduleName), containingDirectory, resolutionMode);
    (ResolvedModule? Result, Slice<DiagAndArgs> Traces, Exception? Error) ITextResolver.ResolveModuleNameFromDirectory(ByteStringValue moduleName, RootedDirectoryPath containingDirectory, ResolutionMode resolutionMode) => ResolveModuleNameFromDirectory(moduleName, containingDirectory, resolutionMode);
    internal (ResolvedModule? Result, Slice<DiagAndArgs> Traces, Exception? Error) ResolveModuleNameFromDirectory(ByteStringValue moduleName, RootedDirectoryPath containingDirectory, ResolutionMode resolutionMode)
    {
        var(result, traces) = resolveModuleName(moduleName, containingDirectory.AsPath(), containingDirectory, resolutionMode, null);
        return (result, traces, null);
    }

    private (ResolvedModule Result, Slice<DiagAndArgs> Traces) resolveModuleName(ByteStringValue name, RootedPath file, RootedDirectoryPath directory, ResolutionMode mode, IResolvedProjectReference? reference)
    {
        var trace = newTraceBuilder();
        var key = new ModuleResolutionCacheKey(directory, new ModeAwareCacheKey(name, mode), ResolutionData.GetRedirectConfigName(reference));
        if (trace is null)
        {
            var(cached, found) = moduleResolutionCache.Get(key);
            if (found)
                return (cached!, default);
        }

        var options = GetCompilerOptionsWithRedirect(compilerOptions, reference);
        trace?.write(Diagnostics.Resolving_module_0_from_1, name, file.AsString());
        trace?.traceResolutionUsingProjectReference(reference);
        var kind = options.GetModuleResolutionKind();
        trace?.write(options.ModuleResolution != kind ? Diagnostics.Module_resolution_kind_is_not_specified_using_0 : Diagnostics.Explicitly_specified_module_resolution_kind_Colon_0, kind.String());
        ResolvedModule result;
        switch (kind)
        {
            case ModuleResolutionKind.Node16:
            case ModuleResolutionKind.NodeNext:
            case ModuleResolutionKind.Bundler:
                result = new resolutionState(name, directory, false, mode, options, reference, this, trace).resolveNodeLike();
                break;
            default:
                throw new InvalidOperationException("Unexpected moduleResolution: " + (int)kind);
        }

        if (trace is not null)
        {
            if (result.IsResolved())
            {
                if (result.PackageId.Name != "")
                    trace.write(Diagnostics.Module_name_0_was_successfully_resolved_to_1_with_Package_ID_2, name, result.ResolvedFileName, result.PackageId.ToString());
                else
                    trace.write(Diagnostics.Module_name_0_was_successfully_resolved_to_1, name, result.ResolvedFileName);
            }
            else
                trace.write(Diagnostics.Module_name_0_was_not_resolved, name);
        }

        var finalResult = tryResolveFromTypingsLocation(name, directory, result, trace);
        moduleResolutionCache.Set(key, finalResult);
        return (finalResult, trace?.getTraces() ?? default);
    }

    public ResolvedModule? ResolvePackageDirectory(string moduleName, RootedFilePath containingFile, ResolutionMode mode, IResolvedProjectReference? reference = null)
    {
        var options = GetCompilerOptionsWithRedirect(compilerOptions, reference);
        var state = new resolutionState(ByteStringValue.FromString(moduleName), containingFile.Directory(), false, mode, options, reference, this, null)
        {
            resolvePackageDirectoryOnly = true
        };
        var result = state.loadModuleFromNearestNodeModulesDirectory(false);
        return isResolved(result) ? state.createResolvedModuleHandlingSymlink(result) : null;
    }

    private ResolvedModule tryResolveFromTypingsLocation(ByteStringValue name, RootedDirectoryPath directory, ResolvedModule original, Tracer? trace)
    {
        if (typingsLocation == default || TsPath.IsExternalModuleNameRelative(name) || (original.ResolvedFileName != default && TsPath.ExtensionIsOneOf(original.Extension, TsPath.SupportedTSExtensionsWithJsonFlat)))
            return original;
        var state = new resolutionState(name, directory, false, ResolutionMode.None, compilerOptions, null, this, trace);
        trace?.write(Diagnostics.Auto_discovery_for_typings_is_enabled_in_project_0_Running_extra_resolution_pass_for_module_1_using_cache_location_2, projectName, name, typingsLocation.AsString());
        var found = state.loadModuleFromImmediateNodeModulesDirectory(Extensions.Declaration, typingsLocation, false);
        if (found is null)
            return original;
        var result = state.createResolvedModule(found, true);
        result.ResolutionDiagnostics = Core.Concatenate(original.ResolutionDiagnostics, result.ResolutionDiagnostics);
        return result;
    }

    private ResolvedModule resolveConfig(ByteStringValue name, RootedFilePath file)
    {
        var state = new resolutionState(name, file.Directory(), false, ResolutionMode.CommonJS, compilerOptions, null, this, null)
        {
            isConfigLookup = true,
            extensions = Extensions.Json
        };
        return state.resolveNodeLike();
    }

    private sealed class FileSystemResolutionHost(IFS fs, RootedDirectoryPath directory) : IResolutionHost
    {
        public IFS FS() => fs;
        public RootedDirectoryPath GetCurrentDirectory() => directory;
    }

    public static ResolvedModule ResolveConfig(string moduleName, RootedFilePath containingFile, IFS fs)
    {
        var resolver = NewResolver(new ResolverOptions { Host = new FileSystemResolutionHost(fs, containingFile.Directory()), CompilerOptions = new() { ModuleResolution = ModuleResolutionKind.NodeNext } });
        return resolver.resolveConfig(ByteStringValue.FromString(moduleName), containingFile);
    }

    public static Slice<string> GetConditions(CompilerOptions options, ResolutionMode mode)
    {
        var kind = options.GetModuleResolutionKind();
        if (mode == ResolutionMode.None && kind == ModuleResolutionKind.Bundler)
            mode = ResolutionMode.ESNext;
        var conditions = Slice<string>.Make(0, 3 + (options.CustomConditions?.Count ?? 0));
        conditions = conditions.Append(mode == ResolutionMode.ESNext ? "import" : "require");
        if (options.NoDtsResolution != Tristate.True)
            conditions = conditions.Append("types");
        if (kind != ModuleResolutionKind.Bundler)
            conditions = conditions.Append("node");
        if (options.CustomConditions is { Count: > 0 } custom)
        {
            // module/resolver.go:2166 uses core.Concatenate, not append:
            // the final nonempty combination owns a detached exact-length buffer.
            var combined = Slice<string>.Make(conditions.Length + custom.Count);
            conditions.AsSpan().CopyTo(combined.AsSpan());
            System.Runtime.InteropServices.CollectionsMarshal.AsSpan(custom).CopyTo(combined.AsSpan().Slice(conditions.Length));
            conditions = combined;
        }
        return conditions;
    }

    private static NodeResolutionFeatures getNodeResolutionFeatures(CompilerOptions options)
    {
        var features = options.GetModuleResolutionKind() switch
        {
            ModuleResolutionKind.Node16 => NodeResolutionFeatures.Node16Default,
            ModuleResolutionKind.NodeNext => NodeResolutionFeatures.NodeNextDefault,
            ModuleResolutionKind.Bundler => NodeResolutionFeatures.BundlerDefault,
            _ => NodeResolutionFeatures.None,
        };
        if (options.ResolvePackageJsonExports == Tristate.True)
            features |= NodeResolutionFeatures.Exports;
        else if (options.ResolvePackageJsonExports == Tristate.False)
            features &= ~NodeResolutionFeatures.Exports;
        if (options.ResolvePackageJsonImports == Tristate.True)
            features |= NodeResolutionFeatures.Imports;
        else if (options.ResolvePackageJsonImports == Tristate.False)
            features &= ~NodeResolutionFeatures.Imports;
        return features;
    }

    internal static int MoveToNextDirectorySeparatorIfAvailable(string path, int previousSeparatorIndex, bool isFolder)
    {
        int offset = previousSeparatorIndex + 1;
        int nextSeparatorIndex = offset <= path.Length ? path.AsSpan(offset).IndexOf('/') : -1;
        return nextSeparatorIndex == -1 ? isFolder ? path.Length : previousSeparatorIndex : nextSeparatorIndex + offset;
    }

    internal sealed class resolutionState
    {
        internal DefaultResolver resolver = null!;
        internal Tracer? tracer;
        internal ByteStringValue name;
        internal RootedDirectoryPath containingDirectory;
        internal bool containingDirectoryHasTrailingSeparator, isConfigLookup;
        internal NodeResolutionFeatures features;
        internal bool esmMode;
        internal Slice<string> conditions;
        internal Extensions extensions;
        internal CompilerOptions compilerOptions = null!;
        internal bool resolvePackageDirectoryOnly, candidateEndingIsFromConfig, resolvedPackageDirectory;
        internal Slice<Diagnostic> diagnostics;
        internal resolutionState()
        {
        }

        internal resolutionState(ByteStringValue name, RootedDirectoryPath directory, bool isTypeDirective, ResolutionMode mode, CompilerOptions options, IResolvedProjectReference? reference, DefaultResolver resolver, Tracer? trace)
        {
            this.name = name;
            containingDirectory = directory;
            compilerOptions = GetCompilerOptionsWithRedirect(options, reference);
            this.resolver = resolver;
            tracer = trace;
            extensions = isTypeDirective ? Extensions.Declaration : options.NoDtsResolution == Tristate.True ? Extensions.ImplementationFiles : Extensions.TypeScript | Extensions.JavaScript | Extensions.Declaration;
            if (!isTypeDirective && options.GetResolveJsonModule())
                extensions |= Extensions.Json;
            switch (options.GetModuleResolutionKind())
            {
                case ModuleResolutionKind.Node16:
                    features = NodeResolutionFeatures.Node16Default;
                    esmMode = mode == ResolutionMode.ESNext;
                    conditions = GetConditions(options, mode);
                    break;
                case ModuleResolutionKind.NodeNext:
                    features = NodeResolutionFeatures.NodeNextDefault;
                    esmMode = mode == ResolutionMode.ESNext;
                    conditions = GetConditions(options, mode);
                    break;
                case ModuleResolutionKind.Bundler:
                    features = getNodeResolutionFeatures(options);
                    conditions = GetConditions(options, mode);
                    break;
            }
        }

        internal string containingDirectoryPath() => containingDirectoryHasTrailingSeparator ? TsPath.EnsureTrailingDirectorySeparator(containingDirectory.AsString()) : containingDirectory.AsString();
        internal ResolvedTypeReferenceDirective resolveTypeReferenceDirective(List<RootedDirectoryPath>? roots, bool fromConfig, bool inferred)
        {
            if ((roots?.Count ?? 0) > 0)
            {
                tracer?.write(Diagnostics.Resolving_with_primary_search_path_0, string.Join(", ", roots!.Select(root => root.AsString())));
                foreach (var root in roots!)
                {
                    var candidate = getCandidateFromTypeRoot(root);
                    if (!resolver.fs.DirectoryExists(root))
                    {
                        tracer?.write(Diagnostics.Directory_0_does_not_exist_skipping_all_lookups_in_it, root);
                        continue;
                    }

                    if (fromConfig && !candidate.HasTrailingDirectorySeparator())
                    {
                        var found = loadModuleFromFile(Extensions.Declaration, candidate);
                        if (found is not null)
                        {
                            var package = ModuleUtilities.NodeModulePackageRootForFile(found.path);
                            if (package != default)
                                found.packageId = getPackageId(found.path, getPackageJsonInfo(packageDirectory(package))!);
                            return createResolvedTypeReferenceDirective(found, true);
                        }
                    }

                    var directoryFound = loadNodeModuleFromDirectory(Extensions.Declaration, candidate, true);
                    if (directoryFound is not null)
                        return createResolvedTypeReferenceDirective(directoryFound, true);
                }
            }
            else
                tracer?.write(Diagnostics.Root_directory_cannot_be_determined_skipping_primary_search_paths);
            ResolvedData? secondary = null;
            if (!fromConfig || !inferred)
            {
                tracer?.write(Diagnostics.Looking_up_in_node_modules_folder_initial_location_0, containingDirectoryPath());
                secondary = !TsPath.IsExternalModuleNameRelative(name) ? loadModuleFromNearestNodeModulesDirectory(false) : nodeLoadModuleByRelativeName(Extensions.Declaration, normalizePathForCJSResolution(containingDirectory, name), true);
            }
            else
                tracer?.write(Diagnostics.Resolving_type_reference_directive_for_program_that_specifies_custom_typeRoots_skipping_lookup_in_node_modules_folder);
            return createResolvedTypeReferenceDirective(secondary, false);
        }

        internal resolutionCandidate getCandidateFromTypeRoot(RootedDirectoryPath root)
        {
            ByteStringValue lookup = root.BaseName() == "@types" && root.AsPath().Directory().BaseName() == "node_modules" ? mangleScopedPackageName(name) : name;
            return resolutionCandidateFromDirectoryPath(root, lookup);
        }

        internal ByteStringValue mangleScopedPackageName(ByteStringValue name)
        {
            var mangled = ModuleUtilities.MangleScopedPackageName(name);
            if (!mangled.EqualsValue(name))
                tracer?.write(Diagnostics.Scoped_package_detected_looking_in_0, mangled);
            return mangled;
        }

        internal ResolvedData? resolveFromTypeRoot()
        {
            if (compilerOptions.TypeRoots is null)
                return null;
            foreach (var root in compilerOptions.TypeRoots)
            {
                var candidate = getCandidateFromTypeRoot(root);
                if (!resolver.fs.DirectoryExists(root))
                {
                    tracer?.write(Diagnostics.Directory_0_does_not_exist_skipping_all_lookups_in_it, root);
                    continue;
                }

                if (!candidate.HasTrailingDirectorySeparator())
                {
                    var found = loadModuleFromFile(Extensions.Declaration, candidate);
                    if (found is not null)
                    {
                        var package = ModuleUtilities.NodeModulePackageRootForFile(found.path);
                        if (package != default)
                            found.packageId = getPackageId(found.path, getPackageJsonInfo(packageDirectory(package))!);
                        return found;
                    }
                }

                var fromDirectory = loadNodeModuleFromDirectory(Extensions.Declaration, candidate, true);
                if (fromDirectory is not null)
                    return fromDirectory;
            }

            return null;
        }

        internal InfoCacheEntry? getPackageScopeForPath(RootedDirectoryPath directory) => PackageDirectories.ForEachAncestorDirectoryStoppingAtGlobalCache(resolver.typingsLocation, resolver.packageJsonInfoCache.PackageDirectory(directory), candidate =>
        {
            var info = getPackageJsonInfo(candidate);
            return (info, info is not null);
        });
        internal ResolvedModule resolveNodeLike()
        {
            if (tracer is not null)
                tracer.write(Diagnostics.Resolving_in_0_mode_with_conditions_1, esmMode ? "ESM" : "CJS", string.Join(", ", Core.Map(conditions, static c => "'" + c + "'") ?? []));
            var result = resolveNodeLikeWorker();
            if (resolvedPackageDirectory && !isConfigLookup && (features & NodeResolutionFeatures.Exports) != 0 && (extensions & (Extensions.TypeScript | Extensions.Declaration)) != 0 && !TsPath.IsExternalModuleNameRelative(name) && result.IsResolved() && result.IsExternalLibraryImport && !extensionIsOk(Extensions.TypeScript | Extensions.Declaration, result.Extension) && GoSlices.Contains(conditions, "import"))
            {
                tracer?.write(Diagnostics.Resolution_of_non_relative_name_failed_trying_with_modern_Node_resolution_features_disabled_to_see_if_npm_library_needs_configuration_update);
                features &= ~NodeResolutionFeatures.Exports;
                extensions &= Extensions.TypeScript | Extensions.Declaration;
                int count = diagnostics.Length;
                var alternative = resolveNodeLikeWorker();
                if (alternative.IsResolved() && alternative.IsExternalLibraryImport)
                    result.AlternateResult = alternative.ResolvedFileName;
                diagnostics = diagnostics.Slice2(0, count);
            }

            return result;
        }

        internal ResolvedModule resolveNodeLikeWorker()
        {
            var optional = tryLoadModuleUsingOptionalResolutionSettings();
            if (optional is not null)
                return createResolvedModuleHandlingSymlink(optional);
            if (!TsPath.IsExternalModuleNameRelative(name))
            {
                if ((features & NodeResolutionFeatures.Imports) != 0 && name.StartsWith("#"u8))
                {
                    var imported = loadModuleFromImports();
                    if (imported is not null)
                        return createResolvedModuleHandlingSymlink(imported);
                }

                if ((features & NodeResolutionFeatures.SelfName) != 0)
                {
                    var self = loadModuleFromSelfNameReference();
                    if (self is not null)
                        return createResolvedModuleHandlingSymlink(self);
                }

                if (name.Contains(':'))
                {
                    tracer?.write(Diagnostics.Skipping_module_0_that_looks_like_an_absolute_URI_target_file_types_Colon_1, name, extensions.String());
                    return createResolvedModule(null, false);
                }

                tracer?.write(Diagnostics.Loading_module_0_from_node_modules_folder_target_file_types_Colon_1, name, extensions.String());
                var nearest = loadModuleFromNearestNodeModulesDirectory(false);
                if (nearest is not null)
                    return createResolvedModuleHandlingSymlink(nearest);
                if ((extensions & Extensions.Declaration) != 0)
                {
                    var root = resolveFromTypeRoot();
                    if (root is not null)
                        return createResolvedModuleHandlingSymlink(root);
                }
            }
            else
            {
                var found = nodeLoadModuleByRelativeName(extensions, normalizePathForCJSResolution(containingDirectory, name), true);
                return createResolvedModule(found, found is not null && found.path.ContainsLowercaseDirectorySequence("/node_modules/"));
            }

            return createResolvedModule(null, false);
        }

        internal ResolvedData? loadModuleFromSelfNameReference()
        {
            var scope = getPackageScopeForPath(containingDirectory);
            if (!(scope?.Exists() ?? false) || scope!.Contents!.Exports.IsFalsy())
                return continueSearching();
            var(packageName, ok) = scope.Contents.Fields.Name.GetValue();
            if (!ok)
                return continueSearching();
            var parts = TsPath.GetPathComponents(name);
            var names = TsPath.GetPathComponents(ByteStringValue.FromString(packageName));
            if (parts.Length < names.Length) return continueSearching();
            for (int i = 0; i < names.Length; i++)
                if (!names[i].EqualsValue(parts[i])) return continueSearching();
            var trailing = parts.Slice2(names.Length, parts.Length);
            var subpath = TsPath.CombinePaths(ByteStringValue.FromString("."), trailing);
            if (compilerOptions.GetAllowJS() && !containingDirectory.ContainsLowercaseDirectorySequence("/node_modules/"))
                return loadModuleFromExports(scope, extensions, subpath);
            var priority = extensions & (Extensions.TypeScript | Extensions.Declaration);
            var secondary = extensions & ~(Extensions.TypeScript | Extensions.Declaration);
            return loadModuleFromExports(scope, priority, subpath) ?? loadModuleFromExports(scope, secondary, subpath);
        }

        internal ResolvedData? loadModuleFromImports()
        {
            if (name.EqualsBytes("#"u8) || (name.StartsWith("#/"u8) && (features & NodeResolutionFeatures.ImportsPatternRoot) == 0))
            {
                tracer?.write(Diagnostics.Invalid_import_specifier_0_has_no_possible_resolutions, name);
                return continueSearching();
            }

            var scope = getPackageScopeForPath(containingDirectory);
            if (!(scope?.Exists() ?? false))
            {
                tracer?.write(Diagnostics.Directory_0_has_no_containing_package_json_scope_Imports_will_not_resolve, containingDirectory.AsString());
                return continueSearching();
            }

            if (scope!.Contents!.Imports.Type != JSONValueType.Object)
            {
                tracer?.write(Diagnostics.X_package_json_scope_0_has_no_imports_defined, scope.PackageDirectory);
                return continueSearching();
            }

            var found = loadModuleFromExportsOrImports(extensions, name, scope.Contents.Imports.AsObject(), scope, true);
            if (found is not null)
                return found;
            tracer?.write(Diagnostics.Import_specifier_0_does_not_exist_in_package_json_scope_at_path_1, name, scope.PackageDirectory);
            return continueSearching();
        }

        internal ResolvedData? loadModuleFromExports(InfoCacheEntry packageInfo, Extensions ext, ByteStringValue subpath)
        {
            if (!packageInfo.Exists() || packageInfo.Contents!.Exports.IsFalsy())
                return continueSearching();
            var exports = packageInfo.Contents.Exports;
            if (subpath.EqualsBytes("."u8))
            {
                ExportsOrImports main = default;
                switch (exports.Type)
                {
                    case JSONValueType.String:
                    case JSONValueType.Array:
                        main = exports;
                        break;
                    case JSONValueType.Object:
                        if (exports.IsConditions())
                            main = exports;
                        else if (exports.AsObject().Get(".")is var (dot, found) && found)
                            main = dot;
                        break;
                }

                if (main.Type != JSONValueType.NotPresent)
                    return loadModuleFromTargetExportOrImport(ext, subpath, packageInfo, false, main, default, false, ByteStringValue.FromString("."));
            }
            else if (exports.Type == JSONValueType.Object && exports.IsSubpaths())
            {
                var found = loadModuleFromExportsOrImports(ext, subpath, exports.AsObject(), packageInfo, false);
                if (found is not null)
                    return found;
            }

            tracer?.write(Diagnostics.Export_specifier_0_does_not_exist_in_package_json_scope_at_path_1, subpath, packageInfo.PackageDirectory);
            return continueSearching();
        }

        internal ResolvedData? loadModuleFromExportsOrImports(Extensions ext, ByteStringValue moduleName, OrderedMap<string, ExportsOrImports> table, InfoCacheEntry scope, bool isImports)
        {
            if (!moduleName.EndsWith("/"u8) && !moduleName.Contains('*'))
            {
                var(target, found) = table.Get(moduleName);
                if (found)
                    return loadModuleFromTargetExportOrImport(ext, moduleName, scope, isImports, target, default, false, moduleName);
            }

            var expanding = table.Keys().Where(key => key.Count(ch => ch == '*') == 1 || key.EndsWith('/')).ToList();
            // Go SortFunc is not stable; preserve its comparator rather than invent priority.
            expanding.Sort(ModuleUtilities.ComparePatternKeys);
            foreach (var candidate in expanding)
            {
                var(target, _) = table.Get(candidate);
                if ((features & NodeResolutionFeatures.ExportsPatternTrailers) != 0 && matchesPatternWithTrailer(candidate, moduleName))
                {
                    int star = candidate.IndexOf('*');
                    var subpath = moduleName.Slice(star, moduleName.Length - (candidate.Length - 1 - star));
                    return loadModuleFromTargetExportOrImport(ext, moduleName, scope, isImports, target, subpath, true, ByteStringValue.FromString(candidate));
                }

                if (candidate.EndsWith('*') && moduleName.StartsWith(ByteStringValue.FromString(candidate).Slice(0, candidate.Length - 1)))
                    return loadModuleFromTargetExportOrImport(ext, moduleName, scope, isImports, target, moduleName.Slice(candidate.Length - 1), true, ByteStringValue.FromString(candidate));
                if (moduleName.StartsWith(ByteStringValue.FromString(candidate)))
                    return loadModuleFromTargetExportOrImport(ext, moduleName, scope, isImports, target, moduleName.Slice(candidate.Length), false, ByteStringValue.FromString(candidate));
            }

            return continueSearching();
        }

        internal ResolvedData? loadModuleFromTargetExportOrImport(Extensions ext, ByteStringValue moduleName, InfoCacheEntry scope, bool isImports, ExportsOrImports target, ByteStringValue subpath, bool isPattern, ByteStringValue key)
        {
            switch (target.Type)
            {
                case JSONValueType.String:
                    string targetString = target.Value as string ?? "";
                    if (!isPattern && subpath.Length > 0 && !targetString.EndsWith('/'))
                    {
                        tracer?.write(Diagnostics.X_package_json_scope_0_has_invalid_type_for_target_of_specifier_1, scope.PackageDirectory, moduleName);
                        return continueSearching();
                    }

                    if (!targetString.StartsWith("./", StringComparison.Ordinal))
                    {
                        if (isImports && !targetString.StartsWith("../", StringComparison.Ordinal) && !targetString.StartsWith('/') && !TsPath.IsRootedDiskPath(targetString))
                        {
                            var lookup = substituteTarget(targetString, subpath, isPattern);
                            tracer?.write(Diagnostics.Using_0_subpath_1_with_target_2, "imports", key, lookup);
                            tracer?.write(Diagnostics.Resolving_module_0_from_1, lookup, TsPath.EnsureTrailingDirectorySeparator(scope.PackageDirectory.AsDirectoryPath().AsString()));
                            var previous = (name, containingDirectory, containingDirectoryHasTrailingSeparator);
                            name = lookup;
                            containingDirectory = scope.PackageDirectory.AsDirectoryPath();
                            containingDirectoryHasTrailingSeparator = true;
                            try
                            {
                                var found = resolveNodeLike();
                                return found.IsResolved() ? new ResolvedData
                                {
                                    path = found.ResolvedFileName,
                                    extension = found.Extension,
                                    packageId = found.PackageId,
                                    originalPath = found.OriginalPath,
                                    resolvedUsingTsExtension = found.ResolvedUsingTsExtension
                                }

                                : continueSearching();
                            }
                            finally
                            {
                                (name, containingDirectory, containingDirectoryHasTrailingSeparator) = previous;
                            }
                        }

                        tracer?.write(Diagnostics.X_package_json_scope_0_has_invalid_type_for_target_of_specifier_1, scope.PackageDirectory, moduleName);
                        return continueSearching();
                    }

                    var parts = TsPath.GetPathComponents(targetString);
                    if (TsPath.PathIsRelative(targetString))
                        parts = parts.Skip(1).ToList();
                    var afterFirst = parts.Skip(1);
                    var subpathParts = TsPath.GetPathComponents(subpath);
                    if (afterFirst.Any(part => part is ".." or "." or "node_modules") || Core.Some(subpathParts, static part => part.EqualsBytes(".."u8) || part.EqualsBytes("."u8) || part.EqualsBytes("node_modules"u8)))
                    {
                        tracer?.write(Diagnostics.X_package_json_scope_0_has_invalid_type_for_target_of_specifier_1, scope.PackageDirectory, moduleName);
                        return continueSearching();
                    }

                    var targetPath = substituteTarget(targetString, subpath, isPattern);
                    tracer?.write(Diagnostics.Using_0_subpath_1_with_target_2, isImports ? "imports" : "exports", key, targetPath);
                    var final = resolutionCandidateFromDirectoryPath(scope.PackageDirectory.AsDirectoryPath(), targetPath);
                    var input = tryLoadInputFileForPath(final, subpath, scope.PackageDirectory.ResolveFile("package.json"), isImports);
                    if (input is not null)
                    {
                        input.packageId = getPackageId(input.path, scope);
                        return input;
                    }

                    var fileResult = loadFileNameFromPackageJSONField(ext, final, targetString);
                    if (fileResult is not null)
                    {
                        fileResult.packageId = getPackageId(fileResult.path, scope);
                        return fileResult;
                    }

                    return continueSearching();
                case JSONValueType.Object:
                    tracer?.write(Diagnostics.Entering_conditional_exports);
                    foreach (var condition in target.AsObject().Keys())
                    {
                        if (conditionMatches(condition))
                        {
                            tracer?.write(Diagnostics.Matched_0_condition_1, isImports ? "imports" : "exports", condition);
                            var(subTarget, _) = target.AsObject().Get(condition);
                            var result = loadModuleFromTargetExportOrImport(ext, moduleName, scope, isImports, subTarget, subpath, isPattern, key);
                            if (result is not null)
                            {
                                if (isResolved(result))
                                    tracer?.write(Diagnostics.Resolved_under_condition_0, condition);
                                tracer?.write(Diagnostics.Exiting_conditional_exports);
                                return result;
                            }

                            tracer?.write(Diagnostics.Failed_to_resolve_under_condition_0, condition);
                        }
                        else
                            tracer?.write(Diagnostics.Saw_non_matching_condition_0, condition);
                    }

                    tracer?.write(Diagnostics.Exiting_conditional_exports);
                    return continueSearching();
                case JSONValueType.Array:
                    if ((target.AsArray()?.Count ?? 0) == 0)
                    {
                        tracer?.write(Diagnostics.X_package_json_scope_0_has_invalid_type_for_target_of_specifier_1, scope.PackageDirectory, moduleName);
                        return continueSearching();
                    }

                    foreach (var element in target.AsArray()!)
                    {
                        var result = loadModuleFromTargetExportOrImport(ext, moduleName, scope, isImports, element, subpath, isPattern, key);
                        if (result is not null)
                            return result;
                    }

                    break;
                case JSONValueType.Null:
                    tracer?.write(Diagnostics.X_package_json_scope_0_explicitly_maps_specifier_1_to_null, scope.PackageDirectory, moduleName);
                    return unresolved();
            }

            tracer?.write(Diagnostics.X_package_json_scope_0_has_invalid_type_for_target_of_specifier_1, scope.PackageDirectory, moduleName);
            return continueSearching();
        }

        internal ResolvedData? tryLoadInputFileForPath(resolutionCandidate final, ByteStringValue entry, RootedFilePath packagePath, bool isImports)
        {
            if (final.directoryOnly)
                return continueSearching();
            var file = TsPath.RootedFilePathFromPath(final.path);
            var sensitivity = resolver.fs.CaseSensitivity();
            if (!isConfigLookup && (compilerOptions.DeclarationDir != default || compilerOptions.OutDir != default) && !file.ContainsLowercaseDirectorySequence("/node_modules/") && (compilerOptions.ConfigFilePath == default || sensitivity.ContainsFilePath(packagePath.Directory(), compilerOptions.ConfigFilePath)))
            {
                RootedDirectoryPath root;
                if (compilerOptions.RootDir != default)
                    root = compilerOptions.RootDir;
                else if (compilerOptions.ConfigFilePath != default)
                    root = compilerOptions.ConfigFilePath.Directory();
                else
                {
                    diagnostics = diagnostics.Append(Ast.NewDiagnostic(null, new TextRange(), isImports ? Diagnostics.The_project_root_is_ambiguous_but_is_required_to_resolve_import_map_entry_0_in_file_1_Supply_the_rootDir_compiler_option_to_disambiguate : Diagnostics.The_project_root_is_ambiguous_but_is_required_to_resolve_export_map_entry_0_in_file_1_Supply_the_rootDir_compiler_option_to_disambiguate, entry.Length == 0 ? "." : entry.Materialize(), packagePath.AsString()));
                    return unresolved();
                }

                foreach (var directory in getOutputDirectories())
                {
                    var(fragment, ok) = sensitivity.RelativeFilePathFromDirectory(directory, file);
                    if (!ok)
                        continue;
                    var inputBase = root.ResolveRelativeFile(fragment);
                    string[] outputExtensions = [".mjs", ".cjs", ".js", ".json", ".d.mts", ".d.cts", ".d.ts"];
                    foreach (var extension in outputExtensions)
                    {
                        if (!inputBase.ExtensionIs(extension))
                            continue;
                        foreach (var inputExtension in inputBase.PossibleOriginalInputExtensions())
                        {
                            if (!extensionIsOk(extensions, inputExtension))
                                continue;
                            var input = inputBase.ChangeExtension(inputExtension);
                            if (!resolver.fs.FileExists(input))
                                continue;
                            var found = loadFileNameFromPackageJSONField(extensions, resolutionCandidateFromFileName(input), "");
                            if (found is not null)
                                return found;
                        }
                    }
                }
            }

            return continueSearching();
        }

        internal Slice<RootedDirectoryPath> getOutputDirectories()
        {
            Slice<RootedDirectoryPath> directories = default;
            if (compilerOptions.DeclarationDir != default)
                directories = directories.Append(compilerOptions.DeclarationDir);
            if (compilerOptions.OutDir != default && compilerOptions.OutDir != compilerOptions.DeclarationDir)
                directories = directories.Append(compilerOptions.OutDir);
            return directories;
        }

        internal ResolvedData? loadModuleFromNearestNodeModulesDirectory(bool typesScopeOnly)
        {
            var mode = esmMode || conditionMatches("import") ? ResolutionMode.ESNext : ResolutionMode.CommonJS;
            var priority = extensions & (Extensions.TypeScript | Extensions.Declaration);
            var secondary = extensions & ~(Extensions.TypeScript | Extensions.Declaration);
            if (priority != 0)
            {
                tracer?.write(Diagnostics.Searching_all_ancestor_node_modules_directories_for_preferred_extensions_Colon_0, priority.String());
                var result = loadModuleFromNearestNodeModulesDirectoryWorker(priority, mode, typesScopeOnly);
                if (result is not null)
                    return result;
            }

            if (secondary != 0 && !typesScopeOnly)
            {
                tracer?.write(Diagnostics.Searching_all_ancestor_node_modules_directories_for_fallback_extensions_Colon_0, secondary.String());
                return loadModuleFromNearestNodeModulesDirectoryWorker(secondary, mode, typesScopeOnly);
            }

            return continueSearching();
        }

        internal ResolvedData? loadModuleFromNearestNodeModulesDirectoryWorker(Extensions ext, ResolutionMode mode, bool typesScopeOnly)
        {
            var(result, _) = containingDirectory.ForEachAncestorDirectory(directory =>
            {
                if (directory.AsPath().BaseName() != "node_modules")
                {
                    var found = loadModuleFromImmediateNodeModulesDirectory(ext, directory, typesScopeOnly);
                    return (found, found is not null);
                }

                return (continueSearching(), false);
            });
            return result;
        }

        internal ResolvedData? loadModuleFromImmediateNodeModulesDirectory(Extensions ext, RootedDirectoryPath directory, bool typesScopeOnly)
        {
            var modules = directory.ResolveDirectory("node_modules");
            if (!resolver.fs.DirectoryExists(modules))
            {
                tracer?.write(Diagnostics.Directory_0_does_not_exist_skipping_all_lookups_in_it, modules);
                return continueSearching();
            }

            if (!typesScopeOnly)
            {
                var found = loadModuleFromSpecificNodeModulesDirectory(ext, name, modules);
                if (found is not null)
                    return found;
            }

            if ((ext & Extensions.Declaration) != 0)
            {
                var types = modules.ResolveDirectory("@types");
                if (!resolver.fs.DirectoryExists(types))
                {
                    tracer?.write(Diagnostics.Directory_0_does_not_exist_skipping_all_lookups_in_it, types);
                    return continueSearching();
                }

                return loadModuleFromSpecificNodeModulesDirectory(Extensions.Declaration, mangleScopedPackageName(name), types);
            }

            return continueSearching();
        }

        internal ResolvedData? loadModuleFromSpecificNodeModulesDirectory(Extensions ext, ByteStringValue moduleName, RootedDirectoryPath nodeModulesDirectory)
        {
            var candidate = resolutionCandidateFromDirectoryPath(nodeModulesDirectory, moduleName);
            var candidateDirectory = candidate.AsDirectoryPath();
            var(packageName, rest) = ModuleUtilities.ParsePackageName(moduleName);
            var directory = packageName.Length == 0 ? candidateDirectory : resolutionCandidateFromDirectoryPath(nodeModulesDirectory, packageName).AsDirectoryPath();
            if (resolvePackageDirectoryOnly)
                return resolver.fs.DirectoryExists(directory) ? new ResolvedData
                {
                    path = TsPath.RootedFilePathFromPath(directory.AsPath())
                }

                : continueSearching();
            InfoCacheEntry? rootInfo = null;
            var info = getPackageJsonInfo(packageDirectory(candidateDirectory));
            if (rest.Length != 0 && (info?.Exists() ?? false))
            {
                if ((features & NodeResolutionFeatures.Exports) != 0)
                    rootInfo = getPackageJsonInfo(packageDirectory(directory));
                if (!(rootInfo?.Exists() ?? false) || rootInfo!.Contents!.Exports.Type == JSONValueType.NotPresent)
                {
                    var file = loadModuleFromFile(ext, candidate);
                    if (file is not null)
                        return file;
                    var fromDirectory = loadNodeModuleFromDirectoryWorker(ext, candidateDirectory, info);
                    if (fromDirectory is not null)
                    {
                        fromDirectory.packageId = getPackageId(fromDirectory.path, info);
                        return fromDirectory;
                    }
                }
            }

            ResolvedData? Loader(Extensions extensions, resolutionCandidate candidate)
            {
                if (rest.Length != 0 || !esmMode)
                {
                    var file = loadModuleFromFile(extensions, candidate);
                    if (file is not null)
                    {
                        file.packageId = getPackageId(file.path, info);
                        return file;
                    }
                }

                var fromDirectory = loadNodeModuleFromDirectoryWorker(extensions, candidate.AsDirectoryPath(), info);
                if (fromDirectory is not null)
                {
                    fromDirectory.packageId = getPackageId(fromDirectory.path, info);
                    return fromDirectory;
                }

                if (rest.Length == 0 && (info?.Exists() ?? false) && (info!.Contents!.Exports.Type is JSONValueType.NotPresent or JSONValueType.Null) && esmMode)
                {
                    var index = loadModuleFromFile(extensions, resolutionCandidateFromDirectoryPath(candidate.AsDirectoryPath(), "index.js"));
                    if (index is not null)
                    {
                        index.packageId = getPackageId(index.path, info);
                        return index;
                    }
                }

                return continueSearching();
            }

            if (rest.Length != 0)
                info = rootInfo ?? getPackageJsonInfo(packageDirectory(directory));
            if (info is not null)
            {
                resolvedPackageDirectory = true;
                if ((features & NodeResolutionFeatures.Exports) != 0 && info.Exists() && !info.Contents!.Exports.IsFalsy())
                    return loadModuleFromExports(info, ext, TsPath.CombinePaths(ByteStringValue.FromString("."), rest));
                if (rest.Length != 0 && info.Exists())
                {
                    var version = info.Contents!.GetVersionPaths(getTraceFunc());
                    if (version.Exists())
                    {
                        tracer?.write(Diagnostics.X_package_json_has_a_typesVersions_entry_0_that_matches_compiler_version_1_looking_for_a_pattern_to_match_module_name_2, version.Version, Core.Version(), rest);
                        var patterns = TryParsePatterns(version.GetPaths());
                        var fromPaths = tryLoadModuleUsingPaths(ext, rest, directory, version.GetPaths(), patterns, Loader);
                        if (fromPaths is not null)
                            return fromPaths;
                    }
                }
            }

            return Loader(ext, candidate);
        }

        internal ResolvedModule createResolvedModuleHandlingSymlink(ResolvedData? found)
        {
            bool external = found is not null && found.path.ContainsLowercaseDirectorySequence("/node_modules/");
            if (compilerOptions.PreserveSymlinks != Tristate.True && external && found!.originalPath == default && !TsPath.IsExternalModuleNameRelative(name))
            {
                var(original, real) = getOriginalAndResolvedFileName(found.path);
                if (original != default)
                {
                    found.path = real;
                    found.originalPath = original;
                }
            }

            return createResolvedModule(found, external);
        }

        internal ResolvedModule createResolvedModule(ResolvedData? found, bool external)
        {
            var result = new ResolvedModule
            {
                ResolutionDiagnostics = diagnostics
            };
            if (found is not null)
            {
                if (isResolved(found))
                {
                    result.ResolvedFileName = found.path;
                    result.ResolvedPath = resolver.fs.CaseSensitivity().PathKey(found.path.AsPath());
                    if (found.originalPath != default)
                        result.OriginalPath = found.originalPath;
                }

                result.IsExternalLibraryImport = external;
                result.ResolvedUsingTsExtension = found.resolvedUsingTsExtension;
                result.ResolvedUsingExtraExtensions = found.resolvedUsingExtraExtensions;
                result.Extension = found.extension;
                result.PackageId = found.packageId;
            }

            return result;
        }

        internal ResolvedTypeReferenceDirective createResolvedTypeReferenceDirective(ResolvedData? found, bool primary)
        {
            var result = new ResolvedTypeReferenceDirective
            {
                ResolutionDiagnostics = diagnostics
            };
            if (isResolved(found))
            {
                if (!TsPath.ExtensionIsTs(found!.extension))
                    throw new InvalidOperationException("expected a TypeScript file extension");
                result.ResolvedFileName = found.path;
                result.Primary = primary;
                result.PackageId = found.packageId;
                result.IsExternalLibraryImport = found.path.ContainsLowercaseDirectorySequence("/node_modules/");
                if (compilerOptions.PreserveSymlinks != Tristate.True)
                {
                    var(original, real) = getOriginalAndResolvedFileName(found.path);
                    if (original != default)
                    {
                        result.ResolvedFileName = real;
                        result.OriginalPath = original;
                    }
                }

                result.ResolvedPath = resolver.fs.CaseSensitivity().PathKey(result.ResolvedFileName.AsPath());
            }

            return result;
        }

        internal (RootedFilePath Original, RootedFilePath Resolved) getOriginalAndResolvedFileName(RootedFilePath file)
        {
            var real = realPath(file);
            return resolver.fs.CaseSensitivity().CompareFilePaths(file, real) == 0 ? (default, file) : (file, real);
        }

        internal ResolvedData? tryLoadModuleUsingOptionalResolutionSettings()
        {
            var result = tryLoadModuleUsingPathsIfEligible();
            if (result != null)
                return result;
            return !TsPath.IsExternalModuleNameRelative(name) ? null : tryLoadModuleUsingRootDirs();
        }

        internal ParsedPatterns getParsedPatternsForPaths() => resolver.getParsedPatternsForPaths(compilerOptions);
        internal ResolvedData? tryLoadModuleUsingPathsIfEligible()
        {
            if ((compilerOptions.Paths?.Size() ?? 0) == 0 || TsPath.PathIsRelative(name))
                return null;
            tracer?.write(Diagnostics.X_paths_option_is_specified_looking_for_a_pattern_to_match_module_name_0, name);
            return tryLoadModuleUsingPaths(extensions, name, compilerOptions.GetPathsBasePath(resolver.baseDirectory), compilerOptions.Paths, getParsedPatternsForPaths(), (ext, candidate) => nodeLoadModuleByRelativeName(ext, candidate, true));
        }

        internal ResolvedData? tryLoadModuleUsingPaths(Extensions ext, ByteStringValue moduleName, RootedDirectoryPath containingDirectory, OrderedMap<string, List<string>?>? paths, ParsedPatterns patterns, Func<Extensions, resolutionCandidate, ResolvedData?> loader)
        {
            var pattern = MatchPatternOrExact(patterns, moduleName);
            if (!pattern.IsValid())
                return null;
            var star = pattern.MatchedText(moduleName);
            tracer?.write(Diagnostics.Module_name_0_matched_pattern_1, moduleName, pattern.Text);
            foreach (var substitution in (paths is null ? null : paths.Get(pattern.TextValue).Value) ?? [])
            {
                var index = substitution.IndexOf('*');
                var path = index < 0 ? ByteStringValue.FromString(substitution) : substituteTarget(substitution, star, true, replaceAll: false);
                var candidate = resolutionCandidateFromDirectoryPath(containingDirectory, path);
                tracer?.write(Diagnostics.Trying_substitution_0_candidate_module_location_Colon_1, substitution, path);
                var extension = TsPath.TryGetExtensionFromPath(substitution);
                if (extension != "")
                {
                    var(file, ok) = tryFile(candidate);
                    if (ok)
                        return new ResolvedData
                        {
                            path = file,
                            extension = extension
                        };
                }

                var previous = candidateEndingIsFromConfig;
                if (extension != "")
                    candidateEndingIsFromConfig = true;
                ResolvedData? result;
                try
                {
                    result = loader(ext, candidate);
                }
                finally
                {
                    candidateEndingIsFromConfig = previous;
                }

                if (result != null)
                    return result;
            }

            return null;
        }

        internal ResolvedData? tryLoadModuleUsingRootDirs()
        {
            var roots = compilerOptions.GetEffectiveRootDirs();
            if (roots == null || roots.Count == 0)
                return null;
            tracer?.write(Diagnostics.X_rootDirs_option_is_set_using_it_to_resolve_relative_module_name_0, name);
            var candidate = resolutionCandidateFromDirectoryPath(containingDirectory, name);
            RootedDirectoryPath matched = default;
            foreach (var root in roots)
            {
                var longest = candidate.HasDirectoryPrefix(root) && (matched == default || matched.AsString().Length < root.AsString().Length);
                tracer?.write(Diagnostics.Checking_if_0_is_the_longest_matching_prefix_for_1_2, TsPath.EnsureTrailingDirectorySeparator(root.AsString()), candidate, longest);
                if (longest)
                    matched = root;
            }

            if (matched == default)
                return null;
            tracer?.write(Diagnostics.Longest_matching_prefix_for_0_is_1, candidate, TsPath.EnsureTrailingDirectorySeparator(matched.AsString()));
            var suffix = candidate.RelativeToDirectory(matched);
            tracer?.write(Diagnostics.Loading_0_from_the_root_dir_1_candidate_location_2, suffix.AsString(), TsPath.EnsureTrailingDirectorySeparator(matched.AsString()), candidate);
            var result = nodeLoadModuleByRelativeName(extensions, candidate, true);
            if (result != null)
                return result;
            tracer?.write(Diagnostics.Trying_other_entries_in_rootDirs);
            foreach (var root in roots)
            {
                if (root == matched)
                    continue;
                var logical = suffix.AsString();
                var directoryOnly = suffix == default || suffix.HasTrailingDirectorySeparator();
                if (TsPath.IsEncodedDynamicFileName(root.AsString()))
                {
                    if (TsPath.IsEncodedDynamicFileName(matched.AsString()))
                        logical = TsPath.DecodeDynamicURIPath(logical);
                    candidate = resolutionCandidateFromDynamicLogicalPath(root, logical);
                }
                else if (TsPath.IsEncodedDynamicFileName(matched.AsString()))
                {
                    var(decoded, ok) = TsPath.DecodeDynamicURIPathForDisk(logical);
                    if (!ok)
                        continue;
                    candidate = resolutionCandidateFromDiskLogicalPath(root, decoded, directoryOnly);
                }
                else
                    candidate = resolutionCandidateFromRelativePath(root, suffix);
                tracer?.write(Diagnostics.Loading_0_from_the_root_dir_1_candidate_location_2, suffix.AsString(), root.AsString(), candidate);
                result = nodeLoadModuleByRelativeName(extensions, candidate, true);
                if (result != null)
                    return result;
            }

            tracer?.write(Diagnostics.Module_resolution_using_rootDirs_has_failed);
            return null;
        }

        internal ResolvedData? nodeLoadModuleByRelativeName(Extensions ext, resolutionCandidate candidate, bool considerPackageJson)
        {
            tracer?.write(Diagnostics.Loading_module_as_file_Slash_folder_candidate_module_location_0_target_file_types_Colon_1, candidate, ext.String());
            if (!candidate.HasTrailingDirectorySeparator())
            {
                var parent = candidate.Directory();
                if (!resolver.fs.DirectoryExists(parent))
                {
                    tracer?.write(Diagnostics.Directory_0_does_not_exist_skipping_all_lookups_in_it, parent);
                    return null;
                }

                var file = loadModuleFromFile(ext, candidate);
                if (file != null)
                {
                    if (considerPackageJson)
                    {
                        var package = ModuleUtilities.NodeModulePackageRootForFile(file.path);
                        if (package != default)
                            file.packageId = getPackageId(file.path, getPackageJsonInfo(packageDirectory(package)));
                    }

                    return file;
                }
            }

            if (!resolver.fs.DirectoryExists(candidate.AsDirectoryPath()))
            {
                tracer?.write(Diagnostics.Directory_0_does_not_exist_skipping_all_lookups_in_it, candidate);
                return null;
            }

            if (esmMode)
                return null;
            // Named legacy checking admission is external to source resolution.
            // Only the reached directory fallback can consult this metadata;
            // ancestor package scope reads and earlier file winners are untouched.
            if (considerPackageJson && resolver.fs is TypeScript.Compiler.ILegacyResolutionAdmissionHost admission)
                admission.ValidateDirectoryPackageJson(candidate.AsDirectoryPath());
            return loadNodeModuleFromDirectory(ext, candidate, considerPackageJson);
        }

        internal ResolvedData? loadModuleFromFile(Extensions ext, resolutionCandidate candidate)
        {
            var result = loadModuleFromFileNoImplicitExtensions(ext, candidate);
            if (result != null)
                return result;
            return !esmMode ? tryAddingExtensions(TsPath.RootedFilePathFromPath(candidate.path).AsStem(), candidate.Directory(), ext, "") : null;
        }

        internal ResolvedData? loadModuleFromFileNoImplicitExtensions(Extensions ext, resolutionCandidate candidate)
        {
            if (!candidate.path.BaseName().Contains('.'))
                return null;
            var(stem, extension) = candidate.SplitExtension(resolver.extraExtensions);
            tracer?.write(Diagnostics.File_name_0_has_a_1_extension_stripping_it, candidate, extension);
            return tryAddingExtensions(stem, candidate.Directory(), ext, extension);
        }

        internal ResolvedData? tryAddingExtensions(FileNameStem stem, RootedDirectoryPath directory, Extensions ext, string original)
        {
            if (directory != default && !resolver.fs.DirectoryExists(directory))
                return null;
            ResolvedData? Try(string suffix, Extensions mask, bool usingTs = false) => (ext & mask) != 0 ? tryExtension(suffix, stem, usingTs) : null;
            ResolvedData? result;
            switch (original)
            {
                case ".mjs":
                case ".mts":
                case ".d.mts":
                    return Try(".mts", Extensions.TypeScript, original is ".mts" or ".d.mts") ?? Try(".d.mts", Extensions.Declaration, original is ".mts" or ".d.mts") ?? Try(".mjs", Extensions.JavaScript);
                case ".cjs":
                case ".cts":
                case ".d.cts":
                    return Try(".cts", Extensions.TypeScript, original is ".cts" or ".d.cts") ?? Try(".d.cts", Extensions.Declaration, original is ".cts" or ".d.cts") ?? Try(".cjs", Extensions.JavaScript);
                case ".json":
                    return Try(".d.json.ts", Extensions.Declaration) ?? Try(".json", Extensions.Json);
                case ".tsx":
                case ".jsx":
                    return Try(".tsx", Extensions.TypeScript, original == ".tsx") ?? Try(".ts", Extensions.TypeScript, original == ".tsx") ?? Try(".d.ts", Extensions.Declaration, original == ".tsx") ?? Try(".jsx", Extensions.JavaScript) ?? Try(".js", Extensions.JavaScript);
                case ".ts":
                case ".d.ts":
                case ".js":
                case "":
                    result = Try(".ts", Extensions.TypeScript, original is ".ts" or ".d.ts") ?? Try(".tsx", Extensions.TypeScript, original is ".ts" or ".d.ts") ?? Try(".d.ts", Extensions.Declaration, original is ".ts" or ".d.ts") ?? Try(".js", Extensions.JavaScript) ?? Try(".jsx", Extensions.JavaScript);
                    return result ?? (isConfigLookup ? tryExtension(".json", stem, false) : null);
                default:
                    if (resolver.extraExtensions?.Contains(original) == true)
                    {
                        result = tryExtension(original, stem, false);
                        if (result != null)
                        {
                            result.resolvedUsingExtraExtensions = true;
                            return result;
                        }
                    }

                    return (ext & Extensions.Declaration) != 0 && !stem.AppendSuffix(original).IsDeclarationFile() ? tryExtension(".d" + original + ".ts", stem, false) : null;
            }
        }

        internal ResolvedData? tryExtension(string extension, FileNameStem stem, bool usingTs)
        {
            var(path, ok) = tryFile(resolutionCandidateFromFileName(stem.AppendSuffix(extension)));
            return ok ? new ResolvedData
            {
                path = path,
                extension = extension,
                resolvedUsingTsExtension = !candidateEndingIsFromConfig && usingTs
            }

            : null;
        }

        internal (RootedFilePath Path, bool Ok) tryFile(resolutionCandidate file)
        {
            if (file.directoryOnly)
                return (default, false);
            if (compilerOptions.ModuleSuffixes == null || compilerOptions.ModuleSuffixes.Count == 0)
            {
                var candidate = TsPath.RootedFilePathFromPath(file.path);
                return tryFileLookup(candidate) ? (candidate, true) : (default, false);
            }

            var extension = TsPath.RootedFilePathFromPath(file.path).Extension();
            var stem = file.RemoveExtension(extension);
            foreach (var suffix in compilerOptions.ModuleSuffixes)
            {
                var path = stem.AppendSuffix(suffix + extension);
                if (tryFileLookup(path))
                    return (path, true);
            }

            return (default, false);
        }

        internal bool tryFileLookup(RootedFilePath file)
        {
            if (resolver.fs.FileExists(file))
            {
                tracer?.write(Diagnostics.File_0_exists_use_it_as_a_name_resolution_result, file);
                return true;
            }

            tracer?.write(Diagnostics.File_0_does_not_exist, file);
            return false;
        }

        internal ResolvedData? loadNodeModuleFromDirectory(Extensions ext, resolutionCandidate candidate, bool considerPackageJson) => loadNodeModuleFromDirectoryWorker(ext, candidate.AsDirectoryPath(), considerPackageJson ? getPackageJsonInfo(packageDirectory(candidate.AsDirectoryPath())) : null);
        internal ResolvedData? loadNodeModuleFromDirectoryWorker(Extensions ext, RootedDirectoryPath candidate, InfoCacheEntry? info)
        {
            resolutionCandidate packageFile = default;
            VersionPaths versionPaths = default;
            if (info?.Exists() == true)
            {
                versionPaths = info.Contents!.GetVersionPaths(getTraceFunc());
                if (resolver.fs.CaseSensitivity().ComparePaths(candidate.AsPath(), info.PackageDirectory.AsDirectoryPath().AsPath()) == 0)
                {
                    var(file, ok) = getPackageFile(ext, info);
                    if (ok)
                        packageFile = file;
                }
            }

            ResolvedData? Loader(Extensions extensions, resolutionCandidate location)
            {
                var fromFile = loadFileNameFromPackageJSONField(extensions, location, packageFile.AsString());
                if (fromFile != null)
                    return fromFile;
                var expanded = extensions == Extensions.Declaration ? Extensions.TypeScript | Extensions.Declaration : extensions;
                var previousMode = esmMode;
                var previousEnding = candidateEndingIsFromConfig;
                candidateEndingIsFromConfig = true;
                if (info?.Exists() == true && info.Contents!.Fields.Type.Value != "module")
                    esmMode = false;
                try
                {
                    return nodeLoadModuleByRelativeName(expanded, location, false);
                }
                finally
                {
                    esmMode = previousMode;
                    candidateEndingIsFromConfig = previousEnding;
                }
            }

            var index = resolutionCandidateFromFileName(candidate.ResolveFile(isConfigLookup ? "tsconfig" : "index"));
            if (versionPaths.Exists() && (packageFile.path == default || CaseSensitivity.CaseInsensitive.ContainsPath(candidate, packageFile.path)))
            {
                var modulePath = packageFile.path != default ? packageFile.path : index.path;
                var(relative, ok) = CaseSensitivity.CaseInsensitive.RelativePathFromDirectory(candidate, TsPath.RootedFilePathFromPath(modulePath));
                if (!ok)
                    throw new InvalidOperationException("package module path must have the same root as its candidate directory");
                var moduleName = relative.AsString();
                if (TsPath.IsEncodedDynamicFileName(candidate.AsString()))
                    moduleName = TsPath.DecodeDynamicURIPath(moduleName);
                tracer?.write(Diagnostics.X_package_json_has_a_typesVersions_entry_0_that_matches_compiler_version_1_looking_for_a_pattern_to_match_module_name_2, versionPaths.Version, Core.Version(), moduleName);
                var result = tryLoadModuleUsingPaths(ext, ByteStringValue.FromString(moduleName), candidate, versionPaths.GetPaths(), TryParsePatterns(versionPaths.GetPaths()), Loader);
                if (result != null)
                {
                    if (result.packageId.Name != "")
                        throw new InvalidOperationException("expected packageId to be empty");
                    return result;
                }
            }

            if (packageFile.path != default)
            {
                var result = Loader(ext, packageFile);
                if (result != null)
                {
                    if (result.packageId.Name != "")
                        throw new InvalidOperationException("expected packageId to be empty");
                    return result;
                }
            }

            if (!esmMode && resolver.fs.DirectoryExists(candidate))
                return loadModuleFromFile(ext, index);
            return null;
        }

        internal ResolvedData? loadFileNameFromPackageJSONField(Extensions ext, resolutionCandidate candidate, string packageJSONValue)
        {
            if (candidate.directoryOnly)
                return null;
            var file = TsPath.RootedFilePathFromPath(candidate.path);
            if ((ext & Extensions.TypeScript) != 0 && file.HasImplementationTSFileExtension() || (ext & Extensions.Declaration) != 0 && file.IsDeclarationFile())
            {
                var(path, ok) = tryFile(candidate);
                if (!ok)
                    return null;
                var extension = path.TryExtractTSExtension();
                return new ResolvedData
                {
                    path = path,
                    extension = extension,
                    resolvedUsingTsExtension = packageJSONValue.EndsWith('*') && extension != ""
                };
            }

            if (isConfigLookup && (ext & Extensions.Json) != 0 && file.ExtensionIs(".json"))
            {
                var(path, ok) = tryFile(candidate);
                if (ok)
                    return new ResolvedData
                    {
                        path = path,
                        extension = ".json"
                    };
            }

            return loadModuleFromFileNoImplicitExtensions(ext, candidate);
        }

        internal (resolutionCandidate Candidate, bool Ok) getPackageFile(Extensions ext, InfoCacheEntry? info)
        {
            if (info?.Exists() != true)
                return (default, false);
            var fields = info.Contents!.Fields;
            var directory = info.PackageDirectory.AsDirectoryPath();
            if (isConfigLookup)
                return getPackageJSONPathField("tsconfig", fields.TSConfig, directory);
            if ((ext & Extensions.Declaration) != 0)
            {
                var file = getPackageJSONPathField("typings", fields.Typings, directory);
                if (file.Ok)
                    return file;
                file = getPackageJSONPathField("types", fields.Types, directory);
                if (file.Ok)
                    return file;
            }

            return (ext & (Extensions.ImplementationFiles | Extensions.Declaration)) != 0 ? getPackageJSONPathField("main", fields.Main, directory) : (default, false);
        }

        internal InfoCacheEntry? getPackageJsonInfo(PackageDirectory directory)
        {
            var existing = resolver.packageJsonInfoCache.Get(directory);
            if (existing != null)
            {
                if (existing.Contents != null)
                {
                    tracer?.write(Diagnostics.File_0_exists_according_to_earlier_cached_lookups, directory.ResolveFile("package.json"));
                    return existing.WithPackageDirectory(directory);
                }

                if (existing.DirectoryExists)
                    tracer?.write(Diagnostics.File_0_does_not_exist_according_to_earlier_cached_lookups, directory.ResolveFile("package.json"));
                return null;
            }

            var file = directory.ResolveFile("package.json");
            var exists = resolver.fs.DirectoryExists(directory.AsDirectoryPath());
            if (exists && resolver.fs.FileExists(file))
            {
                var(contents, _) = resolver.fs.ReadFile(file);
                var(fields, error) = PackageJsonParser.ParseFields(GoString.Bytes(contents));
                tracer?.write(Diagnostics.Found_package_json_at_0, file);
                var entry = new InfoCacheEntry
                {
                    PackageDirectory = directory,
                    DirectoryExists = true,
                    Contents = new TypeScript.PackageJson.PackageJson
                    {
                        Fields = fields,
                        Parseable = error == null
                    }
                };
                return resolver.packageJsonInfoCache.Set(directory, entry).WithPackageDirectory(directory);
            }

            if (exists)
                tracer?.write(Diagnostics.File_0_does_not_exist, file);
            resolver.packageJsonInfoCache.Set(directory, new InfoCacheEntry { PackageDirectory = directory, DirectoryExists = exists });
            return null;
        }

        internal PackageDirectory packageDirectory(RootedDirectoryPath directory) => resolver.packageJsonInfoCache.PackageDirectory(directory);
        internal PackageId getPackageId(RootedFilePath file, InfoCacheEntry? info)
        {
            if (info?.Exists() == true)
            {
                var(name, nameOk) = info.Contents!.Fields.Name.GetValue();
                var(version, versionOk) = info.Contents.Fields.Version.GetValue();
                if (nameOk && versionOk)
                {
                    var(relative, ok) = file.RelativeTo(info.PackageDirectory.AsDirectoryPath());
                    return new PackageId
                    {
                        Name = name,
                        Version = version,
                        SubModuleName = ok ? relative.AsString() : "",
                        PeerDependencies = readPackageJsonPeerDependencies(info)
                    };
                }
            }

            return default;
        }

        internal string readPackageJsonPeerDependencies(InfoCacheEntry info)
        {
            var dependencies = info.Contents!.Fields.PeerDependencies;
            if (!validatePackageJSONField("peerDependencies", dependencies) || dependencies.Value == null || dependencies.Value.Count == 0)
                return "";
            tracer?.write(Diagnostics.X_package_json_has_a_peerDependencies_field);
            var directory = realPath(TsPath.RootedFilePathFromPath(info.PackageDirectory.AsDirectoryPath().AsPath()));
            var(_, nodeModules, ok) = directory.SplitAtLastComponent("node_modules");
            if (!ok)
                return "";
            var builder = new System.Text.StringBuilder();
            foreach (var name in dependencies.Value.Keys.Order(StringComparer.Ordinal))
            {
                var peer = getPackageJsonInfo(packageDirectory(nodeModules.ResolveDirectory(pathForDynamicResolution(nodeModules, name, true))));
                if (peer?.Exists() == true)
                {
                    var version = peer.Contents!.Fields.Version.Value;
                    builder.Append('+').Append(name).Append('@').Append(version);
                    tracer?.write(Diagnostics.Found_peerDependency_0_with_1_version, name, version);
                }
                else
                    tracer?.write(Diagnostics.Failed_to_find_peerDependency_0, name);
            }

            return builder.ToString();
        }

        internal RootedFilePath realPath(RootedFilePath path)
        {
            var real = resolver.fs.Realpath(path.AsPath());
            tracer?.write(Diagnostics.Resolving_real_path_for_0_result_1, path, real);
            return TsPath.RootedFilePathFromPath(real);
        }

        internal bool validatePackageJSONField(string name, TypeValidatedField field)
        {
            if (field.IsPresent())
            {
                if (field.IsValid())
                    return true;
                tracer?.write(Diagnostics.Expected_type_of_0_field_in_package_json_to_be_1_got_2, name, field.ExpectedJSONType(), field.ActualJSONType());
            }

            tracer?.write(Diagnostics.X_package_json_does_not_have_a_0_field, name);
            return false;
        }

        internal (resolutionCandidate Candidate, bool Ok) getPackageJSONPathField(string name, Expected<string> field, RootedDirectoryPath directory)
        {
            if (!validatePackageJSONField(name, field))
                return (default, false);
            if (field.Value == "")
            {
                tracer?.write(Diagnostics.X_package_json_had_a_falsy_0_field, name);
                return (default, false);
            }

            var path = resolutionCandidateFromDirectoryPath(directory, field.Value);
            tracer?.write(Diagnostics.X_package_json_has_0_field_1_that_references_2, name, field.Value, path);
            return (path, true);
        }

        internal bool conditionMatches(string condition) => condition == "default" || GoSlices.Contains(conditions, condition) || GoSlices.Contains(conditions, "types") && ModuleUtilities.IsApplicableVersionedTypesKey(condition);
        internal Action<Message, object[]>? getTraceFunc() => tracer == null ? null : (message, args) => tracer.write(message, args);
        internal Slice<ResolvedEntrypoint> loadEntrypointsFromExportMap(InfoCacheEntry package, string packageName, ExportsOrImports exports)
        {
            Slice<ResolvedEntrypoint> entries = default;
            void Load(string subpath, HashSet<string>? include, HashSet<string>? exclude, ExportsOrImports target)
            {
                if (target.Type == JSONValueType.String && ((string)target.Value!).StartsWith("./", StringComparison.Ordinal))
                {
                    var text = (string)target.Value!;
                    var star = text.IndexOf('*');
                    if (star >= 0)
                    {
                        if (star != text.LastIndexOf('*'))
                            return;
                        var dynamicPackage = TsPath.IsEncodedDynamicFileName(package.PackageDirectory.String());
                        List<string> patterns = [TsPath.ChangeFullExtension(text[..star] + "**/*" + text[(star + 1)..], ".*")];
                        if (dynamicPackage)
                            patterns = ["**/*"];
                        var pattern = text[2..];
                        var split = pattern.IndexOf('*');
                        var leading = pattern[..split];
                        var trailing = pattern[(split + 1)..];
                        var files = VfsMatch.ReadDirectory(resolver.fs, package.PackageDirectory.AsDirectoryPath(), extensions.Array(), (List<string>? )null, patterns, VfsMatch.UnlimitedDepth);
                        foreach (var file in files)
                        {
                            var(relative, ok) = resolver.fs.CaseSensitivity().RelativeFilePathFromDirectory(package.PackageDirectory.AsDirectoryPath(), file);
                            if (!ok)
                                continue;
                            var logical = relative.AsString();
                            if (dynamicPackage)
                                logical = TsPath.DecodeDynamicURIPath(logical);
                            var(matched, matchedOk) = getMatchedStarForPatternEntrypoint(logical, leading, trailing, dynamicPackage);
                            if (!matchedOk)
                                continue;
                            if (dynamicPackage)
                                matched = TsPath.EncodeDynamicLogicalModuleSpecifier(matched);
                            var substar = subpath.IndexOf('*');
                            var resolvedSubpath = substar < 0 ? subpath : subpath[..substar] + matched + subpath[(substar + 1)..];
                            if (resolvedSubpath.StartsWith("./", StringComparison.Ordinal))
                                resolvedSubpath = resolvedSubpath[2..];
                            if (resolvedSubpath == "")
                                continue;
                            entries = entries.Append(resolver.createResolvedEntrypointHandlingSymlink(file, TsPath.ToModuleSpecifier(packageName + "/" + resolvedSubpath), include, exclude, text.EndsWith('*') ? Ending.ExtensionChangeable : Ending.Fixed));
                        }
                    }
                    else
                    {
                        var components = TsPath.GetPathComponents(text).Skip(2);
                        if (components.Any(part => part is ".." or "." or "node_modules"))
                            return;
                        var candidate = resolutionCandidateFromDirectoryPath(package.PackageDirectory.AsDirectoryPath(), text);
                        var result = loadFileNameFromPackageJSONField(extensions, candidate, text);
                        if (isResolved(result))
                            entries = entries.Append(resolver.createResolvedEntrypointHandlingSymlink(result!.path, TsPath.ToModuleSpecifier(TsPath.ResolvePath(packageName, subpath)), include, exclude, text.EndsWith('*') ? Ending.ExtensionChangeable : Ending.Fixed));
                    }
                }
                else if (target.Type == JSONValueType.Array)
                    foreach (var element in target.AsArray() ?? [])
                        Load(subpath, include, exclude, element);
                else if (target.Type == JSONValueType.Object)
                {
                    Slice<string> previous = default;
                    foreach (var(condition, export)in target.AsObject().Entries())
                    {
                        if (exclude?.Contains(condition) == true)
                            continue;
                        var always = condition is "default" or "types" || ModuleUtilities.IsApplicableVersionedTypesKey(condition);
                        var newInclude = include;
                        if (!always)
                        {
                            newInclude = include == null ? [] : new HashSet<string>(include, StringComparer.Ordinal);
                            exclude = exclude == null ? null : new HashSet<string>(exclude, StringComparer.Ordinal);
                            newInclude.Add(condition);
                            foreach (var prior in previous)
                                (exclude ??= []).Add(prior);
                        }

                        previous = previous.Append(condition);
                        Load(subpath, newInclude, exclude, export);
                        if (always)
                            break;
                    }
                }
            }

            if (exports.Type == JSONValueType.Array)
                foreach (var element in exports.AsArray() ?? [])
                    Load(".", null, null, element);
            else if (exports.Type == JSONValueType.Object && exports.IsSubpaths())
                foreach (var(subpath, export)in exports.AsObject().Entries())
                    Load(subpath, null, null, export);
            else
                Load(".", null, null, exports);
            return entries;
        }

        internal (string Matched, bool Ok) getMatchedStarForPatternEntrypoint(string file, string leading, string trailing, bool forceCaseSensitive)
        {
            var sensitive = forceCaseSensitive || resolver.fs.CaseSensitivity().IsCaseSensitive();
            bool Matches(string value) => value.Length >= leading.Length + trailing.Length && (sensitive ? value.StartsWith(leading, StringComparison.Ordinal) && value.EndsWith(trailing, StringComparison.Ordinal) : UnicodeCase.EqualFold(value.AsSpan(0, leading.Length), leading.AsSpan()) && UnicodeCase.EqualFold(value.AsSpan(value.Length - trailing.Length), trailing.AsSpan()));
            if (Matches(file))
                return (file[leading.Length..(file.Length - trailing.Length)], true);
            var js = ModuleUtilities.TryGetJSExtensionForFile(file, compilerOptions);
            if (js != "")
            {
                var swapped = TsPath.ChangeFullExtension(file, js);
                if (Matches(swapped))
                    return (swapped[leading.Length..(swapped.Length - trailing.Length)], true);
            }

            return ("", false);
        }
    }

    internal ParsedPatterns getParsedPatternsForPaths(CompilerOptions options) => parsedPatternsForPaths.Get(options.Paths);
    public static ParsedPatterns TryParsePatterns(OrderedMap<string, List<string>?>? mappings)
    {
        var keys = mappings?.Keys() ?? [];
        int numPatterns = 0, numMatchables = 0;
        foreach (var key in keys)
        {
            var pattern = Core.TryParsePattern(key);
            if (pattern.IsValid())
            {
                if (pattern.StarIndex == -1)
                    numMatchables++;
                else
                    numPatterns++;
            }
        }

        var result = new ParsedPatterns
        {
            patterns = numPatterns == 0 ? default : Slice<Pattern>.Make(0, numPatterns),
            matchableStringSet = numMatchables == 0 ? null : new HashSet<ByteStringKey>(numMatchables, ByteStringKey.Comparer)
        };
        foreach (var key in keys)
        {
            var pattern = Core.TryParsePattern(key);
            if (pattern.IsValid())
            {
                if (pattern.StarIndex == -1)
                    result.matchableStringSet!.Add(ByteStringKey.FromString(key));
                else
                    result.patterns = result.patterns.Append(pattern);
            }
        }

        return result;
    }

    public static Pattern MatchPatternOrExact(ParsedPatterns patterns, string candidate) => MatchPatternOrExact(patterns, ByteStringValue.FromString(candidate));
    internal static Pattern MatchPatternOrExact(ParsedPatterns patterns, ByteStringValue candidate) => patterns.matchableStringSet is not null && !candidate.IsNull && candidate.ContainsIn(patterns.matchableStringSet) ? new Pattern(candidate, -1) : Core.FindBestPatternMatch(patterns.patterns, static value => value, candidate);
    internal static resolutionCandidate normalizePathForCJSResolution(RootedDirectoryPath directory, string name) => normalizePathForCJSResolution(directory, ByteStringValue.FromString(name));
    internal static resolutionCandidate normalizePathForCJSResolution(RootedDirectoryPath directory, ByteStringValue name)
    {
        int end = name.Length;
        while (end > 0 && name[end - 1] is '/' or '\\') end--;
        var trimmed = name.Slice(0, end);
        int separator = trimmed.IsBytes ? trimmed.ByteSpan.LastIndexOfAny((byte)'/', (byte)'\\') : trimmed.CharSpan.LastIndexOfAny('/', '\\');
        var last = trimmed.Slice(separator + 1);
        var candidate = resolutionCandidateFromDirectoryPath(directory, name);
        return candidate with { directoryOnly = end != name.Length || last.EqualsBytes("."u8) || last.EqualsBytes(".."u8) || TsPath.HasTrailingDirectorySeparator(candidate.path.AsString()) };
    }

    internal static (RootedPath Path, string Text, bool DirectoryOnly, bool HasTrailingDirectorySeparator) VerificationNormalizePathForCJSResolution(RootedDirectoryPath directory, string name)
    {
        var candidate = normalizePathForCJSResolution(directory, name);
        return (candidate.path, candidate.AsString(), candidate.directoryOnly, candidate.HasTrailingDirectorySeparator());
    }

    internal static bool matchesPatternWithTrailer(string target, string name) => matchesPatternWithTrailer(target, ByteStringValue.FromString(name));
    internal static bool matchesPatternWithTrailer(string target, ByteStringValue name)
    {
        if (target.EndsWith('*'))
            return false;
        var star = target.IndexOf('*');
        return star >= 0 && name.StartsWith(ByteStringValue.FromString(target).Slice(0, star)) && name.EndsWith(ByteStringValue.FromString(target).Slice(star + 1));
    }

    private static ByteStringValue substituteTarget(string target, ByteStringValue subpath, bool isPattern, bool replaceAll = true)
    {
        var text = ByteStringValue.FromString(target);
        if (!isPattern)
        {
            var builder = new GoBuilder(); builder.WriteString(text); builder.WriteString(subpath); return builder.Value();
        }
        int start = 0, star = text.IndexOf('*');
        if (star < 0) return text;
        var output = new GoBuilder();
        while (star >= 0)
        {
            output.WriteString(text.Slice(start, star)); output.WriteString(subpath); start = star + 1;
            if (!replaceAll) break;
            int relative = text.Slice(start).IndexOf('*'); star = relative < 0 ? -1 : start + relative;
        }
        output.WriteString(text.Slice(start)); return output.Value();
    }
    internal static bool extensionIsOk(Extensions mask, string extension) => (mask & Extensions.JavaScript) != 0 && extension is ".js" or ".jsx" or ".mjs" or ".cjs" || (mask & Extensions.TypeScript) != 0 && extension is ".ts" or ".tsx" or ".mts" or ".cts" || (mask & Extensions.Declaration) != 0 && extension is ".d.ts" or ".d.mts" or ".d.cts" || (mask & Extensions.Json) != 0 && extension == ".json";
    public static List<string>? GetAutomaticTypeDirectiveNames(CompilerOptions options, RootedDirectoryPath directory, IFS fs)
    {
        if (!options.UsesWildcardTypes())
            return options.Types ?? [];
        List<string>? matches = null;
        var(roots, _) = options.GetEffectiveTypeRoots(directory);
        foreach (var root in roots ?? [])
        {
            if (!fs.DirectoryExists(root))
                continue;
            foreach (var type in fs.GetAccessibleEntries(root).Directories)
            {
                var typeDirectory = root.ResolveDirectory(type);
                var packageFile = typeDirectory.ResolveFile("package.json");
                var notNeeded = false;
                if (fs.FileExists(packageFile))
                {
                    var(contents, _ok) = fs.ReadFile(packageFile);
                    var(fields, _error) = PackageJsonParser.ParseFields(GoString.Bytes(contents));
                    notNeeded = fields.Typings.Null;
                }

                if (!notNeeded)
                {
                    var name = typeDirectory.AsPath().BaseName();
                    if (!name.StartsWith('.'))
                        (matches ??= []).Add(name);
                }
            }
        }

        List<string>? result = null;
        foreach (var type in options.Types ?? [])
        {
            if (type == "*")
            {
                if (matches != null)
                    (result ??= []).AddRange(matches);
            }
            else
                (result ??= []).Add(type);
        }

        return Core.Deduplicate(result);
    }

    public Slice<ResolvedEntrypoint> GetEntrypointsFromPackageJsonInfo(InfoCacheEntry package, string packageName, bool enableDirectorySearch)
    {
        const Extensions extensions = Extensions.TypeScript | Extensions.Declaration;
        var state = new resolutionState
        {
            resolver = this,
            extensions = extensions,
            features = NodeResolutionFeatures.All,
            compilerOptions = compilerOptions
        };
        var dynamicPackage = TsPath.IsEncodedDynamicFileName(package.PackageDirectory.String());
        var sourceName = dynamicPackage ? TsPath.DynamicURIPathToModuleSpecifier(packageName) : packageName;
        if (package.Exists() && package.Contents!.Exports.IsPresent())
            return state.loadEntrypointsFromExportMap(package, sourceName, package.Contents.Exports);
        Slice<ResolvedEntrypoint> result = default;
        var main = state.loadNodeModuleFromDirectoryWorker(extensions, package.PackageDirectory.AsDirectoryPath(), package);
        if (isResolved(main))
            result = result.Append(createResolvedEntrypointHandlingSymlink(main!.path, TsPath.ToModuleSpecifier(sourceName), null, null, Ending.Fixed));
        if (enableDirectorySearch)
        {
            var files = VfsMatch.ReadDirectory(fs, package.PackageDirectory.AsDirectoryPath(), extensions.Array(), (List<string>)["node_modules"], (List<string>)["**/*"], VfsMatch.UnlimitedDepth);
            foreach (var file in files)
            {
                if (isResolved(main) && fs.CaseSensitivity().CompareFilePaths(file, main!.path) == 0)
                    continue;
                var(relative, _) = fs.CaseSensitivity().RelativeFilePathFromDirectory(package.PackageDirectory.AsDirectoryPath(), file);
                var specifier = relative.AsString();
                if (dynamicPackage)
                    specifier = TsPath.DynamicURIPathToModuleSpecifier(specifier);
                result = result.Append(createResolvedEntrypointHandlingSymlink(file, TsPath.ToModuleSpecifier(sourceName + "/" + specifier), null, null, Ending.Changeable));
            }
        }

        return result;
    }

    internal ResolvedEntrypoint createResolvedEntrypointHandlingSymlink(RootedFilePath file, ModuleSpecifier specifier, HashSet<string>? include, HashSet<string>? exclude, Ending ending)
    {
        var real = fs.Realpath(file.AsPath());
        return new ResolvedEntrypoint
        {
            OriginalFileName = real != file.AsPath() ? file : default,
            ResolvedFileName = TsPath.RootedFilePathFromPath(real),
            ResolvedPath = fs.CaseSensitivity().PathKey(real),
            ModuleSpecifier = specifier,
            IncludeConditions = include,
            ExcludeConditions = exclude,
            Ending = ending
        };
    }
}

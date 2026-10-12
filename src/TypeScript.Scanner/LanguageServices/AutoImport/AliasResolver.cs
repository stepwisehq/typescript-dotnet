// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of internal/ls/autoimport/aliasresolver.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Compiler;
using TypeScript.Collections;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Symlinks;
using TypeScript.TsOptions;

namespace TypeScript.LanguageServices.AutoImport;
internal readonly record struct PathAndFileName(PathKey path, RootedFilePath fileName);
internal sealed class AliasResolver : ICheckerProgramHost
{
    private readonly CaseSensitivity caseSensitivity;
    private readonly IRegistryCloneHost host;
    private readonly IResolver moduleResolver;
    private readonly Slice<SourceFile> rootFiles;
    private readonly Dictionary<PathKey, PathAndFileName>? symlinks;
    private readonly Action<HasFileName, ByteStringValue> onFailedAmbientModuleLookup;
    private readonly SyncMap<PathKey, SyncMap<ModeAwareCacheKey, ResolvedModule?>> resolvedModules = new();
    // internal/ls/autoimport/aliasresolver.go:33
    internal AliasResolver(Slice<SourceFile> rootFiles, Dictionary<PathKey, PathAndFileName>? symlinks, IRegistryCloneHost host, IResolver moduleResolver, Action<HasFileName, ByteStringValue> onFailedAmbientModuleLookup)
    {
        caseSensitivity = host.FS().CaseSensitivity();
        this.host = host;
        this.moduleResolver = moduleResolver;
        this.rootFiles = rootFiles;
        this.symlinks = symlinks;
        this.onFailedAmbientModuleLookup = onFailedAmbientModuleLookup;
    }

    // internal/ls/autoimport/aliasresolver.go:54. Bound as parsed by getSourceFile.
    public void BindSourceFiles()
    {
    }

    public Slice<SourceFile> SourceFiles => rootFiles;
    internal Dictionary<PathKey, PathAndFileName>? Symlinks => symlinks;
    public CompilerOptions Options => new()
    {
        NoCheck = Tristate.True
    };

    public RootedDirectoryPath BaseDirectory() => moduleResolver.BaseDirectory();
    public CaseSensitivity CaseSensitivity() => caseSensitivity;
    public SourceFile? GetSourceFile(RootedFilePath fileName) => getSourceFile(fileName, caseSensitivity.PathKey(fileName.AsPath()));
    private SourceFile? getSourceFile(RootedFilePath fileName, PathKey path)
    {
        var file = host.GetSourceFile(fileName, path);
        // Original nil handling for symlink/realpath mismatches.
        if (file == null)
            return null;
        Binder.BindSourceFile(file);
        return file;
    }

    internal SourceFile? GetSourceFileByFileName(RootedFilePath fileName, PathKey path) => getSourceFile(fileName, path);
    public ResolutionMode GetDefaultResolutionModeForFile(HasFileName file) => ResolutionMode.ESNext;
    public ModuleKind GetEmitModuleFormatOfFile(SourceFile file) => ModuleKind.ESNext;
    internal ModuleKind GetEmitModuleFormatOfFile(HasFileName file) => ModuleKind.ESNext;
    public ResolutionMode GetEmitSyntaxForUsageLocation(SourceFile file, Node usageLocation) => ResolutionMode.ESNext;
    internal ResolutionMode GetEmitSyntaxForUsageLocation(HasFileName file, Node usageLocation) => ResolutionMode.ESNext;
    public ModuleKind GetImpliedNodeFormatForEmit(SourceFile file) => ModuleKind.ESNext;
    internal ModuleKind GetImpliedNodeFormatForEmit(HasFileName file) => ModuleKind.ESNext;
    public ResolutionMode GetModeForUsageLocation(HasFileName file, Node moduleSpecifier) => ResolutionMode.ESNext;
    // internal/ls/autoimport/aliasresolver.go:129. Required competing cache owner remains real.
    public ResolvedModule? GetResolvedModule(SourceFile sourceFile, string moduleReference, ResolutionMode mode) => GetResolvedModule((HasFileName)sourceFile, ByteStringValue.FromString(moduleReference), mode);
    public ResolvedModule? GetResolvedModule(SourceFile sourceFile, ByteStringValue moduleReference, ResolutionMode mode) => GetResolvedModule((HasFileName)sourceFile, moduleReference, mode);
    internal ResolvedModule? GetResolvedModule(HasFileName sourceFile, ByteStringValue moduleReference, ResolutionMode mode)
    {
        var(cache, _) = resolvedModules.LoadOrStore(sourceFile is SourceFile owned ? PathKey.FromValue(owned.PathKeyValue()) : PathKey.FromGoString(sourceFile.PathKey()), new SyncMap<ModeAwareCacheKey, ResolvedModule?>());
        var key = new ModeAwareCacheKey(moduleReference, mode);
        var(cached, found) = cache.Load(key);
        if (found)
            return cached;
        var(resolved, _, _) = moduleResolver.ResolveModuleName(moduleReference, sourceFile is SourceFile ownedFile ? RootedFilePath.FromValue(ownedFile.FileNameGoValue()) : RootedFilePath.FromGoString(sourceFile.FileName()), mode, null);
        (resolved, _) = cache.LoadOrStore(key, resolved);
        if (!resolved.IsResolved() && !TsPath.PathIsRelative(moduleReference))
            onFailedAmbientModuleLookup(sourceFile, moduleReference);
        return resolved;
    }

    public SourceFile? GetSourceFileForResolvedModule(ResolvedModule resolved) => getSourceFile(resolved.ResolvedFileName, resolved.ResolvedPath);
    // internal/ls/autoimport/aliasresolver.go:149. Original diagnostics-only nil producer.
    public Dictionary<ByteStringKey, ModeAwareCache<ResolvedModule?>> GetResolvedModules() => null!;
    // These leaves are the original upstream panic contracts, not port placeholders.
    // Alias extraction must not call them. Preserve each original function boundary.
    // internal/ls/autoimport/aliasresolver.go:156
    public KnownSymlinks GetSymlinkCache() => throw new InvalidOperationException("unimplemented");
    public SourceFileMetaData GetSourceFileMetaData(PathKey path) => throw new InvalidOperationException("unimplemented");
    public RootedDirectoryPath CommonSourceDirectory() => throw new InvalidOperationException("unimplemented");
    public List<string>? ContentMapperExtensions() => null;
    public bool FileExists(RootedFilePath fileName) => throw new InvalidOperationException("unimplemented");
    public RootedDirectoryPath GetGlobalTypingsCacheLocation() => throw new InvalidOperationException("unimplemented");
    public Node? GetImportHelpersImportSpecifier(PathKey path) => throw new InvalidOperationException("unimplemented");
    public (string ModuleReference, Node? Specifier) GetJSXRuntimeImportSpecifier(PathKey path) => ("", null);
    public (ByteStringValue ModuleReference, Node? Specifier) GetJSXRuntimeImportSpecifierValue(PathKey path) => (default, null);
    public RootedDirectoryPath GetNearestAncestorDirectoryWithPackageJson(RootedDirectoryPath directory) => throw new InvalidOperationException("unimplemented");
    public InfoCacheEntry? GetPackageJsonInfo(RootedFilePath path) => throw new InvalidOperationException("unimplemented");
    public SourceOutputAndProjectReference? GetProjectReferenceFromOutputDts(PathKey path) => throw new InvalidOperationException("unimplemented");
    public SourceOutputAndProjectReference? GetProjectReferenceFromSource(PathKey path) => throw new InvalidOperationException("unimplemented");
    public ParsedCommandLine? GetRedirectForResolution(SourceFile file) => throw new InvalidOperationException("unimplemented");
    public Slice<RootedFilePath> GetRedirectTargets(PathKey path) => throw new InvalidOperationException("unimplemented");
    public ResolvedModule? GetResolvedModuleFromModuleSpecifier(HasFileName file, Node specifier) => throw new InvalidOperationException("unimplemented");
    public RootedFilePath GetSourceOfProjectReferenceIfOutputIncluded(HasFileName file) => throw new InvalidOperationException("unimplemented");
    public bool IsSourceFileDefaultLibrary(SourceFile file) => false;
    internal bool IsSourceFileDefaultLibrary(PathKey path) => false;
    internal bool IsSourceFromProjectReference(PathKey path) => throw new InvalidOperationException("unimplemented");
    public bool SourceFileMayBeEmitted(SourceFile file, bool forceDtsEmit) => throw new InvalidOperationException("unimplemented");
    public Dictionary<string, bool> GetPackagesMap() => null!;
    // Existing managed checker admission/source-byte boundaries, not module resolution.
    public bool AllowsNonRelativeModuleSpecifiers => true;

    public string GetSourceFileNameGoBytes(SourceFile file) => file.FileNameGoValue().Materialize()!;
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete counterpart of internal/ls/autoimport/registry.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.LanguageServices.Utilities;
using TypeScript.ProjectSystem.Logging;
using TypeScript.Compiler;
using TypeScript.Vfs;
using TypeScript.PackageJson;
using TypeScript.ProjectSystem.Dirty;
using TypeScript.Lsp.Protocol;
using CompilerProgram = TypeScript.Compiler.Program;
using TypeScript.Collections;

namespace TypeScript.LanguageServices.AutoImport;
internal static partial class AutoImport
{
    // registry.go:35. Exact pinned package allow-list.
    internal static readonly HashSet<ByteStringKey> KnownRecursiveSearchPackages = new()
    {
        ByteStringKey.FromValue(ByteStringValue.FromString("@material-ui/core")),
        ByteStringKey.FromValue(ByteStringValue.FromString("@material-ui/icons")),
        ByteStringKey.FromValue(ByteStringValue.FromString("@sap/cds")),
        ByteStringKey.FromValue(ByteStringValue.FromString("@testing-library/react-native")),
        ByteStringKey.FromValue(ByteStringValue.FromString("ajv")),
        ByteStringKey.FromValue(ByteStringValue.FromString("asap")),
        ByteStringKey.FromValue(ByteStringValue.FromString("async")),
        ByteStringKey.FromValue(ByteStringValue.FromString("aws-sdk")),
        ByteStringKey.FromValue(ByteStringValue.FromString("braintree-web")),
        ByteStringKey.FromValue(ByteStringValue.FromString("core-js")),
        ByteStringKey.FromValue(ByteStringValue.FromString("core-js-pure")),
        ByteStringKey.FromValue(ByteStringValue.FromString("crypto-js")),
        ByteStringKey.FromValue(ByteStringValue.FromString("cypress-mochawesome-reporter")),
        ByteStringKey.FromValue(ByteStringValue.FromString("dd-trace")),
        ByteStringKey.FromValue(ByteStringValue.FromString("dumi")),
        ByteStringKey.FromValue(ByteStringValue.FromString("dva")),
        ByteStringKey.FromValue(ByteStringValue.FromString("egg-mock")),
        ByteStringKey.FromValue(ByteStringValue.FromString("electron-log")),
        ByteStringKey.FromValue(ByteStringValue.FromString("es-abstract")),
        ByteStringKey.FromValue(ByteStringValue.FromString("es6-promise")),
        ByteStringKey.FromValue(ByteStringValue.FromString("eslint-config-taro")),
        ByteStringKey.FromValue(ByteStringValue.FromString("expo")),
        ByteStringKey.FromValue(ByteStringValue.FromString("expo-router")),
        ByteStringKey.FromValue(ByteStringValue.FromString("flow-remove-types")),
        ByteStringKey.FromValue(ByteStringValue.FromString("gatsby")),
        ByteStringKey.FromValue(ByteStringValue.FromString("glamor")),
        ByteStringKey.FromValue(ByteStringValue.FromString("gluegun")),
        ByteStringKey.FromValue(ByteStringValue.FromString("graphology-indices")),
        ByteStringKey.FromValue(ByteStringValue.FromString("graphology-traversal")),
        ByteStringKey.FromValue(ByteStringValue.FromString("graphology-utils")),
        ByteStringKey.FromValue(ByteStringValue.FromString("jest-expo")),
        ByteStringKey.FromValue(ByteStringValue.FromString("lodash")),
        ByteStringKey.FromValue(ByteStringValue.FromString("lodash-es")),
        ByteStringKey.FromValue(ByteStringValue.FromString("moment")),
        ByteStringKey.FromValue(ByteStringValue.FromString("mz")),
        ByteStringKey.FromValue(ByteStringValue.FromString("next")),
        ByteStringKey.FromValue(ByteStringValue.FromString("pdfjs-dist")),
        ByteStringKey.FromValue(ByteStringValue.FromString("protobufjs")),
        ByteStringKey.FromValue(ByteStringValue.FromString("react-app-polyfill")),
        ByteStringKey.FromValue(ByteStringValue.FromString("react-dev-utils")),
        ByteStringKey.FromValue(ByteStringValue.FromString("react-devtools-inline")),
        ByteStringKey.FromValue(ByteStringValue.FromString("recast")),
        ByteStringKey.FromValue(ByteStringValue.FromString("semver")),
        ByteStringKey.FromValue(ByteStringValue.FromString("stylelint-config-html")),
        ByteStringKey.FromValue(ByteStringValue.FromString("umi")),
        ByteStringKey.FromValue(ByteStringValue.FromString("web3-provider-engine")),
        ByteStringKey.FromValue(ByteStringValue.FromString("webpack")),
    };
    // internal/ls/autoimport/registry.go:1143
    internal static bool HasNewNonNodeModulesFiles(TypeScript.Compiler.Program program, RegistryBucket bucket)
    {
        if (bucket.state.newProgramStructure != NewProgramStructure.DifferentFileNames)
            return false;
        foreach (var file in program.GetSourceFiles())
        {
            if (file.IsContentMapperSupplemental() || RootedFilePath.FromValue(file.FileNameGoValue()).ContainsLowercaseDirectorySequence("/node_modules/") || IsIgnoredFile(program, file))
                continue;
            if (bucket.Paths?.ContainsKey(PathKey.FromValue(file.PathKeyValue())) != true)
                return true;
        }

        return false;
    }

    // internal/ls/autoimport/registry.go:1158
    internal static bool IsIgnoredFile(TypeScript.Compiler.Program program, SourceFile file) => program.IsSourceFileDefaultLibrary(PathKey.FromValue(file.PathKeyValue())) || program.IsGlobalTypingsFile(RootedFilePath.FromValue(file.FileNameGoValue()));
    // internal/ls/autoimport/registry.go:1165
    internal static bool HasSymlinkToNodeModules(PathKey filePath, PathKey projectRootPath, TypeScript.Symlinks.KnownSymlinks? symlinkCache)
    {
        if (symlinkCache == null || projectRootPath.ContainsPath(filePath))
            return false;
        var filesByRealpath = symlinkCache.FilesByRealpath();
        if (filesByRealpath != null)
        {
            var(symlinkPaths, ok) = filesByRealpath.Load(filePath);
            if (ok)
            {
                bool found = false;
                symlinkPaths.Range(path =>
                {
                    if (path.ContainsLowercaseDirectorySequence("/node_modules/"))
                    {
                        found = true;
                        return false;
                    }

                    return true;
                });
                if (found)
                    return true;
            }
        }

        var directoriesByRealpath = symlinkCache.DirectoriesByRealpath();
        if (directoriesByRealpath == null)
            return false;
        bool directoryFound = false;
        filePath.ForEachAncestorDirectory<object?>(path =>
        {
            var(symlinkPaths, ok) = directoriesByRealpath.Load(path);
            if (!ok)
                return (null, false);
            symlinkPaths.Range(symlinkPath =>
            {
                if (symlinkPath.ContainsLowercaseDirectorySequence("/node_modules/"))
                {
                    directoryFound = true;
                    return false;
                }

                return true;
            });
            return (null, directoryFound);
        });
        return directoryFound;
    }
}

internal interface IProjectID : IGoLogStringer
{
}

internal enum NewProgramStructure
{
    False,
    SameFileNames,
    DifferentFileNames
}

internal struct BucketBuildPreferences
{
    internal Slice<string> fileExcludePatterns;
    internal Tristate autoImportEntrypointDirectorySearch;
    internal static BucketBuildPreferences FromUserPreferences(UserPreferences preferences) => new()
    {
        fileExcludePatterns = preferences.AutoImportFileExcludePatterns,
        autoImportEntrypointDirectorySearch = preferences.AutoImportEntrypointDirectorySearch,
    };
    // internal/ls/autoimport/registry.go:109
    internal bool Equal(BucketBuildPreferences other) => Core.UnorderedEqual(fileExcludePatterns, other.fileExcludePatterns) && autoImportEntrypointDirectorySearch == other.autoImportEntrypointDirectorySearch;
    internal BucketBuildPreferences Clone()
    {
        var result = this;
        result.fileExcludePatterns = GoSlices.Clone(fileExcludePatterns);
        return result;
    }
}

internal struct BucketState
{
    internal PathKey dirtyFile;
    internal bool multipleFilesDirty;
    internal NewProgramStructure newProgramStructure;
    internal BucketBuildPreferences buildPreferences;
    internal HashSet<ByteStringKey>? dirtyPackages;
    // nil means all packages were searched; allocated empty means none were.
    internal HashSet<ByteStringKey>? recursiveSearchPackages;
    // internal/ls/autoimport/registry.go:156
    internal BucketState Clone()
    {
        var result = this;
        result.buildPreferences = buildPreferences.Clone();
        result.dirtyPackages = dirtyPackages == null ? null : new(dirtyPackages, dirtyPackages.Comparer);
        result.recursiveSearchPackages = recursiveSearchPackages == null ? null : new(recursiveSearchPackages, recursiveSearchPackages.Comparer);
        return result;
    }

    internal bool Dirty() => multipleFilesDirty || dirtyFile.AsValue().Length != 0 || newProgramStructure > 0 || (dirtyPackages?.Count ?? 0) > 0;
    internal PathKey DirtyFile() => multipleFilesDirty ? default : dirtyFile;
    internal HashSet<ByteStringKey>? DirtyPackages() => multipleFilesDirty ? null : dirtyPackages;
    internal HashSet<ByteStringKey>? RecursiveSearchPackages() => recursiveSearchPackages;
    internal bool PossiblyNeedsRebuildForFile(PathKey file, UserPreferences preferences) => newProgramStructure > 0 || HasDirtyFileBesides(file) || !buildPreferences.Equal(BucketBuildPreferences.FromUserPreferences(preferences)) || (dirtyPackages?.Count ?? 0) > 0;
    internal bool HasDirtyFileBesides(PathKey file) => multipleFilesDirty || dirtyFile.AsValue().Length != 0 && dirtyFile != file;
}

internal sealed class RegistryBucket : TypeScript.ProjectSystem.Dirty.ICloneable<RegistryBucket>
{
    internal BucketState state;
    // These finalized owners are immutable: updates replace them rather than mutating them.
    internal Dictionary<PathKey, ByteStringValue>? Paths;
    internal Dictionary<ByteStringKey, Dictionary<PathKey, RootedFilePath>?>? PackageFiles;
    internal HashSet<ByteStringKey>? ResolvedPackageNames;
    internal HashSet<ByteStringKey>? DependencyNames;
    internal Dictionary<ByteStringKey, Slice<RootedFilePath>>? AmbientModuleNames;
    internal Index<Export>? Index;
    // internal/ls/autoimport/registry.go:262
    internal static RegistryBucket NewRegistryBucket() => new()
    {
        state = new BucketState
        {
            multipleFilesDirty = true,
            newProgramStructure = NewProgramStructure.DifferentFileNames,
        },
    };
    public RegistryBucket Clone() => new()
    {
        state = state.Clone(),
        Paths = Paths,
        PackageFiles = PackageFiles,
        ResolvedPackageNames = ResolvedPackageNames,
        DependencyNames = DependencyNames,
        AmbientModuleNames = AmbientModuleNames,
        Index = Index,
    };
    internal void MarkProjectFileDirty(PathKey file)
    {
        if (state.HasDirtyFileBesides(file))
            state.multipleFilesDirty = true;
        else
            state.dirtyFile = file;
    }

    internal void MarkNodeModulesDirty(ByteStringValue packageName)
    {
        if (state.multipleFilesDirty)
            return;
        if (packageName.Length == 0)
        {
            state.multipleFilesDirty = true;
            return;
        }

        state.dirtyPackages ??= new();
        state.dirtyPackages.Add(ByteStringKey.FromValue(packageName));
    }
}

internal static partial class AutoImport
{
    // internal/ls/autoimport/registry.go:204. nil denotes the all-packages superset.
    internal static bool RecursiveSearchSubset(HashSet<ByteStringKey>? target, HashSet<ByteStringKey>? current)
    {
        if (target == null)
            return current == null;
        if (current == null)
            return true;
        return target.IsSubsetOf(current);
    }
}

// internal/ls/autoimport/registry.go:308. A pointer owner: cloning preserves package metadata identity.
internal sealed class RegistryDirectory : TypeScript.ProjectSystem.Dirty.ICloneable<RegistryDirectory>
{
    internal RootedDirectoryPath name;
    internal TypeScript.PackageJson.InfoCacheEntry? packageJson;
    internal bool hasNodeModules;
    public RegistryDirectory Clone() => new()
    {
        name = name,
        packageJson = packageJson,
        hasNodeModules = hasNodeModules
    };
}

internal sealed partial class Registry
{
    internal CaseSensitivity caseSensitivity;
    internal UserPreferences userPreferences;
    internal Dictionary<PathKey, RegistryDirectory> directories = null!;
    internal Dictionary<PathKey, RegistryBucket>? nodeModules;
    internal Dictionary<IProjectID, RegistryBucket>? projects;
    internal int uniquePackageCount;
    internal Dictionary<PathKey, Slice<TypeScript.Modules.ResolvedEntrypoint>>? entrypoints;
    internal Dictionary<PathKey, TypeScript.Collections.SyncMap<PathKey, ModuleSpecifier>>? specifierCache;
    // internal/ls/autoimport/registry.go:341. Only directories is made in the original constructor.
    internal static Registry NewRegistry(CaseSensitivity caseSensitivity, UserPreferences preferences) => new()
    {
        caseSensitivity = caseSensitivity,
        userPreferences = preferences,
        directories = new(),
    };
    // internal/ls/autoimport/registry.go:350. The original method accepts a nil registry receiver.
    internal static bool IsPreparedForImportingFile(Registry? registry, RootedFilePath fileName, IProjectID projectID, UserPreferences preferences) => registry != null && registry.IsPreparedForImportingFile(fileName, projectID, preferences);
    internal bool IsPreparedForImportingFile(RootedFilePath fileName, IProjectID projectID, UserPreferences preferences)
    {
        if (projects == null || !projects.TryGetValue(projectID, out var projectBucket))
            return false;
        var path = caseSensitivity.PathKey(fileName.AsPath());
        if (projectBucket.state.PossiblyNeedsRebuildForFile(path, preferences))
            return false;
        var directoryPath = path.Parent();
        for (;;)
        {
            if (nodeModules != null && nodeModules.TryGetValue(directoryPath, out var bucket) && bucket.state.PossiblyNeedsRebuildForFile(path, preferences))
                return false;
            var parent = directoryPath.Parent();
            if (parent == directoryPath)
                break;
            directoryPath = parent;
        }

        return true;
    }

    // internal/ls/autoimport/registry.go:382. Required new result-map owner, including an explicit empty map.
    internal Dictionary<PathKey, RootedDirectoryPath> NodeModulesDirectories()
    {
        var result = new Dictionary<PathKey, RootedDirectoryPath>();
        foreach (var(path, directory)in directories)
        {
            if (directory.hasNodeModules)
                result[path.AppendCanonicalComponent("node_modules")] = directory.name.ResolveDirectory("node_modules");
        }

        return result;
    }

    // internal/ls/autoimport/registry.go:392
    internal (Registry Registry, Exception? Error) Clone(CancellationToken cancellation, RegistryChange change, IRegistryCloneHost host, RootedDirectoryPath currentDirectory, LogTree? logger)
    {
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        if (logger != null)
            logger = logger.Fork("Building autoimport registry");
        var builder = RegistryBuilder.NewRegistryBuilder(this, host, currentDirectory);
        if (change.UserPreferences is { } preferences)
        {
            builder.userPreferences = preferences;
            if (!Core.UnorderedEqual(preferences.AutoImportSpecifierExcludeRegexes, userPreferences.AutoImportSpecifierExcludeRegexes))
                builder.specifierCache.Clear();
        }

        builder.UpdateBucketAndDirectoryExistence(change, logger);
        builder.MarkBucketsDirty(change, logger);
        if (change.RequestedFile != default)
            builder.UpdateIndexes(cancellation, change, logger);
        logger?.Logf("Built autoimport registry in %v", System.Diagnostics.Stopwatch.GetElapsedTime(start));
        return (builder.Build(), null);
    }

    // internal/ls/autoimport/registry.go:431
    internal CacheStats GetCacheStats()
    {
        var stats = new CacheStats
        {
            UniquePackageCount = uniquePackageCount
        };
        if (projects != null)
            foreach (var(projectID, bucket)in projects)
                stats.ProjectBuckets = stats.ProjectBuckets.Append(new BucketStats { Name = ByteStringValue.FromString(projectID.GoLogString()), ExportCount = bucket.Index?.Entries.Count ?? 0, FileCount = bucket.Paths?.Count ?? 0, State = bucket.state, DependencyNames = bucket.DependencyNames, PackageNames = null });
        if (nodeModules != null)
            foreach (var(path, bucket)in nodeModules)
            {
                HashSet<ByteStringKey>? packageNames = null;
                int fileCount = 0;
                if (bucket.PackageFiles != null)
                {
                    packageNames = new(bucket.PackageFiles.Count);
                    foreach (var(name, paths)in bucket.PackageFiles)
                    {
                        packageNames.Add(name);
                        fileCount += paths?.Count ?? 0;
                    }
                }

                stats.NodeModulesBuckets = stats.NodeModulesBuckets.Append(new BucketStats { Name = path.AsValue(), ExportCount = bucket.Index?.Entries.Count ?? 0, FileCount = fileCount, State = bucket.state, DependencyNames = bucket.DependencyNames, PackageNames = packageNames });
            }

        GoSlices.SortFunc(stats.ProjectBuckets, static (a, b) => ByteStringKey.FromValue(a.Name).CompareTo(ByteStringKey.FromValue(b.Name)));
        GoSlices.SortFunc(stats.NodeModulesBuckets, static (a, b) => ByteStringKey.FromValue(a.Name).CompareTo(ByteStringKey.FromValue(b.Name)));
        return stats;
    }
}

// internal/ls/autoimport/registry.go:489. Embedded Set values retain their map owners.
internal struct RegistryChange
{
    internal PathKey RequestedFile;
    internal RootedFilePath RequestedFileName;
    internal Dictionary<PathKey, RootedFilePath>? OpenFiles;
    internal HashSet<DocumentUri>? Changed;
    internal HashSet<DocumentUri>? Created;
    internal HashSet<DocumentUri>? Deleted;
    // true: rebuilt with different file names; false: same names, new Program owner.
    internal Dictionary<IProjectID, bool>? RebuiltPrograms;
    internal UserPreferences? UserPreferences;
}

// internal/ls/autoimport/registry.go:506. Real snapshot/source/cache host, not a fixture replacement.
internal interface IRegistryCloneHost
{
    IFS FS();
    (IProjectID? ProjectID, CompilerProgram? Program) GetDefaultProject(PathKey path);
    CompilerProgram? GetProgramForProject(IProjectID projectID);
    InfoCacheEntry? GetPackageJson(RootedFilePath fileName);
    SourceFile? GetSourceFile(RootedFilePath fileName, PathKey path);
    void Dispose();
}

// registry.go:416
internal struct BucketStats
{
    internal ByteStringValue Name;
    internal int ExportCount, FileCount;
    internal BucketState State;
    internal HashSet<ByteStringKey>? DependencyNames, PackageNames;
}

internal sealed class CacheStats
{
    internal Slice<BucketStats> ProjectBuckets, NodeModulesBuckets;
    internal int UniquePackageCount;
}

// registry.go:1216
internal sealed class FailedAmbientModuleLookupSource
{
    internal readonly object mu = new();
    internal RootedFilePath fileName;
    internal ByteStringValue packageName;
}

internal sealed class BucketBuildResult
{
    internal Action<RegistryBucket> replaceBucket = null!;
    internal PathKey resolutionPath;
    internal Exception? error;
    internal RegistryBucket bucket = null!;
    internal Dictionary<PathKey, Slice<TypeScript.Modules.ResolvedEntrypoint>>? entrypoints;
    internal Slice<PathKey> removedEntrypointPaths;
    internal TypeScript.Collections.SyncMap<PathKey, FailedAmbientModuleLookupSource>? possibleFailedAmbientModuleLookupSources;
    internal SyncSet<ByteStringKey>? possibleFailedAmbientModuleLookupTargets;
}

// registry.go:1362
internal sealed class DiscoveredPackage
{
    internal ByteStringValue packageName;
    internal InfoCacheEntry packageJson = null!;
    internal RootedDirectoryPath realpath;
    internal InfoCacheEntry? typesPackageJson;
    internal RootedDirectoryPath typesRealpath;
    internal PathKey dirPath;
    internal bool isLocal;
}

// registry.go:1375
internal sealed class PerPackageExtractionResult
{
    internal Dictionary<PathKey, RootedFilePath>? packageFiles;
    internal Slice<TypeScript.Modules.ResolvedEntrypoint> entrypoints;
    internal Dictionary<PathKey, Slice<Export>>? exports;
    internal Dictionary<ByteStringKey, Slice<RootedFilePath>>? ambientModules;
    internal int statsExports, statsUsedChecker, skippedEntrypoints;
    internal bool isSymlinked;
    internal Dictionary<PathKey, FailedAmbientModuleLookupSource>? failedAmbientModuleLookupSources;
    internal HashSet<ByteStringKey>? failedAmbientModuleLookupTargets;
}

// registry.go:1389
internal sealed class PackageExtractionResult
{
    internal Dictionary<PathKey, Slice<Export>> exports = null!;
    internal Dictionary<ByteStringKey, Dictionary<PathKey, RootedFilePath>?> packageFiles = null!;
    internal Dictionary<ByteStringKey, Slice<RootedFilePath>> ambientModuleNames = null!;
    internal Slice<Slice<TypeScript.Modules.ResolvedEntrypoint>> entrypoints;
    internal HashSet<ByteStringKey>? workspacePackages;
    internal TypeScript.Collections.SyncMap<PathKey, FailedAmbientModuleLookupSource> possibleFailedAmbientModuleLookupSources = null!;
    internal SyncSet<ByteStringKey> possibleFailedAmbientModuleLookupTargets = null!;
    internal ExtractorStats stats = null!;
    internal int skippedEntrypointsCount;
}

// registry.go:797. Original local task owner survives its queued phase workers.
internal sealed class NodeModulesBucketTask
{
    internal TypeScript.ProjectSystem.Dirty.MapEntry<PathKey, RegistryBucket> entry = null!;
    internal HashSet<ByteStringKey>? dependencyNames;
    internal RootedDirectoryPath dirName;
    internal PathKey dirPath;
    internal bool isUpdate;
    internal RegistryBucket? existingBucket;
    internal HashSet<ByteStringKey>? dirtyPackages, packageNames, directoryPackageNames;
    internal Slice<DiscoveredPackage> discovered;
}

internal sealed partial class RegistryBuilder
{
    internal IRegistryCloneHost host;
    internal Registry original;
    internal RootedDirectoryPath currentDirectory;
    internal UserPreferences userPreferences;
    internal TypeScript.ProjectSystem.Dirty.Map<PathKey, RegistryDirectory> directories;
    internal TypeScript.ProjectSystem.Dirty.Map<PathKey, RegistryBucket> nodeModules;
    internal TypeScript.ProjectSystem.Dirty.Map<IProjectID, RegistryBucket> projects;
    internal TypeScript.ProjectSystem.Dirty.MapBuilder<PathKey, TypeScript.Collections.SyncMap<PathKey, ModuleSpecifier>, TypeScript.Collections.SyncMap<PathKey, ModuleSpecifier>> specifierCache;
    // Existing C# options class supplies the embedded Go zero-value struct owner.
    internal TypeScript.Modules.ResolverOptions resolverOptions = new();
    internal int uniquePackageCount;
    internal TypeScript.ProjectSystem.Dirty.MapBuilder<PathKey, Slice<TypeScript.Modules.ResolvedEntrypoint>, Slice<TypeScript.Modules.ResolvedEntrypoint>> entrypoints;
    // internal/ls/autoimport/registry.go:525
    private RegistryBuilder(Registry registry, IRegistryCloneHost host, RootedDirectoryPath currentDirectory)
    {
        this.host = host;
        original = registry;
        this.currentDirectory = currentDirectory;
        userPreferences = registry.userPreferences;
        directories = new(registry.directories);
        nodeModules = new(registry.nodeModules);
        projects = new(registry.projects);
        specifierCache = new(registry.specifierCache, static value => value, static value => value);
        uniquePackageCount = registry.uniquePackageCount;
        entrypoints = new(registry.entrypoints, static value => value, static value => value);
    }

    internal static RegistryBuilder NewRegistryBuilder(Registry registry, IRegistryCloneHost host, RootedDirectoryPath currentDirectory) => new(registry, host, currentDirectory);
    // internal/ls/autoimport/registry.go:541. Finalize preserves unchanged map identity and nil headers.
    internal Registry Build() => new()
    {
        caseSensitivity = original.caseSensitivity,
        userPreferences = userPreferences,
        directories = directories.FinalizeValue().Value!,
        nodeModules = nodeModules.FinalizeValue().Value,
        projects = projects.FinalizeValue().Value,
        specifierCache = specifierCache.Build(),
        uniquePackageCount = uniquePackageCount,
        entrypoints = entrypoints.Build()
    };
    // internal/ls/autoimport/registry.go:554
    internal void UpdateBucketAndDirectoryExistence(RegistryChange change, TypeScript.ProjectSystem.Logging.LogTree? logger)
    {
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        var neededProjects = new Dictionary<IProjectID, byte>();
        var neededDirectories = new Dictionary<PathKey, RootedDirectoryPath>();
        void AddNeededDirectories(PathKey path, RootedFilePath fileName)
        {
            if (fileName.IsDynamic())
                return;
            var directory = fileName.Directory();
            var directoryPath = path.Parent();
            for (;;)
            {
                if (neededDirectories.ContainsKey(directoryPath))
                    break;
                neededDirectories[directoryPath] = directory;
                var parentPath = directoryPath.Parent();
                var parentDirectory = directory.AsPath().Directory();
                if (parentPath == directoryPath || parentDirectory == directory)
                    break;
                directoryPath = parentPath;
                directory = parentDirectory;
            }
        }

        if (change.OpenFiles != null)
            foreach (var(path, fileName)in change.OpenFiles)
            {
                var(projectID, _) = host.GetDefaultProject(path);
                if (projectID != null)
                    neededProjects[projectID] = 0;
                AddNeededDirectories(path, fileName);
                if (!specifierCache.Has(path))
                    specifierCache.Set(path, new());
            }

        if (change.RequestedFile != default)
        {
            AddNeededDirectories(change.RequestedFile, change.RequestedFileName);
            var(projectID, _) = host.GetDefaultProject(change.RequestedFile);
            if (projectID != null)
                neededProjects[projectID] = 0;
            if (!specifierCache.Has(change.RequestedFile))
                specifierCache.Set(change.RequestedFile, new());
        }

        if (original.specifierCache != null)
            foreach (var path in original.specifierCache.Keys)
                if (change.OpenFiles?.ContainsKey(path) != true && path != change.RequestedFile)
                    specifierCache.Delete(path);
        Slice<IProjectID> addedProjects = default, removedProjects = default;
        Core.DiffMapsFunc(original.projects, neededProjects, static (_, _) => throw new InvalidOperationException("never called because onChanged is nil"), (projectID, _) =>
        {
            projects.Add(projectID, RegistryBucket.NewRegistryBucket());
            addedProjects = addedProjects.Append(projectID);
        }, (projectID, _) =>
        {
            projects.Delete(projectID);
            removedProjects = removedProjects.Append(projectID);
        }, null);
        if (logger != null)
        {
            foreach (var projectID in addedProjects)
                logger.Logf("Added project: %s", projectID);
            foreach (var projectID in removedProjects)
                logger.Logf("Removed project: %s", projectID);
        }

        void UpdateDirectory(PathKey path, RootedDirectoryPath name, bool packageJsonChanged)
        {
            var packageJsonFileName = name.ResolveFile("package.json");
            bool hasNodeModules = host.FS().DirectoryExists(name.ResolveDirectory("node_modules"));
            var(entry, ok) = directories.Get(path);
            if (ok)
                entry!.ChangeIf(directory => packageJsonChanged || directory.hasNodeModules != hasNodeModules, directory =>
                {
                    directory.packageJson = host.GetPackageJson(packageJsonFileName);
                    directory.hasNodeModules = hasNodeModules;
                });
            else
                directories.Add(path, new() { name = name, packageJson = host.GetPackageJson(packageJsonFileName), hasNodeModules = hasNodeModules });
            if (hasNodeModules)
            {
                if (!nodeModules.Get(path).Ok)
                    nodeModules.Add(path, RegistryBucket.NewRegistryBucket());
            }
            else
                nodeModules.TryDelete(path);
        }

        Slice<PathKey> addedNodeModulesDirectories = default, removedNodeModulesDirectories = default;
        bool PackageJsonChanged(RootedDirectoryPath name)
        {
            var uri = TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(name.ResolveFile("package.json"));
            return change.Changed?.Contains(uri) == true || change.Deleted?.Contains(uri) == true || change.Created?.Contains(uri) == true;
        }

        Core.DiffMapsFunc(original.directories, neededDirectories, (directory, name) => !PackageJsonChanged(name) && directory.hasNodeModules == host.FS().DirectoryExists(name.ResolveDirectory("node_modules")), (path, name) =>
        {
            bool hadNodeModules = original.nodeModules?.ContainsKey(path) == true;
            UpdateDirectory(path, name, false);
            logger?.Logf("Added directory: %s", path);
            if (nodeModules.Get(path).Ok && !hadNodeModules)
                addedNodeModulesDirectories = addedNodeModulesDirectories.Append(path);
        }, (path, directory) =>
        {
            bool hadNodeModules = original.nodeModules?.ContainsKey(path) == true;
            directories.Delete(path);
            nodeModules.TryDelete(path);
            logger?.Logf("Removed directory: %s", path);
            if (hadNodeModules)
                removedNodeModulesDirectories = removedNodeModulesDirectories.Append(path);
        }, (path, directory, name) =>
        {
            UpdateDirectory(path, name, PackageJsonChanged(name));
            logger?.Logf("Changed directory: %s", path);
        });
        if (logger != null)
        {
            foreach (var path in addedNodeModulesDirectories)
                logger.Logf("Added node_modules bucket: %s", path);
            foreach (var path in removedNodeModulesDirectories)
                logger.Logf("Removed node_modules bucket: %s", path);
            logger.Logf("Updated buckets and directories in %v", System.Diagnostics.Stopwatch.GetElapsedTime(start));
        }
    }

    // internal/ls/autoimport/registry.go:713
    internal void MarkBucketsDirty(RegistryChange change, TypeScript.ProjectSystem.Logging.LogTree? logger)
    {
        if (change.RebuiltPrograms != null)
            foreach (var(projectID, newFileNames)in change.RebuiltPrograms)
            {
                var(bucket, ok) = projects.Get(projectID);
                if (ok)
                    bucket!.Change(value => value.state.newProgramStructure = newFileNames ? NewProgramStructure.DifferentFileNames : NewProgramStructure.SameFileNames);
            }

        var cleanNodeModulesBuckets = new HashSet<PathKey>();
        var cleanProjectBuckets = new HashSet<IProjectID>();
        nodeModules.Range(entry =>
        {
            if (!entry.Value().state.multipleFilesDirty)
                cleanNodeModulesBuckets.Add(entry.Key());
            return true;
        });
        projects.Range(entry =>
        {
            if (!entry.Value().state.multipleFilesDirty)
                cleanProjectBuckets.Add(entry.Key());
            return true;
        });
        void MarkFilesDirty(HashSet<TypeScript.Lsp.Protocol.DocumentUri>? uris)
        {
            if (cleanNodeModulesBuckets.Count == 0 && cleanProjectBuckets.Count == 0)
                return;
            if (uris == null)
                return;
            foreach (var uri in uris)
            {
                var path = original.caseSensitivity.PathKey(uri.FileName().AsPath());
                if (cleanNodeModulesBuckets.Count > 0)
                {
                    var(directoryPath, _, ok) = path.SplitAtCanonicalComponent("node_modules");
                    if (ok)
                    {
                        if (cleanNodeModulesBuckets.Contains(directoryPath))
                        {
                            var entry = nodeModules.Get(directoryPath).Entry!;
                            var paths = entry.Value().Paths;
                            ByteStringValue packageName = paths != null && paths.TryGetValue(path, out var value) ? value : default;
                            entry.Change(bucket => bucket.MarkNodeModulesDirty(packageName));
                            if (!entry.Value().state.multipleFilesDirty)
                                cleanNodeModulesBuckets.Remove(directoryPath);
                        }
                    }
                    else
                    {
                        foreach (var bucketDirectoryPath in cleanNodeModulesBuckets)
                        {
                            var entry = nodeModules.Get(bucketDirectoryPath).Entry!;
                            if (entry.Value().Paths is { } paths && paths.TryGetValue(path, out var packageName))
                            {
                                entry.Change(bucket => bucket.MarkNodeModulesDirty(packageName));
                                if (!entry.Value().state.multipleFilesDirty)
                                    cleanNodeModulesBuckets.Remove(bucketDirectoryPath);
                            }
                        }
                    }
                }

                foreach (var projectID in cleanProjectBuckets)
                {
                    var entry = projects.Get(projectID).Entry!;
                    if (entry.Value().Paths?.ContainsKey(path) == true)
                    {
                        entry.Change(bucket => bucket.MarkProjectFileDirty(path));
                        if (!entry.Value().state.multipleFilesDirty)
                            cleanProjectBuckets.Remove(projectID);
                    }
                }
            }
        }

        MarkFilesDirty(change.Created);
        MarkFilesDirty(change.Deleted);
        MarkFilesDirty(change.Changed);
    }

    // internal/ls/autoimport/registry.go:1327
    internal HashSet<ByteStringKey>? ComputeDependenciesForNodeModulesDirectory(RegistryChange change, Dictionary<IProjectID, HashSet<ByteStringKey>> allResolvedPackageNames, RootedDirectoryPath directoryName, PathKey directoryPath)
    {
        if (change.OpenFiles != null)
            foreach (var path in change.OpenFiles.Keys)
                if (directoryPath.ContainsPath(path) && GetNearestAncestorDirectoryWithPackageJson(path) == null)
                    return null;
        var dependencies = new HashSet<ByteStringKey>();
        directories.Range(entry =>
        {
            if (TypeScript.PackageJson.InfoCacheEntryOperations.Exists(entry.Value().packageJson) && directoryPath.ContainsPath(entry.Key()))
                AutoImport.AddPackageJsonDependencies(entry.Value().packageJson!.Contents!, dependencies);
            return true;
        });
        foreach (var resolvedPackageNames in allResolvedPackageNames.Values)
            foreach (var name in resolvedPackageNames)
                dependencies.Add(name);
        return dependencies;
    }

    // internal/ls/autoimport/registry.go:1403
    internal Slice<DiscoveredPackage> DiscoverBucketPackages(HashSet<ByteStringKey>? packageNames, RootedDirectoryPath directoryName, PathKey directoryPath)
    {
        var result = Slice<DiscoveredPackage>.Make(0, packageNames?.Count ?? 0);
        if (packageNames == null)
            return result;
        foreach (var packageNameKey in packageNames)
        {
            var packageName = packageNameKey.Value;
            var typesPackageName = TypeScript.Modules.ModuleUtilities.GetTypesPackageNameValue(packageName);
            var packageJson = host.GetPackageJson(directoryName.ResolveFile(TsPath.CombinePaths(ByteStringValue.FromString("node_modules"), (ReadOnlySpan<ByteStringValue>)[packageName, ByteStringValue.FromString("package.json")])))!;
            TypeScript.PackageJson.InfoCacheEntry? typesPackageJson = null;
            if (!packageName.EqualsValue(typesPackageName))
            {
                var typesJson = host.GetPackageJson(directoryName.ResolveFile(TsPath.CombinePaths(ByteStringValue.FromString("node_modules"), (ReadOnlySpan<ByteStringValue>)[typesPackageName, ByteStringValue.FromString("package.json")])));
                if (typesJson!.DirectoryExists)
                    typesPackageJson = typesJson;
            }

            RootedDirectoryPath realpath = default, typesRealpath = default;
            if (packageJson.DirectoryExists)
                realpath = TsPath.RootedDirectoryPathFromPath(host.FS().Realpath(packageJson.PackageDirectory.AsDirectoryPath().AsPath()));
            if (typesPackageJson != null)
                typesRealpath = TsPath.RootedDirectoryPathFromPath(host.FS().Realpath(typesPackageJson.PackageDirectory.AsDirectoryPath().AsPath()));
            var sensitivity = host.FS().CaseSensitivity();
            bool isLocal = realpath != default && !realpath.ContainsLowercaseDirectorySequence("/node_modules/") && sensitivity.PathKey(currentDirectory.AsPath()).ContainsPath(sensitivity.PathKey(realpath.AsPath()));
            result = result.Append(new DiscoveredPackage { packageName = packageName, packageJson = packageJson, realpath = realpath, typesPackageJson = typesPackageJson, typesRealpath = typesRealpath, dirPath = directoryPath, isLocal = isLocal });
        }

        return result;
    }

    // internal/ls/autoimport/registry.go:1835
    internal RegistryDirectory? GetNearestAncestorDirectoryWithPackageJson(PathKey filePath) => filePath.Parent().ForEachAncestorDirectory<RegistryDirectory?>(path =>
    {
        var(entry, ok) = directories.Get(path);
        return ok && TypeScript.PackageJson.InfoCacheEntryOperations.Exists(entry!.Value().packageJson) ? (entry.Value(), true) : (null, false);
    }).Result;
    // internal/ls/autoimport/registry.go:1844
    internal Slice<RootedFilePath> ResolveAmbientModuleName(ByteStringValue moduleName, PathKey fromPath) => fromPath.ForEachAncestorDirectory<Slice<RootedFilePath>>(path =>
    {
        var(bucket, ok) = nodeModules.Get(path);
        if (ok && bucket!.Value().AmbientModuleNames is { } names && names.TryGetValue(ByteStringKey.FromValue(moduleName), out var files))
            return (files, true);
        return (default, false);
    }).Result;
    // internal/ls/autoimport/extract.go:73
    internal ExportExtractor NewExportExtractor(ByteStringValue packageName, TypeScript.Checking.Checker checker, TypeScript.Modules.IResolver moduleResolver, Func<RootedFilePath, RootedFilePath>? realpath) => ExportExtractor.NewExportExtractor(packageName, checker, original.caseSensitivity, moduleResolver, realpath);
    // internal/ls/autoimport/registry.go:1449
    internal PerPackageExtractionResult? ExtractPackage(CancellationToken cancellation, TypeScript.PackageJson.InfoCacheEntry? packageJson, ByteStringValue packageName, Dictionary<PathKey, RootedFilePath> projectReferenceOutputs, TypeScript.Vfs.VfsMatch.SpecMatcher? fileExcludePatterns, bool enableDirectorySearch)
    {
        if (packageJson == null || !packageJson.DirectoryExists)
            return null;
        var(toRealpath, toSymlink) = AutoImport.GetPackageRealpathFuncs(host.FS(), packageJson.PackageDirectory.AsDirectoryPath());
        var resolver = AutoImport.GetModuleResolver(host, currentDirectory, toRealpath, resolverOptions);
        var packageEntrypoints = resolver.GetEntrypointsFromPackageJsonInfo(packageJson, packageName, enableDirectorySearch);
        if (packageEntrypoints.IsNil)
            return null;
        int skippedEntrypoints = 0;
        if (fileExcludePatterns != null)
        {
            int count = packageEntrypoints.Count;
            packageEntrypoints = GoSlices.DeleteFunc(packageEntrypoints, entrypoint => fileExcludePatterns.MatchFileName(entrypoint.ResolvedFileName));
            skippedEntrypoints = count - packageEntrypoints.Count;
        }

        if (packageEntrypoints.Count == 0)
            return null;
        var result = new PerPackageExtractionResult
        {
            packageFiles = new(),
            entrypoints = packageEntrypoints,
            exports = new(),
            ambientModules = new(),
            skippedEntrypoints = skippedEntrypoints,
            failedAmbientModuleLookupSources = new(),
            failedAmbientModuleLookupTargets = new()
        };
        var seenFiles = new HashSet<PathKey>(packageEntrypoints.Count);
        var rootFiles = Slice<SourceFile>.Make(packageEntrypoints.Count);
        var symlinks = new Dictionary<PathKey, PathAndFileName>();
        var work = Core.NewWorkGroup(false);
        for (int i = 0; i < packageEntrypoints.Count; i++)
        {
            int index = i;
            var entrypoint = packageEntrypoints[i];
            var fileName = entrypoint.SymlinkOrRealpath();
            var realpathFileName = entrypoint.ResolvedFileName;
            var realpathPath = original.caseSensitivity.PathKey(realpathFileName.AsPath());
            if (projectReferenceOutputs.TryGetValue(realpathPath, out var inputFileName))
            {
                fileName = toSymlink(inputFileName);
                realpathFileName = inputFileName;
                realpathPath = original.caseSensitivity.PathKey(realpathFileName.AsPath());
            }

            if (!seenFiles.Add(realpathPath))
                continue;
            if (fileName != realpathFileName)
            {
                symlinks[realpathPath] = new(original.caseSensitivity.PathKey(fileName.AsPath()), fileName);
                result.isSymlinked = true;
            }

            work.Queue(() =>
            {
                var file = host.GetSourceFile(realpathFileName, realpathPath);
                if (file != null)
                    Binder.BindSourceFile(file);
                rootFiles[index] = file!;
            });
        }

        work.RunAndWait();
        rootFiles = GoSlices.DeleteFunc(rootFiles, static file => file == null);
        var aliasResolver = new AliasResolver(rootFiles, symlinks, host, resolver, (source, moduleName) =>
        {
            result.failedAmbientModuleLookupTargets.Add(ByteStringKey.FromValue(moduleName));
            var sourcePath = source is SourceFile file ? PathKey.FromValue(file.PathKeyValue()) : PathKey.FromGoString(source.PathKey());
            if (!result.failedAmbientModuleLookupSources.ContainsKey(sourcePath))
                result.failedAmbientModuleLookupSources[sourcePath] = new()
                {
                    fileName = source is SourceFile sourceFile ? RootedFilePath.FromValue(sourceFile.FileNameGoValue()) : RootedFilePath.FromGoString(source.FileName())
                };
        });
        var checker = TypeScript.Checking.Checker.NewChecker(aliasResolver).Checker;
        var extractor = NewExportExtractor(packageName, checker, resolver, toRealpath);
        HashSet<PathKey>? nonModuleFiles = null;
        foreach (var entrypoint in aliasResolver.SourceFiles)
        {
            if (cancellation.IsCancellationRequested)
                return null;
            var fileExports = extractor.ExtractFromFile(entrypoint);
            foreach (var name in entrypoint.AmbientModuleNames)
            {
                var key = ByteStringKey.FromValue(name);
                result.ambientModules.TryGetValue(key, out var files);
                result.ambientModules[key] = files.Append(RootedFilePath.FromValue(entrypoint.FileNameGoValue()));
            }

            result.packageFiles[PathKey.FromValue(entrypoint.PathKeyValue())] = RootedFilePath.FromValue(entrypoint.FileNameGoValue());
            bool hasSymlink = aliasResolver.Symlinks!.TryGetValue(PathKey.FromValue(entrypoint.PathKeyValue()), out var symlink);
            if (hasSymlink)
                result.packageFiles[symlink.path] = symlink.fileName;
            bool hasExports = fileExports.Count > 0 && entrypoint.ExternalModuleIndicator != null;
            if (!result.failedAmbientModuleLookupSources.TryGetValue(PathKey.FromValue(entrypoint.PathKeyValue()), out var source))
                result.exports[PathKey.FromValue(entrypoint.PathKeyValue())] = fileExports;
            else
            {
                source.packageName = packageName;
                hasExports = entrypoint.ExternalModuleIndicator != null;
            }

            if (!hasExports)
            {
                (nonModuleFiles ??= new()).Add(PathKey.FromValue(entrypoint.PathKeyValue()));
                if (hasSymlink)
                    nonModuleFiles.Add(symlink.path);
            }
        }

        result.entrypoints = GoSlices.DeleteFunc(result.entrypoints, entrypoint => nonModuleFiles?.Contains(entrypoint.ResolvedPath) == true);
        var stats = extractor.Stats();
        result.statsExports = Volatile.Read(ref stats.exports);
        result.statsUsedChecker = Volatile.Read(ref stats.usedChecker);
        return result;
    }

    // internal/ls/autoimport/registry.go:1580
    internal static PackageExtractionResult InstallExtractions(Slice<DiscoveredPackage> discovered, Dictionary<RootedDirectoryPath, PerPackageExtractionResult> extractionCache)
    {
        var result = new PackageExtractionResult
        {
            exports = new(),
            packageFiles = new(),
            ambientModuleNames = new(),
            workspacePackages = new(),
            possibleFailedAmbientModuleLookupSources = new(),
            possibleFailedAmbientModuleLookupTargets = new(),
            stats = new()
        };
        foreach (var package in discovered)
        {
            if (!extractionCache.TryGetValue(package.realpath, out var extraction))
                extractionCache.TryGetValue(package.typesRealpath, out extraction);
            if (extraction == null)
                continue;
            if (extraction.exports != null)
                foreach (var(path, exports)in extraction.exports)
                    result.exports[path] = exports;
            var packageName = ByteStringKey.FromValue(package.packageName);
            if (!result.packageFiles.TryGetValue(packageName, out var packageFiles) || packageFiles == null)
                result.packageFiles[packageName] = packageFiles = new(extraction.packageFiles?.Count ?? 0);
            if (extraction.packageFiles != null)
                foreach (var(path, fileName)in extraction.packageFiles)
                    packageFiles[path] = fileName;
            if (extraction.ambientModules != null)
                foreach (var(name, fileNames)in extraction.ambientModules)
                {
                    result.ambientModuleNames.TryGetValue(name, out var previous);
                    result.ambientModuleNames[name] = previous.Append(fileNames);
                }

            if (!extraction.entrypoints.IsNil)
                result.entrypoints = result.entrypoints.Append(extraction.entrypoints);
            if (extraction.failedAmbientModuleLookupSources != null)
                foreach (var(path, source)in extraction.failedAmbientModuleLookupSources)
                    result.possibleFailedAmbientModuleLookupSources.LoadOrStore(path, source);
            if (extraction.failedAmbientModuleLookupTargets != null)
                foreach (var target in extraction.failedAmbientModuleLookupTargets)
                    result.possibleFailedAmbientModuleLookupTargets.Add(target);
            if (extraction.isSymlinked && package.isLocal)
                result.workspacePackages.Add(packageName);
            Interlocked.Add(ref result.stats.exports, extraction.statsExports);
            Interlocked.Add(ref result.stats.usedChecker, extraction.statsUsedChecker);
            result.skippedEntrypointsCount += extraction.skippedEntrypoints;
        }

        return result;
    }

    // internal/ls/autoimport/registry.go:1629
    internal void BuildNodeModulesBucket(CancellationToken cancellation, BucketBuildResult result, HashSet<ByteStringKey>? dependencies, PathKey directoryPath, Slice<DiscoveredPackage> discovered, HashSet<ByteStringKey>? directoryPackageNames, Dictionary<RootedDirectoryPath, PerPackageExtractionResult> extractionCache, HashSet<ByteStringKey>? recursiveSearchPackages, TypeScript.ProjectSystem.Logging.LogTree? logger)
    {
        if (cancellation.IsCancellationRequested)
        {
            result.error = new OperationCanceledException(cancellation);
            return;
        }

        var extraction = InstallExtractions(discovered, extractionCache);
        long indexStart = System.Diagnostics.Stopwatch.GetTimestamp();
        var allPackageFiles = new Dictionary<ByteStringKey, Dictionary<PathKey, RootedFilePath>?>(directoryPackageNames?.Count ?? 0);
        if (directoryPackageNames != null)
            foreach (var name in directoryPackageNames)
            {
                extraction.packageFiles.TryGetValue(name, out var files);
                allPackageFiles[name] = files;
            }

        var paths = new Dictionary<PathKey, ByteStringValue>();
        foreach (var packageName in extraction.workspacePackages!)
            if (extraction.packageFiles.TryGetValue(packageName, out var files) && files != null)
                foreach (var path in files.Keys)
                    paths[path] = packageName.Value;
        result.bucket = new()
        {
            Index = new(),
            DependencyNames = dependencies,
            PackageFiles = allPackageFiles,
            AmbientModuleNames = extraction.ambientModuleNames,
            Paths = paths,
            state = new()
            {
                buildPreferences = BucketBuildPreferences.FromUserPreferences(userPreferences),
                recursiveSearchPackages = recursiveSearchPackages == null ? null : new(recursiveSearchPackages, recursiveSearchPackages.Comparer)
            }
        };
        result.entrypoints = new(extraction.exports.Count);
        result.possibleFailedAmbientModuleLookupSources = extraction.possibleFailedAmbientModuleLookupSources;
        result.possibleFailedAmbientModuleLookupTargets = extraction.possibleFailedAmbientModuleLookupTargets;
        foreach (var fileExports in extraction.exports.Values)
            foreach (var export in fileExports)
                result.bucket.Index.InsertAsWords(export);
        foreach (var entrypointSet in extraction.entrypoints)
            foreach (var entrypoint in entrypointSet)
            {
                result.entrypoints.TryGetValue(entrypoint.ResolvedPath, out var previous);
                result.entrypoints[entrypoint.ResolvedPath] = previous.Append(entrypoint);
            }

        var(oldEntry, ok) = nodeModules.Get(directoryPath);
        if (ok && oldEntry!.Value().PackageFiles is { } oldPackages)
            foreach (var files in oldPackages.Values)
                if (files != null)
                    foreach (var path in files.Keys)
                        if (original.entrypoints?.ContainsKey(path) == true)
                            result.removedEntrypointPaths = result.removedEntrypointPaths.Append(path);
        if (logger != null)
        {
            logger.Logf("Installed %d exports (%d used checker)", Volatile.Read(ref extraction.stats.exports), Volatile.Read(ref extraction.stats.usedChecker));
            if (extraction.skippedEntrypointsCount > 0)
                logger.Logf("Skipped %d entrypoints due to exclude patterns", extraction.skippedEntrypointsCount);
            logger.Logf("Built index: %v", System.Diagnostics.Stopwatch.GetElapsedTime(indexStart));
        }

        result.error = cancellation.IsCancellationRequested ? new OperationCanceledException(cancellation) : null;
    }

    // internal/ls/autoimport/registry.go:1717
    internal void UpdateNodeModulesBucket(CancellationToken cancellation, BucketBuildResult result, RegistryBucket existingBucket, HashSet<ByteStringKey> dirtyPackages, Slice<DiscoveredPackage> discovered, Dictionary<RootedDirectoryPath, PerPackageExtractionResult> extractionCache, HashSet<ByteStringKey>? recursiveSearchPackages, TypeScript.ProjectSystem.Logging.LogTree? logger)
    {
        if (cancellation.IsCancellationRequested)
        {
            result.error = new OperationCanceledException(cancellation);
            return;
        }

        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        var extraction = InstallExtractions(discovered, extractionCache);
        long indexStart = System.Diagnostics.Stopwatch.GetTimestamp();
        var newIndex = Index<Export>.CloneNullable(existingBucket.Index, export => !dirtyPackages.Contains(ByteStringKey.FromValue(export.PackageName)));
        var newPackageFiles = existingBucket.PackageFiles == null ? null : new Dictionary<ByteStringKey, Dictionary<PathKey, RootedFilePath>?>(existingBucket.PackageFiles, existingBucket.PackageFiles.Comparer);
        foreach (var name in dirtyPackages)
            newPackageFiles?.Remove(name);
        foreach (var(name, files)in extraction.packageFiles)
            newPackageFiles![name] = files;
        var newPaths = new Dictionary<PathKey, ByteStringValue>(existingBucket.Paths?.Count ?? 0);
        if (existingBucket.Paths != null)
            foreach (var(path, name)in existingBucket.Paths)
                if (!dirtyPackages.Contains(ByteStringKey.FromValue(name)))
                    newPaths[path] = name;
        foreach (var name in extraction.workspacePackages!)
            if (extraction.packageFiles.TryGetValue(name, out var files) && files != null)
                foreach (var path in files.Keys)
                    newPaths[path] = name.Value;
        var newAmbientModuleNames = new Dictionary<ByteStringKey, Slice<RootedFilePath>>(existingBucket.AmbientModuleNames?.Count ?? 0);
        if (existingBucket.AmbientModuleNames != null)
            foreach (var(name, fileNames)in existingBucket.AmbientModuleNames)
            {
                Slice<RootedFilePath> filtered = default;
                foreach (var fileName in fileNames)
                {
                    var path = original.caseSensitivity.PathKey(fileName.AsPath());
                    if (existingBucket.Paths != null && existingBucket.Paths.TryGetValue(path, out var packageName) && dirtyPackages.Contains(ByteStringKey.FromValue(packageName)))
                        continue;
                    filtered = filtered.Append(fileName);
                }

                if (filtered.Count > 0)
                    newAmbientModuleNames[name] = filtered;
            }

        foreach (var(name, fileNames)in extraction.ambientModuleNames)
        {
            newAmbientModuleNames.TryGetValue(name, out var previous);
            newAmbientModuleNames[name] = previous.Append(fileNames);
        }

        Slice<PathKey> removedEntrypointPaths = default;
        if (original.entrypoints != null)
            foreach (var path in original.entrypoints.Keys)
                if (existingBucket.Paths != null && existingBucket.Paths.TryGetValue(path, out var packageName) && dirtyPackages.Contains(ByteStringKey.FromValue(packageName)))
                    removedEntrypointPaths = removedEntrypointPaths.Append(path);
        var newEntrypoints = new Dictionary<PathKey, Slice<TypeScript.Modules.ResolvedEntrypoint>>();
        foreach (var entrypointSet in extraction.entrypoints)
            foreach (var entrypoint in entrypointSet)
            {
                newEntrypoints.TryGetValue(entrypoint.ResolvedPath, out var previous);
                newEntrypoints[entrypoint.ResolvedPath] = previous.Append(entrypoint);
            }

        foreach (var fileExports in extraction.exports.Values)
            foreach (var export in fileExports)
                newIndex!.InsertAsWords(export);
        result.bucket = new()
        {
            Index = newIndex,
            DependencyNames = existingBucket.DependencyNames,
            PackageFiles = newPackageFiles,
            AmbientModuleNames = newAmbientModuleNames,
            Paths = newPaths,
            state = new()
            {
                buildPreferences = BucketBuildPreferences.FromUserPreferences(userPreferences),
                recursiveSearchPackages = recursiveSearchPackages == null ? null : new(recursiveSearchPackages, recursiveSearchPackages.Comparer)
            }
        };
        result.entrypoints = newEntrypoints;
        result.removedEntrypointPaths = removedEntrypointPaths;
        result.possibleFailedAmbientModuleLookupSources = extraction.possibleFailedAmbientModuleLookupSources;
        result.possibleFailedAmbientModuleLookupTargets = extraction.possibleFailedAmbientModuleLookupTargets;
        if (logger != null)
        {
            logger.Logf("Granular update of %d packages: %v (%d exports)", dirtyPackages.Count, System.Diagnostics.Stopwatch.GetElapsedTime(start, indexStart), Volatile.Read(ref extraction.stats.exports));
            logger.Logf("Built index: %v", System.Diagnostics.Stopwatch.GetElapsedTime(indexStart));
        }

        result.error = cancellation.IsCancellationRequested ? new OperationCanceledException(cancellation) : null;
    }

    // internal/ls/autoimport/registry.go:1240
    internal void BuildProjectBucket(CancellationToken cancellation, BucketBuildResult result, IProjectID projectID, HashSet<ByteStringKey>? resolvedPackageNames, TypeScript.ProjectSystem.Logging.LogTree? logger)
    {
        if (cancellation.IsCancellationRequested)
        {
            result.error = new OperationCanceledException(cancellation);
            return;
        }

        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        object mutex = new();
        var fileExcludePatterns = userPreferences.ParsedAutoImportFileExcludePatterns(host.FS().CaseSensitivity());
        result.bucket = new();
        var copiedOptions = CopyResolverOptions(resolverOptions);
        copiedOptions.Host = new AutoImportResolutionHost(host.FS(), currentDirectory);
        copiedOptions.CompilerOptions = CompilerOptionHelpers.EmptyCompilerOptions;
        var moduleResolver = TypeScript.Modules.DefaultResolver.NewResolver(copiedOptions);
        var program = host.GetProgramForProject(projectID)!;
        var projectRootPath = original.caseSensitivity.PathKey(program.BaseDirectory().AsPath());
        var symlinkCache = program.GetSymlinkCache();
        var(getChecker, closePool, checkerCount) = AutoImport.CreateCheckerPool(program);
        try
        {
            var exports = new Dictionary<PathKey, Slice<Export>>();
            var work = Core.NewWorkGroup(false);
            int skippedFileCount = 0;
            var combinedStats = new ExtractorStats();
            foreach (var file in program.GetSourceFiles())
            {
                if (file.IsContentMapperSupplemental() || AutoImport.IsIgnoredFile(program, file))
                    continue;
                if (fileExcludePatterns != null && fileExcludePatterns.MatchFileName(RootedFilePath.FromValue(file.FileNameGoValue())))
                {
                    skippedFileCount++;
                    continue;
                }

                if (file.ContentMapper() == "" && (RootedFilePath.FromValue(file.FileNameGoValue()).ContainsLowercaseDirectorySequence("/node_modules/") || AutoImport.HasSymlinkToNodeModules(PathKey.FromValue(file.PathKeyValue()), projectRootPath, symlinkCache)))
                    continue;
                work.Queue(() =>
                {
                    if (cancellation.IsCancellationRequested)
                        return;
                    var(checker, done) = getChecker();
                    try
                    {
                        var extractor = NewExportExtractor(default, checker, moduleResolver, null);
                        var fileExports = extractor.ExtractFromFile(file);
                        lock (mutex)
                            exports[PathKey.FromValue(file.PathKeyValue())] = fileExports;
                        var stats = extractor.Stats();
                        Interlocked.Add(ref combinedStats.exports, Volatile.Read(ref stats.exports));
                        Interlocked.Add(ref combinedStats.usedChecker, Volatile.Read(ref stats.usedChecker));
                    }
                    finally
                    {
                        done();
                    }
                });
            }

            work.RunAndWait();
            long indexStart = System.Diagnostics.Stopwatch.GetTimestamp();
            var index = new Index<Export>();
            var paths = new Dictionary<PathKey, ByteStringValue>(exports.Count);
            foreach (var(path, fileExports)in exports)
            {
                paths[path] = default;
                foreach (var export in fileExports)
                    index.InsertAsWords(export);
            }

            result.bucket.Paths = paths;
            result.bucket.Index = index;
            result.bucket.ResolvedPackageNames = resolvedPackageNames;
            result.bucket.state.buildPreferences = BucketBuildPreferences.FromUserPreferences(userPreferences);
            if (logger != null)
            {
                logger.Logf("Extracted exports: %v (%d exports, %d used checker, %d created checkers)", System.Diagnostics.Stopwatch.GetElapsedTime(start, indexStart), Volatile.Read(ref combinedStats.exports), Volatile.Read(ref combinedStats.usedChecker), checkerCount());
                if (skippedFileCount > 0)
                    logger.Logf("Skipped %d files due to exclude patterns", skippedFileCount);
                logger.Logf("Built index: %v", System.Diagnostics.Stopwatch.GetElapsedTime(indexStart));
                logger.Logf("Bucket total: %v", System.Diagnostics.Stopwatch.GetElapsedTime(start));
            }
        }
        finally
        {
            closePool();
        }
    }

    // The original ResolverOptions parameter/assignment is a value copy. Existing
    // managed class fields are copied once; referenced caches/slices remain shared.
    private static TypeScript.Modules.ResolverOptions CopyResolverOptions(TypeScript.Modules.ResolverOptions options) => new()
    {
        Host = options.Host,
        CompilerOptions = options.CompilerOptions,
        TypingsLocation = options.TypingsLocation,
        ProjectNameValue = options.ProjectNameValue,
        ExtraExtensions = options.ExtraExtensions,
        PackageJsonCache = options.PackageJsonCache
    };
    // internal/ls/autoimport/registry.go:796. The same wait-group owner is reused
    // only after each successful phase has quiesced; no concurrent external Add/Wait.
    internal void UpdateIndexes(CancellationToken cancellation, RegistryChange change, TypeScript.ProjectSystem.Logging.LogTree? logger)
    {
        var(projectID, _) = host.GetDefaultProject(change.RequestedFile);
        if (projectID == null)
            return;
        var work = new ParallelWorkGroup(reusable: true);
        var allResolvedPackageNames = new Dictionary<IProjectID, HashSet<ByteStringKey>>();
        var projectReferenceOutputs = new Dictionary<PathKey, RootedFilePath>();
        var allDeepImportPackages = new HashSet<ByteStringKey>();
        projects.Range(entry =>
        {
            var program = host.GetProgramForProject(entry.Key());
            if (program != null)
            {
                allResolvedPackageNames[entry.Key()] = AutoImport.GetResolvedPackageNames(cancellation, program);
                AutoImport.AddProjectReferenceOutputMappings(program, projectReferenceOutputs);
                foreach (var name in program.DeepImportPackageNames())
                    allDeepImportPackages.Add(name);
            }

            return true;
        });
        var fileExcludePatterns = userPreferences.ParsedAutoImportFileExcludePatterns(host.FS().CaseSensitivity());
        HashSet<ByteStringKey>? targetRecursivePackages = userPreferences.AutoImportEntrypointDirectorySearch == Tristate.True ? null : allDeepImportPackages;
        Slice<NodeModulesBucketTask> nodeModulesTasks = default;
        change.RequestedFile.ForEachAncestorDirectory<object?>(path =>
        {
            var(entry, ok) = nodeModules.Get(path);
            if (ok)
            {
                var name = directories.Get(path).Entry!.Value().name;
                var dependencies = ComputeDependenciesForNodeModulesDirectory(change, allResolvedPackageNames, name, path);
                var bucketState = entry!.Value().state;
                // !!! Optimization: handle different dependency set via granular updates.
                bool needsFullRebuild = bucketState.multipleFilesDirty || !SetEqual(entry.Value().DependencyNames, dependencies) || !bucketState.buildPreferences.Equal(BucketBuildPreferences.FromUserPreferences(userPreferences)) || !AutoImport.RecursiveSearchSubset(targetRecursivePackages, bucketState.recursiveSearchPackages);
                var dirtyPackages = bucketState.DirtyPackages();
                bool canDoGranularUpdate = !needsFullRebuild && (dirtyPackages?.Count ?? 0) > 0;
                if (needsFullRebuild)
                    nodeModulesTasks = nodeModulesTasks.Append(new NodeModulesBucketTask { entry = entry, dependencyNames = dependencies, dirName = name, dirPath = path });
                else if (canDoGranularUpdate)
                    nodeModulesTasks = nodeModulesTasks.Append(new NodeModulesBucketTask { entry = entry, dependencyNames = dependencies, dirName = name, dirPath = path, isUpdate = true, existingBucket = entry.Value(), dirtyPackages = dirtyPackages });
            }

            return (null, false);
        });
        var nodeModulesLogger = logger != null && nodeModulesTasks.Count > 0 ? logger.Fork("Building node_modules indexes") : null;
        long discoveryStart = System.Diagnostics.Stopwatch.GetTimestamp();
        foreach (var task in nodeModulesTasks)
            work.Queue(() =>
            {
                if (task.isUpdate)
                    task.packageNames = task.dirtyPackages;
                else
                {
                    task.directoryPackageNames = AutoImport.GetPackageNamesInNodeModules(task.dirName.ResolveDirectory("node_modules"), host.FS());
                    task.packageNames = task.dependencyNames ?? task.directoryPackageNames;
                }

                task.discovered = DiscoverBucketPackages(task.packageNames, task.dirName, task.dirPath);
            });
        work.RunAndWait();
        nodeModulesLogger?.Logf("Discovered packages: %v", System.Diagnostics.Stopwatch.GetElapsedTime(discoveryStart));
        long extractionStart = System.Diagnostics.Stopwatch.GetTimestamp();
        var seen = new HashSet<RootedDirectoryPath>();
        var extractionCache = new Dictionary<RootedDirectoryPath, PerPackageExtractionResult>();
        object extractionMutex = new();
        Slice<DiscoveredPackage> typesFallbackCandidates = default;
        foreach (var task in nodeModulesTasks)
            foreach (var package in task.discovered)
            {
                if (package.realpath != default)
                {
                    if (seen.Add(package.realpath))
                    {
                        bool enableDirectorySearch = targetRecursivePackages == null || targetRecursivePackages.Contains(ByteStringKey.FromValue(package.packageName)) || AutoImport.KnownRecursiveSearchPackages.Contains(ByteStringKey.FromValue(package.packageName));
                        if (enableDirectorySearch && targetRecursivePackages != null)
                            targetRecursivePackages.Add(ByteStringKey.FromValue(package.packageName));
                        work.Queue(() =>
                        {
                            if (cancellation.IsCancellationRequested)
                                return;
                            var result = ExtractPackage(cancellation, package.packageJson, package.packageName, projectReferenceOutputs, fileExcludePatterns, enableDirectorySearch);
                            if (result != null)
                                lock (extractionMutex)
                                    extractionCache[package.realpath] = result;
                        });
                    }

                    if (package.typesRealpath != default)
                        typesFallbackCandidates = typesFallbackCandidates.Append(package);
                }
                else if (package.typesRealpath != default && seen.Add(package.typesRealpath))
                {
                    targetRecursivePackages?.Add(ByteStringKey.FromValue(package.packageName));
                    work.Queue(() =>
                    {
                        if (cancellation.IsCancellationRequested)
                            return;
                        var result = ExtractPackage(cancellation, package.typesPackageJson, package.packageName, projectReferenceOutputs, fileExcludePatterns, true);
                        if (result != null)
                            lock (extractionMutex)
                                extractionCache[package.typesRealpath] = result;
                    });
                }
            }

        work.RunAndWait();
        foreach (var package in typesFallbackCandidates)
        {
            bool mainExtracted;
            lock (extractionMutex)
                mainExtracted = extractionCache.ContainsKey(package.realpath);
            if (mainExtracted || seen.Contains(package.typesRealpath))
                continue;
            seen.Add(package.typesRealpath);
            targetRecursivePackages?.Add(ByteStringKey.FromValue(package.packageName));
            work.Queue(() =>
            {
                if (cancellation.IsCancellationRequested)
                    return;
                var result = ExtractPackage(cancellation, package.typesPackageJson, package.packageName, projectReferenceOutputs, fileExcludePatterns, true);
                if (result != null)
                    lock (extractionMutex)
                        extractionCache[package.typesRealpath] = result;
            });
        }

        work.RunAndWait();
        nodeModulesLogger?.Logf("Extracted exports: %v (%d packages)", System.Diagnostics.Stopwatch.GetElapsedTime(extractionStart), seen.Count);
        uniquePackageCount = seen.Count;
        Slice<BucketBuildResult> allResults = default;
        foreach (var task in nodeModulesTasks)
        {
            var result = new BucketBuildResult
            {
                replaceBucket = task.entry.Replace,
                resolutionPath = task.entry.Key()
            };
            allResults = allResults.Append(result);
            work.Queue(() =>
            {
                if (task.isUpdate)
                    UpdateNodeModulesBucket(cancellation, result, task.existingBucket!, task.dirtyPackages!, task.discovered, extractionCache, targetRecursivePackages, nodeModulesLogger?.Fork(task.dirName.AsString()));
                else
                    BuildNodeModulesBucket(cancellation, result, task.dependencyNames, task.dirPath, task.discovered, task.directoryPackageNames, extractionCache, targetRecursivePackages, nodeModulesLogger?.Fork(task.dirName.AsString()));
            });
        }

        var(project, hasProject) = projects.Get(projectID);
        if (hasProject)
        {
            var program = host.GetProgramForProject(projectID)!;
            allResolvedPackageNames.TryGetValue(projectID, out var resolvedPackageNames);
            bool shouldRebuild = project!.Value().state.HasDirtyFileBesides(change.RequestedFile) || !project.Value().state.buildPreferences.Equal(BucketBuildPreferences.FromUserPreferences(userPreferences));
            if (!shouldRebuild && project.Value().state.newProgramStructure > 0)
            {
                if (!SetEqual(project.Value().ResolvedPackageNames, resolvedPackageNames) || AutoImport.HasNewNonNodeModulesFiles(program, project.Value()))
                    shouldRebuild = true;
                else
                    project.Change(bucket => bucket.state.newProgramStructure = NewProgramStructure.False);
            }

            if (shouldRebuild)
            {
                var result = new BucketBuildResult
                {
                    replaceBucket = project.Replace,
                    resolutionPath = original.caseSensitivity.PathKey(program.BaseDirectory().AsPath())
                };
                allResults = allResults.Append(result);
                work.Queue(() => BuildProjectBucket(cancellation, result, projectID, resolvedPackageNames, logger?.Fork("Building project bucket " + projectID.GoLogString())));
            }
        }

        work.RunAndWait();
        foreach (var result in allResults)
        {
            if (result.error != null)
                continue;
            foreach (var path in result.removedEntrypointPaths)
                entrypoints.Delete(path);
            if (result.entrypoints != null)
                foreach (var(path, entries)in result.entrypoints)
                    entrypoints.Set(path, entries);
            result.replaceBucket(result.bucket);
        }

        // Failed non-relative aliases may name ambient declarations in another package.
        // Reprocess sources with the missing declarations, preserving the original second pass.
        long secondPassStart = System.Diagnostics.Stopwatch.GetTimestamp();
        int secondPassFileCount = 0;
        foreach (var result in allResults)
        {
            if (result.error != null || result.possibleFailedAmbientModuleLookupTargets == null)
                continue;
            var rootFiles = new Dictionary<RootedFilePath, SourceFile>();
            foreach (var target in result.possibleFailedAmbientModuleLookupTargets.Keys())
                foreach (var fileName in ResolveAmbientModuleName(target.Value, result.resolutionPath))
                {
                    if (rootFiles.ContainsKey(fileName))
                        continue;
                    rootFiles[fileName] = host.GetSourceFile(fileName, original.caseSensitivity.PathKey(fileName.AsPath()))!;
                    secondPassFileCount++;
                }

            if (rootFiles.Count > 0)
            {
                var copiedOptions = CopyResolverOptions(resolverOptions);
                copiedOptions.Host = new AutoImportResolutionHost(host.FS(), currentDirectory);
                copiedOptions.CompilerOptions = CompilerOptionHelpers.EmptyCompilerOptions;
                var moduleResolver = TypeScript.Modules.DefaultResolver.NewResolver(copiedOptions);
                // slices.Collect(maps.Values(rootFiles)) owns a fresh slice of the existing file pointers.
                Slice<SourceFile> roots = default;
                foreach (var file in rootFiles.Values)
                    roots = roots.Append(file);
                var aliasResolver = new AliasResolver(roots, null, host, moduleResolver, static (_, _) =>
                {
                });
                var checker = TypeScript.Checking.Checker.NewChecker(aliasResolver).Checker;
                result.possibleFailedAmbientModuleLookupSources!.Range((path, source) =>
                {
                    var sourceFile = aliasResolver.GetSourceFileByFileName(source.fileName, path)!;
                    var extractor = NewExportExtractor(source.packageName, checker, moduleResolver, fileName => TsPath.RootedFilePathFromPath(host.FS().Realpath(fileName.AsPath())));
                    foreach (var export in extractor.ExtractFromFile(sourceFile))
                        result.bucket.Index!.InsertAsWords(export);
                    return true;
                });
            }
        }

        if (nodeModulesLogger != null)
        {
            if (secondPassFileCount > 0)
                nodeModulesLogger.Logf("%d files required second pass, took %v", secondPassFileCount, System.Diagnostics.Stopwatch.GetElapsedTime(secondPassStart));
            nodeModulesLogger.Logf("Total: %v", System.Diagnostics.Stopwatch.GetElapsedTime(discoveryStart));
        }
    }

    // collections.Set.Equals treats nil and empty as equal; keys retain byte-content equality.
    private static bool SetEqual(HashSet<ByteStringKey>? first, HashSet<ByteStringKey>? second)
    {
        if ((first?.Count ?? 0) != (second?.Count ?? 0))
            return false;
        if (first != null)
            foreach (var key in first)
                if (second?.Contains(key) != true)
                    return false;
        return true;
    }
}

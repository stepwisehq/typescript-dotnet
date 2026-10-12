// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete mapped owner; pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.ContentMapper;
using TypeScript.LanguageServices.Utilities;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.ProjectSystem.Logging;
using TypeScript.Lsp.Protocol;
using TypeScript.SourceMap;
using TypeScript.Vfs;

namespace TypeScript.ProjectSystem;
internal sealed partial class Snapshot
{
    internal SnapshotHost host = null!;
    internal ulong id, parentId;
    internal int refCount;
    internal Converters converters = null!;
    internal SnapshotFS fs = null!;
    internal ProjectCollection ProjectCollection = null!;
    internal ConfigFileRegistry ConfigFileRegistry = null!;
    internal Registry? AutoImports;
    internal WatchedFiles<Dictionary<PathKey, RootedDirectoryPath>?>? autoImportsWatch;
    internal CompilerOptions? compilerOptionsForInferredProjects;
    internal Slice<Mapper> inferredProjectContentMappers;
    internal Slice<ByteStringValue> inferredProjectContentMapperExtensions;
    internal UserPreferences userPreferences;
    private readonly ProjectOnce contentMapperWatchStateOnce = new();
    private Slice<ByteStringValue> contentMapperExtensions;
    private HashSet<PathKey>? contentMapperWatchedFiles;
    internal LogTree? builderLogs;
    internal Exception? apiError;
    internal bool fileSystemOverride;
    internal Slice<Project> createdPrograms;
}

internal sealed partial class SnapshotHost
{
    // snapshot.go:84. Immutable root owners retain their exact shared headers.
    internal Snapshot NewSnapshot(ulong id, SnapshotFS fs, ConfigFileRegistry? configFileRegistry, CompilerOptions? compilerOptionsForInferredProjects, UserPreferences userPreferences, Registry? autoImports, WatchedFiles<Dictionary<PathKey, RootedDirectoryPath>?>? autoImportsWatch)
    {
        var overlays = Snapshot.SnapshotOverlays(fs);
        var snapshot = new Snapshot
        {
            host = this,
            id = id,
            fs = fs,
            // Clone installs its finalized registry before publishing this owner.
            ConfigFileRegistry = configFileRegistry!,
            ProjectCollection = new ProjectCollection
            {
                caseSensitivity = caseSensitivity,
                openFiles = ProjectCollection.OpenFilePaths(overlays)
            },
            compilerOptionsForInferredProjects = compilerOptionsForInferredProjects,
            userPreferences = userPreferences,
            AutoImports = autoImports,
            autoImportsWatch = autoImportsWatch,
            refCount = 1
        };
        // The original callback may return nil for an absent file. The retained
        // converter only dereferences it at source-governed valid file requests.
        snapshot.converters = Converters.NewConverters(options.PositionEncoding, name => snapshot.LSPLineMap(name)!);
        return snapshot;
    }
}

internal sealed partial class Snapshot
{
    // snapshot.go:62. Once publication belongs to each immutable snapshot.
    internal (Slice<ByteStringValue> Extensions, HashSet<PathKey>? WatchedFiles) ContentMapperWatchState()
    {
        contentMapperWatchStateOnce.Do(() =>
        {
            var configured = ConfigFileRegistry.ContentMappers();
            if (configured is not null)
                contentMapperExtensions = GoSlices.Clone(configured.Extensions);
            contentMapperExtensions = contentMapperExtensions.Append(inferredProjectContentMapperExtensions);
            GoSliceSort.SortFunc(contentMapperExtensions.AsSpan(), static (left, right) => ByteStringKey.FromValue(left).CompareTo(ByteStringKey.FromValue(right)));
            // slices.Compact rewrites the retained header and clears removed tail slots.
            if (contentMapperExtensions.Length > 1)
            {
                int count = 1;
                for (int index = 1; index < contentMapperExtensions.Length; index++)
                    if (!contentMapperExtensions[index].EqualsValue(contentMapperExtensions[count - 1]))
                        contentMapperExtensions[count++] = contentMapperExtensions[index];
                contentMapperExtensions.AsSpan()[count..].Clear();
                contentMapperExtensions = contentMapperExtensions.Slice2(0, count);
            }

            // Go publishes a Set wrapper here but leaves its backing map nil until Add.
            contentMapperWatchedFiles = null;
            foreach (var project in ProjectCollection.Projects())
                if (project.contentMapperWatchedFiles is { } paths)
                    foreach (var path in paths)
                        (contentMapperWatchedFiles ??= new()).Add(path);
        });
        return (contentMapperExtensions, contentMapperWatchedFiles);
    }

    // snapshot.go:313.
    internal Slice<ByteStringValue> ContentMapperExtensions() => ContentMapperWatchState().Extensions;
}

internal sealed partial class Snapshot : TypeScript.LanguageServices.ILanguageServiceHost
{
    // snapshot.go:111. Snapshot FS retains the genuine overlay backing owner.
    internal static Dictionary<PathKey, Overlay>? SnapshotOverlays(SnapshotFS source) => source.fs.Overlays();
    internal Dictionary<PathKey, Overlay>? Overlays() => SnapshotOverlays(fs);
    // snapshot.go:119.
    internal Slice<Project> CreatedPrograms() => createdPrograms;
    // snapshot.go:201.
    internal Project? GetDefaultProject(DocumentUri uri) => ProjectCollection.GetDefaultProject(uri.PathKey(CaseSensitivity()));
    // snapshot.go:207. Upstream TODO concerning symlink handling is retained.
    internal Slice<TypeScript.LanguageServices.IProject> GetLanguageServiceProjectsContainingFile(DocumentUri uri) => ProjectCollection.GetLanguageServiceProjectsContainingFile(CaseSensitivity().PathKey(uri.FileName().AsPath()));
    internal IFileHandle? GetFile(RootedFilePath fileName) => fs.GetFile(fileName);
    // snapshot.go:218/225. Absent file is an actual nil line-map owner.
    internal LSPLineMap? LSPLineMap(RootedFilePath fileName) => fs.GetFile(fileName)?.LSPLineMap();
    public ECMALineInfo? GetECMALineInfo(RootedFilePath fileName) => fs.GetFile(fileName)?.ECMALineInfo();
    public UserPreferences GetPreferences(ByteStringValue activeFile) => userPreferences;
    internal UserPreferences UserPreferences() => userPreferences;
    public Converters Converters() => converters;
    public Registry? AutoImportRegistry() => AutoImports;
    internal ulong ID() => id;
    public CaseSensitivity CaseSensitivity() => fs.sensitivity;
    // snapshot.go:256/261.
    internal bool IsOpenFile(RootedFilePath fileName) => Overlays()?.ContainsKey(CaseSensitivity().PathKey(fileName.AsPath())) == true;
    internal bool HasOverlayWithin(PathKey path)
    {
        if (Overlays()is { } overlays)
            foreach (var overlayPath in overlays.Keys)
                if (path.ContainsPath(overlayPath))
                    return true;
        return false;
    }

    internal IFS FileSystem() => fs.fs;
    internal bool HasFileSystemOverride() => fileSystemOverride;
    // snapshot.go:281. Source bytes remain borrowed from the actual file owner.
    public (ByteStringValue Contents, bool Ok) ReadFile(RootedFilePath fileName)
    {
        var file = GetFile(fileName);
        return file is null ? (default, false) : (file.ContentValue(), true);
    }

    public Slice<ByteStringValue> GetDirectories(RootedDirectoryPath path) => fs.fs.GetAccessibleEntries(path).Directories;
    // snapshot.go:305. This owner records no new reads during public snapshot use.
    internal IFS FS() => new SourceFS(false, fs);
    public bool DirectoryExists(RootedDirectoryPath path) => fs.fs.DirectoryExists(path);
    public bool FileExists(RootedFilePath path) => fs.fs.FileExists(path);
    public Slice<RootedFilePath> ReadDirectory(RootedDirectoryPath path, Slice<ByteStringValue> extensions, Slice<ByteStringValue> excludes, Slice<ByteStringValue> includes, int depth) => TypeScript.Vfs.VfsMatch.VfsMatch.ReadDirectory(fs.fs, path, extensions, excludes, includes, depth);
    internal RootedDirectoryPath GetCurrentDirectory() => host.GetCurrentDirectory();
}

internal sealed partial class Snapshot
{
    // snapshot.go:123. Append only genuine matching synthetic project IDs.
    internal ResourceRequest ResourceRequestForDocument(DocumentUri uri)
    {
        var path = uri.PathKey(CaseSensitivity());
        var request = new ResourceRequest
        {
            Documents = Slice<DocumentUri>.FromArray([uri])
        };
        foreach (var project in ProjectCollection.SyntheticProjects())
            if (project.ContainsFile(path) || project.host is { } host && host.sourceFS.SeenFileOrMissingParentDirectory(path))
                request.Projects = request.Projects.Append(project.ID());
        return request;
    }

    // snapshot.go:193. This map is non-nil even for nil overlays, as in make(map,len).
    internal static Dictionary<PathKey, IFileHandle> OverlayFileHandles(Dictionary<PathKey, Overlay>? overlays)
    {
        var result = new Dictionary<PathKey, IFileHandle>(overlays?.Count ?? 0);
        if (overlays is not null)
            foreach (var pair in overlays)
                result[pair.Key] = pair.Value;
        return result;
    }
}

internal sealed partial class Snapshot
{
    // snapshot.go:134. Every invalidation worker acts on the new dirty-clone
    // builder, while realpath aliases are read from this retained old snapshot.
    private FileChangeSummary ProcessFileChanges(SnapshotFSBuilder nextFS, FileChangeSummary changes, LogTree? logger, ContentMapperContributions? contributions, Dictionary<PathKey, Overlay>? previousOverlays, Dictionary<PathKey, Overlay>? overlays)
    {
        if (nextFS.FS()is IFileChangeExpander expander)
            changes = expander.ExpandFileChanges(changes);
        var previousOpenFiles = OverlayFileHandles(previousOverlays);
        var openFiles = OverlayFileHandles(overlays);
        if (changes.HasExcessiveWatchEvents())
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            if (changes.InvalidateAll)
            {
                nextFS.InvalidateCache();
                logger?.Logf("InvalidateAll: invalidated file cache in %v", System.Diagnostics.Stopwatch.GetElapsedTime(start));
            }
            else if (!nextFS.WatchChangesOverlapCache(changes, previousOpenFiles, openFiles))
            {
                // Original zero Set values have nil backing, not allocated empties.
                changes.Changed = null;
                changes.Deleted = null;
            }
            else if (changes.IncludesWatchChangeOutsideNodeModules)
            {
                nextFS.InvalidateCache();
                logger?.Logf("Excessive watch changes detected, invalidated file cache in %v", System.Diagnostics.Stopwatch.GetElapsedTime(start));
            }
            else
            {
                nextFS.InvalidateNodeModulesCache();
                logger?.Logf("npm install detected, invalidated node_modules cache in %v", System.Diagnostics.Stopwatch.GetElapsedTime(start));
            }
        }
        else
        {
            Slice<ByteStringValue> extensions = default;
            if (contributions is null)
                (extensions, _) = ContentMapperWatchState();
            else
            {
                if (ConfigFileRegistry.ContentMappers()is { } configured)
                    extensions = GoSlices.Clone(configured.Extensions);
                extensions = extensions.Append(contributions.Extensions);
            }

            var(_, watchedFiles) = ContentMapperWatchState();
            changes = nextFS.ExpandAndFilterWatchEvents(changes, extensions, watchedFiles, previousOpenFiles, openFiles);
            changes = fs.ExpandRealpathAliases(changes);
            changes = nextFS.MarkDirtyFiles(changes);
            changes = nextFS.ConvertOpenAndCloseToChanges(changes, previousOpenFiles, openFiles);
        }

        foreach (var path in openFiles.Keys)
        {
            var(entry, found) = nextFS.cacheFiles.Load(path);
            if (found)
                nextFS.DeleteCacheEntry(entry!);
        }

        return changes;
    }
}

internal sealed partial class Snapshot
{
    // snapshot.go:429. Build owners are mutable until finalization; the resulting
    // snapshot retains immutable cache headers and freezes every newly updated host.
    internal Snapshot Clone(ProtocolRequestContext context, SnapshotChange change, Dictionary<PathKey, Overlay>? overlays, ILogger? sessionLogger, IProjectClient? client)
    {
        if (apiError is not null)
            throw new InvalidOperationException("cannot clone snapshot with API error: " + apiError.Message);
        var store = host;
        LogTree? logger = null;
        bool logFailures = store.options.LoggingEnabled && sessionLogger is not null;
        try
        {
            if (logFailures)
            {
                logger = LogTree.NewLogTree(LogFormat.Sprintf("Cloning snapshot %d", id));
                LogCloneReason(logger, change);
            }

            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            var mappers = inferredProjectContentMappers;
            var extensions = inferredProjectContentMapperExtensions;
            if (change.contentMapperContributions is { } contributions)
            {
                mappers = contributions.Mappers;
                extensions = contributions.Extensions;
            }

            var baseFS = change.fs ?? store.fs;
            if (change.replaceFileSystem || fileSystemOverride && !change.fileSystemOverride)
                change.fileChanges.InvalidateAll = true;
            var layered = OverlayFiles.LayerOverlayFileSystem(baseFS, overlays, store.options.PositionEncoding);
            overlays = layered.Overlays();
            var nextFS = new SnapshotFSBuilder(layered, fs.cacheFiles, fs.cacheDirectories, fs.nodeModulesRealpathAliases);
            change.fileChanges = ProcessFileChanges(nextFS, change.fileChanges, logger, change.contentMapperContributions, Overlays(), overlays);
            var inferredOptions = change.compilerOptionsForInferredProjects ?? compilerOptionsForInferredProjects;
            var customConfigName = change.newConfig is { } changedConfig ? ByteStringValue.FromString(changedConfig.CustomConfigFileName) : ConfigFileRegistry.CustomConfigFileName;
            ulong nextID = store.NextSnapshotID();
            var builder = ProjectCollectionBuilder.New(context, nextID, nextFS, overlays, ProjectCollection, ConfigFileRegistry, ProjectCollection.apiState, inferredOptions, mappers, extensions, store.options, customConfigName, store.parseCache, store.contentMappedParseCache, store.extendedConfigCache, store.contentMapperHost, client);
            if ((change.ataChanges?.Count ?? 0) != 0)
                builder.DidUpdateATAState(change.ataChanges!, logger?.Fork("DidUpdateATAState"));
            builder.DidChangeCustomConfigFileName(logger?.Fork("DidChangeCustomConfigFileName"));
            if (change.compilerOptionsForInferredProjects is not null && builder.inferredProject.Value()is { } inferred)
                builder.UpdateInferredProject(inferred.CommandLine!.FileNames(), change.compilerOptionsForInferredProjects, inferred.CommandLine.ProjectReferences(), inferred.CommandLine.Errors, inferred.CommandLine.ContentMappers(), logger?.Fork("DidChangeCompilerOptionsForInferredProjects"));
            if (change.contentMapperContributions is not null)
                builder.DidChangeContentMapperContributions(logger?.Fork("DidChangeContentMapperContributions"));
            if (change.newConfig is { } configuration)
                builder.DidChangeUserPreferences(userPreferences, configuration, logger?.Fork("DidChangeUserPreferences"));
            if (!change.fileChanges.IsEmpty())
                builder.DidChangeFiles(change.fileChanges, logger?.Fork("DidChangeFiles"));
            Exception? nextApiError = null;
            if (change.apiRequest is not null)
                nextApiError = builder.HandleAPIRequest(change.apiRequest, logger?.Fork("HandleAPIRequest"));
            foreach (var uri in change.ResourceRequest.Documents)
                builder.DidRequestFile(uri, false, logger?.Fork("DidRequestFile"));
            foreach (var uri in change.ResourceRequest.ConfiguredProjectDocuments)
                builder.DidRequestFile(uri, true, logger?.Fork("DidRequestFile (optional)"));
            foreach (var projectID in change.ResourceRequest.Projects)
                builder.DidRequestProject(projectID, logger?.Fork("DidRequestProject"));
            if (change.ResourceRequest.ProjectTree is not null)
                builder.DidRequestProjectTrees(change.ResourceRequest.ProjectTree, logger?.Fork("DidRequestProjectTrees"));
            var(projects, configs) = builder.FinalizeCollection(logger);
            var rebuilt = new Dictionary<IProjectID, bool>();
            foreach (var project in projects.Projects())
                if (project.ProgramLastUpdate == nextID && project.ProgramUpdateKind != ProgramUpdateKind.Cloned)
                    rebuilt[project.ID()] = project.ProgramUpdateKind == ProgramUpdateKind.NewFiles;
            bool clean = change.cleanFileCache || change.fileChanges.Opened.Value.Length != 0 || change.fileChanges.Reopened.Value.Length != 0 || (change.fileChanges.Closed?.Count ?? 0) > 0 || (change.fileChanges.Deleted?.Count ?? 0) > 0;
            if (clean && (rebuilt.Count > 0 || change.cleanFileCache))
            {
                long cleanStart = System.Diagnostics.Stopwatch.GetTimestamp();
                int removed = 0;
                nextFS.cacheFiles.Range(entry =>
                {
                    foreach (var project in projects.Projects())
                        if (project.host is not null && project.host.sourceFS.SeenFile(entry.Key()))
                            return true;
                    entry.Delete();
                    removed++;
                    return true;
                });
                logger?.Logf("Removed %d cached file(s) in %v", removed, System.Diagnostics.Stopwatch.GetElapsedTime(cleanStart));
            }

            var config = change.newConfig ?? userPreferences;
            var autoImportHost = AutoImportRegistryCloneHost.New(projects, store.parseCache, nextFS);
            var openFiles = new Dictionary<PathKey, RootedFilePath>(overlays?.Count ?? 0);
            if (overlays is not null)
                foreach (var(path, overlay)in overlays)
                    openFiles[path] = overlay.FileName();
            PathKey requestedPath = default;
            RootedFilePath requestedName = default;
            if (change.ResourceRequest.AutoImports.Value.Length != 0)
            {
                requestedPath = change.ResourceRequest.AutoImports.PathKey(CaseSensitivity());
                requestedName = change.ResourceRequest.AutoImports.FileName();
            }

            var oldImports = AutoImports ?? Registry.NewRegistry(store.caseSensitivity, userPreferences);
            var(imports, error) = oldImports.Clone(context.Cancellation, new RegistryChange { RequestedFile = requestedPath, RequestedFileName = requestedName, OpenFiles = openFiles, Changed = change.fileChanges.Changed, Created = change.fileChanges.Created, Deleted = change.fileChanges.Deleted, RebuiltPrograms = rebuilt, UserPreferences = change.newConfig }, autoImportHost, store.options.CurrentDirectory, logger?.Fork("UpdateAutoImports"));
            WatchedFiles<Dictionary<PathKey, RootedDirectoryPath>?>? nextImportWatch = null;
            if (error is null)
                nextImportWatch = autoImportsWatch?.Clone(imports.NodeModulesDirectories());
            var(snapshotFS, _) = nextFS.FinalizeValue();
            var snapshot = store.NewSnapshot(nextID, snapshotFS, null, inferredOptions, config, imports, nextImportWatch);
            snapshot.parentId = id;
            snapshot.ProjectCollection = projects;
            snapshot.ConfigFileRegistry = configs;
            snapshot.inferredProjectContentMappers = mappers;
            snapshot.inferredProjectContentMapperExtensions = extensions;
            snapshot.builderLogs = logger;
            snapshot.apiError = nextApiError;
            snapshot.fileSystemOverride = change.fileSystemOverride;
            snapshot.createdPrograms = builder.createdPrograms;
            foreach (var project in projects.Projects())
                if (project.Program is not null)
                {
                    store.programCounter.Ref(project.Program);
                    if (project.ProgramLastUpdate == nextID)
                        project.host!.Freeze(snapshotFS, configs);
                }

            if (configs.Configs is not null)
                foreach (var entry in configs.Configs.Values)
                    if (entry.CommandLine?.ConfigFile is { } source)
                        foreach (var fileName in source.ExtendedSourceFiles ?? [])
                            store.extendedConfigCache.AddOwner(store.caseSensitivity.PathKey(fileName.AsPath()), snapshot.id);
            autoImportHost.Dispose();
            logger?.Logf("Finished cloning snapshot %d into snapshot %d in %v", id, snapshot.id, System.Diagnostics.Stopwatch.GetElapsedTime(start));
            return snapshot;
        }
        catch
        {
            if (logFailures)
                sessionLogger!.Log(logger is null ? "" : logger.String());
            throw;
        }
    }
}

internal sealed partial class Snapshot
{
    // snapshot.go:452. Details are computed only for the source branches which
    // request them; each value projection uses the logging owner's Go formatter.
    private static void LogCloneReason(LogTree logger, SnapshotChange change)
    {
        string Details()
        {
            var request = change.ResourceRequest;
            string result = "";
            if (request.Documents.Count != 0)
                result += LogFormat.Sprintf(" Documents: %v", request.Documents);
            if (request.ConfiguredProjectDocuments.Count != 0)
                result += LogFormat.Sprintf(" ConfiguredProjectDocuments: %v", request.ConfiguredProjectDocuments);
            if (request.Projects.Count != 0)
                result += LogFormat.Sprintf(" Projects: %v", request.Projects);
            if (request.ProjectTree is not null)
                result += LogFormat.Sprintf(" ProjectTree: %v", request.ProjectTree.Projects());
            return result;
        }

        switch (change.reason)
        {
            case UpdateReason.DidOpenFile:
                logger.Logf("Reason: DidOpenFile - %s", change.fileChanges.Opened);
                break;
            case UpdateReason.DidCloseFile:
                logger.Logf("Reason: DidCloseFile - %v", change.fileChanges.Closed);
                break;
            case UpdateReason.DidChangeCompilerOptionsForInferredProjects:
                logger.Logf("Reason: DidChangeCompilerOptionsForInferredProjects");
                break;
            case UpdateReason.RequestedLanguageServicePendingChanges:
                logger.Logf("Reason: RequestedLanguageService (pending file changes) - %v", Details());
                break;
            case UpdateReason.RequestedLanguageServiceProjectNotLoaded:
                logger.Logf("Reason: RequestedLanguageService (project not loaded) - %v", Details());
                break;
            case UpdateReason.RequestedLanguageServiceForFileNotOpen:
                logger.Logf("Reason: RequestedLanguageService (file not open) - %v", Details());
                break;
            case UpdateReason.RequestedLanguageServiceProjectDirty:
                logger.Logf("Reason: RequestedLanguageService (project dirty) - %v", Details());
                break;
            case UpdateReason.RequestedLoadProjectTree:
                logger.Logf("Reason: RequestedLoadProjectTree - %v", Details());
                break;
            case UpdateReason.IdleCleanDiskCache:
                logger.Logf("Reason: IdleCleanDiskCache");
                break;
            case UpdateReason.DidChangeConfigFile:
                logger.Logf("Reason: DidChangeConfigFile - %v", Details());
                break;
            case UpdateReason.DidChangeContentMapperContributions:
                logger.Logf("Reason: DidChangeContentMapperContributions - %v", Details());
                break;
        }
    }
}

internal sealed partial class Snapshot
{
    // snapshot.go:731. The increment deliberately remains applied before a failed disposed-owner assertion.
    internal void Ref()
    {
        if (Interlocked.Increment(ref refCount) <= 1)
            throw new InvalidOperationException($"snapshot {id}: ref on disposed snapshot, parentId={parentId}");
    }

    // snapshot.go:740. Failed retain never resurrects a disposed snapshot.
    internal bool TryRef()
    {
        while (true)
        {
            int count = Volatile.Read(ref refCount);
            if (count <= 0)
                return false;
            if (Interlocked.CompareExchange(ref refCount, unchecked(count + 1), count) == count)
                return true;
        }
    }

    // snapshot.go:754. Last reference releases the store-owned resources once.
    internal void Deref()
    {
        int count = Interlocked.Decrement(ref refCount);
        if (count < 0)
            throw new InvalidOperationException($"snapshot {id}: ref count below zero, parentId={parentId}");
        if (count == 0)
            DisposeSnapshot();
    }

    // snapshot.go:764. Program references govern shared source/cache and pool lifetime.
    private void DisposeSnapshot()
    {
        var store = host;
        foreach (var project in ProjectCollection.Projects())
        {
            if (project.Program == null || !store.programCounter.Deref(project.Program))
                continue;
            project.Program.ContentMapperProject()?.Close();
            project.checkerPool?.Discard();
            foreach (var file in project.Program.GetSourceFiles())
            {
                if (file.IsContentMapperFailureStub() || file.IsContentMapperSupplemental())
                    continue;
                if (file.ContentMapper().Length != 0)
                    store.contentMappedParseCache.Deref(ParseCaches.ContentMappedKeyForFile(file));
                else
                    store.parseCache.Deref(ParseCaches.KeyForFile(file));
            }

            foreach (var file in project.Program.DuplicateSourceFiles())
            {
                if (file.IsContentMapperFailureStub)
                    continue;
                if (file.ContentMapper.Length != 0)
                    store.contentMappedParseCache.Deref(ParseCaches.ContentMappedKeyForDuplicate(file));
                else
                    store.parseCache.Deref(ParseCaches.KeyForDuplicate(file));
            }
        }

        if (ConfigFileRegistry.Configs != null)
            foreach (var config in ConfigFileRegistry.Configs.Values)
                if (config.CommandLine != null)
                    foreach (var file in config.CommandLine.ExtendedSourceFiles() ?? [])
                        store.extendedConfigCache.Release(store.caseSensitivity.PathKey(file.AsPath()), id);
    }
}

// internal/project/snapshot.go:395. Go embedded request represented explicitly by value.
internal struct SnapshotChange
{
    internal ResourceRequest ResourceRequest;
    internal UpdateReason reason;
    internal IFS? fs;
    internal bool fileSystemOverride;
    internal bool replaceFileSystem;
    internal FileChangeSummary fileChanges;
    internal CompilerOptions? compilerOptionsForInferredProjects;
    internal ContentMapperContributions? contentMapperContributions;
    internal UserPreferences? newConfig;
    internal Dictionary<ID, ATAStateChange>? ataChanges;
    internal APISnapshotRequest? apiRequest;
    internal bool cleanFileCache;
}

// internal/project/snapshot.go:375. Embedded request value retains original Slice headers.
internal struct ResourceRequest
{
    internal Slice<DocumentUri> Documents;
    internal Slice<DocumentUri> ConfiguredProjectDocuments;
    internal Slice<ID> Projects;
    internal ProjectTreeRequest? ProjectTree;
    internal DocumentUri AutoImports;
}

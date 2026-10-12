// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/project/projectcollectionbuilder.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Compiler;
using TypeScript.ContentMapper;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem.Logging;
using TypeScript.Collections;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using Program = TypeScript.Compiler.Program;
using Diagnostic = TypeScript.Scanner.Diagnostic;
using ConfigParser = TypeScript.TsOptions.TsOptions;

namespace TypeScript.ProjectSystem;
internal enum ProjectLoadKind
{
    Find,
    Create
}

internal sealed class ProjectCollectionBuilder
{
    internal SessionOptions sessionOptions = null!;
    internal RefCountCache<ParseCacheKey, SourceFile, IFileHandle> parseCache = null!;
    internal RefCountCache<ContentMappedParseCacheKey, SourceFiles, ValueTuple> contentMappedParseCache = null!;
    internal OwnerCache<PathKey, ExtendedConfigCacheEntry, ExtendedConfigParseArgs> extendedConfigCache = null!;
    internal TypeScript.ContentMapper.IHost? contentMapperHost;
    internal ProtocolRequestContext ctx = null!;
    internal SnapshotFSBuilder fs = null!;
    internal Dictionary<PathKey, Overlay>? overlays;
    internal ProjectCollection baseCollection = null!;
    internal CompilerOptions? compilerOptionsForInferredProjects;
    internal List<Mapper>? inferredContentMappers;
    internal Slice<ByteStringValue> inferredContentMapperExtensions;
    internal ConfigFileRegistryBuilder configFileRegistryBuilder = null!;
    internal IProjectClient? client;
    internal ulong newSnapshotID;
    internal bool programStructureChanged, defaultProjectsInvalidated, openFilesChanged;
    internal Dictionary<PathKey, ID>? fileDefaultProjects;
    internal Dirty.SyncMap<ConfiguredProjectID, Project> configuredProjects = null!;
    internal Dirty.SyncMap<SyntheticProjectID, Project> syntheticProjects = null!;
    internal Dirty.Box<Project> inferredProject = null!;
    internal Slice<Project> createdPrograms;
    internal ApiState apiState;
    // projectcollectionbuilder.go:67.
    internal static ProjectCollectionBuilder New(ProtocolRequestContext context, ulong id, SnapshotFSBuilder fs, Dictionary<PathKey, Overlay>? overlays, ProjectCollection old, ConfigFileRegistry registry, ApiState api, CompilerOptions? inferredOptions, List<Mapper>? mappers, Slice<ByteStringValue> extensions, SessionOptions options, ByteStringValue custom, RefCountCache<ParseCacheKey, SourceFile, IFileHandle> parses, RefCountCache<ContentMappedParseCacheKey, SourceFiles, ValueTuple> mapped, OwnerCache<PathKey, ExtendedConfigCacheEntry, ExtendedConfigParseArgs> extended, TypeScript.ContentMapper.IHost? mapperHost, IProjectClient? client)
    {
        var open = ProjectCollection.OpenFilePaths(overlays);
        return new()
        {
            ctx = context,
            fs = fs,
            overlays = overlays,
            compilerOptionsForInferredProjects = inferredOptions,
            inferredContentMappers = mappers,
            inferredContentMapperExtensions = extensions,
            sessionOptions = options,
            parseCache = parses,
            contentMappedParseCache = mapped,
            extendedConfigCache = extended,
            contentMapperHost = mapperHost,
            baseCollection = old,
            configFileRegistryBuilder = ConfigFileRegistryBuilder.New(ProtocolContexts.GetClientCapabilities(context).Workspace.DidChangeWatchedFiles.RelativePatternSupport, fs, path => overlays?.ContainsKey(path) == true, registry, extended, id, options, custom, null),
            newSnapshotID = id,
            openFilesChanged = old.openFiles == null ? open.Count != 0 : !open.SetEquals(old.openFiles),
            configuredProjects = new(old.configuredProjects),
            syntheticProjects = new(old.syntheticProjects),
            inferredProject = new(old.inferredProject!),
            apiState = api.Clone(),
            client = client
        };
    }

    // projectcollectionbuilder.go:111/116.
    private bool IsOpenFile(PathKey path) => overlays?.ContainsKey(path) == true;
    private PathKey ToPathKey(RootedFilePath name) => fs.FS().CaseSensitivity().PathKey(name.AsPath());
    // projectcollectionbuilder.go:120.
    internal (ProjectCollection Projects, ConfigFileRegistry Configs) FinalizeCollection(LogTree? logger)
    {
        bool changed = false;
        var result = baseCollection;
        void EnsureCloned()
        {
            if (!changed)
            {
                result = result.Clone();
                changed = true;
            }
        }

        var(configured, configuredChanged) = configuredProjects.FinalizeValue();
        if (configuredChanged)
        {
            EnsureCloned();
            result.configuredProjects = configured;
        }

        var(synthetic, syntheticChanged) = syntheticProjects.FinalizeValue();
        if (syntheticChanged)
        {
            EnsureCloned();
            result.syntheticProjects = synthetic;
        }

        if (openFilesChanged)
        {
            EnsureCloned();
            result.openFiles = ProjectCollection.OpenFilePaths(overlays);
        }

        if (!MapsEqual(fileDefaultProjects, baseCollection.fileDefaultProjects))
        {
            EnsureCloned();
            result.fileDefaultProjects = fileDefaultProjects;
        }

        var(inferred, inferredChanged) = inferredProject.FinalizeValue();
        if (inferredChanged)
        {
            EnsureCloned();
            result.inferredProject = inferred;
        }

        var registry = configFileRegistryBuilder.FinalizeRegistry();
        if (!ReferenceEquals(registry, baseCollection.configFileRegistry))
        {
            EnsureCloned();
            result.configFileRegistry = registry;
        }

        if (!apiState.EqualsState(baseCollection.apiState))
        {
            EnsureCloned();
            result.apiState = apiState;
        }

        return (result, registry);
    }

    private static bool MapsEqual<K, V>(Dictionary<K, V>? left, Dictionary<K, V>? right)
        where K : notnull
    {
        if ((left?.Count ?? 0) != (right?.Count ?? 0))
            return false;
        if (left == null)
            return true;
        foreach (var(key, value)in left)
            if (right == null || !right.TryGetValue(key, out var other) || !EqualityComparer<V>.Default.Equals(value, other))
                return false;
        return true;
    }

    // projectcollectionbuilder.go:168.
    private void ForEachProject(Func<Dirty.IValue<Project>, bool> action)
    {
        bool keepGoing = true;
        configuredProjects.Range(entry => keepGoing = action(entry));
        if (keepGoing)
            syntheticProjects.Range(entry => keepGoing = action(entry));
        if (keepGoing && inferredProject.Value() != null)
            action(inferredProject);
    }

    // projectcollectionbuilder.go:188. Failure rolls back API state, not the original graph work already performed.
    internal Exception? HandleAPIRequest(APISnapshotRequest request, LogTree? logger)
    {
        var previous = apiState;
        apiState = apiState.Clone();
        Exception? error = null;
        try
        {
            error = HandleAPIRequestCore(request, logger);
            return error;
        }
        finally
        {
            if (error != null)
                apiState = previous;
        }
    }

    private Exception? HandleAPIRequestCore(APISnapshotRequest request, LogTree? logger)
    {
        HashSet<PathKey>? closing = null;
        if (request.CloseProjects != null)
            foreach (var path in request.CloseProjects)
            {
                long count = apiState.OpenProjects?.GetValueOrDefault(path) ?? 0;
                if (count > 1)
                    apiState.OpenProjects![path] = count - 1;
                else if (count == 1)
                {
                    apiState.OpenProjects!.Remove(path);
                    (closing ??= new()).Add(path);
                }
            }

        if (request.OpenProjects != null)
            foreach (var name in request.OpenProjects)
            {
                var path = ToPathKey(name);
                if (FindOrCreateProject(name, path, ProjectLoadKind.Create, logger) == null)
                    return new InvalidOperationException(LogFormat.Sprintf("project not found for open: %s", name));
                apiState.OpenProjects ??= new();
                apiState.OpenProjects[path] = apiState.OpenProjects.GetValueOrDefault(path) + 1;
                closing?.Remove(path);
            }

        if (request.CloseFiles != null)
            foreach (var path in request.CloseFiles)
                if (apiState.OpenFiles?.TryGetValue(path, out var file) == true)
                {
                    if (file.RefCount > 1)
                        apiState.OpenFiles[path] = file with
                        {
                            RefCount = file.RefCount - 1
                        };
                    else
                        apiState.OpenFiles.Remove(path);
                }

        if (request.OpenFiles != null)
            foreach (var(path, name)in request.OpenFiles)
            {
                apiState.OpenFiles ??= new();
                var file = apiState.OpenFiles.GetValueOrDefault(path);
                apiState.OpenFiles[path] = new(name, file.RefCount + 1);
            }

        if (overlays != null)
            foreach (var(path, overlay)in overlays)
            {
                var entry = FindDefaultConfiguredProject(overlay.FileName(), path);
                if (entry != null)
                    closing?.Remove(entry.Value().configFilePath);
            }

        if (closing != null)
            foreach (var path in closing)
            {
                var(entry, ok) = configuredProjects.Load(Projects.ConfiguredProjectIDFromPathKey(path));
                if (ok)
                    DeleteProject(entry!, logger);
            }

        if (request.OpenFiles != null)
        {
            HashSet<PathKey>? retain = null;
            bool ensureInferred = false;
            foreach (var(path, name)in request.OpenFiles)
            {
                if (IsOpenFile(path))
                {
                    if (FindDefaultConfiguredProject(name, path) == null && !IsSupportedInInferredProject(name))
                        return new InvalidOperationException(LogFormat.Sprintf("no project found for opened file: %s", name));
                    continue;
                }

                var result = EnsureConfiguredProjectAndAncestorsForFile(name, path, logger);
                if (result.Retain != null)
                    (retain ??= new()).UnionWith(result.Retain);
                if (result.Project == null)
                {
                    if (!IsSupportedInInferredProject(name))
                        return new InvalidOperationException(LogFormat.Sprintf("no project found for opened file: %s", name));
                    ensureInferred = true;
                }
            }

            CleanupConfiguredProjects(retain, logger);
            if (ensureInferred && inferredProject.Value() != null)
                UpdateProgram(inferredProject, logger);
        }
        else if (request.CloseFiles != null)
            CleanupConfiguredProjects(null, logger);
        HashSet<SyntheticProjectID>? seen = null;
        foreach (var reconfigure in request.ReconfigurePrograms)
        {
            if (!(seen ??= new()).Add(reconfigure.ProgramID))
                return new InvalidOperationException(LogFormat.Sprintf("synthetic program reconfigured more than once: %s", reconfigure.ProgramID.AsID()));
            if (request.RemovePrograms?.Contains(reconfigure.ProgramID) == true)
                return new InvalidOperationException(LogFormat.Sprintf("synthetic program cannot be reconfigured and removed: %s", reconfigure.ProgramID.AsID()));
            if (!syntheticProjects.Load(reconfigure.ProgramID).Ok)
                return new InvalidOperationException(LogFormat.Sprintf("synthetic program not found for reconfiguration: %s", reconfigure.ProgramID.AsID()));
        }

        if (request.RemovePrograms != null)
            foreach (var id in request.RemovePrograms)
            {
                var(entry, ok) = syntheticProjects.Load(id);
                if (!ok)
                    return new InvalidOperationException(LogFormat.Sprintf("synthetic program not found for removal: %s", id.AsID()));
                DeleteProject(entry!, logger);
            }

        var created = Slice<Project>.Make(request.CreatePrograms.Count);
        var createdEntries = Slice<Dirty.IValue<Project>>.Make(request.CreatePrograms.Count);
        for (int i = 0; i < request.CreatePrograms.Count; i++)
        {
            var create = request.CreatePrograms[i];
            createdEntries[i] = UpdateOrCreateSyntheticProject(NextSyntheticProjectID(), CloneList(create.RootFileNames), create.CompilerOptions, create.ProjectReferences, create.ConfigFileParsingDiagnostics, create.ModuleResolverFactory, create.ModuleResolverID, inferredContentMappers, logger);
        }

        var reconfigured = Slice<Dirty.IValue<Project>>.Make(request.ReconfigurePrograms.Count);
        for (int i = 0; i < request.ReconfigurePrograms.Count; i++)
        {
            var reconfigure = request.ReconfigurePrograms[i];
            reconfigured[i] = UpdateOrCreateSyntheticProject(reconfigure.ProgramID, CloneList(reconfigure.RootFileNames), reconfigure.CompilerOptions, reconfigure.ProjectReferences, reconfigure.ConfigFileParsingDiagnostics, reconfigure.ModuleResolverFactory, reconfigure.ModuleResolverID, inferredContentMappers, logger);
        }

        var group = Core.NewWorkGroup(false);
        for (int i = 0; i < createdEntries.Count; i++)
        {
            int index = i;
            var entry = createdEntries[i];
            group.Queue(() =>
            {
                if (entry.Value().dirty)
                    UpdateProgram(entry, logger);
                created[index] = entry.Value();
            });
        }

        foreach (var entry in reconfigured)
            group.Queue(() =>
            {
                if (entry.Value().dirty)
                    UpdateProgram(entry, logger);
            });
        group.RunAndWait();
        createdPrograms = created;
        if (request.EnsureFiles != null)
            foreach (var(path, name)in request.EnsureFiles)
            {
                DidRequestFile(name, path, false, logger);
                if (FindDefaultProject(name, path) == null)
                    return new InvalidOperationException(LogFormat.Sprintf("no project found for opened file: %s", name));
            }

        if (request.EnsurePrograms != null)
            foreach (var id in request.EnsurePrograms)
                DidRequestProject(id, logger);
        if (request.EnsureAllPrograms)
            ForEachProject(entry =>
            {
                UpdateProgram(entry, logger);
                return true;
            });
        Exception? resolutionError = null;
        ForEachProject(entry =>
        {
            var project = entry.Value();
            if (project.Program != null)
                resolutionError = project.Program.ModuleResolutionError();
            return resolutionError == null;
        });
        return resolutionError;
    }

    private static List<T>? CloneList<T>(List<T>? values) => values == null ? null : new(values);
    // projectcollectionbuilder.go:388.
    private SyntheticProjectID NextSyntheticProjectID()
    {
        for (long id = 1;; id++)
        {
            var projectID = Projects.NewSyntheticProjectID(id);
            if (!syntheticProjects.Load(projectID).Ok)
                return projectID;
        }
    }

    // projectcollectionbuilder.go:397.
    internal void DidChangeFiles(FileChangeSummary summary, LogTree? logger)
    {
        openFilesChanged = openFilesChanged || summary.Opened.Value.Length != 0 || (summary.Closed?.Count ?? 0) > 0;
        Slice<PathKey> ToPaths(HashSet<DocumentUri>? uris)
        {
            var paths = Slice<PathKey>.Make(0, uris?.Count ?? 0);
            if (uris != null)
                foreach (var uri in uris)
                    paths = paths.Append(uri.PathKey(baseCollection.caseSensitivity));
            return paths;
        }

        var changed = ToPaths(summary.Changed);
        var deleted = ToPaths(summary.Deleted);
        var created = ToPaths(summary.Created);
        if (contentMapperHost != null)
        {
            int allCount = changed.Count + deleted.Count + created.Count;
            var all = allCount == 0 ? default : Slice<PathKey>.Make(allCount);
            changed.AsSpan().CopyTo(all.AsSpan());
            deleted.AsSpan().CopyTo(all.AsSpan().Slice(changed.Count));
            created.AsSpan().CopyTo(all.AsSpan().Slice(changed.Count + deleted.Count));
            ForEachProject(entry =>
            {
                RefreshContentMapperProjectForChanges(entry, all, summary.HasExcessiveNonCreateWatchEvents(), logger);
                return true;
            });
        }

        var configLogger = logger?.Fork("Checking for changes affecting config files");
        var configChanges = configFileRegistryBuilder.DidChangeFiles(summary, configLogger);
        LogChangeFileResult(configChanges, configLogger);
        programStructureChanged = MarkProjectsAffectedByConfigChanges(configChanges, logger);
        ForEachProject(entry =>
        {
            if (summary.HasExcessiveNonCreateWatchEvents())
            {
                entry.Change(project =>
                {
                    project.dirty = true;
                    project.dirtyFilePath = default;
                    logger?.Logf("Marking project as dirty due to excessive watch changes: %s", project.ID());
                });
                return true;
            }

            MarkFilesChanged(entry, changed, FileChangeType.Changed, logger);
            if (entry.Value().Kind == ProjectKind.Inferred && (summary.Closed?.Count ?? 0) > 0)
            {
                var roots = entry.Value().CommandLine!.FilePaths();
                // List is the established command-line API owner. A detached header
                // adaptation avoids shortening a previous snapshot's shared List.
                var names = entry.Value().CommandLine!.FileNames()is { } rootNames ? new List<RootedFilePath>(rootNames) : new List<RootedFilePath>();
                foreach (var uri in summary.Closed!)
                {
                    var name = uri.FileName();
                    if (roots.Contains(ToPathKey(name)))
                        names.RemoveAt(names.IndexOf(name));
                }

                UpdateInferredProjectRoots(names, logger);
            }

            if ((summary.Deleted?.Count ?? 0) > 0)
                MarkFilesChanged(entry, deleted, FileChangeType.Deleted, logger);
            if ((summary.Created?.Count ?? 0) > 0)
                MarkFilesChanged(entry, created, FileChangeType.Created, logger);
            return true;
        });
        if (summary.Opened.Value.Length != 0 || summary.Reopened.Value.Length != 0)
        {
            var name = (summary.Opened.Value.Length != 0 ? summary.Opened : summary.Reopened).FileName();
            var result = EnsureConfiguredProjectAndAncestorsForFile(name, ToPathKey(name), logger);
            CleanupConfiguredProjects(result.Retain, logger);
        }
    }

    // projectcollectionbuilder.go:474.
    private void RefreshContentMapperProjectForChanges(Dirty.IValue<Project> entry, Slice<PathKey> paths, bool refreshAll, LogTree? logger)
    {
        var project = entry.Value();
        if (project.Program == null || project.contentMapperWatchedFiles == null)
            return;
        bool affected = refreshAll;
        if (!affected)
            foreach (var path in paths)
                if (project.contentMapperWatchedFiles.Contains(path))
                {
                    affected = true;
                    break;
                }

        if (!affected)
            return;
        project.Program.ContentMapperProject()?.Refresh();
        entry.Change(value =>
        {
            value.dirty = true;
            value.dirtyFilePath = default;
            logger?.Logf("Marking project as dirty due to content mapper configuration changes: %s", value.ID());
        });
    }

    // projectcollectionbuilder.go:509. Retention considers overlay/API roots and ancestor solutions.
    private void CleanupConfiguredProjects(HashSet<PathKey>? retain, LogTree? logger)
    {
        HashSet<PathKey>? remove = null;
        configuredProjects.Range(entry =>
        {
            (remove ??= new()).Add(entry.Key().PathKey());
            return true;
        });
        void RetainProjectAndReferences(Project project)
        {
            remove?.Remove(project.ConfigFilePath());
            project.Program?.RangeResolvedProjectReference((path, config, parent, index) =>
            {
                if (configuredProjects.Load(Projects.ConfiguredProjectIDFromPathKey(path)).Ok)
                    remove?.Remove(path);
                return true;
            });
        }

        void RetainDefault(PathKey path, Project project)
        {
            RetainProjectAndReferences(project);
            configFileRegistryBuilder.ForEachConfigFileNameFor(path, name =>
            {
                var ancestor = FindOrCreateProject(name, ToPathKey(name), ProjectLoadKind.Find, logger);
                if (ancestor != null)
                    RetainProjectAndReferences(ancestor.Value());
            });
        }

        List<RootedFilePath>? inferred = null;
        if (overlays != null)
            foreach (var(path, overlay)in overlays)
            {
                var name = overlay.FileName();
                var project = FindDefaultConfiguredProject(name, path);
                if (project != null)
                    RetainDefault(path, project.Value());
                else
                    (inferred ??= new()).Add(name);
            }

        if (apiState.OpenFiles != null)
            foreach (var(path, file)in apiState.OpenFiles)
            {
                if (IsOpenFile(path))
                    continue;
                var project = FindDefaultConfiguredProject(file.FileName, path);
                if (project != null)
                    RetainDefault(path, project.Value());
                else
                    (inferred ??= new()).Add(file.FileName);
            }

        if (remove != null)
            foreach (var path in remove)
            {
                if (retain?.Contains(path) == true || apiState.OpenProjects?.ContainsKey(path) == true)
                    continue;
                var(entry, ok) = configuredProjects.Load(Projects.ConfiguredProjectIDFromPathKey(path));
                if (ok)
                    DeleteProject(entry!, logger);
            }

        UpdateInferredProjectRoots(inferred, logger);
        configFileRegistryBuilder.Cleanup();
    }

    // projectcollectionbuilder.go:579/589.
    private void CleanupAllConfiguredProjects(LogTree? logger)
    {
        configuredProjects.Range(entry =>
        {
            var(project, ok) = configuredProjects.Load(entry.Key());
            if (ok)
                DeleteProject(project!, logger);
            return true;
        });
        configFileRegistryBuilder.Cleanup();
    }

    private static void LogChangeFileResult(ChangeFileResult result, LogTree? logger)
    {
        if ((result.AffectedProjects?.Count ?? 0) > 0)
        {
            var ids = Slice<ID>.Make(result.AffectedProjects!.Count);
            int index = 0;
            foreach (var id in result.AffectedProjects)
                ids[index++] = id;
            logger?.Logf("Config file change affected projects: %v", ids);
        }

        if ((result.AffectedFiles?.Count ?? 0) > 0)
            logger?.Logf("Config file change affected config file lookups for %d files", result.AffectedFiles!.Count);
    }

    // projectcollectionbuilder.go:598/611/623.
    private List<RootedFilePath>? CollectInferredProjectRoots()
    {
        List<RootedFilePath>? names = null;
        if (overlays != null)
            foreach (var(path, overlay)in overlays)
                if (FindDefaultConfiguredProject(overlay.FileName(), path) == null)
                    (names ??= new()).Add(overlay.FileName());
        return AppendAPIOpenedInferredRoots(names);
    }

    private List<RootedFilePath>? AppendAPIOpenedInferredRoots(List<RootedFilePath>? names)
    {
        if (apiState.OpenFiles != null)
            foreach (var(path, file)in apiState.OpenFiles)
            {
                if (IsOpenFile(path))
                    continue;
                if (FindDefaultConfiguredProject(file.FileName, path) == null)
                    (names ??= new()).Add(file.FileName);
            }

        return names;
    }

    private void CleanupInferredProject(LogTree? logger) => UpdateInferredProjectRoots(CollectInferredProjectRoots(), logger);
    // projectcollectionbuilder.go:627/634.
    internal void DidChangeContentMapperContributions(LogTree? logger)
    {
        CleanupInferredProject(logger);
        if (inferredProject.Value() != null)
            UpdateProgram(inferredProject, logger);
    }

    private void EnsureInferredProjectIncludesClosedFile(RootedFilePath name, LogTree? logger)
    {
        var names = CollectInferredProjectRoots() ?? new();
        names.Add(name);
        UpdateInferredProjectRoots(names, logger);
        if (inferredProject.Value() != null)
            UpdateProgram(inferredProject, logger);
    }

    // projectcollectionbuilder.go:647/653.
    internal void DidRequestFile(DocumentUri uri, bool configuredOnly, LogTree? logger)
    {
        var name = uri.FileName();
        DidRequestFile(name, ToPathKey(name), configuredOnly, logger);
    }

    private void DidRequestFile(RootedFilePath name, PathKey path, bool configuredOnly, LogTree? logger)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        if (defaultProjectsInvalidated)
        {
            EnsureConfiguredProjectAndAncestorsForFile(name, path, logger);
            if (!IsOpenFile(path))
                return;
        }

        if (IsOpenFile(path))
        {
            bool changes = programStructureChanged;
            var result = FindDefaultProject(name, path);
            if (result != null)
            {
                changes = UpdateProgram(result, logger) || changes;
                if (result.Value()?.ContainsFile(path) == true)
                {
                    if (changes)
                    {
                        CleanupInferredProject(logger);
                        if (inferredProject.Value() != null)
                            UpdateProgram(inferredProject, logger);
                    }

                    return;
                }
            }

            configuredProjects.Range(entry =>
            {
                changes = UpdateProgram(entry, logger) || changes;
                return true;
            });
            if (changes)
                CleanupInferredProject(logger);
            if (inferredProject.Value() != null)
                UpdateProgram(inferredProject, logger);
        }
        else
        {
            var result = EnsureConfiguredProjectAndAncestorsForFile(name, path, logger);
            if (result.Project == null && !configuredOnly)
                EnsureInferredProjectIncludesClosedFile(name, logger);
        }

        logger?.Logf("Completed file request for %s in %v", name, System.Diagnostics.Stopwatch.GetElapsedTime(started));
    }

    // projectcollectionbuilder.go:715.
    internal void DidRequestProject(ID id, LogTree? logger)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        if (id.Inferred().Ok)
        {
            if (inferredProject.Value() != null)
                UpdateProgram(inferredProject, logger);
        }
        else if (id.Synthetic()is var (synthetic, syntheticOk) && syntheticOk)
        {
            var(entry, ok) = syntheticProjects.Load(synthetic);
            if (ok)
                UpdateProgram(entry!, logger);
        }
        else if (id.Configured()is var (configured, configuredOk) && configuredOk)
        {
            var(entry, ok) = configuredProjects.Load(configured);
            if (ok)
                UpdateProgram(entry!, logger);
        }

        logger?.Logf("Completed project update request for %s in %v", id, System.Diagnostics.Stopwatch.GetElapsedTime(started));
    }

    // projectcollectionbuilder.go:738/776. Recursively queued jobs remain in the same genuine WorkGroup.
    internal void DidRequestProjectTrees(ProjectTreeRequest request, LogTree? logger)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        Slice<ConfiguredProjectID> current = default;
        configuredProjects.Range(entry =>
        {
            current = current.Append(entry.Key());
            return true;
        });
        var seen = new TypeScript.Collections.SyncSet<ConfiguredProjectID>();
        var group = Core.NewWorkGroup(false);
        foreach (var id in current)
            group.Queue(() =>
            {
                var(entry, ok) = configuredProjects.Load(id);
                if (!ok)
                    return;
                var project = entry!.Value();
                if (project != null && (request.IsAllProjects() || project.HasPotentialProjectReference(request)))
                    UpdateProgram(entry, logger);
                EnsureProjectTree(group, entry, request, seen, logger);
            });
        group.RunAndWait();
        CleanupInferredProject(logger);
        if (inferredProject.Value() != null)
            UpdateProgram(inferredProject, logger);
        logger?.Logf("Completed project tree request for %v in %v", request.Projects(), System.Diagnostics.Stopwatch.GetElapsedTime(started));
    }

    private void EnsureProjectTree(IWorkGroup group, Dirty.SyncMapEntry<ConfiguredProjectID, Project> entry, ProjectTreeRequest request, TypeScript.Collections.SyncSet<ConfiguredProjectID> seen, LogTree? logger)
    {
        if (!seen.AddIfAbsent(entry.Key()))
            return;
        var project = entry.Value();
        if (project?.Program is not { } program || program.CommandLine().CompilerOptions().DisableReferencedProjectLoad == Tristate.True)
            return;
        foreach (var child in program.GetResolvedProjectReferences())
        {
            if (child == null)
                continue;
            group.Queue(() =>
            {
                if (!request.IsAllProjects() && program.RangeResolvedProjectReferenceInChildConfig(child, (path, config, parent, index) => !request.IsProjectReferenced(path)))
                    return;
                var childEntry = FindOrCreateProject(child.ConfigName(), PathKey.FromValue(child.ConfigFile!.SourceFile.PathKeyValue()), ProjectLoadKind.Create, logger)!;
                UpdateProgram(childEntry, logger);
                EnsureProjectTree(group, childEntry, request, seen, logger);
            });
        }
    }

    // projectcollectionbuilder.go:830. Only still-matching ATA demands can publish an installation result.
    internal void DidUpdateATAState(Dictionary<ID, ATAStateChange>? changes, LogTree? logger)
    {
        void Update(Dirty.IValue<Project> entry, ATAStateChange change)
        {
            entry.ChangeIf(project => project != null && change.TypingsInfo!.Equals(project.ComputeTypingsInfo()), project =>
            {
                project.installedTypingsInfo = change.TypingsInfo;
                project.typingsFiles = change.TypingsFiles;
                var globs = ProjectWatches.GetTypingsLocationsGlobs(change.TypingsFilesToWatch, sessionOptions.TypingsLocation, sessionOptions.CurrentDirectory, fs.FS().CaseSensitivity());
                project.typingsWatch = ProjectWatches.Clone(project.typingsWatch, globs);
                project.dirty = true;
                project.dirtyFilePath = default;
            });
        }

        if (changes == null)
            return;
        foreach (var(id, change)in changes)
        {
            logger?.Embed(change.Logs!);
            if (id.Inferred().Ok)
                Update(inferredProject, change);
            else if (id.Synthetic()is var (synthetic, syntheticOk) && syntheticOk)
            {
                var(entry, ok) = syntheticProjects.Load(synthetic);
                if (ok)
                    Update(entry!, change);
            }
            else if (id.Configured()is var (configured, configuredOk) && configuredOk)
            {
                var(entry, ok) = configuredProjects.Load(configured);
                if (ok)
                    Update(entry!, change);
            }

            logger?.Logf("Updated ATA state for project %s", id);
        }
    }

    // projectcollectionbuilder.go:881/891.
    internal void DidChangeCustomConfigFileName(LogTree? logger)
    {
        if (!configFileRegistryBuilder.DidChangeCustomConfigFileName(logger))
            return;
        fileDefaultProjects = null;
        defaultProjectsInvalidated = true;
        programStructureChanged = true;
    }

    internal void DidChangeUserPreferences(UserPreferences old, UserPreferences current, LogTree? logger)
    {
        if (old.Locale == current.Locale)
            return;
        ForEachProject(entry =>
        {
            entry.Change(project =>
            {
                project.dirty = true;
                project.dirtyFilePath = default;
                logger?.Logf("Marking project as dirty due to locale change: %s", project.ID());
            });
            return true;
        });
    }

    // projectcollectionbuilder.go:907.
    private bool MarkProjectsAffectedByConfigChanges(ChangeFileResult result, LogTree? logger)
    {
        if (result.AffectedProjects != null)
            foreach (var id in result.AffectedProjects)
            {
                Dirty.IValue<Project>? project = null;
                if (id.Inferred().Ok)
                    project = inferredProject;
                else
                {
                    var(synthetic, syntheticOk) = id.Synthetic();
                    if (syntheticOk)
                        project = syntheticProjects.Load(synthetic).Entry;
                    if (project == null)
                    {
                        var(configured, configuredOk) = id.Configured();
                        if (configuredOk)
                            project = configuredProjects.Load(configured).Entry;
                    }
                }

                if (project?.Value() == null)
                    throw new InvalidOperationException(LogFormat.Sprintf("project %s affected by config change not found", id));
                project.ChangeIf(p => !p.dirty || p.dirtyFilePath.AsValue().Length != 0, p =>
                {
                    p.dirty = true;
                    p.dirtyFilePath = default;
                    logger?.Logf("Marking project %s as dirty due to change affecting config", id);
                });
            }

        bool changes = false;
        if (result.AffectedFiles != null)
            foreach (var path in result.AffectedFiles)
            {
                if (overlays?.TryGetValue(path, out var overlay) != true)
                    continue;
                EnsureConfiguredProjectAndAncestorsForFile(overlay!.FileName(), path, logger);
                changes = true;
            }

        return changes;
    }

    // projectcollectionbuilder.go:958/977.
    private Dirty.IValue<Project>? FindDefaultProject(RootedFilePath name, PathKey path)
    {
        var configured = FindDefaultConfiguredProject(name, path);
        if (configured != null)
            return configured;
        if (fileDefaultProjects?.TryGetValue(path, out var id) == true && id.Inferred().Ok)
            return inferredProject;
        var project = inferredProject.Value();
        if (project?.ContainsFile(path) != true)
            return null;
        (fileDefaultProjects ??= new())[path] = Projects.InferredProjectID.AsID();
        return inferredProject;
    }

    private Dirty.SyncMapEntry<ConfiguredProjectID, Project>? FindDefaultConfiguredProject(RootedFilePath name, PathKey path)
    {
        if (fileDefaultProjects?.TryGetValue(path, out var id) == true)
        {
            var(configured, ok) = id.Configured();
            if (ok)
            {
                var(entry, loaded) = configuredProjects.Load(configured);
                if (loaded)
                    return entry;
            }
        }

        Slice<PathKey> paths = default;
        var projects = new Dictionary<PathKey, Dirty.SyncMapEntry<ConfiguredProjectID, Project>>();
        configuredProjects.Range(entry =>
        {
            var projectPath = entry.Key().PathKey();
            paths = paths.Append(projectPath);
            projects[projectPath] = entry;
            return true;
        });
        GoSlices.SortFunc(paths, static (a, b) => ByteStringKey.FromValue(a.AsValue()).CompareTo(ByteStringKey.FromValue(b.AsValue())));
        var(result, multiple) = ProjectCollection.FindDefaultConfiguredProjectFromProgramInclusion(path, paths, key => projects[key].Value());
        if (multiple)
        {
            var selected = FindOrCreateDefaultConfiguredProjectForFile(name, path, ProjectLoadKind.Find, null).Project;
            if (selected != null)
                return selected;
        }

        return projects.GetValueOrDefault(result);
    }

    // projectcollectionbuilder.go:1009/1017.
    private SearchResult EnsureConfiguredProjectAndAncestorsForFile(RootedFilePath name, PathKey path, LogTree? logger)
    {
        var result = FindOrCreateDefaultConfiguredProjectForFile(name, path, ProjectLoadKind.Create, logger);
        if (result.Project != null && IsOpenFile(path))
            CreateAncestorTree(name, path, ref result, logger);
        return result;
    }

    private void CreateAncestorTree(RootedFilePath name, PathKey path, ref SearchResult result, LogTree? logger)
    {
        var project = result.Project!.Value();
        while (true)
        {
            if (project.CommandLine != null && (project.CommandLine.CompilerOptions().Composite != Tristate.True || project.CommandLine.CompilerOptions().DisableSolutionSearching == Tristate.True))
                return;
            var ancestorName = configFileRegistryBuilder.GetAncestorConfigFileName(name, path, project.ConfigFileName(), logger);
            if (ancestorName.AsValue().Length == 0)
                return;
            var ancestorPath = ToPathKey(ancestorName);
            var ancestor = FindOrCreateProject(ancestorName, ancestorPath, ProjectLoadKind.Create, logger);
            if (ancestor == null)
                return;
            (result.Retain ??= new()).Add(ancestorPath);
            if (ancestor.Value().CommandLine == null && (project.CommandLine == null || project.CommandLine.CompilerOptions().Composite == Tristate.True))
            {
                var current = project;
                ancestor.Change(value => value.SetPotentialProjectReference(current.configFilePath));
            }

            project = ancestor.Value();
        }
    }

    private readonly record struct SearchNode(RootedFilePath ConfigFileName, ProjectLoadKind LoadKind, LogTree? Logger);
    private readonly record struct SearchNodeKey(RootedFilePath ConfigFileName, ProjectLoadKind LoadKind);
    private struct SearchResult
    {
        internal Dirty.SyncMapEntry<ConfiguredProjectID, Project>? Project;
        internal HashSet<PathKey>? Retain;
    }

    // projectcollectionbuilder.go:1072. Stable level order gives deterministic retained paths.
    private SearchResult FindOrCreateDefaultConfiguredProjectWorker(RootedFilePath name, PathKey path, RootedFilePath configName, ProjectLoadKind kind, TypeScript.Collections.SyncSet<SearchNodeKey>? visited, SearchResult? fallback, LogTree? logger)
    {
        var configs = new TypeScript.Collections.SyncMap<PathKey, ParsedCommandLine>();
        visited ??= new();
        var search = Core.BreadthFirstSearchParallelEx(new SearchNode(configName, kind, logger), node =>
        {
            var(config, ok) = configs.Load(ToPathKey(node.ConfigFileName));
            if (!ok || (config.ProjectReferences()?.Count ?? 0) == 0)
                return default(Slice<SearchNode>);
            var referenceKind = config.CompilerOptions().DisableReferencedProjectLoad == Tristate.True ? ProjectLoadKind.Find : node.LoadKind;
            var references = config.ResolvedProjectReferencePaths();
            var referenceLogger = (references?.Count ?? 0) > 0 ? node.Logger?.Fork(LogFormat.Sprintf("Searching %d project references of %s", references!.Count, node.ConfigFileName)) : null;
            var children = references == null ? default : Slice<SearchNode>.Make(references.Count);
            for (int i = 0; i < (references?.Count ?? 0); i++)
                children[i] = new(references![i], referenceKind, referenceLogger?.Fork(LogFormat.Sprintf("Searching project reference %s", references![i])));
            return children;
        }, node =>
        {
            var configPath = ToPathKey(node.ConfigFileName);
            var config = configFileRegistryBuilder.FindOrAcquireConfigForFile(node.ConfigFileName, configPath, path, node.LoadKind, node.Logger?.Fork("Acquiring config for open file"));
            if (config == null)
            {
                node.Logger?.Log("Config file for project does not already exist");
                return (false, false);
            }

            configs.Store(configPath, config);
            if ((config.FileNames()?.Count ?? 0) == 0)
            {
                node.Logger?.Log("Project does not contain file (no root files)");
                return (false, false);
            }

            if (config.CompilerOptions().Composite == Tristate.True && !config.FilePaths().Contains(path))
            {
                node.Logger?.Log("Project does not contain file (by composite config inclusion)");
                return (false, false);
            }

            var project = FindOrCreateProject(node.ConfigFileName, configPath, node.LoadKind, node.Logger);
            if (project == null)
            {
                node.Logger?.Log("Project does not already exist");
                return (false, false);
            }

            if (node.LoadKind == ProjectLoadKind.Create)
                UpdateProgram(project, node.Logger);
            if (project.Value().ContainsFile(path))
            {
                bool direct = !project.Value().IsSourceFromProjectReference(path);
                node.Logger?.Logf("Project contains file %s", direct ? "directly" : "as a source of a referenced project");
                return (true, direct);
            }

            node.Logger?.Log("Project does not contain file");
            return (false, false);
        }, new BreadthFirstSearchOptions<SearchNodeKey, SearchNode>(visited, level => level.Range(node =>
        {
            if (node.LoadKind == ProjectLoadKind.Find && level.Has(new(node.ConfigFileName, ProjectLoadKind.Create)))
                level.Delete(new(node.ConfigFileName, node.LoadKind));
            return true;
        })), static node => new SearchNodeKey(node.ConfigFileName, node.LoadKind));
        HashSet<PathKey>? retain = null;
        Dirty.SyncMapEntry<ConfiguredProjectID, Project>? selected = null;
        if (search.Path.Count > 0)
        {
            selected = configuredProjects.Load(Projects.ConfiguredProjectIDFromPathKey(ToPathKey(search.Path[0].ConfigFileName))).Entry;
            foreach (var node in search.Path)
                (retain ??= new()).Add(ToPathKey(node.ConfigFileName));
        }

        if (search.Stopped)
            return new()
            {
                Project = selected,
                Retain = retain
            };
        if (selected != null)
            fallback = new()
            {
                Project = selected,
                Retain = retain
            };
        var(root, loaded) = configs.Load(ToPathKey(configName));
        if (loaded && root.CompilerOptions().DisableSolutionSearching == Tristate.True && fallback != null)
            return fallback.Value;
        var ancestor = configFileRegistryBuilder.GetAncestorConfigFileName(name, path, configName, logger);
        if (ancestor.AsValue().Length != 0)
            return FindOrCreateDefaultConfiguredProjectWorker(name, path, ancestor, kind, visited, fallback, logger?.Fork(LogFormat.Sprintf("Searching ancestor config file at %s", ancestor)));
        if (fallback != null)
            return fallback.Value;
        visited.Range(node =>
        {
            (retain ??= new()).Add(ToPathKey(node.ConfigFileName));
            return true;
        });
        return new()
        {
            Retain = retain
        };
    }

    // projectcollectionbuilder.go:1230/1275.
    private SearchResult FindOrCreateDefaultConfiguredProjectForFile(RootedFilePath name, PathKey path, ProjectLoadKind kind, LogTree? logger)
    {
        if (fileDefaultProjects?.TryGetValue(path, out var id) == true)
        {
            if (id.Inferred().Ok)
                return default;
            return new()
            {
                Project = configuredProjects.Load(id.Configured().ID).Entry
            };
        }

        var configName = configFileRegistryBuilder.GetConfigFileNameForFile(name, path, logger);
        if (configName.AsValue().Length == 0)
            return default;
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        var result = FindOrCreateDefaultConfiguredProjectWorker(name, path, configName, kind, null, null, logger?.Fork(LogFormat.Sprintf("Searching for default configured project for %s", name)));
        if (result.Project != null)
            (fileDefaultProjects ??= new())[path] = result.Project.Value().ID();
        if (logger != null)
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started);
            if (result.Project != null)
                logger.Logf("Found default configured project for %s: %s (in %v)", name, result.Project.Value().ConfigFileName(), elapsed);
            else
                logger.Logf("No default configured project found for %s (searched in %v)", name, elapsed);
        }

        return result;
    }

    private Dirty.SyncMapEntry<ConfiguredProjectID, Project>? FindOrCreateProject(RootedFilePath name, PathKey path, ProjectLoadKind kind, LogTree? logger)
    {
        var id = Projects.ConfiguredProjectIDFromPathKey(path);
        return kind == ProjectLoadKind.Find ? configuredProjects.Load(id).Entry : configuredProjects.LoadOrStore(id, Project.NewConfiguredProject(name, path, this, logger)).Entry;
    }

    // projectcollectionbuilder.go:1289. Core.Filter keeps the original owner when all roots pass.
    private bool UpdateInferredProjectRoots(List<RootedFilePath>? names, LogTree? logger)
    {
        names = Core.Filter(names, IsSupportedInInferredProject);
        List<ProjectReference>? references = null;
        Slice<Diagnostic> diagnostics = default;
        var project = inferredProject.Value();
        if (project != null)
        {
            references = project.CommandLine!.ProjectReferences();
            diagnostics = project.CommandLine.Errors;
        }

        return UpdateInferredProject(names, compilerOptionsForInferredProjects, references, diagnostics, inferredContentMappers, logger);
    }

    // projectcollectionbuilder.go:1302.
    private Dirty.SyncMapEntry<SyntheticProjectID, Project> UpdateOrCreateSyntheticProject(SyntheticProjectID id, List<RootedFilePath>? names, CompilerOptions? options, List<ProjectReference>? references, Slice<Diagnostic> diagnostics, IModuleResolverFactory? resolverFactory, ulong resolverID, List<Mapper>? mappers, LogTree? logger)
    {
        var(entry, loaded) = syntheticProjects.Load(id);
        if (!loaded)
        {
            var project = Project.NewSyntheticProject(id, sessionOptions.CurrentDirectory, options!, names, references, mappers, this, logger);
            project.CommandLine!.Errors = diagnostics;
            project.moduleResolverFactory = resolverFactory;
            project.moduleResolverID = resolverID;
            return syntheticProjects.LoadOrStore(id, project).Entry!;
        }

        var current = entry!.Value();
        options ??= current.CommandLine!.CompilerOptions();
        var newLine = Project.NewInferredProjectCommandLine(options, names, references, mappers, current.projectDirectory, fs.FS().CaseSensitivity());
        newLine.Errors = diagnostics;
        entry.ChangeIf(project => !ListEqual(project.CommandLine!.FileNames(), newLine.FileNames()) || !project.CommandLine.CompilerOptions().Equals(options) || !ProjectReferencesEqual(project.CommandLine.ProjectReferences(), references) || !ProjectDiagnosticEquality.Equal(project.CommandLine.Errors, diagnostics) || !ListEqual(project.CommandLine.ContentMappers(), newLine.ContentMappers()) || project.moduleResolverID != resolverID, project =>
        {
            logger?.Logf("Updating synthetic project config with %d root files", names?.Count ?? 0);
            project.SetCommandLine(newLine);
            project.moduleResolverFactory = resolverFactory;
            project.moduleResolverID = resolverID;
        });
        return entry;
    }

    // projectcollectionbuilder.go:1357/1373/1393. Clone is required upstream before sorting transferred roots.
    private bool UpdateInferredProject(List<RootedFilePath>? names, CompilerOptions? options, List<ProjectReference>? references, Slice<Diagnostic> diagnostics, List<Mapper>? mappers, LogTree? logger)
    {
        if ((names?.Count ?? 0) == 0)
            return DeleteInferredProject(logger);
        names = CloneList(names);
        names!.Sort(static (a, b) => ByteStringKey.FromValue(a.AsValue()).CompareTo(ByteStringKey.FromValue(b.AsValue())));
        return UpdateOrCreateInferredProject(names, options, references, diagnostics, mappers, logger);
    }

    private bool DeleteInferredProject(LogTree? logger)
    {
        var project = inferredProject.Value();
        if (project == null)
            return false;
        logger?.Log("Deleting inferred project");
        project.Program?.RangeResolvedProjectReference((path, config, parent, index) =>
        {
            configFileRegistryBuilder.ReleaseConfigForProject(path, project.ID());
            return true;
        });
        inferredProject.Delete();
        return true;
    }

    private bool UpdateOrCreateInferredProject(List<RootedFilePath>? names, CompilerOptions? options, List<ProjectReference>? references, Slice<Diagnostic> diagnostics, List<Mapper>? mappers, LogTree? logger)
    {
        var project = inferredProject.Value();
        if (project == null)
        {
            project = Project.NewInferredProject(sessionOptions.CurrentDirectory, options, names, references, mappers, this, logger);
            project.CommandLine!.Errors = diagnostics;
            inferredProject.Set(project);
            return true;
        }

        options ??= project.CommandLine!.CompilerOptions();
        var newLine = Project.NewInferredProjectCommandLine(options, names, references, mappers, project.projectDirectory, fs.FS().CaseSensitivity());
        newLine.Errors = diagnostics;
        return inferredProject.ChangeIf(p => !ListEqual(p.CommandLine!.FileNames(), newLine.FileNames()) || !p.CommandLine.CompilerOptions().Equals(options) || !ProjectReferencesEqual(p.CommandLine.ProjectReferences(), references) || !ProjectDiagnosticEquality.Equal(p.CommandLine.Errors, diagnostics) || !ListEqual(p.CommandLine.ContentMappers(), newLine.ContentMappers()), p =>
        {
            logger?.Logf("Updating inferred project config with %d root files", names?.Count ?? 0);
            p.SetCommandLine(newLine);
        });
    }

    private static bool ListEqual<T>(List<T>? left, List<T>? right)
    {
        if ((left?.Count ?? 0) != (right?.Count ?? 0))
            return false;
        return left == null || left.Count == 0 || right != null && left.SequenceEqual(right);
    }

    // projectcollectionbuilder.go:1442.
    private static bool ProjectReferencesEqual(List<ProjectReference>? left, List<ProjectReference>? right)
    {
        if ((left?.Count ?? 0) != (right?.Count ?? 0))
            return false;
        if (left == null)
            return true;
        for (int i = 0; i < left.Count; i++)
        {
            var a = left[i];
            var b = right![i];
            if (a == null || b == null)
            {
                if (!ReferenceEquals(a, b))
                    return false;
            }
            else if (a.Path != b.Path || a.Circular != b.Circular)
                return false;
        }

        return true;
    }

    // projectcollectionbuilder.go:1451.
    private bool IsSupportedInInferredProject(RootedFilePath name)
    {
        if (name.IsDynamic() || Core.GetScriptKindFromFileName(name) != ScriptKind.Unknown)
            return true;
        var file = fs.GetFile(name);
        if (file?.IsOverlay() == true && name.AnyExtensionValue(null, CaseSensitivity.CaseSensitive).Length == 0)
            return true;
        foreach (var extension in inferredContentMapperExtensions)
            if (name.AsValue().Length > extension.Length && name.AsValue().EndsWith(extension))
                return true;
        return false;
    }

    // projectcollectionbuilder.go:1463. Both inspections/changes retain the original dirty-entry lock scopes.
    private bool UpdateProgram(Dirty.IValue<Project> entry, LogTree? logger)
    {
        bool update = false, delete = false, filesChanged = false, notified = false;
        var id = entry.Value().ID();
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        ByteStringValue display = default;
        entry.Locked(locked =>
        {
            if (locked.Value().Kind == ProjectKind.Configured)
            {
                var line = configFileRegistryBuilder.AcquireConfigForProject(locked.Value().ConfigFileName(), locked.Value().configFilePath, locked.Value(), logger?.Fork("Acquiring config for project"));
                if (line == null)
                {
                    delete = true;
                    filesChanged = true;
                    return;
                }

                if (!ReferenceEquals(locked.Value().CommandLine, line))
                {
                    update = true;
                    locked.Change(project => project.SetCommandLine(line));
                }
            }

            if (!update)
                update = locked.Value().dirty;
            if (update && client != null)
            {
                display = locked.Value().DisplayName(sessionOptions.CurrentDirectory);
                notified = true;
            }
        });
        if (notified)
            client?.ProgressStart(Diagnostics.Project_0, display);
        if (delete)
            DeleteProject(entry, logger);
        if (update)
            entry.Locked(locked => locked.Change(project =>
            {
                var oldHost = project.host;
                var oldProgram = project.Program;
                var oldPool = project.checkerPool;
                project.host = ProjectCompilerHost.NewCompilerHost(project, this, logger?.Fork("CompilerHost"));
                var result = project.CreateProgram();
                Slice<RootedFilePath> watched = default;
                if (project.CommandLine!.ContentMappers()is { } mappers)
                    foreach (var mapper in mappers)
                        if (mapper.Package.Length != 0 && mapper.ContributionID.Length == 0 && mapper.PackageDirectory.AsValue().Length != 0)
                            watched = watched.Append(mapper.PackageDirectory.ResolveFile("package.json"));
                bool mapped = false;
                foreach (var file in result.Program.SourceFiles())
                    if (file.ContentMapper().Length != 0)
                    {
                        mapped = true;
                        break;
                    }

                if (mapped && project.host.ContentMapperProject()is { } mapperProject)
                {
                    var(dynamicFiles, _) = mapperProject.WatchedFiles();
                    if (dynamicFiles != null)
                        foreach (var name in dynamicFiles)
                            watched = watched.Append(name);
                }

                GoSlices.SortFunc(watched, static (a, b) => ByteStringKey.FromValue(a.AsValue()).CompareTo(ByteStringKey.FromValue(b.AsValue())));
                watched = CompactWatched(watched);
                project.contentMapperWatch = project.contentMapperWatch.Clone(watched);
                project.contentMapperWatchedFiles = new(watched.Count);
                foreach (var name in watched)
                    project.contentMapperWatchedFiles.Add(baseCollection.caseSensitivity.PathKey(name.AsPath()));
                project.Program = result.Program;
                project.checkerPool = (ProjectCheckerPool)result.Program.GetCheckerPool();
                project.ProgramUpdateKind = result.UpdateKind;
                project.ProgramLastUpdate = newSnapshotID;
                if (result.UpdateKind == ProgramUpdateKind.Cloned)
                    project.host.sourceFS.seenFiles = oldHost.sourceFS.seenFiles;
                if (result.UpdateKind == ProgramUpdateKind.NewFiles)
                {
                    filesChanged = true;
                    project.programFilesWatch = project.CloneWatchers();
                }

                project.dirty = false;
                project.dirtyFilePath = default;
                ReleaseDroppedProjectReferences(oldProgram, result.Program, project.ID());
                oldPool?.Discard();
            }));
        if (notified)
            client?.ProgressFinish(Diagnostics.Project_0, display);
        if (update)
            logger?.Logf("Program update for %s completed in %v", id, System.Diagnostics.Stopwatch.GetElapsedTime(started));
        return filesChanged;
    }

    // projectcollectionbuilder.go:1566. One changed file permits reuse; package identity/deletes/multiple files invalidate it.
    private void MarkFilesChanged(Dirty.IValue<Project> entry, Slice<PathKey> paths, FileChangeType type, LogTree? logger)
    {
        bool dirty = false;
        PathKey dirtyPath = default;
        entry.ChangeIf(project =>
        {
            if (project.Program == null || project.dirty && project.dirtyFilePath.AsValue().Length == 0)
                return false;
            dirtyPath = project.dirtyFilePath;
            foreach (var path in paths)
            {
                if (project.ContainsFile(path))
                {
                    dirty = true;
                    if (type == FileChangeType.Deleted || path.BaseNameValue().EqualsValue(ByteStringValue.FromString("package.json")))
                    {
                        dirtyPath = default;
                        break;
                    }

                    if (dirtyPath.AsValue().Length == 0)
                        dirtyPath = path;
                    else if (dirtyPath != path)
                    {
                        dirtyPath = default;
                        break;
                    }
                }
                else if (project.host != null && (type == FileChangeType.Created && project.host.sourceFS.SeenFileOrMissingParentDirectory(path) || type != FileChangeType.Created && project.host.sourceFS.SeenFile(path)))
                {
                    dirty = true;
                    dirtyPath = default;
                    break;
                }
            }

            return dirty || project.dirtyFilePath != dirtyPath;
        }, project =>
        {
            project.dirty = true;
            project.dirtyFilePath = dirtyPath;
            if (dirtyPath.AsValue().Length != 0)
                logger?.Logf("Marking project %s as dirty due to changes in %s", project.ID(), dirtyPath);
            else
                logger?.Logf("Marking project %s as dirty", project.ID());
        });
    }

    // projectcollectionbuilder.go:1620/1642.
    private void DeleteProject(Dirty.IValue<Project> entry, LogTree? logger)
    {
        var project = entry.Value();
        var id = project.ID();
        logger?.Logf("Deleting %s project: %s", Projects.KindString(project.Kind), id);
        project.Program?.RangeResolvedProjectReference((path, config, parent, index) =>
        {
            configFileRegistryBuilder.ReleaseConfigForProject(path, id);
            return true;
        });
        if (project.Kind == ProjectKind.Configured)
            configFileRegistryBuilder.ReleaseConfigForProject(project.ConfigFilePath(), id);
        entry.Delete();
    }

    private void ReleaseDroppedProjectReferences(Program? old, Program? current, ID id)
    {
        if (old == null || ReferenceEquals(old, current))
            return;
        HashSet<PathKey>? references = null;
        current?.RangeResolvedProjectReference((path, config, parent, index) =>
        {
            (references ??= new()).Add(path);
            return true;
        });
        old.RangeResolvedProjectReference((path, config, parent, index) =>
        {
            if (references?.Contains(path) != true)
                configFileRegistryBuilder.ReleaseConfigForProject(path, id);
            return true;
        });
    }

    // slices.Compact keeps the original backing owner, clears removed tail entries.
    private static Slice<RootedFilePath> CompactWatched(Slice<RootedFilePath> files)
    {
        if (files.Count < 2)
            return files;
        int write = 1;
        for (int read = 1; read < files.Count; read++)
            if (files[read] != files[write - 1])
                files[write++] = files[read];
        files.AsSpan().Slice(write).Clear();
        return files.Slice2(0, write);
    }
}

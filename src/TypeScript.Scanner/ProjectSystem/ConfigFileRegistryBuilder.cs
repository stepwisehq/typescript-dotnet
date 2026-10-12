// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/project/configfileregistrybuilder.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Collections;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Vfs;
using TypeScript.ProjectSystem.Logging;
using ConfigParser = TypeScript.TsOptions.TsOptions;

namespace TypeScript.ProjectSystem;
internal readonly record struct ChangeFileResult(HashSet<ID>? AffectedProjects, HashSet<PathKey>? AffectedFiles)
{
    internal bool IsEmpty() => (AffectedProjects?.Count ?? 0) == 0 && (AffectedFiles?.Count ?? 0) == 0;
}

internal sealed class ConfigFileRegistryBuilder : IExtendedConfigCache
{
    internal bool hasRelativePatternCapability;
    internal SourceFS fs = null!;
    internal Func<PathKey, bool> isOpenFile = null!;
    internal OwnerCache<PathKey, ExtendedConfigCacheEntry, ExtendedConfigParseArgs> extendedConfigCache = null!;
    internal ulong snapshotID;
    internal SessionOptions sessionOptions = null!;
    internal ByteStringValue customConfigFileName;
    internal ConfigFileRegistry baseRegistry = null!;
    internal Dirty.SyncMap<PathKey, ConfigFileEntry> configs = null!;
    internal Dirty.Map<PathKey, ConfigFileNames> configFileNames = null!;
    internal bool customConfigFileNameChanged;
    private readonly SemaphoreSlim contentMappersMu = new(1, 1);
    private ConfiguredContentMappers? allConfiguredContentMappers;
    // configfileregistrybuilder.go:42.
    internal static ConfigFileRegistryBuilder New(bool relative, SnapshotFSBuilder fs, Func<PathKey, bool> isOpen, ConfigFileRegistry old, OwnerCache<PathKey, ExtendedConfigCacheEntry, ExtendedConfigParseArgs> cache, ulong id, SessionOptions options, ByteStringValue custom, LogTree? logger) => new()
    {
        hasRelativePatternCapability = relative,
        fs = new(false, fs),
        isOpenFile = isOpen,
        baseRegistry = old,
        sessionOptions = options,
        extendedConfigCache = cache,
        snapshotID = id,
        customConfigFileName = custom,
        customConfigFileNameChanged = !custom.EqualsValue(old.CustomConfigFileName),
        allConfiguredContentMappers = old.ContentMappers(),
        configs = new(old.Configs),
        configFileNames = new(old.Names)
    };
    // configfileregistrybuilder.go:77.
    internal ConfigFileRegistry FinalizeRegistry()
    {
        bool changed = false;
        var result = baseRegistry;
        void EnsureCloned()
        {
            if (!changed)
            {
                result = result.Clone();
                changed = true;
            }
        }

        var(configValues, configsChanged) = configs.FinalizeValue();
        if (configsChanged)
        {
            EnsureCloned();
            result.Configs = configValues;
            result.AllConfiguredContentMappers = ContentMappers();
        }

        var(nameValues, namesChanged) = configFileNames.FinalizeValue();
        if (namesChanged)
        {
            EnsureCloned();
            result.Names = nameValues;
        }

        if (customConfigFileNameChanged)
        {
            EnsureCloned();
            result.CustomConfigFileName = customConfigFileName;
        }

        return result;
    }

    // configfileregistrybuilder.go:111/130.
    internal ConfiguredContentMappers ContentMappers()
    {
        contentMappersMu.Wait();
        try
        {
            if (allConfiguredContentMappers == null)
            {
                Slice<ParsedCommandLine> commands = default;
                configs.Range(entry =>
                {
                    if (entry.Value().CommandLine is { } line)
                        commands = commands.Append(line);
                    return true;
                });
                allConfiguredContentMappers = ConfiguredContentMappers.Collect(commands);
            }

            return allConfiguredContentMappers;
        }
        finally
        {
            contentMappersMu.Release();
        }
    }

    private void InvalidateContentMappers()
    {
        contentMappersMu.Wait();
        try
        {
            allConfiguredContentMappers = null;
        }
        finally
        {
            contentMappersMu.Release();
        }
    }

    // configfileregistrybuilder.go:136.
    internal ParsedCommandLine? FindOrAcquireConfigForFile(RootedFilePath name, PathKey configPath, PathKey filePath, ProjectLoadKind kind, LogTree? logger) => kind switch
    {
        ProjectLoadKind.Find => configs.Load(configPath)is var (entry, ok) && ok ? entry!.Value().CommandLine : null,
        ProjectLoadKind.Create => AcquireConfigForFile(name, configPath, filePath, logger),
        _ => throw new InvalidOperationException(LogFormat.Sprintf("unknown project load kind: %d", (int)kind))};
    // configfileregistrybuilder.go:158. Called only while the dirty entry's lock is held.
    private bool ReloadIfNeeded(ConfigFileEntry entry, RootedFilePath name, PathKey path, LogTree? logger)
    {
        var old = entry.CommandLine;
        switch (entry.PendingReload)
        {
            case PendingReload.FileNames:
                logger?.Log(LogFormat.Sprintf("Reloading file names for config: %s", name));
                entry.CommandLine = entry.CommandLine!.ReloadFileNamesOfParsedCommandLine(fs);
                break;
            case PendingReload.Full:
                logger?.Log(LogFormat.Sprintf("Loading config file: %s", name));
                var options = sessionOptions.RunExternalCode ? new CompilerOptions
                {
                    RunExternalCode = Tristate.True
                }

                : null;
                entry.CommandLine = ConfigParser.GetParsedCommandLineOfConfigFilePath(name, path, options, null, fs, this).Parsed;
                UpdateExtendingConfigs(path, entry.CommandLine, old);
                UpdateRootFilesWatch(name, entry);
                logger?.Log("Finished loading config file");
                break;
            default:
                return false;
        }

        entry.PendingReload = PendingReload.None;
        return !ReferenceEquals(old, entry.CommandLine);
    }

    // configfileregistrybuilder.go:184.
    private void UpdateExtendingConfigs(PathKey extending, ParsedCommandLine? current, ParsedCommandLine? old)
    {
        HashSet<PathKey>? extended = null;
        if (current?.ExtendedSourceFiles()is { } currentNames)
            foreach (var name in currentNames)
            {
                var path = fs.CaseSensitivity().PathKey(name.AsPath());
                (extended ??= new()).Add(path);
                var(entry, loaded) = configs.LoadOrStore(path, ConfigFileEntry.NewExtended(name, extending));
                if (loaded)
                    entry!.ChangeIf(config => config.RetainingConfigs?.Contains(extending) != true, config => (config.RetainingConfigs ??= new()).Add(extending));
            }

        if (old?.ExtendedSourceFiles()is { } oldNames)
            foreach (var name in oldNames)
            {
                var path = fs.CaseSensitivity().PathKey(name.AsPath());
                if (extended?.Contains(path) == true)
                    continue;
                var(entry, ok) = configs.Load(path);
                if (ok)
                    entry!.ChangeIf(config => config.RetainingConfigs?.Contains(extending) == true, config => config.RetainingConfigs!.Remove(extending));
            }
    }

    // configfileregistrybuilder.go:224.
    private void UpdateRootFilesWatch(RootedFilePath name, ConfigFileEntry entry)
    {
        if (entry.RootFilesWatch == null)
            return;
        HashSet<RootedDirectoryPath>? ignored = null;
        Slice<ByteStringValue> globs = default;
        Slice<RootedDirectoryPath> external = default;
        bool includeWorkspace = false, includeConfig = false;
        var configDirectory = name.Directory();
        var workspace = sessionOptions.CurrentDirectory;
        var sensitivity = fs.CaseSensitivity();
        if (entry.CommandLine!.WildcardDirectories()is { } wildcards)
            foreach (var directory in wildcards.Keys)
            {
                if (sensitivity.ContainsPath(workspace, directory.AsPath()))
                    includeWorkspace = true;
                else if (sensitivity.ContainsPath(configDirectory, directory.AsPath()))
                    includeConfig = true;
                else
                    external = external.Append(directory);
            }

        if (entry.CommandLine.LiteralFileNames()is { } literals)
            foreach (var file in literals)
            {
                if (sensitivity.ContainsFilePath(workspace, file))
                    includeWorkspace = true;
                else if (sensitivity.ContainsFilePath(configDirectory, file))
                    includeConfig = true;
                else
                    external = external.Append(file.Directory());
            }

        if (includeWorkspace)
            globs = globs.Append(ProjectWatches.GetRecursiveGlobPattern(workspace));
        if (includeConfig)
            globs = globs.Append(ProjectWatches.GetRecursiveGlobPattern(configDirectory));
        if (entry.CommandLine.ExtendedSourceFiles()is { } extended)
            foreach (var file in extended)
            {
                if (includeWorkspace && sensitivity.ContainsFilePath(workspace, file))
                    continue;
                globs = globs.Append(file.AsValue());
            }

        if (external.Count > 0)
        {
            var(parents, ignoredExternal) = TsPath.GetCommonParentDirectories(external, ProjectWatches.MinWatchLocationDepth, ProjectWatches.GetPathComponentsForWatching, sensitivity);
            foreach (var parent in parents)
                globs = globs.Append(ProjectWatches.GetRecursiveGlobPattern(parent));
            ignored = ignoredExternal;
        }

        GoSlices.SortFunc(globs, static (a, b) => ByteStringKey.FromValue(a).CompareTo(ByteStringKey.FromValue(b)));
        entry.RootFilesWatch = entry.RootFilesWatch.Clone(new() { PatternsInsideWorkspace = globs, Ignored = ignored });
    }

    // configfileregistrybuilder.go:297/325.
    internal ParsedCommandLine? AcquireConfigForProject(RootedFilePath name, PathKey path, Project project, LogTree? logger)
    {
        var(entry, _) = configs.LoadOrStore(path, ConfigFileEntry.New(hasRelativePatternCapability, name));
        bool retain = false, mappersChanged = false;
        entry!.ChangeIf(config =>
        {
            retain = config.RetainingProjects?.Contains(project.ID()) != true;
            return retain || config.PendingReload != PendingReload.None;
        }, config =>
        {
            if (retain)
                (config.RetainingProjects ??= new()).Add(project.ID());
            mappersChanged = ReloadIfNeeded(config, name, path, logger);
        });
        if (mappersChanged)
            InvalidateContentMappers();
        return entry.Value().CommandLine;
    }

    private ParsedCommandLine? AcquireConfigForFile(RootedFilePath name, PathKey path, PathKey filePath, LogTree? logger)
    {
        var(entry, _) = configs.LoadOrStore(path, ConfigFileEntry.New(hasRelativePatternCapability, name));
        bool retain = false, mappersChanged = false;
        entry!.ChangeIf(config =>
        {
            if (isOpenFile(filePath))
                retain = config.RetainingOpenFiles?.Contains(filePath) != true;
            return retain || config.PendingReload != PendingReload.None;
        }, config =>
        {
            if (retain)
                (config.RetainingOpenFiles ??= new()).Add(filePath);
            mappersChanged = ReloadIfNeeded(config, name, path, logger);
        });
        if (mappersChanged)
            InvalidateContentMappers();
        return entry.Value().CommandLine;
    }

    // configfileregistrybuilder.go:345/361/379.
    internal void ReleaseConfigForProject(PathKey path, ID id)
    {
        var(entry, ok) = configs.Load(path);
        if (ok)
            entry!.ChangeIf(config => config.RetainingProjects?.Contains(id) == true, config => config.RetainingProjects!.Remove(id));
    }

    internal void RetainConfigForProject(PathKey path, ID id)
    {
        var(entry, ok) = configs.Load(path);
        if (ok)
            entry!.ChangeIf(config => config.RetainingProjects?.Contains(id) != true, config => (config.RetainingProjects ??= new()).Add(id));
    }

    private void DidCloseFile(PathKey path)
    {
        if (path.IsDynamic())
            return;
        configFileNames.Delete(path);
        configs.Range(entry =>
        {
            entry!.ChangeIf(config => config.RetainingOpenFiles?.Contains(path) == true, config => config.RetainingOpenFiles!.Remove(path));
            return true;
        });
    }

    // configfileregistrybuilder.go:415.
    internal bool DidChangeCustomConfigFileName(LogTree? logger)
    {
        if (!customConfigFileNameChanged)
            return false;
        configFileNames.Clear();
        return true;
    }

    // configfileregistrybuilder.go:424. Set copy retains Go nil/allocated ownership.
    private static void CopySet<T>(ref HashSet<T>? destination, HashSet<T>? source)
    {
        if (source == null)
            return;
        destination ??= new(source.Comparer);
        destination.UnionWith(source);
    }

    private ChangeFileResult InvalidateCache(LogTree? logger)
    {
        HashSet<ID>? projects = null;
        HashSet<PathKey>? files = null;
        logger?.Log("Too many files changed; marking all configs for reload");
        configFileNames.Range(entry =>
        {
            (files ??= new()).Add(entry.Key());
            return true;
        });
        configFileNames.Clear();
        configs.Range(entry =>
        {
            entry.Change(config =>
            {
                CopySet(ref projects, config.RetainingProjects);
                if (config.PendingReload != PendingReload.Full)
                {
                    var(text, ok) = fs.ReadSourceText(config.FileName);
                    config.PendingReload = !ok || config.CommandLine == null || !ByteStringValue.FromView(text).EqualsValue(ByteStringValue.FromView(config.CommandLine.ConfigFile!.SourceFile.TextView())) ? PendingReload.Full : PendingReload.FileNames;
                }
            });
            return true;
        });
        return new(projects, files);
    }

    // configfileregistrybuilder.go:459.
    private bool IsConfigBaseName(ByteStringValue name) => name.EqualsValue(ByteStringValue.FromString("tsconfig.json")) || name.EqualsValue(ByteStringValue.FromString("jsconfig.json")) || customConfigFileName.Length != 0 && name.EqualsValue(customConfigFileName);
    // configfileregistrybuilder.go:464.
    internal ChangeFileResult DidChangeFiles(FileChangeSummary summary, LogTree? logger)
    {
        if (summary.InvalidateAll)
            return InvalidateCache(logger);
        HashSet<ID>? projects = null;
        HashSet<PathKey>? files = null;
        bool shouldInvalidate = false;
        logger?.Log("Summarizing file changes");
        bool excessive = summary.HasExcessiveWatchEvents() && summary.IncludesWatchChangeOutsideNodeModules;
        var created = new Dictionary<PathKey, RootedPath>(summary.Created?.Count ?? 0);
        var deleted = new Dictionary<PathKey, RootedPath>(summary.Deleted?.Count ?? 0);
        var configChanges = new HashSet<PathKey>();
        var allChanges = new HashSet<PathKey>((summary.Changed?.Count ?? 0) + (summary.Created?.Count ?? 0) + (summary.Deleted?.Count ?? 0));
        if (summary.Changed != null)
            foreach (var uri in summary.Changed)
            {
                var name = uri.FileName();
                if (TsPath.ContainsIgnoredPath(name.AsPath()))
                    continue;
                var path = fs.CaseSensitivity().PathKey(name.AsPath());
                if (IsConfigBaseName(path.BaseNameValue()))
                    configChanges.Add(path);
                allChanges.Add(path);
            }

        if (summary.Deleted != null)
            foreach (var uri in summary.Deleted)
            {
                var name = uri.Path();
                if (TsPath.ContainsIgnoredPath(name))
                    continue;
                var path = fs.CaseSensitivity().PathKey(name);
                deleted[path] = name;
                if (IsConfigBaseName(path.BaseNameValue()))
                    configChanges.Add(path);
                allChanges.Add(path);
            }

        if (summary.Created != null)
            foreach (var uri in summary.Created)
            {
                var name = uri.Path();
                if (TsPath.ContainsIgnoredPath(name))
                    continue;
                var path = fs.CaseSensitivity().PathKey(name);
                created[path] = name;
                if (IsConfigBaseName(path.BaseNameValue()))
                    configChanges.Add(path);
                allChanges.Add(path);
            }

        if (summary.Closed != null)
            foreach (var uri in summary.Closed)
                DidCloseFile(fs.CaseSensitivity().PathKey(uri.FileName().AsPath()));
        logger?.Log("Checking if any changed files are configuration files");
        foreach (var path in allChanges)
        {
            var(entry, ok) = configs.Load(path);
            if (ok)
            {
                if (excessive)
                    return InvalidateCache(logger);
                CopySet(ref projects, HandleConfigChange(entry!, logger));
                if (entry!.Value().RetainingConfigs is { } retaining)
                    foreach (var extending in retaining)
                    {
                        var(parent, parentOk) = configs.Load(extending);
                        if (parentOk)
                            CopySet(ref projects, HandleConfigChange(parent!, logger));
                    }

                created.Remove(path);
            }
            else if (path.BaseNameValue().EqualsValue(ByteStringValue.FromString("package.json")))
            {
                bool manifestChanged = false;
                configs.Range(item =>
                {
                    if (ContentMapperManifestPath(item.Value().CommandLine, path))
                    {
                        CopySet(ref projects, HandleConfigChange(item, logger));
                        manifestChanged = true;
                    }

                    return true;
                });
                if (manifestChanged)
                    InvalidateContentMappers();
            }
        }

        foreach (var path in configChanges)
        {
            if (excessive)
                return InvalidateCache(logger);
            var directory = path.Parent();
            configFileNames.Range(entry =>
            {
                if (directory.ContainsPath(entry.Key()))
                {
                    (files ??= new()).Add(entry.Key());
                    entry.Delete();
                }

                return true;
            });
        }

        foreach (var(path, name)in deleted)
        {
            configs.Range(entry =>
            {
                entry!.ChangeIf(config =>
                {
                    if (config.PendingReload != PendingReload.None || config.CommandLine == null)
                        return false;
                    return config.CommandLine.FilePaths().Contains(path) && config.CommandLine.GetMatchedFileSpec(RootedFilePath.FromValue(name.AsValue())).Length == 0;
                }, config =>
                {
                    config.PendingReload = PendingReload.FileNames;
                    projects ??= new();
                    CopySet(ref projects, config.RetainingProjects);
                    logger?.Logf("Root files for config %s changed", entry.Key());
                    shouldInvalidate = excessive;
                });
                return !shouldInvalidate;
            });
            if (shouldInvalidate)
                return InvalidateCache(logger);
        }

        if (created.Count > 0)
        {
            configs.Range(entry =>
            {
                entry!.ChangeIf(config =>
                {
                    if (config.CommandLine == null || config.RootFilesWatch == null || config.PendingReload != PendingReload.None)
                        return false;
                    logger?.Logf("Checking if any of %d created files match root files for config %s", created.Count, entry.Key());
                    foreach (var(path, name)in created)
                    {
                        if (config.CommandLine.PossiblyMatchesFileName(RootedFilePath.FromValue(name.AsValue())))
                            return true;
                        if (config.CommandLine.PossiblyMatchesDirectoryName(path) && fs.DirectoryExists(RootedDirectoryPath.FromValue(name.AsValue())))
                            return true;
                    }

                    return false;
                }, config =>
                {
                    config.PendingReload = PendingReload.FileNames;
                    projects ??= new();
                    CopySet(ref projects, config.RetainingProjects);
                    logger?.Logf("Root files for config %s changed", entry.Key());
                    shouldInvalidate = excessive;
                });
                return !shouldInvalidate;
            });
            if (shouldInvalidate)
                return InvalidateCache(logger);
        }

        return new(projects, files);
    }

    // configfileregistrybuilder.go:657.
    private HashSet<ID>? HandleConfigChange(Dirty.SyncMapEntry<PathKey, ConfigFileEntry> entry, LogTree? logger)
    {
        bool changed = entry!.ChangeIf(config => config.PendingReload != PendingReload.Full, config => config.PendingReload = PendingReload.Full);
        if (!changed)
            return null;
        logger?.Logf("Config file %s changed", entry.Key());
        var projects = entry.Value().RetainingProjects;
        return projects == null ? null : new(projects, projects.Comparer);
    }

    // configfileregistrybuilder.go:671.
    private static bool ContentMapperManifestPath(ParsedCommandLine? line, PathKey path)
    {
        if (line?.ContentMappers()is not { } mappers)
            return false;
        foreach (var mapper in mappers)
            if (mapper.Package.Length != 0 && mapper.ContributionID.Length == 0 && mapper.PackageDirectory.AsValue().Length != 0 && line.CaseSensitivity().PathKey(mapper.PackageDirectory.ResolveFile("package.json").AsPath()) == path)
                return true;
        return false;
    }

    // configfileregistrybuilder.go:685. Custom-name search precedes standard config search.
    private RootedFilePath ComputeConfigFileName(RootedFilePath name, bool skipDirectory, LogTree? logger)
    {
        var search = name.Directory();
        if (customConfigFileName.Length != 0)
        {
            bool skip = skipDirectory;
            var directory = search;
            while (true)
            {
                if (!skip)
                {
                    var candidate = directory.ResolveFile(customConfigFileName);
                    if (fs.FileExists(candidate))
                    {
                        logger?.Logf("computeConfigFileName:: File: %s:: Result: %s", name, candidate);
                        return candidate;
                    }
                }

                if (directory.AsPath().BaseNameValue().EqualsValue(ByteStringValue.FromString("node_modules")))
                    break;
                skip = false;
                var parent = directory.Directory();
                if (parent == directory)
                    break;
                directory = parent;
            }
        }

        bool skipTs = skipDirectory, skipJs = skipDirectory && !name.BaseNameValue().EqualsValue(ByteStringValue.FromString("tsconfig.json"));
        var current = search;
        RootedFilePath result = default;
        while (true)
        {
            if (!skipTs)
            {
                var candidate = current.ResolveFile("tsconfig.json");
                if (fs.FileExists(candidate))
                {
                    result = candidate;
                    break;
                }
            }

            if (!skipJs)
            {
                var candidate = current.ResolveFile("jsconfig.json");
                if (fs.FileExists(candidate))
                {
                    result = candidate;
                    break;
                }
            }

            if (current.AsPath().BaseNameValue().EqualsValue(ByteStringValue.FromString("node_modules")))
                break;
            skipTs = false;
            skipJs = false;
            var parent = current.Directory();
            if (parent == current)
                break;
            current = parent;
        }

        logger?.Logf("computeConfigFileName:: File: %s:: Result: %s", name, result);
        return result;
    }

    // configfileregistrybuilder.go:741/760/780.
    internal RootedFilePath GetConfigFileNameForFile(RootedFilePath name, PathKey path, LogTree? logger)
    {
        if (name.IsDynamic())
            return default;
        var(entry, ok) = configFileNames.Get(path);
        if (ok)
            return entry!.Value().NearestConfigFileName;
        var config = ComputeConfigFileName(name, false, logger);
        if (isOpenFile(path))
            configFileNames.Add(path, new() { NearestConfigFileName = config });
        return config;
    }

    internal void ForEachConfigFileNameFor(PathKey path, Action<RootedFilePath> callback)
    {
        if (path.IsDynamic())
            return;
        var(entry, ok) = configFileNames.Get(path);
        if (!ok)
            return;
        var name = entry!.Value().NearestConfigFileName;
        while (name.AsValue().Length != 0)
        {
            callback(name);
            if (entry.Value().Ancestors?.TryGetValue(name, out var ancestor) == true)
                name = ancestor;
            else
                return;
        }
    }

    internal RootedFilePath GetAncestorConfigFileName(RootedFilePath name, PathKey path, RootedFilePath config, LogTree? logger)
    {
        if (name.IsDynamic())
            return default;
        var(entry, ok) = configFileNames.Get(path);
        if (!ok)
            return default;
        if (entry!.Value().Ancestors?.TryGetValue(config, out var ancestor) == true)
            return ancestor;
        var result = ComputeConfigFileName(config, true, logger);
        if (isOpenFile(path))
            entry.Change(value => (value.Ancestors ??= new())[config] = result);
        return result;
    }

    // configfileregistrybuilder.go:800.
    public TypeScript.TsOptions.ExtendedConfigCacheEntry GetExtendedConfig(RootedFilePath name, PathKey path, List<PathKey>? stack, IFS configFS)
    {
        var handle = fs.GetFileByPath(name, path);
        var content = handle?.ContentValue() ?? default;
        return extendedConfigCache.LoadAndAcquire(path, snapshotID, new(name, content, fs.source, configFS, stack, this)).Parsed;
    }

    // configfileregistrybuilder.go:819.
    internal void Cleanup()
    {
        bool changed = false;
        configs.Range(entry =>
        {
            entry.DeleteIf(value =>
            {
                bool remove = (value.RetainingProjects?.Count ?? 0) == 0 && (value.RetainingOpenFiles?.Count ?? 0) == 0 && (value.RetainingConfigs?.Count ?? 0) == 0;
                changed = changed || remove;
                return remove;
            });
            return true;
        });
        if (changed)
            InvalidateContentMappers();
    }
}

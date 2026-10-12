// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/snapshotfs.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Vfs;
using TypeScript.LanguageServices.Lsconv;

namespace TypeScript.ProjectSystem;
// snapshotfs.go:20–32. Shared source contract, also inherited by layered overlays.
internal interface IFileHandleSource
{
    IFileHandle? GetFile(RootedFilePath fileName);
    IFileHandle? GetFileByPath(RootedFilePath fileName, PathKey path);
}

internal interface IFileSource : IFileHandleSource
{
    IFS FS();
    bool FileExists(RootedFilePath fileName, PathKey path);
    Entries GetAccessibleEntries(RootedDirectoryPath path);
}

// snapshotfs.go:34. Compose the genuine read cache without changing layered owners.
internal sealed class CachedLayeredFileSystem(ILayeredFileSystem layered) : ILayeredFileSystem, IFileChangeExpander, ISourceTextFileSystem, IOutputTextFileSystem, IMutableFileSystem
{
    private readonly TypeScript.Vfs.CachedVfs.FS cached = TypeScript.Vfs.CachedVfs.FS.From(layered);
    public IFileHandle? GetFile(RootedFilePath fileName) => layered.GetFile(fileName);
    public IFileHandle? GetFileByPath(RootedFilePath fileName, PathKey path) => layered.GetFileByPath(fileName, path);
    public Dictionary<PathKey, Overlay>? Overlays() => layered.Overlays();
    public FileChangeSummary ExpandFileChanges(FileChangeSummary change) => layered is IFileChangeExpander expander ? expander.ExpandFileChanges(change) : change;
    public CaseSensitivity CaseSensitivity() => cached.CaseSensitivity();
    public bool FileExists(RootedFilePath path) => cached.FileExists(path);
    public bool DirectoryExists(RootedDirectoryPath path) => cached.DirectoryExists(path);
    public (string Contents, bool Ok) ReadFile(RootedFilePath path) => cached.ReadFile(path);
    public (ByteStringView Contents, bool Ok) ReadSourceText(RootedFilePath path) => SourceTextReaders.Read(cached, path);
    public Entries GetAccessibleEntries(RootedDirectoryPath path) => cached.GetAccessibleEntries(path);
    public TypeScript.Vfs.FileInfo? Stat(RootedPath path) => cached.Stat(path);
    public RootedPath Realpath(RootedPath path) => cached.Realpath(path);
    public Exception? WriteFile(RootedFilePath path, string content) => cached.WriteFile(path, content);
    public Exception? WriteOutputText(RootedFilePath path, ByteStringValue content) => OutputTextWriters.Write(cached, path, content);
    public Exception? AppendFile(RootedFilePath path, ByteStringValue content) => FileSystemMutations.AppendFile(cached, path, content);
    public Exception? Remove(RootedPath path) => FileSystemMutations.Remove(cached, path);
    public Exception? Chtimes(RootedPath path, DateTimeOffset access, DateTimeOffset modification) => FileSystemMutations.Chtimes(cached, path, access, modification);
}

internal sealed class RealpathAliasSet : Dirty.ICloneable<RealpathAliasSet>
{
    private readonly object gate = new();
    internal Dictionary<PathKey, RootedFilePath>? Paths;
    // snapshotfs.go:87. Guard both lazy allocation and insertion.
    internal void Add(PathKey path, RootedFilePath fileName)
    {
        lock (gate)
        {
            Paths ??= new();
            Paths[path] = fileName;
        }
    }

    public RealpathAliasSet Clone()
    {
        lock (gate)
            return new()
            {
                Paths = Paths is null ? null : new(Paths, Paths.Comparer)
            };
    }
}

internal sealed class SnapshotFS : IFileSource
{
    internal readonly CaseSensitivity sensitivity;
    internal readonly ILayeredFileSystem fs;
    internal readonly Dictionary<PathKey, CachedFile>? cacheFiles;
    internal readonly Dictionary<PathKey, Dirty.CloneableMap<PathKey, ByteStringValue>>? cacheDirectories;
    internal readonly Dictionary<PathKey, RealpathAliasSet>? nodeModulesRealpathAliases;
    private readonly TypeScript.Collections.SyncMap<PathKey, Lazy<IFileHandle?>> readFiles = new();
    internal SnapshotFS(CaseSensitivity sensitivity, ILayeredFileSystem fs, Dictionary<PathKey, CachedFile>? files, Dictionary<PathKey, Dirty.CloneableMap<PathKey, ByteStringValue>>? directories, Dictionary<PathKey, RealpathAliasSet>? aliases)
    {
        this.sensitivity = sensitivity;
        this.fs = fs;
        cacheFiles = files;
        cacheDirectories = directories;
        nodeModulesRealpathAliases = aliases;
    }

    public IFS FS() => fs;
    public IFileHandle? GetFile(RootedFilePath fileName) => GetFileByPath(fileName, sensitivity.PathKey(fileName.AsPath()));
    public bool FileExists(RootedFilePath fileName, PathKey path) => cacheFiles?.ContainsKey(path) == true || fs.FileExists(fileName);
    // snapshotfs.go:134. Lazy publication caches values and exceptions like OnceValue.
    public IFileHandle? GetFileByPath(RootedFilePath fileName, PathKey path)
    {
        if (cacheFiles?.TryGetValue(path, out var file) == true)
            return file;
        var next = new Lazy<IFileHandle?>(() => fs.GetFileByPath(fileName, path), LazyThreadSafetyMode.ExecutionAndPublication);
        return readFiles.LoadOrStore(path, next).Actual.Value;
    }

    public Entries GetAccessibleEntries(RootedDirectoryPath directoryName)
    {
        var lower = fs.GetAccessibleEntries(directoryName);
        if (cacheDirectories?.TryGetValue(sensitivity.PathKey(directoryName.AsPath()), out var directory) != true)
            return lower;
        return SnapshotFileSystems.MergeCachedDirectoryEntries(lower, directory, (owner: this, directoryName), static (path, childName, state) => state.owner.cacheFiles?.ContainsKey(path) == true || state.owner.fs.FileExists(state.directoryName.ResolveFile(childName)), fs.CaseSensitivity());
    }

    // snapshotfs.go:579. Expand symlink URIs only from retained immutable alias owners.
    internal FileChangeSummary ExpandRealpathAliases(FileChangeSummary change)
    {
        if (nodeModulesRealpathAliases is null || nodeModulesRealpathAliases.Count == 0)
            return change;
        Expand(change.Changed);
        Expand(change.Deleted);
        return change;
        void Expand(HashSet<TypeScript.Lsp.Protocol.DocumentUri>? set)
        {
            if (set is null)
                return;
            HashSet<TypeScript.Lsp.Protocol.DocumentUri>? additional = null;
            foreach (var uri in set)
                if (nodeModulesRealpathAliases.TryGetValue(sensitivity.PathKey(uri.FileName().AsPath()), out var aliases) && aliases.Paths is not null)
                    foreach (var name in aliases.Paths.Values)
                    {
                        additional ??= new();
                        additional.Add(Lsconv.FileNameToDocumentURI(name));
                    }

            if (additional is not null)
                set.UnionWith(additional);
        }
    }
}

internal static class SnapshotFileSystems
{
    // snapshotfs.go:158. Own output headers and symlink copy; cached names win spelling.
    internal static Entries MergeCachedDirectoryEntries<TState>(Entries lower, Dirty.CloneableMap<PathKey, ByteStringValue> cached, TState state, Func<PathKey, ByteStringValue, TState, bool> isCachedFile, CaseSensitivity sensitivity)
    {
        var result = new Entries
        {
            Symlinks = lower.Symlinks is null ? null : new(lower.Symlinks, lower.Symlinks.Comparer)
        };
        if (cached.Map is not null)
            foreach (var(childPath, childName)in cached.Map)
            {
                if (result.Symlinks is not null)
                    foreach (var name in result.Symlinks.ToArray())
                        if (sensitivity.CompareValues(name.Value, childName) == 0)
                            result.Symlinks.Remove(name);
                if (isCachedFile(childPath, childName, state))
                    result.Files = result.Files.Append(childName);
                else
                    result.Directories = result.Directories.Append(childName);
            }

        foreach (var name in lower.Files)
            if (!Has(result.Files, name, sensitivity) && !Has(result.Directories, name, sensitivity))
                result.Files = result.Files.Append(name);
        foreach (var name in lower.Directories)
            if (!Has(result.Files, name, sensitivity) && !Has(result.Directories, name, sensitivity))
                result.Directories = result.Directories.Append(name);
        return result;
    }

    private static bool Has(Slice<ByteStringValue> names, ByteStringValue target, CaseSensitivity sensitivity)
    {
        foreach (var name in names)
            if (sensitivity.CompareValues(name, target) == 0)
                return true;
        return false;
    }
}

// snapshotfs.go:189. Mutable construction owner; publication retains immutable maps.
internal sealed class SnapshotFSBuilder : IFileSource
{
    private readonly ILayeredFileSystem fs;
    private readonly CaseSensitivity sensitivity;
    internal readonly Dirty.SyncMap<PathKey, CachedFile> cacheFiles;
    internal readonly Dirty.Map<PathKey, Dirty.CloneableMap<PathKey, ByteStringValue>> cacheDirectories;
    internal readonly Dirty.SyncMap<PathKey, RealpathAliasSet> aliases;
    private readonly HashSet<PathKey> sourceBackedReplacements = new();
    // snapshotfs.go:198 newSnapshotFSBuilderFromSource.
    internal SnapshotFSBuilder(ILayeredFileSystem source, Dictionary<PathKey, CachedFile>? files, Dictionary<PathKey, Dirty.CloneableMap<PathKey, ByteStringValue>>? directories, Dictionary<PathKey, RealpathAliasSet>? realpathAliases)
    {
        fs = new CachedLayeredFileSystem(source);
        sensitivity = fs.CaseSensitivity();
        cacheFiles = new(files);
        cacheDirectories = new(directories);
        aliases = new(realpathAliases);
    }

    public IFS FS() => fs;
    public IFileHandle? GetFile(RootedFilePath fileName) => GetFileByPath(fileName, sensitivity.PathKey(fileName.AsPath()));
    // snapshotfs.go:223 Finalize. Directory and alias changes follow file finalization.
    internal (SnapshotFS Snapshot, bool Changed) FinalizeValue()
    {
        Dictionary<PathKey, CachedFile>? deleted = null;
        void Added(PathKey path, CachedFile file)
        {
            var childPath = path;
            var child = file.FileName().AsPath();
            while (true)
            {
                var parentPath = childPath.Parent();
                var parent = child.Directory();
                if (childPath == parentPath)
                    break;
                var name = child.BaseNameValue();
                var(entry, exists) = cacheDirectories.Get(parentPath);
                if (exists)
                {
                    var key = childPath;
                    entry!.Change(directory => directory.Map![key] = name);
                    break;
                }

                cacheDirectories.Add(parentPath, new(new Dictionary<PathKey, ByteStringValue> { [childPath] = name }));
                childPath = parentPath;
                child = parent.AsPath();
            }
        }

        void Deleted(PathKey path)
        {
            var(entry, exists) = cacheDirectories.Get(path.Parent());
            if (!exists)
                return;
            entry!.Change(directory =>
            {
                directory.Map!.Remove(path);
                if (directory.Map.Count == 0)
                {
                    entry.Delete();
                    Deleted(entry.Key());
                }
            });
        }

        var(files, changed) = cacheFiles.FinalizeWith(new Dirty.FinalizationHooks<PathKey, CachedFile>(OnDelete: (key, value) =>
        {
            if (sourceBackedReplacements.Contains(key))
                return;
            deleted ??= new();
            deleted[key] = value;
        }, OnAdd: Added));
        if (deleted is not null)
        {
            foreach (var path in deleted.Keys)
                Deleted(path);
            foreach (var(path, file)in deleted)
            {
                if (file.RealpathPath == default)
                    continue;
                var(entry, exists) = aliases.Load(file.RealpathPath);
                if (!exists)
                    continue;
                entry!.Locked(value =>
                {
                    value.Change(set => set.Paths?.Remove(path));
                    if (value.Value().Paths is null || value.Value().Paths!.Count == 0)
                        value.Delete();
                });
            }
        }

        var(aliasMap, aliasesChanged) = aliases.FinalizeValue();
        return (new(sensitivity, fs, files, cacheDirectories.FinalizeValue().Value, aliasMap), changed || aliasesChanged);
    }

    // snapshotfs.go:325 deleteCacheEntry.
    private void DeleteCacheEntry(Dirty.SyncMapEntry<PathKey, CachedFile> entry)
    {
        var file = entry.Value();
        if (file is not null && fs.FileExists(file.FileName()))
            sourceBackedReplacements.Add(entry.Key());
        entry.Delete();
    }

    public bool FileExists(RootedFilePath fileName, PathKey path)
    {
        var(entry, exists) = cacheFiles.Load(path);
        return exists ? entry!.Value()is not null && ReloadEntryIfNeeded(entry)is not null : fs.FileExists(fileName);
    }

    public IFileHandle? GetFileByPath(RootedFilePath fileName, PathKey path)
    {
        var(entry, exists) = cacheFiles.Load(path);
        if (exists)
            return ReloadEntryIfNeeded(entry!);
        var file = fs.GetFileByPath(fileName, path);
        if (file is null || file.IsOverlay())
            return file;
        var(next, loaded) = cacheFiles.LoadOrStore(path, new(fileName, file.ContentValue(), file.Hash()));
        if (next is null)
            return null;
        if (!loaded && IsNodeModulesFile(path))
            RecordRealpathAlias(next, fileName, path);
        return ReloadEntryIfNeeded(next);
    }

    public Entries GetAccessibleEntries(RootedDirectoryPath directoryName)
    {
        var lower = fs.GetAccessibleEntries(directoryName);
        var(directory, exists) = cacheDirectories.Get(sensitivity.PathKey(directoryName.AsPath()));
        if (!exists)
            return lower;
        return SnapshotFileSystems.MergeCachedDirectoryEntries(lower, directory!.Value(), (owner: this, directoryName), static (path, name, state) =>
        {
            var(entry, cached) = state.owner.cacheFiles.Load(path);
            return cached && entry!.Value()is not null || state.owner.fs.FileExists(state.directoryName.ResolveFile(name));
        }, sensitivity);
    }

    // snapshotfs.go:377 getCachedFile.
    private IFileHandle? GetCachedFile(RootedFilePath fileName, PathKey path, bool forceReload)
    {
        var(entry, loaded) = cacheFiles.LoadOrStore(path, new CachedFile(fileName, ByteStringValue.FromString("")) { NeedsReload = true });
        if (entry is null)
            return null;
        if (!loaded && IsNodeModulesFile(path))
            RecordRealpathAlias(entry, fileName, path);
        return forceReload ? ReloadEntry(entry) : ReloadEntryIfNeeded(entry);
    }

    private static bool IsNodeModulesFile(PathKey path) => path.AsValue().Contains("/node_modules/"u8);
    // snapshotfs.go:389 recordRealpathAlias.
    private void RecordRealpathAlias(Dirty.SyncMapEntry<PathKey, CachedFile> fileEntry, RootedFilePath fileName, PathKey path)
    {
        var realpath = fs.Realpath(fileName.AsPath());
        var key = sensitivity.PathKey(realpath);
        if (key == path)
            return;
        fileEntry.Change(file => file.RealpathPath = key);
        var(entry, _) = aliases.LoadOrStore(key, new());
        entry!.Change(set => set.Add(path, fileName));
    }

    // snapshotfs.go:405/437. File IO stays outside entry locks.
    private IFileHandle? ReloadEntry(Dirty.SyncMapEntry<PathKey, CachedFile> entry) => Reload(entry, false);
    private IFileHandle? ReloadEntryIfNeeded(Dirty.SyncMapEntry<PathKey, CachedFile> entry) => Reload(entry, true);
    private IFileHandle? Reload(Dirty.SyncMapEntry<PathKey, CachedFile> entry, bool onlyIfNeeded)
    {
        RootedFilePath name = default;
        entry.Locked(value =>
        {
            var file = value.Value();
            if (file is not null && (!onlyIfNeeded || !file.MatchesDiskText()))
                name = file.FileName();
        });
        if (name != default)
        {
            var(content, ok) = SourceTextReaders.Read(fs, name);
            entry.Locked(value =>
            {
                var file = value.Value();
                if (file is null || onlyIfNeeded && file.MatchesDiskText())
                    return;
                if (ok)
                    value.Change(next => next.Reload(ByteStringValue.FromView(content)));
                else
                    value.Delete();
            });
        }

        return entry.Value();
    }

    // snapshotfs.go:471 watchChangesOverlapCache.
    internal bool WatchChangesOverlapCache(FileChangeSummary change, Dictionary<PathKey, IFileHandle>? previous, Dictionary<PathKey, IFileHandle>? current)
    {
        foreach (var set in new[]
        {
            change.Changed,
            change.Deleted
        }

        )
            if (set is not null)
                foreach (var uri in set)
                {
                    var path = sensitivity.PathKey(uri.FileName().AsPath());
                    if (previous?.GetValueOrDefault(path)is not null || current?.GetValueOrDefault(path)is not null || cacheFiles.Load(path).Ok || aliases.Load(path).Ok)
                        return true;
                }

        return false;
    }

    // snapshotfs.go:493/503 invalidateCache/invalidateNodeModulesCache.
    internal void InvalidateCache() => Invalidate(false);
    internal void InvalidateNodeModulesCache() => Invalidate(true);
    private void Invalidate(bool nodeModulesOnly) => cacheFiles.Range(entry =>
    {
        if (!nodeModulesOnly || IsNodeModulesFile(entry.Key()))
            entry.Change(file => file.NeedsReload = true);
        return true;
    });
    // snapshotfs.go:515 markDirtyFiles. WorkGroup preserves concurrent read ownership.
    internal FileChangeSummary MarkDirtyFiles(FileChangeSummary change)
    {
        if (change.Changed is { Count: > 0 })
        {
            var filtered = new TypeScript.Collections.SyncMap<TypeScript.Lsp.Protocol.DocumentUri, bool>();
            var work = Core.NewWorkGroup(false);
            foreach (var uri in change.Changed)
            {
                var path = sensitivity.PathKey(uri.FileName().AsPath());
                var(entry, exists) = cacheFiles.Load(path);
                if (fs.GetFileByPath(uri.FileName(), path)?.IsOverlay() == true || !exists)
                {
                    filtered.Store(uri, true);
                    continue;
                }

                work.Queue(() =>
                {
                    if (ReloadEntryIfContentChanged(entry!))
                        filtered.Store(uri, true);
                });
            }

            work.RunAndWait();
            change.Changed = new(filtered.ToMap().Keys);
        }

        if (change.Deleted is not null)
            foreach (var uri in change.Deleted)
            {
                var(entry, exists) = cacheFiles.Load(sensitivity.PathKey(uri.FileName().AsPath()));
                if (exists)
                    DeleteCacheEntry(entry!);
            }

        return change;
    }

    // snapshotfs.go:552 reloadEntryIfContentChanged.
    private bool ReloadEntryIfContentChanged(Dirty.SyncMapEntry<PathKey, CachedFile> entry)
    {
        var file = entry.Value();
        if (file is null)
            return true;
        var(content, ok) = SourceTextReaders.Read(fs, file.FileName());
        bool changed = true;
        entry.Locked(value =>
        {
            var current = value.Value();
            if (current is null)
                return;
            if (!ok)
            {
                value.Delete();
                return;
            }

            var bytes = ByteStringValue.FromView(content);
            if (bytes.EqualsValue(current.ContentValue()))
            {
                changed = false;
                if (!current.MatchesDiskText())
                    value.Change(next => next.NeedsReload = false);
                return;
            }

            value.Change(next => next.Reload(bytes));
        });
        return changed;
    }

    // snapshotfs.go:617 isRelevantFileName / 638 isRelevantExtension.
    private bool IsRelevantFileName(TypeScript.Lsp.Protocol.DocumentUri uri, IEnumerable<string> extensions, HashSet<PathKey>? watched, Dictionary<PathKey, IFileHandle>? open)
    {
        var name = uri.FileName();
        var path = sensitivity.PathKey(name.AsPath());
        if (watched?.Contains(path) == true || name.ExtensionIsOneOf(extensions) || name.IsDynamic() || open?.ContainsKey(path) == true)
            return true;
        var extension = name.AnyExtensionValue(null, CaseSensitivity.CaseSensitive);
        return extension.EqualsBytes(".js"u8) || extension.EqualsBytes(".jsx"u8) || extension.EqualsBytes(".mjs"u8) || extension.EqualsBytes(".cjs"u8) || extension.EqualsBytes(".ts"u8) || extension.EqualsBytes(".tsx"u8) || extension.EqualsBytes(".mts"u8) || extension.EqualsBytes(".cts"u8) || extension.EqualsBytes(".json"u8);
    }

    // snapshotfs.go:653 expandAndFilterWatchEvents. Created paths may be directory symlinks.
    internal FileChangeSummary ExpandAndFilterWatchEvents(FileChangeSummary change, IEnumerable<string> extensions, HashSet<PathKey>? watched, Dictionary<PathKey, IFileHandle>? previous, Dictionary<PathKey, IFileHandle>? current)
    {
        if (change.Deleted is { Count: > 0 })
        {
            HashSet<TypeScript.Lsp.Protocol.DocumentUri>? filtered = null;
            foreach (var uri in change.Deleted)
            {
                var path = sensitivity.PathKey(uri.FileName().AsPath());
                if (cacheDirectories.Get(path).Ok || HasOpenFileWithin(path, previous, current))
                    CollectFilesRecursive(path, ref filtered, previous, current);
                else if (IsRelevantFileName(uri, extensions, watched, current) || path.AsValue().EndsWith("/node_modules"u8) || IsNodeModulesFile(path))
                {
                    filtered ??= new();
                    filtered.Add(uri);
                }
            }

            change.Deleted = filtered;
        }

        if (change.Changed is { Count: > 0 })
        {
            HashSet<TypeScript.Lsp.Protocol.DocumentUri>? filtered = null;
            foreach (var uri in change.Changed)
                if (IsRelevantFileName(uri, extensions, watched, current))
                {
                    filtered ??= new();
                    filtered.Add(uri);
                }

            change.Changed = filtered;
        }

        return change;
    }

    private static bool HasOpenFileWithin(PathKey path, Dictionary<PathKey, IFileHandle>? previous, Dictionary<PathKey, IFileHandle>? current)
    {
        if (current is not null)
            foreach (var key in current.Keys)
                if (path.ContainsPath(key))
                    return true;
        if (previous is not null)
            foreach (var key in previous.Keys)
                if (path.ContainsPath(key))
                    return true;
        return false;
    }

    // snapshotfs.go:718 collectFilesRecursive.
    private void CollectFilesRecursive(PathKey path, ref HashSet<TypeScript.Lsp.Protocol.DocumentUri>? files, Dictionary<PathKey, IFileHandle>? previous, Dictionary<PathKey, IFileHandle>? current)
    {
        if (current is not null)
            foreach (var(key, file)in current)
                if (path.ContainsPath(key))
                {
                    files ??= new();
                    files.Add(Lsconv.FileNameToDocumentURI(file.FileName()));
                }

        if (previous is not null)
            foreach (var(key, file)in previous)
                if (path.ContainsPath(key))
                {
                    files ??= new();
                    files.Add(Lsconv.FileNameToDocumentURI(file.FileName()));
                }

        var(directory, exists) = cacheDirectories.Get(path);
        if (!exists || directory!.Value().Map is null)
            return;
        foreach (var child in directory.Value().Map!.Keys)
        {
            var(entry, cached) = cacheFiles.Load(child);
            if (cached && entry!.Value()is { } file)
            {
                files ??= new();
                files.Add(Lsconv.FileNameToDocumentURI(file.FileName()));
            }

            CollectFilesRecursive(child, ref files, previous, current);
        }
    }

    // snapshotfs.go:740 convertOpenAndCloseToChanges.
    internal FileChangeSummary ConvertOpenAndCloseToChanges(FileChangeSummary change, Dictionary<PathKey, IFileHandle>? previous, Dictionary<PathKey, IFileHandle>? current)
    {
        if (change.Opened != default && !change.Opened.FileName().IsDynamic())
        {
            var path = sensitivity.PathKey(change.Opened.FileName().AsPath());
            var(entry, exists) = cacheFiles.Load(path);
            if (!exists || entry!.Original()is null)
            {
                change.Created ??= new();
                change.Created.Add(change.Opened);
            }
            else if (current?.GetValueOrDefault(path)is { } open)
            {
                if (open.Hash() != entry.Original().Hash())
                {
                    change.Changed ??= new();
                    change.Changed.Add(change.Opened);
                }

                DeleteCacheEntry(entry);
            }
        }

        if (change.Closed is not null)
            foreach (var uri in change.Closed)
            {
                var name = uri.FileName();
                if (name.IsDynamic())
                    continue;
                var path = sensitivity.PathKey(name.AsPath());
                var file = GetCachedFile(name, path, true);
                if (file is not null)
                {
                    if (previous?.GetValueOrDefault(path)is { } old && file.Hash() != old.Hash())
                    {
                        change.Changed ??= new();
                        change.Changed.Add(uri);
                    }

                    continue;
                }

                change.Deleted ??= new();
                change.Deleted.Add(uri);
            }

        return change;
    }
}

// snapshotfs.go:777 sourceFS. Tracking does not change the underlying file owner.
internal sealed class SourceFS : IFS, IFileHandleSource, ISourceTextFileSystem, IOutputTextFileSystem, IMutableFileSystem
{
    private bool tracking;
    internal IFileSource source;
    private readonly CaseSensitivity sensitivity;
    private readonly TypeScript.Collections.SyncMap<PathKey, bool>? missingDirectories;
    internal TypeScript.Collections.SyncMap<PathKey, RootedFilePath>? seenFiles;
    internal SourceFS(bool tracking, IFileSource source)
    {
        this.tracking = tracking;
        this.source = source;
        sensitivity = source.FS().CaseSensitivity();
        if (tracking)
        {
            missingDirectories = new();
            seenFiles = new();
        }
    }

    internal void DisableTracking() => tracking = false;
    internal void Track(RootedFilePath name)
    {
        if (tracking)
            seenFiles!.Store(sensitivity.PathKey(name.AsPath()), name);
    }

    internal bool SeenFile(PathKey path) => seenFiles?.Load(path).Ok == true;
    internal bool SeenFileOrMissingParentDirectory(PathKey path)
    {
        if (SeenFile(path))
            return true;
        if (missingDirectories is not null)
            while (true)
            {
                if (missingDirectories.Load(path).Ok)
                    return true;
                var parent = path.Parent();
                if (parent == path)
                    break;
                path = parent;
            }

        return false;
    }

    public IFileHandle? GetFile(RootedFilePath name)
    {
        Track(name);
        return source.GetFile(name);
    }

    public IFileHandle? GetFileByPath(RootedFilePath name, PathKey path)
    {
        Track(name);
        return source.GetFileByPath(name, path);
    }

    public bool DirectoryExists(RootedDirectoryPath path)
    {
        var exists = source.FS().DirectoryExists(path);
        if (!exists && tracking)
            missingDirectories!.Store(sensitivity.PathKey(path.AsPath()), true);
        return exists;
    }

    public bool FileExists(RootedFilePath path)
    {
        Track(path);
        return source.FileExists(path, sensitivity.PathKey(path.AsPath()));
    }

    public Entries GetAccessibleEntries(RootedDirectoryPath path) => source.GetAccessibleEntries(path);
    public (ByteStringView Contents, bool Ok) ReadSourceText(RootedFilePath path)
    {
        var file = GetFile(path);
        return file is null ? (default, false) : (file.ContentValue().ByteView, true);
    }

    public (string Contents, bool Ok) ReadFile(RootedFilePath path)
    {
        var file = GetFile(path);
        return file is null ? ("", false) : (file.ContentValue().Materialize()!, true);
    }

    public RootedPath Realpath(RootedPath path) => source.FS().Realpath(path);
    public TypeScript.Vfs.FileInfo? Stat(RootedPath path) => source.FS().Stat(path);
    public CaseSensitivity CaseSensitivity() => sensitivity;
    // snapshotfs.go:892–910: deliberate upstream panic contracts, not unsupported-success defaults.
    public Exception? WriteFile(RootedFilePath path, string content) => throw new InvalidOperationException("unimplemented");
    public Exception? WriteOutputText(RootedFilePath path, ByteStringValue content) => throw new InvalidOperationException("unimplemented");
    public Exception? AppendFile(RootedFilePath path, ByteStringValue content) => throw new InvalidOperationException("unimplemented");
    public Exception? Remove(RootedPath path) => throw new InvalidOperationException("unimplemented");
    public Exception? Chtimes(RootedPath path, DateTimeOffset access, DateTimeOffset modification) => throw new InvalidOperationException("unimplemented");
}

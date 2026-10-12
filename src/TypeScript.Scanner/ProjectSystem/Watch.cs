// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/project/watch.go counterpart at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using FileSystemWatcher = TypeScript.Lsp.Protocol.FileSystemWatcher;
using TypeScript.LanguageServices.Lsconv;

namespace TypeScript.ProjectSystem;
internal readonly struct WatcherID(ByteStringValue value) : IEquatable<WatcherID>, Logging.IGoLogStringer
{
    internal ByteStringValue Value { get; } = value;

    public bool Equals(WatcherID other) => Value.EqualsValue(other.Value);
    public override bool Equals(object? other) => other is WatcherID id && Equals(id);
    public override int GetHashCode() => ByteStringKey.FromValue(Value).GetHashCode();
    public static bool operator ==(WatcherID left, WatcherID right) => left.Equals(right);
    public static bool operator !=(WatcherID left, WatcherID right) => !left.Equals(right);
    public string GoLogString() => Value.Materialize()!;
}

internal readonly record struct FileSystemWatcherKey(ByteStringKey Pattern, WatchKind Kind);
internal sealed class FileSystemWatcherValue(WatcherID id)
{
    internal int Count;
    internal readonly WatcherID ID = id;
}

internal sealed class WatchRegistry
{
    private readonly object gate = new();
    private readonly Dictionary<FileSystemWatcherKey, FileSystemWatcherValue> entries = new();
    private readonly HashSet<WatcherID> pending = new();
    // watch.go:41/51/69. Same owner guards refcounts and retained first registration ID.
    internal bool Acquire(FileSystemWatcher watcher, WatcherID id)
    {
        lock (gate)
        {
            var key = ProjectWatches.ToFileSystemWatcherKey(watcher);
            if (!entries.TryGetValue(key, out var value))
                entries[key] = value = new(id);
            return ++value.Count == 1;
        }
    }

    internal (WatcherID ID, bool Removed) Release(FileSystemWatcher watcher)
    {
        lock (gate)
        {
            var key = ProjectWatches.ToFileSystemWatcherKey(watcher);
            if (!entries.TryGetValue(key, out var value))
                return (default, false);
            if (value.Count <= 1)
            {
                entries.Remove(key);
                return (value.ID, true);
            }

            value.Count--;
            return (default, false);
        }
    }

    internal void MarkPending(WatcherID id)
    {
        lock (gate)
            pending.Add(id);
    }

    internal void ClearPending(WatcherID id)
    {
        lock (gate)
            pending.Remove(id);
    }

    internal bool IsPending(WatcherID id)
    {
        lock (gate)
            return pending.Contains(id);
    }
}

internal struct PatternsAndIgnored
{
    internal Slice<RootedDirectoryPath> DirectoriesOutsideWorkspace;
    internal Slice<ByteStringValue> PatternsInsideWorkspace;
    internal HashSet<RootedDirectoryPath>? Ignored;
}

internal readonly record struct Watchers(WatcherID WatcherID, Slice<FileSystemWatcher> WorkspaceWatchers, Slice<FileSystemWatcher> OutsideWorkspaceWatchers, HashSet<RootedDirectoryPath>? IgnoredPaths);
internal sealed class WatchedFiles<T>
{
    private readonly ByteStringValue name;
    private readonly WatchKind watchKind;
    private readonly bool relativePatterns;
    private readonly Func<T, PatternsAndIgnored> compute;
    private readonly ReaderWriterLockSlim gate = new(LockRecursionPolicy.NoRecursion);
    private T input = default!;
    private bool computed;
    private Slice<FileSystemWatcher> workspaceWatchers, outsideWatchers;
    private HashSet<RootedDirectoryPath>? ignored;
    private ulong id;
    // watch.go:174. New owner allocates an ID even before watcher computation.
    internal WatchedFiles(ByteStringValue name, WatchKind kind, bool relativePatterns, Func<T, PatternsAndIgnored> compute)
    {
        this.name = name;
        watchKind = kind;
        this.relativePatterns = relativePatterns;
        this.compute = compute;
        id = ProjectWatches.NextID();
    }

    private WatchedFiles(WatchedFiles<T> original, T input)
    {
        name = original.name;
        watchKind = original.watchKind;
        relativePatterns = original.relativePatterns;
        compute = original.compute;
        workspaceWatchers = original.workspaceWatchers;
        outsideWatchers = original.outsideWatchers;
        this.input = input;
    }

    // watch.go:218. Computation runs once; failed callbacks are not retried (sync.Once).
    internal Watchers Watchers()
    {
        gate.EnterWriteLock();
        try
        {
            if (!computed)
            {
                computed = true;
                var result = compute(input);
                var globs = ProjectWatches.SortedDistinct(result.PatternsInsideWorkspace);
                ignored = result.Ignored;
                bool changed = false;
                if (!ProjectWatches.EqualWorkspace(workspaceWatchers, globs))
                {
                    workspaceWatchers = Slice<FileSystemWatcher>.Make(globs.Length);
                    for (int i = 0; i < globs.Length; i++)
                        workspaceWatchers[i] = new()
                        {
                            GlobPattern = new()
                            {
                                Pattern = globs[i]
                            },
                            Kind = watchKind
                        };
                    changed = true;
                }

                var directories = ProjectWatches.SortedDistinct(result.DirectoriesOutsideWorkspace);
                if (!ProjectWatches.EqualOutside(outsideWatchers, directories, relativePatterns))
                {
                    outsideWatchers = Slice<FileSystemWatcher>.Make(directories.Length);
                    for (int i = 0; i < directories.Length; i++)
                        outsideWatchers[i] = ProjectWatches.NewRecursiveDirectoryWatcher(directories[i], watchKind, relativePatterns);
                    changed = true;
                }

                if (changed)
                    id = ProjectWatches.NextID();
            }

            return new(new(ByteStringValue.Concat(name, ByteStringValue.FromString(" watcher " + id.ToString(System.Globalization.CultureInfo.InvariantCulture)))), workspaceWatchers, outsideWatchers, ignored is null ? null : new(ignored, ignored.Comparer));
        }
        finally
        {
            gate.ExitWriteLock();
        }
    }

    internal WatcherID ID() => Watchers().WatcherID;
    internal ByteStringValue Name() => name;
    internal WatchKind WatchKind() => watchKind;
    // watch.go:287 Clone. Retain watcher slice owners; input/header and once state are new.
    internal WatchedFiles<T> Clone(T input)
    {
        gate.EnterReadLock();
        try
        {
            return new(this, input);
        }
        finally
        {
            gate.ExitReadLock();
        }
    }
}

internal static class ProjectWatches
{
    internal const int MinWatchLocationDepth = 2;
    // C# explicit nullable receiver adaptations of watch.go:268/287.
    internal static WatchedFiles<T>? Clone<T>(WatchedFiles<T>? owner, T input) => owner?.Clone(input);
    internal static WatcherID ID<T>(WatchedFiles<T>? owner) => owner is null ? default : owner.ID();
    private static long watcherID;
    internal static ulong NextID() => unchecked((ulong)Interlocked.Increment(ref watcherID));
    // watch.go:118/141. Keep original workspace-folder panic rather than permissive formatting.
    internal static FileSystemWatcherKey ToFileSystemWatcherKey(FileSystemWatcher watcher) => new(ByteStringKey.FromValue(FileSystemWatcherGlobString(watcher)), watcher.Kind ?? (WatchKind.Create | WatchKind.Change | WatchKind.Delete));
    internal static ByteStringValue FileSystemWatcherGlobString(FileSystemWatcher watcher)
    {
        if (watcher.GlobPattern.Pattern is { } pattern)
            return pattern;
        if (watcher.GlobPattern.RelativePattern is { } relative)
        {
            ByteStringValue baseUri = ByteStringValue.FromString("");
            if (relative.BaseUri.URI is { } uri)
                baseUri = uri.Value;
            else if (relative.BaseUri.WorkspaceFolder is not null)
                throw new InvalidOperationException("workspace folder-based relative patterns not implemented");
            return ByteStringValue.Concat(baseUri, ByteStringValue.FromString("/"), relative.Pattern);
        }

        return ByteStringValue.FromString("");
    }

    internal static Slice<ByteStringValue> SortedDistinct(Slice<ByteStringValue> input)
    {
        if (input.Length == 0)
            return default; // slices.Sorted of empty sequence is nil.
        var result = Slice<ByteStringValue>.Make(input.Length);
        result.CopyFrom(input);
        result.AsSpan().Sort(static (left, right) => ByteStringKey.FromValue(left).CompareTo(ByteStringKey.FromValue(right)));
        int count = 1;
        for (int i = 1; i < result.Length; i++)
            if (!result[i].EqualsValue(result[count - 1]))
                result[count++] = result[i];
        result.AsSpan()[count..].Clear();
        return result.Slice2(0, count);
    }

    internal static Slice<RootedDirectoryPath> SortedDistinct(Slice<RootedDirectoryPath> input)
    {
        if (input.Length == 0)
            return default;
        var result = Slice<RootedDirectoryPath>.Make(input.Length);
        result.CopyFrom(input);
        result.AsSpan().Sort(static (left, right) => left.Compare(right));
        int count = 1;
        for (int i = 1; i < result.Length; i++)
            if (result[i] != result[count - 1])
                result[count++] = result[i];
        result.AsSpan()[count..].Clear();
        return result.Slice2(0, count);
    }

    internal static bool EqualWorkspace(Slice<FileSystemWatcher> watchers, Slice<ByteStringValue> globs)
    {
        if (watchers.Length != globs.Length)
            return false;
        for (int i = 0; i < watchers.Length; i++)
            if (!watchers[i].GlobPattern.Pattern!.Value.EqualsValue(globs[i]))
                return false;
        return true;
    }

    internal static bool EqualOutside(Slice<FileSystemWatcher> watchers, Slice<RootedDirectoryPath> directories, bool relative)
    {
        if (watchers.Length != directories.Length)
            return false;
        for (int i = 0; i < watchers.Length; i++)
            if (!FileSystemWatcherGlobString(watchers[i]).EqualsValue(RecursiveDirectoryGlobPattern(directories[i], relative)))
                return false;
        return true;
    }

    // watch.go:186 NewWatchedFilesForPaths.
    internal static WatchedFiles<Slice<RootedFilePath>> NewWatchedFilesForPaths(ByteStringValue name, WatchKind kind, bool relative, RootedDirectoryPath workspace, CaseSensitivity sensitivity) => new(name, kind, relative, files =>
    {
        PatternsAndIgnored result = default;
        var workspacePath = sensitivity.PathKey(workspace.AsPath());
        foreach (var file in files)
            if (workspacePath.ContainsPath(sensitivity.PathKey(file.AsPath())))
                result.PatternsInsideWorkspace = result.PatternsInsideWorkspace.Append(file.AsValue());
            else
                result.DirectoriesOutsideWorkspace = result.DirectoriesOutsideWorkspace.Append(file.Directory());
        return result;
    });
    // watch.go:458/464/473.
    internal static ByteStringValue GetRecursiveGlobPattern(RootedDirectoryPath directory) => ByteStringValue.Concat(TsPath.RemoveTrailingDirectorySeparator(directory.AsValue()), ByteStringValue.FromString("/**/*"));
    internal static ByteStringValue RecursiveDirectoryGlobPattern(RootedDirectoryPath directory, bool relative) => relative ? ByteStringValue.Concat(Lsconv.FileNameToDocumentURI(new RootedFilePath(directory.AsValue())).Value, ByteStringValue.FromString("/**/*")) : GetRecursiveGlobPattern(directory);
    internal static FileSystemWatcher NewRecursiveDirectoryWatcher(RootedDirectoryPath directory, WatchKind kind, bool relative) => relative ? new()
    {
        Kind = kind,
        GlobPattern = new()
        {
            RelativePattern = new()
            {
                BaseUri = new()
                {
                    URI = new(Lsconv.FileNameToDocumentURI(new RootedFilePath(directory.AsValue())).Value)
                },
                Pattern = ByteStringValue.FromString("**/*")
            }
        }
    }

    : new()
    {
        Kind = kind,
        GlobPattern = new()
        {
            Pattern = GetRecursiveGlobPattern(directory)
        }
    };
    private sealed class ResolutionGlobCollector
    {
        internal readonly Dictionary<PathKey, RootedDirectoryPath> NodeModules = new(), External = new();
        internal readonly HashSet<PathKey> SeenDirectories = new();
        internal bool IncludeWorkspace, IncludeRoot, IncludeLib;
        internal required RootedDirectoryPath Workspace, Lib, Project;
        internal CaseSensitivity Sensitivity;
        internal PathKey WorkspacePath, LibPath, ProjectPath;
    }

    // watch.go:306. The returned callback owns immutable directory headers; each call
    // has independent accumulator state shared only with its synchronous Range callback.
    internal static Func<TypeScript.Collections.SyncMap<PathKey, RootedFilePath>?, PatternsAndIgnored> CreateResolutionLookupGlobMapper(RootedDirectoryPath workspace, RootedDirectoryPath lib, RootedDirectoryPath project, CaseSensitivity sensitivity)
    {
        var workspacePath = sensitivity.PathKey(workspace.AsPath());
        var libPath = sensitivity.PathKey(lib.AsPath());
        var projectPath = sensitivity.PathKey(project.AsPath());
        return data =>
        {
            var state = new ResolutionGlobCollector
            {
                Workspace = workspace,
                Lib = lib,
                Project = project,
                Sensitivity = sensitivity,
                WorkspacePath = workspacePath,
                LibPath = libPath,
                ProjectPath = projectPath
            };
            data?.Range(state, static (path, file, state) =>
            {
                if (path.IsDynamic() || !state.SeenDirectories.Add(path.Parent()))
                    return true;
                if (state.WorkspacePath.ContainsPath(path))
                    state.IncludeWorkspace = true;
                else if (state.ProjectPath.ContainsPath(path))
                    state.IncludeRoot = true;
                else if (state.LibPath.ContainsPath(path))
                    state.IncludeLib = true;
                else
                {
                    var(_, nodeModules, found) = path.SplitAtCanonicalComponent("node_modules");
                    if (found)
                    {
                        var(_, presentation, valid) = state.Sensitivity.SplitFilePathAtComponent(file, "node_modules");
                        if (!valid)
                            throw new InvalidOperationException("canonical node_modules path did not have a presentation component");
                        state.NodeModules[nodeModules] = presentation;
                    }
                    else
                        state.External[path.Parent()] = file.Directory();
                }

                return true;
            });
            Slice<ByteStringValue> globs = default;
            if (state.IncludeWorkspace)
                globs = globs.Append(GetRecursiveGlobPattern(workspace));
            if (state.IncludeRoot)
                globs = globs.Append(GetRecursiveGlobPattern(project));
            if (state.IncludeLib)
                globs = globs.Append(GetRecursiveGlobPattern(lib));
            if (state.NodeModules.Count > 0)
            {
                var nodeGlobs = Slice<ByteStringValue>.Make(0, state.NodeModules.Count);
                foreach (var directory in state.NodeModules.Values)
                    nodeGlobs = nodeGlobs.Append(GetRecursiveGlobPattern(directory));
                nodeGlobs.AsSpan().Sort(static (left, right) => ByteStringKey.FromValue(left).CompareTo(ByteStringKey.FromValue(right)));
                globs = globs.Append(nodeGlobs);
            }

            Slice<RootedDirectoryPath> outside = default;
            HashSet<RootedDirectoryPath>? ignored = null;
            if (state.External.Count > 0)
            {
                var directories = Slice<RootedDirectoryPath>.Make(state.External.Count);
                int i = 0;
                foreach (var directory in state.External.Values)
                    directories[i++] = directory;
                (outside, ignored) = TsPath.GetCommonParentDirectories(directories, MinWatchLocationDepth, GetPathComponentsForWatching, sensitivity);
                outside.AsSpan().Sort(static (left, right) => left.Compare(right));
            }

            return new PatternsAndIgnored
            {
                PatternsInsideWorkspace = globs,
                DirectoriesOutsideWorkspace = outside,
                Ignored = ignored
            };
        };
    }

    // watch.go:396 getTypingsLocationsGlobs.
    internal static PatternsAndIgnored GetTypingsLocationsGlobs(Slice<RootedPath> files, RootedDirectoryPath typings, RootedDirectoryPath workspace, CaseSensitivity sensitivity)
    {
        bool includeTypings = false, includeWorkspace = false;
        var external = new Dictionary<PathKey, RootedDirectoryPath>();
        var globs = new Dictionary<PathKey, ByteStringValue>();
        foreach (var file in files)
        {
            if (sensitivity.ContainsPath(typings, file))
                includeTypings = true;
            else if (!sensitivity.ContainsPath(workspace, file))
                external[sensitivity.PathKey(file.Directory().AsPath())] = file.Directory();
            else
                includeWorkspace = true;
        }

        Slice<RootedDirectoryPath> directories = default;
        if (external.Count > 0)
        {
            directories = Slice<RootedDirectoryPath>.Make(external.Count);
            int i = 0;
            foreach (var directory in external.Values)
                directories[i++] = directory;
        }

        var(parents, ignored) = TsPath.GetCommonParentDirectories(directories, MinWatchLocationDepth, GetPathComponentsForWatching, sensitivity);
        parents.AsSpan().Sort(static (left, right) => left.Compare(right));
        if (includeWorkspace)
            globs[sensitivity.PathKey(workspace.AsPath())] = GetRecursiveGlobPattern(workspace);
        if (includeTypings)
            globs[sensitivity.PathKey(typings.AsPath())] = GetRecursiveGlobPattern(typings);
        Slice<ByteStringValue> patterns = default;
        if (globs.Count > 0)
        {
            patterns = Slice<ByteStringValue>.Make(globs.Count);
            int i = 0;
            foreach (var glob in globs.Values)
                patterns[i++] = glob;
        }

        return new()
        {
            DirectoriesOutsideWorkspace = parents,
            PatternsInsideWorkspace = patterns,
            Ignored = ignored
        };
    }

    // watch.go:439 getPathComponentsForWatching / perceivedOsRootLengthForWatching.
    internal static Slice<ByteStringValue> GetPathComponentsForWatching(RootedDirectoryPath path)
    {
        var components = path.ComponentsValues();
        int length = PerceivedOsRootLengthForWatching(components);
        if (length <= 1)
            return components;
        var root = TsPath.GetPathFromPathComponents(components.Slice2(0, length));
        var result = Slice<ByteStringValue>.FromArray([root]);
        return result.Append(components.Slice2(length, components.Length));
    }

    internal static int PerceivedOsRootLengthForWatching(Slice<ByteStringValue> components)
    {
        int length = components.Length;
        if (length <= 1)
            return length;
        if (components[0].StartsWith("//"u8))
            return 2;
        var root = components[0];
        if (root.Length == 3 && TsPath.IsVolumeCharacter((char)root[0]) && root[1] == ':' && root[2] == '/')
        {
            if (UnicodeCase.EqualFold(components[1], ByteStringValue.FromString("users")))
                return Math.Min(3, length);
            return 1;
        }

        return components[1].EqualsBytes("home"u8) ? Math.Min(3, length) : 1;
    }
}

// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Complete watcher registry/shared subscription owner from internal/fswatch/watcher.go.
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal sealed partial class Watcher(ByteStringValue name) : IWatcher, TypeScript.ProjectSystem.Logging.IGoLogStringer
{
    private readonly object mu = new();
    internal Func<IWatcherImpl>? Factory;
    private IWatcherImpl? impl;
    private Dictionary<ByteStringKey, DirWatch>? dirWatches;
    private Debounce? debounce;
    internal Func<ulong>? Sequence;
    internal const int RecursiveConsolidateThreshold = 10;
    public ByteStringValue Name() => name;
    // watcher.go:328. Go Stringer retains the raw UTF8 string header.
    internal ByteStringValue String() => name;
    public string GoLogString() => String().Materialize()!;
    public bool Available() => Factory is not null;
    public bool HasFastRecursiveBackend() => name.EqualsBytes("windows"u8) || name.EqualsBytes("fsevents"u8);
    private bool CanShareRecursiveDirWatches() => name.EqualsBytes("fsevents"u8);
    internal (IWatcherImpl? Impl, Exception? Error) GetImpl()
    {
        Func<IWatcherImpl>? factory;
        lock (mu)
        {
            if (impl is not null)
                return (impl, null);
            factory = Factory;
        }

        if (factory is null)
            return (null, FsWatch.ErrUnavailable);
        var created = factory();
        if (created.Run()is { } error)
            return (null, error);
        lock (mu)
        {
            if (impl is null)
            {
                impl = created;
                return (created, null);
            }
        }

        created.Shutdown();
        return (impl, null);
    }

    // Original same-package watcher_test.go inspects map membership under this mutex.
    internal bool HasDirectoryWatchForOriginalAssertion(ByteStringValue directory, bool recursive)
    {
        lock (mu)
            return dirWatches?.ContainsKey(KeyForDirWatch(directory, recursive)) ?? false;
    }

    private ByteStringKey KeyForDirWatch(ByteStringValue dir, bool recursive) => ByteStringKey.FromValue(recursive ? ByteStringValue.Concat(dir, ByteStringValue.FromString("\0recursive")) : dir);
    private DirWatch? FindCoveringRecursiveWatchLocked(ByteStringValue dir, ByteStringValue physicalDir, PathComparer comparer)
    {
        DirWatch? best = null;
        if (dirWatches is not null)
            foreach (var watch in dirWatches.Values)
            {
                if (!watch.Recursive || watch.Comparer.IgnoreCase != comparer.IgnoreCase || !FsWatch.IsInDirectoryOrSelf(watch.Dir, dir) || !FsWatch.IsInDirectoryOrSelf(watch.PhysicalDir, physicalDir))
                    continue;
                if (best is null || watch.Dir.Length > best.Dir.Length)
                    best = watch;
            }

        return best;
    }

    private ByteStringValue FindConsolidationDirLocked(ByteStringValue dir, ByteStringValue physicalDir)
    {
        if (!CanShareRecursiveDirWatches())
            return FsWatch.Empty;
        var parent = FsWatch.DirectoryPath(dir);
        while (!parent.EqualsValue(dir) && !parent.EqualsBytes("."u8))
        {
            if (FsWatch.DirectoryPath(parent).EqualsValue(parent))
                break;
            var physicalParent = FsWatch.PhysicalDirFor(parent);
            if (!FsWatch.IsInDirectoryOrSelf(physicalParent, physicalDir))
                return FsWatch.Empty;
            var count = 1;
            if (dirWatches is not null)
                foreach (var watch in dirWatches.Values)
                    if (FsWatch.IsInDirectoryOrSelf(parent, watch.Dir) && FsWatch.IsInDirectoryOrSelf(physicalParent, watch.PhysicalDir))
                    {
                        count++;
                        if (count >= RecursiveConsolidateThreshold)
                            return parent;
                    }

            var next = FsWatch.DirectoryPath(parent);
            if (next.EqualsValue(parent))
                break;
            dir = parent;
            parent = next;
        }

        return FsWatch.Empty;
    }

    private (PathComparer Comparer, Exception? Error) PathComparer(ByteStringValue dir) => (default, null); // canonicalize_other.go Linux worker.
    private (DirWatch? Watch, Exception? Error) GetOrCreateDirWatch(ByteStringValue dir, ByteStringValue physicalDir, bool recursive, PathComparer comparer)
    {
        lock (mu)
        {
            dirWatches ??= new();
            debounce ??= Debounce.NewDebounce();
            if (CanShareRecursiveDirWatches())
            {
                if (FindCoveringRecursiveWatchLocked(dir, physicalDir, comparer)is { } covering)
                    return (covering, null);
                var consolidationDir = FindConsolidationDirLocked(dir, physicalDir);
                if (consolidationDir.Length != 0)
                {
                    var(parentComparer, error) = PathComparer(consolidationDir);
                    if (error is not null)
                        return (null, error);
                    if (parentComparer.IgnoreCase == comparer.IgnoreCase)
                    {
                        dir = consolidationDir;
                        physicalDir = FsWatch.PhysicalDirFor(dir);
                        recursive = true;
                        if (FindCoveringRecursiveWatchLocked(dir, physicalDir, comparer)is { } existing)
                            return (existing, null);
                    }
                }
            }

            var key = KeyForDirWatch(dir, recursive);
            if (dirWatches.TryGetValue(key, out var value))
                return (value, null);
            value = DirWatch.NewDirWatch(dir, physicalDir, debounce);
            value.SetComparer(comparer);
            value.Sequence = Sequence;
            value.Recursive = recursive;
            dirWatches[key] = value;
            return (value, null);
        }
    }

    internal void RemoveDirWatch(DirWatch watch)
    {
        lock (mu)
        {
            var key = KeyForDirWatch(watch.Dir, watch.Recursive);
            if (dirWatches is not null && dirWatches.TryGetValue(key, out var existing) && ReferenceEquals(existing, watch))
            {
                dirWatches.Remove(key);
                watch.DestroyDebounce();
            }
        }
    }

    public (IWatch? Watch, Exception? Error) WatchDirectory(ByteStringValue directory, WatchCallback callback, Slice<IWatchOption> options = default)
    {
        var result = WatchDirectories(Slice<WatchDirectoryRequest>.FromArray([new() { Dir = directory, Callback = callback, Options = options }]));
        return result.Error is not null ? (null, result.Error) : (result.Watches[0], null);
    }

    private readonly record struct PreparedWatch(DirWatch Watch, ulong ID, bool Recursive, ByteStringValue Dir);
    public (Slice<IWatch> Watches, Exception? Error) WatchDirectories(Slice<WatchDirectoryRequest> requests)
    {
        if (!Available())
            return (default, FsWatch.ErrUnavailable);
        if (requests.Count == 0)
            return (default, null);
        var prepared = Slice<PreparedWatch>.Make(0, requests.Count);
        var unique = Slice<DirWatch>.Make(0, requests.Count);
        var seen = new HashSet<DirWatch>(requests.Count, ReferenceEqualityComparer.Instance);
        void Rollback()
        {
            for (var i = prepared.Count - 1; i >= 0; i--)
            {
                var item = prepared[i];
                item.Watch.Unwatch(item.ID);
                item.Watch.Unref(this);
            }
        }

        foreach (var request in requests)
        {
            var dir = request.Dir;
            var callback = request.Callback;
            if (callback is null)
            {
                Rollback();
                return (default, FsWatch.ErrNilCallback);
            }

            dir = FsWatch.CleanPath(dir);
            if (!FsWatch.IsAbsolutePath(dir))
            {
                Rollback();
                return (default, FsWatch.ErrNotAbsolute);
            }

            dir = FsWatch.CanonicalizePath(dir);
            if (CanShareRecursiveDirWatches() && FsWatch.ValidateWatchDirectory(dir)is { } validationError)
            {
                Rollback();
                return (default, validationError);
            }

            var physicalDir = FsWatch.PhysicalDirFor(dir);
            var options = new WatchOptions();
            foreach (var option in request.Options)
                option.ApplyWatchOption(ref options);
            var(comparer, error) = PathComparer(dir);
            if (error is not null)
            {
                Rollback();
                return (default, error);
            }

            var(watch, createError) = GetOrCreateDirWatch(dir, physicalDir, options.Recursive, comparer);
            if (createError is not null)
            {
                Rollback();
                return (default, createError);
            }

            var(id, _) = watch!.AddCallback(dir, physicalDir, options.Recursive, callback, options.Ignore, options.File);
            prepared = prepared.Append(new PreparedWatch(watch, id, options.Recursive, dir));
            if (seen.Add(watch))
                unique = unique.Append(watch);
        }

        var(implementation, startError) = GetImpl();
        if (startError is not null)
        {
            Rollback();
            return (default, startError);
        }

        if (implementation!.WatchAddMany(unique)is { } addError)
        {
            Rollback();
            return (default, addError);
        }

        var watches = Slice<IWatch>.Make(prepared.Count);
        for (var i = 0; i < prepared.Count; i++)
            watches[i] = new WatchSubscription(this, prepared[i].Watch, implementation, prepared[i].ID);
        return (watches, null);
    }

    public (IWatch? Watch, Exception? Error) WatchFile(ByteStringValue path, WatchCallback callback)
    {
        if (callback is null)
            return (null, FsWatch.ErrNilCallback);
        if (!Available())
            return (null, FsWatch.ErrUnavailable);
        path = FsWatch.CleanPath(path);
        if (!FsWatch.IsAbsolutePath(path))
            return (null, FsWatch.ErrNotAbsolute);
        path = FsWatch.CanonicalizePath(path);
        var dir = FsWatch.DirectoryPath(path);
        if (dir.EqualsValue(path))
            return (null, FsWatch.ErrRootPath);
        return WatchDirectory(dir, callback, Slice<IWatchOption>.FromArray([new FileOption(path)]));
    }
}

internal sealed class WatchSubscription(Watcher watcher, DirWatch directory, IWatcherImpl implementation, ulong id) : IWatch
{
    // Retained original watch.dw pointer for same-package identity assertions.
    internal DirWatch Directory => directory;

    private readonly object mu = new();
    private bool cancelled;
    public Exception? Close()
    {
        lock (mu)
        {
            if (cancelled)
                return null;
            cancelled = true;
            if (directory.Unwatch(id))
            {
                implementation.WatchRemove(directory);
                directory.Unref(watcher);
            }

            return null;
        }
    }
}

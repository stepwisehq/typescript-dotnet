// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Complete dirWatch/callback owners, internal/fswatch/watcher.go:784–1164.
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal struct Callback
{
    internal ulong ID, SinceSeq;
    internal ByteStringValue Dir, PhysicalDir, WatchDir, WatchPhysicalDir;
    internal bool Recursive, Delivered;
    internal WatchCallback Fn;
    internal Func<ByteStringValue, bool>? Ignore;
    internal Exception? Terminal;
    internal PathComparer Comparer;
    internal ComparisonPath DirComparison, PhysicalComparison, FileComparison;
    internal WatchEvent MapEvent(WatchEvent value) => MapEventCached(value, null);
    internal WatchEvent MapEventCached(WatchEvent value, ComparisonCache? cache)
    {
        if (PhysicalDir.Length != 0 && (!PhysicalDir.EqualsValue(Dir) || Comparer.IgnoreCase))
        {
            var physicalPath = new ComparisonPath
            {
                Path = EventPhysicalPath(value.Path),
                Cache = cache
            };
            var root = PhysicalComparison;
            if (root.Path.Length == 0)
                root.Path = PhysicalDir;
            var(path, ok) = Comparer.RebasePrepared(ref physicalPath, root, Dir);
            if (ok)
                value.Path = path;
        }

        return value;
    }

    internal ByteStringValue EventPhysicalPath(ByteStringValue path)
    {
        if (WatchPhysicalDir.Length != 0 && WatchDir.Length != 0 && !WatchPhysicalDir.EqualsValue(WatchDir) && FsWatch.IsInDirectoryOrSelf(WatchDir, path))
            return FsWatch.RebasePath(path, WatchDir, WatchPhysicalDir);
        return path;
    }
}

internal sealed class DirWatch
{
    internal readonly ByteStringValue Dir, PhysicalDir;
    internal bool Recursive;
    internal readonly EventList Events = new();
    internal PathComparer Comparer;
    internal ByteStringValue DirFold, PhysicalDirFold;
    internal object? State;
    internal Func<ulong>? Sequence;
    private readonly object mu = new();
    private Slice<Callback> callbacks;
    // Original same-package terminal assertions lock mu before reading this slice header.
    internal object CallbackGate => mu;
    internal Slice<Callback> CallbacksForOriginalAssertion => callbacks;

    private Debounce? debounce;
    private ulong nextCBID;
    private DirWatch(ByteStringValue dir, ByteStringValue physicalDir, Debounce db)
    {
        Dir = dir;
        PhysicalDir = physicalDir;
        debounce = db;
        db.Add(this, TriggerCallbacks);
    }

    internal static DirWatch NewDirWatch(ByteStringValue dir, ByteStringValue physicalDir, Debounce db) => new(dir, physicalDir, db);
    internal void SetComparer(PathComparer comparer)
    {
        Comparer = comparer;
        DirFold = comparer.Prepare(Dir).Folded;
        PhysicalDirFold = PhysicalDir.EqualsValue(Dir) ? DirFold : comparer.Prepare(PhysicalDir).Folded;
    }

    internal ByteStringValue DisplayPath(ByteStringValue path) => FsWatch.RebasePath(path, PhysicalDir, Dir);
    internal ByteStringValue PhysicalPath(ByteStringValue path) => FsWatch.RebasePath(path, Dir, PhysicalDir);
    internal void DestroyDebounce()
    {
        Debounce? db;
        lock (mu)
        {
            db = debounce;
            debounce = null;
        }

        db?.Remove(this);
    }

    internal void Notify()
    {
        bool hasPending, hasTerminal, hasEvents, hasError;
        Debounce? db;
        lock (mu)
        {
            hasPending = false;
            hasTerminal = false;
            foreach (var callback in callbacks)
            {
                if (!callback.Delivered)
                    hasPending = true;
                if (callback.Terminal is not null && !callback.Delivered)
                    hasTerminal = true;
            }

            hasEvents = Events.Size() > 0;
            hasError = Events.HasError();
            db = debounce;
        }

        if (hasPending && (hasEvents || hasError || hasTerminal))
            db?.Trigger();
    }

    internal void NotifyError(Exception error)
    {
        Slice<Callback> copy;
        lock (mu)
        {
            copy = GoSlices.Clone(callbacks);
            callbacks = default;
        }

        foreach (var callback in copy)
            callback.Fn(default, error);
    }

    internal void TriggerCallbacks()
    {
        Slice<Callback> copy;
        Slice<Slice<WatchEvent>> eventsByCallback;
        Exception? error;
        lock (mu)
        {
            var hasError = Events.HasError();
            var hasEvents = Events.Size() > 0;
            copy = Slice<Callback>.Make(0, callbacks.Count);
            var hasTerminal = false;
            foreach (var callback in callbacks)
            {
                if (callback.Delivered)
                    continue;
                if (callback.Terminal is not null)
                    hasTerminal = true;
                copy = copy.Append(callback);
            }

            if (copy.Count == 0)
            {
                if (hasEvents || hasError)
                    _ = Events.Drain();
                return;
            }

            if (!hasEvents && !hasError && !hasTerminal)
                return;
            var startSequences = Slice<ulong>.Make(copy.Count);
            for (var i = 0; i < copy.Count; i++)
                startSequences[i] = copy[i].SinceSeq;
            (eventsByCallback, error) = Events.DrainForSequences(startSequences);
            foreach (var callback in copy)
            {
                if (callback.Terminal is null)
                    continue;
                for (var i = 0; i < callbacks.Count; i++)
                    if (callbacks[i].ID == callback.ID)
                    {
                        callbacks.AsSpan()[i].Delivered = true;
                        break;
                    }
            }
        }

        var comparisons = new ComparisonCache();
        for (var i = 0; i < copy.Count; i++)
        {
            var callback = copy[i];
            var callbackEvents = eventsByCallback[i];
            if (callback.Ignore is not null || !callback.Recursive || !callback.Dir.EqualsValue(Dir) || callback.FileComparison.Path.Length != 0)
            {
                var filtered = Slice<WatchEvent>.Make(0, callbackEvents.Count);
                foreach (var original in callbackEvents)
                {
                    var value = callback.MapEventCached(original, comparisons);
                    if (callback.FileComparison.Path.Length != 0)
                    {
                        var path = new ComparisonPath
                        {
                            Path = value.Path,
                            Cache = comparisons
                        };
                        var(suffix, ok) = callback.Comparer.SuffixPrepared(callback.FileComparison, ref path);
                        if (!ok || suffix.Length != 0)
                            continue;
                        value.Path = callback.FileComparison.Path;
                    }

                    if (callback.Ignore is not null && callback.Ignore(value.Path))
                        continue;
                    if (!callback.Dir.EqualsValue(Dir) && !value.IncludedWatchRoot && value.Path.EqualsValue(callback.Dir) && value.Kind == EventKind.Update)
                        continue;
                    if (callback.Recursive)
                    {
                        if (!callback.Dir.EqualsValue(Dir) && !FsWatch.IsInDirectoryOrSelf(callback.Dir, value.Path))
                            continue;
                    }
                    else if (!FsWatch.IsDirectChild(callback.Dir, value.Path) && !(!callback.Dir.EqualsValue(Dir) && value.Path.EqualsValue(callback.Dir)))
                        continue;
                    filtered = filtered.Append(value);
                }

                callbackEvents = filtered;
            }

            var callbackError = callback.Terminal ?? error;
            if (callbackEvents.Count > 0 || callbackError is not null)
                callback.Fn(callbackEvents, callbackError);
        }
    }

    internal bool TerminateCallbacksForDeletedRoot(ByteStringValue path, ulong sequence, Exception error)
    {
        lock (mu)
        {
            var changed = false;
            var comparisons = new ComparisonCache();
            var deleted = new ComparisonPath
            {
                Path = path,
                Cache = comparisons
            };
            for (var i = 0; i < callbacks.Count; i++)
            {
                ref var callback = ref callbacks.AsSpan()[i];
                if (callback.Delivered || callback.Terminal is not null || callback.SinceSeq >= sequence)
                    continue;
                var physicalPath = new ComparisonPath
                {
                    Path = callback.EventPhysicalPath(path),
                    Cache = comparisons
                };
                var dir = callback.DirComparison;
                var physical = callback.PhysicalComparison;
                var logicalMatch = callback.Comparer.SuffixPrepared(deleted, ref dir).Ok;
                var physicalMatch = callback.Comparer.SuffixPrepared(physicalPath, ref physical).Ok;
                if (logicalMatch || physicalMatch)
                {
                    callback.Terminal = error;
                    changed = true;
                }
            }

            return changed;
        }
    }

    internal (ulong ID, bool Ok) Watch(ByteStringValue dir, ByteStringValue physicalDir, bool recursive, WatchCallback callback, Func<ByteStringValue, bool>? ignore) => AddCallback(dir, physicalDir, recursive, callback, ignore, FsWatch.Empty);
    internal (ulong ID, bool Ok) AddCallback(ByteStringValue dir, ByteStringValue physicalDir, bool recursive, WatchCallback callback, Func<ByteStringValue, bool>? ignore, ByteStringValue file)
    {
        lock (mu)
        {
            var id = nextCBID = unchecked(nextCBID + 1);
            var sinceSequence = Events.Sequence();
            if (Sequence is not null)
                sinceSequence = Sequence();
            callbacks = callbacks.Append(new Callback { ID = id, Dir = dir, PhysicalDir = physicalDir, WatchDir = Dir, WatchPhysicalDir = PhysicalDir, Recursive = recursive, Fn = callback, Ignore = ignore, SinceSeq = sinceSequence, Comparer = Comparer, DirComparison = Comparer.Prepare(dir), PhysicalComparison = Comparer.Prepare(physicalDir), FileComparison = Comparer.Prepare(file) });
            return (id, true);
        }
    }

    internal bool Unwatch(ulong id)
    {
        lock (mu)
            for (var i = 0; i < callbacks.Count; i++)
                if (callbacks[i].ID == id)
                {
                    callbacks = callbacks.Slice2(0, i).Append(callbacks.Slice2(i + 1));
                    return callbacks.Count == 0;
                }

        return false;
    }

    internal void Unref(Watcher watcher)
    {
        bool empty;
        lock (mu)
            empty = callbacks.Count == 0;
        if (empty)
            watcher.RemoveDirWatch(this);
    }
}

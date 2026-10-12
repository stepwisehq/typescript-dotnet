// Copyright (c) Microsoft Corporation. All rights reserved.
// Complete pinned internal/lsp/lspwatcher/lspwatcher.go owner (split C# files).
using TypeScript.Scanner;
using TypeScript.FsWatch;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem.Logging;
using TypeScript.Vfs;
using TypeScript.LanguageServices.Lsconv;
using FsWatchApi = TypeScript.FsWatch.FsWatch;
using ProtocolWatcher = TypeScript.Lsp.Protocol.FileSystemWatcher;

namespace TypeScript.Lsp.Watching;
internal readonly record struct WatchEvent(RootedPath Path, EventKind Kind);
internal delegate void WatchCallback(Slice<WatchEvent> events, Exception? error);
internal interface IWatcherBackend
{
    (IWatch? Subscription, Exception? Error) WatchDirectory(RootedDirectoryPath directory, WatchCallback callback, Slice<IWatchOption> options = default);
}

internal readonly struct DefaultWatcherBackend(IWatcher watcher) : IWatcherBackend
{
    public (IWatch? Subscription, Exception? Error) WatchDirectory(RootedDirectoryPath directory, WatchCallback callback, Slice<IWatchOption> options = default) => watcher.WatchDirectory(directory.AsValue(), (events, error) =>
    {
        var typed = Slice<WatchEvent>.Make(events.Count);
        for (int i = 0; i < events.Count; i++)
            typed[i] = new WatchEvent(TsPath.RootedPathFromAbsolute(events[i].Path), events[i].Kind);
        callback(typed, error);
    }, options);
}

internal sealed partial class Watcher
{
    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromMilliseconds(75);
    private readonly IFS fs;
    private readonly IWatcherBackend backend;
    private readonly Action<Slice<FileEvent>> onChanges;
    private readonly ILogger logger;
    private readonly TimeProvider time;
    private readonly object mu = new();
    private Dictionary<ByteStringKey, Slice<Watch>>? watches = new();
    private bool closed;
    private Dictionary<ByteStringKey, FileEvent>? pending;
    private ITimer? flushTimer;
    private Watcher(IFS fs, IWatcherBackend backend, Action<Slice<FileEvent>> onChanges, ILogger logger, TimeProvider time)
    {
        this.fs = fs;
        this.backend = backend;
        this.onChanges = onChanges;
        this.logger = logger;
        this.time = time;
    }

    internal static Watcher New(IFS fs, Action<Slice<FileEvent>> onChanges, ILogger logger) => NewWithFSWatcher(fs, FsWatchApi.Default(), onChanges, logger);
    internal static Watcher NewWithFSWatcher(IFS fs, IWatcher watcher, Action<Slice<FileEvent>> onChanges, ILogger logger) => NewWithBackend(fs, new DefaultWatcherBackend(watcher), onChanges, logger);
    internal static Watcher NewWithBackend(IFS fs, IWatcherBackend backend, Action<Slice<FileEvent>> onChanges, ILogger logger, TimeProvider? time = null) => new(fs, backend, onChanges, logger, time ?? TimeProvider.System);
    internal Exception? WatchFiles(ByteStringValue id, Slice<ProtocolWatcher> requests)
    {
        var key = ByteStringKey.FromValue(id);
        lock (mu)
        {
            if (closed)
                return new IOException("lspwatcher: closed");
            if (watches!.ContainsKey(key))
                return QuotedError("lspwatcher: watcher ", id, " already exists");
            watches[key] = default;
        }

        bool failed = false;
        foreach (var request in requests)
        {
            var(directory, ok) = WatchRoot(request);
            if (!ok || directory.AsValue().Length == 0)
            {
                logger.Logf("lspwatcher: skipping watcher %q: unrecognized pattern %q", id, WatchPatternString(request));
                continue;
            }

            var watch = new Watch(this, directory, EffectiveKind(request), IsRecursiveGlob(request));
            if (watch.Reconcile(false)is { } error)
            {
                logger.Logf("lspwatcher: failed to register watcher %q for %q: %v", id, directory, error);
                watch.Close();
                failed = true;
                break;
            }

            lock (mu)
            {
                if (closed)
                {
                    watch.Close();
                    return new IOException("lspwatcher: closed");
                }

                watches![key] = watches.GetValueOrDefault(key).Append(watch);
            }
        }

        if (!failed)
            return null;
        _ = UnwatchFiles(id);
        return QuotedError("lspwatcher: failed to register one or more watchers for ", id, "");
    }

    internal Exception? UnwatchFiles(ByteStringValue id)
    {
        Slice<Watch> values;
        lock (mu)
        {
            var key = ByteStringKey.FromValue(id);
            if (watches is null || !watches.TryGetValue(key, out values))
                return QuotedError("lspwatcher: no watcher with id ", id, "");
            watches.Remove(key);
        }

        foreach (var watch in values)
            watch.Close();
        return null;
    }

    internal void Close()
    {
        Dictionary<ByteStringKey, Slice<Watch>>? values;
        lock (mu)
        {
            if (closed)
                return;
            closed = true;
            values = watches;
            watches = null;
            flushTimer?.Dispose();
            flushTimer = null;
            pending = null;
        }

        if (values is not null)
            foreach (var group in values.Values)
                foreach (var watch in group)
                    watch.Close();
    }

    private static IOException QuotedError(string prefix, ByteStringValue value, string suffix) => new(GoString.ToString(prefix + GoQuote.Quote(value.Materialize()!) + suffix));
    private static (RootedDirectoryPath Directory, bool Ok) NearestExistingAncestor(IFS fs, RootedDirectoryPath directory)
    {
        while (true)
        {
            if (fs.DirectoryExists(directory))
                return (directory, true);
            var parent = directory.AsPath().Directory();
            if (parent == directory)
                return (default, false);
            directory = parent;
        }
    }

    private void ForwardEvents(WatchKind kind, Slice<WatchEvent> events)
    {
        lock (mu)
        {
            if (closed)
                return;
            pending ??= new Dictionary<ByteStringKey, FileEvent>(events.Count);
            foreach (var value in events)
            {
                FileChangeType changeType;
                switch (value.Kind)
                {
                    case EventKind.Update:
                        if ((kind & (WatchKind.Create | WatchKind.Change)) == 0)
                            continue;
                        changeType = FileChangeType.Changed;
                        break;
                    case EventKind.Delete:
                        if ((kind & WatchKind.Delete) == 0)
                            continue;
                        changeType = FileChangeType.Deleted;
                        break;
                    default:
                        continue;
                }

                var uri = Lsconv.PathToDocumentURI(value.Path);
                pending[ByteStringKey.FromValue(uri.Value)] = new FileEvent
                {
                    Uri = uri,
                    Type = changeType
                };
            }

            ScheduleFlushLocked();
        }
    }

    private void EmitSyntheticCreates(RootedDirectoryPath directory, WatchKind kind, bool recursive)
    {
        if ((kind & WatchKind.Create) == 0)
            return;
        var paths = Slice<RootedPath>.FromArray([directory.AsPath()]);
        if (recursive)
            _ = TypeScript.Vfs.Vfs.WalkDir(fs, directory.AsPath(), (path, entry, error) =>
            {
                if (error is not null || path == directory.AsPath())
                    return null;
                paths = paths.Append(path);
                return null;
            });
        else
        {
            var entries = fs.GetAccessibleEntries(directory);
            foreach (var name in entries.Files)
                paths = paths.Append(directory.ResolveFile(name).AsPath());
            foreach (var name in entries.Directories)
                paths = paths.Append(directory.ResolveDirectory(name).AsPath());
        }

        EnqueueSyntheticCreates(paths);
    }

    private void EnqueueSyntheticCreates(Slice<RootedPath> paths)
    {
        lock (mu)
        {
            if (closed)
                return;
            pending ??= new Dictionary<ByteStringKey, FileEvent>(paths.Count);
            foreach (var path in paths)
            {
                var uri = Lsconv.PathToDocumentURI(path);
                var key = ByteStringKey.FromValue(uri.Value);
                if (pending.ContainsKey(key))
                    continue;
                pending[key] = new FileEvent
                {
                    Uri = uri,
                    Type = FileChangeType.Created
                };
            }

            ScheduleFlushLocked();
        }
    }

    private void ScheduleFlushLocked()
    {
        flushTimer ??= time.CreateTimer(static state => ((Watcher)state!).Flush(), this, ThrottleWindow, Timeout.InfiniteTimeSpan);
    }

    private void Flush()
    {
        Dictionary<ByteStringKey, FileEvent>? values;
        lock (mu)
        {
            if (closed)
                return;
            values = pending;
            pending = null;
            flushTimer = null;
        }

        if (values is null || values.Count == 0)
            return;
        var changes = Slice<FileEvent>.Make(0, values.Count);
        foreach (var value in values.Values)
            changes = changes.Append(value);
        onChanges(changes);
    }
}

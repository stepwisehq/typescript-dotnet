// Copyright (c) Microsoft Corporation. All rights reserved.
// Original lspwatcher_test.go waitFor/fakeBackend/fakeWatch assertion adapters.
using System.Diagnostics;
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;
using TypeScript.Vfs;
using TypeScript.Lsp.Protocol;
using FileSystemWatcher = TypeScript.Lsp.Protocol.FileSystemWatcher;
using TypeScript.Lsp.Watching;
using LspWatcher = TypeScript.Lsp.Watching.Watcher;
using LspWatchEvent = TypeScript.Lsp.Watching.WatchEvent;
using LspWatchCallback = TypeScript.Lsp.Watching.WatchCallback;

namespace LanguageServices.Checks;
public static partial class LspWatcherChecks
{
    private static ByteStringValue Text(string value) => ByteStringValue.FromString(GoString.ToUtf8(value));
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void NoError(Exception? error)
    {
        if (error is not null)
            throw new InvalidOperationException(error.Message, error);
    }

    private static void WaitFor(Func<bool> condition, string message)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(5))
        {
            if (condition())
                return;
            Thread.Sleep(20);
        }

        throw new InvalidOperationException("timed out waiting for " + message);
    }

    private sealed class DirectoryOwner : IDisposable
    {
        internal readonly string Root = Directory.CreateTempSubdirectory("lspwatcher-").FullName;
        internal string PathOf(params string[] parts) => Path.Combine([Root, ..parts]);
        public void Dispose()
        {
            Directory.Delete(Root, recursive: true);
        }
    }

    private sealed class Events
    {
        private readonly object mu = new();
        private readonly List<FileEvent> values = new();
        internal void Add(Slice<FileEvent> changes)
        {
            lock (mu)
                foreach (var value in changes)
                    values.Add(value);
        }

        internal FileEvent[] Collected()
        {
            lock (mu)
                return values.ToArray();
        }

        internal bool Has(string suffix, FileChangeType? kind = null) => Collected().Any(value => (!kind.HasValue || value.Type == kind.Value) && value.Uri.Value.EndsWith(Text(suffix)));
    }

    private static readonly IFS Fs = TypeScript.Bundled.Bundled.WrapFS(TypeScript.Vfs.OsVfs.OsVfs.FS());
    private static LspWatcher NewFake(FakeBackend backend, Events? events = null) => LspWatcher.NewWithBackend(Fs, backend, events is null ? static _ =>
    {
    } : events.Add, TypeScript.ProjectSystem.Logging.Logger.NewLogger(Console.OpenStandardError()));
    private static LspWatcher NewReal(Events events) => LspWatcher.New(Fs, events.Add, TypeScript.ProjectSystem.Logging.Logger.NewLogger(Console.OpenStandardError()));
    private static Slice<FileSystemWatcher> Requests(string pattern, WatchKind? kind = null) => Slice<FileSystemWatcher>.FromArray([new FileSystemWatcher { GlobPattern = new PatternOrRelativePattern { Pattern = Text(pattern) }, Kind = kind }]);
    private const WatchKind AllKinds = WatchKind.Create | WatchKind.Change | WatchKind.Delete;
    private static Slice<LspWatchEvent> Update(string path) => Slice<LspWatchEvent>.FromArray([new(RootedPath.FromUnicode(path), EventKind.Update)]);
    // Original tests use errors.Join; retain its newline spelling and marker
    // identity at the existing errors.Is-compatible fixture boundary.
    private static Exception JoinTerminated(Exception error) => new RawWatchError(FsWatch.ErrorString(FsWatch.ErrWatchTerminated) + "\n" + FsWatch.ErrorString(error), FsWatch.ErrWatchTerminated);
    private sealed class FakeWatch(Func<Exception?> close) : IWatch
    {
        public Exception? Close() => close();
    }

    private sealed class FakeBackend : IWatcherBackend
    {
        internal readonly object Mu = new();
        internal readonly Dictionary<RootedDirectoryPath, LspWatchCallback> ByDirectory = new();
        internal readonly Dictionary<RootedDirectoryPath, int> Closed = new();
        internal readonly Dictionary<RootedDirectoryPath, int> OptionCount = new();
        internal readonly Dictionary<RootedDirectoryPath, Exception> FailDirectories = new();
        public (IWatch? Subscription, Exception? Error) WatchDirectory(RootedDirectoryPath directory, LspWatchCallback callback, Slice<IWatchOption> options = default)
        {
            lock (Mu)
            {
                if (FailDirectories.TryGetValue(directory, out var error))
                    return (null, error);
                ByDirectory[directory] = callback;
                OptionCount[directory] = options.Count;
                return (new FakeWatch(() =>
                {
                    lock (Mu)
                    {
                        ByDirectory.Remove(directory);
                        Closed[directory] = Closed.GetValueOrDefault(directory) + 1;
                    }

                    return null;
                }), null);
            }
        }

        internal RootedDirectoryPath[] WatchedDirectories()
        {
            lock (Mu)
                return ByDirectory.Keys.ToArray();
        }

        internal bool IsWatching(string directory)
        {
            lock (Mu)
                return ByDirectory.ContainsKey(RootedDirectoryPath.FromUnicode(directory));
        }

        internal void Emit(string directory, Slice<LspWatchEvent> events, Exception? error = null)
        {
            LspWatchCallback? callback;
            lock (Mu)
                callback = ByDirectory.GetValueOrDefault(RootedDirectoryPath.FromUnicode(directory));
            callback?.Invoke(events, error);
        }

        internal void EmitAll(Slice<LspWatchEvent> events, Exception? error = null)
        {
            LspWatchCallback[] callbacks;
            lock (Mu)
                callbacks = ByDirectory.Values.ToArray();
            foreach (var callback in callbacks)
                callback(events, error);
        }
    }
}

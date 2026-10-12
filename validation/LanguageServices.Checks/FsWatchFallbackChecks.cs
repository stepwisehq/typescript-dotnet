// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Original internal/fswatch/fallback_test.go: all five assertion functions.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static class FsWatchFallbackChecks
{
    public static readonly string[] Names = [nameof(TestFallbackWatcherRoutesUnsupportedDirectories), nameof(TestFallbackWatcherDoesNotFallbackForUnrelatedError), nameof(TestFallbackWatcherDoesNotUseSecondaryOnHappyPath), nameof(TestFallbackWatcherRollsBackRoutedWatchesOnFailure), nameof(TestFanotifyUsesInternalInotifyFallback)];
    private static ByteStringValue Text(string value) => ByteStringValue.FromString(value);
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static Slice<WatchDirectoryRequest> Requests(params string[] directories)
    {
        var requests = Slice<WatchDirectoryRequest>.Make(directories.Length);
        for (int index = 0; index < directories.Length; index++)
            requests[index] = new()
            {
                Dir = Text(directories[index]),
                Callback = static (_, _) =>
                {
                }
            };
        return requests;
    }

    private static void Equal(string[] actual, params string[] expected) => Check(actual.SequenceEqual(expected), "fallback directory assertion differs");
    private sealed class FakeWatch(FakeWatcher watcher, string directory) : IWatch
    {
        public Exception? Close()
        {
            lock (watcher.Gate)
                watcher.Closed.Add(directory);
            return null;
        }
    }

    private sealed class FakeWatcher(string name) : IWatcher
    {
        internal readonly object Gate = new();
        private readonly List<string> watched = new();
        internal readonly List<string> Closed = new();
        internal Exception? FailWith;
        internal Func<string, bool>? FailDirectory;
        public ByteStringValue Name() => Text(name);
        public bool Available() => true;
        public bool HasFastRecursiveBackend() => false;
        private bool ShouldFail(string directory) => FailWith is not null && (FailDirectory is null || FailDirectory(directory));
        private Exception Failure() => new RawWatchError(name + ": " + FsWatch.ErrorString(FailWith!), FailWith);
        public (IWatch? Watch, Exception? Error) WatchDirectory(ByteStringValue directory, WatchCallback callback, Slice<IWatchOption> options = default)
        {
            lock (Gate)
            {
                string path = directory.Materialize()!;
                if (ShouldFail(path))
                    return (null, Failure());
                watched.Add(path);
                return (new FakeWatch(this, path), null);
            }
        }

        public (Slice<IWatch> Watches, Exception? Error) WatchDirectories(Slice<WatchDirectoryRequest> requests)
        {
            lock (Gate)
            {
                foreach (var request in requests)
                    if (ShouldFail(request.Dir.Materialize()!))
                        return (default, Failure());
                var result = Slice<IWatch>.Make(requests.Length);
                for (int index = 0; index < requests.Length; index++)
                {
                    string directory = requests[index].Dir.Materialize()!;
                    watched.Add(directory);
                    result[index] = new FakeWatch(this, directory);
                }

                return (result, null);
            }
        }

        public (IWatch? Watch, Exception? Error) WatchFile(ByteStringValue path, WatchCallback callback) => WatchDirectory(path, callback);
        internal string[] WatchedDirectories()
        {
            lock (Gate)
                return watched.Order(StringComparer.Ordinal).ToArray();
        }

        internal string[] ClosedDirectories()
        {
            lock (Gate)
                return Closed.Order(StringComparer.Ordinal).ToArray();
        }
    }

    private static void CloseAll(Slice<IWatch> watches)
    {
        foreach (var watch in watches)
            _ = watch.Close();
    }

    private static void TestFallbackWatcherRoutesUnsupportedDirectories()
    {
        var primary = new FakeWatcher("fanotify")
        {
            FailWith = FsWatch.ErrFilesystemUnsupported,
            FailDirectory = static dir => dir.StartsWith("/mnt/fuse", StringComparison.Ordinal)
        };
        var secondary = new FakeWatcher("inotify");
        var watcher = new FallbackWatcher(primary, secondary);
        var(watches, error) = watcher.WatchDirectories(Requests("/project", "/project/src", "/mnt/fuse/deps", "/mnt/fuse/deps/a"));
        Check(error is null, "WatchDirectories must succeed");
        try
        {
            Equal(primary.WatchedDirectories(), "/project", "/project/src");
            Equal(secondary.WatchedDirectories(), "/mnt/fuse/deps", "/mnt/fuse/deps/a");
        }
        finally
        {
            CloseAll(watches);
        }
    }

    private static void TestFallbackWatcherDoesNotFallbackForUnrelatedError()
    {
        var primary = new FakeWatcher("fanotify")
        {
            FailWith = FsWatch.ErrUnavailable
        };
        var secondary = new FakeWatcher("inotify");
        var(_, error) = new FallbackWatcher(primary, secondary).WatchDirectory(Text("/project"), static (_, _) =>
        {
        });
        Check(FsWatch.IsError(error, FsWatch.ErrUnavailable), "expected ErrUnavailable");
        Equal(secondary.WatchedDirectories());
    }

    private static void TestFallbackWatcherDoesNotUseSecondaryOnHappyPath()
    {
        var primary = new FakeWatcher("fanotify");
        var secondary = new FakeWatcher("inotify")
        {
            FailWith = new IOException("secondary should not be used")
        };
        var(watches, error) = new FallbackWatcher(primary, secondary).WatchDirectories(Requests("/project", "/project/src"));
        Check(error is null, "WatchDirectories must succeed");
        try
        {
            Equal(secondary.WatchedDirectories());
        }
        finally
        {
            CloseAll(watches);
        }
    }

    private static void TestFallbackWatcherRollsBackRoutedWatchesOnFailure()
    {
        var primary = new FakeWatcher("fanotify")
        {
            FailWith = FsWatch.ErrFilesystemUnsupported,
            FailDirectory = static dir => dir != "/project"
        };
        var secondary = new FakeWatcher("inotify")
        {
            FailWith = FsWatch.ErrUnavailable,
            FailDirectory = static dir => dir == "/broken"
        };
        var(_, error) = new FallbackWatcher(primary, secondary).WatchDirectories(Requests("/project", "/mnt/fuse", "/broken"));
        Check(FsWatch.IsError(error, FsWatch.ErrUnavailable), "expected ErrUnavailable");
        Equal(primary.ClosedDirectories(), "/project");
        Equal(secondary.ClosedDirectories(), "/mnt/fuse");
    }

    private static void TestFanotifyUsesInternalInotifyFallback()
    {
        Check(FsWatch.Fanotify() is FallbackWatcher, "Fanotify must retain the fallback owner");
        var fallback = (FallbackWatcher)FsWatch.Fanotify();
        // Original package fields need focused internal assertion visibility
        // when this complete watcher fixture packet is promoted.
        Check(ReferenceEquals(fallback.Primary, FsWatch.fanotifyWatcher), "Fanotify primary must be the package fanotify watcher");
        Check(ReferenceEquals(fallback.Secondary, FsWatch.Inotify()), "Fanotify secondary must be the package inotify watcher");
    }

    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case nameof(TestFallbackWatcherRoutesUnsupportedDirectories):
                TestFallbackWatcherRoutesUnsupportedDirectories();
                break;
            case nameof(TestFallbackWatcherDoesNotFallbackForUnrelatedError):
                TestFallbackWatcherDoesNotFallbackForUnrelatedError();
                break;
            case nameof(TestFallbackWatcherDoesNotUseSecondaryOnHappyPath):
                TestFallbackWatcherDoesNotUseSecondaryOnHappyPath();
                break;
            case nameof(TestFallbackWatcherRollsBackRoutedWatchesOnFailure):
                TestFallbackWatcherRollsBackRoutedWatchesOnFailure();
                break;
            case nameof(TestFanotifyUsesInternalInotifyFallback):
                TestFanotifyUsesInternalInotifyFallback();
                break;
            default:
                throw new ArgumentException(name);
        }
    }
}

// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Original watcher_test.go/testutil_test.go backend fixture and retry roles.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    private sealed class OriginalSkip(string reason) : Exception(reason);
    private sealed class Fixture(string name, int attempt, List<Action>? sharedCleanup = null) : IDisposable
    {
        private readonly List<Action> cleanup = sharedCleanup ?? new();
        internal readonly string Name = name;
        internal readonly int Attempt = attempt;
        internal void Cleanup(Action action) => cleanup.Add(action);
        internal string NewTmpDir()
        {
            string directory = Directory.CreateTempSubdirectory("fswatch-").FullName;
            Cleanup(() =>
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            });
            // Original filepath.EvalSymlinks so reported backend paths match.
            return GoString.ToString(FsWatch.PhysicalDirFor(Text(directory)).Materialize()!);
        }

        internal void MakeDirSymlink(string target, string link)
        {
            try
            {
                Directory.CreateSymbolicLink(link, target);
            }
            catch (Exception error)when (error is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                throw new OriginalSkip("directory symlink support is not available: " + error.Message);
            }
        }

        internal DirWatch NewDirectWatcher(string directory)
        {
            var value = DirWatch.NewDirWatch(Text(directory), Text(directory), Debounce.NewDebounce());
            value.Recursive = true;
            Cleanup(value.DestroyDebounce);
            return value;
        }

        internal (Recorder Recorder, IWatch Watch) Subscribe(IWatcher watcher, string directory, Slice<IWatchOption> options = default)
        {
            if (ReferenceEquals(watcher, FsWatch.FSEvents()) || ReferenceEquals(watcher, FsWatch.Kqueue()))
                Thread.Sleep(50);
            var recorder = new Recorder
            {
                Watcher = watcher,
                Attempt = Attempt
            };
            var(watch, error) = watcher.WatchDirectory(Text(directory), recorder.Callback, options.IsNil ? Slice<IWatchOption>.FromArray([FsWatch.WithRecursive()]) : options);
            Check(error is null && watch is not null, "subscribe failed: " + error?.Message);
            Cleanup(() => _ = watch!.Close());
            Thread.Sleep(ReferenceEquals(watcher, FsWatch.FSEvents()) || ReferenceEquals(watcher, FsWatch.Kqueue()) ? 300 : 60);
            return (recorder, watch!);
        }

        internal (Recorder Recorder, IWatch Watch) SubscribeFile(IWatcher watcher, string path)
        {
            if (ReferenceEquals(watcher, FsWatch.FSEvents()) || ReferenceEquals(watcher, FsWatch.Kqueue()))
                Thread.Sleep(50);
            var recorder = new Recorder
            {
                Watcher = watcher,
                Attempt = Attempt
            };
            var(watch, error) = watcher.WatchFile(Text(path), recorder.Callback);
            Check(error is null && watch is not null, "subscribeFile failed: " + error?.Message);
            Cleanup(() => _ = watch!.Close());
            Thread.Sleep(ReferenceEquals(watcher, FsWatch.FSEvents()) || ReferenceEquals(watcher, FsWatch.Kqueue()) ? 300 : 60);
            return (recorder, watch!);
        }

        public void Dispose()
        {
            Exception? failure = null;
            for (int index = cleanup.Count - 1; index >= 0; index--)
                try
                {
                    cleanup[index]();
                }
                catch (Exception error)
                {
                    failure ??= error;
                }

            if (failure is not null)
                throw failure;
        }
    }

    private static readonly Lazy<IWatcher[]> availableWatchers = new(CreateAvailableWatchers);
    private static IWatcher[] AvailableWatchers() => availableWatchers.Value;
    private static IWatcher[] CreateAvailableWatchers()
    {
        var values = new List<IWatcher>();
        foreach (var watcher in FsWatch.AllWatchers())
            if (watcher.Available())
                values.Add(watcher);
        // fanotify_linux_test.go original additional test backend forces the
        // FAN_MOVED_FROM/_TO path where supported, without kernel mutation.
        if (FanotifyBackend.Available())
            values.Add(new Watcher(ByteStringValue.FromString("fanotify-no-rename")) { Factory = static () => new FanotifyBackend(true) });
        return values.ToArray();
    }

    private static void RunForEachWatcher(string name, Action<Fixture, IWatcher> body)
    {
        foreach (var watcher in AvailableWatchers())
        {
            Exception? failure = null;
            // retryT.Cleanup delegates to the real backend subtest; owners from
            // every attempt survive until that subtest completes, then unwind LIFO.
            var cleanup = new List<Action>();
            using var owner = new Fixture(name, 1, cleanup);
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var fixture = new Fixture(name, attempt, cleanup);
                    body(fixture, watcher);
                    if (attempt > 1)
                        Console.WriteLine($"retry: succeeded on attempt {attempt}/3");
                    failure = null;
                    break;
                }
                catch (OriginalSkip skip)
                {
                    Console.WriteLine($"ORIGINAL_SKIP {name}/{watcher.Name().Materialize()}: {skip.Message}");
                    failure = null;
                    break;
                }
                catch (InvalidOperationException error)
                {
                    failure = error;
                    if (attempt < 3)
                        Console.WriteLine($"retry: attempt {attempt}/3 failed, retrying with {(attempt == 1 ? 5 : 15)}× timeout scale: {error.Message}");
                }
            }

            if (failure is not null)
                throw new InvalidOperationException($"retry: gave up after 3 attempts ({name})", failure);
        }
    }

    private static long nameCounter;
    private static string UniqueName(params string[] parts)
    {
        var suffix = "test" + Interlocked.Increment(ref nameCounter) + Random.Shared.NextInt64();
        return Path.Combine([..parts, suffix]);
    }
}

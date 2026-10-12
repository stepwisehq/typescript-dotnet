// Copyright (c) Microsoft Corporation. All rights reserved.
// Original lspwatcher_test.go:358–828 missing-directory/recovery assertions.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using TypeScript.Lsp.Protocol;
using LspWatchEvent = TypeScript.Lsp.Watching.WatchEvent;

namespace LanguageServices.Checks;
public static partial class LspWatcherChecks
{
    private static void TestWatcher_RealBackend_MissingThenCreate()
    {
        using var directory = new DirectoryOwner();
        var events = new Events();
        var watcher = NewReal(events);
        try
        {
            string target = directory.PathOf("pkg");
            NoError(watcher.WatchFiles(Text("test"), Requests(target + "/*", AllKinds)));
            Thread.Sleep(200);
            Directory.CreateDirectory(target);
            File.WriteAllText(directory.PathOf("pkg", "index.ts"), "export {}");
            WaitFor(() => events.Has("/pkg/index.ts"), "event for file created in a previously-missing directory");
        }
        finally
        {
            watcher.Close();
        }
    }

    private static void TestWatcher_MissingDirectoryTracksAncestor()
    {
        using var directory = new DirectoryOwner();
        var backend = new FakeBackend();
        var watcher = NewFake(backend);
        try
        {
            string target = directory.PathOf("pkg");
            NoError(watcher.WatchFiles(Text("id"), Requests(target + "/*")));
            Check(backend.IsWatching(directory.Root), "expected ancestor watch");
            Check(backend.WatchedDirectories().Length == 1, "expected exactly one ancestor watch");
            NoError(watcher.UnwatchFiles(Text("id")));
            Check(backend.WatchedDirectories().Length == 0, "expected all watches closed after unwatch");
        }
        finally
        {
            watcher.Close();
        }
    }

    private static void TestWatcher_MissingDirectoryPromotesOnCreate()
    {
        using var directory = new DirectoryOwner();
        var backend = new FakeBackend();
        var events = new Events();
        var watcher = NewFake(backend, events);
        try
        {
            string target = directory.PathOf("pkg");
            NoError(watcher.WatchFiles(Text("id"), Requests(target + "/*", AllKinds)));
            Directory.CreateDirectory(target);
            File.WriteAllText(directory.PathOf("pkg", "index.ts"), "export {}");
            backend.Emit(directory.Root, Update(target));
            WaitFor(() => backend.IsWatching(target), "promotion to target watch");
            WaitFor(() => events.Has("/pkg", FileChangeType.Created) && events.Has("/pkg/index.ts", FileChangeType.Created), "synthetic create events for target and child");
        }
        finally
        {
            watcher.Close();
        }
    }

    private static void TestWatcher_MultiLevelDescend()
    {
        using var directory = new DirectoryOwner();
        var backend = new FakeBackend();
        var watcher = NewFake(backend);
        try
        {
            string target = directory.PathOf("a", "b", "c");
            NoError(watcher.WatchFiles(Text("id"), Requests(target + "/*")));
            Check(backend.IsWatching(directory.Root), "expected initial ancestor watch");
            string a = directory.PathOf("a");
            Directory.CreateDirectory(a);
            backend.Emit(directory.Root, Update(a));
            WaitFor(() => backend.IsWatching(a), "descend to a");
            string ab = directory.PathOf("a", "b");
            Directory.CreateDirectory(ab);
            backend.Emit(a, Update(ab));
            WaitFor(() => backend.IsWatching(ab), "descend to a/b");
            Directory.CreateDirectory(target);
            backend.Emit(ab, Update(target));
            WaitFor(() => backend.IsWatching(target), "promote to target a/b/c");
        }
        finally
        {
            watcher.Close();
        }
    }

    private static void TestWatcher_AtomicTreeCreateRace()
    {
        using var directory = new DirectoryOwner();
        var backend = new FakeBackend();
        var watcher = NewFake(backend);
        try
        {
            string target = directory.PathOf("a", "b", "c");
            NoError(watcher.WatchFiles(Text("id"), Requests(target + "/*")));
            Directory.CreateDirectory(target);
            backend.Emit(directory.Root, Update(directory.PathOf("a")));
            WaitFor(() => backend.IsWatching(target), "promote to target in one pass");
        }
        finally
        {
            watcher.Close();
        }
    }

    private static void TestWatcher_SyntheticCreateDepth()
    {
        foreach (bool recursive in new[]
        {
            false,
            true
        }

        )
        {
            using var directory = new DirectoryOwner();
            var backend = new FakeBackend();
            var events = new Events();
            var watcher = NewFake(backend, events);
            try
            {
                string target = directory.PathOf("pkg");
                NoError(watcher.WatchFiles(Text("id"), Requests(target + (recursive ? "/**/*" : "/*"), AllKinds)));
                Directory.CreateDirectory(directory.PathOf("pkg", "sub"));
                File.WriteAllText(directory.PathOf("pkg", "top.ts"), "export {}");
                File.WriteAllText(directory.PathOf("pkg", "sub", "deep.ts"), "export {}");
                backend.Emit(directory.Root, Update(target));
                WaitFor(() => backend.IsWatching(target), "promotion");
                WaitFor(() => events.Has("/pkg/top.ts", FileChangeType.Created), "synthetic create for immediate child");
                if (recursive)
                {
                    var deadline = System.Diagnostics.Stopwatch.StartNew();
                    while (deadline.Elapsed < TimeSpan.FromSeconds(1) && !events.Has("/pkg/sub/deep.ts", FileChangeType.Created))
                        Thread.Sleep(20);
                    Check(events.Has("/pkg/sub/deep.ts", FileChangeType.Created), "recursive watch should synthesize deep descendant");
                }
                else
                {
                    Thread.Sleep(300);
                    Check(!events.Has("/pkg/sub/deep.ts", FileChangeType.Created), "nonrecursive watch must not synthesize deep descendant");
                }
            }
            finally
            {
                watcher.Close();
            }
        }
    }

    private static void TestWatcher_TerminatedFallsBackAndRecovers()
    {
        using var directory = new DirectoryOwner();
        var backend = new FakeBackend();
        var events = new Events();
        var watcher = NewFake(backend, events);
        try
        {
            string target = directory.PathOf("pkg");
            Directory.CreateDirectory(target);
            NoError(watcher.WatchFiles(Text("id"), Requests(target + "/*", AllKinds)));
            Check(backend.IsWatching(target), "expected target watch");
            Directory.Delete(target, recursive: true);
            backend.Emit(target, Slice<LspWatchEvent>.FromArray([new(RootedPath.FromUnicode(target), EventKind.Delete)]), JoinTerminated(new IOException("removed")));
            WaitFor(() => events.Has("/pkg", FileChangeType.Deleted), "forwarded delete of terminated dir");
            WaitFor(() => backend.IsWatching(directory.Root) && !backend.IsWatching(target), "fallback to ancestor watch");
            Directory.CreateDirectory(target);
            backend.Emit(directory.Root, Update(target));
            WaitFor(() => backend.IsWatching(target), "recovery to target watch after recreation");
        }
        finally
        {
            watcher.Close();
        }
    }

    private static void TestWatcher_GenuineFailureRollsBackForRetry()
    {
        using var directory = new DirectoryOwner();
        var backend = new FakeBackend();
        var watcher = NewFake(backend);
        try
        {
            var request = Requests(directory.Root + "/*");
            lock (backend.Mu)
                backend.FailDirectories[RootedDirectoryPath.FromUnicode(directory.Root)] = new IOException("too many open files");
            Check(watcher.WatchFiles(Text("id"), request)is not null, "expected error from genuine backend failure");
            lock (backend.Mu)
                backend.FailDirectories.Remove(RootedDirectoryPath.FromUnicode(directory.Root));
            NoError(watcher.WatchFiles(Text("id"), request));
            Check(backend.IsWatching(directory.Root), "expected watch after successful retry");
        }
        finally
        {
            watcher.Close();
        }
    }

    private static void TestWatcher_WatchTerminatedDoesNotDropEvents()
    {
        using var directory = new DirectoryOwner();
        var backend = new FakeBackend();
        var events = new Events();
        var watcher = NewFake(backend, events);
        try
        {
            NoError(watcher.WatchFiles(Text("id"), Requests(directory.Root + "/**/*")));
            backend.EmitAll(Update(directory.PathOf("b.ts")), JoinTerminated(new IOException("simulated")));
            WaitFor(() => events.Collected().Length > 0, "events with watch-terminated error");
        }
        finally
        {
            watcher.Close();
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Original lspwatcher_test.go:33–204,278–356 assertion bodies.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using TypeScript.Lsp.Protocol;
using LspWatcher = TypeScript.Lsp.Watching.Watcher;
using LspWatchEvent = TypeScript.Lsp.Watching.WatchEvent;
using FileSystemWatcher = TypeScript.Lsp.Protocol.FileSystemWatcher;

namespace LanguageServices.Checks;
public static partial class LspWatcherChecks
{
    private static void TestWatcher_CreateChangeDelete()
    {
        using var directory = new DirectoryOwner();
        var events = new Events();
        var watcher = NewReal(events);
        try
        {
            NoError(watcher.WatchFiles(Text("test"), Requests(directory.Root + "/**/*", AllKinds)));
            Thread.Sleep(200);
            string file = directory.PathOf("a.ts");
            File.WriteAllText(file, "export {}");
            WaitFor(() => events.Collected().Any(value => value.Type == FileChangeType.Changed), "update event");
            File.Delete(file);
            WaitFor(() => events.Collected().Any(value => value.Type == FileChangeType.Deleted), "delete event");
            NoError(watcher.UnwatchFiles(Text("test")));
        }
        finally
        {
            watcher.Close();
        }
    }

    private static void TestWatcher_KindFilter()
    {
        using var directory = new DirectoryOwner();
        var backend = new FakeBackend();
        var events = new Events();
        var watcher = NewFake(backend, events);
        try
        {
            NoError(watcher.WatchFiles(Text("test"), Requests(directory.Root + "/**/*", WatchKind.Delete)));
            string path = directory.PathOf("x.ts");
            backend.EmitAll(Slice<LspWatchEvent>.FromArray([new(RootedPath.FromUnicode(path), EventKind.Update), new(RootedPath.FromUnicode(path), EventKind.Delete)]));
            WaitFor(() => events.Collected().Any(value => value.Type == FileChangeType.Deleted), "delete event");
            foreach (var value in events.Collected())
                Check(value.Type == FileChangeType.Deleted, "unexpected non-delete event");
        }
        finally
        {
            watcher.Close();
        }
    }

    private static void TestRootFromGlob()
    {
        (string Pattern, string Expected)[] cases = [("/abs/path/**/*", "/abs/path"), ("/abs/path/", "/abs/path"), ("/abs/path/?.ts", "/abs/path"), ("/abs/path/{a,b}/*", "/abs/path"), ("/abs/path/../shared/*", "/abs/shared")];
        foreach (var item in cases)
            Check(LspWatcher.RootFromGlob(Text(item.Pattern)).EqualsValue(Text(item.Expected)), "rootFromGlob " + item.Pattern);
    }

    private static void TestWatchRootFromRelativePattern()
    {
        (string Pattern, string Expected)[] cases = [("**/*", "/workspace/project"), ("src/**/*", "/workspace/project/src"), ("../shared/*", "/workspace/shared")];
        var uri = new URI(Text("file:///workspace/project"));
        foreach (var item in cases)
        {
            var request = new FileSystemWatcher
            {
                GlobPattern = new PatternOrRelativePattern
                {
                    RelativePattern = new RelativePattern
                    {
                        BaseUri = new WorkspaceFolderOrURI
                        {
                            URI = uri
                        },
                        Pattern = Text(item.Pattern)
                    }
                }
            };
            var(actual, ok) = LspWatcher.WatchRoot(request);
            Check(ok && actual == RootedDirectoryPath.FromUnicode(item.Expected), "watchRoot " + item.Pattern);
        }
    }

    private static void TestWatcher_BookkeepingAndOverflow()
    {
        using var directory = new DirectoryOwner();
        var backend = new FakeBackend();
        var events = new Events();
        var watcher = NewFake(backend, events);
        try
        {
            var request = Requests(directory.Root + "/**/*");
            NoError(watcher.WatchFiles(Text("id"), request));
            Check(watcher.WatchFiles(Text("id"), request)is not null, "expected duplicate-id error");
            backend.EmitAll(Update(directory.PathOf("a.ts")), TypeScript.FsWatch.FsWatch.ErrOverflow);
            WaitFor(() => events.Collected().Length > 0, "events after overflow");
            Check(watcher.UnwatchFiles(Text("missing"))is not null, "expected unknown-id error");
            NoError(watcher.UnwatchFiles(Text("id")));
            NoError(watcher.WatchFiles(Text("id2"), default));
            watcher.Close();
            Check(watcher.WatchFiles(Text("id3"), default)is not null, "expected closed error");
        }
        finally
        {
            watcher.Close();
        }
    }

    private static void TestWatcher_NonRecursiveGlobIsNotRecursive()
    {
        using var directory = new DirectoryOwner();
        string sub = directory.PathOf("sub");
        Directory.CreateDirectory(sub);
        var backend = new FakeBackend();
        var watcher = NewFake(backend);
        try
        {
            var requests = Requests(directory.Root + "/**/*").Append(Requests(sub + "/*"));
            NoError(watcher.WatchFiles(Text("id"), requests));
            lock (backend.Mu)
            {
                Check(backend.OptionCount[RootedDirectoryPath.FromUnicode(directory.Root)] == 1, "recursive glob expected1 WithRecursive option");
                Check(backend.OptionCount[RootedDirectoryPath.FromUnicode(sub)] == 0, "nonrecursive glob expected0 options");
            }
        }
        finally
        {
            watcher.Close();
        }
    }
}

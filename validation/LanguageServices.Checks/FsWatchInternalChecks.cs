// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Original watcher_test.go path/lifecycle/callback unit assertion bodies.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static class FsWatchInternalChecks
{
    private static ByteStringValue Text(string display) => ByteStringValue.FromString(GoString.ToUtf8(display));
    private static void Check(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }

    public static void TestRebasePath()
    {
        string root = Path.GetPathRoot(Path.GetTempPath())!, from = Path.Combine(root, "from"), to = Path.Combine(root, "to");
        (string Path, string From, string To, string Expected)[] cases = [(from, from, to, to), (Path.Combine(from, "child"), from, to, Path.Combine(to, "child")), (Path.Combine(root, "from-sibling", "child"), from, to, Path.Combine(root, "from-sibling", "child")), (Path.Combine(root, "child"), root, to, Path.Combine(to, "child")), (Path.Combine(from, "child"), from, root, Path.Combine(root, "child"))];
        foreach (var item in cases)
            Check(FsWatch.RebasePath(Text(item.Path), Text(item.From), Text(item.To)).EqualsValue(Text(item.Expected)), "rebasePath differs");
    }

    public static void TestPhysicalDirForResolvesSymlinkAncestor()
    {
        string root = Directory.CreateTempSubdirectory("fswatch-").FullName;
        try
        {
            string target = Path.Combine(root, "target"), link = Path.Combine(root, "link");
            Directory.CreateDirectory(Path.Combine(target, "nested"));
            Directory.CreateSymbolicLink(link, target);
            var expected = FsWatch.PhysicalDirFor(Text(Path.Combine(target, "nested")));
            Check(FsWatch.PhysicalDirFor(Text(Path.Combine(link, "nested"))).EqualsValue(expected), "physicalDirFor symlink ancestor differs");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    public static void TestIsInDirectoryOrSelf()
    {
        string root = Path.GetPathRoot(Path.GetTempPath())!, parent = Path.Combine(root, "parent"), child = Path.Combine(parent, "child"), nested = Path.Combine(child, "nested"), sibling = Path.Combine(root, "parent-sibling");
        (string Directory, string Path, bool Expected)[] cases = [(parent, parent, true), (parent, child, true), (parent, nested, true), (parent, sibling, false), (root, root, true), (root, Path.Combine(root, "child"), true), ("", child, false)];
        foreach (var item in cases)
            Check(FsWatch.IsInDirectoryOrSelf(Text(item.Directory), Text(item.Path)) == item.Expected, "directory containment differs");
    }

    private sealed class FailingBackend(Exception error) : WatcherBase
    {
        public override Exception? Start() => error;
        public override Exception? Subscribe(DirWatch watch) => null;
        public override Exception? CloseWatch(DirWatch watch) => null;
    }

    public static void TestBackendRunReturnsStartError()
    {
        var expected = new IOException("startup failed");
        Check(ReferenceEquals(new FailingBackend(expected).Run(), expected), "run must return original start error");
    }

    public static void TestDirWatchErrorImplementsError()
    {
        // Go zero dirWatch pointer is never consumed by Error(); retain null!
        // only at this exact original constructor/error projection boundary.
        Exception error = new DirWatchError(new IOException("boom"), null!);
        Check(error.Message == "boom", "dirWatchError spelling differs");
    }

    private readonly record struct Call(Slice<WatchEvent> Events, Exception? Error);
    public static void TestFileCallbackForwardsErrAlongsideEvents()
    {
        var target = Text("/abs/dir/target.txt");
        var other = Text("/abs/dir/sibling.txt");
        var overflow = new IOException("overflow");
        var calls = new List<Call>();
        var watch = DirWatch.NewDirWatch(Text("/abs/dir"), Text("/abs/dir"), Debounce.NewDebounce());
        watch.Recursive = true;
        try
        {
            watch.AddCallback(Text("/abs/dir"), Text("/abs/dir"), false, (events, error) => calls.Add(new(events, error)), null, target);
            void Deliver(Slice<WatchEvent> events, Exception? error)
            {
                foreach (var value in events)
                    if (value.Kind == EventKind.Delete)
                        watch.Events.Remove(value.Path);
                    else
                        watch.Events.Update(value.Path);
                if (error is not null)
                    watch.Events.SetError(error);
                watch.TriggerCallbacks();
            }

            Deliver(Slice<WatchEvent>.FromArray([new() { Kind = EventKind.Update, Path = target }, new() { Kind = EventKind.Update, Path = other }]), null);
            Check(calls.Count == 1 && calls[0].Events.Length == 1 && calls[0].Events[0].Path.EqualsValue(target) && calls[0].Error is null, "plain delivery differs");
            calls.Clear();
            Deliver(Slice<WatchEvent>.FromArray([new() { Kind = EventKind.Update, Path = other }]), overflow);
            Check(calls.Count == 1 && calls[0].Events.Length == 0 && ReferenceEquals(calls[0].Error, overflow), "error-only delivery differs");
            calls.Clear();
            Deliver(Slice<WatchEvent>.FromArray([new() { Kind = EventKind.Delete, Path = target }, new() { Kind = EventKind.Update, Path = other }]), overflow);
            Check(calls.Count == 1 && calls[0].Events.Length == 1 && calls[0].Events[0].Path.EqualsValue(target) && calls[0].Events[0].Kind == EventKind.Delete && ReferenceEquals(calls[0].Error, overflow), "combined delivery differs");
            calls.Clear();
            Deliver(default, null);
            Check(calls.Count == 0, "no-op delivery differs");
        }
        finally
        {
            watch.DestroyDebounce();
        }
    }

    public static readonly string[] Names = [nameof(TestRebasePath), nameof(TestPhysicalDirForResolvesSymlinkAncestor), nameof(TestIsInDirectoryOrSelf), nameof(TestBackendRunReturnsStartError), nameof(TestDirWatchErrorImplementsError), nameof(TestFileCallbackForwardsErrAlongsideEvents)];
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
            case nameof(TestRebasePath):
                TestRebasePath();
                break;
            case nameof(TestPhysicalDirForResolvesSymlinkAncestor):
                TestPhysicalDirForResolvesSymlinkAncestor();
                break;
            case nameof(TestIsInDirectoryOrSelf):
                TestIsInDirectoryOrSelf();
                break;
            case nameof(TestBackendRunReturnsStartError):
                TestBackendRunReturnsStartError();
                break;
            case nameof(TestDirWatchErrorImplementsError):
                TestDirWatchErrorImplementsError();
                break;
            case nameof(TestFileCallbackForwardsErrAlongsideEvents):
                TestFileCallbackForwardsErrAlongsideEvents();
                break;
            default:
                throw new ArgumentException(name);
        }
    }
}

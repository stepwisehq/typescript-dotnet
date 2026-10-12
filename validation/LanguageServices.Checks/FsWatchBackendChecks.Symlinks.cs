// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Direct original watcher_test.go:1212–1312 assertions.
using TypeScript.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static void TestSubscribeSymlinkCreate() => RunForEachWatcher(nameof(TestSubscribeSymlinkCreate), (fixture, watcher) =>
    {
        // DragonFlyBSD's source-defined kqueue skip is outside this Linux backend packet.
        string dir = fixture.NewTmpDir(), first = UniqueName(dir);
        WriteFile(first, "x");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string second = UniqueName(dir);
        FileAction(() => File.CreateSymbolicLink(second, first));
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(second)));
    });
    public static void TestSubscribeSymlinkDelete() => RunForEachWatcher(nameof(TestSubscribeSymlinkDelete), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), first = UniqueName(dir), second = UniqueName(dir);
        WriteFile(first, "x");
        FileAction(() => File.CreateSymbolicLink(second, first));
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        FileAction(() => File.Delete(second));
        ExpectEventSequence(recorder, new WantEvent(EventKind.Delete, Text(second)));
    });
    public static void TestSubscribeSymlinkedDirectoryRebasesTargetEvents() => RunForEachWatcher(nameof(TestSubscribeSymlinkedDirectoryRebasesTargetEvents), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), target = Path.Combine(dir, "target"), link = Path.Combine(dir, "link");
        MakeDirectory(target);
        fixture.MakeDirSymlink(target, link);
        var(recorder, _) = fixture.Subscribe(watcher, link);
        WriteFile(Path.Combine(target, "child"), "x");
        ExpectContains(recorder, EventKind.Update, Text(Path.Combine(link, "child")));
    });
    public static void TestRecursiveSubscribeSymlinkedDirectoryDoesNotFollowDescendantSymlink() => RunForEachWatcher(nameof(TestRecursiveSubscribeSymlinkedDirectoryDoesNotFollowDescendantSymlink), (fixture, watcher) =>
    {
        if (watcher.HasFastRecursiveBackend())
            throw new OriginalSkip("fast recursive backends do not use the userspace recursive walk");
        string dir = fixture.NewTmpDir(), target = Path.Combine(dir, "target"), link = Path.Combine(dir, "link");
        MakeDirectory(target);
        fixture.MakeDirSymlink(target, link);
        string descendantTarget = Path.Combine(dir, "descendant-target"), descendantLink = Path.Combine(target, "descendant-link");
        MakeDirectory(descendantTarget);
        fixture.MakeDirSymlink(descendantTarget, descendantLink);
        var(recorder, _) = fixture.Subscribe(watcher, link);
        string logicalGrandchild = Path.Combine(link, "descendant-link", "grandchild");
        string physicalGrandchild = Path.Combine(descendantTarget, "grandchild");
        WriteFile(logicalGrandchild, "x");
        WriteFile(Path.Combine(target, "marker"), "flush");
        var actual = ExpectContains(recorder, EventKind.Update, Text(Path.Combine(link, "marker")));
        actual = actual.Append(recorder.DrainQuiet(Debounce.MaxWaitTime * 2));
        Check(FilterEventsForPaths(actual, Text(logicalGrandchild)).Length == 0, "expected no events through descendant symlink");
        Check(FilterEventsForPaths(actual, Text(physicalGrandchild)).Length == 0, "expected no events for descendant symlink target");
    });
}

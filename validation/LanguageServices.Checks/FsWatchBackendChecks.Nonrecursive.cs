// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Direct original watcher_test.go:2420–2575; allocated-empty options preserve nonrecursive admission.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static void TestNonRecursiveFileCreate() => RunForEachWatcher(nameof(TestNonRecursiveFileCreate), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir, Slice<IWatchOption>.Make(0));
        string file = UniqueName(dir);
        WriteFile(file, "hello");
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(file)));
    });
    public static void TestNonRecursiveFileUpdate() => RunForEachWatcher(nameof(TestNonRecursiveFileUpdate), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir, Slice<IWatchOption>.Make(0));
        string file = UniqueName(dir);
        WriteFile(file, "v1");
        _ = recorder.WaitForEvent(recorder.Deadline(), static _ => true);
        WriteFile(file, "v2-longer");
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(file)));
    });
    public static void TestNonRecursiveFileDelete() => RunForEachWatcher(nameof(TestNonRecursiveFileDelete), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), file = UniqueName(dir);
        WriteFile(file, "x");
        var(recorder, _) = fixture.Subscribe(watcher, dir, Slice<IWatchOption>.Make(0));
        FileAction(() => File.Delete(file));
        ExpectEventSequence(recorder, new WantEvent(EventKind.Delete, Text(file)));
    });
    public static void TestNonRecursiveDirCreate() => RunForEachWatcher(nameof(TestNonRecursiveDirCreate), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir, Slice<IWatchOption>.Make(0));
        string sub = UniqueName(dir);
        MakeDirectory(sub);
        ExpectContains(recorder, EventKind.Update, Text(sub));
    });
    public static void TestNonRecursiveGrandchildIgnored() => RunForEachWatcher(nameof(TestNonRecursiveGrandchildIgnored), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), sub = Path.Combine(dir, "child");
        MakeDirectory(sub);
        var(recorder, _) = fixture.Subscribe(watcher, dir, Slice<IWatchOption>.Make(0));
        string grandchild = UniqueName(sub);
        WriteFile(grandchild, "deep");
        string marker = UniqueName(dir);
        WriteFile(marker, "flush");
        var actual = ExpectContains(recorder, EventKind.Update, Text(marker));
        actual = actual.Append(recorder.DrainQuiet(Debounce.MaxWaitTime * 2));
        Check(FilterEventsForPaths(actual, Text(grandchild)).Length == 0, "expected no events for grandchild");
    });
    public static void TestNonRecursiveNewSubdirContentIgnored() => RunForEachWatcher(nameof(TestNonRecursiveNewSubdirContentIgnored), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir, Slice<IWatchOption>.Make(0));
        string sub = UniqueName(dir);
        MakeDirectory(sub);
        ExpectContains(recorder, EventKind.Update, Text(sub));
        string grandchild = UniqueName(sub);
        WriteFile(grandchild, "nested");
        string marker = UniqueName(dir);
        WriteFile(marker, "flush");
        var actual = ExpectContains(recorder, EventKind.Update, Text(marker));
        actual = actual.Append(recorder.DrainQuiet(Debounce.MaxWaitTime * 2));
        Check(FilterEventsForPaths(actual, Text(grandchild)).Length == 0, "expected no events for nested file");
    });
    public static void TestNonRecursiveAndRecursiveSameDir() => RunForEachWatcher(nameof(TestNonRecursiveAndRecursiveSameDir), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), sub = Path.Combine(dir, "child");
        MakeDirectory(sub);
        var(nonrecursive, _) = fixture.Subscribe(watcher, dir, Slice<IWatchOption>.Make(0));
        var(recursive, _) = fixture.Subscribe(watcher, dir, Slice<IWatchOption>.FromArray([FsWatch.WithRecursive()]));
        string grandchild = UniqueName(sub);
        WriteFile(grandchild, "deep");
        string marker = UniqueName(dir);
        WriteFile(marker, "flush");
        ExpectContains(recursive, EventKind.Update, Text(grandchild));
        var actual = ExpectContains(nonrecursive, EventKind.Update, Text(marker));
        actual = actual.Append(nonrecursive.DrainQuiet(Debounce.MaxWaitTime * 2));
        Check(FilterEventsForPaths(actual, Text(grandchild)).Length == 0, "non-recursive: expected no events for grandchild");
    });
}

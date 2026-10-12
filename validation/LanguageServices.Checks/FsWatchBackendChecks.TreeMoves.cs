// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Direct original watcher_test.go subtree deletion/move and atomic-save assertions.
using System.Diagnostics;
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static void TestRenameDirOutOfTreeNoStaleEvents() => RunForEachWatcher(nameof(TestRenameDirOutOfTreeNoStaleEvents), (fixture, watcher) =>
    {
        string watched = fixture.NewTmpDir(), outside = fixture.NewTmpDir();
        string sub = Path.Combine(watched, "sub"), inner = Path.Combine(sub, "inner");
        MakeDirectory(inner);
        WriteFile(Path.Combine(inner, "leaf.txt"), "v1");
        var(recorder, _) = fixture.Subscribe(watcher, watched, Slice<IWatchOption>.FromArray([FsWatch.WithRecursive()]));
        string destination = Path.Combine(outside, "moved");
        FileAction(() => Directory.Move(sub, destination));
        _ = recorder.DrainQuiet(TimeSpan.FromMilliseconds(500));
        WriteFile(Path.Combine(destination, "inner", "leaf.txt"), "v2-longer");
        var extra = recorder.DrainQuiet(TimeSpan.FromMilliseconds(800));
        var oldPrefix = Text(sub + Path.DirectorySeparatorChar);
        foreach (var value in extra)
            Check(!value.Path.EqualsValue(Text(sub)) && !value.Path.StartsWith(oldPrefix), "stale event for moved-out path");
    });
    public static void TestSubscribeNestedDirDeletionCleansDescendants() => RunForEachWatcher(nameof(TestSubscribeNestedDirDeletionCleansDescendants), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), sub = Path.Combine(dir, "parent"), nested = Path.Combine(sub, "child");
        MakeDirectory(nested);
        WriteFile(Path.Combine(nested, "file.txt"), "x");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        FileAction(() => Directory.Delete(sub, recursive: true));
        ExpectContains(recorder, EventKind.Delete, Text(sub));
    });
    public static void TestRecursiveMoveInPrePopulated() => RunForEachWatcher(nameof(TestRecursiveMoveInPrePopulated), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), outside = fixture.NewTmpDir();
        MakeDirectory(Path.Combine(outside, "a", "b", "c"));
        var(recorder, _) = fixture.Subscribe(watcher, dir, Slice<IWatchOption>.FromArray([FsWatch.WithRecursive()]));
        string destination = Path.Combine(dir, "tree");
        FileAction(() => Directory.Move(outside, destination));
        _ = recorder.DrainQuiet(TimeSpan.FromMilliseconds(500));
        string nested = Path.Combine(destination, "a", "b", "c");
        var prefix = Text(nested + Path.DirectorySeparatorChar);
        long start = Stopwatch.GetTimestamp();
        Slice<WatchEvent> allSeen = default;
        for (int attempt = 0; Stopwatch.GetElapsedTime(start) < recorder.Deadline(); attempt++)
        {
            WriteFile(Path.Combine(nested, $"deep-{attempt}.txt"), "hello");
            var more = recorder.WaitForEvent(TimeSpan.FromMilliseconds(750), value => value.Kind == EventKind.Update && value.Path.StartsWith(prefix));
            allSeen = allSeen.Append(more);
            foreach (var value in more)
                if (value.Kind == EventKind.Update && value.Path.StartsWith(prefix))
                    return;
        }

        Check(false, "expected update for a file inside moved-in tree; observed " + allSeen.Length + " events");
    });
    public static void TestAtomicSave() => RunForEachWatcher(nameof(TestAtomicSave), (fixture, watcher) =>
    {
        string target = Path.Combine(fixture.NewTmpDir(), "config.json");
        WriteFile(target, "{\"v\":1}");
        var(recorder, _) = fixture.Subscribe(watcher, Path.GetDirectoryName(target)!);
        string temporary = target + ".tmp";
        WriteFile(temporary, "{\"v\":2}");
        FileAction(() => File.Move(temporary, target, overwrite: true));
        var actual = recorder.WaitForEvent(recorder.Deadline(), value => value.Path.EqualsValue(Text(target)));
        Check(FilterEventsForPaths(actual, Text(target)).Length != 0, "expected events after atomic save");
    });
    public static void TestAtomicSaveFileWatch() => RunForEachWatcher(nameof(TestAtomicSaveFileWatch), (fixture, watcher) =>
    {
        string target = Path.Combine(fixture.NewTmpDir(), "target.txt");
        WriteFile(target, "v1");
        var(recorder, _) = fixture.SubscribeFile(watcher, target);
        string temporary = target + ".tmp";
        WriteFile(temporary, "v2");
        FileAction(() => File.Move(temporary, target, overwrite: true));
        var actual = recorder.WaitForEvent(recorder.Deadline(), value => value.Path.EqualsValue(Text(target)));
        Check(FilterEventsForPaths(actual, Text(target)).Length != 0, "expected events after atomic save");
    });
    public static void TestReplaceDirWithFile() => RunForEachWatcher(nameof(TestReplaceDirWithFile), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), child = Path.Combine(dir, "child");
        MakeDirectory(child);
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        FileAction(() => Directory.Delete(child));
        WriteFile(child, "now a file");
        var actual = recorder.WaitForEvent(recorder.Deadline(), value => value.Path.EqualsValue(Text(child)));
        Check(FilterEventsForPaths(actual, Text(child)).Length != 0, "expected events for dir->file replacement");
    });
    public static void TestRecreateSubdirAndModify() => RunForEachWatcher(nameof(TestRecreateSubdirAndModify), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), sub = Path.Combine(dir, "sub");
        MakeDirectory(sub);
        WriteFile(Path.Combine(sub, "file.txt"), "v1");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        FileAction(() => Directory.Delete(sub, recursive: true));
        _ = recorder.DrainQuiet(TimeSpan.FromMilliseconds(500));
        MakeDirectory(sub);
        Thread.Sleep(150);
        var prefix = Text(sub + Path.DirectorySeparatorChar);
        long start = Stopwatch.GetTimestamp();
        Slice<WatchEvent> allSeen = default;
        for (int attempt = 0; Stopwatch.GetElapsedTime(start) < recorder.Deadline() * 2; attempt++)
        {
            WriteFile(Path.Combine(sub, $"attempt-{attempt}.txt"), "hi");
            var more = recorder.WaitForEvent(TimeSpan.FromMilliseconds(750), value => value.Kind == EventKind.Update && value.Path.StartsWith(prefix));
            allSeen = allSeen.Append(more);
            foreach (var value in more)
                if (value.Kind == EventKind.Update && value.Path.StartsWith(prefix))
                    return;
        }

        Check(false, "expected update for a file inside recreated sub; observed " + allSeen.Length + " events");
    });
    public static void TestReplaceParentDirWithDifferent() => RunForEachWatcher(nameof(TestReplaceParentDirWithDifferent), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), sub = Path.Combine(dir, "pkg");
        MakeDirectory(Path.Combine(sub, "old"));
        WriteFile(Path.Combine(sub, "old", "a.txt"), "a");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        FileAction(() => Directory.Delete(sub, recursive: true));
        string newDir = Path.Combine(sub, "new");
        MakeDirectory(newDir);
        WriteFile(Path.Combine(newDir, "b.txt"), "b");
        _ = recorder.DrainQuiet(TimeSpan.FromMilliseconds(500));
        var prefix = Text(newDir + Path.DirectorySeparatorChar);
        long start = Stopwatch.GetTimestamp();
        Slice<WatchEvent> allSeen = default;
        for (int attempt = 0; Stopwatch.GetElapsedTime(start) < recorder.Deadline(); attempt++)
        {
            WriteFile(Path.Combine(newDir, $"attempt-{attempt}.txt"), "hi");
            var more = recorder.WaitForEvent(TimeSpan.FromMilliseconds(750), value => value.Kind == EventKind.Update && value.Path.StartsWith(prefix));
            allSeen = allSeen.Append(more);
            foreach (var value in more)
                if (value.Kind == EventKind.Update && value.Path.StartsWith(prefix))
                    return;
        }

        Check(false, "expected update for a file inside replaced tree; observed " + allSeen.Length + " events");
    });
    public static void TestRoundTripRename() => RunForEachWatcher(nameof(TestRoundTripRename), (fixture, watcher) =>
    {
        if (ReferenceEquals(watcher, FsWatch.Kqueue()))
            throw new OriginalSkip("kqueue fd-based tracking delivers stale delete before parent NOTE_WRITE reconciles");
        string dir = fixture.NewTmpDir(), original = Path.Combine(dir, "data.txt");
        WriteFile(original, "content");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string temporary = Path.Combine(dir, "data.txt.bak");
        FileAction(() => File.Move(original, temporary));
        FileAction(() => File.Move(temporary, original));
        var actual = FilterEventsForPaths(recorder.GatherUntilQuiet(recorder.Deadline(), TimeSpan.FromMilliseconds(500)), Text(original));
        bool hasDelete = Contains(actual, EventKind.Delete, Text(original));
        bool hasUpdate = Contains(actual, EventKind.Update, Text(original));
        Check(!hasDelete || hasUpdate, "round-trip rename left a stale delete without recovery");
    });
}

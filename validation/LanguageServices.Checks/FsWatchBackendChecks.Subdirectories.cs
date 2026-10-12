// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Direct original watcher_test.go:1071–1210 assertions; fixture backend unchanged.
using TypeScript.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static void TestSubscribeSubfileCreate() => RunForEachWatcher(nameof(TestSubscribeSubfileCreate), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string sub = UniqueName(dir);
        MakeDirectory(sub);
        ExpectContains(recorder, EventKind.Update, Text(sub));
        // Original wait for inotify to finish installing the new directory watch.
        Thread.Sleep(100);
        string file = UniqueName(sub);
        WriteFile(file, "hi");
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(file)));
    });
    public static void TestSubscribeSubfileUpdate() => RunForEachWatcher(nameof(TestSubscribeSubfileUpdate), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), sub = UniqueName(dir);
        MakeDirectory(sub);
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string file = UniqueName(sub);
        WriteFile(file, "v1");
        _ = recorder.WaitForEvent(recorder.Deadline(), static _ => true);
        WriteFile(file, "v2-longer");
        ExpectContains(recorder, EventKind.Update, Text(file));
    });
    public static void TestSubscribeSubfileRename() => RunForEachWatcher(nameof(TestSubscribeSubfileRename), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), sub = UniqueName(dir);
        MakeDirectory(sub);
        string first = UniqueName(sub);
        WriteFile(first, "x");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string second = UniqueName(sub);
        FileAction(() => File.Move(first, second));
        WantEvent[] expected = [new WantEvent(EventKind.Delete, Text(first)), new WantEvent(EventKind.Update, Text(second))];
        var actual = recorder.WaitForAll(recorder.Deadline(), expected);
        AssertEventSet(FilterEventsForPaths(actual, Text(first), Text(second)), expected);
    });
    public static void TestSubscribeSubfileDelete() => RunForEachWatcher(nameof(TestSubscribeSubfileDelete), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), sub = UniqueName(dir);
        MakeDirectory(sub);
        string file = UniqueName(sub);
        WriteFile(file, "x");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        FileAction(() => File.Delete(file));
        WantEvent[] expected = [new WantEvent(EventKind.Delete, Text(file))];
        var actual = recorder.WaitForAll(recorder.Deadline(), expected);
        AssertEventSequence(FilterEventsForPaths(actual, Text(file)), expected);
    });
    public static void TestSubscribeSubdirCreate() => RunForEachWatcher(nameof(TestSubscribeSubdirCreate), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), sub = UniqueName(dir);
        MakeDirectory(sub);
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string nested = UniqueName(sub);
        MakeDirectory(nested);
        WantEvent[] expected = [new WantEvent(EventKind.Update, Text(nested))];
        var actual = recorder.WaitForAll(recorder.Deadline(), expected);
        AssertEventSequence(FilterEventsForPaths(actual, Text(nested)), expected);
    });
    public static void TestSubscribeSubdirDeleteWithFiles() => RunForEachWatcher(nameof(TestSubscribeSubdirDeleteWithFiles), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), sub = UniqueName(dir);
        MakeDirectory(sub);
        string child = UniqueName(sub);
        WriteFile(child, "x");
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        FileAction(() => Directory.Delete(sub, recursive: true));
        ExpectEventSet(recorder, new WantEvent(EventKind.Delete, Text(sub)), new WantEvent(EventKind.Delete, Text(child)));
    });
}

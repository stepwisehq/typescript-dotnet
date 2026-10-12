// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Direct original watcher_test.go:2608–2755 file-target assertions.
using TypeScript.Scanner;
using TypeScript.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static void TestFileWatchCreate() => RunForEachWatcher(nameof(TestFileWatchCreate), (fixture, watcher) =>
    {
        string file = Path.Combine(fixture.NewTmpDir(), "target.txt");
        var(recorder, _) = fixture.SubscribeFile(watcher, file);
        WriteFile(file, "hello");
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(file)));
    });
    public static void TestFileWatchUpdate() => RunForEachWatcher(nameof(TestFileWatchUpdate), (fixture, watcher) =>
    {
        string file = Path.Combine(fixture.NewTmpDir(), "target.txt");
        WriteFile(file, "v1");
        var(recorder, _) = fixture.SubscribeFile(watcher, file);
        WriteFile(file, "v2-longer");
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(file)));
    });
    public static void TestFileWatchDelete() => RunForEachWatcher(nameof(TestFileWatchDelete), (fixture, watcher) =>
    {
        string file = Path.Combine(fixture.NewTmpDir(), "target.txt");
        WriteFile(file, "x");
        var(recorder, _) = fixture.SubscribeFile(watcher, file);
        FileAction(() => File.Delete(file));
        ExpectEventSequence(recorder, new WantEvent(EventKind.Delete, Text(file)));
    });
    public static void TestFileWatchIgnoresSiblings() => RunForEachWatcher(nameof(TestFileWatchIgnoresSiblings), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), target = Path.Combine(dir, "target.txt"), sibling = Path.Combine(dir, "sibling.txt");
        var(recorder, _) = fixture.SubscribeFile(watcher, target);
        var(witness, _) = fixture.Subscribe(watcher, dir, Slice<IWatchOption>.Make(0));
        WriteFile(sibling, "noise");
        ExpectContains(witness, EventKind.Update, Text(sibling));
        ExpectNoBufferedEvents(recorder, "expected no events for sibling");
    });
    public static void TestFileWatchMultipleSameDir() => RunForEachWatcher(nameof(TestFileWatchMultipleSameDir), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir(), firstFile = Path.Combine(dir, "a.txt"), secondFile = Path.Combine(dir, "b.txt");
        var(first, _) = fixture.SubscribeFile(watcher, firstFile);
        var(second, _) = fixture.SubscribeFile(watcher, secondFile);
        WriteFile(firstFile, "hello");
        AssertEventSequence(first.Next(first.Deadline()), [new WantEvent(EventKind.Update, Text(firstFile))]);
        ExpectNoBufferedEvents(second, "r2 should not see f1 events");
        WriteFile(secondFile, "world");
        AssertEventSequence(second.Next(second.Deadline()), [new WantEvent(EventKind.Update, Text(secondFile))]);
        ExpectNoBufferedEvents(first, "r1 should not see f2 events");
    });
    public static void TestFileWatchDeleteAndRecreate() => RunForEachWatcher(nameof(TestFileWatchDeleteAndRecreate), (fixture, watcher) =>
    {
        string file = Path.Combine(fixture.NewTmpDir(), "config.json");
        WriteFile(file, "{\"v\":1}");
        var(recorder, _) = fixture.SubscribeFile(watcher, file);
        FileAction(() => File.Delete(file));
        ExpectEventSequence(recorder, new WantEvent(EventKind.Delete, Text(file)));
        WriteFile(file, "{\"v\":2}");
        ExpectContains(recorder, EventKind.Update, Text(file));
    });
    public static void TestFileWatchNonExistentTarget() => RunForEachWatcher(nameof(TestFileWatchNonExistentTarget), (fixture, watcher) =>
    {
        string file = Path.Combine(fixture.NewTmpDir(), "doesnotexist.txt");
        var(recorder, _) = fixture.SubscribeFile(watcher, file);
        WriteFile(file, "appeared");
        ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(file)));
    });
}

// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Direct original watcher_test.go:1438–1532 assertions.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static void TestSubscribeMultipleSameDir() => RunForEachWatcher(nameof(TestSubscribeMultipleSameDir), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        Thread.Sleep(50);
        var first = new Recorder
        {
            Attempt = fixture.Attempt
        };
        var(firstWatch, firstError) = watcher.WatchDirectory(Text(dir), first.Callback);
        Check(firstError is null && firstWatch is not null, "first subscribe failed: " + firstError?.Message);
        fixture.Cleanup(() => _ = firstWatch!.Close());
        var second = new Recorder
        {
            Attempt = fixture.Attempt
        };
        var(secondWatch, secondError) = watcher.WatchDirectory(Text(dir), second.Callback);
        Check(secondError is null && secondWatch is not null, "second subscribe failed: " + secondError?.Message);
        fixture.Cleanup(() => _ = secondWatch!.Close());
        Thread.Sleep(100);
        string file = UniqueName(dir);
        WriteFile(file, "hi");
        AssertEventSequence(first.Next(first.Deadline()), [new WantEvent(EventKind.Update, Text(file))]);
        AssertEventSequence(second.Next(second.Deadline()), [new WantEvent(EventKind.Update, Text(file))]);
    });
    public static void TestSubscribeMultipleDifferentDirs() => RunForEachWatcher(nameof(TestSubscribeMultipleDifferentDirs), (fixture, watcher) =>
    {
        string firstDir = fixture.NewTmpDir(), secondDir = fixture.NewTmpDir();
        var(first, _) = fixture.Subscribe(watcher, firstDir);
        var(second, _) = fixture.Subscribe(watcher, secondDir);
        string firstFile = UniqueName(firstDir), secondFile = UniqueName(secondDir);
        WriteFile(firstFile, "a");
        WriteFile(secondFile, "b");
        AssertEventSequence(first.Next(first.Deadline()), [new WantEvent(EventKind.Update, Text(firstFile))]);
        AssertEventSequence(second.Next(second.Deadline()), [new WantEvent(EventKind.Update, Text(secondFile))]);
    });
    public static void TestWatchDirectoriesBatch() => RunForEachWatcher(nameof(TestWatchDirectoriesBatch), (fixture, watcher) =>
    {
        string firstDir = fixture.NewTmpDir(), secondDir = fixture.NewTmpDir();
        var first = new Recorder
        {
            Attempt = fixture.Attempt,
            Watcher = watcher
        };
        var second = new Recorder
        {
            Attempt = fixture.Attempt,
            Watcher = watcher
        };
        var requests = Slice<WatchDirectoryRequest>.FromArray([new() { Dir = Text(firstDir), Callback = first.Callback, Options = Slice<IWatchOption>.FromArray([FsWatch.WithRecursive()]) }, new() { Dir = Text(secondDir), Callback = second.Callback, Options = Slice<IWatchOption>.FromArray([FsWatch.WithRecursive()]) }]);
        var(watches, error) = watcher.WatchDirectories(requests);
        Check(error is null, "batch subscribe failed: " + error?.Message);
        fixture.Cleanup(() =>
        {
            foreach (var watch in watches)
                _ = watch.Close();
        });
        Thread.Sleep(ReferenceEquals(watcher, FsWatch.FSEvents()) || ReferenceEquals(watcher, FsWatch.Kqueue()) ? 300 : 60);
        string firstFile = UniqueName(firstDir), secondFile = UniqueName(secondDir);
        WriteFile(firstFile, "a");
        WriteFile(secondFile, "b");
        AssertEventSequence(first.Next(first.Deadline()), [new WantEvent(EventKind.Update, Text(firstFile))]);
        AssertEventSequence(second.Next(second.Deadline()), [new WantEvent(EventKind.Update, Text(secondFile))]);
    });
}

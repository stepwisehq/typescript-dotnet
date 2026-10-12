// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Direct original watcher_test.go:1810–1932 admission and close assertions.
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static void TestSubscribeMissingDirError() => RunForEachWatcher(nameof(TestSubscribeMissingDirError), (fixture, watcher) =>
    {
        string bogus = Path.Combine(fixture.NewTmpDir(), "definitely-not-here");
        var(_, error) = watcher.WatchDirectory(Text(bogus), static (_, _) =>
        {
        });
        Check(error is not null, "expected error subscribing to non-existent dir");
    });
    public static void TestSubscribeNotADirError() => RunForEachWatcher(nameof(TestSubscribeNotADirError), (fixture, watcher) =>
    {
        string file = UniqueName(fixture.NewTmpDir());
        WriteFile(file, "x");
        var(_, error) = watcher.WatchDirectory(Text(file), static (_, _) =>
        {
        });
        Check(error is not null, "expected error subscribing to a file");
    });
    public static void TestSubscribeRejectsNilCallback() => RunForEachWatcher(nameof(TestSubscribeRejectsNilCallback), (fixture, watcher) =>
    {
        var(_, error) = watcher.WatchDirectory(Text(fixture.NewTmpDir()), null!);
        Check(error is not null, "WatchDirectory(nil callback) should return an error");
    });
    public static void TestSubscribeRejectsRelativePath() => RunForEachWatcher(nameof(TestSubscribeRejectsRelativePath), (fixture, watcher) =>
    {
        var(_, directoryError) = watcher.WatchDirectory(Text("relative/path"), static (_, _) =>
        {
        });
        Check(directoryError is not null, "WatchDirectory with relative path should return an error");
        var(_, fileError) = watcher.WatchFile(Text("relative/path/file.txt"), static (_, _) =>
        {
        });
        Check(fileError is not null, "WatchFile with relative path should return an error");
    });
    public static void TestSubscribeUnsubscribeIdempotent() => RunForEachWatcher(nameof(TestSubscribeUnsubscribeIdempotent), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var recorder = new Recorder
        {
            Attempt = fixture.Attempt
        };
        var(watch, error) = watcher.WatchDirectory(Text(dir), recorder.Callback);
        Check(error is null && watch is not null, "subscribe failed: " + error?.Message);
        Check(watch!.Close()is null, "first Close failed");
        Check(watch.Close()is null, "second Close should be a no-op");
    });
    public static void TestSubscribeCloseThenReSubscribe() => RunForEachWatcher(nameof(TestSubscribeCloseThenReSubscribe), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var first = new Recorder
        {
            Attempt = fixture.Attempt
        };
        var(firstWatch, firstError) = watcher.WatchDirectory(Text(dir), first.Callback);
        Check(firstError is null && firstWatch is not null, "subscribe failed: " + firstError?.Message);
        Check(firstWatch!.Close()is null, "Close failed");
        var second = new Recorder
        {
            Attempt = fixture.Attempt
        };
        var(secondWatch, secondError) = watcher.WatchDirectory(Text(dir), second.Callback);
        Check(secondError is null && secondWatch is not null, "re-WatchDirectory after Close: " + secondError?.Message);
        fixture.Cleanup(() => _ = secondWatch!.Close());
        Thread.Sleep(ReferenceEquals(watcher, FsWatch.FSEvents()) || ReferenceEquals(watcher, FsWatch.Kqueue()) ? 300 : 60);
        string file = UniqueName(dir);
        WriteFile(file, "hi");
        ExpectEventSequence(second, new WantEvent(EventKind.Update, Text(file)));
        Check(first.DrainQuiet(TimeSpan.FromMilliseconds(50)).Length == 0, "closed watch saw events");
    });
}

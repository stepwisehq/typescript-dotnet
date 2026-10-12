// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Direct original watcher_test.go coalescing assertions. Kernel batch boundaries remain unconstrained.
using TypeScript.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static void TestSubscribeCoalesceCreateUpdate() => RunForEachWatcher(nameof(TestSubscribeCoalesceCreateUpdate), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string file = UniqueName(dir);
        WriteFile(file, "v1");
        WriteFile(file, "v2");
        var actual = recorder.GatherUntilQuiet(recorder.Deadline(), Debounce.MaxWaitTime * 3);
        AssertEventSet(ReplayEventList(FilterEventsForPaths(actual, Text(file))), [new WantEvent(EventKind.Update, Text(file))]);
    });
    public static void TestSubscribeCoalesceDeleteCreateAsUpdate() => RunForEachWatcher(nameof(TestSubscribeCoalesceDeleteCreateAsUpdate), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string file = UniqueName(dir);
        WriteFile(file, "v1");
        _ = recorder.WaitForEvent(recorder.Deadline(), static _ => true);
        FileAction(() => File.Delete(file));
        WriteFile(file, "v2");
        var actual = recorder.GatherUntilQuiet(recorder.Deadline(), Debounce.MaxWaitTime * 3);
        AssertEventSet(ReplayEventList(FilterEventsForPaths(actual, Text(file))), [new WantEvent(EventKind.Update, Text(file))]);
    });
    public static void TestSubscribeCoalesceCreateThenDelete() => RunForEachWatcher(nameof(TestSubscribeCoalesceCreateThenDelete), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string first = UniqueName(dir), second = UniqueName(dir);
        WriteFile(first, "x");
        WriteFile(second, "x");
        FileAction(() => File.Delete(second));
        // Original quiet window exceeds maximum debounce duration; check the net effect, not batch count.
        var actual = recorder.GatherUntilQuiet(recorder.Deadline(), Debounce.MaxWaitTime * 3);
        AssertEventSet(ReplayEventList(actual), [new WantEvent(EventKind.Update, Text(first))]);
    });
    public static void TestSubscribeCoalesceMultipleUpdates() => RunForEachWatcher(nameof(TestSubscribeCoalesceMultipleUpdates), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string file = UniqueName(dir);
        WriteFile(file, "v1");
        _ = recorder.WaitForEvent(recorder.Deadline(), static _ => true);
        foreach (string value in new[]
        {
            "v2",
            "v3",
            "v4"
        }

        )
            WriteFile(file, value);
        var actual = recorder.GatherUntilQuiet(recorder.Deadline(), Debounce.MaxWaitTime * 3);
        AssertEventSet(ReplayEventList(FilterEventsForPaths(actual, Text(file))), [new WantEvent(EventKind.Update, Text(file))]);
    });
    public static void TestSubscribeCoalesceUpdateDelete() => RunForEachWatcher(nameof(TestSubscribeCoalesceUpdateDelete), (fixture, watcher) =>
    {
        string dir = fixture.NewTmpDir();
        var(recorder, _) = fixture.Subscribe(watcher, dir);
        string file = UniqueName(dir);
        // Consume the immediate first event so the subsequent pair exercises coalescing.
        WriteFile(file, "v1");
        _ = recorder.WaitForEvent(recorder.Deadline(), static _ => true);
        WriteFile(file, "v2");
        FileAction(() => File.Delete(file));
        var actual = recorder.GatherUntilQuiet(recorder.Deadline(), Debounce.MaxWaitTime * 3);
        AssertEventSet(ReplayEventList(FilterEventsForPaths(actual, Text(file))), [new WantEvent(EventKind.Delete, Text(file))]);
    });
}

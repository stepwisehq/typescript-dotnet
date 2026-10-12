// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Direct original watcher_test.go:1672–1810 callback assertions.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static void TestConsolidatedSymlinkChildMapsSharedLogicalPath()
    {
        using var fixture = new Fixture(nameof(TestConsolidatedSymlinkChildMapsSharedLogicalPath), 1);
        string root = fixture.NewTmpDir(), physicalParent = Path.Combine(root, "physical-parent");
        MakeDirectory(Path.Combine(physicalParent, "target"));
        string logicalParent = Path.Combine(root, "logical-parent");
        fixture.MakeDirSymlink(physicalParent, logicalParent);
        string link = Path.Combine(logicalParent, "link");
        fixture.MakeDirSymlink(Path.Combine(physicalParent, "target"), link);
        var callback = new Callback
        {
            Dir = Text(link),
            PhysicalDir = FsWatch.PhysicalDirFor(Text(link)),
            WatchDir = Text(logicalParent),
            WatchPhysicalDir = FsWatch.PhysicalDirFor(Text(logicalParent))
        };
        var actual = callback.MapEvent(new WatchEvent { Kind = EventKind.Update, Path = Text(Path.Combine(logicalParent, "target", "file.ts")) });
        Check(actual.Path.EqualsValue(Text(Path.Combine(link, "file.ts"))), "mapEvent path differs");
    }

    public static void TestConsolidatedChildWatchFiltersAgainstRequestedDir()
    {
        using var fixture = new Fixture(nameof(TestConsolidatedChildWatchFiltersAgainstRequestedDir), 1);
        string parent = Path.Combine(fixture.NewTmpDir(), "parent"), child = Path.Combine(parent, "child"), sibling = Path.Combine(parent, "sibling");
        var watch = fixture.NewDirectWatcher(parent);
        Slice<WatchEvent> actual = default;
        watch.Watch(Text(child), Text(child), false, (events, error) =>
        {
            Check(error is null, "callback error");
            actual = actual.Append(events);
        }, null);
        watch.Events.UpdateWatchRootAt(Text(child), 1);
        watch.Events.Update(Text(Path.Combine(child, "file.ts")));
        watch.Events.Update(Text(Path.Combine(child, "nested", "file.ts")));
        watch.Events.Update(Text(Path.Combine(sibling, "file.ts")));
        watch.TriggerCallbacks();
        // Original compares the complete event set, not only wanted paths.
        WantEvent[] expected = [new WantEvent(EventKind.Update, Text(child)), new WantEvent(EventKind.Update, Text(Path.Combine(child, "file.ts")))];
        var actualValues = new WantEvent[actual.Length];
        for (int index = 0; index < actual.Length; index++)
            actualValues[index] = new(actual[index].Kind, actual[index].Path);
        GoSliceSort.SortFunc(actualValues.AsSpan(), CompareEvents);
        GoSliceSort.SortFunc(expected.AsSpan(), CompareEvents);
        AssertWantEvents(actualValues, expected, "complete event set");
    }

    public static void TestConsolidatedChildWatchIgnoresEventsBeforeSubscribe()
    {
        using var fixture = new Fixture(nameof(TestConsolidatedChildWatchIgnoresEventsBeforeSubscribe), 1);
        string parent = Path.Combine(fixture.NewTmpDir(), "parent"), child = Path.Combine(parent, "child");
        var watch = fixture.NewDirectWatcher(parent);
        watch.Events.Update(Text(child));
        Slice<WatchEvent> actual = default;
        watch.Watch(Text(child), Text(child), true, (events, error) =>
        {
            Check(error is null, "callback error");
            actual = actual.Append(events);
        }, null);
        watch.Events.Remove(Text(child));
        watch.TriggerCallbacks();
        AssertEventSequence(actual, [new WantEvent(EventKind.Delete, Text(child))]);
    }

    public static void TestRecursiveWatchWithIgnoreDoesNotFilterByLogicalRoot()
    {
        using var fixture = new Fixture(nameof(TestRecursiveWatchWithIgnoreDoesNotFilterByLogicalRoot), 1);
        string dir = Path.Combine(fixture.NewTmpDir(), "root");
        var watch = fixture.NewDirectWatcher(dir);
        Slice<WatchEvent> actual = default;
        string outside = Path.Combine(fixture.NewTmpDir(), "outside", "pkg", "index.ts");
        watch.Watch(Text(dir), Text(dir), true, (events, error) =>
        {
            Check(error is null, "callback error");
            actual = actual.Append(events);
        }, static _ => false);
        watch.Events.Update(Text(outside));
        watch.TriggerCallbacks();
        AssertEventSequence(actual, [new WantEvent(EventKind.Update, Text(outside))]);
    }
}

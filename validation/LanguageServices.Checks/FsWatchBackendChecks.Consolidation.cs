// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Complete original watcher_test.go counting backend/consolidation assertions.
// Required same-package readonly accessors are queued for the coherent assertion promotion, not reflection.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    private sealed class CountingWatcher : WatcherBase
    {
        internal Slice<DirWatch> Subscribed, Closed;
        public override Exception? Start()
        {
            NotifyStarted();
            return null;
        }

        public override Exception? Subscribe(DirWatch watch)
        {
            Subscribed = Subscribed.Append(watch);
            return null;
        }

        public override Exception? CloseWatch(DirWatch watch)
        {
            Closed = Closed.Append(watch);
            return null;
        }
    }

    public static void TestFastRecursiveWatcherConsolidatesSiblingDirectories()
    {
        using var fixture = new Fixture(nameof(TestFastRecursiveWatcherConsolidatesSiblingDirectories), 1);
        string root = fixture.NewTmpDir(), parent = Path.Combine(root, "node_modules", ".bun");
        MakeDirectory(parent);
        CountingWatcher? implementation = null;
        var watcher = new Watcher(Text("fsevents"))
        {
            Factory = () => implementation = new CountingWatcher()
        };
        Slice<IWatch> subscriptions = default;
        fixture.Cleanup(() =>
        {
            foreach (var watch in subscriptions)
                _ = watch.Close();
        });
        for (int index = 0; index < Watcher.RecursiveConsolidateThreshold + 2; index++)
        {
            string directory = Path.Combine(parent, $"pkg{index}");
            MakeDirectory(directory);
            var(watch, error) = watcher.WatchDirectory(Text(directory), static (_, _) =>
            {
            });
            Check(error is null && watch is not null, "WatchDirectory failed: " + error?.Message);
            subscriptions = subscriptions.Append(watch!);
        }

        Check(implementation!.Subscribed.Length == Watcher.RecursiveConsolidateThreshold, "subscription count after consolidation differs");
        var consolidated = implementation.Subscribed[implementation.Subscribed.Length - 1];
        Check(consolidated.Dir.EqualsValue(Text(parent)) && consolidated.Recursive, "expected consolidated recursive parent watch");
        Check(!watcher.HasDirectoryWatchForOriginalAssertion(Text(Path.Combine(parent, "pkg11")), false), "later package should reuse consolidated parent");
    }

    public static void TestFastRecursiveWatcherDoesNotConsolidateSymlinkOutsideRoot()
    {
        using var fixture = new Fixture(nameof(TestFastRecursiveWatcherDoesNotConsolidateSymlinkOutsideRoot), 1);
        string root = fixture.NewTmpDir(), parent = Path.Combine(root, "node_modules", ".bun");
        MakeDirectory(parent);
        CountingWatcher? implementation = null;
        var watcher = new Watcher(Text("fsevents"))
        {
            Factory = () => implementation = new CountingWatcher()
        };
        Slice<IWatch> subscriptions = default;
        fixture.Cleanup(() =>
        {
            foreach (var watch in subscriptions)
                _ = watch.Close();
        });
        for (int index = 0; index < Watcher.RecursiveConsolidateThreshold; index++)
        {
            string directory = Path.Combine(parent, $"pkg{index}");
            MakeDirectory(directory);
            var(watch, error) = watcher.WatchDirectory(Text(directory), static (_, _) =>
            {
            });
            Check(error is null && watch is not null, "WatchDirectory failed: " + error?.Message);
            subscriptions = subscriptions.Append(watch!);
        }

        var consolidated = implementation!.Subscribed[implementation.Subscribed.Length - 1];
        Check(consolidated.Dir.EqualsValue(Text(parent)) && consolidated.Recursive, "expected consolidated recursive parent watch");
        string target = Path.Combine(root, "outside"), link = Path.Combine(parent, "linked");
        MakeDirectory(target);
        fixture.MakeDirSymlink(target, link);
        var(subscription, subscribeError) = watcher.WatchDirectory(Text(link), static (_, _) =>
        {
        });
        Check(subscribeError is null && subscription is not null, "WatchDirectory symlink failed: " + subscribeError?.Message);
        fixture.Cleanup(() => _ = subscription!.Close());
        var actual = ((WatchSubscription)subscription!).Directory;
        Check(!ReferenceEquals(actual, consolidated), "symlink outside physical root should keep own watch");
        Check(actual.Dir.EqualsValue(Text(link)) && actual.PhysicalDir.EqualsValue(FsWatch.PhysicalDirFor(Text(link))), "symlink root watch differs");
    }

    public static void TestConsolidatedSymlinkChildTerminatesFromSharedLogicalPath()
    {
        using var fixture = new Fixture(nameof(TestConsolidatedSymlinkChildTerminatesFromSharedLogicalPath), 1);
        string root = fixture.NewTmpDir(), physicalParent = Path.Combine(root, "physical-parent");
        MakeDirectory(Path.Combine(physicalParent, "target"));
        string logicalParent = Path.Combine(root, "logical-parent");
        fixture.MakeDirSymlink(physicalParent, logicalParent);
        string link = Path.Combine(logicalParent, "link");
        fixture.MakeDirSymlink(Path.Combine(physicalParent, "target"), link);
        // Initialize the same retained physical header supplied by the original assignment.
        var watch = DirWatch.NewDirWatch(Text(logicalParent), FsWatch.PhysicalDirFor(Text(logicalParent)), Debounce.NewDebounce());
        watch.Recursive = true;
        fixture.Cleanup(watch.DestroyDebounce);
        var(id, _) = watch.Watch(Text(link), FsWatch.PhysicalDirFor(Text(link)), true, static (_, _) =>
        {
        }, null);
        var error = new IOException("terminated");
        Check(watch.TerminateCallbacksForDeletedRoot(Text(Path.Combine(logicalParent, "target")), 1, error), "expected symlink child callback to terminate");
        lock (watch.CallbackGate)
            foreach (var callback in watch.CallbacksForOriginalAssertion)
                if (callback.ID == id)
                    Check(FsWatch.IsError(callback.Terminal, error), "terminal error differs");
    }
}

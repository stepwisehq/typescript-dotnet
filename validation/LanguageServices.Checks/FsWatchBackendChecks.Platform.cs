// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Original watcher_test.go platform selection assertions; Linux implementation contract.
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static void TestDefaultBackendMatchesPlatform()
    {
        string expected;
        if (OperatingSystem.IsLinux())
            expected = FsWatch.Fanotify().Available() ? "fanotify" : "inotify";
        else if (OperatingSystem.IsAndroid())
            expected = "inotify";
        else if (OperatingSystem.IsMacOS())
            expected = "fsevents";
        else if (OperatingSystem.IsWindows())
            expected = "windows";
        else if (OperatingSystem.IsFreeBSD())
            expected = "kqueue";
        else
            throw new OriginalSkip("no expected default watcher for current platform");
        var watcher = FsWatch.Default();
        Check(watcher.Available(), "Default() should be available");
        Check(watcher.Name().EqualsValue(Text(expected)), "Default().Name differs");
    }

    public static void TestUnavailableBackendReturnsError()
    {
        IWatcher? unavailable = null;
        foreach (var watcher in FsWatch.AllWatchers())
            if (!watcher.Available())
            {
                unavailable = watcher;
                break;
            }

        if (unavailable is null)
            throw new OriginalSkip("all watchers are available on this platform");
        using var fixture = new Fixture(nameof(TestUnavailableBackendReturnsError), 1);
        var(_, error) = unavailable.WatchDirectory(Text(fixture.NewTmpDir()), static (_, _) =>
        {
        });
        Check(FsWatch.IsError(error, FsWatch.ErrUnavailable), "expected ErrUnavailable");
    }
}

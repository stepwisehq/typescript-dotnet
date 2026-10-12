// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Complete original fanotify_linux_test.go assertions; real kernel admission and source-defined skips.
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    public static void TestLinuxFanotifyShutdownBeforeStart() => new FanotifyBackend(false).Shutdown();
    public static void TestLinuxFanotifyBackendSelection()
    {
        if (!FanotifyBackend.Available())
            throw new OriginalSkip("fanotify not available");
        var(implementation, error) = FsWatch.fanotifyWatcher.GetImpl();
        Check(error is null, "fanotify getImpl failed: " + error?.Message);
        Check(implementation is FanotifyBackend, "fanotify watcher must be fanotifyBackend");
    }

    public static void TestLinuxFanotifySubscribeCleansUpAfterMarkFailure()
    {
        using var fixture = new Fixture(nameof(TestLinuxFanotifySubscribeCleansUpAfterMarkFailure), 1);
        var watch = fixture.NewDirectWatcher(fixture.NewTmpDir());
        var backend = new FanotifyBackend(false);
        var error = backend.Subscribe(watch);
        Check(error is DirWatchError, "subscribe error must be dirWatchError");
        Check(ReferenceEquals(((DirWatchError)error!).Watch, watch), "dirWatchError must retain actual watch");
        Check(backend.SubscriptionCountForOriginalAssertion == 0, "subscriptions not cleaned up");
    }

    public static void TestLinuxFanotifyParseDfidNameRoundTrip()
    {
        using var fixture = new Fixture(nameof(TestLinuxFanotifyParseDfidNameRoundTrip), 1);
        var directory = Text(fixture.NewTmpDir());
        var(handle, type, error) = LinuxKernel.NameToHandleAt(-100, directory, 0);
        if (error is not null)
            throw new OriginalSkip("NameToHandleAt not supported: " + error.Message);
        var(fsid0, fsid1, statError) = LinuxKernel.StatFs(directory);
        Check(statError is null, "Statfs failed: " + statError?.Message);
        var key = FanotifyHandleKey.Make(fsid0, fsid1, type, handle.AsSpan());
        Check(key.Handle.Value.Length != 0, "empty handle bytes");
        var(secondHandle, secondType, secondError) = LinuxKernel.NameToHandleAt(-100, directory, 0);
        Check(secondError is null, "second NameToHandleAt failed: " + secondError?.Message);
        var second = FanotifyHandleKey.Make(fsid0, fsid1, secondType, secondHandle.AsSpan());
        Check(key == second, "handle keys differ for same path");
    }

    public static void TestFanotifyCrossWatcherSameFs()
    {
        if (!FanotifyBackend.Available())
            throw new OriginalSkip("fanotify not available");
        using var fixture = new Fixture(nameof(TestFanotifyCrossWatcherSameFs) + "/Modify", 1);
        string firstDir = fixture.NewTmpDir(), secondDir = fixture.NewTmpDir();
        string firstPath = Path.Combine(firstDir, "child"), secondPath = Path.Combine(secondDir, "child");
        WriteFile(firstPath, "initial");
        WriteFile(secondPath, "initial");
        var(first, _) = fixture.Subscribe(FsWatch.Fanotify(), firstDir);
        var(second, _) = fixture.Subscribe(FsWatch.Fanotify(), secondDir);
        WriteFile(firstPath, "changed");
        AssertEventSet(first.Gather(first.Deadline(), TimeSpan.FromMilliseconds(200)), [new WantEvent(EventKind.Update, Text(firstPath))]);
        Check(second.DrainQuiet(TimeSpan.FromMilliseconds(200)).Length == 0, "watcher B got phantom events");
    }

    public static void TestLinuxFanotifyMaybeWrapUnsupportedFilesystem()
    {
        foreach (int number in new[]
        {
            LinuxKernel.EOPNOTSUPP,
            LinuxKernel.ENODEV
        }

        )
        {
            var error = LinuxKernel.Error(number);
            var wrapped = FanotifyBackend.MaybeWrapUnsupportedFilesystem(new RawWatchError("name_to_handle_at: " + FsWatch.ErrorString(error), error));
            Check(FsWatch.IsError(wrapped, FsWatch.ErrFilesystemUnsupported), "unsupported errno must be tagged");
            Check(LinuxKernel.IsErrno(wrapped, number), "underlying errno must remain");
        }

        var other = FanotifyBackend.MaybeWrapUnsupportedFilesystem(LinuxKernel.Error(LinuxKernel.EACCES));
        Check(!FsWatch.IsError(other, FsWatch.ErrFilesystemUnsupported), "EACCES should not be tagged");
    }

    public static void TestLinuxFanotifyMarkENODEVTagged()
    {
        var error = FanotifyBackend.MaybeWrapUnsupportedFilesystem(LinuxKernel.Error(LinuxKernel.ENODEV));
        Check(FsWatch.IsError(error, FsWatch.ErrFilesystemUnsupported), "bare ENODEV should be tagged");
        Check(LinuxKernel.IsErrno(error, LinuxKernel.ENODEV), "tagged error must unwrap ENODEV");
    }

    public static void TestLinuxFanotifyUnsupportedTagSurvivesDirWatchError()
    {
        var errno = LinuxKernel.Error(LinuxKernel.EOPNOTSUPP);
        var inner = FanotifyBackend.MaybeWrapUnsupportedFilesystem(new RawWatchError("name_to_handle_at: " + FsWatch.ErrorString(errno), errno));
        var error = new DirWatchError(new RawWatchError("fanotify_mark on '/x' failed: " + FsWatch.ErrorString(inner), inner), null!);
        Check(FsWatch.IsError(error, FsWatch.ErrFilesystemUnsupported), "unsupported tag must survive dirWatchError");
        Check(LinuxKernel.IsErrno(error, LinuxKernel.EOPNOTSUPP), "underlying errno must survive dirWatchError");
    }
}

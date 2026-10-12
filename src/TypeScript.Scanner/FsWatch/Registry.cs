// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Pinned internal/fswatch/watcher.go:188–251, and Linux backend init functions.
using TypeScript.Scanner;
using System.Runtime.InteropServices;

namespace TypeScript.FsWatch;
internal static partial class FsWatch
{
    private static readonly Watcher inotifyWatcher = new(ByteStringValue.FromString("inotify"));
    private static readonly Watcher fseventsWatcher = new(ByteStringValue.FromString("fsevents"));
    private static readonly Watcher kqueueWatcher = new(ByteStringValue.FromString("kqueue"));
    private static readonly Watcher windowsWatcher = new(ByteStringValue.FromString("windows"));
    internal static readonly Watcher fanotifyWatcher = new(ByteStringValue.FromString("fanotify"));
    private static readonly FallbackWatcher fanotifyFallbackWatcher = new(fanotifyWatcher, inotifyWatcher);
    static FsWatch()
    {
        // Only the contracted Linux native owners are implemented. Unsupported
        // platform objects retain the original nil-factory/ErrUnavailable path.
        if (OperatingSystem.IsLinux() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            inotifyWatcher.Factory = static () => new InotifyBackend();
            if (FanotifyBackend.Available())
                fanotifyWatcher.Factory = static () => new FanotifyBackend(false);
        }
    }

    internal static Slice<IWatcher> AllWatchers() => Slice<IWatcher>.FromArray([inotifyWatcher, fseventsWatcher, kqueueWatcher, windowsWatcher, fanotifyFallbackWatcher]);
    internal static IWatcher Inotify() => inotifyWatcher;
    internal static IWatcher FSEvents() => fseventsWatcher;
    internal static IWatcher Kqueue() => kqueueWatcher;
    internal static IWatcher Windows() => windowsWatcher;
    internal static IWatcher Fanotify() => fanotifyFallbackWatcher;
    internal static IWatcher Default()
    {
        if (OperatingSystem.IsLinux())
            return Fanotify().Available() ? Fanotify() : Inotify();
        if (OperatingSystem.IsAndroid())
            return Inotify();
        if (OperatingSystem.IsMacOS())
            return FSEvents().Available() ? FSEvents() : Kqueue();
        if (OperatingSystem.IsWindows())
            return Windows();
        if (OperatingSystem.IsFreeBSD())
            return Kqueue();
        return new Watcher(ByteStringValue.FromString("unsupported"));
    }
}

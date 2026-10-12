// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Pinned internal/fswatch/watcher.go:17–186; licenses/FsWatch-LICENSE.txt (MIT).
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal delegate void WatchCallback(Slice<WatchEvent> events, Exception? error);
internal interface IWatcher
{
    ByteStringValue Name();
    bool Available();
    bool HasFastRecursiveBackend();
    (IWatch? Watch, Exception? Error) WatchDirectory(ByteStringValue directory, WatchCallback callback, Slice<IWatchOption> options = default);
    (Slice<IWatch> Watches, Exception? Error) WatchDirectories(Slice<WatchDirectoryRequest> requests);
    (IWatch? Watch, Exception? Error) WatchFile(ByteStringValue path, WatchCallback callback);
}

internal interface IWatch
{
    Exception? Close();
}

internal interface IWatchOption
{
    void ApplyWatchOption(ref WatchOptions options);
}

internal struct WatchDirectoryRequest
{
    internal ByteStringValue Dir;
    internal WatchCallback Callback;
    internal Slice<IWatchOption> Options;
}

internal struct WatchOptions
{
    internal Func<ByteStringValue, bool>? Ignore;
    internal bool Recursive;
    internal ByteStringValue File;
}

internal readonly struct FileOption(ByteStringValue path) : IWatchOption
{
    public void ApplyWatchOption(ref WatchOptions options) => options.File = path;
}

internal readonly struct IgnoreOption(Func<ByteStringValue, bool> ignore) : IWatchOption
{
    public void ApplyWatchOption(ref WatchOptions options) => options.Ignore = ignore;
}

internal readonly struct RecursiveOption : IWatchOption
{
    public void ApplyWatchOption(ref WatchOptions options) => options.Recursive = true;
}

internal static partial class FsWatch
{
    internal static readonly Exception ErrNilCallback = new IOException("fswatch: callback must not be nil");
    internal static readonly Exception ErrRootPath = new IOException("fswatch: cannot watch a root path");
    internal static readonly Exception ErrNotAbsolute = new IOException("fswatch: path must be absolute");
    internal static readonly Exception ErrOverflow = new IOException("fswatch: event overflow; some changes were missed");
    internal static readonly Exception ErrWatchTerminated = new IOException("fswatch: watch terminated");
    internal static readonly Exception ErrUnavailable = new IOException("fswatch: watcher not available on this platform");
    internal static readonly Exception ErrFilesystemUnsupported = new IOException("fswatch: watcher backend unsupported on this filesystem");
    internal static IWatchOption WithIgnore(Func<ByteStringValue, bool> ignore) => new IgnoreOption(ignore);
    internal static IWatchOption WithRecursive() => new RecursiveOption();
}

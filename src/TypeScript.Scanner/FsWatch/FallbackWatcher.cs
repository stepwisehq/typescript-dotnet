// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Complete internal/fswatch/watcher.go:253–308 filesystem fallback owner.
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal sealed class FallbackWatcher(IWatcher primary, IWatcher secondary) : IWatcher
{
    // Original same-package fallback_test.go identity assertions observe these
    // retained backend pointers through the existing friend verification assembly.
    internal IWatcher Primary => primary;
    internal IWatcher Secondary => secondary;

    public ByteStringValue Name() => primary.Name();
    public bool Available() => primary.Available();
    public bool HasFastRecursiveBackend() => primary.HasFastRecursiveBackend();
    public (IWatch? Watch, Exception? Error) WatchDirectory(ByteStringValue directory, WatchCallback callback, Slice<IWatchOption> options = default)
    {
        var(watches, error) = WatchDirectories(Slice<WatchDirectoryRequest>.FromArray([new() { Dir = directory, Callback = callback, Options = options }]));
        return error is not null ? (null, error) : (watches[0], null);
    }

    public (Slice<IWatch> Watches, Exception? Error) WatchDirectories(Slice<WatchDirectoryRequest> requests)
    {
        var(watches, error) = primary.WatchDirectories(requests);
        if (error is null || !FsWatch.IsError(error, FsWatch.ErrFilesystemUnsupported))
            return (watches, error);
        watches = Slice<IWatch>.Make(0, requests.Count);
        foreach (var request in requests)
        {
            var(watch, watchError) = primary.WatchDirectory(request.Dir, request.Callback, request.Options);
            if (FsWatch.IsError(watchError, FsWatch.ErrFilesystemUnsupported))
                (watch, watchError) = secondary.WatchDirectory(request.Dir, request.Callback, request.Options);
            if (watchError is not null)
            {
                for (var i = watches.Count - 1; i >= 0; i--)
                    _ = watches[i].Close();
                return (default, new RawWatchError("fswatch: failed to watch directory " + GoQuote.Quote(request.Dir.Materialize()!) + ": " + FsWatch.ErrorString(watchError), watchError));
            }

            watches = watches.Append(watch!);
        }

        return (watches, null);
    }

    public (IWatch? Watch, Exception? Error) WatchFile(ByteStringValue path, WatchCallback callback)
    {
        var result = primary.WatchFile(path, callback);
        return FsWatch.IsError(result.Error, FsWatch.ErrFilesystemUnsupported) ? secondary.WatchFile(path, callback) : result;
    }
}

internal sealed class WatchMarkerError(Exception marker, Exception error) : RawWatchError(FsWatch.ErrorString(marker) + ": " + FsWatch.ErrorString(error), error)
{
    internal readonly Exception Marker = marker;
}

internal static partial class FsWatch
{
    internal static bool IsError(Exception? error, Exception marker)
    {
        for (; error is not null; error = error.InnerException)
            if (ReferenceEquals(error, marker) || error is WatchMarkerError joined && ReferenceEquals(joined.Marker, marker) || error is WatchTerminatedError terminated && ReferenceEquals(terminated.Marker, marker) || error is UnsupportedFilesystemError && ReferenceEquals(marker, ErrFilesystemUnsupported))
                return true;
        return false;
    }
}

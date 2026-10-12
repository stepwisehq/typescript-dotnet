// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Pinned internal/fswatch/watcher.go:638–778; upstream MIT license.
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal interface IWatcherImpl
{
    Exception? Start();
    Exception? Run();
    void Shutdown();
    Exception? WatchAdd(DirWatch watch);
    Exception? WatchAddMany(Slice<DirWatch> watches);
    void WatchRemove(DirWatch watch);
    void HandleWatcherError(DirWatchError error);
    Exception? Subscribe(DirWatch watch);
    Exception? CloseWatch(DirWatch watch);
}

internal interface IWatchBatchSubscriber
{
    Exception? SubscribeMany(Slice<DirWatch> watches);
}

internal abstract class WatcherBase : IWatcherImpl
{
    protected readonly object Mu = new();
    private readonly HashSet<DirWatch> subscriptions = new(ReferenceEqualityComparer.Instance);
    private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Exception? startError;
    protected void NotifyStarted() => started.TrySetResult();
    public virtual void Shutdown()
    {
    }

    public abstract Exception? Start();
    public abstract Exception? Subscribe(DirWatch watch);
    public abstract Exception? CloseWatch(DirWatch watch);
    public Exception? Run()
    {
        // Embedded virtual dispatch is this owner; no independent base mutex or
        // subscription state is introduced by the Linux derived backend.
        new Thread(() =>
        {
            try
            {
                if (Start()is { } error)
                    HandleStartError(error);
            }
            catch (Exception panic)
            {
                HandleStartError(panic);
            }
        })
        {
            IsBackground = true
        }.Start();
        started.Task.GetAwaiter().GetResult();
        lock (Mu)
            return startError;
    }

    private void HandleStartError(Exception error)
    {
        Slice<DirWatch> copy;
        lock (Mu)
        {
            startError = error;
            copy = Slice<DirWatch>.Make(0, subscriptions.Count);
            foreach (var watch in subscriptions)
                copy = copy.Append(watch);
        }

        foreach (var watch in copy)
            watch.NotifyError(error);
        NotifyStarted();
    }

    public Exception? WatchAdd(DirWatch watch) => WatchAddMany(Slice<DirWatch>.FromArray([watch]));
    public Exception? WatchAddMany(Slice<DirWatch> watches)
    {
        lock (Mu)
        {
            var toAdd = Slice<DirWatch>.Make(0, watches.Count);
            foreach (var watch in watches)
                if (!subscriptions.Contains(watch))
                    toAdd = toAdd.Append(watch);
            if (toAdd.Count == 0)
                return null;
            if (this is IWatchBatchSubscriber subscriber)
            {
                if (subscriber.SubscribeMany(toAdd)is { } error)
                    return error;
                foreach (var watch in toAdd)
                    subscriptions.Add(watch);
                return null;
            }

            var added = Slice<DirWatch>.Make(0, toAdd.Count);
            foreach (var watch in toAdd)
            {
                if (Subscribe(watch)is { } error)
                {
                    foreach (var addedWatch in added)
                    {
                        subscriptions.Remove(addedWatch);
                        _ = CloseWatch(addedWatch);
                    }

                    return error;
                }

                subscriptions.Add(watch);
                added = added.Append(watch);
            }

            return null;
        }
    }

    public void WatchRemove(DirWatch watch)
    {
        lock (Mu)
        {
            if (!subscriptions.Remove(watch))
                return;
            _ = CloseWatch(watch);
        }
    }

    public void HandleWatcherError(DirWatchError error)
    {
        WatchRemove(error.Watch);
        error.Watch.NotifyError(new WatchTerminatedError(error));
    }
}

internal sealed class DirWatchError(Exception error, DirWatch watch) : RawWatchError(FsWatch.ErrorString(error), error)
{
    internal readonly DirWatch Watch = watch;
}

internal sealed class WatchTerminatedError(Exception error) : RawWatchError(FsWatch.ErrorString(FsWatch.ErrWatchTerminated) + ": " + FsWatch.ErrorString(error), error)
{
    internal readonly Exception Marker = FsWatch.ErrWatchTerminated;
}

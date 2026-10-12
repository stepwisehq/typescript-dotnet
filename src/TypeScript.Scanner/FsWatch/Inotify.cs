// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Complete pinned internal/fswatch/inotify_linux.go backend; MIT license.
using System.Buffers.Binary;
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal sealed class InotifyBackend : WatcherBase
{
    private const uint InotifyMask = LinuxKernel.IN_CREATE | LinuxKernel.IN_DELETE | LinuxKernel.IN_DELETE_SELF | LinuxKernel.IN_MODIFY | LinuxKernel.IN_MOVE_SELF | LinuxKernel.IN_MOVED_FROM | LinuxKernel.IN_MOVED_TO | LinuxKernel.IN_DONT_FOLLOW | LinuxKernel.IN_ONLYDIR | LinuxKernel.IN_EXCL_UNLINK;
    private sealed class Subscription(ByteStringValue path, ByteStringValue watchPath, DirWatch watch, int descriptor)
    {
        internal readonly ByteStringValue Path = path, WatchPath = watchPath;
        internal readonly DirWatch Watch = watch;
        internal readonly int Descriptor = descriptor;
    }

    private readonly record struct InotifyEvent(int Descriptor, uint Mask, uint Cookie, uint Length);
    private readonly int[] pipeFDs = [-1, -1];
    private int pipeWriteFD = -1;
    private int inotify = -1;
    private readonly Dictionary<int, Slice<Subscription>> subscriptions = new();
    private readonly TaskCompletionSource ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly byte[] readBuffer = new byte[8192];
    private readonly HashSet<DirWatch> touched = new(ReferenceEqualityComparer.Instance);
    public override Exception? Start()
    {
        if (LinuxKernel.Pipe2(pipeFDs, LinuxKernel.O_CLOEXEC | LinuxKernel.O_NONBLOCK)is { } pipeError)
            return new RawWatchError("unable to open pipe: " + FsWatch.ErrorString(pipeError), pipeError);
        Volatile.Write(ref pipeWriteFD, pipeFDs[1]);
        try
        {
            var(fd, initError) = LinuxKernel.InotifyInit1(LinuxKernel.O_NONBLOCK | LinuxKernel.O_CLOEXEC);
            if (initError is not null)
                return new RawWatchError("unable to initialize inotify: " + FsWatch.ErrorString(initError), initError);
            inotify = fd;
            LinuxKernel.PollFD[] pollFDs = [new(pipeFDs[0], LinuxKernel.POLLIN), new(inotify, LinuxKernel.POLLIN)];
            NotifyStarted();
            while (true)
            {
                if (LinuxKernel.Poll(pollFDs, 500)is { } pollError)
                {
                    if (LinuxKernel.IsErrno(pollError, LinuxKernel.EINTR))
                        continue;
                    return new RawWatchError("unable to poll: " + FsWatch.ErrorString(pollError), pollError);
                }

                if (pollFDs[0].ReturnedEvents != 0)
                    break;
                if (pollFDs[1].ReturnedEvents != 0 && HandleEvents()is { } readError)
                    return readError;
            }

            return null;
        }
        finally
        {
            CloseFDs();
            ended.TrySetResult();
        }
    }

    private void CloseFDs()
    {
        lock (Mu)
        {
            if (pipeFDs[0] >= 0)
            {
                _ = LinuxKernel.Close(pipeFDs[0]);
                pipeFDs[0] = -1;
            }

            int fd = Interlocked.Exchange(ref pipeWriteFD, -1);
            if (fd >= 0)
                _ = LinuxKernel.Close(fd);
            pipeFDs[1] = -1;
            if (inotify >= 0)
            {
                _ = LinuxKernel.Close(inotify);
                inotify = -1;
            }
        }
    }

    public override void Shutdown()
    {
        int fd = Volatile.Read(ref pipeWriteFD);
        if (fd < 0)
            return;
        _ = LinuxKernel.Write(fd, "X"u8);
        ended.Task.GetAwaiter().GetResult();
    }

    public override Exception? Subscribe(DirWatch watch)
    {
        if (!watch.Recursive)
        {
            var(_, error) = WatchDir(watch, watch.Dir, watch.PhysicalDir);
            return error is null ? null : WatchError(watch, watch.Dir, error);
        }

        Exception? walkError = FsWatch.WalkDir(watch.PhysicalDir, true, (path, isDirectory) =>
        {
            if (!isDirectory)
                return null;
            var displayPath = watch.DisplayPath(path);
            var(_, error) = WatchDir(watch, displayPath, path);
            return error is null ? null : WatchError(watch, displayPath, error);
        });
        if (walkError is not null)
            _ = CloseWatch(watch);
        return walkError;
    }

    private static DirWatchError WatchError(DirWatch watch, ByteStringValue path, Exception error) => new(new RawWatchError("inotify_add_watch on '" + path.Materialize()! + "' failed: " + FsWatch.ErrorString(error), error), watch);
    private (int Descriptor, Exception? Error) WatchDir(DirWatch watch, ByteStringValue path, ByteStringValue watchPath)
    {
        var(descriptor, error) = LinuxKernel.InotifyAddWatch(inotify, watchPath, InotifyMask);
        if (error is not null)
            return (0, error);
        var subscription = new Subscription(path, watchPath, watch, descriptor);
        subscriptions[descriptor] = subscriptions.GetValueOrDefault(descriptor).Append(subscription);
        return (descriptor, null);
    }

    private Exception? HandleEvents()
    {
        while (true)
        {
            var(length, error) = LinuxKernel.Read(inotify, readBuffer);
            if (error is not null)
            {
                if (LinuxKernel.IsErrno(error, LinuxKernel.EAGAIN))
                    break;
                return new RawWatchError("Error reading from inotify: " + FsWatch.ErrorString(error), error);
            }

            if (length == 0)
                break;
            for (int offset = 0; offset < length;)
            {
                var bytes = readBuffer.AsSpan(offset);
                var value = new InotifyEvent(BinaryPrimitives.ReadInt32LittleEndian(bytes), BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4)), BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(8)), BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(12)));
                int recordSize = 16 + checked((int)value.Length);
                var name = FsWatch.Empty;
                if (value.Length > 0)
                {
                    var nameBytes = bytes.Slice(16, recordSize - 16);
                    int terminator = nameBytes.IndexOf((byte)0);
                    if (terminator >= 0)
                        nameBytes = nameBytes.Slice(0, terminator);
                    name = ByteStringValue.FromView(ByteStringView.CopyFrom(nameBytes));
                }

                if ((value.Mask & LinuxKernel.IN_Q_OVERFLOW) != 0)
                {
                    lock (Mu)
                        foreach (var list in subscriptions.Values)
                            foreach (var subscription in list)
                            {
                                subscription.Watch.Events.SetError(FsWatch.ErrOverflow);
                                touched.Add(subscription.Watch);
                            }

                    offset += recordSize;
                    continue;
                }

                HandleEvent(value, name);
                offset += recordSize;
            }
        }

        foreach (var watch in touched)
            watch.Notify();
        touched.Clear();
        return null;
    }

    private void HandleEvent(InotifyEvent value, ByteStringValue name)
    {
        lock (Mu)
            foreach (var subscription in subscriptions.GetValueOrDefault(value.Descriptor))
                if (HandleSubscription(value, name, subscription))
                    touched.Add(subscription.Watch);
    }

    private bool HandleSubscription(InotifyEvent value, ByteStringValue name, Subscription subscription)
    {
        var watch = subscription.Watch;
        var path = subscription.Path;
        var watchPath = subscription.WatchPath;
        bool isDirectory = (value.Mask & LinuxKernel.IN_ISDIR) != 0;
        if (name.Length != 0)
        {
            path = ByteStringValue.Concat(path, ByteStringValue.FromString("/"), name);
            watchPath = ByteStringValue.Concat(watchPath, ByteStringValue.FromString("/"), name);
        }

        if ((value.Mask & (LinuxKernel.IN_CREATE | LinuxKernel.IN_MOVED_TO)) != 0)
        {
            watch.Events.Create(path);
            if (isDirectory && watch.Recursive)
                _ = FsWatch.WalkDir(watchPath, true, (child, childIsDirectory) =>
                {
                    if (childIsDirectory)
                        _ = WatchDir(watch, watch.DisplayPath(child), child);
                    return null;
                });
        }
        else if ((value.Mask & LinuxKernel.IN_MODIFY) != 0)
            watch.Events.Update(path);
        else if ((value.Mask & (LinuxKernel.IN_DELETE | LinuxKernel.IN_DELETE_SELF | LinuxKernel.IN_MOVED_FROM | LinuxKernel.IN_MOVE_SELF)) != 0)
        {
            bool self = (value.Mask & (LinuxKernel.IN_DELETE_SELF | LinuxKernel.IN_MOVE_SELF)) != 0;
            if (self && !path.EqualsValue(watch.Dir))
                return false;
            if (self || isDirectory)
                foreach (var pair in subscriptions)
                {
                    var kept = pair.Value.Slice2(0, 0);
                    foreach (var item in pair.Value)
                    {
                        if (item.Path.EqualsValue(path) || FsWatch.IsInDirectoryOrSelf(path, item.Path))
                            continue;
                        kept = kept.Append(item);
                    }

                    if (kept.Count == 0)
                    {
                        _ = LinuxKernel.InotifyRemoveWatch(inotify, unchecked((uint)pair.Key));
                        subscriptions.Remove(pair.Key);
                    }
                    else
                        subscriptions[pair.Key] = kept;
                }

            watch.Events.Remove(path);
            if (self && path.EqualsValue(watch.Dir))
                watch.Events.SetError(new WatchMarkerError(FsWatch.ErrWatchTerminated, new RawWatchError("watched directory removed")));
        }

        return true;
    }

    public override Exception? CloseWatch(DirWatch watch)
    {
        Exception? firstError = null;
        foreach (var pair in subscriptions)
        {
            var kept = pair.Value.Slice2(0, 0);
            bool removedAny = false;
            foreach (var subscription in pair.Value)
            {
                if (ReferenceEquals(subscription.Watch, watch))
                {
                    removedAny = true;
                    continue;
                }

                kept = kept.Append(subscription);
            }

            if (!removedAny)
                continue;
            if (kept.Count == 0)
            {
                var error = LinuxKernel.InotifyRemoveWatch(inotify, unchecked((uint)pair.Key));
                if (error is not null && firstError is null)
                    firstError = new DirWatchError(new RawWatchError("unable to remove dirWatch: " + FsWatch.ErrorString(error), error), watch);
                subscriptions.Remove(pair.Key);
            }
            else
                subscriptions[pair.Key] = kept;
        }

        return firstError;
    }
}

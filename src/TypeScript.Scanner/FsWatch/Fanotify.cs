// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Complete pinned internal/fswatch/fanotify_linux.go backend; MIT license.
using System.Buffers.Binary;
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal sealed partial class FanotifyBackend(bool noRename) : WatcherBase
{
    private const uint InitFlags = LinuxKernel.FAN_CLASS_NOTIF | LinuxKernel.FAN_CLOEXEC | LinuxKernel.FAN_NONBLOCK | LinuxKernel.FAN_REPORT_FID | LinuxKernel.FAN_REPORT_DFID_NAME;
    private const ulong MarkMaskBase = LinuxKernel.FAN_CREATE | LinuxKernel.FAN_DELETE | LinuxKernel.FAN_MODIFY | LinuxKernel.FAN_DELETE_SELF | LinuxKernel.FAN_MOVE_SELF | LinuxKernel.FAN_ONDIR | LinuxKernel.FAN_EVENT_ON_CHILD;
    private const ulong MarkMaskRename = MarkMaskBase | LinuxKernel.FAN_RENAME;
    private const ulong MarkMaskMoved = MarkMaskBase | LinuxKernel.FAN_MOVED_FROM | LinuxKernel.FAN_MOVED_TO;
    private const uint MarkAddFlags = LinuxKernel.FAN_MARK_ADD | LinuxKernel.FAN_MARK_ONLYDIR | LinuxKernel.FAN_MARK_DONT_FOLLOW;
    private sealed class Subscription(ByteStringValue path, ByteStringValue watchPath, DirWatch watch, FanotifyHandleKey key)
    {
        internal readonly ByteStringValue Path = path, WatchPath = watchPath;
        internal readonly DirWatch Watch = watch;
        internal readonly FanotifyHandleKey Key = key;
    }

    private readonly int[] pipeFDs = [-1, -1];
    private int pipeWriteFD = -1;
    private int fanotifyFD = -1;
    private ulong markMask;
    private readonly Dictionary<FanotifyHandleKey, Slice<Subscription>> subscriptions = new();
    // Original same-package failed-subscribe assertion inspects this existing map.
    internal int SubscriptionCountForOriginalAssertion
    {
        get
        {
            lock (Mu)
                return subscriptions.Count;
        }
    }

    private readonly TaskCompletionSource ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly byte[] readBuffer = new byte[8192];
    private readonly HashSet<DirWatch> touched = new(ReferenceEqualityComparer.Instance);
    internal static bool Available()
    {
        if (OperatingSystem.IsAndroid())
            return false;
        var(fd, error) = LinuxKernel.FanotifyInit(InitFlags, LinuxKernel.O_CLOEXEC);
        if (error is not null)
            return false;
        _ = LinuxKernel.Close(fd);
        return true;
    }

    public override Exception? Start()
    {
        if (LinuxKernel.Pipe2(pipeFDs, LinuxKernel.O_CLOEXEC | LinuxKernel.O_NONBLOCK)is { } pipeError)
            return new RawWatchError("unable to open pipe: " + FsWatch.ErrorString(pipeError), pipeError);
        Volatile.Write(ref pipeWriteFD, pipeFDs[1]);
        try
        {
            var(fd, initError) = LinuxKernel.FanotifyInit(InitFlags, LinuxKernel.O_CLOEXEC);
            if (initError is not null)
                return new RawWatchError("unable to initialize fanotify: " + FsWatch.ErrorString(initError), initError);
            fanotifyFD = fd;
            LinuxKernel.PollFD[] pollFDs = [new(pipeFDs[0], LinuxKernel.POLLIN), new(fanotifyFD, LinuxKernel.POLLIN)];
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
            if (fanotifyFD >= 0)
            {
                _ = LinuxKernel.Close(fanotifyFD);
                fanotifyFD = -1;
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
        if (markMask == 0)
        {
            if (noRename)
                markMask = MarkMaskMoved;
            else
            {
                markMask = MarkMaskRename;
                var error = LinuxKernel.FanotifyMark(fanotifyFD, MarkAddFlags, MarkMaskRename, -100, watch.PhysicalDir);
                if (error is null)
                {
                    while (true)
                    {
                        var removeError = LinuxKernel.FanotifyMark(fanotifyFD, LinuxKernel.FAN_MARK_REMOVE | LinuxKernel.FAN_MARK_ONLYDIR, MarkMaskRename, -100, watch.PhysicalDir);
                        if (removeError is null || !LinuxKernel.IsErrno(removeError, LinuxKernel.EINTR))
                            break;
                    }
                }
                else if (LinuxKernel.IsErrno(error, LinuxKernel.EINVAL) || LinuxKernel.IsErrno(error, LinuxKernel.EOPNOTSUPP))
                    markMask = MarkMaskMoved;
            }
        }

        if (!watch.Recursive)
        {
            var error = MarkDir(watch, watch.Dir, watch.PhysicalDir);
            return error is null ? null : MarkError(watch, watch.Dir, error);
        }

        var walkError = FsWatch.WalkDir(watch.PhysicalDir, true, (path, isDirectory) =>
        {
            if (!isDirectory)
                return null;
            var display = watch.DisplayPath(path);
            var error = MarkDir(watch, display, path);
            return error is null ? null : MarkError(watch, display, error);
        });
        if (walkError is not null)
            _ = CloseWatch(watch);
        return walkError;
    }

    private static DirWatchError MarkError(DirWatch watch, ByteStringValue path, Exception error) => new(new RawWatchError("fanotify_mark on '" + path.Materialize()! + "' failed: " + FsWatch.ErrorString(error), error), watch);
    private Exception? MarkDir(DirWatch watch, ByteStringValue path, ByteStringValue markPath)
    {
        if (LinuxKernel.FanotifyMark(fanotifyFD, MarkAddFlags, markMask, -100, markPath)is { } markError)
            return MaybeWrapUnsupportedFilesystem(markError);
        var(handle, handleType, handleError) = LinuxKernel.NameToHandleAt(-100, markPath, 0);
        if (handleError is not null)
        {
            _ = LinuxKernel.FanotifyMark(fanotifyFD, LinuxKernel.FAN_MARK_REMOVE | LinuxKernel.FAN_MARK_ONLYDIR, markMask, -100, markPath);
            return MaybeWrapUnsupportedFilesystem(new RawWatchError("name_to_handle_at: " + FsWatch.ErrorString(handleError), handleError));
        }

        var(fsid0, fsid1, statError) = LinuxKernel.StatFs(markPath);
        if (statError is not null)
        {
            _ = LinuxKernel.FanotifyMark(fanotifyFD, LinuxKernel.FAN_MARK_REMOVE | LinuxKernel.FAN_MARK_ONLYDIR, markMask, -100, markPath);
            return new RawWatchError("statfs: " + FsWatch.ErrorString(statError), statError);
        }

        var key = FanotifyHandleKey.Make(fsid0, fsid1, handleType, handle.AsSpan());
        subscriptions[key] = subscriptions.GetValueOrDefault(key).Append(new Subscription(path, markPath, watch, key));
        return null;
    }

    internal static Exception MaybeWrapUnsupportedFilesystem(Exception error) => LinuxKernel.IsErrno(error, LinuxKernel.EOPNOTSUPP) || LinuxKernel.IsErrno(error, LinuxKernel.ENODEV) ? new UnsupportedFilesystemError(error) : error;
    private Exception? HandleEvents()
    {
        while (true)
        {
            var(length, error) = LinuxKernel.Read(fanotifyFD, readBuffer);
            if (error is not null)
            {
                if (LinuxKernel.IsErrno(error, LinuxKernel.EAGAIN))
                    break;
                return new RawWatchError("Error reading from fanotify: " + FsWatch.ErrorString(error), error);
            }

            if (length == 0)
                break;
            var data = readBuffer.AsSpan(0, length);
            while (data.Length >= 24)
            {
                uint eventLength = BinaryPrimitives.ReadUInt32LittleEndian(data);
                byte version = data[4];
                int metadataLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6));
                ulong mask = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(8));
                int fd = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(16));
                if (version != LinuxKernel.FANOTIFY_METADATA_VERSION)
                    return new RawWatchError("unsupported fanotify metadata version: " + version.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (eventLength < metadataLength || eventLength > data.Length)
                    break;
                if (fd >= 0)
                    _ = LinuxKernel.Close(fd);
                if ((mask & LinuxKernel.FAN_Q_OVERFLOW) != 0)
                {
                    HandleOverflow();
                    data = data.Slice((int)eventLength);
                    continue;
                }

                var(primary, rename) = ParseDfidNames(data.Slice(metadataLength, (int)eventLength - metadataLength));
                if ((mask & LinuxKernel.FAN_RENAME) != 0)
                {
                    if (primary is not null || rename is not null)
                        HandleRenameEvent(mask, primary, rename);
                }
                else if (primary is not null)
                    HandleParsedEvent(mask, primary);
                data = data.Slice((int)eventLength);
            }
        }

        foreach (var watch in touched)
            watch.Notify();
        touched.Clear();
        return null;
    }

    private void HandleOverflow()
    {
        lock (Mu)
        {
            var seen = new HashSet<DirWatch>(ReferenceEqualityComparer.Instance);
            foreach (var list in subscriptions.Values)
                foreach (var subscription in list)
                {
                    if (!seen.Add(subscription.Watch))
                        continue;
                    subscription.Watch.Events.SetError(FsWatch.ErrOverflow);
                    touched.Add(subscription.Watch);
                }
        }
    }

    private void HandleRenameEvent(ulong mask, FanotifyDfidName? oldName, FanotifyDfidName? newName)
    {
        lock (Mu)
        {
            bool isDirectory = (mask & LinuxKernel.FAN_ONDIR) != 0;
            if (oldName is not null && oldName.Name.Length != 0 && !oldName.Name.EqualsBytes("."u8))
                foreach (var subscription in subscriptions.GetValueOrDefault(oldName.Key))
                {
                    var path = ByteStringValue.Concat(subscription.Path, ByteStringValue.FromString("/"), oldName.Name);
                    if (isDirectory)
                        DropSubsForPathAndDescendantsLocked(path);
                    subscription.Watch.Events.Remove(path);
                    touched.Add(subscription.Watch);
                }

            if (newName is not null && newName.Name.Length != 0 && !newName.Name.EqualsBytes("."u8))
                foreach (var subscription in subscriptions.GetValueOrDefault(newName.Key))
                {
                    var watch = subscription.Watch;
                    var path = ByteStringValue.Concat(subscription.Path, ByteStringValue.FromString("/"), newName.Name);
                    watch.Events.Create(path);
                    if (isDirectory && watch.Recursive)
                        _ = FsWatch.WalkDir(watch.PhysicalPath(path), true, (child, childIsDirectory) =>
                        {
                            if (childIsDirectory)
                                _ = MarkDir(watch, watch.DisplayPath(child), child);
                            return null;
                        });
                    touched.Add(watch);
                }
        }
    }

    private void HandleParsedEvent(ulong mask, FanotifyDfidName value)
    {
        lock (Mu)
            foreach (var subscription in subscriptions.GetValueOrDefault(value.Key))
                if (HandleSubscription(mask, value, subscription))
                    touched.Add(subscription.Watch);
    }

    private bool HandleSubscription(ulong mask, FanotifyDfidName value, Subscription subscription)
    {
        var watch = subscription.Watch;
        bool selfEvent = value.Name.Length == 0 || value.Name.EqualsBytes("."u8);
        var path = selfEvent ? subscription.Path : ByteStringValue.Concat(subscription.Path, ByteStringValue.FromString("/"), value.Name);
        bool isDirectory = (mask & LinuxKernel.FAN_ONDIR) != 0;
        bool wasTouched = false;
        bool hasDelete = (mask & (LinuxKernel.FAN_DELETE | LinuxKernel.FAN_MOVED_FROM)) != 0;
        bool hasCreate = (mask & (LinuxKernel.FAN_CREATE | LinuxKernel.FAN_MOVED_TO)) != 0;
        if (hasCreate && hasDelete && !selfEvent)
        {
            if (LinuxKernel.StatMode(path, followLinks: false).Error is not null)
            {
                watch.Events.Create(path);
                watch.Events.Remove(path);
                return true;
            }
        }

        if ((mask & (LinuxKernel.FAN_DELETE | LinuxKernel.FAN_DELETE_SELF | LinuxKernel.FAN_MOVED_FROM | LinuxKernel.FAN_MOVE_SELF)) != 0)
        {
            bool selfMask = (mask & (LinuxKernel.FAN_DELETE_SELF | LinuxKernel.FAN_MOVE_SELF)) != 0;
            if (!(selfMask && !path.EqualsValue(watch.Dir)))
            {
                if (selfMask || isDirectory)
                    DropSubsForPathAndDescendantsLocked(path);
                else
                    DropSubsForPathLocked(path);
                watch.Events.Remove(path);
                wasTouched = true;
                if (selfMask && path.EqualsValue(watch.Dir))
                    watch.Events.SetError(new WatchMarkerError(FsWatch.ErrWatchTerminated, new RawWatchError("watched directory removed")));
            }
        }

        if (hasCreate)
        {
            watch.Events.Create(path);
            if (isDirectory && watch.Recursive)
                _ = FsWatch.WalkDir(watch.PhysicalPath(path), true, (child, childIsDirectory) =>
                {
                    if (childIsDirectory)
                        _ = MarkDir(watch, watch.DisplayPath(child), child);
                    return null;
                });
            wasTouched = true;
        }

        if ((mask & LinuxKernel.FAN_MODIFY) != 0)
        {
            watch.Events.Update(path);
            wasTouched = true;
        }

        return wasTouched;
    }

    private void DropSubsForPathLocked(ByteStringValue path)
    {
        foreach (var pair in subscriptions)
        {
            var kept = pair.Value.Slice2(0, 0);
            foreach (var subscription in pair.Value)
                if (!subscription.Path.EqualsValue(path))
                    kept = kept.Append(subscription);
            if (kept.Count == 0)
                subscriptions.Remove(pair.Key);
            else
                subscriptions[pair.Key] = kept;
        }
    }

    private void DropSubsForPathAndDescendantsLocked(ByteStringValue path)
    {
        foreach (var pair in subscriptions)
        {
            var kept = pair.Value.Slice2(0, 0);
            foreach (var subscription in pair.Value)
                if (!FsWatch.IsInDirectoryOrSelf(path, subscription.Path))
                    kept = kept.Append(subscription);
            if (kept.Count == 0)
                subscriptions.Remove(pair.Key);
            else
                subscriptions[pair.Key] = kept;
        }
    }

    public override Exception? CloseWatch(DirWatch watch)
    {
        foreach (var pair in subscriptions)
        {
            var kept = pair.Value.Slice2(0, 0);
            bool removedAny = false;
            var removedPath = FsWatch.Empty;
            foreach (var subscription in pair.Value)
            {
                if (ReferenceEquals(subscription.Watch, watch))
                {
                    removedAny = true;
                    removedPath = subscription.WatchPath;
                    continue;
                }

                kept = kept.Append(subscription);
            }

            if (!removedAny)
                continue;
            if (kept.Count == 0)
            {
                if (markMask != 0)
                    _ = LinuxKernel.FanotifyMark(fanotifyFD, LinuxKernel.FAN_MARK_REMOVE, markMask, -100, removedPath);
                subscriptions.Remove(pair.Key);
            }
            else
                subscriptions[pair.Key] = kept;
        }

        return null;
    }
}

internal sealed class UnsupportedFilesystemError(Exception error) : RawWatchError(FsWatch.ErrorString(error) + ": " + FsWatch.ErrorString(FsWatch.ErrFilesystemUnsupported), error);

// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Complete internal/fswatch/walkdir.go. Required ReadDir boundary follows Go1.27.1
// os/dir_unix.go and file_unix.go (BSD license), raw Linux names retained.
using System.Buffers;
using System.Buffers.Binary;
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal static partial class FsWatch
{
    internal static Exception? WalkDirGeneric(ByteStringValue directory, bool recursive, Func<ByteStringValue, bool, Exception?>? visit)
    {
        var(mode, error) = StatPath(directory, followLinks: false);
        if (error is not null)
            return error;
        if ((mode & LinuxKernel.S_IFMT) != LinuxKernel.S_IFDIR)
            return LinuxKernel.Error(LinuxKernel.ENOTDIR);
        return WalkDirGenericVisit(directory, recursive, visit);
    }

    private static Exception? WalkDirGenericVisit(ByteStringValue directory, bool recursive, Func<ByteStringValue, bool, Exception?>? visit)
    {
        var(entries, error) = ReadGenericDirectory(directory);
        if (error is not null)
        {
            if (LinuxKernel.IsErrno(error, LinuxKernel.EACCES) || LinuxKernel.IsErrno(error, LinuxKernel.ENOENT))
                return null;
            return error;
        }

        if (visit?.Invoke(directory, true)is { } directoryError)
            return directoryError;
        foreach (var entry in entries)
        {
            var path = ByteStringValue.Concat(directory, ByteStringValue.FromString("/"), entry.Name);
            if (entry.Type == LinuxKernel.DT_DIR)
            {
                if (recursive)
                {
                    if (WalkDirGenericVisit(path, true, visit)is { } childError)
                        return childError;
                }
                else if (visit?.Invoke(path, true)is { } directChildError)
                    return directChildError;
            }
            else if (visit?.Invoke(path, false)is { } fileError)
                return fileError;
        }

        return null;
    }

    private static (uint Mode, Exception? Error) StatPath(ByteStringValue path, bool followLinks)
    {
        var result = LinuxKernel.StatMode(path, followLinks);
        while (LinuxKernel.IsErrno(result.Error, LinuxKernel.EINTR))
            result = LinuxKernel.StatMode(path, followLinks);
        return result.Error is null ? result : (0, new LinuxPathError(followLinks ? "stat" : "lstat", path, result.Error));
    }

    private static (Slice<UnixDirent> Entries, Exception? Error) ReadGenericDirectory(ByteStringValue directory)
    {
        (int fd, Exception? openError) = LinuxKernel.Open(directory, LinuxKernel.O_CLOEXEC, 0);
        while (LinuxKernel.IsErrno(openError, LinuxKernel.EINTR))
            (fd, openError) = LinuxKernel.Open(directory, LinuxKernel.O_CLOEXEC, 0);
        if (openError is not null)
            return (default, new LinuxPathError("open", directory, openError));
        // Go os.dirBufPool keeps its buffer until EOF or file.Close.
        byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
        Slice<UnixDirent> entries = default;
        try
        {
            while (true)
            {
                (int length, Exception? readError) = LinuxKernel.ReadDirent(fd, buffer.AsSpan(0, 8192));
                while (LinuxKernel.IsErrno(readError, LinuxKernel.EINTR))
                    (length, readError) = LinuxKernel.ReadDirent(fd, buffer.AsSpan(0, 8192));
                if (readError is not null)
                    return (entries, new LinuxPathError("readdirent", directory, readError));
                if (length <= 0)
                    break;
                var data = buffer.AsSpan(0, length);
                while (!data.IsEmpty)
                {
                    if (data.Length < 18)
                        break;
                    int recordLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(16, 2));
                    if (recordLength < 19 || recordLength > data.Length)
                        break;
                    var record = data.Slice(0, recordLength);
                    data = data.Slice(recordLength);
                    // Go os.readdir does not skip inode0 on Linux, unlike the
                    // separate fswatch native readDirEntries worker.
                    var nameBytes = record.Slice(19);
                    int end = nameBytes.IndexOf((byte)0);
                    if (end >= 0)
                        nameBytes = nameBytes.Slice(0, end);
                    if (nameBytes.SequenceEqual("."u8) || nameBytes.SequenceEqual(".."u8))
                        continue;
                    var name = ByteStringValue.FromView(ByteStringView.CopyFrom(nameBytes));
                    byte type = record[18];
                    if (type == LinuxKernel.DT_UNKNOWN)
                    {
                        var path = ByteStringValue.Concat(directory, ByteStringValue.FromString("/"), name);
                        var(mode, statError) = StatPath(path, followLinks: false);
                        if (LinuxKernel.IsErrno(statError, LinuxKernel.ENOENT))
                            continue;
                        if (statError is not null)
                            return (entries, statError);
                        type = (mode & LinuxKernel.S_IFMT) == LinuxKernel.S_IFDIR ? LinuxKernel.DT_DIR : (byte)8;
                    }

                    entries = entries.Append(new UnixDirent(name, type));
                }
            }

            GoSliceSort.SortFunc(entries.AsSpan(), static (left, right) => ByteStringKey.FromValue(left.Name).CompareTo(ByteStringKey.FromValue(right.Name)));
            return (entries, null);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            _ = LinuxKernel.Close(fd);
        }
    }
}

internal sealed class LinuxPathError(string operation, ByteStringValue path, Exception error) : RawWatchError(operation + " " + path.Materialize()! + ": " + FsWatch.ErrorString(error), error);

// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Pinned internal/fswatch/walkdir_unix.go, walkdir_dirent_linux.go (complete).
using System.Buffers.Binary;
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal static partial class FsWatch
{
    private const int DirectoryOpenFlags = LinuxKernel.O_CLOEXEC | LinuxKernel.O_DIRECTORY | LinuxKernel.O_NOCTTY | LinuxKernel.O_NONBLOCK | LinuxKernel.O_NOFOLLOW;
    private readonly record struct UnixDirent(ByteStringValue Name, byte Type);
    internal static Exception? WalkDir(ByteStringValue directory, bool recursive, Func<ByteStringValue, bool, Exception?>? visit)
    {
        var(fd, error) = LinuxKernel.Open(directory, DirectoryOpenFlags, 0);
        if (error is not null)
            return LinuxKernel.IsErrno(error, LinuxKernel.ENOTDIR) ? LinuxKernel.Error(LinuxKernel.ENOTDIR) : error;
        try
        {
            // Original walkState owns exactly one reusable buffer per walk.
            return IterateDir(new byte[8192], fd, directory, recursive, visit);
        }
        finally
        {
            _ = LinuxKernel.Close(fd);
        }
    }

    private static Exception? IterateDir(byte[] buffer, int fd, ByteStringValue directory, bool recursive, Func<ByteStringValue, bool, Exception?>? visit)
    {
        if (visit?.Invoke(directory, true)is { } visitError)
            return visitError;
        var(entries, error) = ReadDirEntries(fd, buffer);
        if (error is not null)
            return error;
        foreach (var entry in entries)
        {
            var path = ByteStringValue.Concat(directory, ByteStringValue.FromString("/"), entry.Name);
            bool isDirectory = entry.Type == LinuxKernel.DT_DIR;
            if (entry.Type == LinuxKernel.DT_UNKNOWN)
            {
                var(mode, statError) = LinuxKernel.StatMode(path, followLinks: false);
                if (statError is not null)
                    continue;
                isDirectory = (mode & LinuxKernel.S_IFMT) == LinuxKernel.S_IFDIR;
            }

            if (!isDirectory)
            {
                if (visit?.Invoke(path, false)is { } errorFile)
                    return errorFile;
                continue;
            }

            if (!recursive)
            {
                if (visit?.Invoke(path, true)is { } errorDirectory)
                    return errorDirectory;
                continue;
            }

            var(childFD, openError) = LinuxKernel.OpenAt(fd, entry.Name, DirectoryOpenFlags, 0);
            if (openError is not null)
            {
                if (LinuxKernel.IsErrno(openError, LinuxKernel.EACCES) || LinuxKernel.IsErrno(openError, LinuxKernel.ENOTDIR) || LinuxKernel.IsErrno(openError, LinuxKernel.ENOENT))
                    continue;
                return openError;
            }

            Exception? childError;
            try
            {
                childError = IterateDir(buffer, childFD, path, recursive, visit);
            }
            finally
            {
                _ = LinuxKernel.Close(childFD);
            }

            if (childError is not null)
                return childError;
        }

        return null;
    }

    private static (Slice<UnixDirent> Entries, Exception? Error) ReadDirEntries(int fd, byte[] buffer)
    {
        Slice<UnixDirent> entries = default;
        while (true)
        {
            var(length, error) = LinuxKernel.ReadDirent(fd, buffer);
            if (error is not null)
                return (default, error);
            if (length <= 0)
                break;
            var data = buffer.AsSpan(0, length);
            while (!data.IsEmpty)
            {
                // Linux x64 dirent64: ino@0, reclen@16, type@18, name@19.
                int recordLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(16, 2));
                if (recordLength == 0 || recordLength > data.Length)
                    break;
                if (BinaryPrimitives.ReadUInt64LittleEndian(data) == 0)
                {
                    data = data.Slice(recordLength);
                    continue;
                }

                var nameBytes = data.Slice(19, recordLength - 19);
                int terminator = nameBytes.IndexOf((byte)0);
                if (terminator >= 0)
                    nameBytes = nameBytes.Slice(0, terminator);
                // Go string(nameBytes) copies: subsequent reads reuse buffer.
                var name = ByteStringValue.FromView(ByteStringView.CopyFrom(nameBytes));
                if (!name.EqualsBytes("."u8) && !name.EqualsBytes(".."u8))
                    entries = entries.Append(new UnixDirent(name, data[18]));
                data = data.Slice(recordLength);
            }
        }

        return (entries, null);
    }
}

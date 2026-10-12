// Copyright (c) Microsoft Corporation. All rights reserved.
// Linux x64 transport adaptation for pinned x/sys/unix v0.48.0 fswatch consumers.
// Algorithms remain in the mapped watcher owners; libc exposes the same syscalls.
using System.Runtime.InteropServices;
using System.Text;
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal sealed class LinuxErrno(int number, string message) : IOException(message)
{
    internal readonly int Number = number;
}

internal static unsafe partial class LinuxKernel
{
    internal const int EINTR = 4, EACCES = 13, ENOENT = 2, ENOTDIR = 20, EAGAIN = 11, EINVAL = 22;
    internal const int O_CLOEXEC = 0x80000, O_DIRECTORY = 0x10000, O_NOCTTY = 0x100, O_NONBLOCK = 0x800, O_NOFOLLOW = 0x20000;
    internal const uint S_IFMT = 0xf000, S_IFDIR = 0x4000;
    internal const byte DT_DIR = 4, DT_UNKNOWN = 0;
    internal const short POLLIN = 1;
    internal const uint IN_CREATE = 0x100, IN_DELETE = 0x200, IN_DELETE_SELF = 0x400, IN_MODIFY = 2, IN_MOVE_SELF = 0x800, IN_MOVED_FROM = 0x40, IN_MOVED_TO = 0x80, IN_DONT_FOLLOW = 0x2000000, IN_ONLYDIR = 0x1000000, IN_EXCL_UNLINK = 0x4000000, IN_Q_OVERFLOW = 0x4000, IN_ISDIR = 0x40000000;
    [StructLayout(LayoutKind.Sequential)]
    internal struct PollFD(int descriptor, short events)
    {
        internal int Descriptor = descriptor;
        internal short Events = events;
        internal short ReturnedEvents;
    }

    [LibraryImport("libc", EntryPoint = "open", SetLastError = true)]
    private static partial int NativeOpen(byte* path, int flags, int mode);
    [LibraryImport("libc", EntryPoint = "openat", SetLastError = true)]
    private static partial int NativeOpenAt(int directory, byte* path, int flags, int mode);
    [LibraryImport("libc", EntryPoint = "close", SetLastError = true)]
    private static partial int NativeClose(int descriptor);
    [LibraryImport("libc", EntryPoint = "read", SetLastError = true)]
    private static partial nint NativeRead(int descriptor, byte* buffer, nuint length);
    [LibraryImport("libc", EntryPoint = "write", SetLastError = true)]
    private static partial nint NativeWrite(int descriptor, byte* buffer, nuint length);
    [LibraryImport("libc", EntryPoint = "pipe2", SetLastError = true)]
    private static partial int NativePipe2(int* descriptors, int flags);
    [LibraryImport("libc", EntryPoint = "poll", SetLastError = true)]
    private static partial int NativePoll(PollFD* descriptors, nuint count, int timeout);
    [LibraryImport("libc", EntryPoint = "inotify_init1", SetLastError = true)]
    private static partial int NativeInotifyInit1(int flags);
    [LibraryImport("libc", EntryPoint = "inotify_add_watch", SetLastError = true)]
    private static partial int NativeInotifyAddWatch(int descriptor, byte* path, uint mask);
    [LibraryImport("libc", EntryPoint = "inotify_rm_watch", SetLastError = true)]
    private static partial int NativeInotifyRemoveWatch(int descriptor, uint watch);
    [LibraryImport("libc", EntryPoint = "fstatat", SetLastError = true)]
    private static partial int NativeStatAt(int directory, byte* path, byte* result, int flags);
    [LibraryImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static partial nint NativeGetDents64(nint syscallNumber, int descriptor, byte* buffer, nuint length);
    internal static LinuxErrno Error(int number) => new(number, ErrorText(number));
    internal static bool IsErrno(Exception? error, int number)
    {
        for (; error is not null; error = error.InnerException)
            if (error is LinuxErrno value && value.Number == number)
                return true;
        return false;
    }

    private static LinuxErrno LastError() => Error(Marshal.GetLastPInvokeError());
    private static (byte[]? Bytes, Exception? Error) PathBytes(ByteStringValue path)
    {
        RequirePlatform();
        var bytes = new byte[path.Length + 1];
        if (path.IsBytes)
            path.ByteSpan.CopyTo(bytes);
        else
            Encoding.Latin1.GetBytes(path.CharSpan, bytes);
        if (bytes.AsSpan(0, path.Length).IndexOf((byte)0) >= 0)
            return (null, Error(EINVAL));
        return (bytes, null);
    }

    private static void RequirePlatform()
    {
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new PlatformNotSupportedException("The fswatch native transport is implemented for Linux x64.");
    }

    internal static (int Descriptor, Exception? Error) Open(ByteStringValue path, int flags, int mode)
    {
        var(bytes, error) = PathBytes(path);
        if (error is not null)
            return (-1, error);
        fixed (byte* pointer = bytes)
        {
            int result = NativeOpen(pointer, flags, mode);
            return result < 0 ? (-1, LastError()) : (result, null);
        }
    }

    internal static (int Descriptor, Exception? Error) OpenAt(int directory, ByteStringValue path, int flags, int mode)
    {
        var(bytes, error) = PathBytes(path);
        if (error is not null)
            return (-1, error);
        fixed (byte* pointer = bytes)
        {
            int result = NativeOpenAt(directory, pointer, flags, mode);
            return result < 0 ? (-1, LastError()) : (result, null);
        }
    }

    internal static Exception? Close(int descriptor) => NativeClose(descriptor) < 0 ? LastError() : null;
    internal static (int Length, Exception? Error) Read(int descriptor, Span<byte> buffer)
    {
        fixed (byte* pointer = buffer)
        {
            nint result = NativeRead(descriptor, pointer, (nuint)buffer.Length);
            return result < 0 ? (0, LastError()) : (checked((int)result), null);
        }
    }

    internal static (int Length, Exception? Error) Write(int descriptor, ReadOnlySpan<byte> buffer)
    {
        fixed (byte* pointer = buffer)
        {
            nint result = NativeWrite(descriptor, pointer, (nuint)buffer.Length);
            return result < 0 ? (0, LastError()) : (checked((int)result), null);
        }
    }

    internal static Exception? Pipe2(int[] descriptors, int flags)
    {
        RequirePlatform();
        fixed (int* pointer = descriptors)
            return NativePipe2(pointer, flags) < 0 ? LastError() : null;
    }

    internal static Exception? Poll(PollFD[] descriptors, int timeout)
    {
        fixed (PollFD* pointer = descriptors)
            return NativePoll(pointer, (nuint)descriptors.Length, timeout) < 0 ? LastError() : null;
    }

    internal static (int Descriptor, Exception? Error) InotifyInit1(int flags)
    {
        int result = NativeInotifyInit1(flags);
        return result < 0 ? (-1, LastError()) : (result, null);
    }

    internal static (int Descriptor, Exception? Error) InotifyAddWatch(int descriptor, ByteStringValue path, uint mask)
    {
        var(bytes, error) = PathBytes(path);
        if (error is not null)
            return (0, error);
        fixed (byte* pointer = bytes)
        {
            int result = NativeInotifyAddWatch(descriptor, pointer, mask);
            return result < 0 ? (0, LastError()) : (result, null);
        }
    }

    internal static Exception? InotifyRemoveWatch(int descriptor, uint watch) => NativeInotifyRemoveWatch(descriptor, watch) < 0 ? LastError() : null;
    internal static (uint Mode, Exception? Error) StatMode(ByteStringValue path, bool followLinks)
    {
        var(bytes, error) = PathBytes(path);
        if (error is not null)
            return (0, error);
        // Linux x64 struct stat is 144 bytes; st_mode is uint32 at offset24.
        byte* result = stackalloc byte[144];
        fixed (byte* pointer = bytes)
        {
            if (NativeStatAt(-100, pointer, result, followLinks ? 0 : 0x100) < 0)
                return (0, LastError());
            return (*(uint*)(result + 24), null);
        }
    }

    internal static (int Length, Exception? Error) ReadDirent(int descriptor, Span<byte> buffer)
    {
        fixed (byte* pointer = buffer)
        {
            nint result = NativeGetDents64(217, descriptor, pointer, (nuint)buffer.Length);
            return result < 0 ? (0, LastError()) : (checked((int)result), null);
        }
    }
}

internal static partial class FsWatch
{
    internal static Exception? ValidateWatchDirectory(ByteStringValue directory)
    {
        var(mode, error) = StatPath(directory, followLinks: true);
        if (error is not null)
            return error;
        return (mode & LinuxKernel.S_IFMT) == LinuxKernel.S_IFDIR ? null : LinuxKernel.Error(LinuxKernel.ENOTDIR);
    }
}

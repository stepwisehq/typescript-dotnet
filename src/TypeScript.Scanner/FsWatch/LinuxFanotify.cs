// Copyright (c) Microsoft Corporation. All rights reserved.
// Linux x64 native transport for pinned x/sys/unix v0.48.0.
// NameToHandleAt retry/buffer ownership follows unix/syscall_linux.go:2371.
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal static unsafe partial class LinuxKernel
{
    internal const int EOPNOTSUPP = 95, ENODEV = 19, EOVERFLOW = 75;
    internal const uint FAN_CLASS_NOTIF = 0, FAN_CLOEXEC = 1, FAN_NONBLOCK = 2, FAN_REPORT_FID = 0x200, FAN_REPORT_DFID_NAME = 0xc00, FAN_MARK_ADD = 1, FAN_MARK_REMOVE = 2, FAN_MARK_DONT_FOLLOW = 4, FAN_MARK_ONLYDIR = 8;
    internal const ulong FAN_CREATE = 0x100, FAN_DELETE = 0x200, FAN_MODIFY = 2, FAN_DELETE_SELF = 0x400, FAN_MOVE_SELF = 0x800, FAN_ONDIR = 0x40000000, FAN_EVENT_ON_CHILD = 0x8000000, FAN_RENAME = 0x10000000, FAN_MOVED_FROM = 0x40, FAN_MOVED_TO = 0x80, FAN_Q_OVERFLOW = 0x4000;
    internal const byte FAN_EVENT_INFO_TYPE_DFID = 3, FAN_EVENT_INFO_TYPE_DFID_NAME = 2, FAN_EVENT_INFO_TYPE_OLD_DFID_NAME = 10, FAN_EVENT_INFO_TYPE_NEW_DFID_NAME = 12, FANOTIFY_METADATA_VERSION = 3;
    [LibraryImport("libc", EntryPoint = "fanotify_init", SetLastError = true)]
    private static partial int NativeFanotifyInit(uint flags, uint eventFlags);
    [LibraryImport("libc", EntryPoint = "fanotify_mark", SetLastError = true)]
    private static partial int NativeFanotifyMark(int descriptor, uint flags, ulong mask, int directory, byte* path);
    [LibraryImport("libc", EntryPoint = "name_to_handle_at", SetLastError = true)]
    private static partial int NativeNameToHandleAt(int directory, byte* path, byte* handle, int* mountID, int flags);
    [LibraryImport("libc", EntryPoint = "statfs", SetLastError = true)]
    private static partial int NativeStatFs(byte* path, byte* result);
    internal static (int Descriptor, Exception? Error) FanotifyInit(uint flags, uint eventFlags)
    {
        RequirePlatform();
        int result = NativeFanotifyInit(flags, eventFlags);
        return result < 0 ? (-1, LastError()) : (result, null);
    }

    internal static Exception? FanotifyMark(int descriptor, uint flags, ulong mask, int directory, ByteStringValue path)
    {
        var(bytes, error) = PathBytes(path);
        if (error is not null)
            return error;
        fixed (byte* pointer = bytes)
            return NativeFanotifyMark(descriptor, flags, mask, directory, pointer) < 0 ? LastError() : null;
    }

    internal static (ByteStringView Handle, int Type, Exception? Error) NameToHandleAt(int directory, ByteStringValue path, int flags)
    {
        uint size = 32 + 8;
        bool resized = false;
        while (true)
        {
            var buffer = new byte[size];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, size - 8);
            // The original syscall wrapper converts its pathname on each retry.
            var(bytes, pathError) = PathBytes(path);
            if (pathError is not null)
                return (default, 0, pathError);
            int mountID;
            Exception? error;
            fixed (byte* pointer = bytes)
            fixed (byte* handle = buffer)
                error = NativeNameToHandleAt(directory, pointer, handle, &mountID, flags) < 0 ? LastError() : null;
            uint length = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
            if (IsErrno(error, EOVERFLOW))
            {
                if (resized)
                    return (default, 0, error);
                resized = true;
                size = unchecked(length + 8);
                continue;
            }

            if (error is not null)
                return (default, 0, error);
            int type = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4));
            // FileHandle.Bytes borrows the required syscall buffer owner.
            return (ByteStringView.FromOwnedBytes(buffer).Slice(8, 8 + checked((int)length)), type, null);
        }
    }

    internal static (int Fsid0, int Fsid1, Exception? Error) StatFs(ByteStringValue path)
    {
        var(bytes, error) = PathBytes(path);
        if (error is not null)
            return (0, 0, error);
        // Exact Linux x64 Statfs_t size120; fsid two int32 values at offset56.
        byte* result = stackalloc byte[120];
        fixed (byte* pointer = bytes)
        {
            if (NativeStatFs(pointer, result) < 0)
                return (0, 0, LastError());
            return (*(int*)(result + 56), *(int*)(result + 60), null);
        }
    }
}

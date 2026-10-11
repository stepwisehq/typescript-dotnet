// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Authored Linux adapter for os.Stat information used by internal/vfs/osvfs/os.go.
// Linux statx uses a stable 256-byte ABI; ordinary stat follows links (flags=0).
// Other platform representations are independent and are not claimed verified here.
using System.Runtime.InteropServices;
using TypeScript.Scanner;

namespace TypeScript.Vfs.OsVfs;
internal static partial class LinuxStat
{
    [StructLayout(LayoutKind.Explicit, Size = 256)]
    private struct StatxBuffer
    {
        [FieldOffset(0)]
        public uint Mask;
        [FieldOffset(28)]
        public ushort Mode;
        [FieldOffset(40)]
        public ulong Size;
        [FieldOffset(112)]
        public long ModifiedSeconds;
        [FieldOffset(120)]
        public uint ModifiedNanoseconds;
    }

    [LibraryImport("libc", EntryPoint = "statx", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int statx(int directory, string path, int flags, uint mask, out StatxBuffer result);
    internal static ushort? PathMode(string nativePath, bool followLinks)
    {
        if (nativePath.Contains('\0') || statx(-100, nativePath, followLinks ? 0 : 0x100, 2, out var result) != 0)
            return null;
        if ((result.Mask & 2) == 0)
            throw new IOException("statx did not return the requested file mode.");
        return result.Mode;
    }

    internal static bool IsSymlink(string nativePath)
    {
        if (nativePath.Contains('\0'))
            return false;
        return PathMode(nativePath, false)is { } mode && (mode & 0xF000) == 0xA000;
    }

    internal static FileInfo? Stat(string nativePath, ByteStringValue rawName)
    {
        // Go syscall pathname conversion rejects NUL; never let libc truncate it.
        if (nativePath.Contains('\0'))
            return null;
        if (statx(-100, nativePath, 0, 0x7FF, out var result) != 0)
            return null;
        if ((result.Mask & 0x242) != 0x242)
            throw new IOException("statx did not return mode, size and modification time");
        FileMode mode = (FileMode)(result.Mode & 0x1FF);
        if ((result.Mode & 0x800) != 0)
            mode |= FileMode.Setuid;
        if ((result.Mode & 0x400) != 0)
            mode |= FileMode.Setgid;
        if ((result.Mode & 0x200) != 0)
            mode |= FileMode.Sticky;
        mode |= (result.Mode & 0xF000) switch
        {
            0x4000 => FileMode.Dir,
            0x1000 => FileMode.NamedPipe,
            0xC000 => FileMode.Socket,
            0x2000 => FileMode.Device | FileMode.CharDevice,
            0x6000 => FileMode.Device,
            0x8000 => FileMode.Regular,
            _ => FileMode.Irregular
        };
        return new linuxFileInfo(rawName, unchecked((long)result.Size), mode, result.ModifiedSeconds, result.ModifiedNanoseconds);
    }

    private sealed class linuxFileInfo(ByteStringValue name, long size, FileMode mode, long seconds, uint nanoseconds) : FileInfo
    {
        public string Name() => name.Materialize()!;
        public long Size() => size;
        public FileMode Mode() => mode;
        public DateTimeOffset ModTime() => DateTimeOffset.FromUnixTimeSeconds(seconds).AddTicks(nanoseconds / 100);
        public bool IsDir() => (mode & FileMode.Dir) != 0;
        // Raw syscall allocator/storage topology is intentionally not a public managed contract.
        public object? Sys() => null;
    }
}

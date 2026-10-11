// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0: licenses/TypeScript-LICENSE.txt.
// Authored Linux x64 read-only syscall boundary for the selected osvfs adapter.
// glibc readdir64 exposes the Linux dirent64 ABI: reclen@16, type@18, name@19.
// Names/targets are read as actual bytes before the strict .NET path boundary;
// .NET directory/link APIs would already have replaced invalid UTF8 here.
using System.Runtime.InteropServices;
using System.Text;
using TypeScript.Scanner;

namespace TypeScript.Vfs.OsVfs;
internal static partial class LinuxReadPaths
{
    private static readonly UTF8Encoding scalarEncoding = new(false, true);
    internal readonly record struct Entry(ByteStringValue Name, bool IsSymlink);
    [LibraryImport("libc", EntryPoint = "opendir", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial nint OpenDirectory(string path);
    [LibraryImport("libc", EntryPoint = "readdir64", SetLastError = true)]
    private static partial nint ReadDirectory(nint directory);
    [LibraryImport("libc", EntryPoint = "closedir", SetLastError = true)]
    private static partial int CloseDirectory(nint directory);
    [LibraryImport("libc", EntryPoint = "__errno_location")]
    private static partial nint ErrnoLocation();
    [LibraryImport("libc", EntryPoint = "readlink", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static unsafe partial nint ReadLink(string path, byte* buffer, nuint size);
    [LibraryImport("libc", EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int Open(string path, int flags, int mode);
    [LibraryImport("libc", EntryPoint = "close", SetLastError = true)]
    private static partial int Close(int descriptor);
    private static void RequirePlatform(string path)
    {
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new PlatformNotSupportedException("Raw OS path validation is currently verified for Linux x64 only.");
        if (path.Contains('\0'))
            throw new ArgumentException("OS path contains a NUL character.", nameof(path));
    }

    private static void Validate(byte[] bytes)
    {
        try
        {
            _ = scalarEncoding.GetCharCount(bytes);
        }
        catch (DecoderFallbackException error)
        {
            throw new FileSystemPathEncodingException(GoString.FromBytes(bytes), error);
        }
    }

    internal static Slice<Entry> Entries(string path)
    {
        RequirePlatform(path);
        nint directory = OpenDirectory(path);
        if (directory == 0)
            throw new IOException("opendir failed: errno " + Marshal.GetLastPInvokeError());
        Slice<Entry> entries = default;
        try
        {
            while (true)
            {
                // readdir64 uses null for both EOF and error; clear native errno
                // before the call rather than only the managed saved-error slot.
                Marshal.WriteInt32(ErrnoLocation(), 0);
                nint entry = ReadDirectory(directory);
                if (entry == 0)
                {
                    int error = Marshal.GetLastPInvokeError();
                    if (error != 0)
                        throw new IOException("readdir64 failed: errno " + error);
                    return entries;
                }

                int recordLength = (ushort)Marshal.ReadInt16(entry, 16);
                if (recordLength <= 19)
                    throw new IOException("Invalid Linux dirent64 record length.");
                int length = 0;
                while (length < recordLength - 19 && Marshal.ReadByte(entry, 19 + length) != 0)
                    length++;
                if (length == recordLength - 19)
                    throw new IOException("Linux dirent64 name lacks a terminator.");
                byte[] name = new byte[length];
                Marshal.Copy(entry + 19, name, 0, length);
                if (name.AsSpan().SequenceEqual("."u8) || name.AsSpan().SequenceEqual(".."u8))
                    continue;
                Validate(name);
                var rawName = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(name));
                byte type = Marshal.ReadByte(entry, 18);
                bool link = type == 10;
                if (type == 0)
                    link = LinuxStat.IsSymlink(System.IO.Path.Combine(path, scalarEncoding.GetString(name)));
                entries = entries.Append(new Entry(rawName, link));
            }
        }
        finally
        {
            _ = CloseDirectory(directory);
        }
    }

    internal static unsafe ByteStringValue? ReadLinkTarget(string path)
    {
        // readlink does not terminate its result and can truncate. Grow until
        // the actual target fits; no guessed fixed path limit or lossy decoder.
        for (int size = 256;; size = checked(size * 2))
        {
            byte[] bytes = new byte[size];
            nint count;
            do
            {
                fixed (byte* buffer = bytes)
                    count = ReadLink(path, buffer, (nuint)size);
            }
            while (count < 0 && Marshal.GetLastPInvokeError() == 4 /* EINTR */);
            if (count < 0)
                return null; // Not a link or an ordinary inaccessible-path failure.
            if (count < size)
            {
                var target = bytes.AsSpan(0, (int)count);
                try { _ = scalarEncoding.GetCharCount(target); }
                catch (DecoderFallbackException error) { throw new FileSystemPathEncodingException(GoString.FromBytes(target), error); }
                return ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes).Slice(0, (int)count));
            }
        }
    }

    internal static ByteStringValue? Realpath(string path)
    {
        RequirePlatform(path);
        return TypeScript.NativePath.NativePath.Realpath(PathByteText.FromUnicodeValue(path));
    }

    internal static string Native(ByteStringValue bytes)
    {
        try
        {
            return PathByteText.ToUnicode(bytes);
        }
        catch (DecoderFallbackException error)
        {
            throw new FileSystemPathEncodingException(bytes.Materialize()!, error);
        }
    }

    internal static int OpenPath(string native)
    {
        int descriptor;
        do
        {
            descriptor = Open(native, 0x80000 /* O_CLOEXEC */ | 0x200000 /* O_PATH */, 0);
        }
        while (descriptor < 0 && Marshal.GetLastPInvokeError() == 4 /* EINTR */);
        return descriptor;
    }

    internal static void ClosePath(int descriptor) => _ = Close(descriptor);
}

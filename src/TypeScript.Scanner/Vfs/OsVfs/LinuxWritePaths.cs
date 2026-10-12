// Copyright 2009 The Go Authors. All rights reserved.
// Modified C# port, distributed under the BSD license in licenses/Go-LICENSE.txt.
// Demanded Linux syscall boundary for os.MkdirAll (Go1.27.1 os/path.go:19-69).
using System.Runtime.InteropServices;

namespace TypeScript.Vfs.OsVfs;
internal static partial class LinuxWritePaths
{
    [LibraryImport("libc", EntryPoint = "mkdir", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int Mkdir(string path, uint mode);
    [LibraryImport("libc", EntryPoint = "strerror")]
    private static partial nint Strerror(int error);
    private static IOException PathError(string path, int error, string operation = "mkdir")
    {
        string text = Marshal.PtrToStringUTF8(Strerror(error)) ?? $"errno {error}";
        // Linux errno names are sentences in libc; Go's syscall table starts lowercase.
        if (text.Length != 0 && char.IsUpper(text[0]))
            text = char.ToLowerInvariant(text[0]) + text[1..];
        return new IOException($"{operation} {path}: {text}");
    }

    [LibraryImport("libc", EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int Open(string path, int flags, uint mode);
    [LibraryImport("libc", EntryPoint = "write", SetLastError = true)]
    private static unsafe partial nint Write(int descriptor, byte* bytes, nuint length);
    [LibraryImport("libc", EntryPoint = "close", SetLastError = true)]
    private static partial int Close(int descriptor);
    // os.OpenFile/os.File.Write: preserve actual errno and original operation/path.
    // Bounded write concurrency and required public-string encoding remain in OsFS.
    internal static unsafe Exception? WriteFile(string path, ReadOnlySpan<byte> bytes, bool append = false)
    {
        if (path.Contains('\0'))
            return PathError(path, 22, "open");
        int descriptor;
        do
        {
            descriptor = Open(path, 1 | 64 | (append ? 1024 : 512) | 0x80000 /* WRONLY|CREAT|APPEND/TRUNC|CLOEXEC */, 0x1B6 /* 0666 */);
        }
        while (descriptor < 0 && Marshal.GetLastPInvokeError() == 4);
        if (descriptor < 0)
            return PathError(path, Marshal.GetLastPInvokeError(), "open");
        try
        {
            fixed (byte* buffer = bytes)
            {
                int offset = 0;
                do
                {
                    int count = Math.Min(bytes.Length - offset, 1 << 30);
                    nint written = Write(descriptor, buffer + offset, (nuint)count);
                    if (written > count)
                        throw new InvalidOperationException($"invalid return from write: got {written} from a write of {count}");
                    if (written < 0)
                    {
                        int error = Marshal.GetLastPInvokeError();
                        if (error == 4)
                            continue;
                        return PathError(path, error, "write");
                    }

                    if (written == 0 && offset < bytes.Length)
                        return new IOException($"write {path}: unexpected EOF");
                    offset += checked((int)written);
                }
                while (offset < bytes.Length);
            }

            return null;
        }
        finally
        {
            // Original deferred Close never replaces the write error/result.
            _ = Close(descriptor);
        }
    }

    internal static Exception? MkdirAll(string path)
    {
        if (LinuxStat.PathMode(path, followLinks: true)is { } mode)
            return (mode & 0xF000) == 0x4000 ? null : PathError(path, 20 /* ENOTDIR */);
        int i = path.Length - 1;
        while (i >= 0 && path[i] == '/')
            i--;
        while (i >= 0 && path[i] != '/')
            i--;
        if (i < 0)
            i = 0;
        // Linux has no volume-name prefix. Preserve the original spelling in errors.
        if (i > 0 && MkdirAll(path[..i])is { } parentError)
            return parentError;
        if (path.Contains('\0'))
            return PathError(path, 22 /* EINVAL: Go pathname conversion rejects NUL */);
        int result;
        do
        {
            result = Mkdir(path, 0x1FF /* 0777, subject to process umask */);
        }
        while (result < 0 && Marshal.GetLastPInvokeError() == 4 /* EINTR */);
        if (result == 0)
            return null;
        int error = Marshal.GetLastPInvokeError();
        // Original Lstat retry handles races and names such as foo/.
        if (LinuxStat.PathMode(path, followLinks: false)is { } finalMode && (finalMode & 0xF000) == 0x4000)
            return null;
        return PathError(path, error);
    }
}

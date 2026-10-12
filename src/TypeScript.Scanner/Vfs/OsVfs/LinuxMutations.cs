// Copyright 2009 The Go Authors. All rights reserved.
// Modified C# adapter, BSD license in licenses/Go-LICENSE.txt.
// Linux syscall boundary for osvfs os.RemoveAll/os.Chtimes consumers.
using System.Runtime.InteropServices;
using TypeScript.Scanner;

namespace TypeScript.Vfs.OsVfs;
internal static partial class LinuxWritePaths
{
    [LibraryImport("libc", EntryPoint = "unlink", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int Unlink(string path);
    [LibraryImport("libc", EntryPoint = "rmdir", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int Rmdir(string path);
    [StructLayout(LayoutKind.Sequential)]
    private struct Timespec
    {
        internal long Seconds;
        internal long Nanoseconds;
    }

    [LibraryImport("libc", EntryPoint = "utimensat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static unsafe partial int Utimensat(int directory, string path, Timespec* times, int flags);
    internal static unsafe Exception? Chtimes(string path, DateTimeOffset access, DateTimeOffset modification)
    {
        if (path.Contains('\0'))
            return PathError(path, 22, "chtimes");
        Timespec* times = stackalloc Timespec[2];
        times[0] = Timestamp(access);
        times[1] = Timestamp(modification);
        return Utimensat(-100 /* AT_FDCWD */, path, times, 0) == 0 ? null : PathError(path, Marshal.GetLastPInvokeError(), "chtimes");
    }

    private static Timespec Timestamp(DateTimeOffset value)
    {
        long seconds = value.ToUnixTimeSeconds();
        long ticks = value.UtcTicks - DateTimeOffset.FromUnixTimeSeconds(seconds).UtcTicks;
        return new()
        {
            Seconds = seconds,
            Nanoseconds = ticks * 100
        };
    }

    [LibraryImport("libc", EntryPoint = "unlinkat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int UnlinkAt(int directory, string name, int flags);
    [LibraryImport("libc", EntryPoint = "openat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int OpenAt(int directory, string name, int flags);
    [LibraryImport("libc", EntryPoint = "readlinkat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static unsafe partial nint ReadLinkAt(int directory, string name, byte* buffer, nuint size);
    [LibraryImport("libc", EntryPoint = "fdopendir", SetLastError = true)]
    private static partial nint FdOpenDir(int descriptor);
    [LibraryImport("libc", EntryPoint = "readdir", SetLastError = true)]
    private static partial nint ReadDir(nint directory);
    [LibraryImport("libc", EntryPoint = "closedir", SetLastError = true)]
    private static partial int CloseDir(nint directory);
    private readonly record struct RemovalError(string Operation, string Path, int Error)
    {
        internal RemovalError Prefix(string parent) => this with
        {
            Path = parent + "/" + Path
        };
        internal Exception Exception() => PathError(Path, Error, Operation);
    }

    // Go1.27.1 os/removeall_at.go:17. Retain Remove's initial operation,
    // then recurse relative to opened directory descriptors, never symlink targets.
    internal static Exception? RemoveAll(string path)
    {
        if (path.Length == 0)
            return null;
        if (path == "." || path.EndsWith("/.", StringComparison.Ordinal))
            return PathError(path, 22, "RemoveAll");
        if (path.Contains('\0'))
            return PathError(path, 22, "open");
        int error = RemovePath(path);
        if (error is 0 or 2)
            return null;
        var(parent, basename) = SplitPath(path);
        int descriptor;
        do
        {
            descriptor = Open(parent, 0x80000, 0);
        }
        while (descriptor < 0 && Marshal.GetLastPInvokeError() == 4);
        if (descriptor < 0)
        {
            error = Marshal.GetLastPInvokeError();
            return error == 2 ? null : PathError(parent, error, "open");
        }

        try
        {
            return RemoveAllFrom(descriptor, basename)?.Prefix(parent).Exception();
        }
        finally
        {
            _ = Close(descriptor);
        }
    }

    // os/file_unix.go:357. Both unlink and rmdir are tried, and ENOTDIR
    // selects unlink's error; all interrupted calls are retried.
    private static int RemovePath(string path)
    {
        int result;
        do
        {
            result = Unlink(path);
        }
        while (result < 0 && Marshal.GetLastPInvokeError() == 4);
        if (result == 0)
            return 0;
        int error = Marshal.GetLastPInvokeError();
        do
        {
            result = Rmdir(path);
        }
        while (result < 0 && Marshal.GetLastPInvokeError() == 4);
        if (result == 0)
            return 0;
        int directoryError = Marshal.GetLastPInvokeError();
        return directoryError == 20 ? error : directoryError;
    }

    // os/path_unix.go:20. Preserve parent spelling and the source slash rules.
    private static (string Parent, string Base) SplitPath(string path)
    {
        while (path.Length > 1 && path[0] == '/' && path[1] == '/')
            path = path[1..];
        int i = path.Length - 1;
        while (i > 0 && path[i] == '/')
        {
            path = path[..i];
            i--;
        }

        for (i--; i >= 0; i--)
            if (path[i] == '/')
                return (i == 0 ? path[..1] : path[..i], path[(i + 1)..]);
        return (".", path);
    }

    private static int RemoveAt(int descriptor, string name, bool directory)
    {
        int result;
        do
        {
            result = UnlinkAt(descriptor, name, directory ? 0x200 : 0);
        }
        while (result < 0 && Marshal.GetLastPInvokeError() == 4);
        return result == 0 ? 0 : Marshal.GetLastPInvokeError();
    }

    // removeall_at.go:71. Bounded reads/reopen prevent deletion reshuffling from
    // skipping children; first recursion error survives only if final removal fails.
    private static unsafe RemovalError? RemoveAllFrom(int parent, string basename)
    {
        int unlinkError = RemoveAt(parent, basename, false);
        if (unlinkError is 0 or 2)
            return null;
        if (unlinkError is not (21 or 1 or 13))
            return new("unlinkat", basename, unlinkError);
        RemovalError? recursionError = null;
        for (;;)
        {
            int descriptor;
            do
            {
                descriptor = OpenAt(parent, basename, 0x80000 | 0x10000 | 0x20000 /* CLOEXEC|DIRECTORY|NOFOLLOW */);
            }
            while (descriptor < 0 && Marshal.GetLastPInvokeError() == 4);
            if (descriptor < 0)
            {
                int error = Marshal.GetLastPInvokeError();
                // os/root_unix.go:112 rootOpenDir distinguishes symlinks from real ENOTDIR.
                bool symlink = (error is 40 or 31 or 20) && IsSymlinkAt(parent, basename);
                if (error == 95)
                    error = 20; // Linux ENOTSUP/EOPNOTSUPP, exactly the original translation.
                if (error == 2)
                    return null;
                if (error == 20 && !symlink)
                    return new("unlinkat", basename, unlinkError);
                recursionError = new("openfdat", basename, symlink ? unlinkError : error);
                break;
            }

            nint directory = FdOpenDir(descriptor);
            if (directory == 0)
            {
                int error = Marshal.GetLastPInvokeError();
                _ = Close(descriptor);
                recursionError = new("openfdat", basename, error);
                break;
            }

            int responseSize = 0;
            try
            {
                for (;;)
                {
                    var names = Slice<string>.Make(0, 1024);
                    while (names.Length < 1024)
                    {
                        Marshal.SetLastSystemError(0);
                        nint entry = ReadDir(directory);
                        if (entry == 0)
                        {
                            int error = Marshal.GetLastPInvokeError();
                            if (error != 0)
                                return error == 2 ? null : new("readdirnames", basename, error);
                            break;
                        }

                        // Linux 64-bit dirent d_name starts after ino/off/reclen/type.
                        byte* nameBytes = (byte*)entry + 19;
                        int length = 0;
                        while (nameBytes[length] != 0)
                            length++;
                        var value = TypeScript.Scanner.ByteStringValue.FromView(TypeScript.Scanner.ByteStringView.FromOwnedBytes(new ReadOnlySpan<byte>(nameBytes, length).ToArray()));
                        string name = TypeScript.Scanner.PathByteText.ToUnicode(value);
                        if (name is "." or "..")
                            continue;
                        names = names.Append(name);
                    }

                    responseSize = names.Length;
                    int errors = 0;
                    foreach (var name in names)
                        if (RemoveAllFrom(descriptor, name)is { } error)
                        {
                            errors++;
                            recursionError ??= error.Prefix(basename);
                        }

                    if (errors != 1024)
                        break;
                }
            }
            finally
            {
                _ = CloseDir(directory);
            }

            if (responseSize < 1024)
                break;
        }

        int finalError = RemoveAt(parent, basename, true);
        return finalError is 0 or 2 ? null : recursionError ?? new RemovalError("unlinkat", basename, finalError);
    }

    // os/root_unix.go:277 checkSymlink / :285 readlinkat. Only success is consumed here;
    // retain the complete growth/EINTR rules before projecting the errSymlink marker.
    private static unsafe bool IsSymlinkAt(int parent, string name)
    {
        for (int length = 128;; length = checked(length * 2))
        {
            var bytes = new byte[length];
            nint count;
            fixed (byte* buffer = bytes)
            {
                do
                {
                    count = ReadLinkAt(parent, name, buffer, (nuint)bytes.Length);
                }
                while (count < 0 && Marshal.GetLastPInvokeError() == 4);
            }

            if (count < 0 && Marshal.GetLastPInvokeError() == 34)
                continue;
            if (count < 0)
                return false;
            if (count < length)
                return true;
        }
    }
}

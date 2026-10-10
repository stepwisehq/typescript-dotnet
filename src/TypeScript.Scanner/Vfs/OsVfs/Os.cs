// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified read-side C# port of tsc/internal/vfs/osvfs/os.go and
// vfs/internal/internal.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// .NET syscall adapter; strict scalar decoding applies only here.
using TypeScript.Scanner;

namespace TypeScript.Vfs.OsVfs;
public sealed class FileSystemPathEncodingException(string bytes, Exception? cause = null) : IOException("The .NET filesystem requires a scalar Unicode path; the original Go byte string is not representable.", cause)
{
    public string PathBytes { get; } = bytes;
}

public static class OsVfs
{
    private static readonly IFS instance = new OsFS();
    public static IFS FS() => instance;
    private sealed class OsFS : IFS, ISourceTextFileSystem
    {
        private readonly CaseSensitivity sensitivity = detectCaseSensitivity();
        private static string Native(string bytes)
        {
            try
            {
                return PathByteText.ToUnicode(bytes).Replace('/', System.IO.Path.DirectorySeparatorChar);
            }
            catch (System.Text.DecoderFallbackException error)
            {
                throw new FileSystemPathEncodingException(bytes, error);
            }
        }

        private static bool readFailure(Exception error) => error is IOException or UnauthorizedAccessException or System.Security.SecurityException;
        private static CaseSensitivity detectCaseSensitivity()
        {
            if (OperatingSystem.IsWindows())
                return global::TypeScript.Scanner.CaseSensitivity.CaseInsensitive;
            if (OperatingSystem.IsBrowser())
                return global::TypeScript.Scanner.CaseSensitivity.CaseSensitive;
            string executable = Environment.ProcessPath ?? throw new IOException("vfs: failed to get executable path");
            string raw = PathByteText.FromUnicode(executable);
            var swapped = new GoBuilder();
            foreach (int rune in GoString.EnumerateRunes(raw))
            {
                int upper = UnicodeCase.ToUpper(rune);
                swapped.WriteRune(upper == rune ? UnicodeCase.ToLower(rune) : upper);
            }

            // File.Exists would swallow permission/I/O failures that source treats
            // as detection failure, so stat the swapped path directly.
            try
            {
                _ = System.IO.File.GetAttributes(Native(swapped.String()));
                return global::TypeScript.Scanner.CaseSensitivity.CaseInsensitive;
            }
            catch (FileNotFoundException)
            {
                return global::TypeScript.Scanner.CaseSensitivity.CaseSensitive;
            }
            catch (DirectoryNotFoundException)
            {
                return global::TypeScript.Scanner.CaseSensitivity.CaseSensitive;
            }
        }

        public CaseSensitivity CaseSensitivity() => sensitivity;
        public bool FileExists(RootedFilePath path) => Stat(path.AsPath())is { } info && !info.IsDir();
        public bool DirectoryExists(RootedDirectoryPath path) => Stat(path.AsPath())?.IsDir() == true;
        public (string Contents, bool Ok) ReadFile(RootedFilePath path)
        {
            var (contents, ok) = ReadSourceText(path);
            return ok ? (contents.Materialize(), true) : ("", false);
        }
        public (ByteStringView Contents, bool Ok) ReadSourceText(RootedFilePath path)
        {
            string native = Native(path.AsString());
            if (native.Contains('\0'))
                return (default, false);
            try
            {
                return VfsInternal.DecodeSourceBytes(System.IO.File.ReadAllBytes(native));
            }
            catch (Exception error)when (readFailure(error))
            {
                return (default, false);
            }
        }

        public FileInfo? Stat(RootedPath path)
        {
            string native = Native(path.AsString());
            if (native.Contains('\0'))
                return null;
            if (OperatingSystem.IsLinux())
                return LinuxStat.Stat(native, path.BaseName());
            try
            {
                var attributes = System.IO.File.GetAttributes(native);
                System.IO.FileSystemInfo info = (attributes & FileAttributes.Directory) != 0 ? new DirectoryInfo(native) : new System.IO.FileInfo(native);
                if (info.LinkTarget != null)
                {
                    info = info.ResolveLinkTarget(true)!;
                    if (info == null || !info.Exists)
                        return null;
                }

                if (!info.Exists)
                    return null;
                return new osFileInfo(path.BaseName(), info);
            }
            catch (Exception error)when (readFailure(error))
            {
                return null;
            }
        }

        public Entries GetAccessibleEntries(RootedDirectoryPath path)
        {
            string native = Native(path.AsString());
            var result = new Entries
            {
                Symlinks = new HashSet<string>(StringComparer.Ordinal)
            };
            if (native.Contains('\0'))
                return result;
            if (OperatingSystem.IsLinux())
            {
                List<LinuxReadPaths.Entry> names;
                try
                {
                    names = LinuxReadPaths.Entries(native);
                }
                catch (FileSystemPathEncodingException)
                {
                    throw;
                }
                catch (Exception error)when (readFailure(error))
                {
                    return result;
                }

                foreach (var entry in names.OrderBy(value => value.Name, StringComparer.Ordinal))
                {
                    FileInfo? info = Stat(path.ResolveFile(entry.Name).AsPath());
                    if (info == null)
                        continue;
                    if (info.IsDir())
                        (result.Directories ??= []).Add(entry.Name);
                    else if ((info.Mode() & FileMode.Type) == FileMode.Regular)
                        (result.Files ??= []).Add(entry.Name);
                    else
                        continue;
                    if (entry.IsSymlink)
                        result.Symlinks!.Add(entry.Name);
                }

                return result;
            }

            List<System.IO.FileSystemInfo> entries;
            try
            {
                entries = new DirectoryInfo(native).EnumerateFileSystemInfos().ToList();
            }
            catch (Exception error)when (readFailure(error))
            {
                return result;
            }

            foreach (var entry in entries.OrderBy(entry => PathByteText.FromUnicode(entry.Name), StringComparer.Ordinal))
            {
                string name = PathByteText.FromUnicode(entry.Name);
                bool link;
                try
                {
                    link = entry.LinkTarget != null || (entry.Attributes & FileAttributes.ReparsePoint) != 0;
                }
                catch (Exception error)when (readFailure(error))
                {
                    continue;
                }

                FileInfo? info = Stat(path.ResolveFile(name).AsPath());
                if (info == null)
                    continue;
                if (info.IsDir())
                    (result.Directories ??= []).Add(name);
                else if ((info.Mode() & FileMode.Type) == FileMode.Regular)
                    (result.Files ??= []).Add(name);
                else
                    continue;
                if (link)
                    result.Symlinks!.Add(name);
            }

            return result;
        }

        public RootedPath Realpath(RootedPath path)
        {
            string native = Native(path.AsString());
            if (native.Contains('\0'))
                return path;
            if (OperatingSystem.IsLinux())
            {
                string? resolved = LinuxReadPaths.Realpath(native);
                return resolved is null ? path : TsPath.RootedPathFromAbsolute(resolved);
            }

            try
            {
                string full = System.IO.Path.GetFullPath(native);
                string root = System.IO.Path.GetPathRoot(full)!;
                string resolved = root;
                foreach (string part in full[root.Length..].Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
                {
                    string candidate = System.IO.Path.Combine(resolved, part);
                    var attrs = System.IO.File.GetAttributes(candidate);
                    System.IO.FileSystemInfo entry = (attrs & FileAttributes.Directory) != 0 ? new DirectoryInfo(candidate) : new System.IO.FileInfo(candidate);
                    if (entry.LinkTarget != null)
                    {
                        var target = entry.ResolveLinkTarget(true);
                        if (target == null || !target.Exists)
                            return path;
                        resolved = target.FullName;
                    }
                    else if (OperatingSystem.IsWindows())
                    {
                        string parent = System.IO.Path.GetDirectoryName(candidate)!;
                        var actual = new DirectoryInfo(parent).EnumerateFileSystemInfos().FirstOrDefault(value => string.Equals(value.Name, part, StringComparison.OrdinalIgnoreCase));
                        resolved = actual?.FullName ?? candidate;
                    }
                    else
                        resolved = candidate;
                }

                return TsPath.RootedPathFromAbsolute(PathByteText.FromUnicode(resolved));
            }
            catch (Exception error)when (readFailure(error))
            {
                return path;
            }
        }
    }

    private sealed class osFileInfo(string name, System.IO.FileSystemInfo info) : FileInfo
    {
        public string Name() => name;
        public long Size() => info is System.IO.FileInfo file ? file.Length : 0;
        public FileMode Mode()
        {
            FileMode mode = (info.Attributes & FileAttributes.Directory) != 0 ? FileMode.Dir : FileMode.Regular;
            if (!OperatingSystem.IsWindows())
            {
                uint permissions = (uint)System.IO.File.GetUnixFileMode(info.FullName);
                mode |= (FileMode)(permissions & 0x1FF);
                if ((permissions & 0x200) != 0)
                    mode |= FileMode.Sticky;
                if ((permissions & 0x400) != 0)
                    mode |= FileMode.Setgid;
                if ((permissions & 0x800) != 0)
                    mode |= FileMode.Setuid;
            }

            return mode;
        }

        public DateTimeOffset ModTime() => new(info.LastWriteTimeUtc);
        public bool IsDir() => (info.Attributes & FileAttributes.Directory) != 0;
        public object? Sys() => info;
    }
}

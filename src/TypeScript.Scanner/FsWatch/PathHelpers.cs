// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// fswatch watcher.go path workers; POSIX Clean/Dir direct Go1.27.1
// internal/filepathlite/path.go/path_unix.go (licenses/Go-LICENSE.txt).
using TypeScript.Scanner;

namespace TypeScript.FsWatch;
internal static partial class FsWatch
{
    internal static bool IsInDirectoryOrSelf(ByteStringValue dir, ByteStringValue path)
    {
        if (dir.Length == 0)
            return false;
        if (path.EqualsValue(dir))
            return true;
        if (!path.StartsWith(dir))
            return false;
        var rest = path.Slice(dir.Length);
        if (rest.Length == 0)
            return false;
        return dir[dir.Length - 1] == '/' || rest[0] == '/';
    }

    internal static bool IsDirectChild(ByteStringValue dir, ByteStringValue path)
    {
        if (!path.StartsWith(dir))
            return false;
        var rest = path.Slice(dir.Length);
        if (rest.Length == 0 || rest[0] != '/')
            return false;
        rest = rest.Slice(1);
        return rest.Length > 0 && rest.IndexOf('/') < 0;
    }

    internal static ByteStringValue RebasePath(ByteStringValue path, ByteStringValue from, ByteStringValue to)
    {
        if (from.EqualsValue(to))
            return path;
        if (path.EqualsValue(from))
            return to;
        if (!path.StartsWith(from))
            return path;
        var suffix = path.Slice(from.Length);
        if (from.Length > 0 && from[from.Length - 1] == '/')
            return JoinPathSuffix(to, suffix);
        if (suffix.Length == 0 || suffix[0] != '/')
            return path;
        return JoinPathSuffix(to, suffix);
    }

    internal static ByteStringValue JoinPathSuffix(ByteStringValue root, ByteStringValue suffix)
    {
        if (suffix.Length == 0)
            return root;
        if (suffix[0] == '/')
            return ByteStringValue.Concat(root, root.Length > 0 && root[root.Length - 1] == '/' ? suffix.Slice(1) : suffix);
        if (root.Length > 0 && root[root.Length - 1] == '/')
            return ByteStringValue.Concat(root, suffix);
        return ByteStringValue.Concat(root, ByteStringValue.FromString("/"), suffix);
    }

    internal static ByteStringValue PhysicalDirFor(ByteStringValue dir)
    {
        var realpath = TypeScript.NativePath.NativePath.Realpath(dir);
        if (realpath is null || realpath.Value.EqualsValue(dir))
            return dir;
        return CanonicalizePath(CleanPath(realpath.Value));
    }

    private struct LazyPathBuffer
    {
        internal ByteStringValue Path;
        internal byte[]? Buffer;
        internal int W;
        internal char Index(int index) => Buffer is null ? (char)Path[index] : (char)Buffer[index];
        internal void Append(char value)
        {
            if (Buffer is null)
            {
                if (W < Path.Length && Path[W] == value)
                {
                    W++;
                    return;
                }

                Buffer = new byte[Path.Length];
                for (var i = 0; i < W; i++)
                    Buffer[i] = (byte)Path[i];
            }

            Buffer[W++] = (byte)value;
        }

        internal ByteStringValue String() => Buffer is null ? Path.Slice(0, W) : ByteStringValue.FromView(ByteStringView.CopyFrom(Buffer.AsSpan(0, W)));
    }

    internal static ByteStringValue CleanPath(ByteStringValue path)
    {
        if (path.Length == 0)
            return ByteStringValue.FromString(".");
        var rooted = path[0] == '/';
        var n = path.Length;
        var output = new LazyPathBuffer
        {
            Path = path
        };
        var r = 0;
        var dotdot = 0;
        if (rooted)
        {
            output.Append('/');
            r = dotdot = 1;
        }

        while (r < n)
        {
            if (path[r] == '/')
                r++;
            else if (path[r] == '.' && (r + 1 == n || path[r + 1] == '/'))
                r++;
            else if (path[r] == '.' && path[r + 1] == '.' && (r + 2 == n || path[r + 2] == '/'))
            {
                r += 2;
                if (output.W > dotdot)
                {
                    output.W--;
                    while (output.W > dotdot && output.Index(output.W) != '/')
                        output.W--;
                }
                else if (!rooted)
                {
                    if (output.W > 0)
                        output.Append('/');
                    output.Append('.');
                    output.Append('.');
                    dotdot = output.W;
                }
            }
            else
            {
                if (rooted && output.W != 1 || !rooted && output.W != 0)
                    output.Append('/');
                for (; r < n && path[r] != '/'; r++)
                    output.Append((char)path[r]);
            }
        }

        if (output.W == 0)
            output.Append('.');
        // POSIX postClean and FromSlash are genuine no-op owners.
        return output.String();
    }

    internal static ByteStringValue DirectoryPath(ByteStringValue path)
    {
        var i = path.Length - 1;
        while (i >= 0 && path[i] != '/')
            i--;
        return CleanPath(path.Slice(0, i + 1));
    }

    internal static bool IsAbsolutePath(ByteStringValue path) => path.Length > 0 && path[0] == '/';
}

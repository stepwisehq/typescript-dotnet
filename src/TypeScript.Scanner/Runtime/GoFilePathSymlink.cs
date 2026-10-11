// Copyright 2012 The Go Authors. BSD license: licenses/Go-LICENSE.txt.
// Selected Linux/POSIX port of Go go1.27.1 src/path/filepath/symlink.go.
// Windows volumes/isWindowsDot are unreachable at this explicit OS boundary.
// Errors are consumed as failure by nativepath/osvfs; invalid scalar encoding
// remains the separately named authored OS exception. Strings retain Go bytes.
using TypeScript.Vfs.OsVfs;
using TypeScript.Scanner;

namespace TypeScript.NativePath;
internal static class GoFilePath
{
    internal static string? WalkSymlinks(string path) => WalkSymlinks(ByteStringValue.FromString(path))?.Materialize();
    internal static ByteStringValue? WalkSymlinks(ByteStringValue path)
    {
        if (path.IsNull)
        {
            throw new NullReferenceException();
        }
        int volLen = path.StartsWith("/"u8) ? 1 : 0;
        ByteStringValue vol = path.Slice(0, volLen), dest = vol;
        int linksWalked = 0;
        for (int start = volLen, end = volLen; start < path.Length; start = end)
        {
            while (start < path.Length && path[start] == '/')
            {
                start++;
            }
            end = start;
            while (end < path.Length && path[end] != '/')
            {
                end++;
            }
            if (end == start)
            {
                break;
            }
            if (path.Slice(start, end).EqualsBytes("."u8))
            {
                continue;
            }
            if (path.Slice(start, end).EqualsBytes(".."u8))
            {
                int r = dest.Length - 1;
                while (r >= volLen && dest[r] != '/')
                {
                    r--;
                }
                if (r < volLen || dest.Slice(r + 1).EqualsBytes(".."u8))
                {
                    if (dest.Length > volLen)
                    {
                        dest = TsPath.pathConcat(dest, ByteStringValue.FromString("/"));
                    }
                    dest = TsPath.pathConcat(dest, ByteStringValue.FromString(".."));
                }
                else
                dest = dest.Slice(0, r);
                continue;
            }

            if (dest.Length > 0 && dest[dest.Length - 1] != '/')
            {
                dest = TsPath.pathConcat(dest, ByteStringValue.FromString("/"));
            }
            dest = TsPath.pathConcat(dest, path.Slice(start, end));
            ushort? mode = LinuxStat.PathMode(LinuxReadPaths.Native(dest), false);
            if (mode is null)
            {
                return null;
            }
            if ((mode.Value & 0xF000) != 0xA000)
            {
                if ((mode.Value & 0xF000) != 0x4000 && end < path.Length)
                {
                    return null;
                }
                continue;
            }

            if (++linksWalked > 255)
            {
                return null;
            }
            ByteStringValue? link = LinuxReadPaths.ReadLinkTarget(LinuxReadPaths.Native(dest));
            if (link is null)
            {
                return null;
            }
            path = TsPath.pathConcat(link.Value, path.Slice(end));
            if (link.Value.StartsWith("/"u8))
            {
                dest = vol = ByteStringValue.FromString("/");
                end = volLen = 1;
            }
            else
            {
                int r = dest.Length - 1;
                while (r >= volLen && dest[r] != '/')
                {
                    r--;
                }
                dest = r < volLen ? vol : dest.Slice(0, r);
                end = 0;
            }
        }

        // POSIX Clean adaptation: no drive/UNC/Windows volume producer reaches
        // this Linux walk. Preserve relative leading .. and absolute root.
        bool absolute = dest.StartsWith("/"u8);
        var parts = Slice<ByteStringValue>.Make(0);
        var remaining = dest;
        for (;;)
        {
            int slash = remaining.IndexOf('/');
            var part = slash < 0 ? remaining : remaining.Slice(0, slash);
            if (part.Length != 0 && !part.EqualsBytes("."u8))
            {
                if (part.EqualsBytes(".."u8) && parts.Length != 0 && !parts[parts.Length - 1].EqualsBytes(".."u8))
                {
                    parts = parts.Slice2(0, parts.Length - 1);
                }
                else if (!part.EqualsBytes(".."u8) || !absolute)
                parts = parts.Append(part);
            }
            if (slash < 0)
            {
                break;
            }
            remaining = remaining.Slice(slash + 1);
        }
        var builder = new GoBuilder();
        if (absolute)
        {
            builder.WriteByte('/');
        }
        for (int i = 0; i < parts.Length; i++)
        {
            if (i > 0)
            {
                builder.WriteByte('/');
            }
            builder.WriteString(parts[i]);
        }
        var clean = builder.Value();
        return clean.Length == 0 ? ByteStringValue.FromString(".") : clean;
    }
}

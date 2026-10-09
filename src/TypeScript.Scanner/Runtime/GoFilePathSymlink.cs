// Copyright 2012 The Go Authors. BSD license: licenses/Go-LICENSE.txt.
// Selected Linux/POSIX port of Go go1.27.1 src/path/filepath/symlink.go.
// Windows volumes/isWindowsDot are unreachable at this explicit OS boundary.
// Errors are consumed as failure by nativepath/osvfs; invalid scalar encoding
// remains the separately named authored OS exception. Strings retain Go bytes.
using TypeScript.Vfs.OsVfs;

namespace TypeScript.NativePath;
internal static class GoFilePath
{
    internal static string? WalkSymlinks(string path)
    {
        int volLen = path.StartsWith('/') ? 1 : 0;
        string vol = path[..volLen], dest = vol;
        int linksWalked = 0;
        for (int start = volLen, end = volLen; start < path.Length; start = end)
        {
            while (start < path.Length && path[start] == '/')
                start++;
            end = start;
            while (end < path.Length && path[end] != '/')
                end++;
            if (end == start)
                break;
            if (path[start..end] == ".")
                continue;
            if (path[start..end] == "..")
            {
                int r = dest.Length - 1;
                while (r >= volLen && dest[r] != '/')
                    r--;
                if (r < volLen || dest[(r + 1)..] == "..")
                {
                    if (dest.Length > volLen)
                        dest += "/";
                    dest += "..";
                }
                else
                    dest = dest[..r];
                continue;
            }

            if (dest.Length > 0 && dest[^1] != '/')
                dest += "/";
            dest += path[start..end];
            ushort? mode = LinuxStat.PathMode(LinuxReadPaths.Native(dest), false);
            if (mode is null)
                return null;
            if ((mode.Value & 0xF000) != 0xA000)
            {
                if ((mode.Value & 0xF000) != 0x4000 && end < path.Length)
                    return null;
                continue;
            }

            if (++linksWalked > 255)
                return null;
            string? link = LinuxReadPaths.ReadLinkTarget(LinuxReadPaths.Native(dest));
            if (link is null)
                return null;
            path = link + path[end..];
            if (link.StartsWith('/'))
            {
                dest = vol = "/";
                end = volLen = 1;
            }
            else
            {
                int r = dest.Length - 1;
                while (r >= volLen && dest[r] != '/')
                    r--;
                dest = r < volLen ? vol : dest[..r];
                end = 0;
            }
        }

        // POSIX Clean adaptation: no drive/UNC/Windows volume producer reaches
        // this Linux walk. Preserve relative leading .. and absolute root.
        bool absolute = dest.StartsWith('/');
        var parts = new List<string>();
        foreach (string part in dest.Split('/'))
        {
            if (part is "" or ".")
                continue;
            if (part == ".." && parts.Count != 0 && parts[^1] != "..")
                parts.RemoveAt(parts.Count - 1);
            else if (part != ".." || !absolute)
                parts.Add(part);
        }

        string clean = (absolute ? "/" : "") + string.Join('/', parts);
        return clean == "" ? "." : clean;
    }
}

// Source-mapped Linux realpath closure from internal/nativepath/realpath_linux.go
// at fed0bf24149fb1ed36039212648bafdafc1ea10e. TypeScript Apache 2.0 license.
// Original PathError objects are consumed only as failure by osvfs.Realpath;
// this internal adapter returns null for that failure, preserving its fallback.
using TypeScript.Vfs.OsVfs;
using TypeScript.Scanner;

namespace TypeScript.NativePath;
internal static class NativePath
{
    private const string procSelfFD = "/proc/self/fd/";
    private static readonly Lazy<bool> hasProcSelfFD = new(() => LinuxStat.PathMode(procSelfFD, true)is not null);
    internal static ByteStringValue? Realpath(ByteStringValue bytes)
    {
        if (!hasProcSelfFD.Value)
            return GoFilePath.WalkSymlinks(bytes);
        int descriptor = LinuxReadPaths.OpenPath(LinuxReadPaths.Native(bytes));
        if (descriptor < 0)
            return null;
        try
        {
            return LinuxReadPaths.ReadLinkTarget(procSelfFD + descriptor.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        finally
        {
            LinuxReadPaths.ClosePath(descriptor);
        }
    }
}

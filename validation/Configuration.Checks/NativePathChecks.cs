// Selected Go go1.27.1 filepath assertions over the actual POSIX fallback.
// Invocation is adapted to the retained private walker. Its null failure channel
// replaces Go error values; errno/messages and process-wide chdir are not claimed.
using TypeScript.NativePath;
using TypeScript.Scanner;
using TypeScript.Vfs.OsVfs;

namespace Configuration.Checks;
public static class NativePathChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["Adapted.TestEvalSymlinks.AbsolutePOSIX", "Adapted.TestEvalSymlinksIsNotExist", "Adapted.TestIssue13582", "Adapted.TestIssue29372", "Adapted.TestEvalSymlinksAboveRoot", "Adapted.TestEvalSymlinksTooManyLinks", "Authored.NativeFastpathAndFallbackLimits"];

    public static void Run(string name)
    {
        if (!OperatingSystem.IsLinux() || System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture != System.Runtime.InteropServices.Architecture.X64)
            throw new PlatformNotSupportedException("Selected nativepath controls require Linux x64.");
        string temporary = Path.Combine(Path.GetTempPath(), "configuration-nativepath-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        string root = Walk(temporary) ?? throw new IOException("Temporary directory cannot be resolved.");
        try
        {
            switch (name)
            {
                case "Adapted.TestEvalSymlinks.AbsolutePOSIX":
                    Farm(root);
                    break;
                case "Adapted.TestEvalSymlinksIsNotExist":
                    Missing(root);
                    break;
                case "Adapted.TestIssue13582":
                    AbsoluteTargets(root);
                    break;
                case "Adapted.TestIssue29372":
                    NotDirectory(root);
                    break;
                case "Adapted.TestEvalSymlinksAboveRoot":
                    AboveRoot(root);
                    break;
                case "Adapted.TestEvalSymlinksTooManyLinks":
                    Loop(root);
                    break;
                case "Authored.NativeFastpathAndFallbackLimits":
                    Limits(root);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(name));
            }
        }
        finally
        {
            Directory.Delete(temporary, true);
        }
    }

    private static readonly (string Path, string Destination)[] farmDirectories = [("test", ""), ("test/dir", ""), ("test/dir/link3", "../../"), ("test/link1", "../test"), ("test/link2", "dir"), ("test/linkabs", "/"), ("test/link4", "../test2"), ("test2", "test/dir"), ("src", ""), ("src/pool", ""), ("src/pool/test", ""), ("src/versions", ""), ("src/versions/current", "../../version"), ("src/versions/v1", ""), ("src/versions/v1/modules", ""), ("src/versions/v1/modules/test", "../../../pool/test"), ("version", "src/versions/v1")];
    private static readonly (string Path, string Destination)[] farmCases = [("test", "test"), ("test/dir", "test/dir"), ("test/dir/../..", "."), ("test/link1", "test"), ("test/link2", "test/dir"), ("test/link1/dir", "test/dir"), ("test/link2/..", "test"), ("test/dir/link3", "."), ("test/link2/link3/test", "test"), ("test/linkabs", "/"), ("test/link4/..", "test"), ("src/versions/current/modules/test", "src/pool/test")];
    private static void Farm(string root)
    {
        foreach (var row in farmDirectories)
        {
            string path = root + "/" + row.Path;
            if (row.Destination == "")
                Directory.CreateDirectory(path);
            else
                Directory.CreateSymbolicLink(path, row.Destination);
        }

        foreach (var row in farmCases)
        {
            string expected = row.Destination == "/" ? "/" : Path.GetFullPath(root + "/" + row.Destination);
            Check.Equal(Walk(root + "/" + row.Path), expected, "Original absolute symlink farm " + row.Path);
        }
    }

    private static void Missing(string root)
    {
        Check.Equal(Walk(root + "/notexist"), null, "Original missing path fails");
        File.CreateSymbolicLink(root + "/link", "notexist");
        Check.Equal(Walk(root + "/link"), null, "Original dangling target fails");
    }

    private static void AbsoluteTargets(string root)
    {
        string dir = root + "/dir", linkToDir = root + "/link_to_dir";
        Directory.CreateDirectory(dir);
        Directory.CreateSymbolicLink(linkToDir, dir);
        string file = linkToDir + "/file", link1 = linkToDir + "/link1", link2 = linkToDir + "/link2";
        File.WriteAllText(file, "");
        File.CreateSymbolicLink(link1, file);
        File.CreateSymbolicLink(link2, link1);
        foreach (var row in new[]
        {
            (dir, dir),
            (linkToDir, dir),
            (file, dir + "/file"),
            (link1, dir + "/file"),
            (link2, dir + "/file")
        }

        )
            Check.Equal(Walk(row.Item1), row.Item2, "Original absolute target " + row.Item1);
    }

    private static void NotDirectory(string root)
    {
        string file = root + "/file.txt";
        File.WriteAllText(file, "");
        foreach (string suffix in new[]
        {
            "/",
            "//",
            "/.",
            "//.",
            "/..",
            "//.."
        }

        )
            Check.Equal(Walk(file + suffix), null, "Original non-directory-before-clean failure " + suffix);
    }

    private static void AboveRoot(string root)
    {
        Directory.CreateDirectory(root + "/a");
        Directory.CreateSymbolicLink(root + "/b", root + "/a");
        File.WriteAllText(root + "/a/file", "");
        int count = root.Count(character => character == '/');
        foreach (int length in new[]
        {
            count,
            count + 1,
            count + 2
        }

        )
        {
            string input = root + "/" + string.Join('/', Enumerable.Repeat("..", length)) + "/" + root[1..] + "/b/file";
            string? result = Walk(input);
            Check.True(result is not null, "Original above-root success " + length);
            Check.True(result!.EndsWith("a/file", StringComparison.Ordinal), "Original above-root suffix " + length);
        }
    }

    private static void Loop(string root)
    {
        string dir = root + "/dir";
        Directory.CreateSymbolicLink(dir, dir);
        Check.Equal(Walk(dir), null, "Original self-link produces failure");
    }

    private static void Limits(string root)
    {
        // Source-directed boundary assertions, not rows from filepath's test file.
        File.WriteAllText(root + "/target", "");
        for (int index = 256; index >= 1; index--)
            File.CreateSymbolicLink(root + "/link" + index, index == 256 ? "target" : "link" + (index + 1));
        Check.Equal(Walk(root + "/link2"), root + "/target", "Fallback accepts exactly 255 links");
        Check.Equal(Walk(root + "/link1"), null, "Fallback rejects 256 links");
        // This assertion is about the genuine proc fastpath, not generic EvalSymlinks.
        Check.True(Directory.Exists("/proc/self/fd"), "Native fastpath fixture requires proc descriptors");
        string path = root + "/link216"; // 41 links: kernel open limit differs from fallback's 255.
        Check.Equal(Walk(path), root + "/target", "Fallback resolves 41 links");
        Check.Equal(NativePath.Realpath(ByteStringValue.FromString(GoString.ToUtf8(path))), null, "Original Linux O_PATH fastpath fails 41 links");
        var input = RootedPath.FromUnicode(path);
        Check.Equal(OsVfs.FS().Realpath(input), input, "Actual OS consumer retains input on native failure");
    }

    private static string? Walk(string unicodePath)
    {
        string? value = GoFilePath.WalkSymlinks(GoString.ToUtf8(unicodePath));
        return value is null ? null : GoString.ToString(value);
    }
}

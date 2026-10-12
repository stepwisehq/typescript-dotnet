// Assertion-preserving port of the seven read-side original cachedvfs tests at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Parallel test scheduling becomes sequential;
// all original calls, cache transitions and exact call-count assertions are retained.
using Configuration.Checks;
using TypeScript.Scanner;
using TypeScript.Vfs;
using CachedFS = TypeScript.Vfs.CachedVfs.FS;
using VfsInfo = TypeScript.Vfs.FileInfo;

namespace Program.Checks;

public static class CachedVfsChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    [
        "TestDirectoryExists", "TestFileExists", "TestGetAccessibleEntries",
        "TestRealpath", "TestStat", "TestReadFile", "TestCaseSensitivity",
    ];

    public static void Run(string name)
    {
        CountingFileSystem underlying = new();
        CachedFS cached = CachedFS.From(underlying);
        Action invoke = name switch
        {
            "TestDirectoryExists" => () => cached.DirectoryExists(Directory("/some/path")),
            "TestFileExists" => () => cached.FileExists(File("/some/path/file.txt")),
            "TestGetAccessibleEntries" => () => cached.GetAccessibleEntries(Directory("/some/path")),
            "TestRealpath" => () => cached.Realpath(Path("/some/path")),
            "TestStat" => () => cached.Stat(Path("/some/path")),
            "TestReadFile" => () => cached.ReadFile(File("/some/path/file.txt")),
            "TestCaseSensitivity" => () => cached.CaseSensitivity(),
            _ => throw new ArgumentException("Unknown original cachedvfs test.", nameof(name)),
        };

        invoke();
        AssertCount(underlying, name, 1);
        bool forwarded = name is "TestReadFile" or "TestCaseSensitivity";
        invoke();
        AssertCount(underlying, name, forwarded ? 2 : 1);
        cached.ClearCache();
        invoke();
        AssertCount(underlying, name, forwarded ? 3 : 2);
        if (!forwarded)
        {
            switch (name)
            {
                case "TestDirectoryExists": cached.DirectoryExists(Directory("/other/path")); break;
                case "TestFileExists": cached.FileExists(File("/other/path/file.txt")); break;
                case "TestGetAccessibleEntries": cached.GetAccessibleEntries(Directory("/other/path")); break;
                case "TestRealpath": cached.Realpath(Path("/other/path")); break;
                case "TestStat": cached.Stat(Path("/other/path")); break;
            }
            AssertCount(underlying, name, 3);
        }
        cached.DisableAndClearCache();
        invoke();
        AssertCount(underlying, name, 4);
        invoke();
        AssertCount(underlying, name, 5);
        cached.Enable();
        invoke();
        AssertCount(underlying, name, 6);
        invoke();
        AssertCount(underlying, name, forwarded ? 7 : 6);
    }

    private static RootedDirectoryPath Directory(string value) => RootedDirectoryPath.FromGoString(value);
    private static RootedFilePath File(string value) => RootedFilePath.FromGoString(value);
    private static RootedPath Path(string value) => RootedPath.FromGoString(value);
    private static void AssertCount(CountingFileSystem fs, string name, int expected) =>
        Check.Equal(fs.Calls[name], expected, name + " underlying calls");

    // Finite original vfsmock call-count adapter over the existing read-side fixture.
    // No production cache or mutation behavior is reproduced in this mock.
    private sealed class CountingFileSystem : IFS
    {
        private readonly ConfigurationFileSystem fs = new(
            [new ConfigurationFileSystem.Input("/some/path/file.txt", "hello world")]);
        internal readonly Dictionary<string, int> Calls = [];
        private void Called(string name) => Calls[name] = Calls.GetValueOrDefault(name) + 1;
        public bool DirectoryExists(RootedDirectoryPath path)
        {
            Called("TestDirectoryExists");
            return fs.DirectoryExists(path);
        }
        public bool FileExists(RootedFilePath path)
        {
            Called("TestFileExists");
            return fs.FileExists(path);
        }
        public Entries GetAccessibleEntries(RootedDirectoryPath path)
        {
            Called("TestGetAccessibleEntries");
            return fs.GetAccessibleEntries(path);
        }
        public RootedPath Realpath(RootedPath path)
        {
            Called("TestRealpath");
            return fs.Realpath(path);
        }
        public VfsInfo? Stat(RootedPath path)
        {
            Called("TestStat");
            return fs.Stat(path);
        }
        public Exception? WriteFile(RootedFilePath path, string data) => fs.WriteFile(path, data);
        public (string Contents, bool Ok) ReadFile(RootedFilePath path)
        {
            Called("TestReadFile");
            return fs.ReadFile(path);
        }
        public CaseSensitivity CaseSensitivity()
        {
            Called("TestCaseSensitivity");
            return fs.CaseSensitivity();
        }
    }
}

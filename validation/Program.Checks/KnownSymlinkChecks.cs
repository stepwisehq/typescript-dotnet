// Complete original knownsymlinks_test.go domains. Go goroutines/channels map
// to ten Tasks and a bounded liveness wait; original raw rune suffixes are retained.
using Configuration.Checks;
using TypeScript.Modules;
using TypeScript.Scanner;
using TypeScript.Symlinks;

namespace Program.Checks;

public static class KnownSymlinkChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    [
        "TestNewKnownSymlink", "TestSetDirectory", "TestKnownDirectoryLinkPreservesChildSpelling",
        "TestSetFile", "TestProcessResolution", "TestGuessDirectorySymlink",
        "TestIsNodeModulesOrScopedPackageDirectory", "TestSetSymlinksFromResolutions",
        "TestKnownSymlinksThreadSafety",
    ];

    private static RootedDirectoryPath Directory(string text) => RootedDirectoryPath.FromGoString(text);
    private static RootedFilePath File(string text) => RootedFilePath.FromGoString(text);
    private static PathKey Key(string text) => CaseSensitivity.CaseSensitive.PathKey(RootedPath.FromGoString(text));

    public static void Run(string name)
    {
        KnownSymlinks cache = KnownSymlinks.NewKnownSymlinks(CaseSensitivity.CaseSensitive);
        switch (name)
        {
            case "TestNewKnownSymlink":
                Check.True(cache is not null, "non-nil cache");
                Check.Equal(cache!.caseSensitivity, CaseSensitivity.CaseSensitive, "stored case sensitivity");
                break;
            case "TestSetDirectory":
            {
                KnownDirectoryLink real = new() { Real = Directory("/real/path"), RealPath = Key("/real/path") };
                cache.SetDirectory(Directory("/test/symlink"), Key("/test/symlink"), real);
                var (stored, ok) = cache.Directories().Load(Key("/test/symlink"));
                Check.True(ok, "directory stored");
                Check.Equal(stored!.Real, real.Real, "real directory");
                Check.Equal(stored.RealPath, real.RealPath, "real directory key");
                Check.Equal(stored.Symlink, Directory("/test/symlink"), "symlink spelling");
                var (set, hasSet) = cache.DirectoriesByRealpath().Load(real.RealPath);
                Check.True(hasSet && set.Size() != 0, "realpath mapping created");
                Check.True(set.Has(Directory("/test/symlink")), "mapping contains symlink");
                break;
            }
            case "TestKnownDirectoryLinkPreservesChildSpelling":
            {
                cache = KnownSymlinks.NewKnownSymlinks(CaseSensitivity.CaseInsensitive);
                RootedDirectoryPath symlink = Directory("/Project/Node_Modules/pkg");
                PathKey path = CaseSensitivity.CaseInsensitive.PathKey(symlink.AsPath());
                cache.SetDirectory(symlink, path, new() { Real = Directory("/Real/Package"), RealPath = PathKey.FromGoString("/real/package") });
                var (link, ok) = cache.Directories().Load(path);
                Check.True(ok, "directory link");
                var (resolved, child) = link!.ResolveFilePath(File("/PROJECT/node_modules/pkg/Src/File.ts"), CaseSensitivity.CaseInsensitive);
                Check.True(child, "child resolves");
                Check.Equal(resolved, File("/Real/Package/Src/File.ts"), "child spelling");
                break;
            }
            case "TestSetFile":
                cache.SetFile(File("/test/symlink/file.ts"), Key("/test/symlink/file.ts"), File("/real/path/file.ts"));
                AssertFile(cache, "/test/symlink/file.ts", "/real/path/file.ts");
                break;
            case "TestProcessResolution":
                cache.ProcessResolution(default, default);
                cache.ProcessResolution(File("/original"), default);
                cache.ProcessResolution(default, File("/resolved"));
                cache.ProcessResolution(File("/test/original/file.ts"), File("/test/resolved/file.ts"));
                AssertFile(cache, "/test/original/file.ts", "/test/resolved/file.ts");
                break;
            case "TestGuessDirectorySymlink":
                Guess(cache);
                break;
            case "TestIsNodeModulesOrScopedPackageDirectory":
                foreach (var row in new (string Directory, bool Want)[]
                { ("node_modules", true), ("@scope", true), ("src", false), ("", false), ("NODE_MODULES", false), ("@SCOPE", true) })
                    Check.Equal(cache.IsNodeModulesOrScopedPackageDirectory(row.Directory), row.Want, row.Directory);
                break;
            case "TestSetSymlinksFromResolutions":
                Resolutions(cache);
                break;
            case "TestKnownSymlinksThreadSafety":
                Concurrent(cache);
                break;
            default:
                throw new ArgumentException("Unknown original known-symlink test.", nameof(name));
        }
    }

    private static void AssertFile(KnownSymlinks cache, string original, string real)
    {
        var (stored, ok) = cache.Files().Load(Key(original));
        Check.True(ok, "file stored " + original);
        Check.Equal(stored, File(real), "resolved file " + original);
    }

    private static void Guess(KnownSymlinks cache)
    {
        (string Name, string A, string B, string Cwd, string Resolved, string Original)[] rows =
        [
            ("identical paths", "/test/path/file.ts", "/test/path/file.ts", "/test/dir", "/", "/"),
            ("different files same directory", "/test/path/file1.ts", "/test/path/file2.ts", "/test/dir", "", ""),
            ("different directories", "/test/path1/file.ts", "/test/path2/file.ts", "/test/dir", "/test/path1", "/test/path2"),
            ("node_modules paths", "/test/node_modules/pkg/file.ts", "/test/node_modules/pkg/file.ts", "/test/dir", "/test/node_modules/pkg", "/test/node_modules/pkg"),
            ("scoped package paths", "/test/node_modules/@scope/pkg/file.ts", "/test/node_modules/@scope/pkg/file.ts", "/test/dir", "/test/node_modules/@scope/pkg", "/test/node_modules/@scope/pkg"),
        ];
        foreach (var row in rows)
        {
            var (resolved, original) = cache.GuessDirectorySymlinkFromFilePaths(
                TsPath.ToRootedFilePath(row.A, Directory(row.Cwd)), TsPath.ToRootedFilePath(row.B, Directory(row.Cwd)));
            Check.Equal(resolved.AsString(), row.Resolved, row.Name + " resolved");
            Check.Equal(original.AsString(), row.Original, row.Name + " original");
        }
    }

    private static void Resolutions(KnownSymlinks cache)
    {
        (string Original, string Resolved, string Name, ResolutionMode Mode, PathKey Path)[] rows =
        [
            ("/test/original/file1.ts", "/test/resolved/file1.ts", "module1", ResolutionMode.None, Key("/test/source.ts")),
            ("/test/original/file2.ts", "/test/resolved/file2.ts", "module2", ResolutionMode.None, Key("/test/source.ts")),
        ];
        cache.SetSymlinksFromResolutions((callback, _) =>
        {
            foreach (var row in rows)
                callback(new ResolvedModule { OriginalPath = File(row.Original), ResolvedFileName = File(row.Resolved) }, row.Name, row.Mode, row.Path);
        }, (_, _) => { });
        foreach (var row in rows) AssertFile(cache, row.Original, row.Resolved);
    }

    private static void Concurrent(KnownSymlinks cache)
    {
        Task[] workers = Enumerable.Range(0, 10).Select(id => Task.Run(() =>
        {
            string suffix = ((char)id).ToString(); // Original string(rune(id)), including raw NUL/control bytes.
            string original = "/test/symlink" + suffix;
            KnownDirectoryLink real = new() { Real = Directory("/real/path" + suffix), RealPath = Key("/real/path" + suffix) };
            cache.SetDirectory(Directory(original), Key(original), real);
            var (stored, ok) = cache.Directories().Load(Key(original));
            Check.True(ok, "concurrent stored worker " + id);
            Check.Equal(stored!.Real, real.Real, "concurrent real worker " + id);
        })).ToArray();
        Check.True(Task.WaitAll(workers, TimeSpan.FromSeconds(30)), "concurrent workers complete");
        Check.Equal(cache.Directories().Size(), 10, "all ten directories stored");
    }
}

// Original bundled/bundled_test.go assertions. Embedded-resource packaging and
// source-tree testing paths are the explicit managed representation adapters.
using Configuration.Checks;
using TypeScript.Scanner;
using TypeScript.Vfs;
using OsVfs = TypeScript.Vfs.OsVfs.OsVfs;
using Bundled = TypeScript.Bundled.Bundled;

namespace Program.Checks;

public static class BundledChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestTestingLibPath", "TestEmbeddedLibs"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestTestingLibPath":
                string path = Bundled.TestingLibPath();
                Check.True(Directory.Exists(path), "original testing library directory stat succeeds");
                Check.True(File.Exists(Path.Combine(path, "lib.d.ts")), "original lib.d.ts stat succeeds");
                break;
            case "TestEmbeddedLibs":
                var fs = Bundled.WrapFS(OsVfs.FS());
                var files = fs.GetAccessibleEntries(Bundled.LibPath()).Files;
                GoSlices.SortFunc(files, static (left, right) => ByteStringKey.FromValue(left).CompareTo(ByteStringKey.FromValue(right)));
                Check.Sequence(GoSlices.ToListOrNull(files)?.Select(static name => name.Materialize()!).ToArray(), GoSlices.ToListOrNull(Bundled.LibNames), "complete original embedded names");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }
}

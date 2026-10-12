// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Original watcher_test.go denied-subdirectory assertions; actual Unix permissions.
using TypeScript.Scanner;
using TypeScript.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    private static void SetOriginalUnixFileMode(string path, UnixFileMode mode)
    {
        // Preserve the original outer Windows skip; this repeats admission at
        // the actual delegate boundary so platform flow remains explicit.
        if (OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("chmod is not meaningful on Windows");
        File.SetUnixFileMode(path, mode);
    }

    public static void TestNonRecursiveWithDeniedSubdir()
    {
        if (OperatingSystem.IsWindows())
            throw new OriginalSkip("chmod is not meaningful on Windows");
        RunForEachWatcher(nameof(TestNonRecursiveWithDeniedSubdir), (fixture, watcher) =>
        {
            string dir = fixture.NewTmpDir(), denied = Path.Combine(dir, "denied");
            MakeDirectory(denied);
            FileAction(() => SetOriginalUnixFileMode(denied, UnixFileMode.None));
            fixture.Cleanup(() =>
            {
                try
                {
                    SetOriginalUnixFileMode(denied, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                }
                catch (IOException)
                {
                }
            });
            var(recorder, _) = fixture.Subscribe(watcher, dir, Slice<IWatchOption>.Make(0));
            string file = UniqueName(dir);
            WriteFile(file, "hello");
            ExpectEventSequence(recorder, new WantEvent(EventKind.Update, Text(file)));
        });
    }

    public static void TestRecursiveWithDeniedSubdir()
    {
        if (OperatingSystem.IsWindows())
            throw new OriginalSkip("chmod is not meaningful on Windows");
        RunForEachWatcher(nameof(TestRecursiveWithDeniedSubdir), (fixture, watcher) =>
        {
            string dir = fixture.NewTmpDir(), accessible = Path.Combine(dir, "ok"), denied = Path.Combine(dir, "denied");
            MakeDirectory(accessible);
            MakeDirectory(denied);
            FileAction(() => SetOriginalUnixFileMode(denied, UnixFileMode.None));
            fixture.Cleanup(() =>
            {
                try
                {
                    SetOriginalUnixFileMode(denied, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                }
                catch (IOException)
                {
                }
            });
            var(recorder, _) = fixture.Subscribe(watcher, dir);
            string file = Path.Combine(accessible, "test.txt");
            WriteFile(file, "hello");
            ExpectContains(recorder, EventKind.Update, Text(file));
        });
    }
}

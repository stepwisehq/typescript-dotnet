// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Original internal/fswatch/walkdir_test.go: all eight assertion functions,
// each exercised against both original native and generic owners.
using System.Runtime.InteropServices;
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchWalkDirChecks
{
    public static readonly string[] Names = [nameof(TestWalkDirDoesNotFollowSymlinkedDir), nameof(TestWalkDirDoesNotFollowRootSymlinkedDir), nameof(TestWalkDirIgnoresUnreadableSubdir), nameof(TestWalkDirMissingDir), nameof(TestWalkDirNotADir), nameof(TestWalkDirEntries), nameof(TestWalkDirCallback), nameof(TestWalkDirCallbackError)];
    private delegate Exception? Walk(ByteStringValue directory, bool recursive, Func<ByteStringValue, bool, Exception?>? callback);
    private static readonly Walk[] Walkers = [FsWatch.WalkDir, FsWatch.WalkDirGeneric];
    private static ByteStringValue Text(string display) => ByteStringValue.FromString(GoString.ToUtf8(display));
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class DirectoryOwner : IDisposable
    {
        internal readonly string Root = Directory.CreateTempSubdirectory("fswatch-").FullName;
        internal string PathOf(params string[] parts) => Path.Combine([Root, ..parts]);
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    private static Dictionary<string, bool> Collect(Walk walk, string root)
    {
        var found = new Dictionary<string, bool>(StringComparer.Ordinal);
        var error = walk(Text(root), true, (path, directory) =>
        {
            found[GoString.ToString(path.Materialize()!)] = directory;
            return null;
        });
        Check(error is null, "walk must succeed: " + error?.Message);
        return found;
    }

    private static void TestWalkDirDoesNotFollowSymlinkedDir(Walk walk)
    {
        using var root = new DirectoryOwner();
        using var other = new DirectoryOwner();
        string target = other.PathOf("target"), link = root.PathOf("link");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "child"), "hidden");
        Directory.CreateSymbolicLink(link, target);
        var found = Collect(walk, root.Root);
        Check(found.TryGetValue(link, out bool directory), "symlink missing from walk");
        Check(!directory, "symlink treated as a directory");
        Check(!found.ContainsKey(Path.Combine(link, "child")), "walk followed symlink directory");
    }

    private static void TestWalkDirDoesNotFollowRootSymlinkedDir(Walk walk)
    {
        using var root = new DirectoryOwner();
        using var other = new DirectoryOwner();
        string target = other.PathOf("target"), link = root.PathOf("link");
        Directory.CreateDirectory(target);
        Directory.CreateSymbolicLink(link, target);
        Check(walk(Text(link), true, null)is not null, "expected error for root symlink directory");
    }

    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint GetEffectiveUserID();
    private static void TestWalkDirIgnoresUnreadableSubdir(Walk walk)
    {
        if (OperatingSystem.IsWindows())
        {
            Console.WriteLine("ORIGINAL_SKIP TestWalkDirIgnoresUnreadableSubdir: Windows does not enforce POSIX directory permission bits");
            return;
        }

        if (GetEffectiveUserID() == 0)
        {
            Console.WriteLine("ORIGINAL_SKIP TestWalkDirIgnoresUnreadableSubdir: root can read directories regardless of mode bits");
            return;
        }

        using var root = new DirectoryOwner();
        string denied = root.PathOf("denied"), child = Path.Combine(denied, "child");
        Directory.CreateDirectory(denied);
        File.SetUnixFileMode(denied, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        File.WriteAllText(child, "hidden");
        File.SetUnixFileMode(denied, 0);
        try
        {
            var found = Collect(walk, root.Root);
            Check(!found.ContainsKey(denied), "unreadable directory should be ignored");
            Check(!found.ContainsKey(child), "unreadable child should be ignored");
        }
        finally
        {
            File.SetUnixFileMode(denied, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static void TestWalkDirMissingDir(Walk walk)
    {
        using var root = new DirectoryOwner();
        Check(walk(Text(root.PathOf("nonexistent")), true, null)is not null, "expected error for missing directory");
    }

    private static void TestWalkDirNotADir(Walk walk)
    {
        using var root = new DirectoryOwner();
        string file = root.PathOf("file");
        File.WriteAllText(file, "x");
        Check(walk(Text(file), true, null)is not null, "expected error for non-directory");
    }

    private static void TestWalkDirEntries(Walk walk)
    {
        using var root = new DirectoryOwner();
        string sub = root.PathOf("sub");
        File.WriteAllText(root.PathOf("a.txt"), "a");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "b.txt"), "b");
        var found = Collect(walk, root.Root);
        Check(found.ContainsKey(root.PathOf("a.txt")), "missing a.txt");
        Check(found.ContainsKey(sub), "missing sub/");
        Check(found.ContainsKey(Path.Combine(sub, "b.txt")), "missing sub/b.txt");
    }

    private static void TestWalkDirCallback(Walk walk)
    {
        using var root = new DirectoryOwner();
        string sub = root.PathOf("sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "f.txt"), "f");
        var directories = new List<ByteStringValue>();
        var files = new List<ByteStringValue>();
        var error = walk(Text(root.Root), true, (path, directory) =>
        {
            (directory ? directories : files).Add(path);
            return null;
        });
        Check(error is null, "walk must succeed");
        Check(directories.Count >= 2, "expected at least two directories");
        Check(files.Count >= 1, "expected at least one file");
    }

    private static void TestWalkDirCallbackError(Walk walk)
    {
        using var root = new DirectoryOwner();
        File.WriteAllText(root.PathOf("a.txt"), "a");
        var sentinel = new IOException("stop");
        var error = walk(Text(root.Root), true, (_, _) => sentinel);
        Check(ReferenceEquals(error, sentinel), "expected sentinel error");
    }

    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        foreach (var walk in Walkers)
            switch (name)
            {
                case nameof(TestWalkDirDoesNotFollowSymlinkedDir):
                    TestWalkDirDoesNotFollowSymlinkedDir(walk);
                    break;
                case nameof(TestWalkDirDoesNotFollowRootSymlinkedDir):
                    TestWalkDirDoesNotFollowRootSymlinkedDir(walk);
                    break;
                case nameof(TestWalkDirIgnoresUnreadableSubdir):
                    TestWalkDirIgnoresUnreadableSubdir(walk);
                    break;
                case nameof(TestWalkDirMissingDir):
                    TestWalkDirMissingDir(walk);
                    break;
                case nameof(TestWalkDirNotADir):
                    TestWalkDirNotADir(walk);
                    break;
                case nameof(TestWalkDirEntries):
                    TestWalkDirEntries(walk);
                    break;
                case nameof(TestWalkDirCallback):
                    TestWalkDirCallback(walk);
                    break;
                case nameof(TestWalkDirCallbackError):
                    TestWalkDirCallbackError(walk);
                    break;
                default:
                    throw new ArgumentException(name);
            }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// All eight pinned internal/vfs/walkdir_test.go bodies, every original assertion.
// Read-only host wrapper substitutes the original wrapvfs.Replacements only.
using TypeScript.Scanner;
using TypeScript.Vfs;
using V = TypeScript.Vfs.Vfs;
using Mode = TypeScript.Vfs.FileMode;

namespace Configuration.Checks;
public static class WalkDirChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestWalkDir", "TestWalkDirDoesNotFollowRootSymlink", "TestWalkDirReportsRootFileSymlink", "TestWalkDirSkipDir", "TestWalkDirSkipAll", "TestWalkDirConsumesSkipDirForRootFile", "TestWalkDirConsumesSkipForMissingRoot", "TestWalkDirUsesSymlinkMetadataWithoutRealpathCalls"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestWalkDir":
                TestWalkDir();
                break;
            case "TestWalkDirDoesNotFollowRootSymlink":
                TestWalkDirDoesNotFollowRootSymlink();
                break;
            case "TestWalkDirReportsRootFileSymlink":
                TestWalkDirReportsRootFileSymlink();
                break;
            case "TestWalkDirSkipDir":
                TestWalkDirSkipDir();
                break;
            case "TestWalkDirSkipAll":
                TestWalkDirSkipAll();
                break;
            case "TestWalkDirConsumesSkipDirForRootFile":
                TestWalkDirConsumesSkipDirForRootFile();
                break;
            case "TestWalkDirConsumesSkipForMissingRoot":
                TestWalkDirConsumesSkipForMissingRoot();
                break;
            case "TestWalkDirUsesSymlinkMetadataWithoutRealpathCalls":
                TestWalkDirUsesSymlinkMetadataWithoutRealpathCalls();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown original walker test.");
        }
    }

    private static RootedPath P(string path) => RootedPath.FromGoString(path);
    private static void NilError(Exception? error) => Check.True(error is null, "WalkDir expected nil error: " + error);
    private static void TestWalkDir()
    {
        var basis = new ConfigurationFileSystem([new("/root/a.ts"), new("/root/dir/b.ts"), new("/root/link/hidden.ts"), new("/target/hidden.ts")]);
        var host = new ReadFileSystemOverrides(basis)
        {
            Entries = path =>
            {
                var entries = basis.GetAccessibleEntries(path);
                entries.Symlinks = null;
                if (path.AsString() == "/root")
                    (entries.Files ??= []).Add("C:/foreign.ts");
                return entries;
            },
            Resolve = path => path.AsString() switch
            {
                "/root/link" => P("/target"),
                "/root/link/hidden.ts" => P("/target/hidden.ts"),
                _ => path
            }
        };
        List<string>? paths = null;
        List<Mode>? modes = null;
        var error = V.WalkDir(host, P("/root"), (path, entry, error) =>
        {
            NilError(error);
            (paths ??= []).Add(path.AsString());
            (modes ??= []).Add(entry!.Type());
            if ((entry.Type() & Mode.Symlink) != 0)
            {
                var(info, infoError) = entry.Info();
                NilError(infoError);
                Check.Equal(info!.Mode(), Mode.Symlink, "symlink info mode");
                Check.True(!info.IsDir(), "symlink info not dir");
            }

            return null;
        });
        NilError(error);
        Check.Sequence(paths, new[] { "/root", "/root/a.ts", "/root/dir", "/root/dir/b.ts", "/root/link" }, "walk paths");
        Check.Sequence(modes, new[] { Mode.Dir, Mode.Regular, Mode.Dir, Mode.Regular, Mode.Symlink }, "walk modes");
    }

    private static void TestWalkDirDoesNotFollowRootSymlink()
    {
        var basis = new ConfigurationFileSystem([new("/root/link/hidden.ts"), new("/target/hidden.ts")]);
        var host = new ReadFileSystemOverrides(basis)
        {
            Entries = path =>
            {
                var entries = basis.GetAccessibleEntries(path);
                entries.Symlinks = null;
                return entries;
            },
            Resolve = path => path.AsString() == "/root/link" ? P("/target") : path
        };
        List<string>? paths = null;
        var error = V.WalkDir(host, P("/root/link"), (path, entry, error) =>
        {
            NilError(error);
            (paths ??= []).Add(path.AsString());
            Check.Equal(entry!.Type(), Mode.Symlink, "root link mode");
            return null;
        });
        NilError(error);
        Check.Sequence(paths, new[] { "/root/link" }, "does not follow root link");
    }

    private static void TestWalkDirReportsRootFileSymlink()
    {
        var basis = new ConfigurationFileSystem([new("/target/file.ts")]);
        var host = new ReadFileSystemOverrides(basis)
        {
            Info = path => basis.Stat(path.AsString() == "/root/link.ts" ? P("/target/file.ts") : path),
            Resolve = path => path.AsString() == "/root/link.ts" ? P("/target/file.ts") : path
        };
        var error = V.WalkDir(host, P("/root/link.ts"), (path, entry, error) =>
        {
            NilError(error);
            Check.Equal(path.AsString(), "/root/link.ts", "root file path");
            Check.Equal(entry!.Name(), "link.ts", "root link name");
            Check.Equal(entry.Type(), Mode.Symlink, "root file link mode");
            return null;
        });
        NilError(error);
    }

    private static void TestWalkDirSkipDir()
    {
        var host = new ConfigurationFileSystem([new("/root/a/hidden.ts"), new("/root/b.ts")]);
        List<string>? paths = null;
        var error = V.WalkDir(host, P("/root"), (path, entry, error) =>
        {
            NilError(error);
            (paths ??= []).Add(path.AsString());
            return path.AsString() == "/root/a" ? V.SkipDir : null;
        });
        NilError(error);
        Check.Sequence(paths, new[] { "/root", "/root/a", "/root/b.ts" }, "SkipDir");
    }

    private static void TestWalkDirSkipAll()
    {
        var host = new ConfigurationFileSystem([new("/root/a.ts"), new("/root/b.ts")]);
        List<string>? paths = null;
        var error = V.WalkDir(host, P("/root"), (path, entry, error) =>
        {
            NilError(error);
            (paths ??= []).Add(path.AsString());
            return path.AsString() == "/root/a.ts" ? V.SkipAll : null;
        });
        NilError(error);
        Check.Sequence(paths, new[] { "/root", "/root/a.ts" }, "SkipAll");
    }

    private static void TestWalkDirConsumesSkipDirForRootFile()
    {
        var host = new ConfigurationFileSystem([new("/root.ts")]);
        var error = V.WalkDir(host, P("/root.ts"), (path, entry, error) =>
        {
            NilError(error);
            return V.SkipDir;
        });
        NilError(error);
    }

    private static void TestWalkDirConsumesSkipForMissingRoot()
    {
        var host = new ConfigurationFileSystem([]);
        foreach (var sentinel in new[]
        {
            V.SkipDir,
            V.SkipAll
        }

        )
        {
            var error = V.WalkDir(host, P("/missing"), (path, entry, error) =>
            {
                Check.True(ReferenceEquals(error, V.ErrNotExist), "missing root ErrNotExist");
                return sentinel;
            });
            NilError(error);
        }
    }

    private static void TestWalkDirUsesSymlinkMetadataWithoutRealpathCalls()
    {
        var basis = new ConfigurationFileSystem([new("/root/dir/file.ts")]);
        List<string>? realpathCalls = null;
        var host = new ReadFileSystemOverrides(basis)
        {
            Resolve = path =>
            {
                (realpathCalls ??= []).Add(path.AsString());
                return path;
            }
        };
        var error = V.WalkDir(host, P("/root"), (path, entry, error) => error);
        NilError(error);
        Check.True(realpathCalls?.Contains("/root/dir") != true, "symlink metadata avoids directory realpath");
    }
}

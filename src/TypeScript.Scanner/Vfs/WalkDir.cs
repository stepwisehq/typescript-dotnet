// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified complete C# port of tsc/internal/vfs/walkdir.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.Vfs;
public static partial class Vfs
{
    private static bool errorIs(Exception? error, Exception target)
    {
        while (error != null)
        {
            if (ReferenceEquals(error, target))
                return true;
            error = error.InnerException;
        }

        return false;
    }

    private static Exception? normalizeWalkDirError(Exception? error) => errorIs(error, SkipDir) || errorIs(error, SkipAll) ? null : error;
    public static Exception? WalkDir(IFS fileSystem, RootedPath root, WalkDirFunc walkFn)
    {
        FileInfo? rootInfo = fileSystem.Stat(root);
        if (rootInfo == null)
            return normalizeWalkDirError(walkFn(root, null, ErrNotExist));
        CaseSensitivity sensitivity = fileSystem.CaseSensitivity();
        var(rootPrefix, _) = root.RootAndRelativePath();
        bool SameRoot(RootedPath path) => sensitivity.ComparePaths(path.RootAndRelativePath().Root.AsPath(), rootPrefix.AsPath()) == 0;
        bool Equivalent(RootedPath left, RootedPath right) => sensitivity.ComparePaths(left, right) == 0;
        var visited = new HashSet<PathKey>();
        Exception? Visit(RootedPath path, DirEntry entry, RootedPath realpath)
        {
            if (entry.IsDir() && !visited.Add(sensitivity.PathKey(realpath)))
                return null;
            Exception? error = walkFn(path, entry, null);
            if (error != null)
            {
                if (errorIs(error, SkipDir) && entry.IsDir())
                    return null;
                return error;
            }

            if (!entry.IsDir())
                return null;
            RootedDirectoryPath directory = TsPath.RootedDirectoryPathFromPath(path);
            Entries entries = fileSystem.GetAccessibleEntries(directory);
            var directories = new HashSet<string>(entries.Directories.Length, StringComparer.Ordinal);
            foreach (var name in entries.Directories) directories.Add(name);
            var names = GoSlices.Clone(entries.Directories).Append(entries.Files);
            GoSlices.SortFunc(names, static (left, right) => string.CompareOrdinal(left, right));
            foreach (string name in names)
            {
                FileMode mode = directories.Contains(name) ? FileMode.Dir : FileMode.Regular;
                RootedPath childPath = (mode & FileMode.Dir) != 0 ? directory.ResolveDirectory(name).AsPath() : directory.ResolveFile(name).AsPath();
                if (!SameRoot(childPath))
                    continue;
                RootedPath childRealpath = default;
                bool isSymlink;
                if (entries.Symlinks != null)
                {
                    isSymlink = entries.Symlinks.Contains(name);
                    if (!isSymlink && (mode & FileMode.Dir) != 0)
                        childRealpath = TsPath.RootedDirectoryPathFromPath(realpath).ResolveDirectory(name).AsPath();
                }
                else
                {
                    childRealpath = fileSystem.Realpath(childPath);
                    RootedDirectoryPath realDirectory = TsPath.RootedDirectoryPathFromPath(realpath);
                    RootedPath expected = (mode & FileMode.Dir) != 0 ? realDirectory.ResolveDirectory(name).AsPath() : realDirectory.ResolveFile(name).AsPath();
                    isSymlink = !Equivalent(childRealpath, expected);
                }

                if (isSymlink)
                    mode = FileMode.Symlink;
                var childEntry = new walkDirEntry(fileSystem, childPath, name, mode);
                if ((mode & FileMode.Dir) == 0)
                {
                    error = Visit(childPath, childEntry, default);
                    if (error != null)
                    {
                        if (errorIs(error, SkipDir))
                            return null;
                        return error;
                    }

                    continue;
                }

                if (childRealpath.AsString() == "")
                    childRealpath = fileSystem.Realpath(childPath);
                error = Visit(childPath, childEntry, childRealpath);
                if (error != null)
                {
                    if (errorIs(error, SkipDir))
                        return null;
                    return error;
                }
            }

            return null;
        }

        DirEntry rootEntry = new fileInfoDirEntry(rootInfo);
        RootedPath rootRealpath = fileSystem.Realpath(root);
        if (root != rootPrefix.AsPath())
        {
            RootedDirectoryPath parent = root.Directory();
            RootedPath expected = TsPath.RootedDirectoryPathFromPath(fileSystem.Realpath(parent.AsPath())).ResolveDirectory(root.BaseName()).AsPath();
            if (!Equivalent(rootRealpath, expected))
                rootEntry = new walkDirEntry(fileSystem, root, root.BaseName(), FileMode.Symlink);
        }

        return normalizeWalkDirError(Visit(root, rootEntry, rootRealpath));
    }

    private sealed class fileInfoDirEntry(FileInfo info) : DirEntry
    {
        public string Name() => info.Name();
        public bool IsDir() => info.IsDir();
        public FileMode Type() => info.Mode() & FileMode.Type;
        public (FileInfo? Info, Exception? Error) Info() => (info, null);
    }

    private sealed class walkDirEntry(IFS fileSystem, RootedPath path, string name, FileMode mode) : DirEntry
    {
        public string Name() => name;
        public bool IsDir() => (mode & FileMode.Dir) != 0;
        public FileMode Type() => mode & FileMode.Type;
        public (FileInfo? Info, Exception? Error) Info()
        {
            if ((mode & FileMode.Symlink) != 0)
                return (new walkDirFileInfo(name, mode), null);
            FileInfo? info = fileSystem.Stat(path);
            return info == null ? (null, ErrNotExist) : (info, null);
        }
    }

    private sealed class walkDirFileInfo(string name, FileMode mode) : FileInfo
    {
        public string Name() => name;
        public long Size() => 0;
        public FileMode Mode() => mode;
        public DateTimeOffset ModTime() => DateTimeOffset.MinValue;
        public bool IsDir() => (mode & FileMode.Dir) != 0;
        public object? Sys() => null;
    }
}

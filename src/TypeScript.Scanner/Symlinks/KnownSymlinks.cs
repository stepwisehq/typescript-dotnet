// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/symlinks/knownsymlinks.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Collections;
using TypeScript.Modules;
using TypeScript.Scanner;

namespace TypeScript.Symlinks;
public sealed class KnownDirectoryLink
{
    public RootedDirectoryPath Symlink, Real;
    public PathKey RealPath;
    public (RootedFilePath File, bool Ok) ResolveFilePath(RootedFilePath fileName, CaseSensitivity caseSensitivity)
    {
        var (relative, ok) = caseSensitivity.RelativeFilePathFromDirectory(Symlink, fileName);
        return ok ? (Real.ResolveRelativeFile(relative), true) : (default, false);
    }
}

public sealed class KnownSymlinks
{
    private readonly SyncMap<PathKey, KnownDirectoryLink?> directories = new();
    private readonly SyncMap<PathKey, SyncSet<RootedDirectoryPath>> directoriesByRealpath = new();
    private readonly SyncMap<PathKey, RootedFilePath> files = new();
    private readonly SyncMap<PathKey, SyncSet<RootedFilePath>> filesByRealpath = new();
    internal readonly CaseSensitivity caseSensitivity;
    private KnownSymlinks(CaseSensitivity caseSensitivity) => this.caseSensitivity = caseSensitivity;
    public static KnownSymlinks NewKnownSymlinks(CaseSensitivity caseSensitivity) => new(caseSensitivity);
    public bool HasDirectory(PathKey symlinkPath) => directories.Load(symlinkPath).Ok;
    public SyncMap<PathKey, KnownDirectoryLink?> Directories() => directories;
    public SyncMap<PathKey, SyncSet<RootedDirectoryPath>> DirectoriesByRealpath() => directoriesByRealpath;
    public SyncMap<PathKey, RootedFilePath> Files() => files;
    public SyncMap<PathKey, SyncSet<RootedFilePath>> FilesByRealpath() => filesByRealpath;

    public void SetDirectory(RootedDirectoryPath symlink, PathKey symlinkPath, KnownDirectoryLink? realDirectory)
    {
        if (realDirectory is not null)
        {
            realDirectory = new() { Symlink = symlink, Real = realDirectory.Real, RealPath = realDirectory.RealPath };
            if (!directories.Load(symlinkPath).Ok)
                directoriesByRealpath.LoadOrStore(realDirectory.RealPath, new()).Actual.Add(symlink);
        }
        directories.Store(symlinkPath, realDirectory);
    }

    public void SetFile(RootedFilePath symlink, PathKey symlinkPath, RootedFilePath realpath)
    {
        if (!files.Load(symlinkPath).Ok)
            filesByRealpath.LoadOrStore(caseSensitivity.PathKey(realpath.AsPath()), new()).Actual.Add(symlink);
        files.Store(symlinkPath, realpath);
    }

    public void SetSymlinksFromResolutions(
        Action<Action<ResolvedModule, string, ResolutionMode, PathKey>, SourceFile?> forEachResolvedModule,
        Action<Action<ResolvedTypeReferenceDirective, string, ResolutionMode, PathKey>, SourceFile?> forEachResolvedTypeReferenceDirective)
    {
        forEachResolvedModule((resolution, _, _, _) => ProcessResolution(resolution.OriginalPath, resolution.ResolvedFileName), null);
        forEachResolvedTypeReferenceDirective((resolution, _, _, _) => ProcessResolution(resolution.OriginalPath, resolution.ResolvedFileName), null);
    }

    public void ProcessResolution(RootedFilePath originalFileName, RootedFilePath resolvedFileName)
    {
        if (originalFileName == default || resolvedFileName == default)
            return;
        SetFile(originalFileName, caseSensitivity.PathKey(originalFileName.AsPath()), resolvedFileName);
        var (commonResolved, commonOriginal) = GuessDirectorySymlinkFromFilePaths(resolvedFileName, originalFileName);
        if (commonResolved != default && commonOriginal != default)
        {
            var symlinkPath = caseSensitivity.PathKey(commonOriginal.AsPath());
            if (!TsPath.ContainsIgnoredPathKey(symlinkPath))
                SetDirectory(commonOriginal, symlinkPath, new() { Real = commonResolved, RealPath = caseSensitivity.PathKey(commonResolved.AsPath()) });
        }
    }

    // Existing selected helper is internal for direct original assertion bodies.
    internal (RootedDirectoryPath Resolved, RootedDirectoryPath Original) GuessDirectorySymlinkFromFilePaths(RootedFilePath a, RootedFilePath b)
    {
        bool isDirectory = false;
        while (true)
        {
            var aParent = a.Directory();
            var bParent = b.Directory();
            if (aParent.AsPath() == a.AsPath() || bParent.AsPath() == b.AsPath() ||
                IsNodeModulesOrScopedPackageDirectory(aParent.AsPath().BaseName()) ||
                IsNodeModulesOrScopedPackageDirectory(bParent.AsPath().BaseName()) ||
                caseSensitivity.Canonicalize(a.BaseName()) != caseSensitivity.Canonicalize(b.BaseName()))
                break;
            a = TsPath.RootedFilePathFromPath(aParent.AsPath());
            b = TsPath.RootedFilePathFromPath(bParent.AsPath());
            isDirectory = true;
        }
        return isDirectory ? (TsPath.RootedDirectoryPathFromPath(a.AsPath()), TsPath.RootedDirectoryPathFromPath(b.AsPath())) : (default, default);
    }

    internal bool IsNodeModulesOrScopedPackageDirectory(string value) => value != "" &&
        (caseSensitivity.Canonicalize(value) == "node_modules" || value.StartsWith("@", StringComparison.Ordinal));
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of selected read-side tsc/internal/vfs/cachedvfs/cachedvfs.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Writable forwarding is external.
using TypeScript.Scanner;
using TypeScript.Collections;

namespace TypeScript.Vfs.CachedVfs;
public sealed class FS : IFS, ISourceTextFileSystem
{
    private readonly IFS fs;
    private int enabled;
    private readonly SyncMap<RootedDirectoryPath, bool> directoryExistsCache = new();
    private readonly SyncMap<RootedFilePath, bool> fileExistsCache = new();
    private readonly SyncMap<RootedDirectoryPath, Entries> getAccessibleEntriesCache = new();
    private readonly SyncMap<RootedPath, RootedPath> realpathCache = new();
    private readonly SyncMap<RootedPath, TypeScript.Vfs.FileInfo?> statCache = new();

    private FS(IFS fs) => this.fs = fs;
    public static FS From(IFS fs)
    {
        var result = new FS(fs);
        Volatile.Write(ref result.enabled, 1);
        return result;
    }

    public void DisableAndClearCache()
    {
        if (Interlocked.CompareExchange(ref enabled, 0, 1) == 1)
            ClearCache();
    }

    public void Enable() => Volatile.Write(ref enabled, 1);
    public void ClearCache()
    {
        directoryExistsCache.Clear();
        fileExistsCache.Clear();
        getAccessibleEntriesCache.Clear();
        realpathCache.Clear();
        statCache.Clear();
    }

    public bool DirectoryExists(RootedDirectoryPath path)
    {
        if (Volatile.Read(ref enabled) != 0)
        {
            var (cached, ok) = directoryExistsCache.Load(path);
            if (ok) return cached;
        }
        var result = fs.DirectoryExists(path);
        if (Volatile.Read(ref enabled) != 0)
            directoryExistsCache.Store(path, result);
        return result;
    }

    public bool FileExists(RootedFilePath path)
    {
        if (Volatile.Read(ref enabled) != 0)
        {
            var (cached, ok) = fileExistsCache.Load(path);
            if (ok) return cached;
        }
        var result = fs.FileExists(path);
        if (Volatile.Read(ref enabled) != 0)
            fileExistsCache.Store(path, result);
        return result;
    }

    public Entries GetAccessibleEntries(RootedDirectoryPath path)
    {
        if (Volatile.Read(ref enabled) != 0)
        {
            var (cached, ok) = getAccessibleEntriesCache.Load(path);
            if (ok) return cached;
        }
        var result = fs.GetAccessibleEntries(path);
        if (Volatile.Read(ref enabled) != 0)
            getAccessibleEntriesCache.Store(path, result);
        return result;
    }

    public RootedPath Realpath(RootedPath path)
    {
        if (Volatile.Read(ref enabled) != 0)
        {
            var (cached, ok) = realpathCache.Load(path);
            if (ok) return cached;
        }
        var result = fs.Realpath(path);
        if (Volatile.Read(ref enabled) != 0)
            realpathCache.Store(path, result);
        return result;
    }

    public TypeScript.Vfs.FileInfo? Stat(RootedPath path)
    {
        if (Volatile.Read(ref enabled) != 0)
        {
            var (cached, ok) = statCache.Load(path);
            if (ok) return cached;
        }
        var result = fs.Stat(path);
        if (Volatile.Read(ref enabled) != 0)
            statCache.Store(path, result);
        return result;
    }

    public (string Contents, bool Ok) ReadFile(RootedFilePath path) => fs.ReadFile(path);
    (ByteStringView Contents, bool Ok) ISourceTextFileSystem.ReadSourceText(RootedFilePath path) => SourceTextReaders.Read(fs, path);
    public CaseSensitivity CaseSensitivity() => fs.CaseSensitivity();

}

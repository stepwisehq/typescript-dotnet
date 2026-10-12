// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/project/autoimport.go ownership adapter.
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.Vfs;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.PackageJson;
using CompilerProgram = TypeScript.Compiler.Program;

namespace TypeScript.ProjectSystem;
internal sealed class AutoImportBuilderFS(SnapshotFSBuilder snapshotFSBuilder) : IFileSource
{
    private readonly SyncMap<PathKey, IFileHandle?> untrackedFiles = new();
    // autoimport.go:27. Never retain autoimport-only files in the long-lived snapshot cache.
    public IFS FS() => snapshotFSBuilder.fs;
    public IFileHandle? GetFile(RootedFilePath fileName) => GetFileByPath(fileName, snapshotFSBuilder.sensitivity.PathKey(fileName.AsPath()));
    public IFileHandle? GetFileByPath(RootedFilePath fileName, PathKey path)
    {
        var(cached, found) = snapshotFSBuilder.cacheFiles.Load(path);
        if (found)
            return snapshotFSBuilder.ReloadEntryIfNeeded(cached!);
        var(file, loaded) = untrackedFiles.Load(path);
        if (loaded)
            return file;
        file = snapshotFSBuilder.fs.GetFileByPath(fileName, path);
        return untrackedFiles.LoadOrStore(path, file).Actual;
    }

    public Entries GetAccessibleEntries(RootedDirectoryPath path) => snapshotFSBuilder.GetAccessibleEntries(path);
    public bool FileExists(RootedFilePath name, PathKey path) => snapshotFSBuilder.FileExists(name, path);
}

internal sealed class AutoImportRegistryCloneHost : IRegistryCloneHost
{
    private ProjectCollection projectCollection = null!;
    private RefCountCache<ParseCacheKey, SourceFile, IFileHandle> parseCache = null!;
    private SourceFS fs = null!;
    private readonly SemaphoreSlim filesMu = new(1, 1);
    private Slice<ParseCacheKey> files;
    // autoimport.go:72.
    internal static AutoImportRegistryCloneHost New(ProjectCollection projects, RefCountCache<ParseCacheKey, SourceFile, IFileHandle> cache, SnapshotFSBuilder fs) => new()
    {
        projectCollection = projects,
        parseCache = cache,
        fs = new SourceFS(false, new AutoImportBuilderFS(fs))
    };
    public IFS FS() => fs;
    public (IProjectID? ProjectID, CompilerProgram? Program) GetDefaultProject(PathKey path)
    {
        var project = projectCollection.GetDefaultProject(path);
        return project is null ? (null, null) : (project.ID(), project.GetProgram());
    }

    // autoimport.go:99. Original shared ref-counted package cache remains an upstream TODO.
    public InfoCacheEntry GetPackageJson(RootedFilePath fileName)
    {
        var file = fs.GetFile(fileName);
        var directory = fileName.Directory();
        var packageDirectory = PackageDirectories.NewPackageDirectory(directory, fs.CaseSensitivity());
        if (file is null)
            return new InfoCacheEntry
            {
                DirectoryExists = fs.DirectoryExists(directory),
                PackageDirectory = packageDirectory
            };
        // Go []byte(content) is the required parse-input owner at this existing API.
        var content = file.ContentValue();
        var bytes = new byte[content.Length];
        for (int index = 0; index < bytes.Length; index++)
            bytes[index] = (byte)content[index];
        var(fields, error) = PackageJsonParser.ParseFields(bytes);
        return new InfoCacheEntry
        {
            DirectoryExists = true,
            PackageDirectory = packageDirectory,
            Contents = error is null ? new TypeScript.PackageJson.PackageJson
            {
                Fields = fields,
                Parseable = true
            }

            : new TypeScript.PackageJson.PackageJson
            {
                Parseable = false
            }
        };
    }

    public CompilerProgram? GetProgramForProject(IProjectID projectID)
    {
        if (projectID is not ID id)
            throw new InvalidOperationException("unexpected project ID type " + projectID.GetType().FullName);
        return projectCollection.GetProject(id)?.GetProgram();
    }

    public SourceFile? GetSourceFile(RootedFilePath fileName, PathKey path)
    {
        var file = fs.GetFile(fileName);
        if (file is null)
            return null;
        var options = new SourceFileParseOptions
        {
            FileNameValue = file.FileName().AsValue(),
            PathKeyValue = path.AsValue()
        };
        var key = ParseCacheKey.New(options, file.Hash(), file.Kind());
        var result = parseCache.Acquire(key, file);
        filesMu.Wait();
        try
        {
            files = files.Append(key);
        }
        finally
        {
            filesMu.Release();
        }

        return result;
    }

    // autoimport.go:163. No invented idempotence: repeated calls repeat the source releases.
    public void Dispose()
    {
        filesMu.Wait();
        try
        {
            foreach (var key in files)
                parseCache.Deref(key);
        }
        finally
        {
            filesMu.Release();
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Read-side port of tsc/internal/compiler/projectreferencedtsfakinghost.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Original write callbacks panic and
// are outside the selected read-only IFS. No declaration text is fabricated.
using TypeScript.Modules;
using TypeScript.Scanner;
using TypeScript.Symlinks;
using TypeScript.Vfs;
using CachedFS = TypeScript.Vfs.CachedVfs.FS;

namespace TypeScript.Compiler;
internal sealed class ProjectReferenceDtsFakingHost : IResolutionHost
{
    private readonly RootedDirectoryPath currentDirectory;
    private readonly CachedFS fs;
    internal ProjectReferenceDtsFakingHost(IResolutionHost host, ProjectReferenceFileMapper mapper)
    {
        currentDirectory = host.GetCurrentDirectory();
        fs = CachedFS.From(new ProjectReferenceDtsFakingVfs(host, mapper));
    }
    public IFS FS() => fs;
    public RootedDirectoryPath GetCurrentDirectory() => currentDirectory;
}
internal sealed class ProjectReferenceDtsFakingVfs(IResolutionHost host, ProjectReferenceFileMapper mapper) : IFS
{
    // Go zero KnownSymlinks has the zero case-sensitivity, deliberately not the
    // underlying host's case mode (source constructor uses KnownSymlinks{}).
    private readonly KnownSymlinks knownSymlinks = KnownSymlinks.NewKnownSymlinks(default);
    public CaseSensitivity CaseSensitivity() => host.FS().CaseSensitivity();
    private PathKey PathKey(RootedPath path) => CaseSensitivity().PathKey(path);
    public bool FileExists(RootedFilePath file) => host.FS().FileExists(file) || file.IsDeclarationFile() && FileExistsUsingSource(file);
    public (string Contents, bool Ok) ReadFile(RootedFilePath path) => host.FS().ReadFile(path);
    // projectreferencedtsfakinghost.go:75. This resolver-only owner must never write.
    public Exception? WriteFile(RootedFilePath path, string data) => throw new InvalidOperationException("should not be called by resolver");
    public bool DirectoryExists(RootedDirectoryPath directory)
    {
        if (host.FS().DirectoryExists(directory))
        {
            HandleDirectoryCouldBeSymlink(directory);
            return true;
        }
        return DirectoryExistsUsingSource(directory);
    }
    public Entries GetAccessibleEntries(RootedDirectoryPath path) => throw new InvalidOperationException("should not be called by resolver");
    public TypeScript.Vfs.FileInfo? Stat(RootedPath path) => throw new InvalidOperationException("should not be called by resolver");
    public RootedPath Realpath(RootedPath path)
    {
        var (result, ok) = knownSymlinks.Files().Load(PathKey(path));
        return ok ? result.AsPath() : host.FS().Realpath(path);
    }
    private void HandleDirectoryCouldBeSymlink(RootedDirectoryPath directory)
    {
        if (TsPath.ContainsIgnoredDirectory(directory) || !directory.ContainsLowercaseDirectorySequence("/node_modules/")) return;
        var path = PathKey(directory.AsPath());
        if (knownSymlinks.Directories().Load(path).Ok) return;
        var realDirectory = TsPath.RootedDirectoryPathFromPath(Realpath(directory.AsPath()));
        if (realDirectory == directory) return;
        var realPath = PathKey(realDirectory.AsPath());
        if (realPath == path) return;
        knownSymlinks.SetDirectory(directory, path, new() { Real = realDirectory, RealPath = realPath });
    }
    private bool FileExistsUsingSource(RootedFilePath file) => FileOrDirectoryExistsUsingSource(file.AsPath(), path => FileExistsIfProjectReferenceDts(TsPath.RootedFilePathFromPath(path)), Modules.ModuleUtilities.NodeModulePackageRootForFile(file), real => knownSymlinks.SetFile(file, PathKey(file.AsPath()), real));
    private bool DirectoryExistsUsingSource(RootedDirectoryPath directory) => FileOrDirectoryExistsUsingSource(directory.AsPath(), path => DirectoryExistsIfProjectReferenceDeclDir(TsPath.RootedDirectoryPathFromPath(path)), Modules.ModuleUtilities.NodeModulePackageRootForDirectory(directory), null);
    private bool FileOrDirectoryExistsUsingSource(RootedPath fileOrDirectory, Func<RootedPath, Tristate> existsUsingSource, RootedDirectoryPath packageRoot, Action<RootedFilePath>? onFileExists)
    {
        var result = existsUsingSource(fileOrDirectory);
        if (result != Tristate.Unknown) return result == Tristate.True;
        var path = PathKey(fileOrDirectory);
        if (!path.ContainsLowercaseDirectorySequence("/node_modules/")) return false;
        if (packageRoot != default) HandleDirectoryCouldBeSymlink(packageRoot);
        var links = knownSymlinks.Directories();
        if (links.Size() == 0) return false;
        if (onFileExists is not null && knownSymlinks.Files().Load(path).Ok) return true;
        bool exists = false;
        links.Range((directoryPath, link) =>
        {
            if (directoryPath == path || !directoryPath.ContainsPath(path)) return true;
            var (real, ok) = link!.ResolveFilePath(TsPath.RootedFilePathFromPath(fileOrDirectory), CaseSensitivity());
            if (!ok) throw new InvalidOperationException("canonical symlink path did not match its presentation path");
            exists = existsUsingSource(real.AsPath()) == Tristate.True;
            if (exists)
            {
                onFileExists?.Invoke(real);
                return false;
            }
            return true;
        });
        return exists;
    }
    private Tristate FileExistsIfProjectReferenceDts(RootedFilePath file)
    {
        var source = mapper.GetProjectReferenceFromOutputDts(PathKey(file.AsPath()));
        return source is null ? Tristate.Unknown : host.FS().FileExists(source.Source) ? Tristate.True : Tristate.False;
    }
    private Tristate DirectoryExistsIfProjectReferenceDeclDir(RootedDirectoryPath directory)
    {
        var path = PathKey(directory.AsPath());
        foreach (var declarationPath in mapper.dtsDirectories)
            if (path.ContainsPath(declarationPath) || declarationPath.ContainsPath(path)) return Tristate.True;
        return Tristate.Unknown;
    }
}

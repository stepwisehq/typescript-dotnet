// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/module/resolver.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Bundler relative disk branch only.
using TypeScript.Compiler;
using TypeScript.Scanner;

namespace TypeScript.Modules;
internal sealed class DefaultResolver
{
    private readonly ProjectDiskHost host;
    private readonly Dictionary<(string Directory, ModeAwareCacheKey Key), ResolvedModule> cache = new();
    internal DefaultResolver(ProjectDiskHost host) => this.host = host;
    internal ResolvedModule ResolveModuleName(string moduleName, string containingFile, ResolutionMode mode)
    {
        // Authored admission before the source algorithm's package/URI branches.
        if (!TsPath.PathIsRelative(moduleName))
            throw new ProjectException("moduleSpecifier", "Only relative module specifiers are supported: " + moduleName, containingFile);
        if (mode is not (ResolutionMode.None or ResolutionMode.ESNext))
            throw new ProjectException("resolutionMode", "Only Bundler ES-module resolution is supported.", containingFile);
        string directory = TsPath.GetDirectoryPath(containingFile);
        var key = (directory, new ModeAwareCacheKey(moduleName, mode));
        if (cache.TryGetValue(key, out ResolvedModule? old))
            return old;
        var candidate = NormalizePathForCJSResolution(directory, moduleName);
        host.EnsureBounded(candidate.Path, inspectSymlinks: false);
        var result = NodeLoadModuleByRelativeName(candidate) ?? new ResolvedModule();
        cache.Add(key, result);
        return result;
    }

    internal readonly record struct ResolutionCandidate(string Path, bool DirectoryOnly)
    {
        internal string AsString() => DirectoryOnly ? TsPath.EnsureTrailingDirectorySeparator(Path) : Path;
        internal bool HasTrailingDirectorySeparator() => DirectoryOnly;
    }

    // resolver.go:2275. Dynamic URI encoding is outside the disk host contract.
    internal static ResolutionCandidate NormalizePathForCJSResolution(string directory, string moduleName)
    {
        if (directory.StartsWith("^/", StringComparison.Ordinal) || moduleName.StartsWith("^/", StringComparison.Ordinal))
            throw new ProjectException("dynamicPath", "Dynamic URI paths are unsupported.", moduleName);
        string trimmed = moduleName.TrimEnd('/', '\\');
        int lastSeparator = trimmed.LastIndexOfAny(['/', '\\']);
        string lastPart = trimmed[(lastSeparator + 1)..];
        string resolved = TsPath.ResolvePath(directory, moduleName);
        bool directoryOnly = trimmed.Length != moduleName.Length || lastPart is "." or ".." || TsPath.HasTrailingDirectorySeparator(resolved);
        string path = TsPath.GetNormalizedAbsolutePath(resolved, "");
        return new(path, directoryOnly);
    }

    // resolver.go:1588. Bundler's state has esmMode=false, unlike Node ESM.
    private ResolvedModule? NodeLoadModuleByRelativeName(ResolutionCandidate candidate)
    {
        if (!candidate.HasTrailingDirectorySeparator())
        {
            if (!host.DirectoryExists(TsPath.GetDirectoryPath(candidate.Path)))
                return null;
            ResolvedModule? fromFile = LoadModuleFromFile(candidate.Path);
            if (fromFile is not null)
                return fromFile;
        }

        if (!host.DirectoryExists(candidate.Path))
            return null;
        return LoadNodeModuleFromDirectory(candidate.Path);
    }

    // resolver.go:1625/1640. Replacement precedes implicit extension lookup.
    private ResolvedModule? LoadModuleFromFile(string candidate)
    {
        ResolvedModule? replaced = LoadModuleFromFileNoImplicitExtensions(candidate);
        return replaced ?? TryAddingExtensions(candidate, "");
    }

    // resolver.go:1640. The explicit-extension branch does not perform the
    // subsequent implicit append; callers preserve that separate search step.
    private ResolvedModule? LoadModuleFromFileNoImplicitExtensions(string candidate)
    {
        string basename = TsPath.GetBaseFileName(candidate);
        if (basename.Contains('.'))
        {
            string extension = GetSpecifierExtension(candidate);
            string extensionless = candidate[..^extension.Length];
            return TryAddingExtensions(extensionless, extension);
        }

        return null;
    }

    // resolver.go:1652,1752. The fixed state enables TS, declaration and JS bits,
    // but admission rejects the first actual unsupported winner before loading.
    private ResolvedModule? TryAddingExtensions(string stem, string originalExtension)
    {
        if (!host.DirectoryExists(TsPath.GetDirectoryPath(stem)))
            return null;
        string[] extensions = originalExtension switch
        {
            ".ts" or ".d.ts" or ".js" or "" => [".ts", ".tsx", ".d.ts", ".js", ".jsx"],
            ".tsx" or ".jsx" => [".tsx", ".ts", ".d.ts", ".jsx", ".js"],
            ".mjs" or ".mts" or ".d.mts" => [".mts", ".d.mts", ".mjs"],
            ".cjs" or ".cts" or ".d.cts" => [".cts", ".d.cts", ".cjs"],
            ".json" => [".d.json.ts"], // ResolveJsonModule=false, genuine declaration probe.
            _ => TsPath.IsDeclarationFileName(stem + originalExtension) ? [] : [".d" + originalExtension + ".ts"]
        };
        foreach (string extension in extensions)
        {
            string path = stem + extension;
            if (!host.FileExists(path))
                continue;
            if (extension != ".ts" || TsPath.IsDeclarationFileName(path))
                throw new ProjectException("resolvedFileForm", "Upstream resolution selects an unsupported user file: " + path, path);
            return new ResolvedModule
            {
                ResolvedFileName = path,
                ResolvedPath = path, // CaseSensitive.PathKey; no symlinks admitted.
                Extension = extension,
                ResolvedUsingTsExtension = originalExtension is ".ts" or ".d.ts"
            };
        }

        return null;
    }

    // resolver.go:1822/1924. The selected no-package branch reaches index lookup.
    private ResolvedModule? LoadNodeModuleFromDirectory(string candidate)
    {
        string packageJson = TsPath.CombinePaths(candidate, "package.json");
        if (host.FileExists(packageJson))
            throw new ProjectException("packageJsonResolution", "Resolution requires unsupported package metadata: " + packageJson, packageJson);
        return LoadModuleFromFile(TsPath.CombinePaths(candidate, "index"));
    }

    // resolver.go:208 SplitExtension: known extension removal first, then last
    // dot suffix. Dotted implementation basenames can still use implicit .ts.
    private static string GetSpecifierExtension(string path)
    {
        string stem = TsPath.RemoveFileExtension(path);
        if (stem.Length != path.Length)
            return path[stem.Length..];
        string basename = TsPath.GetBaseFileName(path);
        int dot = basename.LastIndexOf('.');
        return dot < 0 ? "" : basename[dot..];
    }

    private readonly Dictionary<string, TypeScript.PackageJson.InfoCacheEntry> packageJsonInfoCache = new(StringComparer.Ordinal);
    // resolver.go:408/707. No global typings cache location is configured.
    internal TypeScript.PackageJson.InfoCacheEntry? GetPackageScopeForPath(string directory) => getPackageScopeForPath(directory);
    private TypeScript.PackageJson.InfoCacheEntry? getPackageScopeForPath(string directory)
    {
        while (true)
        {
            var result = getPackageJsonInfo(directory);
            if (result != null)
                return result;
            string parent = TsPath.GetDirectoryPath(directory);
            if (parent == directory)
                return null;
            directory = parent;
        }
    }

    // resolver.go:1985. Single-threaded exact-case cache replaces sync-map storage;
    // preserve cached misses, directory existence and unparseable existing files.
    private TypeScript.PackageJson.InfoCacheEntry? getPackageJsonInfo(string directory)
    {
        if (packageJsonInfoCache.TryGetValue(directory, out var existing))
            return existing.Contents != null ? existing : null;
        bool exists = host.MetadataDirectoryExists(directory);
        string path = TsPath.CombinePaths(directory, "package.json");
        var result = new TypeScript.PackageJson.InfoCacheEntry
        {
            PackageDirectory = directory,
            DirectoryExists = exists
        };
        if (exists && host.MetadataFileExists(path))
            result.Contents = TypeScript.PackageJson.PackageJsonParser.Parse(host.ReadMetadata(path));
        packageJsonInfoCache.Add(directory, result);
        return result.Contents != null ? result : null;
    }
}

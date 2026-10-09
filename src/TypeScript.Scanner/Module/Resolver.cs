// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/module/resolver.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Selected Bundler source-host branch
// plus source configuration/JSON and package-directory resolution families.
using TypeScript.Compiler;
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.Vfs;
using TypeScript.PackageJson;

namespace TypeScript.Modules;
public sealed class ParsedPatterns
{
    internal HashSet<string>? matchableStringSet;
    internal List<Pattern>? patterns;
}

public sealed partial class DefaultResolver
{
    public static ParsedPatterns TryParsePatterns(OrderedMap<string, List<string>>? pathMappings)
    {
        var paths = pathMappings?.Keys() ?? [];
        int numPatterns = 0, numMatchables = 0;
        foreach (string path in paths)
        {
            Pattern pattern = Core.TryParsePattern(path);
            if (pattern.IsValid())
            {
                if (pattern.StarIndex == -1)
                    numMatchables++;
                else
                    numPatterns++;
            }
        }

        List<Pattern>? patterns = numPatterns == 0 ? null : new(numPatterns);
        HashSet<string>? matchableStringSet = numMatchables == 0 ? null : new(numMatchables, StringComparer.Ordinal);
        foreach (string path in paths)
        {
            Pattern pattern = Core.TryParsePattern(path);
            if (pattern.IsValid())
            {
                if (pattern.StarIndex == -1)
                    matchableStringSet!.Add(path);
                else
                    patterns!.Add(pattern);
            }
        }

        return new ParsedPatterns
        {
            patterns = patterns,
            matchableStringSet = matchableStringSet
        };
    }

    public static Pattern MatchPatternOrExact(ParsedPatterns patterns, string candidate)
    {
        if (patterns.matchableStringSet?.Contains(candidate) == true)
            return new Pattern(candidate, -1);
        return patterns.patterns?.Count > 0 ? Core.FindBestPatternMatch(patterns.patterns, value => value, candidate) : default;
    }

    private readonly ProjectDiskHost host = null!;
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
            PackageDirectory = TypeScript.PackageJson.PackageDirectories.NewPackageDirectory(RootedDirectoryPath.FromUnicode(directory), CaseSensitivity.CaseSensitive),
            DirectoryExists = exists
        };
        if (exists && host.MetadataFileExists(path))
            result.Contents = TypeScript.PackageJson.PackageJsonParser.Parse(host.ReadMetadata(path));
        packageJsonInfoCache.Add(directory, result);
        return result.Contents != null ? result : null;
    }
}

// resolver.go selected configuration lookup and directory-only entrypoints.
// ResolveConfig creates fresh NodeNext options, JSON-only extensions and no tracer.
// Source-language/module/source-map/output redirection families remain external.
public sealed partial class DefaultResolver
{
    private readonly IFS? fs;
    private readonly RootedDirectoryPath baseDirectory;
    private readonly CompilerOptions? configCompilerOptions;
    private readonly InfoCache? configPackageJsonInfoCache;
    public DefaultResolver(IFS fs, RootedDirectoryPath baseDirectory, CompilerOptions compilerOptions)
    {
        this.fs = fs;
        this.baseDirectory = baseDirectory;
        configCompilerOptions = compilerOptions;
        configPackageJsonInfoCache = new InfoCache(fs.CaseSensitivity());
    }

    public RootedDirectoryPath BaseDirectory() => baseDirectory;
    public static ResolvedModule ResolveConfig(string moduleName, RootedFilePath containingFile, IFS fs)
    {
        var resolver = new DefaultResolver(fs, containingFile.Directory(), new CompilerOptions { ModuleResolution = ModuleResolutionKind.NodeNext });
        return resolver.resolveConfig(moduleName, containingFile);
    }

    private ResolvedModule resolveConfig(string name, RootedFilePath containingFile)
    {
        var state = new resolutionState(this, name, containingFile.Directory(), isConfigLookup: true);
        return state.resolveNodeLike();
    }

    public ResolvedModule? ResolvePackageDirectory(string name, RootedFilePath containingFile, ResolutionMode mode)
    {
        if (fs == null)
            throw new InvalidOperationException("Directory-only resolution requires the read filesystem resolver host.");
        var state = new resolutionState(this, name, containingFile.Directory(), isConfigLookup: false)
        {
            resolvePackageDirectoryOnly = true
        };
        var result = state.loadModuleFromNearestNodeModulesDirectory();
        return result?.path.AsString()is { Length: > 0 } ? state.createResolvedModuleHandlingSymlink(result) : null;
    }

    public InfoCacheEntry? GetPackageScopeForPath(RootedDirectoryPath directory) => new resolutionState(this, "", directory, true).getPackageScopeForPath(directory);
    public void PackageJsonCacheEntries(Func<PathKey, InfoCacheEntry, bool> callback) => (configPackageJsonInfoCache ?? throw new InvalidOperationException("A filesystem resolver is required.")).Range(callback);
    private sealed class ResolvedData
    {
        internal RootedFilePath path;
        internal string extension = "";
        internal PackageId packageId = new();
        internal RootedFilePath originalPath;
        // Non-null empty ResolvedData is the original explicit blocked/null target.
        internal bool isResolved() => path != default;
    }

    private static bool shouldContinueSearching(ResolvedData? result) => result == null;
    private readonly record struct resolutionCandidate(RootedPath path, RootedDirectoryPath directoryPath = default, bool directoryOnly = false)
    {
        internal string AsString() => directoryOnly ? TsPath.EnsureTrailingDirectorySeparator(path.AsString()) : path.AsString();
        internal RootedDirectoryPath AsDirectoryPath() => directoryPath != default ? directoryPath : new(path.AsString());
        internal RootedDirectoryPath Directory() => path.Directory();
        internal (FileNameStem Stem, string Extension) SplitExtension()
        {
            var file = new RootedFilePath(path.AsString());
            string extension = file.Extension();
            var stem = file.RemoveFileExtension();
            if (stem.AsString() == path.AsString())
            {
                string text = path.AsString();
                extension = text[text.LastIndexOf('.')..];
                stem = file.RemoveExtension(extension);
            }

            return (stem, extension);
        }
    }

    private static string pathForDynamicResolution(RootedDirectoryPath directory, string path, bool directoryOnly)
    {
        if (TsPath.IsEncodedDynamicFileName(directory.AsString()) && !TsPath.PathIsAbsolute(path))
            return directoryOnly ? TsPath.EncodeDynamicDirectorySpecifier(path) : TsPath.EncodeDynamicModuleSpecifier(path);
        return path;
    }

    private static resolutionCandidate resolutionCandidateFromDirectoryPath(RootedDirectoryPath directory, string path)
    {
        string filePath = pathForDynamicResolution(directory, path, false);
        string directoryPath = pathForDynamicResolution(directory, path, true);
        var resolvedDirectory = directory.ResolveDirectory(directoryPath);
        if (TsPath.HasTrailingDirectorySeparator(path))
            return new(resolvedDirectory.AsPath(), directoryOnly: true);
        var resolvedFile = directory.ResolveFile(filePath).AsPath();
        return new(resolvedFile, resolvedDirectory.AsPath() != resolvedFile ? resolvedDirectory : default);
    }

    private static resolutionCandidate normalizePathForCJSResolution(RootedDirectoryPath directory, string name)
    {
        string trimmed = name.TrimEnd('/', '\\');
        int separator = trimmed.LastIndexOfAny(['/', '\\']);
        string lastPart = trimmed[(separator + 1)..];
        var candidate = resolutionCandidateFromDirectoryPath(directory, name);
        return candidate with
        {
            directoryOnly = trimmed.Length != name.Length || lastPart is "." or ".." || TsPath.HasTrailingDirectorySeparator(candidate.path.AsString())
        };
    }

    // Authored read-only test visibility for the selected original candidate helper.
    internal static (RootedPath Path, string Text, bool DirectoryOnly, bool HasTrailingDirectorySeparator) VerificationNormalizePathForCJSResolution(RootedDirectoryPath directory, string name)
    {
        var candidate = normalizePathForCJSResolution(directory, name);
        return (candidate.path, candidate.AsString(), candidate.directoryOnly, candidate.directoryOnly);
    }

    private sealed class resolutionState
    {
        private readonly DefaultResolver resolver;
        private IFS fs => resolver.fs ?? throw new InvalidOperationException("A read filesystem resolver is required.");
        private InfoCache packageCache => resolver.configPackageJsonInfoCache!;
        private CompilerOptions options => resolver.configCompilerOptions!;

        internal bool resolvePackageDirectoryOnly;
        private readonly bool isConfigLookup;
        private string name;
        private RootedDirectoryPath containingDirectory;
        internal resolutionState(DefaultResolver resolver, string name, RootedDirectoryPath containingDirectory, bool isConfigLookup)
        {
            this.resolver = resolver;
            this.name = name;
            this.containingDirectory = containingDirectory;
            this.isConfigLookup = isConfigLookup;
        }

        internal ResolvedModule resolveNodeLike() => resolveNodeLikeWorker();
        private ResolvedModule resolveNodeLikeWorker()
        {
            // Fresh ResolveConfig options have no paths/baseUrl/rootDirs. The
            // directory-only entrypoint never calls this resolution worker.
            if (!isConfigLookup)
                throw new NotSupportedException("Source module resolution uses the separate admitted host entrypoint.");
            if (!TsPath.PathIsRelative(name) && !TsPath.PathIsAbsolute(name))
            {
                if (name.StartsWith('#'))
                {
                    var imports = loadModuleFromImports();
                    if (!shouldContinueSearching(imports))
                        return createResolvedModuleHandlingSymlink(imports);
                }

                var self = loadModuleFromSelfNameReference();
                if (!shouldContinueSearching(self))
                    return createResolvedModuleHandlingSymlink(self);
                if (name.Contains(':'))
                    return createResolvedModule(null, false);
                var package = loadModuleFromNearestNodeModulesDirectory();
                if (!shouldContinueSearching(package))
                    return createResolvedModuleHandlingSymlink(package);
            }
            else
            {
                var found = nodeLoadModuleByRelativeName(normalizePathForCJSResolution(containingDirectory, name), true);
                return createResolvedModule(found, found?.path.ContainsLowercaseDirectorySequence("/node_modules/") == true);
            }

            return createResolvedModule(null, false);
        }

        internal InfoCacheEntry? getPackageScopeForPath(RootedDirectoryPath directory)
        {
            for (;;)
            {
                var info = getPackageJsonInfo(packageCache.PackageDirectory(directory));
                if (info != null)
                    return info;
                var parent = directory.Directory();
                if (parent == directory)
                    return null;
                directory = parent;
            }
        }

        private InfoCacheEntry? getPackageJsonInfo(PackageDirectory directory)
        {
            var existing = packageCache.Get(directory);
            if (existing != null)
                return existing.Contents != null ? existing.WithPackageDirectory(directory) : null;
            var file = directory.ResolveFile("package.json");
            bool directoryExists = fs.DirectoryExists(directory.AsDirectoryPath());
            if (directoryExists && fs.FileExists(file))
            {
                var(contents, _) = fs.ReadFile(file);
                var result = new InfoCacheEntry
                {
                    PackageDirectory = directory,
                    DirectoryExists = true,
                    Contents = PackageJsonParser.Parse(GoString.Bytes(contents))
                };
                return packageCache.Set(directory, result).WithPackageDirectory(directory);
            }

            packageCache.Set(directory, new InfoCacheEntry { PackageDirectory = directory, DirectoryExists = directoryExists });
            return null;
        }

        private ResolvedData? loadModuleFromSelfNameReference()
        {
            var scope = getPackageScopeForPath(containingDirectory);
            if (scope?.Contents == null || scope.Contents.Exports.IsFalsy())
                return null;
            var(packageName, ok) = scope.Contents.Fields.Name.GetValue();
            if (!ok)
                return null;
            var parts = TsPath.GetPathComponents(name);
            var nameParts = TsPath.GetPathComponents(packageName);
            if (parts.Count < nameParts.Count || !parts.Take(nameParts.Count).SequenceEqual(nameParts))
                return null;
            var trailing = parts.Skip(nameParts.Count).ToArray();
            string subpath = trailing.Length > 0 ? TsPath.CombinePaths(".", trailing) : ".";
            // JSON-only has an empty TS/DTS priority pass. It still probes the
            // map in that pass; a null target must block the secondary JSON pass.
            var priority = loadModuleFromExports(scope, subpath, jsonEnabled: false);
            return shouldContinueSearching(priority) ? loadModuleFromExports(scope, subpath, jsonEnabled: true) : priority;
        }

        private ResolvedData? loadModuleFromImports()
        {
            if (name == "#" || name.StartsWith("#/", StringComparison.Ordinal))
                return null;
            var scope = getPackageScopeForPath(containingDirectory);
            if (scope?.Contents?.Imports.Type != JSONValueType.Object)
                return null;
            return loadModuleFromExportsOrImports(name, scope.Contents.Imports.AsObject(), scope, true, true);
        }

        private ResolvedData? loadModuleFromExports(InfoCacheEntry scope, string subpath, bool jsonEnabled)
        {
            if (scope.Contents == null || scope.Contents.Exports.IsFalsy())
                return null;
            var exports = scope.Contents.Exports;
            if (subpath == ".")
            {
                ExportsOrImports main = default;
                if (exports.Type is JSONValueType.String or JSONValueType.Array)
                    main = exports;
                else if (exports.Type == JSONValueType.Object)
                    main = exports.IsConditions() ? exports : exports.AsObject().GetOrZero(".");
                if (main.Type != JSONValueType.NotPresent)
                    return loadModuleFromTargetExportOrImport(subpath, scope, false, main, "", false, ".", jsonEnabled);
            }
            else if (exports.Type == JSONValueType.Object && exports.IsSubpaths())
                return loadModuleFromExportsOrImports(subpath, exports.AsObject(), scope, false, jsonEnabled);
            return null;
        }

        private ResolvedData? loadModuleFromExportsOrImports(string moduleName, OrderedMap<string, ExportsOrImports> table, InfoCacheEntry scope, bool isImports, bool jsonEnabled)
        {
            if (!moduleName.EndsWith('/') && !moduleName.Contains('*'))
            {
                var(target, ok) = table.Get(moduleName);
                if (ok)
                    return loadModuleFromTargetExportOrImport(moduleName, scope, isImports, target, "", false, moduleName, jsonEnabled);
            }

            var keys = table.Keys().Where(key => key.Count(ch => ch == '*') == 1 || key.EndsWith('/')).ToList();
            keys.Sort(ModuleUtilities.ComparePatternKeys);
            foreach (string key in keys)
            {
                int star = key.IndexOf('*');
                if (matchesPatternWithTrailer(key, moduleName))
                {
                    string subpath = moduleName[star..(moduleName.Length - (key.Length - 1 - star))];
                    return loadModuleFromTargetExportOrImport(moduleName, scope, isImports, table.GetOrZero(key), subpath, true, key, jsonEnabled);
                }

                if (key.EndsWith('*') && moduleName.StartsWith(key[..^1], StringComparison.Ordinal))
                    return loadModuleFromTargetExportOrImport(moduleName, scope, isImports, table.GetOrZero(key), moduleName[(key.Length - 1)..], true, key, jsonEnabled);
                if (moduleName.StartsWith(key, StringComparison.Ordinal))
                    return loadModuleFromTargetExportOrImport(moduleName, scope, isImports, table.GetOrZero(key), moduleName[key.Length..], false, key, jsonEnabled);
            }

            return null;
        }

        private ResolvedData? loadModuleFromTargetExportOrImport(string moduleName, InfoCacheEntry scope, bool isImports, ExportsOrImports target, string subpath, bool isPattern, string key, bool jsonEnabled)
        {
            switch (target.Type)
            {
                case JSONValueType.String:
                    string targetString = (string)target.Value!;
                    if (!isPattern && subpath.Length > 0 && !targetString.EndsWith('/'))
                        return null;
                    if (!targetString.StartsWith("./", StringComparison.Ordinal))
                    {
                        if (isImports && !targetString.StartsWith("../", StringComparison.Ordinal) && !targetString.StartsWith('/') && !TsPath.IsRootedDiskPath(targetString))
                        {
                            string lookup = isPattern ? targetString.Replace("*", subpath, StringComparison.Ordinal) : targetString + subpath;
                            string previousName = name;
                            var previousDirectory = containingDirectory;
                            name = lookup;
                            containingDirectory = scope.PackageDirectory.AsDirectoryPath();
                            try
                            {
                                var result = resolveNodeLike();
                                return result.IsResolved() ? new ResolvedData
                                {
                                    path = new(result.ResolvedFileName),
                                    extension = result.Extension,
                                    packageId = result.PackageId,
                                    originalPath = new(result.OriginalPath)
                                }

                                : null;
                            }
                            finally
                            {
                                name = previousName;
                                containingDirectory = previousDirectory;
                            }
                        }

                        return null;
                    }

                    var parts = TsPath.GetPathComponents(targetString);
                    if (TsPath.PathIsRelative(targetString))
                        parts = parts.Skip(1).ToList();
                    if (parts.Skip(1).Any(part => part is ".." or "." or "node_modules"))
                        return null;
                    if (TsPath.GetPathComponents(subpath).Any(part => part is ".." or "." or "node_modules"))
                        return null;
                    string targetPath = isPattern ? targetString.Replace("*", subpath, StringComparison.Ordinal) : targetString + subpath;
                    var candidate = resolutionCandidateFromDirectoryPath(scope.PackageDirectory.AsDirectoryPath(), targetPath);
                    // tryLoadInputFileForPath is unreachable: isConfigLookup is
                    // true, independent of any other original output option.
                    var found = loadFileNameFromPackageJSONField(candidate, jsonEnabled);
                    if (found != null)
                        found.packageId = getPackageId(found.path, scope);
                    return found;
                case JSONValueType.Object:
                    foreach (var(condition, child)in target.AsObject().Entries())
                        if (conditionMatches(condition))
                        {
                            var result = loadModuleFromTargetExportOrImport(moduleName, scope, isImports, child, subpath, isPattern, key, jsonEnabled);
                            if (!shouldContinueSearching(result))
                                return result;
                        }

                    return null;
                case JSONValueType.Array:
                    foreach (var child in target.AsArray() ?? [])
                    {
                        var result = loadModuleFromTargetExportOrImport(moduleName, scope, isImports, child, subpath, isPattern, key, jsonEnabled);
                        if (!shouldContinueSearching(result))
                            return result;
                    }

                    return null;
                case JSONValueType.Null:
                    return new ResolvedData();
                default:
                    return null;
            }
        }

        private static bool conditionMatches(string condition) => condition is "default" or "require" or "types" or "node" || ModuleUtilities.IsApplicableVersionedTypesKey(condition);
        internal ResolvedData? loadModuleFromNearestNodeModulesDirectory()
        {
            // JSON-only lookup has no preferred TS/DTS or @types pass. Directory
            // only returns in the first actual package branch on either pass.
            return loadModuleFromNearestNodeModulesDirectoryWorker();
        }

        private ResolvedData? loadModuleFromNearestNodeModulesDirectoryWorker()
        {
            var directory = containingDirectory;
            for (;;)
            {
                if (directory.BaseName() != "node_modules")
                {
                    var result = loadModuleFromImmediateNodeModulesDirectory(directory);
                    if (!shouldContinueSearching(result))
                        return result;
                }

                var parent = directory.Directory();
                if (parent == directory)
                    return null;
                directory = parent;
            }
        }

        private ResolvedData? loadModuleFromImmediateNodeModulesDirectory(RootedDirectoryPath directory)
        {
            var nodeModules = directory.ResolveDirectory("node_modules");
            if (!fs.DirectoryExists(nodeModules))
                return null;
            var result = loadModuleFromSpecificNodeModulesDirectory(name, nodeModules);
            if (!shouldContinueSearching(result))
                return result;
            if (resolvePackageDirectoryOnly && options.NoDtsResolution != Tristate.True)
            {
                var types = nodeModules.ResolveDirectory("@types");
                if (fs.DirectoryExists(types))
                    return loadModuleFromSpecificNodeModulesDirectory(ModuleUtilities.MangleScopedPackageName(name), types);
            }

            return null;
        }

        private ResolvedData? loadModuleFromSpecificNodeModulesDirectory(string moduleName, RootedDirectoryPath nodeModules)
        {
            var candidate = resolutionCandidateFromDirectoryPath(nodeModules, moduleName);
            var candidateDirectory = candidate.AsDirectoryPath();
            var(packageName, rest) = ModuleUtilities.ParsePackageName(moduleName);
            var packageDirectory = packageName == "" ? candidateDirectory : nodeModules.ResolveDirectory(pathForDynamicResolution(nodeModules, packageName, true));
            if (resolvePackageDirectoryOnly)
                return fs.DirectoryExists(packageDirectory) ? new ResolvedData
                {
                    path = new(packageDirectory.AsString())
                }

                : null;
            InfoCacheEntry? rootInfo = null;
            var info = getPackageJsonInfo(packageCache.PackageDirectory(candidateDirectory));
            if (rest != "" && info != null)
            {
                rootInfo = getPackageJsonInfo(packageCache.PackageDirectory(packageDirectory));
                if (rootInfo?.Contents?.Exports.Type is null or JSONValueType.NotPresent)
                {
                    var fromFile = loadModuleFromFile(candidate);
                    if (!shouldContinueSearching(fromFile))
                        return fromFile;
                    var fromDirectory = loadNodeModuleFromDirectoryWorker(candidateDirectory, info);
                    if (!shouldContinueSearching(fromDirectory))
                    {
                        fromDirectory!.packageId = getPackageId(fromDirectory.path, info);
                        return fromDirectory;
                    }
                }
            }

            ResolvedData? loader(resolutionCandidate value)
            {
                var fromFile = loadModuleFromFile(value);
                if (!shouldContinueSearching(fromFile))
                {
                    fromFile!.packageId = getPackageId(fromFile.path, info);
                    return fromFile;
                }

                var fromDirectory = loadNodeModuleFromDirectoryWorker(value.AsDirectoryPath(), info);
                if (!shouldContinueSearching(fromDirectory))
                {
                    fromDirectory!.packageId = getPackageId(fromDirectory.path, info);
                    return fromDirectory;
                }

                return null;
            }

            if (rest != "")
                info = rootInfo ?? getPackageJsonInfo(packageCache.PackageDirectory(packageDirectory));
            if (info != null)
            {
                if (info.Contents != null && !info.Contents.Exports.IsFalsy())
                    return loadModuleFromExports(info, TsPath.CombinePaths(".", rest), true);
                if (rest != "" && info.Contents != null)
                {
                    var versions = info.Contents.GetVersionPaths();
                    if (versions.Exists())
                    {
                        var paths = tryLoadModuleUsingPaths(rest, packageDirectory, versions.GetPaths()!, loader);
                        if (!shouldContinueSearching(paths))
                            return paths;
                    }
                }
            }

            return loader(candidate);
        }

        private ResolvedData? nodeLoadModuleByRelativeName(resolutionCandidate candidate, bool considerPackageJson)
        {
            if (!candidate.directoryOnly)
            {
                if (!fs.DirectoryExists(candidate.Directory()))
                    return null;
                var fromFile = loadModuleFromFile(candidate);
                if (fromFile != null)
                {
                    if (considerPackageJson)
                    {
                        var root = ModuleUtilities.NodeModulePackageRootForFile(fromFile.path);
                        if (root != default)
                            fromFile.packageId = getPackageId(fromFile.path, getPackageJsonInfo(packageCache.PackageDirectory(root)));
                    }

                    return fromFile;
                }
            }

            if (!fs.DirectoryExists(candidate.AsDirectoryPath()))
                return null;
            return loadNodeModuleFromDirectory(candidate, considerPackageJson);
        }

        private ResolvedData? loadModuleFromFile(resolutionCandidate candidate)
        {
            var replacing = loadModuleFromFileNoImplicitExtensions(candidate);
            return replacing ?? tryAddingExtensions(new FileNameStem(candidate.path.AsString()), candidate.Directory(), "");
        }

        private ResolvedData? loadModuleFromFileNoImplicitExtensions(resolutionCandidate candidate, bool jsonEnabled = true)
        {
            if (!candidate.path.BaseName().Contains('.'))
                return null;
            var(stem, extension) = candidate.SplitExtension();
            return tryAddingExtensions(stem, candidate.Directory(), extension, jsonEnabled);
        }

        private ResolvedData? tryAddingExtensions(FileNameStem stem, RootedDirectoryPath directory, string extension, bool jsonEnabled = true)
        {
            if (directory != default && !fs.DirectoryExists(directory))
                return null;
            // Actual JSON-only branches of the full source switch. Recognized
            // MJS/CJS/TSX/JSX and unknown-extension branches do not include JSON.
            // The TS/JS/empty source branch probes .json for config lookup
            // even in the self-name priority pass whose extension mask is zero.
            if ((extension == ".json" && jsonEnabled) || extension is ".ts" or ".d.ts" or ".js" or "")
                return tryExtension(".json", stem);
            return null;
        }

        private ResolvedData? tryExtension(string extension, FileNameStem stem)
        {
            var path = stem.AppendSuffix(extension);
            return fs.FileExists(path) ? new ResolvedData
            {
                path = path,
                extension = extension
            }

            : null;
        }

        private ResolvedData? loadNodeModuleFromDirectory(resolutionCandidate candidate, bool considerPackageJson)
        {
            var directory = candidate.AsDirectoryPath();
            var info = considerPackageJson ? getPackageJsonInfo(packageCache.PackageDirectory(directory)) : null;
            return loadNodeModuleFromDirectoryWorker(directory, info);
        }

        private ResolvedData? loadNodeModuleFromDirectoryWorker(RootedDirectoryPath candidate, InfoCacheEntry? info)
        {
            resolutionCandidate packageFile = default;
            VersionPaths versions = default;
            if (info?.Contents != null)
            {
                versions = info.Contents.GetVersionPaths();
                if (fs.CaseSensitivity().ComparePaths(candidate.AsPath(), info.PackageDirectory.AsDirectoryPath().AsPath()) == 0)
                {
                    var(file, ok) = getPackageFile(info);
                    if (ok)
                        packageFile = file;
                }
            }

            ResolvedData? loader(resolutionCandidate value) => loadFileNameFromPackageJSONField(value) ?? nodeLoadModuleByRelativeName(value, false);
            var index = new resolutionCandidate(candidate.ResolveFile("tsconfig").AsPath());
            if (versions.Exists() && (packageFile.path == default || CaseSensitivity.CaseInsensitive.ContainsPath(candidate, packageFile.path)))
            {
                var modulePath = packageFile.path != default ? packageFile.path : index.path;
                var(relative, ok) = CaseSensitivity.CaseInsensitive.RelativePathFromDirectory(candidate, new(modulePath.AsString()));
                if (!ok)
                    throw new InvalidOperationException("package module path must have the same root as its candidate directory");
                string moduleName = relative.AsString();
                if (TsPath.IsEncodedDynamicFileName(candidate.AsString()))
                    moduleName = TsPath.DecodeDynamicURIPath(moduleName);
                var result = tryLoadModuleUsingPaths(moduleName, candidate, versions.GetPaths()!, loader);
                if (!shouldContinueSearching(result))
                {
                    if (result!.packageId.Name != "")
                        throw new InvalidOperationException("expected packageId to be empty");
                    return result;
                }
            }

            if (packageFile.path != default)
            {
                var result = loader(packageFile);
                if (!shouldContinueSearching(result))
                {
                    if (result!.packageId.Name != "")
                        throw new InvalidOperationException("expected packageId to be empty");
                    return result;
                }
            }

            return fs.DirectoryExists(candidate) ? loadModuleFromFile(index) : null;
        }

        private ResolvedData? loadFileNameFromPackageJSONField(resolutionCandidate candidate, bool jsonEnabled = true)
        {
            if (candidate.directoryOnly)
                return null;
            var file = new RootedFilePath(candidate.path.AsString());
            if (jsonEnabled && file.ExtensionIs(".json") && fs.FileExists(file))
                return new ResolvedData
                {
                    path = file,
                    extension = ".json"
                };
            return loadModuleFromFileNoImplicitExtensions(candidate, jsonEnabled);
        }

        private (resolutionCandidate Candidate, bool Ok) getPackageFile(InfoCacheEntry info)
        {
            var field = info.Contents!.Fields.TSConfig;
            if (!field.IsPresent() || !field.IsValid() || field.Value == "")
                return (default, false);
            return (resolutionCandidateFromDirectoryPath(info.PackageDirectory.AsDirectoryPath(), field.Value), true);
        }

        private ResolvedData? tryLoadModuleUsingPaths(string moduleName, RootedDirectoryPath directory, OrderedMap<string, List<string>> paths, Func<resolutionCandidate, ResolvedData?> loader)
        {
            Pattern matched = MatchPatternOrExact(TryParsePatterns(paths), moduleName);
            if (!matched.IsValid())
                return null;
            string matchedKey = matched.Text;
            string matchedStar = matched.MatchedText(moduleName);
            foreach (string substitution in paths.GetOrZero(matchedKey))
            {
                int star = substitution.IndexOf('*');
                string path = star >= 0 ? substitution[..star] + matchedStar + substitution[(star + 1)..] : substitution;
                var candidate = resolutionCandidateFromDirectoryPath(directory, path);
                string extension = TsPath.TryGetExtensionFromPath(substitution);
                var file = new RootedFilePath(candidate.path.AsString());
                if (extension != "" && !candidate.directoryOnly && fs.FileExists(file))
                    return new ResolvedData
                    {
                        path = file,
                        extension = extension
                    };
                var result = loader(candidate);
                if (!shouldContinueSearching(result))
                    return result;
            }

            return null;
        }

        private PackageId getPackageId(RootedFilePath file, InfoCacheEntry? info)
        {
            if (info?.Contents != null)
            {
                var(packageName, nameOK) = info.Contents.Fields.Name.GetValue();
                var(version, versionOK) = info.Contents.Fields.Version.GetValue();
                if (nameOK && versionOK)
                {
                    var(relative, ok) = file.RelativeTo(info.PackageDirectory.AsDirectoryPath());
                    return new PackageId
                    {
                        Name = packageName,
                        Version = version,
                        SubModuleName = ok ? relative.AsString() : "",
                        PeerDependencies = readPackageJsonPeerDependencies(info)
                    };
                }
            }

            return new PackageId();
        }

        private string readPackageJsonPeerDependencies(InfoCacheEntry info)
        {
            var peers = info.Contents!.Fields.PeerDependencies;
            if (!peers.IsValid() || peers.Value is not { Count: > 0 })
                return "";
            var packageDirectory = new RootedFilePath(fs.Realpath(info.PackageDirectory.AsDirectoryPath().AsPath()).AsString());
            var(_, nodeModules, ok) = packageDirectory.SplitAtLastComponent("node_modules");
            if (!ok)
                return "";
            var builder = new GoBuilder();
            foreach (string name in peers.Value.Keys.Order(StringComparer.Ordinal))
            {
                var peer = getPackageJsonInfo(packageCache.PackageDirectory(nodeModules.ResolveDirectory(pathForDynamicResolution(nodeModules, name, true))));
                if (peer?.Contents == null)
                    continue;
                builder.WriteString("+" + name + "@" + peer.Contents.Fields.Version.Value);
            }

            return builder.String();
        }

        internal ResolvedModule createResolvedModuleHandlingSymlink(ResolvedData? found)
        {
            bool external = found?.path.ContainsLowercaseDirectorySequence("/node_modules/") == true;
            if (options.PreserveSymlinks != Tristate.True && external && found!.originalPath == default && !TsPath.PathIsRelative(name) && !TsPath.PathIsAbsolute(name))
            {
                var real = new RootedFilePath(fs.Realpath(found.path.AsPath()).AsString());
                if (fs.CaseSensitivity().CompareFilePaths(found.path, real) != 0)
                {
                    found.originalPath = found.path;
                    found.path = real;
                }
            }

            return createResolvedModule(found, external);
        }

        private ResolvedModule createResolvedModule(ResolvedData? found, bool external)
        {
            var result = new ResolvedModule();
            if (found == null)
                return result;
            if (found.isResolved())
            {
                result.ResolvedFileName = found.path.AsString();
                result.ResolvedPath = fs.CaseSensitivity().PathKey(found.path.AsPath()).AsString();
                result.OriginalPath = found.originalPath.AsString();
            }

            result.Extension = found.extension;
            result.PackageId = found.packageId;
            result.IsExternalLibraryImport = external;
            return result;
        }
    }

    private static bool matchesPatternWithTrailer(string target, string name)
    {
        int star = target.IndexOf('*');
        return star >= 0 && name.Length >= target.Length - 1 && name.StartsWith(target[..star], StringComparison.Ordinal) && name.EndsWith(target[(star + 1)..], StringComparison.Ordinal);
    }
}

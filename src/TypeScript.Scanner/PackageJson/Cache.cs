// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/packagejson/cache.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Monitor locks replace sync.Once/SyncMap;
// actual value/cache/presentation behavior is retained.
using TypeScript.Collections;
using TypeScript.Scanner;
using S = TypeScript.Semver.Semver;

namespace TypeScript.PackageJson;
public sealed class PackageJson
{
    public Fields Fields;
    public bool Parseable;
    public ExportsOrImports Imports { get => Fields.Imports; set => Fields.Imports = value; }
    public ExportsOrImports Exports { get => Fields.Exports; set => Fields.Exports = value; }

    private readonly object once = new();
    private bool initialized;
    private VersionPaths versionPaths;
    private List<(Message Message, object[] Args)>? versionTraces;
    private static readonly TypeScript.Semver.Version typeScriptVersion = S.MustParse(Core.Version());
    public VersionPaths GetVersionPaths(Action<Message, object[]>? trace = null)
    {
        lock (once)
        {
            if (!initialized)
            {
                initializeVersionPaths();
                initialized = true;
            }
        }

        if (trace != null)
            foreach (var(message, args)in versionTraces ?? [])
                trace(message, args);
        return versionPaths;
    }

    private void initializeVersionPaths()
    {
        void log(Message message, params object[] args) => (versionTraces ??= new()).Add((message, args));
        var versions = Fields.TypesVersions;
        if (versions.Type == JSONValueType.NotPresent)
        {
            log(Diagnostics.X_package_json_does_not_have_a_0_field, "typesVersions");
            return;
        }

        if (versions.Type != JSONValueType.Object)
        {
            log(Diagnostics.Expected_type_of_0_field_in_package_json_to_be_1_got_2, "typesVersions", "object", versions.Type.String());
            return;
        }

        log(Diagnostics.X_package_json_has_a_typesVersions_field_with_version_specific_path_mappings, "typesVersions");
        foreach (var(key, value)in versions.AsObject().Entries())
        {
            var(range, ok) = S.TryParseVersionRange(key);
            if (!ok)
            {
                log(Diagnostics.X_package_json_has_a_typesVersions_entry_0_that_is_not_a_valid_semver_range, key);
                continue;
            }

            if (!range.Test(typeScriptVersion))
                continue;
            if (value.Type != JSONValueType.Object)
            {
                log(Diagnostics.Expected_type_of_0_field_in_package_json_to_be_1_got_2, "typesVersions['" + key + "']", "object", value.Type.String());
                return;
            }

            versionPaths = new VersionPaths
            {
                Version = key,
                pathsJSON = value.AsObject()
            };
            return;
        }

        log(Diagnostics.X_package_json_does_not_have_a_typesVersions_entry_that_matches_version_0, Core.VersionMajorMinor());
    }
}

public struct VersionPaths
{
    private string? version;
    public string Version { readonly get => version ?? ""; set => version = value; }

    internal OrderedMap<string, JSONValue>? pathsJSON;
    private OrderedMap<string, List<string>>? paths;
    public readonly bool Exists() => Version != "" && pathsJSON != null;
    public OrderedMap<string, List<string>>? GetPaths()
    {
        if (!Exists())
            return null;
        if (paths != null)
            return paths;
        var result = new OrderedMap<string, List<string>>(pathsJSON!.Size());
        foreach (var(key, value)in pathsJSON.Entries())
        {
            if (value.Type != JSONValueType.Array)
                continue;
            var slice = Enumerable.Repeat("", value.AsArray()?.Count ?? 0).ToList();
            for (int i = 0; i < slice.Count; i++)
            {
                JSONValue path = value.AsArray()![i];
                if (path.Type == JSONValueType.String)
                    slice[i] = (string)path.Value!;
            }

            result.Set(key, slice);
        }

        paths = result;
        return paths;
    }
}

public readonly record struct PackageDirectory
{
    private readonly RootedDirectoryPath name;
    private readonly PathKey key;
    internal PackageDirectory(RootedDirectoryPath name, PathKey key)
    {
        this.name = name;
        this.key = key;
    }

    public string String() => name.AsString();
    public override string ToString() => String();
    public RootedDirectoryPath AsDirectoryPath() => name;
    public PathKey PathKey() => key;
    public PackageDirectory Parent()
    {
        var parentName = name.AsPath().Directory();
        var parentKey = key.Parent();
        return parentName == name ? this : new PackageDirectory(parentName, parentKey);
    }

    public RootedFilePath ResolveFile(string path) => name.ResolveFile(path);
}

public static class PackageDirectories
{
    public static PackageDirectory NewPackageDirectory(RootedDirectoryPath directory, CaseSensitivity caseSensitivity) => new(directory, caseSensitivity.PathKey(directory.AsPath()));
    public static T ForEachAncestorDirectoryStoppingAtGlobalCache<T>(RootedDirectoryPath globalCache, PackageDirectory directory, Func<PackageDirectory, (T Result, bool Stop)> callback)
    {
        for (;;)
        {
            var(result, stop) = callback(directory);
            if (stop || directory.AsDirectoryPath() == globalCache)
                return result;
            var parent = directory.Parent();
            if (parent == directory)
                return typeof(T) == typeof(string) ? (T)(object)"" : default!;
            directory = parent;
        }
    }
}

public sealed class InfoCacheEntry
{
    public PackageDirectory PackageDirectory;
    public bool DirectoryExists;
    public PackageJson? Contents;
    public bool Exists() => Contents != null;
    public PackageJson? GetContents() => Contents;
    public InfoCacheEntry WithPackageDirectory(PackageDirectory directory) => PackageDirectory == directory ? this : new InfoCacheEntry
    {
        PackageDirectory = directory,
        DirectoryExists = DirectoryExists,
        Contents = Contents
    };
}

public sealed class InfoCache
{
    private readonly Dictionary<PathKey, InfoCacheEntry> cache = new();
    private readonly CaseSensitivity caseSensitivity;
    private readonly object gate = new();
    public InfoCache(CaseSensitivity caseSensitivity) => this.caseSensitivity = caseSensitivity;
    public InfoCache Clone()
    {
        var clone = new InfoCache(caseSensitivity);
        lock (gate)
            foreach (var(key, value)in cache)
                clone.cache.Add(key, value);
        return clone;
    }

    public PackageDirectory PackageDirectory(RootedDirectoryPath directory) => PackageDirectories.NewPackageDirectory(directory, caseSensitivity);
    public CaseSensitivity CaseSensitivity() => caseSensitivity;
    public InfoCacheEntry? Get(PackageDirectory directory)
    {
        lock (gate)
            return cache.GetValueOrDefault(directory.PathKey());
    }

    public InfoCacheEntry Set(PackageDirectory directory, InfoCacheEntry info)
    {
        lock (gate)
        {
            if (cache.TryGetValue(directory.PathKey(), out var actual))
                return actual;
            cache.Add(directory.PathKey(), info);
            return info;
        }
    }

    public void Range(Func<PathKey, InfoCacheEntry, bool> callback)
    {
        List<KeyValuePair<PathKey, InfoCacheEntry>> snapshot;
        lock (gate)
            snapshot = cache.ToList();
        foreach (var(key, value)in snapshot)
            if (!callback(key, value))
                return;
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/wildcarddirectories.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Vfs.VfsMatch;

namespace TypeScript.TsOptions;
public static partial class TsOptions
{
    internal static Dictionary<RootedDirectoryPath, bool>? getWildcardDirectories(List<PathPattern>? include, List<PathPattern>? exclude, RootedDirectoryPath currentDirectory, CaseSensitivity caseSensitivity)
    {
        if ((include?.Count ?? 0) == 0)
            return null;
        var excludeMatcher = VfsMatch.NewSpecMatcher(exclude, currentDirectory, Usage.Exclude, caseSensitivity);
        var directories = new Dictionary<RootedDirectoryPath, bool>();
        var keyToPath = new Dictionary<PathKey, RootedDirectoryPath>();
        List<PathKey>? recursiveKeys = null;
        foreach (var file in include!)
        {
            string spec = TsPath.NormalizePath(TsPath.CombinePaths(currentDirectory.AsString(), file.AsString()));
            if (excludeMatcher?.MatchString(spec) == true)
                continue;
            var match = getWildcardDirectoryFromSpec(spec);
            if (match != null)
            {
                var path = match.Path;
                if (path.AsString() == "")
                    path = TsPath.RootedDirectoryPathFromNormalized(spec[..TsPath.GetRootLength(spec)]);
                var key = caseSensitivity.PathKey(path.AsPath());
                bool exists = keyToPath.TryGetValue(key, out var existingPath);
                bool existingRecursive = exists && directories.TryGetValue(existingPath, out var oldRecursive) && oldRecursive;
                if (!exists || !existingRecursive && match.Recursive)
                {
                    directories[exists ? existingPath : path] = match.Recursive;
                    if (!exists)
                        keyToPath[key] = path;
                    if (match.Recursive)
                        (recursiveKeys ??= new()).Add(key);
                }
            }

            foreach (var path in directories.Keys.ToList())
                if (recursiveKeys != null)
                    foreach (var recursiveKey in recursiveKeys)
                    {
                        var key = caseSensitivity.PathKey(path.AsPath());
                        if (key != recursiveKey && recursiveKey.ContainsPath(key))
                            directories.Remove(path);
                    }
        }

        return directories;
    }

    internal sealed class wildcardDirectoryMatch
    {
        internal RootedDirectoryPath Path;
        internal bool Recursive;
    }

    internal static wildcardDirectoryMatch? getWildcardDirectoryFromSpec(string spec)
    {
        int firstWildcard = spec.IndexOfAny(['*', '?']);
        if (firstWildcard >= 0)
        {
            int separator = spec[..firstWildcard].LastIndexOf('/');
            if (separator >= 0)
                return new()
                {
                    Path = separator > 0 ? TsPath.RootedDirectoryPathFromAbsolute(spec[..separator]) : default,
                    Recursive = firstWildcard < spec.LastIndexOf('/')
                };
        }

        int lastSeparator = spec.LastIndexOf('/');
        return lastSeparator >= 0 && VfsMatch.IsImplicitGlob(spec[(lastSeparator + 1)..]) ? new()
        {
            Path = TsPath.RootedDirectoryPathFromAbsolute(TsPath.RemoveTrailingDirectorySeparator(spec)),
            Recursive = true
        }

        : null;
    }
}

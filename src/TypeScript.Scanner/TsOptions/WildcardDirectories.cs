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
            var spec = TsPath.NormalizePath(TsPath.CombinePaths(currentDirectory.AsValue(), file.AsValue()));
            if (excludeMatcher?.MatchValue(spec) == true)
                continue;
            var match = getWildcardDirectoryFromSpec(spec);
            if (match != null)
            {
                var path = match.Path;
                if (path.AsValue().Length == 0)
                    path = TsPath.RootedDirectoryPathFromNormalized(spec.Slice(0, TsPath.GetRootLength(spec)));
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

    internal static wildcardDirectoryMatch? getWildcardDirectoryFromSpec(string spec) => getWildcardDirectoryFromSpec(ByteStringValue.FromString(spec));
    internal static wildcardDirectoryMatch? getWildcardDirectoryFromSpec(ByteStringValue spec)
    {
        int firstWildcard = spec.IndexOfAny('*', '?');
        if (firstWildcard >= 0)
        {
            int separator = spec.Slice(0, firstWildcard).LastIndexOf('/');
            if (separator >= 0)
                return new()
                {
                    Path = separator > 0 ? TsPath.RootedDirectoryPathFromAbsolute(spec.Slice(0, separator)) : default,
                    Recursive = firstWildcard < spec.LastIndexOf('/')
                };
        }

        int lastSeparator = spec.LastIndexOf('/');
        return lastSeparator >= 0 && VfsMatch.IsImplicitGlob(spec.Slice(lastSeparator + 1)) ? new()
        {
            Path = TsPath.RootedDirectoryPathFromAbsolute(TsPath.RemoveTrailingDirectorySeparator(spec)),
            Recursive = true
        }

        : null;
    }
}

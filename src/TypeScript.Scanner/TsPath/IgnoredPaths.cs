// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/tspath/ignoredpaths.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
public static partial class TsPath
{
    private static readonly string[] ignoredPaths = ["/node_modules/.", "/.git", ".#"];
    internal static bool containsIgnoredPath(ByteStringValue path)
    {
        foreach (var pattern in ignoredPaths)
        if (path.IndexOf(ByteStringValue.FromString(pattern)) >= 0)
        {
            return true;
        }
        return false;
    }
    public static bool ContainsIgnoredPath(RootedPath path) => containsIgnoredPath(path.AsValue());
    public static bool ContainsIgnoredDirectory(RootedDirectoryPath directory) => containsIgnoredPath(directory.AsValue());
    public static bool ContainsIgnoredPathKey(PathKey path) => containsIgnoredPath(path.AsValue());
}

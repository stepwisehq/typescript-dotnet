// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/core/version.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// The fixed upstream version is not overridden by Go ldflags in this managed build.
namespace TypeScript.Scanner;
internal static partial class Core
{
    private static readonly string version = "7.1.0-dev";
    private static readonly string versionMajorMinor = getVersionMajorMinor();
    public static string Version() => version;
    public static string VersionMajorMinor() => versionMajorMinor;
    private static string getVersionMajorMinor()
    {
        bool seenMajor = false;
        for (int i = 0; i < version.Length; i++)
        {
            if (version[i] != '.')
                continue;
            if (seenMajor)
                return version[..i];
            seenMajor = true;
        }

        throw new InvalidOperationException("invalid version string: " + version);
    }
}

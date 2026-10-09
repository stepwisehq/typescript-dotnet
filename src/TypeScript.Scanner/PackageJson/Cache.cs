// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/packagejson/cache.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Selected case-sensitive package info.
using System.Text.Json;

namespace TypeScript.PackageJson;
internal sealed class PackageJson
{
    internal JsonElement Imports;
    internal bool Parseable;
}

internal sealed class InfoCacheEntry
{
    internal string PackageDirectory = "";
    internal bool DirectoryExists;
    internal PackageJson? Contents;
    internal bool Exists() => Contents != null;
    internal PackageJson? GetContents() => Contents;
}

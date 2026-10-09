// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial port of tsc/internal/module/types.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Paths use canonical string storage.
using TypeScript.Scanner;

namespace TypeScript.Modules;
public sealed class PackageId
{
    public string Name = "", SubModuleName = "", Version = "", PeerDependencies = "";
    public string PackageName() => SubModuleName != "" ? Name + "/" + SubModuleName : Name;
    public override string ToString() => PackageName() + "@" + Version + PeerDependencies;
}

public sealed class ResolvedModule
{
    public List<Diagnostic>? ResolutionDiagnostics;
    public string ResolvedFileName = "", ResolvedPath = "", OriginalPath = "", Extension = "";
    public bool ResolvedUsingTsExtension, ResolvedUsingExtraExtensions, IsExternalLibraryImport, IsCustomResolution;
    public PackageId PackageId = new();
    public string AlternateResult = "";
    public bool IsResolved() => ResolvedFileName != "";
}

public readonly record struct ModeAwareCacheKey(string Name, ResolutionMode Mode);

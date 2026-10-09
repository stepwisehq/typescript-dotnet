// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of metadata models/methods in tsc/internal/contentmapper/contentmapper.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Process execution, IPC, transformation and virtual-source runtime are external to this unit.
using TypeScript.Json;
using TypeScript.Scanner;

namespace TypeScript.ContentMapper;
public class Definition
{
    public string Package = "";
    public List<string>? Extensions;
    public JsonValue? Options;
}

public sealed class Manifest
{
    public string Name = "", Version = "";
    public List<string>? Exec, CompilerOptions;
    public bool DynamicConfig;
}

// Configuration metadata for locating an option diagnostic; external mapper execution is not invoked.
public readonly record struct OptionPathSegment(string Property, int Index, bool IsIndex);
public sealed class Mapper : Definition
{
    public Manifest Manifest = new();
    public string Name { get => Manifest.Name; set => Manifest.Name = value; }
    public string Version { get => Manifest.Version; set => Manifest.Version = value; }
    public List<string>? Exec { get => Manifest.Exec; set => Manifest.Exec = value; }
    public List<string>? CompilerOptions { get => Manifest.CompilerOptions; set => Manifest.CompilerOptions = value; }
    public bool DynamicConfig { get => Manifest.DynamicConfig; set => Manifest.DynamicConfig = value; }

    public RootedDirectoryPath PackageDirectory;
    public string ContributionID = "";
    public string DiagnosticName() => Name != "" ? Name : Package != "" ? Package : ContributionID;
    public string Identity() => ContributionID != "" ? ContributionID + " (" + manifestIdentity() + ")" : manifestIdentity();
    private string manifestIdentity() => Name == "" ? "" : Version == "" ? Name : Name + "@" + Version;
    public bool Equals(Mapper? other) => ReferenceEquals(this, other) || other != null && Package == other.Package && (Extensions == null) == (other.Extensions == null) && OptionModelEquality.SlicesEqual(Extensions, other.Extensions) && (Options?.Bytes is null) == (other.Options?.Bytes is null) && (Options?.Bytes is null || other.Options?.Bytes is not null && Options.Bytes.SequenceEqual(other.Options.Bytes)) && Name == other.Name && Version == other.Version && (Exec == null) == (other.Exec == null) && OptionModelEquality.SlicesEqual(Exec, other.Exec) && (CompilerOptions == null) == (other.CompilerOptions == null) && OptionModelEquality.SlicesEqual(CompilerOptions, other.CompilerOptions) && DynamicConfig == other.DynamicConfig && PackageDirectory == other.PackageDirectory && ContributionID == other.ContributionID;
}

public static class ContentMappers
{
    private static readonly HashSet<string> supportedVirtualExtensions = new()
    {
        ".js",
        ".jsx",
        ".mjs",
        ".cjs",
        ".ts",
        ".tsx",
        ".mts",
        ".cts",
        ".json"
    };
    public static bool IsSupportedVirtualExtension(string extension) => supportedVirtualExtensions.Contains(extension);
}

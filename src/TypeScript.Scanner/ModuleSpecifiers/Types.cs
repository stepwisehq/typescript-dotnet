// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/modulespecifiers/types.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Selected project diagnostic generation.
using TypeScript.PackageJson;
using TypeScript.Modules;
using TypeScript.Scanner;

namespace TypeScript.ModuleSpecifiers;
internal enum ResultKind : byte
{
    None,
    NodeModules,
    Paths,
    Redirect,
    Relative,
    Ambient
}

internal sealed class ModuleSpecifiersResult
{
    internal Slice<ModuleSpecifier> Specifiers;
    internal ResultKind Kind;
    internal Symbol? AmbientModuleSymbol = null;
}

internal readonly record struct ModulePath(RootedFilePath FileName, bool IsInNodeModules, bool IsRedirect);
internal readonly record struct ImportModuleSpecifierPreference
{
    private readonly string value;
    internal ImportModuleSpecifierPreference(string value) => this.value = value;
    internal string Value => value ?? "";

    public bool Equals(ImportModuleSpecifierPreference other) => Value == other.Value;
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    internal static readonly ImportModuleSpecifierPreference None = new("");
    internal static readonly ImportModuleSpecifierPreference Shortest = new("shortest");
    internal static readonly ImportModuleSpecifierPreference ProjectRelative = new("project-relative");
    internal static readonly ImportModuleSpecifierPreference Relative = new("relative");
    internal static readonly ImportModuleSpecifierPreference NonRelative = new("non-relative");
}

internal readonly record struct ImportModuleSpecifierEndingPreference
{
    private readonly string value;
    internal ImportModuleSpecifierEndingPreference(string value) => this.value = value;
    internal string Value => value ?? "";

    public bool Equals(ImportModuleSpecifierEndingPreference other) => Value == other.Value;
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    internal static readonly ImportModuleSpecifierEndingPreference None = new("");
    internal static readonly ImportModuleSpecifierEndingPreference Auto = new("auto");
    internal static readonly ImportModuleSpecifierEndingPreference Minimal = new("minimal");
    internal static readonly ImportModuleSpecifierEndingPreference Index = new("index");
    internal static readonly ImportModuleSpecifierEndingPreference Js = new("js");
}

internal struct UserPreferences
{
    internal ImportModuleSpecifierPreference ImportModuleSpecifierPreference;
    internal ImportModuleSpecifierEndingPreference ImportModuleSpecifierEnding;
    internal Slice<string> AutoImportSpecifierExcludeRegexes;
}

internal sealed class ModuleSpecifierOptions
{
    internal ResolutionMode OverrideImportMode;
}

internal enum RelativePreferenceKind : byte
{
    Relative,
    NonRelative,
    Shortest,
    ExternalNonRelative
}

internal enum ModuleSpecifierEnding : byte
{
    Minimal,
    Index,
    JsExtension,
    TsExtension
}

// types.go:51. Paths and names use actual upstream Go-byte value roles.
internal interface IModuleSpecifierGenerationHost : TypeScript.OutputPaths.IOutputPathsHost
{
    TypeScript.Symlinks.KnownSymlinks GetSymlinkCache();
    RootedDirectoryPath GetGlobalTypingsCacheLocation();
    RootedDirectoryPath BaseDirectory();
    TypeScript.TsOptions.SourceOutputAndProjectReference? GetProjectReferenceFromSource(PathKey path);
    Slice<RootedFilePath> GetRedirectTargets(PathKey path);
    RootedFilePath GetSourceOfProjectReferenceIfOutputIncluded(HasFileName file);
    bool FileExists(RootedFilePath path);
    RootedDirectoryPath GetNearestAncestorDirectoryWithPackageJson(RootedDirectoryPath directory);
    InfoCacheEntry? GetPackageJsonInfo(RootedFilePath path);
    ResolutionMode GetDefaultResolutionModeForFile(HasFileName file);
    ResolvedModule? GetResolvedModuleFromModuleSpecifier(HasFileName file, Node specifier);
    ResolutionMode GetModeForUsageLocation(HasFileName file, Node specifier);
}

internal enum MatchingMode : byte
{
    Exact,
    Directory,
    Pattern
}

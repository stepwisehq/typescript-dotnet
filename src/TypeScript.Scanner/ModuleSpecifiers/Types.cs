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
    internal List<string>? Specifiers;
    internal ResultKind Kind;
    internal Symbol? AmbientModuleSymbol = null;
}

internal readonly record struct ModulePath(string FileName, bool IsInNodeModules, bool IsRedirect);
internal enum ImportModuleSpecifierPreference
{
    None,
    Shortest,
    ProjectRelative,
    Relative,
    NonRelative
}

internal enum ImportModuleSpecifierEndingPreference
{
    None,
    Auto,
    Minimal,
    Index,
    Js
}

internal sealed class UserPreferences
{
    internal ImportModuleSpecifierPreference ImportModuleSpecifierPreference;
    internal ImportModuleSpecifierEndingPreference ImportModuleSpecifierEnding;
    internal List<string>? AutoImportSpecifierExcludeRegexes = null;
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

internal interface IModuleSpecifierGenerationHost
{
    // Explicit compatibility role: legacy project names are Unicode; genuine
    // graph paths and upstream typed path strings are Go byte strings.
    bool FileNamesAreGoBytes => false;
    CaseSensitivity CaseSensitivity() => TypeScript.Scanner.CaseSensitivity.CaseSensitive;
    IReadOnlyList<string> GetRedirectTargets(PathKey path) => [];
    string BaseDirectory();
    string GetSourceOfProjectReferenceIfOutputIncluded(SourceFile file);
    bool FileExists(string path);
    string GetNearestAncestorDirectoryWithPackageJson(string directory);
    InfoCacheEntry? GetPackageJsonInfo(string path);
    ResolutionMode GetDefaultResolutionModeForFile(SourceFile file);
    ResolvedModule? GetResolvedModuleFromModuleSpecifier(SourceFile file, Node specifier);
    ResolutionMode GetModeForUsageLocation(SourceFile file, Node specifier);
}

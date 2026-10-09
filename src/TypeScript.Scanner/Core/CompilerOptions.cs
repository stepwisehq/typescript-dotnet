// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/core/compileroptions.go option accessors; noCopy vet marker has no runtime behavior.
namespace TypeScript.Scanner;
public enum ResolutionMode
{
    None = 0,
    CommonJS = 1,
    ESNext = 99
}

// ResolutionMode remains a distinct managed enum for existing checker API compatibility.
public sealed partial class CompilerOptions
{
    // Upstream compileroptions.go:23.
    public ScriptTarget GetEmitScriptTarget()
    {
        if (Target != ScriptTarget.None)
            return Target;
        return ScriptTarget.LatestStandard;
    }

    // Upstream compileroptions.go:30.
    public ModuleKind GetEmitModuleKind()
    {
        if (Module != ModuleKind.None)
            return Module;
        var target = GetEmitScriptTarget();
        if (target == ScriptTarget.ESNext)
            return ModuleKind.ESNext;
        if (target >= ScriptTarget.ES2022)
            return ModuleKind.ES2022;
        if (target >= ScriptTarget.ES2020)
            return ModuleKind.ES2020;
        if (target >= ScriptTarget.ES2015)
            return ModuleKind.ES2015;
        return ModuleKind.CommonJS;
    }

    // compileroptions.go:51–111.
    public ModuleResolutionKind GetModuleResolutionKind() => ModuleResolution switch
    {
        ModuleResolutionKind.Unknown or ModuleResolutionKind.Classic or ModuleResolutionKind.Node10 => GetEmitModuleKind() switch
        {
            ModuleKind.Node16 or ModuleKind.Node18 or ModuleKind.Node20 => ModuleResolutionKind.Node16,
            ModuleKind.NodeNext => ModuleResolutionKind.NodeNext,
            _ => ModuleResolutionKind.Bundler
        },
        _ => ModuleResolution
    };
    public bool GetResolvePackageJsonExports() => ResolvePackageJsonExports != Tristate.False;
    public bool GetResolvePackageJsonImports() => ResolvePackageJsonImports != Tristate.False;
    public bool GetAllowImportingTsExtensions() => AllowImportingTsExtensions == Tristate.True || RewriteRelativeImportExtensions == Tristate.True;
    public bool AllowImportingTsExtensionsFrom(string fileName) => GetAllowImportingTsExtensions() || fileName.IsDeclarationFile();
    public bool GetResolveJsonModule() => ResolveJsonModule != Tristate.Unknown ? ResolveJsonModule == Tristate.True : GetEmitModuleKind()is ModuleKind.Node20 or ModuleKind.NodeNext || GetModuleResolutionKind() == ModuleResolutionKind.Bundler;
    public bool ShouldPreserveConstEnums() => PreserveConstEnums == Tristate.True || GetIsolatedModules();
    public bool GetAllowJS() => AllowJs != Tristate.Unknown ? AllowJs == Tristate.True : CheckJs == Tristate.True;
    public bool AllowImportingTsExtensionsFrom(RootedFilePath fileName) => GetAllowImportingTsExtensions() || fileName.IsDeclarationFile();
    public ModuleDetectionKind GetEmitModuleDetectionKind()
    {
        if (ModuleDetection != ModuleDetectionKind.None)
            return ModuleDetection;
        var kind = GetEmitModuleKind();
        return kind >= ModuleKind.Node16 && kind <= ModuleKind.NodeNext ? ModuleDetectionKind.Force : ModuleDetectionKind.Auto;
    }

    public bool GetJSXTransformEnabled() => Jsx is JsxEmit.React or JsxEmit.ReactJSX or JsxEmit.ReactJSXDev;
    public (List<RootedDirectoryPath> Result, bool FromConfig) GetEffectiveTypeRoots(RootedDirectoryPath currentDirectory)
    {
        if (TypeRoots != null)
            return (TypeRoots, true);
        var baseDirectory = ConfigFilePath != default ? ConfigFilePath.Directory() : currentDirectory;
        if (baseDirectory == default)
            throw new InvalidOperationException("cannot get effective type roots without a config file path or current directory");
        var result = new List<RootedDirectoryPath>();
        baseDirectory.ForEachAncestorDirectory<object?>((directory) =>
        {
            result.Add(directory.ResolveDirectory("node_modules/@types"));
            return (null, false);
        });
        return (result, false);
    }

    public bool IsIncremental() => Incremental == Tristate.True || Composite == Tristate.True;
    public bool GetUseDefineForClassFields() => UseDefineForClassFields == Tristate.Unknown ? GetEmitScriptTarget() >= ScriptTarget.ES2022 : UseDefineForClassFields == Tristate.True;
    public bool GetEmitDeclarations() => Declaration == Tristate.True || Composite == Tristate.True;
    public bool GetAreDeclarationMapsEnabled() => DeclarationMap == Tristate.True && GetEmitDeclarations();
    public bool HasJsonModuleEmitEnabled() => GetEmitModuleKind()is not (ModuleKind.System or ModuleKind.UMD);
    public List<RootedDirectoryPath>? GetEffectiveRootDirs() => RootDirs;
    public RootedDirectoryPath GetPathsBasePath(RootedDirectoryPath currentDirectory) => (Paths?.Size() ?? 0) == 0 ? default : PathsBasePath != default ? PathsBasePath : currentDirectory;
    // Upstream compileroptions.go:122.
    public bool GetStrictOptionValue(Tristate value)
    {
        if (value != Tristate.Unknown)
            return value == Tristate.True;
        return Strict != Tristate.False;
    }

    // Upstream compileroptions.go:153.
    public bool UsesWildcardTypes() => Types?.Contains("*") == true;
    // Upstream compileroptions.go:157.
    public bool GetIsolatedModules() => IsolatedModules == Tristate.True || VerbatimModuleSyntax == Tristate.True;
    // Upstream compileroptions.go:166.
    public bool GetEmitStandardClassFields() => UseDefineForClassFields != Tristate.False && GetEmitScriptTarget() >= ScriptTarget.ES2022;
}

public static class CompilerOptionHelpers
{
    public static readonly CompilerOptions EmptyCompilerOptions = new();
    public static bool IsNonNodeESM(this ModuleKind kind) => kind >= ModuleKind.ES2015 && kind <= ModuleKind.ESNext;
    public static bool SupportsImportAttributes(this ModuleKind kind) => kind >= ModuleKind.Node18 && kind <= ModuleKind.NodeNext || kind is ModuleKind.Preserve or ModuleKind.ESNext;
    public static bool SupportsDeferredImports(this ModuleKind kind) => kind is ModuleKind.ESNext or ModuleKind.Preserve;
    public static bool SupportsSourcePhaseImports(this ModuleKind kind) => kind is ModuleKind.ESNext or ModuleKind.NodeNext or ModuleKind.Preserve;
    public static NewLineKind GetNewLineKind(string text) => text switch
    {
        "\r\n" => NewLineKind.CRLF,
        "\n" => NewLineKind.LF,
        _ => NewLineKind.None
    };
    public static string GetNewLineCharacter(this NewLineKind kind) => kind == NewLineKind.CRLF ? "\r\n" : "\n";
}

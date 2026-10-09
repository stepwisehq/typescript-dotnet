// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/module/util.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.Modules;
public static class ModuleUtilities
{
    private static readonly TypeScript.Semver.Version typeScriptVersion = TypeScript.Semver.Semver.MustParse(Core.Version());
    public const string InferredTypesContainingFile = "__inferred type names__.ts";

    public static Message? GetResolutionDiagnostic(CompilerOptions options, ResolvedModule resolvedModule, SourceFile file)
    {
        if (resolvedModule.ResolvedUsingExtraExtensions)
            return null;

        Message? NeedJsx() => options.Jsx != JsxEmit.None ? null : Diagnostics.Module_0_was_resolved_to_1_but_jsx_is_not_set;
        Message? NeedAllowJs()
        {
            Tristate noImplicitAny = options.NoImplicitAny != Tristate.Unknown ? options.NoImplicitAny : options.Strict;
            return options.GetAllowJS() || noImplicitAny != Tristate.True ? null : Diagnostics.Could_not_find_a_declaration_file_for_module_0_1_implicitly_has_an_any_type;
        }

        return resolvedModule.Extension switch
        {
            TsPath.ExtensionTs or TsPath.ExtensionDts or TsPath.ExtensionMts or TsPath.ExtensionDmts or TsPath.ExtensionCts or TsPath.ExtensionDcts => null,
            TsPath.ExtensionTsx => NeedJsx(),
            TsPath.ExtensionJsx => NeedJsx() ?? NeedAllowJs(),
            TsPath.ExtensionJs or TsPath.ExtensionMjs or TsPath.ExtensionCjs => NeedAllowJs(),
            TsPath.ExtensionJson => options.GetResolveJsonModule() ? null : Diagnostics.Module_0_was_resolved_to_1_but_resolveJsonModule_is_not_used,
            _ => file.IsDeclarationFile || options.AllowArbitraryExtensions == Tristate.True ? null : Diagnostics.Module_0_was_resolved_to_1_but_allowArbitraryExtensions_is_not_set
        };
    }

    public static bool IsApplicableVersionedTypesKey(string key)
    {
        if (!key.StartsWith("types@", StringComparison.Ordinal))
            return false;
        var(range, ok) = TypeScript.Semver.Semver.TryParseVersionRange(key[6..]);
        return ok && range.Test(typeScriptVersion);
    }

    public static (string PackageName, string Remaining) ParsePackageName(string name)
    {
        int index = name.IndexOf('/');
        if (name.StartsWith('@'))
        {
            int offset = index + 1;
            index = name[offset..].IndexOf('/');
            if (index >= 0)
                index += offset;
        }

        return index < 0 ? (name, "") : (name[..index], name[(index + 1)..]);
    }

    public static int ComparePatternKeys(string a, string b)
    {
        int ai = a.IndexOf('*'), bi = b.IndexOf('*');
        int aBase = ai < 0 ? a.Length : ai + 1, bBase = bi < 0 ? b.Length : bi + 1;
        if (aBase > bBase)
            return -1;
        if (bBase > aBase)
            return 1;
        if (ai < 0)
            return 1;
        if (bi < 0)
            return -1;
        return Math.Sign(b.Length - a.Length);
    }

    public static RootedDirectoryPath NodeModulePackageRootForFile(RootedFilePath path) => parseNodeModulePackageRoot(path.AsString(), false);
    public static RootedDirectoryPath NodeModulePackageRootForDirectory(RootedDirectoryPath path) => parseNodeModulePackageRoot(path.AsString(), true);
    private static RootedDirectoryPath parseNodeModulePackageRoot(string path, bool isDirectory)
    {
        int index = path.LastIndexOf("/node_modules/", StringComparison.Ordinal);
        if (index < 0)
            return default;
        int start = index + "/node_modules/".Length;
        int end = DefaultResolver.MoveToNextDirectorySeparatorIfAvailable(path, start, isDirectory);
        if (path[start] == '@')
            end = DefaultResolver.MoveToNextDirectorySeparatorIfAvailable(path, end, isDirectory);
        return TsPath.RootedDirectoryPathFromAbsolute(path[..end]);
    }

    public static string MangleScopedPackageName(string name)
    {
        if (!name.StartsWith('@'))
            return name;
        int separator = name.IndexOf('/');
        return separator < 0 ? name : name[1..separator] + "__" + name[(separator + 1)..];
    }

    public static string UnmangleScopedPackageName(string name)
    {
        int separator = name.IndexOf("__", StringComparison.Ordinal);
        return separator < 0 ? name : "@" + name[..separator] + "/" + name[(separator + 2)..];
    }

    public static string GetTypesPackageName(string name) => "@types/" + MangleScopedPackageName(name);
    public static string GetPackageNameFromTypesPackageName(string name) => name.StartsWith("@types/", StringComparison.Ordinal) ? UnmangleScopedPackageName(name[7..]) : name;

    public static string TryGetJSExtensionForFile(string fileName, CompilerOptions options)
    {
        return TsPath.TryGetExtensionFromPath(fileName) switch
        {
            TsPath.ExtensionTs or TsPath.ExtensionDts => TsPath.ExtensionJs,
            TsPath.ExtensionTsx => options.Jsx == JsxEmit.Preserve ? TsPath.ExtensionJsx : TsPath.ExtensionJs,
            TsPath.ExtensionJs => TsPath.ExtensionJs,
            TsPath.ExtensionJsx => TsPath.ExtensionJsx,
            TsPath.ExtensionJson => TsPath.ExtensionJson,
            TsPath.ExtensionDmts or TsPath.ExtensionMts or TsPath.ExtensionMjs => TsPath.ExtensionMjs,
            TsPath.ExtensionDcts or TsPath.ExtensionCts or TsPath.ExtensionCjs => TsPath.ExtensionCjs,
            _ => ""
        };
    }

    public static string TryGetJSExtensionForFileName(RootedFilePath fileName, CompilerOptions options) => TryGetJSExtensionForFile(fileName.AsString(), options);
}

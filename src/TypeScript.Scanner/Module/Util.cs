// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Partial port of tsc/internal/module/util.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Compiler;
using TypeScript.Scanner;

namespace TypeScript.Modules;
public static class ModuleUtilities
{
    // util.go:132. These original TS forms are always allowed. Selected disk
    // admission permits only .ts implementation winners; other file families
    // cannot silently enter a successful fixed-profile checker call.
    internal static Message? GetResolutionDiagnostic(CompilerOptions options, ResolvedModule resolvedModule, SourceFile file)
    {
        if (resolvedModule.ResolvedUsingExtraExtensions)
            throw new ProjectException("extraExtensions", "Custom resolution extensions are unsupported.", resolvedModule.ResolvedFileName);
        if (resolvedModule.Extension is ".ts" or ".d.ts" or ".mts" or ".d.mts" or ".cts" or ".d.cts")
            return null;
        throw new ProjectException("resolvedFileForm", "Resolution diagnostics for this file form are unsupported: " + resolvedModule.Extension, resolvedModule.ResolvedFileName);
    }

    public static bool IsApplicableVersionedTypesKey(string key)
    {
        if (!key.StartsWith("types@", StringComparison.Ordinal))
            return false;
        var(range, ok) = TypeScript.Semver.Semver.TryParseVersionRange(key[6..]);
        return ok && range.Test(TypeScript.Semver.Semver.MustParse(Core.Version()));
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
        int next(int offset)
        {
            if (offset >= path.Length)
                return path.Length;
            int separator = path.IndexOf('/', offset);
            return separator < 0 ? isDirectory ? path.Length : offset : separator;
        }

        int end = next(start);
        if (path[start] == '@')
            end = next(end + 1);
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
}

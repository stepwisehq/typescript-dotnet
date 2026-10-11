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

        return resolvedModule.ExtensionValue switch
        {
            var ext when ext.EqualsString(TsPath.ExtensionTs) || ext.EqualsString(TsPath.ExtensionDts) || ext.EqualsString(TsPath.ExtensionMts) || ext.EqualsString(TsPath.ExtensionDmts) || ext.EqualsString(TsPath.ExtensionCts) || ext.EqualsString(TsPath.ExtensionDcts) => null,
            var ext when ext.EqualsString(TsPath.ExtensionTsx) => NeedJsx(),
            var ext when ext.EqualsString(TsPath.ExtensionJsx) => NeedJsx() ?? NeedAllowJs(),
            var ext when ext.EqualsString(TsPath.ExtensionJs) || ext.EqualsString(TsPath.ExtensionMjs) || ext.EqualsString(TsPath.ExtensionCjs) => NeedAllowJs(),
            var ext when ext.EqualsString(TsPath.ExtensionJson) => options.GetResolveJsonModule() ? null : Diagnostics.Module_0_was_resolved_to_1_but_resolveJsonModule_is_not_used,
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
        var (package, remaining) = ParsePackageName(ByteStringValue.FromString(name));
        return (package.Materialize()!, remaining.Materialize()!);
    }
    internal static (ByteStringValue PackageName, ByteStringValue Remaining) ParsePackageName(ByteStringValue name)
    {
        int index = name.IndexOf('/');
        if (name.StartsWith("@"u8))
        {
            int offset = index + 1;
            index = name.Slice(offset).IndexOf('/');
            if (index >= 0) index += offset;
        }
        return index < 0 ? (name, default) : (name.Slice(0, index), name.Slice(index + 1));
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

    public static RootedDirectoryPath NodeModulePackageRootForFile(RootedFilePath path) => parseNodeModulePackageRoot(path.AsValue(), false);
    public static RootedDirectoryPath NodeModulePackageRootForDirectory(RootedDirectoryPath path) => parseNodeModulePackageRoot(path.AsValue(), true);
    private static RootedDirectoryPath parseNodeModulePackageRoot(ByteStringValue path, bool isDirectory)
    {
        int index = path.LastIndexOf(ByteStringValue.FromString("/node_modules/"));
        if (index < 0)
            return default;
        int start = index + "/node_modules/".Length;
        int end = DefaultResolver.MoveToNextDirectorySeparatorIfAvailable(path, start, isDirectory);
        if (path[start] == '@')
            end = DefaultResolver.MoveToNextDirectorySeparatorIfAvailable(path, end, isDirectory);
        return TsPath.RootedDirectoryPathFromAbsolute(path.Slice(0, end));
    }

    public static string MangleScopedPackageName(string name) => MangleScopedPackageName(ByteStringValue.FromString(name)).Materialize()!;
    internal static ByteStringValue MangleScopedPackageName(ByteStringValue name)
    {
        if (!name.StartsWith("@"u8)) return name;
        int separator = name.IndexOf('/');
        if (separator < 0) return name;
        var builder = new GoBuilder();
        builder.WriteString(name.Slice(1, separator)); builder.WriteString("__"); builder.WriteString(name.Slice(separator + 1));
        return builder.Value();
    }

    public static string UnmangleScopedPackageName(string name) => name is null ? throw new NullReferenceException() : UnmangleScopedPackageNameValue(ByteStringValue.FromString(name)).Materialize()!;
    internal static ByteStringValue UnmangleScopedPackageNameValue(ByteStringValue name)
    {
        int separator = name.IndexOf("__"u8);
        if (separator < 0) return name;
        var builder = new GoBuilder(); builder.WriteByte((byte)'@'); builder.WriteString(name.Slice(0, separator)); builder.WriteByte((byte)'/'); builder.WriteString(name.Slice(separator + 2));
        return builder.Value();
    }
    public static string GetTypesPackageName(string name) => "@types/" + MangleScopedPackageName(name);
    public static string GetPackageNameFromTypesPackageName(string name) => name is null ? throw new NullReferenceException() : GetPackageNameFromTypesPackageNameValue(ByteStringValue.FromString(name)).Materialize()!;
    internal static ByteStringValue GetPackageNameFromTypesPackageNameValue(ByteStringValue name) => name.StartsWith("@types/"u8) ? UnmangleScopedPackageNameValue(name.Slice(7)) : name;

    public static string TryGetJSExtensionForFile(string fileName, CompilerOptions options) => TryGetJSExtensionForFile(ByteStringValue.FromString(fileName), options);
    internal static string TryGetJSExtensionForFile(ByteStringValue fileName, CompilerOptions options)
    {
        return TsPath.TryGetExtensionFromPath(fileName) switch
        {
            var ext when ext.EqualsString(TsPath.ExtensionTs) || ext.EqualsString(TsPath.ExtensionDts) => TsPath.ExtensionJs,
            var ext when ext.EqualsString(TsPath.ExtensionTsx) => options.Jsx == JsxEmit.Preserve ? TsPath.ExtensionJsx : TsPath.ExtensionJs,
            var ext when ext.EqualsString(TsPath.ExtensionJs) => TsPath.ExtensionJs,
            var ext when ext.EqualsString(TsPath.ExtensionJsx) => TsPath.ExtensionJsx,
            var ext when ext.EqualsString(TsPath.ExtensionJson) => TsPath.ExtensionJson,
            var ext when ext.EqualsString(TsPath.ExtensionDmts) || ext.EqualsString(TsPath.ExtensionMts) || ext.EqualsString(TsPath.ExtensionMjs) => TsPath.ExtensionMjs,
            var ext when ext.EqualsString(TsPath.ExtensionDcts) || ext.EqualsString(TsPath.ExtensionCts) || ext.EqualsString(TsPath.ExtensionCjs) => TsPath.ExtensionCjs,
            _ => ""
        };
    }

    public static string TryGetJSExtensionForFileName(RootedFilePath fileName, CompilerOptions options) => TryGetJSExtensionForFile(fileName.AsValue(), options);
}

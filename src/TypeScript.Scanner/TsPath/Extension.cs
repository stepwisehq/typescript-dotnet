// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/extension.go, upstream fed0bf24149f.
// Extension classification/removal/mutation and declaration filename recognition; no module resolution.
namespace TypeScript.Scanner;
public static partial class TsPath
{
    private static readonly string[] extensionsToRemove = [".d.ts", ".d.mts", ".d.cts", ".mjs", ".mts", ".cjs", ".cts", ".ts", ".js", ".tsx", ".jsx", ".json"];
    private static readonly string[] supportedDeclarationExtensions = [".d.ts", ".d.cts", ".d.mts"];
    public static bool IsDeclarationFileName(string path) => IsDeclarationFileName(ByteStringValue.FromString(path));
    public static bool IsDeclarationFileName(ByteStringValue path) => GetDeclarationFileExtension(path).Length != 0;
    public static string GetDeclarationFileExtension(string path) => GetDeclarationFileExtension(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue GetDeclarationFileExtension(ByteStringValue path) => getDeclarationFileExtensionFromNormalized(NormalizeSlashes(path));
    internal static ByteStringValue getDeclarationFileExtensionFromNormalized(ByteStringValue path)
    {
        var name = getBaseFileNameFromNormalized(path);
        foreach (var extension in supportedDeclarationExtensions)
        {
            if (name.EndsWith(ByteStringValue.FromString(extension)))
            {
                return ByteStringValue.FromString(extension);
            }
        }
        if (name.EndsWith(".ts"u8))
        {
            int index = name.IndexOf(".d."u8);
            if (index >= 0)
            {
                return name.Slice(index);
            }
        }
        return default;
    }
    public static string RemoveFileExtension(string path) => RemoveFileExtension(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue RemoveFileExtension(ByteStringValue path)
    {
        foreach (var extension in extensionsToRemove)
        {
            if (path.EndsWith(ByteStringValue.FromString(extension)))
            {
                return path.Slice(0, path.Length - extension.Length);
            }
        }
        return path;
    }

    public const string ExtensionTs = ".ts";
    public const string ExtensionTsx = ".tsx";
    public const string ExtensionDts = ".d.ts";
    public const string ExtensionJs = ".js";
    public const string ExtensionJsx = ".jsx";
    public const string ExtensionJson = ".json";
    public const string ExtensionTsBuildInfo = ".tsbuildinfo";
    public const string ExtensionMjs = ".mjs";
    public const string ExtensionMts = ".mts";
    public const string ExtensionDmts = ".d.mts";
    public const string ExtensionCjs = ".cjs";
    public const string ExtensionCts = ".cts";
    public const string ExtensionDcts = ".d.cts";
    public static readonly string[] SupportedDeclarationExtensions = [".d.ts", ".d.cts", ".d.mts"];
    public static readonly string[] SupportedTSImplementationExtensions = [".ts", ".tsx", ".mts", ".cts"];
    private static readonly string[] supportedTSExtensionsForExtractExtension = [".d.ts", ".d.cts", ".d.mts", ".ts", ".tsx", ".mts", ".cts"];
    public static readonly List<List<string>> AllSupportedExtensions = [[".ts", ".tsx", ".d.ts", ".js", ".jsx"], [".cts", ".d.cts", ".cjs"], [".mts", ".d.mts", ".mjs"]];
    public static readonly List<List<string>> SupportedTSExtensions = [[".ts", ".tsx", ".d.ts"], [".cts", ".d.cts"], [".mts", ".d.mts"]];
    public static readonly List<string> SupportedTSExtensionsFlat = [".ts", ".tsx", ".d.ts", ".cts", ".d.cts", ".mts", ".d.mts"];
    public static readonly List<List<string>> SupportedJSExtensions = [[".js", ".jsx"], [".mjs"], [".cjs"]];
    public static readonly List<string> SupportedJSExtensionsFlat = [".js", ".jsx", ".mjs", ".cjs"];
    public static readonly List<List<string>> AllSupportedExtensionsWithJson = [..AllSupportedExtensions, [".json"]];
    public static readonly List<List<string>> SupportedTSExtensionsWithJson = [..SupportedTSExtensions, [".json"]];
    public static readonly List<string> SupportedTSExtensionsWithJsonFlat = [..SupportedTSExtensionsFlat, ".json"];
    public static readonly List<string> ExtensionsNotSupportingExtensionlessResolution = [".mts", ".d.mts", ".mjs", ".cts", ".d.cts", ".cjs"];
    public static bool ExtensionIsTs(string extension) => ExtensionIsTs(ByteStringValue.FromString(extension));
    public static bool ExtensionIsTs(ByteStringValue extension) => extension.EqualsBytes(".ts"u8) || extension.EqualsBytes(".tsx"u8) || extension.EqualsBytes(".d.ts"u8) || extension.EqualsBytes(".mts"u8) || extension.EqualsBytes(".d.mts"u8) || extension.EqualsBytes(".cts"u8) || extension.EqualsBytes(".d.cts"u8) || extension.Length >= 7 && extension.StartsWith(".d."u8) && extension.EndsWith(".ts"u8);
    public static string RemoveAnyFileExtension(string path) => RemoveAnyFileExtension(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue RemoveAnyFileExtension(ByteStringValue path)
    {
        var without = RemoveFileExtension(path);
        if (!without.EqualsValue(path))
        {
            return without;
        }
        var extension = GetAnyExtensionFromPath(path, null, CaseSensitivity.CaseSensitive);
        return extension.Length != 0 ? RemoveExtension(path, extension) : path;
    }
    public static string TryGetExtensionFromPath(string path) => TryGetExtensionFromPath(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue TryGetExtensionFromPath(ByteStringValue path)
    {
        foreach (var extension in extensionsToRemove)
        {
            if (FileExtensionIs(path, extension))
            {
                return ByteStringValue.FromString(extension);
            }
        }
        return default;
    }
    public static string RemoveExtension(string path, string extension) => RemoveExtension(ByteStringValue.FromString(path), ByteStringValue.FromString(extension)).Materialize()!;
    public static ByteStringValue RemoveExtension(ByteStringValue path, string extension) => RemoveExtension(path, ByteStringValue.FromString(extension));
    public static ByteStringValue RemoveExtension(ByteStringValue path, ByteStringValue extension) => path.Slice(0, path.Length - extension.Length);
    public static bool FileExtensionIs(string path, string extension) => FileExtensionIs(ByteStringValue.FromString(path), extension);
    public static bool FileExtensionIs(ByteStringValue path, string extension)
    {
        if (path.IsNull)
        {
            throw new NullReferenceException();
        }
        return path.Length > extension.Length && path.EndsWith(ByteStringValue.FromString(extension));
    }
    public static bool FileExtensionIsOneOf(string path, IEnumerable<string> extensions) => FileExtensionIsOneOf(ByteStringValue.FromString(path), extensions);
    public static bool FileExtensionIsOneOf(ByteStringValue path, IEnumerable<string> extensions)
    {
        if (extensions is null)
        {
            throw new ArgumentNullException("source");
        }
        foreach (var extension in extensions)
        {
            if (FileExtensionIs(path, extension))
            {
                return true;
            }
        }
        return false;
    }
    public static bool ExtensionIsOneOf(string extension, IEnumerable<string> extensions) => extensions.Contains(extension);
    public static bool ExtensionIsOneOf(ByteStringValue extension, IEnumerable<string> extensions)
    {
        if (extensions is null)
        {
            throw new ArgumentNullException("source");
        }
        foreach (var candidate in extensions)
        {
            if (extension.EqualsString(candidate))
            {
                return true;
            }
        }
        return false;
    }
    public static string TryExtractTSExtension(string path) => TryExtractTSExtension(ByteStringValue.FromString(path));
    public static string TryExtractTSExtension(ByteStringValue path)
    {
        foreach (var extension in supportedTSExtensionsForExtractExtension)
        {
            if (FileExtensionIs(path, extension))
            {
                return extension;
            }
        }
        return "";
    }
    public static bool HasTSFileExtension(string path) => HasTSFileExtension(ByteStringValue.FromString(path));
    public static bool HasTSFileExtension(ByteStringValue path) => FileExtensionIsOneOf(path, SupportedTSExtensionsFlat);
    public static bool HasImplementationTSFileExtension(string path) => HasImplementationTSFileExtension(ByteStringValue.FromString(path));
    public static bool HasImplementationTSFileExtension(ByteStringValue path) => FileExtensionIsOneOf(path, SupportedTSImplementationExtensions) && !IsDeclarationFileName(path);
    public static bool HasJSFileExtension(string path) => HasJSFileExtension(ByteStringValue.FromString(path));
    public static bool HasJSFileExtension(ByteStringValue path) => FileExtensionIsOneOf(path, SupportedJSExtensionsFlat);
    public static bool HasJSONFileExtension(string path) => HasJSONFileExtension(ByteStringValue.FromString(path));
    public static bool HasJSONFileExtension(ByteStringValue path) => FileExtensionIs(path, ".json");
    public static string GetDeclarationEmitExtensionForPath(string path) => GetDeclarationEmitExtensionForPath(ByteStringValue.FromString(path)).Materialize()!;
    public static ByteStringValue GetDeclarationEmitExtensionForPath(ByteStringValue path)
    {
        if (FileExtensionIsOneOf(path, [".mjs", ".mts"]))
        {
            return ByteStringValue.FromString(".d.mts");
        }
        if (FileExtensionIsOneOf(path, [".cjs", ".cts"]))
        {
            return ByteStringValue.FromString(".d.cts");
        }
        if (FileExtensionIsOneOf(path, [".ts", ".tsx", ".js", ".jsx"]))
        {
            return ByteStringValue.FromString(".d.ts");
        }
        var extension = GetAnyExtensionFromPath(path, null, CaseSensitivity.CaseSensitive);
        return extension.Length == 0 ? ByteStringValue.FromString(".d.ts") : pathConcat(ByteStringValue.FromString(".d"), extension, ByteStringValue.FromString(".ts"));
    }
    public static string ChangeAnyExtension(string path, string extension, IEnumerable<string>? extensions, CaseSensitivity sensitivity) => ChangeAnyExtension(ByteStringValue.FromString(path), ByteStringValue.FromString(extension), extensions, sensitivity).Materialize()!;
    public static ByteStringValue ChangeAnyExtension(ByteStringValue path, string extension, IEnumerable<string>? extensions, CaseSensitivity sensitivity) => ChangeAnyExtension(path, ByteStringValue.FromString(extension), extensions, sensitivity);
    public static ByteStringValue ChangeAnyExtension(ByteStringValue path, ByteStringValue extension, IEnumerable<string>? extensions, CaseSensitivity sensitivity)
    {
        return changeAnyExtension(path, extension, GetAnyExtensionFromPath(path, extensions, sensitivity));
    }
    public static ByteStringValue ChangeAnyExtension(ByteStringValue path, ByteStringValue extension, Slice<ByteStringValue> extensions, CaseSensitivity sensitivity)
        => changeAnyExtension(path, extension, GetAnyExtensionFromPath(path, extensions, sensitivity));
    private static ByteStringValue changeAnyExtension(ByteStringValue path, ByteStringValue extension, ByteStringValue current)
    {
        if (current.Length == 0)
        {
            return path;
        }
        var result = path.Slice(0, path.Length - current.Length);
        if (extension.IsNull)
        {
            throw new NullReferenceException();
        }
        return extension.Length == 0 ? result : pathConcat(result, extension.StartsWith("."u8) ? default : ByteStringValue.FromString("."), extension);
    }
    public static string ChangeExtension(string path, string extension) => ChangeExtension(ByteStringValue.FromString(path), ByteStringValue.FromString(extension)).Materialize()!;
    public static ByteStringValue ChangeExtension(ByteStringValue path, string extension) => ChangeExtension(path, ByteStringValue.FromString(extension));
    public static ByteStringValue ChangeExtension(ByteStringValue path, ByteStringValue extension) => ChangeAnyExtension(path, extension, extensionsToRemove, CaseSensitivity.CaseSensitive);
    public static string ChangeFullExtension(string path, string extension) => ChangeFullExtension(ByteStringValue.FromString(path), ByteStringValue.FromString(extension)).Materialize()!;
    public static ByteStringValue ChangeFullExtension(ByteStringValue path, string extension) => ChangeFullExtension(path, ByteStringValue.FromString(extension));
    public static ByteStringValue ChangeFullExtension(ByteStringValue path, ByteStringValue extension)
    {
        var declaration = GetDeclarationFileExtension(path);
        if (declaration.Length == 0)
        {
            return ChangeExtension(path, extension);
        }
        if (extension.IsNull)
        {
            throw new NullReferenceException();
        }
        return pathConcat(path.Slice(0, path.Length - declaration.Length), extension.Length == 0 || extension.StartsWith("."u8) ? default : ByteStringValue.FromString("."), extension);
    }
    public static List<string> GetPossibleOriginalInputExtensionForExtension(string path) => projectPathComponents(GetPossibleOriginalInputExtensionForExtension(ByteStringValue.FromString(path)));
    public static Slice<ByteStringValue> GetPossibleOriginalInputExtensionForExtension(ByteStringValue path)
    {
        if (FileExtensionIsOneOf(path, [".d.mts", ".mjs", ".mts"]))
        {
            return Slice<ByteStringValue>.FromArray([ByteStringValue.FromString(".mts"), ByteStringValue.FromString(".mjs")]);
        }
        if (FileExtensionIsOneOf(path, [".d.cts", ".cjs", ".cts"]))
        {
            return Slice<ByteStringValue>.FromArray([ByteStringValue.FromString(".cts"), ByteStringValue.FromString(".cjs")]);
        }
        var extension = GetDeclarationFileExtension(path);
        if (extension.Length != 0 && !extension.EqualsBytes(".d.ts"u8))
        {
            return Slice<ByteStringValue>.FromArray([pathConcat(ByteStringValue.FromString("."), extension.Slice(3, extension.Length - 3))]);
        }
        return Slice<ByteStringValue>.FromArray([ByteStringValue.FromString(".tsx"), ByteStringValue.FromString(".ts"), ByteStringValue.FromString(".jsx"), ByteStringValue.FromString(".js")]);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/extension.go, upstream fed0bf24149f.
// Extension classification/removal/mutation and declaration filename recognition; no module resolution.
namespace TypeScript.Scanner;
public static partial class TsPath
{
    private static readonly string[] extensionsToRemove = [".d.ts", ".d.mts", ".d.cts", ".mjs", ".mts", ".cjs", ".cts", ".ts", ".js", ".tsx", ".jsx", ".json"];
    private static readonly string[] supportedDeclarationExtensions = [".d.ts", ".d.cts", ".d.mts"];
    public static bool IsDeclarationFileName(string fileName) => GetDeclarationFileExtension(fileName) != "";
    public static string GetDeclarationFileExtension(string fileName) => getDeclarationFileExtensionFromNormalized(NormalizeSlashes(fileName));
    internal static string getDeclarationFileExtensionFromNormalized(string fileName)
    {
        var baseName = getBaseFileNameFromNormalized(fileName);
        foreach (var extension in supportedDeclarationExtensions)
            if (baseName.EndsWith(extension, StringComparison.Ordinal))
                return extension;
        if (baseName.EndsWith(".ts", StringComparison.Ordinal))
        {
            var index = baseName.IndexOf(".d.", StringComparison.Ordinal);
            if (index >= 0)
                return baseName[index..];
        }

        return "";
    }

    public static string RemoveFileExtension(string path)
    {
        foreach (var extension in extensionsToRemove)
            if (path.EndsWith(extension, StringComparison.Ordinal))
                return path[..^extension.Length];
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
    public static bool ExtensionIsTs(string extension) => extension is ".ts" or ".tsx" or ".d.ts" or ".mts" or ".d.mts" or ".cts" or ".d.cts" || extension.Length >= 7 && extension.StartsWith(".d.", StringComparison.Ordinal) && extension.EndsWith(".ts", StringComparison.Ordinal);
    public static string RemoveAnyFileExtension(string path)
    {
        string without = RemoveFileExtension(path);
        if (without != path)
            return without;
        string extension = GetAnyExtensionFromPath(path, null, CaseSensitivity.CaseSensitive);
        return extension != "" ? RemoveExtension(path, extension) : path;
    }

    public static string TryGetExtensionFromPath(string path) => extensionsToRemove.FirstOrDefault(extension => FileExtensionIs(path, extension)) ?? "";
    public static string RemoveExtension(string path, string extension) => path[..(path.Length - extension.Length)];
    public static bool FileExtensionIs(string path, string extension) => path.Length > extension.Length && path.EndsWith(extension, StringComparison.Ordinal);
    // extension.go:79. Ordered synchronous traversal; preserve the public
    // IEnumerable null-source failure inherited from Enumerable.Any.
    public static bool FileExtensionIsOneOf(string path, IEnumerable<string> extensions)
    {
        if (extensions is null) throw new ArgumentNullException("source");
        foreach (var extension in extensions)
            if (FileExtensionIs(path, extension)) return true;
        return false;
    }
    public static bool ExtensionIsOneOf(string extension, IEnumerable<string> extensions) => extensions.Contains(extension);
    public static string TryExtractTSExtension(string path) => supportedTSExtensionsForExtractExtension.FirstOrDefault(extension => FileExtensionIs(path, extension)) ?? "";
    public static bool HasTSFileExtension(string path) => FileExtensionIsOneOf(path, SupportedTSExtensionsFlat);
    public static bool HasImplementationTSFileExtension(string path) => FileExtensionIsOneOf(path, SupportedTSImplementationExtensions) && !IsDeclarationFileName(path);
    public static bool HasJSFileExtension(string path) => FileExtensionIsOneOf(path, SupportedJSExtensionsFlat);
    public static bool HasJSONFileExtension(string path) => FileExtensionIs(path, ".json");
    public static string GetDeclarationEmitExtensionForPath(string path)
    {
        if (FileExtensionIsOneOf(path, [".mjs", ".mts"]))
            return ".d.mts";
        if (FileExtensionIsOneOf(path, [".cjs", ".cts"]))
            return ".d.cts";
        if (FileExtensionIsOneOf(path, [".ts", ".tsx", ".js", ".jsx"]))
            return ".d.ts";
        string extension = GetAnyExtensionFromPath(path, null, CaseSensitivity.CaseSensitive);
        return extension == "" ? ".d.ts" : ".d" + extension + ".ts";
    }

    public static string ChangeAnyExtension(string path, string extension, IEnumerable<string>? extensions, CaseSensitivity sensitivity)
    {
        string current = GetAnyExtensionFromPath(path, extensions, sensitivity);
        if (current == "")
            return path;
        string result = path[..(path.Length - current.Length)];
        return extension == "" ? result : result + (extension.StartsWith('.') ? "" : ".") + extension;
    }

    public static string ChangeExtension(string path, string extension) => ChangeAnyExtension(path, extension, extensionsToRemove, CaseSensitivity.CaseSensitive);
    public static string ChangeFullExtension(string path, string extension)
    {
        string declaration = GetDeclarationFileExtension(path);
        if (declaration == "")
            return ChangeExtension(path, extension);
        return path[..(path.Length - declaration.Length)] + (extension == "" || extension.StartsWith('.') ? "" : ".") + extension;
    }

    public static List<string> GetPossibleOriginalInputExtensionForExtension(string path)
    {
        if (FileExtensionIsOneOf(path, [".d.mts", ".mjs", ".mts"]))
            return[".mts", ".mjs"];
        if (FileExtensionIsOneOf(path, [".d.cts", ".cjs", ".cts"]))
            return[".cts", ".cjs"];
        string extension = GetDeclarationFileExtension(path);
        if (extension != "" && extension != ".d.ts")
            return["." + extension[3..^3]];
        return[".tsx", ".ts", ".jsx", ".js"];
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/extension.go, upstream fed0bf24149f.
// Known-extension removal and declaration filename recognition; no module resolution.
namespace TypeScript.Scanner;
internal static partial class TsPath
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
}

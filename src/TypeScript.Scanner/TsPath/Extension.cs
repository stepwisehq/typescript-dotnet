// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tspath/extension.go, upstream fed0bf24149f.
// Binder-required known-extension removal only; no module resolution.
namespace TypeScript.Scanner;
internal static partial class TsPath
{
    private static readonly string[] extensionsToRemove = [".d.ts", ".d.mts", ".d.cts", ".mjs", ".mts", ".cjs", ".cts", ".ts", ".js", ".tsx", ".jsx", ".json"];
    public static string RemoveFileExtension(string path)
    {
        foreach (var extension in extensionsToRemove)
            if (path.EndsWith(extension, StringComparison.Ordinal))
                return path[..^extension.Length];
        return path;
    }
}

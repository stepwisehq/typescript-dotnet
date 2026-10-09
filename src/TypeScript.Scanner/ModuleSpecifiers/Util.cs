// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/modulespecifiers/util.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Selected diagnostic file-ending helpers.
using TypeScript.Scanner;

namespace TypeScript.ModuleSpecifiers;
internal static partial class ModuleSpecifiers
{
    private static int comparePathsByRedirect(ModulePath first, ModulePath second, IModuleSpecifierGenerationHost host)
    {
        int redirect = second.IsRedirect.CompareTo(first.IsRedirect);
        if (redirect != 0)
            return redirect;
        int count = first.FileName.Count(ch => ch == '/').CompareTo(second.FileName.Count(ch => ch == '/'));
        return count != 0 ? count : host.CaseSensitivity().CompareFilePaths(
            TsPath.RootedFilePathFromPath(getPath(first.FileName, host)),
            TsPath.RootedFilePathFromPath(getPath(second.FileName, host)));
    }

    internal static bool PathIsBareSpecifier(string path) => TsPath.GetRootLength(path) == 0 && !TsPath.PathIsRelative(path);
    private static bool hasMandatoryExtension(string path) => new[]
    {
        ".mts",
        ".d.mts",
        ".mjs",
        ".cts",
        ".d.cts",
        ".cjs"
    }.Any(ext => path.EndsWith(ext, StringComparison.Ordinal));
    private static bool hasTsExtension(string path) => new[]
    {
        ".ts",
        ".tsx",
        ".mts",
        ".cts"
    }.Any(ext => path.EndsWith(ext, StringComparison.Ordinal));
    private static bool hasJsExtension(string path) => new[]
    {
        ".js",
        ".jsx",
        ".mjs",
        ".cjs"
    }.Any(ext => path.EndsWith(ext, StringComparison.Ordinal));
    private static string getJSExtensionForFile(string path, CompilerOptions options)
    {
        // util.go:156 -> module.TryGetJSExtensionForFile. Only .ts implementations
        // are selected; the other genuine file-extension branches remain rejected.
        if (path.EndsWith(".ts", StringComparison.Ordinal) && !TsPath.IsDeclarationFileName(path))
            return ".js";
        throw new NotSupportedException("Module specifier generation requires an unsupported file extension: " + path);
    }

    private static bool tryGetAnyFileFromPath(IModuleSpecifierGenerationHost host, string path)
    {
        // util.go:184, original GetSupportedExtensions(AllowJs=true,[.node,.json])
        // order; a file's existence prevents removal of /index even if its source
        // form itself is outside admitted Program roots.
        foreach (string extension in new[]
        {
            ".ts",
            ".tsx",
            ".d.ts",
            ".js",
            ".jsx",
            ".cts",
            ".d.cts",
            ".cjs",
            ".mts",
            ".d.mts",
            ".mjs",
            ".node",
            ".json"
        }

        )
            if (host.FileExists(path + extension))
                return true;
        return false;
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of selected tsc/internal/bundled/bundled.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Embedded resource packaging replaces go:embed; nonembedded build variant is external.
using System.Runtime.CompilerServices;
using TypeScript.Scanner;
using TypeScript.Vfs;

namespace TypeScript.Bundled;
public static partial class Bundled
{
    public const bool Embedded = true;
    public static IFS WrapFS(IFS fs) => new WrappedFS(fs);
    public static RootedDirectoryPath LibPath() => TsPath.RootedDirectoryPathFromNormalized(LibPathText());
    private static readonly Lazy<RootedDirectoryPath> bundledSourceDir = new(() =>
    {
        string fileName = SourceFileName();
        if (!Path.IsPathFullyQualified(fileName))
            throw new InvalidOperationException("bundled: source directory cannot be found when built with trimmed source paths");
        return RootedDirectoryPath.FromUnicode(Path.GetDirectoryName(fileName)!);
    });
    private static string SourceFileName([CallerFilePath] string fileName = "") => fileName;
    private static readonly Lazy<string> testingLibPath = new(() => bundledSourceDir.Value.ResolveDirectory("Libs").ToUnicode());
    // Source-tree-only testing API; it is not a deployed filesystem location.
    public static string TestingLibPath() => testingLibPath.Value;
}

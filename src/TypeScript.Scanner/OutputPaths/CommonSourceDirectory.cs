// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/outputpaths/commonsourcedirectory.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.OutputPaths;
public static partial class OutputPaths
{
    internal static RootedDirectoryPath computeCommonSourceDirectoryOfFilenames(List<RootedFilePath>? fileNames, RootedDirectoryPath currentDirectory, CaseSensitivity caseSensitivity) => fileNames == null || fileNames.Count == 0 ? currentDirectory : caseSensitivity.CommonDirectoryOfFiles(fileNames);
    public static RootedDirectoryPath GetComputedCommonSourceDirectory(List<RootedFilePath>? emittedFiles, RootedDirectoryPath currentDirectory, CaseSensitivity caseSensitivity) => computeCommonSourceDirectoryOfFilenames(emittedFiles, currentDirectory, caseSensitivity);
    public static RootedDirectoryPath GetCommonSourceDirectory(CompilerOptions options, Func<List<RootedFilePath>?> files, RootedDirectoryPath currentDirectory, CaseSensitivity caseSensitivity, Func<List<RootedFilePath>?, RootedDirectoryPath, bool>? checkSourceFilesBelongToPath)
    {
        RootedDirectoryPath commonSourceDirectory;
        if (options.RootDir.AsValue().Length != 0)
        {
            commonSourceDirectory = options.RootDir;
            checkSourceFilesBelongToPath?.Invoke(files(), commonSourceDirectory);
        }
        else if (options.ConfigFilePath.AsValue().Length != 0)
        {
            commonSourceDirectory = options.ConfigFilePath.Directory();
            checkSourceFilesBelongToPath?.Invoke(files(), commonSourceDirectory);
        }
        else
            commonSourceDirectory = computeCommonSourceDirectoryOfFilenames(files(), currentDirectory, caseSensitivity);
        return commonSourceDirectory;
    }
}

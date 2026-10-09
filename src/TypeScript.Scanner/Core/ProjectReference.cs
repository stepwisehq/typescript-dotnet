// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/core/projectreference.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
public sealed class ProjectReference
{
    public RootedPath Path;
    public string OriginalPath = "";
    public bool Circular;
}

public static class ProjectReferences
{
    public static RootedFilePath ResolveProjectReferencePath(ProjectReference reference) => ResolveConfigFileNameOfProjectReference(reference.Path);
    public static RootedFilePath ResolveConfigFileNameOfProjectReference(RootedPath path)
    {
        var file = TsPath.RootedFilePathFromPath(path);
        return file.ExtensionIs(".json") ? file : TsPath.RootedDirectoryPathFromPath(path).ResolveFile("tsconfig.json");
    }
}

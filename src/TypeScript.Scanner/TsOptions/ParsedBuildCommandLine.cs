// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/parsedbuildcommandline.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.TsOptions;
public sealed class ParsedBuildCommandLine
{
    public BuildOptions BuildOptions = new();
    public CompilerOptions CompilerOptions = new();
    public List<string>? Projects;
    public List<Diagnostic>? Errors;
    public object? Raw;
    internal RootedDirectoryPath currentDirectory;
    private List<RootedFilePath>? resolvedProjectPaths;
    private readonly ConfigOnce resolvedProjectPathsOnce = new(), localeOnce = new();
    private TypeScript.Locale.Locale locale;
    public List<RootedFilePath>? ResolvedProjectPaths()
    {
        resolvedProjectPathsOnce.Do(() => resolvedProjectPaths = Core.Map(Projects, project => ProjectReferences.ResolveConfigFileNameOfProjectReference(currentDirectory.ResolveFile(project).AsPath())));
        return resolvedProjectPaths;
    }

    public TypeScript.Locale.Locale Locale()
    {
        localeOnce.Do(() => (locale, _) = TypeScript.Locale.Locales.Parse(CompilerOptions.Locale));
        return locale;
    }
}

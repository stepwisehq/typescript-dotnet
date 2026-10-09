// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/tsoptions/parsedoptions.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.ContentMapper;
using TypeScript.Scanner;

namespace TypeScript.TsOptions;
public sealed class ParsedOptions
{
    public CompilerOptions? CompilerOptions;
    public TypeAcquisition? TypeAcquisition;
    public List<RootedFilePath>? FileNames;
    public List<ProjectReference>? ProjectReferences;
    public List<Mapper>? ContentMappers;
    public bool Equals(ParsedOptions? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other == null)
            return false;
        if (!(ReferenceEquals(CompilerOptions, other.CompilerOptions) || CompilerOptions?.Equals(other.CompilerOptions) == true) || !(ReferenceEquals(TypeAcquisition, other.TypeAcquisition) || TypeAcquisition?.Equals(other.TypeAcquisition) == true))
            return false;
        return (FileNames == null) == (other.FileNames == null) && OptionModelEquality.SlicesEqual(FileNames, other.FileNames) && (ProjectReferences == null) == (other.ProjectReferences == null) && referencesEqual(ProjectReferences, other.ProjectReferences) && (ContentMappers == null) == (other.ContentMappers == null) && mappersEqual(ContentMappers, other.ContentMappers);
    }

    private static bool referencesEqual(List<ProjectReference>? a, List<ProjectReference>? b)
    {
        if ((a?.Count ?? 0) != (b?.Count ?? 0))
            return false;
        if (a == null || b == null)
            return true;
        for (int i = 0; i < a.Count; i++)
            if (!ReferenceEquals(a[i], b[i]) && (a[i] == null || b[i] == null || a[i].Path != b[i].Path || a[i].OriginalPath != b[i].OriginalPath || a[i].Circular != b[i].Circular))
                return false;
        return true;
    }

    private static bool mappersEqual(List<Mapper>? a, List<Mapper>? b)
    {
        if ((a?.Count ?? 0) != (b?.Count ?? 0))
            return false;
        if (a == null || b == null)
            return true;
        for (int i = 0; i < a.Count; i++)
            if (!ReferenceEquals(a[i], b[i]) && a[i]?.Equals(b[i]) != true)
                return false;
        return true;
    }
}

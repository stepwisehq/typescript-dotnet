// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Exact graph request models from internal/project/snapshot.go:318–425. Snapshot algorithms follow separately.
using TypeScript.Scanner;
using Diagnostic = TypeScript.Scanner.Diagnostic;
using TypeScript.ProjectSystem.Logging;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;
using TypeScript.Vfs;

namespace TypeScript.ProjectSystem;
internal class APICreateProgramRequest
{
    internal List<RootedFilePath>? RootFileNames;
    internal CompilerOptions CompilerOptions = null!;
    internal List<ProjectReference>? ProjectReferences;
    internal Slice<Diagnostic> ConfigFileParsingDiagnostics;
    internal IModuleResolverFactory? ModuleResolverFactory;
    internal ulong ModuleResolverID;
}

internal sealed class APIReconfigureProgramRequest : APICreateProgramRequest
{
    internal SyntheticProjectID ProgramID;
}

internal sealed class APISnapshotRequest
{
    internal UserPreferences? UserPreferences;
    internal DocumentUri PrepareAutoImports;
    internal HashSet<RootedFilePath>? OpenProjects;
    internal HashSet<PathKey>? CloseProjects;
    internal Dictionary<PathKey, RootedFilePath>? OpenFiles;
    internal HashSet<PathKey>? CloseFiles;
    internal Slice<APICreateProgramRequest> CreatePrograms;
    internal Slice<APIReconfigureProgramRequest> ReconfigurePrograms;
    internal HashSet<SyntheticProjectID>? RemovePrograms;
    internal HashSet<ID>? EnsurePrograms;
    internal bool EnsureAllPrograms;
    internal Dictionary<PathKey, RootedFilePath>? EnsureFiles;
    internal IFS? FileSystem;
    internal bool ReplaceFileSystem;
}

internal sealed class ProjectTreeRequest
{
    internal HashSet<PathKey>? referencedProjects;
    // snapshot.go:360/364/368. Nil receiver of Go Set.Has returns false.
    internal bool IsAllProjects() => referencedProjects == null;
    internal bool IsProjectReferenced(PathKey id) => referencedProjects?.Contains(id) == true;
    internal Slice<PathKey> Projects()
    {
        if ((referencedProjects?.Count ?? 0) == 0)
            return default;
        var paths = Slice<PathKey>.Make(referencedProjects!.Count);
        int i = 0;
        foreach (var path in referencedProjects)
            paths[i++] = path;
        return paths;
    }
}

internal sealed class ATAStateChange
{
    internal Ata.TypingsInfo? TypingsInfo;
    internal Slice<RootedFilePath> TypingsFiles;
    internal Slice<RootedPath> TypingsFilesToWatch;
    internal LogTree? Logs;
}

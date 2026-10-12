// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/project/projectcollection.go counterpart at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Collections;

namespace TypeScript.ProjectSystem;
// internal/project/projectcollection.go:44. Value snapshot; nil map headers stay nil across Clone.
internal readonly record struct ApiOpenedFile(RootedFilePath FileName, long RefCount);
internal struct ApiState
{
    internal Dictionary<PathKey, long>? OpenProjects;
    internal Dictionary<PathKey, ApiOpenedFile>? OpenFiles;
    // projectcollection.go:56 clone. Required detached map owners retain key comparers.
    internal readonly ApiState Clone() => new()
    {
        OpenProjects = OpenProjects is null ? null : new(OpenProjects, OpenProjects.Comparer),
        OpenFiles = OpenFiles is null ? null : new(OpenFiles, OpenFiles.Comparer)
    };
    // projectcollection.go:63 equals. Go maps.Equal considers nil and empty maps equal.
    internal readonly bool EqualsState(ApiState other) => EqualMaps(OpenProjects, other.OpenProjects) && EqualMaps(OpenFiles, other.OpenFiles);
    private static bool EqualMaps<T>(Dictionary<PathKey, T>? left, Dictionary<PathKey, T>? right)
    {
        if ((left?.Count ?? 0) != (right?.Count ?? 0))
            return false;
        if (left is null)
            return true;
        foreach (var pair in left)
            if (right is null || !right.TryGetValue(pair.Key, out var value) || !EqualityComparer<T>.Default.Equals(pair.Value, value))
                return false;
        return true;
    }
}

internal sealed class ProjectCollection
{
    internal CaseSensitivity caseSensitivity;
    internal ConfigFileRegistry configFileRegistry = null!;
    internal Dictionary<PathKey, ID>? fileDefaultProjects;
    internal Dictionary<ConfiguredProjectID, Project>? configuredProjects;
    internal Dictionary<SyntheticProjectID, Project>? syntheticProjects;
    internal HashSet<PathKey>? openFiles;
    internal Project? inferredProject;
    internal ApiState apiState;
    private readonly ProjectOnce openConfiguredProjectsOnce = new();
    private HashSet<ConfiguredProjectID>? openConfiguredProjects;
    // projectcollection.go:73/75/79. Missing Go map reads return nil.
    internal ConfigFileRegistry ConfigFileRegistry() => configFileRegistry;
    internal Project? ConfiguredProject(PathKey path) => configuredProjects?.GetValueOrDefault(TypeScript.ProjectSystem.Projects.ConfiguredProjectIDFromPathKey(path));
    internal Project? GetProject(ID id)
    {
        if (id.Inferred().Ok)
            return inferredProject;
        var(synthetic, syntheticOk) = id.Synthetic();
        if (syntheticOk)
            return syntheticProjects?.GetValueOrDefault(synthetic);
        var(configured, configuredOk) = id.Configured();
        return configuredOk ? configuredProjects?.GetValueOrDefault(configured) : null;
    }

    // projectcollection.go:93/99. Explicit initial capacity and raw-byte ID ordering.
    internal Slice<Project> ConfiguredProjects()
    {
        var projects = Slice<Project>.Make(0, configuredProjects?.Count ?? 0);
        FillConfiguredProjects(ref projects);
        return projects;
    }

    private void FillConfiguredProjects(ref Slice<Project> projects)
    {
        if (configuredProjects != null)
            foreach (var project in configuredProjects.Values)
                projects = projects.Append(project);
        GoSlices.SortFunc(projects, static (a, b) => ByteStringKey.FromValue(a.ID().Value).CompareTo(ByteStringKey.FromValue(b.ID().Value)));
    }

    // projectcollection.go:110.
    internal Slice<Project> SyntheticProjects()
    {
        var projects = Slice<Project>.Make(0, syntheticProjects?.Count ?? 0);
        if (syntheticProjects != null)
            foreach (var project in syntheticProjects.Values)
                projects = projects.Append(project);
        GoSlices.SortFunc(projects, static (a, b) => ByteStringKey.FromValue(a.ID().Value).CompareTo(ByteStringKey.FromValue(b.ID().Value)));
        return projects;
    }

    // projectcollection.go:122.
    internal OrderedMap<ID, Project> ProjectsByID()
    {
        var projects = new OrderedMap<ID, Project>((configuredProjects?.Count ?? 0) + (syntheticProjects?.Count ?? 0) + (inferredProject == null ? 0 : 1));
        foreach (var project in ConfiguredProjects())
            projects.Set(project.ID(), project);
        foreach (var project in SyntheticProjects())
            projects.Set(project.ID(), project);
        if (inferredProject != null)
            projects.Set(inferredProject.ID(), inferredProject);
        return projects;
    }

    // projectcollection.go:141/153/161.
    internal Slice<Project> Projects()
    {
        var projects = Slice<Project>.Make(0, (configuredProjects?.Count ?? 0) + (syntheticProjects?.Count ?? 0) + (inferredProject == null ? 0 : 1));
        FillConfiguredProjects(ref projects);
        projects = projects.Append(SyntheticProjects());
        if (inferredProject != null)
            projects = projects.Append(inferredProject);
        return projects;
    }

    internal Slice<Project> LanguageServiceProjects()
    {
        var projects = Slice<Project>.Make(0, (configuredProjects?.Count ?? 0) + (inferredProject == null ? 0 : 1));
        FillConfiguredProjects(ref projects);
        if (inferredProject != null)
            projects = projects.Append(inferredProject);
        return projects;
    }

    internal Project? InferredProject() => inferredProject;
    // projectcollection.go:167. Synthetic programs are deliberately excluded.
    internal Slice<TypeScript.LanguageServices.IProject> GetLanguageServiceProjectsContainingFile(PathKey path)
    {
        Slice<TypeScript.LanguageServices.IProject> projects = default;
        foreach (var project in ConfiguredProjects())
            if (project.ContainsFile(path))
                projects = projects.Append(project);
        if (inferredProject?.ContainsFile(path) == true)
            projects = projects.Append(inferredProject);
        return projects;
    }

    // projectcollection.go:182.
    internal HashSet<ConfiguredProjectID> GetOpenConfiguredProjects()
    {
        openConfiguredProjectsOnce.Do(() =>
        {
            var projects = new HashSet<ConfiguredProjectID>(configuredProjects?.Count ?? 0);
            if (openFiles != null)
                foreach (var path in openFiles)
                {
                    if (fileDefaultProjects?.TryGetValue(path, out var id) == true)
                    {
                        var(configured, ok) = id.Configured();
                        if (ok && configuredProjects?.GetValueOrDefault(configured) != null)
                        {
                            projects.Add(configured);
                            continue;
                        }
                    }

                    if (configuredProjects != null)
                        foreach (var project in configuredProjects.Values)
                            if (project.ContainsFile(path))
                                projects.Add(project.ID().Configured().ID);
                }

            openConfiguredProjects = projects;
        });
        return openConfiguredProjects!;
    }

    // projectcollection.go:208.
    internal static HashSet<PathKey> OpenFilePaths(Dictionary<PathKey, Overlay>? overlays)
    {
        var files = new HashSet<PathKey>(overlays?.Count ?? 0);
        if (overlays != null)
            foreach (var path in overlays.Keys)
                files.Add(path);
        return files;
    }

    // projectcollection.go:218.
    internal Project? GetDefaultProject(PathKey path)
    {
        if (fileDefaultProjects?.TryGetValue(path, out var id) == true)
            return id.Inferred().Ok ? inferredProject : configuredProjects?.GetValueOrDefault(id.Configured().ID);
        Slice<Project> containing = default;
        Project? firstConfigured = null, firstDirect = null;
        bool multipleDirect = false;
        foreach (var project in ConfiguredProjects())
            if (project.ContainsFile(path))
            {
                containing = containing.Append(project);
                if (!multipleDirect && !project.IsSourceFromProjectReference(path))
                {
                    if (firstDirect == null)
                        firstDirect = project;
                    else
                        multipleDirect = true;
                }

                firstConfigured ??= project;
            }

        if (containing.Count == 1)
            return containing[0];
        if (containing.Count == 0)
            return inferredProject?.ContainsFile(path) == true ? inferredProject : null;
        if (!multipleDirect)
            return firstDirect ?? firstConfigured;
        return FindDefaultConfiguredProject(path) ?? firstConfigured;
    }

    // projectcollection.go:270/277. The shared visited owner spans ancestor searches.
    private Project? FindDefaultConfiguredProject(PathKey path)
    {
        var name = configFileRegistry.GetConfigFileName(path);
        return name.AsValue().Length != 0 ? FindDefaultConfiguredProjectWorker(path, name, null, null) : null;
    }

    private Project? FindDefaultConfiguredProjectWorker(PathKey path, RootedFilePath name, SyncSet<Project>? visited, Project? fallback)
    {
        var configPath = caseSensitivity.PathKey(name.AsPath());
        var project = configuredProjects?.GetValueOrDefault(TypeScript.ProjectSystem.Projects.ConfiguredProjectIDFromPathKey(configPath));
        if (project == null)
            return null;
        visited ??= new();
        var search = Core.BreadthFirstSearchParallelEx(project, p =>
        {
            Slice<Project> children = default;
            if (p.CommandLine == null)
                return children;
            foreach (var childName in p.CommandLine.ResolvedProjectReferencePaths() ?? [])
            {
                var child = configuredProjects?.GetValueOrDefault(TypeScript.ProjectSystem.Projects.ConfiguredProjectIDFromPathKey(caseSensitivity.PathKey(childName.AsPath())));
                if (child != null)
                    children = children.Append(child);
            }

            return children;
        }, p => p.ContainsFile(path) ? (true, !p.IsSourceFromProjectReference(path)) : (false, false), new BreadthFirstSearchOptions<Project, Project>(visited), static p => p);
        if (search.Stopped)
            return search.Path[0];
        if (search.Path.Count > 0 && fallback == null)
            fallback = search.Path[0];
        if (configFileRegistry.GetConfig(path)?.CompilerOptions().DisableSolutionSearching == Tristate.True)
            return fallback;
        var ancestor = configFileRegistry.GetAncestorConfigFileName(path, name);
        return ancestor.AsValue().Length != 0 ? FindDefaultConfiguredProjectWorker(path, ancestor, visited, fallback) : fallback;
    }

    // projectcollection.go:329. Once and memoized open-project set are deliberately not copied.
    internal ProjectCollection Clone() => new()
    {
        caseSensitivity = caseSensitivity,
        configFileRegistry = configFileRegistry,
        configuredProjects = configuredProjects,
        syntheticProjects = syntheticProjects,
        openFiles = openFiles,
        inferredProject = inferredProject,
        fileDefaultProjects = fileDefaultProjects,
        apiState = apiState
    };
    // projectcollection.go:346. Sorted project order breaks ties before solution search.
    internal static (PathKey Result, bool MultipleCandidates) FindDefaultConfiguredProjectFromProgramInclusion(PathKey path, Slice<PathKey> paths, Func<PathKey, Project> getProject)
    {
        Slice<PathKey> containing = default;
        PathKey firstConfigured = default, firstDirect = default;
        bool multipleDirect = false;
        foreach (var projectPath in paths)
        {
            var project = getProject(projectPath);
            if (project.ContainsFile(path))
            {
                containing = containing.Append(projectPath);
                if (!multipleDirect && !project.IsSourceFromProjectReference(path))
                {
                    if (firstDirect.AsValue().Length == 0)
                        firstDirect = projectPath;
                    else
                        multipleDirect = true;
                }

                if (firstConfigured.AsValue().Length == 0)
                    firstConfigured = projectPath;
            }
        }

        if (containing.Count == 1)
            return (containing[0], false);
        if (!multipleDirect)
            return (firstDirect.AsValue().Length != 0 ? firstDirect : firstConfigured, false);
        return (firstConfigured, true);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Selected port of tsc/internal/compiler/filesparser.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Single-threaded WorkGroup; fresh
// task storage replaces pools. Program reference/content-mapper/lib tasks are external.
using TypeScript.Modules;
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal sealed class ParseTask
{
    internal string normalizedFilePath = "", path = "";
    internal SourceFile? file;
    internal List<ParseTask>? subTasks;
    internal bool loaded, startedSubTasks, failedLookup, increaseDepth, elideOnDepth;
    internal ParseTask? loadedTask;
    internal PackageId packageId;
    internal SourceFileMetaData metadata;
    internal Dictionary<ModeAwareCacheKey, ResolvedModule>? resolutionsInFile;
    internal List<ProjectResolutionEdge>? resolutionEdges;
    internal List<ResolvedProjectEdge>? graphEdges;
    internal List<ResolvedProjectReferenceEdge>? pathReferences;
    internal List<ResolvedProjectTypeDirective>? typeDirectives;
    internal List<ProcessingDiagnostic>? processingDiagnostics;
    internal Node? importHelpersImportSpecifier, jsxRuntimeImportSpecifier;
    internal ResolvedProjectEdge? incomingEdge;
    internal FileIncludeReason includeReason = new();
    internal string FileName() => normalizedFilePath;
    internal string PathKey() => path;

    // filesparser.go:59. Loading is published before parsing or child scheduling.
    internal void Load(FileLoader loader)
    {
        loaded = true;
        if (failedLookup) return;
        if (loader.graph && loader.TypedFile(normalizedFilePath).HasExtension() && loader.options.AllowNonTsExtensions != Tristate.True && !loader.IsSupportedExtension(path))
        {
            Diagnostic diagnostic = loader.UnsupportedExtensionDiagnostic(normalizedFilePath, loader.TypedFile(normalizedFilePath));
            (processingDiagnostics ??= []).Add(new() { diagnosticReason = includeReason, message = diagnostic.message, args = diagnostic.MessageArgs()?.ToArray() ?? [] });
            if (incomingEdge is not null) incomingEdge.Scheduling = DependencyScheduling.UnsupportedExtension;
            return;
        }
        if (loader.graph) metadata = loader.LoadSourceFileMetaData(normalizedFilePath);
        file ??= loader.ParseSourceFile(this);
        if (file is null)
        {
            if (incomingEdge is not null) incomingEdge.Scheduling = DependencyScheduling.MissingFile;
            return;
        }
        loader.ValidateParsedSource(file);
        subTasks = [];
        // Graph collection follows original parser output even when diagnostics
        // exist. The named legacy checking adapter prevents malformed entry.
        if (!loader.graph && file.Diagnostics() is { Count: > 0 }) return;
        if (loader.graph)
        {
            loader.ResolveTripleslashPathReferences(this);
            loader.ResolveTypeReferenceDirectives(this);
        }
        loader.ResolveImportsAndModuleAugmentations(this);
    }

    // filesparser.go:217. Package identity and depth flags are actual resolver output.
    internal void AddSubTask(string fileName, string key, FileIncludeReason reason, bool increaseDepth = false,
        bool elideOnDepth = false, PackageId packageId = default, ResolvedProjectEdge? incomingEdge = null) => (subTasks ??= []).Add(new ParseTask
        {
            normalizedFilePath = fileName, path = key, includeReason = reason, increaseDepth = increaseDepth,
            elideOnDepth = elideOnDepth, packageId = packageId, incomingEdge = incomingEdge
        });
}

internal sealed class ParseTaskData
{
    internal readonly Dictionary<string, ParseTask> tasks = new(StringComparer.Ordinal);
    internal bool startedSubTasks;
    internal long lowestDepth = long.MaxValue;
    internal PackageId packageId;
    internal ParseTaskData(ParseTask task) => tasks.Add(task.normalizedFilePath, task);
}

internal sealed class ProcessedFiles
{
    internal readonly List<SourceFile> files = [];
    internal readonly Dictionary<string, SourceFile> filesByPath = new(StringComparer.Ordinal);
    internal readonly HashSet<string> missingFiles = new(StringComparer.Ordinal);
    internal readonly List<ProjectResolutionEdge> resolutionEdges = [];
    internal readonly List<ResolvedProjectEdge> graphEdges = [];
    internal readonly List<ResolvedProjectReferenceEdge> pathReferences = [];
    internal readonly List<ResolvedProjectTypeDirective> typeDirectives = [];
    internal readonly Dictionary<string, SourceFileMetaData> metadata = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, SourceFile> packageRedirects = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, List<string>> redirectTargetsMap = new(StringComparer.Ordinal);
    internal readonly List<Diagnostic> processingDiagnostics = [];
}

internal sealed class FilesParser
{
    private readonly Dictionary<string, ParseTaskData> taskDataByPath = new(StringComparer.Ordinal);
    private readonly Stack<Action> work = new();
    internal long maxDepth;

    // filesparser.go:268 + core/workgroup.go:57/67/78. LIFO callbacks preserve
    // single-threaded scheduling; ordered sources are collected independently.
    internal void Parse(FileLoader loader, List<ParseTask> tasks)
    {
        Start(loader, tasks, 0);
        while (work.TryPop(out Action? callback)) callback();
    }
    internal void Start(FileLoader loader, List<ParseTask> tasks, long depth)
    {
        for (int i = 0; i < tasks.Count; i++)
        {
            int slot = i;
            ParseTask task = tasks[i];
            if (task.path == "") throw new InvalidOperationException("parse task must have a path key");
            bool loaded = taskDataByPath.TryGetValue(task.path, out ParseTaskData? data);
            if (!loaded) taskDataByPath.Add(task.path, data = new(task));
            ParseTaskData actual = data!;
            work.Push(() =>
            {
                bool startSubtasks = false;
                if (loaded)
                {
                    if (actual.tasks.TryGetValue(task.normalizedFilePath, out var existing)) tasks[slot].loadedTask = existing;
                    else
                    {
                        actual.tasks.Add(task.normalizedFilePath, task);
                        startSubtasks = actual.startedSubTasks;
                    }
                }
                if (actual.packageId.Name == "" && task.packageId.Name != "") actual.packageId = task.packageId;
                long currentDepth = task.increaseDepth ? depth + 1 : depth;
                bool lowered = currentDepth < actual.lowestDepth;
                if (lowered)
                {
                    actual.lowestDepth = currentDepth;
                    startSubtasks = true;
                    actual.startedSubTasks = true;
                }
                if (task.elideOnDepth && currentDepth > maxDepth)
                {
                    if (task.incomingEdge is not null) task.incomingEdge.Scheduling = DependencyScheduling.ElidedJavaScriptDepth;
                    return;
                }
                foreach (ParseTask taskByFileName in actual.tasks.Values)
                {
                    if (!taskByFileName.loaded) taskByFileName.Load(loader);
                    if (startSubtasks && (lowered || !taskByFileName.startedSubTasks))
                    {
                        taskByFileName.startedSubTasks = true;
                        Start(loader, taskByFileName.subTasks ?? [], actual.lowestDepth);
                    }
                }
            });
        }
    }

    // filesparser.go:337/394/488. Package deduplication precedes child traversal,
    // then source postorder and actual metadata/resolution publication follow.
    internal ProcessedFiles GetProcessedFiles(FileLoader loader)
    {
        var result = new ProcessedFiles();
        var seen = new Dictionary<ParseTaskData, string>(ReferenceEqualityComparer.Instance);
        var tasksSeenByNameIgnoreCase = new Dictionary<string, ParseTask>(StringComparer.Ordinal);
        Dictionary<PackageId, SourceFile>? packageFiles = loader.graph && loader.options.DeduplicatePackages != Tristate.False ? new() : null;
        void CollectFiles(List<ParseTask> tasks)
        {
            foreach (ParseTask input in tasks)
            {
                ParseTask task = input.loadedTask ?? input;
                ParseTaskData data = taskDataByPath[task.path];
                loader.includeProcessor.AddIncludeReason(task, input.includeReason);
                if (!task.loaded) continue;
                if (seen.TryGetValue(data, out string? checkedName))
                {
                    if (loader.options.ForceConsistentCasingInFileNames != Tristate.False &&
                        loader.TypedFile(checkedName).WithoutRoot() != loader.TypedFile(task.normalizedFilePath).WithoutRoot())
                        loader.includeProcessor.AddProcessingDiagnosticsForFileCasing(task.path, checkedName, task.normalizedFilePath, input.includeReason);
                    continue;
                }
                seen.Add(data, task.normalizedFilePath);
                if (loader.host.FS().CaseSensitivity() == CaseSensitivity.CaseSensitive)
                {
                    string lower = loader.graph ? PathKey.FromGoString(task.path).CaseInsensitiveKey().AsString() : TsPath.ToFileNameLowerCase(task.path);
                    if (tasksSeenByNameIgnoreCase.TryGetValue(lower, out ParseTask? existing))
                        loader.includeProcessor.AddProcessingDiagnosticsForFileCasing(existing.path, existing.normalizedFilePath, task.normalizedFilePath, input.includeReason);
                    else tasksSeenByNameIgnoreCase.Add(lower, task);
                }
                if (packageFiles is not null && data.packageId.Name != "")
                {
                    if (packageFiles.TryGetValue(data.packageId, out SourceFile? packageFile))
                    {
                        result.filesByPath[task.path] = packageFile;
                        result.packageRedirects[task.path] = packageFile;
                        // filesparser.go:470. Keep the actual acquired spelling,
                        // not an inverse guess from a canonical dictionary key.
                        if (!result.redirectTargetsMap.TryGetValue(packageFile.PathKey(), out var redirectTargets))
                            result.redirectTargetsMap.Add(packageFile.PathKey(), redirectTargets = []);
                        redirectTargets.Add(task.normalizedFilePath);
                        continue;
                    }
                    if (task.file is not null) packageFiles.Add(data.packageId, task.file);
                }
                if (task.subTasks is { Count: > 0 }) CollectFiles(task.subTasks);
                if (task.processingDiagnostics is not null) loader.includeProcessor.processingDiagnostics.AddRange(task.processingDiagnostics);
                if (task.file is null)
                {
                    result.missingFiles.Add(task.path);
                    continue;
                }
                result.files.Add(task.file);
                result.filesByPath.Add(task.path, task.file);
                result.metadata.Add(task.path, task.metadata);
                if (task.resolutionEdges is not null) result.resolutionEdges.AddRange(task.resolutionEdges);
                if (task.graphEdges is not null) result.graphEdges.AddRange(task.graphEdges);
                if (task.pathReferences is not null) result.pathReferences.AddRange(task.pathReferences);
                if (task.typeDirectives is not null) result.typeDirectives.AddRange(task.typeDirectives);
            }
        }
        CollectFiles(loader.rootTasks);
        for (int i = 0; i < result.resolutionEdges.Count; i++)
        {
            ProjectResolutionEdge edge = result.resolutionEdges[i];
            if (edge.Resolution.IsResolved() && result.filesByPath.TryGetValue(loader.KeyText(edge.Resolution.ResolvedPath), out SourceFile? target))
                result.resolutionEdges[i] = edge with { TargetFile = target };
        }
        foreach (var edge in result.graphEdges)
        {
            if (edge.Resolution.IsResolved() && result.filesByPath.TryGetValue(loader.KeyText(edge.Resolution.ResolvedPath), out var target))
            {
                edge.TargetFile = target;
                if (edge.Scheduling == DependencyScheduling.ElidedJavaScriptDepth) edge.Scheduling = DependencyScheduling.Scheduled;
            }
        }
        foreach (var edge in result.pathReferences)
            if (edge.ResolvedPath != default && result.filesByPath.TryGetValue(loader.KeyText(edge.ResolvedPath), out var target)) edge.TargetFile = target;
        foreach (var edge in result.typeDirectives)
            if (edge.Resolution?.IsResolved() == true && result.filesByPath.TryGetValue(loader.KeyText(edge.Resolution.ResolvedPath), out var target)) edge.TargetFile = target;
        loader.includeProcessor.SetProcessedFiles(result, loader.options);
        result.processingDiagnostics.AddRange(loader.includeProcessor.GetDiagnostics());
        foreach (var edge in result.pathReferences)
            if (edge.ProcessingDiagnostic is not null) edge.Diagnostic = edge.ProcessingDiagnostic.ToDiagnostic(loader.includeProcessor);
        return result;
    }
}

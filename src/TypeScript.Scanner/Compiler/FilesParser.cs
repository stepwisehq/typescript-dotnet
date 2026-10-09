// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial port of tsc/internal/compiler/filesparser.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Single-threaded, case-sensitive
// non-package tasks. Pools/mutexes/JS depth/redirect and content-mapper branches
// are excluded; fresh task records retain loading and collection identities.
using TypeScript.Modules;
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal sealed class ParseTask
{
    internal string normalizedFilePath = "", path = "";
    internal SourceFile? file;
    internal List<ParseTask>? subTasks;
    internal bool loaded, startedSubTasks, failedLookup;
    internal ParseTask? loadedTask;
    internal Dictionary<ModeAwareCacheKey, ResolvedModule>? resolutionsInFile;
    internal List<ProjectResolutionEdge>? resolutionEdges;
    internal List<ProcessingDiagnostic>? processingDiagnostics;
    internal FileIncludeReason includeReason = new();
    internal string FileName() => normalizedFilePath;
    internal string PathKey() => path;
    // filesparser.go:59. Mark loaded before child scheduling. The selected
    // explicit libraries are ordinary roots, not default-library tasks.
    internal void Load(FileLoader loader)
    {
        loaded = true;
        if (failedLookup)
            return;
        file ??= loader.ParseSourceFile(this);
        if (file is null)
            return;
        loader.ValidateParsedSource(file);
        subTasks = [];
        if (file.Diagnostics()is { Count: > 0 })
            return;
        // noLib=true skips lib references. User triple-slash/types references
        // and augmentations are rejected by admission; embedded libs have no
        // path/types references requiring discovery in this exact environment.
        loader.ResolveImportsAndModuleAugmentations(this);
    }

    internal void AddSubTask(string fileName, string key, FileIncludeReason reason) => (subTasks ??= []).Add(new ParseTask { normalizedFilePath = fileName, path = key, includeReason = reason });
}

internal sealed class ParseTaskData
{
    internal ParseTask task;
    internal bool startedSubTasks;
    internal int lowestDepth = int.MaxValue;
    internal ParseTaskData(ParseTask task) => this.task = task;
}

internal sealed class ProcessedFiles
{
    internal readonly List<SourceFile> files = [];
    internal readonly Dictionary<string, SourceFile> filesByPath = new(StringComparer.Ordinal);
    internal readonly HashSet<string> missingFiles = new(StringComparer.Ordinal);
    internal readonly List<ProjectResolutionEdge> resolutionEdges = [];
    internal readonly List<Diagnostic> processingDiagnostics = [];
}

internal sealed class FilesParser
{
    private readonly Dictionary<string, ParseTaskData> taskDataByPath = new(StringComparer.Ordinal);
    private readonly Stack<Action> work = new();
    // filesparser.go:268 + core/workgroup.go:57/67/78. LIFO callbacks match the
    // original single-threaded WorkGroup; final source order is collected later.
    internal void Parse(FileLoader loader, List<ParseTask> tasks)
    {
        Start(loader, tasks, 0);
        while (work.TryPop(out Action? callback))
            callback();
    }

    internal void Start(FileLoader loader, List<ParseTask> tasks, int depth)
    {
        for (int i = 0; i < tasks.Count; i++)
        {
            int slot = i;
            ParseTask task = tasks[i];
            if (task.path == "")
                throw new InvalidOperationException("parse task must have a path key");
            bool loaded = taskDataByPath.TryGetValue(task.path, out ParseTaskData? data);
            if (!loaded)
                taskDataByPath.Add(task.path, data = new(task));
            ParseTaskData actual = data!;
            work.Push(() =>
            {
                bool startSubtasks = false;
                if (loaded)
                    tasks[slot].loadedTask = actual.task;
                bool lowered = depth < actual.lowestDepth;
                if (lowered)
                {
                    actual.lowestDepth = depth;
                    startSubtasks = true;
                    actual.startedSubTasks = true;
                }

                ParseTask taskByFileName = actual.task;
                if (!taskByFileName.loaded)
                    taskByFileName.Load(loader);
                if (startSubtasks && (lowered || !taskByFileName.startedSubTasks))
                {
                    taskByFileName.startedSubTasks = true;
                    Start(loader, taskByFileName.subTasks ?? [], actual.lowestDepth);
                }
            });
        }
    }

    // filesparser.go:337/394/488. Mark data visited before subtasks, retain
    // dependency postorder, then final original map publication. Case-sensitive
    // duplicate-name collisions retain original include reasons and diagnostics.
    internal ProcessedFiles GetProcessedFiles(FileLoader loader)
    {
        var result = new ProcessedFiles();
        var seen = new HashSet<ParseTaskData>(ReferenceEqualityComparer.Instance);
        var tasksSeenByNameIgnoreCase = new Dictionary<string, ParseTask>(StringComparer.Ordinal);
        void CollectFiles(List<ParseTask> tasks)
        {
            foreach (ParseTask input in tasks)
            {
                ParseTask task = input.loadedTask ?? input;
                ParseTaskData data = taskDataByPath[task.path];
                loader.includeProcessor.AddIncludeReason(task, input.includeReason);
                if (!task.loaded || !seen.Add(data))
                    continue;
                string lower = TsPath.ToFileNameLowerCase(task.path);
                if (tasksSeenByNameIgnoreCase.TryGetValue(lower, out ParseTask? existing))
                    loader.includeProcessor.AddProcessingDiagnosticsForFileCasing(existing.path, existing.normalizedFilePath, task.normalizedFilePath, input.includeReason);
                else
                    tasksSeenByNameIgnoreCase.Add(lower, task);
                if (task.subTasks is { Count: > 0 })
                    CollectFiles(task.subTasks);
                if (task.processingDiagnostics is not null)
                    loader.includeProcessor.processingDiagnostics.AddRange(task.processingDiagnostics);
                if (task.file is null)
                {
                    result.missingFiles.Add(task.path);
                    continue;
                }

                result.files.Add(task.file);
                result.filesByPath.Add(task.path, task.file);
                if (task.resolutionEdges is not null)
                    result.resolutionEdges.AddRange(task.resolutionEdges);
            }
        }

        CollectFiles(loader.rootTasks);
        for (int i = 0; i < result.resolutionEdges.Count; i++)
        {
            ProjectResolutionEdge edge = result.resolutionEdges[i];
            if (edge.Resolution.IsResolved() && result.filesByPath.TryGetValue(edge.Resolution.ResolvedPath, out SourceFile? target))
                result.resolutionEdges[i] = edge with
                {
                    TargetFile = target
                };
        }

        result.processingDiagnostics.AddRange(loader.includeProcessor.GetDiagnostics());
        return result;
    }
}

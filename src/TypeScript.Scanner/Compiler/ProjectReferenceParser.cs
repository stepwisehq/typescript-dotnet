// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/compiler/projectreferenceparser.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Task allocations replace source pools.
using TypeScript.Collections;
using TypeScript.Scanner;
using TypeScript.TsOptions;

namespace TypeScript.Compiler;
internal sealed class ProjectReferenceParseTask
{
    internal RootedFilePath configName;
    internal ParsedCommandLine? resolved;
    internal Slice<ProjectReferenceParseTask> subTasks;
    internal void Parse(ProjectReferenceParser parser)
    {
        var loader = parser.loader;
        Action? done = loader.tracing?.Push(TypeScript.Tracing.Phase.Parse, "parseJsonSourceFileConfigFileContent", new() { ["path"] = configName.AsString() }, false);
        try
        {
            resolved = loader.compilerHost!.GetResolvedProjectReference(configName, loader.ToPath(configName.AsPath()));
            if (resolved is null) return;
            resolved.ParseInputOutputNames();
            var subReferences = resolved.ResolvedProjectReferencePaths();
            if (subReferences is { Count: > 0 }) subTasks = ProjectReferenceParser.CreateProjectReferenceParseTasks(subReferences);
        }
        finally { done?.Invoke(); }
    }
}
internal sealed class ProjectReferenceParser
{
    internal readonly FileLoader loader;
    private readonly IWorkGroup workGroup;
    private readonly SyncMap<PathKey, ProjectReferenceParseTask> tasksByFileName = new();
    internal ProjectReferenceParser(FileLoader loader, bool singleThreaded) => (this.loader, workGroup) = (loader, Core.NewWorkGroup(singleThreaded));
    internal static Slice<ProjectReferenceParseTask> CreateProjectReferenceParseTasks(List<RootedFilePath>? references) => Core.MapSlice(references, static name => new ProjectReferenceParseTask { configName = name });
    internal void Parse(Slice<ProjectReferenceParseTask> tasks)
    {
        Start(tasks);
        workGroup.RunAndWait();
        InitMapper(tasks);
    }
    private void Start(Slice<ProjectReferenceParseTask> tasks)
    {
        for (int i = 0; i < tasks.Length; i++)
        {
            var task = tasks[i];
            var (loadedTask, loaded) = tasksByFileName.LoadOrStore(loader.ToPath(task.configName.AsPath()), task);
            if (loaded) tasks[i] = loadedTask;
            else workGroup.Queue(() => { task.Parse(this); Start(task.subTasks); });
        }
    }
    private void InitMapper(Slice<ProjectReferenceParseTask> tasks)
    {
        var mapper = loader.projectReferences!;
        mapper.configToProjectReference = new(tasksByFileName.Size() + 1);
        mapper.referencesInConfigFile = new(tasksByFileName.Size() + 1);
        mapper.sourceToProjectReference = [];
        mapper.outputDtsToProjectReference = [];
        mapper.referencesInConfigFile[mapper.RootConfigPath()] = InitMapperWorker(tasks, new(ReferenceEqualityComparer.Instance));
        mapper.host = mapper.ResolutionHost(mapper.host);
    }
    private Slice<PathKey> InitMapperWorker(Slice<ProjectReferenceParseTask> tasks, HashSet<ProjectReferenceParseTask> seen)
    {
        if (tasks.Length == 0) return default;
        var mapper = loader.projectReferences!;
        var results = Slice<PathKey>.Make(0, tasks.Length);
        foreach (var task in tasks)
        {
            var path = loader.ToPath(task.configName.AsPath());
            results = results.Append(path);
            if (!seen.Add(task)) continue;
            mapper.configToProjectReference[path] = task.resolved;
            if (task.resolved is not null && !ReferenceEquals(mapper.config.ConfigFile, task.resolved.ConfigFile))
            {
                foreach (var pair in task.resolved.SourceToProjectReference() ?? []) mapper.sourceToProjectReference[pair.Key] = pair.Value;
                foreach (var pair in task.resolved.OutputDtsToProjectReference() ?? []) mapper.outputDtsToProjectReference[pair.Key] = pair.Value;
                if (mapper.useSourceOfProjectReference)
                {
                    var declarationDirectory = task.resolved.CompilerOptions().DeclarationDir;
                    if (declarationDirectory == default) declarationDirectory = task.resolved.CompilerOptions().OutDir;
                    if (declarationDirectory != default) mapper.dtsDirectories.Add(loader.ToPath(declarationDirectory.AsPath()));
                }
            }
            mapper.referencesInConfigFile[path] = InitMapperWorker(task.subTasks, seen);
        }
        return results;
    }
}

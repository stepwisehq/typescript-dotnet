// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/compiler/filesparser.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Source WorkGroup task scheduling,
// actual Program publication and separate graph observations; fresh task/data
// storage replaces source pools without changing acquire/release ownership.
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
    internal ModeAwareCache<ResolvedModule?>? resolutionsInFile;
    internal ModeAwareCache<ResolvedTypeReferenceDirective?>? typeResolutionsInFile;
    internal List<DiagAndArgs>? resolutionsTrace, typeResolutionsTrace;
    internal LibFile? libFile;
    internal ParseTask? redirectedParseTask;
    internal bool isForAutomaticTypeDirective, isContentMapperSupplemental;
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
        if (isForAutomaticTypeDirective) { loader.ResolveAutomaticTypeDirectives(this); return; }
        if (failedLookup) return;
        if (loader.program)
        {
            var (redirect, redirectPath) = loader.projectReferences!.GetParseFileRedirect(loader.TypedFile(normalizedFilePath), TypeScript.Scanner.PathKey.FromGoString(path));
            if (redirect != default)
            {
                redirectedParseTask = new() { normalizedFilePath = redirect.AsString(), path = redirectPath.AsString(), libFile = libFile, includeReason = includeReason };
                subTasks = [redirectedParseTask];
                return;
            }
        }
        if (loader.graph && !isContentMapperSupplemental && loader.TypedFile(normalizedFilePath).HasExtension() && loader.options.AllowNonTsExtensions != Tristate.True && !loader.IsSupportedExtension(path))
        {
            Diagnostic diagnostic = loader.UnsupportedExtensionDiagnostic(normalizedFilePath, loader.TypedFile(normalizedFilePath));
            (processingDiagnostics ??= []).Add(new() { diagnosticReason = includeReason, message = diagnostic.message, args = diagnostic.MessageArgs()?.ToArray() ?? [] });
            if (incomingEdge is not null) incomingEdge.Scheduling = DependencyScheduling.UnsupportedExtension;
            return;
        }
        if (loader.graph) metadata = libFile is not null ? new("", default, ResolutionMode.CommonJS) : loader.LoadSourceFileMetaData(normalizedFilePath);
        file ??= loader.ParseSourceFile(this);
        if (file is null)
        {
            if (incomingEdge is not null) incomingEdge.Scheduling = DependencyScheduling.MissingFile;
            return;
        }
        loader.ValidateParsedSource(file);
        if (loader.program && file.VirtualFileName() != default) metadata = new(metadata.PackageJsonType, metadata.PackageJsonDirectory, (ResolutionMode)Ast.GetImpliedNodeFormatForFile(file.VirtualFileName(), metadata.PackageJsonType));
        subTasks = [];
        // Graph collection follows original parser output even when diagnostics
        // exist. The named legacy checking adapter prevents malformed entry.
        if (!loader.graph && file.Diagnostics() is { Count: > 0 }) return;
        if (loader.graph && (!loader.program || !loader.opts.SkipModuleResolution && loader.options.NoResolve != Tristate.True))
        {
            loader.ResolveTripleslashPathReferences(this);
            loader.ResolveTypeReferenceDirectives(this);
        }
        if (loader.program && loader.options.NoLib != Tristate.True && !loader.opts.SkipModuleResolution)
        {
            foreach (var reference in file.LibReferenceDirectives ?? [])
            {
                var reason = new FileIncludeReason { importingFile = file, reference = reference, kind = IncludeReferenceKind.Lib };
                var (name, ok) = TypeScript.TsOptions.TsOptions.GetLibFileName(reference.FileName);
                if (ok)
                {
                    var lib = loader.PathForLibFile(name);
                    AddSubTask(lib.path.AsString(), lib.pathKey.AsString(), reason);
                    subTasks![^1].libFile = lib;
                }
                else (processingDiagnostics ??= []).Add(new() { kind = ProcessingDiagnosticKind.UnknownReference, diagnosticReason = reason });
            }
        }
        loader.ResolveImportsAndModuleAugmentations(this);
        if (loader.program)
            foreach (var supplemental in file.SupplementalSourceFiles() ?? [])
                (subTasks ??= []).Add(new() { normalizedFilePath = supplemental.FileNameGoBytes(), path = loader.ToPath(RootedFilePath.FromGoString(supplemental.FileNameGoBytes()).AsPath()).AsString(), file = supplemental, isContentMapperSupplemental = true, includeReason = new() { canonicalSourceFile = path } });
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
    internal readonly object gate = new();
    internal readonly Dictionary<string, ParseTask> tasks = new(StringComparer.Ordinal);
    internal bool startedSubTasks;
    internal long lowestDepth = long.MaxValue;
    internal PackageId packageId;
    internal ParseTaskData(ParseTask task) => tasks.Add(task.normalizedFilePath, task);
}

// filesparser.go/fileloader.go: source-owned publication tables. String keys
// are canonical Go-byte PathKey.AsString() (legacy adapter uses its established
// scalar keys); Program typed entrypoints adapt once at their lookup boundary.
public sealed class DuplicateSourceFile
{
    public SourceFileParseOptions ParseOptions, ContentMapperParseOptions;
    public UInt128 Hash;
    public ScriptKind ScriptKind;
    public string ContentMapper = "";
    public bool IsContentMapperFailureStub;
}
public sealed class LibFile
{
    public string Name = "";
    internal RootedFilePath path;
    internal PathKey pathKey;
    public bool Replaced;
}
internal sealed class RedirectsFile
{
    internal int index;
    internal RootedFilePath fileName;
    internal PathKey path, target;
    internal RootedFilePath FileName() => fileName;
    internal PathKey PathKey() => path;
}
internal sealed class JsxRuntimeImportSpecifier
{
    internal string moduleReference = "";
    internal Node specifier = null!;
}
internal sealed class ProcessedFiles
{
    internal List<SourceFile> files = [];
    internal List<DuplicateSourceFile>? duplicateSourceFiles;
    internal Dictionary<string, SourceFile> filesByPath = new(StringComparer.Ordinal);
    internal ProjectReferenceFileMapper projectReferenceFileMapper = null!;
    internal HashSet<string> missingFiles = new(StringComparer.Ordinal);
    internal Dictionary<string, ModeAwareCache<ResolvedModule?>> resolvedModules = new(StringComparer.Ordinal);
    internal Dictionary<string, ModeAwareCache<ResolvedTypeReferenceDirective?>> typeResolutionsInFile = new(StringComparer.Ordinal);
    internal Dictionary<string, SourceFileMetaData> sourceFileMetaDatas = new(StringComparer.Ordinal);
    internal Dictionary<string, SourceFileMetaData> metadata => sourceFileMetaDatas;
    internal Dictionary<string, JsxRuntimeImportSpecifier>? jsxRuntimeImportSpecifiers;
    internal Dictionary<string, Node>? importHelpersImportSpecifiers;
    internal Dictionary<string, LibFile> libFiles = new(StringComparer.Ordinal);
    internal HashSet<string> sourceFilesFoundSearchingNodeModules = new(StringComparer.Ordinal);
    internal FileIncludeData fileIncludeData = new();
    internal Dictionary<string, RootedFilePath> outputFileToProjectReferenceSource = new(StringComparer.Ordinal);
    internal Dictionary<string, List<string>> redirectTargetsMap = new(StringComparer.Ordinal);
    internal Dictionary<string, RedirectsFile>? redirectFilesByPath;
    internal List<Diagnostic>? contentMapperDiagnostics;
    internal bool finishedProcessing;
    // Existing graph observation associations do not substitute Program tables.
    internal List<ProjectResolutionEdge> resolutionEdges = [];
    internal List<ResolvedProjectEdge> graphEdges = [];
    internal List<ResolvedProjectReferenceEdge> pathReferences = [];
    internal List<ResolvedProjectTypeDirective> typeDirectives = [];
    internal Dictionary<string, SourceFile> packageRedirects = new(StringComparer.Ordinal);
    internal List<Diagnostic> processingDiagnostics = [];
    internal ProcessedFiles ShallowClone() => (ProcessedFiles)MemberwiseClone();
}

internal sealed class FilesParser
{
    private readonly TypeScript.Collections.SyncMap<string, ParseTaskData> taskDataByPath = new();
    private readonly IWorkGroup work;
    internal FilesParser(bool singleThreaded = true) => work = Core.NewWorkGroup(singleThreaded);
    internal long maxDepth;

    // filesparser.go:268 + core/workgroup.go:57/67/78. LIFO callbacks preserve
    // single-threaded scheduling; ordered sources are collected independently.
    internal void Parse(FileLoader loader, List<ParseTask> tasks)
    {
        Start(loader, tasks, 0);
        work.RunAndWait();
    }
    internal void Start(FileLoader loader, List<ParseTask> tasks, long depth)
    {
        for (int i = 0; i < tasks.Count; i++)
        {
            int slot = i;
            ParseTask task = tasks[i];
            if (task.path == "") throw new InvalidOperationException("parse task must have a path key");
            var (actual, loaded) = taskDataByPath.LoadOrStore(task.path, new(task));
            work.Queue(() =>
            {
                lock (actual.gate)
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
                    bool loadSubTasks = startSubtasks;
                    if (!taskByFileName.loaded)
                    {
                        taskByFileName.Load(loader);
                        if (taskByFileName.redirectedParseTask is not null)
                        {
                            loadSubTasks = true;
                            actual.startedSubTasks = true;
                        }
                    }
                    if (loadSubTasks && (lowered || !taskByFileName.startedSubTasks))
                    {
                        taskByFileName.startedSubTasks = true;
                        Start(loader, taskByFileName.subTasks ?? [], actual.lowestDepth);
                    }
                }
                }
            });
        }
    }

    // filesparser.go:337/394/488. Package deduplication precedes child traversal,
    // then source postorder and actual metadata/resolution publication follow.
    internal ProcessedFiles GetProcessedFiles(FileLoader loader)
    {
        var result = new ProcessedFiles { fileIncludeData = loader.includeProcessor.data, projectReferenceFileMapper = loader.projectReferences?.PublishData()! };
        List<SourceFile> libs = [];
        void RecordDuplicate(SourceFile file) => (result.duplicateSourceFiles ??= []).Add(new() { ParseOptions = file.ParseOptions(), ContentMapperParseOptions = file.ContentMapperParseOptions(), Hash = file.Hash, ScriptKind = file.ScriptKind, ContentMapper = file.ContentMapper(), IsContentMapperFailureStub = file.IsContentMapperFailureStub() });
        var seen = new Dictionary<ParseTaskData, string>(ReferenceEqualityComparer.Instance);
        Dictionary<ParseTaskData, HashSet<string>>? recordedDuplicates = null;
        var tasksSeenByNameIgnoreCase = new Dictionary<string, ParseTask>(StringComparer.Ordinal);
        Dictionary<PackageId, SourceFile>? packageFiles = loader.graph && loader.options.DeduplicatePackages != Tristate.False ? new() : null;
        void CollectFiles(List<ParseTask> tasks)
        {
            foreach (ParseTask input in tasks)
            {
                ParseTask task = input;
                if (task.redirectedParseTask is null && !task.isForAutomaticTypeDirective)
                {
                    task = task.loadedTask ?? task;
                    loader.includeProcessor.AddIncludeReason(task, input.includeReason);
                }
                ParseTaskData data = taskDataByPath.Load(task.path).Value;
                if (!task.loaded) continue;
                if (seen.TryGetValue(data, out string? checkedName))
                {
                    if (loader.program && task.file is not null && task.normalizedFilePath != checkedName)
                    {
                        recordedDuplicates ??= new(ReferenceEqualityComparer.Instance);
                        if (!recordedDuplicates.TryGetValue(data, out var duplicates))
                            recordedDuplicates.Add(data, duplicates = new(StringComparer.Ordinal));
                        if (duplicates.Add(task.normalizedFilePath)) RecordDuplicate(task.file);
                    }
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
                if (loader.program)
                {
                    foreach (var trace in task.typeResolutionsTrace ?? []) loader.compilerHost!.Trace(trace.Message, trace.Args?.Select(value => value!).ToArray() ?? []);
                    foreach (var trace in task.resolutionsTrace ?? []) loader.compilerHost!.Trace(trace.Message, trace.Args?.Select(value => value!).ToArray() ?? []);
                }
                if (packageFiles is not null && data.packageId.Name != "")
                {
                    if (packageFiles.TryGetValue(data.packageId, out SourceFile? packageFile))
                    {
                        if (loader.program)
                        {
                            if (task.file is not null) RecordDuplicate(task.file);
                            (result.redirectFilesByPath ??= new(StringComparer.Ordinal))[task.path] = new() { index = result.files.Count + (result.redirectFilesByPath?.Count ?? 0), fileName = loader.TypedFile(task.normalizedFilePath), path = PathKey.FromGoString(task.path), target = PathKey.FromGoString(packageFile.PathKey()) };
                            if (data.lowestDepth > 0) result.sourceFilesFoundSearchingNodeModules.Add(task.path);
                        }
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
                if (task.redirectedParseTask is not null)
                {
                    if (!loader.opts.canUseProjectReferenceSource()) result.outputFileToProjectReferenceSource[task.redirectedParseTask.path] = loader.TypedFile(task.normalizedFilePath);
                    continue;
                }
                if (task.isForAutomaticTypeDirective)
                {
                    if (loader.program || task.typeResolutionsInFile is not null) result.typeResolutionsInFile[task.path] = task.typeResolutionsInFile!;
                    if (task.processingDiagnostics is not null) loader.includeProcessor.processingDiagnostics.AddRange(task.processingDiagnostics);
                    continue;
                }
                if (task.processingDiagnostics is not null) loader.includeProcessor.processingDiagnostics.AddRange(task.processingDiagnostics);
                if (task.file is null)
                {
                    result.missingFiles.Add(task.path);
                    continue;
                }
                if (task.libFile is not null) { libs.Add(task.file); result.libFiles[task.path] = task.libFile; }
                else result.files.Add(task.file);
                result.filesByPath.Add(task.path, task.file);
                result.metadata.Add(task.path, task.metadata);
                if (loader.program || task.resolutionsInFile is not null) result.resolvedModules[task.path] = task.resolutionsInFile!;
                if (loader.program || task.typeResolutionsInFile is not null) result.typeResolutionsInFile[task.path] = task.typeResolutionsInFile!;
                if (task.importHelpersImportSpecifier is not null) (result.importHelpersImportSpecifiers ??= new(StringComparer.Ordinal))[task.path] = task.importHelpersImportSpecifier;
                if (task.jsxRuntimeImportSpecifier is not null) (result.jsxRuntimeImportSpecifiers ??= new(StringComparer.Ordinal))[task.path] = new() { moduleReference = task.jsxRuntimeImportSpecifier.Text(), specifier = task.jsxRuntimeImportSpecifier };
                if (data.lowestDepth > 0) result.sourceFilesFoundSearchingNodeModules.Add(task.path);
                if (task.resolutionEdges is not null) result.resolutionEdges.AddRange(task.resolutionEdges);
                if (task.graphEdges is not null) result.graphEdges.AddRange(task.graphEdges);
                if (task.pathReferences is not null) result.pathReferences.AddRange(task.pathReferences);
                if (task.typeDirectives is not null) result.typeDirectives.AddRange(task.typeDirectives);
            }
        }
        CollectFiles(loader.rootTasks);
        if (loader.program)
        {
            loader.SortLibs(libs);
            if (result.redirectFilesByPath is not null) foreach (var redirect in result.redirectFilesByPath.Values) redirect.index += libs.Count;
            libs.AddRange(result.files);
            result.files = libs;
            var keys = loader.pathForLibFileResolutions.Keys().ToList();
            keys.Sort((a, b) => string.CompareOrdinal(a.AsString(), b.AsString()));
            foreach (var key in keys)
            {
                var value = loader.pathForLibFileResolutions.Load(key).Value;
                result.resolvedModules[key.AsString()] = new() { [new(value.libraryName, ResolutionMode.CommonJS)] = value.resolution };
                foreach (var trace in value.trace ?? []) loader.compilerHost!.Trace(trace.Message, trace.Args?.Select(value => value!).ToArray() ?? []);
            }
            result.contentMapperDiagnostics = loader.ContentMapperDiagnostics();
        }
        result.finishedProcessing = true;
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
        if (!loader.program) result.processingDiagnostics.AddRange(loader.includeProcessor.GetDiagnostics());
        foreach (var edge in result.pathReferences)
            if (edge.ProcessingDiagnostic is not null) edge.Diagnostic = edge.ProcessingDiagnostic.ToDiagnostic(loader.includeProcessor);
        return result;
    }
}

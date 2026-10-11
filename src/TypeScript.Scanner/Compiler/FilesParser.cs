// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/compiler/filesparser.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Source WorkGroup task scheduling,
// actual Program publication and separate graph observations; parse-task data
// candidates follow the source acquire/release ownership.
using TypeScript.Modules;
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal sealed class ParseTask
{
    internal ByteStringKey normalizedFilePath, path;
    internal SourceFile? file;
    internal Slice<ParseTask> subTasks;
    internal bool loaded, startedSubTasks, failedLookup, increaseDepth, elideOnDepth;
    internal ParseTask? loadedTask;
    internal PackageId packageId;
    internal SourceFileMetaData metadata;
    internal ModeAwareCache<ResolvedModule?>? resolutionsInFile;
    internal ModeAwareCache<ResolvedTypeReferenceDirective?>? typeResolutionsInFile;
    internal Slice<DiagAndArgs> resolutionsTrace, typeResolutionsTrace;
    internal LibFile? libFile;
    internal ParseTask? redirectedParseTask;
    internal bool isForAutomaticTypeDirective, isContentMapperSupplemental;
    internal List<ProjectResolutionEdge>? resolutionEdges;
    internal List<ResolvedProjectEdge>? graphEdges;
    internal List<ResolvedProjectReferenceEdge>? pathReferences;
    internal List<ResolvedProjectTypeDirective>? typeDirectives;
    internal Slice<ProcessingDiagnostic> processingDiagnostics;
    internal Node? importHelpersImportSpecifier, jsxRuntimeImportSpecifier;
    internal ResolvedProjectEdge? incomingEdge;
    // filesparser.go:31: a source pointer starts nil; normal producers assign it.
    internal FileIncludeReason? includeReason;
    internal string FileName() => normalizedFilePath.ToString();
    internal string PathKey() => path.ToString();

    // filesparser.go:59. Loading is published before parsing or child scheduling.
    internal void Load(FileLoader loader)
    {
        loaded = true;
        if (isForAutomaticTypeDirective) { loader.ResolveAutomaticTypeDirectives(this); return; }
        if (failedLookup) return;
        if (loader.program)
        {
            var (redirect, redirectPath) = loader.projectReferences!.GetParseFileRedirect(loader.TypedFile(normalizedFilePath), TypeScript.Scanner.PathKey.FromValue(path.Value));
            if (redirect != default)
            {
                redirectedParseTask = new() { normalizedFilePath = ByteStringKey.FromValue(redirect.AsValue()), path = ByteStringKey.FromValue(redirectPath.AsValue()), libFile = libFile, includeReason = includeReason };
                subTasks = Slice<ParseTask>.FromArray([redirectedParseTask]);
                return;
            }
        }
        if (loader.graph && !isContentMapperSupplemental && loader.TypedFile(normalizedFilePath).HasExtension() && loader.options.AllowNonTsExtensions != Tristate.True && !loader.IsSupportedExtension(path))
        {
            Diagnostic diagnostic = loader.UnsupportedExtensionDiagnostic(normalizedFilePath.Value, loader.TypedFile(normalizedFilePath));
            processingDiagnostics = processingDiagnostics.Append(new ProcessingDiagnostic() { diagnosticReason = includeReason, message = diagnostic.message, args = diagnostic.MessageArgs()?.ToArray() ?? [] });
            if (incomingEdge is not null) incomingEdge.Scheduling = DependencyScheduling.UnsupportedExtension;
            return;
        }
        Interlocked.Increment(ref loader.totalFileCount);
        if (libFile is not null) Interlocked.Increment(ref loader.libFileCount);
        if (loader.graph) metadata = libFile is not null ? new("", default, ResolutionMode.CommonJS) : loader.LoadSourceFileMetaData(normalizedFilePath);
        file ??= loader.ParseSourceFile(this);
        if (file is null)
        {
            if (incomingEdge is not null) incomingEdge.Scheduling = DependencyScheduling.MissingFile;
            return;
        }
        loader.ValidateParsedSource(file);
        if (loader.program && file.VirtualFileName() != default) metadata = new(metadata.PackageJsonType, metadata.PackageJsonDirectory, (ResolutionMode)Ast.GetImpliedNodeFormatForFile(file.VirtualFileName(), metadata.PackageJsonType));
        subTasks = default;
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
            foreach (var reference in file.LibReferenceDirectives)
            {
                var reason = new FileIncludeReason { importingFile = file, reference = reference, kind = IncludeReferenceKind.Lib };
                var (name, ok) = TypeScript.TsOptions.TsOptions.GetLibFileName(reference.FileNameValue);
                if (ok)
                {
                    var lib = loader.PathForLibFile(name);
                    AddSubTask(ByteStringKey.FromValue(lib.path.AsValue()), ByteStringKey.FromValue(lib.pathKey.AsValue()), reason);
                    subTasks[subTasks.Length - 1].libFile = lib;
                }
                else processingDiagnostics = processingDiagnostics.Append(new ProcessingDiagnostic() { kind = ProcessingDiagnosticKind.UnknownReference, diagnosticReason = reason });
            }
        }
        loader.ResolveImportsAndModuleAugmentations(this);
        if (loader.program)
            foreach (var supplemental in file.SupplementalSourceFiles())
                subTasks = subTasks.Append(new ParseTask() { normalizedFilePath = ByteStringKey.FromValue(supplemental.FileNameGoValue()), path = ByteStringKey.FromValue(loader.ToPath(RootedFilePath.FromValue(supplemental.FileNameGoValue()).AsPath()).AsValue()), file = supplemental, isContentMapperSupplemental = true, includeReason = new() { canonicalSourceFile = path } });
    }

    // filesparser.go:217. Package identity and depth flags are actual resolver output.
    internal void AddSubTask(string fileName, string key, FileIncludeReason reason, bool increaseDepth = false,
        bool elideOnDepth = false, PackageId packageId = default, ResolvedProjectEdge? incomingEdge = null) => AddSubTask(ByteStringKey.FromString(fileName), ByteStringKey.FromString(key), reason, increaseDepth, elideOnDepth, packageId, incomingEdge);
    internal void AddSubTask(ByteStringKey fileName, ByteStringKey key, FileIncludeReason reason, bool increaseDepth = false,
        bool elideOnDepth = false, PackageId packageId = default, ResolvedProjectEdge? incomingEdge = null) => subTasks = subTasks.Append(new ParseTask
        {
            normalizedFilePath = fileName, path = key, includeReason = reason, increaseDepth = increaseDepth,
            elideOnDepth = elideOnDepth, packageId = packageId, incomingEdge = incomingEdge
        });
}

internal sealed class ParseTaskData
{
    internal readonly object gate = new();
    internal readonly Dictionary<ByteStringKey, ParseTask> tasks = new(1, ByteStringKey.Comparer);
    internal bool startedSubTasks;
    internal long lowestDepth = long.MaxValue;
    internal PackageId packageId;
    internal ParseTaskData() { }
}

// filesparser.go/fileloader.go: source-owned publication tables retain raw path
// content headers (or legacy scalar ingress), with content-based dictionary keys.
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
    internal ByteStringValue NameValue;
    public string Name { get => NameValue.Materialize()!; set => NameValue = ByteStringValue.FromString(value); }
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
    internal ByteStringValue moduleReference;
    internal Node specifier = null!;
}
internal sealed class ProcessedFiles
{
    internal Slice<SourceFile> files;
    internal Slice<DuplicateSourceFile> duplicateSourceFiles;
    internal Dictionary<ByteStringKey, SourceFile> filesByPath = new(ByteStringKey.Comparer);
    internal ProjectReferenceFileMapper projectReferenceFileMapper = null!;
    internal HashSet<ByteStringKey> missingFiles = new(ByteStringKey.Comparer);
    internal Dictionary<ByteStringKey, ModeAwareCache<ResolvedModule?>> resolvedModules = new(ByteStringKey.Comparer);
    internal Dictionary<ByteStringKey, ModeAwareCache<ResolvedTypeReferenceDirective?>> typeResolutionsInFile = new(ByteStringKey.Comparer);
    internal Dictionary<ByteStringKey, SourceFileMetaData> sourceFileMetaDatas = new(ByteStringKey.Comparer);
    internal Dictionary<ByteStringKey, SourceFileMetaData> metadata => sourceFileMetaDatas;
    internal Dictionary<ByteStringKey, JsxRuntimeImportSpecifier>? jsxRuntimeImportSpecifiers;
    internal Dictionary<ByteStringKey, Node>? importHelpersImportSpecifiers;
    internal Dictionary<ByteStringKey, LibFile> libFiles = new(ByteStringKey.Comparer);
    internal HashSet<ByteStringKey> sourceFilesFoundSearchingNodeModules = new(ByteStringKey.Comparer);
    internal FileIncludeData fileIncludeData = new();
    internal Dictionary<ByteStringKey, RootedFilePath> outputFileToProjectReferenceSource = new(ByteStringKey.Comparer);
    internal Dictionary<ByteStringKey, Slice<RootedFilePath>> redirectTargetsMap = new(ByteStringKey.Comparer);
    internal Dictionary<ByteStringKey, RedirectsFile>? redirectFilesByPath;
    internal Slice<Diagnostic> contentMapperDiagnostics;
    internal bool finishedProcessing;
    // Existing graph observation associations do not substitute Program tables.
    internal List<ProjectResolutionEdge> resolutionEdges = [];
    internal List<ResolvedProjectEdge> graphEdges = [];
    internal List<ResolvedProjectReferenceEdge> pathReferences = [];
    internal List<ResolvedProjectTypeDirective> typeDirectives = [];
    internal Dictionary<ByteStringKey, SourceFile> packageRedirects = new(ByteStringKey.Comparer);
    internal Slice<Diagnostic> processingDiagnostics;
    internal ProcessedFiles ShallowClone() => (ProcessedFiles)MemberwiseClone();
}

internal sealed class FilesParser
{
    private readonly TypeScript.Collections.SyncMap<ByteStringKey, ParseTaskData> taskDataByPath = new();
    private readonly IWorkGroup work;
    // filesparser.go:239-257. Only unpublished losing candidates are returned;
    // installed data stays owned by taskDataByPath through processing.
    private static readonly System.Collections.Concurrent.ConcurrentBag<ParseTaskData> parseTaskDataPool = new();
    private static ParseTaskData getParseTaskData(ParseTask task)
    {
        var data = parseTaskDataPool.TryTake(out var candidate) ? candidate : new ParseTaskData();
        data.tasks[task.normalizedFilePath] = task;
        data.lowestDepth = long.MaxValue;
        return data;
    }
    private static void putParseTaskData(ParseTaskData data)
    {
        data.tasks.Clear();
        parseTaskDataPool.Add(data);
    }
    internal FilesParser(bool singleThreaded = true) => work = Core.NewWorkGroup(singleThreaded);
    internal long maxDepth;

    // filesparser.go:268 + core/workgroup.go:57/67/78. LIFO callbacks preserve
    // single-threaded scheduling; ordered sources are collected independently.
    internal void Parse(FileLoader loader, Slice<ParseTask> tasks)
    {
        Start(loader, tasks, 0);
        work.RunAndWait();
    }
    internal void Start(FileLoader loader, Slice<ParseTask> tasks, long depth)
    {
        for (int i = 0; i < tasks.Count; i++)
        {
            int slot = i;
            ParseTask task = tasks[i];
            if (task.path.Value.Length == 0) throw new InvalidOperationException("parse task must have a path key");
            var candidate = getParseTaskData(task);
            var (actual, loaded) = taskDataByPath.LoadOrStore(task.path, candidate);
            if (loaded) putParseTaskData(candidate);
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
                        Start(loader, taskByFileName.subTasks, actual.lowestDepth);
                    }
                }
                }
            });
        }
    }

    // filesparser.go:337/394/488. Package deduplication precedes child traversal,
    // then source postorder and actual metadata/resolution publication follow.
    // Host.Trace is an explicit managed array boundary; stored trace arguments
    // themselves remain Go headers and retain their original values.
    private static object[] traceArgs(Slice<object?> args)
    {
        var result = new object[args.Length];
        for (int i = 0; i < args.Length; i++) result[i] = args[i]!;
        return result;
    }

    internal ProcessedFiles GetProcessedFiles(FileLoader loader)
    {
        var result = new ProcessedFiles { fileIncludeData = loader.includeProcessor.data, projectReferenceFileMapper = loader.projectReferences?.PublishData()! };
        int totalFileCount = Volatile.Read(ref loader.totalFileCount);
        int libFileCount = Volatile.Read(ref loader.libFileCount);
        result.files = Slice<SourceFile>.Make(0, totalFileCount - libFileCount);
        var libs = Slice<SourceFile>.Make(0, totalFileCount);
        void RecordDuplicate(SourceFile file) => result.duplicateSourceFiles = result.duplicateSourceFiles.Append(new DuplicateSourceFile() { ParseOptions = file.ParseOptions(), ContentMapperParseOptions = file.ContentMapperParseOptions(), Hash = file.Hash, ScriptKind = file.ScriptKind, ContentMapper = file.ContentMapper(), IsContentMapperFailureStub = file.IsContentMapperFailureStub() });
        var seen = new Dictionary<ParseTaskData, ByteStringKey>(ReferenceEqualityComparer.Instance);
        Dictionary<ParseTaskData, HashSet<ByteStringKey>>? recordedDuplicates = null;
        var tasksSeenByNameIgnoreCase = new Dictionary<ByteStringKey, ParseTask>(ByteStringKey.Comparer);
        Dictionary<PackageId, SourceFile>? packageFiles = loader.graph && loader.options.DeduplicatePackages != Tristate.False ? new() : null;
        void CollectFiles(Slice<ParseTask> tasks)
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
                if (seen.TryGetValue(data, out ByteStringKey checkedName))
                {
                    if (loader.program && task.file is not null && task.normalizedFilePath != checkedName)
                    {
                        recordedDuplicates ??= new(ReferenceEqualityComparer.Instance);
                        if (!recordedDuplicates.TryGetValue(data, out var duplicates))
                            recordedDuplicates.Add(data, duplicates = new(ByteStringKey.Comparer));
                        if (duplicates.Add(task.normalizedFilePath)) RecordDuplicate(task.file);
                    }
                    if (loader.options.ForceConsistentCasingInFileNames != Tristate.False &&
                        !loader.TypedFile(checkedName).WithoutRootValue().EqualsValue(loader.TypedFile(task.normalizedFilePath).WithoutRootValue()))
                        loader.includeProcessor.AddProcessingDiagnosticsForFileCasing(task.path, checkedName, task.normalizedFilePath, input.includeReason);
                    continue;
                }
                seen.Add(data, task.normalizedFilePath);
                if (loader.host.FS().CaseSensitivity() == CaseSensitivity.CaseSensitive)
                {
                    var lower = ByteStringKey.FromValue(loader.graph ? PathKey.FromValue(task.path.Value).CaseInsensitiveKey().AsValue() : ByteStringValue.FromString(TsPath.ToFileNameLowerCase(task.path.ToString())));
                    if (tasksSeenByNameIgnoreCase.TryGetValue(lower, out ParseTask? existing))
                        loader.includeProcessor.AddProcessingDiagnosticsForFileCasing(existing.path, existing.normalizedFilePath, task.normalizedFilePath, input.includeReason);
                    else tasksSeenByNameIgnoreCase.Add(lower, task);
                }
                if (loader.program)
                {
                    foreach (var trace in task.typeResolutionsTrace) loader.compilerHost!.Trace(trace.Message, traceArgs(trace.Args));
                    foreach (var trace in task.resolutionsTrace) loader.compilerHost!.Trace(trace.Message, traceArgs(trace.Args));
                }
                if (packageFiles is not null && data.packageId.Name != "")
                {
                    if (packageFiles.TryGetValue(data.packageId, out SourceFile? packageFile))
                    {
                        if (loader.program)
                        {
                            if (task.file is not null) RecordDuplicate(task.file);
                            (result.redirectFilesByPath ??= new(ByteStringKey.Comparer))[task.path] = new() { index = result.files.Count + (result.redirectFilesByPath?.Count ?? 0), fileName = loader.TypedFile(task.normalizedFilePath), path = PathKey.FromValue(task.path.Value), target = PathKey.FromValue(packageFile.PathKeyValue()) };
                            if (data.lowestDepth > 0) result.sourceFilesFoundSearchingNodeModules.Add(task.path);
                        }
                        result.filesByPath[task.path] = packageFile;
                        result.packageRedirects[task.path] = packageFile;
                        // filesparser.go:470. Keep the actual acquired spelling,
                        // not an inverse guess from a canonical dictionary key.
                        var redirectTargets = result.redirectTargetsMap.GetValueOrDefault(packageFile.PathKeyKey());
                        result.redirectTargetsMap[packageFile.PathKeyKey()] = redirectTargets.Append(loader.TypedFile(task.normalizedFilePath));
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
                    loader.includeProcessor.processingDiagnostics = loader.includeProcessor.processingDiagnostics.Append(task.processingDiagnostics);
                    continue;
                }
                loader.includeProcessor.processingDiagnostics = loader.includeProcessor.processingDiagnostics.Append(task.processingDiagnostics);
                if (task.file is null)
                {
                    result.missingFiles.Add(task.path);
                    continue;
                }
                if (task.libFile is not null) { libs = libs.Append(task.file); result.libFiles[task.path] = task.libFile; }
                else result.files = result.files.Append(task.file);
                result.filesByPath.Add(task.path, task.file);
                result.metadata.Add(task.path, task.metadata);
                if (loader.program || task.resolutionsInFile is not null) result.resolvedModules[task.path] = task.resolutionsInFile!;
                if (loader.program || task.typeResolutionsInFile is not null) result.typeResolutionsInFile[task.path] = task.typeResolutionsInFile!;
                if (task.importHelpersImportSpecifier is not null) (result.importHelpersImportSpecifiers ??= new(ByteStringKey.Comparer))[task.path] = task.importHelpersImportSpecifier;
                if (task.jsxRuntimeImportSpecifier is not null) (result.jsxRuntimeImportSpecifiers ??= new(ByteStringKey.Comparer))[task.path] = new() { moduleReference = task.jsxRuntimeImportSpecifier.TextValue(), specifier = task.jsxRuntimeImportSpecifier };
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
            libs = libs.Append(result.files);
            result.files = libs;
            var keys = loader.pathForLibFileResolutions.Keys().ToList();
            keys.Sort(static (a, b) => ByteStringKey.FromValue(a.AsValue()).CompareTo(ByteStringKey.FromValue(b.AsValue())));
            foreach (var key in keys)
            {
                var value = loader.pathForLibFileResolutions.Load(key).Value;
                result.resolvedModules[ByteStringKey.FromValue(key.AsValue())] = new() { [new(value.libraryName, ResolutionMode.CommonJS)] = value.resolution };
                foreach (var trace in value.trace) loader.compilerHost!.Trace(trace.Message, traceArgs(trace.Args));
            }
            result.contentMapperDiagnostics = loader.ContentMapperDiagnostics();
        }
        result.finishedProcessing = true;
        for (int i = 0; i < result.resolutionEdges.Count; i++)
        {
            ProjectResolutionEdge edge = result.resolutionEdges[i];
            if (edge.Resolution.IsResolved() && result.filesByPath.TryGetValue(loader.KeyValue(edge.Resolution.ResolvedPath), out SourceFile? target))
                result.resolutionEdges[i] = edge with { TargetFile = target };
        }
        foreach (var edge in result.graphEdges)
        {
            if (edge.Resolution.IsResolved() && result.filesByPath.TryGetValue(loader.KeyValue(edge.Resolution.ResolvedPath), out var target))
            {
                edge.TargetFile = target;
                if (edge.Scheduling == DependencyScheduling.ElidedJavaScriptDepth) edge.Scheduling = DependencyScheduling.Scheduled;
            }
        }
        foreach (var edge in result.pathReferences)
            if (edge.ResolvedPath != default && result.filesByPath.TryGetValue(loader.KeyValue(edge.ResolvedPath), out var target)) edge.TargetFile = target;
        foreach (var edge in result.typeDirectives)
            if (edge.Resolution?.IsResolved() == true && result.filesByPath.TryGetValue(loader.KeyValue(edge.Resolution.ResolvedPath), out var target)) edge.TargetFile = target;
        loader.includeProcessor.SetProcessedFiles(result, loader.options);
        if (!loader.program) result.processingDiagnostics = result.processingDiagnostics.Append(loader.includeProcessor.GetDiagnostics());
        foreach (var edge in result.pathReferences)
            if (edge.ProcessingDiagnostic is not null) edge.Diagnostic = edge.ProcessingDiagnostic.ToDiagnostic(loader.includeProcessor);
        return result;
    }
}

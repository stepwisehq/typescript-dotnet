// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial port of tsc/internal/compiler/fileloader.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Fixed TS roots, no project
// references/content mapper/tracing/automatic types or default library loading.
using TypeScript.Checking;
using TypeScript.Modules;
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal sealed class FileLoader
{
    internal readonly List<ParseTask> rootTasks = [];
    internal readonly ProjectDiskHost host;
    internal readonly DefaultResolver resolver;
    internal readonly CompilerOptions options;
    private readonly IReadOnlyDictionary<string, SourceFile> embeddedFiles;
    internal readonly FilesParser filesParser = new();
    internal readonly IncludeProcessor includeProcessor;
    internal FileLoader(ProjectDiskHost host, DefaultResolver resolver, CompilerOptions options, IReadOnlyList<SourceFile> libraries, ProjectConfiguration configuration)
    {
        this.host = host;
        this.resolver = resolver;
        this.options = options;
        includeProcessor = new(configuration);
        embeddedFiles = libraries.ToDictionary(file => file.PathKey(), StringComparer.Ordinal);
    }

    // fileloader.go:175. Explicit libraries are root files, not libFile/default
    // tasks; noLib=true and types=[] suppress both automatic dependency families.
    internal static ProcessedFiles ProcessAllProgramFiles(ProjectDiskHost host, DefaultResolver resolver, CompilerOptions options, IReadOnlyList<SourceFile> libraries, IReadOnlyList<string> roots, ProjectConfiguration configuration)
    {
        var loader = new FileLoader(host, resolver, options, libraries, configuration);
        foreach (SourceFile file in libraries)
            loader.AddRootFileTask(file.FileName(), file.FileName(), new());
        for (int i = 0; i < roots.Count; i++)
            loader.AddRootFileTask(roots[i], roots[i], new() { rootIndex = configuration.Files.ToList().FindIndex(spec => host.Resolve(configuration.Root, spec) == roots[i]) });
        loader.filesParser.Parse(loader, loader.rootTasks);
        return loader.filesParser.GetProcessedFiles(loader);
    }

    // fileloader.go:253/682. The admitted root has an explicit supported .ts
    // extension; unselected extension-search/allowNonTs branches are rejected.
    internal void AddRootFileTask(string fileName, string referenceText, FileIncludeReason reason)
    {
        var(file, diagnostic) = GetSourceFileFromReference(fileName, referenceText);
        rootTasks.Add(new ParseTask { normalizedFilePath = fileName, path = fileName, file = file, failedLookup = diagnostic is not null, includeReason = reason, processingDiagnostics = diagnostic is null ? null : [new ProcessingDiagnostic { diagnosticReason = reason, message = diagnostic.message, args = [referenceText] }] });
    }

    private (SourceFile? File, Diagnostic? Diagnostic) GetSourceFileFromReference(string fileName, string referenceText)
    {
        if (embeddedFiles.TryGetValue(fileName, out SourceFile? library))
            return (library, null);
        Project.RequireImplementationFile(fileName);
        if (!host.FileExists(fileName))
            return (null, Ast.NewCompilerDiagnostic(Diagnostics.File_0_not_found, GoString.ToUtf8(referenceText)));
        return (null, null);
    }

    // fileloader.go:410. Fixed parsed kind/external-module indicator options are
    // the existing parser defaults for TS+ESNext+ModuleDetectionAuto.
    internal SourceFile? ParseSourceFile(ParseTask task)
    {
        if (!host.FileExists(task.normalizedFilePath))
            return null;
        return Parser.ParseSourceFile(new SourceFileParseOptions(task.normalizedFilePath, task.path), host.Read(task.normalizedFilePath), ScriptKind.TS);
    }

    // fileloader.go:821. Source import order and mode-aware entries are retained;
    // excluded helper/JSX/module-augmentation/project-redirect producers cannot
    // enter this fixed host. Resolution occurs before scheduling child parsing.
    internal void ResolveImportsAndModuleAugmentations(ParseTask task)
    {
        SourceFile file = task.file!;
        if (file.Imports()is not { Count: > 0 } imports)
            return;
        task.resolutionsInFile = new();
        for (int index = 0; index < imports.Count; index++)
        {
            Node entry = imports[index];
            string name = ProjectDiskHost.DecodeModuleReference(entry.Text());
            if (name == "")
                continue;
            ResolutionMode mode = ResolutionMode.ESNext;
            ResolvedModule resolved = resolver.ResolveModuleName(name, file.FileName(), mode);
            task.resolutionsInFile[new(name, mode)] = resolved;
            task.resolutionEdges ??= [];
            task.resolutionEdges.Add(new(file, entry, name, mode, resolved, null));
            if (!resolved.IsResolved())
                continue;
            // Exact shouldAddFile selected conditions: noResolve=false,
            // admitted TS import (not JSDoc), no resolution diagnostic.
            if (resolved.ResolutionDiagnostics is { Count: > 0 } || options.NoResolve == Tristate.True || (entry.Flags & NodeFlags.JSDoc) != 0)
                continue;
            task.AddSubTask(resolved.ResolvedFileName, resolved.ResolvedPath, new() { importingFile = file, specifier = entry });
        }
    }

    internal void ValidateParsedSource(SourceFile file)
    {
        if (embeddedFiles.ContainsKey(file.PathKey()))
            return;
        if (file.Diagnostics()is { Count: > 0 })
            return; // no malformed semantic entry
        ProjectAdmission.Validate(file);
    }
}

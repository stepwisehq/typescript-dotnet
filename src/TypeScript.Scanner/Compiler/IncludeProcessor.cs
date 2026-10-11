// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Partial port of tsc/internal/compiler/includeprocessor.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Selected inclusion diagnostics and actual package redirect/implied-format explanations.
using TypeScript.Scanner;
using TypeScript.Modules;
using TypeScript.Collections;

namespace TypeScript.Compiler;
internal sealed class FileIncludeData
{
    internal Dictionary<string, Slice<FileIncludeReason>> fileIncludeReasons = new(StringComparer.Ordinal);
    internal Slice<ProcessingDiagnostic> processingDiagnostics;
}
internal sealed class IncludeProcessor
{
    private ProjectConfiguration? configurationValue;
    internal ProjectConfiguration configuration => configurationValue ??= new(program!.CommandLine());
    internal readonly SyncMap<(FileIncludeReason Reason, bool RelativeFileName, string RelativeTo), Diagnostic> reasonDiagnostics = new();
    internal readonly SyncMap<FileIncludeReason, ReferenceFileLocation> reasonToReferenceLocation = new();
    internal readonly SyncMap<FileIncludeReason, Diagnostic?> includeReasonToRelatedInfo = new();
    private readonly object compilerOptionsSyntaxGate = new();
    private bool compilerOptionsSyntaxInitialized;
    private ObjectLiteralExpression? compilerOptionsSyntax;
    internal ObjectLiteralExpression? GetCompilerOptionsObjectLiteralSyntax()
    {
        lock (compilerOptionsSyntaxGate)
        {
            if (!compilerOptionsSyntaxInitialized)
            {
                try
                {
                    var file = program?.CommandLine().ConfigFile?.SourceFile;
                    if (file is not null)
                        compilerOptionsSyntax = TypeScript.TsOptions.TsOptions.ForEachTsConfigPropArray(file, "compilerOptions", property => Ast.IsObjectLiteralExpression(property.Initializer) ? property.Initializer.AsObjectLiteralExpression() : null);
                }
                finally { compilerOptionsSyntaxInitialized = true; }
            }
            return compilerOptionsSyntax;
        }
    }
    internal ReferenceFileLocation GetReferenceLocation(FileIncludeReason reason)
    {
        var (location, found) = reasonToReferenceLocation.Load(reason);
        return found ? location : reasonToReferenceLocation.LoadOrStore(reason, program is not null ? reason.GetReferencedLocation(program) : reason.LegacyReferencedLocation()).Actual;
    }
    internal FileIncludeData data = new();
    internal Dictionary<string, Slice<FileIncludeReason>> fileIncludeReasons => data.fileIncludeReasons;
    internal Slice<ProcessingDiagnostic> processingDiagnostics { get => data.processingDiagnostics; set => data.processingDiagnostics = value; }
    internal Program? program;
    private readonly object diagnosticsGate = new();
    private DiagnosticsCollection? computedDiagnostics;
    internal IncludeProcessor(Program program)
    {
        this.program = program;
        pathValuesAreRaw = true;
        data = program.processedFiles.fileIncludeData;
        processed = program.processedFiles;
    }
    internal DiagnosticsCollection GetDiagnostics(Program program)
    {
        lock (diagnosticsGate) return computedDiagnostics ??= ComputeDiagnostics();
    }
    internal readonly bool pathValuesAreRaw;
    private ProcessedFiles? processed;
    private CompilerOptions? options;
    private readonly SyncMap<string, Slice<Diagnostic>> redirectAndFileFormat = new();
    internal IncludeProcessor(ProjectConfiguration configuration, bool pathValuesAreRaw = false) => (configurationValue, this.pathValuesAreRaw) = (configuration, pathValuesAreRaw);
    internal void AddIncludeReason(ParseTask task, FileIncludeReason? reason)
    {
        if (task.redirectedParseTask is not null) { AddIncludeReason(task.redirectedParseTask, reason); return; }
        if (!task.loaded)
            return;
        fileIncludeReasons.TryGetValue(task.path, out var values);
        // Preserve the source pointer itself; automatic-type tasks do not enter this owner.
        fileIncludeReasons[task.path] = values.Append(reason!);
    }

    // includeprocessor.go:65. Preserve message direction, based on actual prior
    // referenced-file reasons rather than substitute a single casing diagnostic.
    internal void AddProcessingDiagnosticsForFileCasing(string file, string existing, string current, FileIncludeReason? reason)
    {
        // fileInclude.go:85 allows a nil reason and reports it as non-referenced.
        bool already = reason?.IsReferencedFile() != true && fileIncludeReasons.TryGetValue(file, out var previous) && Core.Some(previous, static r => r?.IsReferencedFile() == true);
        processingDiagnostics = processingDiagnostics.Append(new ProcessingDiagnostic() { file = file, diagnosticReason = reason, message = already ? Diagnostics.Already_included_file_name_0_differs_from_file_name_1_only_in_casing : Diagnostics.File_name_0_differs_from_already_included_file_name_1_only_in_casing, args = already ? [existing, current] : [current, existing] });
    }

    internal void SetProcessedFiles(ProcessedFiles files, CompilerOptions compilerOptions) => (processed, options) = (files, compilerOptions);

    // includeprocessor.go:129. Project-reference execution is external; actual
    // package dedup redirects and source metadata are already-produced state.
    internal Slice<Diagnostic> ExplainRedirectAndImpliedFormat(Program program, PathKey path, Func<RootedFilePath, string> toFileName) => ExplainRedirectAndImpliedFormat(path.AsString(), toFileName);
    internal Slice<Diagnostic> ExplainRedirectAndImpliedFormat(string path) => ExplainRedirectAndImpliedFormat(path, file => file.AsString());
    private Slice<Diagnostic> ExplainRedirectAndImpliedFormat(string path, Func<RootedFilePath, string> toFileName)
    {
        var (cached, found) = redirectAndFileFormat.Load(path);
        if (found) return cached;
        if (processed is null) return default;
        options ??= program?.Options();
        if (options is null) return default;
        Slice<Diagnostic> result = default;
        SourceFile? source = null;
        if (processed.packageRedirects.TryGetValue(path, out var target))
            result = result.Append(Ast.NewCompilerDiagnostic(Diagnostics.File_redirects_to_file_0, toFileName(RootedFilePath.FromGoString(target.FileNameGoBytes()))));
        else
            processed.filesByPath.TryGetValue(path, out source);
        RootedFilePath presentationName = source is not null ? RootedFilePath.FromGoString(source.FileNameGoBytes()) : processed.redirectFilesByPath?.GetValueOrDefault(path)?.fileName ?? default;
        if (program is not null && presentationName != default && processed.outputFileToProjectReferenceSource.TryGetValue(path, out var referenceSource) && referenceSource != presentationName)
            result = result.Insert(0, Ast.NewCompilerDiagnostic(Diagnostics.File_is_output_of_project_reference_source_0, toFileName(referenceSource)));
        if (source is not null && Ast.IsExternalOrCommonJSModule(source))
        {
            var metadata = processed.metadata[source.PathKey()];
            var fileName = RootedFilePath.FromGoString(source.FileNameGoBytes());
            switch (program is not null ? program.GetImpliedNodeFormatForEmit(source) : Ast.GetImpliedNodeFormatForEmitWorker(fileName, options.GetEmitModuleKind(), metadata))
            {
                case ResolutionMode.ESNext:
                    if (metadata.PackageJsonType == "module")
                        result = result.Append(Ast.NewCompilerDiagnostic(Diagnostics.File_is_ECMAScript_module_because_0_has_field_type_with_value_module, toFileName(metadata.PackageJsonDirectory.ResolveFile("package.json"))));
                    break;
                case ResolutionMode.CommonJS:
                    if (metadata.PackageJsonType != "")
                        result = result.Append(Ast.NewCompilerDiagnostic(Diagnostics.File_is_CommonJS_module_because_0_has_field_type_whose_value_is_not_module, toFileName(metadata.PackageJsonDirectory.ResolveFile("package.json"))));
                    else if (metadata.PackageJsonDirectory != default)
                        result = result.Append(Ast.NewCompilerDiagnostic(Diagnostics.File_is_CommonJS_module_because_0_does_not_have_field_type, toFileName(metadata.PackageJsonDirectory.ResolveFile("package.json"))));
                    else
                        result = result.Append(Ast.NewCompilerDiagnostic(Diagnostics.File_is_CommonJS_module_because_package_json_was_not_found));
                    break;
            }
        }
        return redirectAndFileFormat.LoadOrStore(path, result).Actual;
    }

    // includeprocessor.go:44. The source publishes only the final mode/name
    // association per retained file, not every earlier resolver call result.
    internal Slice<Diagnostic> GetDiagnostics() => ComputeDiagnostics().GetDiagnostics();
    private DiagnosticsCollection ComputeDiagnostics()
    {
        var diagnostics = new DiagnosticsCollection();
        foreach (var row in processingDiagnostics)
            diagnostics.Add(row.ToDiagnostic(this));
        if (processed is not null)
        {
            if (program is not null)
            {
                foreach (var resolutions in processed.resolvedModules.Values)
                    foreach (var result in (IEnumerable<ResolvedModule?>?)resolutions?.Values ?? [])
                        foreach (var diagnostic in result?.ResolutionDiagnostics ?? default) diagnostics.Add(diagnostic);
                foreach (var resolutions in processed.typeResolutionsInFile.Values)
                    foreach (var result in (IEnumerable<ResolvedTypeReferenceDirective?>?)resolutions?.Values ?? [])
                        foreach (var diagnostic in result?.ResolutionDiagnostics ?? default) diagnostics.Add(diagnostic);
            }
            else
            {
                var modules = new Dictionary<(SourceFile File, ModeAwareCacheKey Name), TypeScript.Modules.ResolvedModule>();
                foreach (var edge in processed.graphEdges) modules[(edge.ImportingFile, new ModeAwareCacheKey(edge.ModuleReferenceValue, edge.Mode))] = edge.Resolution;
                foreach (var result in modules.Values)
                    foreach (var diagnostic in result.ResolutionDiagnostics) diagnostics.Add(diagnostic);
                var types = new Dictionary<(SourceFile File, ModeAwareCacheKey Name), TypeScript.Modules.ResolvedTypeReferenceDirective>();
                foreach (var edge in processed.typeDirectives)
                    if (edge.Resolution is not null) types[(edge.ImportingFile, new ModeAwareCacheKey(edge.Reference.FileNameValue, edge.Mode))] = edge.Resolution;
                foreach (var result in types.Values)
                    foreach (var diagnostic in result.ResolutionDiagnostics) diagnostics.Add(diagnostic);
            }
        }
        return diagnostics;
    }
}

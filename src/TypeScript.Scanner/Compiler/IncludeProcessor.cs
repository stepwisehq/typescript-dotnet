// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Partial port of tsc/internal/compiler/includeprocessor.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Selected inclusion diagnostics and actual package redirect/implied-format explanations.
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal sealed class IncludeProcessor
{
    internal readonly ProjectConfiguration configuration;
    internal readonly Dictionary<(FileIncludeReason Reason, bool RelativeFileName, string RelativeTo), Diagnostic> reasonDiagnostics = new();
    internal readonly Dictionary<string, List<FileIncludeReason>> fileIncludeReasons = new(StringComparer.Ordinal);
    internal readonly List<ProcessingDiagnostic> processingDiagnostics = [];
    internal readonly bool pathValuesAreRaw;
    private ProcessedFiles? processed;
    private CompilerOptions? options;
    private readonly Dictionary<string, List<Diagnostic>?> redirectAndFileFormat = new(StringComparer.Ordinal);
    internal IncludeProcessor(ProjectConfiguration configuration, bool pathValuesAreRaw = false) => (this.configuration, this.pathValuesAreRaw) = (configuration, pathValuesAreRaw);
    internal void AddIncludeReason(ParseTask task, FileIncludeReason reason)
    {
        if (!task.loaded)
            return;
        if (!fileIncludeReasons.TryGetValue(task.path, out List<FileIncludeReason>? values))
            fileIncludeReasons.Add(task.path, values = []);
        values.Add(reason);
    }

    // includeprocessor.go:65. Preserve message direction, based on actual prior
    // referenced-file reasons rather than substitute a single casing diagnostic.
    internal void AddProcessingDiagnosticsForFileCasing(string file, string existing, string current, FileIncludeReason reason)
    {
        bool already = !reason.IsReferencedFile() && fileIncludeReasons.TryGetValue(file, out List<FileIncludeReason>? previous) && previous.Any(r => r.IsReferencedFile());
        processingDiagnostics.Add(new() { file = file, diagnosticReason = reason, message = already ? Diagnostics.Already_included_file_name_0_differs_from_file_name_1_only_in_casing : Diagnostics.File_name_0_differs_from_already_included_file_name_1_only_in_casing, args = already ? [existing, current] : [current, existing] });
    }

    internal void SetProcessedFiles(ProcessedFiles files, CompilerOptions compilerOptions) => (processed, options) = (files, compilerOptions);

    // includeprocessor.go:129. Project-reference execution is external; actual
    // package dedup redirects and source metadata are already-produced state.
    internal List<Diagnostic>? ExplainRedirectAndImpliedFormat(string path)
    {
        if (redirectAndFileFormat.TryGetValue(path, out var cached))
            return cached;
        if (processed is null || options is null)
            return null;
        List<Diagnostic>? result = null;
        SourceFile? source = null;
        if (processed.packageRedirects.TryGetValue(path, out var target))
            (result ??= []).Add(Ast.NewCompilerDiagnostic(Diagnostics.File_redirects_to_file_0, target.FileNameGoBytes()));
        else
            processed.filesByPath.TryGetValue(path, out source);
        if (source is not null && Ast.IsExternalOrCommonJSModule(source))
        {
            var metadata = processed.metadata[source.PathKey()];
            var fileName = RootedFilePath.FromGoString(source.FileNameGoBytes());
            switch (Ast.GetImpliedNodeFormatForEmitWorker(fileName, options.GetEmitModuleKind(), metadata))
            {
                case ResolutionMode.ESNext:
                    if (metadata.PackageJsonType == "module")
                        (result ??= []).Add(Ast.NewCompilerDiagnostic(Diagnostics.File_is_ECMAScript_module_because_0_has_field_type_with_value_module, metadata.PackageJsonDirectory.ResolveFile("package.json").AsString()));
                    break;
                case ResolutionMode.CommonJS:
                    if (metadata.PackageJsonType != "")
                        (result ??= []).Add(Ast.NewCompilerDiagnostic(Diagnostics.File_is_CommonJS_module_because_0_has_field_type_whose_value_is_not_module, metadata.PackageJsonDirectory.ResolveFile("package.json").AsString()));
                    else if (metadata.PackageJsonDirectory != default)
                        (result ??= []).Add(Ast.NewCompilerDiagnostic(Diagnostics.File_is_CommonJS_module_because_0_does_not_have_field_type, metadata.PackageJsonDirectory.ResolveFile("package.json").AsString()));
                    else
                        (result ??= []).Add(Ast.NewCompilerDiagnostic(Diagnostics.File_is_CommonJS_module_because_package_json_was_not_found));
                    break;
            }
        }
        redirectAndFileFormat.Add(path, result);
        return result;
    }

    // includeprocessor.go:44. The source publishes only the final mode/name
    // association per retained file, not every earlier resolver call result.
    internal List<Diagnostic> GetDiagnostics()
    {
        var diagnostics = new DiagnosticsCollection();
        foreach (var row in processingDiagnostics)
            diagnostics.Add(row.ToDiagnostic(this));
        if (processed is not null)
        {
            var modules = new Dictionary<(SourceFile File, string Name, ResolutionMode Mode), TypeScript.Modules.ResolvedModule>();
            foreach (var edge in processed.graphEdges)
                modules[(edge.ImportingFile, edge.ModuleReference, edge.Mode)] = edge.Resolution;
            foreach (var result in modules.Values)
                foreach (var diagnostic in result.ResolutionDiagnostics ?? [])
                    diagnostics.Add(diagnostic);
            var types = new Dictionary<(SourceFile File, string Name, ResolutionMode Mode), TypeScript.Modules.ResolvedTypeReferenceDirective>();
            foreach (var edge in processed.typeDirectives)
                if (edge.Resolution is not null)
                    types[(edge.ImportingFile, edge.Reference.FileName, edge.Mode)] = edge.Resolution;
            foreach (var result in types.Values)
                foreach (var diagnostic in result.ResolutionDiagnostics ?? [])
                    diagnostics.Add(diagnostic);
        }
        return diagnostics.GetDiagnostics();
    }
}

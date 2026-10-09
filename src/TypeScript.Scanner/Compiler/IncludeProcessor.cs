// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Partial port of tsc/internal/compiler/includeprocessor.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Fixed no-package/no-redirect host: no redirect/implied-format explanation is produced.
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal sealed class IncludeProcessor
{
    internal readonly ProjectConfiguration configuration;
    internal readonly Dictionary<(FileIncludeReason Reason, bool RelativeFileName, string RelativeTo), Diagnostic> reasonDiagnostics = new();
    internal readonly Dictionary<string, List<FileIncludeReason>> fileIncludeReasons = new(StringComparer.Ordinal);
    internal readonly List<ProcessingDiagnostic> processingDiagnostics = [];
    internal IncludeProcessor(ProjectConfiguration configuration) => this.configuration = configuration;
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

    internal List<Diagnostic> GetDiagnostics() => processingDiagnostics.Select(d => d.CreateDiagnosticExplainingFile(this)).ToList();
}

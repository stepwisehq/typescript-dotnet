// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of the nonincremental invocation in internal/execute/tsc/emit.go.
// Project-facing result and retained listing are authored public boundary adapters.
using System.Text;
using TypeScript.Scanner;

namespace TypeScript.Compiler;
public sealed class ProjectCompilationResult
{
    public required IReadOnlyList<Diagnostic> Diagnostics { get; init; }
    public EmitResult? EmitResult { get; init; }
    public int ExitCode { get; init; }
    public string Listing { get; init; } = "";
}

public sealed partial class Project
{
    // execute/tsc/emit.go:81: EmitFilesAndReportErrors; emit.go:46 final status.
    // The legacy tscheck.json snapshot remains a check-only compatibility API.
    public ProjectCompilationResult Compile(CancellationToken context = default, EmitOptions emitOptions = default)
    {
        var program = genuineProgram ?? throw MissingProgramService("compilation and emission");
        var diagnostics = Program.GetDiagnosticsOfAnyProgram(context, program, default, false, program.GetBindDiagnostics, program.GetSemanticDiagnostics);
        // execute/tsc.go selects the snapshot-backed incremental owner for either
        // Incremental or Composite. Ordinary Program.Emit cannot write its buildinfo.
        // Preserve genuine option/configuration diagnostics before this host boundary.
        if (program.Options().IsIncremental())
        {
            if (program.GetConfigFileParsingDiagnostics().Count != 0 || program.GetProgramDiagnostics().Count != 0)
                return new ProjectCompilationResult
                {
                    Diagnostics = Program.SortAndDeduplicateDiagnostics(diagnostics).AsSpan().ToArray(),
                    EmitResult = new EmitResult
                    {
                        EmitSkipped = true
                    },
                    ExitCode = 1
                };
            throw new ProjectException("incremental compilation", "Incremental or composite compilation requires the upstream incremental snapshot/build-info owner, which is outside the supported command execution scope.");
        }

        EmitResult? emitted = new()
        {
            EmitSkipped = true,
            Diagnostics = Slice<Diagnostic>.Make(0)
        };
        if (program.Options().ListFilesOnly != Tristate.True)
            emitted = program.Emit(context, emitOptions);
        if (emitted is not null)
            diagnostics = diagnostics.Append(emitted.Diagnostics);
        diagnostics = Program.SortAndDeduplicateDiagnostics(diagnostics);
        // execute/tsc/emit.go:144: listFiles. Display text is decoded only at this
        // public boundary; compiler filenames and emitted contents stay byte-based.
        using var listing = new MemoryStream();
        if (program.Options().ListEmittedFiles == Tristate.True && emitted is not null)
            foreach (var file in emitted.EmittedFiles)
                WriteListingLine(listing, "TSFILE: " + file.AsString());
        if (program.Options().ExplainFiles == Tristate.True)
            program.ExplainFiles(listing, default, program.GetCurrentDirectory());
        else if (program.Options().ListFiles == Tristate.True || program.Options().ListFilesOnly == Tristate.True)
            foreach (var file in program.SourceFiles())
                WriteListingLine(listing, file.FileNameGoBytes());
        int exitCode = diagnostics.Count == 0 ? 0 : emitted?.EmitSkipped == true ? 1 : 2;
        return new ProjectCompilationResult
        {
            Diagnostics = diagnostics.AsSpan().ToArray(),
            EmitResult = emitted,
            ExitCode = exitCode,
            Listing = Encoding.UTF8.GetString(listing.GetBuffer().AsSpan(0, checked((int)listing.Length)))
        };
    }

    private static void WriteListingLine(Stream writer, string rawGoBytes)
    {
        writer.Write(GoString.Bytes(rawGoBytes));
        writer.WriteByte((byte)'\n');
    }
}

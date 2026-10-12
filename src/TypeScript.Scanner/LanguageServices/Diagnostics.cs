// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ls/diagnostics.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Collections;
using CompilerProgram = TypeScript.Compiler.Program;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using TypeScript.SpanMap;
using AstDiagnostic = TypeScript.Scanner.Diagnostic;
using LspDiagnostic = TypeScript.Lsp.Protocol.Diagnostic;
using DiagnosticConverters = TypeScript.LanguageServices.Lsconv.Lsconv;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    // diagnostics.go:20. Required owned primary-file slice is appended with the
    // retained supplemental header, preserving exact file/channel order.
    private static Slice<AstDiagnostic> getAllDiagnostics(ProtocolRequestContext context, CompilerProgram program, SourceFile file)
    {
        Slice<AstDiagnostic> diagnostics = default;
        var files = Slice<SourceFile>.FromArray([file]).Append(file.SupplementalSourceFiles());
        foreach (var source in files)
        {
            diagnostics = diagnostics.Append(program.GetSyntacticDiagnostics(context.Cancellation, source));
            diagnostics = diagnostics.Append(program.GetSemanticDiagnostics(context, source));
            diagnostics = diagnostics.Append(program.GetSuggestionDiagnostics(context, source));
            if (program.Options().GetEmitDeclarations())
                diagnostics = diagnostics.Append(program.GetDeclarationDiagnostics(context, source));
        }

        return diagnostics;
    }

    // diagnostics.go:34. No validation still returns an allocated empty report.
    internal (RelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport Result, Exception? Error) ProvideDiagnostics(ProtocolRequestContext context, DocumentUri uri)
    {
        var(program, file) = getProgramAndFile(uri);
        var items = UserPreferences().ValidateEnabled == Tristate.False ? Slice<LspDiagnostic?>.Make(0) : toLSPDiagnostics(context, getAllDiagnostics(context, program, file));
        return (new RelatedFullDocumentDiagnosticReportOrUnchangedDocumentDiagnosticReport { FullDocumentDiagnosticReport = new RelatedFullDocumentDiagnosticReport { Items = items } }, null);
    }

    // diagnostics.go:57. Go's zero OrderedMap allocates on first Set; the CLR owner
    // likewise stays absent until the first synthesized diagnostic is retained.
    private Slice<LspDiagnostic?> toLSPDiagnostics(ProtocolRequestContext context, params Slice<AstDiagnostic>[] slices)
    {
        bool styleWarnings = UserPreferences().ReportStyleChecksAsWarnings == Tristate.True;
        int size = 0;
        foreach (var slice in slices)
            size += slice.Count;
        var result = Slice<LspDiagnostic?>.Make(0, size);
        OrderedMap<SourceFile, Slice<AstDiagnostic>>? synthesizedByFile = null;
        foreach (var slice in slices)
            foreach (var diagnostic in slice)
            {
                if (isSynthesizedContentMappedDiagnostic(diagnostic))
                {
                    synthesizedByFile ??= new();
                    var file = diagnostic.File()!;
                    synthesizedByFile.Set(file, synthesizedByFile.GetOrZero(file).Append(diagnostic));
                    continue;
                }

                result = result.Append(DiagnosticConverters.DiagnosticToLSPPull(context, converters, diagnostic, styleWarnings));
            }

        if (synthesizedByFile is not null)
            foreach (var(file, diagnostics)in synthesizedByFile.EntryValues())
                result = result.Append(DiagnosticConverters.DiagnosticToLSPPull(context, converters, aggregateSynthesizedDiagnostics(file, diagnostics), styleWarnings));
        return result;
    }

    // diagnostics.go:98. Foreign diagnostic sources keep their original locations.
    private static bool isSynthesizedContentMappedDiagnostic(AstDiagnostic diagnostic)
    {
        var file = diagnostic.File();
        if (file is null || file.SpanMap()is null || diagnostic.Source() != "")
            return false;
        return file.SpanMap().VirtualToOriginalSpan(diagnostic.Loc()).Fidelity == Fidelity.None;
    }

    // diagnostics.go:112. The related slice retains the exact original diagnostic owners.
    private static AstDiagnostic aggregateSynthesizedDiagnostics(SourceFile file, Slice<AstDiagnostic> diagnostics)
    {
        var aggregate = Ast.NewDiagnostic(file, new TextRange(0, 0), TypeScript.Scanner.Diagnostics.Virtual_code_produced_by_the_content_mapper_0_has_problems_with_no_corresponding_location_in_this_file, file.ContentMapper());
        aggregate.SetRelatedInfo(diagnostics);
        aggregate.SetCategory(worstCategory(diagnostics));
        return aggregate;
    }

    // diagnostics.go:124. Error wins immediately; warning replaces lesser categories.
    private static DiagnosticCategory worstCategory(Slice<AstDiagnostic> diagnostics)
    {
        var worst = diagnostics[0].Category();
        foreach (var diagnostic in diagnostics)
            switch (diagnostic.Category())
            {
                case DiagnosticCategory.Error:
                    return DiagnosticCategory.Error;
                case DiagnosticCategory.Warning:
                    worst = DiagnosticCategory.Warning;
                    break;
            }

        return worst;
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Partial port of tsc/internal/compiler/processingDiagnostic.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal enum ProcessingDiagnosticKind { UnknownReference, ExplainingFileInclude }
internal sealed class ProcessingDiagnostic
{
    internal ProcessingDiagnosticKind kind = ProcessingDiagnosticKind.ExplainingFileInclude;
    internal string file = "";
    internal FileIncludeReason diagnosticReason = null!;
    internal Message message = null!;
    internal string[] args = [];
    // processingDiagnostic.go:43. Explicit type-reference failure owns its
    // direct source location; it is not an inclusion-explanation chain.
    internal Diagnostic ToDiagnostic(IncludeProcessor processor)
    {
        if (kind == ProcessingDiagnosticKind.UnknownReference)
        {
            if (diagnosticReason.kind != IncludeReferenceKind.Type)
                throw new InvalidOperationException("Unknown reference kind outside the selected graph environment.");
            return diagnosticReason.DiagnosticAt(Diagnostics.Cannot_find_type_definition_file_for_0, diagnosticReason.reference!.FileName);
        }
        return CreateDiagnosticExplainingFile(processor);
    }
    internal Diagnostic CreateDiagnosticExplainingFile(IncludeProcessor processor)
    {
        List<Diagnostic>? includeDetails = null, relatedInfo = null;
        FileIncludeReason? preferred = diagnosticReason.IsReferencedFile() && diagnosticReason.syntheticKind == "" ? diagnosticReason : null;
        var seen = new HashSet<FileIncludeReason>(ReferenceEqualityComparer.Instance);
        void ProcessInclude(FileIncludeReason reason)
        {
            if (!seen.Add(reason))
                return;
            (includeDetails ??= []).Add(reason.ToDiagnostic(processor));
            if (preferred is null && reason.IsReferencedFile() && reason.syntheticKind == "")
                preferred = reason;
            else if (!ReferenceEquals(preferred, reason) && reason.ToRelatedInfo(processor)is Diagnostic info)
                (relatedInfo ??= []).Add(info);
        }

        if (file != "" && processor.fileIncludeReasons.TryGetValue(file, out List<FileIncludeReason>? reasons))
            foreach (FileIncludeReason reason in reasons)
                ProcessInclude(reason);
        ProcessInclude(diagnosticReason);
        List<Diagnostic>? chain = null;
        if (includeDetails is not null && (preferred is null || seen.Count != 1))
            chain = [Ast.NewCompilerDiagnostic(Diagnostics.The_file_is_in_the_program_because_Colon).SetMessageChain(includeDetails)];
        if (file != "" && processor.ExplainRedirectAndImpliedFormat(file) is { } redirectInfo)
            (chain ??= []).AddRange(redirectInfo);
        object[] values = args.Select(value => (object)(processor.pathValuesAreRaw ? value : GoString.ToUtf8(value))).ToArray();
        Diagnostic result = preferred is null ? Ast.NewCompilerDiagnostic(message, values) : preferred.DiagnosticAt(message, values);
        if (chain is not null)
            result.SetMessageChain(chain);
        if (relatedInfo is not null)
            result.SetRelatedInfo(relatedInfo);
        return result;
    }
}

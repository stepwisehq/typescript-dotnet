// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Partial port of tsc/internal/compiler/processingDiagnostic.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal enum ProcessingDiagnosticKind { UnknownReference, ExplainingFileInclude }
internal sealed class ProcessingDiagnostic
{
    internal ProcessingDiagnosticKind kind = ProcessingDiagnosticKind.ExplainingFileInclude;
    internal ByteStringKey file;
    internal FileIncludeReason? diagnosticReason;
    internal Message message = null!;
    internal string[] args = [];
    // processingDiagnostic.go:43. Explicit type-reference failure owns its
    // direct source location; it is not an inclusion-explanation chain.
    internal Diagnostic ToDiagnostic(IncludeProcessor processor)
    {
        if (kind == ProcessingDiagnosticKind.UnknownReference)
        {
            // A true unknown-reference diagnostic requires its source reason pointer.
            var reason = diagnosticReason!;
            if (reason.kind == IncludeReferenceKind.Type)
                return reason.DiagnosticAt(processor, Diagnostics.Cannot_find_type_definition_file_for_0, reason.reference!.FileName);
            if (reason.kind == IncludeReferenceKind.Lib)
            {
                string name = CaseSensitivity.CaseInsensitive.Canonicalize(reason.reference!.FileName);
                string unqualified = name.StartsWith("lib.", StringComparison.Ordinal) ? name[4..] : name;
                if (unqualified.EndsWith(".d.ts", StringComparison.Ordinal)) unqualified = unqualified[..^5];
                string suggestion = Core.GetSpellingSuggestionForStrings(unqualified, TypeScript.TsOptions.TsOptions.Libs);
                return reason.DiagnosticAt(processor, suggestion != "" ? Diagnostics.Cannot_find_lib_definition_for_0_Did_you_mean_1 : Diagnostics.Cannot_find_lib_definition_for_0, name, suggestion);
            }
            throw new InvalidOperationException("unknown include kind");
        }
        return CreateDiagnosticExplainingFile(processor);
    }
    internal Diagnostic CreateDiagnosticExplainingFile(IncludeProcessor processor)
    {
        Slice<Diagnostic> includeDetails = default, relatedInfo = default;
        FileIncludeReason? preferred = diagnosticReason is not null && diagnosticReason.IsReferencedFile() && diagnosticReason.syntheticKind == "" ? diagnosticReason : null;
        var seen = new HashSet<FileIncludeReason>(ReferenceEqualityComparer.Instance);
        void ProcessInclude(FileIncludeReason reason)
        {
            if (!seen.Add(reason))
                return;
            includeDetails = includeDetails.Append(reason.ToDiagnostic(processor));
            if (preferred is null && reason.IsReferencedFile() && reason.syntheticKind == "")
                preferred = reason;
            else if (!ReferenceEquals(preferred, reason) && reason.ToRelatedInfo(processor)is Diagnostic info)
                relatedInfo = relatedInfo.Append(info);
        }

        if (file.Value.Length != 0)
        {
            includeDetails = Slice<Diagnostic>.Make(0);
            if (processor.fileIncludeReasons.TryGetValue(file, out Slice<FileIncludeReason> reasons))
                foreach (FileIncludeReason reason in reasons)
                    ProcessInclude(reason);
        }
        if (diagnosticReason is not null) ProcessInclude(diagnosticReason);
        Slice<Diagnostic> chain = default;
        if (!includeDetails.IsNil && (preferred is null || seen.Count != 1))
            chain = chain.Append(Ast.NewCompilerDiagnostic(Diagnostics.The_file_is_in_the_program_because_Colon).SetMessageChain(includeDetails));
        if (file.Value.Length != 0 && processor.ExplainRedirectAndImpliedFormat(file) is { } redirectInfo)
            chain = chain.Append(redirectInfo);
        object[] values = args.Select(value => (object)(processor.pathValuesAreRaw ? value : GoString.ToUtf8(value))).ToArray();
        Diagnostic result = preferred is null ? Ast.NewCompilerDiagnostic(message, values) : preferred.DiagnosticAt(processor, message, values);
        if (!chain.IsNil)
            result.SetMessageChain(chain);
        if (!relatedInfo.IsNil)
            result.SetRelatedInfo(relatedInfo);
        return result;
    }
}

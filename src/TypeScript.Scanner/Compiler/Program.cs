// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/compiler/program.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Fixed-host diagnostic policy only.
// No project loader or complete Program is provided. The Collections host fixes
// NoCheck, SkipDefaultLibCheck and project-reference membership to false; its
// source set is TS only. Demanded library type resolution is never skipped here.
using TypeScript.Scanner;

namespace TypeScript.Compiler;
internal static class Program
{
    // program.go:1505,1514,1675. Fixed TS host counterpart: noEmit is false,
    // no references/modules/content mapper or checking directives are admitted.
    // Include-processor diagnostics are empty because all explicit roots exist.
    internal static List<Diagnostic>? GetSemanticDiagnosticsWithChecker(SourceFile sourceFile, CompilerOptions options, TypeScript.Checking.Checker checker)
    {
        if (SkipTypeChecking(sourceFile, options))
            return null;
        if (sourceFile.ScriptKind != ScriptKind.TS || sourceFile.CommentDirectives is { Count: > 0 } || sourceFile.CheckJsDirective is not null)
            throw new TypeScript.Checking.UnsupportedSemanticFeatureException("Program.getSemanticDiagnosticsWithChecker: fixed TS host policy");
        var diags = sourceFile.BindDiagnostics()?.ToList();
        var checkedDiags = checker.GetDiagnostics();
        if (checkedDiags is { Count: > 0 })
        {
            diags ??= [];
            diags.AddRange(checkedDiags);
        }

        return SortAndDeduplicateDiagnostics(diags);
    }

    internal static List<Diagnostic>? SortAndDeduplicateDiagnostics(List<Diagnostic>? diagnostics)
    {
        if (diagnostics is null)
            return null;
        diagnostics = diagnostics.ToList();
        GoSlices.SortFunc(diagnostics, Ast.CompareDiagnostics);
        return compactAndMergeRelatedInfos(diagnostics);
    }

    // program.go:1683. The private clone's unused tail is removed in managed
    // storage, matching clear plus the shorter Go result slice.
    private static List<Diagnostic> compactAndMergeRelatedInfos(List<Diagnostic> diagnostics)
    {
        if (diagnostics.Count < 2)
            return diagnostics;
        int i = 0, j = 0;
        while (i < diagnostics.Count)
        {
            var diagnostic = diagnostics[i];
            int n = 1;
            while (i + n < diagnostics.Count && Ast.EqualDiagnosticsNoRelatedInfo(diagnostic, diagnostics[i + n]))
                n++;
            if (n > 1)
            {
                List<Diagnostic>? related = null;
                for (int k = 0; k < n; k++)
                    if (diagnostics[i + k].RelatedInformation()is { Count: > 0 } infos)
                    {
                        related ??= [];
                        related.AddRange(infos);
                    }

                if (related is not null)
                {
                    GoSlices.SortFunc(related, Ast.CompareDiagnostics);
                    int write = 0;
                    foreach (var value in related.ToArray())
                        if (write == 0 || !Ast.EqualDiagnostics(related[write - 1], value))
                            related[write++] = value;
                    related.RemoveRange(write, related.Count - write);
                    diagnostic = diagnostic.Clone().SetRelatedInfo(related);
                }
            }

            diagnostics[j++] = diagnostic;
            i += n;
        }

        diagnostics.RemoveRange(j, diagnostics.Count - j);
        return diagnostics;
    }

    internal static bool SkipTypeChecking(SourceFile sourceFile, CompilerOptions options) => options.SkipLibCheck == Tristate.True && sourceFile.IsDeclarationFile || !canIncludeBindAndCheckDiagnostics(sourceFile);
    private static bool canIncludeBindAndCheckDiagnostics(SourceFile sourceFile)
    {
        if (sourceFile.CheckJsDirective is { Enabled: false })
            return false;
        if (sourceFile.ScriptKind is ScriptKind.TS or ScriptKind.TSX)
            return true;
        throw new TypeScript.Checking.UnsupportedSemanticFeatureException("Program.canIncludeBindAndCheckDiagnostics: fixed TS environment");
    }
}

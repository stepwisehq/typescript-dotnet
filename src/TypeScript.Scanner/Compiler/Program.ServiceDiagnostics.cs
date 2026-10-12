// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// compiler/program.go:719–770,822–841 context-preserving diagnostics closure.
// Ordinary token overloads remain unchanged; explicit LS context retains request
// values only at the actual project checker-pool checkout boundary.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;
using Diagnostic = TypeScript.Scanner.Diagnostic;

namespace TypeScript.Compiler;
public sealed partial class Program
{
    internal Slice<Diagnostic> GetSemanticDiagnostics(ProtocolRequestContext context, SourceFile? source = null) => collectCheckerDiagnostics(context, source, getSemanticDiagnosticsWithChecker);
    internal Slice<Diagnostic> GetSuggestionDiagnostics(ProtocolRequestContext context, SourceFile? source = null) => collectCheckerDiagnostics(context, source, getSuggestionDiagnosticsWithChecker);
    internal Slice<Diagnostic> collectCheckerDiagnostics(ProtocolRequestContext context, SourceFile? source, Func<CancellationToken, Checker, SourceFile, Slice<Diagnostic>> collect)
    {
        if (source is not null)
        {
            if (SkipTypeChecking(source, false))
                return default;
            var(checker, done) = GetTypeCheckerForFileExclusive(context, source);
            Slice<Diagnostic> diagnostics;
            try
            {
                diagnostics = collect(context.Cancellation, checker, source);
            }
            finally
            {
                done();
            }
            return filterAndSortDiagnostics(diagnostics);
        }

        return filterAndSortDiagnostics(concatDiagnostics(collectCheckerDiagnosticsFromFiles(context, files, collect)));
    }

    internal Slice<Slice<Diagnostic>> collectCheckerDiagnosticsFromFiles(ProtocolRequestContext context, Slice<SourceFile> sourceFiles, Func<CancellationToken, Checker, SourceFile, Slice<Diagnostic>> collect)
    {
        var diagnostics = Slice<Slice<Diagnostic>>.Make(sourceFiles.Length);
        if (compilerCheckerPool is not null)
            compilerCheckerPool.forEachCheckerGroupDo(context.Cancellation, sourceFiles, SingleThreaded(), (checker, index, file) => diagnostics[index] = collect(context.Cancellation, checker, file));
        else
        {
            var work = Core.NewWorkGroup(SingleThreaded());
            for (int i = 0; i < sourceFiles.Count; i++)
            {
                int index = i;
                var file = sourceFiles[i];
                if (SkipTypeChecking(file, false))
                    continue;
                work.Queue(() =>
                {
                    var(checker, done) = checkerPool is ProjectCheckerPool projectPool ? projectPool.GetChecker(context, file) : checkerPool.GetChecker(context.Cancellation, file);
                    try
                    {
                        diagnostics[index] = collect(context.Cancellation, checker, file);
                    }
                    finally
                    {
                        done();
                    }
                });
            }

            work.RunAndWait();
        }

        return diagnostics;
    }

    internal Dictionary<SourceFile, Slice<Diagnostic>> GetSemanticDiagnosticsForIncremental(ProtocolRequestContext context, Slice<SourceFile> sourceFiles)
    {
        var diagnostics = collectCheckerDiagnosticsFromFiles(context, sourceFiles, (token, checker, file) => getBindAndCheckDiagnosticsWithChecker(token, checker, file, true));
        var result = new Dictionary<SourceFile, Slice<Diagnostic>>(sourceFiles.Length, ReferenceEqualityComparer.Instance);
        for (int i = 0; i < diagnostics.Count; i++)
            result[sourceFiles[i]] = filterAndSortDiagnostics(diagnostics[i]);
        return result;
    }
}

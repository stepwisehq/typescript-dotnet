// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Focused compiler/program.go checker checkout context propagation and
// internal/project/checkerpool.go:123 request-lifetime consumer join.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;

namespace TypeScript.Compiler;
public sealed partial class Program
{
    // Go carries request values inside context.Context. The ordinary compiler pool
    // remains token-based; the genuine project pool receives the complete request.
    internal (Checker Checker, Action Done) GetTypeCheckerForFile(ProtocolRequestContext context, SourceFile file)
    {
        if (compilerCheckerPool != null)
            return compilerCheckerPool.getCheckerForFileNonExclusive(file);
        return checkerPool is ProjectCheckerPool projectPool ? projectPool.GetChecker(context, file) : checkerPool.GetChecker(context.Cancellation, file);
    }

    // compiler/program.go: general checkout preserves real request lifetime values.
    internal (Checker Checker, Action Done) GetTypeChecker(ProtocolRequestContext context)
    {
        if (compilerCheckerPool != null)
            return compilerCheckerPool.getCheckerNonExclusive();
        return checkerPool is ProjectCheckerPool projectPool ? projectPool.GetChecker(context, null) : checkerPool.GetChecker(context.Cancellation, null);
    }

    // Compiler exclusive affinity remains unchanged; ProjectCheckerPool implements
    // the original project checkout contract, which carries the full request context.
    internal (Checker Checker, Action Done) GetTypeCheckerForFileExclusive(ProtocolRequestContext context, SourceFile file)
    {
        if (compilerCheckerPool != null)
            return compilerCheckerPool.getCheckerForFileExclusive(context.Cancellation, file);
        return checkerPool is ProjectCheckerPool projectPool ? projectPool.GetChecker(context, file) : checkerPool.GetChecker(context.Cancellation, file);
    }
}

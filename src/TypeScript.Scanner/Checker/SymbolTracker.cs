// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/symboltracker.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Diagnostic-only finite data-property/scalar context reuse; general emit excluded.
#nullable disable
using TypeScript.Scanner;
using TypeScript.PseudoChecking;

namespace TypeScript.Checking;
// Only the default diagnostic tracker and its selected inference-fallback callback
// are required. No public tracker/late-painted statement interface is introduced.
internal sealed class SymbolTrackerImpl
{
    private readonly Action<Node> inner;
    private readonly NodeBuilderImpl.NodeBuilderContext context;
    private SymbolTrackerImpl(NodeBuilderImpl.NodeBuilderContext context, Action<Node> inner)
    {
        this.context = context;
        this.inner = inner;
    }

    internal static SymbolTrackerImpl NewSymbolTrackerImpl(NodeBuilderImpl.NodeBuilderContext context, Action<Node> tracker) => new(context, tracker);
    // symboltracker.go:66/106. Reporting records context state even when inner is nil.
    internal void ReportCyclicStructureError()
    {
        onDiagnosticReported();
        if (inner == null)
            return;
        throw Checker.unsupported("ReportCyclicStructureError: external tracker");
    }

    private void onDiagnosticReported() => context.reportedDiagnostic = true;
    internal void ReportInferenceFallback(Node node)
    {
        if (inner == null)
            return;
        inner(node);
    }
}

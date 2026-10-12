// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/symboltracker.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Complete emit and diagnostic tracker forwarding.
#nullable disable
using TypeScript.Scanner;
using TypeScript.PseudoChecking;

namespace TypeScript.Checking;
// Every tracker callback forwards through the actual emit-facing contract.
internal sealed class SymbolTrackerImpl : TypeScript.NodeBuilding.SymbolTracker
{
    private readonly TypeScript.NodeBuilding.SymbolTracker inner;
    private readonly NodeBuilderImpl.NodeBuilderContext context;
    private SymbolTrackerImpl(NodeBuilderImpl.NodeBuilderContext context, TypeScript.NodeBuilding.SymbolTracker inner)
    {
        this.context = context;
        this.inner = inner;
    }

    internal static SymbolTrackerImpl NewSymbolTrackerImpl(NodeBuilderImpl.NodeBuilderContext context, TypeScript.NodeBuilding.SymbolTracker tracker)
    {
        while (tracker is SymbolTrackerImpl previous)
            tracker = previous.inner;
        return new(context, tracker);
    }

    internal bool DisableTrackSymbol;
    // symboltracker.go:28. Nil external trackers still record named references.
    public bool TrackSymbol(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning)
    {
        if (!DisableTrackSymbol)
        {
            if (inner != null && inner.TrackSymbol(symbol, enclosingDeclaration, meaning))
            {
                onDiagnosticReported();
                return true;
            }

            if ((symbol.Flags & SymbolFlags.TypeParameter) == 0)
                context.trackedSymbols = context.trackedSymbols.Append(new NodeBuilderImpl.TrackedSymbolArgs(symbol, enclosingDeclaration, meaning));
        }

        return false;
    }

    // symboltracker.go:66/106. Reporting records context state even when inner is nil.
    public void ReportCyclicStructureError()
    {
        onDiagnosticReported();
        if (inner == null)
            return;
        inner.ReportCyclicStructureError();
    }

    // symboltracker.go:72. Diagnostic state is retained even with no external tracker.
    public void ReportInaccessibleThisError()
    {
        onDiagnosticReported();
        inner?.ReportInaccessibleThisError();
    }

    // symboltracker.go:58. Preserve diagnostic state with a nil external tracker.
    public void ReportInaccessibleUniqueSymbolError()
    {
        onDiagnosticReported();
        inner?.ReportInaccessibleUniqueSymbolError();
    }

    // symboltracker.go:98. Preserve the default tracker diagnostic state too.
    public void ReportNonSerializableProperty(string propertyName)
    {
        onDiagnosticReported();
        inner?.ReportNonSerializableProperty(propertyName);
    }

    // symboltracker.go:82. The source callback observes NoTruncation serializer exhaustion.
    public void ReportTruncationError()
    {
        onDiagnosticReported();
        inner?.ReportTruncationError();
    }

    // symboltracker.go:74. Source unsafe import naming diagnostic callback.
    public void ReportLikelyUnsafeImportRequiredError(string specifier, string symbolName)
    {
        onDiagnosticReported();
        inner?.ReportLikelyUnsafeImportRequiredError(specifier, symbolName);
    }

    private void onDiagnosticReported() => context.reportedDiagnostic = true;
    public void ReportInferenceFallback(Node node)
    {
        if (inner == null)
            return;
        inner.ReportInferenceFallback(node);
    }
    public void ReportPrivateInBaseOfClassExpression(string propertyName)
    {
        onDiagnosticReported();
        inner?.ReportPrivateInBaseOfClassExpression(propertyName);
    }
    public void ReportNonlocalAugmentation(SourceFile containingFile, Symbol parentSymbol, Symbol augmentingSymbol)
    {
        onDiagnosticReported();
        inner?.ReportNonlocalAugmentation(containingFile, parentSymbol, augmentingSymbol);
    }
    public void PushErrorFallbackNode(Node node) => inner?.PushErrorFallbackNode(node);
    public void PopErrorFallbackNode() => inner?.PopErrorFallbackNode();
}

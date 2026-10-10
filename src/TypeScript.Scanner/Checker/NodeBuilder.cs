// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/nodebuilder.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Diagnostic-only scalar, named and composite data-type serialization and selected source-context reuse; general emit excluded.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
internal sealed class NodeBuilder
{
    private readonly NodeBuilderImpl impl;
    private readonly Stack<NodeBuilderImpl.NodeBuilderContext> ctxStack = new();
    internal TypeScript.DiagnosticPrinting.EmitContext EmitContext() => impl.e;
    internal NodeBuilder(Checker checker) => impl = new(checker);
    internal NodeBuilder(Checker checker, TypeScript.DiagnosticPrinting.EmitContext context) => impl = new(checker, context);
    // nodebuilder.go:228–270. Exact source flag/context entrypoints.
    internal Node TypeToTypeNode(Type type, Node enclosingDeclaration, TypeScript.NodeBuilding.Flags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, TypeScript.NodeBuilding.SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.typeToTypeNode(type));
    }

    internal Node SymbolToEntityName(Symbol symbol, SymbolFlags meaning, Node enclosingDeclaration, TypeScript.NodeBuilding.Flags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, TypeScript.NodeBuilding.SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.symbolToName(symbol, meaning, false));
    }

    internal Node SymbolToNode(Symbol symbol, SymbolFlags meaning, Node enclosingDeclaration, TypeScript.NodeBuilding.Flags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, TypeScript.NodeBuilding.SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.symbolToNode(symbol, meaning));
    }

    private void enterContext(Node enclosingDeclaration, TypeScript.NodeBuilding.Flags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, TypeScript.NodeBuilding.SymbolTracker tracker)
    {
        ctxStack.Push(impl.ctx);
        impl.enterContext(enclosingDeclaration, flags, internalFlags, tracker);
    }

    // TypeToTypeNode (267); selected enclosing declaration and fully-qualified flag; external trackers/verbosity excluded.
    internal Node TypeToTypeNode(Type type, Node enclosingDeclaration = null, bool fullyQualified = false, bool noTypeReduction = false, TypeScript.NodeBuilding.Flags? flags = null)
    {
        enterContext(enclosingDeclaration, fullyQualified, noTypeReduction);
        if (flags != null)
            impl.ctx.flags = flags.Value;
        return exitContext(impl.typeToTypeNode(type));
    }

    // nodebuilder.go:238. Default IgnoreErrors; computed-property writing disabled.
    internal Node SymbolToNode(Symbol symbol, SymbolFlags meaning, Node enclosingDeclaration = null, TypeScript.NodeBuilding.InternalFlags internalFlags = TypeScript.NodeBuilding.InternalFlags.None)
    {
        enterContext(enclosingDeclaration);
        impl.ctx.flags = TypeScript.NodeBuilding.Flags.IgnoreErrors;
        impl.ctx.internalFlags = internalFlags;
        return exitContext(impl.symbolToNode(symbol, meaning));
    }

    // nodebuilder.go: SignatureToSignatureDeclaration / TypePredicateToTypePredicateNode.
    internal Node SignatureToSignatureDeclaration(Signature signature, Kind kind, Node enclosingDeclaration, TypeScript.NodeBuilding.Flags flags)
    {
        enterContext(enclosingDeclaration);
        impl.ctx.flags = flags;
        return exitContext(impl.signatureToSignatureDeclarationHelper(signature, kind, new()));
    }

    internal Node TypePredicateToTypePredicateNode(TypePredicate predicate, Node enclosingDeclaration, TypeScript.NodeBuilding.Flags flags)
    {
        enterContext(enclosingDeclaration);
        impl.ctx.flags = flags;
        return exitContext(impl.typePredicateToTypePredicateNode(predicate));
    }

    private void enterContext(Node enclosingDeclaration = null, bool fullyQualified = false, bool noTypeReduction = false)
    {
        ctxStack.Push(impl.ctx);
        impl.enterContext(enclosingDeclaration, fullyQualified, noTypeReduction);
    }

    private Node exitContext(Node result)
    {
        if (impl.ctx.truncating && (impl.ctx.flags & TypeScript.NodeBuilding.Flags.NoTruncation) != 0)
            impl.ctx.tracker.ReportTruncationError();
        bool error = impl.ctx.encounteredError;
        impl.ctx = ctxStack.Pop();
        return error ? null : result;
    }
}

public sealed partial class Checker
{
    private NodeBuilder typeToStringNodebuilder;
    // nodebuilder.go:279. Source shared printer context, managed arenas have no explicit release.
    internal static NodeBuilder NewNodeBuilder(Checker checker, TypeScript.DiagnosticPrinting.EmitContext context) => new(checker, context);
    // nodebuilder.go:289. Managed allocation replaces arena release; cache identity persists.
    private NodeBuilder getNodeBuilder() => typeToStringNodebuilder ??= new NodeBuilder(this);
}

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
        bool error = impl.ctx.encounteredError;
        impl.ctx = ctxStack.Pop();
        return error ? null : result;
    }
}

public sealed partial class Checker
{
    private NodeBuilder typeToStringNodebuilder;
    // nodebuilder.go:289. Managed allocation replaces arena release; cache identity persists.
    private NodeBuilder getNodeBuilder() => typeToStringNodebuilder ??= new NodeBuilder(this);
}

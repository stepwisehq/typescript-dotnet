// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/checker/nodebuilder.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Ordinary callers retain disabled expansion; configured service requests carry verbosity explicitly.
#nullable disable
using TypeScript.Scanner;
using Flags = TypeScript.NodeBuilding.Flags;
using InternalFlags = TypeScript.NodeBuilding.InternalFlags;
using SymbolTracker = TypeScript.NodeBuilding.SymbolTracker;
using EmitContext = TypeScript.DiagnosticPrinting.EmitContext;

namespace TypeScript.Checking;
internal sealed partial class NodeBuilder
{
    private readonly NodeBuilderImpl impl;
    internal VerbosityContext verbosity;
    private Slice<NodeBuilderImpl.NodeBuilderContext> ctxStack = Slice<NodeBuilderImpl.NodeBuilderContext>.Make(0, 1);
    internal EmitContext EmitContext() => impl.e;
    internal NodeBuilder(Checker checker, EmitContext context, Dictionary<Node, Symbol> symbols = null) => impl = new(checker, context, symbols);
    internal NodeBuilder(Checker checker) : this(checker, new EmitContext())
    {
    }

    private void enterContext(Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        ctxStack = ctxStack.Append(impl.ctx);
        impl.enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        impl.ctx.maxExpansionDepth = verbosity?.Level ?? -1;
        impl.ctx.maxTruncationLength = verbosity?.MaxTruncationLength ?? 0;
    }

    private void popContext()
    {
        int size = ctxStack.Count;
        if (size == 0)
            impl.ctx = null;
        else
        {
            impl.ctx = ctxStack[size - 1];
            ctxStack = ctxStack.Slice2(0, size - 1);
        }
    }

    private void exitContextCheck()
    {
        if (impl.ctx.truncating && (impl.ctx.flags & Flags.NoTruncation) != 0)
            impl.ctx.tracker.ReportTruncationError();
    }

    private Node exitContext(Node result)
    {
        propagateVerbosityOut();
        exitContextCheck();
        try
        {
            return impl.ctx.encounteredError ? null : result;
        }
        finally
        {
            popContext();
        }
    }

    private Slice<Node> exitContextSlice(Slice<Node> result)
    {
        propagateVerbosityOut();
        exitContextCheck();
        try
        {
            return impl.ctx.encounteredError ? default : result;
        }
        finally
        {
            popContext();
        }
    }

    internal Node IndexInfoToIndexSignatureDeclaration(IndexInfo info, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.indexInfoToIndexSignatureDeclarationHelper(info, null));
    }

    internal Node SerializeTypeForDeclaration(Node declaration, Symbol symbol, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.serializeTypeForDeclaration(declaration, null, symbol, true));
    }

    internal Node SerializeTypeForExpression(Node expr, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.serializeTypeForExpression(expr));
    }

    internal Node SignatureToSignatureDeclaration(Signature signature, Kind kind, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.signatureToSignatureDeclarationHelper(signature, kind, default));
    }

    internal Node SymbolToEntityName(Symbol symbol, SymbolFlags meaning, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.symbolToName(symbol, meaning, false));
    }

    internal Node SymbolToExpression(Symbol symbol, SymbolFlags meaning, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.symbolToExpression(symbol, meaning));
    }

    internal Node SymbolToNode(Symbol symbol, SymbolFlags meaning, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.symbolToNode(symbol, meaning));
    }

    internal Node SymbolToParameterDeclaration(Symbol symbol, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.symbolToParameterDeclaration(symbol, false));
    }

    internal Slice<Node> SymbolToTypeParameterDeclarations(Symbol symbol, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContextSlice(impl.symbolToTypeParameterDeclarations(symbol));
    }

    internal Node TypeParameterToDeclaration(Type parameter, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.typeParameterToDeclaration(parameter));
    }

    internal Node TypePredicateToTypePredicateNode(TypePredicate predicate, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.typePredicateToTypePredicateNode(predicate));
    }

    internal Node TypeToTypeNode(Type type, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.typeToTypeNode(type));
    }

    internal Node TryJSTypeNodeToTypeNode(Node node, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        return exitContext(impl.tryJSTypeNodeToTypeNode(node));
    }

    internal Node SerializeReturnTypeForSignature(Node signatureDeclaration, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        var signature = impl.ch.getSignatureFromDeclaration(signatureDeclaration);
        var(_, cleanup) = impl.enterSignatureScope(signature);
        var result = impl.serializeReturnTypeForSignature(signature, true);
        cleanup();
        return exitContext(result);
    }

    internal Slice<Node> SerializeTypeParametersForSignature(Node signatureDeclaration, Node enclosingDeclaration, Flags flags, InternalFlags internalFlags, SymbolTracker tracker)
    {
        enterContext(enclosingDeclaration, flags, internalFlags, tracker);
        var symbol = impl.ch.getSymbolOfDeclaration(signatureDeclaration);
        var result = SymbolToTypeParameterDeclarations(symbol, enclosingDeclaration, flags, internalFlags, tracker);
        return exitContextSlice(result);
    }

    // Retained diagnostic callers share the exact request/context core.
    internal Node SignatureToSignatureDeclaration(Signature signature, Kind kind, Node enclosingDeclaration, Flags flags) => SignatureToSignatureDeclaration(signature, kind, enclosingDeclaration, flags, InternalFlags.None, null);
    internal Node TypePredicateToTypePredicateNode(TypePredicate predicate, Node enclosingDeclaration, Flags flags) => TypePredicateToTypePredicateNode(predicate, enclosingDeclaration, flags, InternalFlags.None, null);
    internal Node TypeToTypeNode(Type type, Node enclosingDeclaration = null, bool fullyQualified = false, bool noTypeReduction = false, Flags? flags = null) => TypeToTypeNode(type, enclosingDeclaration, flags ?? (Flags.IgnoreErrors | Flags.AllowUniqueESSymbolType | Flags.UseAliasDefinedOutsideCurrentScope | (fullyQualified ? Flags.UseFullyQualifiedType : 0) | (noTypeReduction ? Flags.NoTypeReduction : 0)), InternalFlags.None, null);
    internal Node SymbolToNode(Symbol symbol, SymbolFlags meaning, Node enclosingDeclaration = null, InternalFlags internalFlags = InternalFlags.None) => SymbolToNode(symbol, meaning, enclosingDeclaration, Flags.IgnoreErrors, internalFlags, null);
}

public sealed partial class Checker
{
    private NodeBuilder typeToStringNodebuilder;
    internal static NodeBuilder NewNodeBuilder(Checker checker, EmitContext context) => NewNodeBuilderEx(checker, context, null);
    internal static NodeBuilder NewNodeBuilderEx(Checker checker, EmitContext context, Dictionary<Node, Symbol> symbols) => new(checker, context, symbols);
    private NodeBuilder getNodeBuilder() => typeToStringNodebuilder ??= getNodeBuilderEx(null);
    private NodeBuilder getNodeBuilderEx(Dictionary<Node, Symbol> symbols) => NewNodeBuilderEx(this, new EmitContext(), symbols);
    private void releaseNodeBuilderNodes() => typeToStringNodebuilder.EmitContext().Factory.ReleaseArenas();
}

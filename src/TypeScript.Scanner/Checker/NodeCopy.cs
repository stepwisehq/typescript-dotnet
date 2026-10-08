// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/nodecopy.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Diagnostic-only finite data-property/scalar context reuse; general emit excluded.
#nullable disable
using TypeScript.Scanner;
using TypeScript.PseudoChecking;

namespace TypeScript.Checking;
internal sealed partial class NodeBuilderImpl
{
    private Node reuseNode(Node node) => node == null ? null : tryReuseExistingNodeHelper(node);
    private Node reuseName(Node node, bool isMethod)
    {
        var res = reuseNode(node);
        if (res == null)
            return res;
        if (res.Kind is not (Kind.Identifier or Kind.StringLiteral or Kind.NumericLiteral))
            throw Checker.unsupported("reuseName: nonstatic name");
        string text = res.Text();
        var kind = classifyPropertyName(text, Ast.IsStringLiteral(res), isMethod);
        if (Ast.IsIdentifier(res) && kind == propertyNameNodeKind.Identifier || Ast.IsStringLiteral(res) && kind == propertyNameNodeKind.StringLiteral)
            return res;
        Node renamed;
        if (kind == propertyNameNodeKind.Identifier)
            renamed = newIdentifier(text, null);
        else if (kind == propertyNameNodeKind.StringLiteral)
            renamed = f.NewStringLiteral(text, TokenFlags.None);
        else
            return res;
        e.SetOriginal(renamed, res);
        return setTextRange(renamed, res);
    }

    private Node reuseTypeNode(Node node)
    {
        if (node == null)
            return node;
        var result = reuseNode(node);
        if (result != null)
            return result; // Expandability walk is disabled by fixed maxExpansionDepth=-1.
        ctx.tracker.ReportInferenceFallback(node);
        return typeToTypeNode(ch.getTypeFromTypeNode(node));
    }

    // nodecopy.go:100/184/192. Restore the actual recovery tracker and
    // symbol-recording boundary now reached by named type references.
    private sealed class recoveryBoundary
    {
        internal NodeBuilderContext ctx;
        internal bool hadError;
        internal List<Action> deferredReports;
        internal SymbolTrackerImpl oldTracker;
        internal List<TrackedSymbolArgs> oldTrackedSymbols, trackedSymbols;
        internal bool oldEncounteredError;
        internal int oldApproximateLength;
        internal originalRecoveryScopeState startRecoveryScope() => new(ctx.trackedSymbols?.Count ?? 0, deferredReports?.Count ?? 0, hadError);
        internal void endRecoveryScope(originalRecoveryScopeState state)
        {
            hadError = state.hadError;
            if (ctx.trackedSymbols != null)
                ctx.trackedSymbols.RemoveRange(state.trackedSymbolsTop, ctx.trackedSymbols.Count - state.trackedSymbolsTop);
            if (deferredReports != null)
                deferredReports.RemoveRange(state.unreportedErrorsTop, deferredReports.Count - state.unreportedErrorsTop);
        }

        internal void markError(Action callback)
        {
            hadError = true;
            if (callback != null)
                (deferredReports ??= new()).Add(callback);
        }
    }

    private readonly record struct originalRecoveryScopeState(int trackedSymbolsTop, int unreportedErrorsTop, bool hadError);
    private sealed class wrappingTracker(SymbolTrackerImpl wrapped, recoveryBoundary bound) : TypeScript.NodeBuilding.SymbolTracker
    {
        public bool TrackSymbol(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning)
        {
            (bound.trackedSymbols ??= new()).Add(new(symbol, enclosingDeclaration, meaning));
            return false;
        }

        public void ReportCyclicStructureError() => bound.markError(wrapped.ReportCyclicStructureError);
        public void ReportInaccessibleThisError() => bound.markError(wrapped.ReportInaccessibleThisError);
        public void ReportInferenceFallback(Node node) => wrapped.ReportInferenceFallback(node);
    }

    private recoveryBoundary createRecoveryBoundary()
    {
        var bound = new recoveryBoundary
        {
            ctx = ctx,
            oldTracker = ctx.tracker,
            oldTrackedSymbols = ctx.trackedSymbols,
            oldEncounteredError = encounteredError,
            oldApproximateLength = approximateLength
        };
        ctx.tracker = SymbolTrackerImpl.NewSymbolTrackerImpl(ctx, new wrappingTracker(ctx.tracker, bound));
        ctx.trackedSymbols = null;
        return bound;
    }

    private bool finalizeBoundary(recoveryBoundary bound)
    {
        ctx.tracker = bound.oldTracker;
        ctx.trackedSymbols = bound.oldTrackedSymbols;
        encounteredError = bound.oldEncounteredError;
        approximateLength = bound.oldApproximateLength;
        foreach (var callback in bound.deferredReports ?? [])
            callback();
        if (bound.hadError)
            return false;
        foreach (var argument in bound.trackedSymbols ?? [])
            ctx.tracker.TrackSymbol(argument.symbol, argument.enclosingDeclaration, argument.meaning);
        return true;
    }

    private Node tryReuseExistingNodeHelper(Node existing)
    {
        var bound = createRecoveryBoundary();
        var visitor = getExistingNodeTreeVisitor(bound);
        var transformed = visitor.VisitNode(existing);
        if (!finalizeBoundary(bound))
            return null;
        approximateLength += existing.End() - existing.Pos();
        return transformed;
    }

    private NodeVisitor getExistingNodeTreeVisitor(recoveryBoundary bound)
    {
        NodeVisitor visitor = null;
        bool nonLocalNode = true;
        // nodecopy.go:292/314. Retain resolution in both original and target
        // scopes; a spelling match is insufficient when names are shadowed.
        Node attachSymbolToLeftmostIdentifier(Node leftmost, Node node, Symbol symbol)
        {
            NodeVisitor attaching = null;
            Node Attach(Node current)
            {
                if (current == leftmost)
                {
                    if (symbol != null && (symbol.Flags & SymbolFlags.TypeParameter) != 0)
                        throw Checker.unsupported("attachSymbolToLeftmostIdentifier: source type parameter");
                    var name = setTextRange(newIdentifier(current.Text(), symbol), current);
                    e.AddEmitFlags(name, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
                    return name;
                }

                return setTextRange(attaching.VisitEachChild(current), current);
            }

            attaching = new NodeVisitor(Attach, f, new());
            return Attach(node);
        }

        (bool introducesError, Node newName, Symbol symbol) trackExistingEntityName(Node node)
        {
            var leftmost = Ast.GetFirstIdentifier(node);
            var meaning = Checker.getMeaningOfEntityNameReference(node);
            var symbol = ch.resolveEntityName(leftmost, meaning, true, true, null);
            if (ctx.enclosingDeclaration != null && !(symbol != null && (symbol.Flags & SymbolFlags.TypeParameter) != 0))
            {
                symbol = ch.getExportSymbolOfValueSymbolIfExported(symbol);
                var atLocation = ch.resolveEntityName(leftmost, meaning, true, true, ctx.enclosingDeclaration);
                if (atLocation == ch.unknownSymbol || atLocation == null && symbol != null || atLocation != null && symbol != null && ch.getSymbolIfSameReference(ch.getExportSymbolOfValueSymbolIfExported(atLocation), symbol) == null)
                {
                    if (atLocation != ch.unknownSymbol)
                        ctx.tracker.ReportInferenceFallback(node);
                    return (true, setTextRange(f.DeepCloneNode(node), node), symbol);
                }

                symbol = atLocation;
            }

            if (symbol != null)
            {
                bool introducesError = (symbol.Flags & SymbolFlags.TypeParameter) == 0 && !Ast.IsDeclarationName(node) && ch.IsSymbolAccessible(symbol, ctx.enclosingDeclaration, meaning, false).Accessibility != TypeScript.DiagnosticPrinting.SymbolAccessibility.Accessible;
                if (introducesError)
                    ctx.tracker.ReportInferenceFallback(node);
                else
                    ctx.tracker.TrackSymbol(symbol, ctx.enclosingDeclaration, meaning);
                return (introducesError, attachSymbolToLeftmostIdentifier(leftmost, node, symbol), null);
            }

            return (false, setTextRange(f.DeepCloneNode(node), node), null);
        }

        Node tryVisitTypeReference(Node node)
        {
            if (Ast.IsConstTypeReference(node))
                return null;
            var symbol = tryGetResolvedSymbolFromTypeNode(node);
            if (symbol == null)
                return null;
            if ((symbol.Flags & SymbolFlags.TypeParameter) != 0 && ctx.mapper != null && Checker.getMappedType(ch.getDeclaredTypeOfSymbol(symbol), ctx.mapper) != ch.getDeclaredTypeOfSymbol(symbol))
                return null;
            if (!canReuseExistingJSTypeNode(node, getTypeFromTypeNode(node, false)))
                return null;
            var(error, name, _) = trackExistingEntityName(node.AsTypeReferenceNode().TypeName);
            var arguments = visitor.VisitNodes(node.AsTypeReferenceNode().TypeArguments);
            if (!error)
                return setTextRange(f.UpdateTypeReferenceNode(node.AsTypeReferenceNode(), name, arguments), node);
            var serialized = serializeTypeName(node.AsTypeReferenceNode().TypeName, false, arguments);
            return serialized != null ? setTextRange(serialized, node.AsTypeReferenceNode().TypeName) : null;
        }

        Node worker(Node node)
        {
            if (Ast.IsKeywordTypeNode(node) || node.Kind is Kind.Identifier or Kind.NumericLiteral or Kind.TrueKeyword or Kind.FalseKeyword or Kind.NullKeyword or Kind.ReadonlyKeyword or Kind.QuestionToken or Kind.MinusToken or Kind.PlusToken)
                return visitor.VisitEachChild(node);
            if (node.Kind == Kind.PropertySignature)
            {
                var prop = node.AsPropertySignatureDeclaration();
                if (prop.Type == null && prop.Initializer == null)
                {
                    var visited = visitor.VisitEachChild(node);
                    if (visited == node)
                        visited = setTextRange(node.Clone(f), node);
                    var data = visited.AsPropertySignatureDeclaration();
                    return f.UpdatePropertySignatureDeclaration(data, data.Modifiers(), data.Name(), data.PostfixToken, f.NewKeywordTypeNode(Kind.AnyKeyword), null);
                }

                return visitor.VisitEachChild(node);
            }

            if (node.Kind == Kind.TypeLiteral)
            {
                var res = visitor.VisitEachChild(node);
                if (res == node)
                    res = setTextRange(res.Clone(f), node);
                return res; // EFSingleLine is fixed on diagnostic type literal emission.
            }

            if (node.Kind is Kind.StringLiteral or Kind.NoSubstitutionTemplateLiteral)
            {
                var clone = node.Clone(f);
                e.AddEmitFlags(clone, TypeScript.DiagnosticPrinting.EmitFlags.NoAsciiEscaping);
                return clone;
            }

            if (node.Kind == Kind.ThisType)
                return node;
            if (node.Kind == Kind.TypeReference)
            {
                if (node.AsTypeReferenceNode().TypeName.Kind == Kind.Identifier && node.AsTypeReferenceNode().TypeName.Text() == "")
                {
                    var replacement = f.NewKeywordTypeNode(Kind.AnyKeyword);
                    e.SetOriginal(replacement, node);
                    return replacement;
                }

                var reference = tryVisitTypeReference(node);
                if (reference != null)
                    return reference;
                bound.markError(null);
                return node;
            }

            if (node.Kind is Kind.LiteralType or Kind.ParenthesizedType or Kind.PrefixUnaryExpression or Kind.UnionType or Kind.IntersectionType)
                return visitor.VisitEachChild(node);
            throw Checker.unsupported("getExistingNodeTreeVisitor: references/signatures/indexes/JS/dynamic syntax");
        }

        visitor = new NodeVisitor(node =>
        {
            if (bound.hadError)
                return node;
            var recovery = bound.startRecoveryScope();
            var result = worker(node);
            if (result == node && !Ast.NodeIsSynthesized(node))
                result = f.DeepCloneNode(node);
            result = setTextRange(result, node);
            if (bound.hadError)
            {
                if (Ast.IsTypeNode(node) && node.Kind != Kind.TypePredicate)
                {
                    bound.endRecoveryScope(recovery);
                    return typeToTypeNode(getTypeFromTypeNode(node, false));
                }

                return setTextRange(node.Clone(f), node);
            }

            return result;
        }, f, new NodeVisitorHooks { VisitNodes = (nodes, v) =>
        {
            var res = v.VisitNodes(nodes);
            if (nonLocalNode && res != null)
            {
                if (res == nodes)
                    res = nodes.Clone(f);
                res.Loc = new(-1, -1);
            }

            return res;
        }, VisitNode = (node, v) =>
        {
            bool old = nonLocalNode;
            nonLocalNode = ctx.enclosingFile == null || ctx.enclosingFile != Ast.GetSourceFileOfNode(mostOriginal(node));
            var res = v.VisitNode(node);
            nonLocalNode = old;
            return res;
        } });
        return visitor;
    }
}

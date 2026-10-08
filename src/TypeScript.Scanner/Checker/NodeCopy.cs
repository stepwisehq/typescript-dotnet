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

    // No admitted static anonymous node has an entity-name/type-parameter/JSDoc
    // branch capable of setting hadError or queuing tracker reports. Retain the
    // actual boundary restoration and length update around the selected visitor.
    private sealed class recoveryBoundary
    {
        internal bool hadError = false;
        internal bool oldEncounteredError;
        internal int oldApproximateLength;
        internal SymbolTrackerImpl oldTracker;
    }

    private recoveryBoundary createRecoveryBoundary() => new()
    {
        oldTracker = ctx.tracker,
        oldEncounteredError = encounteredError,
        oldApproximateLength = approximateLength
    };
    private bool finalizeBoundary(recoveryBoundary bound)
    {
        ctx.tracker = bound.oldTracker;
        encounteredError = bound.oldEncounteredError;
        approximateLength = bound.oldApproximateLength;
        return !bound.hadError;
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

            if (node.Kind is Kind.LiteralType or Kind.ParenthesizedType or Kind.PrefixUnaryExpression)
                return visitor.VisitEachChild(node);
            throw Checker.unsupported("getExistingNodeTreeVisitor: references/signatures/indexes/JS/dynamic syntax");
        }

        visitor = new NodeVisitor(node =>
        {
            if (bound.hadError)
                return node;
            var result = worker(node);
            if (result == node && !Ast.NodeIsSynthesized(node))
                result = f.DeepCloneNode(node);
            return setTextRange(result, node);
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of estransforms/nullishcoalescing.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;

namespace TypeScript.Transformers.EsTransforms;
internal sealed class NullishCoalescingTransformer : Transformer
{
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/nullishcoalescing.go:12:1.
    private Node visit(Node node)
    {
        if ((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsNullishCoalescing) == 0))
        {
            return node;
        }

        switch (node.Kind)
        {
            case Kind.BinaryExpression:
            {
                return this.visitBinaryExpression(node.AsBinaryExpression());
            }

            default:
            {
                return this.Visitor().VisitEachChild(node);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/nullishcoalescing.go:24:1.
    private Node visitBinaryExpression(BinaryExpression node)
    {
        switch (node.OperatorToken.Kind)
        {
            case Kind.QuestionQuestionToken:
            {
                var left = this.Visitor().VisitNode(node.Left);
                var right = left;
                if (!Transformers.IsSimpleCopiableExpression(left))
                {
                    right = this.Factory().NewTempVariable();
                    this.EmitContext().AddVariableDeclaration(right);
                    left = this.Factory().NewAssignmentExpression(right, left);
                }

                return this.Factory().NewConditionalExpression(EsTransforms.CreateNotNullCondition(this.EmitContext(), left, right, false), this.Factory().NewToken(Kind.QuestionToken), right, this.Factory().NewToken(Kind.ColonToken), this.Visitor().VisitNode(node.Right));
            }

            default:
            {
                return this.Visitor().VisitEachChild(node.AsNode());
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/nullishcoalescing.go:46:1.
    internal static Transformer NewNullishCoalescingTransformer(TransformOptions opts)
    {
        var tx = new NullishCoalescingTransformer
        {
        };
        return tx.NewTransformer(tx.visit, opts.Context);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of estransforms/logicalassignment.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;

namespace TypeScript.Transformers.EsTransforms;
internal sealed class LogicalAssignmentTransformer : Transformer
{
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/logicalassignment.go:12:1.
    private Node visit(Node node)
    {
        if ((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsLogicalAssignments) == 0))
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

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/logicalassignment.go:24:1.
    private Node visitBinaryExpression(BinaryExpression node)
    {
        Kind nonAssignmentOperator = default;
        switch (node.OperatorToken.Kind)
        {
            case Kind.BarBarEqualsToken:
            {
                nonAssignmentOperator = Kind.BarBarToken;
                break;
            }

            case Kind.AmpersandAmpersandEqualsToken:
            {
                nonAssignmentOperator = Kind.AmpersandAmpersandToken;
                break;
            }

            case Kind.QuestionQuestionEqualsToken:
            {
                nonAssignmentOperator = Kind.QuestionQuestionToken;
                break;
            }

            default:
            {
                return this.Visitor().VisitEachChild(node.AsNode());
            }
        }

        var left = Ast.SkipParentheses(this.Visitor().VisitNode(node.Left));
        var assignmentTarget = left;
        var right = Ast.SkipParentheses(this.Visitor().VisitNode(node.Right));
        if (Ast.IsAccessExpression(left))
        {
            var propertyAccessTargetSimpleCopiable = Transformers.IsSimpleCopiableExpression(left.Expression());
            var propertyAccessTarget = left.Expression();
            var propertyAccessTargetAssignment = left.Expression();
            if (!propertyAccessTargetSimpleCopiable)
            {
                propertyAccessTarget = this.Factory().NewTempVariable();
                this.EmitContext().AddVariableDeclaration(propertyAccessTarget);
                propertyAccessTargetAssignment = this.Factory().NewAssignmentExpression(propertyAccessTarget, left.Expression());
            }

            if (Ast.IsPropertyAccessExpression(left))
            {
                assignmentTarget = this.Factory().NewPropertyAccessExpression(propertyAccessTarget, null, left.Name(), NodeFlags.None);
                left = this.Factory().NewPropertyAccessExpression(propertyAccessTargetAssignment, null, left.Name(), NodeFlags.None);
            }
            else
            {
                var elementAccessArgumentSimpleCopiable = Transformers.IsSimpleCopiableExpression(left.AsElementAccessExpression().ArgumentExpression);
                var elementAccessArgument = left.AsElementAccessExpression().ArgumentExpression;
                var argumentExpr = elementAccessArgument;
                if (!elementAccessArgumentSimpleCopiable)
                {
                    elementAccessArgument = this.Factory().NewTempVariable();
                    this.EmitContext().AddVariableDeclaration(elementAccessArgument);
                    argumentExpr = this.Factory().NewAssignmentExpression(elementAccessArgument, left.AsElementAccessExpression().ArgumentExpression);
                }

                assignmentTarget = this.Factory().NewElementAccessExpression(propertyAccessTarget, null, elementAccessArgument, NodeFlags.None);
                left = this.Factory().NewElementAccessExpression(propertyAccessTargetAssignment, null, argumentExpr, NodeFlags.None);
            }
        }

        return this.Factory().NewBinaryExpression(null, left, null, this.Factory().NewToken(nonAssignmentOperator), this.Factory().NewParenthesizedExpression(this.Factory().NewAssignmentExpression(assignmentTarget, right)));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/logicalassignment.go:110:1.
    internal static Transformer NewLogicalAssignmentTransformer(TransformOptions opts)
    {
        var tx = new LogicalAssignmentTransformer
        {
        };
        return tx.NewTransformer(tx.visit, opts.Context);
    }
}

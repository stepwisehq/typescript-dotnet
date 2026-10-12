// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of estransforms/exponentiation.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;

namespace TypeScript.Transformers.EsTransforms;
internal sealed class ExponentiationTransformer : Transformer
{
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/exponentiation.go:12:1.
    private Node visit(Node node)
    {
        if ((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsExponentiationOperator) == 0))
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

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/exponentiation.go:24:1.
    private Node visitBinaryExpression(BinaryExpression node)
    {
        switch (node.OperatorToken.Kind)
        {
            case Kind.AsteriskAsteriskEqualsToken:
            {
                return this.visitExponentiationAssignmentExpression(node);
            }

            case Kind.AsteriskAsteriskToken:
            {
                return this.visitExponentiationExpression(node);
            }
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/exponentiation.go:34:1.
    private Node visitExponentiationAssignmentExpression(BinaryExpression node)
    {
        Node target = default;
        Node value = default;
        var left = this.Visitor().VisitNode(node.Left);
        var right = this.Visitor().VisitNode(node.Right);
        if (Ast.IsElementAccessExpression(left))
        {
            var expressionTemp = this.Factory().NewTempVariable();
            this.EmitContext().AddVariableDeclaration(expressionTemp);
            var argumentExpressionTemp = this.Factory().NewTempVariable();
            this.EmitContext().AddVariableDeclaration(argumentExpressionTemp);
            var objExpr = this.Factory().NewAssignmentExpression(expressionTemp, left.Expression());
            objExpr.Loc = left.Expression().Loc;
            var accessExpr = this.Factory().NewAssignmentExpression(argumentExpressionTemp, left.AsElementAccessExpression().ArgumentExpression);
            accessExpr.Loc = left.AsElementAccessExpression().ArgumentExpression.Loc;
            target = this.Factory().NewElementAccessExpression(objExpr, null, accessExpr, NodeFlags.None);
            value = this.Factory().NewElementAccessExpression(expressionTemp, null, argumentExpressionTemp, NodeFlags.None);
            value.Loc = left.Loc;
        }
        else
        {
            if (Ast.IsPropertyAccessExpression(left))
            {
                var expressionTemp2 = this.Factory().NewTempVariable();
                this.EmitContext().AddVariableDeclaration(expressionTemp2);
                var assignment = this.Factory().NewAssignmentExpression(expressionTemp2, left.Expression());
                assignment.Loc = left.Expression().Loc;
                target = this.Factory().NewPropertyAccessExpression(assignment, null, left.Name(), NodeFlags.None);
                target.Loc = left.Loc;
                value = this.Factory().NewPropertyAccessExpression(expressionTemp2, null, left.Name(), NodeFlags.None);
                value.Loc = left.Loc;
            }
            else
            {
                target = left;
                value = left;
            }
        }

        var rhs = this.Factory().NewGlobalMethodCall("Math", "pow", Slice<Node>.FromArray([value, right]));
        rhs.Loc = node.Loc;
        var result = this.Factory().NewAssignmentExpression(target, rhs);
        result.Loc = node.Loc;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/exponentiation.go:79:1.
    private Node visitExponentiationExpression(BinaryExpression node)
    {
        var left = this.Visitor().VisitNode(node.Left);
        var right = this.Visitor().VisitNode(node.Right);
        var result = this.Factory().NewGlobalMethodCall("Math", "pow", Slice<Node>.FromArray([left, right]));
        result.Loc = node.Loc;
        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/exponentiation.go:87:1.
    internal static Transformer NewExponentiationTransformer(TransformOptions opts)
    {
        var tx = new ExponentiationTransformer
        {
        };
        return tx.NewTransformer(tx.visit, opts.Context);
    }
}

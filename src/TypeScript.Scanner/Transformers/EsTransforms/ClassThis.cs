// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete classthis.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
namespace TypeScript.Transformers.EsTransforms;
internal static partial class EsTransforms
{
    // Gets whether a node is a `static {}` block containing only a single assignment of the static `this` to the `_classThis`
    // (or similar) variable stored in the `classthis` property of the block's `EmitNode`.
    // Upstream tsc/internal/transformers/estransforms/classthis.go:10:1.
    internal static bool IsClassThisAssignmentBlock(EmitContext emitContext, Node node)
    {
        if (Ast.IsClassStaticBlockDeclaration(node))
        {
            var n = node.AsClassStaticBlockDeclaration();
            var body = n.Body.AsBlock();
            if ((GoRuntime.Len(body.Statements.Nodes) == 1))
            {
                var statement = body.Statements.Nodes[0];
                if (Ast.IsExpressionStatement(statement))
                {
                    var expression = statement.Expression();
                    if (Ast.IsAssignmentExpression(expression, true))
                    {
                        var binary = expression.AsBinaryExpression();
                        return ((Ast.IsIdentifier(binary.Left) && (emitContext.ClassThis(node) == binary.Left)) && (binary.Right.Kind == Kind.ThisKeyword));
                    }
                }
            }
        }
        return false;
    }

}

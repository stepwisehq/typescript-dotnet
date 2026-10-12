// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of estransforms/optionalcatch.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;

namespace TypeScript.Transformers.EsTransforms;
internal sealed class OptionalCatchTransformer : Transformer
{
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalcatch.go:12:1.
    private Node visit(Node node)
    {
        if ((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsMissingCatchClauseVariable) == 0))
        {
            return node;
        }

        switch (node.Kind)
        {
            case Kind.CatchClause:
            {
                return this.visitCatchClause(node.AsCatchClause());
            }

            default:
            {
                return this.Visitor().VisitEachChild(node);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalcatch.go:24:1.
    private Node visitCatchClause(CatchClause node)
    {
        if ((node.VariableDeclaration == null))
        {
            return this.Factory().NewCatchClause(this.Factory().NewVariableDeclaration(this.Factory().NewTempVariable(), null, null, null), this.Visitor().Visit(node.Block));
        }

        return this.Visitor().VisitEachChild(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/optionalcatch.go:34:1.
    internal static Transformer NewOptionalCatchTransformer(TransformOptions opts)
    {
        var tx = new OptionalCatchTransformer
        {
        };
        return tx.NewTransformer(tx.visit, opts.Context);
    }
}

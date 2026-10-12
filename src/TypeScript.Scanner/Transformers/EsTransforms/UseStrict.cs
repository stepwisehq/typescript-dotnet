// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of estransforms/usestrict.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;

namespace TypeScript.Transformers.EsTransforms;
internal sealed class UseStrictTransformer : Transformer
{
    private CompilerOptions compilerOptions;
    private Func<HasFileName, ModuleKind> getEmitModuleFormatOfFile;
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/usestrict.go:9:1.
    internal static Transformer NewUseStrictTransformer(TransformOptions opts)
    {
        var tx = new UseStrictTransformer
        {
            compilerOptions = opts.CompilerOptions,
            getEmitModuleFormatOfFile = opts.GetEmitModuleFormatOfFile
        };
        return tx.NewTransformer(tx.visit, opts.Context);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/usestrict.go:23:1.
    private Node visit(Node node)
    {
        if ((node.Kind != Kind.SourceFile))
        {
            return node;
        }

        return this.visitSourceFile(node.AsSourceFile());
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/usestrict.go:30:1.
    private Node visitSourceFile(SourceFile node)
    {
        if ((node.ScriptKind == ScriptKind.JSON))
        {
            return node.AsNode();
        }

        var isExternalModule = Ast.IsExternalModule(node);
        var moduleKind = this.compilerOptions.GetEmitModuleKind();
        var format = this.getEmitModuleFormatOfFile(node);
        if (((isExternalModule && (moduleKind >= ModuleKind.ES2015)) && (((moduleKind == ModuleKind.Preserve) || (format >= ModuleKind.ES2015)))))
        {
            return node.AsNode();
        }

        var statements = this.Factory().EnsureUseStrict(node.Statements.Nodes);
        var statementList = this.Factory().NewNodeList(statements);
        statementList.Loc = node.Statements.Loc;
        return this.Factory().UpdateSourceFile(node, statementList, node.EndOfFileToken).AsSourceFile().AsNode();
    }
}

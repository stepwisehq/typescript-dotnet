// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using NodeFactory = TypeScript.DiagnosticPrinting.NodeFactory;

namespace TypeScript.Transformers.ModuleTransforms;
internal sealed class ImpliedModuleTransformer : Transformer
{
    private TransformOptions opts;
    // Pinned embedded owner is assigned but never read by upstream.
#pragma warning disable CS0414
    private IReferenceResolver resolver;
#pragma warning restore CS0414
    private Func<HasFileName, ModuleKind> getEmitModuleFormatOfFile;
    private Transformer cjsTransformer, esmTransformer;
    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/impliedmodule.go:19:1.
    public static Transformer NewImpliedModuleTransformer(TransformOptions opts)
    {
        var tx = new ImpliedModuleTransformer
        {
            opts = opts,
            resolver = opts.Resolver,
            getEmitModuleFormatOfFile = opts.GetEmitModuleFormatOfFile
        };
        return tx.NewTransformer(tx.visit, opts.Context);
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/impliedmodule.go:24:1.
    private Node visit(Node node)
    {
        switch (node.Kind)
        {
            case Kind.SourceFile:
            {
                node = this.visitSourceFile(node.AsSourceFile());
                break;
            }
        }

        return node;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/moduletransforms/impliedmodule.go:32:1.
    private Node visitSourceFile(SourceFile node)
    {
        if (node.IsDeclarationFile)
        {
            return node.AsNode();
        }

        var format = this.getEmitModuleFormatOfFile(node);
        Transformer transformer = default;
        if ((format >= ModuleKind.ES2015))
        {
            if ((this.esmTransformer == null))
            {
                this.esmTransformer = ESModuleTransformer.NewESModuleTransformer(this.opts);
            }

            transformer = this.esmTransformer;
        }
        else
        {
            if ((this.cjsTransformer == null))
            {
                this.cjsTransformer = CommonJSModuleTransformer.NewCommonJSModuleTransformer(this.opts);
            }

            transformer = this.cjsTransformer;
        }

        return transformer.TransformSourceFile(node).AsNode();
    }
}

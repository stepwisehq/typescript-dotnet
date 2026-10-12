// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/transformers/transformer.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using EmitNodeFactory = TypeScript.DiagnosticPrinting.NodeFactory;
namespace TypeScript.Transformers;
internal class Transformer
{
    protected EmitContext emitContext;
    protected EmitNodeFactory factory;
    protected NodeVisitor visitor;
    internal Transformer NewTransformer(Func<Node, Node> visit, EmitContext context)
    {
        if (emitContext != null) throw new InvalidOperationException("Transformer already initialized");
        context ??= new(); emitContext = context; factory = context.Factory;
        visitor = context.NewNodeVisitor(visit); return this;
    }
    internal EmitContext EmitContext() => emitContext;
    internal NodeVisitor Visitor() => visitor;
    internal EmitNodeFactory Factory() => factory;
    internal SourceFile TransformSourceFile(SourceFile file) => visitor.VisitSourceFile(file);
}

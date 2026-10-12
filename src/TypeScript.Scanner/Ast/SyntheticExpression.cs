// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/ast/ast_generated.go:6264-6296,
// upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Handwritten selected checker-created payload supplement to the syntax generator.
#nullable disable
namespace TypeScript.Scanner;
public sealed class SyntheticExpression : NodeData, ExpressionBase
{
    public new object Type { get; set; }
    public bool IsSpread { get; set; }
    public Node TupleNameSource { get; set; }

    public override bool ForEachChild(Visitor visitor) => Ast.Visit(visitor, TupleNameSource);
    public override Node VisitEachChild(NodeVisitor visitor) => visitor.Factory.UpdateSyntheticExpression(this, Type, IsSpread, visitor.VisitNode(TupleNameSource));
    public override IEnumerable<Node> IterChildren()
    {
        if (TupleNameSource != null)
            yield return TupleNameSource;
    }

    public override Node Clone(NodeFactoryCoercible factory) => Ast.CloneNode(factory.AsNodeFactory().NewSyntheticExpression(Type, IsSpread, TupleNameSource), AsNode(), factory.AsNodeFactory().hooks);
    public override string DataType() => "SyntheticExpression";
    public override IEnumerable<SyntaxField> SyntaxFields()
    {
        // Type is checker state rather than syntax and remains directly accessible.
        yield return new("IsSpread", "bool", IsSpread);
        yield return new("TupleNameSource", "node", TupleNameSource);
    }
}

public partial class NodeFactory
{
    public Node NewSyntheticExpression(object typeNode, bool isSpread, Node tupleNameSource) => newNode(Kind.SyntheticExpression, new SyntheticExpression { Type = typeNode, IsSpread = isSpread, TupleNameSource = tupleNameSource });
    public Node UpdateSyntheticExpression(SyntheticExpression node, object typeNode, bool isSpread, Node tupleNameSource)
    {
        if (!Equals(typeNode, node.Type) || isSpread != node.IsSpread || tupleNameSource != node.TupleNameSource)
            return Ast.UpdateNode(NewSyntheticExpression(typeNode, isSpread, tupleNameSource), node.AsNode(), hooks);
        return node.AsNode();
    }
}

public partial class Node
{
    public SyntheticExpression AsSyntheticExpression() => this as SyntheticExpression ?? (SyntheticExpression)data;
}

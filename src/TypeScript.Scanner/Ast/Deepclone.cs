// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/deepclone.go.
#nullable disable
namespace TypeScript.Scanner;
public partial class NodeFactory
{
    private NodeVisitor getDeepCloneVisitor(bool syntheticLocation)
    {
        NodeVisitor visitor = null;
        visitor = new NodeVisitor(node =>
        {
            var visited = visitor.VisitEachChild(node);
            if (visited != node)
            {
                if (syntheticLocation)
                    visited.Loc = new(-1, -1);
                return visited;
            }

            var clone = node.Clone(this);
            if (syntheticLocation)
                clone.Loc = new(-1, -1);
            return clone;
        }, this, new NodeVisitorHooks { VisitNodes = (nodes, v) =>
        {
            if (nodes is null)
                return null;
            var visited = v.VisitNodes(nodes);
            var list = visited != nodes ? visited : nodes.Clone(v.Factory);
            if (syntheticLocation)
            {
                list.Loc = new(-1, -1);
                if (nodes.HasTrailingComma())
                    list.Nodes[^1].Loc = new(-2, -2);
            }

            return list;
        }, VisitModifiers = (nodes, v) =>
        {
            if (nodes is null)
                return null;
            var visited = v.VisitModifiers(nodes);
            var list = visited != nodes ? visited : nodes.Clone(v.Factory);
            if (syntheticLocation)
            {
                list.Loc = new(-1, -1);
                if (nodes.HasTrailingComma())
                    list.Nodes[^1].Loc = new(-2, -2);
            }

            return list;
        }, });
        return visitor;
    }

    public Node DeepCloneNode(Node node) => getDeepCloneVisitor(true).VisitNode(node);
    public Node DeepCloneReparse(Node node)
    {
        if (node is not null)
        {
            node = getDeepCloneVisitor(false).VisitNode(node);
            Ast.SetParentInChildren(node);
            node.Flags |= NodeFlags.Reparsed;
        }

        return node;
    }

    public ModifierList DeepCloneReparseModifiers(ModifierList modifiers) => getDeepCloneVisitor(false).VisitModifiers(modifiers);
}

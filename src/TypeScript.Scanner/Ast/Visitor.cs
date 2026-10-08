// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/visitor.go.
#nullable disable
namespace TypeScript.Scanner;
public sealed class NodeVisitorHooks
{
    public Func<Node, NodeVisitor, Node> VisitNode, VisitToken, VisitEmbeddedStatement, VisitIterationBody, VisitFunctionBody;
    public Func<NodeList, NodeVisitor, NodeList> VisitNodes, VisitParameters, VisitTopLevelStatements;
    public Func<ModifierList, NodeVisitor, ModifierList> VisitModifiers;
}

public sealed class NodeVisitor
{
    public Func<Node, Node> Visit;
    public NodeFactory Factory;
    public NodeVisitorHooks Hooks;
    public NodeVisitor(Func<Node, Node> visit, NodeFactory factory, NodeVisitorHooks hooks = null)
    {
        Visit = visit;
        Factory = factory ?? new();
        Hooks = hooks ?? new();
    }

    public SourceFile VisitSourceFile(SourceFile node) => VisitNode(node.AsNode()).AsSourceFile();
    public Node VisitNode(Node node)
    {
        if (node is null || Visit is null)
            return node;
        var visited = Visit(node);
        if (visited is not null && visited.Kind == Kind.SyntaxList)
        {
            var nodes = visited.AsSyntaxList().Children;
            if (nodes?.Count != 1)
                throw new InvalidOperationException("Expected only a single node to be written to output");
            visited = nodes[0];
            if (visited is not null && visited.Kind == Kind.SyntaxList)
                throw new InvalidOperationException("The result of visiting and lifting a Node may not be SyntaxList");
        }

        return visited;
    }

    public Node VisitEmbeddedStatement(Node node)
    {
        if (node is null || Visit is null)
            return node;
        var visited = Visit(node);
        return visited is null ? null : liftToBlock(visited);
    }

    public NodeList VisitNodes(NodeList nodes)
    {
        if (nodes is null || Visit is null)
            return nodes;
        var(result, changed) = VisitSlice(nodes.Nodes);
        if (!changed)
            return nodes;
        var list = Factory.NewNodeList(result);
        list.Loc = nodes.Loc;
        return list;
    }

    public ModifierList VisitModifiers(ModifierList nodes)
    {
        if (nodes is null || Visit is null)
            return nodes;
        var(result, changed) = VisitSlice(nodes.Nodes);
        if (!changed)
            return nodes;
        var list = Factory.NewModifierList(result);
        list.Loc = nodes.Loc;
        return list;
    }

    public (List<Node> Result, bool Changed) VisitSlice(List<Node> nodes)
    {
        if (nodes is null || Visit is null)
            return (nodes, false);
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (Visit is null)
                break;
            var visited = Visit(node);
            if (visited is null || visited != node)
            {
                var updated = nodes.GetRange(0, i);
                while (true)
                {
                    if (visited is not null)
                    {
                        if (visited.Kind == Kind.SyntaxList)
                            updated.AddRange(visited.AsSyntaxList().Children ?? []);
                        else
                            updated.Add(visited);
                    }

                    i++;
                    if (i >= nodes.Count)
                        break;
                    if (Visit is not null)
                    {
                        node = nodes[i];
                        visited = Visit(node);
                    }
                    else
                    {
                        updated.AddRange(nodes.GetRange(i, nodes.Count - i));
                        break;
                    }
                }

                return (updated, true);
            }
        }

        return (nodes, false);
    }

    public Node VisitEachChild(Node node) => node is null || Visit is null ? node : node.VisitEachChild(this);
    internal Node visitNode(Node node) => Hooks.VisitNode is not null ? Hooks.VisitNode(node, this) : VisitNode(node);
    internal Node visitToken(Node node) => Hooks.VisitToken is not null ? Hooks.VisitToken(node, this) : VisitNode(node);
    internal Node visitEmbeddedStatement(Node node) => Hooks.VisitEmbeddedStatement is not null ? Hooks.VisitEmbeddedStatement(node, this) : Hooks.VisitNode is not null ? liftToBlock(Hooks.VisitNode(node, this)) : VisitEmbeddedStatement(node);
    internal Node visitIterationBody(Node node) => Hooks.VisitIterationBody is not null ? Hooks.VisitIterationBody(node, this) : visitEmbeddedStatement(node);
    internal Node visitFunctionBody(Node node) => Hooks.VisitFunctionBody is not null ? Hooks.VisitFunctionBody(node, this) : visitNode(node);
    internal NodeList visitNodes(NodeList nodes) => Hooks.VisitNodes is not null ? Hooks.VisitNodes(nodes, this) : VisitNodes(nodes);
    internal ModifierList visitModifiers(ModifierList nodes) => Hooks.VisitModifiers is not null ? Hooks.VisitModifiers(nodes, this) : VisitModifiers(nodes);
    internal NodeList visitParameters(NodeList nodes) => Hooks.VisitParameters is not null ? Hooks.VisitParameters(nodes, this) : visitNodes(nodes);
    internal NodeList visitTopLevelStatements(NodeList nodes) => Hooks.VisitTopLevelStatements is not null ? Hooks.VisitTopLevelStatements(nodes, this) : visitNodes(nodes);
    private Node liftToBlock(Node node)
    {
        List<Node> nodes = null;
        if (node is not null)
            nodes = node.Kind == Kind.SyntaxList ? node.AsSyntaxList().Children : [node];
        node = nodes?.Count == 1 ? nodes[0] : Factory.NewBlock(Factory.NewNodeList(nodes), true);
        if (node.Kind == Kind.SyntaxList)
            throw new InvalidOperationException("The result of visiting and lifting a Node may not be SyntaxList");
        return node;
    }
}

public static partial class Ast
{
    public static NodeVisitor NewNodeVisitor(Func<Node, Node> visit, NodeFactory factory, NodeVisitorHooks hooks) => new(visit, factory, hooks);
}

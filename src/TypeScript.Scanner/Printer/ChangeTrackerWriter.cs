// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/printer/changetrackerwriter.go.
#nullable disable
using TypeScript.Scanner;
using ByteScanner = TypeScript.Scanner.Scanner;
namespace TypeScript.DiagnosticPrinting;
internal sealed class ChangeTrackerWriter : TextWriter
{
    private int lastNonTriviaPosition;
    private readonly Dictionary<object, int> pos = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, int> end = new(ReferenceEqualityComparer.Instance);
    internal static ChangeTrackerWriter NewChangeTrackerWriter(string newline, int indentSize)
    {
        var writer = new ChangeTrackerWriter { newLine = newline, indentSize = indentSize < 0 ? defaultIndentSize : indentSize };
        writer.Clear();
        return writer;
    }
    internal PrintHandlers GetPrintHandlers() => new()
    {
        OnBeforeEmitNode = node => { if (node != null) setPos(node); },
        OnAfterEmitNode = node => { if (node != null) setEnd(node); },
        OnBeforeEmitNodeList = nodes => { if (nodes != null) setPos(nodes); },
        OnAfterEmitNodeList = nodes => { if (nodes != null) setEnd(nodes); },
        OnBeforeEmitToken = node => { if (node != null) setPos(node); },
        OnAfterEmitToken = node => { if (node != null) setEnd(node); },
    };
    private void setPos(object node) => pos[node] = lastNonTriviaPosition;
    private void setEnd(object node) => end[node] = lastNonTriviaPosition;
    private int getPos(object node) => pos.GetValueOrDefault(node);
    private int getEnd(object node) => end.GetValueOrDefault(node);
    private void setLastNonTriviaPosition(ByteStringValue text, bool force)
    {
        if (force || (text.IsBytes ? ByteScanner.SkipTriviaByteString(text.ByteView, 0) : ByteScanner.SkipTriviaByteString(text.Materialize(), 0)) != text.Length)
        {
            lastNonTriviaPosition = base.GetTextPos();
            int endPos = text.Length;
            while (endPos > 0)
            {
                var (r, size) = text.IsBytes ? Utf8.DecodeLastRuneInString(text.ByteSpan[..endPos]) : Utf8.DecodeLastRuneInString(text.CharSpan[..endPos]);
                if (!StringUtil.IsWhiteSpaceLike(r)) break;
                endPos -= size;
            }
            lastNonTriviaPosition -= text.Length - endPos;
        }
    }
    internal Node AssignPositionsToNode(Node node, NodeFactory factory)
    {
        NodeVisitor visitor = null;
        visitor = new NodeVisitor(n => assignPositionsToNodeWorker(n, visitor), factory, new NodeVisitorHooks
        {
            VisitNode = assignPositionsToNodeWorker,
            VisitNodes = assignPositionsToNodeArray,
            VisitToken = assignPositionsToNodeWorker,
            VisitModifiers = (modifiers, v) => modifiers == null ? null : factory.NewModifierList(assignPositionsToNodeArray(modifiers, v).Nodes),
        });
        return assignPositionsToNodeWorker(node, visitor);
    }
    private Node assignPositionsToNodeWorker(Node node, NodeVisitor visitor)
    {
        if (node == null) return node;
        var visited = node.VisitEachChild(visitor);
        var newNode = ReferenceEquals(visited, node) ? visited.Clone(visitor.Factory) : visited;
        newNode.ForEachChild(child => { child.Parent = newNode; return true; });
        newNode.Loc = new TextRange(getPos(node), getEnd(node));
        return newNode;
    }
    private NodeList assignPositionsToNodeArray(NodeList nodes, NodeVisitor visitor)
    {
        var visited = visitor.VisitNodes(nodes);
        if (visited == null) return null;
        if (nodes == null) throw new InvalidOperationException("if nodes is nil, visited should not be nil");
        var nodeArray = ReferenceEquals(visited, nodes) ? visited.Clone(visitor.Factory) : visited;
        nodeArray.Loc = new TextRange(getPos(nodes), getEnd(nodes));
        return nodeArray;
    }
    public override void Clear() { base.Clear(); lastNonTriviaPosition = 0; }
    public override void Write(ByteStringValue text) { base.Write(text); setLastNonTriviaPosition(text, false); }
    public override void RawWrite(ByteStringValue text) { base.RawWrite(text); setLastNonTriviaPosition(text, false); }
    public override void WriteLiteral(ByteStringValue text) { base.Write(text); setLastNonTriviaPosition(text, true); }
    public override void WriteComment(string text) => base.WriteComment(text);
}

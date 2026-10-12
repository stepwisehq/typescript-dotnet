// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ls/selectionranges.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;
using ProtocolRange = TypeScript.Lsp.Protocol.Range;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    private const int maxSelectionRangeDepth = 1000;
    private sealed class SelectionRangeBuilder(int capacity)
    {
        private Slice<ProtocolRange> ranges = Slice<ProtocolRange>.Make(0, capacity);
        private int oldestIndex;
        internal void Push(ProtocolRange range)
        {
            if (ranges.Length < ranges.Capacity)
            {
                ranges = ranges.Append(range);
                return;
            }

            ranges[oldestIndex] = range;
            oldestIndex = (oldestIndex + 1) % ranges.Length;
        }

        internal SelectionRange? Build(SelectionRange? result)
        {
            for (int i = 0; i < ranges.Length; i++)
                result = new SelectionRange
                {
                    Range = ranges[(oldestIndex + i) % ranges.Length],
                    Parent = result
                };
            return result;
        }
    }

    internal (SelectionRangesOrNull Result, Exception? Error) ProvideSelectionRanges(ProtocolRequestContext context, SelectionRangeParams parameters)
    {
        var(_, sourceFile) = getProgramAndFile(parameters.TextDocument.Uri);
        if (sourceFile is null)
            return (new(), null);
        var results = Slice<SelectionRange?>.Make(0, parameters.Positions.Length);
        foreach (var position in parameters.Positions)
        {
            var positions = converters.FromLSPPositionForSourceFile(sourceFile, position, Feature.SelectionRanges);
            if (positions.Length != 1 || !positions[0].Fidelity.IsSingleSegment())
                return (new(), null);
            var result = getSmartSelectionRange(this, positions[0].Script, positions[0].Position);
            if (result is not null)
                results = results.Append(result);
        }

        return (new() { SelectionRanges = results }, null);
    }

    private static Slice<Node> getSelectionChildren(NodeFactory factory, Node node, SourceFile sourceFile)
    {
        if (!Ast.IsMappedTypeNode(node))
            return getChildrenFromNonJSDocNode(node, sourceFile);
        var children = getChildrenFromNonJSDocNode(node, sourceFile);
        if (children.Length < 2)
            return children;
        var open = children[0];
        var close = children[children.Length - 1];
        if (open.Kind != Kind.OpenBraceToken || close.Kind != Kind.CloseBraceToken)
            return children;
        var mapped = node.AsMappedTypeNode();
        children = children.Slice2(1, children.Length - 1);
        var groupedSigns = groupChildren(factory, children, child => child == mapped.ReadonlyToken || child.Kind == Kind.ReadonlyKeyword || child == mapped.QuestionToken || child.Kind == Kind.QuestionToken);
        var groupedBrackets = groupChildren(factory, groupedSigns, static child => child.Kind == Kind.OpenBracketToken || child.Kind == Kind.TypeParameter || child.Kind == Kind.CloseBracketToken);
        return Slice<Node>.FromArray([open, createSyntaxList(factory, splitChildren(factory, groupedBrackets, static child => child.Kind == Kind.ColonToken, false)), close]);
    }

    private static Slice<Node> groupChildren(NodeFactory factory, Slice<Node> children, Func<Node, bool> groupOn)
    {
        Slice<Node> result = default, group = default;
        foreach (var child in children)
        {
            if (groupOn(child))
                group = group.Append(child);
            else
            {
                if (group.Length > 0)
                {
                    result = result.Append(createSyntaxList(factory, group));
                    group = default;
                }

                result = result.Append(child);
            }
        }

        if (group.Length > 0)
            result = result.Append(createSyntaxList(factory, group));
        return result;
    }

    private static Slice<Node> splitChildren(NodeFactory factory, Slice<Node> children, Func<Node, bool> pivotOn, bool separateTrailingSemicolon)
    {
        if (children.Length < 2)
            return children;
        int split = -1;
        for (int i = 0; i < children.Length; i++)
            if (pivotOn(children[i]))
            {
                split = i;
                break;
            }

        if (split == -1)
            return children;
        var left = children.Slice2(0, split);
        var token = children[split];
        var last = children[children.Length - 1];
        bool separate = separateTrailingSemicolon && last.Kind == Kind.SemicolonToken;
        int end = children.Length;
        if (separate)
            end--;
        var right = children.Slice2(split + 1, end);
        var result = Slice<Node>.Make(0, 4);
        if (left.Length > 0)
            result = result.Append(createSyntaxList(factory, left));
        result = result.Append(token);
        if (right.Length > 0)
            result = result.Append(createSyntaxList(factory, right));
        if (separate)
            result = result.Append(last);
        return result;
    }

    private static Node createSyntaxList(NodeFactory factory, Slice<Node> children)
    {
        var list = factory.NewSyntaxList(children);
        list.Loc = new TextRange(children[0].Pos(), children[children.Length - 1].End());
        return list;
    }

    internal static SelectionRange? getSmartSelectionRange(LanguageService service, SourceFile sourceFile, int pos)
    {
        var factory = new NodeFactory();
        var ranges = new SelectionRangeBuilder(maxSelectionRangeDepth - 1);
        SelectionRange? root = null;
        ProtocolRange lastRange = default;
        if (sourceFile.ContentMapper() == "")
        {
            var(fullRange, _) = service.converters.ToLSPRange(sourceFile, new TextRange(sourceFile.Pos(), sourceFile.End()));
            root = new SelectionRange
            {
                Range = fullRange
            };
            lastRange = fullRange;
        }

        bool NodeContainsPosition(Node? node)
        {
            if (node is null)
                return false;
            int start = ScannerApi.GetTokenPosOfNode(node, sourceFile, true);
            return start <= pos && pos < node.End();
        }

        bool PositionShouldSnapToNode(Node node)
        {
            if (pos < node.End())
                return true;
            if (node.End() == pos)
            {
                var touching = AstNav.GetTouchingPropertyName(sourceFile, pos);
                return touching is not null && touching.Pos() < node.End();
            }

            return false;
        }

        void PushSelectionRange(int start, int end)
        {
            if (start == end || !(start <= pos && pos <= end))
                return;
            var(range, fidelity) = service.converters.ToLSPRangeForFeature(sourceFile, new TextRange(start, end), Feature.SelectionRanges);
            if (fidelity.IsNone())
                return;
            if (lastRange.Start.Line == range.Start.Line && lastRange.Start.Character == range.Start.Character && lastRange.End.Line == range.End.Line && lastRange.End.Character == range.End.Character)
                return;
            lastRange = range;
            ranges.Push(range);
        }

        void PushSelectionCommentRange(int start, int end)
        {
            PushSelectionRange(start, end);
            int commentPos = start;
            var text = sourceFile.TextView();
            while (commentPos < end && commentPos < text.Length && text[commentPos] == '/')
                commentPos++;
            PushSelectionRange(commentPos, end);
        }

        bool PositionsAreOnSameLine(int first, int second)
        {
            if (first == second)
                return true;
            var starts = sourceFile.ECMALineMap();
            return ScannerApi.ComputeLineOfPosition(starts.AsSpan(), first) == ScannerApi.ComputeLineOfPosition(starts.AsSpan(), second);
        }

        static bool ShouldSkipNode(Node node, Node? parent)
        {
            if (Ast.IsBlock(node) || Ast.IsTemplateSpan(node) || Ast.IsTemplateHead(node) || Ast.IsTemplateTail(node))
                return true;
            if (parent is not null && Ast.IsVariableDeclarationList(node) && Ast.IsVariableStatement(parent))
                return true;
            if (parent is not null && Ast.IsVariableDeclaration(node) && Ast.IsVariableDeclarationList(parent))
            {
                var declaration = parent.AsVariableDeclarationList();
                if (declaration is not null && declaration.Declarations.Nodes.Length == 1)
                    return true;
            }

            return Ast.IsJSDocTypeExpression(node) || Ast.IsJSDocSignature(node) || Ast.IsJSDocTypeLiteral(node);
        }

        for (Node? current = sourceFile.AsNode(); current is not null;)
        {
            Node? next = null;
            var parent = current;
            Node? Visit(Node? node)
            {
                if (node is not null && next is null)
                {
                    CommentRange? found = null;
                    foreach (var comment in ScannerApi.GetTrailingCommentRangesValue(factory, sourceFile.TextView(), node.End()))
                    {
                        found = comment;
                        break;
                    }

                    if (found is { } trailing && trailing.Kind == Kind.SingleLineCommentTrivia)
                        PushSelectionCommentRange(trailing.Pos, trailing.End);
                    if (NodeContainsPosition(node))
                    {
                        if (Ast.IsBlock(node) && Ast.IsFunctionLikeDeclaration(parent) && !PositionsAreOnSameLine(AstNav.GetStartOfNode(node, sourceFile, false), node.End()))
                            PushSelectionRange(AstNav.GetStartOfNode(node, sourceFile, false), node.End());
                        if (Ast.IsTemplateSpan(parent))
                        {
                            var span = parent.AsTemplateSpan();
                            if (span.Literal is not null)
                            {
                                int start = node.Pos() - 2, end = AstNav.GetStartOfNode(span.Literal, sourceFile, false) + 1;
                                if (start >= 0 && end <= sourceFile.TextView().Length && start < end)
                                    PushSelectionRange(start, end);
                            }
                        }

                        if (!ShouldSkipNode(node, parent))
                        {
                            int start = AstNav.GetStartOfNode(node, sourceFile, false), end = node.End();
                            PushSelectionRange(start, end);
                            if (Ast.IsMappedTypeNode(node))
                            {
                                for (var selectionParent = node;;)
                                {
                                    Node? childFound = null;
                                    foreach (var child in getSelectionChildren(factory, selectionParent, sourceFile))
                                    {
                                        int childStart = ScannerApi.GetTokenPosOfNode(child, sourceFile, true);
                                        if (childStart > pos)
                                            break;
                                        if (PositionShouldSnapToNode(child))
                                        {
                                            PushSelectionRange(childStart, child.End());
                                            childFound = child;
                                            break;
                                        }
                                    }

                                    if (childFound is null || !Ast.IsSyntaxList(childFound))
                                        break;
                                    selectionParent = childFound;
                                }
                            }

                            if ((Ast.IsStringLiteral(node) || node.Kind == Kind.TemplateExpression || node.Kind == Kind.NoSubstitutionTemplateLiteral) && start + 1 < end - 1)
                                PushSelectionRange(start + 1, end - 1);
                        }

                        next = node;
                    }
                }

                return node;
            }

            NodeList? VisitNodes(NodeList? nodes, NodeVisitor visitor)
            {
                if (nodes is not null && nodes.Nodes.Length > 0)
                {
                    bool skipList = parent is not null && (Ast.IsVariableDeclarationList(parent) || Ast.IsTemplateExpression(parent));
                    if (!skipList)
                    {
                        int start = AstNav.GetStartOfNode(nodes.Nodes[0], sourceFile, false), end = nodes.Nodes[nodes.Nodes.Length - 1].End();
                        if (start <= pos && pos < end)
                            PushSelectionRange(start, end);
                    }
                }

                return visitor.VisitNodes(nodes!);
            }

            foreach (var jsdoc in current.JSDoc(sourceFile))
                Visit(jsdoc);
            var visitor = new NodeVisitor(node => Visit(node)!, null!, new NodeVisitorHooks { VisitNodes = (nodes, owner) => VisitNodes(nodes, owner)! });
            current.VisitEachChild(visitor);
            current = next;
        }

        return ranges.Build(root);
    }
}

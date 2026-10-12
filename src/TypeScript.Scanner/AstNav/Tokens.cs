// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned internal/astnav/tokens.go counterpart.
#nullable disable
using TypeScript.Scanner;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.AstNavigation;
public static class AstNav
{
    // tokens.go:329. Binary-search ordering constants.
    private const int comparisonLessThan = -1, comparisonEqualTo = 0, comparisonGreaterThan = 1;
    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:12:1.
    private static bool shouldRescanLessThanLessThanToken(TypeScript.Scanner.Scanner s, Node containingNode, Kind token)
    {
        return ((token == Kind.LessThanLessThanToken) && Ast.IsJsxChild(containingNode));
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:16:1.
    private static Kind scanNavigationToken(TypeScript.Scanner.Scanner s, Node containingNode)
    {
        var token = s.Token();
        if (shouldRescanLessThanLessThanToken(s, containingNode, token))
        {
            return s.ReScanJsxToken(true);
        }

        return token;
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:24:1.
    public static Node GetTouchingPropertyName(SourceFile sourceFile, int position)
    {
        return getTokenAtPosition(sourceFile, position, false, (node) =>
        {
            return ((Ast.IsPropertyNameLiteral(node) || Ast.IsKeywordKind(node.Kind)) || Ast.IsPrivateIdentifier(node));
        });
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:30:1.
    public static Node GetTouchingToken(SourceFile sourceFile, int position)
    {
        return getTokenAtPosition(sourceFile, position, false, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:34:1.
    public static Node GetTokenAtPosition(SourceFile sourceFile, int position)
    {
        return getTokenAtPosition(sourceFile, position, true, null);
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:38:1.
    private static Node getTokenAtPosition(SourceFile sourceFile, int position, bool allowPositionInLeadingTrivia, Func<Node, bool> includePrecedingTokenAtEndPosition)
    {
        Node next = default;
        Node prevSubtree = default;
        var current = sourceFile.AsNode();
        var left = 0;
        Node nodeAfterLeft = default;
        Func<Node, Node> getIncludedPrecedingToken = (subtree) =>
        {
            var child = FindPrecedingTokenEx(sourceFile, position, subtree, false);
            if ((((child != null) && (child.End() == position)) && includePrecedingTokenAtEndPosition(child)))
            {
                return child;
            }

            return null;
        };
        Func<Node, int> testNode = (node2) =>
        {
            if (((((node2.Kind != Kind.EndOfFile) && (node2.End() == position)) && (includePrecedingTokenAtEndPosition != null)) && ((node2.Flags & NodeFlags.Reparsed) == 0)))
            {
                if (((prevSubtree != null) && (getIncludedPrecedingToken(prevSubtree) != null)))
                {
                    return 0;
                }

                prevSubtree = node2;
            }

            if (((node2.End() < position) || (((node2.End() == position) && (node2.Kind != Kind.EndOfFile)) && ((!Ast.IsJSDocKind(node2.Kind) || (node2.End() != sourceFile.EndOfFileToken.End()))))))
            {
                return -1;
            }

            var nodePos = getPosition(node2, sourceFile, allowPositionInLeadingTrivia);
            if ((nodePos > position))
            {
                return 1;
            }

            return 0;
        };
        Func<Node, NodeVisitor, Node> visitNode = (node3, _) =>
        {
            if (((node3 == null) || ((node3.Flags & NodeFlags.Reparsed) != 0)))
            {
                return null;
            }

            if ((nodeAfterLeft == null))
            {
                nodeAfterLeft = node3;
            }

            if ((next == null))
            {
                var result = testNode(node3);
                switch (result)
                {
                    case -1:
                    {
                        if (!Ast.IsJSDocKind(node3.Kind))
                        {
                            left = node3.End();
                        }

                        nodeAfterLeft = null;
                        break;
                    }

                    case 0:
                    {
                        next = node3;
                        break;
                    }
                }
            }

            return node3;
        };
        Func<NodeList, NodeVisitor, NodeList> visitNodeList = (nodeList, _2) =>
        {
            if (((nodeList == null) || (GoRuntime.Len(nodeList.Nodes) == 0)))
            {
                return nodeList;
            }

            if ((nodeAfterLeft == null))
            {
                {
                    for (int range1Index = 0; range1Index < nodeList.Nodes.Count; range1Index++)
                    {
                        var range1 = new KeyValuePair<int, Node>(range1Index, nodeList.Nodes[range1Index]);
                        var _3 = range1.Key;
                        var node4 = range1.Value;
                        if (((node4.Flags & NodeFlags.Reparsed) == 0))
                        {
                            nodeAfterLeft = node4;
                            break;
                        }
                    }
                }
            }

            if ((next == null))
            {
                if (((nodeList.End() == position) && (includePrecedingTokenAtEndPosition != null)))
                {
                    left = nodeList.End();
                    nodeAfterLeft = null;
                    {
                        for (int range2Index = nodeList.Nodes.Count - 1; range2Index >= 0; range2Index--)
                        {
                            var range2 = new KeyValuePair<int, Node>(range2Index, nodeList.Nodes[range2Index]);
                            var _4 = range2.Key;
                            var v = range2.Value;
                            if (((v.Flags & NodeFlags.Reparsed) == 0))
                            {
                                prevSubtree = v;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if ((nodeList.End() <= position))
                    {
                        left = nodeList.End();
                        nodeAfterLeft = null;
                    }
                    else
                    {
                        if ((nodeList.Pos() <= position))
                        {
                            var nodes = nodeList.Nodes;
                            var tuple3 = Core.BinarySearchUniqueFunc(nodes, (middle, node5) =>
                            {
                                if (((node5.Flags & NodeFlags.Reparsed) != 0))
                                {
                                    return 0;
                                }

                                var cmp = testNode(node5);
                                if ((cmp < 0))
                                {
                                    left = node5.End();
                                    nodeAfterLeft = null;
                                    {
                                        var i = (middle + 1);
                                        for (; (i < GoRuntime.Len(nodes)); i++)
                                        {
                                            if (((nodes[i].Flags & NodeFlags.Reparsed) == 0))
                                            {
                                                nodeAfterLeft = nodes[i];
                                                break;
                                            }
                                        }
                                    }
                                }

                                return cmp;
                            });
                            var index = tuple3.Item1;
                            var match = tuple3.Item2;
                            if ((match && ((nodes[index].Flags & NodeFlags.Reparsed) != 0)))
                            {
                                nodes = Core.Filter(nodes, (node6) =>
                                {
                                    return ((node6.Flags & NodeFlags.Reparsed) == 0);
                                });
                                (index, match) = Core.BinarySearchUniqueFunc(nodes, (middle2, node7) =>
                                {
                                    var cmp2 = testNode(node7);
                                    if ((cmp2 < 0))
                                    {
                                        left = node7.End();
                                        if (((middle2 + 1) < GoRuntime.Len(nodes)))
                                        {
                                            nodeAfterLeft = nodes[(middle2 + 1)];
                                        }
                                        else
                                        {
                                            nodeAfterLeft = null;
                                        }
                                    }

                                    return cmp2;
                                });
                            }

                            if (match)
                            {
                                next = nodes[index];
                            }
                        }
                    }
                }
            }

            return nodeList;
        };
        while (true)
        {
            VisitEachChildAndJSDoc(current, sourceFile, visitNode, visitNodeList);
            if ((prevSubtree != null))
            {
                {
                    var child2 = getIncludedPrecedingToken(prevSubtree);
                    if ((child2 != null))
                    {
                        return child2;
                    }
                }

                prevSubtree = null;
            }

            if ((next == null))
            {
                if ((Ast.IsTokenKind(current.Kind) || shouldSkipChild(current)))
                {
                    return current;
                }

                var scanner = ScannerApi.GetScannerForSourceFile(sourceFile, left);
                var end = current.End();
                if ((nodeAfterLeft != null))
                {
                    end = nodeAfterLeft.Pos();
                }

                while ((left < end))
                {
                    var token = scanNavigationToken(scanner, current);
                    var tokenFullStart = scanner.TokenFullStart();
                    var tokenStart = Core.IfElse(allowPositionInLeadingTrivia, tokenFullStart, scanner.TokenStart());
                    var tokenEnd = scanner.TokenEnd();
                    var flags = scanner.TokenFlags();
                    if ((tokenEnd > end))
                    {
                        break;
                    }

                    if (((tokenStart <= position) && ((position < tokenEnd))))
                    {
                        if (((token == Kind.Identifier) || !Ast.IsTokenKind(token)))
                        {
                            if (Ast.IsJSDocKind(current.Kind))
                            {
                                return current;
                            }

                            throw new InvalidOperationException($"did not expect {current.Kind} to have {token} in its trivia");
                        }

                        return sourceFile.GetOrCreateToken(token, tokenFullStart, tokenEnd, current, flags);
                    }

                    if (((includePrecedingTokenAtEndPosition != null) && (tokenEnd == position)))
                    {
                        var prevToken = sourceFile.GetOrCreateToken(token, tokenFullStart, tokenEnd, current, flags);
                        if (includePrecedingTokenAtEndPosition(prevToken))
                        {
                            return prevToken;
                        }
                    }

                    left = tokenEnd;
                    scanner.Scan();
                }

                return current;
            }

            current = next;
            left = current.Pos();
            nodeAfterLeft = null;
            next = null;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:276:1.
    private static int getPosition(Node node, SourceFile sourceFile, bool allowPositionInLeadingTrivia)
    {
        if (allowPositionInLeadingTrivia)
        {
            return node.Pos();
        }

        return ScannerApi.GetTokenPosOfNode(node, sourceFile, true);
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:283:1.
    private static Node findRightmostNode(Node node)
    {
        Node next = default;
        var current = node;
        Func<Node, NodeVisitor, Node> visitNode = (node2, _) =>
        {
            if ((node2 != null))
            {
                next = node2;
            }

            return node2;
        };
        Func<NodeList, NodeVisitor, NodeList> visitNodes = (nodeList, visitor) =>
        {
            if ((nodeList != null))
            {
                {
                    var rightmost = Ast.FindLastVisibleNode(nodeList.Nodes);
                    if ((rightmost != null))
                    {
                        next = rightmost;
                    }
                }
            }

            return nodeList;
        };
        var visitor2 = getNodeVisitor(visitNode, visitNodes);
        while (true)
        {
            current.VisitEachChild(visitor2);
            if ((next == null))
            {
                return current;
            }

            current = next;
            next = null;
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:312:1.
    public static void VisitEachChildAndJSDoc(Node node, SourceFile sourceFile, Func<Node, NodeVisitor, Node> visitNode, Func<NodeList, NodeVisitor, NodeList> visitNodes)
    {
        var visitor = getNodeVisitor(visitNode, visitNodes);
        {
            foreach (var jsdoc in node.JSDoc(sourceFile))
            {
                if ((visitor.Hooks.VisitNode != null))
                {
                    visitor.Hooks.VisitNode(jsdoc, visitor);
                }
                else
                {
                    visitor.VisitNode(jsdoc);
                }
            }
        }

        node.VisitEachChild(visitor);
    }

    // Finds the leftmost token satisfying `position < token.End()`.
    // If the leftmost token satisfying `position < token.End()` is invalid, or if position
    // is in the trivia of that leftmost token,
    // we will find the rightmost valid token with `token.End() <= position`.
    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:339:1.
    public static Node FindPrecedingToken(SourceFile sourceFile, int position)
    {
        return FindPrecedingTokenEx(sourceFile, position, null, false);
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:343:1.
    public static Node FindPrecedingTokenEx(SourceFile sourceFile, int position, Node startNode, bool excludeJSDoc)
    {
        Func<Node, Node> find = default;
        find = (n) =>
        {
            if ((Ast.IsNonWhitespaceToken(n) && (n.Kind != Kind.EndOfFile)))
            {
                return n;
            }

            Node foundChild = default;
            Node prevChild = default;
            Func<Node, NodeVisitor, Node> visitNode = (node2, _) =>
            {
                if (((node2 == null) || ((node2.Flags & NodeFlags.Reparsed) != 0)))
                {
                    return node2;
                }

                if ((foundChild != null))
                {
                    return node2;
                }

                if (((position < node2.End()) && (((prevChild == null) || (prevChild.End() <= position)))))
                {
                    foundChild = node2;
                }
                else
                {
                    prevChild = node2;
                }

                return node2;
            };
            Func<NodeList, NodeVisitor, NodeList> visitNodes = (nodeList, _2) =>
            {
                if ((foundChild != null))
                {
                    return nodeList;
                }

                if (((nodeList != null) && (GoRuntime.Len(nodeList.Nodes) > 0)))
                {
                    var nodes = nodeList.Nodes;
                    var tuple5 = Core.BinarySearchUniqueFunc(nodes, (middle, _3) =>
                    {
                        if (((nodes[middle].Flags & NodeFlags.Reparsed) != 0))
                        {
                            return comparisonLessThan;
                        }

                        if ((position < nodes[middle].End()))
                        {
                            if (((middle == 0) || (position >= nodes[(middle - 1)].End())))
                            {
                                return comparisonEqualTo;
                            }

                            return comparisonGreaterThan;
                        }

                        return comparisonLessThan;
                    });
                    var index = tuple5.Item1;
                    var match = tuple5.Item2;
                    if (match)
                    {
                        foundChild = nodes[index];
                    }

                    var validLookupIndex = Core.IfElse(match, (index - 1), (GoRuntime.Len(nodes) - 1));
                    {
                        var i = validLookupIndex;
                        for (; (i >= 0); i--)
                        {
                            if (((nodes[i].Flags & NodeFlags.Reparsed) != 0))
                            {
                                continue;
                            }

                            if ((prevChild == null))
                            {
                                prevChild = nodes[i];
                            }
                        }
                    }
                }

                return nodeList;
            };
            VisitEachChildAndJSDoc(n, sourceFile, visitNode, visitNodes);
            if ((foundChild != null))
            {
                var start = GetStartOfNode(foundChild, sourceFile, !excludeJSDoc);
                var lookInPreviousChild = ((start >= position) || !isValidPrecedingNode(foundChild, sourceFile));
                if (lookInPreviousChild)
                {
                    if ((position >= foundChild.Pos()))
                    {
                        Node jsDoc = default;
                        var nodeJSDoc = n.JSDoc(sourceFile);
                        {
                            for (int range6Index = nodeJSDoc.Count - 1; range6Index >= 0; range6Index--)
                            {
                                var range6 = new KeyValuePair<int, Node>(range6Index, nodeJSDoc[range6Index]);
                                var _4 = range6.Key;
                                var n2 = range6.Value;
                                if ((n2.Pos() >= foundChild.Pos()))
                                {
                                    jsDoc = n2;
                                    break;
                                }
                            }
                        }

                        if ((jsDoc != null))
                        {
                            if ((!excludeJSDoc && (position < jsDoc.End())))
                            {
                                return find(jsDoc);
                            }
                            else
                            {
                                return findRightmostValidToken(jsDoc.End(), sourceFile, n, position, excludeJSDoc);
                            }
                        }

                        return findRightmostValidToken(foundChild.Pos(), sourceFile, n, -1, excludeJSDoc);
                    }
                    else
                    {
                        return findRightmostValidToken(foundChild.Pos(), sourceFile, n, position, excludeJSDoc);
                    }
                }
                else
                {
                    return find(foundChild);
                }
            }

            if ((position >= n.End()))
            {
                return findRightmostValidToken(n.End(), sourceFile, n, -1, excludeJSDoc);
            }
            else
            {
                return findRightmostValidToken(n.End(), sourceFile, n, position, excludeJSDoc);
            }
        };
        Node node3 = default;
        if ((startNode != null))
        {
            node3 = startNode;
        }
        else
        {
            node3 = sourceFile.AsNode();
        }

        var result = find(node3);
        if (((result != null) && Ast.IsWhitespaceOnlyJsxText(result)))
        {
            throw new InvalidOperationException("Expected result to be a non-whitespace token.");
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:465:1.
    private static bool isValidPrecedingNode(Node node, SourceFile sourceFile)
    {
        if ((node.Kind == Kind.EndOfFile))
        {
            return (GoRuntime.Len(node.JSDoc(sourceFile)) > 0);
        }

        var start = GetStartOfNode(node, sourceFile, false);
        var width = (node.End() - start);
        return !((Ast.IsWhitespaceOnlyJsxText(node) || (width == 0)));
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:474:1.
    public static int GetStartOfNode(Node node, SourceFile file, bool includeJSDoc)
    {
        return ScannerApi.GetTokenPosOfNode(node, file, includeJSDoc);
    }

    // Looks for rightmost valid token in the range [startPos, endPos).
    // If position is >= 0, looks for rightmost valid token that precedes or touches that position.
    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:480:1.
    private static Node findRightmostValidToken(int endPos, SourceFile sourceFile, Node containingNode, int position, bool excludeJSDoc)
    {
        if ((position == -1))
        {
            position = containingNode.End();
        }

        Func<Node, int, Node> find = default;
        find = (n2, endPos3) =>
        {
            if ((n2 == null))
            {
                return null;
            }

            if (Ast.IsNonWhitespaceToken(n2))
            {
                return n2;
            }

            Node rightmostValidNode = default;
            var rightmostVisitedNodes = Slice<Node>.Make(0, 1);
            var hasChildren = false;
            Func<Node, bool> shouldVisitNode = (node) =>
            {
                return !(((((node.Flags & NodeFlags.Reparsed) != 0) || (node.End() > endPos3)) || (GetStartOfNode(node, sourceFile, !excludeJSDoc) >= position)));
            };
            Func<Node, NodeVisitor, Node> visitNode = (node2, _) =>
            {
                if (((node2 == null) || ((node2.Flags & NodeFlags.Reparsed) != 0)))
                {
                    return node2;
                }

                hasChildren = true;
                if (!shouldVisitNode(node2))
                {
                    return node2;
                }

                rightmostVisitedNodes = rightmostVisitedNodes.Append(node2);
                if (isValidPrecedingNode(node2, sourceFile))
                {
                    rightmostValidNode = node2;
                    rightmostVisitedNodes = rightmostVisitedNodes.Slice2(0, 0);
                }

                return node2;
            };
            Func<NodeList, NodeVisitor, NodeList> visitNodes = (nodeList, _2) =>
            {
                if (((nodeList != null) && (GoRuntime.Len(nodeList.Nodes) > 0)))
                {
                    hasChildren = true;
                    var tuple7 = Core.BinarySearchUniqueFunc(nodeList.Nodes, (middle, node3) =>
                    {
                        if ((node3.End() > endPos3))
                        {
                            return comparisonGreaterThan;
                        }

                        return comparisonLessThan;
                    });
                    var index = tuple7.Item1;
                    var validIndex = -1;
                    {
                        var i = (index - 1);
                        for (; (i >= 0); i--)
                        {
                            if (!shouldVisitNode(nodeList.Nodes[i]))
                            {
                                continue;
                            }

                            if (isValidPrecedingNode(nodeList.Nodes[i], sourceFile))
                            {
                                validIndex = i;
                                rightmostValidNode = nodeList.Nodes[i];
                                break;
                            }
                        }
                    }

                    {
                        var i2 = (validIndex + 1);
                        for (; (i2 < index); i2++)
                        {
                            if (!shouldVisitNode(nodeList.Nodes[i2]))
                            {
                                continue;
                            }

                            rightmostVisitedNodes = rightmostVisitedNodes.Append(nodeList.Nodes[i2]);
                        }
                    }
                }

                return nodeList;
            };
            VisitEachChildAndJSDoc(n2, sourceFile, visitNode, visitNodes);
            if (!shouldSkipChild(n2))
            {
                int startPos = default;
                if ((rightmostValidNode != null))
                {
                    startPos = rightmostValidNode.End();
                }
                else
                {
                    startPos = n2.Pos();
                }

                var scanner = ScannerApi.GetScannerForSourceFile(sourceFile, startPos);
                Slice<Node> tokens = default;
                {
                    for (int range8Index = 0; range8Index < rightmostVisitedNodes.Count; range8Index++)
                    {
                        var range8 = new KeyValuePair<int, Node>(range8Index, rightmostVisitedNodes[range8Index]);
                        var _4 = range8.Key;
                        var visitedNode = range8.Value;
                        while ((startPos < Math.Min(visitedNode.Pos(), position)))
                        {
                            var token = scanNavigationToken(scanner, n2);
                            var tokenStart = scanner.TokenStart();
                            if ((tokenStart >= Math.Min(visitedNode.Pos(), position)))
                            {
                                break;
                            }

                            var tokenFullStart = scanner.TokenFullStart();
                            var tokenEnd = scanner.TokenEnd();
                            startPos = tokenEnd;
                            var flags = scanner.TokenFlags();
                            tokens = tokens.Append(sourceFile.GetOrCreateToken(token, tokenFullStart, tokenEnd, n2, flags));
                            scanner.Scan();
                        }

                        startPos = visitedNode.End();
                        scanner.ResetPos(startPos);
                        scanner.Scan();
                    }
                }

                while ((startPos < Math.Min(endPos3, position)))
                {
                    var token2 = scanNavigationToken(scanner, n2);
                    var tokenStart2 = scanner.TokenStart();
                    if ((tokenStart2 >= Math.Min(endPos3, position)))
                    {
                        break;
                    }

                    var tokenFullStart2 = scanner.TokenFullStart();
                    var tokenEnd2 = scanner.TokenEnd();
                    startPos = tokenEnd2;
                    var flags2 = scanner.TokenFlags();
                    tokens = tokens.Append(sourceFile.GetOrCreateToken(token2, tokenFullStart2, tokenEnd2, n2, flags2));
                    scanner.Scan();
                }

                var lastToken = (GoRuntime.Len(tokens) - 1);
                {
                    var i3 = lastToken;
                    for (; (i3 >= 0); i3--)
                    {
                        if (!Ast.IsWhitespaceOnlyJsxText(tokens[i3]))
                        {
                            return tokens[i3];
                        }
                    }
                }
            }

            if (!hasChildren)
            {
                if ((n2 != containingNode))
                {
                    return n2;
                }

                return null;
            }

            if ((rightmostValidNode != null))
            {
                endPos3 = rightmostValidNode.End();
            }

            return find(rightmostValidNode, endPos3);
        };
        return find(containingNode, endPos);
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:622:1.
    public static Node FindNextToken(Node previousToken, Node parent, SourceFile file)
    {
        Func<Node, Node> find = default;
        find = (n2) =>
        {
            if ((Ast.IsTokenKind(n2.Kind) && (n2.Pos() == previousToken.End())))
            {
                return n2;
            }

            Node foundNode = default;
            Func<Node, NodeVisitor, Node> visitNode = (node, _) =>
            {
                if (((((node != null) && ((node.Flags & NodeFlags.Reparsed) == 0)) && (node.Pos() <= previousToken.End())) && (node.End() > previousToken.End())))
                {
                    foundNode = node;
                }

                return node;
            };
            Func<NodeList, NodeVisitor, NodeList> visitNodes = (nodeList, _2) =>
            {
                if ((((nodeList != null) && (GoRuntime.Len(nodeList.Nodes) > 0)) && (foundNode == null)))
                {
                    var nodes = nodeList.Nodes;
                    var tuple9 = Core.BinarySearchUniqueFunc(nodes, (_3, node2) =>
                    {
                        if (((node2.Flags & NodeFlags.Reparsed) != 0))
                        {
                            return comparisonLessThan;
                        }

                        if ((node2.Pos() > previousToken.End()))
                        {
                            return comparisonGreaterThan;
                        }

                        if ((node2.End() <= previousToken.Pos()))
                        {
                            return comparisonLessThan;
                        }

                        return comparisonEqualTo;
                    });
                    var index = tuple9.Item1;
                    var match = tuple9.Item2;
                    if (match)
                    {
                        foundNode = nodes[index];
                    }
                }

                return nodeList;
            };
            VisitEachChildAndJSDoc(n2, file, visitNode, visitNodes);
            if ((foundNode != null))
            {
                return find(foundNode);
            }

            var startPos = previousToken.End();
            if (((startPos >= n2.Pos()) && (startPos < n2.End())))
            {
                var scanner = ScannerApi.GetScannerForSourceFile(file, startPos);
                var token = scanner.Token();
                var tokenFullStart = scanner.TokenFullStart();
                var tokenEnd = scanner.TokenEnd();
                var flags = scanner.TokenFlags();
                if ((tokenFullStart == previousToken.End()))
                {
                    return file.GetOrCreateToken(token, tokenFullStart, tokenEnd, n2, flags);
                }

                throw new InvalidOperationException($"Expected to find next token at {previousToken.End()}, got token {token} at {tokenFullStart}");
            }

            return null;
        };
        return find(parent);
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:691:1.
    private static NodeVisitor getNodeVisitor(Func<Node, NodeVisitor, Node> visitNode, Func<NodeList, NodeVisitor, NodeList> visitNodes)
    {
        Func<Node, NodeVisitor, Node> wrappedVisitNode = default;
        Func<NodeList, NodeVisitor, NodeList> wrappedVisitNodes = default;
        if ((visitNode != null))
        {
            wrappedVisitNode = (n, v) =>
            {
                if (Ast.IsJSDocSingleCommentNodeComment(n))
                {
                    return n;
                }

                return visitNode(n, v);
            };
        }

        if ((visitNodes != null))
        {
            wrappedVisitNodes = (n2, v2) =>
            {
                if (Ast.IsJSDocSingleCommentNodeList(n2))
                {
                    return n2;
                }

                return visitNodes(n2, v2);
            };
        }

        return Ast.NewNodeVisitor(static node => node, null, new NodeVisitorHooks { VisitNode = wrappedVisitNode, VisitToken = wrappedVisitNode, VisitNodes = wrappedVisitNodes, VisitModifiers = (modifiers, visitor) =>
        {
            if ((modifiers != null))
            {
                wrappedVisitNodes(modifiers, visitor);
            }

            return modifiers;
        } });
    }

    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:728:1.
    private static bool shouldSkipChild(Node node)
    {
        return ((((((node.Kind == Kind.JSDoc) || (node.Kind == Kind.JSDocText)) || (node.Kind == Kind.JSDocTypeLiteral)) || (node.Kind == Kind.JSDocSignature)) || Ast.IsJSDocLinkLike(node)) || Ast.IsJSDocTag(node));
    }

    // FindChildOfKind searches for a child node or token of the specified kind within a containing node.
    // This function scans through both AST nodes and intervening tokens to find the first match.
    // Upstream upstream/TypeScript/tsc/internal/astnav/tokens.go:739:1.
    public static Node FindChildOfKind(Node containingNode, Kind kind, SourceFile sourceFile)
    {
        var lastNodePos = containingNode.Pos();
        var scan = ScannerApi.GetScannerForSourceFile(sourceFile, lastNodePos);
        Node foundChild = default;
        Visitor visitNode = (node) =>
        {
            if (((node == null) || ((node.Flags & NodeFlags.Reparsed) != 0)))
            {
                return false;
            }

            var startPos = lastNodePos;
            while ((startPos < node.Pos()))
            {
                var tokenKind = scan.Token();
                var tokenEnd = scan.TokenEnd();
                if ((tokenKind == kind))
                {
                    var tokenFullStart = scan.TokenFullStart();
                    var flags = scan.TokenFlags();
                    foundChild = sourceFile.GetOrCreateToken(tokenKind, tokenFullStart, tokenEnd, containingNode, flags);
                    return true;
                }

                startPos = tokenEnd;
                scan.Scan();
            }

            if ((node.Kind == kind))
            {
                foundChild = node;
                return true;
            }

            lastNodePos = node.End();
            scan.ResetPos(lastNodePos);
            return false;
        };
        Ast.ForEachChildAndJSDoc(containingNode, sourceFile, visitNode);
        if ((foundChild != null))
        {
            return foundChild;
        }

        var startPos2 = lastNodePos;
        while ((startPos2 < containingNode.End()))
        {
            var tokenKind2 = scan.Token();
            var tokenEnd2 = scan.TokenEnd();
            if ((tokenKind2 == kind))
            {
                var tokenFullStart2 = scan.TokenFullStart();
                var flags2 = scan.TokenFlags();
                var token = sourceFile.GetOrCreateToken(tokenKind2, tokenFullStart2, tokenEnd2, containingNode, flags2);
                return token;
            }

            startPos2 = tokenEnd2;
            scan.Scan();
        }

        return null;
    }
}

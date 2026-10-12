// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
#nullable disable
using TypeScript.Scanner;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.LanguageServices.Utilities;
internal static partial class LsUtil
{
    // Replaces last(node.getChildren(sourceFile))
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/children.go:13:1.
    internal static Node GetLastChild(Node node, SourceFile sourceFile)
    {
        var lastChildNode = GetLastVisitedChild(node, sourceFile);
        if ((Ast.IsJSDocSingleCommentNode(node) && (lastChildNode == null)))
        {
            return null;
        }

        int tokenStartPos = default;
        if ((lastChildNode != null))
        {
            tokenStartPos = lastChildNode.End();
        }
        else
        {
            tokenStartPos = node.Pos();
        }

        Node lastToken = default;
        var scanner = ScannerApi.GetScannerForSourceFile(sourceFile, tokenStartPos);
        {
            var startPos = tokenStartPos;
            while ((startPos < node.End()))
            {
                var tokenKind = scanner.Token();
                var tokenFullStart = scanner.TokenFullStart();
                var tokenEnd = scanner.TokenEnd();
                lastToken = sourceFile.GetOrCreateToken(tokenKind, tokenFullStart, tokenEnd, node, scanner.TokenFlags());
                startPos = tokenEnd;
                scanner.Scan();
            }
        }

        return Core.IfElse((lastToken != null), lastToken, lastChildNode);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/children.go:37:1.
    internal static Node GetLastToken(Node node, SourceFile sourceFile)
    {
        if ((node == null))
        {
            return null;
        }

        if ((Ast.IsTokenKind(node.Kind) || Ast.IsIdentifier(node)))
        {
            return null;
        }

        AssertHasRealPosition(node);
        var lastChild = GetLastChild(node, sourceFile);
        if ((lastChild == null))
        {
            return null;
        }

        if ((lastChild.Kind < Kind.FirstNode))
        {
            return lastChild;
        }
        else
        {
            return GetLastToken(lastChild, sourceFile);
        }
    }

    // Gets the last visited child of the given node.
    // NOTE: This doesn't include unvisited tokens; for this, use `getLastChild` or `getLastToken`.
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/children.go:62:1.
    internal static Node GetLastVisitedChild(Node node, SourceFile sourceFile)
    {
        Node lastChild = default;
        Func<Node, NodeVisitor, Node> visitNode = (n, _) =>
        {
            if (((n != null) && ((n.Flags & NodeFlags.Reparsed) == 0)))
            {
                lastChild = n;
            }

            return n;
        };
        Func<NodeList, NodeVisitor, NodeList> visitNodeList = (nodeList, _2) =>
        {
            if (((nodeList != null) && (GoRuntime.Len(nodeList.Nodes) > 0)))
            {
                {
                    for (int index = nodeList.Nodes.Count - 1; index >= 0; index--)
                    {
                        var v = nodeList.Nodes[index];
                        if (((v.Flags & NodeFlags.Reparsed) == 0))
                        {
                            lastChild = v;
                            break;
                        }
                    }
                }
            }

            return nodeList;
        };
        AstNav.VisitEachChildAndJSDoc(node, sourceFile, visitNode, visitNodeList);
        return lastChild;
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/children.go:87:1.
    internal static Node GetFirstToken(Node node, SourceFile sourceFile)
    {
        if ((Ast.IsIdentifier(node) || Ast.IsTokenKind(node.Kind)))
        {
            return null;
        }

        AssertHasRealPosition(node);
        Node firstChild = default;
        node.ForEachChild((n) =>
        {
            if (((n == null) || ((node.Flags & NodeFlags.Reparsed) != 0)))
            {
                return false;
            }

            firstChild = n;
            return true;
        });
        int tokenEndPosition = default;
        if ((firstChild != null))
        {
            tokenEndPosition = firstChild.Pos();
        }
        else
        {
            tokenEndPosition = node.End();
        }

        var scanner = ScannerApi.GetScannerForSourceFile(sourceFile, node.Pos());
        Node firstToken = default;
        if ((node.Pos() < tokenEndPosition))
        {
            var tokenKind = scanner.Token();
            var tokenFullStart = scanner.TokenFullStart();
            var tokenEnd = scanner.TokenEnd();
            firstToken = sourceFile.GetOrCreateToken(tokenKind, tokenFullStart, tokenEnd, node, scanner.TokenFlags());
        }

        if ((firstToken != null))
        {
            return firstToken;
        }

        if ((firstChild == null))
        {
            return null;
        }

        if ((firstChild.Kind < Kind.FirstNode))
        {
            return firstChild;
        }

        return GetFirstToken(firstChild, sourceFile);
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/children.go:128:1.
    internal static void AssertHasRealPosition(Node node)
    {
        if ((Ast.PositionIsSynthesized(node.Pos()) || Ast.PositionIsSynthesized(node.End())))
        {
            throw new InvalidOperationException("Node must have a real position for this operation.");
        }
    }
}

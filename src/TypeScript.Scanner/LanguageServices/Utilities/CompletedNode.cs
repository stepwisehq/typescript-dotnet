// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
#nullable disable
using TypeScript.Scanner;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.LanguageServices.Utilities;
internal static partial class LsUtil
{
    // PositionBelongsToNode returns true if the position belongs to the node.
    // Assumes `candidate.Pos() <= position` holds.
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/completednode.go:12:1.
    internal static bool PositionBelongsToNode(Node candidate, int position, SourceFile file)
    {
        if ((candidate.Pos() > position))
        {
            throw new InvalidOperationException("Expected candidate.pos <= position");
        }

        return ((position < candidate.End()) || !IsCompletedNode(candidate, file));
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/completednode.go:19:1.
    internal static bool IsCompletedNode(Node n, SourceFile sourceFile)
    {
        if (((n == null) || Ast.NodeIsMissing(n)))
        {
            return false;
        }

        switch (n.Kind)
        {
            case Kind.ClassDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.EnumDeclaration:
            case Kind.ObjectLiteralExpression:
            case Kind.ObjectBindingPattern:
            case Kind.TypeLiteral:
            case Kind.Block:
            case Kind.ModuleBlock:
            case Kind.CaseBlock:
            case Kind.NamedImports:
            case Kind.NamedExports:
            {
                return nodeEndsWith(n, Kind.CloseBraceToken, sourceFile);
            }

            case Kind.CatchClause:
            {
                return IsCompletedNode(n.AsCatchClause().Block, sourceFile);
            }

            case Kind.NewExpression:
            {
                if ((n.ArgumentList() == null))
                {
                    return true;
                }

                goto case Kind.CallExpression;
            }

            case Kind.CallExpression:
            case Kind.ParenthesizedExpression:
            case Kind.ParenthesizedType:
            {
                return nodeEndsWith(n, Kind.CloseParenToken, sourceFile);
            }

            case Kind.FunctionType:
            case Kind.ConstructorType:
            {
                return IsCompletedNode(n.Type(), sourceFile);
            }

            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.ConstructSignature:
            case Kind.CallSignature:
            case Kind.ArrowFunction:
            {
                if ((n.Body() != null))
                {
                    return IsCompletedNode(n.Body(), sourceFile);
                }

                if ((n.Type() != null))
                {
                    return IsCompletedNode(n.Type(), sourceFile);
                }

                return hasChildOfKind(n, Kind.CloseParenToken, sourceFile);
            }

            case Kind.ModuleDeclaration:
            {
                return ((n.Body() != null) && IsCompletedNode(n.Body(), sourceFile));
            }

            case Kind.IfStatement:
            {
                if ((n.AsIfStatement().ElseStatement != null))
                {
                    return IsCompletedNode(n.AsIfStatement().ElseStatement, sourceFile);
                }

                return IsCompletedNode(n.AsIfStatement().ThenStatement, sourceFile);
            }

            case Kind.ExpressionStatement:
            {
                return (IsCompletedNode(n.Expression(), sourceFile) || hasChildOfKind(n, Kind.SemicolonToken, sourceFile));
            }

            case Kind.ArrayLiteralExpression:
            case Kind.ArrayBindingPattern:
            case Kind.ElementAccessExpression:
            case Kind.ComputedPropertyName:
            case Kind.TupleType:
            {
                return nodeEndsWith(n, Kind.CloseBracketToken, sourceFile);
            }

            case Kind.IndexSignature:
            {
                if ((n.AsIndexSignatureDeclaration().Type != null))
                {
                    return IsCompletedNode(n.AsIndexSignatureDeclaration().Type, sourceFile);
                }

                return hasChildOfKind(n, Kind.CloseBracketToken, sourceFile);
            }

            case Kind.CaseClause:
            case Kind.DefaultClause:
            {
                return false;
            }

            case Kind.ForStatement:
            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            case Kind.WhileStatement:
            {
                return IsCompletedNode(n.Statement(), sourceFile);
            }

            case Kind.DoStatement:
            {
                if (hasChildOfKind(n, Kind.WhileKeyword, sourceFile))
                {
                    return nodeEndsWith(n, Kind.CloseParenToken, sourceFile);
                }

                return IsCompletedNode(n.Statement(), sourceFile);
            }

            case Kind.TypeQuery:
            {
                return IsCompletedNode(n.AsTypeQueryNode().ExprName, sourceFile);
            }

            case Kind.TypeOfExpression:
            case Kind.DeleteExpression:
            case Kind.VoidExpression:
            case Kind.YieldExpression:
            case Kind.SpreadElement:
            {
                return IsCompletedNode(n.Expression(), sourceFile);
            }

            case Kind.TaggedTemplateExpression:
            {
                return IsCompletedNode(n.AsTaggedTemplateExpression().Template, sourceFile);
            }

            case Kind.TemplateExpression:
            {
                if ((n.AsTemplateExpression().TemplateSpans == null))
                {
                    return false;
                }

                var lastSpan = Core.LastOrNil(n.AsTemplateExpression().TemplateSpans.Nodes);
                return IsCompletedNode(lastSpan, sourceFile);
            }

            case Kind.TemplateSpan:
            {
                return Ast.NodeIsPresent(n.AsTemplateSpan().Literal);
            }

            case Kind.ExportDeclaration:
            case Kind.ImportDeclaration:
            {
                return Ast.NodeIsPresent(n.ModuleSpecifier());
            }

            case Kind.PrefixUnaryExpression:
            {
                return IsCompletedNode(n.AsPrefixUnaryExpression().Operand, sourceFile);
            }

            case Kind.BinaryExpression:
            {
                return IsCompletedNode(n.AsBinaryExpression().Right, sourceFile);
            }

            case Kind.ConditionalExpression:
            {
                return IsCompletedNode(n.AsConditionalExpression().WhenFalse, sourceFile);
            }

            default:
            {
                return true;
            }
        }
    }

    // Checks if node ends with 'expectedLastToken'.
    // If child at position 'length - 1' is 'SemicolonToken' it is skipped and 'expectedLastToken' is compared with child at position 'length - 2'.
    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/completednode.go:162:1.
    private static bool nodeEndsWith(Node n, Kind expectedLastToken, SourceFile sourceFile)
    {
        var lastChildNode = GetLastVisitedChild(n, sourceFile);
        Slice<Node> lastNodeAndTokens = default;
        int tokenStartPos = default;
        if ((lastChildNode != null))
        {
            lastNodeAndTokens = Slice<Node>.FromArray([lastChildNode]);
            tokenStartPos = lastChildNode.End();
        }
        else
        {
            tokenStartPos = n.Pos();
        }

        var scanner = ScannerApi.GetScannerForSourceFile(sourceFile, tokenStartPos);
        {
            var startPos = tokenStartPos;
            while ((startPos < n.End()))
            {
                var tokenKind = scanner.Token();
                var tokenFullStart = scanner.TokenFullStart();
                var tokenEnd = scanner.TokenEnd();
                var token = sourceFile.GetOrCreateToken(tokenKind, tokenFullStart, tokenEnd, n, scanner.TokenFlags());
                lastNodeAndTokens = lastNodeAndTokens.Append(token);
                startPos = tokenEnd;
                scanner.Scan();
            }
        }

        if ((GoRuntime.Len(lastNodeAndTokens) == 0))
        {
            return false;
        }

        var lastChild = lastNodeAndTokens[(GoRuntime.Len(lastNodeAndTokens) - 1)];
        if ((lastChild.Kind == expectedLastToken))
        {
            return true;
        }
        else
        {
            if (((lastChild.Kind == Kind.SemicolonToken) && (GoRuntime.Len(lastNodeAndTokens) > 1)))
            {
                return (lastNodeAndTokens[(GoRuntime.Len(lastNodeAndTokens) - 2)].Kind == expectedLastToken);
            }
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/ls/lsutil/completednode.go:194:1.
    private static bool hasChildOfKind(Node containingNode, Kind kind, SourceFile sourceFile)
    {
        return (AstNav.FindChildOfKind(containingNode, kind, sourceFile) != null);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of upstream/TypeScript/tsc/internal/parser/utilities.go, upstream fed0bf24149f.
#nullable disable
namespace TypeScript.Scanner;
public partial class Parser
{
    // Upstream upstream/TypeScript/tsc/internal/parser/utilities.go:11:1.
    private static LanguageVariant getLanguageVariant(ScriptKind scriptKind)
    {
        switch (scriptKind)
        {
            case ScriptKind.TSX:
            case ScriptKind.JSX:
            case ScriptKind.JS:
            case ScriptKind.JSON:
            {
                return LanguageVariant.JSX;
            }
        }

        return LanguageVariant.Standard;
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/utilities.go:20:1.
    private static bool tokenIsIdentifierOrKeyword(Kind token)
    {
        return (token >= Kind.Identifier);
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/utilities.go:24:1.
    private static bool tokenIsIdentifierOrKeywordOrGreaterThan(Kind token)
    {
        return ((token == Kind.GreaterThanToken) || tokenIsIdentifierOrKeyword(token));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/utilities.go:28:1.
    public static List<CommentRange> GetJSDocCommentRanges(NodeFactory f, List<CommentRange> commentRanges, Node node, string text)
    {
        switch (node.Kind)
        {
            case Kind.Parameter:
            case Kind.TypeParameter:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.ParenthesizedExpression:
            case Kind.VariableDeclaration:
            case Kind.ExportSpecifier:
            {
                {
                    foreach (var commentRange in GoRuntime.RangeKeys(Scanner.GetTrailingCommentRangesByteString(text, node.Pos())))
                    {
                        commentRanges = GoRuntime.Append(commentRanges, commentRange);
                    }
                }

                {
                    foreach (var commentRange2 in GoRuntime.RangeKeys(Scanner.GetLeadingCommentRangesByteString(text, node.Pos())))
                    {
                        commentRanges = GoRuntime.Append(commentRanges, commentRange2);
                    }
                }

                break;
            }

            default:
            {
                {
                    foreach (var commentRange3 in GoRuntime.RangeKeys(Scanner.GetLeadingCommentRangesByteString(text, node.Pos())))
                    {
                        commentRanges = GoRuntime.Append(commentRanges, commentRange3);
                    }
                }

                break;
            }
        }

        return GoSlices.DeleteFunc(commentRanges, (comment) =>
        {
            var commentStart = comment.Pos;
            var commentLen = (comment.End - commentStart);
            return (((((comment.End > node.End()) || (commentLen < 4)) || (text[(commentStart + 1)] != '*')) || (text[(commentStart + 2)] != '*')) || (text[(commentStart + 3)] == '/'));
        });
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/utilities.go:50:1.
    private static bool isKeywordOrPunctuation(Kind token)
    {
        return (Ast.IsKeywordKind(token) || Ast.IsPunctuationKind(token));
    }

    // Upstream upstream/TypeScript/tsc/internal/parser/utilities.go:54:1.
    private static bool isJSDocLikeText(string text)
    {
        return ((((GoRuntime.Len(text) >= 4) && (text[1] == '*')) && (text[2] == '*')) && (text[3] != '/'));
    }
}

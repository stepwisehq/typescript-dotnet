// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;
using TypeScript.AstNavigation;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.LanguageServices.Formatting;
internal static partial class Formatting
{
    // util.go:10.
    internal static bool RangeIsOnOneLine(TextRange range, SourceFile file) => ScannerApi.GetECMALineOfPosition(file, range.Pos()) == ScannerApi.GetECMALineOfPosition(file, range.End());
    // util.go:16. NodeList reference identity decides which delimiter is owned by this list.
    internal static Kind GetOpenTokenForList(Node node, NodeList list)
    {
        switch (node.Kind)
        {
            case Kind.Constructor:
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.ArrowFunction:
            case Kind.CallSignature:
            case Kind.ConstructSignature:
            case Kind.FunctionType:
            case Kind.ConstructorType:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
                if (ReferenceEquals(node.TypeParameterList(), list))
                    return Kind.LessThanToken;
                if (ReferenceEquals(node.ParameterList(), list))
                    return Kind.OpenParenToken;
                break;
            case Kind.CallExpression:
            case Kind.NewExpression:
                if (ReferenceEquals(node.TypeArgumentList(), list))
                    return Kind.LessThanToken;
                if (ReferenceEquals(node.ArgumentList(), list))
                    return Kind.OpenParenToken;
                break;
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
                if (ReferenceEquals(node.TypeParameterList(), list))
                    return Kind.LessThanToken;
                break;
            case Kind.TypeReference:
            case Kind.TaggedTemplateExpression:
            case Kind.TypeQuery:
            case Kind.ExpressionWithTypeArguments:
            case Kind.ImportType:
                if (ReferenceEquals(node.TypeArgumentList(), list))
                    return Kind.LessThanToken;
                break;
            case Kind.TypeLiteral:
                return Kind.OpenBraceToken;
        }

        return Kind.Unknown;
    }

    // util.go:63. Original TODO intentionally does not add square-bracket handling.
    internal static Kind GetCloseTokenForOpenToken(Kind kind) => kind switch
    {
        Kind.OpenParenToken => Kind.CloseParenToken,
        Kind.LessThanToken => Kind.GreaterThanToken,
        Kind.OpenBraceToken => Kind.CloseBraceToken,
        _ => Kind.Unknown
    };
    // util.go:76.
    internal static int GetLineStartPositionForPosition(int position, SourceFile file) => ScannerApi.GetECMALineStarts(file)[ScannerApi.GetECMALineOfPosition(file, position)];
    // util.go:87.
    internal static Node? FindImmediatelyPrecedingTokenOfKind(int end, Kind kind, SourceFile file)
    {
        var token = AstNav.FindPrecedingToken(file, end);
        return token != null && token.Kind == kind && token.End() == end ? token : null;
    }

    // util.go:108.
    internal static Node? FindOutermostNodeWithinListLevel(Node? node)
    {
        var current = node;
        while (current != null && current.Parent != null && current.Parent.End() == node!.End() && !IsListElement(current.Parent, current))
            current = current.Parent;
        return current;
    }

    // util.go:122.
    internal static bool IsListElement(Node parent, Node node)
    {
        switch (parent.Kind)
        {
            case Kind.ClassDeclaration:
            case Kind.InterfaceDeclaration:
                return node.Loc.ContainedBy(parent.MemberList().Loc);
            case Kind.ModuleDeclaration:
                var body = parent.Body();
                return body != null && body.Kind == Kind.ModuleBlock && node.Loc.ContainedBy(body.StatementList().Loc);
            case Kind.SourceFile:
            case Kind.Block:
            case Kind.ModuleBlock:
                return node.Loc.ContainedBy(parent.StatementList().Loc);
            case Kind.CatchClause:
                return node.Loc.ContainedBy(parent.AsCatchClause().Block.StatementList().Loc);
        }

        return false;
    }

    // util.go:138.
    internal static bool IsMemberListElement(Node parent, Node node) => parent.Kind switch
    {
        Kind.ClassDeclaration or Kind.ClassExpression or Kind.InterfaceDeclaration or Kind.EnumDeclaration or Kind.TypeLiteral or Kind.MappedType => node.Loc.ContainedBy(parent.MemberList().Loc),
        _ => false
    };
}

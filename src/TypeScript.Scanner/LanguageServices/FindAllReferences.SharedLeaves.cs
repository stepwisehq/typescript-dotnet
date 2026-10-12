// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Focused shared models/workers from internal/ls/findallreferences.go; full feature owner remains unfinished.
// Pin fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal sealed class RefInfo
{
    // findallreferences.go:48. Preserve independently optional pointer owners.
    internal SourceFile? file;
    internal RootedFilePath fileName;
    internal FileReference? reference;
    internal bool unverified;
}

internal sealed partial class LanguageService
{
    // findallreferences.go:335. Only string-literal ranges strip the original quote bounds.
    private static TextRange getRangeOfNode(Node node, SourceFile? sourceFile, Node? endNode)
    {
        sourceFile ??= Ast.GetSourceFileOfNode(node);
        int start = ScannerApi.GetTokenPosOfNode(node, sourceFile, false);
        int end = (endNode ?? node).End();
        if (Ast.IsStringLiteralLike(node) && end - start > 2)
        {
            if (endNode != null)
                throw new InvalidOperationException("endNode is not nil for stringLiteralLike");
            start++;
            end--;
        }

        if (endNode != null && endNode.Kind == Kind.CaseBlock)
            end = endNode.Pos();
        return new TextRange(start, end);
    }

    // findallreferences.go:286. Upstream's for/switch TODO leaves remain true nil.
    private static Node? getContextNode(Node? node)
    {
        if (node == null)
            return null;
        switch (node.Kind)
        {
            case Kind.VariableDeclaration:
                if (!Ast.IsVariableDeclarationList(node.Parent) || node.Parent.AsVariableDeclarationList().Declarations.Nodes.Length != 1)
                    return node;
                if (Ast.IsVariableStatement(node.Parent.Parent))
                    return node.Parent.Parent;
                if (Ast.IsForInOrOfStatement(node.Parent.Parent))
                    return getContextNode(node.Parent.Parent);
                return node.Parent;
            case Kind.BindingElement:
                return getContextNode(node.Parent.Parent);
            case Kind.ImportSpecifier:
                return node.Parent.Parent.Parent;
            case Kind.ExportSpecifier:
            case Kind.NamespaceImport:
                return node.Parent.Parent;
            case Kind.ImportClause:
            case Kind.NamespaceExport:
                return node.Parent;
            case Kind.BinaryExpression:
                return node.Parent.Kind == Kind.ExpressionStatement ? node.Parent : node;
            case Kind.ForOfStatement:
            case Kind.ForInStatement:
            case Kind.SwitchStatement:
                // !!! not implemented in pinned upstream.
                return null;
            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
                return Ast.IsArrayLiteralOrObjectLiteralDestructuringPattern(node.Parent) ? getContextNode(Ast.FindAncestor(node.Parent, static ancestor => ancestor.Kind == Kind.BinaryExpression || Ast.IsForInOrOfStatement(ancestor))) : node;
            default:
                return node;
        }
    }

    // findallreferences.go:496. Shared declaration-map query worker, not a references response.
    private static (SourceFile File, int Position) getFileAndStartPosFromDeclaration(Node declaration)
    {
        var file = Ast.GetSourceFileOfNode(declaration);
        var name = Ast.GetNameOfDeclaration(declaration) ?? declaration;
        return (file, getRangeOfNode(name, file, null).Pos());
    }
}

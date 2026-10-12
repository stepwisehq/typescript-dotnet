// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/change/delete.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.AstNavigation;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.SpanMap;
using Kind = TypeScript.Scanner.Kind;
using ScannerApi = TypeScript.Scanner.Scanner;
using Debug = TypeScript.Scanner.Debug;

namespace TypeScript.LanguageServices.Change;
internal sealed partial class Tracker
{
    // delete.go:17.
    private static void deleteDeclaration(Tracker t, Dictionary<Node, bool> deletedInLists, SourceFile file, Node node)
    {
        switch (node.Kind)
        {
            case Kind.Parameter:
                var function = node.Parent;
                if (function.Kind == Kind.ArrowFunction && function.AsArrowFunction().Parameters.Nodes.Length == 1 && AstNav.FindChildOfKind(function, Kind.OpenParenToken, file)is null)
                    t.ReplaceTextRangeWithText(file, t.GetAdjustedRange(file, node, node, LeadingTriviaOption.IncludeAll, TrailingTriviaOption.Include), ByteStringValue.FromString("()"));
                else
                    deleteNodeInList(t, deletedInLists, file, node);
                break;
            case Kind.ImportDeclaration:
            case Kind.ImportEqualsDeclaration:
                var imports = file.Imports();
                bool first = imports.Length > 0 && ReferenceEquals(node, imports[0].Parent) || ReferenceEquals(node, Core.Find(file.Statements.Nodes, static s => Ast.IsAnyImportSyntax(s)));
                var leading = first ? LeadingTriviaOption.Exclude : hasJSDocNodes(node) ? LeadingTriviaOption.JSDoc : LeadingTriviaOption.StartLine;
                deleteNode(t, file, node, leading, TrailingTriviaOption.Include);
                break;
            case Kind.BindingElement:
                var pattern = node.Parent;
                bool preserveComma = pattern.Kind == Kind.ArrayBindingPattern && !ReferenceEquals(node, pattern.AsBindingPattern().Elements.Nodes[pattern.AsBindingPattern().Elements.Nodes.Length - 1]);
                if (preserveComma)
                    deleteNode(t, file, node, LeadingTriviaOption.IncludeAll, TrailingTriviaOption.Exclude);
                else
                    deleteNodeInList(t, deletedInLists, file, node);
                break;
            case Kind.VariableDeclaration:
                deleteVariableDeclaration(t, deletedInLists, file, node);
                break;
            case Kind.TypeParameter:
                deleteNodeInList(t, deletedInLists, file, node);
                break;
            case Kind.ImportSpecifier:
                if (node.Parent.AsNamedImports().Elements.Nodes.Length == 1)
                    deleteImportBinding(t, file, node.Parent);
                else
                    deleteNodeInList(t, deletedInLists, file, node);
                break;
            case Kind.NamespaceImport:
                deleteImportBinding(t, file, node);
                break;
            case Kind.SemicolonToken:
                deleteNode(t, file, node, LeadingTriviaOption.IncludeAll, TrailingTriviaOption.Exclude);
                break;
            case Kind.TypeKeyword:
            case Kind.FunctionKeyword:
                deleteNode(t, file, node, LeadingTriviaOption.Exclude, TrailingTriviaOption.Include);
                break;
            case Kind.ClassDeclaration:
            case Kind.FunctionDeclaration:
                deleteNode(t, file, node, hasJSDocNodes(node) ? LeadingTriviaOption.JSDoc : LeadingTriviaOption.StartLine, TrailingTriviaOption.Include);
                break;
            default:
                if (node.Parent is null)
                    deleteNode(t, file, node, LeadingTriviaOption.IncludeAll, TrailingTriviaOption.Include);
                else if (node.Parent.Kind == Kind.ImportClause && ReferenceEquals(node.Parent.AsImportClause().Name(), node))
                    deleteDefaultImport(t, file, node.Parent);
                else if (node.Parent.Kind == Kind.CallExpression && GoSlices.Contains(node.Parent.AsCallExpression().Arguments.Nodes, node))
                    deleteNodeInList(t, deletedInLists, file, node);
                else
                    deleteNode(t, file, node, LeadingTriviaOption.IncludeAll, TrailingTriviaOption.Include);
                break;
        }
    }

    // delete.go:104.
    private static void deleteDefaultImport(Tracker t, SourceFile file, Node importClause)
    {
        var clause = importClause.AsImportClause();
        if (clause.NamedBindings is null)
            deleteNode(t, file, importClause.Parent, LeadingTriviaOption.IncludeAll, TrailingTriviaOption.Include);
        else
        {
            var name = clause.Name();
            int start = AstNav.GetStartOfNode(name, file, false);
            var next = AstNav.GetTokenAtPosition(file, name.End());
            if (next is not null && next.Kind == Kind.CommaToken)
            {
                int end = ScannerApi.SkipTriviaEx(file.TextView(), next.End(), new SkipTriviaOptions { StopAfterLineBreak = false, StopAtComments = true });
                t.ReplaceTextRangeWithText(file, new TextRange(start, end), default);
            }
            else
                deleteNode(t, file, name, LeadingTriviaOption.IncludeAll, TrailingTriviaOption.Include);
        }
    }

    // delete.go:124.
    private static void deleteImportBinding(Tracker t, SourceFile file, Node node)
    {
        var clause = node.Parent.AsImportClause();
        if (clause.Name()is not null)
        {
            var previous = AstNav.GetTokenAtPosition(file, node.Pos() - 1);
            Debug.Assert(previous is not null, "previousToken should not be nil");
            int start = AstNav.GetStartOfNode(previous!, file, false);
            t.ReplaceTextRangeWithText(file, new TextRange(start, node.End()), default);
        }
        else
        {
            var declaration = Ast.FindAncestorKind(node, Kind.ImportDeclaration);
            Debug.Assert(declaration is not null, "importDecl should not be nil");
            deleteNode(t, file, declaration!, LeadingTriviaOption.IncludeAll, TrailingTriviaOption.Include);
        }
    }

    // delete.go:144.
    private static void deleteVariableDeclaration(Tracker t, Dictionary<Node, bool> deletedInLists, SourceFile file, Node node)
    {
        var parent = node.Parent;
        if (parent.Kind == Kind.CatchClause)
        {
            var open = AstNav.FindChildOfKind(parent, Kind.OpenParenToken, file);
            var close = AstNav.FindChildOfKind(parent, Kind.CloseParenToken, file);
            Debug.Assert(open is not null && close is not null, "catch clause should have parens");
            t.DeleteNodeRange(file, open!, close!, LeadingTriviaOption.IncludeAll, TrailingTriviaOption.Include);
            return;
        }

        if (parent.AsVariableDeclarationList().Declarations.Nodes.Length != 1)
        {
            deleteNodeInList(t, deletedInLists, file, node);
            return;
        }

        var grandparent = parent.Parent;
        switch (grandparent.Kind)
        {
            case Kind.ForOfStatement:
            case Kind.ForInStatement:
                t.ReplaceNode(file, node, t.NodeFactory.NewObjectLiteralExpression(t.NodeFactory.NewNodeList(Slice<Node>.Make(0)), false), null);
                break;
            case Kind.ForStatement:
                deleteNode(t, file, parent, LeadingTriviaOption.IncludeAll, TrailingTriviaOption.Include);
                break;
            case Kind.VariableStatement:
                deleteNode(t, file, grandparent, hasJSDocNodes(grandparent) ? LeadingTriviaOption.JSDoc : LeadingTriviaOption.StartLine, TrailingTriviaOption.Include);
                break;
            default:
                throw new InvalidOperationException("Unexpected grandparent kind: " + grandparent.Kind.String());
        }
    }

    // delete.go:183.
    private static void deleteNode(Tracker t, SourceFile file, Node node, LeadingTriviaOption leading, TrailingTriviaOption trailing) => t.ReplaceTextRangeWithText(file, new TextRange(t.getAdjustedStartPosition(file, node, leading, false), t.getAdjustedEndPosition(file, node, trailing)), default);
    // delete.go:189.
    private static void deleteNodeInList(Tracker t, Dictionary<Node, bool> deletedInLists, SourceFile file, Node node)
    {
        var list = Formatting.Formatting.GetContainingList(node, file);
        Debug.Assert(list is not null, "containingList should not be nil");
        int index = GoSlices.Index(list!.Nodes, node);
        Debug.Assert(index != -1, "node should be in containing list");
        if (list.Nodes.Length == 1)
        {
            deleteNode(t, file, node, LeadingTriviaOption.IncludeAll, TrailingTriviaOption.Include);
            return;
        }

        Debug.Assert(!deletedInLists.GetValueOrDefault(node), "Deleting a node twice");
        deletedInLists[node] = true;
        int start = t.startPositionToDeleteNodeInList(file, node);
        int end = index == list.Nodes.Length - 1 ? t.getAdjustedEndPosition(file, node, TrailingTriviaOption.None) : t.endPositionToDeleteNodeInList(file, node, index > 0 ? list.Nodes[index - 1] : null, list.Nodes[index + 1]);
        t.ReplaceTextRangeWithText(file, new TextRange(start, end), default);
    }

    // delete.go:221.
    private int startPositionToDeleteNodeInList(SourceFile file, Node node) => ScannerApi.SkipTriviaEx(file.TextView(), getAdjustedStartPosition(file, node, LeadingTriviaOption.IncludeAll, false), new SkipTriviaOptions { StopAfterLineBreak = false, StopAtComments = true });
    // delete.go:226.
    private int endPositionToDeleteNodeInList(SourceFile file, Node node, Node? previousNode, Node nextNode)
    {
        int end = startPositionToDeleteNodeInList(file, nextNode);
        if (previousNode is null || positionsAreOnSameLine(getAdjustedEndPosition(file, node, TrailingTriviaOption.Include), end, file))
            return end;
        var token = AstNav.FindPrecedingToken(file, AstNav.GetStartOfNode(nextNode, file, false));
        if (isSeparator(node, token))
        {
            var previous = AstNav.FindPrecedingToken(file, AstNav.GetStartOfNode(node, file, false));
            if (isSeparator(previousNode, previous))
            {
                int pos = ScannerApi.SkipTriviaEx(file.TextView(), token!.End(), new SkipTriviaOptions { StopAfterLineBreak = true, StopAtComments = true });
                if (positionsAreOnSameLine(AstNav.GetStartOfNode(previous!, file, false), AstNav.GetStartOfNode(token, file, false), file))
                {
                    if (pos > 0 && StringUtil.IsLineBreak(ByteStringValue.FromView(file.TextView())[pos - 1]))
                        return pos - 1;
                    return pos;
                }

                if (StringUtil.IsLineBreak(ByteStringValue.FromView(file.TextView())[pos]))
                    return pos;
            }
        }

        return end;
    }

    // delete.go:250/255.
    private static bool positionsAreOnSameLine(int first, int second, SourceFile file) => Formatting.Formatting.GetLineStartPositionForPosition(first, file) == Formatting.Formatting.GetLineStartPositionForPosition(second, file);
    private static bool hasJSDocNodes(Node? node) => node is not null && node.JSDoc(null).Length > 0;
}

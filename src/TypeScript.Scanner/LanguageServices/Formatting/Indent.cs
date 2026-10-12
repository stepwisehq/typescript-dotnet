// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using Debug = TypeScript.Scanner.Debug;
using TypeScript.Scanner;
using TypeScript.AstNavigation;
using TypeScript.LanguageServices.Utilities;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.LanguageServices.Formatting;
internal static partial class Formatting
{
    // indent.go:427. Indentation/settings use the pinned 64-bit Go int role; source offsets remain TextPos.
    internal static long GetActualIndentationForListStartLine(NodeList? list, SourceFile file, FormatCodeSettings options)
    {
        if (list == null)
            return -1;
        var(line, character) = ScannerApi.GetECMALineAndByteOffsetOfPosition(file, list.Loc.Pos());
        return FindColumnForFirstNonWhitespaceCharacterInLine(line, character, file, options);
    }

    // indent.go:435.
    internal static long DeriveActualIndentationFromList(NodeList list, int index, SourceFile file, FormatCodeSettings options)
    {
        Debug.Assert(index >= 0 && index < list.Nodes.Length);
        var(line, character) = GetStartLineAndCharacterForNode(list.Nodes[index], file);
        for (var i = index; i >= 0; i--)
        {
            if (list.Nodes[i].Kind == Kind.CommaToken)
                continue;
            var previousEndLine = ScannerApi.GetECMALineOfPosition(file, list.Nodes[i].End());
            if (previousEndLine != line)
                return FindColumnForFirstNonWhitespaceCharacterInLine(line, character, file, options);
            (line, character) = GetStartLineAndCharacterForNode(list.Nodes[i], file);
        }

        return -1;
    }

    // indent.go:460.
    internal static long FindColumnForFirstNonWhitespaceCharacterInLine(int line, int character, SourceFile file, FormatCodeSettings options)
    {
        var start = ScannerApi.GetECMAPositionOfLineAndByteOffset(file, line, 0);
        return FindFirstNonWhitespaceColumn(start, start + character, file, options);
    }

    // indent.go:465.
    internal static long FindFirstNonWhitespaceColumn(int start, int end, SourceFile file, FormatCodeSettings options) => FindFirstNonWhitespaceCharacterAndColumn(start, end, file, options).Column;
    // indent.go:477. Retain original tab formula (addition of remainder, not subtraction).
    internal static (int Character, long Column) FindFirstNonWhitespaceCharacterAndColumn(int start, int end, SourceFile file, FormatCodeSettings options)
    {
        long column = 0;
        var text = file.TextView();
        var pos = start;
        while (pos < end)
        {
            var(rune, size) = Utf8.DecodeRuneInString(text.Slice(pos).AsSpan());
            if (!StringUtil.IsWhiteSpaceSingleLine(rune))
                break;
            if (rune == '\t')
            {
                if (options.TabSize > 0)
                    column += options.TabSize + column % options.TabSize;
            }
            else
                column++;
            pos += size;
        }

        return (pos - start, column);
    }

    // indent.go:500.
    internal static bool ChildStartsOnTheSameLineWithElseInIfStatement(Node parent, Node child, int childLine, SourceFile file)
    {
        if (parent.Kind == Kind.IfStatement && parent.AsIfStatement().ElseStatement == child)
        {
            var token = AstNav.FindPrecedingToken(file, child.Pos());
            Debug.Assert(token != null);
            return GetStartLineForNode(token!, file) == childLine;
        }

        return false;
    }

    // indent.go:510.
    internal static (int Line, int Character) GetStartLineAndCharacterForNode(Node node, SourceFile file) => ScannerApi.GetECMALineAndByteOffsetOfPosition(file, ScannerApi.GetTokenPosOfNode(node, file, false));
    // indent.go:514.
    internal static int GetStartLineForNode(Node node, SourceFile file) => ScannerApi.GetECMALineOfPosition(file, ScannerApi.GetTokenPosOfNode(node, file, false));
    // indent.go:518.
    internal static NodeList? GetContainingList(Node node, SourceFile file) => node.Parent == null ? null : GetListByRange(ScannerApi.GetTokenPosOfNode(node, file, false), node.End(), node.Parent, file);
    // indent.go:525.
    internal static NodeList? GetListByPosition(int position, Node? node, SourceFile file) => node == null ? null : GetListByRange(position, position, node, file);
    // indent.go:532. Type parameter/type argument lists take precedence over parameter/argument lists.
    internal static NodeList? GetListByRange(int start, int end, Node node, SourceFile file)
    {
        var range = new TextRange(start, end);
        switch (node.Kind)
        {
            case Kind.TypeReference:
                return GetList(node.TypeArgumentList(), range, node, file);
            case Kind.ObjectLiteralExpression:
                return GetList(node.PropertyList(), range, node, file);
            case Kind.ArrayLiteralExpression:
                return GetList(node.ElementList(), range, node, file);
            case Kind.TypeLiteral:
                return GetList(node.MemberList(), range, node, file);
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            case Kind.CallSignature:
            case Kind.Constructor:
            case Kind.ConstructorType:
            case Kind.ConstructSignature:
                return GetList(node.TypeParameterList(), range, node, file) ?? GetList(node.ParameterList(), range, node, file);
            case Kind.GetAccessor:
                return GetList(node.ParameterList(), range, node, file);
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSDocTemplateTag:
                return GetList(node.TypeParameterList(), range, node, file);
            case Kind.NewExpression:
            case Kind.CallExpression:
                return GetList(node.TypeArgumentList(), range, node, file) ?? GetList(node.ArgumentList(), range, node, file);
            case Kind.VariableDeclarationList:
                return GetList(node.AsVariableDeclarationList().Declarations, range, node, file);
            case Kind.ObjectBindingPattern:
            case Kind.ArrayBindingPattern:
            case Kind.NamedImports:
            case Kind.NamedExports:
                return GetList(node.ElementList(), range, node, file);
        }

        return null; // Original TODO asks whether this should panic; original does not.
    }

    // indent.go:579.
    internal static NodeList? GetList(NodeList? list, TextRange range, Node node, SourceFile file) => list != null && range.ContainedBy(GetVisualListRange(node, list.Loc, file)) ? list : null;
    // indent.go:589. Expand the list range through neighboring trivia using actual scanner/token owners.
    internal static TextRange GetVisualListRange(Node node, TextRange list, SourceFile file)
    {
        var prior = AstNav.FindPrecedingToken(file, list.Pos());
        var priorEnd = prior == null ? list.Pos() : prior.End();
        var scan = ScannerApi.GetScannerForSourceFile(file, list.End());
        var nextStart = scan.Token() == Kind.EndOfFile ? list.End() : scan.TokenStart();
        return new(priorEnd, nextStart);
    }

    // indent.go:613.
    internal static (int Line, int Character) GetContainingListOrParentStart(Node parent, Node child, SourceFile file)
    {
        var list = GetContainingList(child, file);
        var start = list != null ? list.Loc.Pos() : ScannerApi.GetTokenPosOfNode(parent, file, false);
        return ScannerApi.GetECMALineAndByteOffsetOfPosition(file, start);
    }

    // indent.go:624.
    internal static bool IsControlFlowEndingStatement(Kind kind, Kind parentKind) => kind is Kind.ReturnStatement or Kind.ThrowStatement or Kind.ContinueStatement or Kind.BreakStatement && parentKind != Kind.Block;
    internal enum NextTokenKind
    {
        Unknown,
        OpenBrace,
        CloseBrace
    }

    // indent.go:217.
    internal static NextTokenKind NextTokenIsCurlyBraceOnSameLineAsCursor(Node preceding, Node current, int line, SourceFile file)
    {
        var next = AstNav.FindNextToken(preceding, current, file);
        if (next == null)
            return NextTokenKind.Unknown;
        if (next.Kind == Kind.OpenBraceToken)
            return NextTokenKind.OpenBrace;
        if (next.Kind == Kind.CloseBraceToken && line == GetStartLineForNode(next, file))
            return NextTokenKind.CloseBrace;
        return NextTokenKind.Unknown;
    }

    // indent.go:238.
    internal static long GetSmartIndent(SourceFile file, int position, Node preceding, int line, bool assumeNewLineBeforeCloseBrace, FormatCodeSettings options)
    {
        Node? previous = null;
        Node? current = preceding;
        while (current != null)
        {
            if (LsUtil.PositionBelongsToNode(current, position, file) && ShouldIndentChildNode(options, current, previous, file, true))
            {
                var(startLine, startCharacter) = GetStartLineAndCharacterForNode(current, file);
                var next = NextTokenIsCurlyBraceOnSameLineAsCursor(preceding, current, line, file);
                long delta = 0;
                if (next != NextTokenKind.Unknown)
                {
                    if (assumeNewLineBeforeCloseBrace && next == NextTokenKind.CloseBrace)
                        delta = options.IndentSize;
                }
                else if (line != startLine)
                    delta = options.IndentSize;
                return GetIndentationForNodeWorker(current, startLine, startCharacter, null, delta, file, true, options);
            }

            var actual = GetActualIndentationForListItem(current, file, options, true);
            if (actual != -1)
                return actual;
            previous = current;
            current = current.Parent;
        }

        return options.BaseIndentSize;
    }

    // indent.go:279. Walk parents in the original order; source-text indentation may terminate the accumulation.
    internal static long GetIndentationForNodeWorker(Node current, int currentLine, int currentCharacter, TextRange? ignore, long delta, SourceFile file, bool isNextChild, FormatCodeSettings options)
    {
        var parent = current.Parent;
        while (parent != null)
        {
            var useActual = true;
            if (ignore is { } range)
            {
                var start = ScannerApi.GetTokenPosOfNode(current, file, false);
                useActual = start < range.Pos() || start > range.End();
            }

            var(parentLine, parentCharacter) = GetContainingListOrParentStart(parent, current, file);
            var shareLine = parentLine == currentLine || ChildStartsOnTheSameLineWithElseInIfStatement(parent, current, currentLine, file);
            if (useActual)
            {
                var list = GetContainingList(current, file);
                var first = list != null && list.Nodes.Length != 0 ? list.Nodes[0] : null;
                var listIndents = first != null && GetStartLineForNode(first, file) > parentLine;
                var actual = GetActualIndentationForListItem(current, file, options, listIndents);
                if (actual != -1)
                    return actual + delta;
                actual = GetActualIndentationForNode(current, parent, currentLine, currentCharacter, shareLine, file, options);
                if (actual != -1)
                    return actual + delta;
            }

            if (ShouldIndentChildNode(options, parent, current, file, isNextChild) && !shareLine)
                delta += options.IndentSize;
            var useTrueStart = IsArgumentAndStartLineOverlapsExpressionBeingCalled(parent, current, currentLine, file);
            current = parent;
            parent = current.Parent;
            if (useTrueStart)
                (currentLine, currentCharacter) = ScannerApi.GetECMALineAndByteOffsetOfPosition(file, ScannerApi.GetTokenPosOfNode(current, file, false));
            else
            {
                currentLine = parentLine;
                currentCharacter = parentCharacter;
            }
        }

        return delta + options.BaseIndentSize;
    }

    // indent.go:378.
    internal static long GetActualIndentationForNode(Node current, Node parent, int line, int character, bool shareLine, SourceFile file, FormatCodeSettings options) => (Ast.IsDeclaration(current) || Ast.IsStatementButNotDeclaration(current)) && (parent.Kind == Kind.SourceFile || !shareLine) ? FindColumnForFirstNonWhitespaceCharacterInLine(line, character, file, options) : -1;
    // indent.go:391.
    internal static bool IsArgumentAndStartLineOverlapsExpressionBeingCalled(Node parent, Node child, int childLine, SourceFile file) => Ast.IsCallExpression(parent) && GoSlices.Contains(parent.Arguments(), child) && ScannerApi.GetECMALineOfPosition(file, parent.Expression().End()) == childLine;
    // indent.go:400. Search is synchronous and carries the retained node explicitly, without a per-call closure.
    internal static long GetActualIndentationForListItem(Node node, SourceFile file, FormatCodeSettings options, bool listIndentsChild)
    {
        if (node.Parent != null && node.Parent.Kind == Kind.VariableDeclarationList)
            return -1;
        var list = GetContainingList(node, file);
        if (list == null)
            return -1;
        var index = Core.FindIndex(list.Nodes, node, static (element, expected) => element == expected);
        if (index != -1)
        {
            var result = DeriveActualIndentationFromList(list, index, file, options);
            if (result != -1)
                return result;
        }

        var delta = listIndentsChild ? options.IndentSize : 0;
        var start = GetActualIndentationForListStartLine(list, file, options);
        return start == -1 ? delta : start + delta;
    }

    // indent.go:193.
    internal static long GetActualIndentationForListItemBeforeComma(Node comma, SourceFile file, FormatCodeSettings options)
    {
        if (comma.Parent == null)
            return -1;
        var list = GetContainingList(comma, file);
        if (list == null)
            return -1;
        var index = Core.FindIndex(list.Nodes, comma, static (node, expected) => node == expected);
        return index > 0 ? DeriveActualIndentationFromList(list, index - 1, file, options) : -1;
    }

    // indent.go:177. Decode at the current byte offset before moving backward, as in the original.
    internal static long GetBlockIndent(SourceFile file, int position, FormatCodeSettings options)
    {
        var current = position;
        var text = file.TextView();
        while (current > 0)
        {
            var(rune, size) = Utf8.DecodeRuneInString(text.Slice(current).AsSpan());
            if (!StringUtil.IsWhiteSpaceLike(rune))
                break;
            current -= size;
        }

        return FindFirstNonWhitespaceColumn(GetLineStartPositionForPosition(current, file), current, file, options);
    }

    // indent.go:637. Optional variadic flag only consumes the first value upstream.
    internal static bool ShouldIndentChildNode(FormatCodeSettings settings, Node parent, Node? child, SourceFile? file, bool isNextChild = false) => NodeWillIndentChild(settings, parent, child, file, false) && !(isNextChild && child != null && IsControlFlowEndingStatement(child.Kind, parent.Kind));
    // indent.go:646.
    internal static bool NodeWillIndentChild(FormatCodeSettings settings, Node parent, Node? child, SourceFile? file, bool indentByDefault)
    {
        var childKind = child?.Kind ?? Kind.Unknown;
        switch (parent.Kind)
        {
            case Kind.ExpressionStatement:
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
            case Kind.InterfaceDeclaration:
            case Kind.EnumDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.ArrayLiteralExpression:
            case Kind.Block:
            case Kind.ModuleBlock:
            case Kind.ObjectLiteralExpression:
            case Kind.TypeLiteral:
            case Kind.MappedType:
            case Kind.TupleType:
            case Kind.ParenthesizedExpression:
            case Kind.PropertyAccessExpression:
            case Kind.CallExpression:
            case Kind.NewExpression:
            case Kind.VariableStatement:
            case Kind.ExportAssignment:
            case Kind.ReturnStatement:
            case Kind.ConditionalExpression:
            case Kind.ArrayBindingPattern:
            case Kind.ObjectBindingPattern:
            case Kind.JsxOpeningElement:
            case Kind.JsxOpeningFragment:
            case Kind.JsxSelfClosingElement:
            case Kind.JsxExpression:
            case Kind.MethodSignature:
            case Kind.CallSignature:
            case Kind.ConstructSignature:
            case Kind.Parameter:
            case Kind.FunctionType:
            case Kind.ConstructorType:
            case Kind.ParenthesizedType:
            case Kind.TaggedTemplateExpression:
            case Kind.AwaitExpression:
            case Kind.NamedExports:
            case Kind.NamedImports:
            case Kind.ExportSpecifier:
            case Kind.ImportSpecifier:
            case Kind.PropertyDeclaration:
            case Kind.CaseClause:
            case Kind.DefaultClause:
                return true;
            case Kind.CaseBlock:
                return settings.IndentSwitchCase != Tristate.False;
            case Kind.VariableDeclaration:
            case Kind.PropertyAssignment:
            case Kind.BinaryExpression:
                if (settings.IndentMultiLineObjectLiteralBeginningOnBlankLine != Tristate.True && file != null && childKind == Kind.ObjectLiteralExpression)
                    return RangeIsOnOneLine(child!.Loc, file);
                if (parent.Kind == Kind.BinaryExpression && file != null && childKind == Kind.JsxElement)
                {
                    var parentLine = ScannerApi.GetECMALineOfPosition(file, ScannerApi.SkipTrivia(file.TextView(), parent.Pos()));
                    var childLine = ScannerApi.GetECMALineOfPosition(file, ScannerApi.SkipTrivia(file.TextView(), child!.Pos()));
                    return parentLine != childLine;
                }

                return parent.Kind != Kind.BinaryExpression || indentByDefault;
            case Kind.DoStatement:
            case Kind.WhileStatement:
            case Kind.ForInStatement:
            case Kind.ForOfStatement:
            case Kind.ForStatement:
            case Kind.IfStatement:
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.MethodDeclaration:
            case Kind.Constructor:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
                return childKind != Kind.Block;
            case Kind.ArrowFunction:
                if (file != null && childKind == Kind.ParenthesizedExpression)
                    return RangeIsOnOneLine(child!.Loc, file);
                return childKind != Kind.Block;
            case Kind.ExportDeclaration:
                return childKind != Kind.NamedExports;
            case Kind.ImportDeclaration:
                return childKind != Kind.ImportClause || child!.AsImportClause().NamedBindings != null && child.AsImportClause().NamedBindings.Kind != Kind.NamedImports;
            case Kind.JsxElement:
                return childKind != Kind.JsxClosingElement;
            case Kind.JsxFragment:
                return childKind != Kind.JsxClosingFragment;
            case Kind.IntersectionType:
            case Kind.UnionType:
            case Kind.SatisfiesExpression:
                return childKind is Kind.TypeLiteral or Kind.TupleType or Kind.MappedType ? false : indentByDefault;
            case Kind.TryStatement:
                return childKind == Kind.Block ? false : indentByDefault;
        }

        return indentByDefault;
    }

    // indent.go:779. Avoid double indentation for multiline conditional branches sharing their predecessor's end line.
    internal static bool ChildIsUnindentedBranchOfConditionalExpression(Node parent, Node child, int childLine, SourceFile file)
    {
        if (parent.Kind != Kind.ConditionalExpression)
            return false;
        var expression = parent.AsConditionalExpression();
        if (child != expression.WhenTrue && child != expression.WhenFalse)
            return false;
        var conditionEndLine = ScannerApi.GetECMALineOfPosition(file, expression.Condition.End());
        if (child == expression.WhenTrue)
            return childLine == conditionEndLine;
        var trueStartLine = GetStartLineForNode(expression.WhenTrue, file);
        var trueEndLine = ScannerApi.GetECMALineOfPosition(file, expression.WhenTrue.End());
        return conditionEndLine == trueStartLine && trueEndLine == childLine;
    }

    // indent.go:800.
    internal static bool ArgumentStartsOnSameLineAsPreviousArgument(Node parent, Node child, int childLine, SourceFile file)
    {
        if (!Ast.IsCallExpression(parent) && !Ast.IsNewExpression(parent))
            return false;
        var arguments = parent.Arguments();
        if (arguments.Length == 0)
            return false;
        var index = Core.FindIndex(arguments, child, static (node, expected) => node == expected);
        if (index <= 0)
            return false;
        return childLine == ScannerApi.GetECMALineOfPosition(file, arguments[index - 1].End());
    }

    // indent.go:17.
    internal static long GetIndentationForNode(Node node, TextRange? ignore, SourceFile file, FormatCodeSettings options)
    {
        var(line, character) = ScannerApi.GetECMALineAndByteOffsetOfPosition(file, ScannerApi.GetTokenPosOfNode(node, file, false));
        return GetIndentationForNodeWorker(node, line, character, ignore, 0, file, false, options);
    }

    // indent.go:24.
    internal static long GetIndentation(int position, SourceFile file, FormatCodeSettings options, bool assumeNewLineBeforeCloseBrace)
    {
        if (position > file.TextView().Length)
            return options.BaseIndentSize;
        if (options.IndentStyle == IndentStyle.None)
            return 0;
        var preceding = AstNav.FindPrecedingTokenEx(file, position, null, true);
        var enclosing = GetRangeOfEnclosingComment(file, position, preceding);
        if (enclosing is { Kind: Kind.MultiLineCommentTrivia } comment)
            return GetCommentIndent(file, position, options, comment);
        if (preceding == null)
            return options.BaseIndentSize;
        if (IsStringOrRegularExpressionOrTemplateLiteral(preceding.Kind))
        {
            var tokenStart = ScannerApi.GetTokenPosOfNode(preceding, file, false);
            if (tokenStart <= position && position < preceding.End())
                return 0;
        }

        var line = ScannerApi.GetECMALineOfPosition(file, position);
        var token = AstNav.GetTokenAtPosition(file, position);
        var isObject = token.Kind == Kind.OpenBraceToken && token.Parent != null && token.Parent.Kind == Kind.ObjectLiteralExpression;
        if (options.IndentStyle == IndentStyle.Block || isObject)
            return GetBlockIndent(file, position, options);
        if (preceding.Kind == Kind.CommaToken && preceding.Parent != null && preceding.Parent.Kind != Kind.BinaryExpression)
        {
            var actual = GetActualIndentationForListItemBeforeComma(preceding, file, options);
            if (actual != -1)
                return actual;
        }

        var list = GetListByPosition(position, preceding.Parent, file);
        if (list != null && !preceding.Loc.ContainedBy(list.Loc))
        {
            var sameBase = token.Parent != null && token.Parent.Kind is Kind.FunctionExpression or Kind.ArrowFunction;
            var delta = sameBase ? 0 : options.IndentSize;
            var actual = GetActualIndentationForListStartLine(list, file, options);
            return actual == -1 ? delta : actual + delta;
        }

        return GetSmartIndent(file, position, preceding, line, assumeNewLineBeforeCloseBrace, options);
    }

    // indent.go:111.
    internal static long GetCommentIndent(SourceFile file, int position, FormatCodeSettings options, CommentRange comment)
    {
        var previousLine = ScannerApi.GetECMALineOfPosition(file, position) - 1;
        var commentStartLine = ScannerApi.GetECMALineOfPosition(file, comment.Pos);
        Debug.Assert(commentStartLine >= 0);
        var starts = ScannerApi.GetECMALineStarts(file);
        if (previousLine <= commentStartLine)
            return FindFirstNonWhitespaceColumn(starts[commentStartLine], position, file, options);
        var start = starts[previousLine];
        var(character, column) = FindFirstNonWhitespaceCharacterAndColumn(start, position, file, options);
        if (column == 0)
            return column;
        return file.TextView()[start + character] == '*' ? column - 1 : column;
    }

    // indent.go:137. Value sequence retains the original optional iterator and borrowed source owner.
    internal static ScannerApi.CommentRangeSequence GetLeadingCommentRangesOfNode(Node node, SourceFile file) => node.Kind == Kind.JsxText ? default : ScannerApi.GetLeadingCommentRangesValue(new NodeFactory(), file.TextView(), node.Pos());
    // indent.go:144. Visit trailing ranges before leading ranges; return the original value snapshot.
    internal static CommentRange? GetRangeOfEnclosingComment(SourceFile file, int position, Node? preceding)
    {
        var token = AstNav.GetTokenAtPosition(file, position);
        var jsdoc = Ast.FindAncestor(token, static node => node.IsJSDoc());
        if (jsdoc != null)
            token = jsdoc.Parent;
        var tokenStart = AstNav.GetStartOfNode(token, file, false);
        if (tokenStart <= position && position < token.End())
            return null;
        var trailing = preceding == null ? default : ScannerApi.GetTrailingCommentRangesValue(new NodeFactory(), file.TextView(), preceding.End());
        foreach (var range in trailing)
            if (range.ContainsExclusive(position) || position == range.End && (range.Kind == Kind.SingleLineCommentTrivia || position == file.TextView().Length))
                return range;
        foreach (var range in GetLeadingCommentRangesOfNode(token, file))
            if (range.ContainsExclusive(position) || position == range.End && (range.Kind == Kind.SingleLineCommentTrivia || position == file.TextView().Length))
                return range;
        return null;
    }
}

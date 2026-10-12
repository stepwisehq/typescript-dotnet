// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using Debug = TypeScript.Scanner.Debug;
using TypeScript.Scanner;
using TypeScript.AstNavigation;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;
using ScannerApi = TypeScript.Scanner.Scanner;
using Diagnostic = TypeScript.Scanner.Diagnostic;
using static TypeScript.LanguageServices.Formatting.Formatting;

namespace TypeScript.LanguageServices.Formatting;
internal static partial class Formatting
{
    // span.go:20. One scoped search owner retains its visitor; children are visited in original order.
    private sealed class EnclosingNodeSearch
    {
        private readonly TextRange range;
        private readonly SourceFile file;
        private readonly Visitor visitor;
        private Node? candidate;
        internal EnclosingNodeSearch(TextRange range, SourceFile file)
        {
            this.range = range;
            this.file = file;
            visitor = Visit;
        }

        private bool Visit(Node child)
        {
            if ((child.Flags & NodeFlags.Reparsed) != 0 || !range.ContainedBy(WithTokenStart(child, file)))
                return false;
            candidate = child;
            return true;
        }

        internal Node Find()
        {
            var current = file.AsNode();
            while (true)
            {
                candidate = null;
                current.ForEachChild(visitor);
                if (candidate == null)
                    return current;
                current = candidate;
            }
        }
    }

    internal static Node FindEnclosingNode(TextRange range, SourceFile file) => new EnclosingNodeSearch(range, file).Find();
    // span.go:51.
    internal static int GetScanStartPosition(Node node, TextRange range, SourceFile file)
    {
        var start = WithTokenStart(node, file).Pos();
        if (start == range.Pos() && node.End() == range.End())
            return start;
        var preceding = AstNav.FindPrecedingTokenEx(file, range.Pos(), null, true);
        return preceding == null || preceding.End() >= range.Pos() ? node.Pos() : preceding.End();
    }

    // span.go:89.
    internal static long GetOwnOrInheritedDelta(Node? node, FormatCodeSettings options, SourceFile file)
    {
        var previousLine = -1;
        Node? child = null;
        while (node != null)
        {
            var line = ScannerApi.GetECMALineOfPosition(file, WithTokenStart(node, file).Pos());
            if (previousLine != -1 && line != previousLine)
                break;
            if (ShouldIndentChildNode(options, node, child, file))
                return options.IndentSize;
            previousLine = line;
            child = node;
            node = node.Parent;
        }

        return 0;
    }

    // span.go:109.
    internal static bool RangeHasNoErrors(TextRange range) => false;
    // span.go:113. One retained monotonic error-range predicate owner per formatting operation.
    internal static Func<TextRange, bool> PrepareRangeContainsErrorFunction(Slice<Diagnostic> errors, TextRange range)
    {
        if (errors.Length == 0)
            return RangeHasNoErrors;
        var sorted = Core.Filter(errors, range, static (diagnostic, selected) => selected.Overlaps(diagnostic.Loc()));
        if (sorted.Length == 0)
            return RangeHasNoErrors;
        GoSlices.SortStableFunc(sorted, static (a, b) => a.Pos() - b.Pos());
        var index = 0;
        return current =>
        {
            while (true)
            {
                if (index >= sorted.Length)
                    return false;
                var error = sorted[index];
                if (current.End() <= error.Pos())
                    return false;
                if (current.Overlaps(error.Loc()))
                    return true;
                index++;
            }
        };
    }

    // span.go:211.
    internal static int GetNonDecoratorTokenPosOfNode(Node node, SourceFile? file)
    {
        Node? last = null;
        if (Ast.HasDecorators(node))
            for (var index = node.ModifierNodes().Length - 1; index >= 0; index--)
                if (Ast.IsDecorator(node.ModifierNodes()[index]))
                {
                    last = node.ModifierNodes()[index];
                    break;
                }

        file ??= Ast.GetSourceFileOfNode(node);
        return last == null ? WithTokenStart(node, file).Pos() : ScannerApi.SkipTrivia(file.TextView(), last.End());
    }

    // span.go:870.
    internal static bool IsStringOrRegularExpressionOrTemplateLiteral(Kind kind) => kind == Kind.StringLiteral || kind == Kind.RegularExpressionLiteral || Ast.IsTemplateLiteralKind(kind);
    // span.go:874.
    internal static bool IsComment(Kind kind) => kind is Kind.SingleLineCommentTrivia or Kind.MultiLineCommentTrivia;
    // span.go:998. Preserve floor division and the original zero-tab-size branch.
    internal static ByteStringValue GetIndentationString(long indentation, FormatCodeSettings options)
    {
        if (options.ConvertTabsToSpaces == Tristate.True)
            return ByteStringValue.FromString(new string (' ', checked((int)indentation)));
        if (options.TabSize == 0)
            return ByteStringValue.FromString("");
        var tabs = (long)Math.Floor((double)indentation / options.TabSize);
        var spaces = indentation - tabs * options.TabSize;
        var result = ByteStringValue.FromString(new string ('\t', checked((int)tabs)));
        return spaces > 0 ? ByteStringValue.Concat(result, ByteStringValue.FromString(new string (' ', checked((int)spaces)))) : result;
    }

    // span.go:1017.
    internal static TextChange CreateTextChangeFromStartLength(int start, int length, ByteStringValue text) => new(new TextRange(start, start + length), text);
    // span.go:1214. The original caller requires a decorator before slicing its modifiers.
    internal static Kind GetFirstNonDecoratorTokenOfNode(Node node)
    {
        if (Ast.CanHaveModifiers(node))
        {
            var modifiers = node.ModifierNodes();
            var index = Core.FindIndex(modifiers, static modifier => Ast.IsDecorator(modifier));
            foreach (var modifier in modifiers.Slice2(index))
                if (Ast.IsModifier(modifier))
                    return modifier.Kind;
        }

        switch (node.Kind)
        {
            case Kind.ClassDeclaration:
                return Kind.ClassKeyword;
            case Kind.InterfaceDeclaration:
                return Kind.InterfaceKeyword;
            case Kind.FunctionDeclaration:
                return Kind.FunctionKeyword;
            case Kind.EnumDeclaration:
                return Kind.EnumDeclaration;
            case Kind.GetAccessor:
                return Kind.GetKeyword;
            case Kind.SetAccessor:
                return Kind.SetKeyword;
            case Kind.MethodDeclaration:
                if (node.AsMethodDeclaration().AsteriskToken != null)
                    return Kind.AsteriskToken;
                goto case Kind.PropertyDeclaration;
            case Kind.PropertyDeclaration:
            case Kind.Parameter:
                var name = Ast.GetNameOfDeclaration(node);
                if (name != null)
                    return name.Kind;
                break;
        }

        return Kind.Unknown;
    }
}

internal sealed class DynamicIndenter
{
    internal Node node = null!;
    internal int nodeStartLine;
    internal long indentation, delta;
    internal FormatCodeSettings options;
    internal SourceFile sourceFile = null!;
    // span.go:1131.
    internal long GetIndentationForComment(Kind kind, long tokenIndentation, Node container) => kind is Kind.CloseBraceToken or Kind.CloseBracketToken or Kind.CloseParenToken ? indentation + GetDelta(container) : tokenIndentation != -1 ? tokenIndentation : indentation;
    // span.go:1159.
    internal long GetIndentationForToken(int line, Kind kind, Node container, bool suppressDelta) => !suppressDelta && ShouldAddDelta(line, kind, container) ? indentation + GetDelta(container) : indentation;
    // span.go:1166.
    internal long GetIndentation() => indentation;
    // span.go:1170.
    internal long GetDelta(Node? child) => Formatting.NodeWillIndentChild(options, node, child, sourceFile, true) ? delta : 0;
    // span.go:1178.
    internal void RecomputeIndentation(bool lineAdded, Node parent)
    {
        if (Formatting.ShouldIndentChildNode(options, parent, node, sourceFile))
        {
            indentation += lineAdded ? options.IndentSize : -options.IndentSize;
            delta = Formatting.ShouldIndentChildNode(options, node, null, null) ? options.IndentSize : 0;
        }
    }

    // span.go:1193.
    private bool ShouldAddDelta(int line, Kind kind, Node container)
    {
        switch (kind)
        {
            case Kind.OpenBraceToken:
            case Kind.CloseBraceToken:
            case Kind.CloseParenToken:
            case Kind.ElseKeyword:
            case Kind.WhileKeyword:
            case Kind.AtToken:
                return false;
            case Kind.SlashToken:
            case Kind.GreaterThanToken:
                if (container.Kind is Kind.JsxOpeningElement or Kind.JsxClosingElement or Kind.JsxSelfClosingElement)
                    return false;
                break;
            case Kind.OpenBracketToken:
            case Kind.CloseBracketToken:
                if (container.Kind != Kind.MappedType)
                    return false;
                break;
        }

        return nodeStartLine != line && !(Ast.HasDecorators(node) && kind == Formatting.GetFirstNonDecoratorTokenOfNode(node));
    }
}

internal enum LineAction
{
    None,
    LineAdded,
    LineRemoved
}

internal sealed partial class FormatSpanWorker
{
    private TextRange originalRange;
    private Node enclosingNode = null!;
    private long initialIndentation, delta;
    private FormatRequestKind requestKind;
    private Func<TextRange, bool> rangeContainsError = null!;
    private SourceFile sourceFile = null!;
    private ProtocolRequestContext ctx = null!;
    private FormattingScanner formattingScanner = null!;
    private FormattingContext formattingContext = null!;
    private Slice<TextChange> edits;
    private TextRangeWithKind previousRange;
    private int previousRangeTriviaEnd;
    private Node previousParent = null!;
    private int previousRangeStartLine;
    private Node childContextNode = null!;
    private int lastIndentedLine;
    private long indentationOnLastIndentedLine;
    private NodeVisitor visitor = null!;
    private Node visitingNode = null!;
    private DynamicIndenter visitingIndenter = null!;
    private int visitingNodeStartLine, visitingUndecoratedNodeStartLine;
    private Slice<RuleImpl> currentRules;
    // span.go:188. One worker retains rule capacity32 and scoped visitor state.
    internal static FormatSpanWorker NewFormatSpanWorker(ProtocolRequestContext context, TextRange range, Node node, long initial, long delta, FormatRequestKind kind, Func<TextRange, bool> containsError, SourceFile file) => new()
    {
        ctx = context,
        originalRange = range,
        enclosingNode = node,
        initialIndentation = initial,
        delta = delta,
        requestKind = kind,
        rangeContainsError = containsError,
        sourceFile = file,
        currentRules = Slice<RuleImpl>.Make(0, 32)
    };
    // span.go:650. Reverse application makes higher-priority rules win overlapping edits.
    private LineAction ProcessPair(TextRangeWithKind current, int currentLine, Node currentParent, TextRangeWithKind previous, int previousLine, Node previousParent, Node contextNode, DynamicIndenter? indenter)
    {
        formattingContext.UpdateContext(previous, previousParent, current, currentParent, contextNode);
        currentRules = GetRules(formattingContext, currentRules.Slice2(0, 0));
        var trim = formattingContext.Options.TrimTrailingWhitespace != Tristate.False;
        var action = LineAction.None;
        if (currentRules.Length != 0)
        {
            for (var index = currentRules.Length - 1; index >= 0; index--)
            {
                var rule = currentRules[index];
                action = ApplyRuleEdits(rule, previous, previousLine, current, currentLine);
                if (indenter != null)
                {
                    switch (action)
                    {
                        case LineAction.LineRemoved:
                            if (ScannerApi.GetTokenPosOfNode(currentParent, sourceFile, false) == current.Loc.Pos())
                                indenter.RecomputeIndentation(false, contextNode);
                            break;
                        case LineAction.LineAdded:
                            if (ScannerApi.GetTokenPosOfNode(currentParent, sourceFile, false) == current.Loc.Pos())
                                indenter.RecomputeIndentation(true, contextNode);
                            break;
                        default:
                            Debug.Assert(action == LineAction.None);
                            break;
                    }
                }

                trim = trim && (rule.Action() & RuleAction.DeleteSpace) == 0 && rule.Flags() != RuleFlags.CanDeleteNewLines;
            }
        }
        else
            trim = trim && current.Kind != Kind.EndOfFile;
        if (currentLine != previousLine && trim)
            TrimTrailingWhitespacesForLines(previousLine, currentLine, previous);
        return action;
    }

    // span.go:700.
    private LineAction ApplyRuleEdits(RuleImpl rule, TextRangeWithKind previous, int previousLine, TextRangeWithKind current, int currentLine)
    {
        var later = currentLine != previousLine;
        switch (rule.Action())
        {
            case RuleAction.StopProcessingSpaceActions:
                return LineAction.None;
            case RuleAction.DeleteSpace:
                if (previous.Loc.End() != current.Loc.Pos())
                {
                    RecordDelete(previous.Loc.End(), current.Loc.Pos() - previous.Loc.End());
                    return later ? LineAction.LineRemoved : LineAction.None;
                }

                break;
            case RuleAction.DeleteToken:
                RecordDelete(previous.Loc.Pos(), previous.Loc.Len());
                break;
            case RuleAction.InsertNewLine:
                if (rule.Flags() != RuleFlags.CanDeleteNewLines && previousLine != currentLine)
                    return LineAction.None;
                if (currentLine - previousLine != 1)
                {
                    RecordReplace(previous.Loc.End(), current.Loc.Pos() - previous.Loc.End(), GetNewLineOrDefaultFromContext(ctx));
                    return later ? LineAction.None : LineAction.LineAdded;
                }

                break;
            case RuleAction.InsertSpace:
                if (rule.Flags() != RuleFlags.CanDeleteNewLines && previousLine != currentLine)
                    return LineAction.None;
                var length = current.Loc.Pos() - previous.Loc.End();
                var text = sourceFile.TextView();
                if (length != 1 || previous.Loc.End() == text.Length || text[previous.Loc.End()] != ' ')
                {
                    RecordReplace(previous.Loc.End(), length, ByteStringValue.FromString(" "));
                    return later ? LineAction.LineRemoved : LineAction.None;
                }

                break;
            case RuleAction.InsertTrailingSemicolon:
                RecordInsert(previous.Loc.End(), ByteStringValue.FromString(";"));
                break;
        }

        return LineAction.None;
    }

    // span.go:762.
    private LineAction ProcessRange(TextRangeWithKind range, int line, int character, Node parent, Node contextNode, DynamicIndenter? indenter)
    {
        var action = LineAction.None;
        if (!rangeContainsError(range.Loc))
        {
            if (previousRange.Equals(default(TextRangeWithKind)))
                TrimTrailingWhitespacesForLines(ScannerApi.GetECMALineOfPosition(sourceFile, originalRange.Pos()), line, default);
            else
                action = ProcessPair(range, line, parent, previousRange, previousRangeStartLine, previousParent, contextNode, indenter);
        }

        previousRange = range;
        previousRangeTriviaEnd = range.Loc.End();
        previousParent = parent;
        previousRangeStartLine = line;
        return action;
    }

    // span.go:783.
    private void ProcessTrivia(Slice<TextRangeWithKind> trivia, Node parent, Node contextNode, DynamicIndenter? indenter)
    {
        foreach (var item in trivia)
            if (IsComment(item.Kind) && item.Loc.ContainedBy(originalRange))
            {
                var(line, character) = ScannerApi.GetECMALineAndByteOffsetOfPosition(sourceFile, item.Loc.Pos());
                ProcessRange(item, line, character, parent, contextNode, indenter);
            }
    }

    // span.go:796.
    private void TrimTrailingWhitespacesForRemainingRange(Slice<TextRangeWithKind> trivia)
    {
        var start = previousRange.Equals(default(TextRangeWithKind)) ? originalRange.Pos() : previousRange.Loc.End();
        foreach (var item in trivia)
            if (IsComment(item.Kind))
            {
                if (start < item.Loc.Pos())
                    TrimTrailingWitespacesForPositions(start, item.Loc.Pos() - 1, previousRange);
                start = item.Loc.End() + 1;
            }

        if (start < originalRange.End())
            TrimTrailingWitespacesForPositions(start, originalRange.End(), previousRange);
    }

    // span.go:817.
    private void TrimTrailingWitespacesForPositions(int start, int end, TextRangeWithKind previous) => TrimTrailingWhitespacesForLines(ScannerApi.GetECMALineOfPosition(sourceFile, start), ScannerApi.GetECMALineOfPosition(sourceFile, end) + 1, previous);
    // span.go:824.
    private void TrimTrailingWhitespacesForLines(int first, int last, TextRangeWithKind range)
    {
        var starts = ScannerApi.GetECMALineStarts(sourceFile);
        for (var line = first; line < last; line++)
        {
            var start = starts[line];
            var end = ScannerApi.GetECMAEndLinePosition(sourceFile, line);
            if (!range.Equals(default(TextRangeWithKind)) && (IsComment(range.Kind) || IsStringOrRegularExpressionOrTemplateLiteral(range.Kind)) && range.Loc.Pos() <= end && range.Loc.End() > end)
                continue;
            var whitespace = GetTrailingWhitespaceStartPosition(start, end);
            if (whitespace != -1)
            {
                if (whitespace != start)
                {
                    var(rune, _) = Utf8.DecodeRuneInString(sourceFile.TextView().Slice(whitespace - 1).AsSpan());
                    Debug.Assert(!StringUtil.IsWhiteSpaceSingleLine(rune));
                }

                RecordDelete(whitespace, end + 1 - whitespace);
            }
        }
    }

    // span.go:850.
    private int GetTrailingWhitespaceStartPosition(int start, int end)
    {
        var pos = end;
        var text = sourceFile.TextView();
        while (pos >= start)
        {
            var(rune, size) = Utf8.DecodeRuneInString(text.Slice(pos).AsSpan());
            if (size == 0)
            {
                pos--;
                continue;
            }

            if (!StringUtil.IsWhiteSpaceSingleLine(rune))
                break;
            pos--;
        }

        return pos != end ? pos + 1 : -1;
    }

    // span.go:878.
    private void InsertIndentation(int position, long indentation, bool lineAdded)
    {
        var text = GetIndentationString(indentation, formattingContext.Options);
        if (lineAdded)
            RecordReplace(position, 0, text);
        else
        {
            var(line, character) = ScannerApi.GetECMALineAndByteOffsetOfPosition(sourceFile, position);
            var start = ScannerApi.GetECMALineStarts(sourceFile)[line];
            if (indentation != CharacterToColumn(start, character) || IndentationIsDifferent(text, start))
                RecordReplace(start, character, text);
        }
    }

    // span.go:893. This column conversion uses subtraction of the tab remainder, unlike the original indentation helper.
    private long CharacterToColumn(int start, int character)
    {
        long column = 0;
        var text = sourceFile.TextView();
        for (var index = 0; index < character; index++)
            if (text[start + index] == '\t')
            {
                var tab = formattingContext.Options.TabSize;
                if (tab > 0)
                    column += tab - column % tab;
            }
            else
                column++;
        return column;
    }

    // span.go:907.
    private bool IndentationIsDifferent(ByteStringValue indentation, int start) => start + indentation.Length > sourceFile.TextView().Length || !indentation.EqualsValue(ByteStringValue.FromView(sourceFile.TextView().Slice(start, start + indentation.Length)));
    // span.go:1024.
    private void RecordDelete(int start, int length)
    {
        if (length != 0)
            edits = edits.Append(CreateTextChangeFromStartLength(start, length, ByteStringValue.FromString("")));
    }

    // span.go:1030.
    private void RecordReplace(int start, int length, ByteStringValue text)
    {
        if (length != 0 || text.Length != 0)
            edits = edits.Append(CreateTextChangeFromStartLength(start, length, text));
    }

    // span.go:1036.
    private void RecordInsert(int start, ByteStringValue text)
    {
        if (text.Length != 0)
            edits = edits.Append(CreateTextChangeFromStartLength(start, 0, text));
    }

    // span.go:1251.
    private DynamicIndenter GetDynamicIndentation(Node node, int line, long indentation, long delta) => new()
    {
        node = node,
        nodeStartLine = line,
        indentation = indentation,
        delta = delta,
        options = formattingContext.Options,
        sourceFile = sourceFile
    };
    // span.go:528. Restore mutable traversal slots in source order after synchronous visitation.
    private void ExecuteProcessNodeVisitor(Node node, DynamicIndenter indenter, int line, int undecoratedLine)
    {
        var oldNode = visitingNode;
        var oldIndenter = visitingIndenter;
        var oldLine = visitingNodeStartLine;
        var oldUndecoratedLine = visitingUndecoratedNodeStartLine;
        visitingNode = node;
        visitingIndenter = indenter;
        visitingNodeStartLine = line;
        visitingUndecoratedNodeStartLine = undecoratedLine;
        node.VisitEachChild(visitor);
        visitingNode = oldNode;
        visitingIndenter = oldIndenter;
        visitingNodeStartLine = oldLine;
        visitingUndecoratedNodeStartLine = oldUndecoratedLine;
    }

    // span.go:544.
    private long GetCurrentIndentationAtPosition(int position) => FindFirstNonWhitespaceColumn(GetLineStartPositionForPosition(position, sourceFile), position, sourceFile, formattingContext.Options);
    // span.go:549.
    private (long Indentation, long Delta) ComputeIndentation(Node node, int line, long inherited, Node parent, DynamicIndenter parentIndentation, int effectiveParentLine)
    {
        long delta = ShouldIndentChildNode(formattingContext.Options, node, null, null) ? formattingContext.Options.IndentSize : 0;
        if (effectiveParentLine == line)
        {
            var indentation = line == lastIndentedLine ? indentationOnLastIndentedLine : parentIndentation.GetIndentation();
            delta = Math.Min(formattingContext.Options.IndentSize, parentIndentation.GetDelta(node) + delta);
            return (indentation, delta);
        }

        if (inherited == -1)
        {
            if (node.Kind == Kind.OpenParenToken && line == lastIndentedLine)
                return (indentationOnLastIndentedLine, parentIndentation.GetDelta(node));
            if (ChildStartsOnTheSameLineWithElseInIfStatement(parent, node, line, sourceFile) || ChildIsUnindentedBranchOfConditionalExpression(parent, node, line, sourceFile) || ArgumentStartsOnSameLineAsPreviousArgument(parent, node, line, sourceFile))
                return (parentIndentation.GetIndentation(), delta);
            var indentation = parentIndentation.GetIndentation();
            return (indentation == -1 ? parentIndentation.GetIndentation() : indentation + parentIndentation.GetDelta(node), delta);
        }

        return (inherited, delta);
    }

    // span.go:593. Include contained zero-width nodes when propagating list indentation.
    private long TryComputeIndentationForListItem(int start, int end, int parentLine, TextRange range, long inherited)
    {
        var item = new TextRange(start, end);
        if (range.Overlaps(item) || item.ContainedBy(range))
        {
            if (inherited != -1)
                return inherited;
        }
        else
        {
            var line = ScannerApi.GetECMALineOfPosition(sourceFile, start);
            var column = GetCurrentIndentationAtPosition(start);
            if (line != parentLine || start == column)
                return Math.Max(formattingContext.Options.BaseIndentSize, column);
        }

        return -1;
    }

    // span.go:615. Interleaved tokens are processed by the following child's traversal and then the node tail.
    private void ProcessNode(Node node, Node contextNode, int line, int undecoratedLine, long indentation, long delta)
    {
        if (!originalRange.Overlaps(WithTokenStart(node, sourceFile)))
            return;
        var indenter = GetDynamicIndentation(node, line, indentation, delta);
        childContextNode = contextNode;
        ExecuteProcessNodeVisitor(node, indenter, line, undecoratedLine);
        while (formattingScanner.IsOnToken() && formattingScanner.GetTokenFullStart() < originalRange.End())
        {
            var info = formattingScanner.ReadTokenInfo(node);
            if (info.token.Loc.End() > Math.Min(node.End(), originalRange.End()))
                break;
            ConsumeTokenAndAdvanceScanner(info, node, indenter, node, false);
        }
    }

    // span.go:334.
    private long ProcessChildNode(Node node, DynamicIndenter indenter, int nodeLine, int undecoratedNodeLine, Node child, long inherited, Node parent, DynamicIndenter parentIndentation, int parentLine, int undecoratedParentLine, bool isListItem, bool isFirstListItem)
    {
        Debug.Assert(!Ast.NodeIsSynthesized(child));
        if (Ast.NodeIsMissing(child) || (child.Flags & NodeFlags.Reparsed) != 0)
            return inherited;
        var start = ScannerApi.GetTokenPosOfNode(child, sourceFile, false);
        var line = ScannerApi.GetECMALineOfPosition(sourceFile, start);
        var undecoratedLine = Ast.HasDecorators(child) ? ScannerApi.GetECMALineOfPosition(sourceFile, GetNonDecoratorTokenPosOfNode(child, sourceFile)) : line;
        var errorMember = (child.Flags & NodeFlags.ThisNodeHasError) != 0 && IsMemberListElement(parent, child);
        long amount = -1;
        if (!errorMember && isListItem && parent.Loc.ContainedBy(originalRange))
        {
            amount = TryComputeIndentationForListItem(start, child.End(), parentLine, originalRange, inherited);
            if (amount != -1)
                inherited = amount;
        }

        if (!originalRange.Overlaps(child.Loc))
        {
            if (child.End() < originalRange.Pos())
                formattingScanner.SkipToEndOf(child.Loc);
            return inherited;
        }

        if (child.Loc.Len() == 0)
            return inherited;
        while (formattingScanner.IsOnToken() && formattingScanner.GetTokenFullStart() < originalRange.End())
        {
            var info = formattingScanner.ReadTokenInfo(node);
            if (info.token.Loc.End() > originalRange.End())
                return inherited;
            if (info.token.Loc.End() > start)
            {
                if (info.token.Loc.Pos() > start)
                    formattingScanner.SkipToStartOf(child.Loc);
                break;
            }

            ConsumeTokenAndAdvanceScanner(info, node, parentIndentation, node, false);
        }

        if (!formattingScanner.IsOnToken() || formattingScanner.GetTokenFullStart() >= originalRange.End())
            return inherited;
        if (Ast.IsTokenKind(child.Kind))
        {
            var info = formattingScanner.ReadTokenInfo(child);
            if (child.Kind != Kind.JsxText)
            {
                Debug.Assert(info.token.Loc.End() == child.Loc.End(), "Token end is child end");
                ConsumeTokenAndAdvanceScanner(info, node, parentIndentation, child, false);
                return inherited;
            }
        }

        var effectiveParentLine = child.Kind == Kind.Decorator ? line : undecoratedParentLine;
        long indentation = 0, delta = 0;
        if (errorMember)
            indentation = GetCurrentIndentationAtPosition(start);
        else
            (indentation, delta) = ComputeIndentation(child, line, amount, node, parentIndentation, effectiveParentLine);
        ProcessNode(child, childContextNode, line, undecoratedLine, indentation, delta);
        childContextNode = node;
        if (isFirstListItem && parent.Kind == Kind.ArrayLiteralExpression && inherited == -1)
            inherited = indentation;
        return inherited;
    }

    // span.go:439.
    private void ProcessChildNodes(Node node, DynamicIndenter indenter, int nodeLine, int undecoratedNodeLine, NodeList nodes, Node parent, int parentLine, DynamicIndenter parentIndentation)
    {
        Debug.Assert(!Ast.PositionIsSynthesized(nodes.Pos()) && !Ast.PositionIsSynthesized(nodes.End()));
        var open = GetOpenTokenForList(parent, nodes);
        var listIndentation = parentIndentation;
        var line = parentLine;
        if (!originalRange.Overlaps(nodes.Loc))
        {
            if (nodes.End() < originalRange.Pos() && (nodes.Nodes.Length == 0 || (nodes.Nodes[0].Flags & NodeFlags.Reparsed) == 0))
                formattingScanner.SkipToEndOf(nodes.Loc);
            return;
        }

        if (open != Kind.Unknown)
        {
            while (formattingScanner.IsOnToken() && formattingScanner.GetTokenFullStart() < originalRange.End())
            {
                var info = formattingScanner.ReadTokenInfo(parent);
                if (info.token.Loc.End() > nodes.Pos())
                    break;
                if (info.token.Kind == open)
                {
                    line = ScannerApi.GetECMALineOfPosition(sourceFile, info.token.Loc.Pos());
                    ConsumeTokenAndAdvanceScanner(info, parent, parentIndentation, parent, false);
                    var indentation = indentationOnLastIndentedLine != -1 ? indentationOnLastIndentedLine : GetCurrentIndentationAtPosition(info.token.Loc.Pos());
                    listIndentation = GetDynamicIndentation(parent, parentLine, indentation, formattingContext.Options.IndentSize);
                }
                else
                    ConsumeTokenAndAdvanceScanner(info, parent, parentIndentation, parent, false);
            }
        }

        long inherited = -1;
        for (var index = 0; index < nodes.Nodes.Length; index++)
            inherited = ProcessChildNode(node, indenter, nodeLine, undecoratedNodeLine, nodes.Nodes[index], inherited, node, listIndentation, line, line, true, index == 0);
        var close = GetCloseTokenForOpenToken(open);
        if (close != Kind.Unknown && formattingScanner.IsOnToken() && formattingScanner.GetTokenFullStart() < originalRange.End())
        {
            var info = formattingScanner.ReadTokenInfo(parent);
            if (info.token.Kind == Kind.CommaToken)
            {
                ConsumeTokenAndAdvanceScanner(info, parent, listIndentation, parent, false);
                if (formattingScanner.IsOnToken())
                    info = formattingScanner.ReadTokenInfo(parent);
                else
                    return;
            }

            if (info.token.Kind == close && info.token.Loc.ContainedBy(parent.Loc))
                ConsumeTokenAndAdvanceScanner(info, parent, listIndentation, parent, true);
        }
    }

    // span.go:916. Explicit synchronous state preserves callback ordering without a captured wrapper per invocation.
    private bool IndentTriviaItems<TState>(Slice<TextRangeWithKind> trivia, long indentation, bool indentNext, TState state, Action<TextRangeWithKind, TState> indentSingle)
    {
        foreach (var item in trivia)
        {
            var inRange = item.Loc.ContainedBy(originalRange);
            switch (item.Kind)
            {
                case Kind.MultiLineCommentTrivia:
                    if (inRange)
                        IndentMultilineComment(item.Loc, indentation, !indentNext, true);
                    indentNext = false;
                    break;
                case Kind.SingleLineCommentTrivia:
                    if (indentNext && inRange)
                        indentSingle(item, state);
                    indentNext = false;
                    break;
                case Kind.NewLineTrivia:
                    indentNext = true;
                    break;
            }
        }

        return indentNext;
    }

    // span.go:937. Preserve explicit CountLF capacity and original per-line replacement order.
    private void IndentMultilineComment(TextRange range, long indentation, bool firstIndented, bool indentFinal)
    {
        var startLine = ScannerApi.GetECMALineOfPosition(sourceFile, range.Pos());
        var endLine = ScannerApi.GetECMALineOfPosition(sourceFile, range.End());
        if (startLine == endLine)
        {
            if (!firstIndented)
                InsertIndentation(range.Pos(), indentation, false);
            return;
        }

        var text = sourceFile.TextView();
        var count = 0;
        foreach (var value in text.Slice(range.Pos(), range.End()).AsSpan())
            if (value == '\n')
                count++;
        var parts = Slice<TextRange>.Make(0, count);
        var pos = range.Pos();
        var starts = ScannerApi.GetECMALineStarts(sourceFile);
        for (var line = startLine; line < endLine; line++)
        {
            parts = parts.Append(new TextRange(pos, ScannerApi.GetECMAEndLinePosition(sourceFile, line)));
            pos = starts[line + 1];
        }

        if (indentFinal)
            parts = parts.Append(new TextRange(pos, range.End()));
        if (parts.Length == 0)
            return;
        var(firstCharacter, firstColumn) = FindFirstNonWhitespaceCharacterAndColumn(starts[startLine], parts[0].Pos(), sourceFile, formattingContext.Options);
        var firstIndex = 0;
        if (firstIndented)
        {
            firstIndex = 1;
            startLine++;
        }

        var shift = indentation - firstColumn;
        for (var index = firstIndex; index < parts.Length; index++)
        {
            var start = starts[startLine];
            var character = firstCharacter;
            var column = firstColumn;
            if (index != 0)
                (character, column) = FindFirstNonWhitespaceCharacterAndColumn(parts[index].Pos(), parts[index].End(), sourceFile, formattingContext.Options);
            var newIndentation = column + shift;
            if (newIndentation > 0)
                RecordReplace(start, character, GetIndentationString(newIndentation, formattingContext.Options));
            else
                RecordDelete(start, character);
            startLine++;
        }
    }

    // span.go:225. One visitor and hook pair belongs to each original Execute operation.
    internal Slice<TextChange> Execute(FormattingScanner scanner)
    {
        formattingScanner = scanner;
        indentationOnLastIndentedLine = -1;
        lastIndentedLine = -1;
        var options = GetFormatCodeSettingsFromContext(ctx);
        formattingContext = FormattingContext.NewFormattingContext(sourceFile, requestKind, options);
        visitor = new NodeVisitor(VisitChild, new NodeFactory(), new NodeVisitorHooks { VisitNodes = VisitChildren });
        scanner.Advance();
        if (scanner.IsOnToken())
        {
            var line = ScannerApi.GetECMALineOfPosition(sourceFile, WithTokenStart(enclosingNode, sourceFile).Pos());
            var undecoratedLine = Ast.HasDecorators(enclosingNode) ? ScannerApi.GetECMALineOfPosition(sourceFile, GetNonDecoratorTokenPosOfNode(enclosingNode, sourceFile)) : line;
            ProcessNode(enclosingNode, enclosingNode, line, undecoratedLine, initialIndentation, delta);
        }

        var remaining = scanner.GetCurrentLeadingTrivia();
        if (remaining.Length > 0)
        {
            var indentation = initialIndentation;
            if (NodeWillIndentChild(options, enclosingNode, null, sourceFile, false))
                indentation += options.IndentSize;
            IndentTriviaItems(remaining, indentation, true, (Worker: this, Indentation: indentation), static (item, state) =>
            {
                var worker = state.Worker;
                var(line, character) = ScannerApi.GetECMALineAndByteOffsetOfPosition(worker.sourceFile, item.Loc.Pos());
                worker.ProcessRange(item, line, character, worker.enclosingNode, worker.enclosingNode, null);
                worker.InsertIndentation(item.Loc.Pos(), state.Indentation, false);
            });
            if (options.TrimTrailingWhitespace == Tristate.True)
                TrimTrailingWhitespacesForRemainingRange(remaining);
        }

        if (!previousRange.Equals(default(TextRangeWithKind)) && scanner.GetTokenFullStart() >= originalRange.End())
        {
            TextRangeWithKind token = default;
            if (scanner.IsOnEOF())
                token = scanner.ReadEOFTokenRange();
            else if (scanner.IsOnToken())
                token = scanner.ReadTokenInfo(enclosingNode).token;
            // Only a contiguous pair can produce a valid edit at the end of the requested selection.
            if (token.Loc.Pos() == previousRangeTriviaEnd)
            {
                var parent = AstNav.FindPrecedingToken(sourceFile, token.Loc.End())?.Parent ?? previousParent;
                var line = ScannerApi.GetECMALineOfPosition(sourceFile, token.Loc.Pos());
                ProcessPair(token, line, parent, previousRange, previousRangeStartLine, previousParent, parent, null);
            }
        }

        return edits;
    }

    private Node VisitChild(Node child)
    {
        if (child == null)
            return child!;
        ProcessChildNode(visitingNode, visitingIndenter, visitingNodeStartLine, visitingUndecoratedNodeStartLine, child, -1, visitingNode, visitingIndenter, visitingNodeStartLine, visitingUndecoratedNodeStartLine, false, false);
        return child;
    }

    private NodeList VisitChildren(NodeList nodes, NodeVisitor visitor)
    {
        if (nodes == null)
            return nodes!;
        ProcessChildNodes(visitingNode, visitingIndenter, visitingNodeStartLine, visitingUndecoratedNodeStartLine, nodes, visitingNode, visitingNodeStartLine, visitingIndenter);
        return nodes;
    }

    // span.go:1042.
    private void ConsumeTokenAndAdvanceScanner(TokenInfo info, Node parent, DynamicIndenter indenter, Node container, bool listEnd)
    {
        var lastNewLine = formattingScanner.LastTrailingTriviaWasNewLine();
        var indentToken = false;
        if (info.leadingTrivia.Length > 0)
            ProcessTrivia(info.leadingTrivia, parent, childContextNode, indenter);
        var action = LineAction.None;
        var tokenInRange = info.token.Loc.ContainedBy(originalRange);
        var(line, character) = ScannerApi.GetECMALineAndByteOffsetOfPosition(sourceFile, info.token.Loc.Pos());
        if (tokenInRange)
        {
            var hasError = rangeContainsError(info.token.Loc);
            var previous = previousRange;
            action = ProcessRange(info.token, line, character, parent, childContextNode, indenter);
            if (!hasError)
            {
                if (action == LineAction.None)
                    indentToken = previous.Equals(default(TextRangeWithKind)) ? lastNewLine : lastNewLine && line != ScannerApi.GetECMALineOfPosition(sourceFile, previous.Loc.End());
                else
                    indentToken = action == LineAction.LineAdded;
            }
        }

        if (info.trailingTrivia.Length > 0)
        {
            previousRangeTriviaEnd = info.trailingTrivia[info.trailingTrivia.Length - 1].Loc.End();
            foreach (var trivia in info.trailingTrivia)
                if (IsComment(trivia.Kind) && !trivia.Loc.ContainedBy(originalRange))
                {
                    previousRangeTriviaEnd = trivia.Loc.Pos();
                    break;
                }

            ProcessTrivia(info.trailingTrivia, parent, childContextNode, indenter);
        }

        if (indentToken)
        {
            long indentation = -1;
            if (tokenInRange && !rangeContainsError(info.token.Loc))
                indentation = indenter.GetIndentationForToken(line, info.token.Kind, container, listEnd);
            var indentNext = true;
            if (info.leadingTrivia.Length > 0)
            {
                var commentIndentation = indenter.GetIndentationForComment(info.token.Kind, indentation, container);
                indentNext = IndentTriviaItems(info.leadingTrivia, commentIndentation, indentNext, (Worker: this, Indentation: commentIndentation), static (item, state) => state.Worker.InsertIndentation(item.Loc.Pos(), state.Indentation, false));
            }

            if (indentation != -1 && indentNext)
            {
                InsertIndentation(info.token.Loc.Pos(), indentation, action == LineAction.LineAdded);
                lastIndentedLine = line;
                indentationOnLastIndentedLine = indentation;
            }
        }

        formattingScanner.Advance();
        childContextNode = parent;
    }
}

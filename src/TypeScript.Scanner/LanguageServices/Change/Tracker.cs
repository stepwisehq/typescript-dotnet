// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/change/tracker.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.DiagnosticPrinting;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.LanguageServices.Utilities;
using TypeScript.AstNavigation;
using TypeScript.SpanMap;
using Kind = TypeScript.Scanner.Kind;
using ScannerApi = TypeScript.Scanner.Scanner;
using TypeScript.Lsp.Protocol;
using LspRange = TypeScript.Lsp.Protocol.Range;
using NodeFactory = TypeScript.Scanner.NodeFactory;

namespace TypeScript.LanguageServices.Change;
// tracker.go:24. Optional integer fields retain pointer absence separately from zero.
internal struct NodeOptions
{
    internal ByteStringValue Prefix;
    internal ByteStringValue Suffix;
    internal long? indentation;
    internal long? delta;
    internal LeadingTriviaOption LeadingTriviaOption;
    internal TrailingTriviaOption TrailingTriviaOption;
    internal ByteStringValue joiner;
}

internal enum LeadingTriviaOption
{
    None,
    Exclude,
    IncludeAll,
    JSDoc,
    StartLine
}

internal enum TrailingTriviaOption
{
    None,
    Exclude,
    ExcludeWhitespace,
    Include
}

internal enum TrackerEditKind
{
    Text = 1,
    Remove,
    ReplaceWithSingleNode,
    ReplaceWithMultipleNodes
}

internal sealed class TrackerEdit
{
    internal TrackerEditKind kind;
    internal TextRange TextRange;
    internal ByteStringValue NewText;
    internal Node? Node;
    internal Slice<Node> nodes;
    internal NodeOptions options;
}

internal sealed class NodesInsertedAtStartState
{
    internal required Node node;
    internal required SourceFile sourceFile;
}

internal readonly record struct DeletedNode(SourceFile sourceFile, Node node);
internal sealed partial class Tracker : NodeFactoryCoercible
{
    internal FormatCodeSettings formatSettings;
    internal ByteStringValue newLine;
    internal Converters converters = null!;
    internal ProtocolRequestContext ctx = null!;
    internal EmitContext EmitContext = null!;
    internal NodeFactory NodeFactory = null!;
    private MultiMap<SourceFile, TrackerEdit> changes = null!;
    private Slice<DeletedNode> deletedNodes;
    private Dictionary<Node, NodesInsertedAtStartState> nodesWithInsertionsAtStart = null!;
    private HashSet<RootedFilePath>? unmappableFiles;
    // Upstream retains an unused writer slot; no extra writer owner is created.
#pragma warning disable CS0169, CS0649
    private ChangeTrackerWriter? writer;
#pragma warning restore CS0169, CS0649
    public NodeFactory AsNodeFactory() => NodeFactory;
    // tracker.go:114. Factory, formatting context and mutable edit owners share this operation lifetime.
    internal static Tracker NewTracker(ProtocolRequestContext ctx, CompilerOptions options, FormatCodeSettings formatOptions, Converters converters)
    {
        var emitContext = new EmitContext();
        var newLine = ByteStringValue.FromString(options.NewLine.GetNewLineCharacter());
        return new Tracker
        {
            formatSettings = formatOptions,
            newLine = newLine,
            converters = converters,
            ctx = Formatting.Formatting.WithFormatCodeSettings(ctx, formatOptions, newLine),
            EmitContext = emitContext,
            NodeFactory = emitContext.Factory.AsNodeFactory(),
            changes = new(),
            nodesWithInsertionsAtStart = new(),
        };
    }

    // tracker.go:135. Unmappable projections invalidate all edits for that original file.
    internal (Dictionary<RootedFilePath, Slice<TextEdit>>, Slice<RootedFilePath>) GetChanges()
    {
        finishDeleteDeclarations();
        finishNodesWithInsertionsAtStart();
        var result = getTextChangesFromChanges();
        if (unmappableFiles is null || unmappableFiles.Count == 0)
            return (result, default);
        var unmappable = Slice<RootedFilePath>.Make(0, unmappableFiles.Count);
        foreach (var fileName in unmappableFiles)
        {
            result.Remove(fileName);
            unmappable = unmappable.Append(fileName);
        }

        GoSlices.SortFunc(unmappable, static (a, b) => a.Compare(b));
        return (result, unmappable);
    }

    // tracker.go:155.
    private TextRange fromLSPEditRange(SourceFile sourceFile, LspRange range)
    {
        var spans = converters.FromLSPRangeForSourceFile(sourceFile, range, TypeScript.SpanMap.Feature.All);
        foreach (var span in spans)
        {
            if (span.Fidelity.IsExact() && ReferenceEquals(span.Script, sourceFile))
                return span.Span;
        }

        (unmappableFiles ??= new()).Add(sourceFile.OriginalFileName());
        return spans.Length > 0 ? spans[0].Span : new TextRange(0, 0);
    }

    // tracker.go:173.
    private LspRange toLSPEditRange(SourceFile sourceFile, TextRange range)
    {
        var(mapped, fidelity) = converters.ToLSPRange(sourceFile, range);
        if (!fidelity.IsExact())
            (unmappableFiles ??= new()).Add(sourceFile.OriginalFileName());
        return mapped;
    }

    // tracker.go:184.
    internal void ReplaceNode(SourceFile sourceFile, Node oldNode, Node newNode, NodeOptions? options)
    {
        var effective = options ?? new NodeOptions
        {
            LeadingTriviaOption = LeadingTriviaOption.Exclude,
            TrailingTriviaOption = TrailingTriviaOption.Exclude,
        };
        ReplaceRange(sourceFile, GetAdjustedRange(sourceFile, oldNode, oldNode, effective.LeadingTriviaOption, effective.TrailingTriviaOption), newNode, effective);
    }

    // tracker.go:195.
    internal void ReplaceNodeWithNodes(SourceFile sourceFile, Node oldNode, Slice<Node> newNodes, NodeOptions? options)
    {
        var effective = options ?? new NodeOptions
        {
            LeadingTriviaOption = LeadingTriviaOption.Exclude,
            TrailingTriviaOption = TrailingTriviaOption.Exclude,
        };
        ReplaceRangeWithNodes(sourceFile, GetAdjustedRange(sourceFile, oldNode, oldNode, effective.LeadingTriviaOption, effective.TrailingTriviaOption), newNodes, effective);
    }

    // tracker.go:206.
    internal void ReplaceRange(SourceFile sourceFile, TextRange range, Node newNode, NodeOptions options) => changes.Add(sourceFile, new TrackerEdit { kind = TrackerEditKind.ReplaceWithSingleNode, TextRange = range, Node = newNode, options = options, });
    // tracker.go:212.
    internal void ReplaceRangeWithText(SourceFile sourceFile, LspRange range, ByteStringValue text) => ReplaceTextRangeWithText(sourceFile, fromLSPEditRange(sourceFile, range), text);
    // tracker.go:218.
    internal void ReplaceTextRangeWithText(SourceFile sourceFile, TextRange range, ByteStringValue text) => changes.Add(sourceFile, new TrackerEdit { kind = TrackerEditKind.Text, TextRange = range, NewText = text, });
    // tracker.go:223. The multiple-node owner retains the original slice header.
    internal void ReplaceRangeWithNodes(SourceFile sourceFile, TextRange range, Slice<Node> newNodes, NodeOptions options)
    {
        if (newNodes.Length == 1)
        {
            ReplaceRange(sourceFile, range, newNodes[0], options);
            return;
        }

        changes.Add(sourceFile, new TrackerEdit { kind = TrackerEditKind.ReplaceWithMultipleNodes, TextRange = range, nodes = newNodes, options = options, });
    }

    // tracker.go:232/237/241/245.
    private void insertTextAt(SourceFile sourceFile, int pos, ByteStringValue text) => ReplaceTextRangeWithText(sourceFile, new TextRange(pos, pos), text);
    internal void InsertText(SourceFile sourceFile, Position pos, ByteStringValue text) => ReplaceRangeWithText(sourceFile, new LspRange { Start = pos, End = pos }, text);
    internal void InsertNodeAt(SourceFile sourceFile, int pos, Node newNode, NodeOptions options) => ReplaceRange(sourceFile, new TextRange(pos, pos), newNode, options);
    internal void InsertNodesAt(SourceFile sourceFile, int pos, Slice<Node> newNodes, NodeOptions options) => ReplaceRangeWithNodes(sourceFile, new TextRange(pos, pos), newNodes, options);
    // tracker.go:249/254/259.
    internal void InsertNodeAfter(SourceFile sourceFile, Node after, Node newNode) => InsertNodeAt(sourceFile, endPosForInsertNodeAfter(sourceFile, after, newNode), newNode, getInsertNodeAfterOptions(sourceFile, after));
    internal void InsertNodesAfter(SourceFile sourceFile, Node after, Slice<Node> newNodes) => InsertNodesAt(sourceFile, endPosForInsertNodeAfter(sourceFile, after, newNodes[0]), newNodes, getInsertNodeAfterOptions(sourceFile, after));
    internal void InsertNodeBefore(SourceFile sourceFile, Node before, Node newNode, bool blankLineBetween, LeadingTriviaOption leadingTriviaOption) => InsertNodeAt(sourceFile, getAdjustedStartPosition(sourceFile, before, leadingTriviaOption, false), newNode, getOptionsForInsertNodeBefore(before, newNode, blankLineBetween));
    // tracker.go:266.
    internal bool TryInsertTypeAnnotation(SourceFile sourceFile, Node node, Node typeNode)
    {
        Node? endNode = null;
        if (Ast.IsFunctionLike(node))
        {
            endNode = AstNav.FindChildOfKind(node, Kind.CloseParenToken, sourceFile);
            if (endNode is null)
            {
                if (!Ast.IsArrowFunction(node))
                    return false;
                var parameters = node.Parameters();
                if (parameters.Length == 0)
                    return false;
                endNode = parameters[0];
            }
        }
        else
        {
            switch (node.Kind)
            {
                case Kind.VariableDeclaration:
                    endNode = node.AsVariableDeclaration().ExclamationToken;
                    break;
                case Kind.PropertySignature:
                    endNode = node.AsPropertySignatureDeclaration().PostfixToken;
                    break;
                case Kind.PropertyDeclaration:
                    endNode = node.AsPropertyDeclaration().PostfixToken;
                    break;
                case Kind.Parameter:
                    endNode = node.AsParameterDeclaration().QuestionToken;
                    break;
            }

            endNode ??= node.Name();
        }

        if (endNode is null)
            return false;
        InsertNodeAt(sourceFile, endNode.End(), typeNode, new NodeOptions { Prefix = ByteStringValue.FromString(": ") });
        return true;
    }

    // tracker.go:305.
    internal void ParenthesizeArrowParameters(SourceFile sourceFile, Node arrowFunc)
    {
        if (AstNav.FindChildOfKind(arrowFunc, Kind.CloseParenToken, sourceFile)is not null)
            return;
        var parameters = arrowFunc.Parameters();
        if (parameters.Length == 0)
            return;
        int startPos = AstNav.GetStartOfNode(parameters[0], sourceFile, false);
        insertTextAt(sourceFile, startPos, ByteStringValue.FromString("("));
        insertTextAt(sourceFile, parameters[parameters.Length - 1].End(), ByteStringValue.FromString(")"));
    }

    // tracker.go:321.
    internal void InsertModifierBefore(SourceFile sourceFile, Kind modifier, Node before)
    {
        int pos = AstNav.GetStartOfNode(before, sourceFile, false);
        var token = NodeFactory.NewToken(modifier);
        token.Loc = new TextRange(pos, pos);
        token.Parent = before.Parent;
        InsertNodeAt(sourceFile, pos, token, new NodeOptions { Suffix = ByteStringValue.FromString(" ") });
    }

    // tracker.go:331/336/342/347.
    internal void Delete(SourceFile sourceFile, Node node) => deletedNodes = deletedNodes.Append(new DeletedNode(sourceFile, node));
    internal void DeleteRange(SourceFile sourceFile, TextRange range) => ReplaceTextRangeWithText(sourceFile, range, default);
    internal void DeleteNode(SourceFile sourceFile, Node node, LeadingTriviaOption leadingTrivia, TrailingTriviaOption trailingTrivia) => ReplaceTextRangeWithText(sourceFile, GetAdjustedRange(sourceFile, node, node, leadingTrivia, trailingTrivia), default);
    internal void DeleteNodeRange(SourceFile sourceFile, Node startNode, Node endNode, LeadingTriviaOption leadingTrivia, TrailingTriviaOption trailingTrivia) => ReplaceTextRangeWithText(sourceFile, new TextRange(getAdjustedStartPosition(sourceFile, startNode, leadingTrivia, false), getAdjustedEndPosition(sourceFile, endNode, trailingTrivia)), default);
    // tracker.go:354. This is an explicit make(map), unlike a zero collections.Set.
    private void finishDeleteDeclarations()
    {
        var deletedNodesInLists = new Dictionary<Node, bool>();
        foreach (var deleted in deletedNodes)
        {
            bool contained = false;
            foreach (var other in deletedNodes)
            {
                if (ReferenceEquals(other.sourceFile, deleted.sourceFile) && !ReferenceEquals(other.node, deleted.node) && rangeContainsRangeExclusive(other.node, deleted.node))
                {
                    contained = true;
                    break;
                }
            }

            if (!contained)
                deleteDeclaration(this, deletedNodesInLists, deleted.sourceFile, deleted.node);
        }

        foreach (var node in deletedNodesInLists.Keys)
        {
            var sourceFile = Ast.GetSourceFileOfNode(node);
            var list = Formatting.Formatting.GetContainingList(node, sourceFile);
            if (list is null || !ReferenceEquals(node, list.Nodes[list.Nodes.Length - 1]))
                continue;
            int lastNonDeletedIndex = -1;
            for (int j = list.Nodes.Length - 2; j >= 0; j--)
            {
                if (!deletedNodesInLists.GetValueOrDefault(list.Nodes[j]))
                {
                    lastNonDeletedIndex = j;
                    break;
                }
            }

            if (lastNonDeletedIndex != -1)
                ReplaceTextRangeWithText(sourceFile, new TextRange(list.Nodes[lastNonDeletedIndex].End(), startPositionToDeleteNodeInList(sourceFile, list.Nodes[lastNonDeletedIndex + 1])), default);
        }
    }

    // tracker.go:398.
    private int endPosForInsertNodeAfter(SourceFile sourceFile, Node after, Node newNode)
    {
        if (needSemicolonBetween(after, newNode) && ByteStringValue.FromView(sourceFile.TextView())[after.End() - 1] != ';')
        {
            var semicolon = NodeFactory.NewToken(Kind.SemicolonToken);
            semicolon.Loc = new TextRange(after.End(), after.End());
            semicolon.Parent = after.Parent;
            ReplaceRange(sourceFile, new TextRange(after.End(), after.End()), semicolon, default);
        }

        return getAdjustedEndPosition(sourceFile, after, TrailingTriviaOption.None);
    }

    // tracker.go:421. Separator edits preserve the next element's exact retained trivia.
    internal void InsertNodeInListAfter(SourceFile sourceFile, Node after, Node newNode, NodeList? containingList)
    {
        containingList ??= Formatting.Formatting.GetContainingList(after, sourceFile);
        if (containingList is null)
            return;
        int index = GoSlices.Index(containingList.Nodes, after);
        if (index < 0)
            return;
        int end = after.End();
        if (index != containingList.Nodes.Length - 1)
        {
            var nextToken = AstNav.GetTokenAtPosition(sourceFile, after.End());
            if (nextToken is not null && isSeparator(after, nextToken))
            {
                var nextNode = containingList.Nodes[index + 1];
                int startPos = ScannerApi.SkipTriviaEx(sourceFile.TextView(), nextNode.Pos(), new SkipTriviaOptions { StopAfterLineBreak = false, StopAtComments = true });
                var suffix = ByteStringValue.Concat(ByteStringValue.FromString(ScannerApi.TokenToString(nextToken.Kind)), ByteStringValue.FromView(sourceFile.TextView()).Slice(nextToken.End(), startPos));
                InsertNodesAt(sourceFile, startPos, Slice<Node>.FromArray([newNode]), new NodeOptions { Suffix = suffix });
            }

            return;
        }

        int afterStart = AstNav.GetStartOfNode(after, sourceFile, false);
        int afterLineStart = Formatting.Formatting.GetLineStartPositionForPosition(afterStart, sourceFile);
        bool multiline = false;
        var separator = Kind.CommaToken;
        if (containingList.Nodes.Length != 1)
        {
            var preceding = AstNav.FindPrecedingToken(sourceFile, after.Pos());
            separator = isSeparator(after, preceding) ? preceding.Kind : Kind.CommaToken;
            int previousLineStart = Formatting.Formatting.GetLineStartPositionForPosition(AstNav.GetStartOfNode(containingList.Nodes[index - 1], sourceFile, false), sourceFile);
            multiline = previousLineStart != afterLineStart;
        }

        if (hasCommentsBeforeLineBreak(ByteStringValue.FromView(sourceFile.TextView()), after.End()) || !positionsAreOnSameLine(containingList.Pos(), containingList.End(), sourceFile))
            multiline = true;
        var separatorText = ByteStringValue.FromString(ScannerApi.TokenToString(separator));
        if (multiline)
        {
            var separatorToken = NodeFactory.NewToken(separator);
            separatorToken.Loc = new TextRange(end, end + separatorText.Length);
            separatorToken.Parent = after.Parent;
            ReplaceRange(sourceFile, new TextRange(end, end), separatorToken, default);
            long indentation = Formatting.Formatting.FindFirstNonWhitespaceColumn(afterLineStart, afterStart, sourceFile, formatSettings);
            int insertPos = ScannerApi.SkipTriviaEx(sourceFile.TextView(), end, new SkipTriviaOptions { StopAfterLineBreak = true, StopAtComments = false });
            while (insertPos != end && StringUtil.IsLineBreak(ByteStringValue.FromView(sourceFile.TextView())[insertPos - 1]))
                insertPos--;
            ReplaceRange(sourceFile, new TextRange(insertPos, insertPos), newNode, new NodeOptions { indentation = indentation, Prefix = newLine });
        }
        else
            ReplaceRange(sourceFile, new TextRange(end, end), newNode, new NodeOptions { Prefix = ByteStringValue.Concat(separatorText, ByteStringValue.FromString(" ")) });
    }

    // tracker.go:523.
    internal void InsertImportSpecifierAtIndex(SourceFile sourceFile, Node newSpecifier, Node namedImports, int index)
    {
        var elements = namedImports.AsNamedImports().Elements.Nodes;
        Node? previous = index > 0 && index - 1 < elements.Length ? elements[index - 1] : null;
        if (previous is not null)
            InsertNodeInListAfter(sourceFile, previous, newSpecifier, null);
        else
            InsertNodeBefore(sourceFile, elements[0], newSpecifier, !positionsAreOnSameLine(AstNav.GetStartOfNode(elements[0], sourceFile, false), AstNav.GetStartOfNode(namedImports.Parent.Parent, sourceFile, false), sourceFile), LeadingTriviaOption.None);
    }

    // tracker.go:544. A synthesized header must not become the insertion's original position.
    internal void InsertAtTopOfFile(SourceFile sourceFile, Slice<Node> insert, bool blankLineBetween)
    {
        if (insert.Length == 0)
            return;
        int pos = getInsertionPositionAtSourceFileTop(sourceFile);
        int originalPos = pos;
        if (sourceFile.SpanMap()is { } spanMap && spanMap.Segments()is { } segments)
        {
            foreach (var segment in segments)
            {
                if (segment.Kind != TypeScript.SpanMap.Kind.Verbatim || segment.VirtualEnd <= pos)
                    continue;
                if (segment.VirtualStart > pos)
                    pos = segment.VirtualStart;
                originalPos = segment.OriginalStart + pos - segment.VirtualStart;
                break;
            }
        }

        NodeOptions options = default;
        if (originalPos != 0)
            options.Prefix = newLine;
        if (ByteStringValue.FromView(sourceFile.TextView()).Length == 0 || !StringUtil.IsLineBreak(ByteStringValue.FromView(sourceFile.TextView())[pos]))
            options.Suffix = newLine;
        if (blankLineBetween)
            options.Suffix = ByteStringValue.Concat(options.Suffix, newLine);
        if (insert.Length == 1)
            InsertNodeAt(sourceFile, pos, insert[0], options);
        else
            InsertNodesAt(sourceFile, pos, insert, options);
    }

    // tracker.go:583/587.
    internal void InsertMemberAtStart(SourceFile sourceFile, Node node, Node newElement) => insertNodeAtStartWorker(sourceFile, node, newElement);
    private void insertNodeAtStartWorker(SourceFile sourceFile, Node node, Node newElement)
    {
        long indentation = tryComputeIndentationFromExistingMembers(sourceFile, node);
        if (indentation < 0)
            indentation = tryComputeIndentationForNewMember(sourceFile, node);
        var members = getMembersOrProperties(node);
        if (members is null)
            return;
        InsertNodeAt(sourceFile, members.Pos(), newElement, getInsertNodeAtStartInsertOptions(sourceFile, node, indentation));
    }

    // tracker.go:601.
    private long tryComputeIndentationForNewMember(SourceFile sourceFile, Node node)
    {
        int start = AstNav.GetStartOfNode(node, sourceFile, false);
        int lineStart = Formatting.Formatting.GetLineStartPositionForPosition(start, sourceFile);
        long tabSize = formatSettings.TabSize <= 0 ? 4 : formatSettings.TabSize;
        long indentSize = formatSettings.IndentSize <= 0 ? 4 : formatSettings.IndentSize;
        return Math.Max(findIndentationColumn(ByteStringValue.FromView(sourceFile.TextView()), lineStart, start, tabSize), 0) + indentSize;
    }

    // tracker.go:617.
    private long tryComputeIndentationFromExistingMembers(SourceFile sourceFile, Node node)
    {
        var members = getMembersOrProperties(node);
        if (members is null)
            return -1;
        long indentation = -1;
        long tabSize = formatSettings.TabSize <= 0 ? 4 : formatSettings.TabSize;
        var last = node;
        foreach (var member in members.Nodes)
        {
            if (member is null)
                continue;
            if (PrinterUtilities.RangeStartPositionsAreOnSameLine(last.Loc, member.Loc, sourceFile))
                return -1;
            int memberStart = AstNav.GetStartOfNode(member, sourceFile, false);
            int lineStart = Formatting.Formatting.GetLineStartPositionForPosition(memberStart, sourceFile);
            long column = findIndentationColumn(ByteStringValue.FromView(sourceFile.TextView()), lineStart, memberStart, tabSize);
            if (column < 0)
                return -1;
            if (indentation >= 0)
            {
                if (indentation != column)
                    return -1;
                last = member;
                continue;
            }

            indentation = column;
            last = member;
        }

        return indentation;
    }

    // tracker.go:662.
    private NodeOptions getInsertNodeAfterOptions(SourceFile sourceFile, Node node)
    {
        NodeOptions options;
        switch (node.Kind)
        {
            case Kind.Parameter:
                options = default;
                break;
            case Kind.ClassDeclaration:
            case Kind.ModuleDeclaration:
                options = new NodeOptions
                {
                    Prefix = newLine,
                    Suffix = newLine
                };
                break;
            case Kind.VariableDeclaration:
            case Kind.StringLiteral:
            case Kind.Identifier:
                options = new NodeOptions
                {
                    Prefix = ByteStringValue.FromString(", ")
                };
                break;
            case Kind.PropertyAssignment:
                options = new NodeOptions
                {
                    Suffix = ByteStringValue.Concat(ByteStringValue.FromString(","), newLine)
                };
                break;
            case Kind.ExportKeyword:
                options = new NodeOptions
                {
                    Prefix = ByteStringValue.FromString(" ")
                };
                break;
            default:
                if (!(Ast.IsStatement(node) || Ast.IsClassOrTypeElement(node)))
                    throw new InvalidOperationException("unimplemented node type " + node.Kind.String() + " in changeTracker.getInsertNodeAfterOptions");
                options = new NodeOptions
                {
                    Suffix = newLine
                };
                break;
        }

        if (node.End() == sourceFile.End() && Ast.IsStatement(node))
            options.Prefix = ByteStringValue.Concat(newLine, options.Prefix);
        return options;
    }

    // tracker.go:695.
    private NodeOptions getOptionsForInsertNodeBefore(Node before, Node inserted, bool blankLineBetween)
    {
        if (Ast.IsStatement(before) || Ast.IsClassOrTypeElement(before))
            return new NodeOptions
            {
                Suffix = blankLineBetween ? ByteStringValue.Concat(newLine, newLine) : newLine
            };
        if (before.Kind == Kind.VariableDeclaration)
            return new NodeOptions
            {
                Suffix = ByteStringValue.FromString(", ")
            };
        if (before.Kind == Kind.Parameter)
            return inserted.Kind == Kind.Parameter ? new NodeOptions
            {
                Suffix = ByteStringValue.FromString(", ")
            }

            : default;
        if ((before.Kind == Kind.StringLiteral && before.Parent is not null && before.Parent.Kind == Kind.ImportDeclaration) || before.Kind == Kind.NamedImports)
            return new NodeOptions
            {
                Suffix = ByteStringValue.FromString(", ")
            };
        if (before.Kind == Kind.ImportSpecifier)
            return new NodeOptions
            {
                Suffix = ByteStringValue.Concat(ByteStringValue.FromString(","), blankLineBetween ? newLine : ByteStringValue.FromString(" "))
            };
        throw new InvalidOperationException("unimplemented node type " + before.Kind.String() + " in changeTracker.getOptionsForInsertNodeBefore");
    }

    // tracker.go:724.
    private NodeOptions getInsertNodeAtStartInsertOptions(SourceFile sourceFile, Node node, long indentation)
    {
        var state = nodesWithInsertionsAtStart.GetValueOrDefault(node);
        bool previous = state is not null;
        if (state is null)
        {
            state = new NodesInsertedAtStartState
            {
                node = node,
                sourceFile = sourceFile
            };
            nodesWithInsertionsAtStart[node] = state;
        }

        var members = getMembersOrProperties(node);
        bool objectLiteral = Ast.IsObjectLiteralExpression(node);
        bool json = Ast.IsJsonSourceFile(sourceFile);
        bool hasMembers = members is not null && members.Nodes.Length > 0;
        bool trailingComma = objectLiteral && (hasMembers || !json);
        bool leadingComma = objectLiteral && json && !hasMembers && previous;
        var suffix = trailingComma ? ByteStringValue.FromString(",") : Ast.IsInterfaceDeclaration(node) && !hasMembers ? ByteStringValue.FromString(";") : default;
        var prefix = leadingComma ? ByteStringValue.Concat(ByteStringValue.FromString(","), newLine) : newLine;
        return new NodeOptions
        {
            indentation = indentation,
            Prefix = prefix,
            Suffix = suffix
        };
    }

    // tracker.go:759.
    private void finishNodesWithInsertionsAtStart()
    {
        foreach (var state in nodesWithInsertionsAtStart.Values)
        {
            if (state is null)
                continue;
            var open = AstNav.FindChildOfKind(state.node, Kind.OpenBraceToken, state.sourceFile);
            if (open is null)
                continue;
            var close = AstNav.FindChildOfKind(state.node, Kind.CloseBraceToken, state.sourceFile);
            if (close is null)
                continue;
            var members = getMembersOrProperties(state.node);
            bool empty = members is null || members.Nodes.Length == 0;
            bool singleLine = positionsAreOnSameLine(open.End(), close.End(), state.sourceFile);
            if (empty && singleLine && open.End() != close.End() - 1)
                DeleteRange(state.sourceFile, new TextRange(open.End(), close.End() - 1));
            if (singleLine)
                insertTextAt(state.sourceFile, close.End() - 1, newLine);
        }
    }

    // tracker.go:789/796/800.
    private static NodeList? getMembersOrProperties(Node node) => Ast.IsObjectLiteralExpression(node) ? node.PropertyList() : node.MemberList();
    private static bool rangeContainsRangeExclusive(Node outer, Node inner) => outer.Pos() < inner.Pos() && inner.End() < outer.End();
    private static bool isSeparator(Node node, Node? candidate) => candidate is not null && node.Parent is not null && (candidate.Kind == Kind.CommaToken || candidate.Kind == Kind.SemicolonToken && node.Parent.Kind == Kind.ObjectLiteralExpression);
    // tracker.go:804/823. Source scans individual bytes, not decoded Unicode runes.
    private static long findIndentationColumn(ByteStringValue text, int lineStart, int memberStart, long tabSize)
    {
        long column = 0;
        for (int j = lineStart; j < memberStart && j < text.Length; j++)
        {
            int ch = text[j];
            if (StringUtil.IsLineBreak(ch))
                return -1;
            if (StringUtil.IsWhiteSpaceSingleLine(ch))
            {
                column = advanceIndentationColumn(column, ch, tabSize);
                continue;
            }

            return column;
        }

        return column;
    }

    private static long advanceIndentationColumn(long column, int ch, long tabSize) => ch == '\t' ? column + tabSize - column % tabSize : column + 1;
}

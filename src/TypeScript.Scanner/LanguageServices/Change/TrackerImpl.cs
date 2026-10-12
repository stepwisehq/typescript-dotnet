// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/change/trackerimpl.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.AstNavigation;
using TypeScript.SpanMap;
using Kind = TypeScript.Scanner.Kind;
using ScannerApi = TypeScript.Scanner.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.LanguageServices.Utilities;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.Lsp.Protocol;

namespace TypeScript.LanguageServices.Change;
internal sealed partial class Tracker
{
    // trackerimpl.go:23.
    private Dictionary<RootedFilePath, Slice<TextEdit>> getTextChangesFromChanges()
    {
        var result = new Dictionary<RootedFilePath, Slice<TextEdit>>();
        var projections = new Dictionary<RootedFilePath, int>();
        if (changes.M is not null)
        {
            foreach (var(sourceFile, changesInFile)in changes.M)
            {
                var fileName = sourceFile.OriginalFileName();
                if (unmappableFiles?.Contains(fileName) == true)
                    continue;
                Slice<TextEdit> textChanges = default;
                foreach (var change in changesInFile)
                    textChanges = textChanges.Append(new TextEdit { NewText = computeNewText(change, sourceFile, sourceFile), Range = toLSPEditRange(sourceFile, change.TextRange), });
                if (textChanges.Length > 0)
                {
                    result[fileName] = result.GetValueOrDefault(fileName).Append(textChanges);
                    projections[fileName] = projections.GetValueOrDefault(fileName) + 1;
                }
            }
        }

        foreach (var(fileName, originalEdits)in result)
        {
            if (unmappableFiles?.Contains(fileName) == true)
                continue;
            var edits = originalEdits;
            GoSlices.SortStableFunc(edits, static (a, b) => ProtocolUtilities.CompareRanges(a.Range, b.Range));
            bool multiple = projections[fileName] > 1;
            if (multiple)
            {
                edits = dedupeIdenticalEdits(edits);
                result[fileName] = edits;
            }

            for (int i = 0; i < edits.Length - 1; i++)
            {
                if (!textEditsConflict(edits[i], edits[i + 1], multiple))
                    continue;
                if (multiple)
                {
                    (unmappableFiles ??= new()).Add(fileName);
                    break;
                }

                throw new InvalidOperationException($"changes overlap: {formatRange(edits[i].Range)} and {formatRange(edits[i + 1].Range)}");
            }
        }

        return result;
    }

    // trackerimpl.go:92. Compaction reuses the source slice's backing owner.
    private static Slice<TextEdit> dedupeIdenticalEdits(Slice<TextEdit> edits)
    {
        var deduped = edits.Slice2(0, 0);
        foreach (var edit in edits)
        {
            if (deduped.Length > 0 && ProtocolUtilities.CompareRanges(deduped[deduped.Length - 1].Range, edit.Range) == 0 && deduped[deduped.Length - 1].NewText.EqualsValue(edit.NewText))
                continue;
            deduped = deduped.Append(edit);
        }

        return deduped;
    }

    // trackerimpl.go:103.
    internal static bool textEditsConflict(TextEdit a, TextEdit b, bool multipleProjections) => ProtocolUtilities.ComparePositions(a.Range.End, b.Range.Start) > 0 || multipleProjections && ProtocolUtilities.ComparePositions(a.Range.Start, a.Range.End) == 0 && ProtocolUtilities.CompareRanges(a.Range, b.Range) == 0 && !a.NewText.EqualsValue(b.NewText);
    // trackerimpl.go:114. Every exact projection must produce the same original edit.
    private ByteStringValue computeNewText(TrackerEdit change, SourceFile targetFile, SourceFile sourceFile)
    {
        if (change.kind == TrackerEditKind.Remove)
            return default;
        if (change.kind == TrackerEditKind.Text)
            return change.NewText;
        var positions = converters.FromLSPPositionForSourceFile(sourceFile, toLSPEditRange(sourceFile, change.TextRange).Start, TypeScript.SpanMap.Feature.All);
        ByteStringValue result = default;
        bool found = false;
        foreach (var mapped in positions)
        {
            if (!mapped.Fidelity.IsExact())
                continue;
            var projection = mapped.Script;
            int pos = mapped.Position;
            ByteStringValue text;
            switch (change.kind)
            {
                case TrackerEditKind.ReplaceWithMultipleNodes:
                    var joiner = change.options.joiner.Length == 0 ? newLine : change.options.joiner;
                    var formatted = change.nodes.IsNil ? default : Slice<ByteStringValue>.Make(change.nodes.Length);
                    for (int j = 0; j < change.nodes.Length; j++)
                        formatted[j] = GoStrings.TrimSuffix(getFormattedTextOfNode(change.nodes[j], targetFile, projection, pos, change.options), newLine);
                    if (formatted.Length == 0)
                        text = default;
                    else if (formatted.Length == 1)
                        text = formatted[0];
                    else
                    {
                        int length = checked(joiner.Length * (formatted.Length - 1));
                        foreach (var part in formatted)
                            length = checked(length + part.Length);
                        var builder = new GoBuilder();
                        builder.Grow(length);
                        for (int i = 0; i < formatted.Length; i++)
                        {
                            if (i > 0)
                                builder.WriteString(joiner);
                            builder.WriteString(formatted[i]);
                        }

                        text = builder.Value();
                    }

                    break;
                case TrackerEditKind.ReplaceWithSingleNode:
                    text = getFormattedTextOfNode(change.Node!, targetFile, projection, pos, change.options);
                    break;
                default:
                    throw new InvalidOperationException($"change kind {(int)change.kind} should have been handled earlier");
            }

            var noIndent = text;
            if (!(change.options.indentation is not null || Formatting.Formatting.GetLineStartPositionForPosition(pos, projection) == pos))
                noIndent = trimLeadingSpace(text);
            var candidate = ByteStringValue.Concat(change.options.Prefix, noIndent, noIndent.EndsWith(change.options.Suffix) ? default : change.options.Suffix);
            if (found && !candidate.EqualsValue(result))
            {
                (unmappableFiles ??= new()).Add(sourceFile.OriginalFileName());
                return default;
            }

            result = candidate;
            found = true;
        }

        if (!found)
            (unmappableFiles ??= new()).Add(sourceFile.OriginalFileName());
        return reindentInsertedLines(sourceFile, change, result);
    }

    // trackerimpl.go:186.
    private ByteStringValue reindentInsertedLines(SourceFile sourceFile, TrackerEdit change, ByteStringValue text)
    {
        if (text.Length == 0 || change.TextRange.Pos() != change.TextRange.End() || change.options.indentation is not null)
            return text;
        if (!text.EndsWith(newLine))
            return text;
        var original = ((IScript)sourceFile).OriginalTextValue();
        int pos = change.TextRange.Pos();
        if (sourceFile.SpanMap()is { } spans)
        {
            var(mapped, fidelity) = spans.VirtualToOriginalPosition(pos);
            if (!fidelity.IsExact())
                return text;
            pos = mapped;
        }

        if (pos < 0 || pos > original.Length)
            return text;
        int lineStart = lastLineBreak(original, pos) + 1;
        var beforePoint = original.Slice(lineStart, pos);
        if (beforePoint.Length == 0)
        {
            if (leadingIndentation(text).Length != 0)
                return text;
            return ByteStringValue.Concat(leadingIndentation(original.Slice(lineStart)), text);
        }

        if (!leadingIndentation(beforePoint).EqualsValue(beforePoint))
            return text;
        return ByteStringValue.Concat(text, beforePoint);
    }

    // trackerimpl.go:223.
    private static ByteStringValue leadingIndentation(ByteStringValue text)
    {
        int end = 0;
        while (end < text.Length && text[end] is (byte)' ' or (byte)'\t')
            end++;
        return text.Slice(0, end);
    }

    // trackerimpl.go:231.
    private ByteStringValue getFormattedTextOfNode(Node node, SourceFile targetFile, SourceFile file, int pos, NodeOptions options)
    {
        var(text, synthetic) = getNonformattedText(node, targetFile);
        var formatOptions = GetFormatCodeSettingsForWriting(formatSettings, targetFile);
        long initialIndentation = options.indentation ?? Formatting.Formatting.GetIndentation(pos, file, formatOptions, options.Prefix.EqualsValue(newLine) || Formatting.Formatting.GetLineStartPositionForPosition(pos, file) == pos);
        long delta = options.delta ?? (formatOptions.IndentSize != 0 && Formatting.Formatting.ShouldIndentChildNode(formatOptions, node, null, null) ? formatOptions.IndentSize : 0);
        var edits = Formatting.Formatting.FormatNodeGivenIndentation(ctx, synthetic, synthetic.AsSourceFile(), targetFile.LanguageVariant, initialIndentation, delta);
        return Core.ApplyBulkEdits(text, edits);
    }

    // trackerimpl.go:253. Value settings are detached exactly as the Go parameter copy.
    internal static FormatCodeSettings GetFormatCodeSettingsForWriting(FormatCodeSettings options, SourceFile file)
    {
        bool detect = options.Semicolons == SemicolonPreference.Ignore;
        bool remove = options.Semicolons == SemicolonPreference.Remove || detect && !LsUtil.ProbablyUsesSemicolons(file);
        if (remove)
            options.Semicolons = SemicolonPreference.Remove;
        return options;
    }

    // trackerimpl.go:263. Existing printer string API is owned raw Go-byte text.
    private (ByteStringValue, Node) getNonformattedText(Node node, SourceFile file)
    {
        var(text, positioned) = SyntheticFile.PrintAndPositionNode(EmitContext.Factory, node, file, newLine.Materialize(), checked((int)formatSettings.IndentSize), EmitContext);
        var synthetic = SyntheticFile.CreateSyntheticSourceFile(EmitContext.Factory, positioned, text, new SourceFileParseOptions { FileNameValue = file.FileNameGoValue(), PathKeyValue = file.PathKeyValue() });
        return (ByteStringValue.FromString(text), synthetic.AsNode());
    }

    // trackerimpl.go:276.
    internal TextRange GetAdjustedRange(SourceFile file, Node startNode, Node endNode, LeadingTriviaOption leading, TrailingTriviaOption trailing) => new(getAdjustedStartPosition(file, startNode, leading, false), getAdjustedEndPosition(file, endNode, trailing));
    // trackerimpl.go:284.
    private int getAdjustedStartPosition(SourceFile file, Node node, LeadingTriviaOption leading, bool hasTrailingComment)
    {
        if (leading == LeadingTriviaOption.JSDoc)
        {
            var comments = Parser.GetJSDocCommentRanges(NodeFactory, default(Slice<CommentRange>), node, file.TextView());
            if (comments.Length > 0)
                return Formatting.Formatting.GetLineStartPositionForPosition(comments[0].Pos, file);
        }

        int start = AstNav.GetStartOfNode(node, file, false);
        int startOfLine = Formatting.Formatting.GetLineStartPositionForPosition(start, file);
        if (leading == LeadingTriviaOption.Exclude)
            return start;
        if (leading == LeadingTriviaOption.StartLine)
            return node.Loc.ContainsInclusive(startOfLine) ? startOfLine : start;
        int fullStart = node.Pos();
        if (fullStart == start)
            return start;
        var lineStarts = file.ECMALineMap();
        int fullLine = ScannerApi.ComputeLineOfPosition(lineStarts.AsSpan(), fullStart);
        int fullLinePos = lineStarts[fullLine];
        if (startOfLine == fullLinePos)
            return leading == LeadingTriviaOption.IncludeAll ? fullStart : start;
        if (hasTrailingComment)
        {
            Slice<CommentRange> comments = default;
            foreach (var comment in ScannerApi.GetLeadingCommentRangesValue(NodeFactory, file.TextView(), fullStart))
                comments = comments.Append(comment);
            if (comments.Length == 0)
                foreach (var comment in ScannerApi.GetTrailingCommentRangesValue(NodeFactory, file.TextView(), fullStart))
                    comments = comments.Append(comment);
            if (comments.Length > 0)
                return ScannerApi.SkipTriviaEx(file.TextView(), comments[0].End, new SkipTriviaOptions { StopAfterLineBreak = true, StopAtComments = true });
        }

        int adjusted = lineStarts[fullLine + (fullStart > 0 ? 1 : 0)];
        adjusted = ScannerApi.SkipTriviaEx(file.TextView(), adjusted, new SkipTriviaOptions { StopAtComments = true });
        return lineStarts[ScannerApi.ComputeLineOfPosition(lineStarts.AsSpan(), adjusted)];
    }

    // trackerimpl.go:349.
    private int getEndPositionOfMultilineTrailingComment(SourceFile file, Node node, TrailingTriviaOption trailing)
    {
        if (trailing == TrailingTriviaOption.Include)
        {
            var lines = file.ECMALineMap();
            int nodeEndLine = ScannerApi.ComputeLineOfPosition(lines.AsSpan(), node.End());
            foreach (var comment in ScannerApi.GetTrailingCommentRangesValue(NodeFactory, file.TextView(), node.End()))
            {
                if (comment.Kind == Kind.SingleLineCommentTrivia || ScannerApi.ComputeLineOfPosition(lines.AsSpan(), comment.Pos) > nodeEndLine)
                    break;
                if (ScannerApi.ComputeLineOfPosition(lines.AsSpan(), comment.End) > nodeEndLine)
                    return ScannerApi.SkipTriviaEx(file.TextView(), comment.End, new SkipTriviaOptions { StopAfterLineBreak = true, StopAtComments = true });
            }
        }

        return 0;
    }

    // trackerimpl.go:375. Collect/AppendSeq retain the original temporary slice owner.
    private int getAdjustedEndPosition(SourceFile file, Node node, TrailingTriviaOption trailing)
    {
        if (trailing == TrailingTriviaOption.Exclude)
            return node.End();
        if (trailing == TrailingTriviaOption.ExcludeWhitespace)
        {
            Slice<CommentRange> comments = default;
            foreach (var comment in ScannerApi.GetTrailingCommentRangesValue(NodeFactory, file.TextView(), node.End()))
                comments = comments.Append(comment);
            foreach (var comment in ScannerApi.GetLeadingCommentRangesValue(NodeFactory, file.TextView(), node.End()))
                comments = comments.Append(comment);
            if (comments.Length > 0 && comments[comments.Length - 1].End != 0)
                return comments[comments.Length - 1].End;
            return node.End();
        }

        int multilineEnd = getEndPositionOfMultilineTrailingComment(file, node, trailing);
        if (multilineEnd != 0)
            return multilineEnd;
        int newEnd = ScannerApi.SkipTriviaEx(file.TextView(), node.End(), new SkipTriviaOptions { StopAfterLineBreak = true });
        if (newEnd != node.End() && (trailing == TrailingTriviaOption.Include || StringUtil.IsLineBreak(ByteStringValue.FromView(file.TextView())[newEnd - 1])))
            return newEnd;
        return node.End();
    }

    // trackerimpl.go:405. []rune traversal is decoded, unlike indentation's byte scan.
    private static bool hasCommentsBeforeLineBreak(ByteStringValue text, int start)
    {
        for (int i = start; i < text.Length;)
        {
            var(rune, size) = text.IsBytes ? Utf8.DecodeRuneInString(text.ByteSpan[i..]) : Utf8.DecodeRuneInString(text.CharSpan[i..]);
            if (!StringUtil.IsWhiteSpaceSingleLine(rune))
                return rune == '/';
            i += size;
        }

        return false;
    }

    // trackerimpl.go:414.
    private static bool needSemicolonBetween(Node a, Node b) => (Ast.IsPropertySignatureDeclaration(a) || Ast.IsPropertyDeclaration(a)) && Ast.IsClassOrTypeElement(b) && b.Name().Kind == Kind.ComputedPropertyName || Ast.IsStatementButNotDeclaration(a) && Ast.IsStatementButNotDeclaration(b);
    // trackerimpl.go:422.
    private int getInsertionPositionAtSourceFileTop(SourceFile file)
    {
        Node? lastPrologue = null;
        foreach (var node in file.Statements.Nodes)
        {
            if (!Ast.IsPrologueDirective(node))
                break;
            lastPrologue = node;
        }

        int position = 0;
        var text = ByteStringValue.FromView(file.TextView());
        if (lastPrologue is not null)
        {
            position = lastPrologue.End();
            advancePastLineBreak(text, ref position);
            return position;
        }

        var shebang = ScannerApi.GetShebangByteString(text.ByteView);
        if (shebang.Length != 0)
        {
            position = shebang.Length;
            advancePastLineBreak(text, ref position);
        }

        Slice<CommentRange> ranges = default;
        foreach (var comment in ScannerApi.GetLeadingCommentRangesValue(NodeFactory, file.TextView(), position))
            ranges = ranges.Append(comment);
        if (ranges.Length == 0)
            return position;
        CommentRange? lastComment = null;
        bool pinnedOrTripleSlash = false;
        int firstNodeLine = -1;
        var lines = file.ECMALineMap();
        foreach (var comment in ranges)
        {
            if (comment.Kind == Kind.MultiLineCommentTrivia)
            {
                if (PrinterUtilities.IsPinnedComment(file.TextView(), comment))
                {
                    lastComment = comment;
                    pinnedOrTripleSlash = true;
                    continue;
                }
            }
            else if (PrinterUtilities.IsRecognizedTripleSlashComment(file.TextView(), comment))
            {
                lastComment = comment;
                pinnedOrTripleSlash = true;
                continue;
            }

            if (lastComment is { } previous)
            {
                if (pinnedOrTripleSlash)
                    break;
                if (ScannerApi.ComputeLineOfPosition(lines.AsSpan(), comment.Pos) >= ScannerApi.ComputeLineOfPosition(lines.AsSpan(), previous.End) + 2)
                    break;
            }

            if (file.Statements.Nodes.Length > 0)
            {
                if (firstNodeLine == -1)
                    firstNodeLine = ScannerApi.ComputeLineOfPosition(lines.AsSpan(), AstNav.GetStartOfNode(file.Statements.Nodes[0], file, false));
                if (firstNodeLine < ScannerApi.ComputeLineOfPosition(lines.AsSpan(), comment.End) + 2)
                    break;
            }

            lastComment = comment;
            pinnedOrTripleSlash = false;
        }

        if (lastComment is { } last)
        {
            position = last.End;
            advancePastLineBreak(text, ref position);
        }

        return position;
    }

    // Authored explicit-state adaptation of trackerimpl.go:437 synchronous local callback.
    private static void advancePastLineBreak(ByteStringValue text, ref int position)
    {
        if (position >= text.Length)
            return;
        int ch = text[position];
        if (!StringUtil.IsLineBreak(ch))
            return;
        position++;
        if (position < text.Length && ch == '\r' && text[position] == '\n')
            position++;
    }

    // Explicit byte-role adaptations of strings rune trim and LastIndexAny.
    private static ByteStringValue trimLeadingSpace(ByteStringValue text)
    {
        int start = 0;
        while (start < text.Length)
        {
            var(rune, size) = text.IsBytes ? Utf8.DecodeRuneInString(text.ByteSpan[start..]) : Utf8.DecodeRuneInString(text.CharSpan[start..]);
            if (!GoUnicode.IsSpace(rune))
                break;
            start += size;
        }

        return text.Slice(start);
    }

    private static int lastLineBreak(ByteStringValue text, int end)
    {
        for (int i = end - 1; i >= 0; i--)
            if (text[i] is '\r' or '\n')
                return i;
        return -1;
    }

    // Go fmt %v for Range (two Position values) has no Stringer override.
    private static string formatRange(TypeScript.Lsp.Protocol.Range range) => FormattableString.Invariant($"{{{{{range.Start.Line} {range.Start.Character}}} {{{range.End.Line} {range.End.Character}}}}}");
}

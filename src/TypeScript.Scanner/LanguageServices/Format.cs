// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ls/format.go counterpart at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using FormatApi = TypeScript.LanguageServices.Formatting.Formatting;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;
using SpanMapKind = TypeScript.SpanMap.Kind;
using LspRange = TypeScript.Lsp.Protocol.Range;

namespace TypeScript.LanguageServices;
internal struct MappedFormattingRange
{
    internal SourceFile projection;
    internal Segment segment;
    internal TextRange originalRange;
}

internal sealed partial class LanguageService
{
    private Slice<TextEdit?> toLSProtoTextEdits(SourceFile file, Slice<TextChange> changes)
    {
        var result = Slice<TextEdit?>.Make(0, changes.Count);
        foreach (var change in changes)
        {
            var (range, fidelity) = converters.ToLSPRange(file, new TextRange(change.TextRange.Pos(), change.TextRange.End()));
            if (!fidelity.IsExact())
                return default;
            result = result.Append(new TextEdit { NewText = change.NewText, Range = range });
        }

        return result;
    }

    internal (TextEditsOrNull Response, Exception? Error) ProvideFormatDocument(ProtocolRequestContext context, DocumentUri documentUri, FormattingOptions options)
    {
        if (UserPreferences().FormatEnabled == Tristate.False)
            return (new TextEditsOrNull(), null);
        var (_, file) = getProgramAndFile(documentUri);
        var formatOptions = LsUtil.FromLSFormatOptions(FormatOptions(), options);
        var edits = file.ContentMapper() == "" ? toLSProtoTextEdits(file, getFormattingEditsForDocument(context, file, formatOptions))
            : getFormattingEditsForMappedRange(context, file, formatOptions, new TextRange(0, ((TypeScript.LanguageServices.Lsconv.IScript)file).OriginalTextValue().Length));
        return (new TextEditsOrNull { TextEdits = edits }, null);
    }

    // format.go:52. Only verbatim formatting-enabled intersections are admitted.
    private Slice<TextEdit?> getFormattingEditsForMappedRange(ProtocolRequestContext context, SourceFile file, FormatCodeSettings options, TextRange originalRange)
    {
        var projections = Slice<SourceFile>.FromArray([file]).Append(file.SupplementalSourceFiles());
        Slice<MappedFormattingRange> candidates = default;
        foreach (var projection in projections)
        {
            var spanMap = projection.SpanMap();
            if (spanMap == null)
                continue;
            if (spanMap.Segments() is not { } segments)
                continue;
            foreach (var segment in segments)
            {
                if (segment.Kind != SpanMapKind.Verbatim || (segment.Features & Feature.Formatting) == 0)
                    continue;
                int start = Math.Max(originalRange.Pos(), segment.OriginalStart);
                int end = Math.Min(originalRange.End(), segment.OriginalEnd);
                if (start >= end)
                    continue;
                candidates = candidates.Append(new MappedFormattingRange { projection = projection, segment = segment, originalRange = new TextRange(start, end) });
            }
        }

        Slice<TextEdit?> edits = default;
        foreach (var candidate in nonOverlappingFormattingRanges(candidates))
        {
            var virtualRange = new TextRange(candidate.segment.VirtualStart + candidate.originalRange.Pos() - candidate.segment.OriginalStart, candidate.segment.VirtualStart + candidate.originalRange.End() - candidate.segment.OriginalStart);
            foreach (var change in getFormattingEditsForRange(context, candidate.projection, options, virtualRange))
            {
                if (change.TextRange.Pos() < virtualRange.Pos() || change.TextRange.End() > virtualRange.End())
                    continue;
                var (range, fidelity) = converters.ToLSPRangeForFeature(candidate.projection, new TextRange(change.TextRange.Pos(), change.TextRange.End()), Feature.Formatting);
                if (!fidelity.IsExact())
                    continue;
                edits = edits.Append(new TextEdit { Range = range, NewText = change.NewText });
            }
        }

        GoSlices.SortStableFunc(edits, static (first, second) =>
        {
            int comparison = ProtocolUtilities.CompareRanges(first!.Range, second!.Range);
            return comparison != 0 ? comparison : ByteStringKey.FromValue(first.NewText).CompareTo(ByteStringKey.FromValue(second.NewText));
        });
        return edits;
    }

    // format.go:126. Clone before sorting; compact into that exact retained backing owner.
    internal static Slice<MappedFormattingRange> nonOverlappingFormattingRanges(Slice<MappedFormattingRange> candidates)
    {
        candidates = GoSlices.Clone(candidates);
        GoSlices.SortStableFunc(candidates, static (first, second) =>
        {
            int comparison = first.originalRange.Pos().CompareTo(second.originalRange.Pos());
            return comparison != 0 ? comparison : second.originalRange.End().CompareTo(first.originalRange.End());
        });
        var result = candidates.Slice2(0, 0);
        foreach (var original in candidates)
        {
            var candidate = original;
            if (result.Count > 0)
                candidate.originalRange = candidate.originalRange.WithPos(Math.Max(candidate.originalRange.Pos(), result[result.Count - 1].originalRange.End()));
            if (candidate.originalRange.Len() > 0)
                result = result.Append(candidate);
        }

        return result;
    }

    internal (TextEditsOrNull Response, Exception? Error) ProvideFormatDocumentRange(ProtocolRequestContext context, DocumentUri documentUri, FormattingOptions options, LspRange range)
    {
        if (UserPreferences().FormatEnabled == Tristate.False)
            return (new TextEditsOrNull(), null);
        var (_, file) = getProgramAndFile(documentUri);
        var formatOptions = LsUtil.FromLSFormatOptions(FormatOptions(), options);
        if (file.ContentMapper() != "")
        {
            var mappedEdits = getFormattingEditsForMappedRange(context, file, formatOptions, converters.FromLSPRangeToOriginal(file, range));
            return (new TextEditsOrNull { TextEdits = mappedEdits }, null);
        }

        var ranges = converters.FromLSPRangeForSourceFile(file, range, Feature.Formatting);
        if (ranges.Count != 1 || !ranges[0].Fidelity.IsExact())
            return (new TextEditsOrNull(), null);
        file = ranges[0].Script;
        var edits = toLSProtoTextEdits(file, getFormattingEditsForRange(context, file, formatOptions, ranges[0].Span));
        return (new TextEditsOrNull { TextEdits = edits }, null);
    }

    internal (TextEditsOrNull Response, Exception? Error) ProvideFormatDocumentOnType(ProtocolRequestContext context, DocumentUri documentUri, FormattingOptions options, Position position, ByteStringValue character)
    {
        if (UserPreferences().FormatEnabled == Tristate.False)
            return (new TextEditsOrNull(), null);
        var (_, file) = getProgramAndFile(documentUri);
        var formatOptions = LsUtil.FromLSFormatOptions(FormatOptions(), options);
        var positions = converters.FromLSPPositionForSourceFile(file, position, Feature.Formatting);
        if (positions.Count != 1 || !positions[0].Fidelity.IsExact())
            return (new TextEditsOrNull(), null);
        file = positions[0].Script;
        var edits = toLSProtoTextEdits(file, getFormattingEditsAfterKeystroke(context, file, formatOptions, positions[0].Position, character));
        return (new TextEditsOrNull { TextEdits = edits }, null);
    }

    internal Slice<TextChange> getFormattingEditsForRange(ProtocolRequestContext context, SourceFile file, FormatCodeSettings options, TextRange range) => FormatApi.FormatSelection(FormatApi.WithFormatCodeSettings(context, options, ByteStringValue.FromString(options.NewLineCharacter)), file, range.Pos(), range.End());
    private Slice<TextChange> getFormattingEditsForDocument(ProtocolRequestContext context, SourceFile file, FormatCodeSettings options) => FormatApi.FormatDocument(FormatApi.WithFormatCodeSettings(context, options, ByteStringValue.FromString(options.NewLineCharacter)), file);
    internal Slice<TextChange> getFormattingEditsAfterKeystroke(ProtocolRequestContext context, SourceFile file, FormatCodeSettings options, int position, ByteStringValue key)
    {
        context = FormatApi.WithFormatCodeSettings(context, options, ByteStringValue.FromString(options.NewLineCharacter));
        var tokenAtPosition = AstNav.GetTokenAtPosition(file, position);
        if (isInComment(file, position, tokenAtPosition) != null)
            return default;
        if (key.EqualsString("{"))
            return FormatApi.FormatOnOpeningCurly(context, file, position);
        if (key.EqualsString("}"))
            return FormatApi.FormatOnClosingCurly(context, file, position);
        if (key.EqualsString(";"))
            return FormatApi.FormatOnSemicolon(context, file, position);
        if (key.EqualsString("\n"))
            return FormatApi.FormatOnEnter(context, file, position);
        return default;
    }

    // format.go:257. No implicit token defaults, exactly as upstream documents.
    private static CommentRange? getRangeOfEnclosingComment(SourceFile file, int position, Node? precedingToken, Node tokenAtPosition)
    {
        var jsdoc = Ast.FindAncestor(tokenAtPosition, static node => node.IsJSDoc());
        if (jsdoc != null)
            tokenAtPosition = jsdoc.Parent;
        int tokenStart = AstNav.GetStartOfNode(tokenAtPosition, file, false);
        if (tokenStart <= position && position < tokenAtPosition.End())
            return null;
        ScannerApi.CommentRangeSequence trailing = default;
        if (precedingToken != null)
            trailing = ScannerApi.GetTrailingCommentRangesValue(null, file.TextView(), precedingToken.End());
        var leading = getLeadingCommentRangesOfNode(tokenAtPosition, file);
        foreach (var comment in trailing)
            if (comment.ContainsExclusive(position) || position == comment.End && (comment.Kind == Kind.SingleLineCommentTrivia || position == file.TextValue().Length))
                return comment;
        foreach (var comment in leading)
            if (comment.ContainsExclusive(position) || position == comment.End && (comment.Kind == Kind.SingleLineCommentTrivia || position == file.TextValue().Length))
                return comment;
        return null;
    }
}

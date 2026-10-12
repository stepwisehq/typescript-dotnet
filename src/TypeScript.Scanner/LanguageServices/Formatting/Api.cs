// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;
using ScannerApi = TypeScript.Scanner.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.Utilities;

namespace TypeScript.LanguageServices.Formatting;
internal enum FormatRequestKind
{
    FormatDocument,
    FormatSelection,
    FormatOnEnter,
    FormatOnSemicolon,
    FormatOnOpeningCurlyBrace,
    FormatOnClosingCurlyBrace
}

internal static partial class Formatting
{
    // api.go:33. Explicit context derivation retains unrelated request values.
    internal static ProtocolRequestContext WithFormatCodeSettings(ProtocolRequestContext context, FormatCodeSettings options, ByteStringValue newLine) => context.WithFormatCodeSettings(options, newLine);
    // api.go:40.
    internal static FormatCodeSettings GetFormatCodeSettingsFromContext(ProtocolRequestContext context) => context.FormatSettings ?? LsUtil.GetDefaultFormatCodeSettings();
    // api.go:47. The host key is a required Go context value when no editor newline overrides it.
    internal static ByteStringValue GetNewLineOrDefaultFromContext(ProtocolRequestContext context)
    {
        var options = GetFormatCodeSettingsFromContext(context);
        if (!string.IsNullOrEmpty(options.NewLineCharacter))
            return ByteStringValue.FromString(options.NewLineCharacter);
        var host = context.FormatHostNewLine ?? throw new InvalidOperationException("missing formatting host newline context value");
        return host.Length > 0 ? host : ByteStringValue.FromString("\n");
    }

    // api.go:59.
    internal static Slice<TextChange> FormatSpan(ProtocolRequestContext context, TextRange span, SourceFile file, FormatRequestKind kind)
    {
        var enclosing = FindEnclosingNode(span, file);
        var options = GetFormatCodeSettingsFromContext(context);
        return FormattingScanner.NewFormattingScanner(file.TextView(), file.LanguageVariant, GetScanStartPosition(enclosing, span, file), span.End(), FormatSpanWorker.NewFormatSpanWorker(context, span, enclosing, GetIndentationForNode(enclosing, span, file, options), GetOwnOrInheritedDelta(enclosing, options, file), kind, PrepareRangeContainsErrorFunction(file.Diagnostics(), span), file));
    }

    // api.go:81. This original callback is stateless and has one retained delegate representation.
    internal static Slice<TextChange> FormatNodeGivenIndentation(ProtocolRequestContext context, Node node, SourceFile file, LanguageVariant variant, long initialIndentation, long delta)
    {
        var range = new TextRange(node.Pos(), node.End());
        return FormattingScanner.NewFormattingScanner(file.TextView(), variant, range.Pos(), range.End(), FormatSpanWorker.NewFormatSpanWorker(context, range, node, initialIndentation, delta, FormatRequestKind.FormatSelection, static _ => false, file));
    }

    // api.go:101.
    private static Slice<TextChange> FormatNodeLines(ProtocolRequestContext context, SourceFile file, Node? node, FormatRequestKind kind)
    {
        if (node == null)
            return default;
        var tokenStart = ScannerApi.GetTokenPosOfNode(node, file, false);
        return FormatSpan(context, new TextRange(GetLineStartPositionForPosition(tokenStart, file), node.End()), file, kind);
    }

    // api.go:111.
    internal static Slice<TextChange> FormatDocument(ProtocolRequestContext context, SourceFile file) => FormatSpan(context, new TextRange(0, file.End()), file, FormatRequestKind.FormatDocument);
    // api.go:115.
    internal static Slice<TextChange> FormatSelection(ProtocolRequestContext context, SourceFile file, int start, int end) => FormatSpan(context, new TextRange(GetLineStartPositionForPosition(start, file), end), file, FormatRequestKind.FormatSelection);
    // api.go:119. End the span at the typed opening brace so a temporarily unmatched closing brace stays outside it.
    internal static Slice<TextChange> FormatOnOpeningCurly(ProtocolRequestContext context, SourceFile file, int position)
    {
        var opening = FindImmediatelyPrecedingTokenOfKind(position, Kind.OpenBraceToken, file);
        if (opening == null)
            return default;
        var outermost = FindOutermostNodeWithinListLevel(opening.Parent)!;
        var range = new TextRange(GetLineStartPositionForPosition(ScannerApi.GetTokenPosOfNode(outermost, file, false), file), position);
        return FormatSpan(context, range, file, FormatRequestKind.FormatOnOpeningCurlyBrace);
    }

    // api.go:143.
    internal static Slice<TextChange> FormatOnClosingCurly(ProtocolRequestContext context, SourceFile file, int position) => FormatNodeLines(context, file, FindOutermostNodeWithinListLevel(FindImmediatelyPrecedingTokenOfKind(position, Kind.CloseBraceToken, file)), FormatRequestKind.FormatOnClosingCurlyBrace);
    // api.go:148.
    internal static Slice<TextChange> FormatOnSemicolon(ProtocolRequestContext context, SourceFile file, int position) => FormatNodeLines(context, file, FindOutermostNodeWithinListLevel(FindImmediatelyPrecedingTokenOfKind(position, Kind.SemicolonToken, file)), FormatRequestKind.FormatOnSemicolon);
    // api.go:153. Back up by raw bytes exactly as the source, including intermediate continuation-byte decoding.
    internal static Slice<TextChange> FormatOnEnter(ProtocolRequestContext context, SourceFile file, int position)
    {
        var line = ScannerApi.GetECMALineOfPosition(file, position);
        if (line == 0)
            return default;
        var start = ScannerApi.GetECMALineStarts(file)[line - 1];
        var end = ScannerApi.GetECMAEndLinePosition(file, line);
        var text = file.TextView();
        while (end > start)
        {
            var(rune, size) = Utf8.DecodeRuneInString(text.Slice(end).AsSpan());
            if (size == 0 || StringUtil.IsWhiteSpaceSingleLine(rune))
            {
                end--;
                continue;
            }

            break;
        }

        var(last, _) = Utf8.DecodeRuneInString(text.Slice(end).AsSpan());
        if (StringUtil.IsLineBreak(last))
            end--;
        return FormatSpan(context, new TextRange(start, end + 1), file, FormatRequestKind.FormatOnEnter);
    }
}

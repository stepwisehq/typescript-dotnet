// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;
using TypeScript.AstNavigation;
using TypeScript.LanguageServices.Utilities;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.LanguageServices.Formatting;
internal sealed class FormattingContext
{
    internal TextRangeWithKind currentTokenSpan, nextTokenSpan;
    internal Node contextNode = null!, currentTokenParent = null!, nextTokenParent = null!;
    private Tristate contextNodeAllOnSameLine, nextNodeAllOnSameLine, tokensAreOnSameLine, contextNodeBlockIsOnOneLine, nextNodeBlockIsOnOneLine;
    internal SourceFile SourceFile = null!;
    internal FormatRequestKind FormattingRequestKind;
    internal FormatCodeSettings Options;
    // context.go:33.
    internal static FormattingContext NewFormattingContext(SourceFile file, FormatRequestKind kind, FormatCodeSettings options) => new()
    {
        SourceFile = file,
        FormattingRequestKind = kind,
        Options = options
    };
    // context.go:42. Context changes invalidate every cached line predicate.
    internal void UpdateContext(TextRangeWithKind current, Node currentParent, TextRangeWithKind next, Node nextParent, Node commonParent)
    {
        if (currentParent == null)
            throw new InvalidOperationException("nil current range node parent in update context");
        if (nextParent == null)
            throw new InvalidOperationException("nil next range node parent in update context");
        if (commonParent == null)
            throw new InvalidOperationException("nil common parent node in update context");
        currentTokenSpan = current;
        currentTokenParent = currentParent;
        nextTokenSpan = next;
        nextTokenParent = nextParent;
        contextNode = commonParent;
        contextNodeAllOnSameLine = nextNodeAllOnSameLine = tokensAreOnSameLine = contextNodeBlockIsOnOneLine = nextNodeBlockIsOnOneLine = Tristate.Unknown;
    }

    // context.go:68/75.
    private Tristate RangeIsOnOneLine(TextRange range) => Formatting.RangeIsOnOneLine(range, SourceFile) ? Tristate.True : Tristate.False;
    private Tristate NodeIsOnOneLine(Node node) => RangeIsOnOneLine(Formatting.WithTokenStart(node, SourceFile));
    // context.go:84.
    private Tristate BlockIsOnOneLine(Node node)
    {
        var open = AstNav.FindChildOfKind(node, Kind.OpenBraceToken, SourceFile);
        var close = AstNav.FindChildOfKind(node, Kind.CloseBraceToken, SourceFile);
        return open != null && close != null ? RangeIsOnOneLine(new(open.End(), ScannerApi.GetTokenPosOfNode(close, SourceFile, false))) : Tristate.False;
    }

    // context.go:94/101/108/115/122.
    internal bool ContextNodeAllOnSameLine()
    {
        if (contextNodeAllOnSameLine == Tristate.Unknown)
            contextNodeAllOnSameLine = NodeIsOnOneLine(contextNode);
        return contextNodeAllOnSameLine == Tristate.True;
    }

    internal bool NextNodeAllOnSameLine()
    {
        if (nextNodeAllOnSameLine == Tristate.Unknown)
            nextNodeAllOnSameLine = NodeIsOnOneLine(nextTokenParent);
        return nextNodeAllOnSameLine == Tristate.True;
    }

    internal bool TokensAreOnSameLine()
    {
        if (tokensAreOnSameLine == Tristate.Unknown)
            tokensAreOnSameLine = RangeIsOnOneLine(new(currentTokenSpan.Loc.Pos(), nextTokenSpan.Loc.Pos()));
        return tokensAreOnSameLine == Tristate.True;
    }

    internal bool ContextNodeBlockIsOnOneLine()
    {
        if (contextNodeBlockIsOnOneLine == Tristate.Unknown)
            contextNodeBlockIsOnOneLine = BlockIsOnOneLine(contextNode);
        return contextNodeBlockIsOnOneLine == Tristate.True;
    }

    internal bool NextNodeBlockIsOnOneLine()
    {
        if (nextNodeBlockIsOnOneLine == Tristate.Unknown)
            nextNodeBlockIsOnOneLine = BlockIsOnOneLine(nextTokenParent);
        return nextNodeBlockIsOnOneLine == Tristate.True;
    }
}

internal static partial class Formatting
{
    // context.go:79.
    internal static TextRange WithTokenStart(Node node, SourceFile file) => new(ScannerApi.GetTokenPosOfNode(node, file, false), node.End());
}

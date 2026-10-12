// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using Debug = TypeScript.Scanner.Debug;
using TypeScript.Scanner;
using ScannerApi = TypeScript.Scanner.Scanner;

namespace TypeScript.LanguageServices.Formatting;
internal record struct TextRangeWithKind
{
    internal TextRange Loc;
    internal Kind Kind;
    // scanner.go:17.
    internal static TextRangeWithKind NewTextRangeWithKind(int pos, int end, Kind kind) => new()
    {
        Loc = new(pos, end),
        Kind = kind
    };
}

internal struct TokenInfo
{
    internal Slice<TextRangeWithKind> leadingTrivia;
    internal TextRangeWithKind token;
    internal Slice<TextRangeWithKind> trailingTrivia;
}

internal enum ScanAction
{
    Scan,
    GreaterThan,
    Slash,
    Template,
    JsxIdentifier,
    JsxText,
    JsxAttributeValue
}

internal sealed class FormattingScanner
{
    private ScannerApi s = null!;
    private int startPos, endPos, savedPos;
    private bool hasLastTokenInfo, wasNewLine;
    private TokenInfo lastTokenInfo;
    private ScanAction lastScanAction;
    private Slice<TextRangeWithKind> leadingTrivia, trailingTrivia;
    // scanner.go:43. The scanner retains the original immutable source-byte owner.
    internal static Slice<TextChange> NewFormattingScanner(ByteStringView text, LanguageVariant variant, int start, int end, FormatSpanWorker worker)
    {
        var scan = ScannerApi.NewScanner();
        scan.SetSkipTrivia(false);
        scan.SetLanguageVariant(variant);
        scan.SetByteTextInternal(text);
        scan.ResetTokenState(start);
        var formatting = new FormattingScanner
        {
            s = scan,
            startPos = start,
            endPos = end,
            wasNewLine = true
        };
        var result = worker.Execute(formatting);
        formatting.hasLastTokenInfo = false;
        scan.Reset();
        return result;
    }

    // scanner.go:65.
    internal void Advance()
    {
        hasLastTokenInfo = false;
        if (s.TokenFullStart() != startPos)
            wasNewLine = trailingTrivia.Length > 0 && trailingTrivia[trailingTrivia.Length - 1].Kind == Kind.NewLineTrivia;
        else
            s.Scan();
        leadingTrivia = default;
        trailingTrivia = default;
        var pos = s.TokenFullStart();
        while (pos < endPos)
        {
            var token = s.Token();
            if (!Ast.IsTrivia(token))
                break;
            s.Scan();
            var item = TextRangeWithKind.NewTextRangeWithKind(pos, s.TokenFullStart(), token);
            pos = s.TokenFullStart();
            leadingTrivia = leadingTrivia.Append(item);
        }

        savedPos = s.TokenFullStart();
    }

    // scanner.go:99.
    private static bool ShouldRescanGreaterThanToken(Node node) => node.Kind is Kind.GreaterThanEqualsToken or Kind.GreaterThanGreaterThanEqualsToken or Kind.GreaterThanGreaterThanGreaterThanEqualsToken or Kind.GreaterThanGreaterThanGreaterThanToken or Kind.GreaterThanGreaterThanToken;
    // scanner.go:111.
    private static bool ShouldRescanJsxIdentifier(Node node)
    {
        if (node.Parent == null)
            return false;
        switch (node.Parent.Kind)
        {
            case Kind.JsxAttribute:
            case Kind.JsxOpeningElement:
            case Kind.JsxClosingElement:
            case Kind.JsxSelfClosingElement:
            case Kind.JsxNamespacedName:
                return Ast.IsKeywordKind(node.Kind) || node.Kind == Kind.Identifier;
            case Kind.PropertyAccessExpression:
                return (Ast.IsKeywordKind(node.Kind) || node.Kind == Kind.Identifier) && IsLeftmostJsxTagName(node);
        }

        return false;
    }

    // scanner.go:129. Stateless ancestor callback is shared across calls.
    private static bool IsLeftmostJsxTagName(Node node) => Ast.FindAncestorOrQuit(node, static n => n.Parent == null ? FindAncestorResult.Quit : Ast.IsJsxTagName(n) ? FindAncestorResult.True : Ast.IsPropertyAccessExpression(n.Parent) && n.Parent.Expression() == n ? FindAncestorResult.False : FindAncestorResult.Quit) != null;
    // scanner.go:144.
    private bool ShouldRescanJsxText(Node node) => Ast.IsJsxText(node) || Ast.IsJsxElement(node) && hasLastTokenInfo && lastTokenInfo.token.Kind == Kind.JsxText;
    // scanner.go:155.
    private static bool ShouldRescanSlashToken(Node node) => node.Kind == Kind.RegularExpressionLiteral;
    // scanner.go:159.
    private static bool ShouldRescanTemplateToken(Node node) => node.Kind is Kind.TemplateMiddle or Kind.TemplateTail;
    // scanner.go:164.
    private static bool ShouldRescanJsxAttributeValue(Node node) => node.Parent != null && Ast.IsJsxAttribute(node.Parent) && node.Parent.Initializer() == node;
    // scanner.go:168.
    private static bool StartsWithSlashToken(Kind token) => token is Kind.SlashToken or Kind.SlashEqualsToken;
    // scanner.go:184. Value snapshot retains trivia headers.
    private static TokenInfo FixTokenKind(TokenInfo info, Node container)
    {
        if (Ast.IsTokenKind(container.Kind) && info.token.Kind != container.Kind)
            info.token.Kind = container.Kind;
        return info;
    }

    // scanner.go:191.
    internal TokenInfo ReadTokenInfo(Node node)
    {
        Debug.Assert(IsOnToken());
        var expected = ShouldRescanGreaterThanToken(node) ? ScanAction.GreaterThan : ShouldRescanSlashToken(node) ? ScanAction.Slash : ShouldRescanTemplateToken(node) ? ScanAction.Template : ShouldRescanJsxIdentifier(node) ? ScanAction.JsxIdentifier : ShouldRescanJsxText(node) ? ScanAction.JsxText : ShouldRescanJsxAttributeValue(node) ? ScanAction.JsxAttributeValue : ScanAction.Scan;
        if (hasLastTokenInfo && expected == lastScanAction)
        {
            lastTokenInfo = FixTokenKind(lastTokenInfo, node);
            return lastTokenInfo;
        }

        if (s.TokenFullStart() != savedPos)
        {
            s.ResetTokenState(savedPos);
            s.Scan();
        }

        var current = GetNextToken(node, expected);
        var token = TextRangeWithKind.NewTextRangeWithKind(s.TokenFullStart(), s.TokenEnd(), current);
        trailingTrivia = default;
        while (s.TokenFullStart() < endPos)
        {
            current = s.Scan();
            if (!Ast.IsTrivia(current))
                break;
            trailingTrivia = trailingTrivia.Append(TextRangeWithKind.NewTextRangeWithKind(s.TokenFullStart(), s.TokenEnd(), current));
            if (current == Kind.NewLineTrivia)
            {
                s.Scan();
                break;
            }
        }

        hasLastTokenInfo = true;
        lastTokenInfo = new TokenInfo
        {
            leadingTrivia = GoSlices.Clone(leadingTrivia),
            token = token,
            trailingTrivia = GoSlices.Clone(trailingTrivia)
        };
        lastTokenInfo = FixTokenKind(lastTokenInfo, node);
        return lastTokenInfo;
    }

    // scanner.go:272.
    private Kind GetNextToken(Node node, ScanAction expected)
    {
        var token = s.Token();
        lastScanAction = ScanAction.Scan;
        switch (expected)
        {
            case ScanAction.GreaterThan:
                if (token == Kind.GreaterThanToken)
                {
                    lastScanAction = expected;
                    var next = s.ReScanGreaterThanToken();
                    Debug.Assert(node.Kind == next);
                    return next;
                }

                break;
            case ScanAction.Slash:
                if (StartsWithSlashToken(token))
                {
                    lastScanAction = expected;
                    var next = s.ReScanSlashToken();
                    Debug.Assert(node.Kind == next);
                    return next;
                }

                break;
            case ScanAction.Template:
                if (token == Kind.CloseBraceToken)
                {
                    lastScanAction = expected;
                    return s.ReScanTemplateToken(false);
                }

                break;
            case ScanAction.JsxIdentifier:
                lastScanAction = expected;
                return s.ScanJsxIdentifier();
            case ScanAction.JsxText:
                lastScanAction = expected;
                return s.ReScanJsxToken(false);
            case ScanAction.JsxAttributeValue:
                lastScanAction = expected;
                return s.ReScanJsxAttributeValue();
            case ScanAction.Scan:
                break;
            default:
                throw new InvalidOperationException("unhandled scan action kind");
        }

        return token;
    }

    // scanner.go:312.
    internal TextRangeWithKind ReadEOFTokenRange()
    {
        Debug.Assert(IsOnEOF());
        return TextRangeWithKind.NewTextRangeWithKind(s.TokenFullStart(), s.TokenEnd(), Kind.EndOfFile);
    }

    // scanner.go:321.
    internal bool IsOnToken()
    {
        var token = hasLastTokenInfo ? lastTokenInfo.token.Kind : s.Token();
        return token != Kind.EndOfFile && !Ast.IsTrivia(token);
    }

    // scanner.go:329.
    internal bool IsOnEOF() => (hasLastTokenInfo ? lastTokenInfo.token.Kind : s.Token()) == Kind.EndOfFile;
    // scanner.go:337.
    internal void SkipToEndOf(TextRange range)
    {
        s.ResetTokenState(range.End());
        savedPos = s.TokenFullStart();
        lastScanAction = ScanAction.Scan;
        hasLastTokenInfo = false;
        wasNewLine = false;
        leadingTrivia = default;
        trailingTrivia = default;
    }

    // scanner.go:347.
    internal void SkipToStartOf(TextRange range)
    {
        s.ResetTokenState(range.Pos());
        savedPos = s.TokenFullStart();
        lastScanAction = ScanAction.Scan;
        hasLastTokenInfo = false;
        wasNewLine = false;
        leadingTrivia = default;
        trailingTrivia = default;
    }

    // scanner.go:357.
    internal Slice<TextRangeWithKind> GetCurrentLeadingTrivia() => leadingTrivia;
    // scanner.go:361.
    internal bool LastTrailingTriviaWasNewLine() => wasNewLine;
    // scanner.go:365.
    internal int GetTokenFullStart() => hasLastTokenInfo ? lastTokenInfo.token.Loc.Pos() : s.TokenFullStart();
    // scanner.go:372.
    internal int GetStartPos() => GetTokenFullStart();
}

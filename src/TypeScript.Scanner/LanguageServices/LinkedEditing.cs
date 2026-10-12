// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ls/linkedediting.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using ScannerApi = TypeScript.Scanner.Scanner;
using AstNav = TypeScript.AstNavigation.AstNav;
using Kind = TypeScript.Scanner.Kind;
using ProtocolRange = TypeScript.Lsp.Protocol.Range;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    // One retained package pointer to the original raw-byte word pattern.
    private static readonly ByteStringValue? jsxTagWordPattern = ByteStringValue.FromString("[a-zA-Z0-9:\\-\\._$]*");
    internal (LinkedEditingRangesOrNull Result, Exception? Error) ProvideLinkedEditingRange(ProtocolRequestContext context, LinkedEditingRangeParams parameters)
    {
        var(_, sourceFile) = getProgramAndFile(parameters.TextDocument.Uri);
        var positions = converters.FromLSPPositionForSourceFile(sourceFile, parameters.Position, Feature.LinkedEditing);
        if (positions.Length != 1 || !positions[0].Fidelity.IsExact())
            return (new(), null);
        sourceFile = positions[0].Script;
        int position = positions[0].Position;
        var token = AstNav.FindPrecedingToken(sourceFile, position);
        if (token is null || token.Parent.Kind == Kind.SourceFile)
            return (new(), null);
        if (Ast.IsJsxFragment(token.Parent.Parent))
        {
            var fragment = token.Parent.Parent.AsJsxFragment();
            var openFragment = fragment.OpeningFragment;
            var closeFragment = fragment.ClosingFragment;
            if ((openFragment.Flags & NodeFlags.ThisNodeOrAnySubNodesHasError) != 0 || (closeFragment.Flags & NodeFlags.ThisNodeOrAnySubNodesHasError) != 0)
                return (new(), null);
            int openPos = AstNav.GetStartOfNode(openFragment, sourceFile, false) + 1;
            int closePos = AstNav.GetStartOfNode(closeFragment, sourceFile, false) + 2;
            if (position != openPos && position != closePos)
                return (new(), null);
            var(openLineChar, openFidelity) = converters.ToLSPPositionForFeature(sourceFile, openPos, Feature.LinkedEditing);
            var(closeLineChar, closeFidelity) = converters.ToLSPPositionForFeature(sourceFile, closePos, Feature.LinkedEditing);
            if (!openFidelity.IsExact() || !closeFidelity.IsExact())
                return (new(), null);
            return (new() { LinkedEditingRanges = new() { Ranges = Slice<ProtocolRange>.FromArray([new() { Start = openLineChar, End = openLineChar }, new() { Start = closeLineChar, End = closeLineChar }]), WordPattern = jsxTagWordPattern } }, null);
        }
        else
        {
            var tag = Ast.FindAncestor(token.Parent, static node => Ast.IsJsxOpeningElement(node) || Ast.IsJsxClosingElement(node));
            if (tag is null)
                return (new(), null);
            Debug.Assert(Ast.IsJsxOpeningElement(tag) || Ast.IsJsxClosingElement(tag), "tag should be opening or closing element");
            var element = tag.Parent.AsJsxElement();
            var openTag = element.OpeningElement;
            var closeTag = element.ClosingElement;
            int openStart = AstNav.GetStartOfNode(openTag.TagName(), sourceFile, false);
            int openEnd = openTag.TagName().End();
            int closeStart = AstNav.GetStartOfNode(closeTag.TagName(), sourceFile, false);
            int closeEnd = closeTag.TagName().End();
            if (openStart == AstNav.GetStartOfNode(openTag, sourceFile, false) || closeStart == AstNav.GetStartOfNode(closeTag, sourceFile, false) || openEnd == openTag.End() || closeEnd == closeTag.End())
                return (new(), null);
            if (!(openStart <= position && position <= openEnd || closeStart <= position && position <= closeEnd))
                return (new(), null);
            var openingText = ScannerApi.GetTextOfNodeValue(openTag.TagName());
            if (!openingText.EqualsValue(ScannerApi.GetTextOfNodeValue(closeTag.TagName())))
                return (new(), null);
            var(openRange, openFidelity) = converters.ToLSPRangeForFeature(sourceFile, new TextRange(openStart, openEnd), Feature.LinkedEditing);
            var(closeRange, closeFidelity) = converters.ToLSPRangeForFeature(sourceFile, new TextRange(closeStart, closeEnd), Feature.LinkedEditing);
            if (!openFidelity.IsExact() || !closeFidelity.IsExact())
                return (new(), null);
            return (new() { LinkedEditingRanges = new() { Ranges = Slice<ProtocolRange>.FromArray([openRange, closeRange]), WordPattern = jsxTagWordPattern } }, null);
        }
    }
}

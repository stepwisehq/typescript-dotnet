// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ls/autoinsert.go, fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.SpanMap;
using AstNav = TypeScript.AstNavigation.AstNav;
using ScannerApi = TypeScript.Scanner.Scanner;
using Kind = TypeScript.Scanner.Kind;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService
{
    internal (VSOnAutoInsertResponseItemOrNull Result, Exception? Error) ProvideOnAutoInsert(ProtocolRequestContext context, VSOnAutoInsertParams parameters)
    {
        if (UserPreferences().AutoClosingTags == Tristate.False)
            return (new(), null);
        if (!parameters.VSCh.EqualsBytes(">"u8))
            return (new(), null);
        var(_, sourceFile) = getProgramAndFile(parameters.VSTextDocument.Uri);
        var positions = converters.FromLSPPositionForSourceFile(sourceFile, parameters.VSPosition, Feature.AutoInsert);
        if (positions.Length != 1 || !positions[0].Fidelity.IsExact())
            return (new(), null);
        sourceFile = positions[0].Script;
        int position = positions[0].Position;
        var token = AstNav.FindPrecedingToken(sourceFile, position);
        if (token is null)
            return (new(), null);
        ByteStringValue closingText = default;
        Node? element = null;
        if (token.Kind == Kind.GreaterThanToken && Ast.IsJsxOpeningElement(token.Parent))
            element = token.Parent.Parent;
        else if (Ast.IsJsxText(token) && Ast.IsJsxElement(token.Parent))
            element = token.Parent;
        if (element is not null && isUnclosedTag(element.AsJsxElement()))
        {
            var tagName = element.AsJsxElement().OpeningElement.TagName();
            // Original deliberately uses entity spelling rather than verbatim opening-tag text.
            closingText = ByteStringValue.Concat(ByteStringValue.FromString("</"), Ast.EntityNameToStringValue(tagName, ScannerApi.GetTextOfNodeValue), ByteStringValue.FromString(">"));
        }
        else
        {
            Node? fragment = null;
            if (token.Kind == Kind.GreaterThanToken && Ast.IsJsxOpeningFragment(token.Parent))
                fragment = token.Parent.Parent;
            else if (Ast.IsJsxText(token) && Ast.IsJsxFragment(token.Parent))
                fragment = token.Parent;
            if (fragment is not null && isUnclosedFragment(fragment.AsJsxFragment()))
                closingText = ByteStringValue.FromString("</>");
        }

        if (closingText.Length == 0)
            return (new(), null);
        return (new() { VSOnAutoInsertResponseItem = new() { VSTextEditFormat = InsertTextFormat.Snippet, VSTextEdit = new() { Range = new() { Start = parameters.VSPosition, End = parameters.VSPosition }, NewText = ByteStringValue.Concat(ByteStringValue.FromString("$0"), escapeSnippetText(closingText)) } } }, null);
    }

    private static bool isUnclosedTag(JsxElement node)
    {
        var opening = node.OpeningElement;
        var closing = node.ClosingElement;
        if (!Ast.TagNamesAreEquivalent(opening.TagName(), closing.TagName()))
            return true;
        var parent = node.Parent;
        if (Ast.IsJsxElement(parent))
        {
            var parentElement = parent.AsJsxElement();
            return Ast.TagNamesAreEquivalent(opening.TagName(), parentElement.OpeningElement.TagName()) && isUnclosedTag(parentElement);
        }

        return false;
    }

    private static bool isUnclosedFragment(JsxFragment node)
    {
        if ((node.ClosingFragment.Flags & NodeFlags.ThisNodeHasError) != 0)
            return true;
        var parent = node.Parent;
        if (Ast.IsJsxFragment(parent) && isUnclosedFragment(parent.AsJsxFragment()))
            return true;
        return false;
    }
}

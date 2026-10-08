// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/scanner/utilities.go, upstream fed0bf24149f.
// Complete scanner syntax-node/text/identifier helpers; public strings decode the Go byte-string representation.
namespace TypeScript.Scanner;
public partial class Scanner
{
    private static bool tokenIsIdentifierOrKeyword(Kind token) => token >= Kind.Identifier;
    public static Kind IdentifierToKeywordKind(Identifier node) => textToKeyword.TryGetValue(node.Text, out Kind kind) ? kind : Kind.Unknown;
    internal static bool isJSDocTypeExpressionOrChild(Node node)
    {
        if (Ast.IsJSDocTypeExpression(node))
            return true;
        if ((node.Flags & (NodeFlags.JSDoc | NodeFlags.Reparsed)) == 0)
            return false;
        for (Node current = node; current != null; current = current.Parent)
            if (Ast.IsTypeNode(current))
                return true;
        return false;
    }

    internal static string normalizeJSDocTypeSourceText(string text)
    {
        var lineStarts = Core.ComputeECMALineStarts(text);
        if (lineStarts.Count == 1)
            return stripLeadingJSDocComment(text);
        var result = new GoBuilder();
        result.Grow(text.Length);
        for (int i = 0; i < lineStarts.Count; i++)
        {
            if (i > 0)
                result.WriteString("\n");
            int lineEnd = text.Length;
            if (i + 1 < lineStarts.Count)
                lineEnd = lineStarts[i + 1];
            string line = GoStrings.TrimRightFunc(text[lineStarts[i]..lineEnd], StringUtil.IsLineBreak);
            result.WriteString(stripLeadingJSDocComment(line));
        }

        return result.String();
    }

    private static string stripLeadingJSDocComment(string line)
    {
        line = GoStrings.TrimLeftFunc(line, StringUtil.IsWhiteSpaceLike);
        if (line.Length > 0 && line[0] == '*')
            line = line[1..];
        return GoStrings.TrimLeftFunc(line, StringUtil.IsWhiteSpaceLike);
    }

    internal static string GetTextOfNodeFromSourceTextByteString(string sourceText, Node node, bool includeTrivia)
    {
        if (Ast.NodeIsMissing(node))
            return "";
        int pos = node.Pos();
        if (!includeTrivia)
            pos = SkipTriviaByteString(sourceText, pos);
        string text = sourceText[pos..node.End()];
        if (isJSDocTypeExpressionOrChild(node))
            text = normalizeJSDocTypeSourceText(text);
        if ((node.Flags & NodeFlags.ReparserTransformedLiteral) != 0)
        {
            if (Ast.IsStringLiteral(node))
                return (node.AsStringLiteral().TokenFlags & global::TypeScript.Scanner.TokenFlags.SingleQuote) != 0 ? "'" + text + "'" : "\"" + text + "\"";
            if (Ast.IsIdentifier(node))
                return node.Text();
            throw new InvalidOperationException("Unexpected reparser-transformed node kind: " + node.Kind);
        }

        return text;
    }

    public static string GetTextOfNodeFromSourceText(string sourceText, Node node, bool includeTrivia) => ScannerString.ToJsString(GetTextOfNodeFromSourceTextByteString(GoString.ToUtf8(sourceText), node, includeTrivia));
    public static byte[] GetTextOfNodeFromSourceTextBytes(byte[] sourceText, Node node, bool includeTrivia) => GoString.Bytes(GetTextOfNodeFromSourceTextByteString(GoString.FromBytes(sourceText), node, includeTrivia));
    internal static string GetSourceTextOfNodeFromSourceFileByteString(SourceFile sourceFile, Node node, bool includeTrivia) => GetTextOfNodeFromSourceTextByteString(sourceFile.Text(), node, includeTrivia);
    public static string GetSourceTextOfNodeFromSourceFile(SourceFile sourceFile, Node node, bool includeTrivia) => ScannerString.ToJsString(GetSourceTextOfNodeFromSourceFileByteString(sourceFile, node, includeTrivia));
    public static byte[] GetSourceTextOfNodeFromSourceFileBytes(SourceFile sourceFile, Node node, bool includeTrivia) => GoString.Bytes(GetSourceTextOfNodeFromSourceFileByteString(sourceFile, node, includeTrivia));
    internal static string GetTextOfNodeByteString(Node node) => GetSourceTextOfNodeFromSourceFileByteString(Ast.GetSourceFileOfNode(node), node, false);
    internal static string GetTextOfJSDocCommentByteString(NodeList? comment)
    {
        if (comment == null)
            return "";
        var builder = new GoBuilder();
        foreach (Node node in comment.Nodes ?? [])
        {
            switch (node.Kind)
            {
                case Kind.JSDocText:
                    builder.WriteString(node.Text());
                    break;
                case Kind.JSDocLink:
                case Kind.JSDocLinkCode:
                case Kind.JSDocLinkPlain:
                    builder.WriteString(GetTextOfNodeByteString(node));
                    break;
            }
        }

        // unicode.IsSpace uses the Unicode White_Space property, distinct from
        // ECMAScript whitespace (which also includes the BOM and zero-width space).
        return GoStrings.TrimRightFunc(builder.String(), rune => rune is >= 0x09 and <= 0x0D or 0x20 or 0x85 or 0xA0 or 0x1680 or >= 0x2000 and <= 0x200A or 0x2028 or 0x2029 or 0x202F or 0x205F or 0x3000);
    }

    public static string GetTextOfJSDocComment(NodeList? comment) => ScannerString.ToJsString(GetTextOfJSDocCommentByteString(comment));
    public static byte[] GetTextOfJSDocCommentBytes(NodeList? comment) => GoString.Bytes(GetTextOfJSDocCommentByteString(comment));
    internal static string DeclarationNameToStringByteString(Node? name)
    {
        if (name == null || name.Pos() == name.End())
            return "(Missing)";
        return GetTextOfNodeByteString(name);
    }

    public static string DeclarationNameToString(Node? name) => ScannerString.ToJsString(DeclarationNameToStringByteString(name));
    public static byte[] DeclarationNameToStringBytes(Node? name) => GoString.Bytes(DeclarationNameToStringByteString(name));
    public static string GetTextOfNode(Node node) => GetSourceTextOfNodeFromSourceFile(Ast.GetSourceFileOfNode(node), node, false);
    public static byte[] GetTextOfNodeBytes(Node node) => GetSourceTextOfNodeFromSourceFileBytes(Ast.GetSourceFileOfNode(node), node, false);
}

public partial class Scanner
{
    public static bool IsIdentifierText(string name, LanguageVariant languageVariant) => IsIdentifierTextByteString(GoString.ToUtf8(name), languageVariant);
    public static bool IsIdentifierText(byte[] name, LanguageVariant languageVariant) => IsIdentifierTextByteString(GoString.FromBytes(name), languageVariant);
    public static bool IsIntrinsicJsxName(string name) => name.Length != 0 && (name[0] is >= 'a' and <= 'z' || name.Contains('-'));
    // utilities.go:129. Input is the scanner's internal UTF8 byte-string.
    internal static bool IsIdentifierTextByteString(string name, LanguageVariant languageVariant)
    {
        var(ch, size) = Utf8.DecodeRuneInString(name);
        if (!IsIdentifierStart(ch))
            return false;
        for (int i = size; i < name.Length;)
        {
            (ch, size) = Utf8.DecodeRuneInString(name.AsSpan(i));
            if (!IsIdentifierPartEx(ch, languageVariant))
                return false;
            i += size;
        }

        return true;
    }
}

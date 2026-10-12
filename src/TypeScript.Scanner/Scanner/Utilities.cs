// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/scanner/utilities.go, upstream fed0bf24149f.
// Complete scanner syntax-node/text/identifier helpers; public strings decode the Go byte-string representation.
namespace TypeScript.Scanner;
public partial class Scanner
{
    private static bool tokenIsIdentifierOrKeyword(Kind token) => token >= Kind.Identifier;
    public static Kind IdentifierToKeywordKind(Identifier node) => node.TextValue.TryGetValue(textToKeyword, out Kind kind) ? kind : Kind.Unknown;
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

    internal static string normalizeJSDocTypeSourceText(ByteStringView text) => normalizeJSDocTypeSourceTextValue(text).Materialize()!;
    internal static ByteStringValue normalizeJSDocTypeSourceTextValue(ByteStringView text)
    {
        var lineStarts = Core.ComputeECMALineStarts(text);
        if (lineStarts.Count == 1)
            return ByteStringValue.FromView(stripLeadingJSDocComment(text));
        var result = new GoBuilder();
        result.Grow(text.Length);
        for (int i = 0; i < lineStarts.Count; i++)
        {
            if (i > 0)
                result.WriteString("\n");
            int lineEnd = text.Length;
            if (i + 1 < lineStarts.Count)
                lineEnd = lineStarts[i + 1];
            var line = GoStrings.TrimRightFunc(text.Slice(lineStarts[i], lineEnd), StringUtil.IsLineBreak);
            result.WriteString(stripLeadingJSDocComment(line).AsSpan());
        }

        return result.Value();
    }

    private static ByteStringView stripLeadingJSDocComment(ByteStringView line)
    {
        line = GoStrings.TrimLeftFunc(line, StringUtil.IsWhiteSpaceLike);
        if (line.Length > 0 && line[0] == '*')
            line = line.Slice(1);
        return GoStrings.TrimLeftFunc(line, StringUtil.IsWhiteSpaceLike);
    }

    internal static string GetTextOfNodeFromSourceTextByteString(string sourceText, Node node, bool includeTrivia)
        => GetTextOfNodeFromSourceTextByteString((ByteStringView)sourceText, node, includeTrivia);

    internal static string GetTextOfNodeFromSourceTextByteString(ByteStringView sourceText, Node node, bool includeTrivia) =>
        GetTextOfNodeFromSourceTextValue(sourceText, node, includeTrivia).Materialize()!;
    internal static ByteStringValue GetTextOfNodeFromSourceTextValue(ByteStringView sourceText, Node node, bool includeTrivia)
    {
        if (Ast.NodeIsMissing(node)) return default;
        int pos = node.Pos();
        if (!includeTrivia) pos = SkipTriviaByteString(sourceText, pos);
        var borrowed = sourceText.Slice(pos, node.End());
        var text = isJSDocTypeExpressionOrChild(node)
            ? normalizeJSDocTypeSourceTextValue(borrowed) : ByteStringValue.FromView(borrowed);
        if ((node.Flags & NodeFlags.ReparserTransformedLiteral) != 0)
        {
            if (Ast.IsStringLiteral(node))
            {
                var builder = new GoBuilder();
                int quote = (node.AsStringLiteral().TokenFlags & global::TypeScript.Scanner.TokenFlags.SingleQuote) != 0 ? '\'' : '"';
                builder.WriteByte(quote);
                builder.WriteString(text);
                builder.WriteByte(quote);
                return builder.Value();
            }
            if (Ast.IsIdentifier(node)) return node.TextValue();
            throw new InvalidOperationException("Unexpected reparser-transformed node kind: " + node.Kind);
        }
        return text;
    }

    private static byte[] TextBytes(ByteStringValue text) => text.IsBytes ? text.ByteSpan.ToArray() : GoString.Bytes(text.Materialize()!);
    public static string GetTextOfNodeFromSourceText(string sourceText, Node node, bool includeTrivia) => ScannerString.ToJsString(GetTextOfNodeFromSourceTextValue(ByteStringView.FromUnicode(sourceText), node, includeTrivia));
    public static byte[] GetTextOfNodeFromSourceTextBytes(byte[] sourceText, Node node, bool includeTrivia) => TextBytes(GetTextOfNodeFromSourceTextValue(ByteStringView.CopyFrom(sourceText), node, includeTrivia));
    internal static ByteStringValue GetSourceTextOfNodeFromSourceFileValue(SourceFile sourceFile, Node node, bool includeTrivia) => GetTextOfNodeFromSourceTextValue(sourceFile.TextView(), node, includeTrivia);
    internal static string GetSourceTextOfNodeFromSourceFileByteString(SourceFile sourceFile, Node node, bool includeTrivia) => GetSourceTextOfNodeFromSourceFileValue(sourceFile, node, includeTrivia).Materialize()!;
    public static string GetSourceTextOfNodeFromSourceFile(SourceFile sourceFile, Node node, bool includeTrivia) => ScannerString.ToJsString(GetSourceTextOfNodeFromSourceFileValue(sourceFile, node, includeTrivia));
    public static byte[] GetSourceTextOfNodeFromSourceFileBytes(SourceFile sourceFile, Node node, bool includeTrivia) => TextBytes(GetSourceTextOfNodeFromSourceFileValue(sourceFile, node, includeTrivia));
    internal static ByteStringValue GetTextOfNodeValue(Node node) => GetSourceTextOfNodeFromSourceFileValue(Ast.GetSourceFileOfNode(node), node, false);
    internal static string GetTextOfNodeByteString(Node node) => GetTextOfNodeValue(node).Materialize()!;
    internal static string GetTextOfJSDocCommentByteString(NodeList? comment) => GetTextOfJSDocCommentValue(comment).Materialize()!;
    internal static ByteStringValue GetTextOfJSDocCommentValue(NodeList? comment)
    {
        if (comment == null) return default;
        var builder = new GoBuilder();
        foreach (Node node in comment.Nodes)
        {
            switch (node.Kind)
            {
                case Kind.JSDocText:
                    builder.WriteString(node.TextValue());
                    break;
                case Kind.JSDocLink:
                case Kind.JSDocLinkCode:
                case Kind.JSDocLinkPlain:
                    builder.WriteString(GetTextOfNodeValue(node));
                    break;
            }
        }
        // unicode.IsSpace differs from ECMAScript whitespace.
        return GoStrings.TrimRightFunc(builder.Value(), static rune => rune is >= 0x09 and <= 0x0D or 0x20 or 0x85 or 0xA0 or 0x1680 or >= 0x2000 and <= 0x200A or 0x2028 or 0x2029 or 0x202F or 0x205F or 0x3000);
    }
    public static string GetTextOfJSDocComment(NodeList? comment) => ScannerString.ToJsString(GetTextOfJSDocCommentValue(comment));
    public static byte[] GetTextOfJSDocCommentBytes(NodeList? comment) => TextBytes(GetTextOfJSDocCommentValue(comment));
    // scanner utilities: declarationNameToString. Retain borrowed source bytes for emit consumers.
    internal static ByteStringValue DeclarationNameToStringValue(Node? name)
    {
        if (name == null || name.Pos() == name.End())
            return ByteStringValue.FromString("(Missing)");
        return GetTextOfNodeValue(name);
    }
    internal static string DeclarationNameToStringByteString(Node? name) => DeclarationNameToStringValue(name).Materialize()!;

    public static string DeclarationNameToString(Node? name) => ScannerString.ToJsString(DeclarationNameToStringByteString(name));
    public static byte[] DeclarationNameToStringBytes(Node? name) => GoString.Bytes(DeclarationNameToStringByteString(name));
    public static string GetTextOfNode(Node node) => GetSourceTextOfNodeFromSourceFile(Ast.GetSourceFileOfNode(node), node, false);
    public static byte[] GetTextOfNodeBytes(Node node) => GetSourceTextOfNodeFromSourceFileBytes(Ast.GetSourceFileOfNode(node), node, false);
}

public partial class Scanner
{
    public static bool IsIdentifierText(string name, LanguageVariant languageVariant) => IsIdentifierTextByteString(ByteStringValue.FromView(ByteStringView.FromUnicode(name)), languageVariant);
    public static bool IsIdentifierText(byte[] name, LanguageVariant languageVariant) => IsIdentifierTextByteString(ByteStringValue.FromView(ByteStringView.CopyFrom(name)), languageVariant);
    public static bool IsIntrinsicJsxName(string name)
    {
        _ = name.Length;
        return IsIntrinsicJsxName(ByteStringValue.FromString(name));
    }
    internal static bool IsIntrinsicJsxName(ByteStringValue name)
    {
        if (name.IsNull) throw new NullReferenceException();
        return name.Length != 0 && (name[0] is >= 'a' and <= 'z' ||
            (name.IsBytes ? name.ByteSpan.IndexOf((byte)'-') >= 0 : name.CharSpan.IndexOf('-') >= 0));
    }
    // utilities.go:129. Input is the scanner's internal UTF8 byte-string.
    internal static bool IsIdentifierTextByteString(string name, LanguageVariant languageVariant) =>
        IsIdentifierTextByteString(ByteStringValue.FromString(name), languageVariant);
    internal static bool IsIdentifierTextByteString(ByteStringValue name, LanguageVariant languageVariant)
    {
        if (name.IsNull) throw new NullReferenceException();
        var(ch, size) = name.IsBytes ? Utf8.DecodeRuneInString(name.ByteSpan)
            : Utf8.DecodeRuneInString(name.CharSpan);
        if (!IsIdentifierStart(ch))
            return false;
        for (int i = size; i < name.Length;)
        {
            (ch, size) = name.IsBytes ? Utf8.DecodeRuneInString(name.ByteSpan[i..])
                : Utf8.DecodeRuneInString(name.CharSpan[i..]);
            if (!IsIdentifierPartEx(ch, languageVariant))
                return false;
            i += size;
        }

        return true;
    }
}

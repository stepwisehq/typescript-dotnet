// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/scanner/utilities.go, upstream fed0bf24149f.
// Token predicate and parser-required syntax-node source-text helpers.
namespace TypeScript.Scanner;
public partial class Scanner
{
    private static bool tokenIsIdentifierOrKeyword(Kind token) => token >= Kind.Identifier;
    private static bool isJSDocTypeExpressionOrChild(Node node)
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

    private static string normalizeJSDocTypeSourceText(string text)
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned language-service counterpart; body assembly awaiting centralized compilation.
using AST = TypeScript.Scanner.Ast;
using AstNav = TypeScript.AstNavigation.AstNav;
using Format = TypeScript.LanguageServices.Formatting.Formatting;
using Kind = TypeScript.Scanner.Kind;
using Protocol = TypeScript.Lsp.Protocol;
using ScannerApi = TypeScript.Scanner.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using TypeScript.SpanMap;

namespace TypeScript.LanguageServices;
// Source assembly component: JSDocSnippet.cs
internal sealed partial class LanguageService
{
    private sealed class DocCommentTemplate
    {
        internal ByteStringValue newText;
    }

    private sealed class CommentOwnerInfo
    {
        internal Node commentOwner = null!;
        internal Slice<Node> parameters;
        internal bool hasReturn;
    }

    private CompletionList? getJSDocSnippetCompletion(ProtocolRequestContext context, SourceFile file, int position)
    {
        if (UserPreferences().CompleteJSDocs == Tristate.False || !isPotentiallyValidJSDocSnippetCompletionPosition(file, position))
            return null;
        var newline = ByteStringValue.FromString(FormatOptions().NewLineCharacter);
        if (newline.Length == 0)
            newline = ByteStringValue.FromString("\n");
        var template = getDocCommentTemplateAtPosition(file, position, UserPreferences().GenerateReturnInDocTemplate == Tristate.True, newline);
        if (template == null)
            return null;
        var insertText = template.newText;
        InsertTextFormat? insertFormat = null;
        if (clientSupportsItemSnippet(context))
        {
            insertText = templateToSnippet(insertText, newline);
            insertFormat = InsertTextFormat.Snippet;
        }

        var editRange = getJSDocSnippetCompletionRange(context, file, position, insertText);
        Slice<ByteStringValue>? commitCharacters = clientSupportsItemCommitCharacters(context) ? Slice<ByteStringValue>.Make(0) : null;
        var item = new CompletionItem
        {
            Item = new Protocol.CompletionItem
            {
                Label = ByteStringValue.FromString("/** */"),
                Kind = CompletionItemKind.Text,
                Detail = ByteStringValue.FromString(Diagnostics.JSDoc_comment.Localize(context.Locale)),
                SortText = ByteStringValue.FromString("\0"),
                InsertTextFormat = insertFormat,
                TextEdit = editRange,
                CommitCharacters = commitCharacters,
            }
        };
        return new CompletionList
        {
            Items = Slice<CompletionItem?>.FromArray([item])
        };
    }

    private static bool isPotentiallyValidJSDocSnippetCompletionPosition(SourceFile file, int position)
    {
        var text = ByteStringValue.FromView(file.TextView());
        int lineStart = Format.GetLineStartPositionForPosition(position, file);
        if (!isJSDocSnippetPrefix(text.Slice(lineStart, position)))
            return false;
        int lineEnd = getLineEndOfPosition(file, position);
        return isJSDocSnippetSuffix(text.Slice(position, lineEnd));
    }

    private TextEditOrInsertReplaceEdit? getJSDocSnippetCompletionRange(ProtocolRequestContext context, SourceFile file, int position, ByteStringValue newText)
    {
        var text = ByteStringValue.FromView(file.TextView());
        int lineStart = Format.GetLineStartPositionForPosition(position, file), start = position;
        var(prefixStart, hasPrefix) = getJSDocSnippetPrefixStart(text.Slice(lineStart, position));
        if (hasPrefix)
            start = lineStart + prefixStart;
        int lineEnd = getLineEndOfPosition(file, position), end = position;
        var(suffixEnd, hasSuffix) = getJSDocSnippetSuffixEnd(text.Slice(position, lineEnd));
        if (hasSuffix)
            end += suffixEnd;
        var(replacementRange, fidelity) = createLspRangeFromBounds(start, end, file);
        if (!fidelity.IsExact())
            return null;
        return clientSupportsItemInsertReplace(context) ? new TextEditOrInsertReplaceEdit
        {
            InsertReplaceEdit = new InsertReplaceEdit
            {
                NewText = newText,
                Insert = replacementRange,
                Replace = replacementRange
            }
        }

        : new TextEditOrInsertReplaceEdit
        {
            TextEdit = new TextEdit
            {
                NewText = newText,
                Range = replacementRange
            }
        };
    }

    private static DocCommentTemplate? getDocCommentTemplateAtPosition(SourceFile sourceFile, int position, bool generateReturn, ByteStringValue newline)
    {
        var token = AstNav.GetTokenAtPosition(sourceFile, position);
        if (token == null)
            return null;
        var existing = Ast.FindAncestor(token, Ast.IsJSDoc);
        var(docEnd, hasDoc, hasClosing) = getDocCommentEndAtPosition(sourceFile, position);
        bool isEmpty = existing != null || hasDoc;
        if (isNonEmptyJSDoc(existing) && hasDoc && !hasClosing)
        {
            var text = ByteStringValue.FromView(sourceFile.TextView());
            var reparseText = ByteStringValue.Concat(text.Slice(0, position), ByteStringValue.FromString(" */"), text.Slice(position));
            var reparse = Parser.ParseSourceFileOwned(sourceFile.ParseOptions(), reparseText.ByteView, sourceFile.ScriptKind);
            return getDocCommentTemplateAtPosition(reparse, position, generateReturn, newline);
        }

        if (isNonEmptyJSDoc(existing))
            return null;
        if (existing == null && hasDoc)
        {
            token = AstNav.GetTokenAtPosition(sourceFile, skipWhitespace(ByteStringValue.FromView(sourceFile.TextView()), docEnd));
            if (token == null)
                return null;
        }

        int tokenStart = AstNav.GetStartOfNode(token, sourceFile, false);
        if (!isEmpty && tokenStart < position)
            return null;
        var info = getCommentOwnerInfo(token, generateReturn);
        if (info == null)
            return null;
        var owner = info.commentOwner;
        var docs = owner.JSDoc(sourceFile);
        var last = docs.Length == 0 ? null : docs[docs.Length - 1];
        if (AstNav.GetStartOfNode(owner, sourceFile, false) < position || last != null && existing != null && last != existing)
            return null;
        var indentation = getIndentationStringAtPosition(sourceFile, position);
        var tags = parameterDocComments(info.parameters, Ast.IsSourceFileJS(sourceFile), indentation, newline);
        if (info.hasReturn)
            tags = ByteStringValue.Concat(tags, returnsDocComment(indentation, newline));
        if (tags.Length != 0 && !hasJSDocTags(owner, sourceFile))
        {
            var b = new GoBuilder();
            b.WriteString("/**");
            b.WriteString(newline);
            b.WriteString(indentation);
            b.WriteString(" * ");
            b.WriteString(newline);
            b.WriteString(tags);
            b.WriteString(indentation);
            b.WriteString(" */");
            if (tokenStart == position)
            {
                b.WriteString(newline);
                b.WriteString(indentation);
            }

            return new DocCommentTemplate
            {
                newText = b.Value()
            };
        }

        return new DocCommentTemplate
        {
            newText = ByteStringValue.FromString("/** */")
        };
    }

    private static (int End, bool Ok, bool HasClosing) getDocCommentEndAtPosition(SourceFile file, int position)
    {
        var text = ByteStringValue.FromView(file.TextView());
        int start = Format.GetLineStartPositionForPosition(position, file), end = getLineEndOfPosition(file, position);
        if (!trimRightSingleLineWhitespace(text.Slice(start, position)).EndsWith(ByteStringValue.FromString("/**")))
            return (0, false, false);
        var(suffixEnd, hasClosing) = getJSDocSnippetSuffixEnd(text.Slice(position, end));
        return (position + suffixEnd, true, hasClosing);
    }

    private static int skipWhitespace(ByteStringValue text, int position)
    {
        while (position < text.Length)
        {
            var(ch, size) = StringUtil.DecodeJSStringRune(text.Slice(position));
            if (size == 0 || !StringUtil.IsWhiteSpaceLike(ch))
                break;
            position += size;
        }

        return position;
    }

    private static CommentOwnerInfo? getCommentOwnerInfo(Node token, bool generateReturn)
    {
        for (Node? node = token; node != null; node = node.Parent)
        {
            var(info, quit) = getCommentOwnerInfoWorker(node, generateReturn);
            if (info != null || quit)
                return info;
        }

        return null;
    }

    private static (CommentOwnerInfo? Info, bool Quit) getCommentOwnerInfoWorker(Node? owner, bool generateReturn)
    {
        if (owner == null)
            return (null, false);
        CommentOwnerInfo info(Node node, Node? parameterOwner = null) => new()
        {
            commentOwner = node,
            parameters = parameterOwner?.Parameters() ?? default,
            hasReturn = parameterOwner != null && hasReturn(parameterOwner, generateReturn)
        };
        switch (owner.Kind)
        {
            case Kind.FunctionDeclaration:
            case Kind.FunctionExpression:
            case Kind.MethodDeclaration:
            case Kind.Constructor:
            case Kind.MethodSignature:
            case Kind.ArrowFunction:
                return (info(owner, owner), false);
            case Kind.PropertyAssignment:
                return getCommentOwnerInfoWorker(owner.AsPropertyAssignment().Initializer, generateReturn);
            case Kind.ClassDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.EnumDeclaration:
            case Kind.EnumMember:
            case Kind.TypeAliasDeclaration:
                return (info(owner), false);
            case Kind.PropertySignature:
                var type = owner.AsPropertySignatureDeclaration().Type;
                return (info(owner, type != null && Ast.IsFunctionTypeNode(type) ? type : null), false);
            case Kind.VariableStatement:
                var declarations = owner.AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes;
                if (declarations.Length == 1 && declarations[0].AsVariableDeclaration().Initializer is { } initializer && getRightHandSideOfAssignment(initializer)is { } host)
                    return (info(owner, host), false);
                return (info(owner), false);
            case Kind.SourceFile:
                return (null, true);
            case Kind.ModuleDeclaration:
                return owner.Parent.Kind == Kind.ModuleDeclaration ? (null, false) : (info(owner), false);
            case Kind.ExpressionStatement:
                return getCommentOwnerInfoWorker(owner.AsExpressionStatement().Expression, generateReturn);
            case Kind.BinaryExpression:
                var binary = owner.AsBinaryExpression();
                if (Ast.GetAssignmentDeclarationKind(owner) == JSDeclarationKind.None)
                    return (null, true);
                return (info(owner, Ast.IsFunctionLike(binary.Right) ? binary.Right : null), false);
            case Kind.PropertyDeclaration:
                var value = owner.AsPropertyDeclaration().Initializer;
                if (value != null && Ast.IsFunctionExpressionOrArrowFunction(value))
                    return (info(owner, value), false);
                break;
        }

        return (null, false);
    }

    private static bool hasReturn(Node node, bool generateReturn)
    {
        if (!generateReturn)
            return false;
        if (Ast.IsFunctionTypeNode(node))
            return true;
        if (Ast.IsArrowFunction(node) && node.Body()is { } expression && Ast.IsExpression(expression))
            return true;
        return Ast.IsFunctionLikeDeclaration(node) && node.Body()is { } body && Ast.IsBlock(body) && Ast.ForEachReturnStatement(body, static _ => true);
    }

    private static Node? getRightHandSideOfAssignment(Node? right)
    {
        if (right == null)
            return null;
        while (right.Kind == Kind.ParenthesizedExpression)
            right = right.AsParenthesizedExpression().Expression;
        if (right.Kind is Kind.FunctionExpression or Kind.ArrowFunction)
            return right;
        if (right.Kind == Kind.ClassExpression)
            return Core.Find(right.Members(), Ast.IsConstructorDeclaration);
        return null;
    }

    private static ByteStringValue parameterDocComments(Slice<Node> parameters, bool javascript, ByteStringValue indentation, ByteStringValue newline)
    {
        var b = new GoBuilder();
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var name = Ast.IsIdentifier(parameter.Name()) ? parameter.Name().TextValue() : ByteStringValue.FromString("param" + i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            b.WriteString(indentation);
            b.WriteString(" * @param ");
            if (javascript)
                b.WriteString(parameter.AsParameterDeclaration().DotDotDotToken != null ? "{...any} " : "{any} ");
            b.WriteString(name);
            b.WriteString(newline);
        }

        return b.Value();
    }

    private static ByteStringValue returnsDocComment(ByteStringValue indentation, ByteStringValue newline) => ByteStringValue.Concat(indentation, ByteStringValue.FromString(" * @returns"), newline);
    private static ByteStringValue getIndentationStringAtPosition(SourceFile file, int position)
    {
        var text = ByteStringValue.FromView(file.TextView());
        int start = Format.GetLineStartPositionForPosition(position, file), pos = start;
        while (pos < position)
        {
            var(ch, size) = StringUtil.DecodeJSStringRune(text.Slice(pos));
            if (size == 0 || !StringUtil.IsWhiteSpaceSingleLine(ch))
                break;
            pos += size;
        }

        return text.Slice(start, pos);
    }

    private static bool isNonEmptyJSDoc(Node? doc) => doc != null && (doc.AsJSDoc().Comment is { } comment && comment.Nodes.Length > 0 || doc.AsJSDoc().Tags is { } tags && tags.Nodes.Length > 0);
    private static bool hasJSDocTags(Node node, SourceFile file)
    {
        var docs = node.JSDoc(file);
        return docs.Length > 0 && docs[docs.Length - 1].AsJSDoc().Tags is { } tags && tags.Nodes.Length > 0;
    }

    private static ByteStringValue templateToSnippet(ByteStringValue template, ByteStringValue newline)
    {
        if (template.EqualsString("/** */"))
        {
            var b = new GoBuilder();
            b.WriteString("/**");
            b.WriteString(newline);
            b.WriteString(" * $0");
            b.WriteString(newline);
            b.WriteString(" */");
            return b.Value();
        }

        int index = 1;
        template = escapeSnippetText(template);
        template = stripJSDocTemplateIndentation(template, newline);
        return transformJSDocTemplateLines(template, newline, ref index);
    }

    private static ByteStringValue stripJSDocTemplateIndentation(ByteStringValue template, ByteStringValue newline)
    {
        var lines = splitCompletionText(template, newline);
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = trimJSDocSpacesAndTabs(lines[i]);
            if (trimmed.StartsWith(ByteStringValue.FromString("/")))
                lines[i] = trimmed;
            else if (trimmed.StartsWith(ByteStringValue.FromString("*")))
                lines[i] = ByteStringValue.Concat(ByteStringValue.FromString(" "), trimmed);
        }

        return GoStrings.Join(lines, newline);
    }

    private static ByteStringValue transformJSDocTemplateLines(ByteStringValue template, ByteStringValue newline, ref int index)
    {
        var lines = splitCompletionText(template, newline);
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (i > 0 && lines[i - 1].StartsWith(ByteStringValue.FromString("/**")) && lineHasOnlyJSDocAsterisk(line))
            {
                lines[i] = ByteStringValue.Concat(line, ByteStringValue.FromString("$0"));
                continue;
            }

            var(param, isParam) = transformJSDocParamLine(line, ref index);
            if (isParam)
            {
                lines[i] = param;
                continue;
            }

            var(returns, isReturns) = transformJSDocReturnsLine(line, ref index);
            if (isReturns)
                lines[i] = returns;
        }

        return GoStrings.Join(lines, newline);
    }

    private static ByteStringValue trimJSDocSpacesAndTabs(ByteStringValue line)
    {
        int start = 0;
        while (start < line.Length && line[start] is ' ' or '\t')
            start++;
        return line.Slice(start);
    }

    private static bool lineHasOnlyJSDocAsterisk(ByteStringValue line)
    {
        line = trimJSDocSpacesAndTabs(line);
        return line.StartsWith(ByteStringValue.FromString("*")) && isOnlySpacesOrTabs(line.Slice(1));
    }

    private static (ByteStringValue Text, bool Ok) transformJSDocParamLine(ByteStringValue line, ref int index)
    {
        var prefix = ByteStringValue.FromString("");
        var rest = line;
        if (rest.StartsWith(ByteStringValue.FromString(" ")))
        {
            prefix = ByteStringValue.FromString(" ");
            rest = rest.Slice(1);
        }

        if (!rest.StartsWith(ByteStringValue.FromString("* @param")))
            return (default, false);
        rest = rest.Slice("* @param".Length);
        if (!startsWithSingleLineWhitespace(rest))
            return (default, false);
        rest = trimJSDocSpacesAndTabs(rest);
        ByteStringValue type = default;
        if (rest.StartsWith(ByteStringValue.FromString("{")))
        {
            int close = rest.IndexOf('}');
            if (close < 0)
                return (default, false);
            type = ByteStringValue.Concat(ByteStringValue.FromString(" "), rest.Slice(0, close + 1));
            rest = rest.Slice(close + 1);
            if (!startsWithSingleLineWhitespace(rest))
                return (default, false);
            rest = trimJSDocSpacesAndTabs(rest);
        }

        var(name, remaining, ok) = scanNonWhitespace(rest);
        if (!ok || !isOnlySpacesOrTabs(remaining))
            return (default, false);
        var b = new GoBuilder();
        b.WriteString(prefix);
        b.WriteString("* @param ");
        if (type.EqualsString(" {any}") || type.EqualsString(" {*}"))
        {
            b.WriteString("{${");
            b.WriteString(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            b.WriteString(":*}} ");
            index++;
        }
        else if (type.Length != 0)
        {
            b.WriteString(type);
            b.WriteString(" ");
        }

        b.WriteString(name);
        b.WriteString(" ${");
        b.WriteString(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        b.WriteString("}");
        index++;
        return (b.Value(), true);
    }

    private static (ByteStringValue Text, bool Ok) transformJSDocReturnsLine(ByteStringValue line, ref int index)
    {
        var prefix = ByteStringValue.FromString("");
        var rest = line;
        if (rest.StartsWith(ByteStringValue.FromString(" ")))
        {
            prefix = ByteStringValue.FromString(" ");
            rest = rest.Slice(1);
        }

        if (!rest.StartsWith(ByteStringValue.FromString("* @returns")) || !isOnlySpacesOrTabs(rest.Slice("* @returns".Length)))
            return (default, false);
        var b = new GoBuilder();
        b.WriteString(prefix);
        b.WriteString("* @returns ${");
        b.WriteString(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        b.WriteString("}");
        index++;
        return (b.Value(), true);
    }

    private static (ByteStringValue Word, ByteStringValue Remaining, bool Ok) scanNonWhitespace(ByteStringValue text)
    {
        if (text.Length == 0)
            return (default, default, false);
        for (int i = 0; i < text.Length;)
        {
            var(ch, size) = StringUtil.DecodeJSStringRune(text.Slice(i));
            if (size == 0 || StringUtil.IsWhiteSpaceLike(ch))
                return i == 0 ? (default, default, false) : (text.Slice(0, i), text.Slice(i), true);
            i += size;
        }

        return (text, ByteStringValue.FromString(""), true);
    }

    private static bool isJSDocSnippetPrefix(ByteStringValue prefix)
    {
        var trimmed = trimRightSingleLineWhitespace(prefix);
        if (trimmed.EndsWith(ByteStringValue.FromString("/**")))
            return true;
        int start = skipSingleLineWhitespace(prefix, 0);
        if (start >= trimmed.Length || trimmed[start] != '/' || start + 3 > trimmed.Length)
            return false;
        for (int i = start + 1; i < trimmed.Length; i++)
            if (trimmed[i] != '*')
                return false;
        return trimmed.Length - start >= 3;
    }

    private static (int Start, bool Ok) getJSDocSnippetPrefixStart(ByteStringValue prefix)
    {
        var trimmed = trimRightSingleLineWhitespace(prefix);
        for (int i = trimmed.Length - 1; i >= 0 && trimmed[i] == '*'; i--)
            if (i > 0 && trimmed[i - 1] == '/')
                return (i - 1, true);
        if (trimmed.EndsWith(ByteStringValue.FromString("/")))
            return (trimmed.Length - 1, true);
        return (0, false);
    }

    private static bool isJSDocSnippetSuffix(ByteStringValue suffix)
    {
        suffix = suffix.Slice(skipSingleLineWhitespace(suffix, 0));
        var trimmed = trimRightSingleLineWhitespace(suffix);
        if (trimmed.Length == 0)
            return true;
        if (!trimmed.EndsWith(ByteStringValue.FromString("/")))
            return false;
        for (int i = 0; i < trimmed.Length - 1; i++)
            if (trimmed[i] != '*')
                return false;
        return true;
    }

    private static (int End, bool Ok) getJSDocSnippetSuffixEnd(ByteStringValue suffix)
    {
        int pos = skipSingleLineWhitespace(suffix, 0);
        while (pos < suffix.Length && suffix[pos] == '*')
            pos++;
        return pos < suffix.Length && suffix[pos] == '/' ? (pos + 1, true) : (0, false);
    }

    private static ByteStringValue trimRightSingleLineWhitespace(ByteStringValue text)
    {
        int end = 0;
        for (int pos = 0; pos < text.Length;)
        {
            var(ch, size) = StringUtil.DecodeJSStringRune(text.Slice(pos));
            if (size == 0)
                break;
            pos += size;
            if (!StringUtil.IsWhiteSpaceSingleLine(ch))
                end = pos;
        }

        return text.Slice(0, end);
    }

    private static int skipSingleLineWhitespace(ByteStringValue text, int pos)
    {
        while (pos < text.Length)
        {
            var(ch, size) = StringUtil.DecodeJSStringRune(text.Slice(pos));
            if (size == 0 || !StringUtil.IsWhiteSpaceSingleLine(ch))
                break;
            pos += size;
        }

        return pos;
    }

    private static bool isOnlySingleLineWhitespace(ByteStringValue text) => skipSingleLineWhitespace(text, 0) == text.Length;
    private static bool startsWithSingleLineWhitespace(ByteStringValue text)
    {
        if (text.Length == 0)
            return false;
        var(ch, size) = StringUtil.DecodeJSStringRune(text);
        return size != 0 && StringUtil.IsWhiteSpaceSingleLine(ch);
    }

    private static bool isOnlySpacesOrTabs(ByteStringValue text)
    {
        for (int i = 0; i < text.Length; i++)
            if (text[i] != ' ' && text[i] != '\t')
                return false;
        return true;
    }
}

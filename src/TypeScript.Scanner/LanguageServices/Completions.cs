// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete pinned language-service counterpart; body assembly awaiting centralized compilation.
using AST = TypeScript.Scanner.Ast;
using AstNav = TypeScript.AstNavigation.AstNav;
using AutoImportFunctions = TypeScript.LanguageServices.AutoImport.AutoImport;
using BuilderFlags = TypeScript.NodeBuilding.Flags;
using CheckerType = TypeScript.Checking.Type;
using Debug = TypeScript.Scanner.Debug;
using Format = TypeScript.LanguageServices.Formatting.Formatting;
using Formatting = TypeScript.LanguageServices.Formatting.Formatting;
using Kind = TypeScript.Scanner.Kind;
using UnionReduction = TypeScript.Checking.Checker.UnionReduction;
using EF = TypeScript.DiagnosticPrinting.EmitFlags;
using NodeFactory = TypeScript.Scanner.NodeFactory;
using Protocol = TypeScript.Lsp.Protocol;
using ScannerApi = TypeScript.Scanner.Scanner;
using Type = TypeScript.Checking.Type;
using TypeScript.Checking;
using TypeScript.Compiler;
using TypeScript.DiagnosticPrinting;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.LanguageServices.Change;
using TypeScript.LanguageServices.Utilities;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using TypeScript.SpanMap;

namespace TypeScript.LanguageServices;
// Source assembly component: CompletionLiteralNames.cs
internal sealed partial class LanguageService
{
    private static ByteStringValue completionNameForLiteral(SourceFile file, UserPreferences preferences, object literal)
    {
        switch (literal)
        {
            case ByteStringValue text:
                return quote(file, preferences, text);
            case string text:
                return quote(file, preferences, ByteStringValue.FromString(text));
            case JsNum.Number number:
                var(name, _) = Core.StringifyJson(number, default, default);
                return name;
            case JsNum.PseudoBigInt number:
                return completionConcat(ByteStringValue.FromString(number.String()), ByteStringValue.FromString("n"));
        }

        throw new InvalidOperationException("Unhandled literal value: " + TypeScript.ProjectSystem.Logging.LogFormat.Sprint(literal));
    }
}

// Source assembly component: CompletionSnippetWriter.cs
internal sealed partial class LanguageService
{
    private sealed class SnippetEmitTextWriter(ChangeTrackerWriter writer) : EmitTextWriter
    {
        internal Slice<TextChange> escapes;
        public void Write(string text)
        {
            var value = ByteStringValue.FromString(text);
            var escaped = escapeSnippetText(value);
            if (!escaped.EqualsValue(value))
            {
                int start = writer.GetTextPos();
                writer.Write(text);
                int end = writer.GetTextPos();
                escapes = escapes.Append(new TextChange { NewText = escaped, TextRange = new TextRange(start, end) });
            }
            else
                writer.Write(text);
        }

        public void WriteTrailingSemicolon(string text) => writer.WriteTrailingSemicolon(text);
        public void WriteComment(string text)
        {
            var value = ByteStringValue.FromString(text);
            var escaped = escapeSnippetText(value);
            if (!escaped.EqualsValue(value))
            {
                int start = writer.GetTextPos();
                writer.WriteComment(text);
                int end = writer.GetTextPos();
                escapes = escapes.Append(new TextChange { NewText = escaped, TextRange = new TextRange(start, end) });
            }
            else
                writer.WriteComment(text);
        }

        public void WriteKeyword(string text) => writer.WriteKeyword(text);
        public void WriteOperator(string text) => writer.WriteOperator(text);
        public void WritePunctuation(string text) => writer.WritePunctuation(text);
        public void WriteSpace(string text) => writer.WriteSpace(text);
        public void WriteStringLiteral(string text)
        {
            var value = ByteStringValue.FromString(text);
            var escaped = escapeSnippetText(value);
            if (!escaped.EqualsValue(value))
            {
                int start = writer.GetTextPos();
                writer.WriteStringLiteral(text);
                int end = writer.GetTextPos();
                escapes = escapes.Append(new TextChange { NewText = escaped, TextRange = new TextRange(start, end) });
            }
            else
                writer.WriteStringLiteral(text);
        }

        public void WriteParameter(string text)
        {
            var value = ByteStringValue.FromString(text);
            var escaped = escapeSnippetText(value);
            if (!escaped.EqualsValue(value))
            {
                int start = writer.GetTextPos();
                writer.WriteParameter(text);
                int end = writer.GetTextPos();
                escapes = escapes.Append(new TextChange { NewText = escaped, TextRange = new TextRange(start, end) });
            }
            else
                writer.WriteParameter(text);
        }

        public void WriteProperty(string text)
        {
            var value = ByteStringValue.FromString(text);
            var escaped = escapeSnippetText(value);
            if (!escaped.EqualsValue(value))
            {
                int start = writer.GetTextPos();
                writer.WriteProperty(text);
                int end = writer.GetTextPos();
                escapes = escapes.Append(new TextChange { NewText = escaped, TextRange = new TextRange(start, end) });
            }
            else
                writer.WriteProperty(text);
        }

        public void WriteSymbol(string text, Symbol symbol)
        {
            var value = ByteStringValue.FromString(text);
            var escaped = escapeSnippetText(value);
            if (!escaped.EqualsValue(value))
            {
                int start = writer.GetTextPos();
                writer.WriteSymbol(text, symbol);
                int end = writer.GetTextPos();
                escapes = escapes.Append(new TextChange { NewText = escaped, TextRange = new TextRange(start, end) });
            }
            else
                writer.WriteSymbol(text, symbol);
        }

        public void WriteLine() => writer.WriteLine();
        public void WriteLineForce(bool force) => writer.WriteLineForce(force);
        public void IncreaseIndent() => writer.IncreaseIndent();
        public void DecreaseIndent() => writer.DecreaseIndent();
        public void Clear() => writer.Clear();
        public void RawWrite(string text) => writer.RawWrite(text);
        public void WriteLiteral(string text) => writer.WriteLiteral(text);
        public string String() => writer.String();
        public ByteStringValue TextValue() => writer.TextValue();
        public int GetTextPos() => writer.GetTextPos();
        public int GetLine() => writer.GetLine();
        public int GetColumn() => writer.GetColumn();
        public int GetIndent() => writer.GetIndent();
        public bool IsAtStartOfLine() => writer.IsAtStartOfLine();
        public bool HasTrailingComment() => writer.HasTrailingComment();
        public bool HasTrailingWhitespace() => writer.HasTrailingWhitespace();
        public void Write(ByteStringValue text)
        {
            var value = text;
            var escaped = escapeSnippetText(value);
            if (!escaped.EqualsValue(value))
            {
                int start = writer.GetTextPos();
                writer.Write(text);
                int end = writer.GetTextPos();
                escapes = escapes.Append(new TextChange { NewText = escaped, TextRange = new TextRange(start, end) });
            }
            else
                writer.Write(text);
        }

        public void WriteComment(ByteStringValue text)
        {
            var value = text;
            var escaped = escapeSnippetText(value);
            if (!escaped.EqualsValue(value))
            {
                int start = writer.GetTextPos();
                writer.WriteComment(text);
                int end = writer.GetTextPos();
                escapes = escapes.Append(new TextChange { NewText = escaped, TextRange = new TextRange(start, end) });
            }
            else
                writer.WriteComment(text);
        }

        public void WriteKeyword(ByteStringValue text) => writer.WriteKeyword(text);
        public void WriteOperator(ByteStringValue text) => writer.WriteOperator(text);
        public void WritePunctuation(ByteStringValue text) => writer.WritePunctuation(text);
        public void WriteSpace(ByteStringValue text) => writer.WriteSpace(text);
        public void WriteStringLiteral(ByteStringValue text)
        {
            var value = text;
            var escaped = escapeSnippetText(value);
            if (!escaped.EqualsValue(value))
            {
                int start = writer.GetTextPos();
                writer.WriteStringLiteral(text);
                int end = writer.GetTextPos();
                escapes = escapes.Append(new TextChange { NewText = escaped, TextRange = new TextRange(start, end) });
            }
            else
                writer.WriteStringLiteral(text);
        }

        public void WriteParameter(ByteStringValue text)
        {
            var value = text;
            var escaped = escapeSnippetText(value);
            if (!escaped.EqualsValue(value))
            {
                int start = writer.GetTextPos();
                writer.WriteParameter(text);
                int end = writer.GetTextPos();
                escapes = escapes.Append(new TextChange { NewText = escaped, TextRange = new TextRange(start, end) });
            }
            else
                writer.WriteParameter(text);
        }

        public void WriteProperty(ByteStringValue text)
        {
            var value = text;
            var escaped = escapeSnippetText(value);
            if (!escaped.EqualsValue(value))
            {
                int start = writer.GetTextPos();
                writer.WriteProperty(text);
                int end = writer.GetTextPos();
                escapes = escapes.Append(new TextChange { NewText = escaped, TextRange = new TextRange(start, end) });
            }
            else
                writer.WriteProperty(text);
        }

        public void WriteSymbol(ByteStringValue text, Symbol symbol)
        {
            var value = text;
            var escaped = escapeSnippetText(value);
            if (!escaped.EqualsValue(value))
            {
                int start = writer.GetTextPos();
                writer.WriteSymbol(text, symbol);
                int end = writer.GetTextPos();
                escapes = escapes.Append(new TextChange { NewText = escaped, TextRange = new TextRange(start, end) });
            }
            else
                writer.WriteSymbol(text, symbol);
        }

        public void WriteLiteral(ByteStringValue text) => writer.WriteLiteral(text);
        public void RawWrite(ByteStringValue text) => writer.RawWrite(text);
    }
}

// Source assembly component: CompletionTextContext.cs
internal sealed partial class LanguageService
{
    private (AutoImport.IImportAdder? , Exception? ) createImportAdder(ProtocolRequestContext context, Checker checker, SourceFile file)
    {
        if (TsPath.IsDynamicFileName(file.FileName()))
            return (null, null);
        var(view, error) = getPreparedAutoImportView(file, checker);
        if (error != null)
            return (null, error);
        if (view == null)
            return (null, null);
        return (AutoImport.AutoImport.NewImportAdder(context, GetProgram(), checker, file, view, FormatOptions(), converters, UserPreferences()), null);
    }

    private static bool isRecommendedCompletionMatch(Symbol local, Symbol? recommended, Checker checker) => local == recommended || (local.Flags & SymbolFlags.ExportValue) != 0 && checker.GetExportSymbolOfSymbol(local) == recommended;
    private static readonly HashSet<int> wordSeparators = ['`', '~', '!', '@', '%', '^', '&', '*', '(', ')', '-', '=', '+', '[', '{', ']', '}', '\\', '|', ';', ':', '\'', '"', ',', '.', '<', '>', '/', '?'];
    private static (int Length, int Start) getWordLengthAndStart(SourceFile file, int position)
    {
        var text = file.TextView().Slice(0, position);
        int total = 0, first = 0;
        for (var decoded = Utf8.DecodeLastRuneInString(text.AsSpan()); decoded.Size != 0; decoded = Utf8.DecodeLastRuneInString(text.Slice(0, text.Length - total).AsSpan()))
        {
            if (wordSeparators.Contains(decoded.Rune) || GoUnicode.IsSpace(decoded.Rune))
                break;
            total += decoded.Size;
            first = decoded.Rune;
        }

        if (first == '@')
        {
            total--;
            (first, _) = Utf8.DecodeRuneInString(text.Slice(text.Length - total).AsSpan());
        }

        return (total, first);
    }

    private static ByteStringValue trimElementAccess(ByteStringValue text)
    {
        if (text.StartsWith(ByteStringValue.FromString("[")))
            text = text.Slice(1);
        if (text.EndsWith(ByteStringValue.FromString("]")))
            text = text.Slice(0, text.Length - 1);
        if (text.StartsWith(ByteStringValue.FromString("'")) && text.EndsWith(ByteStringValue.FromString("'")))
            text = text.Slice(1, text.Length - 1);
        if (text.StartsWith(ByteStringValue.FromString("\"")) && text.EndsWith(ByteStringValue.FromString("\"")))
            text = text.Slice(1, text.Length - 1);
        return text;
    }

    private static ByteStringValue getFilterText(SourceFile file, int position, ByteStringValue insert, ByteStringValue label, int wordStart, ByteStringValue dot)
    {
        if (label.StartsWith(ByteStringValue.FromString("#")))
        {
            var after = label.Slice(1);
            if (insert.Length != 0)
            {
                if (insert.StartsWith(ByteStringValue.FromString("this.#")))
                    return wordStart == '#' ? default : insert.Slice(6);
            }
            else
                return wordStart == '#' ? default : after;
        }

        if (insert.StartsWith(ByteStringValue.FromString("this.")))
            return default;
        if (insert.StartsWith(ByteStringValue.FromString("[")))
            return completionConcat(dot, trimElementAccess(insert));
        if (insert.StartsWith(ByteStringValue.FromString("?.")))
            return completionConcat(dot, insert.StartsWith(ByteStringValue.FromString("?.[")) ? trimElementAccess(insert.Slice(2)) : insert.Slice(2));
        return insert;
    }

    private static ByteStringValue getDotAccessor(SourceFile file, int position)
    {
        var text = ByteStringValue.FromView(file.TextView().Slice(0, position));
        if (text.EndsWith(ByteStringValue.FromString("?.")))
            return text.Slice(position - 2, position);
        if (text.EndsWith(ByteStringValue.FromString(".")))
            return text.Slice(position - 1, position);
        return default;
    }

    private static bool strPtrIsEmpty(ByteStringValue? value) => value == null || value.Value.Length == 0;
    private static ByteStringValue? strPtrTo(ByteStringValue value) => value.Length == 0 ? null : value;
    private static bool? boolToPtr(bool value) => value ? true : null;
    private static bool isClassLikeMemberCompletion(Symbol symbol, Node location, SourceFile file)
    {
        if (AST.IsInJSFile(location))
            return false;
        var flags = SymbolFlags.ClassMember & SymbolFlags.EnumMemberExcludes;
        return (symbol.Flags & flags) != 0 && (AST.IsClassLike(location) || location.Parent != null && location.Parent.Parent != null && AST.IsClassElement(location.Parent) && location == location.Parent.Name() && LsUtil.GetLastToken(location.Parent, file) == location.Parent.Name() && AST.IsClassLike(location.Parent.Parent) || location.Parent != null && AST.IsSyntaxList(location) && AST.IsClassLike(location.Parent));
    }

    private static bool symbolAppearsToBeTypeOnly(Symbol symbol, Checker checker)
    {
        var flags = checker.SkipAlias(symbol).CombinedLocalAndExportSymbolFlags();
        return (flags & SymbolFlags.Value) == 0 && (symbol.Declarations.Length == 0 || !AST.IsInJSFile(symbol.Declarations[0]) || (flags & SymbolFlags.Type) != 0);
    }
}

// Source assembly component: CompletionClassJsxFilters.cs
internal sealed partial class LanguageService
{
    private static Slice<Symbol> filterClassMembersList(Slice<Symbol> baseSymbols, Slice<Node> existingMembers, ModifierFlags flags, SourceFile file, int position)
    {
        HashSet<ByteStringKey>? names = null;
        foreach (var member in existingMembers)
        {
            if (member.Kind is not (Kind.PropertyDeclaration or Kind.MethodDeclaration or Kind.GetAccessor or Kind.SetAccessor))
                continue;
            if (isCurrentlyEditingNode(member, file, position))
                continue;
            if ((member.ModifierFlags() & ModifierFlags.Private) != 0)
                continue;
            if (AST.IsStatic(member) != ((flags & ModifierFlags.Static) != 0))
                continue;
            var name = AST.GetPropertyNameValueForPropertyNameNode(member.Name());
            if (name.Length != 0)
                (names ??= new()).Add(ByteStringKey.FromValue(name));
        }

        return Core.Filter(baseSymbols, symbol => !(names?.Contains(ByteStringKey.FromValue(AST.SymbolNameValue(symbol))) ?? false) && symbol.Declarations.Length > 0 && (Checker.GetDeclarationModifierFlagsFromSymbol(symbol) & ModifierFlags.Private) == 0 && !(symbol.ValueDeclaration != null && AST.IsPrivateIdentifierClassElementDeclaration(symbol.ValueDeclaration)));
    }

    private static Node? tryGetContainingJsxElement(Node? token, SourceFile file)
    {
        if (token == null)
            return null;
        var parent = token.Parent;
        switch (token.Kind)
        {
            case Kind.GreaterThanToken:
            case Kind.LessThanSlashToken:
            case Kind.SlashToken:
            case Kind.Identifier:
            case Kind.PropertyAccessExpression:
            case Kind.JsxNamespacedName:
            case Kind.JsxAttributes:
            case Kind.JsxAttribute:
            case Kind.JsxSpreadAttribute:
                if (parent != null && parent.Kind is Kind.JsxSelfClosingElement or Kind.JsxOpeningElement)
                {
                    if (token.Kind == Kind.GreaterThanToken)
                    {
                        var preceding = AstNav.FindPrecedingToken(file, token.Pos());
                        if (parent.TypeArguments().Length == 0 || preceding != null && preceding.Kind == Kind.SlashToken)
                            return null;
                    }

                    return parent;
                }

                if (parent != null && AST.IsJsxNamespacedName(parent) && parent.Parent != null && parent.Parent.Kind is Kind.JsxSelfClosingElement or Kind.JsxOpeningElement)
                    return parent.Parent;
                if (parent != null && parent.Kind == Kind.JsxAttribute)
                    return parent.Parent!.Parent;
                break;
            case Kind.StringLiteral:
                if (parent != null && parent.Kind is Kind.JsxAttribute or Kind.JsxSpreadAttribute)
                    return parent.Parent!.Parent;
                break;
            case Kind.CloseBraceToken:
                if (parent != null && parent.Kind == Kind.JsxExpression && parent.Parent != null && parent.Parent.Kind == Kind.JsxAttribute)
                    return parent.Parent.Parent!.Parent;
                if (parent != null && parent.Kind == Kind.JsxSpreadAttribute)
                    return parent.Parent!.Parent;
                break;
        }

        return null;
    }

    private static (Slice<Symbol> filteredMembers, HashSet<ByteStringKey>? spreadMemberNames) filterJsxAttributes(Slice<Symbol> symbols, Slice<Node> attributes, SourceFile file, int position, Checker checker)
    {
        HashSet<ByteStringKey>? names = null, spread = null;
        foreach (var attribute in attributes)
        {
            if (isCurrentlyEditingNode(attribute, file, position))
                continue;
            if (attribute.Kind == Kind.JsxAttribute)
                (names ??= new()).Add(ByteStringKey.FromValue(attribute.Name()!.TextValue()));
            else if (AST.IsJsxSpreadAttribute(attribute))
                setMemberDeclaredBySpreadAssignment(attribute, ref spread, checker);
        }

        return (Core.Filter(symbols, symbol => !(names?.Contains(ByteStringKey.FromValue(symbol.NameValue)) ?? false)), spread);
    }

    private static bool isTypeKeywordTokenOrIdentifier(Node node) => AST.IsTypeKeywordToken(node) || AST.IsIdentifier(node) && ScannerApi.IdentifierToKeywordKind(node.AsIdentifier()) == Kind.TypeKeyword;
}

// Source assembly component: CompletionItemCreation.cs
internal sealed partial class LanguageService
{
    private static ByteStringValue completionConcat(params ReadOnlySpan<ByteStringValue> values) => ByteStringValue.Concat(values);
    private (Protocol.CompletionItem? , Exception? ) createCompletionItem(ProtocolRequestContext context, Checker checker, Symbol symbol, ByteStringValue sortText, Node? replacementToken, CompletionDataData data, int position, SourceFile file, ByteStringValue name, bool convert, SymbolOriginInfo? origin, bool semicolons, CompilerOptions options, bool member)
    {
        var contextToken = data.contextToken;
        ByteStringValue insert = default, filter = default;
        var replacement = getReplacementRangeForContextToken(file, replacementToken, position);
        bool snippet = false, action = false;
        var source = getSourceFromOrigin(origin);
        CompletionItemLabelDetails? label = null;
        var preferences = UserPreferences();
        bool questionDot = originIsNullableMember(origin);
        bool braces = originIsSymbolMember(origin) || convert;
        if (originIsThisTypeNode(origin))
        {
            insert = convert ? completionConcat(ByteStringValue.FromString("this"), ByteStringValue.FromString(questionDot ? "?." : ""), ByteStringValue.FromString("["), quotePropertyName(file, preferences, name), ByteStringValue.FromString("]")) : completionConcat(ByteStringValue.FromString("this"), ByteStringValue.FromString(questionDot ? "?." : "."), name);
        }
        else if (data.propertyAccessToConvert is { } access && (braces || questionDot))
        {
            insert = braces ? completionConcat(ByteStringValue.FromString("["), convert ? quotePropertyName(file, preferences, name) : name, ByteStringValue.FromString("]")) : name;
            if (questionDot || access.QuestionDotToken() != null)
                insert = completionConcat(ByteStringValue.FromString("?."), insert);
            var dot = AstNav.FindChildOfKind(access, Kind.DotToken, file) ?? AstNav.FindChildOfKind(access, Kind.QuestionDotToken, file);
            if (dot == null)
                return (null, null);
            int end = name.StartsWith(access.Name()!.TextValue()) ? access.End() : dot.End();
            var(range, fidelity) = createLspRangeFromBounds(AstNav.GetStartOfNode(dot, file, false), end, file);
            if (!fidelity.IsExact())
                return (null, null);
            replacement = range;
        }

        if (data.jsxInitializer.isInitializer)
        {
            if (insert.Length == 0)
                insert = name;
            insert = completionConcat(ByteStringValue.FromString("{"), insert, ByteStringValue.FromString("}"));
            if (data.jsxInitializer.initializer != null)
            {
                var(range, fidelity) = createLspRangeFromNode(data.jsxInitializer.initializer, file);
                if (!fidelity.IsExact())
                    return (null, null);
                replacement = range;
            }
        }

        if (originIsPromise(origin) && data.propertyAccessToConvert is { } awaitedAccess)
        {
            if (insert.Length == 0)
                insert = name;
            var preceding = AstNav.FindPrecedingToken(file, awaitedAccess.Pos());
            var awaitText = completionConcat(ByteStringValue.FromString(preceding != null && LsUtil.PositionIsASICandidate(preceding.End(), preceding.Parent, file) ? ";" : ""), ByteStringValue.FromString("(await "), ScannerApi.GetTextOfNodeValue(awaitedAccess.Expression()), ByteStringValue.FromString(")"));
            insert = convert ? completionConcat(awaitText, insert) : completionConcat(awaitText, ByteStringValue.FromString(questionDot ? "?." : "."), insert);
            var wrap = AST.IsAwaitExpression(awaitedAccess.Parent) ? awaitedAccess.Parent! : awaitedAccess.Expression()!;
            var(range, fidelity) = createLspRangeFromBounds(AstNav.GetStartOfNode(wrap, file, false), awaitedAccess.End(), file);
            if (!fidelity.IsExact())
                return (null, null);
            replacement = range;
        }

        if (originIsTypeOnlyAlias(origin))
            action = true;
        if (data.completionKind == CompletionKind.ObjectPropertyDeclaration && contextToken != null && !AST.NodeHasKind(AstNav.FindPrecedingTokenEx(file, contextToken.Pos(), contextToken, false), Kind.CommaToken))
        {
            var parent = contextToken.Parent!;
            if (AST.IsMethodDeclaration(parent.Parent) || AST.IsGetAccessorDeclaration(parent.Parent) || AST.IsSetAccessorDeclaration(parent.Parent) || AST.IsSpreadAssignment(parent) || LsUtil.GetLastToken(AST.FindAncestor(parent, AST.IsPropertyAssignment), file) == contextToken || AST.IsShorthandPropertyAssignment(parent) && getLineOfPosition(file, contextToken.End()) != getLineOfPosition(file, position))
            {
                source = CompletionSource.ObjectLiteralMemberWithComma;
                action = true;
            }
        }

        Slice<TextEdit?>? additional = null;
        if (preferences.IncludeCompletionsWithClassMemberSnippets == Tristate.True && data.completionKind == CompletionKind.MemberLike && isClassLikeMemberCompletion(symbol, data.location, file))
        {
            var(entry, error) = getEntryForMemberCompletion(context, checker, symbol, name, data.location, position, contextToken, file);
            if (error != null)
                return (null, error);
            if (entry == null)
                return (null, null);
            insert = entry.insertText;
            filter = entry.filterText;
            snippet = entry.isSnippet;
            if (entry.additionalTextEdits.Length > 0)
            {
                additional = entry.additionalTextEdits;
                action = true;
                source = CompletionSource.ClassMemberSnippet;
            }
        }

        if (originIsObjectLiteralMethod(origin))
        {
            var method = origin!.asObjectLiteralMethod();
            insert = method.insertText;
            snippet = method.isSnippet;
            label = method.labelDetails;
            if (!clientSupportsItemLabelDetails(context))
            {
                name = completionConcat(name, label!.Detail!.Value);
                label = null;
            }

            source = CompletionSource.ObjectLiteralMethodSnippet;
            sortText = CompletionSortText.SortBelow(sortText);
        }

        if (data.isJsxIdentifierExpected && !data.isRightOfOpenTag && clientSupportsItemSnippet(context) && preferences.JsxAttributeCompletionStyle != JsxAttributeCompletionStyle.None && !(data.location.Parent != null && AST.IsJsxAttribute(data.location.Parent) && data.location.Parent.Initializer() != null))
        {
            bool useBraces = preferences.JsxAttributeCompletionStyle == JsxAttributeCompletionStyle.Braces;
            var type = checker.GetTypeOfSymbolAtLocation(symbol, data.location);
            if (preferences.JsxAttributeCompletionStyle == JsxAttributeCompletionStyle.Auto && !type.IsBooleanLike() && !(type.IsUnion() && Core.Some(type.Types(), value => value.IsBooleanLike())))
            {
                if (type.IsStringLike() || type.IsUnion() && Core.Every(type.Types(), value => (value.Flags() & (TypeFlags.StringLike | TypeFlags.Undefined)) != 0 || isStringAndEmptyAnonymousObjectIntersection(checker, value)))
                {
                    insert = completionConcat(escapeSnippetText(name), ByteStringValue.FromString("="), quote(file, preferences, ByteStringValue.FromString("$1")));
                    snippet = true;
                }
                else
                    useBraces = true;
            }

            if (useBraces)
            {
                insert = completionConcat(escapeSnippetText(name), ByteStringValue.FromString("={$1}"));
                snippet = true;
            }
        }

        var named = AST.FindAncestor(data.location, isNamedImportsOrExports);
        if (named != null)
        {
            if (!ScannerApi.IsIdentifierTextByteString(name, LanguageVariant.Standard))
            {
                insert = quotePropertyName(file, preferences, name);
                if (named.Kind == Kind.NamedImports)
                {
                    var scanner = ScannerApi.NewScanner();
                    scanner.SetByteTextInternal(file.TextView());
                    scanner.ResetPos(position);
                    if (!(scanner.Scan() == Kind.AsKeyword && scanner.Scan() == Kind.Identifier))
                        insert = completionConcat(insert, ByteStringValue.FromString(" as "), generateIdentifierForArbitraryString(name));
                }
            }
            else if (named.Kind == Kind.NamedImports)
            {
                var token = ScannerApi.StringToToken(name);
                if (token != Kind.Unknown && (token == Kind.AwaitKeyword || LsUtil.IsNonContextualKeyword(token)))
                    insert = completionConcat(name, ByteStringValue.FromString(" as "), name, ByteStringValue.FromString("_"));
            }
        }

        var elementKind = LsUtil.GetSymbolKind(checker, symbol, data.location);
        Slice<ByteStringValue>? commits = null;
        if (clientSupportsItemCommitCharacters(context))
        {
            if (elementKind == ScriptElementKind.Warning || elementKind == ScriptElementKind.String)
                commits = Slice<ByteStringValue>.Make(0);
            else if (!clientSupportsDefaultCommitCharacters(context))
                commits = data.defaultCommitCharacters;
        }

        bool select = isRecommendedCompletionMatch(symbol, data.recommendedCompletion, checker);
        var modifiers = LsUtil.GetSymbolModifiers(checker, symbol);
        return (createLSPCompletionItem(context, name, insert, filter, sortText, elementKind, modifiers, replacement, commits, label, file, position, member, snippet, action, select, source, null, additional, null), null);
    }

    private sealed class MemberCompletionEntry
    {
        internal ByteStringValue insertText, filterText;
        internal bool isSnippet;
        internal Slice<TextEdit?> additionalTextEdits;
    }
}

// Source assembly component: CompletionSymbolContext.cs
internal sealed partial class LanguageService
{
    private static bool shouldIncludeSymbol(Symbol symbol, CompletionDataData data, Node? closest, SourceFile file, Checker checker, CompilerOptions options)
    {
        var flags = symbol.Flags;
        var location = data.location;
        if (location.Parent != null && AST.IsExportAssignment(location.Parent))
            return true;
        if (closest != null && AST.IsVariableDeclaration(closest) && symbol.ValueDeclaration == closest)
            return false;
        var declaration = symbol.ValueDeclaration ?? Core.FirstOrNil(symbol.Declarations);
        if (closest != null && declaration != null)
        {
            if (AST.IsParameterDeclaration(closest) && AST.IsParameterDeclaration(declaration))
            {
                var parameters = closest.Parent!.ParameterList();
                if (declaration.Pos() >= closest.Pos() && declaration.Pos() < parameters!.End())
                    return false;
            }
            else if (AST.IsTypeParameterDeclaration(closest) && AST.IsTypeParameterDeclaration(declaration))
            {
                if (closest == declaration && data.contextToken?.Kind == Kind.ExtendsKeyword)
                    return false;
                if (isInTypeParameterDefault(data.contextToken) && !AST.IsInferTypeNode(closest.Parent))
                {
                    var parameters = closest.Parent!.TypeParameterList();
                    if (parameters != null && declaration.Pos() >= closest.Pos() && declaration.Pos() < parameters.End())
                        return false;
                }
            }
        }

        var origin = checker.SkipAlias(symbol);
        ByteStringValue sort = default;
        data.symbolToSortTextMap?.TryGetValue(AST.GetSymbolId(symbol), out sort);
        if (file.ExternalModuleIndicator != null && options.AllowUmdGlobalAccess != Tristate.True && symbol != origin && sort.EqualsValue(CompletionSortText.GlobalsOrKeywords) && symbol.Parent != null && Checker.IsExternalModuleSymbol(symbol.Parent))
            return false;
        flags |= origin.CombinedLocalAndExportSymbolFlags();
        if ((symbol.Flags & SymbolFlags.Alias) != 0)
            flags |= checker.GetSymbolFlags(symbol);
        if (isInRightSideOfInternalImportEqualsDeclaration(location))
            return (flags & SymbolFlags.Namespace) != 0;
        if (data.isTypeOnlyLocation)
            return symbolCanBeReferencedAtTypeLocation(symbol, checker, null);
        return (flags & SymbolFlags.Value) != 0;
    }

    private static (ByteStringValue, bool) getCompletionEntryDisplayNameForSymbol(SourceFile file, UserPreferences preferences, Symbol symbol, SymbolOriginInfo? origin, CompletionKind kind, bool jsx)
    {
        if (originIsIgnore(origin))
            return (default, false);
        var name = originIncludesSymbolName(origin) ? origin!.symbolName() : AST.SymbolNameValue(symbol);
        if (name.Length == 0 || (symbol.Flags & SymbolFlags.Module) != 0 && startsWithQuote(name) || Checker.IsKnownSymbol(symbol))
            return (default, false);
        if (ScannerApi.IsIdentifierTextByteString(name, jsx ? LanguageVariant.JSX : LanguageVariant.Standard) || symbol.ValueDeclaration != null && AST.IsPrivateIdentifierClassElementDeclaration(symbol.ValueDeclaration))
            return (name, false);
        if ((symbol.Flags & SymbolFlags.Alias) != 0)
            return (name, true);
        switch (kind)
        {
            case CompletionKind.MemberLike:
                return originIsComputedPropertyName(origin) ? (origin!.symbolName(), false) : (default, false);
            case CompletionKind.ObjectPropertyDeclaration:
                return (quote(file, preferences, name), false);
            case CompletionKind.PropertyAccess:
            case CompletionKind.Global:
                return completionDecodeRune(name).Rune == ' ' ? (default, false) : (name, true);
            case CompletionKind.None:
            case CompletionKind.String:
                return (name, false);
            default:
                throw new InvalidOperationException("Unexpected completion kind: " + (int)kind);
        }
    }

    private static bool originIsIgnore(SymbolOriginInfo? origin) => origin != null && (origin.kind & SymbolOriginInfoKind.Ignore) != 0;
    private static bool originIncludesSymbolName(SymbolOriginInfo? origin) => originIsComputedPropertyName(origin);
    private static bool originIsComputedPropertyName(SymbolOriginInfo? origin) => origin != null && (origin.kind & SymbolOriginInfoKind.ComputedPropertyName) != 0;
    private static bool originIsObjectLiteralMethod(SymbolOriginInfo? origin) => origin != null && (origin.kind & SymbolOriginInfoKind.ObjectLiteralMethod) != 0;
    private static bool originIsThisTypeNode(SymbolOriginInfo? origin) => origin != null && (origin.kind & SymbolOriginInfoKind.ThisType) != 0;
    private static bool originIsTypeOnlyAlias(SymbolOriginInfo? origin) => origin != null && (origin.kind & SymbolOriginInfoKind.TypeOnlyAlias) != 0;
    private static bool originIsSymbolMember(SymbolOriginInfo? origin) => origin != null && (origin.kind & SymbolOriginInfoKind.SymbolMember) != 0;
    private static bool originIsNullableMember(SymbolOriginInfo? origin) => origin != null && (origin.kind & SymbolOriginInfoKind.Nullable) != 0;
    private static bool originIsPromise(SymbolOriginInfo? origin) => origin != null && (origin.kind & SymbolOriginInfoKind.Promise) != 0;
    private static ByteStringValue getSourceFromOrigin(SymbolOriginInfo? origin) => originIsThisTypeNode(origin) ? CompletionSource.ThisProperty : originIsTypeOnlyAlias(origin) ? CompletionSource.TypeOnlyAlias : default;
    private static (Node? Context, Node? Previous) getRelevantTokens(int position, SourceFile file)
    {
        var previous = AstNav.FindPrecedingToken(file, position);
        if (previous != null && position <= previous.End() && (AST.IsMemberName(previous) || AST.IsKeywordKind(previous.Kind)))
            return (AstNav.FindPrecedingToken(file, previous.Pos()), previous);
        return (previous, previous);
    }

    private static bool isValidTrigger(SourceFile file, ByteStringValue trigger, Node? token, int position)
    {
        if (trigger.EqualsValue(ByteStringValue.FromString(".")) || trigger.EqualsValue(ByteStringValue.FromString("@")))
            return true;
        if (trigger.EqualsValue(ByteStringValue.FromString("\"")) || trigger.EqualsValue(ByteStringValue.FromString("'")) || trigger.EqualsValue(ByteStringValue.FromString("`")))
            return token != null && isStringLiteralOrTemplate(token) && position == AstNav.GetStartOfNode(token, file, false) + 1;
        if (trigger.EqualsValue(ByteStringValue.FromString("#")))
            return token != null && AST.IsPrivateIdentifier(token) && AST.GetContainingClass(token) != null;
        if (trigger.EqualsValue(ByteStringValue.FromString("<")))
            return token != null && token.Kind == Kind.LessThanToken && (!AST.IsBinaryExpression(token.Parent) || binaryExpressionMayBeOpenTag(token.Parent!.AsBinaryExpression()));
        if (trigger.EqualsValue(ByteStringValue.FromString("/")))
            return token != null && (AST.IsStringLiteralLike(token) ? AST.TryGetImportFromModuleSpecifier(token) != null : token.Kind == Kind.LessThanSlashToken && AST.IsJsxClosingElement(token.Parent));
        if (trigger.EqualsValue(ByteStringValue.FromString(" ")))
            return token != null && token.Kind == Kind.ImportKeyword && token.Parent!.Kind == Kind.SourceFile;
        if (trigger.EqualsValue(ByteStringValue.FromString("*")))
            return isPotentiallyValidJSDocSnippetCompletionPosition(file, position);
        throw new InvalidOperationException("Unknown trigger character: " + trigger.Materialize());
    }

    private static bool isStringLiteralOrTemplate(Node node) => node.Kind is Kind.StringLiteral or Kind.NoSubstitutionTemplateLiteral or Kind.TemplateExpression or Kind.TaggedTemplateExpression;
    private static bool binaryExpressionMayBeOpenTag(BinaryExpression binary) => AST.NodeIsMissing(binary.Left);
    private static bool isCheckedFile(SourceFile file, CompilerOptions options) => !AST.IsSourceFileJS(file) || AST.IsCheckJSEnabledForFile(file, options);
    private static bool isContextTokenValueLocation(Node? token) => token != null && (token.Kind == Kind.TypeOfKeyword && (token.Parent!.Kind == Kind.TypeQuery || AST.IsTypeOfExpression(token.Parent)) || token.Kind == Kind.AssertsKeyword && token.Parent!.Kind == Kind.TypePredicate);
    private static bool isPossiblyTypeArgumentPosition(Node? token, SourceFile file, Checker checker)
    {
        var info = getPossibleTypeArgumentsInfo(token, file);
        return info != null && (AST.IsPartOfTypeNode(info.called) || getPossibleGenericSignatures(info.called, info.nTypeArguments, checker).Length != 0 || isPossiblyTypeArgumentPosition(info.called, file, checker));
    }

    private static bool isContextTokenTypeLocation(Node? token)
    {
        if (token == null)
            return false;
        var parent = token.Parent!.Kind;
        return token.Kind switch
        {
            Kind.ColonToken => parent is Kind.PropertyDeclaration or Kind.PropertySignature or Kind.Parameter or Kind.VariableDeclaration || AST.IsFunctionLikeKind(parent),
            Kind.EqualsToken => parent is Kind.TypeAliasDeclaration or Kind.TypeParameter,
            Kind.AsKeyword => parent == Kind.AsExpression,
            Kind.LessThanToken => parent is Kind.TypeReference or Kind.TypeAssertionExpression,
            Kind.ExtendsKeyword => parent == Kind.TypeParameter,
            Kind.SatisfiesKeyword => parent == Kind.SatisfiesExpression,
            Kind.OpenBracketToken or Kind.CommaToken => parent == Kind.TupleType,
            _ => false,
        };
    }

    private static bool symbolCanBeReferencedAtTypeLocation(Symbol symbol, Checker checker, HashSet<ulong>? seen) => nonAliasCanBeReferencedAtTypeLocation(symbol, checker, seen) || nonAliasCanBeReferencedAtTypeLocation(checker.SkipAlias(symbol.ExportSymbol ?? symbol), checker, seen);
    private static bool nonAliasCanBeReferencedAtTypeLocation(Symbol symbol, Checker checker, HashSet<ulong>? seen)
    {
        if ((symbol.Flags & SymbolFlags.Type) != 0 || checker.IsUnknownSymbol(symbol))
            return true;
        if ((symbol.Flags & SymbolFlags.Module) == 0)
            return false;
        seen ??= new HashSet<ulong>();
        return seen.Add(AST.GetSymbolId(symbol)) && Core.Some(checker.GetExportsOfModule(symbol), export => symbolCanBeReferencedAtTypeLocation(export, checker, seen));
    }

    private static Slice<Symbol> getPropertiesForCompletion(Type type, Checker checker) => type.IsUnion() ? checkCompletionSymbolsDefined(checker.GetAllPossiblePropertiesOfTypes(type.Types()), "getAllPossiblePropertiesOfTypes() should all be defined.") : checkCompletionSymbolsDefined(checker.GetApparentProperties(type), "getApparentProperties() should all be defined.");
    private static Node? getLeftMostName(Node expression) => AST.IsIdentifier(expression) ? expression : AST.IsPropertyAccessExpression(expression) ? getLeftMostName(expression.Expression()!) : null;
    private static Symbol? getFirstSymbolInChain(Symbol symbol, Node? enclosing, Checker checker)
    {
        var chain = checker.getAccessibleSymbolChain(symbol, enclosing!, SymbolFlags.All, false);
        if (chain.Length > 0)
            return chain[0];
        if (symbol.Parent != null)
            return isModuleSymbol(symbol.Parent) ? symbol : getFirstSymbolInChain(symbol.Parent, enclosing, checker);
        return null;
    }

    private static bool isModuleSymbol(Symbol symbol) => Core.Some(symbol.Declarations, declaration => declaration.Kind == Kind.SourceFile);
    private static SymbolOriginInfoKind getNullableSymbolOriginInfoKind(SymbolOriginInfoKind kind, bool questionDot) => questionDot ? kind | SymbolOriginInfoKind.Nullable : kind;
    private static bool isStaticProperty(Symbol symbol) => symbol.ValueDeclaration != null && (symbol.ValueDeclaration.ModifierFlags() & ModifierFlags.Static) != 0 && AST.IsClassLike(symbol.ValueDeclaration.Parent);
}

// Source assembly component: CompletionMemberSnippets.cs
internal sealed partial class LanguageService
{
    private SymbolOriginInfoObjectLiteralMethod? getEntryForObjectLiteralMethodCompletion(ProtocolRequestContext context, Checker checker, Symbol symbol, Node enclosing, SourceFile file)
    {
        var printer = createSnippetPrinter(new PrinterOptions { RemoveComments = true, NewLine = CompilerOptionHelpers.GetNewLineKind(FormatOptions().NewLineCharacter), Target = GetProgram().Options().GetEmitScriptTarget() }, null);
        bool snippet = clientSupportsItemSnippet(context);
        var method = createObjectLiteralMethod(printer, checker, symbol, enclosing, file, snippet);
        if (method == null)
            return null;
        var insert = completionConcat(printer.printAndFormatNodeWithSettings(context, method, file, Tracker.GetFormatCodeSettingsForWriting(FormatOptions(), file)), ByteStringValue.FromString(","));
        return new SymbolOriginInfoObjectLiteralMethod
        {
            insertText = insert,
            labelDetails = new CompletionItemLabelDetails
            {
                Detail = printObjectLiteralMethodLabelDetail(method, file, printer.factory)
            },
            isSnippet = snippet
        };
    }

    private Node? createObjectLiteralMethod(SnippetPrinter printer, Checker checker, Symbol symbol, Node enclosing, SourceFile file, bool snippet)
    {
        var factory = printer.factory;
        var declaration = Core.FirstOrNil(symbol.Declarations);
        if (!isObjectLiteralMethodCompletionCandidateDeclaration(declaration))
            return null;
        var effective = checker.GetWidenedType(checker.GetTypeOfSymbolAtLocation(symbol, enclosing));
        if ((effective.Flags() & TypeFlags.Union) != 0 && effective.Types().Length < 10)
            effective = checker.GetUnionTypeEx(effective.Types(), UnionReduction.Subtype);
        if ((effective.Flags() & TypeFlags.Union) != 0)
        {
            Type? function = null;
            foreach (var part in effective.Types())
            {
                if (checker.GetSignaturesOfType(part, SignatureKind.Call).Count == 0)
                    continue;
                if (function != null)
                    return null;
                function = part;
            }

            if (function == null)
                return null;
            effective = function;
        }

        if (checker.GetSignaturesOfType(effective, SignatureKind.Call).Count != 1)
            return null;
        var flags = BuilderFlags.OmitThisParameter;
        if (LsUtil.GetQuotePreference(file, UserPreferences()) == QuotePreference.Single)
            flags |= BuilderFlags.UseSingleQuotesForStringLiteralType;
        var type = checker.TypeToTypeNode(effective, enclosing, flags, null);
        if (type == null || type.Kind != Kind.FunctionType)
            return null;
        var parameters = Slice<Node>.Make(0, type.AsFunctionTypeNode().Parameters.Nodes.Length);
        foreach (var parameter in type.AsFunctionTypeNode().Parameters.Nodes)
            parameters = parameters.Append(factory.NewParameterDeclaration(null, parameter.AsParameterDeclaration().DotDotDotToken, parameter.Name()!.Clone(factory), null, null, parameter.AsParameterDeclaration().Initializer));
        var body = factory.NewBlock(factory.NewNodeList(default(Slice<Node>)), true);
        if (snippet)
            body = createSnippetTabStopBody(factory, printer.emitContext);
        return factory.NewMethodDeclaration(null, null, declaration!.Name()!.Clone(factory), null, null, factory.NewNodeList(parameters), null, null, body);
    }

    private static bool isObjectLiteralMethodCompletionCandidateDeclaration(Node? declaration) => declaration?.Kind is Kind.PropertySignature or Kind.PropertyDeclaration or Kind.MethodSignature or Kind.MethodDeclaration;
    private readonly record struct ObjectLiteralMethodSymbol(Symbol symbol, SymbolOriginInfo origin);
    private Slice<ObjectLiteralMethodSymbol> collectObjectLiteralMethodSymbols(ProtocolRequestContext context, Checker checker, Slice<Symbol> members, Node enclosing, SourceFile file)
    {
        if (AST.IsSourceFileJS(file))
            return default;
        var preferences = UserPreferences();
        Slice<ObjectLiteralMethodSymbol> methods = default;
        foreach (var member in members)
        {
            if (!isObjectLiteralMethodSymbol(member))
                continue;
            var(name, _) = getCompletionEntryDisplayNameForSymbol(file, preferences, member, null, CompletionKind.ObjectPropertyDeclaration, false);
            if (name.Length == 0)
                continue;
            var entry = getEntryForObjectLiteralMethodCompletion(context, checker, member, enclosing, file);
            if (entry != null)
                methods = methods.Append(new ObjectLiteralMethodSymbol(member, new SymbolOriginInfo { kind = SymbolOriginInfoKind.ObjectLiteralMethod, data = entry }));
        }

        return methods;
    }

    private static bool isObjectLiteralMethodSymbol(Symbol symbol) => (symbol.Flags & (SymbolFlags.Property | SymbolFlags.Method)) != 0;
    private ByteStringValue printObjectLiteralMethodLabelDetail(Node method, SourceFile file, NodeFactory factory)
    {
        var declaration = method.AsMethodDeclaration();
        var signature = factory.NewMethodSignatureDeclaration(null, factory.NewIdentifierWithTextValue(default), declaration.PostfixToken, declaration.TypeParameters, declaration.Parameters, declaration.Type);
        var printer = Printer.NewPrinter(new PrinterOptions { RemoveComments = true, OmitTrailingSemicolon = true, NewLine = CompilerOptionHelpers.GetNewLineKind(FormatOptions().NewLineCharacter), Target = GetProgram().Options().GetEmitScriptTarget() }, new PrintHandlers(), null);
        return printer.EmitValue(signature, file);
    }

    private (MemberCompletionEntry? , Exception? ) getEntryForMemberCompletion(ProtocolRequestContext context, Checker checker, Symbol symbol, ByteStringValue name, Node location, int position, Node? token, SourceFile file)
    {
        var classDeclaration = AST.FindAncestor(location, AST.IsClassLike);
        if (classDeclaration == null)
            return (null, null);
        var(importAdder, error) = createImportAdder(context, checker, file);
        if (error != null)
            return (null, error);
        var tracker = Tracker.NewTracker(context, GetProgram().Options(), FormatOptions(), converters);
        var fixer = newMissingMemberFixer(tracker, GetProgram(), checker, UserPreferences(), importAdder, context.Locale);
        var present = getPresentMemberModifiers(token, file, position);
        bool isAbstract = (present.modifiers & ModifierFlags.Abstract) != 0 && (classDeclaration.ModifierFlags() & ModifierFlags.Abstract) != 0;
        bool snippet = clientSupportsItemSnippet(context);
        var body = tracker.NodeFactory.NewBlock(tracker.NodeFactory.NewNodeList(default(Slice<Node>)), true);
        if (snippet)
            body = createSnippetTabStopBody(tracker.NodeFactory, tracker.EmitContext);
        var nodes = fixer.createMemberFromSymbol(symbol, classDeclaration, file, body, PreserveOptionalFlags.Property, isAbstract);
        Slice<TextEdit?> edits = default;
        if (importAdder != null && importAdder.HasFixes())
            edits = nullableTextEditHeader(importAdder.Edits());
        if (present.eraseRange != null)
            edits = edits.Append(new TextEdit { Range = present.eraseRange.Value, NewText = default });
        var modifiers = ModifierFlags.None;
        var completionNodes = Slice<Node>.Make(0, nodes.Length);
        foreach (var node in nodes)
        {
            if (node == null)
                continue;
            if (completionNodes.Length == 0)
            {
                modifiers = node.ModifierFlags();
                if (isAbstract)
                    modifiers |= ModifierFlags.Abstract;
                if (AST.IsClassElement(node) && checker.GetMemberOverrideModifierStatus(classDeclaration, node, symbol) == MemberOverrideStatus.NeedsOverride)
                    modifiers |= ModifierFlags.Override;
            }

            completionNodes = completionNodes.Append(node);
        }

        if (completionNodes.Length == 0)
            return (new MemberCompletionEntry { insertText = name, filterText = name, isSnippet = snippet, additionalTextEdits = edits }, null);
        var allowed = modifiers | ModifierFlags.Override | ModifierFlags.Public;
        allowed |= (symbol.Flags & SymbolFlags.Method) != 0 ? ModifierFlags.Async : ModifierFlags.Ambient | ModifierFlags.Readonly;
        var allowedPresent = present.modifiers & allowed;
        if ((present.modifiers & ~allowed) != 0)
            return (null, null);
        if ((modifiers & ModifierFlags.Protected) != 0 && (allowedPresent & ModifierFlags.Public) != 0)
            modifiers &= ~ModifierFlags.Protected;
        if (allowedPresent != ModifierFlags.None && (allowedPresent & ModifierFlags.Public) == 0)
            modifiers &= ~ModifierFlags.Public;
        modifiers |= allowedPresent;
        var newline = FormatOptions().NewLineCharacter;
        var printer = createSnippetPrinter(new PrinterOptions { RemoveComments = true, NewLine = CompilerOptionHelpers.GetNewLineKind(newline), Target = GetProgram().Options().GetEmitScriptTarget() }, tracker.EmitContext);
        Node? decorated = null;
        if (present.decorators.Length > 0 && AST.CanHaveDecorators(completionNodes[completionNodes.Length - 1]))
            decorated = completionNodes[completionNodes.Length - 1];
        var texts = Slice<ByteStringValue>.Make(0, completionNodes.Length);
        foreach (var original in completionNodes)
        {
            var node = AST.ReplaceModifiers(tracker.NodeFactory, original, createModifierList(tracker.NodeFactory, modifiers, original == decorated ? present.decorators : default));
            texts = texts.Append(printer.printAndFormatNodeWithSettings(context, node, file, Tracker.GetFormatCodeSettingsForWriting(FormatOptions(), file)));
        }

        var insert = joinCompletionText(texts, ByteStringValue.FromString(newline));
        if (insert.Length == 0)
            return (null, null);
        return (new MemberCompletionEntry { insertText = insert, filterText = name, isSnippet = snippet, additionalTextEdits = edits }, null);
    }

    private struct PresentMemberModifiers
    {
        internal ModifierFlags modifiers;
        internal Slice<Node> decorators;
        internal TypeScript.Lsp.Protocol.Range? eraseRange;
    }

    private PresentMemberModifiers getPresentMemberModifiers(Node? token, SourceFile file, int position)
    {
        if (token == null || getLineOfPosition(file, position) > getLineOfPosition(file, token.End()))
            return default;
        var modifiers = ModifierFlags.None;
        Slice<Node> decorators = default;
        int start = position, end = position;
        if (AST.IsPropertyDeclaration(token.Parent))
        {
            var kind = modifierLikeKind(token);
            if (kind == Kind.Unknown)
                return default;
            var nodes = token.Parent!.ModifierNodes();
            if (nodes.Length > 0)
            {
                modifiers |= AST.ModifiersToFlags(nodes) & ModifierFlags.Modifier;
                foreach (var modifier in nodes)
                {
                    if (AST.IsDecorator(modifier))
                        decorators = decorators.Append(modifier);
                    start = Math.Min(start, ScannerApi.GetTokenPosOfNode(modifier, file, false));
                }
            }

            var flag = AST.ModifierToFlag(kind);
            if ((modifiers & flag) == 0)
            {
                modifiers |= flag;
                start = Math.Min(start, AstNav.GetStartOfNode(token, file, false));
            }

            if (token.Parent.Name() != token)
                end = AstNav.GetStartOfNode(token.Parent.Name(), file, false);
        }

        TypeScript.Lsp.Protocol.Range? erase = null;
        if (start < end)
        {
            var(range, fidelity) = createLspRangeFromBounds(start, end, file);
            if (fidelity.IsExact())
                erase = range;
        }

        return new PresentMemberModifiers
        {
            modifiers = modifiers,
            decorators = decorators,
            eraseRange = erase
        };
    }

    private static Kind modifierLikeKind(Node? node)
    {
        if (node == null)
            return Kind.Unknown;
        if (AST.IsModifier(node))
            return node.Kind;
        if (AST.IsIdentifier(node))
        {
            var kind = ScannerApi.IdentifierToKeywordKind(node.AsIdentifier());
            if (kind != Kind.Unknown && AST.IsModifierKind(kind))
                return kind;
        }

        return Kind.Unknown;
    }

    private static ModifierList? createModifierList(NodeFactory factory, ModifierFlags flags, Slice<Node> decorators)
    {
        Slice<Node> nodes = default;
        foreach (var decorator in decorators)
            nodes = nodes.Append(decorator.Clone(factory));
        nodes = nodes.Append(AST.CreateModifiersFromModifierFlags(flags, factory.NewModifier));
        return nodes.Length == 0 ? null : factory.NewModifierList(nodes);
    }

    private static Node createSnippetTabStopBody(NodeFactory factory, EmitContext context)
    {
        var empty = factory.NewEmptyStatement();
        context.SetSnippetElement(empty, new SnippetElement { Kind = SnippetKind.TabStop, Order = 0 });
        return factory.NewBlock(factory.NewNodeList(Slice<Node>.FromArray([empty])), true);
    }
}

// Source assembly component: CompletionTypeExpressions.cs
internal sealed partial class LanguageService
{
    private static Node? typeNodeToExpression(Node typeNode, ScriptTarget target, QuotePreference quotePreference, NodeFactory factory)
    {
        switch (typeNode.Kind)
        {
            case Kind.TypeReference:
                return entityNameToExpression(typeNode.AsTypeReferenceNode().TypeName, target, quotePreference, factory);
            case Kind.IndexedAccessType:
                var objectExpression = typeNodeToExpression(typeNode.AsIndexedAccessTypeNode().ObjectType, target, quotePreference, factory);
                var indexExpression = typeNodeToExpression(typeNode.AsIndexedAccessTypeNode().IndexType, target, quotePreference, factory);
                return objectExpression != null && indexExpression != null ? factory.NewElementAccessExpression(objectExpression, null, indexExpression, NodeFlags.None) : null;
            case Kind.LiteralType:
                var literal = typeNode.AsLiteralTypeNode().Literal;
                return literal.Kind switch
                {
                    Kind.StringLiteral => factory.NewStringLiteralWithTextValue(literal.TextValue(), quotePreference == QuotePreference.Single ? TokenFlags.SingleQuote : TokenFlags.None),
                    Kind.NumericLiteral => factory.NewNumericLiteralWithTextValue(literal.TextValue(), literal.AsNumericLiteral().TokenFlags),
                    _ => null,
                };
            case Kind.ParenthesizedType:
                var expression = typeNodeToExpression(typeNode.AsParenthesizedTypeNode().Type, target, quotePreference, factory);
                if (expression == null || AST.IsIdentifier(expression))
                    return expression;
                return factory.NewParenthesizedExpression(expression);
            case Kind.TypeQuery:
                return entityNameToExpression(typeNode.AsTypeQueryNode().ExprName, target, quotePreference, factory);
            case Kind.ImportType:
                Debug.Fail("We should not get an import type after calling 'typeToAutoImportableTypeNode'.");
                return null;
        }

        return null;
    }

    private static Node entityNameToExpression(Node entityName, ScriptTarget target, QuotePreference quotePreference, NodeFactory factory)
    {
        if (AST.IsIdentifier(entityName))
            return entityName;
        return factory.NewPropertyAccessExpression(entityNameToExpression(entityName.AsQualifiedName().Left, target, quotePreference, factory), null, entityName.AsQualifiedName().Right, NodeFlags.None);
    }
}

// Source assembly component: CompletionJSDocParameters.cs
internal sealed partial class LanguageService
{
    private static ByteStringValue joinCompletionText(Slice<ByteStringValue> texts, ByteStringValue separator) => GoStrings.Join(texts, separator);
    private static Slice<CompletionItem?> getJSDocParameterCompletions(ProtocolRequestContext context, SourceFile file, int position, Checker checker, CompilerOptions options, UserPreferences preferences, bool nameOnly)
    {
        var token = AstNav.GetTokenAtPosition(file, position);
        if (!AST.IsJSDocTag(token) && !token.IsJSDoc())
            return default;
        var doc = token.IsJSDoc() ? token : token.Parent!;
        if (!doc.IsJSDoc())
            return default;
        var function = doc.Parent!;
        if (!AST.IsFunctionLike(function))
            return default;
        bool js = AST.IsSourceFileJS(file), snippet = false; // Exact upstream TODO: snippet printer not wired here.
        int count = 0;
        var tags = doc.AsJSDoc().Tags?.Nodes ?? default;
        foreach (var tag in tags)
            if (AST.IsJSDocParameterTag(tag) && AstNav.GetStartOfNode(tag, file, false) < position && AST.IsIdentifier(tag.Name()))
                count++;
        int index = -1;
        EmitContext? emit = null;
        return Core.MapNonNilSlice(function.Parameters(), parameter =>
        {
            index++;
            if (index < count)
                return null;
            if (AST.IsIdentifier(parameter.Name()))
            {
                int tabstop = 1;
                var name = parameter.Name()!.TextValue();
                var display = getJSDocParamAnnotation(ref emit, name, parameter.Initializer(), parameter.AsParameterDeclaration().DotDotDotToken, js, false, false, checker, options, preferences, ref tabstop);
                ByteStringValue snippetText = default;
                if (snippet)
                    snippetText = getJSDocParamAnnotation(ref emit, name, parameter.Initializer(), parameter.AsParameterDeclaration().DotDotDotToken, js, false, true, checker, options, preferences, ref tabstop);
                if (nameOnly)
                {
                    display = display.Slice(1);
                    if (snippetText.Length != 0)
                        snippetText = snippetText.Slice(1);
                }

                return new CompletionItem
                {
                    Item = new Protocol.CompletionItem
                    {
                        Label = display,
                        Kind = CompletionItemKind.Variable,
                        SortText = CompletionSortText.LocationPriority,
                        InsertText = strPtrTo(snippetText),
                        InsertTextFormat = snippet ? Protocol.InsertTextFormat.Snippet : null
                    }
                };
            }

            if (index != count)
                return null;
            var path = ByteStringValue.FromString("param" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var displayResult = generateJSDocParamTagsForDestructuring(ref emit, path, parameter.Name()!, parameter.Initializer(), parameter.AsParameterDeclaration().DotDotDotToken, js, false, checker, options, preferences);
            ByteStringValue snippetResult = default;
            var separator = completionConcat(ByteStringValue.FromString(options.NewLine.GetNewLineCharacter()), ByteStringValue.FromString("* "));
            if (snippet)
                snippetResult = joinCompletionText(generateJSDocParamTagsForDestructuring(ref emit, path, parameter.Name()!, parameter.Initializer(), parameter.AsParameterDeclaration().DotDotDotToken, js, true, checker, options, preferences), separator);
            var displayText = joinCompletionText(displayResult, separator);
            if (nameOnly)
            {
                if (displayText.StartsWith(ByteStringValue.FromString("@")))
                    displayText = displayText.Slice(1);
                if (snippetResult.StartsWith(ByteStringValue.FromString("@")))
                    snippetResult = snippetResult.Slice(1);
            }

            return new CompletionItem
            {
                Item = new Protocol.CompletionItem
                {
                    Label = displayText,
                    Kind = CompletionItemKind.Variable,
                    SortText = CompletionSortText.LocationPriority,
                    InsertText = strPtrTo(snippetResult),
                    InsertTextFormat = snippet ? Protocol.InsertTextFormat.Snippet : null
                }
            };
        });
    }

    private static ByteStringValue getJSDocParamAnnotation(ref EmitContext? emit, ByteStringValue name, Node? initializer, Node? dots, bool js, bool obj, bool snippet, Checker checker, CompilerOptions options, UserPreferences preferences, ref int counter)
    {
        if (initializer != null)
            name = getJSDocParamNameWithInitializer(name, initializer);
        if (snippet)
            name = escapeSnippetText(name);
        ByteStringValue description = default;
        if (js)
        {
            var typeText = ByteStringValue.FromString("*");
            if (obj)
            {
                Debug.Assert(dots == null, "Cannot annotate a rest parameter with type 'object'.");
                typeText = ByteStringValue.FromString("object");
            }
            else
            {
                if (initializer != null)
                {
                    var inferred = checker.GetTypeAtLocation(initializer.Parent);
                    if ((inferred.Flags() & (TypeFlags.Any | TypeFlags.Void)) == 0)
                    {
                        var file = AST.GetSourceFileOfNode(initializer);
                        var flags = LsUtil.GetQuotePreference(file, preferences) == QuotePreference.Single ? BuilderFlags.UseSingleQuotesForStringLiteralType : BuilderFlags.None;
                        var node = checker.TypeToTypeNode(inferred, AST.FindAncestor(initializer, AST.IsFunctionLike), flags, null);
                        if (node != null)
                        {
                            emit ??= new EmitContext();
                            var printer = Printer.NewPrinter(new PrinterOptions { RemoveComments = true }, new PrintHandlers(), emit);
                            emit.SetEmitFlags(node, EF.SingleLine);
                            typeText = printer.EmitValue(node, file);
                        }
                    }
                }

                if (snippet && typeText.EqualsValue(ByteStringValue.FromString("*")))
                    typeText = completionConcat(ByteStringValue.FromString("${"), ByteStringValue.FromString((counter++).ToString(System.Globalization.CultureInfo.InvariantCulture)), ByteStringValue.FromString(":"), typeText, ByteStringValue.FromString("}"));
            }

            if (snippet)
                description = completionConcat(ByteStringValue.FromString("${"), ByteStringValue.FromString((counter++).ToString(System.Globalization.CultureInfo.InvariantCulture)), ByteStringValue.FromString("}"));
            return completionConcat(ByteStringValue.FromString("@param {"), ByteStringValue.FromString(!obj && dots != null ? "..." : ""), typeText, ByteStringValue.FromString("} "), name, ByteStringValue.FromString(" "), description);
        }

        if (snippet)
            description = completionConcat(ByteStringValue.FromString("${"), ByteStringValue.FromString((counter++).ToString(System.Globalization.CultureInfo.InvariantCulture)), ByteStringValue.FromString("}"));
        return completionConcat(ByteStringValue.FromString("@param "), name, ByteStringValue.FromString(" "), description);
    }

    private static ByteStringValue getJSDocParamNameWithInitializer(ByteStringValue name, Node initializer)
    {
        var text = GoStrings.TrimFunc(ScannerApi.GetTextOfNodeValue(initializer), GoUnicode.IsSpace);
        return text.Contains((int)'\n') || text.Length > 80 ? completionConcat(ByteStringValue.FromString("["), name, ByteStringValue.FromString("]")) : completionConcat(ByteStringValue.FromString("["), name, ByteStringValue.FromString("="), text, ByteStringValue.FromString("]"));
    }

    private static Slice<ByteStringValue> generateJSDocParamTagsForDestructuring(ref EmitContext? emit, ByteStringValue path, Node pattern, Node? initializer, Node? dots, bool js, bool snippet, Checker checker, CompilerOptions options, UserPreferences preferences)
    {
        int counter = 1;
        if (!js)
            return Slice<ByteStringValue>.FromArray([getJSDocParamAnnotation(ref emit, path, initializer, dots, js, false, snippet, checker, options, preferences, ref counter)]);
        return jsDocParamPatternWorker(ref emit, path, pattern, initializer, dots, js, snippet, checker, options, preferences, ref counter);
    }

    private static Slice<ByteStringValue> jsDocParamPatternWorker(ref EmitContext? emit, ByteStringValue path, Node pattern, Node? initializer, Node? dots, bool js, bool snippet, Checker checker, CompilerOptions options, UserPreferences preferences, ref int counter)
    {
        if (AST.IsObjectBindingPattern(pattern) && dots == null)
        {
            int childCounter = counter;
            var root = getJSDocParamAnnotation(ref emit, path, initializer, dots, js, true, snippet, checker, options, preferences, ref childCounter);
            Slice<ByteStringValue> children = default;
            foreach (var element in pattern.Elements())
            {
                var tags = jsDocParamElementWorker(ref emit, path, element, initializer, dots, js, snippet, checker, options, preferences, ref childCounter);
                if (tags.Length == 0)
                {
                    children = default;
                    break;
                }

                children = children.Append(tags);
            }

            if (children.Length > 0)
            {
                counter = childCounter;
                return Slice<ByteStringValue>.FromArray([root]).Append(children);
            }
        }

        return Slice<ByteStringValue>.FromArray([getJSDocParamAnnotation(ref emit, path, initializer, dots, js, false, snippet, checker, options, preferences, ref counter)]);
    }

    private static Slice<ByteStringValue> jsDocParamElementWorker(ref EmitContext? emit, ByteStringValue path, Node element, Node? initializer, Node? dots, bool js, bool snippet, Checker checker, CompilerOptions options, UserPreferences preferences, ref int counter)
    {
        if (AST.IsIdentifier(element.Name()))
        {
            ByteStringValue name;
            if (element.PropertyName() != null)
                (name, _) = AST.TryGetTextValueOfPropertyName(element.PropertyName());
            else
                name = element.Name()!.TextValue();
            if (name.Length == 0)
                return default;
            return Slice<ByteStringValue>.FromArray([getJSDocParamAnnotation(ref emit, completionConcat(path, ByteStringValue.FromString("."), name), element.Initializer(), element.AsBindingElement().DotDotDotToken, js, false, snippet, checker, options, preferences, ref counter)]);
        }

        if (element.PropertyName() != null)
        {
            var(name, _) = AST.TryGetTextValueOfPropertyName(element.PropertyName());
            if (name.Length == 0)
                return default;
            return jsDocParamPatternWorker(ref emit, completionConcat(path, ByteStringValue.FromString("."), name), element.Name()!, element.Initializer(), element.AsBindingElement().DotDotDotToken, js, snippet, checker, options, preferences, ref counter);
        }

        return default;
    }

    private static Slice<CompletionItem?> getJSDocParameterNameCompletions(Node tag)
    {
        if (!AST.IsIdentifier(tag.Name()))
            return default;
        var prefix = tag.Name()!.TextValue();
        var doc = tag.Parent!;
        var function = doc.Parent!;
        if (!AST.IsFunctionLike(function))
            return default;
        var tags = doc.AsJSDoc().Tags?.Nodes ?? default;
        return Core.MapNonNilSlice(function.Parameters(), parameter =>
        {
            if (!AST.IsIdentifier(parameter.Name()))
                return null;
            var name = parameter.Name()!.TextValue();
            if (Core.Some(tags, candidate => candidate != tag && AST.IsJSDocParameterTag(candidate) && AST.IsIdentifier(candidate.Name()) && candidate.Name()!.TextValue().EqualsValue(name)) || prefix.Length != 0 && !name.StartsWith(prefix))
                return null;
            return new CompletionItem
            {
                Item = new Protocol.CompletionItem
                {
                    Label = name,
                    Kind = CompletionItemKind.Variable,
                    SortText = CompletionSortText.LocationPriority
                }
            };
        });
    }
}

// Source assembly component: CompletionImportStatement.cs
internal sealed partial class LanguageService
{
    private ImportStatementCompletionInfo getImportStatementCompletionInfo(Node token, SourceFile file)
    {
        var result = new ImportStatementCompletionInfo();
        Node? candidate = null;
        var parent = token.Parent!;
        if (AST.IsImportEqualsDeclaration(parent))
        {
            var last = LsUtil.GetLastToken(parent, file);
            if (token.Kind == Kind.Identifier && last != token)
            {
                result.keywordCompletion = Kind.FromKeyword;
                result.isKeywordOnlyCompletion = true;
            }
            else
            {
                if (token.Kind != Kind.TypeKeyword)
                    result.keywordCompletion = Kind.TypeKeyword;
                if (isModuleSpecifierMissingOrEmpty(parent.AsImportEqualsDeclaration().ModuleReference))
                    candidate = parent;
            }
        }
        else if (couldBeTypeOnlyImportSpecifier(parent, token) && canCompleteFromNamedBindings(parent.Parent!))
            candidate = parent;
        else if (AST.IsNamedImports(parent) || AST.IsNamespaceImport(parent))
        {
            if (!parent.Parent!.IsTypeOnly() && token.Kind is Kind.OpenBraceToken or Kind.ImportKeyword or Kind.CommaToken)
                result.keywordCompletion = Kind.TypeKeyword;
            if (canCompleteFromNamedBindings(parent))
            {
                if (token.Kind is Kind.CloseBraceToken or Kind.Identifier)
                {
                    result.isKeywordOnlyCompletion = true;
                    result.keywordCompletion = Kind.FromKeyword;
                }
                else
                    candidate = parent.Parent!.Parent;
            }
        }
        else if (AST.IsExportDeclaration(parent) && token.Kind == Kind.AsteriskToken || AST.IsNamedExports(parent) && token.Kind == Kind.CloseBraceToken)
        {
            result.isKeywordOnlyCompletion = true;
            result.keywordCompletion = Kind.FromKeyword;
        }
        else if (token.Kind == Kind.ImportKeyword)
        {
            if (AST.IsSourceFile(parent))
            {
                result.keywordCompletion = Kind.TypeKeyword;
                candidate = token;
            }
            else if (AST.IsImportDeclaration(parent))
            {
                result.keywordCompletion = Kind.TypeKeyword;
                if (isModuleSpecifierMissingOrEmpty(parent.ModuleSpecifier()))
                    candidate = parent;
            }
        }

        if (candidate != null)
        {
            result.isNewIdentifierLocation = true;
            result.replacementSpan = getSingleLineReplacementSpanForImportCompletionNode(candidate);
            result.couldBeTypeOnlyImportSpecifier = couldBeTypeOnlyImportSpecifier(candidate, token);
            if (AST.IsImportDeclaration(candidate))
            {
                var clause = candidate.ImportClause();
                if (clause != null)
                    result.isTopLevelTypeOnly = clause.IsTypeOnly();
            }
            else if (candidate.Kind == Kind.ImportEqualsDeclaration)
                result.isTopLevelTypeOnly = candidate.IsTypeOnly();
        }
        else
            result.isNewIdentifierLocation = result.keywordCompletion == Kind.TypeKeyword;
        return result;
    }

    private TypeScript.Lsp.Protocol.Range? getSingleLineReplacementSpanForImportCompletionNode(Node node)
    {
        var ancestor = AST.FindAncestor(node, static candidate => AST.IsImportDeclaration(candidate) || AST.IsImportEqualsDeclaration(candidate) || AST.IsJSDocImportTag(candidate));
        if (ancestor != null)
            node = ancestor;
        var file = AST.GetSourceFileOfNode(node);
        int start = ScannerApi.GetTokenPosOfNode(node, file, false);
        if (PrinterUtilities.GetLinesBetweenPositions(file, start, node.End()) == 0)
        {
            var(range, fidelity) = createLspRangeFromNode(node, file);
            return fidelity.IsExact() ? range : null;
        }

        if (node.Kind is Kind.ImportKeyword or Kind.ImportSpecifier)
            throw new InvalidOperationException("ImportKeyword was necessarily on one line; ImportSpecifier was necessarily parented in an ImportDeclaration");
        Node? split;
        if (node.Kind is Kind.ImportDeclaration or Kind.JSDocImportTag)
        {
            var clause = node.ImportClause();
            var specifier = clause == null ? null : getPotentiallyInvalidImportSpecifier(clause.AsImportClause().NamedBindings);
            split = specifier ?? node.ModuleSpecifier();
        }
        else
            split = node.AsImportEqualsDeclaration().ModuleReference;
        int withoutStart = ScannerApi.GetTokenPosOfNode(LsUtil.GetFirstToken(node, file), file, false), withoutEnd = split!.Pos();
        if (PrinterUtilities.GetLinesBetweenPositions(file, withoutStart, withoutEnd) == 0)
        {
            var(range, fidelity) = createLspRangeFromBounds(withoutStart, withoutEnd, file);
            return fidelity.IsExact() ? range : null;
        }

        return null;
    }

    private static bool couldBeTypeOnlyImportSpecifier(Node specifier, Node token) => AST.IsImportSpecifier(specifier) && (specifier.IsTypeOnly() || token == specifier.Name() && isTypeKeywordTokenOrIdentifier(token));
    private static bool canCompleteFromNamedBindings(Node bindings)
    {
        if (!isModuleSpecifierMissingOrEmpty(bindings.Parent!.Parent!.ModuleSpecifier()) || bindings.Parent.Name() != null)
            return false;
        if (AST.IsNamedImports(bindings))
        {
            var invalid = getPotentiallyInvalidImportSpecifier(bindings);
            var elements = bindings.Elements();
            int valid = elements.Length;
            if (invalid != null)
                valid = GoSlices.Index(elements, invalid);
            return valid < 2 && valid > -1;
        }

        return true;
    }

    private static Node? getPotentiallyInvalidImportSpecifier(Node? bindings)
    {
        if (bindings == null || bindings.Kind != Kind.NamedImports)
            return null;
        return Core.Find(bindings.Elements(), element => element.PropertyName() == null && LsUtil.IsNonContextualKeyword(ScannerApi.StringToToken(element.Name()!.TextValue())) && AstNav.FindPrecedingToken(AST.GetSourceFileOfNode(bindings), element.Name()!.Pos())!.Kind != Kind.CommaToken);
    }

    private static bool isModuleSpecifierMissingOrEmpty(Node? specifier)
    {
        if (AST.NodeIsMissing(specifier))
            return true;
        var node = AST.IsExternalModuleReference(specifier) ? specifier!.Expression() : specifier;
        return !AST.IsStringLiteralLike(node) || node!.TextValue().Length == 0;
    }
}

// Source assembly component: CompletionData.cs
internal enum KeywordCompletionFilters
{
    None,
    All,
    ClassElementKeywords,
    InterfaceElementKeywords,
    ConstructorParameterKeywords,
    FunctionLikeBodyKeywords,
    TypeAssertionKeywords,
    TypeKeywords,
    TypeKeyword,
    Last = TypeKeyword,
}

internal enum CompletionKind
{
    None,
    ObjectPropertyDeclaration,
    Global,
    PropertyAccess,
    MemberLike,
    String
}

[Flags]
internal enum SymbolOriginInfoKind
{
    ThisType = 1,
    SymbolMember = 2,
    Promise = 4,
    Nullable = 8,
    TypeOnlyAlias = 16,
    ObjectLiteralMethod = 32,
    Ignore = 64,
    ComputedPropertyName = 128,
}

internal abstract class CompletionData;
internal sealed class CompletionDataData : CompletionData
{
    internal Slice<Symbol> symbols;
    internal Slice<FixAndExport> autoImports;
    internal CompletionKind completionKind;
    internal bool isInSnippetScope;
    internal Node? propertyAccessToConvert;
    internal bool isNewIdentifierLocation;
    internal Node location = null!;
    internal KeywordCompletionFilters keywordFilters;
    internal Slice<object> literals;
    internal Dictionary<int, SymbolOriginInfo>? symbolToOriginInfoMap;
    internal Dictionary<ulong, ByteStringValue>? symbolToSortTextMap;
    internal Symbol? recommendedCompletion;
    internal Node? previousToken, contextToken;
    internal JsxInitializer jsxInitializer;
    internal bool insideJSDocTagTypeExpression, isTypeOnlyLocation, isJsxIdentifierExpected, isRightOfOpenTag, isRightOfDotOrQuestionDot;
    internal ImportStatementCompletionInfo? importStatementCompletion;
    internal bool hasUnresolvedAutoImports;
    internal Slice<ByteStringValue> defaultCommitCharacters;
}

internal sealed class CompletionDataKeyword : CompletionData
{
    internal Slice<CompletionItem?> keywordCompletions;
    internal bool isNewIdentifierLocation;
}

internal sealed class CompletionDataJSDocTagName : CompletionData;
internal sealed class CompletionDataJSDocTag : CompletionData;
internal sealed class CompletionDataJSDocParameterName : CompletionData
{
    internal Node tag = null!;
}

internal sealed class ImportStatementCompletionInfo
{
    internal bool isKeywordOnlyCompletion;
    internal Kind keywordCompletion;
    internal bool isNewIdentifierLocation, isTopLevelTypeOnly, couldBeTypeOnlyImportSpecifier;
    internal Protocol.Range? replacementSpan;
}

internal struct JsxInitializer
{
    internal bool isInitializer;
    internal Node? initializer;
}

internal sealed class SymbolOriginInfo
{
    internal SymbolOriginInfoKind kind;
    internal bool isDefaultExport, isFromPackageJson;
    internal object? data;
    internal ByteStringValue symbolName() => data is SymbolOriginInfoComputedPropertyName computed ? computed.symbolName : throw new InvalidOperationException("symbolOriginInfo: unknown data type for symbolName(): " + GoCompletionTypeName(data));
    internal SymbolOriginInfoObjectLiteralMethod asObjectLiteralMethod() => (SymbolOriginInfoObjectLiteralMethod)data!;
    private static string GoCompletionTypeName(object? data) => data switch
    {
        null => "<nil>",
        SymbolOriginInfoObjectLiteralMethod => "*ls.symbolOriginInfoObjectLiteralMethod",
        SymbolOriginInfoTypeOnlyAlias => "*ls.symbolOriginInfoTypeOnlyAlias",
        SymbolOriginInfoComputedPropertyName => "*ls.symbolOriginInfoComputedPropertyName",
        _ => throw new InvalidOperationException("Unexpected completion origin owner"),
    };
}

internal sealed class SymbolOriginInfoObjectLiteralMethod
{
    internal ByteStringValue insertText;
    internal Protocol.CompletionItemLabelDetails? labelDetails;
    internal bool isSnippet;
}

internal sealed class SymbolOriginInfoTypeOnlyAlias
{
    internal Node declaration = null!;
}

internal sealed class SymbolOriginInfoComputedPropertyName
{
    internal ByteStringValue symbolName;
}

internal enum GlobalsSearch
{
    Continue,
    Success,
    Fail
}

internal static class CompletionSortText
{
    internal static readonly ByteStringValue LocalDeclarationPriority = ByteStringValue.FromString("10"), LocationPriority = ByteStringValue.FromString("11"), OptionalMember = ByteStringValue.FromString("12"), MemberDeclaredBySpreadAssignment = ByteStringValue.FromString("13"), SuggestedClassMembers = ByteStringValue.FromString("14"), GlobalsOrKeywords = ByteStringValue.FromString("15"), AutoImportSuggestions = ByteStringValue.FromString("16"), ClassMemberSnippets = ByteStringValue.FromString("17"), JavascriptIdentifiers = ByteStringValue.FromString("18");
    internal static ByteStringValue DeprecateSortText(ByteStringValue original) => ByteStringValue.Concat(ByteStringValue.FromString("z"), original);
    internal static ByteStringValue ObjectLiteralPropertySortText(ByteStringValue preset, ByteStringValue symbolName)
    {
        var b = new GoBuilder();
        b.WriteString(preset);
        b.WriteByte(0);
        b.WriteString(symbolName);
        b.WriteByte(0);
        return b.Value();
    }

    internal static ByteStringValue SortBelow(ByteStringValue original) => ByteStringValue.Concat(original, ByteStringValue.FromString("1"));
}

internal static class CompletionSource
{
    internal static readonly ByteStringValue ThisProperty = ByteStringValue.FromString("ThisProperty/"), ClassMemberSnippet = ByteStringValue.FromString("ClassMemberSnippet/"), TypeOnlyAlias = ByteStringValue.FromString("TypeOnlyAlias/"), ObjectLiteralMethodSnippet = ByteStringValue.FromString("ObjectLiteralMethodSnippet/"), SwitchCases = ByteStringValue.FromString("SwitchCases/"), ObjectLiteralMemberWithComma = ByteStringValue.FromString("ObjectLiteralMemberWithComma/");
}

// Source assembly component: CompletionKeywords.cs
internal sealed partial class LanguageService
{
    private static ByteStringValue generateIdentifierForArbitraryString(ByteStringValue text)
    {
        bool underscore = false;
        var builder = new GoBuilder();
        for (int pos = 0, size = 0; pos < text.Length; pos += size)
        {
            var(rune, runeSize) = completionDecodeRune(text.Slice(pos));
            size = runeSize;
            bool valid = pos == 0 ? ScannerApi.IsIdentifierStart(rune) : ScannerApi.IsIdentifierPart(rune);
            if (size > 0 && valid)
            {
                if (underscore)
                    builder.WriteRune('_');
                builder.WriteRune(rune);
                underscore = false;
            }
            else
                underscore = true;
        }

        if (underscore)
            builder.WriteRune('_');
        var result = builder.Value();
        return result.Length == 0 ? ByteStringValue.FromString("_") : result;
    }

    private static CompletionItemKind getCompletionsSymbolKind(ScriptElementKind kind)
    {
        if (kind == ScriptElementKind.PrimitiveType || kind == ScriptElementKind.Keyword)
            return CompletionItemKind.Keyword;
        if (kind == ScriptElementKind.ConstElement || kind == ScriptElementKind.LetElement || kind == ScriptElementKind.VariableElement || kind == ScriptElementKind.LocalVariableElement || kind == ScriptElementKind.Alias || kind == ScriptElementKind.ParameterElement)
            return CompletionItemKind.Variable;
        if (kind == ScriptElementKind.MemberVariableElement || kind == ScriptElementKind.MemberGetAccessorElement || kind == ScriptElementKind.MemberSetAccessorElement)
            return CompletionItemKind.Field;
        if (kind == ScriptElementKind.FunctionElement || kind == ScriptElementKind.LocalFunctionElement)
            return CompletionItemKind.Function;
        if (kind == ScriptElementKind.MemberFunctionElement || kind == ScriptElementKind.ConstructSignatureElement || kind == ScriptElementKind.CallSignatureElement || kind == ScriptElementKind.IndexSignatureElement)
            return CompletionItemKind.Method;
        if (kind == ScriptElementKind.EnumElement)
            return CompletionItemKind.Enum;
        if (kind == ScriptElementKind.EnumMemberElement)
            return CompletionItemKind.EnumMember;
        if (kind == ScriptElementKind.ModuleElement || kind == ScriptElementKind.ExternalModuleName)
            return CompletionItemKind.Module;
        if (kind == ScriptElementKind.ClassElement || kind == ScriptElementKind.TypeElement)
            return CompletionItemKind.Class;
        if (kind == ScriptElementKind.InterfaceElement)
            return CompletionItemKind.Interface;
        if (kind == ScriptElementKind.Warning)
            return CompletionItemKind.Text;
        if (kind == ScriptElementKind.ScriptElement)
            return CompletionItemKind.File;
        if (kind == ScriptElementKind.Directory)
            return CompletionItemKind.Folder;
        if (kind == ScriptElementKind.String)
            return CompletionItemKind.Constant;
        return CompletionItemKind.Property;
    }

    internal static int CompareCompletionEntries(Protocol.CompletionItem left, Protocol.CompletionItem right)
    {
        int result = StringUtil.CompareStringsCaseInsensitiveThenSensitive(left.SortText!.Value, right.SortText!.Value);
        return result == 0 ? StringUtil.CompareStringsCaseInsensitiveThenSensitive(left.Label, right.Label) : result;
    }

    private static readonly TypeScript.Collections.SyncMap<KeywordCompletionFilters, Slice<Protocol.CompletionItem?>> keywordCompletionsCache = new();
    private static readonly Lazy<Slice<Protocol.CompletionItem?>> allKeywordCompletions = new(static () =>
    {
        var result = Slice<Protocol.CompletionItem?>.Make(0, (int)Kind.LastKeyword - (int)Kind.FirstKeyword + 1);
        for (var kind = Kind.FirstKeyword; kind <= Kind.LastKeyword; kind++)
            result = result.Append(new Protocol.CompletionItem { Label = ByteStringValue.FromString(ScannerApi.TokenToString(kind)), Kind = CompletionItemKind.Keyword, SortText = CompletionSortText.GlobalsOrKeywords });
        return result;
    });
    private static Protocol.CompletionItem cloneCompletionProtocolItem(Protocol.CompletionItem item) => new()
    {
        Label = item.Label,
        LabelDetails = item.LabelDetails,
        Kind = item.Kind,
        Tags = item.Tags,
        Detail = item.Detail,
        Documentation = item.Documentation,
        Deprecated = item.Deprecated,
        Preselect = item.Preselect,
        SortText = item.SortText,
        FilterText = item.FilterText,
        InsertText = item.InsertText,
        InsertTextFormat = item.InsertTextFormat,
        InsertTextMode = item.InsertTextMode,
        TextEdit = item.TextEdit,
        TextEditText = item.TextEditText,
        AdditionalTextEdits = item.AdditionalTextEdits,
        CommitCharacters = item.CommitCharacters,
        Command = item.Command,
        Data = item.Data,
    };
    private static Slice<CompletionItem?> cloneItems(Slice<Protocol.CompletionItem?> items)
    {
        if (items.IsNil)
            return default;
        var entries = Slice<CompletionItem?>.Make(items.Length);
        for (int i = 0; i < items.Length; i++)
            entries[i] = new CompletionItem
            {
                Item = cloneCompletionProtocolItem(items[i]!)
            };
        return entries;
    }

    private static Slice<CompletionItem?> getKeywordCompletions(KeywordCompletionFilters filter, bool excludeTS)
    {
        if (!excludeTS)
            return cloneItems(getTypescriptKeywordCompletions(filter));
        var index = (KeywordCompletionFilters)((int)filter + (int)KeywordCompletionFilters.Last + 1);
        var(cached, loaded) = keywordCompletionsCache.Load(index);
        if (loaded)
            return cloneItems(cached);
        var result = Core.Filter(getTypescriptKeywordCompletions(filter), item => !isTypeScriptOnlyKeyword(ScannerApi.StringToToken(item!.Label)));
        keywordCompletionsCache.Store(index, result);
        return cloneItems(result);
    }

    private static Slice<Protocol.CompletionItem?> getTypescriptKeywordCompletions(KeywordCompletionFilters filter)
    {
        var(cached, loaded) = keywordCompletionsCache.Load(filter);
        if (loaded)
            return cached;
        var result = Core.Filter(allKeywordCompletions.Value, item =>
        {
            var kind = ScannerApi.StringToToken(item!.Label);
            return filter switch
            {
                KeywordCompletionFilters.None => false,
                KeywordCompletionFilters.All => isFunctionLikeBodyKeyword(kind) || kind is Kind.DeclareKeyword or Kind.ModuleKeyword or Kind.TypeKeyword or Kind.NamespaceKeyword or Kind.AbstractKeyword || isTypeKeyword(kind) && kind != Kind.UndefinedKeyword,
                KeywordCompletionFilters.FunctionLikeBodyKeywords => isFunctionLikeBodyKeyword(kind),
                KeywordCompletionFilters.ClassElementKeywords => isClassMemberCompletionKeyword(kind),
                KeywordCompletionFilters.InterfaceElementKeywords => isInterfaceOrTypeLiteralCompletionKeyword(kind),
                KeywordCompletionFilters.ConstructorParameterKeywords => AST.IsParameterPropertyModifier(kind),
                KeywordCompletionFilters.TypeAssertionKeywords => isTypeKeyword(kind) || kind == Kind.ConstKeyword,
                KeywordCompletionFilters.TypeKeywords => isTypeKeyword(kind),
                KeywordCompletionFilters.TypeKeyword => kind == Kind.TypeKeyword,
                _ => throw new InvalidOperationException("Unknown keyword filter: " + (int)filter),
            };
        });
        keywordCompletionsCache.Store(filter, result);
        return result;
    }

    private static bool isTypeScriptOnlyKeyword(Kind kind) => kind is Kind.AbstractKeyword or Kind.AnyKeyword or Kind.BigIntKeyword or Kind.BooleanKeyword or Kind.DeclareKeyword or Kind.EnumKeyword or Kind.GlobalKeyword or Kind.ImplementsKeyword or Kind.InferKeyword or Kind.InterfaceKeyword or Kind.IsKeyword or Kind.KeyOfKeyword or Kind.ModuleKeyword or Kind.NamespaceKeyword or Kind.NeverKeyword or Kind.NumberKeyword or Kind.ObjectKeyword or Kind.OverrideKeyword or Kind.PrivateKeyword or Kind.ProtectedKeyword or Kind.PublicKeyword or Kind.ReadonlyKeyword or Kind.StringKeyword or Kind.SymbolKeyword or Kind.TypeKeyword or Kind.UniqueKeyword or Kind.UnknownKeyword;
    private static bool isFunctionLikeBodyKeyword(Kind kind) => kind is Kind.AsyncKeyword or Kind.AwaitKeyword or Kind.UsingKeyword or Kind.AsKeyword or Kind.SatisfiesKeyword or Kind.TypeKeyword || !AST.IsContextualKeyword(kind) && !isClassMemberCompletionKeyword(kind);
    private static bool isClassMemberCompletionKeyword(Kind kind) => kind is Kind.AbstractKeyword or Kind.AccessorKeyword or Kind.ConstructorKeyword or Kind.GetKeyword or Kind.SetKeyword or Kind.AsyncKeyword or Kind.DeclareKeyword or Kind.OverrideKeyword || AST.IsClassMemberModifier(kind);
    private static bool isInterfaceOrTypeLiteralCompletionKeyword(Kind kind) => kind == Kind.ReadonlyKeyword;
    private static bool isContextualKeywordInAutoImportableExpressionSpace(ByteStringValue keyword) => keyword.EqualsValue(ByteStringValue.FromString("abstract")) || keyword.EqualsValue(ByteStringValue.FromString("async")) || keyword.EqualsValue(ByteStringValue.FromString("await")) || keyword.EqualsValue(ByteStringValue.FromString("declare")) || keyword.EqualsValue(ByteStringValue.FromString("module")) || keyword.EqualsValue(ByteStringValue.FromString("namespace")) || keyword.EqualsValue(ByteStringValue.FromString("type")) || keyword.EqualsValue(ByteStringValue.FromString("satisfies")) || keyword.EqualsValue(ByteStringValue.FromString("as"));
    private static Slice<Protocol.CompletionItem?> getContextualKeywords(SourceFile file, Node? token, int position)
    {
        Slice<Protocol.CompletionItem?> entries = default;
        if (token != null)
        {
            var parent = token.Parent!;
            int tokenLine = ScannerApi.GetECMALineOfPosition(file, token.End()), currentLine = ScannerApi.GetECMALineOfPosition(file, position);
            if ((AST.IsImportDeclaration(parent) || AST.IsExportDeclaration(parent) && parent.ModuleSpecifier() != null) && token == parent.ModuleSpecifier() && tokenLine == currentLine)
                entries = entries.Append(new Protocol.CompletionItem { Label = ByteStringValue.FromString(ScannerApi.TokenToString(Kind.AssertKeyword)), Kind = CompletionItemKind.Keyword, SortText = CompletionSortText.GlobalsOrKeywords });
        }

        return entries;
    }

    private Slice<CompletionItem?> getJSCompletionEntries(ProtocolRequestContext context, SourceFile file, int position, ref HashSet<ByteStringKey>? names, Slice<CompletionItem?> entries)
    {
        foreach (var(nameKey, pos)in file.GetNameTable())
        {
            var name = nameKey.Value;
            if (pos == position)
                continue;
            if (names?.Contains(ByteStringKey.FromValue(name)) != true && ScannerApi.IsIdentifierTextByteString(name, LanguageVariant.Standard))
            {
                (names ??= new()).Add(ByteStringKey.FromValue(name));
                entries = entries.Append(new CompletionItem { Item = new Protocol.CompletionItem { Label = name, Kind = CompletionItemKind.Text, SortText = CompletionSortText.JavascriptIdentifiers, CommitCharacters = Slice<ByteStringValue>.Make(0) } });
            }
        }

        return entries;
    }

    private TypeScript.Lsp.Protocol.Range? getOptionalReplacementSpan(Node? location, SourceFile file)
    {
        if (location?.Kind is Kind.Identifier or Kind.PrivateIdentifier)
        {
            var(range, fidelity) = createLspRangeFromBounds(AstNav.GetStartOfNode(location, file, false), location.End(), file);
            if (fidelity.IsExact())
                return range;
        }

        return null;
    }

    private static bool isMemberCompletionKind(CompletionKind kind) => kind is CompletionKind.ObjectPropertyDeclaration or CompletionKind.MemberLike or CompletionKind.PropertyAccess;
    private static Node? tryGetFunctionLikeBodyCompletionContainer(Node? token)
    {
        if (token == null)
            return null;
        Node? previous = null;
        return AST.FindAncestorOrQuit(token, node =>
        {
            if (AST.IsClassLike(node))
                return FindAncestorResult.Quit;
            if (AST.IsFunctionLikeDeclaration(node) && previous == node.Body())
                return FindAncestorResult.True;
            previous = node;
            return FindAncestorResult.False;
        });
    }
}

// Source assembly component: CompletionSelection.cs
internal sealed partial class LanguageService
{
    private (CompletionData? Data, Exception? Error) getCompletionData(ProtocolRequestContext context, Checker checker, SourceFile file, int position, UserPreferences preferences, bool forItemResolve)
    {
        bool inCheckedFile = isCheckedFile(file, GetProgram().Options());
        var currentToken = AstNav.GetTokenAtPosition(file, position);
        var insideComment = isInComment(file, position, currentToken);
        bool insideJSDocTagTypeExpression = false, insideJsDocImportTag = false, isInSnippetScope = false;
        if (insideComment != null)
        {
            if (hasDocComment(file, position))
            {
                if (position > 0 && file.TextView()[position - 1] == '@')
                    return (new CompletionDataJSDocTagName(), null);
                int lineStart = Format.GetLineStartPositionForPosition(position, file);
                bool noCommentPrefix = true;
                var prefix = ByteStringValue.FromView(file.TextView()).Slice(lineStart, position);
                for (int pos = 0; pos < prefix.Length;)
                {
                    var(rune, size) = completionDecodeRune(prefix.Slice(pos));
                    if (!(StringUtil.IsWhiteSpaceSingleLine(rune) || rune is '*' or '/' or '(' or ')' or '|'))
                    {
                        noCommentPrefix = false;
                        break;
                    }

                    pos += size;
                }

                if (noCommentPrefix)
                    return (new CompletionDataJSDocTag(), null);
            }

            var tag = getJSDocTagAtPosition(currentToken, position);
            if (tag != null)
            {
                if (tag.TagName().Pos() <= position && position <= tag.TagName().End())
                    return (new CompletionDataJSDocTagName(), null);
                if (Ast.IsJSDocImportTag(tag))
                    insideJsDocImportTag = true;
                else
                {
                    var typeExpression = tryGetTypeExpressionFromTag(tag);
                    if (typeExpression != null)
                    {
                        currentToken = AstNav.GetTokenAtPosition(file, position);
                        if (currentToken == null || !Ast.IsDeclarationName(currentToken) && (currentToken!.Parent.Kind != Kind.JSDocPropertyTag || currentToken!.Parent.Name() != currentToken))
                            insideJSDocTagTypeExpression = isCurrentlyEditingNode(typeExpression, file, position);
                    }

                    if (!insideJSDocTagTypeExpression && Ast.IsJSDocParameterTag(tag) && (Ast.NodeIsMissing(tag.Name()) || tag.Name().Pos() <= position && position <= tag.Name().End()))
                        return (new CompletionDataJSDocParameterName { tag = tag }, null);
                }
            }

            if (!insideJSDocTagTypeExpression && !insideJsDocImportTag)
                return (null, null);
        }

        bool isJSOnlyLocation = !insideJSDocTagTypeExpression && !insideJsDocImportTag && Ast.IsSourceFileJS(file);
        var(contextToken, previousToken) = getRelevantTokens(position, file);
        var node = currentToken;
        Node? propertyAccessToConvert = null;
        bool isRightOfDot = false, isRightOfQuestionDot = false, isRightOfOpenTag = false, isStartingCloseTag = false;
        JsxInitializer jsxInitializer = default;
        bool isJsxIdentifierExpected = false;
        ImportStatementCompletionInfo? importStatementCompletion = null;
        var location = AstNav.GetTouchingPropertyName(file, position);
        var keywordFilters = KeywordCompletionFilters.None;
        bool isNewIdentifierLocation = false;
        Slice<ByteStringValue> defaultCommitCharacters = default;
        if (contextToken != null)
        {
            var importInfo = getImportStatementCompletionInfo(contextToken, file);
            if (importInfo.keywordCompletion != Kind.Unknown)
            {
                if (importInfo.isKeywordOnlyCompletion)
                    return (new CompletionDataKeyword { keywordCompletions = Slice<CompletionItem?>.FromArray([new CompletionItem { Item = new Protocol.CompletionItem { Label = ByteStringValue.FromString(ScannerApi.TokenToString(importInfo.keywordCompletion)), Kind = CompletionItemKind.Keyword, SortText = CompletionSortText.GlobalsOrKeywords } }]), isNewIdentifierLocation = importInfo.isNewIdentifierLocation, }, null);
                keywordFilters = keywordFiltersFromSyntaxKind(importInfo.keywordCompletion);
            }

            if (importInfo.replacementSpan != null && preferences.IncludeCompletionsForImportStatements == Tristate.True)
            {
                importStatementCompletion = importInfo;
                isNewIdentifierLocation = importInfo.isNewIdentifierLocation;
            }

            if (importInfo.replacementSpan == null && isCompletionListBlocker(contextToken, previousToken, location, file, position, checker))
            {
                if (keywordFilters != KeywordCompletionFilters.None)
                {
                    var(isNew, _) = computeCommitCharactersAndIsNewIdentifier(contextToken, file, position);
                    return (keywordCompletionData(keywordFilters, isJSOnlyLocation, isNew), null);
                }

                return (null, null);
            }

            var parent = contextToken!.Parent;
            if (contextToken!.Kind is Kind.DotToken or Kind.QuestionDotToken)
            {
                isRightOfDot = contextToken!.Kind == Kind.DotToken;
                isRightOfQuestionDot = contextToken!.Kind == Kind.QuestionDotToken;
                switch (parent!.Kind)
                {
                    case Kind.PropertyAccessExpression:
                        propertyAccessToConvert = parent;
                        node = parent.Expression();
                        var leftmost = Ast.GetLeftmostAccessExpression(parent);
                        if (Ast.NodeIsMissing(leftmost) || (Ast.IsCallExpression(node) || Ast.IsFunctionLike(node)) && node.End() == contextToken!.Pos() && LsUtil.GetLastChild(node, file).Kind != Kind.CloseParenToken)
                            return (null, null);
                        break;
                    case Kind.QualifiedName:
                        node = parent!.AsQualifiedName().Left;
                        break;
                    case Kind.ModuleDeclaration:
                        node = parent.Name();
                        break;
                    case Kind.ImportType:
                        node = parent;
                        break;
                    case Kind.MetaProperty:
                        node = LsUtil.GetFirstToken(parent!, file);
                        if (node!.Kind is not (Kind.ImportKeyword or Kind.NewKeyword))
                            throw new InvalidOperationException("Unexpected token kind: " + node!.Kind.String());
                        break;
                    default:
                        return (null, null);
                }
            }
            else if (importStatementCompletion == null)
            {
                if (parent != null && parent!.Kind == Kind.PropertyAccessExpression)
                {
                    contextToken = parent;
                    parent = parent!.Parent;
                }

                if (parent == location)
                {
                    switch (currentToken!.Kind)
                    {
                        case Kind.GreaterThanToken:
                            if (parent!.Kind is Kind.JsxElement or Kind.JsxOpeningElement)
                                location = currentToken;
                            break;
                        case Kind.LessThanSlashToken:
                            if (parent!.Kind == Kind.JsxSelfClosingElement)
                                location = currentToken;
                            break;
                    }
                }

                switch (parent!.Kind)
                {
                    case Kind.JsxClosingElement:
                        if (contextToken!.Kind == Kind.LessThanSlashToken)
                        {
                            isStartingCloseTag = true;
                            location = contextToken;
                        }

                        break;
                    case Kind.BinaryExpression:
                        if (!binaryExpressionMayBeOpenTag(parent!.AsBinaryExpression()))
                            break;
                        goto case Kind.JsxSelfClosingElement;
                    case Kind.JsxSelfClosingElement:
                    case Kind.JsxElement:
                    case Kind.JsxOpeningElement:
                        isJsxIdentifierExpected = true;
                        if (contextToken!.Kind == Kind.LessThanToken)
                        {
                            isRightOfOpenTag = true;
                            location = contextToken;
                        }

                        break;
                    case Kind.JsxExpression:
                    case Kind.JsxSpreadAttribute:
                        if (previousToken!.Kind == Kind.CloseBraceToken || previousToken!.Kind == Kind.Identifier && previousToken!.Parent!.Kind == Kind.JsxAttribute)
                            isJsxIdentifierExpected = true;
                        break;
                    case Kind.JsxAttribute:
                        if (parent.Initializer() == previousToken && previousToken!.End() < position)
                            isJsxIdentifierExpected = true;
                        else
                            switch (previousToken!.Kind)
                            {
                                case Kind.EqualsToken:
                                    jsxInitializer.isInitializer = true;
                                    break;
                                case Kind.Identifier:
                                    isJsxIdentifierExpected = true;
                                    if (parent != previousToken.Parent && parent.Initializer() == null && AstNav.FindChildOfKind(parent, Kind.EqualsToken, file) != null)
                                        jsxInitializer.initializer = previousToken;
                                    break;
                            }

                        break;
                }
            }
        }

        var completionKind = CompletionKind.None;
        bool hasUnresolvedAutoImports = false;
        Slice<Symbol> symbols = default;
        Slice<FixAndExport> autoImports = default;
        var symbolToOriginInfoMap = new Dictionary<int, SymbolOriginInfo>();
        var symbolToSortTextMap = new Dictionary<ulong, ByteStringValue>();
        HashSet<ulong>? seenPropertySymbols = null;
        bool isTypeOnlyLocation = insideJSDocTagTypeExpression || insideJsDocImportTag || importStatementCompletion != null && location.Parent != null && Ast.IsTypeOnlyImportOrExportDeclaration(location.Parent) || !isContextTokenValueLocation(contextToken) && (isPossiblyTypeArgumentPosition(contextToken, file, checker) || Ast.IsPartOfTypeNode(location) || isContextTokenTypeLocation(contextToken));
        void addSymbolOriginInfo(Symbol symbol, bool insertQuestionDot, bool insertAwait)
        {
            var id = Ast.GetSymbolId(symbol);
            if (insertAwait && (seenPropertySymbols ??= new()).Add(id))
                symbolToOriginInfoMap[symbols.Length - 1] = new SymbolOriginInfo
                {
                    kind = getNullableSymbolOriginInfoKind(SymbolOriginInfoKind.Promise, insertQuestionDot)
                };
            else if (insertQuestionDot)
                symbolToOriginInfoMap[symbols.Length - 1] = new SymbolOriginInfo
                {
                    kind = SymbolOriginInfoKind.Nullable
                };
        }

        void addSymbolSortInfo(Symbol symbol)
        {
            if (isStaticProperty(symbol))
                symbolToSortTextMap[Ast.GetSymbolId(symbol)] = CompletionSortText.LocalDeclarationPriority;
        }

        void addPropertySymbol(Symbol symbol, bool insertAwait, bool insertQuestionDot)
        {
            Node? computed = null;
            foreach (var declaration in symbol.Declarations)
                if (Ast.GetNameOfDeclaration(declaration)is { Kind: Kind.ComputedPropertyName } name)
                {
                    computed = name;
                    break;
                }

            if (computed != null)
            {
                var leftmost = getLeftMostName(computed.Expression());
                var nameSymbol = leftmost != null ? checker.GetSymbolAtLocation(leftmost) : null;
                var accessible = nameSymbol != null ? getFirstSymbolInChain(nameSymbol, contextToken, checker) : null;
                var id = accessible != null ? Ast.GetSymbolId(accessible) : default;
                if (id != default && (seenPropertySymbols ??= new()).Add(id))
                {
                    symbols = symbols.Append(accessible!);
                    symbolToSortTextMap[id] = CompletionSortText.GlobalsOrKeywords;
                    var module = accessible!.Parent;
                    if (module == null || !Checker.IsExternalModuleSymbol(module) || checker.TryGetMemberInModuleExportsAndProperties(accessible.NameValue, module) != accessible)
                        symbolToOriginInfoMap[symbols.Length - 1] = new SymbolOriginInfo
                        {
                            kind = getNullableSymbolOriginInfoKind(SymbolOriginInfoKind.SymbolMember, insertQuestionDot)
                        };
                // Upstream auto-import computed-symbol TODO remains source TODO.
                }
                else if (id == default || seenPropertySymbols?.Contains(id) != true)
                {
                    symbols = symbols.Append(symbol);
                    addSymbolOriginInfo(symbol, insertQuestionDot, insertAwait);
                    addSymbolSortInfo(symbol);
                }
            }
            else
            {
                symbols = symbols.Append(symbol);
                addSymbolOriginInfo(symbol, insertQuestionDot, insertAwait);
                addSymbolSortInfo(symbol);
            }
        }

        void addTypeProperties(Type type, bool insertAwait, bool insertQuestionDot)
        {
            if (checker.GetStringIndexType(type) != null)
            {
                isNewIdentifierLocation = true;
                defaultCommitCharacters = Slice<ByteStringValue>.Make(0);
            }

            if (isRightOfQuestionDot && checker.GetCallSignatures(type).Length != 0)
            {
                isNewIdentifierLocation = true;
                if (defaultCommitCharacters.IsNil)
                    defaultCommitCharacters = GoSlices.Clone(allCommitCharacters);
            }

            var access = node!.Kind == Kind.ImportType ? node : node!.Parent;
            if (inCheckedFile)
            {
                foreach (var symbol in checker.GetApparentProperties(type))
                    if (checker.IsValidPropertyAccessForCompletions(access, type, symbol))
                        addPropertySymbol(symbol, false, insertQuestionDot);
            }
            else
                foreach (var symbol in getPropertiesForCompletion(type, checker))
                    if (checker.IsValidPropertyAccessForCompletions(access, type, symbol))
                        symbols = symbols.Append(symbol);
            if (insertAwait && checker.GetPromisedTypeOfPromise(type)is { } promise)
                foreach (var symbol in checker.GetApparentProperties(promise))
                    if (checker.IsValidPropertyAccessForCompletions(access, promise, symbol))
                        addPropertySymbol(symbol, true, insertQuestionDot);
        }

        void getTypeScriptMemberSymbols()
        {
            completionKind = CompletionKind.PropertyAccess;
            bool isImportType = Ast.IsLiteralImportTypeNode(node);
            bool isTypeLocation = isImportType && !node!.AsImportTypeNode().IsTypeOf || Ast.IsPartOfTypeNode(node!.Parent) || isPossiblyTypeArgumentPosition(contextToken, file, checker);
            bool rhsImport = isInRightSideOfInternalImportEqualsDeclaration(node);
            if (Ast.IsEntityName(node) || isImportType || Ast.IsPropertyAccessExpression(node))
            {
                bool namespaceName = Ast.IsModuleDeclaration(node!.Parent);
                if (namespaceName)
                {
                    isNewIdentifierLocation = true;
                    defaultCommitCharacters = Slice<ByteStringValue>.Make(0);
                }

                var originalSymbol = checker.GetSymbolAtLocation(node);
                if (originalSymbol != null)
                {
                    var symbol = checker.SkipAlias(originalSymbol);
                    if ((symbol.Flags & (SymbolFlags.Module | SymbolFlags.Enum)) != 0)
                    {
                        var valueAccess = isImportType ? node : node!.Parent;
                        foreach (var exported in checker.GetExportsOfModule(symbol))
                        {
                            if (exported == null)
                                throw new InvalidOperationException("getExporsOfModule() should all be defined");
                            bool validValue() => checker.IsValidPropertyAccess(valueAccess, exported.NameValue);
                            bool validType() => symbolCanBeReferencedAtTypeLocation(exported, checker, null);
                            bool valid = namespaceName ? (exported.Flags & SymbolFlags.Namespace) != 0 && !Core.Every(exported.Declarations, node!.Parent, static (declaration, parent) => declaration.Parent == parent) : rhsImport ? validType() || validValue() : isTypeLocation || insideJSDocTagTypeExpression ? validType() : validValue();
                            if (valid)
                                symbols = symbols.Append(exported);
                        }

                        if (!isTypeLocation && !insideJSDocTagTypeExpression && Core.Some(symbol.Declarations, static declaration => declaration.Kind is not (Kind.SourceFile or Kind.ModuleDeclaration or Kind.EnumDeclaration)))
                        {
                            var type = checker.GetNonOptionalType(checker.GetTypeOfSymbolAtLocation(symbol, node));
                            bool insertQuestion = false;
                            if (checker.IsNullableType(type))
                            {
                                bool canCorrect = isRightOfDot && !isRightOfQuestionDot && preferences.IncludeAutomaticOptionalChainCompletions != Tristate.False;
                                if (canCorrect || isRightOfQuestionDot)
                                {
                                    type = checker.GetNonNullableType(type);
                                    insertQuestion = canCorrect;
                                }
                            }

                            addTypeProperties(type, (node.Flags & NodeFlags.AwaitContext) != 0, insertQuestion);
                        }

                        return;
                    }
                }
            }

            if (!isTypeLocation || Checker.IsInTypeQuery(node))
            {
                checker.TryGetThisTypeAtEx(node, false, null);
                var type = checker.GetNonOptionalType(checker.GetTypeAtLocation(node));
                if (!isTypeLocation)
                {
                    bool insertQuestion = false;
                    if (checker.IsNullableType(type))
                    {
                        bool canCorrect = isRightOfDot && !isRightOfQuestionDot && preferences.IncludeAutomaticOptionalChainCompletions != Tristate.False;
                        if (canCorrect || isRightOfQuestionDot)
                        {
                            type = checker.GetNonNullableType(type);
                            insertQuestion = canCorrect;
                        }
                    }

                    addTypeProperties(type, (node.Flags & NodeFlags.AwaitContext) != 0, insertQuestion);
                }
                else
                    addTypeProperties(checker.GetNonNullableType(type), false, false);
            }
        }

        (GlobalsSearch Result, Exception? Error) tryGetObjectTypeLiteralInTypeArgumentCompletionSymbols()
        {
            var literal = tryGetTypeLiteralNode(contextToken);
            if (literal == null)
                return (GlobalsSearch.Continue, null);
            var container = Ast.IsIntersectionTypeNode(literal.Parent) ? literal.Parent : literal;
            var expected = getConstraintOfTypeArgumentProperty(container, checker);
            if (expected == null)
                return (GlobalsSearch.Continue, null);
            var actual = checker.GetTypeFromTypeNode(container);
            var members = getPropertiesForCompletion(expected, checker);
            var existingMembers = getPropertiesForCompletion(actual, checker);
            HashSet<ByteStringKey>? existing = null;
            foreach (var member in existingMembers)
                (existing ??= new()).Add(ByteStringKey.FromValue(member.NameValue));
            symbols = symbols.Append(Core.Filter(members, existing, static (member, names) => names?.Contains(ByteStringKey.FromValue(member.NameValue)) != true));
            completionKind = CompletionKind.ObjectPropertyDeclaration;
            isNewIdentifierLocation = true;
            return (GlobalsSearch.Success, null);
        }

        (GlobalsSearch Result, Exception? Error) tryGetObjectLikeCompletionSymbols()
        {
            if (contextToken != null && contextToken!.Kind == Kind.DotDotDotToken)
                return (GlobalsSearch.Continue, null);
            var container = tryGetObjectLikeCompletionContainer(contextToken, position, file);
            if (container == null)
                return (GlobalsSearch.Continue, null);
            completionKind = CompletionKind.ObjectPropertyDeclaration;
            Slice<Symbol> typeMembers = default;
            Slice<Node> existingMembers = default;
            if (container.Kind == Kind.ObjectLiteralExpression)
            {
                var instantiated = tryGetObjectLiteralContextualType(container, checker);
                if (instantiated == null)
                    return ((container.Flags & NodeFlags.InWithStatement) != 0 ? GlobalsSearch.Fail : GlobalsSearch.Continue, null);
                var contextual = checker.GetContextualType(container, ContextFlags.IgnoreNodeInferences);
                var type = contextual ?? instantiated;
                var stringIndex = checker.GetStringIndexType(type);
                var numberIndex = checker.GetNumberIndexType(type);
                isNewIdentifierLocation = stringIndex != null || numberIndex != null;
                typeMembers = getPropertiesForObjectExpression(instantiated, contextual, container, checker);
                existingMembers = container.Properties();
                if (typeMembers.Length == 0 && numberIndex == null)
                    return (GlobalsSearch.Continue, null);
            }
            else
            {
                if (container.Kind != Kind.ObjectBindingPattern)
                    throw new InvalidOperationException("Expected 'objectLikeContainer' to be an object binding pattern.");
                isNewIdentifierLocation = false;
                var root = Ast.GetRootDeclaration(container.Parent);
                if (!Ast.IsVariableLike(root))
                    throw new InvalidOperationException("Root declaration is not variable-like.");
                bool canGetType = Ast.HasInitializer(root) || Ast.GetTypeAnnotationNode(root) != null || root.Parent!.Parent.Kind == Kind.ForOfStatement;
                if (!canGetType && root.Kind == Kind.Parameter)
                {
                    if (Ast.IsExpression(root.Parent))
                        canGetType = checker.GetContextualType(root.Parent, ContextFlags.None) != null;
                    else if (root.Parent.Kind is Kind.MethodDeclaration or Kind.SetAccessor)
                        canGetType = Ast.IsExpression(root.Parent!.Parent) && checker.GetContextualType(root.Parent!.Parent, ContextFlags.None) != null;
                }

                if (canGetType)
                {
                    var type = checker.GetTypeAtLocation(container);
                    if (type == null)
                        return (GlobalsSearch.Fail, null);
                    typeMembers = Core.Filter(checker.GetPropertiesOfType(type), (container, type, checker), static (property, state) => state.checker.IsPropertyAccessible(state.container, false, false, state.type, property));
                    existingMembers = container.Elements();
                }
            }

            if (typeMembers.Length > 0)
            {
                foreach (var member in existingMembers)
                    if (member == null)
                        throw new InvalidOperationException("object like properties or elements should all be defined");
                var(filtered, spreadNames) = filterObjectMembersList(typeMembers, existingMembers, file, position, checker);
                symbols = symbols.Append(filtered);
                foreach (var member in filtered)
                {
                    var id = Ast.GetSymbolId(member);
                    if (spreadNames?.Contains(ByteStringKey.FromValue(member.NameValue)) == true)
                        symbolToSortTextMap[id] = CompletionSortText.MemberDeclaredBySpreadAssignment;
                    if ((member.Flags & SymbolFlags.Optional) != 0 && !symbolToSortTextMap.ContainsKey(id))
                        symbolToSortTextMap[id] = CompletionSortText.OptionalMember;
                    if (container.Kind == Kind.ObjectLiteralExpression && preferences.IncludeCompletionsWithObjectLiteralMethodSnippets == Tristate.True)
                    {
                        var(displayName, _) = getCompletionEntryDisplayNameForSymbol(file, preferences, member, null, CompletionKind.ObjectPropertyDeclaration, false);
                        if (displayName.Length != 0)
                        {
                            var originalSort = symbolToSortTextMap.GetValueOrDefault(id);
                            symbolToSortTextMap[id] = CompletionSortText.ObjectLiteralPropertySortText(originalSort.Length != 0 ? originalSort : CompletionSortText.LocationPriority, displayName);
                        }
                    }
                }

                if (container.Kind == Kind.ObjectLiteralExpression && preferences.IncludeCompletionsWithObjectLiteralMethodSnippets == Tristate.True)
                    foreach (var entry in collectObjectLiteralMethodSymbols(context, checker, filtered, container, file))
                    {
                        symbolToOriginInfoMap[symbols.Length] = entry.origin;
                        symbols = symbols.Append(entry.symbol);
                    }
            }

            return (GlobalsSearch.Success, null);
        }

        bool shouldOfferImportCompletions()
        {
            if (RootedFilePath.FromValue(file.FileNameGoValue()).IsDynamic())
                return false;
            if (importStatementCompletion != null)
                return true;
            return preferences.IncludeCompletionsForModuleExports != Tristate.False;
        }

        Exception? collectAutoImports()
        {
            if (forItemResolve || !shouldOfferImportCompletions())
                return null;
            ByteStringValue lowerToken = default;
            var(usagePosition, fidelity) = createLspPosition(position, file);
            if (!fidelity.IsExact())
                return null;
            if (previousToken != null && Ast.IsIdentifier(previousToken))
            {
                (usagePosition, fidelity) = createLspPosition(ScannerApi.GetTokenPosOfNode(previousToken, file, false), file);
                if (!fidelity.IsExact())
                    return null;
                if (!(previousToken == contextToken && importStatementCompletion != null))
                    lowerToken = GoStrings.ToLower(previousToken.TextValue());
            }

            var(view, error) = getPreparedAutoImportView(file, checker);
            if (error != null)
                return error;
            if (view == null)
                return null;
            autoImports = view.GetCompletions(lowerToken, usagePosition, isRightOfOpenTag, isTypeOnlyLocation);
            return null;
        }

        (GlobalsSearch Result, Exception? Error) tryGetImportCompletionSymbols()
        {
            if (importStatementCompletion == null)
                return (GlobalsSearch.Continue, null);
            isNewIdentifierLocation = true;
            var error = collectAutoImports();
            return (error != null ? GlobalsSearch.Fail : GlobalsSearch.Success, error);
        }

        (GlobalsSearch Result, Exception? Error) tryGetImportOrExportClauseCompletionSymbols()
        {
            if (contextToken == null)
                return (GlobalsSearch.Continue, null);
            Node? named = null;
            if (contextToken!.Kind is Kind.OpenBraceToken or Kind.CommaToken)
                named = isNamedImportsOrExports(contextToken!.Parent) ? contextToken!.Parent : null;
            else if (isTypeKeywordTokenOrIdentifier(contextToken))
                named = isNamedImportsOrExports(contextToken!.Parent!.Parent) ? contextToken!.Parent!.Parent : null;
            if (named == null)
                return (GlobalsSearch.Continue, null);
            if (!isTypeKeywordTokenOrIdentifier(contextToken))
                keywordFilters = KeywordCompletionFilters.TypeKeyword;
            var specifier = (named.Kind == Kind.NamedImports ? named.Parent!.Parent : named.Parent).ModuleSpecifier();
            if (specifier == null)
            {
                isNewIdentifierLocation = true;
                return (named.Kind == Kind.NamedImports ? GlobalsSearch.Fail : GlobalsSearch.Continue, null);
            }

            var module = checker.GetSymbolAtLocation(specifier);
            if (module == null)
            {
                isNewIdentifierLocation = true;
                return (GlobalsSearch.Fail, null);
            }

            completionKind = CompletionKind.MemberLike;
            isNewIdentifierLocation = false;
            var exports = checker.GetExportsAndPropertiesOfModule(module);
            HashSet<ByteStringKey>? existing = null;
            foreach (var element in named.Elements())
            {
                if (isCurrentlyEditingNode(element, file, position))
                    continue;
                (existing ??= new()).Add(ByteStringKey.FromValue(element.PropertyNameOrName().TextValue()));
            }

            var uniques = Core.Filter(exports, existing, static (symbol, names) => !Ast.SymbolNameValue(symbol).EqualsString(Ast.InternalSymbolNameDefault) && names?.Contains(ByteStringKey.FromValue(Ast.SymbolNameValue(symbol))) != true);
            symbols = symbols.Append(uniques);
            if (uniques.Length == 0)
                keywordFilters = KeywordCompletionFilters.None;
            return (GlobalsSearch.Success, null);
        }

        (GlobalsSearch Result, Exception? Error) tryGetImportAttributesCompletionSymbols()
        {
            if (contextToken == null)
                return (GlobalsSearch.Continue, null);
            Node? attributes = contextToken!.Kind switch
            {
                Kind.OpenBraceToken or Kind.CommaToken => contextToken!.Parent,
                Kind.ColonToken => contextToken!.Parent!.Parent,
                _ => null
            };
            if (attributes == null || !Ast.IsImportAttributes(attributes))
                return (GlobalsSearch.Continue, null);
            Slice<Node> elements = attributes.AsImportAttributes().Attributes?.Nodes ?? default;
            var names = Core.MapSlice(elements, static element => ByteStringKey.FromValue(element.AsImportAttribute().Name().TextValue()));
            var existing = new HashSet<ByteStringKey>();
            foreach (var name in names)
                existing.Add(name);
            var uniques = Core.Filter(checker.GetApparentProperties(checker.GetTypeAtLocation(attributes)), existing, static (symbol, names) => !names.Contains(ByteStringKey.FromValue(Ast.SymbolNameValue(symbol))));
            symbols = symbols.Append(uniques);
            return (GlobalsSearch.Success, null);
        }

        (GlobalsSearch Result, Exception? Error) tryGetLocalNamedExportCompletionSymbols()
        {
            if (contextToken == null)
                return (GlobalsSearch.Continue, null);
            var named = contextToken!.Kind is Kind.OpenBraceToken or Kind.CommaToken && Ast.IsNamedExports(contextToken!.Parent) ? contextToken!.Parent : null;
            if (named == null)
                return (GlobalsSearch.Continue, null);
            var container = Ast.FindAncestor(named, static node => Ast.IsSourceFile(node) || Ast.IsModuleDeclaration(node));
            completionKind = CompletionKind.None;
            isNewIdentifierLocation = false;
            var localExports = container.Symbol()?.Exports;
            var locals = container.Locals();
            if (locals != null)
                foreach (var(name, symbol)in locals)
                {
                    symbols = symbols.Append(symbol);
                    if (localExports?.ContainsKey(name) == true)
                        symbolToSortTextMap[Ast.GetSymbolId(symbol)] = CompletionSortText.OptionalMember;
                }

            return (GlobalsSearch.Success, null);
        }

        (GlobalsSearch Result, Exception? Error) tryGetConstructorCompletion()
        {
            if (tryGetConstructorLikeCompletionContainer(contextToken) == null)
                return (GlobalsSearch.Continue, null);
            completionKind = CompletionKind.None;
            isNewIdentifierLocation = true;
            keywordFilters = KeywordCompletionFilters.ConstructorParameterKeywords;
            return (GlobalsSearch.Success, null);
        }

        (GlobalsSearch Result, Exception? Error) tryGetClassLikeCompletionSymbols()
        {
            var declaration = tryGetObjectTypeDeclarationCompletionContainer(file, contextToken, location, position);
            if (declaration == null)
                return (GlobalsSearch.Continue, null);
            completionKind = CompletionKind.MemberLike;
            isNewIdentifierLocation = true;
            keywordFilters = contextToken!.Kind == Kind.AsteriskToken ? KeywordCompletionFilters.None : Ast.IsClassLike(declaration) ? KeywordCompletionFilters.ClassElementKeywords : KeywordCompletionFilters.InterfaceElementKeywords;
            if (!Ast.IsClassLike(declaration))
                return (GlobalsSearch.Success, null);
            var element = contextToken!.Kind == Kind.SemicolonToken ? contextToken!.Parent!.Parent : contextToken!.Parent;
            var flags = Ast.IsClassElement(element) ? element.ModifierFlags() : ModifierFlags.None;
            if (contextToken!.Kind == Kind.Identifier && !isCurrentlyEditingNode(contextToken, file, position))
            {
                var text = contextToken.TextValue();
                if (text.EqualsString("private"))
                    flags |= ModifierFlags.Private;
                else if (text.EqualsString("static"))
                    flags |= ModifierFlags.Static;
                else if (text.EqualsString("override"))
                    flags |= ModifierFlags.Override;
            }

            if (Ast.IsClassStaticBlockDeclaration(element))
                flags |= ModifierFlags.Static;
            if ((flags & ModifierFlags.Private) == 0)
            {
                var baseNodes = Ast.IsClassLike(declaration) && (flags & ModifierFlags.Override) != 0 ? Core.SingleElementSlice(Ast.GetClassExtendsHeritageElement(declaration)) : getAllSuperTypeNodes(declaration);
                Slice<Symbol> baseSymbols = default;
                foreach (var baseNode in baseNodes)
                {
                    var type = checker.GetTypeAtLocation(baseNode);
                    if ((flags & ModifierFlags.Static) != 0)
                    {
                        if (type.Symbol()is { } typeSymbol)
                            baseSymbols = baseSymbols.Append(checker.GetPropertiesOfType(checker.GetTypeOfSymbolAtLocation(typeSymbol, declaration)));
                    }
                    else if (type != null)
                        baseSymbols = baseSymbols.Append(checker.GetPropertiesOfType(type));
                }

                symbols = symbols.Append(filterClassMembersList(baseSymbols, declaration.Members(), flags, file, position));
                for (int index = 0; index < symbols.Length; index++)
                {
                    var symbol = symbols[index];
                    var value = symbol.ValueDeclaration;
                    if (value != null && Ast.IsClassElement(value) && value.Name() != null && Ast.IsComputedPropertyName(value.Name()))
                        symbolToOriginInfoMap[index] = new SymbolOriginInfo
                        {
                            kind = SymbolOriginInfoKind.ComputedPropertyName,
                            data = new SymbolOriginInfoComputedPropertyName
                            {
                                symbolName = checker.SymbolToStringValue(symbol)
                            }
                        };
                }
            }

            return (GlobalsSearch.Success, null);
        }

        (GlobalsSearch Result, Exception? Error) tryGetJsxCompletionSymbols()
        {
            var jsx = tryGetContainingJsxElement(contextToken, file);
            if (jsx == null)
                return (GlobalsSearch.Continue, null);
            var attributes = jsx.Attributes();
            var type = checker.GetContextualType(attributes, ContextFlags.None);
            if (type == null)
                return (GlobalsSearch.Continue, null);
            var contextual = checker.GetContextualType(attributes, ContextFlags.IgnoreNodeInferences);
            var(filtered, spreadNames) = filterJsxAttributes(getPropertiesForObjectExpression(type, contextual, attributes, checker), attributes.Properties(), file, position, checker);
            symbols = symbols.Append(filtered);
            foreach (var symbol in filtered)
            {
                var id = Ast.GetSymbolId(symbol);
                if (spreadNames?.Contains(ByteStringKey.FromValue(Ast.SymbolNameValue(symbol))) == true)
                    symbolToSortTextMap[id] = CompletionSortText.MemberDeclaredBySpreadAssignment;
                if ((symbol.Flags & SymbolFlags.Optional) != 0 && !symbolToSortTextMap.ContainsKey(id))
                    symbolToSortTextMap[id] = CompletionSortText.OptionalMember;
            }

            completionKind = CompletionKind.MemberLike;
            isNewIdentifierLocation = false;
            return (GlobalsSearch.Success, null);
        }

        (GlobalsSearch Result, Exception? Error) getGlobalCompletions()
        {
            keywordFilters = tryGetFunctionLikeBodyCompletionContainer(contextToken) != null ? KeywordCompletionFilters.FunctionLikeBodyKeywords : KeywordCompletionFilters.All;
            completionKind = CompletionKind.Global;
            (isNewIdentifierLocation, defaultCommitCharacters) = computeCommitCharactersAndIsNewIdentifier(contextToken, file, position);
            if (previousToken != contextToken && previousToken == null)
                throw new InvalidOperationException("Expected 'contextToken' to be defined when different from 'previousToken'.");
            int adjustedPosition = previousToken != contextToken ? AstNav.GetStartOfNode(previousToken, file, false) : position;
            var scope = getScopeNode(contextToken, adjustedPosition, file) ?? file.AsNode();
            isInSnippetScope = isSnippetScope(scope);
            var meanings = (isTypeOnlyLocation ? SymbolFlags.None : SymbolFlags.Value) | SymbolFlags.Type | SymbolFlags.Namespace | SymbolFlags.Alias;
            bool needsPromotion = previousToken != null && !Ast.IsValidTypeOnlyAliasUseSite(previousToken);
            symbols = symbols.Append(checker.GetSymbolsInScope(scope, meanings));
            for (int index = 0; index < symbols.Length; index++)
            {
                var symbol = symbols[index];
                if (symbol == null)
                    throw new InvalidOperationException("getSymbolsInScope() should all be defined");
                if (!checker.IsArgumentsSymbol(symbol) && !Core.Some(symbol.Declarations, file, static (declaration, file) => Ast.GetSourceFileOfNode(declaration) == file))
                    symbolToSortTextMap[Ast.GetSymbolId(symbol)] = CompletionSortText.GlobalsOrKeywords;
                if (needsPromotion && (symbol.Flags & SymbolFlags.Value) == 0)
                {
                    var typeOnly = Core.Find(symbol.Declarations, Ast.IsTypeOnlyImportDeclaration);
                    if (typeOnly != null)
                        symbolToOriginInfoMap[index] = new SymbolOriginInfo
                        {
                            kind = SymbolOriginInfoKind.TypeOnlyAlias,
                            data = new SymbolOriginInfoTypeOnlyAlias
                            {
                                declaration = typeOnly
                            }
                        };
                }
            }

            if (scope.Kind != Kind.SourceFile)
            {
                var thisType = checker.TryGetThisTypeAtEx(scope, false, Ast.IsClassLike(scope.Parent) ? scope : null);
                if (thisType != null && !isProbablyGlobalType(thisType, file, checker))
                    foreach (var symbol in getPropertiesForCompletion(thisType, checker))
                    {
                        symbols = symbols.Append(symbol);
                        symbolToOriginInfoMap[symbols.Length - 1] = new SymbolOriginInfo
                        {
                            kind = SymbolOriginInfoKind.ThisType
                        };
                        symbolToSortTextMap[Ast.GetSymbolId(symbol)] = CompletionSortText.SuggestedClassMembers;
                    }
            }

            var error = collectAutoImports();
            if (error != null)
                return (GlobalsSearch.Fail, error);
            if (isTypeOnlyLocation)
                keywordFilters = contextToken != null && Ast.IsAssertionExpression(contextToken!.Parent) ? KeywordCompletionFilters.TypeAssertionKeywords : KeywordCompletionFilters.TypeKeywords;
            return (GlobalsSearch.Success, null);
        }

        (bool Ok, Exception? Error) tryGetGlobalSymbols()
        {
            Func<(GlobalsSearch Result, Exception? Error)>[] functions = [tryGetObjectTypeLiteralInTypeArgumentCompletionSymbols, tryGetObjectLikeCompletionSymbols, tryGetImportCompletionSymbols, tryGetImportOrExportClauseCompletionSymbols, tryGetImportAttributesCompletionSymbols, tryGetLocalNamedExportCompletionSymbols, tryGetConstructorCompletion, tryGetClassLikeCompletionSymbols, tryGetJsxCompletionSymbols, getGlobalCompletions];
            var result = GlobalsSearch.Continue;
            foreach (var function in functions)
            {
                var outcome = function();
                result = outcome.Result;
                if (outcome.Error != null)
                    return (false, outcome.Error);
                if (result != GlobalsSearch.Continue)
                    break;
            }

            return (result == GlobalsSearch.Success, null);
        }

        if (isRightOfDot || isRightOfQuestionDot)
            getTypeScriptMemberSymbols();
        else if (isRightOfOpenTag)
        {
            symbols = checker.GetJsxIntrinsicTagNamesAt(location);
            foreach (var symbol in symbols)
                if (symbol == null)
                    throw new InvalidOperationException("GetJsxIntrinsicTagNamesAt() should all be defined");
            var(_, error) = tryGetGlobalSymbols();
            if (error != null)
                return (null, error);
            completionKind = CompletionKind.Global;
            keywordFilters = KeywordCompletionFilters.None;
        }
        else if (isStartingCloseTag)
        {
            var name = contextToken!.Parent!.Parent.AsJsxElement().OpeningElement.TagName();
            var symbol = checker.GetSymbolAtLocation(name);
            if (symbol != null)
                symbols = Slice<Symbol>.FromArray([symbol]);
            completionKind = CompletionKind.Global;
            keywordFilters = KeywordCompletionFilters.None;
        }
        else
        {
            var(ok, error) = tryGetGlobalSymbols();
            if (!ok)
            {
                if (error != null)
                    return (null, error);
                return (keywordFilters != KeywordCompletionFilters.None ? keywordCompletionData(keywordFilters, isJSOnlyLocation, isNewIdentifierLocation) : null, null);
            }
        }

        Type? contextual = previousToken != null ? getContextualType(previousToken, position, file, checker) ?? getConstraintOfTypeArgumentProperty(previousToken, checker) : null;
        bool literalExpected = !(previousToken != null && Ast.IsStringLiteralLike(previousToken)) && !isJsxIdentifierExpected;
        Slice<object> literals = default;
        if (literalExpected)
        {
            var types = contextual != null && contextual.IsUnion() ? contextual.Types() : contextual != null ? Slice<Type>.FromArray([contextual]) : default;
            foreach (var type in types)
                if (isLiteral(type) && !type.IsEnumLiteral())
                    literals = literals.Append(type.AsLiteralType().Value());
        }

        var recommended = previousToken != null && contextual != null ? getRecommendedCompletion(previousToken, contextual, checker) : null;
        if (defaultCommitCharacters.IsNil)
            defaultCommitCharacters = getDefaultCommitCharacters(isNewIdentifierLocation);
        return (new CompletionDataData { symbols = symbols, autoImports = autoImports, completionKind = completionKind, isInSnippetScope = isInSnippetScope, propertyAccessToConvert = propertyAccessToConvert, isNewIdentifierLocation = isNewIdentifierLocation, location = location, keywordFilters = keywordFilters, literals = literals, symbolToOriginInfoMap = symbolToOriginInfoMap, symbolToSortTextMap = symbolToSortTextMap, recommendedCompletion = recommended, previousToken = previousToken, contextToken = contextToken, jsxInitializer = jsxInitializer, insideJSDocTagTypeExpression = insideJSDocTagTypeExpression, isTypeOnlyLocation = isTypeOnlyLocation, isJsxIdentifierExpected = isJsxIdentifierExpected, isRightOfOpenTag = isRightOfOpenTag, isRightOfDotOrQuestionDot = isRightOfDot || isRightOfQuestionDot, importStatementCompletion = importStatementCompletion, hasUnresolvedAutoImports = hasUnresolvedAutoImports, defaultCommitCharacters = defaultCommitCharacters, }, null);
    }

    private static CompletionDataKeyword keywordCompletionData(KeywordCompletionFilters filters, bool filterTS, bool isNewIdentifier) => new()
    {
        keywordCompletions = getKeywordCompletions(filters, filterTS),
        isNewIdentifierLocation = isNewIdentifier
    };
    private static Slice<ByteStringValue> getDefaultCommitCharacters(bool isNewIdentifier) => isNewIdentifier ? Slice<ByteStringValue>.Make(0) : GoSlices.Clone(allCommitCharacters);
}

// Source assembly component: CompletionText.cs
internal sealed partial class LanguageService
{
    private static (int Rune, int Size) completionDecodeRune(ByteStringValue value) => value.IsBytes ? Utf8.DecodeRuneInString(value.ByteSpan) : Utf8.DecodeRuneInString(value.CharSpan);
    private static Slice<ByteStringValue> splitCompletionText(ByteStringValue text, ByteStringValue separator)
    {
        // strings.genSplit: every result retains the input header, with one
        // precisely sized owner for the returned []string header array.
        if (separator.Length == 0)
        {
            int count = 0;
            for (int pos = 0; pos < text.Length; count++)
            {
                var(_, size) = completionDecodeRune(text.Slice(pos));
                pos += size;
            }

            var runes = Slice<ByteStringValue>.Make(count);
            int index = 0;
            for (int pos = 0; pos < text.Length;)
            {
                var(_, size) = completionDecodeRune(text.Slice(pos));
                runes[index++] = text.Slice(pos, pos + size);
                pos += size;
            }

            return runes;
        }

        int pieces = 1;
        for (int pos = 0; pos <= text.Length;)
        {
            int at = text.Slice(pos).IndexOf(separator);
            if (at < 0)
                break;
            pieces++;
            pos += at + separator.Length;
        }

        var result = Slice<ByteStringValue>.Make(pieces);
        int start = 0;
        for (int i = 0; i < pieces - 1; i++)
        {
            int end = start + text.Slice(start).IndexOf(separator);
            result[i] = text.Slice(start, end);
            start = end + separator.Length;
        }

        result[pieces - 1] = text.Slice(start);
        return result;
    }
}

// Source assembly component: CompletionObjectContext.cs
internal sealed partial class LanguageService
{
    private static CheckerType? getConstraintOfTypeArgumentProperty(Node? node, Checker checker)
    {
        if (node == null)
            return null;
        if (AST.IsTypeNode(node))
        {
            var constraint = checker.GetTypeArgumentConstraint(node);
            if (constraint != null)
                return constraint;
        }

        var type = getConstraintOfTypeArgumentProperty(node.Parent, checker);
        if (type == null)
            return null;
        switch (node.Kind)
        {
            case Kind.PropertySignature:
                var reparsed = AST.GetReparsedNodeForNode(node);
                if (reparsed.Symbol()is { } symbol)
                    return checker.GetTypeOfPropertyOfContextualType(type, symbol.NameValue);
                var(name, ok) = AST.TryGetTextValueOfPropertyName(reparsed.Name());
                return ok ? checker.GetTypeOfPropertyOfContextualType(type, name) : null;
            case Kind.ColonToken:
                if (node.Parent!.Kind == Kind.PropertySignature)
                    return type;
                break;
            case Kind.IntersectionType:
            case Kind.TypeLiteral:
            case Kind.UnionType:
                return type;
            case Kind.OpenBracketToken:
                return checker.GetElementTypeOfArrayType(type);
        }

        return null;
    }

    private static Node? tryGetObjectLikeCompletionContainer(Node? token, int position, SourceFile file)
    {
        if (token == null)
            return null;
        var parent = token.Parent!;
        switch (token.Kind)
        {
            case Kind.OpenBraceToken:
            case Kind.CommaToken:
                if (AST.IsObjectLiteralExpression(parent) || AST.IsObjectBindingPattern(parent))
                    return parent;
                break;
            case Kind.AsteriskToken:
                if (AST.IsMethodDeclaration(parent) && AST.IsObjectLiteralExpression(parent.Parent))
                    return parent.Parent;
                break;
            case Kind.AsyncKeyword:
                if (AST.IsObjectLiteralExpression(parent.Parent))
                    return parent.Parent;
                break;
            case Kind.Identifier:
                if (token.TextValue().EqualsValue(ByteStringValue.FromString("async")) && AST.IsShorthandPropertyAssignment(parent))
                    return parent.Parent;
                else
                {
                    if (AST.IsObjectLiteralExpression(parent.Parent) && (AST.IsSpreadAssignment(parent) || AST.IsShorthandPropertyAssignment(parent) && getLineOfPosition(file, token.End()) != getLineOfPosition(file, position)))
                        return parent.Parent;
                    var assignment = AST.FindAncestor(parent, AST.IsPropertyAssignment);
                    if (assignment != null && LsUtil.GetLastToken(assignment, file) == token && AST.IsObjectLiteralExpression(assignment.Parent))
                        return assignment.Parent;
                }

                break;
            default:
                if (parent.Parent != null && parent.Parent.Parent != null && (AST.IsMethodDeclaration(parent.Parent) || AST.IsGetAccessorDeclaration(parent.Parent) || AST.IsSetAccessorDeclaration(parent.Parent)) && AST.IsObjectLiteralExpression(parent.Parent.Parent))
                    return parent.Parent.Parent;
                if (AST.IsSpreadAssignment(parent) && AST.IsObjectLiteralExpression(parent.Parent))
                    return parent.Parent;
                var ancestor = AST.FindAncestor(parent, AST.IsPropertyAssignment);
                if (token.Kind != Kind.ColonToken && ancestor != null && LsUtil.GetLastToken(ancestor, file) == token && AST.IsObjectLiteralExpression(ancestor.Parent))
                    return ancestor.Parent;
                break;
        }

        return null;
    }

    private static CheckerType? tryGetObjectLiteralContextualType(Node node, Checker checker)
    {
        var type = checker.GetContextualType(node, ContextFlags.None);
        if (type != null)
            return type;
        var parent = AST.WalkUpParenthesizedExpressions(node.Parent);
        if (AST.IsBinaryExpression(parent) && parent.AsBinaryExpression().OperatorToken.Kind == Kind.EqualsToken && node == parent.AsBinaryExpression().Left)
            return checker.GetTypeAtLocation(parent);
        return AST.IsExpression(parent) ? checker.GetContextualType(parent, ContextFlags.None) : null;
    }

    private static Slice<Symbol> getPropertiesForObjectExpression(CheckerType contextual, CheckerType? completionType, Node obj, Checker checker)
    {
        bool hasCompletion = completionType != null && completionType != contextual;
        var types = contextual.IsUnion() ? contextual.Types() : Slice<CheckerType>.FromArray([contextual]);
        var promiseFiltered = checker.GetUnionType(Core.Filter(types, type => checker.GetPromisedTypeOfPromise(type) == null));
        var combined = hasCompletion && (completionType!.Flags() & TypeFlags.AnyOrUnknown) == 0 ? checker.GetUnionType(Slice<CheckerType>.FromArray([promiseFiltered, completionType!])) : promiseFiltered;
        bool HasOtherDeclaration(Symbol member) => member.Declarations.Length == 0 || Core.Some(member.Declarations, declaration => declaration.Parent != obj);
        var properties = getApparentProperties(combined, obj, checker);
        if (combined.IsClass() && containsNonPublicProperties(properties))
            return default;
        return hasCompletion ? Core.Filter(properties, HasOtherDeclaration) : properties;
    }

    private static Slice<Symbol> getApparentProperties(CheckerType type, Node node, Checker checker)
    {
        if (!type.IsUnion())
            return checker.GetApparentProperties(type);
        return checker.GetAllPossiblePropertiesOfTypes(Core.Filter(type.Types(), member => !((member.Flags() & TypeFlags.Primitive) != 0 || checker.IsArrayLikeType(member) || checker.IsTypeInvalidDueToUnionDiscriminant(member, node) || checker.TypeHasCallOrConstructSignatures(member) || member.IsClass() && containsNonPublicProperties(checker.GetApparentProperties(member)))));
    }

    private static bool containsNonPublicProperties(Slice<Symbol> properties) => Core.Some(properties, property => (Checker.GetDeclarationModifierFlagsFromSymbol(property) & ModifierFlags.NonPublicAccessibilityModifier) != 0);
    private static (Slice<Symbol>, HashSet<ByteStringKey>? ) filterObjectMembersList(Slice<Symbol> contextual, Slice<Node> existing, SourceFile file, int position, Checker checker)
    {
        if (existing.Length == 0)
            return (contextual, null);
        HashSet<ByteStringKey>? spread = null, names = null;
        foreach (var member in existing)
        {
            if (member.Kind is not (Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment or Kind.BindingElement or Kind.MethodDeclaration or Kind.GetAccessor or Kind.SetAccessor or Kind.SpreadAssignment))
                continue;
            if (isCurrentlyEditingNode(member, file, position))
                continue;
            ByteStringValue name = default;
            if (AST.IsSpreadAssignment(member))
                setMemberDeclaredBySpreadAssignment(member, ref spread, checker);
            else if (AST.IsBindingElement(member) && member.PropertyName() != null)
            {
                if (member.PropertyName()!.Kind == Kind.Identifier)
                    name = member.PropertyName()!.TextValue();
            }
            else
            {
                var declarationName = AST.GetNameOfDeclaration(member);
                if (declarationName != null && AST.IsPropertyNameLiteral(declarationName))
                    name = declarationName.TextValue();
            }

            if (name.Length != 0)
                (names ??= new()).Add(ByteStringKey.FromValue(name));
        }

        return (Core.Filter(contextual, member => names?.Contains(ByteStringKey.FromValue(member.NameValue)) != true), spread);
    }

    private static bool isCurrentlyEditingNode(Node node, SourceFile file, int position) => AstNav.GetStartOfNode(node, file, false) <= position && position <= node.End();
    private static void setMemberDeclaredBySpreadAssignment(Node declaration, ref HashSet<ByteStringKey>? members, Checker checker)
    {
        var expression = declaration.Expression();
        var symbol = checker.GetSymbolAtLocation(expression);
        var type = symbol == null ? null : checker.GetTypeOfSymbolAtLocation(symbol, expression);
        Slice<Symbol> properties = default;
        if (type != null && (type.Flags() & TypeFlags.StructuredType) != 0)
            properties = type.AsStructuredType().Properties();
        foreach (var property in properties)
            (members ??= new()).Add(ByteStringKey.FromValue(property.NameValue));
    }

    private static Node? tryGetConstructorLikeCompletionContainer(Node? token)
    {
        if (token == null)
            return null;
        if (token.Kind is Kind.OpenParenToken or Kind.CommaToken)
            return AST.IsConstructorDeclaration(token.Parent) ? token.Parent : null;
        return isConstructorParameterCompletion(token) ? token.Parent!.Parent : null;
    }

    private static bool isConstructorParameterCompletion(Node node) => node.Parent != null && AST.IsParameterDeclaration(node.Parent) && AST.IsConstructorDeclaration(node.Parent.Parent) && (AST.IsParameterPropertyModifier(node.Kind) || AST.IsDeclarationName(node));
    private static Node? tryGetObjectTypeDeclarationCompletionContainer(SourceFile file, Node? token, Node location, int position)
    {
        switch (location.Kind)
        {
            case Kind.SyntaxList:
                return AST.IsObjectTypeDeclaration(location.Parent) ? location.Parent : null;
            case Kind.EndOfFile:
                var statements = location.Parent!.StatementList();
                if (statements != null && statements.Nodes.Length > 0 && AST.IsObjectTypeDeclaration(statements.Nodes[statements.Nodes.Length - 1]))
                {
                    var last = statements.Nodes[statements.Nodes.Length - 1];
                    if (AstNav.FindChildOfKind(last, Kind.CloseBraceToken, file) == null)
                        return last;
                }

                break;
            case Kind.PrivateIdentifier:
                if (AST.IsPropertyDeclaration(location.Parent))
                    return AST.FindAncestor(location, AST.IsClassLike);
                break;
            case Kind.Identifier:
                if (ScannerApi.IdentifierToKeywordKind(location.AsIdentifier()) != Kind.Unknown)
                    return null;
                if (AST.IsPropertyDeclaration(location.Parent) && location.Parent!.Initializer() == location)
                    return null;
                if (isFromObjectTypeDeclaration(location))
                    return AST.FindAncestor(location, AST.IsObjectTypeDeclaration);
                break;
        }

        if (token == null)
            return null;
        if (location.Kind == Kind.ConstructorKeyword || AST.IsIdentifier(token) && AST.IsPropertyDeclaration(token.Parent) && AST.IsClassLike(location))
            return AST.FindAncestor(token, AST.IsClassLike);
        switch (token.Kind)
        {
            case Kind.EqualsToken:
                return null;
            case Kind.SemicolonToken:
            case Kind.CloseBraceToken:
                if (isFromObjectTypeDeclaration(location) && location.Parent!.Name() == location)
                    return location.Parent.Parent;
                return AST.IsObjectTypeDeclaration(location) ? location : null;
            case Kind.OpenBraceToken:
            case Kind.CommaToken:
                return AST.IsObjectTypeDeclaration(token.Parent) ? token.Parent : null;
            default:
                if (AST.IsObjectTypeDeclaration(location))
                {
                    if (getLineOfPosition(file, token.End()) != getLineOfPosition(file, position))
                        return location;
                    bool Valid(Kind kind) => AST.IsClassLike(token.Parent!.Parent) ? isClassMemberCompletionKeyword(kind) : isInterfaceOrTypeLiteralCompletionKeyword(kind);
                    if (Valid(token.Kind) || token.Kind == Kind.AsteriskToken || AST.IsIdentifier(token) && Valid(ScannerApi.IdentifierToKeywordKind(token.AsIdentifier())))
                        return token.Parent!.Parent;
                }

                return null;
        }
    }

    private static bool isFromObjectTypeDeclaration(Node node) => node.Parent != null && AST.IsClassOrTypeElement(node.Parent) && AST.IsObjectTypeDeclaration(node.Parent.Parent);
}

// Source assembly component: CompletionModels.cs
internal sealed class CompletionItem
{
    internal Protocol.CompletionItem? Item;
    internal Symbol? Symbol;
}

internal sealed class CompletionList
{
    internal bool IsIncomplete;
    internal Protocol.CompletionItemDefaults? ItemDefaults;
    internal Protocol.CompletionItemApplyKinds? ApplyKind;
    internal Slice<CompletionItem?> Items;
    internal static Protocol.CompletionList? ToLSP(CompletionList? list)
    {
        if (list == null)
            return null;
        var items = Slice<Protocol.CompletionItem?>.Make(0, list.Items.Length);
        foreach (var entry in list.Items)
        {
            if (entry?.Item is { } item)
                items = items.Append(item);
        }

        return new Protocol.CompletionList
        {
            IsIncomplete = list.IsIncomplete,
            ItemDefaults = list.ItemDefaults,
            ApplyKind = list.ApplyKind,
            Items = items,
        };
    }
}

// Source assembly component: CompletionResolve.cs
internal sealed partial class LanguageService
{
    internal (Protocol.CompletionItem? , Exception? ) ResolveCompletionItem(ProtocolRequestContext context, Protocol.CompletionItem item, CompletionItemData? data, RootedFilePath fileName)
    {
        if (data == null)
            return (null, new InvalidOperationException("completion item data is nil"));
        var(owner, file) = tryGetProgramAndFile(fileName);
        if (file == null)
            return (null, new InvalidOperationException("file not found: " + data.FileName.Materialize()));
        file = sourceFileForSupplementalFileIndex(file, data.SupplementalFileIndex);
        if (file == null)
            return (null, new InvalidOperationException("supplemental source file index not found: " + data.SupplementalFileIndex!.Value));
        var(checker, done) = owner!.GetTypeCheckerForFile(context, file);
        try
        {
            return (getCompletionItemDetails(context, owner, checker, data.Position, file, item, data), null);
        }
        finally
        {
            done();
        }
    }

    private static MarkupKind getCompletionDocumentationFormat(ProtocolRequestContext context) => ProtocolUtilities.PreferredMarkupKind(ProtocolContexts.GetClientCapabilities(context).TextDocument.Completion.CompletionItem.DocumentationFormat);
    private Protocol.CompletionItem getCompletionItemDetails(ProtocolRequestContext context, Compiler.Program owner, Checker checker, int position, SourceFile file, Protocol.CompletionItem item, CompletionItemData data)
    {
        var format = getCompletionDocumentationFormat(context);
        var(token, previous) = getRelevantTokens(position, file);
        if (IsInString(file, position, previous))
            return getStringLiteralCompletionDetails(context, checker, item, data.Name, file, position, token, format);
        if (data.AutoImport != null)
        {
            if (data.IsImportStatementCompletion)
                return item;
            var(edits, description, _) = new AutoImport.Fix
            {
                AutoImportFix = data.AutoImport
            }.Edits(context, file, owner.Options(), FormatOptions(), converters, UserPreferences());
            item.AdditionalTextEdits = nullableTextEditHeader(edits);
            item.Detail = strPtrTo(description);
            return item;
        }

        var completion = getSymbolCompletionFromItemData(context, checker, file, position, data);
        var preferences = UserPreferences();
        if (completion.request != null)
        {
            switch (completion.request)
            {
                case CompletionDataJSDocTagName:
                case CompletionDataJSDocTag:
                case CompletionDataJSDocParameterName:
                    return createSimpleDetails(item, data.Name, format);
                case CompletionDataKeyword keyword:
                    return Core.Some(keyword.keywordCompletions, entry => entry!.Item!.Label.EqualsValue(data.Name)) ? createSimpleDetails(item, data.Name, format) : item;
                default:
                    throw new InvalidOperationException("Unexpected completion data type: " + completionDataTypeName(completion.request));
            }
        }

        if (completion.symbol != null)
            return createCompletionDetailsForSymbol(item, completion.symbol.symbol, checker, completion.symbol.location, position, format);
        if (completion.literal != null)
            return createSimpleDetails(item, completionNameForLiteral(file, preferences, completion.literal), format);
        if (completion.cases)
            return item;
        return Core.Some(allKeywordCompletions.Value, entry => entry!.Label.EqualsValue(data.Name)) ? createSimpleDetails(item, data.Name, format) : item;
    }

    private struct DetailsData
    {
        internal SymbolDetails? symbol;
        internal CompletionData? request;
        internal object? literal;
        internal bool cases;
    }

    private sealed class SymbolDetails
    {
        internal Symbol symbol = null!;
        internal Node location = null!;
        internal SymbolOriginInfo? origin;
        internal Node? previousToken, contextToken;
        internal JsxInitializer jsxInitializer;
        internal bool isTypeOnlyLocation;
    }

    private DetailsData getSymbolCompletionFromItemData(ProtocolRequestContext context, Checker checker, SourceFile file, int position, CompletionItemData itemData)
    {
        if (itemData.Source.EqualsValue(CompletionSource.SwitchCases))
            return new DetailsData
            {
                cases = true
            };
        var(completion, error) = getCompletionData(context, checker, file, position, UserPreferences(), true);
        if (error != null)
            throw error;
        if (completion == null)
            return default;
        if (completion is not CompletionDataData data)
            return new DetailsData
            {
                request = completion
            };
        var preferences = UserPreferences();
        foreach (var literal in data.literals)
            if (completionNameForLiteral(file, preferences, literal).EqualsValue(itemData.Name))
                return new DetailsData
                {
                    literal = literal
                };
        for (int index = 0; index < data.symbols.Length; index++)
        {
            var symbol = data.symbols[index];
            SymbolOriginInfo? origin = null;
            data.symbolToOriginInfoMap?.TryGetValue(index, out origin);
            var(display, _) = getCompletionEntryDisplayNameForSymbol(file, preferences, symbol, origin, data.completionKind, data.isJsxIdentifierExpected);
            if (display.EqualsValue(itemData.Name) && (itemData.Source.EqualsValue(CompletionSource.ClassMemberSnippet) && (symbol.Flags & SymbolFlags.ClassMember) != 0 || itemData.Source.EqualsValue(CompletionSource.ObjectLiteralMethodSnippet) && (symbol.Flags & (SymbolFlags.Property | SymbolFlags.Method)) != 0 || getSourceFromOrigin(origin).EqualsValue(itemData.Source) || itemData.Source.EqualsValue(CompletionSource.ObjectLiteralMemberWithComma)))
                return new DetailsData
                {
                    symbol = new SymbolDetails
                    {
                        symbol = symbol,
                        location = data.location,
                        origin = origin,
                        previousToken = data.previousToken,
                        contextToken = data.contextToken,
                        jsxInitializer = data.jsxInitializer,
                        isTypeOnlyLocation = data.isTypeOnlyLocation
                    }
                };
        }

        return default;
    }

    private static Protocol.CompletionItem createSimpleDetails(Protocol.CompletionItem item, ByteStringValue name, MarkupKind format) => createCompletionDetails(item, name, default, format);
    private static Protocol.CompletionItem createCompletionDetails(Protocol.CompletionItem item, ByteStringValue detail, ByteStringValue documentation, MarkupKind format)
    {
        if (item.Detail == null && detail.Length != 0)
            item.Detail = detail;
        if (documentation.Length != 0)
            item.Documentation = new StringOrMarkupContent
            {
                MarkupContent = new MarkupContent
                {
                    Kind = format,
                    Value = documentation
                }
            };
        return item;
    }

    private Protocol.CompletionItem createCompletionDetailsForSymbol(Protocol.CompletionItem item, Symbol symbol, Checker checker, Node location, int position, MarkupKind format)
    {
        var(info, documentation, _, _) = getQuickInfoAndDocumentationForSymbol(checker, symbol, location, format, null!, false);
        return createCompletionDetails(item, info, documentation, format);
    }
}

// Source assembly component: CompletionContextualType.cs
internal sealed partial class LanguageService
{
    private static TypeScript.Checking.Type? getContextualTypeForConditionalExpression(Node conditional, int position, SourceFile file, Checker checker)
    {
        var info = getArgumentInfoForCompletions(conditional, position, file, checker);
        if (info != null)
            return checker.GetContextualTypeForArgumentAtIndex(info.invocation, info.argumentIndex);
        return checker.GetContextualType(conditional, ContextFlags.IgnoreNodeInferences) ?? checker.GetContextualType(conditional, ContextFlags.None);
    }

    private static TypeScript.Checking.Type? getContextualType(Node previous, int position, SourceFile file, Checker checker)
    {
        var parent = previous.Parent!;
        switch (previous.Kind)
        {
            case Kind.Identifier:
                return getContextualTypeFromParent(previous, checker, ContextFlags.None);
            case Kind.EqualsToken:
                return parent.Kind switch
                {
                    Kind.VariableDeclaration => checker.GetContextualType(parent.Initializer(), ContextFlags.None),
                    Kind.BinaryExpression => checker.GetTypeAtLocation(parent.AsBinaryExpression().Left),
                    Kind.JsxAttribute => checker.GetContextualTypeForJsxAttribute(parent),
                    _ => null
                };
            case Kind.NewKeyword:
                return checker.GetContextualType(parent, ContextFlags.None);
            case Kind.CaseKeyword:
                return AST.IsCaseClause(parent) ? getSwitchedType(parent, checker) : null;
            case Kind.OpenBraceToken:
                return AST.IsJsxExpression(parent) && !AST.IsJsxElement(parent.Parent) && !AST.IsJsxFragment(parent.Parent) ? checker.GetContextualTypeForJsxAttribute(parent.Parent) : null;
            case Kind.OpenBracketToken:
                if (AST.IsArrayLiteralExpression(parent))
                {
                    var array = checker.GetContextualType(parent, ContextFlags.None);
                    if (array != null)
                        return checker.GetContextualTypeForArrayLiteralAtPosition(array, parent, position);
                }

                return null;
            case Kind.CloseBracketToken:
                return null;
            case Kind.QuestionToken:
                return AST.IsConditionalExpression(parent) ? getContextualTypeForConditionalExpression(parent, position, file, checker) : null;
            case Kind.ColonToken:
                if (AST.IsConditionalExpression(parent))
                    return getContextualTypeForConditionalExpression(parent, position, file, checker);
                break;
            case Kind.CommaToken:
                if (AST.IsArrayLiteralExpression(parent))
                {
                    var array = checker.GetContextualType(parent, ContextFlags.None);
                    return array == null ? null : checker.GetContextualTypeForArrayLiteralAtPosition(array, parent, position);
                }

                break;
        }

        var argument = getArgumentInfoForCompletions(previous, position, file, checker);
        if (argument != null)
            return checker.GetContextualTypeForArgumentAtIndex(argument.invocation, argument.argumentIndex);
        if (isEqualityOperatorKind(previous.Kind) && AST.IsBinaryExpression(parent) && isEqualityOperatorKind(parent.AsBinaryExpression().OperatorToken.Kind))
            return checker.GetTypeAtLocation(parent.AsBinaryExpression().Left);
        return checker.GetContextualType(previous, ContextFlags.IgnoreNodeInferences) ?? checker.GetContextualType(previous, ContextFlags.None);
    }

    // Exact rehome of accepted completions shared leaves; old leaf is removed only at coherent promotion.
    private static TypeScript.Checking.Type? getSwitchedType(Node caseClause, Checker checker) => checker.GetTypeAtLocation(caseClause.Parent.Parent.Expression());
    private static bool isEqualityOperatorKind(Kind kind) => kind is Kind.EqualsEqualsEqualsToken or Kind.EqualsEqualsToken or Kind.ExclamationEqualsEqualsToken or Kind.ExclamationEqualsToken;
    private static bool isLiteral(TypeScript.Checking.Type type) => type.IsStringLiteral() || type.IsNumberLiteral() || type.IsBigIntLiteral();
    private static Symbol? getRecommendedCompletion(Node previous, TypeScript.Checking.Type contextual, Checker checker)
    {
        var types = contextual.IsUnion() ? contextual.Types() : Slice<TypeScript.Checking.Type>.FromArray([contextual]);
        foreach (var type in types)
        {
            var symbol = type.Symbol();
            if (symbol != null && (symbol.Flags & (SymbolFlags.EnumMember | SymbolFlags.Enum | SymbolFlags.Class)) != 0 && !isAbstractConstructorSymbol(symbol))
            {
                var first = getFirstSymbolInChain(symbol, previous, checker);
                if (first != null)
                    return first;
            }
        }

        return null;
    }

    private static bool isAbstractConstructorSymbol(Symbol symbol)
    {
        if ((symbol.Flags & SymbolFlags.Class) == 0)
            return false;
        var declaration = AST.GetClassLikeDeclarationOfSymbol(symbol);
        return declaration != null && AST.HasSyntacticModifier(declaration, ModifierFlags.Abstract);
    }

    private static bool startsWithQuote(ByteStringValue text) => completionDecodeRune(text).Rune is '"' or '\'';
    private static Node? getClosestSymbolDeclaration(Node? token, Node location)
    {
        if (token == null)
            return null;
        var closest = AST.FindAncestorOrQuit(token, static node =>
        {
            if (AST.IsFunctionBlock(node) || isArrowFunctionBody(node) || AST.IsBindingPattern(node))
                return FindAncestorResult.Quit;
            return (AST.IsParameterDeclaration(node) || AST.IsTypeParameterDeclaration(node)) && !AST.IsIndexSignatureDeclaration(node.Parent) ? FindAncestorResult.True : FindAncestorResult.False;
        });
        return closest ?? AST.FindAncestorOrQuit(location, static node =>
        {
            if (AST.IsFunctionBlock(node) || isArrowFunctionBody(node) || AST.IsBindingPattern(node))
                return FindAncestorResult.Quit;
            return AST.IsVariableDeclaration(node) ? FindAncestorResult.True : FindAncestorResult.False;
        });
    }

    private static bool isArrowFunctionBody(Node node) => node.Parent != null && AST.IsArrowFunction(node.Parent) && (node.Parent.Body() == node || node.Kind == Kind.EqualsGreaterThanToken);
    private static bool isInTypeParameterDefault(Node? token)
    {
        if (token == null)
            return false;
        var node = token;
        for (var parent = token.Parent; parent != null; parent = parent.Parent)
        {
            if (AST.IsTypeParameterDeclaration(parent))
                return parent.AsTypeParameterDeclaration().DefaultType == node || node.Kind == Kind.EqualsToken;
            node = parent;
        }

        return false;
    }

    private static bool isDeprecated(Symbol symbol, Checker checker)
    {
        var declarations = checker.SkipAlias(symbol).Declarations;
        return declarations.Length > 0 && Core.Every(declarations, checker.IsDeprecatedDeclaration);
    }

    private TypeScript.Lsp.Protocol.Range? getReplacementRangeForContextToken(SourceFile file, Node? token, int position)
    {
        if (token == null)
            return null;
        if (token.Kind is Kind.StringLiteral or Kind.NoSubstitutionTemplateLiteral)
            return createRangeFromStringLiteralLikeContent(file, token, position);
        var(range, fidelity) = createLspRangeFromNode(token, file);
        return fidelity.IsExact() ? range : null;
    }

    private TypeScript.Lsp.Protocol.Range? createRangeFromStringLiteralLikeContent(SourceFile file, Node node, int position)
    {
        int end = node.End() - 1, start = AstNav.GetStartOfNode(node, file, false);
        if (AST.IsUnterminatedLiteral(node))
        {
            if (start == end)
                return null;
            end = Math.Min(position, node.End());
        }

        var(range, fidelity) = createLspRangeFromBounds(start + 1, end, file);
        return fidelity.IsExact() ? range : null;
    }

    private static ByteStringValue quotePropertyName(SourceFile file, Utilities.UserPreferences preferences, ByteStringValue name) => GoUnicode.IsDigit(completionDecodeRune(name).Rune) ? name : quote(file, preferences, name);
    private static bool isStringAndEmptyAnonymousObjectIntersection(Checker checker, TypeScript.Checking.Type type) => type.IsIntersection() && type.Types().Length == 2 && (areIntersectedTypesAvoidingStringReduction(checker, type.Types()[0], type.Types()[1]) || areIntersectedTypesAvoidingStringReduction(checker, type.Types()[1], type.Types()[0]));
    private static bool areIntersectedTypesAvoidingStringReduction(Checker checker, TypeScript.Checking.Type first, TypeScript.Checking.Type second) => first.IsString() && checker.IsEmptyAnonymousObjectType(second);
    private static bool isNamedImportsOrExports(Node node) => AST.IsNamedImports(node) || AST.IsNamedExports(node);
}

// Source assembly component: CompletionCommitContext.cs
internal sealed partial class LanguageService
{
    private static (bool, Slice<ByteStringValue>) computeCommitCharactersAndIsNewIdentifier(Node? token, SourceFile file, int position)
    {
        if (token == null)
            return (false, allCommitCharacters);
        var parent = token.Parent!.Kind;
        var kind = keywordForNode(token);
        switch (kind)
        {
            case Kind.CommaToken:
                switch (parent)
                {
                    case Kind.CallExpression:
                    case Kind.NewExpression:
                        return (true, getLineOfPosition(file, token.Parent.Expression()!.End()) != getLineOfPosition(file, position) ? noCommaCommitCharacters : allCommitCharacters);
                    case Kind.BinaryExpression:
                        return (true, noCommaCommitCharacters);
                    case Kind.Constructor:
                    case Kind.FunctionType:
                    case Kind.ObjectLiteralExpression:
                        return (true, emptyCommitCharacters);
                    case Kind.ArrayLiteralExpression:
                        return (true, allCommitCharacters);
                    default:
                        return (false, allCommitCharacters);
                }

            case Kind.OpenParenToken:
                switch (parent)
                {
                    case Kind.CallExpression:
                    case Kind.NewExpression:
                        return (true, getLineOfPosition(file, token.Parent.Expression()!.End()) != getLineOfPosition(file, position) ? noCommaCommitCharacters : allCommitCharacters);
                    case Kind.ParenthesizedExpression:
                        return (true, noCommaCommitCharacters);
                    case Kind.Constructor:
                    case Kind.ParenthesizedType:
                        return (true, emptyCommitCharacters);
                    default:
                        return (false, allCommitCharacters);
                }

            case Kind.OpenBracketToken:
                return (parent is Kind.ArrayLiteralExpression or Kind.IndexSignature or Kind.TupleType or Kind.ComputedPropertyName, allCommitCharacters);
            case Kind.ModuleKeyword:
            case Kind.NamespaceKeyword:
            case Kind.ImportKeyword:
                return (true, emptyCommitCharacters);
            case Kind.DotToken:
                return parent == Kind.ModuleDeclaration ? (true, emptyCommitCharacters) : (false, allCommitCharacters);
            case Kind.OpenBraceToken:
                return parent is Kind.ClassDeclaration or Kind.ObjectLiteralExpression ? (true, emptyCommitCharacters) : (false, allCommitCharacters);
            case Kind.EqualsToken:
                return (parent is Kind.VariableDeclaration or Kind.BinaryExpression, allCommitCharacters);
            case Kind.TemplateHead:
                return (parent == Kind.TemplateExpression, allCommitCharacters);
            case Kind.TemplateMiddle:
                return (parent == Kind.TemplateSpan, allCommitCharacters);
            case Kind.AsyncKeyword:
                return parent is Kind.MethodDeclaration or Kind.ShorthandPropertyAssignment ? (true, emptyCommitCharacters) : (false, allCommitCharacters);
            case Kind.AsteriskToken:
                return parent == Kind.MethodDeclaration ? (true, emptyCommitCharacters) : (false, allCommitCharacters);
        }

        return isClassMemberCompletionKeyword(kind) ? (true, emptyCommitCharacters) : (false, allCommitCharacters);
    }

    private static Kind keywordForNode(Node node) => AST.IsIdentifier(node) ? ScannerApi.IdentifierToKeywordKind(node.AsIdentifier()) : node.Kind;
    private static Node? getScopeNode(Node? initial, int position, SourceFile file)
    {
        var scope = initial;
        while (scope != null && !positionBelongsToNode(scope, position, file))
            scope = scope.Parent;
        return scope;
    }

    private static bool isSnippetScope(Node scope) => scope.Kind is Kind.SourceFile or Kind.TemplateExpression or Kind.JsxExpression or Kind.Block || AST.IsStatement(scope);
    private static bool isProbablyGlobalType(TypeScript.Checking.Type type, SourceFile file, Checker checker)
    {
        var self = checker.GetGlobalSymbol("self", SymbolFlags.Value, null);
        if (self != null && checker.GetTypeOfSymbolAtLocation(self, file.AsNode()) == type)
            return true;
        var global = checker.GetGlobalSymbol("global", SymbolFlags.Value, null);
        if (global != null && checker.GetTypeOfSymbolAtLocation(global, file.AsNode()) == type)
            return true;
        var globalThis = checker.GetGlobalSymbol("globalThis", SymbolFlags.Value, null);
        return globalThis != null && checker.GetTypeOfSymbolAtLocation(globalThis, file.AsNode()) == type;
    }

    private static Node? tryGetTypeLiteralNode(Node? node)
    {
        if (node == null)
            return null;
        var parent = node.Parent!;
        switch (node.Kind)
        {
            case Kind.OpenBraceToken:
                if (AST.IsTypeLiteralNode(parent))
                    return parent;
                break;
            case Kind.SemicolonToken:
            case Kind.CommaToken:
            case Kind.Identifier:
                if (parent.Kind == Kind.PropertySignature && AST.IsTypeLiteralNode(parent.Parent))
                    return parent.Parent;
                break;
        }

        return null;
    }
}

// Source assembly component: CompletionBlockers.cs
internal sealed partial class LanguageService
{
    private static bool isCompletionListBlocker(Node token, Node? previous, Node location, SourceFile file, int position, Checker checker) => isInStringOrRegularExpressionOrTemplateLiteral(token, position) || isSolelyIdentifierDefinitionLocation(token, previous, file, position, checker) || isDotOfNumericLiteral(token, file) || isInJsxText(token, location) || AST.IsBigIntLiteral(token);
    private static bool isInStringOrRegularExpressionOrTemplateLiteral(Node token, int position) => (AST.IsRegularExpressionLiteral(token) || AST.IsStringTextContainingNode(token)) && token.Loc.ContainsExclusive(position) || position == token.End() && (AST.IsUnterminatedLiteral(token) || AST.IsRegularExpressionLiteral(token));
    private static bool isSolelyIdentifierDefinitionLocation(Node token, Node? previous, SourceFile file, int position, Checker checker)
    {
        var parent = token.Parent!;
        var containing = parent.Kind;
        switch (token.Kind)
        {
            case Kind.CommaToken:
                return containing == Kind.VariableDeclaration || isVariableDeclarationListButNotTypeArgument(token, file, checker) || containing is Kind.VariableStatement or Kind.EnumDeclaration || isFunctionLikeButNotConstructor(containing) || containing is Kind.InterfaceDeclaration or Kind.ArrayBindingPattern or Kind.TypeAliasDeclaration || AST.IsClassLike(parent) && parent.TypeParameterList() != null && parent.TypeParameterList()!.End() >= token.Pos();
            case Kind.DotToken:
                return containing == Kind.ArrayBindingPattern;
            case Kind.ColonToken:
                return containing == Kind.BindingElement;
            case Kind.OpenBracketToken:
                return containing == Kind.ArrayBindingPattern;
            case Kind.OpenParenToken:
                return containing == Kind.CatchClause || isFunctionLikeButNotConstructor(containing);
            case Kind.OpenBraceToken:
                return containing == Kind.EnumDeclaration;
            case Kind.LessThanToken:
                return containing is Kind.ClassDeclaration or Kind.ClassExpression or Kind.InterfaceDeclaration or Kind.TypeAliasDeclaration || AST.IsFunctionLikeKind(containing);
            case Kind.StaticKeyword:
                return containing == Kind.PropertyDeclaration && !AST.IsClassLike(parent.Parent);
            case Kind.DotDotDotToken:
                return containing == Kind.Parameter || parent.Parent?.Kind == Kind.ArrayBindingPattern;
            case Kind.PublicKeyword:
            case Kind.PrivateKeyword:
            case Kind.ProtectedKeyword:
                return containing == Kind.Parameter && !AST.IsConstructorDeclaration(parent.Parent);
            case Kind.AsKeyword:
                return containing is Kind.ImportSpecifier or Kind.ExportSpecifier or Kind.NamespaceImport;
            case Kind.GetKeyword:
            case Kind.SetKeyword:
                return !isFromObjectTypeDeclaration(token);
            case Kind.Identifier:
                if (containing is Kind.ImportSpecifier or Kind.ExportSpecifier && token == parent.Name() && token.TextValue().EqualsValue(ByteStringValue.FromString("type")))
                    return false;
                var variable = AST.FindAncestor(parent, AST.IsVariableDeclaration);
                if (variable != null && getLineEndOfPosition(file, token.End()) < position)
                    return false;
                break;
            case Kind.ClassKeyword:
            case Kind.EnumKeyword:
            case Kind.InterfaceKeyword:
            case Kind.FunctionKeyword:
            case Kind.VarKeyword:
            case Kind.ImportKeyword:
            case Kind.LetKeyword:
            case Kind.ConstKeyword:
            case Kind.InferKeyword:
                return true;
            case Kind.TypeKeyword:
                return containing != Kind.ImportSpecifier;
            case Kind.AsteriskToken:
                return AST.IsFunctionLike(parent) && !AST.IsMethodDeclaration(parent);
        }

        var kind = keywordForNode(token);
        if (isClassMemberCompletionKeyword(kind) && isFromObjectTypeDeclaration(token))
            return false;
        if (isConstructorParameterCompletion(token) && (!AST.IsIdentifier(token) || AST.IsParameterPropertyModifier(kind) || isCurrentlyEditingNode(token, file, position)))
            return false;
        switch (kind)
        {
            case Kind.AbstractKeyword:
            case Kind.ClassKeyword:
            case Kind.DeclareKeyword:
            case Kind.EnumKeyword:
            case Kind.FunctionKeyword:
            case Kind.InterfaceKeyword:
            case Kind.LetKeyword:
            case Kind.PrivateKeyword:
            case Kind.ProtectedKeyword:
            case Kind.PublicKeyword:
            case Kind.StaticKeyword:
            case Kind.VarKeyword:
                return true;
            case Kind.AsyncKeyword:
                return AST.IsPropertyDeclaration(token.Parent);
        }

        var cls = AST.FindAncestor(parent, AST.IsClassLike);
        if (cls != null && token == previous && isPreviousPropertyDeclarationTerminated(token, file, position))
            return false;
        var property = AST.FindAncestor(parent, AST.IsPropertyDeclaration);
        if (property != null && token != previous && AST.IsClassLike(previous!.Parent!.Parent) && position <= previous.End())
        {
            if (isPreviousPropertyDeclarationTerminated(token, file, previous.End()))
                return false;
            if (token.Kind != Kind.EqualsToken && (AST.IsInitializedProperty(property) || property.Type() != null))
                return true;
        }

        if (kind == Kind.ConstKeyword)
            return true;
        return AST.IsDeclarationName(token) && !AST.IsShorthandPropertyAssignment(parent) && !AST.IsJsxAttribute(parent) && !((AST.IsClassLike(parent) || AST.IsInterfaceDeclaration(parent) || AST.IsTypeParameterDeclaration(parent)) && (token != previous || position > previous!.End()));
    }

    private static bool isVariableDeclarationListButNotTypeArgument(Node node, SourceFile file, Checker checker) => node.Parent!.Kind == Kind.VariableDeclarationList && !isPossiblyTypeArgumentPosition(node, file, checker);
    private static bool isFunctionLikeButNotConstructor(Kind kind) => AST.IsFunctionLikeKind(kind) && kind != Kind.Constructor;
    private static bool isPreviousPropertyDeclarationTerminated(Node token, SourceFile file, int position) => token.Kind != Kind.EqualsToken && (token.Kind == Kind.SemicolonToken || getLineOfPosition(file, token.End()) != getLineOfPosition(file, position));
    private static bool isDotOfNumericLiteral(Node token, SourceFile file) => token.Kind == Kind.NumericLiteral && Utf8.DecodeLastRuneInString(file.TextView().Slice(token.Pos(), token.End()).AsSpan()).Rune == '.';
    private static bool isInJsxText(Node token, Node location)
    {
        if (token.Kind == Kind.JsxText)
            return true;
        if (token.Kind == Kind.GreaterThanToken && token.Parent != null)
        {
            if (location == token.Parent && AST.IsJsxOpeningLikeElement(location))
                return false;
            if (token.Parent.Kind == Kind.JsxOpeningElement)
                return location.Parent!.Kind != Kind.JsxOpeningElement;
            if (token.Parent.Kind is Kind.JsxClosingElement or Kind.JsxSelfClosingElement)
                return token.Parent.Parent?.Kind == Kind.JsxElement;
        }

        return false;
    }

    private static bool clientSupportsItemLabelDetails(ProtocolRequestContext context) => ProtocolContexts.GetClientCapabilities(context).TextDocument.Completion.CompletionItem.LabelDetailsSupport;
    private static bool clientSupportsItemSnippet(ProtocolRequestContext context) => ProtocolContexts.GetClientCapabilities(context).TextDocument.Completion.CompletionItem.SnippetSupport;
    private static bool clientSupportsItemCommitCharacters(ProtocolRequestContext context) => ProtocolContexts.GetClientCapabilities(context).TextDocument.Completion.CompletionItem.CommitCharactersSupport;
    private static bool clientSupportsItemInsertReplace(ProtocolRequestContext context) => ProtocolContexts.GetClientCapabilities(context).TextDocument.Completion.CompletionItem.InsertReplaceSupport;
    private static bool clientSupportsDefaultCommitCharacters(ProtocolRequestContext context) => Core.Some(ProtocolContexts.GetClientCapabilities(context).TextDocument.Completion.CompletionList.ItemDefaults, value => value.EqualsValue(ByteStringValue.FromString("commitCharacters")));
    private static bool clientSupportsDefaultEditRange(ProtocolRequestContext context) => Core.Some(ProtocolContexts.GetClientCapabilities(context).TextDocument.Completion.CompletionList.ItemDefaults, value => value.EqualsValue(ByteStringValue.FromString("editRange")));
    private sealed class ArgumentInfoForCompletions
    {
        internal Node invocation = null!;
        internal int argumentIndex, argumentCount;
    }

    private static ArgumentInfoForCompletions? getArgumentInfoForCompletions(Node node, int position, SourceFile file, Checker checker)
    {
        var info = getImmediatelyContainingArgumentInfo(node, position, file, checker);
        if (info == null || info.isTypeParameterList || info.invocation.callInvocation == null)
            return null;
        return new ArgumentInfoForCompletions
        {
            invocation = info.invocation.callInvocation.node,
            argumentIndex = info.argumentIndex,
            argumentCount = info.argumentCount
        };
    }
}

// Source assembly component: CompletionProtocolItems.cs
internal sealed partial class LanguageService
{
    private Protocol.CompletionItem createLSPCompletionItem(ProtocolRequestContext context, ByteStringValue name, ByteStringValue insert, ByteStringValue filter, ByteStringValue sort, ScriptElementKind elementKind, ScriptElementKindModifier modifiers, Protocol.Range? replacement, Slice<ByteStringValue>? commits, CompletionItemLabelDetails? labelDetails, SourceFile file, int position, bool member, bool snippet, bool action, bool preselect, ByteStringValue source, AutoImportFix? autoImport, Slice<TextEdit?>? additional, ByteStringValue? detail)
    {
        var kind = getCompletionsSymbolKind(elementKind);
        var data = new CompletionItemData
        {
            FileName = file.OriginalFileName().AsValue(),
            Position = position,
            SupplementalFileIndex = supplementalFileIndex(file),
            Source = source,
            Name = name,
            AutoImport = autoImport
        };
        TextEditOrInsertReplaceEdit? edit = null;
        if (replacement != null)
            edit = new TextEditOrInsertReplaceEdit
            {
                TextEdit = new TextEdit
                {
                    NewText = insert.Length == 0 ? name : insert,
                    Range = replacement.Value
                }
            };
        var(wordSize, wordStart) = getWordLengthAndStart(file, position);
        var dot = getDotAccessor(file, position - wordSize);
        if (filter.Length == 0)
            filter = getFilterText(file, position, insert, name, wordStart, dot);
        Slice<CompletionItemTag>? tags = null;
        if (member && (modifiers & ScriptElementKindModifier.Optional) != 0)
        {
            if (insert.Length == 0)
                insert = name;
            if (filter.Length == 0 || snippet)
                filter = name;
            name = completionConcat(name, ByteStringValue.FromString("?"));
        }

        if ((modifiers & ScriptElementKindModifier.Deprecated) != 0)
            tags = Slice<CompletionItemTag>.FromArray([CompletionItemTag.Deprecated]);
        // Upstream action+source adjustment is explicitly unfinished; retain source behavior.
        return new Protocol.CompletionItem
        {
            Label = name,
            LabelDetails = labelDetails,
            Kind = kind,
            Tags = tags,
            Detail = detail,
            Preselect = boolToPtr(preselect),
            SortText = sort,
            FilterText = strPtrTo(filter),
            InsertText = strPtrTo(insert),
            InsertTextFormat = snippet ? Protocol.InsertTextFormat.Snippet : null,
            TextEdit = edit,
            CommitCharacters = commits,
            AdditionalTextEdits = additional,
            Data = data,
        };
    }

    private CompletionList? getLabelCompletionsAtPosition(ProtocolRequestContext context, Node node, SourceFile file, int position, Protocol.Range? replacement)
    {
        var items = getLabelStatementCompletions(context, node, file, position);
        if (items.Length == 0)
            return null;
        var commits = getDefaultCommitCharacters(false);
        return new CompletionList
        {
            IsIncomplete = false,
            ItemDefaults = setItemDefaults(context, position, file, items, ref commits, replacement),
            Items = items
        };
    }

    private Slice<CompletionItem?> getLabelStatementCompletions(ProtocolRequestContext context, Node node, SourceFile file, int position)
    {
        HashSet<ByteStringKey>? uniques = null;
        Slice<CompletionItem?> items = default;
        for (var current = node; current != null; current = current.Parent)
        {
            if (AST.IsFunctionLike(current))
                break;
            if (AST.IsLabeledStatement(current))
            {
                var name = current.Label()!.TextValue();
                if ((uniques ??= new()).Add(ByteStringKey.FromValue(name)))
                {
                    var item = createLSPCompletionItem(context, name, default, default, CompletionSortText.LocationPriority, ScriptElementKind.Label, ScriptElementKindModifier.None, null, null, null, file, position, false, false, false, false, default, null, null, null);
                    items = items.Append(new CompletionItem { Item = item });
                }
            }
        }

        return items;
    }
}

// Source assembly component: CompletionJSDocTags.cs
internal sealed partial class LanguageService
{
    private static readonly Slice<ByteStringValue> jsDocTagNames = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("abstract"), ByteStringValue.FromString("access"), ByteStringValue.FromString("alias"), ByteStringValue.FromString("argument"), ByteStringValue.FromString("async"), ByteStringValue.FromString("augments"), ByteStringValue.FromString("author"), ByteStringValue.FromString("borrows"), ByteStringValue.FromString("callback"), ByteStringValue.FromString("class"), ByteStringValue.FromString("classdesc"), ByteStringValue.FromString("constant"), ByteStringValue.FromString("constructor"), ByteStringValue.FromString("constructs"), ByteStringValue.FromString("copyright"), ByteStringValue.FromString("default"), ByteStringValue.FromString("deprecated"), ByteStringValue.FromString("description"), ByteStringValue.FromString("emits"), ByteStringValue.FromString("enum"), ByteStringValue.FromString("event"), ByteStringValue.FromString("example"), ByteStringValue.FromString("exports"), ByteStringValue.FromString("extends"), ByteStringValue.FromString("external"), ByteStringValue.FromString("field"), ByteStringValue.FromString("file"), ByteStringValue.FromString("fileoverview"), ByteStringValue.FromString("fires"), ByteStringValue.FromString("function"), ByteStringValue.FromString("generator"), ByteStringValue.FromString("global"), ByteStringValue.FromString("hideconstructor"), ByteStringValue.FromString("host"), ByteStringValue.FromString("ignore"), ByteStringValue.FromString("implements"), ByteStringValue.FromString("import"), ByteStringValue.FromString("inheritdoc"), ByteStringValue.FromString("inner"), ByteStringValue.FromString("instance"), ByteStringValue.FromString("interface"), ByteStringValue.FromString("kind"), ByteStringValue.FromString("lends"), ByteStringValue.FromString("license"), ByteStringValue.FromString("link"), ByteStringValue.FromString("linkcode"), ByteStringValue.FromString("linkplain"), ByteStringValue.FromString("listens"), ByteStringValue.FromString("member"), ByteStringValue.FromString("memberof"), ByteStringValue.FromString("method"), ByteStringValue.FromString("mixes"), ByteStringValue.FromString("module"), ByteStringValue.FromString("name"), ByteStringValue.FromString("namespace"), ByteStringValue.FromString("overload"), ByteStringValue.FromString("override"), ByteStringValue.FromString("package"), ByteStringValue.FromString("param"), ByteStringValue.FromString("private"), ByteStringValue.FromString("prop"), ByteStringValue.FromString("property"), ByteStringValue.FromString("protected"), ByteStringValue.FromString("public"), ByteStringValue.FromString("readonly"), ByteStringValue.FromString("requires"), ByteStringValue.FromString("returns"), ByteStringValue.FromString("satisfies"), ByteStringValue.FromString("see"), ByteStringValue.FromString("since"), ByteStringValue.FromString("static"), ByteStringValue.FromString("summary"), ByteStringValue.FromString("template"), ByteStringValue.FromString("this"), ByteStringValue.FromString("throws"), ByteStringValue.FromString("todo"), ByteStringValue.FromString("tutorial"), ByteStringValue.FromString("type"), ByteStringValue.FromString("typedef"), ByteStringValue.FromString("var"), ByteStringValue.FromString("variation"), ByteStringValue.FromString("version"), ByteStringValue.FromString("virtual"), ByteStringValue.FromString("yields"), ]);
    private static readonly Lazy<Slice<Protocol.CompletionItem?>> jsDocTagNameCompletionItems = new(static () => makeJSDocTagCompletionItems(false));
    private static readonly Lazy<Slice<Protocol.CompletionItem?>> jsDocTagCompletionItems = new(static () => makeJSDocTagCompletionItems(true));
    private static Slice<Protocol.CompletionItem?> makeJSDocTagCompletionItems(bool prefix)
    {
        var items = Slice<Protocol.CompletionItem?>.Make(0, jsDocTagNames.Length);
        foreach (var name in jsDocTagNames)
            items = items.Append(new Protocol.CompletionItem { Label = prefix ? completionConcat(ByteStringValue.FromString("@"), name) : name, Kind = CompletionItemKind.Keyword, SortText = CompletionSortText.LocationPriority });
        return items;
    }

    private static Slice<CompletionItem?> getJSDocTagNameCompletions() => cloneItems(jsDocTagNameCompletionItems.Value);
    private static Slice<CompletionItem?> getJSDocTagCompletions() => cloneItems(jsDocTagCompletionItems.Value);
    private static bool hasDocComment(SourceFile file, int position) => AST.FindAncestor(AstNav.GetTokenAtPosition(file, position), static node => node.IsJSDoc()) != null;
    private static Node? getJSDocTagAtPosition(Node node, int position) => AST.FindAncestorOrQuit(node, candidate => AST.IsJSDocTag(candidate) && candidate.Loc.ContainsInclusive(position) ? FindAncestorResult.True : candidate.IsJSDoc() ? FindAncestorResult.Quit : FindAncestorResult.False);
    private static Node? tryGetTypeExpressionFromTag(Node tag)
    {
        if (isTagWithTypeExpression(tag))
        {
            var expression = AST.IsJSDocTemplateTag(tag) ? tag.AsJSDocTemplateTag().Constraint : tag.TypeExpression();
            if (expression?.Kind == Kind.JSDocTypeExpression)
                return expression;
        }

        return AST.IsJSDocAugmentsTag(tag) || AST.IsJSDocImplementsTag(tag) ? tag.ClassName() : null;
    }

    private static bool isTagWithTypeExpression(Node tag) => tag.Kind is Kind.JSDocParameterTag or Kind.JSDocPropertyTag or Kind.JSDocReturnTag or Kind.JSDocTypeTag or Kind.JSDocTypedefTag or Kind.JSDocThrowsTag or Kind.JSDocSatisfiesTag || tag.Kind == Kind.JSDocTemplateTag && tag.AsJSDocTemplateTag().Constraint != null;
    private CompletionList jsDocCompletionInfo(ProtocolRequestContext context, int position, SourceFile file, Slice<CompletionItem?> items)
    {
        var commits = getDefaultCommitCharacters(false);
        return new CompletionList
        {
            IsIncomplete = false,
            ItemDefaults = setItemDefaults(context, position, file, items, ref commits, null),
            Items = items
        };
    }
}

// Source assembly component: CompletionExhaustiveCases.cs
internal sealed partial class LanguageService
{
    private (TypeScript.Lsp.Protocol.CompletionItem? Item, Exception? Error) getExhaustiveCaseSnippets(ProtocolRequestContext context, Node caseBlock, SourceFile file, int position, CompilerOptions options, Program program, Checker checker)
    {
        var clauses = caseBlock.AsCaseBlock().Clauses.Nodes;
        var switchType = checker.GetTypeAtLocation(caseBlock.Parent!.Expression());
        if (switchType == null || !switchType.IsUnion() || !Core.Every(switchType.Types(), isLiteral))
            return (null, null);
        var tracker = newCaseClauseTracker(checker, clauses);
        var target = options.GetEmitScriptTarget();
        var quotes = LsUtil.GetQuotePreference(file, UserPreferences());
        AutoImport.IImportAdder? adder = null;
        if (!TsPath.IsDynamicFileName(file.FileName()))
        {
            var(view, error) = getPreparedAutoImportView(file, checker);
            if (error != null)
                return (null, error);
            if (view != null)
                adder = AutoImportFunctions.NewImportAdder(context, program, checker, file, view, FormatOptions(), converters, UserPreferences());
        }

        Slice<Node> elements = default;
        var factory = Ast.NewNodeFactory(new NodeFactoryHooks());
        foreach (var type in switchType.Types())
        {
            if (type.IsEnumLiteral())
            {
                Debug.Assert(type.Symbol() != null, "An enum member type should have a symbol");
                Debug.Assert(type.Symbol()!.Parent != null, "An enum member type should have a parent symbol (the enum symbol)");
                object? value = type.Symbol()!.ValueDeclaration != null ? checker.GetConstantValue(type.Symbol()!.ValueDeclaration) : null;
                if (value != null)
                {
                    if (tracker.hasValue(value))
                        continue;
                    tracker.addValue(value);
                }

                var typeNode = AutoImportFunctions.TypeToAutoImportableTypeNode(checker, adder, type, caseBlock);
                if (typeNode == null)
                    return (null, null);
                var expression = typeNodeToExpression(typeNode, target, quotes, factory);
                if (expression == null)
                    return (null, null);
                elements = elements.Append(expression);
            }
            else
            {
                var value = type.AsLiteralType().value;
                if (tracker.hasValue(value))
                    continue;
                switch (value)
                {
                    case JsNum.PseudoBigInt integer:
                        var magnitude = integer.Negative ? integer with
                        {
                            Negative = false
                        }

                        : integer;
                        var bigInt = factory.NewBigIntLiteral(magnitude.String() + "n", TokenFlags.None);
                        elements = elements.Append(integer.Negative ? factory.NewPrefixUnaryExpression(Kind.MinusToken, bigInt) : bigInt);
                        break;
                    case JsNum.Number number:
                        var numeric = factory.NewNumericLiteral((number.Value < 0 ? number.Abs() : number).String(), TokenFlags.None);
                        elements = elements.Append(number.Value < 0 ? factory.NewPrefixUnaryExpression(Kind.MinusToken, numeric) : numeric);
                        break;
                    case ByteStringValue text:
                        elements = elements.Append(factory.NewStringLiteralWithTextValue(text, quotes == QuotePreference.Single ? TokenFlags.SingleQuote : TokenFlags.None));
                        break;
                    case string text:
                        elements = elements.Append(factory.NewStringLiteralWithTextValue(ByteStringValue.FromString(text), quotes == QuotePreference.Single ? TokenFlags.SingleQuote : TokenFlags.None));
                        break;
                }
            }
        }

        if (elements.Length == 0)
            return (null, null);
        var newClauses = Core.MapSlice(elements, element => factory.NewCaseOrDefaultClause(Kind.CaseClause, element, factory.NewNodeList(default(Slice<Node>))));
        var newline = FormatOptions().NewLineCharacter;
        var printer = createSnippetPrinter(new PrinterOptions { RemoveComments = true, NewLine = CompilerOptionHelpers.GetNewLineKind(newline) }, null);
        var printed = Core.MapIndexSlice(newClauses, (clause, index) => clientSupportsItemSnippet(context) ? completionConcat(printer.printAndFormatNode(context, clause, file), ByteStringValue.FromString("$"), ByteStringValue.FromString((index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture))) : printer.printUnescapedNode(clause));
        var insert = GoStrings.Join(printed, ByteStringValue.FromString(newline));
        var name = completionConcat(printer.printUnescapedNode(newClauses[0]), ByteStringValue.FromString(" ..."));
        Slice<TextEdit>? edits = null;
        if (adder != null)
        {
            var additions = adder.Edits();
            if (additions.Length != 0)
                edits = additions;
        }

        return (new TypeScript.Lsp.Protocol.CompletionItem { Label = name, Kind = CompletionItemKind.Snippet, SortText = CompletionSortText.GlobalsOrKeywords, InsertText = insert, AdditionalTextEdits = edits.HasValue ? nullableTextEditHeader(edits.Value) : null, InsertTextFormat = clientSupportsItemSnippet(context) ? InsertTextFormat.Snippet : null, Data = new CompletionItemData { FileName = file.OriginalFileName().AsValue(), Position = position, SupplementalFileIndex = supplementalFileIndex(file), Name = name, Source = CompletionSource.SwitchCases }, }, null);
    }
}

// Source assembly component: CompletionSnippetPrinter.cs
internal sealed partial class LanguageService
{
    private sealed class SnippetPrinter
    {
        internal required ChangeTrackerWriter baseWriter;
        internal required EmitContext emitContext;
        internal required Printer printer;
        internal required SnippetEmitTextWriter writer;
        internal required NodeFactory factory;
        internal ByteStringValue printNode(Node node)
        {
            var unescaped = printUnescapedNode(node);
            return writer.escapes.Length > 0 ? Core.ApplyBulkEdits(unescaped, writer.escapes) : unescaped;
        }

        internal ByteStringValue printUnescapedNode(Node node)
        {
            writer.escapes = default;
            writer.Clear();
            printer.Write(node, null, writer, null);
            return writer.TextValue();
        }

        internal ByteStringValue printAndFormatNode(ProtocolRequestContext context, Node node, SourceFile file) => printAndFormatNodeWithSettings(context, node, file, Format.GetFormatCodeSettingsFromContext(context));
        internal ByteStringValue printAndFormatNodeWithSettings(ProtocolRequestContext context, Node node, SourceFile file, FormatCodeSettings settings)
        {
            var text = printUnescapedNode(node);
            var positioned = baseWriter.AssignPositionsToNode(node, emitContext.Factory);
            var synthetic = SyntheticFile.CreateSyntheticSourceFile(emitContext.Factory, positioned, text, file.ParseOptions());
            context = Format.WithFormatCodeSettings(context, settings, ByteStringValue.FromString(settings.NewLineCharacter));
            var changes = Format.FormatNodeGivenIndentation(context, positioned, synthetic, file.LanguageVariant, 0, 0);
            var allChanges = changes;
            if (writer.escapes.Length > 0)
            {
                allChanges = changes.Append(writer.escapes);
                GoSliceSort.SortFunc(allChanges.AsSpan(), static (a, b) => Core.CompareTextRanges(a.TextRange, b.TextRange));
            }

            return Core.ApplyBulkEdits(ByteStringValue.FromView(synthetic.TextView()), allChanges);
        }
    }

    private static SnippetPrinter createSnippetPrinter(PrinterOptions options, EmitContext? context)
    {
        context ??= new EmitContext();
        var baseWriter = ChangeTrackerWriter.NewChangeTrackerWriter(options.NewLine.GetNewLineCharacter(), -1);
        var printer = Printer.NewPrinter(options, baseWriter.GetPrintHandlers(), context);
        return new SnippetPrinter
        {
            baseWriter = baseWriter,
            emitContext = context,
            printer = printer,
            writer = new SnippetEmitTextWriter(baseWriter),
            factory = context.Factory.AsNodeFactory()
        };
    }
}

// Source assembly component: CompletionListDefaults.cs
internal sealed partial class LanguageService
{
    private CompletionItemDefaults? setItemDefaults(ProtocolRequestContext context, int position, SourceFile file, Slice<CompletionItem?> items, ref Slice<ByteStringValue> commits, Protocol.Range? replacement)
    {
        CompletionItemDefaults? defaults = null;
        bool supportsCommits = clientSupportsItemCommitCharacters(context);
        if (clientSupportsDefaultCommitCharacters(context) && supportsCommits)
            defaults = new CompletionItemDefaults
            {
                CommitCharacters = commits
            };
        else if (supportsCommits)
            foreach (var wrapper in items)
                if (wrapper!.Item!.CommitCharacters == null)
                    wrapper.Item.CommitCharacters = commits;
        if (replacement != null)
        {
            var(end, fidelity) = createLspPosition(position, file);
            if (!fidelity.IsExact())
                return defaults;
            var insert = new Protocol.Range
            {
                Start = replacement.Value.Start,
                End = end
            };
            if (clientSupportsDefaultEditRange(context))
            {
                defaults ??= new CompletionItemDefaults();
                defaults.EditRange = new RangeOrEditRangeWithInsertReplace
                {
                    EditRangeWithInsertReplace = new EditRangeWithInsertReplace
                    {
                        Insert = insert,
                        Replace = replacement.Value
                    }
                };
                foreach (var wrapper in items)
                {
                    var item = wrapper!.Item!;
                    if (item.InsertText != null && item.TextEdit == null)
                    {
                        item.TextEdit = new TextEditOrInsertReplaceEdit
                        {
                            InsertReplaceEdit = new InsertReplaceEdit
                            {
                                NewText = item.InsertText.Value,
                                Insert = insert,
                                Replace = replacement.Value
                            }
                        };
                        item.InsertText = null;
                    }
                }
            }
            else if (clientSupportsItemInsertReplace(context))
                foreach (var wrapper in items)
                {
                    var item = wrapper!.Item!;
                    if (item.TextEdit == null)
                        item.TextEdit = new TextEditOrInsertReplaceEdit
                        {
                            InsertReplaceEdit = new InsertReplaceEdit
                            {
                                NewText = item.InsertText ?? item.Label,
                                Insert = insert,
                                Replace = replacement.Value
                            }
                        };
                }
        }

        return defaults;
    }

    private CompletionList specificKeywordCompletionInfo(ProtocolRequestContext context, int position, SourceFile file, Slice<CompletionItem?> items, bool newIdentifier, Protocol.Range? replacement)
    {
        var commits = getDefaultCommitCharacters(newIdentifier);
        return new CompletionList
        {
            IsIncomplete = false,
            ItemDefaults = setItemDefaults(context, position, file, items, ref commits, replacement),
            Items = items
        };
    }

    private CompletionList? getJsxClosingTagCompletion(ProtocolRequestContext context, Node location, SourceFile file, int position)
    {
        var closing = AST.FindAncestorOrQuit(location, static node => node.Kind switch
        {
            Kind.JsxClosingElement => FindAncestorResult.True,
            Kind.LessThanSlashToken or Kind.GreaterThanToken or Kind.Identifier or Kind.PropertyAccessExpression => FindAncestorResult.False,
            _ => FindAncestorResult.Quit,
        });
        if (closing == null)
            return null;
        bool hasAngle = AstNav.FindChildOfKind(closing, Kind.GreaterThanToken, file) != null;
        var name = closing.Parent!.AsJsxElement().OpeningElement.TagName();
        var fullName = completionConcat(ScannerApi.GetTextOfNodeValue(name), ByteStringValue.FromString(hasAngle ? "" : ">"));
        var(replacement, fidelity) = createLspRangeFromNode(closing.TagName(), file);
        if (!fidelity.IsExact())
            return null;
        var commits = getDefaultCommitCharacters(false);
        var item = createLSPCompletionItem(context, fullName, default, default, CompletionSortText.LocationPriority, ScriptElementKind.ClassElement, ScriptElementKindModifier.None, null, null, null, file, position, true, false, false, false, default, null, null, null);
        var items = Slice<CompletionItem?>.FromArray([new CompletionItem { Item = item }]);
        return new CompletionList
        {
            IsIncomplete = false,
            ItemDefaults = setItemDefaults(context, position, file, items, ref commits, replacement),
            Items = items
        };
    }
}

// Source assembly component: CompletionEntries.cs
internal sealed partial class LanguageService
{
    private (CompletionList? , Exception? ) completionInfoFromData(ProtocolRequestContext context, Checker checker, SourceFile file, CompilerOptions options, CompletionDataData data, int position, Protocol.Range? replacementSpan, bool includeSymbols)
    {
        var filters = data.keywordFilters;
        var contextToken = data.contextToken;
        var literals = data.literals;
        var preferences = UserPreferences();
        if (file.LanguageVariant == LanguageVariant.JSX)
        {
            var closing = getJsxClosingTagCompletion(context, data.location, file, position);
            if (closing != null)
                return (closing, null);
        }

        var caseClause = AST.FindAncestor(contextToken, AST.IsCaseClause);
        if (caseClause != null && (contextToken!.Kind == Kind.CaseKeyword || AST.IsNodeDescendantOf(contextToken, caseClause.Expression())))
        {
            var tracker = newCaseClauseTracker(checker, caseClause.Parent!.AsCaseBlock().Clauses.Nodes);
            literals = Core.Filter(literals, literal => !tracker.hasValue(literal));
            data.symbols = Core.Filter(data.symbols, symbol =>
            {
                if (symbol.ValueDeclaration != null && AST.IsEnumMember(symbol.ValueDeclaration))
                {
                    var value = checker.GetConstantValue(symbol.ValueDeclaration);
                    if (value != null && tracker.hasValue(value))
                        return false;
                }

                return true;
            });
        }

        bool checkedFile = isCheckedFile(file, options);
        if (checkedFile && !data.isNewIdentifierLocation && data.symbols.Length == 0 && filters == KeywordCompletionFilters.None)
            return (null, null);
        var(uniqueNames, entries, error) = getCompletionEntriesFromSymbols(context, checker, data, null, position, file, options, includeSymbols);
        if (error != null)
            return (null, error);
        if (filters != KeywordCompletionFilters.None)
        {
            foreach (var entry in getKeywordCompletions(filters, !data.insideJSDocTagTypeExpression && AST.IsSourceFileJS(file)))
            {
                var label = entry!.Item!.Label;
                if (data.isTypeOnlyLocation && isTypeKeyword(ScannerApi.StringToToken(label)) || !data.isTypeOnlyLocation && isContextualKeywordInAutoImportableExpressionSpace(label) || uniqueNames?.Contains(ByteStringKey.FromValue(label)) != true)
                {
                    (uniqueNames ??= new()).Add(ByteStringKey.FromValue(label));
                    entries = entries.Append(entry);
                }
            }
        }

        foreach (var entry in getContextualKeywords(file, contextToken, position))
            if (uniqueNames?.Contains(ByteStringKey.FromValue(entry!.Label)) != true)
            {
                (uniqueNames ??= new()).Add(ByteStringKey.FromValue(entry!.Label));
                entries = entries.Append(new CompletionItem { Item = entry });
            }

        foreach (var literal in literals)
        {
            var entry = createCompletionItemForLiteral(file, preferences, literal);
            (uniqueNames ??= new()).Add(ByteStringKey.FromValue(entry!.Label));
            entries = entries.Append(new CompletionItem { Item = entry });
        }

        if (!checkedFile)
            entries = getJSCompletionEntries(context, file, position, ref uniqueNames, entries);
        if (contextToken != null && !data.isRightOfOpenTag && !data.isRightOfDotOrQuestionDot)
        {
            var block = AST.FindAncestorKind(contextToken, Kind.CaseBlock);
            if (block != null)
            {
                var(item, caseError) = getExhaustiveCaseSnippets(context, block.AsCaseBlock(), file, position, options, program, checker);
                if (caseError != null)
                    return (null, caseError);
                if (item != null)
                    entries = entries.Append(new CompletionItem { Item = item });
            }
        }

        var defaults = setItemDefaults(context, position, file, entries, ref data.defaultCommitCharacters, replacementSpan);
        return (new CompletionList { IsIncomplete = data.hasUnresolvedAutoImports, ItemDefaults = defaults, Items = entries }, null);
    }

    private (HashSet<ByteStringKey>? , Slice<CompletionItem?>, Exception? ) getCompletionEntriesFromSymbols(ProtocolRequestContext context, Checker checker, CompletionDataData data, Node? replacementToken, int position, SourceFile file, CompilerOptions options, bool includeSymbols)
    {
        var closest = getClosestSymbolDeclaration(data.contextToken, data.location);
        bool semicolons = LsUtil.ProbablyUsesSemicolons(file);
        var preferences = UserPreferences();
        bool member = isMemberCompletionKind(data.completionKind);
        Slice<CompletionItem?> entries = default;
        entries = entries.Grow(data.symbols.Length + data.autoImports.Length);
        var uniques = new Dictionary<ByteStringKey, bool>();
        for (int index = 0; index < data.symbols.Length; index++)
        {
            var symbol = data.symbols[index];
            SymbolOriginInfo? origin = null;
            data.symbolToOriginInfoMap?.TryGetValue(index, out origin);
            var(name, convert) = getCompletionEntryDisplayNameForSymbol(file, preferences, symbol, origin, data.completionKind, data.isJsxIdentifierExpected);
            if (name.Length == 0 || uniques.GetValueOrDefault(ByteStringKey.FromValue(name)) && (origin == null || !originIsObjectLiteralMethod(origin)) || data.completionKind == CompletionKind.Global && !shouldIncludeSymbol(symbol, data, closest, file, checker, options))
                continue;
            if (!data.isTypeOnlyLocation && AST.IsSourceFileJS(file) && symbolAppearsToBeTypeOnly(symbol, checker))
                continue;
            ByteStringValue originalSort = default;
            data.symbolToSortTextMap?.TryGetValue(AST.GetSymbolId(symbol), out originalSort);
            if (originalSort.Length == 0)
                originalSort = CompletionSortText.LocationPriority;
            var sort = isDeprecated(symbol, checker) ? CompletionSortText.DeprecateSortText(originalSort) : originalSort;
            var(entry, error) = createCompletionItem(context, checker, symbol, sort, replacementToken, data, position, file, name, convert, origin, semicolons, options, member);
            if (error != null)
                return (null, default, error);
            if (entry == null)
                continue;
            bool shadow = (origin == null || originIsTypeOnlyAlias(origin)) && !(symbol.Parent == null && !Core.Some(symbol.Declarations, declaration => AST.GetSourceFileOfNode(declaration) == file));
            uniques[ByteStringKey.FromValue(name)] = shadow;
            entries = entries.Append(new CompletionItem { Item = entry, Symbol = includeSymbols ? symbol : null });
        }

        foreach (var autoImport in data.autoImports)
        {
            Protocol.Range? replacement = null;
            ByteStringValue insert = default, filter = default;
            bool snippet = false;
            var sort = CompletionSortText.AutoImportSuggestions;
            if (data.importStatementCompletion != null)
            {
                snippet = clientSupportsItemSnippet(context);
                (insert, replacement) = getInsertTextAndReplacementSpanForImportCompletion(autoImport.Fix, AutoImport.AutoImport.GetImportKindForImportStatement(file, autoImport.Export, GetProgram()), data.importStatementCompletion, semicolons, file, preferences, snippet);
                filter = insert;
                sort = CompletionSortText.LocationPriority;
            }

            var token = ScannerApi.StringToToken(autoImport.Fix.Name);
            if (token != Kind.Unknown && AST.IsNonContextualKeyword(token))
                continue;
            if (!autoImport.Export.IsUnresolvedAlias())
            {
                if (data.isTypeOnlyLocation)
                {
                    if ((autoImport.Export.Flags & (SymbolFlags.Type | SymbolFlags.Module)) == 0)
                        continue;
                }
                else if (data.importStatementCompletion == null && (autoImport.Export.Flags & SymbolFlags.Value) == 0)
                    continue;
            }

            var entry = createLSPCompletionItem(context, autoImport.Fix.Name, insert, filter, sort, autoImport.Export.ScriptElementKind, autoImport.Export.ScriptElementKindModifiers, replacement, null, new CompletionItemLabelDetails { Description = autoImport.Fix.ModuleSpecifier }, file, position, false, snippet, data.importStatementCompletion == null, false, autoImport.Fix.ModuleSpecifier, autoImport.Fix.AutoImportFix, default, null);
            entry.Data!.IsImportStatementCompletion = data.importStatementCompletion != null;
            if (!uniques.GetValueOrDefault(ByteStringKey.FromValue(autoImport.Fix.Name)))
            {
                uniques[ByteStringKey.FromValue(autoImport.Fix.Name)] = false;
                entries = entries.Append(new CompletionItem { Item = entry });
            }
        }

        var uniqueSet = new HashSet<ByteStringKey>(uniques.Count);
        foreach (var name in uniques.Keys)
            uniqueSet.Add(name);
        return (uniqueSet, entries, null);
    }

    private static (ByteStringValue, Protocol.Range? ) getInsertTextAndReplacementSpanForImportCompletion(AutoImport.Fix fix, ImportKind kind, ImportStatementCompletionInfo statement, bool semicolons, SourceFile file, UserPreferences preferences, bool snippet)
    {
        var quoted = escapeSnippetText(quote(file, preferences, fix.ModuleSpecifier));
        var stop = ByteStringValue.FromString(snippet ? "$1" : "");
        var suffix = ByteStringValue.FromString(semicolons ? ";" : "");
        var top = statement.isTopLevelTypeOnly ? ByteStringValue.Concat(ByteStringValue.FromString(" "), ByteStringValue.FromString(ScannerApi.TokenToString(Kind.TypeKeyword)), ByteStringValue.FromString(" ")) : ByteStringValue.FromString(" ");
        var name = escapeSnippetText(fix.Name);
        var output = new GoBuilder();
        output.WriteString("import");
        output.WriteString(top);
        switch (kind)
        {
            case ImportKind.CommonJS:
                output.WriteString(name);
                output.WriteString(stop);
                output.WriteString(" = require(");
                output.WriteString(quoted);
                output.WriteString(")");
                break;
            case ImportKind.Default:
                output.WriteString(name);
                output.WriteString(stop);
                output.WriteString(" from ");
                output.WriteString(quoted);
                break;
            case ImportKind.Namespace:
                output.WriteString("* as ");
                output.WriteString(name);
                output.WriteString(" from ");
                output.WriteString(quoted);
                break;
            case ImportKind.Named:
                output.WriteString("{ ");
                if (statement.couldBeTypeOnlyImportSpecifier)
                {
                    output.WriteString(ByteStringValue.FromString(ScannerApi.TokenToString(Kind.TypeKeyword)));
                    output.WriteString(" ");
                }

                output.WriteString(name);
                output.WriteString(stop);
                output.WriteString(" } from ");
                output.WriteString(quoted);
                break;
            default:
                throw new InvalidOperationException("unhandled import kind: " + kind.String());
        }

        output.WriteString(suffix);
        return (output.Value(), statement.replacementSpan);
    }

    private static Protocol.CompletionItem createCompletionItemForLiteral(SourceFile file, UserPreferences preferences, object literal) => new()
    {
        Label = completionNameForLiteral(file, preferences, literal),
        Kind = CompletionItemKind.Constant,
        SortText = CompletionSortText.LocationPriority,
        CommitCharacters = Slice<ByteStringValue>.Make(0),
    };
}

// Source assembly component: Completions.cs
internal sealed partial class LanguageService
{
    internal static readonly Slice<ByteStringValue> CompletionTriggerCharacters = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("."), ByteStringValue.FromString("\""), ByteStringValue.FromString("'"), ByteStringValue.FromString("`"), ByteStringValue.FromString("/"), ByteStringValue.FromString("@"), ByteStringValue.FromString("<"), ByteStringValue.FromString("#"), ByteStringValue.FromString(" "), ByteStringValue.FromString("*")]);
    private static readonly Slice<ByteStringValue> allCommitCharacters = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("."), ByteStringValue.FromString(","), ByteStringValue.FromString(";")]);
    private static readonly Slice<ByteStringValue> noCommaCommitCharacters = Slice<ByteStringValue>.FromArray([ByteStringValue.FromString("."), ByteStringValue.FromString(";")]);
    private static readonly Slice<ByteStringValue> emptyCommitCharacters = Slice<ByteStringValue>.Make(0);
    internal (CompletionItemsOrListOrNull Result, Exception? Error) ProvideCompletion(ProtocolRequestContext context, DocumentUri uri, Protocol.Position lspPosition, CompletionContext? completionContext)
    {
        var(program, file) = getProgramAndFile(uri);
        ByteStringValue? triggerCharacter = completionContext?.TriggerCharacter;
        context = context.WithFormatCodeSettings(FormatOptions(), ByteStringValue.FromString(FormatOptions().NewLineCharacter));
        var positions = converters.FromLSPPositionForSourceFile(file, lspPosition, Feature.Completion);
        if (positions.Length == 0 || !positions[0].Fidelity.IsExact())
            return (new(), null);
        file = positions[0].Script;
        int position = positions[0].Position;
        var(internalList, error) = getCompletionsAtPosition(context, file, position, triggerCharacter, false);
        if (error != null)
            return (new(), error);
        var list = ensureItemData(file, position, CompletionList.ToLSP(internalList));
        if (file.SpanMap() != null)
            filterContentMappedAutoImports(context, program, file, list);
        return (new CompletionItemsOrListOrNull { List = list }, null);
    }

    private void filterContentMappedAutoImports(ProtocolRequestContext context, Program program, SourceFile file, Protocol.CompletionList? list)
    {
        if (list == null)
            return;
        var filtered = list.Items.Slice2(0, 0);
        foreach (var nullableItem in list.Items)
        {
            var item = nullableItem!;
            if (item.Data?.AutoImport == null)
            {
                filtered = filtered.Append(item);
                continue;
            }

            var fix = new Fix
            {
                AutoImportFix = item.Data.AutoImport
            };
            var(edits, description, ok) = fix.Edits(context, file, program.Options(), FormatOptions(), converters, UserPreferences());
            if (!ok)
                continue;
            item.AdditionalTextEdits = nullableTextEditHeader(edits);
            item.Detail = description;
            filtered = filtered.Append(item);
        }

        list.Items = filtered;
    }

    internal (CompletionList? Result, Exception? Error) GetCompletionsAtPosition(ProtocolRequestContext context, SourceFile file, int position, ByteStringValue? triggerCharacter, bool includeSymbols) => getCompletionsAtPosition(context, file, position, triggerCharacter, includeSymbols);
    private static Protocol.CompletionList? ensureItemData(SourceFile file, int position, Protocol.CompletionList? list)
    {
        if (list == null)
            return null;
        foreach (var nullableItem in list.Items)
        {
            var item = nullableItem!;
            item.Data ??= new CompletionItemData
            {
                FileName = file.OriginalFileName().AsValue(),
                Position = position,
                SupplementalFileIndex = supplementalFileIndex(file),
                Name = item.Label,
            };
        }

        return list;
    }

    private static KeywordCompletionFilters keywordFiltersFromSyntaxKind(Kind keyword) => keyword == Kind.TypeKeyword ? KeywordCompletionFilters.TypeKeyword : throw new InvalidOperationException("Unknown mapping from ast.Kind `" + keyword.String() + "` to KeywordCompletionFilters");
    private (CompletionList? Result, Exception? Error) getCompletionsAtPosition(ProtocolRequestContext context, SourceFile file, int position, ByteStringValue? trigger, bool includeSymbols)
    {
        var(_, previousToken) = getRelevantTokens(position, file);
        if (trigger is { } character && !IsInString(file, position, previousToken) && !isValidTrigger(file, character, previousToken, position))
            return (null, null);
        if (trigger is { } space && space.EqualsString(" "))
            return (UserPreferences().IncludeCompletionsForImportStatements == Tristate.True ? new CompletionList { IsIncomplete = true } : null, null);
        var jsdoc = getJSDocSnippetCompletion(context, file, position);
        if (jsdoc != null)
            return (jsdoc, null);
        var options = GetProgram().Options();
        var(checker, done) = GetProgram().GetTypeCheckerForFile(context, file);
        try
        {
            var strings = getStringLiteralCompletions(context, file, position, previousToken, checker, options, includeSymbols);
            if (strings != null)
                return (strings, null);
            if (previousToken != null && previousToken!.Kind is Kind.BreakKeyword or Kind.ContinueKeyword or Kind.Identifier && Ast.IsBreakOrContinueStatement(previousToken.Parent))
                return (getLabelCompletionsAtPosition(context, previousToken.Parent, file, position, getOptionalReplacementSpan(previousToken, file)), null);
            var preferences = UserPreferences();
            var(data, error) = getCompletionData(context, checker, file, position, preferences, false);
            if (error != null)
                return (null, error);
            if (data == null)
                return (null, null);
            switch (data)
            {
                case CompletionDataData symbols:
                    return completionInfoFromData(context, checker, file, options, symbols, position, getOptionalReplacementSpan(symbols.location, file), includeSymbols);
                case CompletionDataKeyword keywords:
                    return (specificKeywordCompletionInfo(context, position, file, keywords.keywordCompletions, keywords.isNewIdentifierLocation, getOptionalReplacementSpan(previousToken, file)), null);
                case CompletionDataJSDocTagName:
                    var names = getJSDocTagNameCompletions().Append(getJSDocParameterCompletions(context, file, position, checker, options, preferences, true));
                    return (jsDocCompletionInfo(context, position, file, names), null);
                case CompletionDataJSDocTag:
                    var tags = getJSDocTagCompletions().Append(getJSDocParameterCompletions(context, file, position, checker, options, preferences, false));
                    return (jsDocCompletionInfo(context, position, file, tags), null);
                case CompletionDataJSDocParameterName parameters:
                    return (jsDocCompletionInfo(context, position, file, getJSDocParameterNameCompletions(parameters.tag)), null);
                default:
                    throw new InvalidOperationException("getCompletionData() returned unexpected type: " + completionDataTypeName(data));
            }
        }
        finally
        {
            done();
        }
    }

    private static string completionDataTypeName(CompletionData data) => data switch
    {
        CompletionDataData => "*ls.completionDataData",
        CompletionDataKeyword => "*ls.completionDataKeyword",
        CompletionDataJSDocTagName => "*ls.completionDataJSDocTagName",
        CompletionDataJSDocTag => "*ls.completionDataJSDocTag",
        CompletionDataJSDocParameterName => "*ls.completionDataJSDocParameterName",
        _ => throw new InvalidOperationException("Unknown completion data owner"),
    };
}

internal sealed partial class LanguageService
{
    private static Slice<Symbol> checkCompletionSymbolsDefined(Slice<Symbol> symbols, string message)
    {
        foreach (var symbol in symbols)
            if (symbol is null)
                throw new InvalidOperationException(message);
        return symbols;
    }
}

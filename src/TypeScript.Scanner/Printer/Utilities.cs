// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/printer/utilities.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.SourceMap;
using ByteScanner = TypeScript.Scanner.Scanner;
using static TypeScript.DiagnosticPrinting.PrinterUtilities;
namespace TypeScript.DiagnosticPrinting;
[Flags]
internal enum getLiteralTextFlags { None=0, NeverAsciiEscape=1, JsxAttributeEscape=2, TerminateUnterminatedLiterals=4, AllowNumericSeparator=8 }
internal enum QuoteChar { SingleQuote='\'', DoubleQuote='"', Backtick='`' }
internal static partial class PrinterUtilities
{
    // Managed AST interfaces expose the external string property; concrete payloads
    // retain Go byte headers, so this embedding join borrows the existing owner.
    internal static ByteStringValue templateRawTextValue(Node node) => node.Kind switch
    {
        Kind.NoSubstitutionTemplateLiteral => node.AsNoSubstitutionTemplateLiteral().RawTextValue,
        Kind.TemplateHead => node.AsTemplateHead().RawTextValue,
        Kind.TemplateMiddle => node.AsTemplateMiddle().RawTextValue,
        Kind.TemplateTail => node.AsTemplateTail().RawTextValue,
        _ => throw new InvalidOperationException("Expected template literal"),
    };

    internal static bool containsFunc<T>(Slice<T> values, Func<T, bool> test)
    {
        foreach (var value in values) if (test(value)) return true;
        return false;
    }
    internal static Slice<T> collect<T>(IEnumerable<T> values)
    {
        Slice<T> result = default;
        foreach (var value in values) result = result.Append(value);
        return result;
    }

    private static readonly Dictionary<int,string> jsxEscapedCharsMap = new() { ['"']="&quot;", ['\'']="&apos;" };
    private static readonly Dictionary<int,string> escapedCharsMap = new()
    {
        ['\t']="\\t", ['\v']="\\v", ['\f']="\\f", ['\b']="\\b", ['\r']="\\r", ['\n']="\\n", ['\\']="\\\\", ['"']="\\\"", ['\'']="\\'", ['`']="\\`", ['$']="\\$", [0x2028]="\\u2028", [0x2029]="\\u2029", [0x85]="\\u0085",
    };
    internal static (int End,bool Ok) tryGetEnd(object node) => node switch
    {
        Node value => (value.End(),true), NodeList value => (value.End(),true), TextRange value => (value.End(),true),
        null => (0,false), _ => throw new InvalidOperationException("unhandled type: "+node.GetType().Name),
    };
    internal static int greatestEnd(int end, params object[] nodes)
    {
        for (int i=nodes.Length-1; i>=0; i--) { var (nodeEnd,ok)=tryGetEnd(nodes[i]); if (ok && end<nodeEnd) end=nodeEnd; }
        return end;
    }
    internal static string EscapeNonAsciiString(string text, QuoteChar quote) => escapeNonAsciiString(text,quote);
    internal static string EscapeJsxAttributeString(string text, QuoteChar quote) => escapeJsxAttributeString(text,quote);
// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:58:1.
internal static void encodeJsxCharacterEntity(GoBuilder b, int charCode)
{
    var hexCharCode = Strconv.FormatUint((ulong)(charCode), 16).ToUpperInvariant();
    b.WriteString("&#x");
    b.WriteString(hexCharCode);
    b.WriteByte(';');
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:65:1.
internal static void encodeUtf16EscapeSequence(GoBuilder b, int charCode)
{
    var hexCharCode = Strconv.FormatUint((ulong)(charCode), 16).ToUpperInvariant();
    b.WriteString("\\u");
    {
        var i = GoRuntime.Len(hexCharCode);
        for (; (i < 4); i++)
        {
            b.WriteByte('0');
        }
    }
    b.WriteString(hexCharCode);
}

// Based heavily on the abstract 'Quote'/'QuoteJSONString' operation from ECMA-262 (24.3.2.2),
// but augmented for a few select characters (e.g. lineSeparator, paragraphSeparator, nextLine)
// Note that this doesn't actually wrap the input in double quotes.
// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:77:1.
internal static void escapeStringWorker(ByteStringValue s, QuoteChar quoteChar, getLiteralTextFlags flags, GoBuilder b)
{
    var pos = 0;
    var i = 0;
    while ((i < s.Length))
    {
        var tuple1 = StringUtil.DecodeJSStringRune(s.Slice(i, s.Length)); var ch = tuple1.Item1; var size = tuple1.Item2;
        var escape = false;
        if (((ch >= 0xD800) && (ch <= 0xDFFF)))
        {
            escape = true;
        }
        else
        {
            if (((ch == Utf8.RuneError) && (size == 1)))
            {
                escape = true;
            }
        }
        switch (ch)
        {
            case '\\':
            {
                if (((flags & getLiteralTextFlags.JsxAttributeEscape) == 0))
                {
                    escape = true;
                }
                break;
            }
            case '$':
            {
                if ((((quoteChar == QuoteChar.Backtick) && ((i + 1) < s.Length)) && (s[(i + 1)] == '{')))
                {
                    escape = true;
                }
                break;
            }
            case int quoteCode when quoteCode == (int)quoteChar:
            case '\u2028':
            case '\u2029':
            case '\u0085':
            case '\r':
            {
                escape = true;
                break;
            }
            case '\n':
            {
                if ((quoteChar != QuoteChar.Backtick))
                {
                    escape = true;
                }
                break;
            }
            default:
            {
                if (((ch <= '\u001f') || (((flags & getLiteralTextFlags.NeverAsciiEscape) == 0) && (ch > '\u007f'))))
                {
                    escape = true;
                }
                break;
            }
        }
        if (escape)
        {
            if ((pos < i))
            {
                b.WriteString(s.Slice(pos, i));
            }
            switch (true)
            {
                case true when ((flags & getLiteralTextFlags.JsxAttributeEscape) != 0):
                {
                    if ((ch == 0))
                    {
                        b.WriteString("&#0;");
                    }
                    else
                    {
                        {
                            var ok = jsxEscapedCharsMap.TryGetValue(ch, out var match);
                            if (ok)
                            {
                                b.WriteString(match);
                            }
                            else
                            {
                                encodeJsxCharacterEntity(b, ch);
                            }
                        }
                    }
                    break;
                }
                default:
                {
                    if (((((ch == '\r') && (quoteChar == QuoteChar.Backtick)) && ((i + 1) < s.Length)) && (s[(i + 1)] == '\n')))
                    {
                        size++;
                        b.WriteString("\\r\\n");
                    }
                    else
                    {
                        if ((ch > 0xffff))
                        {
                            ch -= 0x10000;
                            encodeUtf16EscapeSequence(b, ((((ch & 0b11111111110000000000) >> (int)(10))) + 0xD800));
                            encodeUtf16EscapeSequence(b, (((ch & 0b00000000001111111111)) + 0xDC00));
                        }
                        else
                        {
                            if (((ch >= 0xD800) && (ch <= 0xDFFF)))
                            {
                                encodeUtf16EscapeSequence(b, ch);
                            }
                            else
                            {
                                if ((ch == 0))
                                {
                                    if ((((i + 1) < s.Length) && StringUtil.IsDigit((int)(s[(i + 1)]))))
                                    {
                                        b.WriteString("\\x00");
                                    }
                                    else
                                    {
                                        b.WriteString("\\0");
                                    }
                                }
                                else
                                {
                                    {
                                        var ok2 = escapedCharsMap.TryGetValue(ch, out var match2);
                                        if (ok2)
                                        {
                                            b.WriteString(match2);
                                        }
                                        else
                                        {
                                            encodeUtf16EscapeSequence(b, ch);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
            }
            pos = (i + size);
        }
        i += size;
    }
    if ((pos < i))
    {
        b.WriteString(s.Slice(pos, s.Length));
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:178:1.
public static string EscapeString(string s, QuoteChar quoteChar)
{
    GoBuilder b = new GoBuilder();
    b.Grow((s.Length + 2));
    escapeStringWorker(ByteStringValue.FromString(s), quoteChar, getLiteralTextFlags.NeverAsciiEscape, b);
    return b.String();
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:185:1.
internal static string escapeNonAsciiString(string s, QuoteChar quoteChar)
{
    GoBuilder b = new GoBuilder();
    b.Grow((s.Length + 2));
    escapeStringWorker(ByteStringValue.FromString(s), quoteChar, getLiteralTextFlags.None, b);
    return b.String();
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:192:1.
internal static string escapeJsxAttributeString(string s, QuoteChar quoteChar)
{
    GoBuilder b = new GoBuilder();
    b.Grow((s.Length + 2));
    escapeStringWorker(ByteStringValue.FromString(s), quoteChar, (getLiteralTextFlags.JsxAttributeEscape | getLiteralTextFlags.NeverAsciiEscape), b);
    return b.String();
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:199:1.
internal static bool canUseOriginalText(Node node, getLiteralTextFlags flags)
{
    if (((Ast.NodeIsSynthesized(node) || (node.Parent == null)) || (((flags & getLiteralTextFlags.TerminateUnterminatedLiterals) != 0) && Ast.IsUnterminatedLiteral(node))))
    {
        return false;
    }
    if ((node.Kind == Kind.NumericLiteral))
    {
        var tokenFlags = node.AsNumericLiteral().TokenFlags;
        if (((tokenFlags & TokenFlags.IsInvalid) != 0))
        {
            return false;
        }
        if (((tokenFlags & TokenFlags.ContainsSeparator) != 0))
        {
            return ((flags & getLiteralTextFlags.AllowNumericSeparator) != 0);
        }
    }
    return (node.Kind != Kind.BigIntLiteral);
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:227:1.
internal static ByteStringValue getLiteralTextValue(Node node, SourceFile sourceFile, getLiteralTextFlags flags)
{
    if (((sourceFile != null) && canUseOriginalText(node, flags)))
    {
        return ByteScanner.GetTextOfNodeFromSourceTextValue(sourceFile.TextView(), node, false);
    }
    switch (node.Kind)
    {
        case Kind.StringLiteral:
        {
            GoBuilder b = new GoBuilder();
            QuoteChar quoteChar = default;
            if (((node.AsStringLiteral().TokenFlags & TokenFlags.SingleQuote) != 0))
            {
                quoteChar = QuoteChar.SingleQuote;
            }
            else
            {
                quoteChar = QuoteChar.DoubleQuote;
            }
            var text = node.TextValue();
            b.Grow((text.Length + 2));
            b.WriteRune((int)(quoteChar));
            escapeStringWorker(text, quoteChar, flags, b);
            b.WriteRune((int)(quoteChar));
            return b.Value();
        }
        case Kind.NoSubstitutionTemplateLiteral:
        case Kind.TemplateHead:
        case Kind.TemplateMiddle:
        case Kind.TemplateTail:
        {
            GoBuilder b2 = new GoBuilder();
            var text2 = node.TextValue();
            var rawText = templateRawTextValue(node);
            var raw = ((rawText.Length > 0) || (text2.Length == 0));
            int textLen = default;
            if (raw)
            {
                textLen = rawText.Length;
            }
            else
            {
                textLen = text2.Length;
            }
            switch (node.Kind)
            {
                case Kind.NoSubstitutionTemplateLiteral:
                {
                    b2.Grow((2 + textLen));
                    b2.WriteRune('`');
                    break;
                }
                case Kind.TemplateHead:
                {
                    b2.Grow((3 + textLen));
                    b2.WriteRune('`');
                    break;
                }
                case Kind.TemplateMiddle:
                {
                    b2.Grow((3 + textLen));
                    b2.WriteRune('}');
                    break;
                }
                case Kind.TemplateTail:
                {
                    b2.Grow((2 + textLen));
                    b2.WriteRune('}');
                    break;
                }
            }
            switch (true)
            {
                case true when ((rawText.Length > 0) || (text2.Length == 0)):
                {
                    b2.WriteString(rawText);
                    break;
                }
                default:
                {
                    escapeStringWorker(text2, QuoteChar.Backtick, flags, b2);
                    break;
                }
            }
            switch (node.Kind)
            {
                case Kind.NoSubstitutionTemplateLiteral:
                {
                    b2.WriteRune('`');
                    break;
                }
                case Kind.TemplateHead:
                {
                    b2.WriteString("${");
                    break;
                }
                case Kind.TemplateMiddle:
                {
                    b2.WriteString("${");
                    break;
                }
                case Kind.TemplateTail:
                {
                    b2.WriteRune('`');
                    break;
                }
            }
            return b2.Value();
        }
        case Kind.NumericLiteral:
        case Kind.BigIntLiteral:
        {
            return node.TextValue();
        }
        case Kind.RegularExpressionLiteral:
        {
            if ((((flags & getLiteralTextFlags.TerminateUnterminatedLiterals) != 0) && Ast.IsUnterminatedLiteral(node)))
            {
                GoBuilder b3 = new GoBuilder();
                var text3 = node.TextValue();
                if (((text3.Length > 0) && (text3[(text3.Length - 1)] == '\\')))
                {
                    b3.Grow((2 + text3.Length));
                    b3.WriteString(text3);
                    b3.WriteString(" /");
                }
                else
                {
                    b3.Grow((1 + text3.Length));
                    b3.WriteString(text3);
                    b3.WriteString("/");
                }
                return b3.Value();
            }
            return node.TextValue();
        }
        default:
        {
            throw new InvalidOperationException("Unsupported LiteralLikeNode");
        }
    }
}

internal static string getLiteralText(Node node, SourceFile sourceFile, getLiteralTextFlags flags) => getLiteralTextValue(node, sourceFile, flags).Materialize();

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:341:1.
internal static bool isNotPrologueDirective(Node node)
{
    return !Ast.IsPrologueDirective(node);
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:345:1.
public static bool RangeIsOnSingleLine(TextRange r, SourceFile sourceFile)
{
    return rangeStartIsOnSameLineAsRangeEnd(r, r, sourceFile);
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:349:1.
public static bool RangeStartPositionsAreOnSameLine(TextRange range1, TextRange range2, SourceFile sourceFile)
{
    return PositionsAreOnSameLine(getStartPositionOfRange(range1, sourceFile, false), getStartPositionOfRange(range2, sourceFile, false), sourceFile);
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:357:1.
internal static bool rangeEndPositionsAreOnSameLine(TextRange range1, TextRange range2, SourceFile sourceFile)
{
    return PositionsAreOnSameLine(range1.End(), range2.End(), sourceFile);
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:361:1.
internal static bool rangeStartIsOnSameLineAsRangeEnd(TextRange range1, TextRange range2, SourceFile sourceFile)
{
    return PositionsAreOnSameLine(getStartPositionOfRange(range1, sourceFile, false), range2.End(), sourceFile);
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:365:1.
internal static bool rangeEndIsOnSameLineAsRangeStart(TextRange range1, TextRange range2, SourceFile sourceFile)
{
    return PositionsAreOnSameLine(range1.End(), getStartPositionOfRange(range2, sourceFile, false), sourceFile);
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:369:1.
internal static int getStartPositionOfRange(TextRange r, SourceFile sourceFile, bool includeComments)
{
    if (Ast.PositionIsSynthesized(r.Pos()))
    {
        return -1;
    }
    return ByteScanner.SkipTriviaEx(sourceFile.TextView(), r.Pos(), new SkipTriviaOptions { StopAtComments = includeComments });
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:376:1.
public static bool PositionsAreOnSameLine(int pos1, int pos2, SourceFile sourceFile)
{
    return (GetLinesBetweenPositions(sourceFile, pos1, pos2) == 0);
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:380:1.
public static int GetLinesBetweenPositions(SourceFile sourceFile, int pos1, int pos2)
{
    if ((pos1 == pos2))
    {
        return 0;
    }
    var lineStarts = sourceFile.ECMALineMap();
    var lower = Core.IfElse((pos1 < pos2), pos1, pos2);
    var isNegative = (lower == pos2);
    var upper = Core.IfElse(isNegative, pos1, pos2);
    var lowerLine = ByteScanner.ComputeLineOfPosition(lineStarts.AsSpan(), lower);
    var upperLine = (lowerLine + ByteScanner.ComputeLineOfPosition(lineStarts.Slice2(lowerLine, lineStarts.Length).AsSpan(), upper));
    if (isNegative)
    {
        return (lowerLine - upperLine);
    }
    else
    {
        return (upperLine - lowerLine);
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:397:1.
internal static int getLinesBetweenRangeEndAndRangeStart(TextRange range1, TextRange range2, SourceFile sourceFile, bool includeSecondRangeComments)
{
    var range2Start = getStartPositionOfRange(range2, sourceFile, includeSecondRangeComments);
    return GetLinesBetweenPositions(sourceFile, range1.End(), range2Start);
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:402:1.
internal static int getLinesBetweenPositionAndPrecedingNonWhitespaceCharacter(int pos, int stopPos, SourceFile sourceFile, bool includeComments)
{
    var startPos = ByteScanner.SkipTriviaEx(sourceFile.TextView(), pos, new SkipTriviaOptions { StopAtComments = includeComments });
    var prevPos = getPreviousNonWhitespacePosition(startPos, stopPos, sourceFile);
    return GetLinesBetweenPositions(sourceFile, Core.IfElse((prevPos >= 0), prevPos, stopPos), startPos);
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:408:1.
internal static int getLinesBetweenPositionAndNextNonWhitespaceCharacter(int pos, int stopPos, SourceFile sourceFile, bool includeComments)
{
    var nextPos = ByteScanner.SkipTriviaEx(sourceFile.TextView(), pos, new SkipTriviaOptions { StopAtComments = includeComments });
    return GetLinesBetweenPositions(sourceFile, pos, Core.IfElse((stopPos < nextPos), stopPos, nextPos));
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:413:1.
internal static int getPreviousNonWhitespacePosition(int pos, int stopPos, SourceFile sourceFile)
{
    for (; (pos >= stopPos); pos--)
    {
        if (!StringUtil.IsWhiteSpaceLike((int)(sourceFile.TextView()[pos])))
        {
            return pos;
        }
    }
    return -1;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:422:1.
internal static bool siblingNodePositionsAreComparable(EmitContext emitContext, Node previousNode, Node nextNode)
{
    if ((nextNode.Pos() < previousNode.End()))
    {
        return false;
    }
    previousNode = emitContext.MostOriginal(previousNode);
    nextNode = emitContext.MostOriginal(nextNode);
    var parent = previousNode.Parent;
    if (((parent == null) || (parent != nextNode.Parent)))
    {
        return false;
    }
    var parentNodeArray = getContainingNodeArray(previousNode);
    if ((parentNodeArray != null))
    {
        var prevNodeIndex = GoSlices.Index(parentNodeArray.Nodes, previousNode);
        return ((prevNodeIndex >= 0) && (GoSlices.Index(parentNodeArray.Nodes, nextNode) == (prevNodeIndex + 1)));
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:443:1.
internal static NodeList getContainingNodeArray(Node node)
{
    var parent = node.Parent;
    if ((parent == null))
    {
        return null;
    }
    switch (node.Kind)
    {
        case Kind.TypeParameter:
        {
            switch (true)
            {
                case true when (((Ast.IsFunctionLike(parent) || Ast.IsClassLike(parent)) || Ast.IsInterfaceDeclaration(parent)) || Ast.IsTypeOrJSTypeAliasDeclaration(parent)):
                {
                    return parent.TypeParameterList();
                }
                case true when Ast.IsInferTypeNode(parent):
                {
                    break;
                }
                default:
                {
                    throw new InvalidOperationException(Fmt.Sprintf("Unexpected TypeParameter parent: %#v", parent.Kind));
                }
            }
            break;
        }
        case Kind.Parameter:
        {
            return node.Parent.FunctionLikeData().Parameters;
        }
        case Kind.TemplateLiteralTypeSpan:
        {
            return node.Parent.AsTemplateLiteralTypeNode().TemplateSpans;
        }
        case Kind.TemplateSpan:
        {
            return node.Parent.AsTemplateExpression().TemplateSpans;
        }
        case Kind.Decorator:
        {
            if (canHaveDecorators(node.Parent))
            {
                {
                    var modifiers = node.Parent.Modifiers();
                    if ((modifiers != null))
                    {
                        return modifiers;
                    }
                }
            }
            return null;
        }
        case Kind.HeritageClause:
        {
            if (Ast.IsClassLike(node.Parent))
            {
                return node.Parent.ClassLikeData().HeritageClauses;
            }
            else
            {
                return node.Parent.AsInterfaceDeclaration().HeritageClauses;
            }
        }
    }
    switch (parent.Kind)
    {
        case Kind.TypeLiteral:
        case Kind.InterfaceDeclaration:
        {
            if (Ast.IsTypeElement(node))
            {
                return parent.MemberList();
            }
            break;
        }
        case Kind.UnionType:
        {
            return parent.AsUnionTypeNode().Types;
        }
        case Kind.IntersectionType:
        {
            return parent.AsIntersectionTypeNode().Types;
        }
        case Kind.ArrayLiteralExpression:
        case Kind.TupleType:
        case Kind.NamedImports:
        case Kind.NamedExports:
        {
            return parent.ElementList();
        }
        case Kind.ObjectLiteralExpression:
        case Kind.JsxAttributes:
        {
            return parent.PropertyList();
        }
        case Kind.CallExpression:
        {
            var p = parent.AsCallExpression();
            switch (true)
            {
                case true when Ast.IsTypeNode(node):
                {
                    return p.TypeArguments;
                }
                case true when (node != p.Expression):
                {
                    return p.Arguments;
                }
            }
            break;
        }
        case Kind.NewExpression:
        {
            var p2 = parent.AsNewExpression();
            switch (true)
            {
                case true when Ast.IsTypeNode(node):
                {
                    return p2.TypeArguments;
                }
                case true when (node != p2.Expression):
                {
                    return p2.Arguments;
                }
            }
            break;
        }
        case Kind.JsxElement:
        case Kind.JsxFragment:
        {
            if (Ast.IsJsxChild(node))
            {
                return parent.Children();
            }
            break;
        }
        case Kind.JsxOpeningElement:
        case Kind.JsxSelfClosingElement:
        {
            if (Ast.IsTypeNode(node))
            {
                return parent.TypeArgumentList();
            }
            break;
        }
        case Kind.Block:
        case Kind.ModuleBlock:
        case Kind.CaseClause:
        case Kind.DefaultClause:
        {
            return parent.StatementList();
        }
        case Kind.CaseBlock:
        {
            return parent.AsCaseBlock().Clauses;
        }
        case Kind.ClassDeclaration:
        case Kind.ClassExpression:
        {
            if (Ast.IsClassElement(node))
            {
                return parent.MemberList();
            }
            break;
        }
        case Kind.EnumDeclaration:
        {
            if (Ast.IsEnumMember(node))
            {
                return parent.MemberList();
            }
            break;
        }
        case Kind.SourceFile:
        {
            if (Ast.IsStatement(node))
            {
                return parent.StatementList();
            }
            break;
        }
    }
    if (Ast.IsModifier(node))
    {
        {
            var modifiers2 = parent.Modifiers();
            if ((modifiers2 != null))
            {
                return modifiers2;
            }
        }
    }
    return null;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:553:1.
internal static bool canHaveDecorators(Node node)
{
    switch (node.Kind)
    {
        case Kind.Parameter:
        case Kind.PropertyDeclaration:
        case Kind.MethodDeclaration:
        case Kind.GetAccessor:
        case Kind.SetAccessor:
        case Kind.ClassExpression:
        case Kind.ClassDeclaration:
        {
            return true;
        }
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:567:1.
internal static bool originalNodesHaveSameParent(EmitContext emitContext, Node nodeA, Node nodeB)
{
    nodeA = emitContext.MostOriginal(nodeA);
    if ((nodeA.Parent != null))
    {
        nodeB = emitContext.MostOriginal(nodeB);
        return (nodeA.Parent == nodeB.Parent);
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:614:1.
internal static Node skipSynthesizedParentheses(Node node)
{
    while (((node.Kind == Kind.ParenthesizedExpression) && Ast.NodeIsSynthesized(node)))
    {
        node = node.Expression();
    }
    return node;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:621:1.
internal static bool isNewExpressionWithoutArguments(Node node)
{
    return ((node.Kind == Kind.NewExpression) && (node.ArgumentList() == null));
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:625:1.
internal static bool isBinaryOperation(Node node, Kind token)
{
    node = Ast.SkipPartiallyEmittedExpressions(node);
    return ((node.Kind == Kind.BinaryExpression) && (node.AsBinaryExpression().OperatorToken.Kind == token));
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:631:1.
internal static bool mixingBinaryOperatorsRequiresParentheses(Kind a, Kind b)
{
    if ((a == Kind.QuestionQuestionToken))
    {
        return ((b == Kind.AmpersandAmpersandToken) || (b == Kind.BarBarToken));
    }
    if ((b == Kind.QuestionQuestionToken))
    {
        return ((a == Kind.AmpersandAmpersandToken) || (a == Kind.BarBarToken));
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:641:1.
internal static bool isImmediatelyInvokedFunctionExpressionOrArrowFunction(Node node)
{
    node = Ast.SkipPartiallyEmittedExpressions(node);
    if (!Ast.IsCallExpression(node))
    {
        return false;
    }
    node = Ast.SkipPartiallyEmittedExpressions(node.Expression());
    return (Ast.IsFunctionExpression(node) || Ast.IsArrowFunction(node));
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:650:1.
internal static bool hasLeadingHash(string text)
{
    return ((text.Length > 0) && (text[0] == '#'));
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:654:1.
internal static string removeLeadingHash(string text)
{
    if (hasLeadingHash(text))
    {
        return text.Substring(1);
    }
    else
    {
        return text;
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:662:1.
internal static string ensureLeadingHash(string text)
{
    if (hasLeadingHash(text))
    {
        return text;
    }
    else
    {
        return ("#" + text);
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:670:1.
public static string FormatGeneratedName(bool privateName, string prefix, string @base, string suffix)
{
    var name = ((removeLeadingHash(prefix) + removeLeadingHash(@base)) + removeLeadingHash(suffix));
    if (privateName)
    {
        return ensureLeadingHash(name);
    }
    return name;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:678:1.
internal static bool isASCIIWordCharacter(int ch)
{
    return ((StringUtil.IsASCIILetter(ch) || StringUtil.IsDigit(ch)) || (ch == '_'));
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:682:1.
internal static string makeIdentifierFromModuleName(string moduleName)
{
    moduleName = TsPath.GetBaseFileName(moduleName);
    GoBuilder builder = new GoBuilder();
    var start = 0;
    var pos = 0;
    while ((pos < GoRuntime.Len(moduleName)))
    {
        var ch = (int)(moduleName[pos]);
        if (((pos == 0) && StringUtil.IsDigit(ch)))
        {
            builder.WriteByte('_');
        }
        else
        {
            if (!isASCIIWordCharacter(ch))
            {
                if ((start < pos))
                {
                    builder.WriteString(moduleName.Slice2(start, pos));
                }
                builder.WriteByte('_');
                start = (pos + 1);
            }
        }
        pos++;
    }
    if ((start < pos))
    {
        builder.WriteString(moduleName.Slice2(start, pos));
    }
    return builder.String();
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:706:1.
internal static int findSpanEndWithEmitContext<T>(EmitContext c, Slice<T> array, Func<EmitContext, T, bool> test, int start)
{
    var i = start;
    while (((i < GoRuntime.Len(array)) && test(c, array[i])))
    {
        i++;
    }
    return i;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:714:1.
internal static int findSpanEnd<T>(Slice<T> array, Func<T, bool> test, int start)
{
    var i = start;
    while (((i < GoRuntime.Len(array)) && test(array[i])))
    {
        i++;
    }
    return i;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:722:1.
internal static void skipWhiteSpaceSingleLine(string text, ref int pos)
{
    while ((pos < text.Length))
    {
        var tuple2 = Utf8.DecodeRuneInString(text.Slice2(pos, text.Length)); var ch = tuple2.Item1; var size = tuple2.Item2;
        if (!StringUtil.IsWhiteSpaceSingleLine(ch))
        {
            break;
        }
        pos += size;
    }
}

internal static void skipWhiteSpaceSingleLine(ByteStringView text, ref int pos)
{
    while ((pos < text.Length))
    {
        var tuple2 = Utf8.DecodeRuneInString(text.Slice(pos, text.Length).AsSpan()); var ch = tuple2.Item1; var size = tuple2.Item2;
        if (!StringUtil.IsWhiteSpaceSingleLine(ch))
        {
            break;
        }
        pos += size;
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:732:1.
internal static bool matchWhiteSpaceSingleLine(string text, ref int pos)
{
    var startPos = pos;
    skipWhiteSpaceSingleLine(text, ref pos);
    return (pos != startPos);
}

internal static bool matchWhiteSpaceSingleLine(ByteStringView text, ref int pos)
{
    var startPos = pos;
    skipWhiteSpaceSingleLine(text, ref pos);
    return (pos != startPos);
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:738:1.
internal static bool matchRune(string text, ref int pos, int expected)
{
    var tuple3 = Utf8.DecodeRuneInString(text.Slice2(pos, text.Length)); var ch = tuple3.Item1; var size = tuple3.Item2;
    if ((ch == expected))
    {
        pos += size;
        return true;
    }
    return false;
}

internal static bool matchRune(ByteStringView text, ref int pos, int expected)
{
    var tuple3 = Utf8.DecodeRuneInString(text.Slice(pos, text.Length).AsSpan()); var ch = tuple3.Item1; var size = tuple3.Item2;
    if ((ch == expected))
    {
        pos += size;
        return true;
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:747:1.
internal static bool matchString(string text, ref int pos, string expected)
{
    var textPos = pos;
    var expectedPos = 0;
    while ((expectedPos < GoRuntime.Len(expected)))
    {
        if ((textPos >= text.Length))
        {
            return false;
        }
        var tuple4 = Utf8.DecodeRuneInString(expected.Slice2(expectedPos, expected.Length)); var expectedRune = tuple4.Item1; var expectedSize = tuple4.Item2;
        if (!matchRune(text, ref textPos, expectedRune))
        {
            return false;
        }
        expectedPos += expectedSize;
    }
    pos = textPos;
    return true;
}

internal static bool matchString(ByteStringView text, ref int pos, string expected)
{
    var textPos = pos;
    var expectedPos = 0;
    while ((expectedPos < GoRuntime.Len(expected)))
    {
        if ((textPos >= text.Length))
        {
            return false;
        }
        var tuple4 = Utf8.DecodeRuneInString(expected.Slice2(expectedPos, expected.Length)); var expectedRune = tuple4.Item1; var expectedSize = tuple4.Item2;
        if (!matchRune(text, ref textPos, expectedRune))
        {
            return false;
        }
        expectedPos += expectedSize;
    }
    pos = textPos;
    return true;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:767:1.
internal static bool matchQuotedString(string text, ref int pos)
{
    var textPos = pos;
    int quoteChar = default;
    switch (true)
    {
        case true when matchRune(text, ref textPos, '\''):
        {
            quoteChar = '\'';
            break;
        }
        case true when matchRune(text, ref textPos, '"'):
        {
            quoteChar = '"';
            break;
        }
        default:
        {
            return false;
        }
    }
    while ((textPos < text.Length))
    {
        var tuple5 = Utf8.DecodeRuneInString(text.Slice2(textPos, text.Length)); var ch = tuple5.Item1; var size = tuple5.Item2;
        textPos += size;
        if ((ch == quoteChar))
        {
            pos = textPos;
            return true;
        }
    }
    return false;
}

internal static bool matchQuotedString(ByteStringView text, ref int pos)
{
    var textPos = pos;
    int quoteChar = default;
    switch (true)
    {
        case true when matchRune(text, ref textPos, '\''):
        {
            quoteChar = '\'';
            break;
        }
        case true when matchRune(text, ref textPos, '"'):
        {
            quoteChar = '"';
            break;
        }
        default:
        {
            return false;
        }
    }
    while ((textPos < text.Length))
    {
        var tuple5 = Utf8.DecodeRuneInString(text.Slice(textPos, text.Length).AsSpan()); var ch = tuple5.Item1; var size = tuple5.Item2;
        textPos += size;
        if ((ch == quoteChar))
        {
            pos = textPos;
            return true;
        }
    }
    return false;
}

// /// <reference path="..." />
// /// <reference types="..." />
// /// <reference lib="..." />
// /// <reference no-default-lib="..." />
// /// <amd-dependency path="..." />
// /// <amd-module />
// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:795:1.
public static bool IsRecognizedTripleSlashComment(string text, CommentRange commentRange)
{
    if (((((commentRange.Kind == Kind.SingleLineCommentTrivia) && (commentRange.Len() > 2)) && (text[(commentRange.Pos + 1)] == '/')) && (text[(commentRange.Pos + 2)] == '/')))
    {
        text = text.Slice2((commentRange.Pos + 3), commentRange.End);
        var pos = 0;
        skipWhiteSpaceSingleLine(text, ref pos);
        if (!matchRune(text, ref pos, '<'))
        {
            return false;
        }
        switch (true)
        {
            case true when matchString(text, ref pos, "reference"):
            {
                if (!matchWhiteSpaceSingleLine(text, ref pos))
                {
                    return false;
                }
                if ((((!matchString(text, ref pos, "path") && !matchString(text, ref pos, "types")) && !matchString(text, ref pos, "lib")) && !matchString(text, ref pos, "no-default-lib")))
                {
                    return false;
                }
                skipWhiteSpaceSingleLine(text, ref pos);
                if (!matchRune(text, ref pos, '='))
                {
                    return false;
                }
                skipWhiteSpaceSingleLine(text, ref pos);
                if (!matchQuotedString(text, ref pos))
                {
                    return false;
                }
                break;
            }
            case true when matchString(text, ref pos, "amd-dependency"):
            {
                if (!matchWhiteSpaceSingleLine(text, ref pos))
                {
                    return false;
                }
                if (!matchString(text, ref pos, "path"))
                {
                    return false;
                }
                skipWhiteSpaceSingleLine(text, ref pos);
                if (!matchRune(text, ref pos, '='))
                {
                    return false;
                }
                skipWhiteSpaceSingleLine(text, ref pos);
                if (!matchQuotedString(text, ref pos))
                {
                    return false;
                }
                break;
            }
            case true when matchString(text, ref pos, "amd-module"):
            {
                skipWhiteSpaceSingleLine(text, ref pos);
                break;
            }
            default:
            {
                return false;
            }
        }
        var index = GoStrings.Index(text.Slice2(pos, text.Length), "/>");
        return (index != -1);
    }
    return false;
}

public static bool IsRecognizedTripleSlashComment(ByteStringView text, CommentRange commentRange)
{
    if (((((commentRange.Kind == Kind.SingleLineCommentTrivia) && (commentRange.Len() > 2)) && (text[(commentRange.Pos + 1)] == '/')) && (text[(commentRange.Pos + 2)] == '/')))
    {
        text = text.Slice((commentRange.Pos + 3), commentRange.End);
        var pos = 0;
        skipWhiteSpaceSingleLine(text, ref pos);
        if (!matchRune(text, ref pos, '<'))
        {
            return false;
        }
        switch (true)
        {
            case true when matchString(text, ref pos, "reference"):
            {
                if (!matchWhiteSpaceSingleLine(text, ref pos))
                {
                    return false;
                }
                if ((((!matchString(text, ref pos, "path") && !matchString(text, ref pos, "types")) && !matchString(text, ref pos, "lib")) && !matchString(text, ref pos, "no-default-lib")))
                {
                    return false;
                }
                skipWhiteSpaceSingleLine(text, ref pos);
                if (!matchRune(text, ref pos, '='))
                {
                    return false;
                }
                skipWhiteSpaceSingleLine(text, ref pos);
                if (!matchQuotedString(text, ref pos))
                {
                    return false;
                }
                break;
            }
            case true when matchString(text, ref pos, "amd-dependency"):
            {
                if (!matchWhiteSpaceSingleLine(text, ref pos))
                {
                    return false;
                }
                if (!matchString(text, ref pos, "path"))
                {
                    return false;
                }
                skipWhiteSpaceSingleLine(text, ref pos);
                if (!matchRune(text, ref pos, '='))
                {
                    return false;
                }
                skipWhiteSpaceSingleLine(text, ref pos);
                if (!matchQuotedString(text, ref pos))
                {
                    return false;
                }
                break;
            }
            case true when matchString(text, ref pos, "amd-module"):
            {
                skipWhiteSpaceSingleLine(text, ref pos);
                break;
            }
            default:
            {
                return false;
            }
        }
        var index = GoStrings.Index(text.Slice(pos, text.Length).AsSpan(), "/>");
        return (index != -1);
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:852:1.
internal static bool isJSDocLikeText(string text, CommentRange comment)
{
    return ((((comment.Kind == Kind.MultiLineCommentTrivia) && (comment.Len() >= 5)) && (text[(comment.Pos + 2)] == '*')) && (text[(comment.Pos + 3)] != '/'));
}

internal static bool isJSDocLikeText(ByteStringView text, CommentRange comment)
{
    return ((((comment.Kind == Kind.MultiLineCommentTrivia) && (comment.Len() >= 5)) && (text[(comment.Pos + 2)] == '*')) && (text[(comment.Pos + 3)] != '/'));
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:859:1.
public static bool IsPinnedComment(string text, CommentRange comment)
{
    return (((comment.Kind == Kind.MultiLineCommentTrivia) && (comment.Len() > 5)) && (text[(comment.Pos + 2)] == '!'));
}

public static bool IsPinnedComment(ByteStringView text, CommentRange comment)
{
    return (((comment.Kind == Kind.MultiLineCommentTrivia) && (comment.Len() > 5)) && (text[(comment.Pos + 2)] == '!'));
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:865:1.
internal static int calculateIndent(string text, int pos, int end)
{
    var currentLineIndent = 0;
    var indentSize = TextWriter.GetDefaultIndentSize();
    while ((pos < end))
    {
        var tuple6 = Utf8.DecodeRuneInString(text.Slice2(pos, text.Length)); var ch = tuple6.Item1; var size = tuple6.Item2;
        if (!StringUtil.IsWhiteSpaceSingleLine(ch))
        {
            break;
        }
        if ((ch == '\t'))
        {
            currentLineIndent += (indentSize - ((currentLineIndent % indentSize)));
        }
        else
        {
            currentLineIndent++;
        }
        pos += size;
    }
    return currentLineIndent;
}

internal static int calculateIndent(ByteStringView text, int pos, int end)
{
    var currentLineIndent = 0;
    var indentSize = TextWriter.GetDefaultIndentSize();
    while ((pos < end))
    {
        var tuple6 = Utf8.DecodeRuneInString(text.Slice(pos, text.Length).AsSpan()); var ch = tuple6.Item1; var size = tuple6.Item2;
        if (!StringUtil.IsWhiteSpaceSingleLine(ch))
        {
            break;
        }
        if ((ch == '\t'))
        {
            currentLineIndent += (indentSize - ((currentLineIndent % indentSize)));
        }
        else
        {
            currentLineIndent++;
        }
        pos += size;
    }
    return currentLineIndent;
}

// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:903:1.
internal static lineCharacterCache newLineCharacterCache(Source source)
{
    return new lineCharacterCache { lineMap = source.ECMALineMap(), text = source.Text() };
}

}
internal sealed class lineCharacterCache
{
    internal Slice<int> lineMap;
    internal ByteStringView text;
    private int cachedLine,cachedPos,cachedChar;
    private bool hasCached;
// getLineAndCharacter returns the 0-based line number and UTF-16 code unit
// offset from the start of that line for the given byte position.
// Upstream upstream/TypeScript/tsc/internal/printer/utilities.go:912:1.
internal (int, int) getLineAndCharacter(int pos)
{
    int line = default;
    int character = default;
    line = ByteScanner.ComputeLineOfPosition(this.lineMap.AsSpan(), pos);
    var lineStart = (int)(this.lineMap[line]);
    var endPos = Math.Min(pos, GoRuntime.Len(this.text));
    if (((this.hasCached && (line == this.cachedLine)) && (endPos >= this.cachedPos)))
    {
        character = (this.cachedChar + Core.UTF16Len(this.text.Slice(this.cachedPos, endPos)));
    }
    else
    {
        character = Core.UTF16Len(this.text.Slice(lineStart, endPos));
    }
    var cachedChar = character;
    character += (int)((pos - endPos));
    this.cachedLine = line;
    this.cachedPos = endPos;
    this.cachedChar = cachedChar;
    this.hasCached = true;
    return (line, character);
}

}

internal static class PrinterStringSlices
{
    internal static string Slice2(this string value, int start, int end) => value.Substring(start, end - start);
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/printer/printer.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.SourceMap;
using ByteScanner = TypeScript.Scanner.Scanner;
using static TypeScript.DiagnosticPrinting.PrinterUtilities;
namespace TypeScript.DiagnosticPrinting;
internal enum WriteKind : int
{
    None = 0,
    Keyword = 1,
    Operator = 2,
    Punctuation = 3,
    StringLiteral = 4,
    Parameter = 5,
    Property = 6,
    Comment = 7,
    Literal = 8,
}
[Flags]
internal enum ListFormat : int
{
    None = 0,
    SingleLine = 0,
    MultiLine = 1 << 0,
    PreserveLines = 1 << 1,
    LinesMask = SingleLine | MultiLine | PreserveLines,
    NotDelimited = 0,
    BarDelimited = 1 << 2,
    AmpersandDelimited = 1 << 3,
    CommaDelimited = 1 << 4,
    AsteriskDelimited = 1 << 5,
    DelimitersMask = BarDelimited | AmpersandDelimited | CommaDelimited | AsteriskDelimited,
    AllowTrailingComma = 1 << 6,
    Indented = 1 << 7,
    SpaceBetweenBraces = 1 << 8,
    SpaceBetweenSiblings = 1 << 9,
    Braces = 1 << 10,
    Parenthesis = 1 << 11,
    AngleBrackets = 1 << 12,
    SquareBrackets = 1 << 13,
    BracketsMask = Braces | Parenthesis | AngleBrackets | SquareBrackets,
    OptionalIfNil = 1 << 14,
    OptionalIfEmpty = 1 << 15,
    Optional = OptionalIfNil | OptionalIfEmpty,
    PreferNewLine = 1 << 16,
    NoTrailingNewLine = 1 << 17,
    NoInterveningComments = 1 << 18,
    NoSpaceIfEmpty = 1 << 19,
    SingleElement = 1 << 20,
    SpaceAfterList = 1 << 21,
    Modifiers = SingleLine | SpaceBetweenSiblings | NoInterveningComments | SpaceAfterList,
    HeritageClauses = SingleLine | SpaceBetweenSiblings,
    SingleLineTypeLiteralMembers = SingleLine | SpaceBetweenBraces | SpaceBetweenSiblings,
    MultiLineTypeLiteralMembers = MultiLine | Indented | OptionalIfEmpty,
    SingleLineTupleTypeElements = CommaDelimited | SpaceBetweenSiblings | SingleLine,
    MultiLineTupleTypeElements = CommaDelimited | Indented | SpaceBetweenSiblings | MultiLine,
    UnionTypeConstituents = BarDelimited | SpaceBetweenSiblings | SingleLine,
    IntersectionTypeConstituents = AmpersandDelimited | SpaceBetweenSiblings | SingleLine,
    ObjectBindingPatternElements = SingleLine | AllowTrailingComma | SpaceBetweenBraces | CommaDelimited | SpaceBetweenSiblings | NoSpaceIfEmpty,
    ArrayBindingPatternElements = SingleLine | AllowTrailingComma | CommaDelimited | SpaceBetweenSiblings | NoSpaceIfEmpty,
    ObjectLiteralExpressionProperties = PreserveLines | CommaDelimited | SpaceBetweenSiblings | SpaceBetweenBraces | Indented | Braces | NoSpaceIfEmpty,
    ImportAttributes = PreserveLines | CommaDelimited | SpaceBetweenSiblings | SpaceBetweenBraces | Indented | Braces | NoSpaceIfEmpty,
    ArrayLiteralExpressionElements = PreserveLines | CommaDelimited | SpaceBetweenSiblings | AllowTrailingComma | Indented | SquareBrackets,
    CommaListElements = CommaDelimited | SpaceBetweenSiblings | SingleLine,
    CallExpressionArguments = CommaDelimited | SpaceBetweenSiblings | SingleLine | Parenthesis,
    NewExpressionArguments = CommaDelimited | SpaceBetweenSiblings | SingleLine | Parenthesis | OptionalIfNil,
    TemplateExpressionSpans = SingleLine | NoInterveningComments,
    SingleLineBlockStatements = SpaceBetweenBraces | SpaceBetweenSiblings | SingleLine,
    MultiLineBlockStatements = Indented | MultiLine,
    VariableDeclarationList = CommaDelimited | SpaceBetweenSiblings | SingleLine,
    SingleLineFunctionBodyStatements = SingleLine | SpaceBetweenSiblings | SpaceBetweenBraces,
    MultiLineFunctionBodyStatements = MultiLine,
    ClassHeritageClauses = SingleLine,
    ClassMembers = Indented | MultiLine,
    InterfaceMembers = Indented | MultiLine,
    EnumMembers = CommaDelimited | Indented | MultiLine,
    CaseBlockClauses = Indented | MultiLine,
    NamedImportsOrExportsElements = CommaDelimited | SpaceBetweenSiblings | AllowTrailingComma | SingleLine | SpaceBetweenBraces | NoSpaceIfEmpty,
    JsxElementOrFragmentChildren = SingleLine | NoInterveningComments,
    JsxElementAttributes = SingleLine | SpaceBetweenSiblings | NoInterveningComments,
    CaseOrDefaultClauseStatements = Indented | MultiLine | NoTrailingNewLine | OptionalIfEmpty,
    HeritageClauseTypes = CommaDelimited | SpaceBetweenSiblings | SingleLine,
    SourceFileStatements = MultiLine | NoTrailingNewLine,
    Decorators = MultiLine | Optional | SpaceAfterList,
    TypeArguments = CommaDelimited | SpaceBetweenSiblings | SingleLine | AngleBrackets | Optional,
    TypeParameters = CommaDelimited | SpaceBetweenSiblings | SingleLine | AngleBrackets | Optional,
    Parameters = CommaDelimited | SpaceBetweenSiblings | SingleLine | Parenthesis,
    SingleArrowParameter = CommaDelimited | SpaceBetweenSiblings | SingleLine,
    IndexSignatureParameters = CommaDelimited | SpaceBetweenSiblings | SingleLine | Indented | SquareBrackets,
    JSDocComment = MultiLine | AsteriskDelimited,
    ImportClauseEntries = ImportAttributes,
}
[Flags]
internal enum tokenEmitFlags : uint
{
    NoComments = 1 << 0,
    IndentLeadingComments = 1 << 1,
    NoSourceMaps = 1 << 2,
    None = 0,
}
[Flags]
internal enum commentSeparator : uint
{
    None = 0,
    Before = 1,
    After = 2,
}
internal struct PrinterOptions
{
    internal bool RemoveComments;
    internal NewLineKind NewLine;
    internal bool OmitTrailingSemicolon, NoEmitHelpers;
    internal ScriptTarget Target;
    internal bool SourceMap, InlineSourceMap, InlineSources, OmitBraceSourceMapPositions;
    internal bool OnlyPrintJSDocStyle, NeverAsciiEscape, PreserveSourceNewlines, TerminateUnterminatedLiterals;
}
internal struct PrintHandlers
{
    internal Func<string,bool> HasGlobalName;
    internal Func<Source,int,(Source Source,int Pos,bool Ok)> MapSourcePosition;
    internal Action<Node> OnBeforeEmitNode, OnAfterEmitNode;
    internal Action<NodeList> OnBeforeEmitNodeList, OnAfterEmitNodeList;
    internal Action<Node> OnBeforeEmitToken, OnAfterEmitToken;
}
internal readonly record struct detachedCommentsInfo(int nodePos, int detachedCommentEndPos);
internal readonly record struct commentState(EmitFlags emitFlags, TextRange commentRange, int containerPos, int containerEnd, int declarationListContainerEnd);
internal readonly record struct sourceMapState(EmitFlags emitFlags, TextRange sourceMapRange, bool hasTokenSourceMapRange);
internal struct printerState
{
    internal commentState? commentState;
    internal sourceMapState? sourceMapState;
}
internal sealed partial class Printer
{
    internal PrinterOptions Options;
    private PrintHandlers PrintHandlers;
    private Func<string,bool> HasGlobalName => PrintHandlers.HasGlobalName;
    private Func<Source,int,(Source Source,int Pos,bool Ok)> MapSourcePosition => PrintHandlers.MapSourcePosition;
    private Action<Node> OnBeforeEmitNode => PrintHandlers.OnBeforeEmitNode;
    private Action<Node> OnAfterEmitNode => PrintHandlers.OnAfterEmitNode;
    private Action<NodeList> OnBeforeEmitNodeList => PrintHandlers.OnBeforeEmitNodeList;
    private Action<NodeList> OnAfterEmitNodeList => PrintHandlers.OnAfterEmitNodeList;
    private Action<Node> OnBeforeEmitToken => PrintHandlers.OnBeforeEmitToken;
    private Action<Node> OnAfterEmitToken => PrintHandlers.OnAfterEmitToken;
    private EmitContext emitContext;
    private SourceFile currentSourceFile;
    private Dictionary<string,Node> uniqueHelperNames;
    private Node externalHelpersModuleName;
    private int nextListElementPos;
    private EmitTextWriter writer, ownWriter;
    private WriteKind writeKind;
    private bool sourceMapsDisabled;
    private Generator sourceMapGenerator;
    private Source sourceMapSource;
    private long sourceMapSourceIndex;
    private bool sourceMapSourceIsJson;
    private lineCharacterCache sourceMapLineCharCache;
    private Source mostRecentSourceMapSource;
    private long mostRecentSourceMapSourceIndex;
    private int containerPos, containerEnd, declarationListContainerEnd;
    private Stack<detachedCommentsInfo> detachedCommentsInfo = new();
    private bool commentsDisabled, inExtends;
    private readonly NameGenerator nameGenerator = new();
    private Func<string,string> makeFileLevelOptimisticUniqueName;
    internal Dictionary<Node,Symbol> IdToSymbol;
    private Printer() { }
    // Established checker diagnostic entry shape forwards the complete printer.
    internal Printer(EmitContext context, bool removeComments = true, bool omitTrailingSemicolon = false, bool neverAsciiEscape = false)
    {
        Options = new PrinterOptions { RemoveComments = removeComments, OmitTrailingSemicolon = omitTrailingSemicolon, NeverAsciiEscape = neverAsciiEscape };
        emitContext = context ?? new EmitContext();
        nameGenerator.Context = emitContext;
        nameGenerator.GetTextOfNode = node => getTextOfNode(node,false);
        nameGenerator.IsFileLevelUniqueNameInCurrentFile = isFileLevelUniqueNameInCurrentFile;
        makeFileLevelOptimisticUniqueName = nameGenerator.MakeFileLevelOptimisticUniqueName;
        containerPos = containerEnd = declarationListContainerEnd = -1;
        commentsDisabled = removeComments;
    }
    internal void Write(Node node, TextWriter output, SourceFile sourceFile = null) => Write(node, sourceFile, output, null);
    internal static string EscapeString(string text, char quote) => PrinterUtilities.EscapeString(text,(QuoteChar)quote);
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:173:1.
internal static Printer NewPrinter(PrinterOptions options, PrintHandlers handlers, EmitContext emitContext = null)
{
    var printer = new Printer { PrintHandlers = handlers, Options = options, emitContext = emitContext };
    if ((printer.emitContext == null))
    {
        printer.emitContext = new EmitContext();
    }
    printer.nameGenerator.Context = printer.emitContext;
    printer.nameGenerator.GetTextOfNode = (node) =>
{
    return printer.getTextOfNode(node, false);
};
    printer.nameGenerator.IsFileLevelUniqueNameInCurrentFile = printer.isFileLevelUniqueNameInCurrentFile;
    printer.makeFileLevelOptimisticUniqueName = (name) =>
{
    return printer.nameGenerator.MakeFileLevelOptimisticUniqueName(name);
};
    printer.containerPos = -1;
    printer.containerEnd = -1;
    printer.declarationListContainerEnd = -1;
    printer.commentsDisabled = options.RemoveComments;
    return printer;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:196:1.
private ByteStringValue getLiteralTextOfNodeValue(Node node, SourceFile sourceFile, getLiteralTextFlags flags)
{
    if (Ast.IsStringLiteral(node))
    {
        {
            var textSourceNode = this.emitContext.textSource?.GetValueOrDefault(node);
            var ok = textSourceNode != null;
            if ((ok && (textSourceNode != null)))
            {
                ByteStringValue text = default;
                switch (textSourceNode.Kind)
                {
                    default:
                    {
                        return this.getLiteralTextOfNodeValue(textSourceNode, Ast.GetSourceFileOfNode(textSourceNode), flags);
                    }
                    case Kind.NumericLiteral:
                    {
                        text = textSourceNode.TextValue();
                        break;
                    }
                    case Kind.Identifier:
                    case Kind.PrivateIdentifier:
                    case Kind.JsxNamespacedName:
                    {
                        text = this.getTextOfNodeValue(textSourceNode, false);
                        break;
                    }
                }
                switch (true)
                {
                    case true when ((flags & getLiteralTextFlags.JsxAttributeEscape) != 0):
                    {
                        var builder = new GoBuilder();
                        builder.WriteByte('"');
                        escapeStringWorker(text, QuoteChar.DoubleQuote, getLiteralTextFlags.JsxAttributeEscape | getLiteralTextFlags.NeverAsciiEscape, builder);
                        builder.WriteByte('"');
                        return builder.Value();
                    }
                    case true when (((flags & getLiteralTextFlags.NeverAsciiEscape) != 0) || ((this.emitContext.EmitFlags(node) & EmitFlags.NoAsciiEscaping) != 0)):
                    {
                        var builder = new GoBuilder();
                        builder.WriteByte('"');
                        escapeStringWorker(text, QuoteChar.DoubleQuote, getLiteralTextFlags.NeverAsciiEscape, builder);
                        builder.WriteByte('"');
                        return builder.Value();
                    }
                    default:
                    {
                        var builder = new GoBuilder();
                        builder.WriteByte('"');
                        escapeStringWorker(text, QuoteChar.DoubleQuote, getLiteralTextFlags.None, builder);
                        builder.WriteByte('"');
                        return builder.Value();
                    }
                }
            }
        }
    }
    if (((this.emitContext.EmitFlags(node) & EmitFlags.NoAsciiEscaping) != 0))
    {
        flags |= getLiteralTextFlags.NeverAsciiEscape;
    }
    if ((this.Options.Target >= ScriptTarget.ES2021))
    {
        flags |= getLiteralTextFlags.AllowNumericSeparator;
    }
    return getLiteralTextValue(node, (sourceFile ?? this.currentSourceFile), flags);
}

private string getLiteralTextOfNode(Node node, SourceFile sourceFile, getLiteralTextFlags flags) => getLiteralTextOfNodeValue(node, sourceFile, flags).Materialize();

// `node` must be one of Identifier | PrivateIdentifier | LiteralExpression | JsxNamespacedName
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:230:1.
private ByteStringValue getTextOfNodeValue(Node node, bool includeTrivia)
{
    if ((Ast.IsMemberName(node) && (this.emitContext.autoGenerate?.GetValueOrDefault(node) != null)))
    {
        return ByteStringValue.FromString(this.nameGenerator.GenerateName(node));
    }
    if (Ast.IsStringLiteral(node))
    {
        {
            var textSourceNode = this.emitContext.textSource?.GetValueOrDefault(node);
            if ((textSourceNode != null))
            {
                return this.getTextOfNodeValue(textSourceNode, includeTrivia);
            }
        }
    }
    var canUseSourceFile = (((this.currentSourceFile != null) && (node.Parent != null)) && !Ast.NodeIsSynthesized(node));
    switch (node.Kind)
    {
        case Kind.Identifier:
        case Kind.PrivateIdentifier:
        case Kind.JsxNamespacedName:
        {
            if ((!canUseSourceFile || (Ast.GetSourceFileOfNode(node) != this.emitContext.MostOriginal(this.currentSourceFile.AsNode()).AsSourceFile())))
            {
                return node.TextValue();
            }
            break;
        }
        case Kind.StringLiteral:
        case Kind.NumericLiteral:
        case Kind.BigIntLiteral:
        case Kind.NoSubstitutionTemplateLiteral:
        case Kind.TemplateHead:
        case Kind.TemplateMiddle:
        case Kind.TemplateTail:
        {
            return this.getLiteralTextOfNodeValue(node, null, getLiteralTextFlags.None);
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected node: %v", node.Kind));
        }
    }
    return ByteScanner.GetTextOfNodeFromSourceTextValue(this.currentSourceFile.TextView(), node, includeTrivia);
}

private string getTextOfNode(Node node, bool includeTrivia) => getTextOfNodeValue(node, includeTrivia).Materialize();

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:282:1.
private void writeAs(string text, WriteKind writeKind)
{
    switch (writeKind)
    {
        case WriteKind.None:
        {
            this.writer.Write(text);
            break;
        }
        case WriteKind.Parameter:
        {
            this.writeParameter(text);
            break;
        }
        case WriteKind.Keyword:
        {
            this.writeKeyword(text);
            break;
        }
        case WriteKind.Operator:
        {
            this.writeOperator(text);
            break;
        }
        case WriteKind.Property:
        {
            this.writeProperty(text);
            break;
        }
        case WriteKind.Punctuation:
        {
            this.writePunctuation(text);
            break;
        }
        case WriteKind.StringLiteral:
        {
            this.writer.WriteStringLiteral(text);
            break;
        }
        case WriteKind.Comment:
        {
            this.writeComment(text);
            break;
        }
        case WriteKind.Literal:
        {
            this.writeLiteral(text);
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected printer.WriteKind: %v", writeKind));
        }
    }
}

private void writeAs(ByteStringValue text, WriteKind writeKind)
{
    switch (writeKind)
    {
        case WriteKind.None:
        {
            this.writer.Write(text);
            break;
        }
        case WriteKind.Parameter:
        {
            this.writeParameter(text);
            break;
        }
        case WriteKind.Keyword:
        {
            this.writeKeyword(text);
            break;
        }
        case WriteKind.Operator:
        {
            this.writeOperator(text);
            break;
        }
        case WriteKind.Property:
        {
            this.writeProperty(text);
            break;
        }
        case WriteKind.Punctuation:
        {
            this.writePunctuation(text);
            break;
        }
        case WriteKind.StringLiteral:
        {
            this.writer.WriteStringLiteral(text);
            break;
        }
        case WriteKind.Comment:
        {
            this.writeComment(text);
            break;
        }
        case WriteKind.Literal:
        {
            this.writeLiteral(text);
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected printer.WriteKind: %v", writeKind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:307:1.
private void write(string text)
{
    this.writeAs(text, this.writeKind);
}

private void write(ByteStringValue text)
{
    this.writeAs(text, this.writeKind);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:311:1.
private WriteKind setWriteKind(WriteKind kind)
{
    var previous = this.writeKind;
    this.writeKind = kind;
    return previous;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:317:1.
private void writeSymbol(string text, Symbol optSymbol)
{
    if ((optSymbol == null))
    {
        this.write(text);
    }
    else
    {
        this.writer.WriteSymbol(text, optSymbol);
    }
}

private void writeSymbol(ByteStringValue text, Symbol optSymbol)
{
    if ((optSymbol == null))
    {
        this.write(text);
    }
    else
    {
        this.writer.WriteSymbol(text, optSymbol);
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:325:1.
private void writeLiteral(string text)
{
    this.writer.WriteLiteral(text);
}

private void writeLiteral(ByteStringValue text)
{
    this.writer.WriteLiteral(text);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:329:1.
private void writePunctuation(string text)
{
    this.writer.WritePunctuation(text);
}

private void writePunctuation(ByteStringValue text)
{
    this.writer.WritePunctuation(text);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:333:1.
private void writeOperator(string text)
{
    this.writer.WriteOperator(text);
}

private void writeOperator(ByteStringValue text)
{
    this.writer.WriteOperator(text);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:337:1.
private void writeKeyword(string text)
{
    this.writer.WriteKeyword(text);
}

private void writeKeyword(ByteStringValue text)
{
    this.writer.WriteKeyword(text);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:341:1.
private void writeProperty(string text)
{
    this.writer.WriteProperty(text);
}

private void writeProperty(ByteStringValue text)
{
    this.writer.WriteProperty(text);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:345:1.
private void writeParameter(string text)
{
    this.writer.WriteParameter(text);
}

private void writeParameter(ByteStringValue text)
{
    this.writer.WriteParameter(text);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:349:1.
private void writeComment(string text)
{
    this.writer.WriteComment(text);
}

private void writeComment(ByteStringValue text)
{
    this.writer.WriteComment(text);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:353:1.
private void writeSpace()
{
    this.writer.WriteSpace(" ");
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:357:1.
private void writeLine()
{
    this.writer.WriteLine();
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:361:1.
private void writeLineRepeat(int count)
{
    {
        foreach (var ignored1 in GoRuntime.RangeKeys(count))
        {
            this.writeLine();
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:367:1.
private void writeLines(string text)
{
    var lines = StringUtil.SplitLines(ByteStringValue.FromString(text));
    var indentation = StringUtil.GuessIndentation(lines);
    {
        foreach (var sourceLine in GoRuntime.RangeValues(lines))
        {
            var line = sourceLine;
            if ((indentation > 0))
            {
                line = line.Slice(indentation, line.Length);
            }
            if ((GoRuntime.Len(line) > 0))
            {
                this.writeLine();
                this.write(line);
            }
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:381:1.
private void writeTrailingSemicolon()
{
    this.writer.WriteTrailingSemicolon(";");
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:385:1.
private void increaseIndent()
{
    this.writer.IncreaseIndent();
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:389:1.
private void decreaseIndent()
{
    this.writer.DecreaseIndent();
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:393:1.
private void increaseIndentIf(bool indentRequested)
{
    if (indentRequested)
    {
        this.increaseIndent();
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:399:1.
private void decreaseIndentIf(bool indentRequested)
{
    if (indentRequested)
    {
        this.decreaseIndent();
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:405:1.
private void writeLineOrSpace(Node parentNode, Node prevChildNode, Node nextChildNode)
{
    if (this.shouldEmitOnSingleLine(parentNode))
    {
        this.writeSpace();
    }
    else
    {
        if (this.Options.PreserveSourceNewlines)
        {
            var lines = this.getLinesBetweenNodes(parentNode, prevChildNode, nextChildNode);
            if ((lines > 0))
            {
                this.writeLineRepeat(lines);
            }
            else
            {
                this.writeSpace();
            }
        }
        else
        {
            this.writeLine();
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:420:1.
private void writeLinesAndIndent(int lineCount, bool writeSpaceIfNotIndenting)
{
    if ((lineCount > 0))
    {
        this.increaseIndent();
        this.writeLineRepeat(lineCount);
    }
    else
    {
        if (writeSpaceIfNotIndenting)
        {
            this.writeSpace();
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:429:1.
private bool writeLineSeparatorsAndIndentBefore(Node node, Node parent)
{
    if (this.Options.PreserveSourceNewlines)
    {
        var leadingNewlines = this.getLeadingLineTerminatorCount(parent, node, ListFormat.None);
        if ((leadingNewlines > 0))
        {
            this.writeLinesAndIndent(leadingNewlines, false);
            return true;
        }
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:440:1.
private void writeLineSeparatorsAfter(Node node, Node parent)
{
    if (this.Options.PreserveSourceNewlines)
    {
        var trailingNewlines = this.getClosingLineTerminatorCount(parent, node, ListFormat.None, Core.NewTextRange(-1, -1));
        if ((trailingNewlines > 0))
        {
            this.writeLineRepeat(trailingNewlines);
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:449:1.
private int getLinesBetweenNodes(Node parent, Node node1, Node node2)
{
    if (this.shouldElideIndentation(parent))
    {
        return 0;
    }
    parent = skipSynthesizedParentheses(parent);
    node1 = skipSynthesizedParentheses(node1);
    node2 = skipSynthesizedParentheses(node2);
    if (this.shouldEmitOnNewLine(node2, ListFormat.None))
    {
        return 1;
    }
    if (((((this.currentSourceFile != null) && !Ast.NodeIsSynthesized(parent)) && !Ast.NodeIsSynthesized(node1)) && !Ast.NodeIsSynthesized(node2)))
    {
        if (this.Options.PreserveSourceNewlines)
        {
            return this.getEffectiveLines((includeComments) =>
{
    return getLinesBetweenRangeEndAndRangeStart(node1.Loc, node2.Loc, this.currentSourceFile, includeComments);
});
        }
        return Core.IfElse(rangeEndIsOnSameLineAsRangeStart(node1.Loc, node2.Loc, this.currentSourceFile), 0, 1);
    }
    return 0;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:482:1.
private int getEffectiveLines(Func<bool, int> getLineDifference)
{
    if (!this.Options.PreserveSourceNewlines)
    {
        throw new InvalidOperationException("Should not be called when preserveSourceNewlines is false");
    }
    var lines = getLineDifference(true);
    if ((lines == 0))
    {
        return getLineDifference(false);
    }
    return lines;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:508:1.
private int getLeadingLineTerminatorCount(Node parentNode, Node firstChild, ListFormat format)
{
    if ((((format & ListFormat.PreserveLines) != 0) || this.Options.PreserveSourceNewlines))
    {
        if (((format & ListFormat.PreferNewLine) != 0))
        {
            return 1;
        }
        if ((firstChild == null))
        {
            return Core.IfElse(((parentNode == null) || ((this.currentSourceFile != null) && RangeIsOnSingleLine(parentNode.Loc, this.currentSourceFile))), 0, 1);
        }
        if (((this.nextListElementPos > 0) && (firstChild.Pos() == this.nextListElementPos)))
        {
            return 0;
        }
        if ((firstChild.Kind == Kind.JsxText))
        {
            return 0;
        }
        if ((((((this.currentSourceFile != null) && (parentNode != null)) && !Ast.PositionIsSynthesized(parentNode.Pos())) && !Ast.NodeIsSynthesized(firstChild)) && ((firstChild.Parent == null))))
        {
            if (this.Options.PreserveSourceNewlines)
            {
                return this.getEffectiveLines((includeComments) =>
{
    return getLinesBetweenPositionAndPrecedingNonWhitespaceCharacter(firstChild.Pos(), parentNode.Pos(), this.currentSourceFile, includeComments);
});
            }
            return Core.IfElse(RangeStartPositionsAreOnSameLine(parentNode.Loc, firstChild.Loc, this.currentSourceFile), 0, 1);
        }
        if (this.shouldEmitOnNewLine(firstChild, format))
        {
            return 1;
        }
    }
    return Core.IfElse(((format & ListFormat.MultiLine) != 0), 1, 0);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:564:1.
private int getSeparatingLineTerminatorCount(Node previousNode, Node nextNode, ListFormat format)
{
    if ((((format & ListFormat.PreserveLines) != 0) || this.Options.PreserveSourceNewlines))
    {
        if (((previousNode == null) || (nextNode == null)))
        {
            return 0;
        }
        if ((nextNode.Kind == Kind.JsxText))
        {
            return 0;
        }
        else
        {
            if ((((this.currentSourceFile != null) && !Ast.NodeIsSynthesized(previousNode)) && !Ast.NodeIsSynthesized(nextNode)))
            {
                if ((this.Options.PreserveSourceNewlines && siblingNodePositionsAreComparable(this.emitContext, previousNode, nextNode)))
                {
                    return this.getEffectiveLines((includeComments) =>
{
    return getLinesBetweenRangeEndAndRangeStart(previousNode.Loc, nextNode.Loc, this.currentSourceFile, includeComments);
});
                }
                else
                {
                    if ((!this.Options.PreserveSourceNewlines && originalNodesHaveSameParent(this.emitContext, previousNode, nextNode)))
                    {
                        return Core.IfElse(rangeEndIsOnSameLineAsRangeStart(previousNode.Loc, nextNode.Loc, this.currentSourceFile), 0, 1);
                    }
                }
                return Core.IfElse(((format & ListFormat.PreferNewLine) != 0), 1, 0);
            }
            else
            {
                if ((this.shouldEmitOnNewLine(previousNode, format) || this.shouldEmitOnNewLine(nextNode, format)))
                {
                    return 1;
                }
            }
        }
    }
    else
    {
        if (this.shouldEmitOnNewLine(nextNode, ListFormat.None))
        {
            return 1;
        }
    }
    return Core.IfElse(((format & ListFormat.MultiLine) != 0), 1, 0);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:604:1.
private int getClosingLineTerminatorCount(Node parentNode, Node lastChild, ListFormat format, TextRange childrenTextRange)
{
    if ((((format & ListFormat.PreserveLines) != 0) || this.Options.PreserveSourceNewlines))
    {
        if (((format & ListFormat.PreferNewLine) != 0))
        {
            return 1;
        }
        if ((lastChild == null))
        {
            return Core.IfElse(((parentNode == null) || ((this.currentSourceFile != null) && RangeIsOnSingleLine(parentNode.Loc, this.currentSourceFile))), 0, 1);
        }
        if ((((((this.currentSourceFile != null) && (parentNode != null)) && !Ast.PositionIsSynthesized(parentNode.Pos())) && !Ast.NodeIsSynthesized(lastChild)) && (((lastChild.Parent == null) || (lastChild.Parent == parentNode)))))
        {
            if (this.Options.PreserveSourceNewlines)
            {
                var end = greatestEnd(lastChild.End(), childrenTextRange);
                return this.getEffectiveLines((includeComments) =>
{
    return getLinesBetweenPositionAndNextNonWhitespaceCharacter(end, parentNode.End(), this.currentSourceFile, includeComments);
});
            }
            return Core.IfElse(rangeEndPositionsAreOnSameLine(parentNode.Loc, lastChild.Loc, this.currentSourceFile), 0, 1);
        }
        if (this.shouldEmitOnNewLine(lastChild, format))
        {
            return 1;
        }
    }
    if ((((format & ListFormat.MultiLine) != 0) && ((format & ListFormat.NoTrailingNewLine) == 0)))
    {
        return 1;
    }
    return 0;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:638:1.
private void writeCommentRange(CommentRange comment)
{
    if ((this.currentSourceFile == null))
    {
        return ;
    }
    var text = this.currentSourceFile.TextView();
    var lineMap = this.currentSourceFile.ECMALineMap();
    this.writeCommentRangeWorker(text, lineMap, comment.Kind, comment.Loc);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:648:1.
private void writeCommentRangeWorker(ByteStringView text, Slice<int> lineMap, Kind kind, TextRange loc)
{
    if ((kind == Kind.MultiLineCommentTrivia))
    {
        var indentSize = TextWriter.GetDefaultIndentSize();
        var firstLine = ByteScanner.ComputeLineOfPosition(lineMap.AsSpan(), loc.Pos());
        var lineCount = GoRuntime.Len(lineMap);
        var firstCommentLineIndent = -1;
        var pos = loc.Pos();
        var currentLine = firstLine;
        for (; (pos < loc.End()); currentLine++)
        {
            int nextLineStart = default;
            if (((currentLine + 1) == lineCount))
            {
                nextLineStart = (GoRuntime.Len(text) + 1);
            }
            else
            {
                nextLineStart = (int)(lineMap[(currentLine + 1)]);
            }
            if ((pos != loc.Pos()))
            {
                if ((firstCommentLineIndent == -1))
                {
                    firstCommentLineIndent = calculateIndent(text, (int)(lineMap[firstLine]), loc.Pos());
                }
                var currentWriterIndentSpacing = (this.writer.GetIndent() * indentSize);
                var spacesToEmit = ((currentWriterIndentSpacing - firstCommentLineIndent) + calculateIndent(text, pos, nextLineStart));
                if ((spacesToEmit > 0))
                {
                    var numberOfSingleSpacesToEmit = (spacesToEmit % indentSize);
                    var indentSizeSpaceString = TextWriter.getIndentString((((spacesToEmit - numberOfSingleSpacesToEmit)) / indentSize), indentSize);
                    this.writer.RawWrite(indentSizeSpaceString);
                    while ((numberOfSingleSpacesToEmit > 0))
                    {
                        this.writer.RawWrite(" ");
                        numberOfSingleSpacesToEmit--;
                    }
                }
                else
                {
                    this.writer.RawWrite("");
                }
            }
            var end = Math.Min(loc.End(), nextLineStart);
            {
                var scan = pos;
                while ((scan < end))
                {
                    var tuple3 = Utf8.DecodeRuneInString(text.Slice(scan, end).AsSpan()); var ch = tuple3.Item1; var size = tuple3.Item2;
                    if ((size == 0))
                    {
                        break;
                    }
                    if (StringUtil.IsLineBreak(ch))
                    {
                        end = scan;
                        break;
                    }
                    scan += size;
                }
            }
            var currentLineText = GoStrings.TrimRightFunc(GoStrings.TrimLeftFunc(text.Slice(pos, end), GoUnicode.IsSpace), GoUnicode.IsSpace);
            if ((GoRuntime.Len(currentLineText) > 0))
            {
                this.writeComment(ByteStringValue.FromView(currentLineText));
                if ((end != loc.End()))
                {
                    this.writeLine();
                }
            }
            else
            {
                this.writer.WriteLineForce(true);
            }
            pos = nextLineStart;
        }
    }
    else
    {
        this.writeComment(ByteStringValue.FromView(text.Slice(loc.Pos(), loc.End())));
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:742:1.
private bool shouldEmitComments(Node node)
{
    return ((!this.commentsDisabled && (this.currentSourceFile != null)) && !Ast.IsSourceFile(node));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:748:1.
private bool shouldWriteComment(CommentRange comment)
{
    return ((!this.Options.OnlyPrintJSDocStyle || ((this.currentSourceFile != null) && isJSDocLikeText(this.currentSourceFile.TextView(), comment))) || ((this.currentSourceFile != null) && IsPinnedComment(this.currentSourceFile.TextView(), comment)));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:754:1.
private bool shouldEmitIndented(Node node)
{
    return ((this.emitContext.EmitFlags(node) & EmitFlags.Indented) != 0);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:758:1.
private bool shouldElideIndentation(Node node)
{
    return ((this.emitContext.EmitFlags(node) & EmitFlags.NoIndentation) != 0);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:762:1.
private bool shouldEmitOnSingleLine(Node node)
{
    return ((this.emitContext.EmitFlags(node) & EmitFlags.SingleLine) != 0);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:766:1.
private bool shouldEmitOnMultipleLines(Node node)
{
    return ((this.emitContext.EmitFlags(node) & EmitFlags.MultiLine) != 0);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:770:1.
private bool shouldEmitBlockFunctionBodyOnSingleLine(Block body)
{
    if (this.shouldEmitOnSingleLine(body.AsNode()))
    {
        return true;
    }
    if (body.MultiLine)
    {
        return false;
    }
    if (((!Ast.NodeIsSynthesized(body.AsNode()) && (this.currentSourceFile != null)) && !RangeIsOnSingleLine(body.Loc, this.currentSourceFile)))
    {
        return false;
    }
    if (((this.getLeadingLineTerminatorCount(body.AsNode(), Core.FirstOrNil(body.Statements.Nodes), ListFormat.PreserveLines) > 0) || (this.getClosingLineTerminatorCount(body.AsNode(), Core.LastOrNil(body.Statements.Nodes), ListFormat.PreserveLines, body.Statements.Loc) > 0)))
    {
        return false;
    }
    Node previousStatement = default;
    {
        foreach (var statement in GoRuntime.RangeValues(body.Statements.Nodes))
        {
            if ((this.getSeparatingLineTerminatorCount(previousStatement, statement, ListFormat.PreserveLines) > 0))
            {
                return false;
            }
            previousStatement = statement;
        }
    }
    return true;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:808:1.
private bool shouldEmitOnNewLine(Node node, ListFormat format)
{
    if (((this.emitContext.EmitFlags(node) & EmitFlags.StartOnNewLine) != 0))
    {
        return true;
    }
    return ((format & ListFormat.PreferNewLine) != 0);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:815:1.
private bool shouldEmitSourceMaps(Node node)
{
    return (((!this.sourceMapsDisabled && (this.sourceMapSource != null)) && !Ast.IsSourceFile(node)) && !Ast.IsInJsonFile(node));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:822:1.
private bool shouldEmitTokenSourceMaps(Kind token, int pos, Node contextNode, tokenEmitFlags flags)
{
    return (((((flags & tokenEmitFlags.NoSourceMaps) == 0) && this.shouldEmitSourceMaps(contextNode)) && !this.Options.OmitBraceSourceMapPositions) && (((token == Kind.OpenBraceToken) || (token == Kind.CloseBraceToken))));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:832:1.
private bool shouldEmitLeadingComments(Node node)
{
    return ((this.emitContext.EmitFlags(node) & EmitFlags.NoLeadingComments) == 0);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:836:1.
private bool shouldEmitTrailingComments(Node node)
{
    return ((this.emitContext.EmitFlags(node) & EmitFlags.NoTrailingComments) == 0);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:840:1.
private bool shouldEmitNestedComments(Node node)
{
    return ((this.emitContext.EmitFlags(node) & EmitFlags.NoNestedComments) == 0);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:844:1.
private bool shouldEmitDetachedComments(Node node)
{
    if (!Ast.IsSourceFile(node))
    {
        return true;
    }
    var file = node.AsSourceFile();
    return (((GoRuntime.Len(file.Statements.Nodes) == 0) || !Ast.IsPrologueDirective(file.Statements.Nodes[0])) || Ast.NodeIsSynthesized(file.Statements.Nodes[0]));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:858:1.
private bool hasCommentsAtPosition(int pos)
{
    if ((this.currentSourceFile == null))
    {
        return false;
    }
    {
        foreach (var ignored5 in GoRuntime.RangeValues(ByteScanner.GetTrailingCommentRanges(this.emitContext.Factory, this.currentSourceFile.TextView(), (pos + 1))))
        {
            return true;
        }
    }
    {
        foreach (var ignored6 in GoRuntime.RangeValues(ByteScanner.GetLeadingCommentRanges(this.emitContext.Factory, this.currentSourceFile.TextView(), (pos + 1))))
        {
            return true;
        }
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:872:1.
private bool shouldEmitIndirectCall(Node node)
{
    return ((this.emitContext.EmitFlags(node) & EmitFlags.IndirectCall) != 0);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:876:1.
private bool shouldAllowTrailingComma(Node node, NodeList list)
{
    if (((this.currentSourceFile == null) || (this.currentSourceFile.ScriptKind == ScriptKind.JSON)))
    {
        return false;
    }
    switch (node.Kind)
    {
        case Kind.ObjectLiteralExpression:
        {
            return true;
        }
        case Kind.ArrayLiteralExpression:
        case Kind.ArrowFunction:
        case Kind.Constructor:
        case Kind.GetAccessor:
        case Kind.SetAccessor:
        case Kind.TypeAliasDeclaration:
        case Kind.JSTypeAliasDeclaration:
        case Kind.FunctionType:
        case Kind.ConstructorType:
        case Kind.CallSignature:
        case Kind.ConstructSignature:
        case Kind.TaggedTemplateExpression:
        case Kind.ObjectBindingPattern:
        case Kind.ArrayBindingPattern:
        case Kind.NamedImports:
        case Kind.NamedExports:
        case Kind.ImportAttributes:
        {
            return true;
        }
        case Kind.ClassExpression:
        case Kind.ClassDeclaration:
        case Kind.InterfaceDeclaration:
        {
            return (list == node.TypeParameterList());
        }
        case Kind.FunctionDeclaration:
        case Kind.FunctionExpression:
        case Kind.MethodDeclaration:
        {
            return true;
        }
        case Kind.CallExpression:
        {
            return true;
        }
        case Kind.NewExpression:
        {
            return true;
        }
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:923:1.
private int writeTokenText(Kind token, WriteKind writeKind, int pos)
{
    var tokenString = ByteScanner.TokenToString(token);
    this.writeAs(tokenString, writeKind);
    if (Ast.PositionIsSynthesized(pos))
    {
        return pos;
    }
    else
    {
        return (pos + GoRuntime.Len(tokenString));
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:935:1.
private int emitToken(Kind token, int pos, WriteKind writeKind, Node contextNode)
{
    return this.emitTokenEx(token, pos, writeKind, contextNode, tokenEmitFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:939:1.
private int emitTokenEx(Kind token, int pos, WriteKind writeKind, Node contextNode, tokenEmitFlags flags)
{
    var tuple7 = this.enterToken(token, pos, contextNode, flags); var state = tuple7.Item1; pos = tuple7.Item2;
    pos = this.writeTokenText(token, writeKind, pos);
    this.exitToken(token, pos, contextNode, state);
    return pos;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:946:1.
private void emitKeywordNode(Node node)
{
    this.emitKeywordNodeEx(node, tokenEmitFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:950:1.
private void emitKeywordNodeEx(Node node, tokenEmitFlags flags)
{
    if ((node == null))
    {
        return ;
    }
    var state = this.enterTokenNode(node, flags);
    this.writeTokenText(node.Kind, WriteKind.Keyword, node.Pos());
    this.exitTokenNode(node, state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:960:1.
private void emitPunctuationNode(Node node)
{
    this.emitPunctuationNodeEx(node, tokenEmitFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:964:1.
private void emitPunctuationNodeEx(Node node, tokenEmitFlags flags)
{
    if ((node == null))
    {
        return ;
    }
    var state = this.enterTokenNode(node, flags);
    this.writeTokenText(node.Kind, WriteKind.Punctuation, node.Pos());
    this.exitTokenNode(node, state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:974:1.
private void emitTokenNode(Node node)
{
    this.emitTokenNodeEx(node, tokenEmitFlags.None);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:978:1.
private void emitTokenNodeEx(Node node, tokenEmitFlags flags)
{
    if ((node == null))
    {
        return ;
    }
    switch (true)
    {
        case true when Ast.IsKeywordKind(node.Kind):
        {
            this.emitKeywordNodeEx(node, flags);
            break;
        }
        case true when Ast.IsPunctuationKind(node.Kind):
        {
            this.emitPunctuationNodeEx(node, flags);
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected TokenNode: %v", node.Kind));
        }
    }
}

// Emits literals of the following kinds
//
//	SyntaxKindNumericLiteral
//	SyntaxKindBigIntLiteral
//	SyntaxKindStringLiteral
//	SyntaxKindNoSubstitutionTemplateLiteral
//	SyntaxKindRegularExpressionLiteral
//	SyntaxKindTemplateHead
//	SyntaxKindTemplateMiddle
//	SyntaxKindTemplateTail
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1007:1.
private void emitLiteral(Node node, getLiteralTextFlags flags)
{
    if (this.Options.NeverAsciiEscape)
    {
        flags |= getLiteralTextFlags.NeverAsciiEscape;
    }
    if (this.Options.TerminateUnterminatedLiterals)
    {
        flags |= getLiteralTextFlags.TerminateUnterminatedLiterals;
    }
    var text = this.getLiteralTextOfNodeValue(node, null, flags);
    this.writer.WriteStringLiteral(text);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1035:1.
private void emitNumericLiteral(NumericLiteral node)
{
    var state = this.enterNode(node.AsNode());
    this.emitLiteral(node.AsNode(), getLiteralTextFlags.None);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1041:1.
private void emitBigIntLiteral(BigIntLiteral node)
{
    var state = this.enterNode(node.AsNode());
    this.emitLiteral(node.AsNode(), getLiteralTextFlags.None);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1047:1.
private void emitStringLiteral(StringLiteral node)
{
    var state = this.enterNode(node.AsNode());
    this.emitLiteral(node.AsNode(), getLiteralTextFlags.None);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1053:1.
private void emitNoSubstitutionTemplateLiteral(NoSubstitutionTemplateLiteral node)
{
    var state = this.enterNode(node.AsNode());
    this.emitLiteral(node.AsNode(), getLiteralTextFlags.None);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1059:1.
private void emitRegularExpressionLiteral(RegularExpressionLiteral node)
{
    var state = this.enterNode(node.AsNode());
    this.emitLiteral(node.AsNode(), getLiteralTextFlags.None);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1069:1.
private void emitTemplateHead(TemplateHead node)
{
    var state = this.enterNode(node.AsNode());
    this.emitLiteral(node.AsNode(), getLiteralTextFlags.None);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1075:1.
private void emitTemplateMiddle(TemplateMiddle node)
{
    var state = this.enterNode(node.AsNode());
    this.emitLiteral(node.AsNode(), getLiteralTextFlags.None);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1081:1.
private void emitTemplateTail(TemplateTail node)
{
    var state = this.enterNode(node.AsNode());
    this.emitLiteral(node.AsNode(), getLiteralTextFlags.None);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1087:1.
private void emitTemplateMiddleTail(Node node)
{
    switch (node.Kind)
    {
        case Kind.TemplateMiddle:
        {
            this.emitTemplateMiddle(node.AsTemplateMiddle());
            break;
        }
        case Kind.TemplateTail:
        {
            this.emitTemplateTail(node.AsTemplateTail());
            break;
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1100:1.
private void emitSnippetNode(Node node, SnippetElement snippetElement)
{
    switch (snippetElement.Kind)
    {
        case SnippetKind.TabStop:
        {
            this.emitTabStop(node, snippetElement);
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("Unhandled snippet element kind: %v", snippetElement.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1109:1.
private void emitTabStop(Node node, SnippetElement snippetElement)
{
    Debug.Assert((node.Kind == Kind.EmptyStatement), "Snippet tab stops can only be emitted on empty statements");
    this.writer.RawWrite(Fmt.Sprintf("$%d", snippetElement.Order));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1118:1.
private void emitIdentifierText(Identifier node)
{
    var f = Ast.GetSourceFileOfNode(node.AsNode());
    Debug.Assert((((f == null) || (this.currentSourceFile == null)) || (f.FileName() == this.currentSourceFile.FileName())));
    var text = this.getTextOfNodeValue(node.AsNode(), false);
    if ((this.IdToSymbol != null))
    {
        {
            var ok = this.IdToSymbol.TryGetValue(node.AsNode(), out var symbol);
            if (ok)
            {
                this.writeSymbol(text, symbol);
                return ;
            }
        }
    }
    this.write(text);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1132:1.
private void emitIdentifierName(Identifier node)
{
    var state = this.enterNode(node.AsNode());
    this.emitIdentifierText(node);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1138:1.
private void emitIdentifierNameNode(Node node)
{
    if ((node == null))
    {
        return ;
    }
    this.emitIdentifierName(node.AsIdentifier());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1145:1.
private Node getUniqueHelperName(string name)
{
    var helperName = this.uniqueHelperNames.GetValueOrDefault(name);
    if ((helperName == null))
    {
        var helperName2 = this.emitContext.Factory.NewUniqueNameEx(name, new AutoGenerateOptions { Flags = (GeneratedIdentifierFlags.FileLevel | GeneratedIdentifierFlags.Optimistic) });
        this.generateName(helperName2);
        this.uniqueHelperNames[name] = helperName2;
        return helperName2;
    }
    return helperName.Clone(this.emitContext.Factory);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1156:1.
private void emitIdentifierReference(Identifier node)
{
    if (((((this.externalHelpersModuleName != null) || (this.uniqueHelperNames != null))) && ((this.emitContext.EmitFlags(node.AsNode()) & EmitFlags.HelperName) != 0)))
    {
        if ((this.externalHelpersModuleName != null))
        {
            var helper = this.emitContext.Factory.NewPropertyAccessExpression(this.externalHelpersModuleName.Clone(this.emitContext.Factory), null, node.Clone(this.emitContext.Factory), NodeFlags.None);
            this.emitContext.AssignCommentAndSourceMapRanges(helper, node.AsNode());
            this.emitPropertyAccessExpression(helper.AsPropertyAccessExpression());
            return ;
        }
        if ((this.uniqueHelperNames != null))
        {
            var helperName = this.getUniqueHelperName(node.Text);
            this.emitContext.AssignCommentAndSourceMapRanges(helperName, node.AsNode());
            node = helperName.AsIdentifier();
        }
    }
    var state = this.enterNode(node.AsNode());
    this.emitIdentifierText(node);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1184:1.
private void emitBindingIdentifier(Identifier node)
{
    if (((this.uniqueHelperNames != null) && ((this.emitContext.EmitFlags(node.AsNode()) & EmitFlags.HelperName) != 0)))
    {
        var helperName = this.getUniqueHelperName(node.Text);
        this.emitContext.AssignCommentAndSourceMapRanges(helperName, node.AsNode());
        node = helperName.AsIdentifier();
    }
    var state = this.enterNode(node.AsNode());
    this.emitIdentifierText(node);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1198:1.
private void emitLabelIdentifier(Identifier node)
{
    var state = this.enterNode(node.AsNode());
    this.emitIdentifierText(node);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1204:1.
private void emitPrivateIdentifier(PrivateIdentifier node)
{
    var state = this.enterNode(node.AsNode());
    this.write(this.getTextOfNodeValue(node.AsNode(), false));
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1210:1.
private void emitQualifiedName(QualifiedName node)
{
    var state = this.enterNode(node.AsNode());
    this.emitEntityName(node.Left);
    this.writePunctuation(".");
    this.emitMemberName(node.Right);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1218:1.
private void emitComputedPropertyName(ComputedPropertyName node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("[");
    this.emitExpression(node.Expression, OperatorPrecedence.DisallowComma);
    this.writePunctuation("]");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1226:1.
private void emitEntityName(Node node)
{
    switch (node.Kind)
    {
        case Kind.Identifier:
        {
            this.emitIdentifierReference(node.AsIdentifier());
            break;
        }
        case Kind.QualifiedName:
        {
            this.emitQualifiedName(node.AsQualifiedName());
            break;
        }
        case Kind.PropertyAccessExpression:
        {
            this.emitExpression(node, OperatorPrecedence.DisallowComma);
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected EntityName: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1241:1.
private void emitBindingName(Node node)
{
    if ((node == null))
    {
        return ;
    }
    switch (node.Kind)
    {
        case Kind.Identifier:
        {
            this.emitBindingIdentifier(node.AsIdentifier());
            break;
        }
        case Kind.ObjectBindingPattern:
        {
            this.emitObjectBindingPattern(node.AsBindingPattern());
            break;
        }
        case Kind.ArrayBindingPattern:
        {
            this.emitArrayBindingPattern(node.AsBindingPattern());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected BindingName: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1258:1.
private void emitPropertyName(Node node)
{
    if ((node == null))
    {
        return ;
    }
    var savedWriteKind = this.writeKind;
    this.writeKind = WriteKind.Property;
    switch (node.Kind)
    {
        case Kind.Identifier:
        {
            this.emitIdentifierName(node.AsIdentifier());
            break;
        }
        case Kind.PrivateIdentifier:
        {
            this.emitPrivateIdentifier(node.AsPrivateIdentifier());
            break;
        }
        case Kind.StringLiteral:
        {
            this.emitStringLiteral(node.AsStringLiteral());
            break;
        }
        case Kind.NoSubstitutionTemplateLiteral:
        {
            this.emitNoSubstitutionTemplateLiteral(node.AsNoSubstitutionTemplateLiteral());
            break;
        }
        case Kind.NumericLiteral:
        {
            this.emitNumericLiteral(node.AsNumericLiteral());
            break;
        }
        case Kind.BigIntLiteral:
        {
            this.emitBigIntLiteral(node.AsBigIntLiteral());
            break;
        }
        case Kind.ComputedPropertyName:
        {
            this.emitComputedPropertyName(node.AsComputedPropertyName());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected PropertyName: %v", node.Kind));
        }
    }
    this.writeKind = savedWriteKind;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1288:1.
private void emitMemberName(Node node)
{
    if ((node == null))
    {
        return ;
    }
    switch (node.Kind)
    {
        case Kind.Identifier:
        {
            this.emitIdentifierName(node.AsIdentifier());
            break;
        }
        case Kind.PrivateIdentifier:
        {
            this.emitPrivateIdentifier(node.AsPrivateIdentifier());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected MemberName: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1303:1.
private void emitModuleName(Node node)
{
    if ((node == null))
    {
        return ;
    }
    switch (node.Kind)
    {
        case Kind.Identifier:
        {
            this.emitBindingIdentifier(node.AsIdentifier());
            break;
        }
        case Kind.StringLiteral:
        {
            this.emitStringLiteral(node.AsStringLiteral());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected ModuleName: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1318:1.
private void emitModuleExportName(Node node)
{
    if ((node == null))
    {
        return ;
    }
    switch (node.Kind)
    {
        case Kind.Identifier:
        {
            this.emitIdentifierName(node.AsIdentifier());
            break;
        }
        case Kind.StringLiteral:
        {
            this.emitStringLiteral(node.AsStringLiteral());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected ModuleExportName: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1333:1.
private void emitImportAttributeName(Node node)
{
    switch (node.Kind)
    {
        case Kind.Identifier:
        {
            this.emitIdentifierName(node.AsIdentifier());
            break;
        }
        case Kind.StringLiteral:
        {
            this.emitStringLiteral(node.AsStringLiteral());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected ImportAttributeName: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1344:1.
private void emitNestedModuleName(Node node)
{
    if ((node == null))
    {
        return ;
    }
    switch (node.Kind)
    {
        case Kind.Identifier:
        {
            this.emitIdentifierName(node.AsIdentifier());
            break;
        }
        case Kind.StringLiteral:
        {
            this.emitStringLiteral(node.AsStringLiteral());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected ModuleName: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1363:1.
private int emitModifierList(Node parentNode, ModifierList modifiers, bool allowDecorators)
{
    if (((modifiers == null) || (GoRuntime.Len(modifiers.Nodes) == 0)))
    {
        return parentNode.Pos();
    }
    if (Core.Every(modifiers.Nodes, Ast.IsModifier))
    {
        this.emitList((binderCallback, argnode) => binderCallback.emitKeywordNode(argnode), parentNode, modifiers, ListFormat.Modifiers);
    }
    else
    {
        if (Core.Every(modifiers.Nodes, Ast.IsDecorator))
        {
            if (!allowDecorators)
            {
                return parentNode.Pos();
            }
            this.emitList((binderCallback, argnode) => binderCallback.emitModifierLike(argnode), parentNode, modifiers, ListFormat.Decorators);
        }
        else
        {
            if ((this.OnBeforeEmitNodeList != null))
            {
                this.OnBeforeEmitNodeList(modifiers);
            }
            const int ModeNone = 0;
            const int ModeModifiers = 1;
            const int ModeDecorators = 2;
            var lastMode = ModeNone;
            var mode = ModeNone;
            var start = 0;
            var pos = 0;
            Node lastModifier = default;
            while ((start < GoRuntime.Len(modifiers.Nodes)))
            {
                while ((pos < GoRuntime.Len(modifiers.Nodes)))
                {
                    lastModifier = modifiers.Nodes[pos];
                    if (Ast.IsDecorator(lastModifier))
                    {
                        mode = ModeDecorators;
                    }
                    else
                    {
                        mode = ModeModifiers;
                    }
                    if ((lastMode == ModeNone))
                    {
                        lastMode = mode;
                    }
                    else
                    {
                        if ((mode != lastMode))
                        {
                            break;
                        }
                    }
                    pos++;
                }
                var textRange = Core.NewTextRange(-1, -1);
                if ((start == 0))
                {
                    textRange = Core.NewTextRange(modifiers.Pos(), textRange.End());
                }
                if ((pos == (GoRuntime.Len(modifiers.Nodes) - 1)))
                {
                    textRange = Core.NewTextRange(textRange.Pos(), modifiers.End());
                }
                if ((allowDecorators || (lastMode == ModeModifiers)))
                {
                    this.emitListItems((binderCallback, argnode) => binderCallback.emitModifierLike(argnode), parentNode, modifiers.Nodes.Slice2(start, pos), Core.IfElse((lastMode == ModeModifiers), ListFormat.Modifiers, ListFormat.Decorators), false, textRange);
                }
                start = pos;
                lastMode = mode;
                pos++;
            }
            if ((this.OnAfterEmitNodeList != null))
            {
                this.OnAfterEmitNodeList(modifiers);
            }
        }
    }
    return greatestEnd(parentNode.Pos(), Core.LastOrNil(modifiers.Nodes));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1444:1.
private void emitTypeParameter(TypeParameterDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    this.emitBindingIdentifier(node.Name().AsIdentifier());
    if ((node.Constraint != null))
    {
        this.writeSpace();
        this.writeKeyword("extends");
        this.writeSpace();
        this.emitTypeNodeOutsideExtends(node.Constraint);
    }
    if ((node.DefaultType != null))
    {
        this.writeSpace();
        this.writeOperator("=");
        this.writeSpace();
        this.emitTypeNodeOutsideExtends(node.DefaultType);
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1463:1.
private void emitTypeParameterDeclarationNode(Node node)
{
    if (Ast.IsTypeParameterDeclaration(node))
    {
        this.emitTypeParameter(node.AsTypeParameterDeclaration());
    }
    else
    {
        this.emitTypeArgument(node);
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1473:1.
private void emitParameterName(Node node)
{
    var savedWriteKind = this.writeKind;
    this.writeKind = WriteKind.Parameter;
    this.emitBindingName(node);
    this.writeKind = savedWriteKind;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1480:1.
private void emitParameter(ParameterDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), true);
    this.emitTokenNode(node.DotDotDotToken);
    this.emitParameterName(node.Name());
    this.emitTokenNode(node.QuestionToken);
    this.emitTypeAnnotation(node.Type);
    this.emitInitializer(node.Initializer, greatestEnd(node.Pos(), node.Type, node.QuestionToken, node.Name(), node.Modifiers()), node.AsNode());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1495:1.
private void emitParameterDeclarationNode(Node node)
{
    this.emitParameter(node.AsParameterDeclaration());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1499:1.
private void emitDecorator(Decorator node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("@");
    this.emitExpression(node.Expression, OperatorPrecedence.LeftHandSide);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1506:1.
private void emitModifierLike(Node node)
{
    switch (true)
    {
        case true when Ast.IsDecorator(node):
        {
            this.emitDecorator(node.AsDecorator());
            break;
        }
        case true when Ast.IsModifier(node):
        {
            this.emitKeywordNode(node);
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled ModifierLike: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1517:1.
private void emitTypeParameters(Node parentNode, NodeList nodes)
{
    if ((nodes == null))
    {
        return ;
    }
    this.emitList((binderCallback, argnode) => binderCallback.emitTypeParameterDeclarationNode(argnode), parentNode, nodes, (ListFormat.TypeParameters | Core.IfElse(Ast.IsArrowFunction(parentNode), ListFormat.AllowTrailingComma, ListFormat.None)));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1524:1.
private void emitTypeAnnotation(Node node)
{
    if ((node == null))
    {
        return ;
    }
    this.writePunctuation(":");
    this.writeSpace();
    this.emitTypeNodeOutsideExtends(node);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1534:1.
private void emitInitializer(Node node, int equalTokenPos, Node contextNode)
{
    if ((node == null))
    {
        return ;
    }
    this.writeSpace();
    this.emitToken(Kind.EqualsToken, equalTokenPos, WriteKind.Operator, contextNode);
    this.writeSpace();
    this.emitExpression(node, OperatorPrecedence.DisallowComma);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1545:1.
private void emitParameters(Node parentNode, NodeList parameters)
{
    this.generateAllNames(parameters);
    this.emitList((binderCallback, argnode) => binderCallback.emitParameterDeclarationNode(argnode), parentNode, parameters, ListFormat.Parameters);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1550:1.
private static bool canEmitSimpleArrowHead(Node parentNode, NodeList parameters)
{
    if ((!Ast.IsArrowFunction(parentNode) || (GoRuntime.Len(parameters.Nodes) != 1)))
    {
        return false;
    }
    var parent = parentNode.AsArrowFunction();
    var parameter = parameters.Nodes[0].AsParameterDeclaration();
    return (((((((((((parameter.Pos() == parent.Pos()) && (parent.TypeParameters == null)) && (parent.Type == null)) && (((parent.Modifiers() == null) || (GoRuntime.Len(parent.Modifiers().Nodes) == 0)))) && !parameters.HasTrailingComma()) && (parameter.Modifiers() == null)) && (parameter.DotDotDotToken == null)) && (parameter.QuestionToken == null)) && (parameter.Type == null)) && (parameter.Initializer == null)) && Ast.IsIdentifier(parameter.Name()));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1572:1.
private void emitParametersForArrow(Node parentNode, NodeList parameters)
{
    if (canEmitSimpleArrowHead(parentNode, parameters))
    {
        this.generateAllNames(parameters);
        this.emitList((binderCallback, argnode) => binderCallback.emitParameterDeclarationNode(argnode), parentNode, parameters, ListFormat.SingleArrowParameter);
    }
    else
    {
        this.emitParameters(parentNode, parameters);
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1581:1.
private void emitParametersForIndexSignature(Node parentNode, NodeList parameters)
{
    this.generateAllNames(parameters);
    this.emitList((binderCallback, argnode) => binderCallback.emitParameterDeclarationNode(argnode), parentNode, parameters, ListFormat.IndexSignatureParameters);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1586:1.
private void emitSignature(Node node)
{
    var n = node.FunctionLikeData();
    this.emitTypeParameters(node, n.TypeParameters);
    this.emitParameters(node, n.Parameters);
    this.emitTypeAnnotation(n.Type);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1600:1.
private void emitFunctionBody(Block body)
{
    this.emitContext.AddEmitFlags(body.AsNode(), EmitFlags.NoSourceMap);
    if ((this.OnBeforeEmitNode != null))
    {
        this.OnBeforeEmitNode(body.AsNode());
    }
    this.generateNames(body.AsNode());
    this.writePunctuation("{");
    this.increaseIndent();
    var detachedState = this.emitDetachedCommentsBeforeStatementList(body.AsNode(), body.Statements.Loc);
    var statementOffset = this.emitPrologueDirectives(body.Statements);
    var pos = this.writer.GetTextPos();
    this.emitHelpers(body.AsNode());
    if (((this.shouldEmitBlockFunctionBodyOnSingleLine(body) && (statementOffset == 0)) && (pos == this.writer.GetTextPos())))
    {
        this.decreaseIndent();
        this.emitListRange((binderCallback, argnode) => binderCallback.emitStatement(argnode), body.AsNode(), body.Statements, ListFormat.SingleLineFunctionBodyStatements, statementOffset, -1);
        this.increaseIndent();
    }
    else
    {
        this.emitListRange((binderCallback, argnode) => binderCallback.emitStatement(argnode), body.AsNode(), body.Statements, ListFormat.MultiLineFunctionBodyStatements, statementOffset, -1);
    }
    this.emitDetachedCommentsAfterStatementList(body.AsNode(), body.Statements.Loc, detachedState);
    this.decreaseIndent();
    this.emitTokenEx(Kind.CloseBraceToken, body.Statements.End(), WriteKind.Punctuation, body.AsNode(), tokenEmitFlags.NoComments);
    if ((this.OnAfterEmitNode != null))
    {
        this.OnAfterEmitNode(body.AsNode());
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1643:1.
private void emitFunctionBodyNode(Node node)
{
    if ((node == null))
    {
        this.writeTrailingSemicolon();
        return ;
    }
    this.writeSpace();
    this.emitFunctionBody(node.AsBlock());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1657:1.
private void emitPropertySignature(PropertySignatureDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    this.emitPropertyName(node.Name());
    this.emitTokenNode(node.PostfixToken);
    this.emitTypeAnnotation(node.Type);
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1667:1.
private void emitPropertyDeclaration(PropertyDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), true);
    this.emitPropertyName(node.Name());
    this.emitTokenNode(node.PostfixToken);
    this.emitTypeAnnotation(node.Type);
    this.emitInitializer(node.Initializer, greatestEnd(node.Name().End(), node.Type, node.PostfixToken), node.AsNode());
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1678:1.
private void emitMethodSignature(MethodSignatureDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    this.emitPropertyName(node.Name());
    this.emitTokenNode(node.PostfixToken);
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.emitSignature(node.AsNode());
    this.writeTrailingSemicolon();
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1693:1.
private void emitMethodDeclaration(MethodDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), true);
    this.emitTokenNode(node.AsteriskToken);
    this.emitPropertyName(node.Name());
    this.emitTokenNode(node.PostfixToken);
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.emitSignature(node.AsNode());
    this.emitFunctionBodyNode(node.Body);
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1709:1.
private void emitClassStaticBlockDeclaration(ClassStaticBlockDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.writeKeyword("static");
    this.pushNameGenerationScope(node.AsNode());
    this.emitFunctionBodyNode(node.Body);
    this.popNameGenerationScope(node.AsNode());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1718:1.
private void emitConstructor(ConstructorDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    this.writeKeyword("constructor");
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.emitSignature(node.AsNode());
    this.emitFunctionBodyNode(node.Body);
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1732:1.
private void emitAccessorDeclaration(Kind token, AccessorDeclarationBase node)
{
    var state = this.enterNode(node.AsNode());
    var pos = this.emitModifierList(node.AsNode(), node.Modifiers(), true);
    this.emitToken(token, pos, WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitPropertyName(node.Name());
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.emitSignature(node.AsNode());
    this.emitFunctionBodyNode(node.Body);
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1748:1.
private void emitGetAccessorDeclaration(GetAccessorDeclaration node)
{
    this.emitAccessorDeclaration(Kind.GetKeyword, node);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1752:1.
private void emitSetAccessorDeclaration(SetAccessorDeclaration node)
{
    this.emitAccessorDeclaration(Kind.SetKeyword, node);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1756:1.
private void emitCallSignature(CallSignatureDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.emitSignature(node.AsNode());
    this.writeTrailingSemicolon();
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1768:1.
private void emitConstructSignature(ConstructSignatureDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.writeKeyword("new");
    this.writeSpace();
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.emitSignature(node.AsNode());
    this.writeTrailingSemicolon();
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1782:1.
private void emitIndexSignature(IndexSignatureDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.emitParametersForIndexSignature(node.AsNode(), node.Parameters);
    this.emitTypeAnnotation(node.Type);
    this.writeTrailingSemicolon();
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1796:1.
private void emitClassElement(Node node)
{
    switch (node.Kind)
    {
        case Kind.PropertyDeclaration:
        {
            this.emitPropertyDeclaration(node.AsPropertyDeclaration());
            break;
        }
        case Kind.MethodDeclaration:
        {
            this.emitMethodDeclaration(node.AsMethodDeclaration());
            break;
        }
        case Kind.ClassStaticBlockDeclaration:
        {
            this.emitClassStaticBlockDeclaration(node.AsClassStaticBlockDeclaration());
            break;
        }
        case Kind.Constructor:
        {
            this.emitConstructor(node.AsConstructorDeclaration());
            break;
        }
        case Kind.GetAccessor:
        {
            this.emitGetAccessorDeclaration(node.AsGetAccessorDeclaration());
            break;
        }
        case Kind.SetAccessor:
        {
            this.emitSetAccessorDeclaration(node.AsSetAccessorDeclaration());
            break;
        }
        case Kind.IndexSignature:
        {
            this.emitIndexSignature(node.AsIndexSignatureDeclaration());
            break;
        }
        case Kind.SemicolonClassElement:
        {
            this.emitSemicolonClassElement(node.AsSemicolonClassElement());
            break;
        }
        case Kind.NotEmittedStatement:
        {
            this.emitNotEmittedStatement(node.AsNotEmittedStatement());
            break;
        }
        case Kind.JSTypeAliasDeclaration:
        {
            this.emitTypeAliasDeclaration(node.AsTypeAliasDeclaration());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected ClassElement: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1823:1.
private void emitTypeElement(Node node)
{
    switch (node.Kind)
    {
        case Kind.PropertySignature:
        {
            this.emitPropertySignature(node.AsPropertySignatureDeclaration());
            break;
        }
        case Kind.MethodSignature:
        {
            this.emitMethodSignature(node.AsMethodSignatureDeclaration());
            break;
        }
        case Kind.CallSignature:
        {
            this.emitCallSignature(node.AsCallSignatureDeclaration());
            break;
        }
        case Kind.ConstructSignature:
        {
            this.emitConstructSignature(node.AsConstructSignatureDeclaration());
            break;
        }
        case Kind.GetAccessor:
        {
            this.emitGetAccessorDeclaration(node.AsGetAccessorDeclaration());
            break;
        }
        case Kind.SetAccessor:
        {
            this.emitSetAccessorDeclaration(node.AsSetAccessorDeclaration());
            break;
        }
        case Kind.IndexSignature:
        {
            this.emitIndexSignature(node.AsIndexSignatureDeclaration());
            break;
        }
        case Kind.NotEmittedTypeElement:
        {
            this.emitNotEmittedTypeElement(node.AsNotEmittedTypeElement());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected TypeElement: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1846:1.
private void emitObjectLiteralElement(Node node)
{
    switch (node.Kind)
    {
        case Kind.PropertyAssignment:
        {
            this.emitPropertyAssignment(node.AsPropertyAssignment());
            break;
        }
        case Kind.ShorthandPropertyAssignment:
        {
            this.emitShorthandPropertyAssignment(node.AsShorthandPropertyAssignment());
            break;
        }
        case Kind.SpreadAssignment:
        {
            this.emitSpreadAssignment(node.AsSpreadAssignment());
            break;
        }
        case Kind.MethodDeclaration:
        {
            this.emitMethodDeclaration(node.AsMethodDeclaration());
            break;
        }
        case Kind.GetAccessor:
        {
            this.emitGetAccessorDeclaration(node.AsGetAccessorDeclaration());
            break;
        }
        case Kind.SetAccessor:
        {
            this.emitSetAccessorDeclaration(node.AsSetAccessorDeclaration());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled ObjectLiteralElement: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1869:1.
private void emitKeywordTypeNode(KeywordTypeNode node)
{
    this.emitKeywordNode(node.AsNode());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1873:1.
private void emitTypePredicateParameterName(Node node)
{
    switch (node.Kind)
    {
        case Kind.Identifier:
        {
            this.emitIdentifierReference(node.AsIdentifier());
            break;
        }
        case Kind.ThisType:
        {
            this.emitThisType(node.AsThisTypeNode());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected TypePredicateParameterName: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1884:1.
private void emitTypePredicate(TypePredicateNode node)
{
    var state = this.enterNode(node.AsNode());
    if ((node.AssertsModifier != null))
    {
        this.emitTokenNode(node.AssertsModifier);
        this.writeSpace();
    }
    this.emitTypePredicateParameterName(node.ParameterName);
    if ((node.Type != null))
    {
        this.writeSpace();
        this.writeKeyword("is");
        this.writeSpace();
        this.emitTypeNodeOutsideExtends(node.Type);
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1900:1.
private void emitTypeArgument(Node node)
{
    this.emitTypeNodeOutsideExtends(node);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1904:1.
private void emitTypeArguments(Node parentNode, NodeList nodes)
{
    if ((nodes == null))
    {
        return ;
    }
    this.emitList((binderCallback, argnode) => binderCallback.emitTypeParameterDeclarationNode(argnode), parentNode, nodes, ListFormat.TypeArguments);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1911:1.
private void emitTypeReference(TypeReferenceNode node)
{
    var state = this.enterNode(node.AsNode());
    this.emitEntityName(node.TypeName);
    this.emitTypeArguments(node.AsNode(), node.TypeArguments);
    this.exitNode(node.AsNode(), state);
}

// Emits the return type of a FunctionTypeNode or ConstructorTypeNode, including the arrow (`=>`)
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1919:1.
private void emitReturnType(Node node)
{
    if ((node == null))
    {
        return ;
    }
    this.writePunctuation("=>");
    this.writeSpace();
    if (((this.inExtends && (node.Kind == Kind.InferType)) && (node.AsInferTypeNode().TypeParameter.AsTypeParameterDeclaration().Constraint != null)))
    {
        this.emitTypeNodePreservingExtends(node, TypePrecedence.Highest);
    }
    else
    {
        this.emitTypeNodePreservingExtends(node, TypePrecedence.Lowest);
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1937:1.
private void emitFunctionType(FunctionTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.emitTypeParameters(node.AsNode(), node.TypeParameters);
    this.emitParameters(node.AsNode(), node.Parameters);
    this.writeSpace();
    this.emitReturnType(node.Type);
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1952:1.
private void emitConstructorType(ConstructorTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    this.writeKeyword("new");
    this.writeSpace();
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.emitTypeParameters(node.AsNode(), node.TypeParameters);
    this.emitParameters(node.AsNode(), node.Parameters);
    this.writeSpace();
    this.emitReturnType(node.Type);
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1970:1.
private void emitTypeQuery(TypeQueryNode node)
{
    var state = this.enterNode(node.AsNode());
    this.writeKeyword("typeof");
    this.writeSpace();
    this.emitEntityName(node.ExprName);
    this.emitTypeArguments(node.AsNode(), node.TypeArguments);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1979:1.
private void emitTypeLiteral(TypeLiteralNode node)
{
    var state = this.enterNode(node.AsNode());
    this.pushNameGenerationScope(node.AsNode());
    this.generateAllMemberNames(node.Members);
    this.writePunctuation("{");
    var flags = Core.IfElse(this.shouldEmitOnSingleLine(node.AsNode()), ListFormat.SingleLineTypeLiteralMembers, ListFormat.MultiLineTypeLiteralMembers);
    this.emitList((binderCallback, argnode) => binderCallback.emitTypeElement(argnode), node.AsNode(), node.Members, (flags | ListFormat.NoSpaceIfEmpty));
    this.writePunctuation("}");
    this.popNameGenerationScope(node.AsNode());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:1991:1.
private void emitArrayType(ArrayTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.emitPostfixTypeOperand(node.ElementType, node.AsNode());
    this.writePunctuation("[");
    this.writePunctuation("]");
    this.exitNode(node.AsNode(), state);
}

// emitPostfixTypeOperand emits the operand of a postfix type (ArrayType, IndexedAccessType,
// OptionalType). It is equivalent to `emitTypeNode(operand, TypePrecedencePostfix)` except
// that it preserves a parsed `typeof X` operand without adding parentheses (e.g.,
// `typeof C[K]` instead of `(typeof C)[K]`). TypeScript's `parenthesizeNonArrayTypeOfPostfixType`
// factory rule wraps `TypeQuery` in `ParenthesizedType` only when a postfix type is constructed
// via the factory, so parsed postfix types preserve the source as written during round-trip
// emit while synthesized postfix types (e.g., from declaration emit) still get the parentheses.
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2006:1.
private void emitPostfixTypeOperand(Node operand, Node parent)
{
    if ((Ast.IsParseTreeNode(parent) && (operand.Kind == Kind.TypeQuery)))
    {
        this.emitTypeNode(operand, TypePrecedence.TypeOperator);
        return ;
    }
    this.emitTypeNode(operand, TypePrecedence.Postfix);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2014:1.
private void emitTupleElementType(Node node)
{
    this.emitTypeNodeOutsideExtends(node);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2018:1.
private void emitTupleType(TupleTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.OpenBracketToken, node.Pos(), WriteKind.Punctuation, node.AsNode());
    var flags = Core.IfElse(this.shouldEmitOnSingleLine(node.AsNode()), ListFormat.SingleLineTupleTypeElements, ListFormat.MultiLineTupleTypeElements);
    this.emitList((binderCallback, argnode) => binderCallback.emitTupleElementType(argnode), node.AsNode(), node.Elements, (flags | ListFormat.NoSpaceIfEmpty));
    this.emitToken(Kind.CloseBracketToken, node.Elements.End(), WriteKind.Punctuation, node.AsNode());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2027:1.
private void emitRestType(RestTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("...");
    this.emitTypeNodeOutsideExtends(node.Type);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2034:1.
private void emitOptionalType(OptionalTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.emitPostfixTypeOperand(node.Type, node.AsNode());
    this.writePunctuation("?");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2042:1.
private void emitNamedTupleMember(NamedTupleMember node)
{
    var state = this.enterNode(node.AsNode());
    this.emitPunctuationNode(node.DotDotDotToken);
    this.emitIdentifierName(node.Name().AsIdentifier());
    this.emitPunctuationNode(node.QuestionToken);
    this.emitToken(Kind.ColonToken, greatestEnd(node.Name().End(), node.QuestionToken), WriteKind.Punctuation, node.AsNode());
    this.writeSpace();
    this.emitTypeNodeOutsideExtends(node.Type);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2053:1.
private void emitUnionTypeConstituent(Node node)
{
    this.emitTypeNode(node, TypePrecedence.TypeOperator);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2057:1.
private void emitUnionType(UnionTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.emitList((binderCallback, argnode) => binderCallback.emitUnionTypeConstituent(argnode), node.AsNode(), node.Types, ListFormat.UnionTypeConstituents);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2063:1.
private void emitIntersectionTypeConstituent(Node node)
{
    this.emitTypeNode(node, TypePrecedence.TypeOperator);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2067:1.
private void emitIntersectionType(IntersectionTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.emitList((binderCallback, argnode) => binderCallback.emitIntersectionTypeConstituent(argnode), node.AsNode(), node.Types, ListFormat.IntersectionTypeConstituents);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2073:1.
private void emitConditionalType(ConditionalTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.emitTypeNode(node.CheckType, TypePrecedence.Union);
    this.writeSpace();
    this.writeKeyword("extends");
    this.writeSpace();
    this.emitTypeNodeInExtends(node.ExtendsType);
    this.writeSpace();
    this.writePunctuation("?");
    this.writeSpace();
    this.emitTypeNodeOutsideExtends(node.TrueType);
    this.writeSpace();
    this.writePunctuation(":");
    this.writeSpace();
    this.emitTypeNodeOutsideExtends(node.FalseType);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2091:1.
private void emitInferTypeParameter(TypeParameterDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitBindingIdentifier(node.Name().AsIdentifier());
    if ((node.Constraint != null))
    {
        this.writeSpace();
        this.writeKeyword("extends");
        this.writeSpace();
        this.emitTypeNodeInExtends(node.Constraint);
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2103:1.
private void emitInferType(InferTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.writeKeyword("infer");
    this.writeSpace();
    this.emitInferTypeParameter(node.TypeParameter.AsTypeParameterDeclaration());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2111:1.
private void emitParenthesizedType(ParenthesizedTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("(");
    this.emitTypeNodeOutsideExtends(node.Type);
    this.writePunctuation(")");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2119:1.
private void emitThisType(ThisTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.writeKeyword("this");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2125:1.
private void emitTypeOperator(TypeOperatorNode node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(node.Operator, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitTypeNode(node.Type, Core.IfElse((node.Operator == Kind.ReadonlyKeyword), TypePrecedence.Postfix, TypePrecedence.TypeOperator));
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2133:1.
private void emitIndexedAccessType(IndexedAccessTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.emitPostfixTypeOperand(node.ObjectType, node.AsNode());
    this.writePunctuation("[");
    this.emitTypeNodeOutsideExtends(node.IndexType);
    this.writePunctuation("]");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2142:1.
private void emitMappedTypeParameter(TypeParameterDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitBindingIdentifier(node.Name().AsIdentifier());
    this.writeSpace();
    this.writeKeyword("in");
    this.writeSpace();
    this.emitTypeNodeOutsideExtends(node.Constraint);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2152:1.
private void emitMappedType(MappedTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    var singleLine = this.shouldEmitOnSingleLine(node.AsNode());
    this.writePunctuation("{");
    if (singleLine)
    {
        this.writeSpace();
    }
    else
    {
        this.writeLine();
        this.increaseIndent();
    }
    if ((node.ReadonlyToken != null))
    {
        this.emitTokenNode(node.ReadonlyToken);
        if ((node.ReadonlyToken.Kind != Kind.ReadonlyKeyword))
        {
            this.writeKeyword("readonly");
        }
        this.writeSpace();
    }
    this.writePunctuation("[");
    this.emitMappedTypeParameter(node.TypeParameter.AsTypeParameterDeclaration());
    if ((node.NameType != null))
    {
        this.writeSpace();
        this.writeKeyword("as");
        this.writeSpace();
        this.emitTypeNodeOutsideExtends(node.NameType);
    }
    this.writePunctuation("]");
    if ((node.QuestionToken != null))
    {
        this.emitPunctuationNode(node.QuestionToken);
        if ((node.QuestionToken.Kind != Kind.QuestionToken))
        {
            this.writePunctuation("?");
        }
    }
    if ((node.Type != null))
    {
        this.writePunctuation(":");
        this.writeSpace();
        this.emitTypeNodeOutsideExtends(node.Type);
    }
    this.writeTrailingSemicolon();
    if ((node.Members != null))
    {
        if ((GoRuntime.Len(node.Members.Nodes) > 0))
        {
            if (singleLine)
            {
                this.writeSpace();
            }
            else
            {
                this.writeLine();
            }
            this.emitList((binderCallback, argnode) => binderCallback.emitTypeElement(argnode), node.AsNode(), node.Members, ListFormat.PreserveLines);
        }
    }
    if (singleLine)
    {
        this.writeSpace();
    }
    else
    {
        this.writeLine();
        this.decreaseIndent();
    }
    this.writePunctuation("}");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2210:1.
private void emitLiteralType(LiteralTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.emitExpression(node.Literal, OperatorPrecedence.Comma);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2216:1.
private void emitTemplateTypeSpan(TemplateLiteralTypeSpan node)
{
    var state = this.enterNode(node.AsNode());
    this.emitTypeNodeOutsideExtends(node.Type);
    this.emitTemplateMiddleTail(node.Literal);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2223:1.
private void emitTemplateTypeSpanNode(Node node)
{
    this.emitTemplateTypeSpan(node.AsTemplateLiteralTypeSpan());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2227:1.
private void emitTemplateType(TemplateLiteralTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    this.emitTemplateHead(node.Head.AsTemplateHead());
    this.emitList((binderCallback, argnode) => binderCallback.emitTemplateTypeSpanNode(argnode), node.AsNode(), node.TemplateSpans, ListFormat.TemplateExpressionSpans);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2234:1.
private void emitImportTypeNodeAttributes(ImportAttributes node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("{");
    this.writeSpace();
    this.writeKeyword(Core.IfElse((node.Token == Kind.AssertKeyword), "assert", "with"));
    this.writePunctuation(":");
    this.writeSpace();
    this.emitList((binderCallback, argnode) => binderCallback.emitImportAttributeNode(argnode), node.AsNode(), node.Attributes, ListFormat.ImportAttributes);
    this.writeSpace();
    this.writePunctuation("}");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2247:1.
private void emitImportTypeNode(ImportTypeNode node)
{
    var state = this.enterNode(node.AsNode());
    if (node.IsTypeOf)
    {
        this.writeKeyword("typeof");
        this.writeSpace();
    }
    this.writeKeyword("import");
    this.writePunctuation("(");
    this.emitTypeNodeOutsideExtends(node.Argument);
    if ((node.Attributes != null))
    {
        this.writePunctuation(",");
        this.writeSpace();
        this.emitImportTypeNodeAttributes(node.Attributes.AsImportAttributes());
    }
    this.writePunctuation(")");
    if ((node.Qualifier != null))
    {
        this.writePunctuation(".");
        this.emitEntityName(node.Qualifier);
    }
    this.emitTypeArguments(node.AsNode(), node.TypeArguments);
    this.exitNode(node.AsNode(), state);
}

// emits a Type node in the `extends` clause of a ConditionalType
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2271:1.
private void emitTypeNodeInExtends(Node node)
{
    var savedInExtends = this.inExtends;
    this.inExtends = true;
    this.emitTypeNodePreservingExtends(node, TypePrecedence.Lowest);
    this.inExtends = savedInExtends;
}

// emits a Type node not in the `extends` clause of a ConditionalType or InferType
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2279:1.
private void emitTypeNodeOutsideExtends(Node node)
{
    var savedInExtends = this.inExtends;
    this.inExtends = false;
    this.emitTypeNodePreservingExtends(node, TypePrecedence.Lowest);
    this.inExtends = savedInExtends;
}

// emits a Type node preserving whether or not we are currently in the `extends` clause of a ConditionalType or InferType
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2287:1.
private void emitTypeNodePreservingExtends(Node node, TypePrecedence precedence)
{
    this.emitTypeNode(node, precedence);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2291:1.
private void emitTypeNode(Node node, TypePrecedence precedence)
{
    if ((this.inExtends && (precedence <= TypePrecedence.Conditional)))
    {
        precedence = TypePrecedence.Function;
    }
    var savedInExtends = this.inExtends;
    var parens = (Ast.GetTypeNodePrecedence(node) < precedence);
    if (parens)
    {
        this.inExtends = false;
        this.writePunctuation("(");
    }
    switch (node.Kind)
    {
        case Kind.AnyKeyword:
        case Kind.UnknownKeyword:
        case Kind.NumberKeyword:
        case Kind.BigIntKeyword:
        case Kind.ObjectKeyword:
        case Kind.BooleanKeyword:
        case Kind.StringKeyword:
        case Kind.SymbolKeyword:
        case Kind.VoidKeyword:
        case Kind.UndefinedKeyword:
        case Kind.NeverKeyword:
        case Kind.IntrinsicKeyword:
        {
            this.emitKeywordTypeNode(node.AsKeywordTypeNode());
            break;
        }
        case Kind.TypePredicate:
        {
            this.emitTypePredicate(node.AsTypePredicateNode());
            break;
        }
        case Kind.TypeReference:
        {
            this.emitTypeReference(node.AsTypeReferenceNode());
            break;
        }
        case Kind.FunctionType:
        {
            this.emitFunctionType(node.AsFunctionTypeNode());
            break;
        }
        case Kind.ConstructorType:
        {
            this.emitConstructorType(node.AsConstructorTypeNode());
            break;
        }
        case Kind.TypeQuery:
        {
            this.emitTypeQuery(node.AsTypeQueryNode());
            break;
        }
        case Kind.TypeLiteral:
        {
            this.emitTypeLiteral(node.AsTypeLiteralNode());
            break;
        }
        case Kind.ArrayType:
        {
            this.emitArrayType(node.AsArrayTypeNode());
            break;
        }
        case Kind.TupleType:
        {
            this.emitTupleType(node.AsTupleTypeNode());
            break;
        }
        case Kind.OptionalType:
        {
            this.emitOptionalType(node.AsOptionalTypeNode());
            break;
        }
        case Kind.RestType:
        {
            this.emitRestType(node.AsRestTypeNode());
            break;
        }
        case Kind.UnionType:
        {
            this.emitUnionType(node.AsUnionTypeNode());
            break;
        }
        case Kind.IntersectionType:
        {
            this.emitIntersectionType(node.AsIntersectionTypeNode());
            break;
        }
        case Kind.ConditionalType:
        {
            this.emitConditionalType(node.AsConditionalTypeNode());
            break;
        }
        case Kind.InferType:
        {
            this.emitInferType(node.AsInferTypeNode());
            break;
        }
        case Kind.ParenthesizedType:
        {
            this.emitParenthesizedType(node.AsParenthesizedTypeNode());
            break;
        }
        case Kind.ThisType:
        {
            this.emitThisType(node.AsThisTypeNode());
            break;
        }
        case Kind.TypeOperator:
        {
            this.emitTypeOperator(node.AsTypeOperatorNode());
            break;
        }
        case Kind.IndexedAccessType:
        {
            this.emitIndexedAccessType(node.AsIndexedAccessTypeNode());
            break;
        }
        case Kind.MappedType:
        {
            this.emitMappedType(node.AsMappedTypeNode());
            break;
        }
        case Kind.LiteralType:
        {
            this.emitLiteralType(node.AsLiteralTypeNode());
            break;
        }
        case Kind.NamedTupleMember:
        {
            this.emitNamedTupleMember(node.AsNamedTupleMember());
            break;
        }
        case Kind.TemplateLiteralType:
        {
            this.emitTemplateType(node.AsTemplateLiteralTypeNode());
            break;
        }
        case Kind.TemplateLiteralTypeSpan:
        {
            this.emitTemplateTypeSpan(node.AsTemplateLiteralTypeSpan());
            break;
        }
        case Kind.ImportType:
        {
            this.emitImportTypeNode(node.AsImportTypeNode());
            break;
        }
        case Kind.PropertyAccessExpression:
        {
            this.emitPropertyAccessExpression(node.AsPropertyAccessExpression());
            break;
        }
        case Kind.ExpressionWithTypeArguments:
        {
            this.emitExpressionWithTypeArguments(node.AsExpressionWithTypeArguments());
            break;
        }
        case Kind.JSDocAllType:
        {
            this.emitJSDocAllType(node);
            break;
        }
        case Kind.JSDocNonNullableType:
        {
            this.emitJSDocNonNullableType(node.AsJSDocNonNullableType());
            break;
        }
        case Kind.JSDocNullableType:
        {
            this.emitJSDocNullableType(node.AsJSDocNullableType());
            break;
        }
        case Kind.JSDocOptionalType:
        {
            this.emitJSDocOptionalType(node.AsJSDocOptionalType());
            break;
        }
        case Kind.JSDocVariadicType:
        {
            this.emitJSDocVariadicType(node.AsJSDocVariadicType());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled TypeNode: %v", node.Kind));
        }
    }
    if (parens)
    {
        this.writePunctuation(")");
    }
    this.inExtends = savedInExtends;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2403:1.
private void emitObjectBindingPattern(BindingPattern node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("{");
    this.emitList((binderCallback, argnode) => binderCallback.emitBindingElementNode(argnode), node.AsNode(), node.Elements, ListFormat.ObjectBindingPatternElements);
    this.writePunctuation("}");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2411:1.
private void emitArrayBindingPattern(BindingPattern node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("[");
    this.emitList((binderCallback, argnode) => binderCallback.emitBindingElementNode(argnode), node.AsNode(), node.Elements, ListFormat.ArrayBindingPatternElements);
    this.writePunctuation("]");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2419:1.
private void emitBindingElement(BindingElement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitTokenNode(node.DotDotDotToken);
    if ((node.PropertyName != null))
    {
        this.emitPropertyName(node.PropertyName);
        this.writePunctuation(":");
        this.writeSpace();
    }
    {
        var name = node.Name();
        if ((name != null))
        {
            this.emitBindingName(name);
            this.emitInitializer(node.Initializer, node.Name().End(), node.AsNode());
        }
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2435:1.
private void emitBindingElementNode(Node node)
{
    this.emitBindingElement(node.AsBindingElement());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2439:1.
private void emitJSDocAllType(Node node)
{
    this.emitKeywordNode(node);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2443:1.
private void emitJSDocNonNullableType(JSDocNonNullableType node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("!");
    this.emitTypeNode(node.Type, TypePrecedence.NonArray);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2450:1.
private void emitJSDocNullableType(JSDocNullableType node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("?");
    this.emitTypeNode(node.Type, TypePrecedence.NonArray);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2457:1.
private void emitJSDocOptionalType(JSDocOptionalType node)
{
    var state = this.enterNode(node.AsNode());
    this.emitTypeNode(node.Type, TypePrecedence.JSDoc);
    this.writePunctuation("=");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2464:1.
private void emitJSDocVariadicType(JSDocVariadicType node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("...");
    this.emitTypeNode(node.Type, TypePrecedence.JSDoc);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2475:1.
private void emitKeywordExpression(KeywordExpression node)
{
    this.emitKeywordNode(node.AsNode());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2479:1.
private void emitArrayLiteralExpressionElement(Node node)
{
    this.emitExpression(node, OperatorPrecedence.Spread);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2483:1.
private void emitArrayLiteralExpression(ArrayLiteralExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitList((binderCallback, argnode) => binderCallback.emitArrayLiteralExpressionElement(argnode), node.AsNode(), node.Elements, (ListFormat.ArrayLiteralExpressionElements | Core.IfElse(node.MultiLine, ListFormat.PreferNewLine, ListFormat.None)));
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2489:1.
private void emitObjectLiteralExpression(ObjectLiteralExpression node)
{
    var state = this.enterNode(node.AsNode());
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.generateAllMemberNames(node.Properties);
    this.emitList((binderCallback, argnode) => binderCallback.emitObjectLiteralElement(argnode), node.AsNode(), node.Properties, ((ListFormat.ObjectLiteralExpressionProperties | Core.IfElse(node.MultiLine, ListFormat.PreferNewLine, ListFormat.None)) | Core.IfElse(this.shouldAllowTrailingComma(node.AsNode(), node.Properties), ListFormat.AllowTrailingComma, ListFormat.None)));
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// 1..toString is a valid property access, emit a dot after the literal
// Also emit a dot if expression is a integer const enum value - it will appear in generated code as numeric literal
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2505:1.
private bool mayNeedDotDotForPropertyAccess(Node expression)
{
    expression = Ast.SkipPartiallyEmittedExpressions(expression);
    if (Ast.IsNumericLiteral(expression))
    {
        var text = this.getLiteralTextOfNodeValue(expression, null, getLiteralTextFlags.NeverAsciiEscape);
        return (((((expression.AsNumericLiteral().TokenFlags & TokenFlags.WithSpecifier) == 0) && !text.Contains('.')) && !text.Contains('E')) && !text.Contains('e'));
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2520:1.
private void emitPropertyAccessExpression(PropertyAccessExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitExpression(node.Expression, Core.IfElse(Ast.IsOptionalChain(node.AsNode()), OperatorPrecedence.OptionalChain, OperatorPrecedence.Member));
    var token = node.QuestionDotToken;
    if ((token == null))
    {
        token = this.emitContext.Factory.NewToken(Kind.DotToken);
        token.Loc = Core.NewTextRange(node.Expression.End(), node.Name().Pos());
        this.emitContext.AddEmitFlags(token, EmitFlags.NoSourceMap);
    }
    var linesBeforeDot = this.getLinesBetweenNodes(node.AsNode(), node.Expression, token);
    this.writeLineRepeat(linesBeforeDot);
    this.increaseIndentIf((linesBeforeDot > 0));
    var shouldEmitDotDot = ((((token.Kind != Kind.QuestionDotToken) && this.mayNeedDotDotForPropertyAccess(node.Expression)) && !this.writer.HasTrailingComment()) && !this.writer.HasTrailingWhitespace());
    if (shouldEmitDotDot)
    {
        this.writePunctuation(".");
    }
    if ((node.QuestionDotToken != null))
    {
        this.emitTokenNode(token);
    }
    else
    {
        this.emitToken(Kind.DotToken, node.Expression.End(), WriteKind.Punctuation, node.AsNode());
    }
    var linesAfterDot = this.getLinesBetweenNodes(node.AsNode(), token, node.Name());
    this.writeLineRepeat(linesAfterDot);
    this.increaseIndentIf((linesAfterDot > 0));
    this.emitMemberName(node.Name());
    this.decreaseIndentIf((linesAfterDot > 0));
    this.decreaseIndentIf((linesBeforeDot > 0));
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2553:1.
private void emitElementAccessExpression(ElementAccessExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitExpression(node.Expression, Core.IfElse(Ast.IsOptionalChain(node.AsNode()), OperatorPrecedence.OptionalChain, OperatorPrecedence.Member));
    this.emitTokenNode(node.QuestionDotToken);
    this.emitToken(Kind.OpenBracketToken, greatestEnd(-1, node.Expression, node.QuestionDotToken), WriteKind.Punctuation, node.AsNode());
    this.emitExpression(node.ArgumentExpression, OperatorPrecedence.Comma);
    this.emitToken(Kind.CloseBracketToken, node.ArgumentExpression.End(), WriteKind.Punctuation, node.AsNode());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2563:1.
private void emitArgument(Node node)
{
    this.emitExpression(node, OperatorPrecedence.Spread);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2567:1.
private void emitCallee(Node callee, Node parentNode)
{
    if (this.shouldEmitIndirectCall(parentNode))
    {
        this.writePunctuation("(");
        this.writeLiteral("0");
        this.writePunctuation(",");
        this.writeSpace();
        this.emitExpression(callee, OperatorPrecedence.Comma);
        this.writePunctuation(")");
    }
    else
    {
        if (((parentNode.Kind == Kind.CallExpression) && isNewExpressionWithoutArguments(Ast.SkipPartiallyEmittedExpressions(callee))))
        {
            this.emitExpression(callee, OperatorPrecedence.Parentheses);
        }
        else
        {
            this.emitExpression(callee, Core.IfElse(Ast.IsOptionalChain(parentNode), OperatorPrecedence.OptionalChain, OperatorPrecedence.Member));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2583:1.
private void emitCallExpression(CallExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitCallee(node.Expression, node.AsNode());
    this.emitTokenNode(node.QuestionDotToken);
    this.emitTypeArguments(node.AsNode(), node.TypeArguments);
    this.emitList((binderCallback, argnode) => binderCallback.emitArgument(argnode), node.AsNode(), node.Arguments, ListFormat.CallExpressionArguments);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2592:1.
private void emitNewExpression(NewExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.NewKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    if ((Ast.SkipPartiallyEmittedExpressions(node.Expression).Kind == Kind.CallExpression))
    {
        this.emitExpression(node.Expression, OperatorPrecedence.Parentheses);
    }
    else
    {
        this.emitExpression(node.Expression, OperatorPrecedence.Member);
    }
    this.emitTypeArguments(node.AsNode(), node.TypeArguments);
    this.emitList((binderCallback, argnode) => binderCallback.emitArgument(argnode), node.AsNode(), node.Arguments, ListFormat.NewExpressionArguments);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2607:1.
private void emitTemplateLiteral(Node node)
{
    switch (node.Kind)
    {
        case Kind.NoSubstitutionTemplateLiteral:
        {
            this.emitNoSubstitutionTemplateLiteral(node.AsNoSubstitutionTemplateLiteral());
            break;
        }
        case Kind.TemplateExpression:
        {
            this.emitTemplateExpression(node.AsTemplateExpression());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled TemplateLiteral: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2618:1.
private void emitTaggedTemplateExpression(TaggedTemplateExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitCallee(node.Tag, node.AsNode());
    this.emitTypeArguments(node.AsNode(), node.TypeArguments);
    this.writeSpace();
    this.emitTemplateLiteral(node.Template);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2627:1.
private void emitTypeAssertionExpression(TypeAssertion node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("<");
    this.emitTypeNodeOutsideExtends(node.Type);
    this.writePunctuation(">");
    this.emitExpression(node.Expression, OperatorPrecedence.Update);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2636:1.
private void emitParenthesizedExpression(ParenthesizedExpression node)
{
    var state = this.enterNode(node.AsNode());
    var openParenPos = this.emitToken(Kind.OpenParenToken, node.Pos(), WriteKind.Punctuation, node.AsNode());
    var indented = this.writeLineSeparatorsAndIndentBefore(node.Expression, node.AsNode());
    this.emitExpression(node.Expression, OperatorPrecedence.Comma);
    this.writeLineSeparatorsAfter(node.Expression, node.AsNode());
    this.decreaseIndentIf(indented);
    var closeParenPos = openParenPos;
    if ((node.Expression != null))
    {
        closeParenPos = node.Expression.End();
    }
    this.emitToken(Kind.CloseParenToken, closeParenPos, WriteKind.Punctuation, node.AsNode());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2651:1.
private void emitFunctionExpression(FunctionExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.generateNameIfNeeded(node.Name());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    this.writeKeyword("function");
    this.emitTokenNode(node.AsteriskToken);
    this.writeSpace();
    this.emitIdentifierNameNode(node.Name());
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.emitSignature(node.AsNode());
    this.emitFunctionBodyNode(node.Body);
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2669:1.
private void emitConciseBody(Node node)
{
    switch (true)
    {
        case true when Ast.IsBlock(node):
        {
            this.emitFunctionBody(node.AsBlock());
            break;
        }
        case true when Ast.IsObjectLiteralExpression(Ast.GetLeftmostExpression(node, false)):
        {
            var paren = this.emitContext.Factory.NewParenthesizedExpression(node);
            paren.Loc = node.Loc;
            this.emitExpression(paren, OperatorPrecedence.Lowest);
            break;
        }
        case true when Ast.IsExpression(node):
        {
            this.emitExpression(node, OperatorPrecedence.Yield);
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected ConciseBody: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2687:1.
private void emitArrowFunction(ArrowFunction node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.emitTypeParameters(node.AsNode(), node.TypeParameters);
    this.emitParametersForArrow(node.AsNode(), node.Parameters);
    this.emitTypeAnnotation(node.Type);
    this.writeSpace();
    this.emitTokenNode(node.EqualsGreaterThanToken);
    this.writeSpace();
    this.emitConciseBody(node.Body);
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2705:1.
private void emitDeleteExpression(DeleteExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.DeleteKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitExpression(node.Expression, OperatorPrecedence.Unary);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2713:1.
private void emitTypeOfExpression(TypeOfExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.TypeOfKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitExpression(node.Expression, OperatorPrecedence.Unary);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2721:1.
private void emitVoidExpression(VoidExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.VoidKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitExpression(node.Expression, OperatorPrecedence.Unary);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2729:1.
private void emitAwaitExpression(AwaitExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.AwaitKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitExpression(node.Expression, OperatorPrecedence.Unary);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2737:1.
private void emitPrefixUnaryExpression(PrefixUnaryExpression node)
{
    var state = this.enterNode(node.AsNode());
    var @operator = node.Operator;
    var operand = node.Operand;
    this.emitToken(@operator, node.Pos(), WriteKind.Operator, node.AsNode());
    if ((operand.Kind == Kind.PrefixUnaryExpression))
    {
        var inner = operand.AsPrefixUnaryExpression().Operator;
        if (((((@operator == Kind.PlusToken) && (((inner == Kind.PlusToken) || (inner == Kind.PlusPlusToken))))) || (((@operator == Kind.MinusToken) && (((inner == Kind.MinusToken) || (inner == Kind.MinusMinusToken)))))))
        {
            this.writeSpace();
        }
    }
    this.emitExpression(node.Operand, OperatorPrecedence.Unary);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2767:1.
private void emitPostfixUnaryExpression(PostfixUnaryExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitExpression(node.Operand, OperatorPrecedence.LeftHandSide);
    this.emitToken(node.Operator, node.Operand.End(), WriteKind.Operator, node.AsNode());
    this.exitNode(node.AsNode(), state);
}

// This function determines whether an expression consists of a homogeneous set of
// literal expressions or binary plus expressions that all share the same literal kind.
// It is used to determine whether the right-hand operand of a binary plus expression can be
// emitted without parentheses.
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2778:1.
private Kind getLiteralKindOfBinaryPlusOperand(Node node)
{
    node = Ast.SkipPartiallyEmittedExpressions(node);
    if (Ast.IsLiteralKind(node.Kind))
    {
        return node.Kind;
    }
    if ((node.Kind == Kind.BinaryExpression))
    {
        {
            var n = node.AsBinaryExpression();
            if ((n.OperatorToken.Kind == Kind.PlusToken))
            {
                var leftKind = this.getLiteralKindOfBinaryPlusOperand(n.Left);
                var literalKind = Kind.Unknown;
                if ((Ast.IsLiteralKind(leftKind) && (leftKind == this.getLiteralKindOfBinaryPlusOperand(n.Right))))
                {
                    literalKind = leftKind;
                }
                return literalKind;
            }
        }
    }
    return Kind.Unknown;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2806:1.
private (OperatorPrecedence, OperatorPrecedence) getBinaryExpressionPrecedence(BinaryExpression node)
{
    OperatorPrecedence leftPrec = default;
    OperatorPrecedence rightPrec = default;
    var precedence = Ast.GetExpressionPrecedence(node.AsNode());
    leftPrec = precedence;
    rightPrec = precedence;
    switch (precedence)
    {
        case OperatorPrecedence.Comma:
        {
            break;
        }
        case OperatorPrecedence.Assignment:
        {
            leftPrec = OperatorPrecedence.Conditional;
            rightPrec = OperatorPrecedence.Yield;
            break;
        }
        case OperatorPrecedence.LogicalOR:
        {
            rightPrec = OperatorPrecedence.LogicalAND;
            break;
        }
        case OperatorPrecedence.LogicalAND:
        {
            rightPrec = OperatorPrecedence.BitwiseOR;
            break;
        }
        case OperatorPrecedence.BitwiseOR:
        {
            break;
        }
        case OperatorPrecedence.BitwiseXOR:
        {
            break;
        }
        case OperatorPrecedence.BitwiseAND:
        {
            break;
        }
        case OperatorPrecedence.Equality:
        {
            rightPrec = OperatorPrecedence.Relational;
            break;
        }
        case OperatorPrecedence.Relational:
        {
            rightPrec = OperatorPrecedence.Shift;
            break;
        }
        case OperatorPrecedence.Shift:
        {
            rightPrec = OperatorPrecedence.Additive;
            break;
        }
        case OperatorPrecedence.Additive:
        {
            if (((node.OperatorToken.Kind == Kind.PlusToken) && isBinaryOperation(node.Right, Kind.PlusToken)))
            {
                var leftKind = this.getLiteralKindOfBinaryPlusOperand(node.Left);
                if ((Ast.IsLiteralKind(leftKind) && (leftKind == this.getLiteralKindOfBinaryPlusOperand(node.Right))))
                {
                    break;
                }
            }
            rightPrec = OperatorPrecedence.Multiplicative;
            break;
        }
        case OperatorPrecedence.Multiplicative:
        {
            if (((node.OperatorToken.Kind == Kind.AsteriskToken) && isBinaryOperation(node.Right, Kind.AsteriskToken)))
            {
                break;
            }
            rightPrec = OperatorPrecedence.Exponentiation;
            break;
        }
        case OperatorPrecedence.Exponentiation:
        {
            leftPrec = OperatorPrecedence.Update;
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled precedence: %v", precedence));
        }
    }
    return (leftPrec, rightPrec);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2872:1.
private void emitBinaryExpression(BinaryExpression node)
{
    var tuple8 = this.getBinaryExpressionPrecedence(node); var leftPrec = tuple8.Item1; var rightPrec = tuple8.Item2;
    {
        var emittedLeft = Ast.SkipPartiallyEmittedExpressions(node.Left);
        if (((Ast.NodeIsSynthesized(emittedLeft) && (emittedLeft.Kind == Kind.BinaryExpression)) && mixingBinaryOperatorsRequiresParentheses(node.OperatorToken.Kind, emittedLeft.AsBinaryExpression().OperatorToken.Kind)))
        {
            leftPrec = OperatorPrecedence.Highest;
        }
    }
    {
        var emittedRight = Ast.SkipPartiallyEmittedExpressions(node.Right);
        if (((Ast.NodeIsSynthesized(emittedRight) && (emittedRight.Kind == Kind.BinaryExpression)) && mixingBinaryOperatorsRequiresParentheses(node.OperatorToken.Kind, emittedRight.AsBinaryExpression().OperatorToken.Kind)))
        {
            rightPrec = OperatorPrecedence.Highest;
        }
    }
    var state = this.enterNode(node.AsNode());
    this.emitExpression(node.Left, leftPrec);
    var linesBeforeOperator = this.getLinesBetweenNodes(node.AsNode(), node.Left, node.OperatorToken);
    var linesAfterOperator = this.getLinesBetweenNodes(node.AsNode(), node.OperatorToken, node.Right);
    this.writeLinesAndIndent(linesBeforeOperator, (node.OperatorToken.Kind != Kind.CommaToken));
    this.emitTokenNodeEx(node.OperatorToken, tokenEmitFlags.NoSourceMaps);
    this.writeLinesAndIndent(linesAfterOperator, true);
    this.emitExpression(node.Right, rightPrec);
    this.decreaseIndentIf((linesAfterOperator > 0));
    this.decreaseIndentIf((linesBeforeOperator > 0));
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2893:1.
private void emitShortCircuitExpression(Node node)
{
    if (isBinaryOperation(Ast.SkipPartiallyEmittedExpressions(node), Kind.QuestionQuestionToken))
    {
        this.emitExpression(node, OperatorPrecedence.Coalesce);
    }
    else
    {
        this.emitExpression(node, OperatorPrecedence.LogicalOR);
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2901:1.
private void emitConditionalExpression(ConditionalExpression node)
{
    var state = this.enterNode(node.AsNode());
    var linesBeforeQuestion = this.getLinesBetweenNodes(node.AsNode(), node.Condition, node.QuestionToken);
    var linesAfterQuestion = this.getLinesBetweenNodes(node.AsNode(), node.QuestionToken, node.WhenTrue);
    var linesBeforeColon = this.getLinesBetweenNodes(node.AsNode(), node.WhenTrue, node.ColonToken);
    var linesAfterColon = this.getLinesBetweenNodes(node.AsNode(), node.ColonToken, node.WhenFalse);
    this.emitShortCircuitExpression(node.Condition);
    this.writeLinesAndIndent(linesBeforeQuestion, true);
    this.emitPunctuationNode(node.QuestionToken);
    this.writeLinesAndIndent(linesAfterQuestion, true);
    this.emitExpression(node.WhenTrue, OperatorPrecedence.Yield);
    this.decreaseIndentIf((linesAfterQuestion > 0));
    this.decreaseIndentIf((linesBeforeQuestion > 0));
    this.writeLinesAndIndent(linesBeforeColon, true);
    this.emitPunctuationNode(node.ColonToken);
    this.writeLinesAndIndent(linesAfterColon, true);
    this.emitExpression(node.WhenFalse, OperatorPrecedence.Yield);
    this.decreaseIndentIf((linesAfterColon > 0));
    this.decreaseIndentIf((linesBeforeColon > 0));
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2923:1.
private void emitTemplateExpression(TemplateExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitTemplateHead(node.Head.AsTemplateHead());
    this.emitList((binderCallback, argnode) => binderCallback.emitTemplateSpanNode(argnode), node.AsNode(), node.TemplateSpans, ListFormat.TemplateExpressionSpans);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2930:1.
private void emitYieldExpression(YieldExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.YieldKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.emitPunctuationNode(node.AsteriskToken);
    if ((node.Expression != null))
    {
        this.writeSpace();
        this.emitExpressionNoASI(node.Expression, OperatorPrecedence.DisallowComma);
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2941:1.
private void emitSpreadElement(SpreadElement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.DotDotDotToken, node.Pos(), WriteKind.Punctuation, node.AsNode());
    this.emitExpression(node.Expression, OperatorPrecedence.DisallowComma);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2948:1.
private void emitClassExpression(ClassExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.generateNameIfNeeded(node.Name());
    var pos = this.emitModifierList(node.AsNode(), node.Modifiers(), true);
    this.emitToken(Kind.ClassKeyword, pos, WriteKind.Keyword, node.AsNode());
    if ((node.Name() != null))
    {
        this.writeSpace();
        this.emitIdentifierName(node.Name().AsIdentifier());
    }
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.emitTypeParameters(node.AsNode(), node.TypeParameters);
    this.emitList((binderCallback, argnode) => binderCallback.emitHeritageClauseNode(argnode), node.AsNode(), node.HeritageClauses, ListFormat.ClassHeritageClauses);
    this.writeSpace();
    this.writePunctuation("{");
    this.pushNameGenerationScope(node.AsNode());
    this.generateAllMemberNames(node.Members);
    this.emitList((binderCallback, argnode) => binderCallback.emitClassElement(argnode), node.AsNode(), node.Members, ListFormat.ClassMembers);
    this.popNameGenerationScope(node.AsNode());
    this.writePunctuation("}");
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2977:1.
private void emitOmittedExpression(Node node)
{
    this.exitNode(node, this.enterNode(node));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2981:1.
private void emitExpressionWithTypeArguments(ExpressionWithTypeArguments node)
{
    var state = this.enterNode(node.AsNode());
    this.emitExpression(node.Expression, OperatorPrecedence.Member);
    this.emitTypeArguments(node.AsNode(), node.TypeArguments);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2988:1.
private void emitAsExpression(AsExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitExpression(node.Expression, OperatorPrecedence.Relational);
    this.writeSpace();
    this.writeKeyword("as");
    this.writeSpace();
    this.emitTypeNodeOutsideExtends(node.Type);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:2998:1.
private void emitSatisfiesExpression(SatisfiesExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitExpression(node.Expression, OperatorPrecedence.Relational);
    this.writeSpace();
    this.writeKeyword("satisfies");
    this.writeSpace();
    this.emitTypeNodeOutsideExtends(node.Type);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3008:1.
private void emitNonNullExpression(NonNullExpression node)
{
    var state = this.enterNode(node.AsNode());
    this.emitExpression(node.Expression, OperatorPrecedence.Member);
    this.writeOperator("!");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3015:1.
private void emitMetaProperty(MetaProperty node)
{
    var state = this.enterNode(node.AsNode());
    var pos = this.emitToken(node.KeywordToken, node.Pos(), WriteKind.Punctuation, node.AsNode());
    this.emitToken(Kind.DotToken, pos, WriteKind.Punctuation, node.AsNode());
    this.emitIdentifierName(node.Name().AsIdentifier());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3023:1.
private void emitPartiallyEmittedExpression(PartiallyEmittedExpression node)
{
    Stack<(PartiallyEmittedExpression node, printerState state)> stack = new();
    while (true)
    {
        var state2 = this.enterNode(node.AsNode());
        var emitFlags = this.emitContext.EmitFlags(node.AsNode());
        if ((((emitFlags & EmitFlags.NoLeadingComments) == 0) && (node.Pos() != node.Expression.Pos())))
        {
            this.emitTrailingCommentsOfPosition(node.Expression.Pos(), false, false);
        }
        stack.Push((node, state2));
        if (!Ast.IsPartiallyEmittedExpression(node.Expression))
        {
            break;
        }
        node = node.Expression.AsPartiallyEmittedExpression();
    }
    this.emitExpression(node.Expression, OperatorPrecedence.Lowest);
    while ((stack.Count > 0))
    {
        var entry = stack.Pop();
        var emitFlags2 = this.emitContext.EmitFlags(node.AsNode());
        if ((((emitFlags2 & EmitFlags.NoTrailingComments) == 0) && (node.End() != node.Expression.End())))
        {
            this.emitLeadingCommentsOfPosition(node.Expression.End());
        }
        this.exitNode(node.AsNode(), entry.state);
        node = entry.node;
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3057:1.
private bool commentWillEmitNewLine(CommentRange comment)
{
    return ((comment.Kind == Kind.SingleLineCommentTrivia) || comment.HasTrailingNewLine);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3061:1.
private bool syntheticCommentWillEmitNewLine(SynthesizedComment comment)
{
    return ((comment.Kind == Kind.SingleLineCommentTrivia) || comment.HasTrailingNewLine);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3065:1.
private bool willEmitLeadingNewLine(Node node)
{
    if ((this.currentSourceFile == null))
    {
        return false;
    }
    var hasLeadingCommentRanges = false;
    var hasNewLineComment = false;
    {
        foreach (var comment in GoRuntime.RangeKeys(ByteScanner.GetLeadingCommentRanges(this.emitContext.Factory, this.currentSourceFile.TextView(), node.Pos())))
        {
            hasLeadingCommentRanges = true;
            if (this.commentWillEmitNewLine(comment))
            {
                hasNewLineComment = true;
            }
        }
    }
    if (hasLeadingCommentRanges)
    {
        var parseNode = this.emitContext.ParseNode(node);
        if (((parseNode != null) && Ast.IsParenthesizedExpression(parseNode.Parent)))
        {
            return true;
        }
    }
    if (hasNewLineComment)
    {
        return true;
    }
    if (containsFunc(this.emitContext.GetSyntheticLeadingComments(node), this.syntheticCommentWillEmitNewLine))
    {
        return true;
    }
    if (Ast.IsPartiallyEmittedExpression(node))
    {
        var pee = node.AsPartiallyEmittedExpression();
        if ((node.Pos() != pee.Expression.Pos()))
        {
            {
                foreach (var comment2 in GoRuntime.RangeKeys(ByteScanner.GetTrailingCommentRanges(this.emitContext.Factory, this.currentSourceFile.TextView(), pee.Expression.Pos())))
                {
                    if (this.commentWillEmitNewLine(comment2))
                    {
                        return true;
                    }
                }
            }
        }
        return this.willEmitLeadingNewLine(pee.Expression);
    }
    return false;
}

// parenthesizeExpressionForNoAsi wraps an expression in parens if we would emit a leading comment
// that would introduce a line separator between the node and its parent.
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3105:1.
private Node parenthesizeExpressionForNoAsi(Node node)
{
    if (!this.commentsDisabled)
    {
        switch (node.Kind)
        {
            case Kind.PartiallyEmittedExpression:
            {
                if (this.willEmitLeadingNewLine(node))
                {
                    var pee = node.AsPartiallyEmittedExpression();
                    var parseNode = this.emitContext.ParseNode(node);
                    if (((parseNode != null) && Ast.IsParenthesizedExpression(parseNode)))
                    {
                        var parens = this.emitContext.Factory.NewParenthesizedExpression(pee.Expression);
                        this.emitContext.SetOriginal(parens, node);
                        parens.Loc = parseNode.Loc;
                        return parens;
                    }
                    return this.emitContext.Factory.NewParenthesizedExpression(node);
                }
                var pee2 = node.AsPartiallyEmittedExpression();
                return this.emitContext.Factory.UpdatePartiallyEmittedExpression(pee2, this.parenthesizeExpressionForNoAsi(pee2.Expression));
            }
            case Kind.PropertyAccessExpression:
            {
                var pae = node.AsPropertyAccessExpression();
                return this.emitContext.Factory.UpdatePropertyAccessExpression(pae, this.parenthesizeExpressionForNoAsi(pae.Expression), pae.QuestionDotToken, pae.Name(), pae.Flags);
            }
            case Kind.ElementAccessExpression:
            {
                var eae = node.AsElementAccessExpression();
                return this.emitContext.Factory.UpdateElementAccessExpression(eae, this.parenthesizeExpressionForNoAsi(eae.Expression), eae.QuestionDotToken, eae.ArgumentExpression, eae.Flags);
            }
            case Kind.CallExpression:
            {
                var ce = node.AsCallExpression();
                return this.emitContext.Factory.UpdateCallExpression(ce, this.parenthesizeExpressionForNoAsi(ce.Expression), ce.QuestionDotToken, ce.TypeArguments, ce.Arguments, ce.Flags);
            }
            case Kind.TaggedTemplateExpression:
            {
                var tte = node.AsTaggedTemplateExpression();
                return this.emitContext.Factory.UpdateTaggedTemplateExpression(tte, this.parenthesizeExpressionForNoAsi(tte.Tag), tte.QuestionDotToken, tte.TypeArguments, tte.Template, tte.Flags);
            }
            case Kind.PostfixUnaryExpression:
            {
                var pue = node.AsPostfixUnaryExpression();
                return this.emitContext.Factory.UpdatePostfixUnaryExpression(pue, this.parenthesizeExpressionForNoAsi(pue.Operand), pue.Operator);
            }
            case Kind.BinaryExpression:
            {
                var be = node.AsBinaryExpression();
                return this.emitContext.Factory.UpdateBinaryExpression(be, be.Modifiers(), this.parenthesizeExpressionForNoAsi(be.Left), be.Type, be.OperatorToken, be.Right);
            }
            case Kind.ConditionalExpression:
            {
                var ce2 = node.AsConditionalExpression();
                return this.emitContext.Factory.UpdateConditionalExpression(ce2, this.parenthesizeExpressionForNoAsi(ce2.Condition), ce2.QuestionToken, ce2.WhenTrue, ce2.ColonToken, ce2.WhenFalse);
            }
            case Kind.AsExpression:
            {
                var ae = node.AsAsExpression();
                return this.emitContext.Factory.UpdateAsExpression(ae, this.parenthesizeExpressionForNoAsi(ae.Expression), ae.Type);
            }
            case Kind.SatisfiesExpression:
            {
                var se = node.AsSatisfiesExpression();
                return this.emitContext.Factory.UpdateSatisfiesExpression(se, this.parenthesizeExpressionForNoAsi(se.Expression), se.Type);
            }
            case Kind.NonNullExpression:
            {
                var nne = node.AsNonNullExpression();
                return this.emitContext.Factory.UpdateNonNullExpression(nne, this.parenthesizeExpressionForNoAsi(nne.Expression), nne.Flags);
            }
        }
    }
    return node;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3217:1.
private void emitExpressionNoASI(Node node, OperatorPrecedence precedence)
{
    node = this.parenthesizeExpressionForNoAsi(node);
    this.emitExpression(node, precedence);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3222:1.
private void emitExpression(Node node, OperatorPrecedence precedence)
{
    var parens = (Ast.GetExpressionPrecedence(Ast.SkipPartiallyEmittedExpressions(node)) < precedence);
    if (parens)
    {
        this.writePunctuation("(");
    }
    switch (node.Kind)
    {
        case Kind.TrueKeyword:
        case Kind.FalseKeyword:
        case Kind.NullKeyword:
        {
            this.emitTokenNode(node);
            break;
        }
        case Kind.ThisKeyword:
        case Kind.SuperKeyword:
        case Kind.ImportKeyword:
        {
            this.emitKeywordExpression(node.AsKeywordExpression());
            break;
        }
        case Kind.NumericLiteral:
        {
            this.emitNumericLiteral(node.AsNumericLiteral());
            break;
        }
        case Kind.BigIntLiteral:
        {
            this.emitBigIntLiteral(node.AsBigIntLiteral());
            break;
        }
        case Kind.StringLiteral:
        {
            this.emitStringLiteral(node.AsStringLiteral());
            break;
        }
        case Kind.RegularExpressionLiteral:
        {
            this.emitRegularExpressionLiteral(node.AsRegularExpressionLiteral());
            break;
        }
        case Kind.NoSubstitutionTemplateLiteral:
        {
            this.emitNoSubstitutionTemplateLiteral(node.AsNoSubstitutionTemplateLiteral());
            break;
        }
        case Kind.Identifier:
        {
            this.emitIdentifierReference(node.AsIdentifier());
            break;
        }
        case Kind.PrivateIdentifier:
        {
            this.emitPrivateIdentifier(node.AsPrivateIdentifier());
            break;
        }
        case Kind.ArrayLiteralExpression:
        {
            this.emitArrayLiteralExpression(node.AsArrayLiteralExpression());
            break;
        }
        case Kind.ObjectLiteralExpression:
        {
            this.emitObjectLiteralExpression(node.AsObjectLiteralExpression());
            break;
        }
        case Kind.PropertyAccessExpression:
        {
            this.emitPropertyAccessExpression(node.AsPropertyAccessExpression());
            break;
        }
        case Kind.ElementAccessExpression:
        {
            this.emitElementAccessExpression(node.AsElementAccessExpression());
            break;
        }
        case Kind.CallExpression:
        {
            this.emitCallExpression(node.AsCallExpression());
            break;
        }
        case Kind.NewExpression:
        {
            this.emitNewExpression(node.AsNewExpression());
            break;
        }
        case Kind.TaggedTemplateExpression:
        {
            this.emitTaggedTemplateExpression(node.AsTaggedTemplateExpression());
            break;
        }
        case Kind.TypeAssertionExpression:
        {
            this.emitTypeAssertionExpression(node.AsTypeAssertion());
            break;
        }
        case Kind.ParenthesizedExpression:
        {
            this.emitParenthesizedExpression(node.AsParenthesizedExpression());
            break;
        }
        case Kind.FunctionExpression:
        {
            this.emitFunctionExpression(node.AsFunctionExpression());
            break;
        }
        case Kind.ArrowFunction:
        {
            this.emitArrowFunction(node.AsArrowFunction());
            break;
        }
        case Kind.DeleteExpression:
        {
            this.emitDeleteExpression(node.AsDeleteExpression());
            break;
        }
        case Kind.TypeOfExpression:
        {
            this.emitTypeOfExpression(node.AsTypeOfExpression());
            break;
        }
        case Kind.VoidExpression:
        {
            this.emitVoidExpression(node.AsVoidExpression());
            break;
        }
        case Kind.AwaitExpression:
        {
            this.emitAwaitExpression(node.AsAwaitExpression());
            break;
        }
        case Kind.PrefixUnaryExpression:
        {
            this.emitPrefixUnaryExpression(node.AsPrefixUnaryExpression());
            break;
        }
        case Kind.PostfixUnaryExpression:
        {
            this.emitPostfixUnaryExpression(node.AsPostfixUnaryExpression());
            break;
        }
        case Kind.BinaryExpression:
        {
            this.emitBinaryExpression(node.AsBinaryExpression());
            break;
        }
        case Kind.ConditionalExpression:
        {
            this.emitConditionalExpression(node.AsConditionalExpression());
            break;
        }
        case Kind.TemplateExpression:
        {
            this.emitTemplateExpression(node.AsTemplateExpression());
            break;
        }
        case Kind.YieldExpression:
        {
            this.emitYieldExpression(node.AsYieldExpression());
            break;
        }
        case Kind.SpreadElement:
        {
            this.emitSpreadElement(node.AsSpreadElement());
            break;
        }
        case Kind.ClassExpression:
        {
            this.emitClassExpression(node.AsClassExpression());
            break;
        }
        case Kind.OmittedExpression:
        {
            this.emitOmittedExpression(node);
            break;
        }
        case Kind.AsExpression:
        {
            this.emitAsExpression(node.AsAsExpression());
            break;
        }
        case Kind.NonNullExpression:
        {
            this.emitNonNullExpression(node.AsNonNullExpression());
            break;
        }
        case Kind.ExpressionWithTypeArguments:
        {
            this.emitExpressionWithTypeArguments(node.AsExpressionWithTypeArguments());
            break;
        }
        case Kind.SatisfiesExpression:
        {
            this.emitSatisfiesExpression(node.AsSatisfiesExpression());
            break;
        }
        case Kind.MetaProperty:
        {
            this.emitMetaProperty(node.AsMetaProperty());
            break;
        }
        case Kind.SyntheticExpression:
        {
            throw new InvalidOperationException("SyntheticExpression should never be printed.");
        }
        case Kind.MissingDeclaration:
        {
            break;
        }
        case Kind.JsxElement:
        {
            this.emitJsxElement(node.AsJsxElement());
            break;
        }
        case Kind.JsxSelfClosingElement:
        {
            this.emitJsxSelfClosingElement(node.AsJsxSelfClosingElement());
            break;
        }
        case Kind.JsxFragment:
        {
            this.emitJsxFragment(node.AsJsxFragment());
            break;
        }
        case Kind.SyntaxList:
        {
            throw new InvalidOperationException("SyntaxList should not be printed");
        }
        case Kind.NotEmittedStatement:
        {
            return ;
        }
        case Kind.PartiallyEmittedExpression:
        {
            this.emitPartiallyEmittedExpression(node.AsPartiallyEmittedExpression());
            break;
        }
        case Kind.SyntheticReferenceExpression:
        {
            throw new InvalidOperationException("SyntheticReferenceExpression should not be printed");
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unexpected Expression: %v", node.Kind));
        }
    }
    if (parens)
    {
        this.writePunctuation(")");
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3350:1.
private void emitTemplateSpan(TemplateSpan node)
{
    var state = this.enterNode(node.AsNode());
    this.emitExpression(node.Expression, OperatorPrecedence.Comma);
    this.emitTemplateMiddleTail(node.Literal);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3357:1.
private void emitTemplateSpanNode(Node node)
{
    this.emitTemplateSpan(node.AsTemplateSpan());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3361:1.
private void emitSemicolonClassElement(SemicolonClassElement node)
{
    var state = this.enterNode(node.AsNode());
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3371:1.
private bool isEmptyBlock(Node block, NodeList statements)
{
    return ((GoRuntime.Len(statements.Nodes) == 0) && (((this.currentSourceFile == null) || rangeEndIsOnSameLineAsRangeStart(block.Loc, block.Loc, this.currentSourceFile))));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3376:1.
private void emitBlock(Block node)
{
    var state = this.enterNode(node.AsNode());
    this.generateNames(node.AsNode());
    this.emitToken(Kind.OpenBraceToken, node.Pos(), WriteKind.Punctuation, node.AsNode());
    var format = Core.IfElse(((!node.MultiLine && this.isEmptyBlock(node.AsNode(), node.Statements)) || this.shouldEmitOnSingleLine(node.AsNode())), ListFormat.SingleLineBlockStatements, ListFormat.MultiLineBlockStatements);
    this.emitList((binderCallback, argnode) => binderCallback.emitStatement(argnode), node.AsNode(), node.Statements, format);
    this.emitTokenEx(Kind.CloseBraceToken, node.Statements.End(), WriteKind.Punctuation, node.AsNode(), Core.IfElse(((format & ListFormat.MultiLine) != 0), tokenEmitFlags.IndentLeadingComments, tokenEmitFlags.None));
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3390:1.
private void emitVariableStatement(VariableStatement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    this.emitVariableDeclarationList(node.DeclarationList.AsVariableDeclarationList());
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3398:1.
private void emitEmptyStatement(EmptyStatement node, bool isEmbeddedStatement)
{
    var state = this.enterNode(node.AsNode());
    if (isEmbeddedStatement)
    {
        this.writePunctuation(";");
    }
    else
    {
        this.writeTrailingSemicolon();
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3411:1.
private void emitExpressionStatement(ExpressionStatement node)
{
    var state = this.enterNode(node.AsNode());
    if (((this.currentSourceFile != null) && (this.currentSourceFile.ScriptKind == ScriptKind.JSON)))
    {
        this.emitExpression(node.Expression, OperatorPrecedence.Comma);
    }
    else
    {
        if (isImmediatelyInvokedFunctionExpressionOrArrowFunction(node.Expression))
        {
            this.emitIIFEWithParenthesizedCallee(node.Expression);
        }
        else
        {
            switch (Ast.GetLeftmostExpression(node.Expression, false).Kind)
            {
                case Kind.FunctionExpression:
                case Kind.ObjectLiteralExpression:
                {
                    this.emitExpression(node.Expression, OperatorPrecedence.Parentheses);
                    break;
                }
                default:
                {
                    this.emitExpression(node.Expression, OperatorPrecedence.Comma);
                    break;
                }
            }
        }
    }
    if ((((this.currentSourceFile == null) || (this.currentSourceFile.ScriptKind != ScriptKind.JSON)) || Ast.NodeIsSynthesized(node.Expression)))
    {
        this.writeTrailingSemicolon();
    }
    this.exitNode(node.AsNode(), state);
}

// emitIIFEWithParenthesizedCallee emits a call expression that is an IIFE,
// wrapping just the callee in parens rather than the entire call expression.
// This matches TypeScript's parenthesizeExpressionOfExpressionStatement behavior:
//
//	(function() { })()   -- parens around callee only
//
// instead of:
//
//	(function() { }())   -- parens around entire call
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3451:1.
private void emitIIFEWithParenthesizedCallee(Node node)
{
    var call = Ast.SkipPartiallyEmittedExpressions(node).AsCallExpression();
    var state = this.enterNode(call.AsNode());
    this.writePunctuation("(");
    this.emitExpression(call.Expression, OperatorPrecedence.Lowest);
    this.writePunctuation(")");
    this.emitTokenNode(call.QuestionDotToken);
    this.emitTypeArguments(call.AsNode(), call.TypeArguments);
    this.emitList((binderCallback, argnode) => binderCallback.emitArgument(argnode), call.AsNode(), call.Arguments, ListFormat.CallExpressionArguments);
    this.exitNode(call.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3465:1.
private void emitIfStatement(IfStatement node)
{
    var state = this.enterNode(node.AsNode());
    var pos = this.emitToken(Kind.IfKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitToken(Kind.OpenParenToken, pos, WriteKind.Punctuation, node.AsNode());
    this.emitExpression(node.Expression, OperatorPrecedence.Lowest);
    this.emitToken(Kind.CloseParenToken, node.Expression.End(), WriteKind.Punctuation, node.AsNode());
    this.emitEmbeddedStatement(node.AsNode(), node.ThenStatement);
    if ((node.ElseStatement != null))
    {
        this.writeLineOrSpace(node.AsNode(), node.ThenStatement, node.ElseStatement);
        this.emitToken(Kind.ElseKeyword, node.ThenStatement.End(), WriteKind.Keyword, node.AsNode());
        if ((node.ElseStatement.Kind == Kind.IfStatement))
        {
            this.writeSpace();
            this.emitIfStatement(node.ElseStatement.AsIfStatement());
        }
        else
        {
            this.emitEmbeddedStatement(node.AsNode(), node.ElseStatement);
        }
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3486:1.
private void emitWhileClause(Node node, Node expression, int startPos)
{
    var pos = this.emitToken(Kind.WhileKeyword, startPos, WriteKind.Keyword, node);
    this.writeSpace();
    this.emitToken(Kind.OpenParenToken, pos, WriteKind.Punctuation, node);
    this.emitExpression(expression, OperatorPrecedence.Lowest);
    this.emitToken(Kind.CloseParenToken, expression.End(), WriteKind.Punctuation, node);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3494:1.
private void emitDoStatement(DoStatement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.DoKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.emitEmbeddedStatement(node.AsNode(), node.Statement);
    if ((Ast.IsBlock(node.Statement) && !this.Options.PreserveSourceNewlines))
    {
        this.writeSpace();
    }
    else
    {
        this.writeLineOrSpace(node.AsNode(), node.Statement, node.Expression);
    }
    this.emitWhileClause(node.AsNode(), node.Expression, node.Statement.End());
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3509:1.
private void emitWhileStatement(WhileStatement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitWhileClause(node.AsNode(), node.Expression, node.Pos());
    this.emitEmbeddedStatement(node.AsNode(), node.Statement);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3516:1.
private void emitForInitializer(Node node)
{
    if ((node.Kind == Kind.VariableDeclarationList))
    {
        this.emitVariableDeclarationList(node.AsVariableDeclarationList());
    }
    else
    {
        this.emitExpression(node, OperatorPrecedence.Lowest);
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3524:1.
private void emitForStatement(ForStatement node)
{
    var state = this.enterNode(node.AsNode());
    var pos = this.emitToken(Kind.ForKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    pos = this.emitToken(Kind.OpenParenToken, pos, WriteKind.Punctuation, node.AsNode());
    if ((node.Initializer != null))
    {
        this.emitForInitializer(node.Initializer);
        pos = node.Initializer.End();
    }
    pos = this.emitToken(Kind.SemicolonToken, pos, WriteKind.Punctuation, node.AsNode());
    if ((node.Condition != null))
    {
        this.writeSpace();
        this.emitExpression(node.Condition, OperatorPrecedence.Lowest);
        pos = node.Condition.End();
    }
    pos = this.emitToken(Kind.SemicolonToken, pos, WriteKind.Punctuation, node.AsNode());
    if ((node.Incrementor != null))
    {
        this.writeSpace();
        this.emitExpression(node.Incrementor, OperatorPrecedence.Lowest);
        pos = node.Incrementor.End();
    }
    this.emitToken(Kind.CloseParenToken, pos, WriteKind.Punctuation, node.AsNode());
    this.emitEmbeddedStatement(node.AsNode(), node.Statement);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3550:1.
private void emitForInStatement(ForInOrOfStatement node)
{
    var state = this.enterNode(node.AsNode());
    var pos = this.emitToken(Kind.ForKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitToken(Kind.OpenParenToken, pos, WriteKind.Punctuation, node.AsNode());
    this.emitForInitializer(node.Initializer);
    this.writeSpace();
    this.emitToken(Kind.InKeyword, node.Initializer.End(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitExpression(node.Expression, OperatorPrecedence.Lowest);
    this.emitToken(Kind.CloseParenToken, node.Expression.End(), WriteKind.Punctuation, node.AsNode());
    this.emitEmbeddedStatement(node.AsNode(), node.Statement);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3565:1.
private void emitForOfStatement(ForInOrOfStatement node)
{
    var state = this.enterNode(node.AsNode());
    var openParenPos = this.emitToken(Kind.ForKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    if ((node.AwaitModifier != null))
    {
        this.emitKeywordNode(node.AwaitModifier);
        this.writeSpace();
    }
    this.emitToken(Kind.OpenParenToken, openParenPos, WriteKind.Punctuation, node.AsNode());
    this.emitForInitializer(node.Initializer);
    this.writeSpace();
    this.emitToken(Kind.OfKeyword, node.Initializer.End(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitExpression(node.Expression, OperatorPrecedence.Lowest);
    this.emitToken(Kind.CloseParenToken, node.Expression.End(), WriteKind.Punctuation, node.AsNode());
    this.emitEmbeddedStatement(node.AsNode(), node.Statement);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3584:1.
private void emitContinueStatement(ContinueStatement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.ContinueKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    if ((node.Label != null))
    {
        this.writeSpace();
        this.emitLabelIdentifier(node.Label.AsIdentifier());
    }
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3595:1.
private void emitBreakStatement(BreakStatement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.BreakKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    if ((node.Label != null))
    {
        this.writeSpace();
        this.emitLabelIdentifier(node.Label.AsIdentifier());
    }
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3606:1.
private void emitReturnStatement(ReturnStatement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.ReturnKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    if ((node.Expression != null))
    {
        this.writeSpace();
        this.emitExpressionNoASI(node.Expression, OperatorPrecedence.Lowest);
    }
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3617:1.
private void emitWithStatement(WithStatement node)
{
    var state = this.enterNode(node.AsNode());
    var pos = this.emitToken(Kind.WithKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitToken(Kind.OpenParenToken, pos, WriteKind.Punctuation, node.AsNode());
    this.emitExpression(node.Expression, OperatorPrecedence.Lowest);
    this.emitToken(Kind.CloseParenToken, node.Expression.End(), WriteKind.Punctuation, node.AsNode());
    this.emitEmbeddedStatement(node.AsNode(), node.Statement);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3628:1.
private void emitSwitchStatement(SwitchStatement node)
{
    var state = this.enterNode(node.AsNode());
    var pos = this.emitToken(Kind.SwitchKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitToken(Kind.OpenParenToken, pos, WriteKind.Punctuation, node.AsNode());
    this.emitExpression(node.Expression, OperatorPrecedence.Lowest);
    this.emitToken(Kind.CloseParenToken, node.Expression.End(), WriteKind.Punctuation, node.AsNode());
    this.writeSpace();
    this.emitCaseBlock(node.CaseBlock.AsCaseBlock());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3640:1.
private void emitLabeledStatement(LabeledStatement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitLabelIdentifier(node.Label.AsIdentifier());
    this.emitToken(Kind.ColonToken, node.Label.End(), WriteKind.Punctuation, node.AsNode());
    this.writeSpace();
    this.emitStatement(node.Statement);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3656:1.
private void emitThrowStatement(ThrowStatement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.ThrowKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitExpressionNoASI(node.Expression, OperatorPrecedence.Lowest);
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3665:1.
private void emitTryStatement(TryStatement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.TryKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitBlock(node.TryBlock.AsBlock());
    if ((node.CatchClause != null))
    {
        this.writeLineOrSpace(node.AsNode(), node.TryBlock, node.CatchClause);
        this.emitCatchClause(node.CatchClause.AsCatchClause());
    }
    if ((node.FinallyBlock != null))
    {
        this.writeLineOrSpace(node.AsNode(), (node.CatchClause ?? node.TryBlock), node.FinallyBlock);
        this.emitToken(Kind.FinallyKeyword, (node.CatchClause ?? node.TryBlock).End(), WriteKind.Keyword, node.AsNode());
        this.writeSpace();
        this.emitBlock(node.FinallyBlock.AsBlock());
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3683:1.
private void emitDebuggerStatement(DebuggerStatement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.DebuggerKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3690:1.
private void emitNotEmittedStatement(NotEmittedStatement node)
{
    this.exitNode(node.AsNode(), this.enterNode(node.AsNode()));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3694:1.
private void emitNotEmittedTypeElement(NotEmittedTypeElement node)
{
    this.exitNode(node.AsNode(), this.enterNode(node.AsNode()));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3702:1.
private void emitVariableDeclaration(VariableDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitBindingName(node.Name());
    this.emitPunctuationNode(node.ExclamationToken);
    this.emitTypeAnnotation(node.Type);
    this.emitInitializer(node.Initializer, greatestEnd(node.Name().End(), node.Type, this.emitContext.GetTypeNode(node.Name())), node.AsNode());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3711:1.
private void emitVariableDeclarationNode(Node node)
{
    this.emitVariableDeclaration(node.AsVariableDeclaration());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3715:1.
private void emitVariableDeclarationList(VariableDeclarationList node)
{
    var state = this.enterNode(node.AsNode());
    switch (true)
    {
        case true when Ast.IsVarLet(node.AsNode()):
        {
            this.writeKeyword("let");
            break;
        }
        case true when Ast.IsVarConst(node.AsNode()):
        {
            this.writeKeyword("const");
            break;
        }
        case true when Ast.IsVarUsing(node.AsNode()):
        {
            this.writeKeyword("using");
            break;
        }
        case true when Ast.IsVarAwaitUsing(node.AsNode()):
        {
            this.writeKeyword("await");
            this.writeSpace();
            this.writeKeyword("using");
            break;
        }
        default:
        {
            this.writeKeyword("var");
            break;
        }
    }
    this.writeSpace();
    this.emitList((binderCallback, argnode) => binderCallback.emitVariableDeclarationNode(argnode), node.AsNode(), node.Declarations, ListFormat.VariableDeclarationList);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3736:1.
private void emitFunctionDeclaration(FunctionDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.generateNameIfNeeded(node.Name());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    this.writeKeyword("function");
    this.emitTokenNode(node.AsteriskToken);
    this.writeSpace();
    {
        var name = node.Name();
        if ((name != null))
        {
            this.emitIdentifierName(name.AsIdentifier());
        }
    }
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.pushNameGenerationScope(node.AsNode());
    this.emitSignature(node.AsNode());
    this.emitFunctionBodyNode(node.Body);
    this.popNameGenerationScope(node.AsNode());
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3756:1.
private void emitClassDeclaration(ClassDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.generateNameIfNeeded(node.Name());
    var pos = this.emitModifierList(node.AsNode(), node.Modifiers(), true);
    this.emitToken(Kind.ClassKeyword, pos, WriteKind.Keyword, node.AsNode());
    if ((node.Name() != null))
    {
        this.writeSpace();
        this.emitIdentifierName(node.Name().AsIdentifier());
    }
    var indented = this.shouldEmitIndented(node.AsNode());
    this.increaseIndentIf(indented);
    this.emitTypeParameters(node.AsNode(), node.TypeParameters);
    this.emitList((binderCallback, argnode) => binderCallback.emitHeritageClauseNode(argnode), node.AsNode(), node.HeritageClauses, ListFormat.ClassHeritageClauses);
    this.writeSpace();
    this.writePunctuation("{");
    this.pushNameGenerationScope(node.AsNode());
    this.generateAllMemberNames(node.Members);
    this.emitList((binderCallback, argnode) => binderCallback.emitClassElement(argnode), node.AsNode(), node.Members, ListFormat.ClassMembers);
    this.popNameGenerationScope(node.AsNode());
    this.writePunctuation("}");
    this.decreaseIndentIf(indented);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3780:1.
private void emitInterfaceDeclaration(InterfaceDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    this.writeKeyword("interface");
    this.writeSpace();
    this.emitBindingIdentifier(node.Name().AsIdentifier());
    this.emitTypeParameters(node.AsNode(), node.TypeParameters);
    this.emitList((binderCallback, argnode) => binderCallback.emitHeritageClauseNode(argnode), node.AsNode(), node.HeritageClauses, ListFormat.HeritageClauses);
    this.writeSpace();
    this.writePunctuation("{");
    this.pushNameGenerationScope(node.AsNode());
    this.generateAllMemberNames(node.Members);
    this.emitList((binderCallback, argnode) => binderCallback.emitTypeElement(argnode), node.AsNode(), node.Members, ListFormat.InterfaceMembers);
    this.popNameGenerationScope(node.AsNode());
    this.writePunctuation("}");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3798:1.
private void emitTypeAliasDeclaration(TypeAliasDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    this.writeKeyword("type");
    this.writeSpace();
    this.emitBindingIdentifier(node.Name().AsIdentifier());
    this.emitTypeParameters(node.AsNode(), node.TypeParameters);
    this.writeSpace();
    this.writePunctuation("=");
    this.writeSpace();
    this.emitTypeNodeOutsideExtends(node.Type);
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3813:1.
private void emitEnumDeclaration(EnumDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    this.writeKeyword("enum");
    this.writeSpace();
    this.emitBindingIdentifier(node.Name().AsIdentifier());
    this.writeSpace();
    this.writePunctuation("{");
    this.emitList((binderCallback, argnode) => binderCallback.emitEnumMemberNode(argnode), node.AsNode(), node.Members, ListFormat.EnumMembers);
    this.writePunctuation("}");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3826:1.
private void emitModuleDeclaration(ModuleDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    if ((node.Keyword != Kind.GlobalKeyword))
    {
        this.writeKeyword(Core.IfElse((node.Keyword == Kind.NamespaceKeyword), "namespace", "module"));
        this.writeSpace();
    }
    this.emitModuleName(node.Name());
    var body = node.Body;
    while (((body != null) && Ast.IsModuleDeclaration(body)))
    {
        var module = body.AsModuleDeclaration();
        this.writePunctuation(".");
        this.emitNestedModuleName(module.Name());
        body = module.Body;
    }
    if ((node.Attributes != null))
    {
        this.writeSpace();
        this.writeKeyword("with");
        this.writeSpace();
        this.emitTypeNode(node.Attributes, TypePrecedence.NonArray);
    }
    if ((body == null))
    {
        this.writeTrailingSemicolon();
    }
    else
    {
        this.writeSpace();
        this.emitModuleBlock(body.AsModuleBlock());
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3856:1.
private void emitModuleBlock(ModuleBlock node)
{
    var state = this.enterNode(node.AsNode());
    this.generateNames(node.AsNode());
    this.emitToken(Kind.OpenBraceToken, node.Pos(), WriteKind.Punctuation, node.AsNode());
    var format = Core.IfElse((this.isEmptyBlock(node.AsNode(), node.Statements) || this.shouldEmitOnSingleLine(node.AsNode())), ListFormat.SingleLineBlockStatements, ListFormat.MultiLineBlockStatements);
    this.emitList((binderCallback, argnode) => binderCallback.emitStatement(argnode), node.AsNode(), node.Statements, format);
    this.emitTokenEx(Kind.CloseBraceToken, node.Statements.End(), WriteKind.Punctuation, node.AsNode(), Core.IfElse(((format & ListFormat.MultiLine) != 0), tokenEmitFlags.IndentLeadingComments, tokenEmitFlags.None));
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3868:1.
private void emitCaseBlock(CaseBlock node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.OpenBraceToken, node.Pos(), WriteKind.Punctuation, node.AsNode());
    this.emitList((binderCallback, argnode) => binderCallback.emitCaseOrDefaultClauseNode(argnode), node.AsNode(), node.Clauses, ListFormat.CaseBlockClauses);
    this.emitTokenEx(Kind.CloseBraceToken, node.Clauses.End(), WriteKind.Punctuation, node.AsNode(), tokenEmitFlags.IndentLeadingComments);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3876:1.
private void emitImportEqualsDeclaration(ImportEqualsDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    var pos = this.emitToken(Kind.ImportKeyword, greatestEnd(node.Pos(), node.Modifiers()), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    if (node.IsTypeOnly)
    {
        this.emitToken(Kind.TypeKeyword, pos, WriteKind.Keyword, node.AsNode());
        this.writeSpace();
    }
    this.emitBindingIdentifier(node.Name().AsIdentifier());
    this.writeSpace();
    this.emitToken(Kind.EqualsToken, node.Name().End(), WriteKind.Punctuation, node.AsNode());
    this.writeSpace();
    this.emitModuleReference(node.ModuleReference);
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3894:1.
private void emitModuleReference(Node node)
{
    switch (node.Kind)
    {
        case Kind.Identifier:
        {
            this.emitIdentifierReference(node.AsIdentifier());
            break;
        }
        case Kind.QualifiedName:
        {
            this.emitQualifiedName(node.AsQualifiedName());
            break;
        }
        case Kind.ExternalModuleReference:
        {
            this.emitExternalModuleReference(node.AsExternalModuleReference());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled ModuleReference: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3907:1.
private void emitImportDeclaration(ImportDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    this.emitToken(Kind.ImportKeyword, greatestEnd(node.Pos(), node.Modifiers()), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    if ((node.ImportClause != null))
    {
        this.emitImportClause(node.ImportClause.AsImportClause());
        this.writeSpace();
        this.emitToken(Kind.FromKeyword, node.ImportClause.End(), WriteKind.Keyword, node.AsNode());
        this.writeSpace();
    }
    this.emitExpression(node.ModuleSpecifier, OperatorPrecedence.Lowest);
    if ((node.Attributes != null))
    {
        this.writeSpace();
        this.emitImportAttributes(node.Attributes.AsImportAttributes());
    }
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3927:1.
private void emitImportClause(ImportClause node)
{
    var state = this.enterNode(node.AsNode());
    if ((node.PhaseModifier != Kind.Unknown))
    {
        this.emitToken(node.PhaseModifier, node.Pos(), WriteKind.Keyword, node.AsNode());
        this.writeSpace();
    }
    {
        var name = node.Name();
        if ((name != null))
        {
            this.emitBindingIdentifier(node.Name().AsIdentifier());
            if ((node.NamedBindings != null))
            {
                this.emitToken(Kind.CommaToken, name.End(), WriteKind.Punctuation, node.AsNode());
                this.writeSpace();
            }
        }
    }
    this.emitNamedImportBindings(node.NamedBindings);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3944:1.
private void emitNamespaceImport(NamespaceImport node)
{
    var state = this.enterNode(node.AsNode());
    var pos = this.emitToken(Kind.AsteriskToken, node.Pos(), WriteKind.Punctuation, node.AsNode());
    this.writeSpace();
    this.emitToken(Kind.AsKeyword, pos, WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitBindingIdentifier(node.Name().AsIdentifier());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3954:1.
private void emitNamedImports(NamedImports node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("{");
    this.emitList((binderCallback, argnode) => binderCallback.emitImportSpecifierNode(argnode), node.AsNode(), node.Elements, ListFormat.NamedImportsOrExportsElements);
    this.writePunctuation("}");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3962:1.
private void emitNamedImportBindings(Node node)
{
    if ((node == null))
    {
        return ;
    }
    switch (node.Kind)
    {
        case Kind.NamespaceImport:
        {
            this.emitNamespaceImport(node.AsNamespaceImport());
            break;
        }
        case Kind.NamedImports:
        {
            this.emitNamedImports(node.AsNamedImports());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled NamedImportBindings: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3976:1.
private void emitImportSpecifier(ImportSpecifier node)
{
    var state = this.enterNode(node.AsNode());
    if (node.IsTypeOnly)
    {
        this.writeKeyword("type");
        this.writeSpace();
    }
    if ((node.PropertyName != null))
    {
        this.emitModuleExportName(node.PropertyName);
        this.writeSpace();
        this.emitToken(Kind.AsKeyword, node.PropertyName.End(), WriteKind.Keyword, node.AsNode());
        this.writeSpace();
    }
    this.emitBindingIdentifier(node.Name().AsIdentifier());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3992:1.
private void emitImportSpecifierNode(Node node)
{
    this.emitImportSpecifier(node.AsImportSpecifier());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:3996:1.
private void emitExportAssignment(ExportAssignment node)
{
    var state = this.enterNode(node.AsNode());
    var nextPos = this.emitToken(Kind.ExportKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    if (node.IsExportEquals)
    {
        this.emitToken(Kind.EqualsToken, nextPos, WriteKind.Operator, node.AsNode());
    }
    else
    {
        this.emitToken(Kind.DefaultKeyword, nextPos, WriteKind.Keyword, node.AsNode());
    }
    this.writeSpace();
    if (node.IsExportEquals)
    {
        this.emitExpression(node.Expression, OperatorPrecedence.Assignment);
    }
    else
    {
        var expr = Ast.GetLeftmostExpression(node.Expression, false);
        if ((Ast.IsClassExpression(expr) || Ast.IsFunctionExpression(expr)))
        {
            this.emitExpression(node.Expression, OperatorPrecedence.Parentheses);
        }
        else
        {
            this.emitExpression(node.Expression, OperatorPrecedence.Assignment);
        }
    }
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4021:1.
private void emitExportDeclaration(ExportDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    this.emitModifierList(node.AsNode(), node.Modifiers(), false);
    var pos = this.emitToken(Kind.ExportKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    if (node.IsTypeOnly)
    {
        pos = this.emitToken(Kind.TypeKeyword, pos, WriteKind.Keyword, node.AsNode());
        this.writeSpace();
    }
    if ((node.ExportClause != null))
    {
        this.emitNamedExportBindings(node.ExportClause);
    }
    else
    {
        pos = this.emitToken(Kind.AsteriskToken, pos, WriteKind.Punctuation, node.AsNode());
    }
    if ((node.ModuleSpecifier != null))
    {
        this.writeSpace();
        this.emitToken(Kind.FromKeyword, greatestEnd(pos, node.ExportClause), WriteKind.Keyword, node.AsNode());
        this.writeSpace();
        this.emitExpression(node.ModuleSpecifier, OperatorPrecedence.Lowest);
    }
    if ((node.Attributes != null))
    {
        this.writeSpace();
        this.emitImportAttributes(node.Attributes.AsImportAttributes());
    }
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4049:1.
private void emitImportAttributes(ImportAttributes node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(node.Token, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitList((binderCallback, argnode) => binderCallback.emitImportAttributeNode(argnode), node.AsNode(), node.Attributes, ListFormat.ImportAttributes);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4057:1.
private void emitImportAttribute(ImportAttribute node)
{
    var state = this.enterNode(node.AsNode());
    this.emitImportAttributeName(node.Name());
    this.writePunctuation(":");
    this.writeSpace();
    var value = node.Value;
    if (((this.emitContext.EmitFlags(node.Value) & EmitFlags.NoLeadingComments) == 0))
    {
        var commentRange = this.emitContext.CommentRange(value);
        this.emitTrailingComments(commentRange.Pos(), commentSeparator.After);
    }
    this.emitExpression(value, OperatorPrecedence.DisallowComma);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4071:1.
private void emitImportAttributeNode(Node node)
{
    this.emitImportAttribute(node.AsImportAttribute());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4075:1.
private void emitNamespaceExportDeclaration(NamespaceExportDeclaration node)
{
    var state = this.enterNode(node.AsNode());
    var pos = this.emitToken(Kind.ExportKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    pos = this.emitToken(Kind.AsKeyword, pos, WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitToken(Kind.NamespaceKeyword, pos, WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitBindingIdentifier(node.Name().AsIdentifier());
    this.writeTrailingSemicolon();
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4088:1.
private void emitNamespaceExport(NamespaceExport node)
{
    var state = this.enterNode(node.AsNode());
    var pos = this.emitToken(Kind.AsteriskToken, node.Pos(), WriteKind.Punctuation, node.AsNode());
    this.writeSpace();
    this.emitToken(Kind.AsKeyword, pos, WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitModuleExportName(node.Name());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4098:1.
private void emitNamedExports(NamedExports node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("{");
    this.emitList((binderCallback, argnode) => binderCallback.emitExportSpecifierNode(argnode), node.AsNode(), node.Elements, ListFormat.NamedImportsOrExportsElements);
    this.writePunctuation("}");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4106:1.
private void emitNamedExportBindings(Node node)
{
    switch (node.Kind)
    {
        case Kind.NamespaceExport:
        {
            this.emitNamespaceExport(node.AsNamespaceExport());
            break;
        }
        case Kind.NamedExports:
        {
            this.emitNamedExports(node.AsNamedExports());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled NamedExportBindings: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4117:1.
private void emitExportSpecifier(ExportSpecifier node)
{
    var state = this.enterNode(node.AsNode());
    if (node.IsTypeOnly)
    {
        this.writeKeyword("type");
        this.writeSpace();
    }
    if ((node.PropertyName != null))
    {
        this.emitModuleExportName(node.PropertyName);
        this.writeSpace();
        this.emitToken(Kind.AsKeyword, node.PropertyName.End(), WriteKind.Keyword, node.AsNode());
        this.writeSpace();
    }
    this.emitModuleExportName(node.Name());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4133:1.
private void emitExportSpecifierNode(Node node)
{
    this.emitExportSpecifier(node.AsExportSpecifier());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4137:1.
private void emitEmbeddedStatement(Node parentNode, Node node)
{
    if (((Ast.IsBlock(node) || this.shouldEmitOnSingleLine(parentNode)) || (this.Options.PreserveSourceNewlines && (this.getLeadingLineTerminatorCount(parentNode, node, ListFormat.None) == 0))))
    {
        this.writeSpace();
        this.emitStatement(node);
    }
    else
    {
        this.writeLine();
        this.increaseIndent();
        if ((node.Kind == Kind.EmptyStatement))
        {
            this.emitEmptyStatement(node.AsEmptyStatement(), true);
        }
        else
        {
            this.emitStatement(node);
        }
        this.decreaseIndent();
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4155:1.
private void emitStatement(Node node)
{
    {
        var snippetElement = this.emitContext.SnippetElement(node);
        if ((snippetElement != null))
        {
            this.emitSnippetNode(node, snippetElement.Value);
            return ;
        }
    }
    switch (node.Kind)
    {
        case Kind.Block:
        {
            this.emitBlock(node.AsBlock());
            break;
        }
        case Kind.EmptyStatement:
        {
            this.emitEmptyStatement(node.AsEmptyStatement(), false);
            break;
        }
        case Kind.VariableStatement:
        {
            this.emitVariableStatement(node.AsVariableStatement());
            break;
        }
        case Kind.ExpressionStatement:
        {
            this.emitExpressionStatement(node.AsExpressionStatement());
            break;
        }
        case Kind.IfStatement:
        {
            this.emitIfStatement(node.AsIfStatement());
            break;
        }
        case Kind.DoStatement:
        {
            this.emitDoStatement(node.AsDoStatement());
            break;
        }
        case Kind.WhileStatement:
        {
            this.emitWhileStatement(node.AsWhileStatement());
            break;
        }
        case Kind.ForStatement:
        {
            this.emitForStatement(node.AsForStatement());
            break;
        }
        case Kind.ForInStatement:
        {
            this.emitForInStatement(node.AsForInOrOfStatement());
            break;
        }
        case Kind.ForOfStatement:
        {
            this.emitForOfStatement(node.AsForInOrOfStatement());
            break;
        }
        case Kind.ContinueStatement:
        {
            this.emitContinueStatement(node.AsContinueStatement());
            break;
        }
        case Kind.BreakStatement:
        {
            this.emitBreakStatement(node.AsBreakStatement());
            break;
        }
        case Kind.ReturnStatement:
        {
            this.emitReturnStatement(node.AsReturnStatement());
            break;
        }
        case Kind.WithStatement:
        {
            this.emitWithStatement(node.AsWithStatement());
            break;
        }
        case Kind.SwitchStatement:
        {
            this.emitSwitchStatement(node.AsSwitchStatement());
            break;
        }
        case Kind.LabeledStatement:
        {
            this.emitLabeledStatement(node.AsLabeledStatement());
            break;
        }
        case Kind.ThrowStatement:
        {
            this.emitThrowStatement(node.AsThrowStatement());
            break;
        }
        case Kind.TryStatement:
        {
            this.emitTryStatement(node.AsTryStatement());
            break;
        }
        case Kind.DebuggerStatement:
        {
            this.emitDebuggerStatement(node.AsDebuggerStatement());
            break;
        }
        case Kind.NotEmittedStatement:
        {
            this.emitNotEmittedStatement(node.AsNotEmittedStatement());
            break;
        }
        case Kind.FunctionDeclaration:
        {
            this.emitFunctionDeclaration(node.AsFunctionDeclaration());
            break;
        }
        case Kind.ClassDeclaration:
        {
            this.emitClassDeclaration(node.AsClassDeclaration());
            break;
        }
        case Kind.InterfaceDeclaration:
        {
            this.emitInterfaceDeclaration(node.AsInterfaceDeclaration());
            break;
        }
        case Kind.TypeAliasDeclaration:
        case Kind.JSTypeAliasDeclaration:
        {
            this.emitTypeAliasDeclaration(node.AsTypeAliasDeclaration());
            break;
        }
        case Kind.EnumDeclaration:
        {
            this.emitEnumDeclaration(node.AsEnumDeclaration());
            break;
        }
        case Kind.ModuleDeclaration:
        {
            this.emitModuleDeclaration(node.AsModuleDeclaration());
            break;
        }
        case Kind.MissingDeclaration:
        {
            break;
        }
        case Kind.NamespaceExportDeclaration:
        {
            this.emitNamespaceExportDeclaration(node.AsNamespaceExportDeclaration());
            break;
        }
        case Kind.ImportEqualsDeclaration:
        {
            this.emitImportEqualsDeclaration(node.AsImportEqualsDeclaration());
            break;
        }
        case Kind.ImportDeclaration:
        {
            this.emitImportDeclaration(node.AsImportDeclaration());
            break;
        }
        case Kind.ExportAssignment:
        {
            this.emitExportAssignment(node.AsExportAssignment());
            break;
        }
        case Kind.ExportDeclaration:
        {
            this.emitExportDeclaration(node.AsExportDeclaration());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled statement: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4241:1.
private void emitExternalModuleReference(ExternalModuleReference node)
{
    var state = this.enterNode(node.AsNode());
    this.writeKeyword("require");
    this.writePunctuation("(");
    this.emitExpression(node.Expression, OperatorPrecedence.DisallowComma);
    this.writePunctuation(")");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4254:1.
private void emitJsxElement(JsxElement node)
{
    var state = this.enterNode(node.AsNode());
    this.emitJsxOpeningElement(node.OpeningElement.AsJsxOpeningElement());
    this.emitList((binderCallback, argnode) => binderCallback.emitJsxChild(argnode), node.AsNode(), node.Children, ListFormat.JsxElementOrFragmentChildren);
    this.emitJsxClosingElement(node.ClosingElement.AsJsxClosingElement());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4262:1.
private void emitJsxSelfClosingElement(JsxSelfClosingElement node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("<");
    this.emitJsxTagName(node.TagName);
    this.emitTypeArguments(node.AsNode(), node.TypeArguments);
    this.writeSpace();
    this.emitJsxAttributes(node.Attributes.AsJsxAttributes());
    this.writePunctuation("/>");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4273:1.
private void emitJsxFragment(JsxFragment node)
{
    var state = this.enterNode(node.AsNode());
    this.emitJsxOpeningFragment(node.OpeningFragment.AsJsxOpeningFragment());
    this.emitList((binderCallback, argnode) => binderCallback.emitJsxChild(argnode), node.AsNode(), node.Children, ListFormat.JsxElementOrFragmentChildren);
    this.emitJsxClosingFragment(node.ClosingFragment.AsJsxClosingFragment());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4281:1.
private void emitJsxOpeningElement(JsxOpeningElement node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("<");
    var indented = this.writeLineSeparatorsAndIndentBefore(node.TagName, node.AsNode());
    this.emitJsxTagName(node.TagName);
    this.emitTypeArguments(node.AsNode(), node.TypeArguments);
    if ((GoRuntime.Len(node.Attributes.Properties()) > 0))
    {
        this.writeSpace();
    }
    this.emitJsxAttributes(node.Attributes.AsJsxAttributes());
    this.writeLineSeparatorsAfter(node.Attributes, node.AsNode());
    this.decreaseIndentIf(indented);
    this.writePunctuation(">");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4297:1.
private void emitJsxClosingElement(JsxClosingElement node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("</");
    this.emitJsxTagName(node.TagName);
    this.writePunctuation(">");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4305:1.
private void emitJsxOpeningFragment(JsxOpeningFragment node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("<");
    this.writePunctuation(">");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4312:1.
private void emitJsxClosingFragment(JsxClosingFragment node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("</");
    this.writePunctuation(">");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4319:1.
private void emitJsxText(JsxText node)
{
    var state = this.enterNode(node.AsNode());
    this.writeLiteral(node.AsNode().TextValue());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4326:1.
private void emitJsxAttributes(JsxAttributes node)
{
    var state = this.enterNode(node.AsNode());
    this.emitList((binderCallback, argnode) => binderCallback.emitJsxAttributeLike(argnode), node.AsNode(), node.Properties, ListFormat.JsxElementAttributes);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4332:1.
private void emitJsxAttribute(JsxAttribute node)
{
    var state = this.enterNode(node.AsNode());
    this.emitJsxAttributeName(node.Name());
    if ((node.Initializer != null))
    {
        this.writePunctuation("=");
        this.emitJsxAttributeValue(node.Initializer);
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4342:1.
private void emitJsxSpreadAttribute(JsxSpreadAttribute node)
{
    var state = this.enterNode(node.AsNode());
    this.writePunctuation("{...");
    this.emitExpression(node.Expression, OperatorPrecedence.Lowest);
    this.writePunctuation("}");
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4350:1.
private void emitJsxAttributeLike(Node node)
{
    switch (node.Kind)
    {
        case Kind.JsxAttribute:
        {
            this.emitJsxAttribute(node.AsJsxAttribute());
            break;
        }
        case Kind.JsxSpreadAttribute:
        {
            this.emitJsxSpreadAttribute(node.AsJsxSpreadAttribute());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled JsxAttributeLike: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4361:1.
private void emitJsxExpression(JsxExpression node)
{
    var state = this.enterNode(node.AsNode());
    if (((node.Expression != null) || ((!this.commentsDisabled && !Ast.NodeIsSynthesized(node.AsNode())) && this.hasCommentsAtPosition(node.Pos()))))
    {
        var indented = (((this.currentSourceFile != null) && !Ast.NodeIsSynthesized(node.AsNode())) && (GetLinesBetweenPositions(this.currentSourceFile, node.Pos(), node.End()) != 0));
        this.increaseIndentIf(indented);
        var end = this.emitToken(Kind.OpenBraceToken, node.Pos(), WriteKind.Punctuation, node.AsNode());
        this.emitTokenNode(node.DotDotDotToken);
        if ((node.Expression != null))
        {
            this.emitExpression(node.Expression, OperatorPrecedence.DisallowComma);
        }
        this.emitToken(Kind.CloseBraceToken, greatestEnd(end, node.Expression, node.DotDotDotToken), WriteKind.Punctuation, node.AsNode());
        this.decreaseIndentIf(indented);
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4377:1.
private void emitJsxNamespacedName(JsxNamespacedName node)
{
    var state = this.enterNode(node.AsNode());
    this.emitIdentifierName(node.Namespace.AsIdentifier());
    this.writePunctuation(":");
    this.emitIdentifierName(node.Name().AsIdentifier());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4385:1.
private void emitJsxChild(Node node)
{
    switch (node.Kind)
    {
        case Kind.JsxText:
        {
            this.emitJsxText(node.AsJsxText());
            break;
        }
        case Kind.JsxExpression:
        {
            this.emitJsxExpression(node.AsJsxExpression());
            break;
        }
        case Kind.JsxElement:
        {
            this.emitJsxElement(node.AsJsxElement());
            break;
        }
        case Kind.JsxSelfClosingElement:
        {
            this.emitJsxSelfClosingElement(node.AsJsxSelfClosingElement());
            break;
        }
        case Kind.JsxFragment:
        {
            this.emitJsxFragment(node.AsJsxFragment());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled JsxChild: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4402:1.
private void emitJsxTagName(Node node)
{
    switch (node.Kind)
    {
        case Kind.Identifier:
        {
            this.emitIdentifierReference(node.AsIdentifier());
            break;
        }
        case Kind.ThisKeyword:
        {
            this.emitKeywordExpression(node.AsKeywordExpression());
            break;
        }
        case Kind.JsxNamespacedName:
        {
            this.emitJsxNamespacedName(node.AsJsxNamespacedName());
            break;
        }
        case Kind.PropertyAccessExpression:
        {
            this.emitPropertyAccessExpression(node.AsPropertyAccessExpression());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled JsxTagName: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4417:1.
private void emitJsxAttributeName(Node node)
{
    switch (node.Kind)
    {
        case Kind.Identifier:
        {
            this.emitIdentifierName(node.AsIdentifier());
            break;
        }
        case Kind.JsxNamespacedName:
        {
            this.emitJsxNamespacedName(node.AsJsxNamespacedName());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled JsxAttributeName: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4428:1.
private void emitJsxAttributeValue(Node node)
{
    switch (node.Kind)
    {
        case Kind.StringLiteral:
        {
            this.emitStringLiteral(node.AsStringLiteral());
            break;
        }
        case Kind.JsxExpression:
        {
            this.emitJsxExpression(node.AsJsxExpression());
            break;
        }
        case Kind.JsxElement:
        {
            this.emitJsxElement(node.AsJsxElement());
            break;
        }
        case Kind.JsxSelfClosingElement:
        {
            this.emitJsxSelfClosingElement(node.AsJsxSelfClosingElement());
            break;
        }
        case Kind.JsxFragment:
        {
            this.emitJsxFragment(node.AsJsxFragment());
            break;
        }
        default:
        {
            this.emitExpression(node, OperatorPrecedence.Lowest);
            break;
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4449:1.
private void emitCaseOrDefaultClauseStatements(CaseOrDefaultClause node, int colonPos)
{
    var emitAsSingleStatement = ((GoRuntime.Len(node.Statements.Nodes) == 1) && (((((this.currentSourceFile == null) || Ast.NodeIsSynthesized(node.AsNode())) || Ast.NodeIsSynthesized(node.Statements.Nodes[0])) || RangeStartPositionsAreOnSameLine(node.Loc, node.Statements.Nodes[0].Loc, this.currentSourceFile))));
    var format = ListFormat.CaseOrDefaultClauseStatements;
    if (emitAsSingleStatement)
    {
        this.writeTokenText(Kind.ColonToken, WriteKind.Punctuation, colonPos);
        this.writeSpace();
        format &= ~((ListFormat.MultiLine | ListFormat.Indented));
    }
    else
    {
        this.emitToken(Kind.ColonToken, colonPos, WriteKind.Punctuation, node.AsNode());
    }
    this.emitList((binderCallback, argnode) => binderCallback.emitStatement(argnode), node.AsNode(), node.Statements, format);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4471:1.
private void emitCaseClause(CaseOrDefaultClause node)
{
    var state = this.enterNode(node.AsNode());
    this.emitToken(Kind.CaseKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitExpression(node.Expression, OperatorPrecedence.Lowest);
    this.emitCaseOrDefaultClauseStatements(node, node.Expression.End());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4480:1.
private void emitDefaultClause(CaseOrDefaultClause node)
{
    var state = this.enterNode(node.AsNode());
    var pos = this.emitToken(Kind.DefaultKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.emitCaseOrDefaultClauseStatements(node, pos);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4487:1.
private void emitCaseOrDefaultClauseNode(Node node)
{
    switch (node.Kind)
    {
        case Kind.CaseClause:
        {
            this.emitCaseClause(node.AsCaseOrDefaultClause());
            break;
        }
        case Kind.DefaultClause:
        {
            this.emitDefaultClause(node.AsCaseOrDefaultClause());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled CaseOrDefaultClause: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4498:1.
private void emitHeritageClause(HeritageClause node)
{
    var state = this.enterNode(node.AsNode());
    this.writeSpace();
    this.emitToken(node.Token, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    this.emitList((binderCallback, argnode) => binderCallback.emitHeritageClauseElement(argnode), node.AsNode(), node.Types, ListFormat.HeritageClauseTypes);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4507:1.
private void emitHeritageClauseElement(Node node)
{
    switch (node.Kind)
    {
        case Kind.ExpressionWithTypeArguments:
        {
            this.emitExpressionWithTypeArguments(node.AsExpressionWithTypeArguments());
            break;
        }
        case Kind.TypeReference:
        {
            this.emitTypeReference(node.AsTypeReferenceNode());
            break;
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("unhandled HeritageClauseElement: %v", node.Kind));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4518:1.
private void emitHeritageClauseNode(Node node)
{
    this.emitHeritageClause(node.AsHeritageClause());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4522:1.
private void emitCatchClause(CatchClause node)
{
    var state = this.enterNode(node.AsNode());
    var openParenPos = this.emitToken(Kind.CatchKeyword, node.Pos(), WriteKind.Keyword, node.AsNode());
    this.writeSpace();
    if ((node.VariableDeclaration != null))
    {
        this.emitToken(Kind.OpenParenToken, openParenPos, WriteKind.Punctuation, node.AsNode());
        this.emitVariableDeclaration(node.VariableDeclaration.AsVariableDeclaration());
        this.emitToken(Kind.CloseParenToken, node.VariableDeclaration.End(), WriteKind.Punctuation, node.AsNode());
        this.writeSpace();
    }
    this.emitBlock(node.Block.AsBlock());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4542:1.
private void emitPropertyAssignment(PropertyAssignment node)
{
    var state = this.enterNode(node.AsNode());
    this.emitPropertyName(node.Name());
    this.writePunctuation(":");
    this.writeSpace();
    var initializer = node.Initializer;
    if (((this.emitContext.EmitFlags(initializer) & EmitFlags.NoLeadingComments) == 0))
    {
        var commentRange = this.emitContext.CommentRange(initializer);
        this.emitTrailingComments(commentRange.Pos(), commentSeparator.After);
    }
    this.emitExpression(initializer, OperatorPrecedence.DisallowComma);
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4563:1.
private void emitShorthandPropertyAssignment(ShorthandPropertyAssignment node)
{
    var state = this.enterNode(node.AsNode());
    this.emitPropertyName(node.Name());
    if ((node.ObjectAssignmentInitializer != null))
    {
        this.writeSpace();
        this.writePunctuation("=");
        this.writeSpace();
        this.emitExpression(node.ObjectAssignmentInitializer, OperatorPrecedence.DisallowComma);
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4575:1.
private void emitSpreadAssignment(SpreadAssignment node)
{
    var state = this.enterNode(node.AsNode());
    if ((node.Expression != null))
    {
        this.emitToken(Kind.DotDotDotToken, node.Pos(), WriteKind.Punctuation, node.AsNode());
        this.emitExpression(node.Expression, OperatorPrecedence.DisallowComma);
    }
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4588:1.
private void emitEnumMember(EnumMember node)
{
    var state = this.enterNode(node.AsNode());
    this.emitPropertyName(node.Name());
    this.emitInitializer(node.Initializer, node.Name().End(), node.AsNode());
    this.exitNode(node.AsNode(), state);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4595:1.
private void emitEnumMemberNode(Node node)
{
    this.emitEnumMember(node.AsEnumMember());
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4603:1.
private void emitJSDocNode(Node node)
{
    throw new InvalidOperationException("not implemented");
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4612:1.
private void emitShebangIfNeeded(SourceFile node)
{
    if (Ast.NodeIsSynthesized(node.AsNode()))
    {
        return ;
    }
    var shebang = ByteScanner.GetShebangByteString(node.TextView());
    if ((shebang.Length != 0))
    {
        this.writeComment(ByteStringValue.FromView(shebang));
        this.writeLine();
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4623:1.
private int emitPrologueDirectives(NodeList statements)
{
    {
        foreach (var range11 in GoRuntime.Range(statements.Nodes))
        {
            var i = range11.Key;
            var statement = range11.Value;
            if (Ast.IsPrologueDirective(statement))
            {
                this.writeLine();
                this.emitStatement(statement);
            }
            else
            {
                return i;
            }
        }
    }
    return GoRuntime.Len(statements.Nodes);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4635:1.
private bool emitHelpers(Node node)
{
    var helpersEmitted = false;
    var sourceFile = this.currentSourceFile;
    var shouldSkip = (this.Options.NoEmitHelpers || (((sourceFile != null) && this.emitContext.HasRecordedExternalHelpers(sourceFile))));
    var helpers = GoSlices.Clone(this.emitContext.GetEmitHelpers(node));
    if ((GoRuntime.Len(helpers) > 0))
    {
        GoSlices.SortStableFunc(helpers, EmitHelpers.compareEmitHelpers);
        {
            foreach (var helper in GoRuntime.RangeValues(helpers))
            {
                if (!helper.Scoped)
                {
                    if (shouldSkip)
                    {
                        continue;
                    }
                }
                if ((helper.TextCallback != null))
                {
                    this.writeLines(helper.TextCallback(this.makeFileLevelOptimisticUniqueName));
                }
                else
                {
                    this.writeLines(helper.Text);
                }
                helpersEmitted = true;
            }
        }
    }
    return helpersEmitted;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4663:1.
private void emitSourceFile(SourceFile node)
{
    var savedCurrentSourceFile = this.currentSourceFile;
    var savedCommentsDisabled = this.commentsDisabled;
    this.currentSourceFile = node;
    this.writeLine();
    this.pushNameGenerationScope(node.AsNode());
    this.generateAllNames(node.Statements);
    var index = 0;
    commentState? state = default;
    if ((node.ScriptKind != ScriptKind.JSON))
    {
        this.emitShebangIfNeeded(node);
        index = this.emitPrologueDirectives(node.Statements);
        if (!this.writer.IsAtStartOfLine())
        {
            this.writeLine();
        }
        state = this.emitDetachedCommentsBeforeStatementList(node.AsNode(), node.Statements.Loc);
        this.emitHelpers(node.AsNode());
        if (node.IsDeclarationFile)
        {
            this.emitTripleSlashDirectives(node);
        }
    }
    else
    {
        state = this.emitDetachedCommentsBeforeStatementList(node.AsNode(), node.Statements.Loc);
    }
    this.emitListRange((binderCallback, argnode) => binderCallback.emitStatement(argnode), node.AsNode(), node.Statements, ListFormat.MultiLine, index, -1);
    this.popNameGenerationScope(node.AsNode());
    this.emitDetachedCommentsAfterStatementList(node.AsNode(), node.Statements.Loc, state);
    this.currentSourceFile = savedCurrentSourceFile;
    this.commentsDisabled = savedCommentsDisabled;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4705:1.
private void emitTripleSlashDirectives(SourceFile node)
{
    this.emitDirective("path", node.ReferencedFiles);
    this.emitDirective("types", node.TypeReferenceDirectives);
    this.emitDirective("lib", node.LibReferenceDirectives);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4711:1.
private void emitDirective(string kind, Slice<FileReference> refs)
{
    {
        foreach (var @ref in GoRuntime.RangeValues(refs))
        {
            string resolutionMode = "";
            if ((@ref.ResolutionMode != ResolutionMode.None))
            {
                resolutionMode = Fmt.Sprintf("resolution-mode=\"%s\" ", Core.IfElse((@ref.ResolutionMode == ResolutionMode.ESNext), "import", "require"));
            }
            this.writeComment(Fmt.Sprintf("/// <reference %s=\"%s\" %s%s/>", kind, @ref.FileName, resolutionMode, Core.IfElse(@ref.Preserve, "preserve=\"true\" ", "")));
            this.writeLine();
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4726:1.
private void emitList(Action<Printer, Node> emit, Node parentNode, NodeList children, ListFormat format)
{
    if (this.shouldEmitOnMultipleLines(parentNode))
    {
        format |= (ListFormat.PreferNewLine | ListFormat.Indented);
    }
    this.emitListRange(emit, parentNode, children, format, -1, -1);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4734:1.
private void emitListRange(Action<Printer, Node> emit, Node parentNode, NodeList children, ListFormat format, int start, int count)
{
    var isNil = (children == null);
    var length = 0;
    if (!isNil)
    {
        length = GoRuntime.Len(children.Nodes);
    }
    if ((start < 0))
    {
        start = 0;
    }
    if ((count < 0))
    {
        count = (length - start);
    }
    if ((isNil && ((format & ListFormat.OptionalIfNil) != 0)))
    {
        return ;
    }
    var isEmpty = ((isNil || (start >= length)) || (count <= 0));
    if ((isEmpty && ((format & ListFormat.OptionalIfEmpty) != 0)))
    {
        if ((this.OnBeforeEmitNodeList != null))
        {
            this.OnBeforeEmitNodeList(children);
        }
        if ((this.OnAfterEmitNodeList != null))
        {
            this.OnAfterEmitNodeList(children);
        }
        return ;
    }
    if (((format & ListFormat.BracketsMask) != 0))
    {
        this.writePunctuation(getOpeningBracket(format));
        if ((isEmpty && !isNil))
        {
            this.emitTrailingComments(children.Pos(), commentSeparator.Before);
        }
    }
    if ((this.OnBeforeEmitNodeList != null))
    {
        this.OnBeforeEmitNodeList(children);
    }
    if (isEmpty)
    {
        if ((((format & ListFormat.MultiLine) != 0) && !((this.Options.PreserveSourceNewlines && (((parentNode == null) || ((this.currentSourceFile != null) && RangeIsOnSingleLine(parentNode.Loc, this.currentSourceFile))))))))
        {
            this.writeLine();
        }
        else
        {
            if ((((format & ListFormat.SpaceBetweenBraces) != 0) && ((format & ListFormat.NoSpaceIfEmpty) == 0)))
            {
                this.writeSpace();
            }
        }
    }
    else
    {
        var end = Math.Min((start + count), length);
        this.emitListItems(emit, parentNode, children.Nodes.Slice2(start, end), format, this.hasTrailingComma(parentNode, children), children.Loc);
    }
    if ((this.OnAfterEmitNodeList != null))
    {
        this.OnAfterEmitNodeList(children);
    }
    if (((format & ListFormat.BracketsMask) != 0))
    {
        if ((isEmpty && !isNil))
        {
            this.emitLeadingComments(children.End(), false);
        }
        this.writePunctuation(getClosingBracket(format));
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4801:1.
private bool hasTrailingComma(Node parentNode, NodeList children)
{
    if (!children.HasTrailingComma())
    {
        return false;
    }
    var originalParent = this.emitContext.MostOriginal(parentNode);
    if ((originalParent == parentNode))
    {
        return true;
    }
    if ((originalParent.Kind != parentNode.Kind))
    {
        return false;
    }
    var originalList = children;
    switch (originalParent.Kind)
    {
        case Kind.ObjectLiteralExpression:
        {
            originalList = originalParent.PropertyList();
            break;
        }
        case Kind.ArrayLiteralExpression:
        {
            originalList = originalParent.ElementList();
            break;
        }
        case Kind.CallExpression:
        case Kind.NewExpression:
        {
            switch (children)
            {
                case var list when ReferenceEquals(list, parentNode.TypeArgumentList()):
                {
                    originalList = originalParent.TypeArgumentList();
                    break;
                }
                case var list when ReferenceEquals(list, parentNode.ArgumentList()):
                {
                    originalList = originalParent.ArgumentList();
                    break;
                }
            }
            break;
        }
        case Kind.Constructor:
        case Kind.MethodDeclaration:
        case Kind.GetAccessor:
        case Kind.SetAccessor:
        case Kind.FunctionDeclaration:
        case Kind.FunctionExpression:
        case Kind.ArrowFunction:
        case Kind.FunctionType:
        case Kind.ConstructorType:
        case Kind.CallSignature:
        case Kind.ConstructSignature:
        {
            switch (children)
            {
                case var list when ReferenceEquals(list, parentNode.TypeParameterList()):
                {
                    originalList = originalParent.TypeParameterList();
                    break;
                }
                case var list when ReferenceEquals(list, parentNode.ParameterList()):
                {
                    originalList = originalParent.ParameterList();
                    break;
                }
            }
            break;
        }
        case Kind.ClassDeclaration:
        case Kind.ClassExpression:
        case Kind.InterfaceDeclaration:
        case Kind.TypeAliasDeclaration:
        case Kind.JSTypeAliasDeclaration:
        {
            switch (children)
            {
                case var list when ReferenceEquals(list, parentNode.TypeParameterList()):
                {
                    originalList = originalParent.TypeParameterList();
                    break;
                }
            }
            break;
        }
        case Kind.ObjectBindingPattern:
        case Kind.ArrayBindingPattern:
        {
            switch (children)
            {
                case var list when ReferenceEquals(list, parentNode.ElementList()):
                {
                    originalList = originalParent.ElementList();
                    break;
                }
            }
            break;
        }
        case Kind.NamedImports:
        case Kind.NamedExports:
        {
            originalList = originalParent.ElementList();
            break;
        }
        case Kind.ImportAttributes:
        {
            originalList = originalParent.AsImportAttributes().Attributes;
            break;
        }
    }
    if ((originalList != null))
    {
        return originalList.HasTrailingComma();
    }
    return false;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4875:1.
private void writeDelimiter(ListFormat format)
{
    switch ((format & ListFormat.DelimitersMask))
    {
        case ListFormat.None:
        {
            break;
        }
        case ListFormat.CommaDelimited:
        {
            this.writePunctuation(",");
            break;
        }
        case ListFormat.BarDelimited:
        {
            this.writeSpace();
            this.writePunctuation("|");
            break;
        }
        case ListFormat.AsteriskDelimited:
        {
            this.writeSpace();
            this.writePunctuation("*");
            this.writeSpace();
            break;
        }
        case ListFormat.AmpersandDelimited:
        {
            this.writeSpace();
            this.writePunctuation("&");
            break;
        }
    }
}

// Emits a list without brackets or raising events.
//
// NOTE: You probably don't want to call this directly and should be using `emitList` instead.
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:4897:1.
private void emitListItems(Action<Printer, Node> emit, Node parentNode, Slice<Node> children, ListFormat format, bool hasTrailingComma, TextRange childrenTextRange)
{
    var mayEmitInterveningComments = ((format & ListFormat.NoInterveningComments) == 0);
    var shouldEmitInterveningComments = mayEmitInterveningComments;
    var leadingLineTerminatorCount = 0;
    if ((GoRuntime.Len(children) > 0))
    {
        leadingLineTerminatorCount = this.getLeadingLineTerminatorCount(parentNode, children[0], format);
    }
    if ((leadingLineTerminatorCount > 0))
    {
        {
            foreach (var ignored14 in GoRuntime.RangeKeys(leadingLineTerminatorCount))
            {
                this.writeLine();
            }
        }
        shouldEmitInterveningComments = false;
    }
    else
    {
        if (((format & ListFormat.SpaceBetweenBraces) != 0))
        {
            this.writeSpace();
        }
    }
    if (((format & ListFormat.Indented) != 0))
    {
        this.increaseIndent();
    }
    var parentEnd = greatestEnd(-1, parentNode);
    Node previousSibling = default;
    var shouldDecreaseIndentAfterEmit = false;
    {
        foreach (var child in GoRuntime.RangeValues(children))
        {
            if (((format & ListFormat.AsteriskDelimited) != 0))
            {
                this.writeLine();
                this.writeDelimiter(format);
            }
            else
            {
                if ((previousSibling != null))
                {
                    if ((((format & ListFormat.DelimitersMask) != 0) && (previousSibling.End() != parentEnd)))
                    {
                        if ((!this.commentsDisabled && this.shouldEmitTrailingComments(previousSibling)))
                        {
                            this.emitLeadingComments(previousSibling.End(), false);
                        }
                    }
                    this.writeDelimiter(format);
                    var separatingLineTerminatorCount = this.getSeparatingLineTerminatorCount(previousSibling, child, format);
                    if ((separatingLineTerminatorCount > 0))
                    {
                        if (((format & ((ListFormat.LinesMask | ListFormat.Indented))) == ListFormat.SingleLine))
                        {
                            this.increaseIndent();
                            shouldDecreaseIndentAfterEmit = true;
                        }
                        if ((((shouldEmitInterveningComments && ((format & ListFormat.DelimitersMask) != 0)) && !Ast.PositionIsSynthesized(child.Pos())) && this.shouldEmitLeadingComments(child)))
                        {
                            var commentRange = this.emitContext.CommentRange(child);
                            this.emitTrailingCommentsOfPosition(commentRange.Pos(), ((format & ListFormat.SpaceBetweenSiblings) != 0), true);
                        }
                        {
                            foreach (var ignored16 in GoRuntime.RangeKeys(separatingLineTerminatorCount))
                            {
                                this.writeLine();
                            }
                        }
                        shouldEmitInterveningComments = false;
                    }
                    else
                    {
                        if (((format & ListFormat.SpaceBetweenSiblings) != 0))
                        {
                            this.writeSpace();
                        }
                    }
                }
            }
            if ((shouldEmitInterveningComments && this.shouldEmitLeadingComments(child)))
            {
                var commentRange2 = this.emitContext.CommentRange(child);
                this.emitTrailingCommentsOfPosition(commentRange2.Pos(), false, false);
            }
            else
            {
                shouldEmitInterveningComments = mayEmitInterveningComments;
            }
            this.nextListElementPos = child.Pos();
            emit(this, child);
            if (shouldDecreaseIndentAfterEmit)
            {
                this.decreaseIndent();
                shouldDecreaseIndentAfterEmit = false;
            }
            previousSibling = child;
        }
    }
    var skipTrailingComments = (this.commentsDisabled || !this.shouldEmitTrailingComments(previousSibling));
    var emitTrailingComma = ((hasTrailingComma && ((format & ListFormat.AllowTrailingComma) != 0)) && ((format & ListFormat.CommaDelimited) != 0));
    if (emitTrailingComma)
    {
        if (((previousSibling != null) && !skipTrailingComments))
        {
            this.emitToken(Kind.CommaToken, previousSibling.End(), WriteKind.Punctuation, previousSibling);
        }
        else
        {
            this.writePunctuation(",");
        }
    }
    if (((((previousSibling != null) && (parentEnd != previousSibling.End())) && ((format & ListFormat.DelimitersMask) != 0)) && !skipTrailingComments))
    {
        int commentsPos = default;
        if ((emitTrailingComma && (childrenTextRange.End() > 0)))
        {
            commentsPos = childrenTextRange.End();
        }
        else
        {
            commentsPos = previousSibling.End();
        }
        this.emitLeadingComments(commentsPos, false);
    }
    if (((format & ListFormat.Indented) != 0))
    {
        this.decreaseIndent();
    }
    var closingLineTerminatorCount = this.getClosingLineTerminatorCount(parentNode, Core.LastOrNil(children), format, childrenTextRange);
    if ((closingLineTerminatorCount > 0))
    {
        {
            foreach (var ignored17 in GoRuntime.RangeKeys(closingLineTerminatorCount))
            {
                this.writeLine();
            }
        }
    }
    else
    {
        if (((format & ((ListFormat.SpaceAfterList | ListFormat.SpaceBetweenBraces))) != 0))
        {
            this.writeSpace();
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5044:1.
internal string Emit(Node node, SourceFile sourceFile = null)
{
    if ((this.ownWriter == null))
    {
        this.ownWriter = TextWriter.NewTextWriter(this.Options.NewLine.GetNewLineCharacter(), 0);
    }
    this.Write(node, sourceFile, this.ownWriter, null);
    var text = this.ownWriter.String();
    this.ownWriter.Clear();
    return text;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5057:1.
public string EmitSourceFile(SourceFile sourceFile)
{
    return this.Emit(sourceFile.AsNode(), sourceFile);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5061:1.
private void setSourceFile(SourceFile sourceFile)
{
    this.currentSourceFile = sourceFile;
    this.uniqueHelperNames = null;
    this.externalHelpersModuleName = null;
    if ((sourceFile != null))
    {
        if (((this.emitContext.EmitFlags(this.emitContext.MostOriginal(sourceFile.AsNode())) & EmitFlags.ExternalHelpers) != 0))
        {
            this.uniqueHelperNames = new Dictionary<string, Node>();
        }
        this.externalHelpersModuleName = this.emitContext.GetExternalHelpersModuleName(sourceFile);
        this.setSourceMapSource(sourceFile);
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5076:1.
internal void Write(Node node, SourceFile sourceFile, EmitTextWriter writer, Generator sourceMapGenerator = null)
{
    var savedCurrentSourceFile = this.currentSourceFile;
    var savedWriter = this.writer;
    var savedUniqueHelperNames = this.uniqueHelperNames;
    var savedSourceMapsDisabled = this.sourceMapsDisabled;
    var savedSourceMapGenerator = this.sourceMapGenerator;
    var savedSourceMapSource = this.sourceMapSource;
    var savedSourceMapSourceIndex = this.sourceMapSourceIndex;
    var savedSourceMapLineCharCache = this.sourceMapLineCharCache;
    this.sourceMapsDisabled = (sourceMapGenerator == null);
    this.sourceMapGenerator = sourceMapGenerator;
    this.sourceMapSource = null;
    this.sourceMapSourceIndex = -1;
    this.sourceMapLineCharCache = null;
    this.setSourceFile(sourceFile);
    if (this.Options.OmitTrailingSemicolon)
    {
        writer = trailingSemicolonDeferringWriter.getTrailingSemicolonDeferringWriter(writer);
    }
    this.writer = writer;
    this.writer.Clear();
    if ((sourceFile != null))
    {
        {
            var grower = this.writer as GrowableEmitTextWriter; var ok = grower != null;
            if (ok)
            {
                grower.Grow(GoRuntime.Len(sourceFile.TextView()));
            }
        }
    }
    switch (node.Kind)
    {
        case Kind.TemplateHead:
        {
            this.emitTemplateHead(node.AsTemplateHead());
            break;
        }
        case Kind.TemplateMiddle:
        {
            this.emitTemplateMiddle(node.AsTemplateMiddle());
            break;
        }
        case Kind.TemplateTail:
        {
            this.emitTemplateTail(node.AsTemplateTail());
            break;
        }
        case Kind.Identifier:
        {
            this.emitIdentifierName(node.AsIdentifier());
            break;
        }
        case Kind.PrivateIdentifier:
        {
            this.emitPrivateIdentifier(node.AsPrivateIdentifier());
            break;
        }
        case Kind.QualifiedName:
        {
            this.emitQualifiedName(node.AsQualifiedName());
            break;
        }
        case Kind.ComputedPropertyName:
        {
            this.emitComputedPropertyName(node.AsComputedPropertyName());
            break;
        }
        case Kind.TypeParameter:
        {
            this.emitTypeParameter(node.AsTypeParameterDeclaration());
            break;
        }
        case Kind.Parameter:
        {
            this.emitParameter(node.AsParameterDeclaration());
            break;
        }
        case Kind.Decorator:
        {
            this.emitDecorator(node.AsDecorator());
            break;
        }
        case Kind.PropertySignature:
        {
            this.emitPropertySignature(node.AsPropertySignatureDeclaration());
            break;
        }
        case Kind.PropertyDeclaration:
        {
            this.emitPropertyDeclaration(node.AsPropertyDeclaration());
            break;
        }
        case Kind.MethodSignature:
        {
            this.emitMethodSignature(node.AsMethodSignatureDeclaration());
            break;
        }
        case Kind.MethodDeclaration:
        {
            this.emitMethodDeclaration(node.AsMethodDeclaration());
            break;
        }
        case Kind.ClassStaticBlockDeclaration:
        {
            this.emitClassStaticBlockDeclaration(node.AsClassStaticBlockDeclaration());
            break;
        }
        case Kind.Constructor:
        {
            this.emitConstructor(node.AsConstructorDeclaration());
            break;
        }
        case Kind.GetAccessor:
        {
            this.emitGetAccessorDeclaration(node.AsGetAccessorDeclaration());
            break;
        }
        case Kind.SetAccessor:
        {
            this.emitSetAccessorDeclaration(node.AsSetAccessorDeclaration());
            break;
        }
        case Kind.CallSignature:
        {
            this.emitCallSignature(node.AsCallSignatureDeclaration());
            break;
        }
        case Kind.ConstructSignature:
        {
            this.emitConstructSignature(node.AsConstructSignatureDeclaration());
            break;
        }
        case Kind.IndexSignature:
        {
            this.emitIndexSignature(node.AsIndexSignatureDeclaration());
            break;
        }
        case Kind.ObjectBindingPattern:
        {
            this.emitObjectBindingPattern(node.AsBindingPattern());
            break;
        }
        case Kind.ArrayBindingPattern:
        {
            this.emitArrayBindingPattern(node.AsBindingPattern());
            break;
        }
        case Kind.BindingElement:
        {
            this.emitBindingElement(node.AsBindingElement());
            break;
        }
        case Kind.TemplateSpan:
        {
            this.emitTemplateSpan(node.AsTemplateSpan());
            break;
        }
        case Kind.SemicolonClassElement:
        {
            this.emitSemicolonClassElement(node.AsSemicolonClassElement());
            break;
        }
        case Kind.VariableDeclaration:
        {
            this.emitVariableDeclaration(node.AsVariableDeclaration());
            break;
        }
        case Kind.VariableDeclarationList:
        {
            this.emitVariableDeclarationList(node.AsVariableDeclarationList());
            break;
        }
        case Kind.ModuleBlock:
        {
            this.emitModuleBlock(node.AsModuleBlock());
            break;
        }
        case Kind.CaseBlock:
        {
            this.emitCaseBlock(node.AsCaseBlock());
            break;
        }
        case Kind.ImportClause:
        {
            this.emitImportClause(node.AsImportClause());
            break;
        }
        case Kind.NamespaceImport:
        {
            this.emitNamespaceImport(node.AsNamespaceImport());
            break;
        }
        case Kind.NamespaceExport:
        {
            this.emitNamespaceExport(node.AsNamespaceExport());
            break;
        }
        case Kind.NamedImports:
        {
            this.emitNamedImports(node.AsNamedImports());
            break;
        }
        case Kind.ImportSpecifier:
        {
            this.emitImportSpecifier(node.AsImportSpecifier());
            break;
        }
        case Kind.NamedExports:
        {
            this.emitNamedExports(node.AsNamedExports());
            break;
        }
        case Kind.ExportSpecifier:
        {
            this.emitExportSpecifier(node.AsExportSpecifier());
            break;
        }
        case Kind.ImportAttributes:
        {
            this.emitImportAttributes(node.AsImportAttributes());
            break;
        }
        case Kind.ImportAttribute:
        {
            this.emitImportAttribute(node.AsImportAttribute());
            break;
        }
        case Kind.ExternalModuleReference:
        {
            this.emitExternalModuleReference(node.AsExternalModuleReference());
            break;
        }
        case Kind.JsxText:
        {
            this.emitJsxText(node.AsJsxText());
            break;
        }
        case Kind.JsxOpeningElement:
        {
            this.emitJsxOpeningElement(node.AsJsxOpeningElement());
            break;
        }
        case Kind.JsxOpeningFragment:
        {
            this.emitJsxOpeningFragment(node.AsJsxOpeningFragment());
            break;
        }
        case Kind.JsxClosingElement:
        {
            this.emitJsxClosingElement(node.AsJsxClosingElement());
            break;
        }
        case Kind.JsxClosingFragment:
        {
            this.emitJsxClosingFragment(node.AsJsxClosingFragment());
            break;
        }
        case Kind.JsxAttribute:
        {
            this.emitJsxAttribute(node.AsJsxAttribute());
            break;
        }
        case Kind.JsxAttributes:
        {
            this.emitJsxAttributes(node.AsJsxAttributes());
            break;
        }
        case Kind.JsxSpreadAttribute:
        {
            this.emitJsxSpreadAttribute(node.AsJsxSpreadAttribute());
            break;
        }
        case Kind.JsxExpression:
        {
            this.emitJsxExpression(node.AsJsxExpression());
            break;
        }
        case Kind.JsxNamespacedName:
        {
            this.emitJsxNamespacedName(node.AsJsxNamespacedName());
            break;
        }
        case Kind.CaseClause:
        {
            this.emitCaseClause(node.AsCaseOrDefaultClause());
            break;
        }
        case Kind.DefaultClause:
        {
            this.emitDefaultClause(node.AsCaseOrDefaultClause());
            break;
        }
        case Kind.HeritageClause:
        {
            this.emitHeritageClause(node.AsHeritageClause());
            break;
        }
        case Kind.CatchClause:
        {
            this.emitCatchClause(node.AsCatchClause());
            break;
        }
        case Kind.PropertyAssignment:
        {
            this.emitPropertyAssignment(node.AsPropertyAssignment());
            break;
        }
        case Kind.ShorthandPropertyAssignment:
        {
            this.emitShorthandPropertyAssignment(node.AsShorthandPropertyAssignment());
            break;
        }
        case Kind.SpreadAssignment:
        {
            this.emitSpreadAssignment(node.AsSpreadAssignment());
            break;
        }
        case Kind.EnumMember:
        {
            this.emitEnumMember(node.AsEnumMember());
            break;
        }
        case Kind.SourceFile:
        {
            this.emitSourceFile(node.AsSourceFile());
            break;
        }
        case Kind.NotEmittedTypeElement:
        {
            this.emitNotEmittedTypeElement(node.AsNotEmittedTypeElement());
            break;
        }
        default:
        {
            switch (true)
            {
                case true when Ast.IsTypeNode(node):
                {
                    this.emitTypeNodeOutsideExtends(node);
                    break;
                }
                case true when Ast.IsStatement(node):
                {
                    this.emitStatement(node);
                    break;
                }
                case true when Ast.IsExpression(node):
                {
                    this.emitExpression(node, OperatorPrecedence.Lowest);
                    break;
                }
                case true when Ast.IsKeywordKind(node.Kind):
                {
                    this.emitKeywordNode(node);
                    break;
                }
                case true when Ast.IsPunctuationKind(node.Kind):
                {
                    this.emitPunctuationNode(node);
                    break;
                }
                case true when Ast.IsJSDocKind(node.Kind):
                {
                    this.emitJSDocNode(node);
                    break;
                }
                default:
                {
                    throw new InvalidOperationException(Fmt.Sprintf("unhandled Node: %v", node.Kind));
                }
            }
            break;
        }
    }
    this.currentSourceFile = savedCurrentSourceFile;
    this.writer = savedWriter;
    this.uniqueHelperNames = savedUniqueHelperNames;
    this.sourceMapsDisabled = savedSourceMapsDisabled;
    this.sourceMapGenerator = savedSourceMapGenerator;
    this.sourceMapSource = savedSourceMapSource;
    this.sourceMapSourceIndex = savedSourceMapSourceIndex;
    this.sourceMapLineCharCache = savedSourceMapLineCharCache;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5291:1.
private commentState? emitCommentsBeforeNode(Node node)
{
    if (!this.shouldEmitComments(node))
    {
        return null;
    }
    var emitFlags = this.emitContext.EmitFlags(node);
    var commentRange = this.emitContext.CommentRange(node);
    var containerPos = this.containerPos;
    var containerEnd = this.containerEnd;
    var declarationListContainerEnd = this.declarationListContainerEnd;
    this.emitLeadingCommentsOfNode(node, emitFlags, commentRange);
    this.emitLeadingSyntheticCommentsOfNode(node, emitFlags);
    if (((emitFlags & EmitFlags.NoNestedComments) != 0))
    {
        this.commentsDisabled = true;
    }
    return new commentState(emitFlags, commentRange, containerPos, containerEnd, declarationListContainerEnd);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5314:1.
private void emitCommentsAfterNode(Node node, commentState? state)
{
    if ((state == null))
    {
        return ;
    }
    var emitFlags = state.Value.emitFlags;
    var commentRange = state.Value.commentRange;
    var containerPos = state.Value.containerPos;
    var containerEnd = state.Value.containerEnd;
    var declarationListContainerEnd = state.Value.declarationListContainerEnd;
    if (((emitFlags & EmitFlags.NoNestedComments) != 0))
    {
        this.commentsDisabled = false;
    }
    this.emitTrailingSyntheticCommentsOfNode(node, emitFlags);
    this.emitTrailingCommentsOfNode(node, emitFlags, commentRange, containerPos, containerEnd, declarationListContainerEnd);
    {
        var typeNode = this.emitContext.GetTypeNode(node);
        if ((typeNode != null))
        {
            this.emitTrailingCommentsOfNode(node, emitFlags, typeNode.Loc, containerPos, containerEnd, declarationListContainerEnd);
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5339:1.
private (commentState?, int) emitCommentsBeforeToken(Kind token, int pos, Node contextNode, tokenEmitFlags flags)
{
    if ((((flags & tokenEmitFlags.NoComments) != 0) || this.commentsDisabled))
    {
        if (((this.currentSourceFile != null) && !Ast.PositionIsSynthesized(pos)))
        {
            pos = ByteScanner.SkipTrivia(this.currentSourceFile.TextView(), pos);
        }
        return (null, pos);
    }
    var startPos = pos;
    if ((this.currentSourceFile != null))
    {
        pos = ByteScanner.SkipTrivia(this.currentSourceFile.TextView(), startPos);
    }
    var node = this.emitContext.ParseNode(contextNode);
    var isSimilarNode = ((node != null) && (node.Kind == contextNode.Kind));
    if (!isSimilarNode)
    {
        return (null, pos);
    }
    if ((contextNode.Pos() != startPos))
    {
        var indentLeading = ((flags & tokenEmitFlags.IndentLeadingComments) != 0);
        var needsIndent = ((indentLeading && (this.currentSourceFile != null)) && !PositionsAreOnSameLine(startPos, pos, this.currentSourceFile));
        this.increaseIndentIf(needsIndent);
        this.emitLeadingComments(startPos, false);
        this.decreaseIndentIf(needsIndent);
    }
    return (new commentState(), pos);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5371:1.
private void emitCommentsAfterToken(Kind token, int pos, Node contextNode, commentState? state)
{
    if ((state == null))
    {
        return ;
    }
    if ((contextNode.End() != pos))
    {
        var isJsxExprContext = (contextNode.Kind == Kind.JsxExpression);
        this.emitTrailingComments(pos, Core.IfElse(isJsxExprContext, commentSeparator.None, commentSeparator.Before));
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5382:1.
private commentState? emitDetachedCommentsBeforeStatementList(Node node, TextRange detachedRange)
{
    if (!this.shouldEmitDetachedComments(node))
    {
        return null;
    }
    var emitFlags = this.emitContext.EmitFlags(node);
    var containerPos = this.containerPos;
    var containerEnd = this.containerEnd;
    var declarationListContainerEnd = this.declarationListContainerEnd;
    var skipLeadingComments = (Ast.PositionIsSynthesized(detachedRange.Pos()) || ((emitFlags & EmitFlags.NoLeadingComments) != 0));
    if (!skipLeadingComments)
    {
        this.emitDetachedCommentsAndUpdateCommentsInfo(detachedRange);
    }
    if (((emitFlags & EmitFlags.NoNestedComments) != 0))
    {
        this.commentsDisabled = true;
    }
    return new commentState(emitFlags, detachedRange, containerPos, containerEnd, declarationListContainerEnd);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5404:1.
private void emitDetachedCommentsAfterStatementList(Node node, TextRange detachedRange, commentState? state)
{
    if ((state == null))
    {
        return ;
    }
    var emitFlags = state.Value.emitFlags;
    var skipTrailingComments = ((this.commentsDisabled || Ast.PositionIsSynthesized(detachedRange.End())) || ((emitFlags & EmitFlags.NoTrailingComments) != 0));
    if (!skipTrailingComments)
    {
        var hasWrittenComment = this.emitLeadingComments(detachedRange.End(), false);
        if ((hasWrittenComment && !this.writer.IsAtStartOfLine()))
        {
            this.writeLine();
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5420:1.
private void emitLeadingCommentsOfNode(Node node, EmitFlags emitFlags, TextRange commentRange)
{
    var pos = commentRange.Pos();
    var end = commentRange.End();
    if ((((!Ast.PositionIsSynthesized(pos) || !Ast.PositionIsSynthesized(end))) && (pos != end)))
    {
        var skipLeadingComments = ((Ast.PositionIsSynthesized(pos) || ((emitFlags & EmitFlags.NoLeadingComments) != 0)) || (node.Kind == Kind.JsxText));
        var skipTrailingComments = ((Ast.PositionIsSynthesized(end) || ((emitFlags & EmitFlags.NoTrailingComments) != 0)) || (node.Kind == Kind.JsxText));
        if (!skipLeadingComments)
        {
            this.emitLeadingComments(pos, (node.Kind == Kind.NotEmittedStatement));
        }
        if ((!skipLeadingComments || (((pos >= 0) && (((emitFlags & EmitFlags.NoLeadingComments)) != 0)))))
        {
            this.containerPos = pos;
        }
        if ((!skipTrailingComments || (((end >= 0) && (((emitFlags & EmitFlags.NoTrailingComments)) != 0)))))
        {
            this.containerEnd = end;
            if ((node.Kind == Kind.VariableDeclarationList))
            {
                this.declarationListContainerEnd = end;
            }
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5455:1.
private void emitTrailingCommentsOfNode(Node node, EmitFlags emitFlags, TextRange commentRange, int containerPos, int containerEnd, int declarationListContainerEnd)
{
    var pos = commentRange.Pos();
    var end = commentRange.End();
    var skipTrailingComments = (((end < 0) || (((emitFlags & EmitFlags.NoTrailingComments)) != 0)) || (node.Kind == Kind.JsxText));
    if ((((!Ast.PositionIsSynthesized(pos) || !Ast.PositionIsSynthesized(end))) && (pos != end)))
    {
        this.containerPos = containerPos;
        this.containerEnd = containerEnd;
        this.declarationListContainerEnd = declarationListContainerEnd;
        if ((!skipTrailingComments && (node.Kind != Kind.NotEmittedStatement)))
        {
            this.emitTrailingComments(end, commentSeparator.Before);
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5473:1.
private void emitLeadingSyntheticCommentsOfNode(Node node, EmitFlags emitFlags)
{
    if (((emitFlags & EmitFlags.NoLeadingComments) != 0))
    {
        return ;
    }
    var synth = this.emitContext.GetSyntheticLeadingComments(node);
    {
        foreach (var c in GoRuntime.RangeValues(synth))
        {
            this.emitLeadingSynthesizedComment(c);
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5483:1.
private void emitLeadingSynthesizedComment(SynthesizedComment comment)
{
    if ((comment.HasLeadingNewLine || (comment.Kind == Kind.SingleLineCommentTrivia)))
    {
        this.writer.WriteLine();
    }
    this.writeSynthesizedComment(comment);
    if ((comment.HasTrailingNewLine || (comment.Kind == Kind.SingleLineCommentTrivia)))
    {
        this.writer.WriteLine();
    }
    else
    {
        this.writer.WriteSpace(" ");
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5495:1.
private void emitTrailingSyntheticCommentsOfNode(Node node, EmitFlags emitFlags)
{
    if (((emitFlags & EmitFlags.NoTrailingComments) != 0))
    {
        return ;
    }
    var synth = this.emitContext.GetSyntheticTrailingComments(node);
    {
        foreach (var c in GoRuntime.RangeValues(synth))
        {
            this.emitTrailingSynthesizedComment(c);
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5505:1.
private void emitTrailingSynthesizedComment(SynthesizedComment comment)
{
    if (!this.writer.IsAtStartOfLine())
    {
        this.writer.WriteSpace(" ");
    }
    this.writeSynthesizedComment(comment);
    if (comment.HasTrailingNewLine)
    {
        this.writer.WriteLine();
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5515:1.
private static string formatSynthesizedComment(SynthesizedComment comment)
{
    if ((comment.Kind == Kind.MultiLineCommentTrivia))
    {
        return (("/*" + comment.Text) + "*/");
    }
    return ("//" + comment.Text);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5522:1.
private void writeSynthesizedComment(SynthesizedComment comment)
{
    var text = formatSynthesizedComment(comment);
    Slice<int> lineMap = default;
    if ((comment.Kind == Kind.MultiLineCommentTrivia))
    {
        lineMap = Core.ComputeECMALineStarts(text);
    }
    this.writeCommentRangeWorker(text, lineMap, comment.Kind, Core.NewTextRange(0, GoRuntime.Len(text)));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5531:1.
private bool emitLeadingComments(int pos, bool elided)
{
    if ((((this.commentsDisabled || (this.currentSourceFile == null)) || Ast.PositionIsSynthesized(pos)) || (pos == this.containerPos)))
    {
        return false;
    }
    var tripleSlash = Tristate.Unknown;
    if (!elided)
    {
        if ((((pos == 0) && (this.currentSourceFile != null)) && this.currentSourceFile.IsDeclarationFile))
        {
            tripleSlash = Tristate.False;
        }
    }
    else
    {
        if ((pos == 0))
        {
            tripleSlash = Tristate.True;
        }
        else
        {
            return false;
        }
    }
    if ((this.detachedCommentsInfo.Count > 0))
    {
        {
            var info = this.detachedCommentsInfo.Peek();
            if ((info.nodePos == pos))
            {
                pos = this.detachedCommentsInfo.Pop().detachedCommentEndPos;
            }
        }
    }
    Slice<CommentRange> comments = default;
    {
        foreach (var comment in GoRuntime.RangeKeys(ByteScanner.GetLeadingCommentRanges(this.emitContext.Factory, this.currentSourceFile.TextView(), pos)))
        {
            if ((this.shouldWriteComment(comment) && this.shouldEmitCommentIfTripleSlash(comment, tripleSlash)))
            {
                comments = comments.Append(comment);
            }
        }
    }
    if (((GoRuntime.Len(comments) > 0) && this.shouldEmitNewLineBeforeLeadingCommentOfPosition(pos, comments[0].Pos)))
    {
        this.writeLine();
    }
    return this.emitComments(comments, commentSeparator.After);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5578:1.
private bool shouldEmitCommentIfTripleSlash(CommentRange comment, Tristate tripleSlash)
{
    switch (tripleSlash)
    {
        case Tristate.True:
        {
            return this.isTripleSlashComment(comment);
        }
        case Tristate.False:
        {
            return !this.isTripleSlashComment(comment);
        }
        default:
        {
            return true;
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5589:1.
private bool shouldEmitNewLineBeforeLeadingCommentOfPosition(int pos, int commentPos)
{
    return (((this.currentSourceFile != null) && (pos != commentPos)) && (ByteScanner.ComputeLineOfPosition(this.currentSourceFile.ECMALineMap().AsSpan(), pos) != ByteScanner.ComputeLineOfPosition(this.currentSourceFile.ECMALineMap().AsSpan(), commentPos)));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5596:1.
private void emitLeadingCommentsOfPosition(int pos)
{
    if ((this.commentsDisabled || (pos == -1)))
    {
        return ;
    }
    this.emitLeadingComments(pos, false);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5604:1.
private void emitTrailingComments(int pos, commentSeparator commentSeparator)
{
    if (this.commentsDisabled)
    {
        return ;
    }
    if (((this.commentsDisabled || (this.currentSourceFile == null)) || ((this.containerEnd != -1) && (((pos == this.containerEnd) || (pos == this.declarationListContainerEnd))))))
    {
        return ;
    }
    Slice<CommentRange> comments = default;
    {
        foreach (var comment in GoRuntime.RangeKeys(ByteScanner.GetTrailingCommentRanges(this.emitContext.Factory, this.currentSourceFile.TextView(), pos)))
        {
            if (this.shouldWriteComment(comment))
            {
                comments = comments.Append(comment);
            }
        }
    }
    this.emitComments(comments, commentSeparator);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5624:1.
private void emitTrailingCommentsOfPosition(int pos, bool prefixSpace, bool forceNoNewline)
{
    if ((this.commentsDisabled || (this.currentSourceFile == null)))
    {
        return ;
    }
    if (((this.containerEnd != -1) && (((pos == this.containerEnd) || (pos == this.declarationListContainerEnd)))))
    {
        return ;
    }
    Slice<CommentRange> comments = default;
    {
        foreach (var comment in GoRuntime.RangeKeys(ByteScanner.GetTrailingCommentRanges(this.emitContext.Factory, this.currentSourceFile.TextView(), pos)))
        {
            comments = comments.Append(comment);
        }
    }
    if ((GoRuntime.Len(comments) == 0))
    {
        return ;
    }
    {
        foreach (var comment2 in GoRuntime.RangeValues(comments))
        {
            if (prefixSpace)
            {
                if (!this.shouldWriteComment(comment2))
                {
                    continue;
                }
                if (!this.writer.IsAtStartOfLine())
                {
                    this.writeSpace();
                }
                this.emitComment(comment2);
                if (comment2.HasTrailingNewLine)
                {
                    this.writeLine();
                }
                continue;
            }
            this.emitComment(comment2);
            switch (true)
            {
                case true when forceNoNewline:
                {
                    if ((comment2.Kind == Kind.SingleLineCommentTrivia))
                    {
                        this.writeLine();
                    }
                    break;
                }
                case true when comment2.HasTrailingNewLine:
                {
                    this.writeLine();
                    break;
                }
                default:
                {
                    this.writeSpace();
                    break;
                }
            }
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5669:1.
private void emitDetachedCommentsAndUpdateCommentsInfo(TextRange textRange)
{
    if ((this.currentSourceFile == null))
    {
        return ;
    }
    {
        var tuple24 = this.emitDetachedComments(textRange); var currentDetachedCommentInfo = tuple24.Item1; var ok = tuple24.Item2;
        if (ok)
        {
            this.detachedCommentsInfo.Push(currentDetachedCommentInfo);
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5678:1.
private (detachedCommentsInfo, bool) emitDetachedComments(TextRange textRange)
{
    detachedCommentsInfo result = default;
    bool hasResult = default;
    if ((this.currentSourceFile == null))
    {
        return (result, hasResult);
    }
    var text = this.currentSourceFile.TextView();
    var lineMap = this.currentSourceFile.ECMALineMap();
    Slice<CommentRange> leadingComments = default;
    if (this.commentsDisabled)
    {
        if ((textRange.Pos() == 0))
        {
            {
                foreach (var comment in GoRuntime.RangeKeys(ByteScanner.GetLeadingCommentRanges(this.emitContext.Factory, text, textRange.Pos())))
                {
                    if (IsPinnedComment(text, comment))
                    {
                        leadingComments = leadingComments.Append(comment);
                    }
                }
            }
        }
    }
    else
    {
        leadingComments = collect(ByteScanner.GetLeadingCommentRanges(this.emitContext.Factory, text, textRange.Pos()));
    }
    if ((GoRuntime.Len(leadingComments) > 0))
    {
        Slice<CommentRange> detachedComments = default;
        CommentRange lastComment = default;
        {
            foreach (var range26 in GoRuntime.Range(leadingComments))
            {
                var i = range26.Key;
                var comment2 = range26.Value;
                if ((i > 0))
                {
                    var lastCommentLine = ByteScanner.ComputeLineOfPosition(lineMap.AsSpan(), lastComment.End);
                    var commentLine = ByteScanner.ComputeLineOfPosition(lineMap.AsSpan(), comment2.Pos);
                    if ((commentLine >= (lastCommentLine + 2)))
                    {
                        break;
                    }
                }
                detachedComments = detachedComments.Append(comment2);
                lastComment = comment2;
            }
        }
        if ((GoRuntime.Len(detachedComments) > 0))
        {
            var lastCommentLine2 = ByteScanner.ComputeLineOfPosition(lineMap.AsSpan(), Core.LastOrNil(detachedComments).End);
            var nodeLine = ByteScanner.ComputeLineOfPosition(lineMap.AsSpan(), ByteScanner.SkipTrivia(text, textRange.Pos()));
            if ((nodeLine >= (lastCommentLine2 + 2)))
            {
                Slice<CommentRange> commentsToEmit = default;
                {
                    foreach (var comment3 in GoRuntime.RangeValues(detachedComments))
                    {
                        if (this.shouldWriteComment(comment3))
                        {
                            commentsToEmit = commentsToEmit.Append(comment3);
                        }
                    }
                }
                if ((GoRuntime.Len(commentsToEmit) > 0))
                {
                    if (this.shouldEmitNewLineBeforeLeadingCommentOfPosition(textRange.Pos(), commentsToEmit[0].Pos))
                    {
                        this.writeLine();
                    }
                    this.emitComments(commentsToEmit, commentSeparator.After);
                }
                result = new detachedCommentsInfo { nodePos = textRange.Pos(), detachedCommentEndPos = Core.LastOrNil(detachedComments).End };
                hasResult = true;
            }
        }
    }
    return (result, hasResult);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5765:1.
private bool emitComments(Slice<CommentRange> comments, commentSeparator commentSeparator)
{
    var interveningSeparator = false;
    if ((GoRuntime.Len(comments) == 0))
    {
        return false;
    }
    if ((commentSeparator == commentSeparator.Before))
    {
        this.writeSpace();
    }
    {
        foreach (var comment in GoRuntime.RangeValues(comments))
        {
            if (interveningSeparator)
            {
                this.writeSpace();
                interveningSeparator = false;
            }
            this.emitComment(comment);
            if (((comment.Kind == Kind.SingleLineCommentTrivia) || (comment.HasTrailingNewLine && (commentSeparator != commentSeparator.None))))
            {
                this.writeLine();
            }
            else
            {
                interveningSeparator = (commentSeparator != commentSeparator.None);
            }
        }
    }
    if ((interveningSeparator && (commentSeparator == commentSeparator.After)))
    {
        this.writeSpace();
    }
    return true;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5797:1.
private void emitComment(CommentRange comment)
{
    this.emitPos(comment.Pos);
    this.writeCommentRange(comment);
    this.emitPos(comment.End);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5803:1.
private bool isTripleSlashComment(CommentRange comment)
{
    return ((this.currentSourceFile != null) && IsRecognizedTripleSlashComment(this.currentSourceFile.TextView(), comment));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5812:1.
private void setSourceMapSource(Source source)
{
    if (this.sourceMapsDisabled)
    {
        return ;
    }
    this.sourceMapSource = source;
    this.sourceMapLineCharCache = newLineCharacterCache(source);
    if ((this.mostRecentSourceMapSource == source))
    {
        this.sourceMapSourceIndex = this.mostRecentSourceMapSourceIndex;
        return ;
    }
    this.sourceMapSourceIsJson = source.FileName().ExtensionIs(TsPath.ExtensionJson);
    if (this.sourceMapSourceIsJson)
    {
        return ;
    }
    this.sourceMapSourceIndex = this.sourceMapGenerator.AddSource(source.FileName());
    if (this.Options.InlineSources)
    {
        {
            var err = this.sourceMapGenerator.SetSourceContent(this.sourceMapSourceIndex, ByteStringValue.FromView(source.Text()));
            if ((err != null))
            {
                throw err;
            }
        }
    }
    this.mostRecentSourceMapSource = source;
    this.mostRecentSourceMapSourceIndex = this.sourceMapSourceIndex;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5840:1.
private void emitPos(int pos)
{
    if (((((this.sourceMapsDisabled || (this.sourceMapSource == null)) || (this.sourceMapGenerator == null)) || this.sourceMapSourceIsJson) || Ast.PositionIsSynthesized(pos)))
    {
        return ;
    }
    var source = this.sourceMapSource;
    var sourceIndex = this.sourceMapSourceIndex;
    var lineCharCache = this.sourceMapLineCharCache;
    if ((this.MapSourcePosition != null))
    {
        var tuple29 = this.MapSourcePosition(source, pos); var mappedSource = tuple29.Item1; var mappedPos = tuple29.Item2; var ok = tuple29.Item3;
        if (!ok)
        {
            {
                var err = this.sourceMapGenerator.AddGeneratedMapping(this.writer.GetLine(), this.writer.GetColumn());
                if ((err != null))
                {
                    throw err;
                }
            }
            return ;
        }
        pos = mappedPos;
        if ((mappedSource != source))
        {
            var savedSource = this.sourceMapSource;
            var savedSourceIndex = this.sourceMapSourceIndex;
            var savedSourceIsJson = this.sourceMapSourceIsJson;
            var savedLineCharCache = this.sourceMapLineCharCache;
            this.setSourceMapSource(mappedSource);
            sourceIndex = this.sourceMapSourceIndex;
            lineCharCache = this.sourceMapLineCharCache;
            this.sourceMapSource = savedSource;
            this.sourceMapSourceIndex = savedSourceIndex;
            this.sourceMapSourceIsJson = savedSourceIsJson;
            this.sourceMapLineCharCache = savedLineCharCache;
        }
    }
    var tuple30 = lineCharCache.getLineAndCharacter(pos); var sourceLine = tuple30.Item1; var sourceCharacter = tuple30.Item2;
    {
        var err2 = this.sourceMapGenerator.AddSourceMapping(this.writer.GetLine(), this.writer.GetColumn(), sourceIndex, sourceLine, sourceCharacter);
        if ((err2 != null))
        {
            throw err2;
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5904:1.
private void emitSourcePos(Source source, int pos)
{
    if ((source != this.sourceMapSource))
    {
        var savedSourceMapSource = this.sourceMapSource;
        var savedSourceMapSourceIndex = this.sourceMapSourceIndex;
        var savedSourceMapLineCharCache = this.sourceMapLineCharCache;
        this.setSourceMapSource(source);
        this.emitPos(pos);
        this.sourceMapSource = savedSourceMapSource;
        this.sourceMapSourceIndex = savedSourceMapSourceIndex;
        this.sourceMapLineCharCache = savedSourceMapLineCharCache;
    }
    else
    {
        this.emitPos(pos);
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5933:1.
private sourceMapState? emitSourceMapsBeforeNode(Node node)
{
    if (!this.shouldEmitSourceMaps(node))
    {
        return null;
    }
    var emitFlags = this.emitContext.EmitFlags(node);
    var loc = this.emitContext.SourceMapRange(node);
    if ((((!Ast.IsNotEmittedStatement(node) && ((emitFlags & EmitFlags.NoLeadingSourceMap) == 0)) && (this.currentSourceFile != null)) && !Ast.PositionIsSynthesized(loc.Pos())))
    {
        this.emitSourcePos(this.sourceMapSource, ByteScanner.SkipTrivia(this.currentSourceFile.TextView(), loc.Pos()));
    }
    if (((emitFlags & EmitFlags.NoNestedSourceMaps) != 0))
    {
        this.sourceMapsDisabled = true;
    }
    return new sourceMapState(emitFlags, loc, false);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5957:1.
private void emitSourceMapsAfterNode(Node node, sourceMapState? previousState)
{
    if ((previousState == null))
    {
        return ;
    }
    var emitFlags = previousState.Value.emitFlags;
    var loc = previousState.Value.sourceMapRange;
    if (((emitFlags & EmitFlags.NoNestedSourceMaps) != 0))
    {
        this.sourceMapsDisabled = false;
    }
    if (((!Ast.IsNotEmittedStatement(node) && ((emitFlags & EmitFlags.NoTrailingSourceMap) == 0)) && !Ast.PositionIsSynthesized(loc.End())))
    {
        this.emitSourcePos(this.sourceMapSource, loc.End());
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5976:1.
private sourceMapState? emitSourceMapsBeforeToken(Kind token, int pos, Node contextNode, tokenEmitFlags flags)
{
    if (!this.shouldEmitTokenSourceMaps(token, pos, contextNode, flags))
    {
        return null;
    }
    var emitFlags = this.emitContext.EmitFlags(contextNode);
    var tuple31 = this.emitContext.TokenSourceMapRange(contextNode, token); var loc = tuple31.Item1; var hasLoc = tuple31.Item2;
    if (hasLoc)
    {
        pos = loc.Pos();
    }
    if (((pos >= 0) && (this.currentSourceFile != null)))
    {
        pos = ByteScanner.SkipTrivia(this.currentSourceFile.TextView(), pos);
    }
    if ((((emitFlags & EmitFlags.NoTokenLeadingSourceMaps) == 0) && (pos >= 0)))
    {
        this.emitSourcePos(this.sourceMapSource, pos);
    }
    return new sourceMapState(emitFlags, loc, hasLoc);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:5998:1.
private void emitSourceMapsAfterToken(Kind token, int pos, Node contextNode, sourceMapState? previousState)
{
    if ((previousState == null))
    {
        return ;
    }
    var emitFlags = previousState.Value.emitFlags;
    var loc = previousState.Value.sourceMapRange;
    var hasLoc = previousState.Value.hasTokenSourceMapRange;
    if (((emitFlags & EmitFlags.NoTokenTrailingSourceMaps) == 0))
    {
        if (hasLoc)
        {
            pos = loc.End();
        }
        if ((pos >= 0))
        {
            this.emitSourcePos(this.sourceMapSource, pos);
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6020:1.
private bool shouldReuseTempVariableScope(Node node)
{
    return ((node != null) && ((this.emitContext.EmitFlags(node) & EmitFlags.ReuseTempVariableScope) != 0));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6024:1.
private void pushNameGenerationScope(Node node)
{
    this.nameGenerator.PushScope(this.shouldReuseTempVariableScope(node));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6028:1.
private void popNameGenerationScope(Node node)
{
    this.nameGenerator.PopScope(this.shouldReuseTempVariableScope(node));
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6032:1.
private void generateAllNames(NodeList nodes)
{
    if ((nodes == null))
    {
        return ;
    }
    {
        foreach (var node in GoRuntime.RangeValues(nodes.Nodes))
        {
            this.generateNames(node);
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6041:1.
private void generateNames(Node node)
{
    if ((node == null))
    {
        return ;
    }
    switch (node.Kind)
    {
        case Kind.Block:
        case Kind.CaseClause:
        case Kind.DefaultClause:
        {
            this.generateAllNames(node.StatementList());
            break;
        }
        case Kind.LabeledStatement:
        case Kind.WithStatement:
        case Kind.DoStatement:
        case Kind.WhileStatement:
        {
            this.generateNames(node.Statement());
            break;
        }
        case Kind.IfStatement:
        {
            this.generateNames(node.AsIfStatement().ThenStatement);
            this.generateNames(node.AsIfStatement().ElseStatement);
            break;
        }
        case Kind.ForStatement:
        case Kind.ForOfStatement:
        case Kind.ForInStatement:
        {
            this.generateNames(node.Initializer());
            this.generateNames(node.Statement());
            break;
        }
        case Kind.SwitchStatement:
        {
            this.generateNames(node.AsSwitchStatement().CaseBlock);
            break;
        }
        case Kind.CaseBlock:
        {
            this.generateAllNames(node.AsCaseBlock().Clauses);
            break;
        }
        case Kind.TryStatement:
        {
            this.generateNames(node.AsTryStatement().TryBlock);
            this.generateNames(node.AsTryStatement().CatchClause);
            this.generateNames(node.AsTryStatement().FinallyBlock);
            break;
        }
        case Kind.CatchClause:
        {
            this.generateNames(node.AsCatchClause().VariableDeclaration);
            this.generateNames(node.AsCatchClause().Block);
            break;
        }
        case Kind.VariableStatement:
        {
            this.generateNames(node.AsVariableStatement().DeclarationList);
            break;
        }
        case Kind.VariableDeclarationList:
        {
            this.generateAllNames(node.AsVariableDeclarationList().Declarations);
            break;
        }
        case Kind.VariableDeclaration:
        case Kind.Parameter:
        case Kind.BindingElement:
        case Kind.ClassDeclaration:
        {
            this.generateNameIfNeeded(node.Name());
            break;
        }
        case Kind.FunctionDeclaration:
        {
            this.generateNameIfNeeded(node.Name());
            if (this.shouldReuseTempVariableScope(node))
            {
                this.generateAllNames(node.AsFunctionDeclaration().Parameters);
                this.generateNames(node.AsFunctionDeclaration().Body);
            }
            break;
        }
        case Kind.ObjectBindingPattern:
        case Kind.ArrayBindingPattern:
        {
            this.generateAllNames(node.ElementList());
            break;
        }
        case Kind.ImportDeclaration:
        case Kind.JSImportDeclaration:
        {
            this.generateNames(node.AsImportDeclaration().ImportClause);
            break;
        }
        case Kind.ImportClause:
        {
            this.generateNameIfNeeded(node.AsImportClause().Name());
            this.generateNames(node.AsImportClause().NamedBindings);
            break;
        }
        case Kind.NamespaceImport:
        case Kind.NamespaceExport:
        {
            this.generateNameIfNeeded(node.Name());
            break;
        }
        case Kind.NamedImports:
        {
            this.generateAllNames(node.ElementList());
            break;
        }
        case Kind.ImportSpecifier:
        {
            var n = node.AsImportSpecifier();
            if ((n.PropertyName != null))
            {
                this.generateNameIfNeeded(n.PropertyName);
            }
            else
            {
                this.generateNameIfNeeded(n.Name());
            }
            break;
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6101:1.
private void generateAllMemberNames(NodeList nodes)
{
    if ((nodes == null))
    {
        return ;
    }
    {
        foreach (var node in GoRuntime.RangeValues(nodes.Nodes))
        {
            this.generateMemberNames(node);
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6110:1.
private void generateMemberNames(Node node)
{
    if ((node == null))
    {
        return ;
    }
    switch (node.Kind)
    {
        case Kind.PropertyAssignment:
        case Kind.ShorthandPropertyAssignment:
        case Kind.PropertyDeclaration:
        case Kind.PropertySignature:
        case Kind.MethodDeclaration:
        case Kind.MethodSignature:
        case Kind.GetAccessor:
        case Kind.SetAccessor:
        {
            this.generateNameIfNeeded(node.Name());
            break;
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6127:1.
private void generateNameIfNeeded(Node name)
{
    if ((name != null))
    {
        if (Ast.IsMemberName(name))
        {
            this.generateName(name);
        }
        else
        {
            if (Ast.IsBindingPattern(name))
            {
                this.generateNames(name);
            }
        }
    }
}

// Generate the text for a generated identifier or private identifier
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6138:1.
private void generateName(Node name)
{
    _ = this.nameGenerator.GenerateName(name);
}

// Returns a value indicating whether a name is unique globally or within the current file.
// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6143:1.
private bool isFileLevelUniqueNameInCurrentFile(string name, bool _)
{
    if ((this.currentSourceFile != null))
    {
        return this.emitContext.IsFileLevelUniqueName(this.currentSourceFile, name, this.HasGlobalName);
    }
    else
    {
        return true;
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6155:1.
private printerState enterNode(Node node)
{
    var state = new printerState {  };
    if ((this.OnBeforeEmitNode != null))
    {
        this.OnBeforeEmitNode(node);
    }
    state.commentState = this.emitCommentsBeforeNode(node);
    state.sourceMapState = this.emitSourceMapsBeforeNode(node);
    return state;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6167:1.
private void exitNode(Node node, printerState previousState)
{
    this.emitSourceMapsAfterNode(node, previousState.sourceMapState);
    this.emitCommentsAfterNode(node, previousState.commentState);
    if ((this.OnAfterEmitNode != null))
    {
        this.OnAfterEmitNode(node);
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6176:1.
private printerState enterTokenNode(Node node, tokenEmitFlags flags)
{
    var state = new printerState {  };
    if ((this.OnBeforeEmitToken != null))
    {
        this.OnBeforeEmitToken(node);
    }
    if (((flags & tokenEmitFlags.NoComments) == 0))
    {
        state.commentState = this.emitCommentsBeforeNode(node);
    }
    if (((flags & tokenEmitFlags.NoSourceMaps) == 0))
    {
        state.sourceMapState = this.emitSourceMapsBeforeNode(node);
    }
    return state;
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6192:1.
private void exitTokenNode(Node node, printerState previousState)
{
    this.emitSourceMapsAfterNode(node, previousState.sourceMapState);
    this.emitCommentsAfterNode(node, previousState.commentState);
    if ((this.OnAfterEmitToken != null))
    {
        this.OnAfterEmitToken(node);
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6211:1.
private (printerState, int) enterToken(Kind token, int pos, Node contextNode, tokenEmitFlags flags)
{
    var state = new printerState {  };
    (state.commentState, pos) = this.emitCommentsBeforeToken(token, pos, contextNode, flags);
    state.sourceMapState = this.emitSourceMapsBeforeToken(token, pos, contextNode, flags);
    return (state, pos);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6218:1.
private void exitToken(Kind token, int pos, Node contextNode, printerState previousState)
{
    this.emitSourceMapsAfterToken(token, pos, contextNode, previousState.sourceMapState);
    this.emitCommentsAfterToken(token, pos, contextNode, previousState.commentState);
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6313:1.
private static string getOpeningBracket(ListFormat format)
{
    switch ((format & ListFormat.BracketsMask))
    {
        case ListFormat.Braces:
        {
            return "{";
        }
        case ListFormat.Parenthesis:
        {
            return "(";
        }
        case ListFormat.AngleBrackets:
        {
            return "<";
        }
        case ListFormat.SquareBrackets:
        {
            return "[";
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("Unexpected bracket: %v", (format & ListFormat.BracketsMask)));
        }
    }
}

// Upstream upstream/TypeScript/tsc/internal/printer/printer.go:6328:1.
private static string getClosingBracket(ListFormat format)
{
    switch ((format & ListFormat.BracketsMask))
    {
        case ListFormat.Braces:
        {
            return "}";
        }
        case ListFormat.Parenthesis:
        {
            return ")";
        }
        case ListFormat.AngleBrackets:
        {
            return ">";
        }
        case ListFormat.SquareBrackets:
        {
            return "]";
        }
        default:
        {
            throw new InvalidOperationException(Fmt.Sprintf("Unexpected bracket: %v", (format & ListFormat.BracketsMask)));
        }
    }
}

}

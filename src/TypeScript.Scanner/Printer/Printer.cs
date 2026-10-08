// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/printer/printer.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.DiagnosticPrinting;
internal sealed partial class Printer
{
    private readonly EmitContext emitContext;
    private readonly bool removeComments;
    internal Printer(EmitContext context, bool removeComments = true)
    {
        emitContext = context;
        this.removeComments = removeComments;
    }

    private TextWriter writer;
    private SourceFile currentSourceFile;
    // Write (5076): diagnostic data nodes and selected source reuse; source comments/maps excluded.
    internal void Write(Node node, TextWriter output, SourceFile sourceFile = null)
    {
        var savedWriter = writer;
        var savedSourceFile = currentSourceFile;
        currentSourceFile = sourceFile;
        writer = output;
        writer.Clear();
        if (node?.Kind == Kind.Identifier)
            emitIdentifierReference(node.AsIdentifier());
        else
            emitTypeNode(node);
        writer = savedWriter;
        currentSourceFile = savedSourceFile;
    }

    private void emitKeywordTypeNode(Node node) => emitKeywordNode(node);
    private void emitKeywordNode(Node node)
    {
        if (node == null)
            return;
        // emitKeywordNodeEx -> enterTokenNode -> emitCommentsBeforeNode.
        // Selected synthesized keyword nodes have no source-range/trailing comments.
        if (shouldEmitComments(node))
            emitLeadingSyntheticCommentsOfNode(node, emitContext.EmitFlags(node));
        writeTokenText(node.Kind);
    }

    // printer.go:742. RemoveComments=false alone does not make comments emit:
    // the original printer also requires a current source file.
    private bool shouldEmitComments(Node node) => !removeComments && currentSourceFile != null && !Ast.IsSourceFile(node);
    private void writeTokenText(Kind token) => writer.WriteKeyword(TypeScript.Scanner.Scanner.TokenToString(token));
    private void emitLiteralType(Node node) => emitExpression(node.AsLiteralTypeNode().Literal);
    private void emitExpression(Node node)
    {
        switch (node.Kind)
        {
            case Kind.StringLiteral:
            case Kind.NumericLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
                emitLiteral(node);
                return;
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NullKeyword:
                emitKeywordNode(node);
                return;
            case Kind.PrefixUnaryExpression:
                var unary = node.AsPrefixUnaryExpression();
                if (unary.Operator is not (Kind.MinusToken or Kind.PlusToken))
                    throw TypeScript.Checking.Checker.unsupported("Printer.emitExpression: unary operator");
                writer.Write(unary.Operator == Kind.MinusToken ? "-" : "+");
                emitExpression(unary.Operand);
                return;
            default:
                throw TypeScript.Checking.Checker.unsupported("Printer.emitExpression: unsupported synthesized node");
        }
    }

    private void emitLiteral(Node node) => writer.WriteStringLiteral(getLiteralText(node));
    // printer.go:1118/1156, getTextOfNode:230. Synthesized diagnostic identifiers
    // have no source-file/generated-helper substitution and carry EFNoAsciiEscaping.
    private void emitIdentifierReference(Identifier node) => emitIdentifierText(node);
    private void emitIdentifierText(Identifier node) => writer.Write(getTextOfNode(node.AsNode(), false));
    private string getTextOfNode(Node node, bool includeTrivia)
    {
        bool canUseSourceFile = currentSourceFile != null && node.Parent != null && !Ast.NodeIsSynthesized(node);
        if (node.Kind == Kind.Identifier)
        {
            if (!canUseSourceFile || Ast.GetSourceFileOfNode(node) != currentSourceFile)
                return node.Text();
        }
        else if (node.Kind is Kind.StringLiteral or Kind.NumericLiteral or Kind.NoSubstitutionTemplateLiteral)
            return getLiteralText(node);
        else
            throw TypeScript.Checking.Checker.unsupported("Printer.getTextOfNode: unselected name/literal");
        return TypeScript.Scanner.Scanner.GetSourceTextOfNodeFromSourceFileByteString(currentSourceFile, node, includeTrivia);
    }

    // printer.go:2298/5076. Selected synthesized type dispatch. The diagnostic
    // printer has RemoveComments/OmitTrailingSemicolon/NeverAsciiEscape options;
    // source-map, enter/exit emit bookkeeping and name generation remain excluded.
    private void emitTypeNode(Node node, TypePrecedence precedence = TypePrecedence.Lowest)
    {
        if (node == null)
            return;
        // Conditional extends/infer scopes are excluded by diagnostic admission.
        bool parens = Ast.GetTypeNodePrecedence(node) < precedence;
        if (parens)
            writePunctuation("(");
        emitTypeNodeWorker(node);
        if (parens)
            writePunctuation(")");
    }

    // printer.go:5473/5483/5515/5522. Selected single-line synthesized block
    // comments follow the original helper boundaries; source comments are excluded.
    private void emitLeadingSyntheticCommentsOfNode(Node node, EmitFlags emitFlags)
    {
        if ((emitFlags & EmitFlags.NoLeadingComments) != 0)
            return;
        foreach (var comment in emitContext.GetSyntheticLeadingComments(node) ?? [])
            emitLeadingSynthesizedComment(comment);
    }

    private void emitLeadingSynthesizedComment(SynthesizedComment comment)
    {
        if (comment.HasLeadingNewLine || comment.HasTrailingNewLine || comment.Kind == Kind.SingleLineCommentTrivia)
            throw TypeScript.Checking.Checker.unsupported("Printer.emitLeadingSynthesizedComment: multiline diagnostic output");
        writeSynthesizedComment(comment);
        writer.WriteSpace(" ");
    }

    private static string formatSynthesizedComment(SynthesizedComment comment) => comment.Kind == Kind.MultiLineCommentTrivia ? "/*" + comment.Text + "*/" : "//" + comment.Text;
    private void writeSynthesizedComment(SynthesizedComment comment)
    {
        var text = formatSynthesizedComment(comment);
        writeCommentRangeWorker(text, comment.Kind, new(0, text.Length));
    }

    private void writeCommentRangeWorker(string text, Kind kind, TextRange loc)
    {
        if (kind != Kind.MultiLineCommentTrivia || text.AsSpan(loc.Pos(), loc.End() - loc.Pos()).ContainsAny('\r', '\n'))
            throw TypeScript.Checking.Checker.unsupported("Printer.writeCommentRangeWorker: source/multiline comment");
        // Original single-line block-comment arm trims the current line before writing.
        writer.WriteComment(text[loc.Pos()..loc.End()].Trim());
    }

    // Authored factoring keeps return branches from bypassing closing punctuation.
    private void emitTypeNodeWorker(Node node)
    {
        if (Ast.IsKeywordTypeNode(node))
        {
            emitKeywordTypeNode(node);
            return;
        }

        switch (node.Kind)
        {
            case Kind.ParenthesizedType:
                writePunctuation("(");
                emitTypeNode(node.AsParenthesizedTypeNode().Type);
                writePunctuation(")");
                return;
            case Kind.LiteralType:
                emitLiteralType(node);
                return;
            case Kind.Identifier:
                emitIdentifierReference(node.AsIdentifier());
                return;
            case Kind.TypeLiteral:
                emitTypeLiteral(node.AsTypeLiteralNode());
                return;
            case Kind.UnionType:
                emitUnionType(node.AsUnionTypeNode());
                return;
            case Kind.IntersectionType:
                emitIntersectionType(node.AsIntersectionTypeNode());
                return;
            case Kind.ThisType:
                writer.Write("this");
                return;
            case Kind.TypeReference:
                var reference = node.AsTypeReferenceNode();
                if (reference.TypeArguments?.Nodes is { Count: > 0 })
                    throw TypeScript.Checking.Checker.unsupported("Printer.emitTypeNode: named/generic reference");
                emitEntityName(reference.TypeName);
                return;
            case Kind.TypeQuery:
                emitTypeQuery(node.AsTypeQueryNode());
                return;
            case Kind.TypeOperator:
                emitTypeOperator(node.AsTypeOperatorNode());
                return;
            default:
                throw TypeScript.Checking.Checker.unsupported("Printer.emitTypeNode: unselected synthesized type");
        }
    }

    // printer.go:1970. Internal globalThis formatting uses no type arguments.
    private void emitTypeQuery(TypeQueryNode node)
    {
        writer.Write("typeof");
        writeSpace();
        emitEntityName(node.ExprName);
        if (node.TypeArguments?.Nodes is { Count: > 0 })
            throw TypeScript.Checking.Checker.unsupported("Printer.emitTypeQuery: source generic arguments");
    }

    // printer.go:2125. Selected internal finite-key origins only.
    private void emitTypeOperator(TypeOperatorNode node)
    {
        if (node.Operator != Kind.KeyOfKeyword)
            throw TypeScript.Checking.Checker.unsupported("Printer.emitTypeOperator: unselected operator");
        writer.Write("keyof");
        writeSpace();
        emitTypeNode(node.Type, TypePrecedence.TypeOperator);
    }

    // printer.go:1657. Only readonly static data-property modifiers are admitted.
    private void emitPropertySignature(PropertySignatureDeclaration node)
    {
        emitModifierList(node.Modifiers());
        emitPropertyName(node.Name());
        emitTokenNode(node.PostfixToken);
        emitTypeAnnotation(node.Type);
        writeTrailingSemicolon();
    }

    private void emitModifierList(ModifierList modifiers)
    {
        if (modifiers?.Nodes == null)
            return;
        foreach (var modifier in modifiers.Nodes)
        {
            if (modifier.Kind != Kind.ReadonlyKeyword)
                throw TypeScript.Checking.Checker.unsupported("Printer.emitModifierList: modifier");
            emitKeywordNode(modifier);
            writeSpace();
        }
    }

    private void emitPropertyName(Node node)
    {
        if (node.Kind == Kind.Identifier)
            emitIdentifierReference(node.AsIdentifier());
        else if (node.Kind is Kind.StringLiteral or Kind.NumericLiteral)
            emitLiteral(node);
        else
            throw TypeScript.Checking.Checker.unsupported("Printer.emitPropertyName: dynamic/private name");
    }

    private void emitTokenNode(Node node)
    {
        if (node != null)
            writeTokenText(node.Kind);
    }

    // printer.go:1524.
    private void emitTypeAnnotation(Node node)
    {
        if (node == null)
            return;
        writePunctuation(":");
        writeSpace();
        emitTypeNode(node);
    }

    // printer.go:1979. NodeBuilder sets EFSingleLine for default diagnostic output.
    private void emitTypeLiteral(TypeLiteralNode node)
    {
        writePunctuation("{");
        emitList(emitTypeElement, node.AsNode(), node.Members, ListFormat.SingleLineTypeLiteralMembers | ListFormat.NoSpaceIfEmpty);
        writePunctuation("}");
    }

    private void emitTypeElement(Node node)
    {
        if (node.Kind == Kind.PropertySignature)
            emitPropertySignature(node.AsPropertySignatureDeclaration());
        else if (node.Kind == Kind.IndexSignature)
            emitIndexSignature(node.AsIndexSignatureDeclaration());
        else
            throw TypeScript.Checking.Checker.unsupported("Printer.emitTypeElement: signatures");
    }

    // printer.go:2053/2057. Selected constituents have scalar/primary precedence;
    // named references and nested composite types retain actual type precedence.
    private void emitEntityName(Node node)
    {
        if (node.Kind == Kind.Identifier)
            emitIdentifierReference(node.AsIdentifier());
        else if (node.Kind == Kind.QualifiedName)
        {
            emitEntityName(node.AsQualifiedName().Left);
            writePunctuation(".");
            emitIdentifierReference(node.AsQualifiedName().Right.AsIdentifier());
        }
        else
            throw TypeScript.Checking.Checker.unsupported("Printer.emitEntityName: entity name");
    }

    // printer.go:1782. Diagnostic synthetic index parameter has no modifiers,
    // rest/default/optional state; preserve punctuation/type/semicolon order.
    private void emitIndexSignature(IndexSignatureDeclaration node)
    {
        emitModifierList(node.Modifiers());
        writePunctuation("[");
        var parameters = node.Parameters?.Nodes;
        if (parameters is not { Count: 1 })
            throw TypeScript.Checking.Checker.unsupported("Printer.emitIndexSignature: parameter shape");
        var parameter = parameters[0].AsParameterDeclaration();
        emitIdentifierReference(parameter.Name().AsIdentifier());
        emitTypeAnnotation(parameter.Type);
        writePunctuation("]");
        emitTypeAnnotation(node.Type);
        writeTrailingSemicolon();
    }

    private void emitIntersectionTypeConstituent(Node node) => emitTypeNode(node, TypePrecedence.TypeOperator);
    private void emitIntersectionType(IntersectionTypeNode node) => emitList(emitIntersectionTypeConstituent, node.AsNode(), node.Types, ListFormat.IntersectionTypeConstituents);
    private void emitUnionTypeConstituent(Node node) => emitTypeNode(node, TypePrecedence.TypeOperator);
    private void emitUnionType(UnionTypeNode node) => emitList(emitUnionTypeConstituent, node.AsNode(), node.Types, ListFormat.UnionTypeConstituents);
    [Flags]
    private enum ListFormat
    {
        SingleLine = 0,
        BarDelimited = 1 << 2,
        AmpersandDelimited = 1 << 3,
        SpaceBetweenBraces = 1 << 8,
        SpaceBetweenSiblings = 1 << 9,
        SpaceAfterList = 1 << 21,
        NoSpaceIfEmpty = 1 << 19,
        SingleLineTypeLiteralMembers = SingleLine | SpaceBetweenBraces | SpaceBetweenSiblings,
        UnionTypeConstituents = BarDelimited | SpaceBetweenSiblings | SingleLine,
        IntersectionTypeConstituents = AmpersandDelimited | SpaceBetweenSiblings | SingleLine,
    }

    // printer.go:4726/4734. Fixed single-line synthesized lists: no comments,
    // events, source newline preservation, optional/bracket/trailing comma modes.
    private void emitList(Action<Node> emit, Node parentNode, NodeList children, ListFormat format) => emitListRange(emit, parentNode, children, format, -1, -1);
    private void emitListRange(Action<Node> emit, Node parentNode, NodeList children, ListFormat format, int start, int count)
    {
        bool isNil = children == null;
        int length = isNil ? 0 : children.Nodes?.Count ?? 0;
        if (start < 0)
            start = 0;
        if (count < 0)
            count = length - start;
        bool isEmpty = isNil || start >= length || count <= 0;
        if (isEmpty)
        {
            if ((format & ListFormat.SpaceBetweenBraces) != 0 && (format & ListFormat.NoSpaceIfEmpty) == 0)
                writeSpace();
        }
        else
        {
            int end = Math.Min(start + count, length);
            emitListItems(emit, parentNode, children.Nodes.GetRange(start, end - start), format);
        }
    }

    // printer.go:4887/4897. Original delimiter/leading/sibling/closing whitespace
    // flow with physical-newline/comment/indent branches excluded by fixed options.
    private void writeDelimiter(ListFormat format)
    {
        if ((format & ListFormat.BarDelimited) != 0)
        {
            writeSpace();
            writePunctuation("|");
        }
        else if ((format & ListFormat.AmpersandDelimited) != 0)
        {
            writeSpace();
            writePunctuation("&");
        }
    }

    private void emitListItems(Action<Node> emit, Node parentNode, List<Node> children, ListFormat format)
    {
        if ((format & ListFormat.SpaceBetweenBraces) != 0)
            writeSpace();
        Node previousSibling = null;
        foreach (var child in children)
        {
            if (previousSibling != null)
            {
                writeDelimiter(format);
                if ((format & ListFormat.SpaceBetweenSiblings) != 0)
                    writeSpace();
            }

            emit(child);
            previousSibling = child;
        }

        if ((format & (ListFormat.SpaceAfterList | ListFormat.SpaceBetweenBraces)) != 0)
            writeSpace();
    }

    private void writeSpace() => writer.WriteSpace(" ");
    private void writePunctuation(string text) => writer.WritePunctuation(text);
    private void writeTrailingSemicolon() => writer.WriteTrailingSemicolon(";");
}

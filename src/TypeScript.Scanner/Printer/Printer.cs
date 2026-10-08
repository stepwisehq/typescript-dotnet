// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/printer/printer.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.DiagnosticPrinting;
internal sealed partial class Printer
{
    private readonly EmitContext emitContext;
    internal Printer(EmitContext context) => emitContext = context;
    private TextWriter writer;
    private SourceFile currentSourceFile;
    // Write (5076): diagnostic scalar/object nodes and selected source reuse; no source maps/comments.
    internal void Write(Node node, TextWriter output, SourceFile sourceFile = null)
    {
        var savedWriter = writer;
        var savedSourceFile = currentSourceFile;
        currentSourceFile = sourceFile;
        writer = output;
        writer.Clear();
        emitTypeNode(node);
        writer = savedWriter;
        currentSourceFile = savedSourceFile;
    }

    private void emitKeywordTypeNode(Node node) => emitKeywordNode(node);
    private void emitKeywordNode(Node node)
    {
        if (node == null)
            return;
        writeTokenText(node.Kind);
    }

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
    private void emitTypeNode(Node node)
    {
        if (node == null)
            return;
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
            case Kind.TypeReference:
                var reference = node.AsTypeReferenceNode();
                if (reference.TypeName.Kind != Kind.Identifier || reference.TypeArguments != null)
                    throw TypeScript.Checking.Checker.unsupported("Printer.emitTypeNode: named/generic reference");
                emitIdentifierReference(reference.TypeName.AsIdentifier());
                return;
            default:
                throw TypeScript.Checking.Checker.unsupported("Printer.emitTypeNode: unselected synthesized type");
        }
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
        if (node.Kind != Kind.PropertySignature)
            throw TypeScript.Checking.Checker.unsupported("Printer.emitTypeElement: signatures/indexes");
        emitPropertySignature(node.AsPropertySignatureDeclaration());
    }

    // printer.go:2053/2057. Selected constituents have scalar/primary precedence;
    // references are synthesized ellipsis placeholders, not source type aliases.
    private void emitUnionTypeConstituent(Node node) => emitTypeNode(node);
    private void emitUnionType(UnionTypeNode node) => emitList(emitUnionTypeConstituent, node.AsNode(), node.Types, ListFormat.UnionTypeConstituents);
    [Flags]
    private enum ListFormat
    {
        SingleLine = 0,
        BarDelimited = 1 << 2,
        SpaceBetweenBraces = 1 << 8,
        SpaceBetweenSiblings = 1 << 9,
        SpaceAfterList = 1 << 21,
        NoSpaceIfEmpty = 1 << 19,
        SingleLineTypeLiteralMembers = SingleLine | SpaceBetweenBraces | SpaceBetweenSiblings,
        UnionTypeConstituents = BarDelimited | SpaceBetweenSiblings | SingleLine,
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

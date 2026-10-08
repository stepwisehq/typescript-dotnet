// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/printer/printer.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.DiagnosticPrinting;
internal sealed partial class Printer
{
    private TextWriter writer;
    // Write (5076): synthesized scalar type nodes; no source maps/comments.
    internal void Write(Node node, TextWriter output)
    {
        var savedWriter = writer;
        writer = output;
        writer.Clear();
        if (Ast.IsKeywordTypeNode(node))
            emitKeywordTypeNode(node);
        else if (node.Kind == Kind.LiteralType)
            emitLiteralType(node);
        else if (node.Kind == Kind.Identifier)
            emitIdentifierReference(node.AsIdentifier());
        else
            throw TypeScript.Checking.Checker.unsupported("Printer.Write: nonscalar type node");
        writer = savedWriter;
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
                emitLiteral(node);
                return;
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NullKeyword:
                emitKeywordNode(node);
                return;
            case Kind.PrefixUnaryExpression:
                var unary = node.AsPrefixUnaryExpression();
                if (unary.Operator != Kind.MinusToken)
                    throw TypeScript.Checking.Checker.unsupported("Printer.emitExpression: unary operator");
                writer.Write("-");
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
        if (node.Kind != Kind.Identifier || !Ast.NodeIsSynthesized(node))
            throw TypeScript.Checking.Checker.unsupported("Printer.getTextOfNode: source/general node");
        return node.Text();
    }
}

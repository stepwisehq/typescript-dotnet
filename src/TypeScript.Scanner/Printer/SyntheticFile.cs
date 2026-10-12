// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/printer/syntheticfile.go.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.DiagnosticPrinting;
internal static class SyntheticFile
{
    internal static (string text, Node positioned) PrintAndPositionNode(NodeFactory factory, Node node, SourceFile sourceFile, string newLine, int indentSize, EmitContext emitContext)
    {
        var writer = ChangeTrackerWriter.NewChangeTrackerWriter(newLine, indentSize);
        Printer.NewPrinter(new PrinterOptions { NewLine = CompilerOptionHelpers.GetNewLineKind(newLine), NeverAsciiEscape = true, PreserveSourceNewlines = true, TerminateUnterminatedLiterals = true, }, writer.GetPrintHandlers(), emitContext).Write(node, sourceFile, writer, null);
        var text = GoStrings.TrimSuffix(writer.String(), newLine);
        var positioned = writer.AssignPositionsToNode(node, factory);
        return (text, positioned);
    }

    internal static SourceFile CreateSyntheticSourceFile(NodeFactory factory, Node node, string text, SourceFileParseOptions parseOptions)
    {
        var eof = factory.NewToken(Kind.EndOfFile);
        eof.Loc = new TextRange(text.Length, text.Length);
        var statements = factory.NewNodeList(Slice<Node>.FromArray([node]));
        statements.Loc = new TextRange(node.Pos(), node.End());
        // Printed text is Go byte-role text; borrow its owned bytes at the AST boundary.
        var syntheticFile = factory.NewSourceFileWithTextView(parseOptions, (ByteStringView)text, statements, eof);
        syntheticFile.Loc = new TextRange(0, text.Length);
        Ast.SetParentInChildren(syntheticFile);
        return syntheticFile.AsSourceFile();
    }

    // Borrow the retained printed byte owner at the complete source-file producer boundary.
    internal static SourceFile CreateSyntheticSourceFile(NodeFactory factory, Node node, ByteStringValue text, SourceFileParseOptions parseOptions)
    {
        var eof = factory.NewToken(Kind.EndOfFile);
        eof.Loc = new TextRange(text.Length, text.Length);
        var statements = factory.NewNodeList(Slice<Node>.FromArray([node]));
        statements.Loc = new TextRange(node.Pos(), node.End());
        var synthetic = factory.NewSourceFileWithTextView(parseOptions, text.ByteView, statements, eof);
        synthetic.Loc = new TextRange(0, text.Length);
        Ast.SetParentInChildren(synthetic);
        return synthetic.AsSourceFile();
    }
}

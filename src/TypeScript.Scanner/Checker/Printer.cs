// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/printer.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    private int serializationLevel;
    public string TypeToString(Type t) => ScannerString.ToJsString(typeToStringByteString(t));
    internal string typeToStringByteString(Type t) => typeToString(t, null);
    // printer.go:47/59. Default AllowUniqueESSymbolType|UseAliasDefinedOutsideCurrentScope,
    // no verbosity, and no enclosing declaration are the selected diagnostic contract.
    private string typeToString(Type t, Node enclosingDeclaration) => typeToStringEx(t, enclosingDeclaration);
    private string typeToStringEx(Type t, Node enclosingDeclaration)
    {
        if (enclosingDeclaration != null)
            throw unsupported("typeToStringEx: enclosing declaration");
        if (serializationLevel >= 2)
            return "?";
        if (t.checker != this)
            throw new ArgumentException("Type belongs to a different checker");
        var writer = TypeScript.DiagnosticPrinting.TextWriter.NewTextWriter("", 0);
        var nodeBuilder = new NodeBuilder(this);
        serializationLevel++;
        var typeNode = nodeBuilder.TypeToTypeNode(t);
        serializationLevel--;
        var printer = new TypeScript.DiagnosticPrinting.Printer();
        printer.Write(typeNode, writer);
        var result = writer.String();
        // Default TypeFormatFlags, no verbosity and NoErrorTruncation false.
        const int maxLength = 320; // defaultMaximumTruncationLength * 2 upstream.
        return result.Length >= maxLength ? result[..(maxLength - 3)] + "..." : result;
    }

    // printer.go:126/133. Default All meaning and AllowAnyNodeKind select SymbolToNode.
    private string symbolToString(Symbol symbol) => symbolToStringEx(symbol, null, SymbolFlags.All);
    private string symbolToStringEx(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning)
    {
        if (enclosingDeclaration != null)
            throw unsupported("symbolToStringEx: enclosing declaration");
        var writer = TypeScript.DiagnosticPrinting.TextWriter.NewTextWriter("", 0);
        var nodeBuilder = new NodeBuilder(this);
        var printer = new TypeScript.DiagnosticPrinting.Printer();
        var entity = nodeBuilder.SymbolToNode(symbol, meaning);
        printer.Write(entity, writer);
        return writer.String();
    }
}

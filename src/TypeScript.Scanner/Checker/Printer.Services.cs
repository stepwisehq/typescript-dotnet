// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of internal/checker/printer.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using NodeBuilderFlags = TypeScript.NodeBuilding.Flags;
using InternalFlags = TypeScript.NodeBuilding.InternalFlags;
using Printing = TypeScript.DiagnosticPrinting;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    // printer.go:52/55. Public display retains the existing Unicode boundary.
    private static NodeBuilderFlags toNodeBuilderFlags(TypeFormatFlags flags) => (NodeBuilderFlags)(flags & TypeFormatFlags.NodeBuilderFlagsMask);
    internal string TypeToStringEx(Type type, Node enclosingDeclaration, TypeFormatFlags flags, VerbosityContext verbosity) => ScannerString.ToJsString(TypeToStringExValue(type, enclosingDeclaration, flags, verbosity).Materialize());
    // printer.go:43/47. Keep the ordinary default alias/unique-symbol flag path.
    internal ByteStringValue TypeToStringValue(Type type) => typeToStringExValue(type, null);
    internal ByteStringValue TypeToStringExValue(Type type, Node enclosingDeclaration, TypeFormatFlags flags, VerbosityContext verbosity) => typeToStringExValue(type, enclosingDeclaration, flags: toNodeBuilderFlags(flags), verbosity: verbosity);
    // printer.go:183/187.
    internal string SignatureToStringEx(Signature signature, Node enclosingDeclaration, TypeFormatFlags flags, VerbosityContext verbosity) => ScannerString.ToJsString(SignatureToStringExValue(signature, enclosingDeclaration, flags, verbosity).Materialize());
    internal ByteStringValue SignatureToStringExValue(Signature signature, Node enclosingDeclaration, TypeFormatFlags flags, VerbosityContext verbosity)
    {
        bool isConstructor = (signature.flags & SignatureFlags.Construct) != 0 && (flags & TypeFormatFlags.WriteCallStyleSignature) == 0;
        var kind = (flags & TypeFormatFlags.WriteArrowStyleSignature) != 0 ? (isConstructor ? Kind.ConstructorType : Kind.FunctionType) : (isConstructor ? Kind.ConstructSignature : Kind.CallSignature);
        var builder = getNodeBuilder();
        var oldVerbosity = builder.verbosity;
        builder.verbosity = verbosity;
        try
        {
            var node = builder.SignatureToSignatureDeclaration(signature, kind, enclosingDeclaration, toNodeBuilderFlags(flags) | NodeBuilderFlags.IgnoreErrors | NodeBuilderFlags.WriteTypeParametersInQualifiedName, InternalFlags.None, null);
            var printer = new Printing.Printer(builder.EmitContext(), true, true, true);
            var sourceFile = enclosingDeclaration != null ? Ast.GetSourceFileOfNode(enclosingDeclaration) : null;
            if ((flags & TypeFormatFlags.MultilineObjectLiterals) != 0)
            {
                var writer = Printing.TextWriter.NewTextWriter("\n", 0);
                printer.Write(node, writer, sourceFile);
                return writer.TextValue();
            }

            var(singleLineWriter, putWriter) = Printing.singleLineStringWriter.GetSingleLineStringWriter();
            try
            {
                printer.Write(node, sourceFile, singleLineWriter);
                return singleLineWriter.TextValue();
            }
            finally
            {
                putWriter();
            }
        }
        finally
        {
            builder.verbosity = oldVerbosity;
            releaseNodeBuilderNodes();
        }
    }

    // printer.go:300.
    internal string ExpandSymbolForHover(Symbol symbol, SymbolFlags meaning, VerbosityContext verbosity) => ScannerString.ToJsString(ExpandSymbolForHoverValue(symbol, meaning, verbosity).Materialize());
    internal ByteStringValue ExpandSymbolForHoverValue(Symbol symbol, SymbolFlags meaning, VerbosityContext verbosity)
    {
        var builder = getNodeBuilder();
        var oldVerbosity = builder.verbosity;
        builder.verbosity = verbosity;
        try
        {
            var nodes = builder.ExpandSymbolForHover(symbol, meaning);
            if (nodes.Count == 0)
                return default;
            var printer = new Printing.Printer(builder.EmitContext(), true);
            var sourceFile = symbol.ValueDeclaration != null ? Ast.GetSourceFileOfNode(symbol.ValueDeclaration) : null;
            var output = new GoBuilder();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (i > 0)
                    output.WriteString("\n");
                output.WriteString(printer.EmitValue(nodes[i], sourceFile));
            }

            return output.Value();
        }
        finally
        {
            builder.verbosity = oldVerbosity;
            releaseNodeBuilderNodes();
        }
    }

    // printer.go:328.
    internal string TypeParameterToStringEx(Type type, Node enclosingDeclaration, VerbosityContext verbosity) => ScannerString.ToJsString(TypeParameterToStringExValue(type, enclosingDeclaration, verbosity).Materialize());
    internal ByteStringValue TypeParameterToStringExValue(Type type, Node enclosingDeclaration, VerbosityContext verbosity)
    {
        var builder = getNodeBuilder();
        var oldVerbosity = builder.verbosity;
        builder.verbosity = verbosity;
        try
        {
            var node = builder.TypeParameterToDeclaration(type, enclosingDeclaration, NodeBuilderFlags.IgnoreErrors, InternalFlags.None, null);
            if (node == null)
                return typeToStringExValue(type, null);
            var sourceFile = enclosingDeclaration != null ? Ast.GetSourceFileOfNode(enclosingDeclaration) : null;
            return new Printing.Printer(builder.EmitContext(), true).EmitValue(node, sourceFile);
        }
        finally
        {
            builder.verbosity = oldVerbosity;
            releaseNodeBuilderNodes();
        }
    }

    // printer.go:288. A fresh builder retains the caller's exact identifier-symbol map;
    // its returned nodes keep their real factory ownership, matching getNodeBuilderEx.
    internal Node TypeToTypeNode(Type type, Node enclosingDeclaration, NodeBuilderFlags flags, Dictionary<Node, Symbol> idToSymbol)
    {
        var builder = getNodeBuilderEx(idToSymbol);
        return builder.TypeToTypeNode(type, enclosingDeclaration, flags, InternalFlags.None, null);
    }
}

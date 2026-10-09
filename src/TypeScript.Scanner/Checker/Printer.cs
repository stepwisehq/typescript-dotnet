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
    private string typeToStringNoTypeReduction(Type t) => typeToStringEx(t, null, flags: TypeScript.NodeBuilding.Flags.NoTypeReduction);
    // checker.go:19952 selects WriteArrayAsGenericType without the default alias flags.
    private string typeToStringWriteArrayAsGenericType(Type t) => typeToStringEx(t, null, flags: TypeScript.NodeBuilding.Flags.WriteArrayAsGenericType);
    // printer.go:47/59. Default AllowUniqueESSymbolType|UseAliasDefinedOutsideCurrentScope,
    // no verbosity; selected diagnostic calls may supply an enclosing declaration.
    private string typeToString(Type t, Node enclosingDeclaration) => typeToStringEx(t, enclosingDeclaration);
    private string typeToStringEx(Type t, Node enclosingDeclaration, bool fullyQualified = false, bool noTypeReduction = false, TypeScript.NodeBuilding.Flags? flags = null)
    {
        if (serializationLevel >= 2)
            return "?";
        if (t.checker != this)
            throw new ArgumentException("Type belongs to a different checker");
        var writer = TypeScript.DiagnosticPrinting.TextWriter.NewTextWriter("", 0);
        var nodeBuilder = getNodeBuilder();
        serializationLevel++;
        var combinedFlags = flags != null ? flags.Value | TypeScript.NodeBuilding.Flags.IgnoreErrors : (TypeScript.NodeBuilding.Flags? )null;
        var typeNode = nodeBuilder.TypeToTypeNode(t, enclosingDeclaration, fullyQualified, noTypeReduction, combinedFlags);
        serializationLevel--;
        if (typeNode == null)
            throw new InvalidOperationException("should always get typenode");
        // printer.go:97: only the bare unresolved type retains synthesized comments.
        var printer = new TypeScript.DiagnosticPrinting.Printer(nodeBuilder.EmitContext(), t != unresolvedType);
        printer.Write(typeNode, writer, enclosingDeclaration != null ? Ast.GetSourceFileOfNode(enclosingDeclaration) : null);
        var result = writer.String();
        // Default TypeFormatFlags, no verbosity and NoErrorTruncation false.
        const int maxLength = 320; // defaultMaximumTruncationLength * 2 upstream.
        return result.Length >= maxLength ? result[..(maxLength - 3)] + "..." : result;
    }

    // printer.go:126/133. Default All meaning and AllowAnyNodeKind select SymbolToNode.
    private string symbolToString(Symbol symbol) => symbolToStringEx(symbol, null, SymbolFlags.All);
    // checker.go:16300 selects All/AllowAnyNodeKind with DoNotIncludeSymbolChain.
    private string symbolToStringWithoutSymbolChain(Symbol symbol, Node enclosingDeclaration) => symbolToStringEx(symbol, enclosingDeclaration, SymbolFlags.All, TypeScript.NodeBuilding.InternalFlags.DoNotIncludeSymbolChain);
    private string symbolToStringEx(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning, TypeScript.NodeBuilding.InternalFlags internalFlags = TypeScript.NodeBuilding.InternalFlags.None)
    {
        var writer = TypeScript.DiagnosticPrinting.TextWriter.NewTextWriter("", 0);
        var nodeBuilder = getNodeBuilder();
        var printer = new TypeScript.DiagnosticPrinting.Printer(nodeBuilder.EmitContext());
        var entity = nodeBuilder.SymbolToNode(symbol, meaning, enclosingDeclaration, internalFlags);
        printer.Write(entity, writer, enclosingDeclaration != null ? Ast.GetSourceFileOfNode(enclosingDeclaration) : null);
        return writer.String();
    }

    // printer.go:180/224. Default signature and predicate diagnostic flags.
    private string signatureToString(Signature signature)
    {
        if ((signature.flags & SignatureFlags.Construct) != 0)
            throw unsupported("signatureToString: construct signature");
        var builder = getNodeBuilder();
        var node = builder.SignatureToSignatureDeclaration(signature, Kind.CallSignature, null, TypeScript.NodeBuilding.Flags.IgnoreErrors | TypeScript.NodeBuilding.Flags.WriteTypeParametersInQualifiedName);
        var writer = TypeScript.DiagnosticPrinting.TextWriter.NewTextWriter("", 0);
        new TypeScript.DiagnosticPrinting.Printer(builder.EmitContext(), true, true).Write(node, writer, null);
        return writer.String();
    }

    private string typePredicateToString(TypePredicate predicate)
    {
        var builder = getNodeBuilder();
        var node = builder.TypePredicateToTypePredicateNode(predicate, null, TypeScript.NodeBuilding.Flags.UseAliasDefinedOutsideCurrentScope | TypeScript.NodeBuilding.Flags.IgnoreErrors | TypeScript.NodeBuilding.Flags.WriteTypeParametersInQualifiedName);
        var writer = TypeScript.DiagnosticPrinting.TextWriter.NewTextWriter("", 0);
        new TypeScript.DiagnosticPrinting.Printer(builder.EmitContext()).Write(node, writer, null);
        return writer.String();
    }

    // checker/printer.go:253. Boolean grouping and nullable suffix ordering.
    internal List<Type> formatUnionTypes(List<Type> types, bool expandingEnum)
    {
        List<Type> result = null;
        TypeFlags flags = TypeFlags.None;
        for (int i = 0; i < types.Count; i++)
        {
            var t = types[i];
            flags |= t.flags;
            if ((t.flags & TypeFlags.Nullable) == 0)
            {
                if ((t.flags & TypeFlags.BooleanLiteral) != 0 || (!expandingEnum && (t.flags & TypeFlags.EnumLike) != 0))
                {
                    if ((t.flags & TypeFlags.BooleanLiteral) == 0)
                        throw unsupported("formatUnionTypes: enum");
                    var baseType = booleanType;
                    if ((baseType.flags & TypeFlags.Union) != 0)
                    {
                        int count = baseType.AsUnionType().types.Count;
                        if (i + count <= types.Count && getRegularTypeOfLiteralType(types[i + count - 1]) == getRegularTypeOfLiteralType(baseType.AsUnionType().types[count - 1]))
                        {
                            (result ??= new()).Add(baseType);
                            i += count - 1;
                            continue;
                        }
                    }
                }

                (result ??= new()).Add(t);
            }
        }

        if ((flags & TypeFlags.Null) != 0)
            (result ??= new()).Add(nullType);
        if ((flags & TypeFlags.Undefined) != 0)
            (result ??= new()).Add(undefinedType);
        return result;
    }
}

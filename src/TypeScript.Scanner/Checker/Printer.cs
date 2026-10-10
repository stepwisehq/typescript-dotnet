// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/printer.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    private int serializationLevel;
    // printer.go:249. Preserve the source numeric/string display producer.
    private string valueToString(object value) => ValueToString(value);
    public string TypeToString(Type t) => ScannerString.ToJsString(typeToStringByteString(t));
    // printer.go:120. Public display follows the existing Unicode API boundary.
    public string SymbolToString(Symbol symbol) => ScannerString.ToJsString(symbolToString(symbol));
    internal string typeToStringByteString(Type t) => typeToString(t, null);
    private string typeToStringNoTypeReduction(Type t) => typeToStringEx(t, null, flags: TypeScript.NodeBuilding.Flags.NoTypeReduction);
    // checker.go:19952 selects WriteArrayAsGenericType without the default alias flags.
    private string typeToStringWriteArrayAsGenericType(Type t) => typeToStringEx(t, null, flags: TypeScript.NodeBuilding.Flags.WriteArrayAsGenericType);
    // printer.go:47/59. Preserve diagnostic flags and the compiler's truncation
    // option; service verbosity remains outside the diagnostic entry point.
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
        var noTruncation = compilerOptions.NoErrorTruncation == Tristate.True ||
            (flags.GetValueOrDefault() & TypeScript.NodeBuilding.Flags.NoTruncation) != 0;
        var combinedFlags = (flags ?? (TypeScript.NodeBuilding.Flags.AllowUniqueESSymbolType |
            TypeScript.NodeBuilding.Flags.UseAliasDefinedOutsideCurrentScope |
            (fullyQualified ? TypeScript.NodeBuilding.Flags.UseFullyQualifiedType : 0) |
            (noTypeReduction ? TypeScript.NodeBuilding.Flags.NoTypeReduction : 0))) |
            TypeScript.NodeBuilding.Flags.IgnoreErrors;
        if (noTruncation)
            combinedFlags |= TypeScript.NodeBuilding.Flags.NoTruncation;
        var typeNode = nodeBuilder.TypeToTypeNode(t, enclosingDeclaration, combinedFlags, TypeScript.NodeBuilding.InternalFlags.None, null);
        serializationLevel--;
        if (typeNode == null)
            throw new InvalidOperationException("should always get typenode");
        // printer.go:97: only the bare unresolved type retains synthesized comments.
        var printer = new TypeScript.DiagnosticPrinting.Printer(nodeBuilder.EmitContext(), t != unresolvedType);
        printer.Write(typeNode, writer, enclosingDeclaration != null ? Ast.GetSourceFileOfNode(enclosingDeclaration) : null);
        var result = writer.String();
        var maxLength = (noTruncation ? NodeBuilderImpl.noTruncationMaximumTruncationLength :
            NodeBuilderImpl.defaultMaximumTruncationLength) * 2;
        return result.Length >= maxLength ? result[..(maxLength - 3)] + "..." : result;
    }

    // printer.go:120–177. Public display returns scalar strings; producer arguments and output retain Go bytes.
    public string SymbolToStringEx(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning, SymbolFormatFlags flags) => ScannerString.ToJsString(symbolToStringEx(symbol, enclosingDeclaration, meaning, flags));
    internal string symbolToString(Symbol symbol) => symbolToStringEx(symbol, null, SymbolFlags.All, SymbolFormatFlags.AllowAnyNodeKind);
    private string symbolToStringWithoutSymbolChain(Symbol symbol, Node enclosingDeclaration) => symbolToStringEx(symbol, enclosingDeclaration, SymbolFlags.All, SymbolFormatFlags.AllowAnyNodeKind | SymbolFormatFlags.DoNotIncludeSymbolChain);
    private string symbolToStringEx(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning, SymbolFormatFlags flags = SymbolFormatFlags.AllowAnyNodeKind)
    {
        var writer = TypeScript.DiagnosticPrinting.TextWriter.NewTextWriter("", 0);
        var nodeFlags = TypeScript.NodeBuilding.Flags.IgnoreErrors;
        var internalNodeFlags = TypeScript.NodeBuilding.InternalFlags.None;
        if ((flags & SymbolFormatFlags.UseOnlyExternalAliasing) != 0)
            nodeFlags |= TypeScript.NodeBuilding.Flags.UseOnlyExternalAliasing;
        if ((flags & SymbolFormatFlags.WriteTypeParametersOrArguments) != 0)
            nodeFlags |= TypeScript.NodeBuilding.Flags.WriteTypeParametersInQualifiedName;
        if ((flags & SymbolFormatFlags.UseAliasDefinedOutsideCurrentScope) != 0)
            nodeFlags |= TypeScript.NodeBuilding.Flags.UseAliasDefinedOutsideCurrentScope;
        if ((flags & SymbolFormatFlags.DoNotIncludeSymbolChain) != 0)
            internalNodeFlags |= TypeScript.NodeBuilding.InternalFlags.DoNotIncludeSymbolChain;
        if ((flags & SymbolFormatFlags.WriteComputedProps) != 0)
            internalNodeFlags |= TypeScript.NodeBuilding.InternalFlags.WriteComputedProps;
        var nodeBuilder = getNodeBuilder();
        var sourceFile = enclosingDeclaration == null ? null : Ast.GetSourceFileOfNode(enclosingDeclaration);
        var printer = new TypeScript.DiagnosticPrinting.Printer(nodeBuilder.EmitContext(), true, true, enclosingDeclaration != null && enclosingDeclaration.Kind == Kind.SourceFile);
        var entity = (flags & SymbolFormatFlags.AllowAnyNodeKind) != 0 ? nodeBuilder.SymbolToNode(symbol, meaning, enclosingDeclaration, nodeFlags, internalNodeFlags, null) : nodeBuilder.SymbolToEntityName(symbol, meaning, enclosingDeclaration, nodeFlags, internalNodeFlags, null);
        printer.Write(entity, writer, sourceFile);
        return writer.String();
    }

    // printer.go:180/224. Default signature and predicate diagnostic flags.
    private string signatureToString(Signature signature)
    {
        bool isConstructor = (signature.flags & SignatureFlags.Construct) != 0;
        var builder = getNodeBuilder();
        var node = builder.SignatureToSignatureDeclaration(signature, isConstructor ? Kind.ConstructSignature : Kind.CallSignature, null, TypeScript.NodeBuilding.Flags.IgnoreErrors | TypeScript.NodeBuilding.Flags.WriteTypeParametersInQualifiedName);
        var writer = TypeScript.DiagnosticPrinting.TextWriter.NewTextWriter("", 0);
        new TypeScript.DiagnosticPrinting.Printer(builder.EmitContext(), true, true, true).Write(node, writer, null);
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
                    var baseType = (t.flags & TypeFlags.BooleanLiteral) != 0 ? booleanType : getBaseTypeOfEnumLikeType(t);
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

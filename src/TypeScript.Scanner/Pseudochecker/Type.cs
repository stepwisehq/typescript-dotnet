// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/pseudochecker/type.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Syntactic type skeletons shared by diagnostic and public type-node serialization.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.PseudoChecking;
// PseudoTypes are syntactic skeletons, not semantic types. Managed payloads flatten
// Go's embedded bases into one object retaining the header tag and identity.
internal enum PseudoTypeKind : short
{
    Direct,
    Inferred,
    NoResult,
    MaybeConstLocation,
    Union,
    Undefined,
    Null,
    Any,
    String,
    Number,
    BigInt,
    Boolean,
    False,
    True,
    SingleCallSignature,
    Tuple,
    ObjectLiteral,
    StringLiteral,
    NumericLiteral,
    BigIntLiteral
}

internal class PseudoType
{
    internal PseudoTypeKind Kind;
    // type.go:43/62 embeds the header in its payload. A standalone header still
    // has no payload; canonical factories return the payload object itself.
    private PseudoTypeBase data => this as PseudoTypeBase;
    private static PseudoType newPseudoType(PseudoTypeKind kind, PseudoTypeBase data)
    {
        var n = data.AsPseudoType();
        n.Kind = kind;
        return n;
    }

    internal PseudoTypeTuple AsPseudoTypeTuple() => (PseudoTypeTuple)data;
    internal static PseudoType NewPseudoTypeTuple(List<PseudoType> elements) => newPseudoType(PseudoTypeKind.Tuple, new PseudoTypeTuple { Elements = elements });
    internal PseudoTypeSingleCallSignature AsPseudoTypeSingleCallSignature() => (PseudoTypeSingleCallSignature)data;
    internal static PseudoType NewPseudoTypeSingleCallSignature(Node signature, List<PseudoParameter> parameters, List<TypeParameterDeclaration> typeParameters, PseudoType returnType) => newPseudoType(PseudoTypeKind.SingleCallSignature, new PseudoTypeSingleCallSignature { Signature = signature, Parameters = parameters, TypeParameters = typeParameters, ReturnType = returnType });
    internal PseudoTypeDirect AsPseudoTypeDirect() => (PseudoTypeDirect)data;
    internal PseudoTypeInferred AsPseudoTypeInferred() => (PseudoTypeInferred)data;
    internal PseudoTypeNoResult AsPseudoTypeNoResult() => (PseudoTypeNoResult)data;
    internal PseudoTypeMaybeConstLocation AsPseudoTypeMaybeConstLocation() => (PseudoTypeMaybeConstLocation)data;
    internal PseudoTypeUnion AsPseudoTypeUnion() => (PseudoTypeUnion)data;
    internal PseudoTypeObjectLiteral AsPseudoTypeObjectLiteral() => (PseudoTypeObjectLiteral)data;
    internal PseudoTypeLiteral AsPseudoTypeLiteral() => (PseudoTypeLiteral)data;
    internal static readonly PseudoType PseudoTypeUndefined = newPseudoType(PseudoTypeKind.Undefined, new PseudoTypeBase());
    internal static readonly PseudoType PseudoTypeNull = newPseudoType(PseudoTypeKind.Null, new PseudoTypeBase());
    internal static readonly PseudoType PseudoTypeAny = newPseudoType(PseudoTypeKind.Any, new PseudoTypeBase());
    internal static readonly PseudoType PseudoTypeString = newPseudoType(PseudoTypeKind.String, new PseudoTypeBase());
    internal static readonly PseudoType PseudoTypeNumber = newPseudoType(PseudoTypeKind.Number, new PseudoTypeBase());
    internal static readonly PseudoType PseudoTypeBigInt = newPseudoType(PseudoTypeKind.BigInt, new PseudoTypeBase());
    internal static readonly PseudoType PseudoTypeBoolean = newPseudoType(PseudoTypeKind.Boolean, new PseudoTypeBase());
    internal static readonly PseudoType PseudoTypeFalse = newPseudoType(PseudoTypeKind.False, new PseudoTypeBase());
    internal static readonly PseudoType PseudoTypeTrue = newPseudoType(PseudoTypeKind.True, new PseudoTypeBase());
    internal static PseudoType NewPseudoTypeDirect(Node node) => newPseudoType(PseudoTypeKind.Direct, new PseudoTypeDirect { TypeNode = node });
    internal static PseudoType NewPseudoTypeInferred(Node expr, bool signatureReturn) => newPseudoType(PseudoTypeKind.Inferred, new PseudoTypeInferred { Expression = expr, IsSignatureReturn = signatureReturn });
    internal static PseudoType NewPseudoTypeInferredWithErrors(Node expr, bool signatureReturn, List<Node> errors) => newPseudoType(PseudoTypeKind.Inferred, new PseudoTypeInferred { Expression = expr, IsSignatureReturn = signatureReturn, ErrorNodes = errors });
    internal static PseudoType NewPseudoTypeNoResult(Node decl) => newPseudoType(PseudoTypeKind.NoResult, new PseudoTypeNoResult { Declaration = decl });
    internal static PseudoType NewPseudoTypeMaybeConstLocation(Node loc, PseudoType ct, PseudoType reg) => newPseudoType(PseudoTypeKind.MaybeConstLocation, new PseudoTypeMaybeConstLocation { Node = loc, ConstType = ct, RegularType = reg });
    internal static PseudoType NewPseudoTypeUnion(List<PseudoType> types) => newPseudoType(PseudoTypeKind.Union, new PseudoTypeUnion { Types = types });
    internal static PseudoType NewPseudoTypeObjectLiteral(List<PseudoObjectElement> elements) => newPseudoType(PseudoTypeKind.ObjectLiteral, new PseudoTypeObjectLiteral { Elements = elements });
    internal static PseudoType NewPseudoTypeStringLiteral(Node node) => newPseudoType(PseudoTypeKind.StringLiteral, new PseudoTypeLiteral { Node = node });
    internal static PseudoType NewPseudoTypeNumericLiteral(Node node) => newPseudoType(PseudoTypeKind.NumericLiteral, new PseudoTypeLiteral { Node = node });
    internal static PseudoType NewPseudoTypeBigIntLiteral(Node node) => newPseudoType(PseudoTypeKind.BigIntLiteral, new PseudoTypeLiteral { Node = node });
}

internal class PseudoTypeBase : PseudoType
{
    internal PseudoType AsPseudoType() => this;
}

internal sealed class PseudoTypeDirect : PseudoTypeBase
{
    internal Node TypeNode;
}

internal sealed class PseudoTypeInferred : PseudoTypeBase
{
    internal Node Expression;
    internal List<Node> ErrorNodes;
    internal bool IsSignatureReturn;
}

internal sealed class PseudoTypeNoResult : PseudoTypeBase
{
    internal Node Declaration;
}

internal sealed class PseudoTypeMaybeConstLocation : PseudoTypeBase
{
    internal Node Node;
    internal PseudoType ConstType, RegularType;
}

internal sealed class PseudoTypeUnion : PseudoTypeBase
{
    internal List<PseudoType> Types;
}

internal sealed class PseudoTypeObjectLiteral : PseudoTypeBase
{
    internal List<PseudoObjectElement> Elements;
}

internal sealed class PseudoTypeLiteral : PseudoTypeBase
{
    internal Node Node;
}

internal enum PseudoObjectElementKind : sbyte
{
    Method,
    PropertyAssignment,
    SetAccessor,
    GetAccessor
}

internal class PseudoObjectElement
{
    internal Node Name;
    internal bool Optional;
    internal PseudoObjectElementKind Kind;
    internal PseudoObjectMethod AsPseudoObjectMethod() => (PseudoObjectMethod)this;
    internal PseudoSetAccessor AsPseudoSetAccessor() => (PseudoSetAccessor)this;
    internal PseudoGetAccessor AsPseudoGetAccessor() => (PseudoGetAccessor)this;
    internal Node Signature() => Kind switch
    {
        PseudoObjectElementKind.Method => AsPseudoObjectMethod().SignatureNode,
        PseudoObjectElementKind.SetAccessor => AsPseudoSetAccessor().SignatureNode,
        PseudoObjectElementKind.GetAccessor => AsPseudoGetAccessor().SignatureNode,
        _ => null
    };
    // pseudochecker/type.go:306/323. Accessor skeletons retain the source signature.
    internal static PseudoObjectElement NewPseudoSetAccessor(Node signature, Node name, bool optional, PseudoParameter parameter) => new PseudoSetAccessor
    {
        Kind = PseudoObjectElementKind.SetAccessor, SignatureNode = signature,
        Name = name, Optional = optional, Parameter = parameter
    };
    internal static PseudoObjectElement NewPseudoGetAccessor(Node signature, Node name, bool optional, PseudoType type) => new PseudoGetAccessor
    {
        Kind = PseudoObjectElementKind.GetAccessor, SignatureNode = signature,
        Name = name, Optional = optional, Type = type
    };
    internal static PseudoObjectElement NewPseudoObjectMethod(Node signature, Node name, bool optional, List<TypeParameterDeclaration> typeParameters, List<PseudoParameter> parameters, PseudoType returnType) => new PseudoObjectMethod
    {
        Kind = PseudoObjectElementKind.Method,
        SignatureNode = signature,
        Name = name,
        Optional = optional,
        TypeParameters = typeParameters,
        Parameters = parameters,
        ReturnType = returnType
    };
    internal PseudoPropertyAssignment AsPseudoPropertyAssignment() => (PseudoPropertyAssignment)this;
    internal static PseudoObjectElement NewPseudoPropertyAssignment(bool isReadonly, Node name, bool optional, PseudoType type) => new PseudoPropertyAssignment
    {
        Kind = PseudoObjectElementKind.PropertyAssignment,
        Name = name,
        Optional = optional,
        Readonly = isReadonly,
        Type = type
    };
}

internal sealed class PseudoPropertyAssignment : PseudoObjectElement
{
    internal bool Readonly;
    internal PseudoType Type;
}

internal sealed class PseudoParameter
{
    internal bool Rest, Optional;
    internal Node Name;
    internal PseudoType Type;
    internal static PseudoParameter NewPseudoParameter(bool rest, Node name, bool optional, PseudoType type) => new()
    {
        Rest = rest,
        Name = name,
        Optional = optional,
        Type = type
    };
}

internal sealed class PseudoTypeSingleCallSignature : PseudoTypeBase
{
    internal Node Signature;
    internal List<PseudoParameter> Parameters;
    internal List<TypeParameterDeclaration> TypeParameters;
    internal PseudoType ReturnType;
}

internal sealed class PseudoObjectMethod : PseudoObjectElement
{
    internal Node SignatureNode;
    internal List<PseudoParameter> Parameters;
    internal List<TypeParameterDeclaration> TypeParameters;
    internal PseudoType ReturnType;
}

// pseudochecker/type.go:210. Actual syntactic const-tuple skeleton.
internal sealed class PseudoTypeTuple : PseudoTypeBase
{
    internal List<PseudoType> Elements;
}

internal sealed class PseudoSetAccessor : PseudoObjectElement
{
    internal Node SignatureNode;
    internal PseudoParameter Parameter;
}

internal sealed class PseudoGetAccessor : PseudoObjectElement
{
    internal Node SignatureNode;
    internal PseudoType Type;
}

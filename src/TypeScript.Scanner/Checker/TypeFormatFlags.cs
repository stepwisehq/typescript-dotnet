// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of internal/checker/types.go:61 at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Checking;
[Flags]
internal enum TypeFormatFlags : uint
{
    None = 0,
    NoTruncation = 1 << 0,
    WriteArrayAsGenericType = 1 << 1,
    GenerateNamesForShadowedTypeParams = 1 << 2,
    UseStructuralFallback = 1 << 3,
    WriteTypeArgumentsOfSignature = 1 << 5,
    UseFullyQualifiedType = 1 << 6,
    SuppressAnyReturnType = 1 << 8,
    MultilineObjectLiterals = 1 << 10,
    WriteClassExpressionAsTypeLiteral = 1 << 11,
    UseTypeOfFunction = 1 << 12,
    OmitParameterModifiers = 1 << 13,
    UseAliasDefinedOutsideCurrentScope = 1 << 14,
    UseSingleQuotesForStringLiteralType = 1 << 28,
    NoTypeReduction = 1 << 29,
    UseInstantiationExpressions = 1 << 30,
    OmitThisParameter = 1 << 25,
    WriteCallStyleSignature = 1 << 27,
    AllowUniqueESSymbolType = 1 << 20,
    AddUndefined = 1 << 17,
    WriteArrowStyleSignature = 1 << 18,
    InArrayType = 1 << 19,
    InElementType = 1 << 21,
    InFirstTypeArgument = 1 << 22,
    InTypeAlias = 1 << 23,
    NodeBuilderFlagsMask = NoTruncation | WriteArrayAsGenericType | GenerateNamesForShadowedTypeParams | UseStructuralFallback | WriteTypeArgumentsOfSignature | UseFullyQualifiedType | SuppressAnyReturnType | MultilineObjectLiterals | WriteClassExpressionAsTypeLiteral | UseTypeOfFunction | OmitParameterModifiers | UseAliasDefinedOutsideCurrentScope | AllowUniqueESSymbolType | InTypeAlias | UseInstantiationExpressions | UseSingleQuotesForStringLiteralType | NoTypeReduction | OmitThisParameter,
}

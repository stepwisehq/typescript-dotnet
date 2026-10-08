// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Partial counterpart of tsc/internal/nodebuilder/types.go; generated enum slice.
using TypeScript.Scanner;

namespace TypeScript.NodeBuilding;
[Flags]
public enum Flags : uint
{
    None = 0,
    NoTruncation = (1u << 0),
    WriteArrayAsGenericType = (1u << 1),
    GenerateNamesForShadowedTypeParams = (1u << 2),
    UseStructuralFallback = (1u << 3),
    ForbidIndexedAccessSymbolReferences = (1u << 4),
    WriteTypeArgumentsOfSignature = (1u << 5),
    UseFullyQualifiedType = (1u << 6),
    UseOnlyExternalAliasing = (1u << 7),
    SuppressAnyReturnType = (1u << 8),
    WriteTypeParametersInQualifiedName = (1u << 9),
    MultilineObjectLiterals = (1u << 10),
    WriteClassExpressionAsTypeLiteral = (1u << 11),
    UseTypeOfFunction = (1u << 12),
    OmitParameterModifiers = (1u << 13),
    UseAliasDefinedOutsideCurrentScope = (1u << 14),
    UseSingleQuotesForStringLiteralType = (1u << 28),
    NoTypeReduction = (1u << 29),
    UseInstantiationExpressions = (1u << 30),
    OmitThisParameter = (1u << 25),
    WriteCallStyleSignature = (1u << 27),
    AllowThisInObjectLiteral = (1u << 15),
    AllowQualifiedNameInPlaceOfIdentifier = (1u << 16),
    AllowAnonymousIdentifier = (1u << 17),
    AllowEmptyUnionOrIntersection = (1u << 18),
    AllowEmptyTuple = (1u << 19),
    AllowUniqueESSymbolType = (1u << 20),
    AllowEmptyIndexInfoType = (1u << 21),
    AllowNodeModulesRelativePaths = (1u << 26),
    IgnoreErrors = AllowThisInObjectLiteral | AllowQualifiedNameInPlaceOfIdentifier | AllowAnonymousIdentifier | AllowEmptyUnionOrIntersection | AllowEmptyTuple | AllowEmptyIndexInfoType | AllowNodeModulesRelativePaths,
    InObjectTypeLiteral = (1u << 22),
    InTypeAlias = (1u << 23),
    InInitialEntityName = (1u << 24),
}

[Flags]
public enum InternalFlags : int
{
    None = 0,
    WriteComputedProps = (1 << 0),
    NoSyntacticPrinter = (1 << 1),
    DoNotIncludeSymbolChain = (1 << 2),
    AllowUnresolvedNames = (1 << 3),
}

// BEGIN MANUAL SYMBOL TRACKER INTERFACE
// Selected diagnostic callbacks from types.go:9. The general emit interface's
// module, private/class and nonserializable callbacks remain explicit exclusions.
internal interface SymbolTracker
{
    bool TrackSymbol(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning);
    void ReportCyclicStructureError();
    void ReportInaccessibleThisError();
    void ReportInferenceFallback(Node node);
}

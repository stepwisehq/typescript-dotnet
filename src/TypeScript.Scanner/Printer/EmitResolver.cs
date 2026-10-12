// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/printer/emitresolver.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Complete shared semantic emit contract; checker provides the genuine implementation.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.DiagnosticPrinting;
internal enum SymbolAccessibility
{
    Accessible,
    NotAccessible,
    CannotBeNamed,
    NotResolved
}

internal sealed class SymbolAccessibilityResult
{
    internal SymbolAccessibility Accessibility;
    internal Slice<Node> AliasesToMakeVisible;
    internal string ErrorSymbolName = "", ErrorModuleName = "";
    internal Node ErrorNode;
}

internal enum TypeReferenceSerializationKind
{
    Unknown, TypeWithConstructSignatureAndValue, VoidNullableOrNeverType, NumberLikeType,
    BigIntLikeType, StringLikeType, BooleanType, ArrayLikeType, ESSymbolType, Promise,
    TypeWithCallSignature, ObjectType
}
internal interface IEmitResolver : IReferenceResolver
{
    EmitContext EmitContext();
    bool IsReferencedAliasDeclaration(Node node);
    bool IsValueAliasDeclaration(Node node);
    bool IsTopLevelValueImportEqualsWithEntityName(Node node);
    void MarkLinkedReferencesRecursively(SourceFile file);
    SourceFile GetExternalModuleFileFromDeclaration(Node node);
    ModifierFlags GetEffectiveDeclarationFlags(Node node, ModifierFlags flags);
    TypeReferenceSerializationKind GetTypeReferenceSerializationKind(Node name, Node serialScope);
    object GetConstantValue(Node node);
    Node GetJsxFactoryEntity(Node location);
    Node GetJsxFragmentFactoryEntity(Node location);
    void SetReferencedImportDeclaration(Node node, Node declaration);
    void PrecalculateDeclarationEmitVisibility(SourceFile file);
    SymbolAccessibilityResult IsSymbolAccessible(Symbol symbol, Node enclosingDeclaration, SymbolFlags meaning, bool shouldComputeAliasToMarkVisible);
    SymbolAccessibilityResult IsEntityNameVisible(Node entityName, Node enclosingDeclaration);
    bool IsExpandoFunctionDeclaration(Node node);
    bool IsExpandoFunctionDeclarationUnsafe(Node node);
    bool IsLiteralConstDeclaration(Node node);
    bool RequiresAddingImplicitUndefined(Node node, Symbol symbol, Node enclosingDeclaration);
    bool IsDeclarationVisible(Node node);
    bool IsNameResolvable(Node location, string name);
    bool IsImportRequiredByAugmentation(ImportDeclaration declaration);
    bool IsDefinitelyReferenceToGlobalSymbolObject(Node node);
    bool IsImplementationOfOverload(Node node);
    Evaluator.Result GetEnumMemberValue(Node node);
    bool IsLateBound(Node node);
    bool IsOptionalParameter(Node node);
    bool IsThisPropertyAssignmentDeclarationRedundant(Node node);
    Slice<Symbol> GetPropertiesOfContainerFunction(Node node);
    bool RequiresAddingImplicitUndefinedUnsafe(Node node, Symbol symbol, Node enclosingDeclaration);
    Node GetReferencedValueDeclarationUnsafe(Node node);
    Node CreateTypeOfDeclaration(Node declaration, Node enclosingDeclaration, TypeScript.NodeBuilding.Flags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, TypeScript.NodeBuilding.SymbolTracker tracker);
    Node CreateReturnTypeOfSignatureDeclaration(Node declaration, Node enclosingDeclaration, TypeScript.NodeBuilding.Flags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, TypeScript.NodeBuilding.SymbolTracker tracker);
    Slice<Node> CreateTypeParametersOfSignatureDeclaration(Node declaration, Node enclosingDeclaration, TypeScript.NodeBuilding.Flags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, TypeScript.NodeBuilding.SymbolTracker tracker);
    Node CreateLiteralConstValue(Node node, TypeScript.NodeBuilding.SymbolTracker tracker);
    Node CreateTypeOfExpression(Node expression, Node enclosingDeclaration, TypeScript.NodeBuilding.Flags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, TypeScript.NodeBuilding.SymbolTracker tracker);
    Slice<Node> CreateLateBoundIndexSignatures(Node container, Node enclosingDeclaration, TypeScript.NodeBuilding.Flags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, TypeScript.NodeBuilding.SymbolTracker tracker);
    Node TryJSTypeNodeToTypeNode(Node typeNode, Node enclosingDeclaration, TypeScript.NodeBuilding.Flags flags, TypeScript.NodeBuilding.InternalFlags internalFlags, TypeScript.NodeBuilding.SymbolTracker tracker);
}

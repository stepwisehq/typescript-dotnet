// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/emitsupport.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Diagnostic-only finite data-property, scalar and non-generic callable context reuse; general emit excluded.
#nullable disable
using TypeScript.Scanner;
using TypeScript.PseudoChecking;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    private readonly EmitResolverLinks emitResolverLinks = new();
    // emitsupport.go:12. Preserve declaration visibility's cached tristate.
    private bool isDeclarationVisible(Node node)
    {
        if (!Ast.IsParseTreeNode(node) || node == null)
            return false;
        var links = emitResolverLinks.Get(node);
        if (links.isVisible == Tristate.Unknown)
            links.isVisible = determineIfDeclarationIsVisible(node) ? Tristate.True : Tristate.False;
        return links.isVisible == Tristate.True;
    }

    private bool determineIfDeclarationIsVisible(Node node)
    {
        switch (node.Kind)
        {
            case Kind.VariableDeclaration:
            case Kind.FunctionDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
                if (Ast.IsVariableDeclaration(node) && Ast.IsBindingPattern(node.Name()) && (node.Name().Elements()?.Count ?? 0) == 0)
                    return false;
                var parent = Ast.GetDeclarationContainer(node);
                if ((getCombinedModifierFlagsCached(node) & ModifierFlags.Export) == 0 && !(parent.Kind != Kind.SourceFile && (parent.Flags & NodeFlags.Ambient) != 0))
                    return Ast.IsGlobalSourceFile(parent);
                return isDeclarationVisible(parent);
            case Kind.PropertySignature:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
                if (getEffectiveDeclarationFlags(node, ModifierFlags.Private | ModifierFlags.Protected) != 0)
                    return false;
                return isDeclarationVisible(node.Parent);
            case Kind.CallSignature:
            case Kind.ConstructSignature:
            case Kind.IndexSignature:
            case Kind.Parameter:
            case Kind.FunctionType:
            case Kind.TypeLiteral:
            case Kind.TypeReference:
            case Kind.ArrayType:
            case Kind.TupleType:
            case Kind.NamedTupleMember:
            case Kind.UnionType:
            case Kind.IntersectionType:
            case Kind.ParenthesizedType:
                return isDeclarationVisible(node.Parent);
            case Kind.TypeParameter:
            case Kind.SourceFile:
                return true;
            case Kind.ExportSpecifier:
                var exportDeclaration = node.Parent.Parent;
                return Ast.IsExportDeclaration(exportDeclaration) && exportDeclaration.ModuleSpecifier() == null && isDeclarationVisible(exportDeclaration.Parent);
            case Kind.ImportClause:
            case Kind.NamespaceImport:
            case Kind.ImportSpecifier:
            case Kind.ExportAssignment:
            case Kind.Block:
            case Kind.VariableStatement:
            case Kind.VariableDeclarationList:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.TypePredicate:
                return false; // Original switch default, not a manufactured visibility.
            case Kind.ClassDeclaration:
            case Kind.ModuleDeclaration:
            case Kind.EnumDeclaration:
                throw unsupported("determineIfDeclarationIsVisible: excluded class/namespace/enum producer");
            default:
                return false; // Original default (including import/export declarations).
        }
    }

    private TypeScript.DiagnosticPrinting.SymbolAccessibilityResult hasVisibleDeclarations(Symbol symbol, bool shouldComputeAliasToMakeVisible)
    {
        if (shouldComputeAliasToMakeVisible)
            throw unsupported("hasVisibleDeclarations: declaration emit alias marking");
        foreach (var declaration in symbol.Declarations ?? [])
        {
            if (Ast.IsIdentifier(declaration))
                continue;
            if (isDeclarationVisible(declaration))
                continue;
            var importSyntax = getAnyImportSyntax(declaration);
            if (importSyntax != null && !Ast.HasSyntacticModifier(importSyntax, ModifierFlags.Export) && isDeclarationVisible(importSyntax.Parent))
                continue; // Original noopAddVisibleAlias when shouldComputeAlias=false.
            if (Ast.IsVariableDeclaration(declaration) && Ast.IsVariableStatement(declaration.Parent.Parent) && !Ast.HasSyntacticModifier(declaration.Parent.Parent, ModifierFlags.Export) && isDeclarationVisible(declaration.Parent.Parent.Parent))
                continue;
            if (Ast.IsLateVisibilityPaintedStatement(declaration) && !Ast.HasSyntacticModifier(declaration, ModifierFlags.Export) && isDeclarationVisible(declaration.Parent))
                continue;
            if (Ast.IsBindingElement(declaration))
                throw unsupported("hasVisibleDeclarations: binding declaration visibility");
            return null;
        }

        // Original slices.Collect(maps.Values(nil)) is an allocated empty slice.
        return new()
        {
            Accessibility = TypeScript.DiagnosticPrinting.SymbolAccessibility.Accessible,
            AliasesToMakeVisible = new()
        };
    }

    // emitsupport.go:136. Value/type/namespace meaning of a reused entity name.
    internal static SymbolFlags getMeaningOfEntityNameReference(Node entityName)
    {
        var parent = entityName.Parent;
        if (parent.Kind == Kind.TypeQuery || parent.Kind == Kind.ExpressionWithTypeArguments && !Ast.IsPartOfTypeNode(parent) || parent.Kind == Kind.ComputedPropertyName || parent.Kind == Kind.TypePredicate && parent.AsTypePredicateNode().ParameterName == entityName || parent.Kind == Kind.BinaryExpression)
            return SymbolFlags.Value | SymbolFlags.ExportValue;
        if (entityName.Kind is Kind.QualifiedName or Kind.PropertyAccessExpression || parent.Kind == Kind.ImportEqualsDeclaration || parent.Kind == Kind.QualifiedName && parent.AsQualifiedName().Left == entityName || parent.Kind is Kind.PropertyAccessExpression or Kind.ElementAccessExpression && parent.Expression() == entityName)
            return SymbolFlags.Namespace;
        return SymbolFlags.Type;
    }

    // emitsupport.go:285. The anonymous property-signature branch can add undefined
    // only for a reverse-mapped symbol; mapped types remain guarded; ordinary parameter defaults retain the original worker.
    internal bool requiresAddingImplicitUndefined(Node declaration, Symbol symbol, Node enclosingDeclaration)
    {
        if (!Ast.IsParseTreeNode(declaration))
            return false;
        switch (declaration.Kind)
        {
            case Kind.PropertySignature:
                symbol ??= getSymbolOfDeclaration(declaration);
                var t = getTypeOfSymbol(symbol);
                if ((symbol.CheckFlags & CheckFlags.ReverseMapped) != 0 || (t.objectFlags & ObjectFlags.ReverseMapped) != 0)
                    throw unsupported("requiresAddingImplicitUndefined: reverse mapping");
                return false; // Exact final conjunct (ReverseMappedSymbolLinks.Has) is false in this guarded slice.
            case Kind.Parameter:
                return requiresAddingImplicitUndefinedWorker(declaration, enclosingDeclaration);
            case Kind.PropertyDeclaration:
            case Kind.JSDocPropertyTag:
            case Kind.JSDocParameterTag:
                throw unsupported("requiresAddingImplicitUndefined: class/JS");
            default:
                throw new InvalidOperationException("Node cannot possibly require adding undefined");
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/emitsupport.go:304:1.
    private bool requiresAddingImplicitUndefinedWorker(Node parameter, Node enclosingDeclaration)
    {
        return (((this.isRequiredInitializedParameter(parameter, enclosingDeclaration) || this.isOptionalUninitializedParameterProperty(parameter))) && !this.declaredParameterTypeContainsUndefined(parameter));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/emitsupport.go:308:1.
    private bool declaredParameterTypeContainsUndefined(Node parameter)
    {
        var typeNode = parameter.Type();
        if ((typeNode == null))
        {
            return false;
        }

        var t = this.getTypeFromTypeNode(typeNode);
        return (this.isErrorType(t) || this.containsUndefinedType(t));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/emitsupport.go:321:1.
    private bool isOptionalUninitializedParameterProperty(Node parameter)
    {
        return (((this.strictNullChecks && this.isOptionalParameter(parameter)) && ((parameter.Initializer() == null))) && Ast.HasSyntacticModifier(parameter, ModifierFlags.ParameterPropertyModifier));
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/emitsupport.go:328:1.
    private bool isRequiredInitializedParameter(Node parameter, Node enclosingDeclaration)
    {
        if (((!this.strictNullChecks || this.isOptionalParameter(parameter)) || (parameter.Initializer() == null)))
        {
            return false;
        }

        if (Ast.HasSyntacticModifier(parameter, ModifierFlags.ParameterPropertyModifier))
        {
            return ((enclosingDeclaration != null) && Ast.IsFunctionLikeDeclaration(enclosingDeclaration));
        }

        return true;
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/emitsupport.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Declaration visibility and implicit-undefined metadata used by diagnostic/public type-node reuse; emit execution excluded.
#nullable disable
using TypeScript.Scanner;
using TypeScript.PseudoChecking;
using TypeScript.DiagnosticPrinting;

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
            case Kind.JSDocCallbackTag:
            case Kind.JSDocTypedefTag:
            {
                return ((((node.Parent != null) && (node.Parent.Parent != null)) && (node.Parent.Parent.Parent != null)) && Ast.IsSourceFile(node.Parent.Parent.Parent));
            }
            case Kind.BindingElement:
            {
                return this.isDeclarationVisible(node.Parent.Parent);
            }
            case Kind.VariableDeclaration:
            case Kind.ModuleDeclaration:
            case Kind.ClassDeclaration:
            case Kind.InterfaceDeclaration:
            case Kind.TypeAliasDeclaration:
            case Kind.JSTypeAliasDeclaration:
            case Kind.FunctionDeclaration:
            case Kind.EnumDeclaration:
            case Kind.ImportEqualsDeclaration:
            {
                if (Ast.IsVariableDeclaration(node))
                {
                    if ((Ast.IsBindingPattern(node.Name()) && (GoRuntime.Len(node.Name().Elements()) == 0)))
                    {
                        return false;
                    }
                }
                if ((Ast.IsExternalModuleAugmentation(node) || Ast.IsImplicitlyExportedJSDocDeclaration(node)))
                {
                    return true;
                }
                var parent = Ast.GetDeclarationContainer(node);
                if ((((this.getCombinedModifierFlagsCached(node) & ModifierFlags.Export) == 0) && !((((node.Kind != Kind.ImportEqualsDeclaration) && (parent.Kind != Kind.SourceFile)) && ((parent.Flags & NodeFlags.Ambient) != 0)))))
                {
                    return Ast.IsGlobalSourceFile(parent);
                }
                return this.isDeclarationVisible(parent);
            }
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.MethodDeclaration:
            case Kind.MethodSignature:
            {
                if ((this.getEffectiveDeclarationFlags(node, (ModifierFlags.Private | ModifierFlags.Protected)) != 0))
                {
                    return false;
                }
                return this.isDeclarationVisible(node.Parent);
            }
            case Kind.Constructor:
            case Kind.ConstructSignature:
            case Kind.CallSignature:
            case Kind.IndexSignature:
            case Kind.Parameter:
            case Kind.ModuleBlock:
            case Kind.FunctionType:
            case Kind.ConstructorType:
            case Kind.TypeLiteral:
            case Kind.TypeReference:
            case Kind.ArrayType:
            case Kind.TupleType:
            case Kind.UnionType:
            case Kind.IntersectionType:
            case Kind.ParenthesizedType:
            case Kind.NamedTupleMember:
            {
                return this.isDeclarationVisible(node.Parent);
            }
            case Kind.ImportClause:
            case Kind.NamespaceImport:
            case Kind.ImportSpecifier:
            {
                return false;
            }
            case Kind.TypeParameter:
            {
                return true;
            }
            case Kind.SourceFile:
            case Kind.NamespaceExportDeclaration:
            {
                return true;
            }
            case Kind.ExportAssignment:
            {
                return false;
            }
            case Kind.ExportSpecifier:
            {
                var exportDecl = node.Parent.Parent;
                if ((Ast.IsExportDeclaration(exportDecl) && (exportDecl.AsExportDeclaration().ModuleSpecifier == null)))
                {
                    return this.isDeclarationVisible(exportDecl.Parent);
                }
                return false;
            }
            default:
            {
                return false;
            }
        }
    }

    private TypeScript.DiagnosticPrinting.SymbolAccessibilityResult hasVisibleDeclarations(Symbol symbol, bool shouldComputeAliasToMakeVisible)
    {
        Dictionary<Node, Node> aliasesToMakeVisible = null;
        void addVisibleAlias(Node declaration, Node aliasingStatement)
        {
            if (!shouldComputeAliasToMakeVisible)
                return;
            emitResolverLinks.Get(declaration).isVisible = Tristate.True;
            (aliasesToMakeVisible ??= new())[declaration] = aliasingStatement;
        }
        foreach (var declaration in symbol.Declarations)
        {
            if (Ast.IsIdentifier(declaration))
                continue;
            if (isDeclarationVisible(declaration))
                continue;
            var importSyntax = getAnyImportSyntax(declaration);
            if (importSyntax != null && !Ast.HasSyntacticModifier(importSyntax, ModifierFlags.Export) && isDeclarationVisible(importSyntax.Parent))
            {
                addVisibleAlias(declaration, importSyntax);
                continue;
            }
            if (Ast.IsVariableDeclaration(declaration) && Ast.IsVariableStatement(declaration.Parent.Parent) && !Ast.HasSyntacticModifier(declaration.Parent.Parent, ModifierFlags.Export) && isDeclarationVisible(declaration.Parent.Parent.Parent))
            {
                addVisibleAlias(declaration, declaration.Parent.Parent);
                continue;
            }
            if (Ast.IsLateVisibilityPaintedStatement(declaration) && !Ast.HasSyntacticModifier(declaration, ModifierFlags.Export) && isDeclarationVisible(declaration.Parent))
            {
                addVisibleAlias(declaration, declaration);
                continue;
            }
            if (Ast.IsBindingElement(declaration))
            {
                if ((symbol.Flags & SymbolFlags.Alias) != 0 && Ast.IsInJSFile(declaration) &&
                    declaration.Parent != null && declaration.Parent.Parent != null &&
                    Ast.IsVariableDeclaration(declaration.Parent.Parent) &&
                    declaration.Parent.Parent.Parent.Parent != null &&
                    Ast.IsVariableStatement(declaration.Parent.Parent.Parent.Parent) &&
                    !Ast.HasSyntacticModifier(declaration.Parent.Parent.Parent.Parent, ModifierFlags.Export) &&
                    declaration.Parent.Parent.Parent.Parent.Parent != null &&
                    isDeclarationVisible(declaration.Parent.Parent.Parent.Parent.Parent))
                {
                    addVisibleAlias(declaration, declaration.Parent.Parent.Parent.Parent);
                    continue;
                }
                if ((symbol.Flags & SymbolFlags.BlockScopedVariable) != 0)
                {
                    var rootDeclaration = Ast.WalkUpBindingElementsAndPatterns(declaration);
                    if (Ast.IsParameterDeclaration(rootDeclaration))
                        return null;
                    var variableStatement = rootDeclaration.Parent.Parent;
                    if (!Ast.IsVariableStatement(variableStatement))
                        return null;
                    if (Ast.HasSyntacticModifier(variableStatement, ModifierFlags.Export))
                        continue;
                    if (!isDeclarationVisible(variableStatement.Parent))
                        return null;
                    addVisibleAlias(declaration, variableStatement);
                    continue;
                }
            }
            return null;
        }

        // Collect retains an allocated empty slice when no aliases were marked.
        return new()
        {
            Accessibility = TypeScript.DiagnosticPrinting.SymbolAccessibility.Accessible,
            AliasesToMakeVisible = aliasesToMakeVisible?.Values.ToList() ?? new()
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

    // emitsupport.go:285. Diagnostic type-node reuse preserves the source reverse-mapped property condition.
    internal bool requiresAddingImplicitUndefined(Node declaration, Symbol symbol, Node enclosingDeclaration)
    {
        if (!Ast.IsParseTreeNode(declaration))
            return false;
        switch (declaration.Kind)
        {
            case Kind.PropertyDeclaration:
            case Kind.PropertySignature:
            case Kind.JSDocPropertyTag:
                symbol ??= getSymbolOfDeclaration(declaration);
                var t = getTypeOfSymbol(symbol);
                _ = mappedSymbolLinks.Has(symbol);
                return (symbol.Flags & SymbolFlags.Property) != 0 &&
                    (symbol.Flags & SymbolFlags.Optional) != 0 && isOptionalDeclaration(declaration) &&
                    ReverseMappedSymbolLinks.Has(symbol) && ReverseMappedSymbolLinks.Get(symbol).mappedType != null &&
                    containsNonMissingUndefinedType(this, t);
            case Kind.Parameter:
            case Kind.JSDocParameterTag:
                return requiresAddingImplicitUndefinedWorker(declaration, enclosingDeclaration);
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

public sealed partial class Checker
{
    internal SymbolAccessibilityResult isEntityNameVisible(Node entityName, Node enclosingDeclaration, bool shouldComputeAliasToMakeVisible)
    {
        if (!Ast.IsParseTreeNode(entityName))
        {
            return new SymbolAccessibilityResult { Accessibility = SymbolAccessibility.NotAccessible };
        }
        var meaning = getMeaningOfEntityNameReference(entityName);
        var firstIdentifier = Ast.GetFirstIdentifier(entityName);
        var symbol = this.resolveName(enclosingDeclaration, firstIdentifier.Text(), meaning, null, false, false);
        if ((((symbol != null) && ((symbol.Flags & SymbolFlags.TypeParameter) != 0)) && ((meaning & SymbolFlags.Type) != 0)))
        {
            return new SymbolAccessibilityResult { Accessibility = SymbolAccessibility.Accessible };
        }
        if (((symbol == null) && Ast.IsThisIdentifier(firstIdentifier)))
        {
            var sym = this.getSymbolOfDeclaration(this.getThisContainer(firstIdentifier, false, false));
            if ((this.IsSymbolAccessible(sym, enclosingDeclaration, meaning, false).Accessibility == SymbolAccessibility.Accessible))
            {
                return new SymbolAccessibilityResult { Accessibility = SymbolAccessibility.Accessible };
            }
        }
        if ((symbol == null))
        {
            return new SymbolAccessibilityResult { Accessibility = SymbolAccessibility.NotResolved, ErrorSymbolName = firstIdentifier.Text(), ErrorNode = firstIdentifier };
        }
        var visible = this.hasVisibleDeclarations(symbol, shouldComputeAliasToMakeVisible);
        if ((visible != null))
        {
            return visible;
        }
        return new SymbolAccessibilityResult { Accessibility = SymbolAccessibility.NotAccessible, ErrorSymbolName = firstIdentifier.Text(), ErrorNode = firstIdentifier };
    }
}

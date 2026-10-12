// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of tstransforms/typeeraser.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
namespace TypeScript.Transformers.TsTransforms;
internal sealed class TypeEraserTransformer : Transformer
{
    private CompilerOptions compilerOptions;
    private Node parentNode;
    private Node currentNode;
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/typeeraser.go:18:1.
public static Transformer NewTypeEraserTransformer(TransformOptions opt)
{
    var compilerOptions = opt.CompilerOptions;
    var emitContext = opt.Context;
    var tx = new TypeEraserTransformer { compilerOptions = compilerOptions };
    return tx.NewTransformer(tx.visit, emitContext);
}

// Pushes a new child node onto the ancestor tracking stack, returning the grandparent node to be restored later via `popNode`.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/typeeraser.go:26:1.
private Node pushNode(Node node)
{
    Node grandparentNode = default;
    grandparentNode = this.parentNode;
    this.parentNode = this.currentNode;
    this.currentNode = node;
    return grandparentNode;
}

// Pops the last child node off the ancestor tracking stack, restoring the grandparent node.
// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/typeeraser.go:34:1.
private void popNode(Node grandparentNode)
{
    this.currentNode = this.parentNode;
    this.parentNode = grandparentNode;
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/typeeraser.go:39:1.
private Node elide(Node node)
{
    return this.EmitContext().NewNotEmittedStatement(node.AsNode());
}

// Upstream upstream/TypeScript/tsc/internal/transformers/tstransforms/typeeraser.go:43:1.
private Node visit(Node node)
{
    if ((((uint)node.SubtreeFacts() & SubtreeFactValues.ContainsTypeScript) == 0))
    {
        return node;
    }
    if ((Ast.IsStatement(node) && Ast.HasSyntacticModifier(node, ModifierFlags.Ambient)))
    {
        return this.elide(node);
    }
    var grandparentNode = this.pushNode(node);
    try
    {
    switch (node.Kind)
    {
        case Kind.PublicKeyword:
        case Kind.PrivateKeyword:
        case Kind.ProtectedKeyword:
        case Kind.AbstractKeyword:
        case Kind.OverrideKeyword:
        case Kind.ConstKeyword:
        case Kind.DeclareKeyword:
        case Kind.ReadonlyKeyword:
        case Kind.ArrayType:
        case Kind.TupleType:
        case Kind.OptionalType:
        case Kind.RestType:
        case Kind.TypeLiteral:
        case Kind.TypePredicate:
        case Kind.TypeParameter:
        case Kind.AnyKeyword:
        case Kind.UnknownKeyword:
        case Kind.BooleanKeyword:
        case Kind.StringKeyword:
        case Kind.NumberKeyword:
        case Kind.NeverKeyword:
        case Kind.VoidKeyword:
        case Kind.SymbolKeyword:
        case Kind.ConstructorType:
        case Kind.FunctionType:
        case Kind.TypeQuery:
        case Kind.TypeReference:
        case Kind.UnionType:
        case Kind.IntersectionType:
        case Kind.ConditionalType:
        case Kind.ParenthesizedType:
        case Kind.ThisType:
        case Kind.TypeOperator:
        case Kind.IndexedAccessType:
        case Kind.MappedType:
        case Kind.LiteralType:
        case Kind.IndexSignature:
        {
            return null;
        }
        case Kind.InKeyword:
        case Kind.OutKeyword:
        {
            if (((this.parentNode == null) || !Ast.IsBinaryExpression(this.parentNode)))
            {
                return null;
            }
            return this.Visitor().VisitEachChild(node);
        }
        case Kind.JSImportDeclaration:
        {
            return null;
        }
        case Kind.TypeAliasDeclaration:
        case Kind.JSTypeAliasDeclaration:
        case Kind.InterfaceDeclaration:
        {
            return this.elide(node);
        }
        case Kind.NamespaceExportDeclaration:
        {
            return null;
        }
        case Kind.ModuleDeclaration:
        {
            if (((!Ast.IsIdentifier(node.Name()) || !Ast.IsInstantiatedModule(node, this.compilerOptions.ShouldPreserveConstEnums())) || (RuntimeSyntaxTransformer.GetInnermostModuleDeclarationFromDottedModule(node.AsModuleDeclaration()).Body == null)))
            {
                return this.elide(node);
            }
            return this.Visitor().VisitEachChild(node);
        }
        case Kind.ExpressionWithTypeArguments:
        {
            var n = node.AsExpressionWithTypeArguments();
            return this.Factory().UpdateExpressionWithTypeArguments(n, this.Visitor().VisitNode(n.Expression), null);
        }
        case Kind.PropertyDeclaration:
        {
            if ((((this.compilerOptions.ExperimentalDecorators == Tristate.True) && Ast.HasSyntacticModifier(node, (ModifierFlags.Ambient | ModifierFlags.Abstract))) && Ast.HasDecorators(node)))
            {
                var n2 = node.AsPropertyDeclaration();
                return this.Factory().UpdatePropertyDeclaration(n2, this.Visitor().VisitModifiers(n2.Modifiers()), this.Visitor().VisitNode(n2.Name()), null, null, this.Visitor().VisitNode(n2.Initializer));
            }
            if (Ast.HasSyntacticModifier(node, (ModifierFlags.Ambient | ModifierFlags.Abstract)))
            {
                return null;
            }
            var n3 = node.AsPropertyDeclaration();
            return this.Factory().UpdatePropertyDeclaration(n3, this.Visitor().VisitModifiers(n3.Modifiers()), this.Visitor().VisitNode(n3.Name()), null, null, this.Visitor().VisitNode(n3.Initializer));
        }
        case Kind.Constructor:
        {
            var n4 = node.AsConstructorDeclaration();
            if (Ast.NodeIsMissing(n4.Body))
            {
                return null;
            }
            return this.Factory().UpdateConstructorDeclaration(n4, null, null, this.Visitor().VisitNodes(n4.Parameters), null, null, this.Visitor().VisitNode(n4.Body));
        }
        case Kind.MethodDeclaration:
        {
            var n5 = node.AsMethodDeclaration();
            if (Ast.NodeIsMissing(n5.Body))
            {
                return null;
            }
            return this.Factory().UpdateMethodDeclaration(n5, this.Visitor().VisitModifiers(n5.Modifiers()), n5.AsteriskToken, this.Visitor().VisitNode(n5.Name()), null, null, this.Visitor().VisitNodes(n5.Parameters), null, null, this.Visitor().VisitNode(n5.Body));
        }
        case Kind.GetAccessor:
        {
            var n6 = node.AsGetAccessorDeclaration();
            if ((Ast.NodeIsMissing(n6.Body) && Ast.HasSyntacticModifier(node, ModifierFlags.Abstract)))
            {
                return null;
            }
            var body = this.Visitor().VisitNode(n6.Body);
            if ((body == null))
            {
                body = this.Factory().NewBlock(this.Factory().NewNodeList(default(Slice<Node>)), false);
            }
            return this.Factory().UpdateGetAccessorDeclaration(n6, this.Visitor().VisitModifiers(n6.Modifiers()), this.Visitor().VisitNode(n6.Name()), null, this.Visitor().VisitNodes(n6.Parameters), null, null, body);
        }
        case Kind.SetAccessor:
        {
            var n7 = node.AsSetAccessorDeclaration();
            if ((Ast.NodeIsMissing(n7.Body) && Ast.HasSyntacticModifier(node, ModifierFlags.Abstract)))
            {
                return null;
            }
            var body2 = this.Visitor().VisitNode(n7.Body);
            if ((body2 == null))
            {
                body2 = this.Factory().NewBlock(this.Factory().NewNodeList(default(Slice<Node>)), false);
            }
            return this.Factory().UpdateSetAccessorDeclaration(n7, this.Visitor().VisitModifiers(n7.Modifiers()), this.Visitor().VisitNode(n7.Name()), null, this.Visitor().VisitNodes(n7.Parameters), null, null, body2);
        }
        case Kind.VariableDeclaration:
        {
            var n8 = node.AsVariableDeclaration();
            var updated = this.Factory().UpdateVariableDeclaration(n8, this.Visitor().VisitNode(n8.Name()), null, null, this.Visitor().VisitNode(n8.Initializer));
            if ((n8.Type != null))
            {
                this.EmitContext().SetTypeNode(updated.AsVariableDeclaration().Name(), n8.Type);
            }
            return updated;
        }
        case Kind.HeritageClause:
        {
            var n9 = node.AsHeritageClause();
            if ((n9.Token == Kind.ImplementsKeyword))
            {
                return null;
            }
            return this.Factory().UpdateHeritageClause(n9, n9.Token, this.Visitor().VisitNodes(n9.Types));
        }
        case Kind.ClassDeclaration:
        {
            var n10 = node.AsClassDeclaration();
            return this.Factory().UpdateClassDeclaration(n10, this.Visitor().VisitModifiers(n10.Modifiers()), this.Visitor().VisitNode(n10.Name()), null, this.Visitor().VisitNodes(n10.HeritageClauses), this.Visitor().VisitNodes(n10.Members));
        }
        case Kind.ClassExpression:
        {
            var n11 = node.AsClassExpression();
            return this.Factory().UpdateClassExpression(n11, this.Visitor().VisitModifiers(n11.Modifiers()), this.Visitor().VisitNode(n11.Name()), null, this.Visitor().VisitNodes(n11.HeritageClauses), this.Visitor().VisitNodes(n11.Members));
        }
        case Kind.FunctionDeclaration:
        {
            var n12 = node.AsFunctionDeclaration();
            if (Ast.NodeIsMissing(n12.Body))
            {
                return this.elide(node);
            }
            return this.Factory().UpdateFunctionDeclaration(n12, this.Visitor().VisitModifiers(n12.Modifiers()), n12.AsteriskToken, this.Visitor().VisitNode(n12.Name()), null, this.Visitor().VisitNodes(n12.Parameters), null, null, this.Visitor().VisitNode(n12.Body));
        }
        case Kind.FunctionExpression:
        {
            var n13 = node.AsFunctionExpression();
            return this.Factory().UpdateFunctionExpression(n13, this.Visitor().VisitModifiers(n13.Modifiers()), n13.AsteriskToken, this.Visitor().VisitNode(n13.Name()), null, this.Visitor().VisitNodes(n13.Parameters), null, null, this.Visitor().VisitNode(n13.Body));
        }
        case Kind.ArrowFunction:
        {
            var n14 = node.AsArrowFunction();
            return this.Factory().UpdateArrowFunction(n14, this.Visitor().VisitModifiers(n14.Modifiers()), null, this.Visitor().VisitNodes(n14.Parameters), null, null, n14.EqualsGreaterThanToken, this.Visitor().VisitNode(n14.Body));
        }
        case Kind.Parameter:
        {
            if (Ast.IsThisParameter(node))
            {
                return null;
            }
            var n15 = node.AsParameterDeclaration();
            ModifierList modifiers = default;
            if (Ast.IsParameterPropertyDeclaration(node, this.parentNode))
            {
                modifiers = Transformers.ExtractModifiers(this.EmitContext(), n15.Modifiers(), ModifierFlags.ParameterPropertyModifier);
            }
            if (Ast.HasDecorators(node))
            {
                var decorators = node.Decorators();
                var tuple1 = this.Visitor().VisitSlice(decorators); var visited = tuple1.Item1;
                if ((modifiers == null))
                {
                    modifiers = this.Factory().NewModifierList(visited);
                }
                else
                {
                    modifiers = this.Factory().NewModifierList(GoSlices.Concat(modifiers.Nodes, visited));
                }
            }
            return this.Factory().UpdateParameterDeclaration(n15, modifiers, n15.DotDotDotToken, this.Visitor().VisitNode(n15.Name()), null, null, this.Visitor().VisitNode(n15.Initializer));
        }
        case Kind.CallExpression:
        {
            var n16 = node.AsCallExpression();
            return this.Factory().UpdateCallExpression(n16, this.Visitor().VisitNode(n16.Expression), n16.QuestionDotToken, null, this.Visitor().VisitNodes(n16.Arguments), n16.Flags);
        }
        case Kind.NewExpression:
        {
            var n17 = node.AsNewExpression();
            return this.Factory().UpdateNewExpression(n17, this.Visitor().VisitNode(n17.Expression), null, this.Visitor().VisitNodes(n17.Arguments));
        }
        case Kind.TaggedTemplateExpression:
        {
            var n18 = node.AsTaggedTemplateExpression();
            return this.Factory().UpdateTaggedTemplateExpression(n18, this.Visitor().VisitNode(n18.Tag), n18.QuestionDotToken, null, this.Visitor().VisitNode(n18.Template), n18.Flags);
        }
        case Kind.NonNullExpression:
        case Kind.TypeAssertionExpression:
        case Kind.AsExpression:
        case Kind.SatisfiesExpression:
        {
            var partial = this.Factory().NewPartiallyEmittedExpression(this.Visitor().VisitNode(node.Expression()));
            this.EmitContext().SetOriginal(partial, node);
            partial.Loc = node.Loc;
            return partial;
        }
        case Kind.ParenthesizedExpression:
        {
            if (!Ast.IsJSDocTypeAssertion(node))
            {
                var n19 = node.AsParenthesizedExpression();
                var expression = Ast.SkipOuterExpressions(n19.Expression, OuterExpressionKinds.AllExceptAssertionsOrExpressionsWithTypeArguments);
                if ((Ast.IsAssertionExpression(expression) || Ast.IsSatisfiesExpression(expression)))
                {
                    var partial2 = this.Factory().NewPartiallyEmittedExpression(this.Visitor().VisitNode(n19.Expression));
                    this.EmitContext().SetOriginal(partial2, node);
                    partial2.Loc = node.Loc;
                    return partial2;
                }
            }
            return this.Visitor().VisitEachChild(node);
        }
        case Kind.JsxSelfClosingElement:
        {
            var n20 = node.AsJsxSelfClosingElement();
            return this.Factory().UpdateJsxSelfClosingElement(n20, this.Visitor().VisitNode(n20.TagName), null, this.Visitor().VisitNode(n20.Attributes));
        }
        case Kind.JsxOpeningElement:
        {
            var n21 = node.AsJsxOpeningElement();
            return this.Factory().UpdateJsxOpeningElement(n21, this.Visitor().VisitNode(n21.TagName), null, this.Visitor().VisitNode(n21.Attributes));
        }
        case Kind.ImportEqualsDeclaration:
        {
            var n22 = node.AsImportEqualsDeclaration();
            if (n22.IsTypeOnly)
            {
                return null;
            }
            return this.Visitor().VisitEachChild(node);
        }
        case Kind.ImportDeclaration:
        {
            var n23 = node.AsImportDeclaration();
            if ((n23.ImportClause == null))
            {
                return node;
            }
            var importClause = this.Visitor().VisitNode(n23.ImportClause);
            if ((importClause == null))
            {
                return null;
            }
            return this.Factory().UpdateImportDeclaration(n23, n23.Modifiers(), importClause, n23.ModuleSpecifier, n23.Attributes);
        }
        case Kind.ImportClause:
        {
            var n24 = node.AsImportClause();
            if (n24.IsTypeOnly())
            {
                return null;
            }
            var name = n24.Name();
            var namedBindings = this.Visitor().VisitNode(n24.NamedBindings);
            if ((((((name != null) && (namedBindings != null)) && Ast.IsNamedImports(namedBindings)) && (GoRuntime.Len(namedBindings.AsNamedImports().Elements.Nodes) == 0)) && (GoRuntime.Len(n24.NamedBindings.AsNamedImports().Elements.Nodes) != 0)))
            {
                namedBindings = null;
            }
            if (((name == null) && (namedBindings == null)))
            {
                return null;
            }
            return this.Factory().UpdateImportClause(n24, n24.PhaseModifier, name, namedBindings);
        }
        case Kind.NamedImports:
        {
            var n25 = node.AsNamedImports();
            if ((GoRuntime.Len(n25.Elements.Nodes) == 0))
            {
                return node;
            }
            var elements = this.Visitor().VisitNodes(n25.Elements);
            if ((this.compilerOptions.VerbatimModuleSyntax != Tristate.True && (GoRuntime.Len(elements.Nodes) == 0)))
            {
                return null;
            }
            return this.Factory().UpdateNamedImports(n25, elements);
        }
        case Kind.ImportSpecifier:
        {
            var n26 = node.AsImportSpecifier();
            if (n26.IsTypeOnly)
            {
                return null;
            }
            return node;
        }
        case Kind.ExportDeclaration:
        {
            var n27 = node.AsExportDeclaration();
            if (n27.IsTypeOnly)
            {
                return null;
            }
            Node exportClause = default;
            if ((n27.ExportClause != null))
            {
                exportClause = this.Visitor().VisitNode(n27.ExportClause);
                if ((exportClause == null))
                {
                    return null;
                }
            }
            return this.Factory().UpdateExportDeclaration(n27, null, false, exportClause, this.Visitor().VisitNode(n27.ModuleSpecifier), this.Visitor().VisitNode(n27.Attributes));
        }
        case Kind.NamedExports:
        {
            var n28 = node.AsNamedExports();
            if ((GoRuntime.Len(n28.Elements.Nodes) == 0))
            {
                return node;
            }
            var elements2 = this.Visitor().VisitNodes(n28.Elements);
            if ((this.compilerOptions.VerbatimModuleSyntax != Tristate.True && (GoRuntime.Len(elements2.Nodes) == 0)))
            {
                return null;
            }
            return this.Factory().UpdateNamedExports(n28, elements2);
        }
        case Kind.ExportSpecifier:
        {
            var n29 = node.AsExportSpecifier();
            if (n29.IsTypeOnly)
            {
                return null;
            }
            return node;
        }
        case Kind.EnumDeclaration:
        {
            if (Ast.IsEnumConst(node))
            {
                return node;
            }
            return this.Visitor().VisitEachChild(node);
        }
        default:
        {
            return this.Visitor().VisitEachChild(node);
        }
    }
    }
    finally { this.popNode(grandparentNode); }
}


}

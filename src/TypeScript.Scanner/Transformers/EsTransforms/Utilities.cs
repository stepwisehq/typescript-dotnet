// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete port of estransforms/utilities.go, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using NodeFactory = TypeScript.DiagnosticPrinting.NodeFactory;
using TypeScript.Collections;
using Debug = TypeScript.Scanner.Debug;

namespace TypeScript.Transformers.EsTransforms;
internal static partial class EsTransforms
{
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/utilities.go:10:1.
    internal static Node ConvertClassDeclarationToClassExpression(EmitContext emitContext, ClassDeclaration node)
    {
        var updated = emitContext.Factory.NewClassExpression(Transformers.ExtractModifiers(emitContext, node.Modifiers(), ~ModifierFlags.ExportDefault), node.Name(), node.TypeParameters, node.HeritageClauses, node.Members);
        emitContext.SetOriginal(updated, node.AsNode());
        updated.Loc = node.Loc;
        return updated;
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/utilities.go:23:1.
    internal static Node CreateNotNullCondition(EmitContext emitContext, Node left, Node right, bool invert)
    {
        var token = Kind.ExclamationEqualsEqualsToken;
        var op = Kind.AmpersandAmpersandToken;
        if (invert)
        {
            token = Kind.EqualsEqualsEqualsToken;
            op = Kind.BarBarToken;
        }

        return emitContext.Factory.NewBinaryExpression(null, emitContext.Factory.NewBinaryExpression(null, left, null, emitContext.Factory.NewToken(token), emitContext.Factory.NewKeywordExpression(Kind.NullKeyword)), null, emitContext.Factory.NewToken(op), emitContext.Factory.NewBinaryExpression(null, right, null, emitContext.Factory.NewToken(token), emitContext.Factory.NewVoidZeroExpression()));
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/utilities.go:280:1.
    internal static Node CreateAccessorPropertyBackingField(NodeFactory f, PropertyDeclaration node, ModifierList modifiers, Node initializer)
    {
        return f.UpdatePropertyDeclaration(node, modifiers, f.NewGeneratedPrivateNameForNodeEx(node.Name(), new AutoGenerateOptions { Suffix = "_accessor_storage" }), null, null, initializer);
    }
}

// Go Async/ForAwait embed both Transformer and superAccessState. The base
// owns that shared embedded storage without a forwarding object or callback closure.
internal abstract class SuperAccessTransformer : Transformer
{
    protected NodeFactory superFactory;
    protected OrderedSet<ByteStringKey> capturedSuperProperties;
    protected bool hasSuperElementAccess, hasSuperPropertyAssignment;
    protected Node superBinding, superIndexBinding;
    protected NodeVisitor superAccessVisitor;
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/utilities.go:69:1.
    protected void initSuperAccessVisitor(EmitContext emitContext, NodeFactory factory)
    {
        this.superFactory = factory;
        this.superAccessVisitor = emitContext.NewNodeVisitor(this.visitSuperAccessNode);
    }

    // visitSuperAccessNode walks the async/generator body and replaces super property/element
    // accesses with _super/_superIndex references. This is necessary because the async body
    // ends up inside a generator function where `super` is not valid.
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/utilities.go:77:1.
    protected Node visitSuperAccessNode(Node node)
    {
        switch (node.Kind)
        {
            case Kind.CallExpression:
            {
                var call = node.AsCallExpression();
                if (Ast.IsSuperProperty(call.Expression))
                {
                    return this.substituteCallExpressionWithSuperAccess(call, this.superAccessVisitor);
                }

                return this.superAccessVisitor.VisitEachChild(node);
            }

            case Kind.PropertyAccessExpression:
            {
                if ((node.Expression().Kind == Kind.SuperKeyword))
                {
                    return this.superFactory.NewPropertyAccessExpression(this.superBinding, null, node.Name(), NodeFlags.None);
                }

                return this.superAccessVisitor.VisitEachChild(node);
            }

            case Kind.ElementAccessExpression:
            {
                if ((node.Expression().Kind == Kind.SuperKeyword))
                {
                    return this.createSuperElementAccessInAsyncMethod(node.AsElementAccessExpression().ArgumentExpression);
                }

                return this.superAccessVisitor.VisitEachChild(node);
            }

            case Kind.FunctionExpression:
            case Kind.FunctionDeclaration:
            case Kind.MethodDeclaration:
            case Kind.GetAccessor:
            case Kind.SetAccessor:
            case Kind.Constructor:
            case Kind.ClassDeclaration:
            case Kind.ClassExpression:
            {
                return node;
            }

            default:
            {
                return this.superAccessVisitor.VisitEachChild(node);
            }
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/utilities.go:111:1.
    protected Node substituteSuperAccessesInBody(Node body)
    {
        return this.superAccessVisitor.VisitNode(body);
    }

    // substituteCallExpressionWithSuperAccess handles super.x(args) and super[x](args).
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/utilities.go:116:1.
    protected Node substituteCallExpressionWithSuperAccess(CallExpression call, NodeVisitor visitor)
    {
        var expression = call.Expression;
        Node target = default;
        if (Ast.IsPropertyAccessExpression(expression))
        {
            target = this.superFactory.NewPropertyAccessExpression(this.superBinding, null, expression.AsPropertyAccessExpression().Name(), NodeFlags.None);
        }
        else
        {
            if (Ast.IsElementAccessExpression(expression))
            {
                target = this.createSuperElementAccessInAsyncMethod(expression.AsElementAccessExpression().ArgumentExpression);
            }
            else
            {
                return visitor.VisitEachChild(call.AsNode());
            }
        }

        var callTarget = this.superFactory.NewPropertyAccessExpression(target, null, this.superFactory.NewIdentifier("call"), NodeFlags.None);
        Slice<Node> allArgs = default;
        allArgs = allArgs.Append(this.superFactory.NewThisExpression());
        if ((call.Arguments != null))
        {
            var visitedArgs = visitor.VisitNodes(call.Arguments);
            if ((visitedArgs != null))
            {
                allArgs = allArgs.Append(visitedArgs.Nodes);
            }
        }

        var result = this.superFactory.NewCallExpression(callTarget, null, null, this.superFactory.NewNodeList(allArgs), NodeFlags.None);
        result.Loc = call.Loc;
        return result;
    }

    // createSuperElementAccessInAsyncMethod creates _superIndex(x) or _superIndex(x).value.
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/utilities.go:158:1.
    protected Node createSuperElementAccessInAsyncMethod(Node argumentExpression)
    {
        var superIndexCall = this.superFactory.NewCallExpression(this.superIndexBinding, null, null, this.superFactory.NewNodeList(Slice<Node>.FromArray([argumentExpression])), NodeFlags.None);
        if (this.hasSuperPropertyAssignment)
        {
            return this.superFactory.NewPropertyAccessExpression(superIndexCall, null, this.superFactory.NewIdentifier("value"), NodeFlags.None);
        }

        return superIndexCall;
    }

    // createSuperAccessVariableStatement creates a variable named `_super` with accessor
    // properties for the given property names.
    //
    // Create a variable declaration with a getter/setter (if binding) definition for each name:
    //
    //	const _super = Object.create(null, {
    //	    x: { get: () => super.x },                           // read-only
    //	    x: { get: () => super.x, set: (v) => super.x = v }, // read-write
    //	});
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/utilities.go:182:1.
    protected Node createSuperAccessVariableStatement()
    {
        var f = this.superFactory;
        Slice<Node> accessors = default;
        {
            foreach (var name in this.capturedSuperProperties.Values())
            {
                Slice<Node> descriptorProperties = default;
                var getterBody = f.NewPropertyAccessExpression(f.NewKeywordExpression(Kind.SuperKeyword), null, f.NewIdentifierWithTextValue(name.Value), NodeFlags.None);
                var getterArrow = f.NewArrowFunction(null, null, f.NewNodeList(Slice<Node>.Make(0)), null, null, f.NewToken(Kind.EqualsGreaterThanToken), getterBody);
                var getter = f.NewPropertyAssignment(null, f.NewIdentifier("get"), null, null, getterArrow);
                descriptorProperties = descriptorProperties.Append(getter);
                if (this.hasSuperPropertyAssignment)
                {
                    var vParam = f.NewParameterDeclaration(null, null, f.NewIdentifier("v"), null, null, null);
                    var superProp = f.NewPropertyAccessExpression(f.NewKeywordExpression(Kind.SuperKeyword), null, f.NewIdentifierWithTextValue(name.Value), NodeFlags.None);
                    var assignExpr = f.NewAssignmentExpression(superProp, f.NewIdentifier("v"));
                    var setterArrow = f.NewArrowFunction(null, null, f.NewNodeList(Slice<Node>.FromArray([vParam])), null, null, f.NewToken(Kind.EqualsGreaterThanToken), assignExpr);
                    var setter = f.NewPropertyAssignment(null, f.NewIdentifier("set"), null, null, setterArrow);
                    descriptorProperties = descriptorProperties.Append(setter);
                }

                var descriptor = f.NewObjectLiteralExpression(f.NewNodeList(descriptorProperties), false);
                var accessor = f.NewPropertyAssignment(null, f.NewIdentifierWithTextValue(name.Value), null, null, descriptor);
                accessors = accessors.Append(accessor);
            }
        }

        var descriptorsObject = f.NewObjectLiteralExpression(f.NewNodeList(accessors), true);
        var objectCreateCall = f.NewCallExpression(f.NewPropertyAccessExpression(f.NewIdentifier("Object"), null, f.NewIdentifier("create"), NodeFlags.None), null, null, f.NewNodeList(Slice<Node>.FromArray([f.NewKeywordExpression(Kind.NullKeyword), descriptorsObject])), NodeFlags.None);
        var decl = f.NewVariableDeclaration(this.superBinding, null, null, objectCreateCall);
        var declList = f.NewVariableDeclarationList(f.NewNodeList(Slice<Node>.FromArray([decl])), NodeFlags.Const);
        return f.NewVariableStatement(null, declList);
    }

    // trackSuperAccess records super property/element accesses and super property assignments
    // for the enclosing async method body. Called from both the main visitor and auxiliary
    // visitors to ensure super accesses are tracked regardless of whether the node has
    // transform flags.
    // Upstream upstream/TypeScript/tsc/internal/transformers/estransforms/utilities.go:251:1.
    protected void trackSuperAccess(Node node)
    {
        if ((this.capturedSuperProperties == null))
        {
            return;
        }

        switch (node.Kind)
        {
            case Kind.PropertyAccessExpression:
            {
                if ((node.Expression().Kind == Kind.SuperKeyword))
                {
                    this.capturedSuperProperties.Add(ByteStringKey.FromValue(node.Name().TextValue()));
                }

                break;
            }

            case Kind.ElementAccessExpression:
            {
                if ((node.Expression().Kind == Kind.SuperKeyword))
                {
                    this.hasSuperElementAccess = true;
                }

                break;
            }

            case Kind.BinaryExpression:
            {
                if ((Ast.IsAssignmentOperator(node.AsBinaryExpression().OperatorToken.Kind) && EsTransforms.AssignmentTargetContainsSuperProperty(node.AsBinaryExpression().Left)))
                {
                    this.hasSuperPropertyAssignment = true;
                }

                break;
            }

            case Kind.PrefixUnaryExpression:
            {
                if ((EsTransforms.IsUpdateExpression(node) && EsTransforms.AssignmentTargetContainsSuperProperty(node.AsPrefixUnaryExpression().Operand)))
                {
                    this.hasSuperPropertyAssignment = true;
                }

                break;
            }

            case Kind.PostfixUnaryExpression:
            {
                if ((EsTransforms.IsUpdateExpression(node) && EsTransforms.AssignmentTargetContainsSuperProperty(node.AsPostfixUnaryExpression().Operand)))
                {
                    this.hasSuperPropertyAssignment = true;
                }

                break;
            }
        }
    }
// createAccessorPropertyBackingField creates a protected backing field for an `accessor` PropertyDeclaration.
}

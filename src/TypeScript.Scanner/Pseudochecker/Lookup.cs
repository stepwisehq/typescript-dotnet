// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/pseudochecker/lookup.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Syntactic declaration/expression/return skeletons used by actual type-node serialization.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.PseudoChecking;
using static PseudoType;
using static PseudoParameter;
using static PseudoObjectElement;

internal sealed partial class PseudoChecker
{
    internal PseudoType GetTypeOfExpression(Node node) => typeFromExpression(node);
    internal PseudoType GetTypeOfDeclaration(Node node)
    {
        switch (node.Kind)
        {
            case Kind.Parameter:
                return typeFromParameter(node.AsParameterDeclaration());
            case Kind.VariableDeclaration:
                return typeFromVariable(node.AsVariableDeclaration());
            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
            case Kind.BinaryExpression:
                return typeFromExpandoProperty(node);
            case Kind.CallExpression:
                return NewPseudoTypeNoResult(node);
            case Kind.PropertySignature:
            case Kind.PropertyDeclaration:
            case Kind.JSDocPropertyTag:
                return typeFromProperty(node);
            case Kind.BindingElement:
                return NewPseudoTypeNoResult(node);
            case Kind.ExportAssignment:
                return typeFromExpression(node.AsExportAssignment().Expression);
            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
                return typeFromPropertyAssignment(node);
            default:
                throw new InvalidOperationException("Node needs to be an inferrable node");
        }
    }

    private PseudoType typeFromPropertyAssignment(Node node)
    {
        var annotation = node.Type();
        if (annotation != null)
            return NewPseudoTypeDirect(annotation);
        if (node.Kind == Kind.PropertyAssignment)
        {
            var init = node.Initializer();
            if (init != null)
            {
                var expr = typeFromExpression(init);
                if (expr != null && (expr.Kind != PseudoTypeKind.Inferred || (expr.AsPseudoTypeInferred().ErrorNodes?.Count ?? 0) > 0))
                    return expr;
            }
        }

        return NewPseudoTypeNoResult(node);
    }

    private PseudoType typeFromProperty(Node node)
    {
        var t = node.Type();
        if (t != null)
            return NewPseudoTypeDirect(t);
        if (Ast.IsPropertyDeclaration(node))
        {
            var init = node.Initializer();
            if (init != null && !isContextuallyTyped(node))
            {
                if (Ast.HasModifier(node, ModifierFlags.Readonly) && Ast.IsTemplateExpression(init))
                    return NewPseudoTypeNoResult(node);
                var expr = typeFromExpression(init);
                if (expr != null && (expr.Kind != PseudoTypeKind.Inferred || GoRuntime.Len(expr.AsPseudoTypeInferred().ErrorNodes) > 0))
                {
                    if (expr.Kind != PseudoTypeKind.Direct && node.AsPropertyDeclaration().PostfixToken?.Kind == Kind.QuestionToken)
                        return addUndefinedIfDefinitelyRequired(expr);
                    return expr;
                }
            }
        }
        return NewPseudoTypeNoResult(node);
    }

    // pseudochecker/lookup.go:26/146–194. Shared accessor annotations and return fallback.
    internal PseudoType GetTypeOfAccessor(Node accessor) => typeFromAccessor(accessor);
    private PseudoType typeFromAccessor(Node accessor)
    {
        var accessors = Ast.GetAllAccessorDeclarationsForDeclaration(accessor, accessor.DeclarationData().Symbol.Declarations);
        var annotation = getTypeAnnotationFromAllAccessorDeclarations(accessor, accessors);
        if (annotation != null && !Ast.IsTypePredicateNode(annotation))
            return NewPseudoTypeDirect(annotation);
        if (accessors.GetAccessor != null)
        {
            var result = createReturnFromSignature(accessors.GetAccessor.AsNode());
            if (result.Kind == PseudoTypeKind.Inferred && GoRuntime.Len(result.AsPseudoTypeInferred().ErrorNodes) == 0)
            {
                List<Node> errors = [accessors.GetAccessor.AsNode()];
                if (accessors.SetAccessor != null)
                    errors.Add(accessors.SetAccessor.AsNode());
                var inferred = result.AsPseudoTypeInferred();
                result = NewPseudoTypeInferredWithErrors(inferred.Expression, inferred.IsSignatureReturn, errors);
            }
            return result;
        }
        return NewPseudoTypeNoResult(accessor);
    }

    private static Node getTypeAnnotationFromAllAccessorDeclarations(Node node, AllAccessorDeclarations accessors)
    {
        var annotation = getTypeAnnotationFromAccessor(node);
        if (annotation == null && node != accessors.FirstAccessor)
            annotation = getTypeAnnotationFromAccessor(accessors.FirstAccessor);
        if (annotation == null && accessors.SecondAccessor != null && node != accessors.SecondAccessor)
            annotation = getTypeAnnotationFromAccessor(accessors.SecondAccessor);
        return annotation;
    }

    private static Node getTypeAnnotationFromAccessor(Node node)
    {
        if (node == null)
            return null;
        if (node.Kind == Kind.GetAccessor)
            return node.AsGetAccessorDeclaration().Type;
        var setter = node.AsSetAccessorDeclaration();
        if (setter.Parameters == null || GoRuntime.Len(setter.Parameters.Nodes) < 1)
            return null;
        var parameter = setter.Parameters.Nodes[0];
        return Ast.IsParameterDeclaration(parameter) ? parameter.AsParameterDeclaration().Type : null;
    }

    // pseudochecker/lookup.go:363. Preserve separately annotated getter/setter types.
    private PseudoObjectElement getAccessorMember(Node accessor, Node name)
    {
        var accessors = Ast.GetAllAccessorDeclarationsForDeclaration(accessor, accessor.Symbol().Declarations);
        if (accessors.GetAccessor != null && accessors.GetAccessor.Type != null &&
            accessors.SetAccessor != null && GoRuntime.Len(accessors.SetAccessor.Parameters.Nodes) > 0 &&
            accessors.SetAccessor.Parameters.Nodes[0].AsParameterDeclaration().Type != null)
        {
            if (Ast.IsGetAccessorDeclaration(accessor))
                return NewPseudoGetAccessor(accessor, name, false, typeFromAccessor(accessor));
            return NewPseudoSetAccessor(accessor, name, false, cloneParameters(accessor.AsSetAccessorDeclaration().Parameters)[0]);
        }
        if (accessor == accessors.FirstAccessor)
            return NewPseudoPropertyAssignment(Ast.IsGetAccessorDeclaration(accessor) && accessors.SecondAccessor == null,
                name, false, typeFromAccessor(accessor));
        return null;
    }

    // pseudochecker/lookup.go:510. Const assertions retain the expression skeleton.
    private PseudoType typeFromTypeAssertion(Node expression, Node typeNode) =>
        Ast.IsConstTypeReference(typeNode) ? typeFromExpression(expression) : NewPseudoTypeDirect(typeNode);

    private PseudoType typeFromExpression(Node node)
    {
        switch (node.Kind)
        {
            case Kind.OmittedExpression:
                return PseudoTypeUndefined;
            case Kind.ParenthesizedExpression:
                return typeFromExpression(node.Expression());
            case Kind.Identifier:
                if (node.Text() == "undefined")
                    return PseudoTypeUndefined;
                break;
            case Kind.NullKeyword:
                return PseudoTypeNull;
            case Kind.PrefixUnaryExpression:
                if (Ast.IsPrimitiveLiteralValue(node, true))
                    return typeFromPrimitiveLiteralPrefix(node.AsPrefixUnaryExpression());
                break;
            case Kind.ObjectLiteralExpression:
                return typeFromObjectLiteral(node.AsObjectLiteralExpression());
            case Kind.NumericLiteral:
                return NewPseudoTypeMaybeConstLocation(node, NewPseudoTypeNumericLiteral(node), PseudoTypeNumber);
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.StringLiteral:
                return NewPseudoTypeMaybeConstLocation(node, NewPseudoTypeStringLiteral(node), PseudoTypeString);
            case Kind.BigIntLiteral:
                return NewPseudoTypeMaybeConstLocation(node, NewPseudoTypeBigIntLiteral(node), PseudoTypeBigInt);
            case Kind.TrueKeyword:
                return NewPseudoTypeMaybeConstLocation(node, PseudoTypeTrue, PseudoTypeBoolean);
            case Kind.FalseKeyword:
                return NewPseudoTypeMaybeConstLocation(node, PseudoTypeFalse, PseudoTypeBoolean);
            case Kind.ArrowFunction:
            case Kind.FunctionExpression:
                return typeFromFunctionLikeExpression(node);
            case Kind.ArrayLiteralExpression:
                return typeFromArrayLiteral(node.AsArrayLiteralExpression());
            case Kind.TypeAssertionExpression:
            case Kind.AsExpression:
                return typeFromTypeAssertion(node.Expression(), node.Type());
            case Kind.ClassExpression:
                return NewPseudoTypeInferredWithErrors(node, false, [node]);
            case Kind.TemplateExpression:
                if (IsInConstContext(node))
                    return NewPseudoTypeInferred(node, false);
                return NewPseudoTypeMaybeConstLocation(node, NewPseudoTypeInferred(node, false), PseudoTypeString);
        }

        return NewPseudoTypeInferred(node, false);
    }

    private PseudoType typeFromObjectLiteral(ObjectLiteralExpression node)
    {
        var errors = canGetTypeFromObjectLiteral(node);
        if (errors != null)
            return NewPseudoTypeInferredWithErrors(node.AsNode(), false, errors);
        if (node.Properties == null || (node.Properties.Nodes.Count) == 0)
            return NewPseudoTypeObjectLiteral(null);
        var elements = new List<PseudoObjectElement>(node.Properties.Nodes.Count);
        foreach (var element in node.Properties.Nodes)
        {
            switch (element.Kind)
            {
                case Kind.MethodDeclaration:
                    var optional = element.AsMethodDeclaration().PostfixToken?.Kind == Kind.QuestionToken;
                    if (element.FunctionLikeData().FullSignature != null)
                        elements.Add(NewPseudoPropertyAssignment(false, element.Name(), optional, NewPseudoTypeDirect(element.FunctionLikeData().FullSignature)));
                    else
                        elements.Add(NewPseudoObjectMethod(element, element.Name(), optional, cloneTypeParameters(element.AsMethodDeclaration().TypeParameters), cloneParameters(element.ParameterList()), createReturnFromSignature(element)));
                    break;
                case Kind.PropertyAssignment:
                    elements.Add(NewPseudoPropertyAssignment(false, element.Name(), element.AsPropertyAssignment().PostfixToken?.Kind == Kind.QuestionToken, typeFromExpression(element.Initializer())));
                    break;
                case Kind.SetAccessor:
                case Kind.GetAccessor:
                    var member = getAccessorMember(element, element.Name());
                    if (member != null)
                        elements.Add(member);
                    break;
            }
        }

        return NewPseudoTypeObjectLiteral(elements);
    }

    private List<Node> canGetTypeFromObjectLiteral(ObjectLiteralExpression node)
    {
        if (node.Properties == null || (node.Properties.Nodes.Count) == 0)
            return null;
        List<Node> errors = null;
        foreach (var e in node.Properties.Nodes)
        {
            if ((e.Flags & NodeFlags.ThisNodeHasError) != 0)
            {
                (errors ??= []).Add(e);
                continue;
            }

            if (e.Kind is Kind.ShorthandPropertyAssignment or Kind.SpreadAssignment)
            {
                (errors ??= []).Add(e);
                continue;
            }

            if ((e.Name().Flags & NodeFlags.ThisNodeHasError) != 0)
            {
                (errors ??= []).Add(e.Name());
                continue;
            }

            if (e.Name().Kind == Kind.PrivateIdentifier)
            {
                (errors ??= []).Add(e);
                continue;
            }

            if (e.Name().Kind == Kind.ComputedPropertyName && !Ast.IsPrimitiveLiteralValue(e.Name().Expression(), false))
                (errors ??= []).Add(e.Name());
        }

        return errors;
    }

    private static bool isConstContextPropagatingKind(Kind kind) => kind is Kind.ArrayLiteralExpression or Kind.ObjectLiteralExpression or Kind.ParenthesizedExpression or Kind.SpreadElement or Kind.PropertyAssignment or Kind.ShorthandPropertyAssignment or Kind.TemplateSpan or Kind.PrefixUnaryExpression;
    internal static bool IsInConstContext(Node node)
    {
        var assertion = Ast.FindAncestor(node.Parent, n => Ast.IsAssertionExpression(n) || !isConstContextPropagatingKind(n.Kind));
        return assertion != null && Ast.IsConstAssertion(assertion);
    }

    private PseudoType typeFromPrimitiveLiteralPrefix(PrefixUnaryExpression node)
    {
        var expr = node.Operator == Kind.PlusToken ? node.Operand : node.AsNode();
        var inner = node.Operand;
        if (inner.Kind == Kind.BigIntLiteral)
            return NewPseudoTypeMaybeConstLocation(node.AsNode(), NewPseudoTypeBigIntLiteral(expr), PseudoTypeBigInt);
        if (inner.Kind == Kind.NumericLiteral)
            return NewPseudoTypeMaybeConstLocation(node.AsNode(), NewPseudoTypeNumericLiteral(expr), PseudoTypeNumber);
        throw new InvalidOperationException("Invalid primitive literal prefix");
    }

    // This is _not_ redundant with the reparser; see how expandoFunctionSymbolProperty.ts and similar behaves
    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:88:1.
    private PseudoType typeFromExpandoProperty(Node node)
    {
        var declaredType = node.Type();
        if ((declaredType != null))
        {
            return NewPseudoTypeDirect(declaredType);
        }

        return NewPseudoTypeNoResult(node);
    }

    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:124:1.
    private PseudoType typeFromVariable(VariableDeclaration declaration)
    {
        var t = declaration.Type;
        if ((t != null))
        {
            return NewPseudoTypeDirect(t);
        }

        var init = declaration.Initializer;
        if ((((init != null) && (declaration.Symbol != null)) && (((GoRuntime.Len(declaration.Symbol.Declarations) == 1) || (declaration.Symbol.Declarations.Count(Ast.IsVariableDeclaration) == 1)))))
        {
            if (!isContextuallyTyped(declaration.AsNode()))
            {
                if ((Ast.IsVarConst(declaration.AsNode()) && Ast.IsTemplateExpression(init)))
                {
                    return NewPseudoTypeNoResult(declaration.AsNode());
                }

                var expr = this.typeFromExpression(init);
                if (((expr != null) && (((expr.Kind != PseudoTypeKind.Inferred) || (GoRuntime.Len(expr.AsPseudoTypeInferred().ErrorNodes) > 0)))))
                {
                    return expr;
                }
            }
        }

        return NewPseudoTypeNoResult(declaration.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:196:1.
    private static bool isValueSignatureDeclaration(Node node)
    {
        return (((((Ast.IsFunctionExpression(node) || Ast.IsArrowFunction(node)) || Ast.IsMethodDeclaration(node)) || Ast.IsAccessor(node)) || Ast.IsFunctionDeclaration(node)) || Ast.IsConstructorDeclaration(node));
    }

    // does not return `nil`, returns a `NoResult` pseudotype instead
    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:201:1.
    private PseudoType createReturnFromSignature(Node fn)
    {
        if (Ast.IsFunctionLike(fn))
        {
            var d = fn.FunctionLikeData();
            var r = d.Type;
            if ((r != null))
            {
                return NewPseudoTypeDirect(r);
            }
        }

        if (isValueSignatureDeclaration(fn))
        {
            return this.typeFromSingleReturnExpression(fn);
        }

        return NewPseudoTypeNoResult(fn);
    }

    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:216:1.
    private PseudoType typeFromSingleReturnExpression(Node fn)
    {
        Node candidateExpr = default;
        if (((fn != null) && !Ast.NodeIsMissing(fn.Body())))
        {
            var flags = Ast.GetFunctionFlags(fn);
            if (((flags & FunctionFlags.AsyncGenerator) != 0))
            {
                return NewPseudoTypeInferred(fn, true);
            }

            var body = fn.Body();
            if (Ast.IsBlock(body))
            {
                Ast.ForEachReturnStatement(body, (stmt) =>
                {
                    if ((stmt.Parent != body))
                    {
                        candidateExpr = null;
                        return true;
                    }

                    if ((candidateExpr == null))
                    {
                        candidateExpr = stmt.AsReturnStatement().Expression;
                    }
                    else
                    {
                        candidateExpr = null;
                        return true;
                    }

                    return false;
                });
            }
            else
            {
                candidateExpr = body;
            }
        }

        if ((candidateExpr != null))
        {
            if (isContextuallyTyped(candidateExpr))
            {
                Node t = default;
                if ((candidateExpr.Kind == Kind.TypeAssertionExpression))
                {
                    t = candidateExpr.AsTypeAssertion().Type;
                }
                else
                {
                    if ((candidateExpr.Kind == Kind.AsExpression))
                    {
                        t = candidateExpr.AsAsExpression().Type;
                    }
                }

                if (((t != null) && !Ast.IsConstTypeReference(t)))
                {
                    return NewPseudoTypeDirect(t);
                }
            }
            else
            {
                return this.typeFromExpression(candidateExpr);
            }
        }

        return NewPseudoTypeInferred(fn, true);
    }

    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:517:1.
    private PseudoType typeFromFunctionLikeExpression(Node node)
    {
        if ((node.FunctionLikeData().FullSignature != null))
        {
            return NewPseudoTypeDirect(node.FunctionLikeData().FullSignature);
        }

        var returnType = this.createReturnFromSignature(node);
        var typeParameters = this.cloneTypeParameters(node.FunctionLikeData().TypeParameters);
        var parameters = this.cloneParameters(node.FunctionLikeData().Parameters);
        return NewPseudoTypeSingleCallSignature(node, parameters, typeParameters, returnType);
    }

    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:532:1.
    private List<TypeParameterDeclaration> cloneTypeParameters(NodeList nodes)
    {
        if ((nodes == null))
        {
            return null;
        }

        if ((GoRuntime.Len(nodes.Nodes) == 0))
        {
            return null;
        }

        var result = GoRuntime.MakeSlice<TypeParameterDeclaration>(0);
        {
            foreach (var e in GoRuntime.RangeValues(nodes.Nodes))
            {
                result = GoRuntime.Append(result, e.AsTypeParameterDeclaration());
            }
        }

        return result;
    }

    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:550:1.
    private static bool typeNodeCouldReferToUndefined(Node node)
    {
        while ((node.Kind == Kind.ParenthesizedType))
        {
            node = node.AsParenthesizedTypeNode().Type;
        }

        switch (node.Kind)
        {
            case Kind.TypeReference:
            case Kind.IndexedAccessType:
            case Kind.TypeQuery:
            case Kind.OptionalType:
            case Kind.RestType:
            case Kind.ImportType:
            {
                return true;
            }

            case Kind.IntersectionType:
            {
                return Core.Some(node.AsIntersectionTypeNode().Types.Nodes, typeNodeCouldReferToUndefined);
            }

            case Kind.UnionType:
            {
                return Core.Some(node.AsUnionTypeNode().Types.Nodes, typeNodeCouldReferToUndefined);
            }

            case Kind.ConditionalType:
            {
                return true;
            }

            case Kind.TypeOperator:
            {
                return true;
            }

            case Kind.TypePredicate:
            {
                return true;
            }

            case Kind.UndefinedKeyword:
            {
                return true;
            }

            default:
            {
                return false;
            }
        }
    }

    // see this as the inverse of `canAddUndefined` in `expressionToTypeNode` in strada
    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:578:1.
    private static bool isUndefinedPseudoType(PseudoType t) => t.Kind == PseudoTypeKind.Undefined || t.Kind == PseudoTypeKind.MaybeConstLocation && isUndefinedPseudoType(t.AsPseudoTypeMaybeConstLocation().ConstType);
    public static bool CouldAlreadyReferToUndefinedType(PseudoType t)
    {
        if ((((t.Kind == PseudoTypeKind.NoResult) || (t.Kind == PseudoTypeKind.Inferred)) || isUndefinedPseudoType(t)))
        {
            return true;
        }

        if ((t.Kind == PseudoTypeKind.MaybeConstLocation))
        {
            var mc = t.AsPseudoTypeMaybeConstLocation();
            return CouldAlreadyReferToUndefinedType(mc.RegularType);
        }

        if ((t.Kind == PseudoTypeKind.Direct))
        {
            var node = t.AsPseudoTypeDirect().TypeNode;
            return typeNodeCouldReferToUndefined(node);
        }

        if ((t.Kind == PseudoTypeKind.Union))
        {
            return Core.Some(t.AsPseudoTypeUnion().Types, CouldAlreadyReferToUndefinedType);
        }

        return false;
    }

    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:597:1.
    private static bool isOptionalInitializedOrRestParameter(Node node)
    {
        var p = node.AsParameterDeclaration();
        if ((((p.DotDotDotToken != null) || (p.Initializer != null)) || (p.QuestionToken != null)))
        {
            return true;
        }

        return false;
    }

    // lastRequiredParamIndex returns the index just past the last required parameter
    // in the list. A parameter is "required" if it has no question token, no initializer,
    // and no rest token. This is computed in a single reverse pass so callers can
    // determine "has required parameter after index i" with `i+1 < lastRequired`
    // (equivalently, `i < lastRequired-1`) in O(1).
    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:610:1.
    private static int lastRequiredParamIndex(Slice<Node> parameters)
    {
        for (int i = GoRuntime.Len(parameters) - 1; i >= 0; i--)
            if (!isOptionalInitializedOrRestParameter(parameters[i]))
                return i + 1;
        return 0;
    }

    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:619:1.
    private static PseudoType addUndefinedIfDefinitelyRequired(PseudoType expr)
    {
        if (CouldAlreadyReferToUndefinedType(expr))
        {
            return expr;
        }

        return NewPseudoTypeUnion(new List<PseudoType> { expr, PseudoTypeUndefined });
    }

    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:631:1.
    private PseudoType typeFromParameter(ParameterDeclaration node)
    {
        var parent = node.Parent;
        if ((parent.Kind == Kind.SetAccessor))
        {
            return GetTypeOfAccessor(parent);
        }

        if ((node.Initializer == null))
        {
            if ((node.Type != null))
            {
                return NewPseudoTypeDirect(node.Type);
            }

            return NewPseudoTypeNoResult(node.AsNode());
        }

        var p = parent.Parameters();
        var selfIdx = GoSlices.Index(p, node.AsNode());
        var lastRequired = lastRequiredParamIndex(p);
        return this.typeFromParameterWorker(node, selfIdx, lastRequired);
    }

    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:649:1.
    private PseudoType typeFromParameterWorker(ParameterDeclaration node, int selfIdx, int lastRequired)
    {
        var parent = node.Parent;
        if ((parent.Kind == Kind.SetAccessor))
        {
            return GetTypeOfAccessor(parent);
        }

        var hasRequiredAfter = (selfIdx < (lastRequired - 1));
        var declaredType = node.Type;
        if ((declaredType != null))
        {
            var result = NewPseudoTypeDirect(declaredType);
            if (((this.strictNullChecks && (node.Initializer != null)) && hasRequiredAfter))
            {
                return addUndefinedIfDefinitelyRequired(result);
            }

            return result;
        }

        if ((((node.Initializer != null) && Ast.IsIdentifier(node.Name())) && !isContextuallyTyped(node.AsNode())))
        {
            var expr = this.typeFromExpression(node.Initializer);
            if (((expr != null) && (((expr.Kind == PseudoTypeKind.Inferred) && (GoRuntime.Len(expr.AsPseudoTypeInferred().ErrorNodes) == 0)))))
            {
                expr = NewPseudoTypeInferredWithErrors(expr.AsPseudoTypeInferred().Expression, false, new List<Node> { node.AsNode() });
            }

            if (!this.strictNullChecks)
            {
                return expr;
            }

            if (!hasRequiredAfter)
            {
                return expr;
            }

            return addUndefinedIfDefinitelyRequired(expr);
        }

        return NewPseudoTypeNoResult(node.AsNode());
    }

    // Upstream upstream/TypeScript/tsc/internal/pseudochecker/lookup.go:687:1.
    private List<PseudoParameter> cloneParameters(NodeList nodes)
    {
        if ((nodes == null))
        {
            return null;
        }

        if ((GoRuntime.Len(nodes.Nodes) == 0))
        {
            return null;
        }

        var lastRequired = lastRequiredParamIndex(nodes.Nodes);
        var result = GoRuntime.MakeSlice<PseudoParameter>(0);
        {
            foreach (var range3 in GoRuntime.Range(nodes.Nodes))
            {
                var i = range3.Key;
                var e = range3.Value;
                var p = e.AsParameterDeclaration();
                var optional = (p.QuestionToken != null);
                if ((!optional && (p.Initializer != null)))
                {
                    optional = (i >= (lastRequired - 1));
                }

                result = GoRuntime.Append(result, NewPseudoParameter((p.DotDotDotToken != null), e.Name(), optional, this.typeFromParameterWorker(p, i, lastRequired)));
            }
        }

        return result;
    }

    internal PseudoType GetReturnTypeOfSignature(Node node)
    {
        switch (node.Kind)
        {
            case Kind.GetAccessor:
                return GetTypeOfAccessor(node);
            case Kind.MethodDeclaration:
            case Kind.FunctionDeclaration:
            case Kind.Constructor:
            case Kind.MethodSignature:
            case Kind.CallSignature:
            case Kind.ConstructSignature:
            case Kind.SetAccessor:
            case Kind.IndexSignature:
            case Kind.FunctionType:
            case Kind.ConstructorType:
            case Kind.FunctionExpression:
            case Kind.ArrowFunction:
            case Kind.JSDocSignature:
                return createReturnFromSignature(node);
            default:
                throw new InvalidOperationException("Node needs to be an inferrable node");
        }
    }

    // lookup.go:715. Annotated parents and call contexts affect local pseudo inference.
    private static bool isContextuallyTyped(Node node) => Ast.FindAncestor(node.Parent, n =>
    {
        if (Ast.IsCallExpression(n) || Ast.IsSatisfiesExpression(n))
            return true;
        if ((Ast.IsVariableParameterOrProperty(n) || Ast.IsAssertionExpression(n)) && n.Type() != null && !Ast.IsConstAssertion(n))
            return true;
        return Ast.IsJsxElement(n) || Ast.IsJsxExpression(n);
    }) != null;
    // lookup.go:438/457. Ordinary arrays deliberately return the original inferred
    // skeleton/error-node state; syntactic const tuples retain all elements.
    private PseudoType typeFromArrayLiteral(ArrayLiteralExpression node)
    {
        var errors = canGetTypeFromArrayLiteral(node);
        if (errors != null)
            return NewPseudoTypeInferredWithErrors(node.AsNode(), false, errors);
        if (IsInConstContext(node.AsNode()) && isContextuallyTyped(node.AsNode()))
            return NewPseudoTypeInferred(node.AsNode(), false);
        var results = new List<PseudoType>(node.Elements?.Nodes.Count ?? 0);
        foreach (var element in node.Elements?.Nodes ?? default)
            results.Add(typeFromExpression(element));
        return NewPseudoTypeTuple(results);
    }

    private static List<Node> canGetTypeFromArrayLiteral(ArrayLiteralExpression node)
    {
        if (!IsInConstContext(node.AsNode()))
            return[node.AsNode()];
        foreach (var element in node.Elements?.Nodes ?? default)
            if (element.Kind == Kind.SpreadElement)
                return[element];
        return null;
    }
}

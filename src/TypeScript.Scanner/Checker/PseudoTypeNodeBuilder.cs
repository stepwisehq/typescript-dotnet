// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/pseudotypenodebuilder.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Source syntactic reuse/equivalence and semantic fallback for diagnostic/public type-node serialization.
#nullable disable
using TypeScript.Scanner;
using TypeScript.PseudoChecking;
using NodeBuilderFlags = TypeScript.NodeBuilding.Flags;

namespace TypeScript.Checking;
internal sealed partial class NodeBuilderImpl
{
    private Node pseudoTypeToNodeWithCheckerFallback(PseudoType t, Type checkerType)
    {
        if (t.Kind == PseudoTypeKind.Inferred)
        {
            if (!ctx.suppressReportInferenceFallback)
            {
                var inferred = t.AsPseudoTypeInferred();
                if (inferred.ErrorNodes is { Count: > 0 })
                    foreach (var n in inferred.ErrorNodes)
                        ctx.tracker.ReportInferenceFallback(n);
                else
                    ctx.tracker.ReportInferenceFallback(inferred.Expression);
            }

            var oldSuppress = ctx.suppressReportInferenceFallback;
            ctx.suppressReportInferenceFallback = true;
            var result = typeToTypeNode(checkerType);
            ctx.suppressReportInferenceFallback = oldSuppress;
            return result;
        }
        else if (t.Kind == PseudoTypeKind.Direct && !canReuseExistingJSTypeNode(t.AsPseudoTypeDirect().TypeNode, checkerType))
        {
            if (!ctx.suppressReportInferenceFallback)
                ctx.tracker.ReportInferenceFallback(t.AsPseudoTypeDirect().TypeNode);
            var oldSuppress = ctx.suppressReportInferenceFallback;
            ctx.suppressReportInferenceFallback = true;
            var result = typeToTypeNode(checkerType);
            ctx.suppressReportInferenceFallback = oldSuppress;
            return result;
        }

        return pseudoTypeToNode(t);
    }

    private Node pseudoTypeToNode(PseudoType t)
    {
        Debug.Assert(t != null);
        switch (t.Kind)
        {
            case PseudoTypeKind.Direct:
                return reuseTypeNode(t.AsPseudoTypeDirect().TypeNode);
            case PseudoTypeKind.Inferred:
                var inferred = t.AsPseudoTypeInferred();
                var node = inferred.Expression;
                if (inferred.ErrorNodes is { Count: > 0 })
                    foreach (var n in inferred.ErrorNodes)
                        ctx.tracker.ReportInferenceFallback(n);
                else if (Ast.IsEntityNameExpression(node) && Ast.IsDeclaration(node.Parent))
                    ctx.tracker.ReportInferenceFallback(node.Parent);
                else
                    ctx.tracker.ReportInferenceFallback(node);
                if (inferred.IsSignatureReturn)
                    return serializeReturnTypeForSignature(ch.getSignatureFromDeclaration(node), false);
                if (Ast.IsReturnStatement(node.Parent))
                {
                    var enclosing = Ast.GetContainingFunction(node);
                    if (Ast.IsAccessor(enclosing))
                        return serializeTypeForDeclaration(enclosing, null, null, false);
                    return serializeReturnTypeForSignature(ch.getSignatureFromDeclaration(enclosing), false);
                }

                if (Ast.IsArrowFunction(node.Parent) && node.Parent.Body() == node)
                    return serializeReturnTypeForSignature(ch.getSignatureFromDeclaration(node.Parent), false);
                if (Ast.IsDeclaration(node.Parent))
                    return serializeTypeForDeclaration(node.Parent, null, null, false);
                return typeToTypeNode(ch.getTypeOfExpression(node));
            case PseudoTypeKind.NoResult:
                var decl = t.AsPseudoTypeNoResult().Declaration;
                ctx.tracker.ReportInferenceFallback(decl);
                if (Ast.IsFunctionLike(decl) && !Ast.IsAccessor(decl))
                    return serializeReturnTypeForSignature(ch.getSignatureFromDeclaration(decl), false);
                return serializeTypeForDeclaration(decl, null, null, false);
            case PseudoTypeKind.MaybeConstLocation:
                var d = t.AsPseudoTypeMaybeConstLocation();
                var isConst = ch.isConstContext(d.Node);
                if (!isConst && PseudoChecker.IsInConstContext(d.Node))
                {
                    var contextualType = ch.getContextualType(d.Node, ContextFlags.None);
                    var constType = pseudoTypeToType(d.ConstType);
                    if (constType != null && ch.isLiteralOfContextualType(constType,
                        ch.instantiateContextualType(contextualType, d.Node, ContextFlags.None)))
                        isConst = true;
                }
                return pseudoTypeToNode(isConst ? d.ConstType : d.RegularType);
            case PseudoTypeKind.Union:
                Slice<Node> res = default;
                bool hasElidedType = false, hasUndefined = false;
                void appendTypeNode(Node n)
                {
                    if (n.Kind == Kind.UnionType)
                    {
                        foreach (var child in n.AsUnionTypeNode().Types.Nodes)
                            appendTypeNode(child);
                        return;
                    }

                    if (n.Kind == Kind.UndefinedKeyword)
                    {
                        if (hasUndefined)
                            return;
                        hasUndefined = true;
                    }

                    res = res.Append(n);
                }

                foreach (var m in t.AsPseudoTypeUnion().Types)
                {
                    if (!ch.strictNullChecks && m.Kind is PseudoTypeKind.Undefined or PseudoTypeKind.Null)
                    {
                        hasElidedType = true;
                        continue;
                    }

                    appendTypeNode(pseudoTypeToNode(m));
                }

                if (res.Count == 1)
                    return res[0];
                if (res.Count == 0)
                    return f.NewKeywordTypeNode(hasElidedType ? Kind.AnyKeyword : Kind.NeverKeyword);
                return f.NewUnionTypeNode(f.NewNodeList(res));
            case PseudoTypeKind.Undefined:
                return f.NewKeywordTypeNode(ch.strictNullChecks ? Kind.UndefinedKeyword : Kind.AnyKeyword);
            case PseudoTypeKind.Null:
                return ch.strictNullChecks ? f.NewLiteralTypeNode(f.NewKeywordExpression(Kind.NullKeyword)) : f.NewKeywordTypeNode(Kind.AnyKeyword);
            case PseudoTypeKind.Any:
                return f.NewKeywordTypeNode(Kind.AnyKeyword);
            case PseudoTypeKind.String:
                return f.NewKeywordTypeNode(Kind.StringKeyword);
            case PseudoTypeKind.Number:
                return f.NewKeywordTypeNode(Kind.NumberKeyword);
            case PseudoTypeKind.BigInt:
                return f.NewKeywordTypeNode(Kind.BigIntKeyword);
            case PseudoTypeKind.Boolean:
                return f.NewKeywordTypeNode(Kind.BooleanKeyword);
            case PseudoTypeKind.False:
                return f.NewLiteralTypeNode(f.NewKeywordExpression(Kind.FalseKeyword));
            case PseudoTypeKind.True:
                return f.NewLiteralTypeNode(f.NewKeywordExpression(Kind.TrueKeyword));
            case PseudoTypeKind.SingleCallSignature:
                var single = t.AsPseudoTypeSingleCallSignature();
                var singleSignature = ch.getSignatureFromDeclaration(single.Signature);
                var expanded = ch.getExpandedParameters(singleSignature, true)[0];
                var cleanupSingle = enterNewScope(single.Signature, expanded, singleSignature.typeParameters, singleSignature.parameters, singleSignature.mapper);
                NodeList singleTypeParameters = null;
                if (GoRuntime.Len(single.TypeParameters) != 0)
                    singleTypeParameters = f.NewNodeList(Core.MapSlice(single.TypeParameters, parameter => reuseNode(parameter.AsNode())));
                var singleParameters = pseudoParametersToNodeList(single.Parameters);
                var singleReturn = pseudoTypeToNode(single.ReturnType);
                cleanupSingle();
                return f.NewFunctionTypeNode(singleTypeParameters, singleParameters, singleReturn);
            case PseudoTypeKind.Tuple:
                var tupleElements = Core.MapSlice(t.AsPseudoTypeTuple().Elements, pseudoTypeToNode);
                var tupleNode = f.NewTupleTypeNode(f.NewNodeList(tupleElements));
                e.AddEmitFlags(tupleNode, TypeScript.DiagnosticPrinting.EmitFlags.SingleLine);
                return f.NewTypeOperatorNode(Kind.ReadonlyKeyword, tupleNode);
            case PseudoTypeKind.ObjectLiteral:
                var elements = t.AsPseudoTypeObjectLiteral().Elements;
                if (elements is not { Count: > 0 })
                {
                    var emptyObject = f.NewTypeLiteralNode(f.NewNodeList(default(Slice<Node>)));
                    e.AddEmitFlags(emptyObject, TypeScript.DiagnosticPrinting.EmitFlags.SingleLine);
                    return emptyObject;
                }
                var isConstObject = ch.isConstContext(elements[0].Name.Parent.Parent);
                var newElements = Slice<Node>.Make(0,elements.Count);
                var restoreObjectFlags = saveRestoreFlags();
                ctx.flags |= NodeBuilderFlags.InObjectTypeLiteral;
                foreach (var e in elements)
                {
                    ModifierList modifiers = isConstObject || e.Kind == PseudoObjectElementKind.PropertyAssignment && e.AsPseudoPropertyAssignment().Readonly ? f.NewModifierList(Slice<Node>.FromArray([f.NewModifier(Kind.ReadonlyKeyword)])) : null;
                    Action cleanup = null;
                    if (e.Kind != PseudoObjectElementKind.PropertyAssignment)
                    {
                        var signature = ch.getSignatureFromDeclaration(e.Signature());
                        var paramsExpanded = ch.getExpandedParameters(signature, true)[0];
                        cleanup = enterNewScope(e.Signature(), paramsExpanded, signature.typeParameters, signature.parameters, signature.mapper);
                    }

                    Node newProperty;
                    switch (e.Kind)
                    {
                        case PseudoObjectElementKind.Method:
                            var method = e.AsPseudoObjectMethod();
                            NodeList methodTypeParameters = null;
                            if (GoRuntime.Len(method.TypeParameters) != 0)
                                methodTypeParameters = f.NewNodeList(Core.MapSlice(method.TypeParameters, parameter => reuseNode(parameter.AsNode())));
                            newProperty = isConstObject ? f.NewPropertySignatureDeclaration(modifiers, reuseName(e.Name, false), null, f.NewFunctionTypeNode(methodTypeParameters, pseudoParametersToNodeList(method.Parameters), pseudoTypeToNode(method.ReturnType)), null) : f.NewMethodSignatureDeclaration(modifiers, reuseName(e.Name, true), null, methodTypeParameters, pseudoParametersToNodeList(method.Parameters), pseudoTypeToNode(method.ReturnType));
                            break;
                        case PseudoObjectElementKind.PropertyAssignment:
                            var data = e.AsPseudoPropertyAssignment();
                            newProperty = f.NewPropertySignatureDeclaration(modifiers, reuseName(e.Name, false), null, pseudoTypeToNode(data.Type), null);
                            break;
                        case PseudoObjectElementKind.SetAccessor:
                            var setter = e.AsPseudoSetAccessor();
                            newProperty = f.NewSetAccessorDeclaration(null, reuseName(e.Name, false), null,
                                f.NewNodeList(Slice<Node>.FromArray([pseudoParameterToNode(setter.Parameter)])), null, null, null);
                            break;
                        case PseudoObjectElementKind.GetAccessor:
                            var getter = e.AsPseudoGetAccessor();
                            newProperty = f.NewGetAccessorDeclaration(null, reuseName(e.Name, false), null,
                                null, pseudoTypeToNode(getter.Type), null, null);
                            break;
                        default:
                            throw new InvalidOperationException("Unknown pseudo object element kind");
                    }

                    if (ctx.enclosingFile == Ast.GetSourceFileOfNode(e.Name))
                        this.e.SetCommentRange(newProperty, e.Name.Parent.Loc);
                    newElements = newElements.Append(newProperty);
                    cleanup?.Invoke();
                }

                restoreObjectFlags();
                var objectResult = f.NewTypeLiteralNode(f.NewNodeList(newElements));
                if ((ctx.flags & NodeBuilderFlags.MultilineObjectLiterals) == 0)
                    e.AddEmitFlags(objectResult, TypeScript.DiagnosticPrinting.EmitFlags.SingleLine);
                return objectResult;
            case PseudoTypeKind.StringLiteral:
            case PseudoTypeKind.NumericLiteral:
            case PseudoTypeKind.BigIntLiteral:
                return f.NewLiteralTypeNode(reuseNode(t.AsPseudoTypeLiteral().Node));
            default:
                throw new InvalidOperationException("Unhandled pseudotype kind in pseudotype node construction");
        }
    }

    private bool pseudoTypeEquivalentToType(PseudoType t, Type type_, bool isOptionalAnnotated, bool reportErrors)
    {
        if (type_ != null && ch.isErrorType(type_))
            return true;
        var typeFromPseudo = pseudoTypeToType(t);
        if (typeFromPseudo == type_)
            return true;
        var undefinedStripped = isOptionalAnnotated ? ch.getTypeWithFacts(type_, Checker.TypeFacts.NEUndefined) : type_;
        if (typeFromPseudo != null && type_ != null)
        {
            if (isOptionalAnnotated)
            {
                if (undefinedStripped == typeFromPseudo)
                    return true;
                if ((typeFromPseudo.flags & TypeFlags.Union) != 0 && (undefinedStripped.flags & TypeFlags.Union) != 0 && ch.compareTypesIdentical(typeFromPseudo, undefinedStripped) == Ternary.True)
                    return true;
            }

            if (ch.getRegularTypeOfLiteralType(typeFromPseudo) == ch.getRegularTypeOfLiteralType(type_))
                return true;
            if ((typeFromPseudo.flags & TypeFlags.Union) != 0 && (type_.flags & TypeFlags.Union) != 0 && ch.compareTypesIdentical(typeFromPseudo, type_) == Ternary.True)
                return true;
        }

        switch (t.Kind)
        {
            case PseudoTypeKind.Inferred:
                var inferred = t.AsPseudoTypeInferred();
                if (inferred.ErrorNodes is { Count: > 0 })
                {
                    if (reportErrors)
                        foreach (var n in inferred.ErrorNodes)
                            ctx.tracker.ReportInferenceFallback(n);
                    return false;
                }

                if (reportErrors)
                    ctx.tracker.ReportInferenceFallback(inferred.Expression);
                return false;
            case PseudoTypeKind.ObjectLiteral:
                if (type_ == null)
                    return false;
                var pt = t.AsPseudoTypeObjectLiteral();
                var props = ch.getPropertiesOfType(undefinedStripped);
                int declCount = 0;
                foreach (var prop in props)
                    declCount += prop.Declarations.Count;
                if ((pt.Elements?.Count ?? 0) != declCount)
                    return false;
                foreach (var e in pt.Elements ?? [])
                {
                    Symbol targetProp = null;
                    var elemSymbol = e.Name.Parent.Symbol();
                    if (elemSymbol != null)
                        targetProp = ch.getPropertyOfType(undefinedStripped, elemSymbol.NameValue);
                    if (targetProp == null)
                    {
                        foreach (var prop in props)
                            if (prop.ValueDeclaration != null && prop.ValueDeclaration.Name() == e.Name)
                            {
                                targetProp = prop;
                                break;
                            }

                        if (targetProp == null)
                        {
                            if (reportErrors)
                                ctx.tracker.ReportInferenceFallback(e.Name.Parent);
                            return false;
                        }
                    }

                    bool targetOptional = (targetProp.Flags & SymbolFlags.Optional) != 0;
                    if (e.Optional != targetOptional)
                    {
                        if (reportErrors)
                            ctx.tracker.ReportInferenceFallback(e.Name.Parent);
                        return false;
                    }

                    var propType = ch.removeMissingType(ch.getTypeOfSymbol(targetProp), targetOptional);
                    if (e.Kind == PseudoObjectElementKind.Method)
                    {
                        var method = e.AsPseudoObjectMethod();
                        var signature = ch.getSingleCallSignature(propType);
                        if (signature == null)
                            continue;
                        if (!pseudoParametersEquivalentToParameters(method.Parameters, signature, reportErrors, e.Name.Parent))
                            return false;
                        var predicate = ch.getTypePredicateOfSignature(signature);
                        if (predicate != null ? !pseudoReturnTypeMatchesPredicate(method.ReturnType, predicate) : !pseudoTypeEquivalentToType(method.ReturnType, ch.getReturnTypeOfSignature(signature), false, false))
                        {
                            if (reportErrors)
                                ctx.tracker.ReportInferenceFallback(e.Name.Parent);
                            return false;
                        }

                        continue;
                    }

                    if (e.Kind is PseudoObjectElementKind.GetAccessor or PseudoObjectElementKind.SetAccessor)
                    {
                        var accessorType = e.Kind == PseudoObjectElementKind.GetAccessor
                            ? e.AsPseudoGetAccessor().Type : e.AsPseudoSetAccessor().Parameter.Type;
                        var targetType = e.Kind == PseudoObjectElementKind.GetAccessor
                            ? propType : ch.getWriteTypeOfSymbol(targetProp);
                        if (!pseudoTypeEquivalentToType(accessorType, targetType, false, false))
                        {
                            if (reportErrors)
                                ctx.tracker.ReportInferenceFallback(e.Name.Parent);
                            return false;
                        }
                        continue;
                    }
                    var data = e.AsPseudoPropertyAssignment();
                    if (!pseudoTypeEquivalentToType(data.Type, propType, e.Optional, false))
                    {
                        if (reportErrors)
                        {
                            if (data.Type.Kind == PseudoTypeKind.Inferred && data.Type.AsPseudoTypeInferred().ErrorNodes is { Count: > 0 })
                                foreach (var n in data.Type.AsPseudoTypeInferred().ErrorNodes)
                                    ctx.tracker.ReportInferenceFallback(n);
                            else if (!isStructuralPseudoType(data.Type))
                                ctx.tracker.ReportInferenceFallback(e.Name.Parent);
                        }

                        return false;
                    }
                }

                return true;
            case PseudoTypeKind.NoResult:
                if (reportErrors)
                    ctx.tracker.ReportInferenceFallback(t.AsPseudoTypeNoResult().Declaration);
                return false;
            case PseudoTypeKind.SingleCallSignature:
                var targetSig = ch.getSingleCallSignature(undefinedStripped);
                if (targetSig == null)
                    return false;
                var signatureType = t.AsPseudoTypeSingleCallSignature();
                if (GoRuntime.Len(targetSig.typeParameters) != GoRuntime.Len(signatureType.TypeParameters))
                {
                    if (reportErrors)
                        ctx.tracker.ReportInferenceFallback(signatureType.Signature);
                    return false;
                }

                if (!pseudoParametersEquivalentToParameters(signatureType.Parameters, targetSig, reportErrors, signatureType.Signature))
                    return false;
                var targetPredicate = ch.getTypePredicateOfSignature(targetSig);
                if (targetPredicate != null)
                {
                    if (!pseudoReturnTypeMatchesPredicate(signatureType.ReturnType, targetPredicate))
                    {
                        if (reportErrors)
                            ctx.tracker.ReportInferenceFallback(signatureType.Signature);
                        return false;
                    }
                }
                else if (!pseudoTypeEquivalentToType(signatureType.ReturnType, ch.getReturnTypeOfSignature(targetSig), false, reportErrors))
                    return false;
                return true;
            case PseudoTypeKind.Tuple:
                if (undefinedStripped == null || !Checker.isTupleType(undefinedStripped))
                    return false;
                if ((undefinedStripped.TargetTupleType().combinedFlags & ElementFlags.NonRequired) != 0)
                    return false;
                var pseudoElements = t.AsPseudoTypeTuple().Elements;
                var elementTypes = ch.getTypeArguments(undefinedStripped);
                if (GoRuntime.Len(pseudoElements) != GoRuntime.Len(elementTypes))
                    return false;
                for (int i = 0; i < GoRuntime.Len(pseudoElements); i++)
                    if (!pseudoTypeEquivalentToType(pseudoElements[i], elementTypes[i], false, reportErrors))
                        return false;
                return true;
            default:
                return false;
        }
    }

    private static bool isStructuralPseudoType(PseudoType t) => t.Kind switch
    {
        PseudoTypeKind.ObjectLiteral or PseudoTypeKind.Tuple or PseudoTypeKind.SingleCallSignature => true,
        PseudoTypeKind.MaybeConstLocation => isStructuralPseudoType(t.AsPseudoTypeMaybeConstLocation().ConstType) || isStructuralPseudoType(t.AsPseudoTypeMaybeConstLocation().RegularType),
        _ => false
    };
    private Type pseudoTypeToType(PseudoType t)
    {
        Debug.Assert(t != null);
        switch (t.Kind)
        {
            case PseudoTypeKind.Direct:
                return ch.getTypeFromTypeNode(t.AsPseudoTypeDirect().TypeNode);
            case PseudoTypeKind.Inferred:
                if (t.AsPseudoTypeInferred().IsSignatureReturn)
                    return ch.getReturnTypeOfSignature(ch.getSignatureFromDeclaration(t.AsPseudoTypeInferred().Expression));
                return ch.getWidenedType(ch.getRegularTypeOfExpression(t.AsPseudoTypeInferred().Expression));
            case PseudoTypeKind.NoResult:
                return null;
            case PseudoTypeKind.MaybeConstLocation:
                var d = t.AsPseudoTypeMaybeConstLocation();
                return pseudoTypeToType(ch.isConstContext(d.Node) ? d.ConstType : d.RegularType);
            case PseudoTypeKind.Union:
                Slice<Type> res = default;
                bool hasElided = false;
                foreach (var m in t.AsPseudoTypeUnion().Types)
                {
                    if (!ch.strictNullChecks && m.Kind is PseudoTypeKind.Undefined or PseudoTypeKind.Null)
                    {
                        hasElided = true;
                        continue;
                    }

                    var type = pseudoTypeToType(m);
                    if (type == null)
                        return null;
                    res = res.Append(type);
                }

                if (res.Count == 1)
                    return res[0];
                if (res.Count == 0)
                    return hasElided ? ch.anyType : ch.neverType;
                return ch.getUnionType(res);
            case PseudoTypeKind.Undefined:
                return ch.undefinedWideningType;
            case PseudoTypeKind.Null:
                return ch.nullWideningType;
            case PseudoTypeKind.Any:
                return ch.anyType;
            case PseudoTypeKind.String:
                return ch.stringType;
            case PseudoTypeKind.Number:
                return ch.numberType;
            case PseudoTypeKind.BigInt:
                return ch.bigintType;
            case PseudoTypeKind.Boolean:
                return ch.booleanType;
            case PseudoTypeKind.False:
                return ch.falseType;
            case PseudoTypeKind.True:
                return ch.trueType;
            case PseudoTypeKind.StringLiteral:
            case PseudoTypeKind.NumericLiteral:
            case PseudoTypeKind.BigIntLiteral:
                return ch.getRegularTypeOfExpression(t.AsPseudoTypeLiteral().Node);
            case PseudoTypeKind.ObjectLiteral:
            case PseudoTypeKind.SingleCallSignature:
            case PseudoTypeKind.Tuple:
                return null;
            default:
                throw new InvalidOperationException("Unhandled pseudotype kind in pseudotype to type conversion");
        }
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/pseudotypenodebuilder.go:327:1.
    private NodeList pseudoParametersToNodeList(List<PseudoParameter> @params)
    {
        var res = Slice<Node>.Make(0, GoRuntime.Len(@params));
        {
            foreach (var p in GoRuntime.RangeValues(@params))
            {
                res = GoRuntime.Append(res, this.pseudoParameterToNode(p));
            }
        }

        return this.f.NewNodeList(res);
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/pseudotypenodebuilder.go:335:1.
    private Node pseudoParameterToNode(PseudoParameter p)
    {
        Node dotDotDot = default;
        Node questionMark = default;
        if (p.Rest)
        {
            dotDotDot = this.f.NewToken(Kind.DotDotDotToken);
        }

        if (p.Optional)
        {
            questionMark = this.f.NewToken(Kind.QuestionToken);
        }

        var parameter = this.f.NewParameterDeclaration(null, dotDotDot, this.parameterToParameterDeclarationName(p.Name.Parent.Symbol(), p.Name.Parent), questionMark, this.pseudoTypeToNode(p.Type), null);
        {
            var original = p.Name.Parent;
            if (Ast.IsParameterDeclaration(original))
            {
                setCommentRange(parameter, original);
            }
        }

        return parameter;
    }

    // Upstream upstream/TypeScript/tsc/internal/checker/pseudotypenodebuilder.go:585:1.
    private bool pseudoParametersEquivalentToParameters(List<PseudoParameter> @params, Signature targetSig, bool reportErrors, Node nonParamErrorLocation)
    {
        if (((targetSig.thisParameter != null) && (GoRuntime.Len(@params) == 0)))
        {
            if (reportErrors)
            {
                this.ctx.tracker.ReportInferenceFallback(nonParamErrorLocation);
            }

            return false;
        }
        else
        {
            if (((targetSig.thisParameter != null) && Ast.IsThisIdentifier(@params[0].Name)))
            {
                var targetParam = targetSig.thisParameter;
                var paramType = this.ch.getTypeOfParameter(targetParam);
                if (!this.pseudoTypeEquivalentToType(@params[0].Type, paramType, @params[0].Optional, false))
                {
                    if (reportErrors)
                    {
                        this.ctx.tracker.ReportInferenceFallback(@params[0].Name.Parent);
                    }

                    return false;
                }

                @params = GoRuntime.Slice(@params, 1);
            }
            else
            {
                if ((targetSig.thisParameter != null))
                {
                    if (reportErrors)
                    {
                        this.ctx.tracker.ReportInferenceFallback(nonParamErrorLocation);
                    }

                    return false;
                }
            }
        }

        if ((GoRuntime.Len(targetSig.parameters) != GoRuntime.Len(@params)))
        {
            if (reportErrors)
            {
                this.ctx.tracker.ReportInferenceFallback(nonParamErrorLocation);
            }

            return false;
        }

        {
            foreach (var range2 in GoRuntime.Range(@params))
            {
                var i = range2.Key;
                var p = range2.Value;
                var targetParam2 = targetSig.parameters[i];
                if ((p.Optional != this.ch.isOptionalParameter(targetParam2.ValueDeclaration)))
                {
                    if (reportErrors)
                    {
                        this.ctx.tracker.ReportInferenceFallback(p.Name.Parent);
                    }

                    return false;
                }

                var paramType2 = this.ch.getTypeOfParameter(targetParam2);
                if (!this.pseudoTypeEquivalentToType(p.Type, paramType2, p.Optional, false))
                {
                    if (reportErrors)
                    {
                        this.ctx.tracker.ReportInferenceFallback(p.Name.Parent);
                    }

                    return false;
                }
            }
        }

        return true;
    }

    // pseudoReturnTypeMatchesPredicate checks if a pseudo return type (which should be a Direct type
    // wrapping a TypePredicate) matches the given type predicate from the checker.
    // Upstream upstream/TypeScript/tsc/internal/checker/pseudotypenodebuilder.go:645:1.
    private bool pseudoReturnTypeMatchesPredicate(PseudoType rt, TypePredicate predicate)
    {
        if ((rt.Kind != PseudoTypeKind.Direct))
        {
            return false;
        }

        var node = rt.AsPseudoTypeDirect().TypeNode;
        if (!Ast.IsTypePredicateNode(node))
        {
            return false;
        }

        var tp = node.AsTypePredicateNode();
        var isAsserts = (tp.AssertsModifier != null);
        var predicateIsAsserts = ((predicate.kind == TypePredicateKind.AssertsThis) || (predicate.kind == TypePredicateKind.AssertsIdentifier));
        if ((isAsserts != predicateIsAsserts))
        {
            return false;
        }

        var isThis = Ast.IsThisTypeNode(tp.ParameterName);
        var predicateIsThis = ((predicate.kind == TypePredicateKind.This) || (predicate.kind == TypePredicateKind.AssertsThis));
        if ((isThis != predicateIsThis))
        {
            return false;
        }

        if (!isThis)
        {
            if ((!tp.ParameterName.TextValue().EqualsString(predicate.parameterName)))
            {
                return false;
            }
        }

        if ((predicate.t != null))
        {
            if ((tp.Type == null))
            {
                return false;
            }

            var predicateTypeFromNode = this.ch.getTypeFromTypeNode(tp.Type);
            if ((predicateTypeFromNode != predicate.t))
            {
                if ((this.ch.compareTypesIdentical(predicateTypeFromNode, predicate.t) != Ternary.True))
                {
                    return false;
                }
            }
        }
        else
        {
            if ((tp.Type != null))
            {
                return false;
            }
        }

        return true;
    }
}

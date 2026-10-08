// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/checker/pseudotypenodebuilder.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Diagnostic-only finite data-property/scalar context reuse; general emit excluded.
#nullable disable
using TypeScript.Scanner;
using TypeScript.PseudoChecking;

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
                if (inferred.IsSignatureReturn || node.Parent?.Kind is Kind.ReturnStatement or Kind.ArrowFunction)
                    throw Checker.unsupported("pseudoTypeToNode: callable inference");
                if (Ast.IsDeclaration(node.Parent))
                    return serializeTypeForDeclaration(node.Parent, null, null, false);
                return typeToTypeNode(ch.getTypeOfExpression(node));
            case PseudoTypeKind.NoResult:
                var decl = t.AsPseudoTypeNoResult().Declaration;
                ctx.tracker.ReportInferenceFallback(decl);
                if (Ast.IsFunctionLike(decl))
                    throw Checker.unsupported("pseudoTypeToNode: callable declaration");
                return serializeTypeForDeclaration(decl, null, null, false);
            case PseudoTypeKind.MaybeConstLocation:
                var d = t.AsPseudoTypeMaybeConstLocation();
                var isConst = ch.isConstContext(d.Node);
                if (!isConst && PseudoChecker.IsInConstContext(d.Node))
                    throw Checker.unsupported("pseudoTypeToNode: assertion/const generic context");
                return pseudoTypeToNode(isConst ? d.ConstType : d.RegularType);
            case PseudoTypeKind.Union:
                List<Node> res = new();
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

                    res.Add(n);
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
            case PseudoTypeKind.ObjectLiteral:
                var elements = t.AsPseudoTypeObjectLiteral().Elements;
                if (elements is not { Count: > 0 })
                    return f.NewTypeLiteralNode(f.NewNodeList(null));
                var isConstObject = ch.isConstContext(elements[0].Name.Parent.Parent);
                List<Node> newElements = new(elements.Count);
                var savedObjectFlag = inObjectTypeLiteral;
                inObjectTypeLiteral = true;
                foreach (var e in elements)
                {
                    if (e.Kind != PseudoObjectElementKind.PropertyAssignment)
                        throw Checker.unsupported("pseudoTypeToNode: object method/accessor");
                    var data = e.AsPseudoPropertyAssignment();
                    ModifierList modifiers = isConstObject || data.Readonly ? f.NewModifierList([f.NewModifier(Kind.ReadonlyKeyword)]) : null;
                    newElements.Add(f.NewPropertySignatureDeclaration(modifiers, reuseName(e.Name, false), null, pseudoTypeToNode(data.Type), null));
                }

                inObjectTypeLiteral = savedObjectFlag;
                return f.NewTypeLiteralNode(f.NewNodeList(newElements));
            case PseudoTypeKind.StringLiteral:
            case PseudoTypeKind.NumericLiteral:
            case PseudoTypeKind.BigIntLiteral:
                return f.NewLiteralTypeNode(reuseNode(t.AsPseudoTypeLiteral().Node));
            default:
                throw Checker.unsupported("pseudoTypeToNode: tuple/signature payload");
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
                foreach (var prop in props ?? [])
                    declCount += prop.Declarations?.Count ?? 0;
                if ((pt.Elements?.Count ?? 0) != declCount)
                    return false;
                foreach (var e in pt.Elements ?? [])
                {
                    Symbol targetProp = null;
                    var elemSymbol = e.Name.Parent.Symbol();
                    if (elemSymbol != null)
                        targetProp = ch.getPropertyOfType(undefinedStripped, elemSymbol.Name);
                    if (targetProp == null)
                    {
                        foreach (var prop in props ?? [])
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
                    if (e.Kind != PseudoObjectElementKind.PropertyAssignment)
                        throw Checker.unsupported("pseudoTypeEquivalentToType: method/accessor");
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
            case PseudoTypeKind.Tuple:
            case PseudoTypeKind.SingleCallSignature:
                throw Checker.unsupported("pseudoTypeEquivalentToType: tuple/signature");
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
                    throw Checker.unsupported("pseudoTypeToType: signature return");
                return ch.getWidenedType(ch.getRegularTypeOfExpression(t.AsPseudoTypeInferred().Expression));
            case PseudoTypeKind.NoResult:
                return null;
            case PseudoTypeKind.MaybeConstLocation:
                var d = t.AsPseudoTypeMaybeConstLocation();
                return pseudoTypeToType(ch.isConstContext(d.Node) ? d.ConstType : d.RegularType);
            case PseudoTypeKind.Union:
                List<Type> res = new();
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
                    res.Add(type);
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
                return null;
            default:
                throw Checker.unsupported("pseudoTypeToType: tuple/signature");
        }
    }
}

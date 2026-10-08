// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/pseudochecker/lookup.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Diagnostic-only data-property/scalar reuse; callable/tuple/emit paths excluded.
#nullable disable
using TypeScript.Scanner;

namespace TypeScript.PseudoChecking;
using static PseudoType;
using static PseudoObjectElement;

internal sealed partial class PseudoChecker
{
    internal PseudoType GetTypeOfExpression(Node node) => typeFromExpression(node);
    internal PseudoType GetTypeOfDeclaration(Node node)
    {
        switch (node.Kind)
        {
            case Kind.PropertySignature:
                return typeFromProperty(node);
            case Kind.PropertyAssignment:
            case Kind.ShorthandPropertyAssignment:
                return typeFromPropertyAssignment(node);
            default:
                throw TypeScript.Checking.Checker.unsupported("PseudoChecker.GetTypeOfDeclaration: unselected declaration");
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
        if (node.Kind != Kind.PropertySignature)
            throw TypeScript.Checking.Checker.unsupported("PseudoChecker.typeFromProperty: class/JS");
        return NewPseudoTypeNoResult(node);
    }

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
            case Kind.TypeAssertionExpression:
            case Kind.AsExpression:
            case Kind.ArrayLiteralExpression:
            case Kind.ClassExpression:
            case Kind.TemplateExpression:
                throw TypeScript.Checking.Checker.unsupported("PseudoChecker.typeFromExpression: callable/asserted/array/template expression");
        }

        return NewPseudoTypeInferred(node, false);
    }

    private PseudoType typeFromObjectLiteral(ObjectLiteralExpression node)
    {
        var errors = canGetTypeFromObjectLiteral(node);
        if (errors != null)
            return NewPseudoTypeInferredWithErrors(node.AsNode(), false, errors);
        if (node.Properties == null || (node.Properties.Nodes?.Count ?? 0) == 0)
            return NewPseudoTypeObjectLiteral(null);
        var elements = new List<PseudoObjectElement>(node.Properties.Nodes.Count);
        foreach (var element in node.Properties.Nodes)
        {
            if (element.Kind != Kind.PropertyAssignment)
                throw TypeScript.Checking.Checker.unsupported("PseudoChecker.typeFromObjectLiteral: callable/accessor");
            elements.Add(NewPseudoPropertyAssignment(false, element.Name(), element.AsPropertyAssignment().PostfixToken?.Kind == Kind.QuestionToken, typeFromExpression(element.Initializer())));
        }

        return NewPseudoTypeObjectLiteral(elements);
    }

    private List<Node> canGetTypeFromObjectLiteral(ObjectLiteralExpression node)
    {
        if (node.Properties == null || (node.Properties.Nodes?.Count ?? 0) == 0)
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

            if (e.Name().Kind == Kind.ComputedPropertyName)
                throw TypeScript.Checking.Checker.unsupported("PseudoChecker.canGetTypeFromObjectLiteral: computed name");
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
}

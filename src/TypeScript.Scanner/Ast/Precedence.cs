// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/precedence.go; complete expression and type syntax precedence helpers.
#nullable disable
namespace TypeScript.Scanner;
public static partial class Ast
{
    // Upstream function: GetBinaryOperatorPrecedence
    public static OperatorPrecedence GetBinaryOperatorPrecedence(Kind operatorKind)
    {
        switch (operatorKind)
        {
            case Kind.QuestionQuestionToken:
            {
                return OperatorPrecedence.Coalesce;
            }

            case Kind.BarBarToken:
            {
                return OperatorPrecedence.LogicalOR;
            }

            case Kind.AmpersandAmpersandToken:
            {
                return OperatorPrecedence.LogicalAND;
            }

            case Kind.BarToken:
            {
                return OperatorPrecedence.BitwiseOR;
            }

            case Kind.CaretToken:
            {
                return OperatorPrecedence.BitwiseXOR;
            }

            case Kind.AmpersandToken:
            {
                return OperatorPrecedence.BitwiseAND;
            }

            case Kind.EqualsEqualsToken:
            case Kind.ExclamationEqualsToken:
            case Kind.EqualsEqualsEqualsToken:
            case Kind.ExclamationEqualsEqualsToken:
            {
                return OperatorPrecedence.Equality;
            }

            case Kind.LessThanToken:
            case Kind.GreaterThanToken:
            case Kind.LessThanEqualsToken:
            case Kind.GreaterThanEqualsToken:
            case Kind.InstanceOfKeyword:
            case Kind.InKeyword:
            case Kind.AsKeyword:
            case Kind.SatisfiesKeyword:
            {
                return OperatorPrecedence.Relational;
            }

            case Kind.LessThanLessThanToken:
            case Kind.GreaterThanGreaterThanToken:
            case Kind.GreaterThanGreaterThanGreaterThanToken:
            {
                return OperatorPrecedence.Shift;
            }

            case Kind.PlusToken:
            case Kind.MinusToken:
            {
                return OperatorPrecedence.Additive;
            }

            case Kind.AsteriskToken:
            case Kind.SlashToken:
            case Kind.PercentToken:
            {
                return OperatorPrecedence.Multiplicative;
            }

            case Kind.AsteriskAsteriskToken:
            {
                return OperatorPrecedence.Exponentiation;
            }
        }

        return OperatorPrecedence.Invalid;
    }
}

public enum OperatorPrecedence : int
{
    Comma = 0,
    Spread = 1,
    Yield = 2,
    Assignment = 3,
    Conditional = 4,
    LogicalOR = 5,
    LogicalAND = 6,
    BitwiseOR = 7,
    BitwiseXOR = 8,
    BitwiseAND = 9,
    Equality = 10,
    Relational = 11,
    Shift = 12,
    Additive = 13,
    Multiplicative = 14,
    Exponentiation = 15,
    Unary = 16,
    Update = 17,
    LeftHandSide = 18,
    OptionalChain = 19,
    Member = 20,
    Primary = 21,
    Parentheses = 22,
    Lowest = Comma,
    Highest = Parentheses,
    DisallowComma = Yield,
    Coalesce = LogicalOR,
    Invalid = -1,
}

// precedence.go:403/648. Exact numeric type precedence, without grammar production comments.
public enum TypePrecedence
{
    Conditional = 0,
    JSDoc = 1,
    Function = 2,
    Union = 3,
    Intersection = 4,
    TypeOperator = 5,
    Postfix = 6,
    NonArray = 7,
    Lowest = Conditional,
    Highest = NonArray,
}

public static partial class Ast
{
    // precedence.go:655. Syntax classification is independent of semantic admission.
    public static TypePrecedence GetTypeNodePrecedence(Node n)
    {
        switch (n.Kind)
        {
            case Kind.ConditionalType:
                return TypePrecedence.Conditional;
            case Kind.JSDocOptionalType:
            case Kind.JSDocVariadicType:
                return TypePrecedence.JSDoc;
            case Kind.FunctionType:
            case Kind.ConstructorType:
                return TypePrecedence.Function;
            case Kind.UnionType:
                return TypePrecedence.Union;
            case Kind.IntersectionType:
                return TypePrecedence.Intersection;
            case Kind.TypeOperator:
                return TypePrecedence.TypeOperator;
            case Kind.InferType:
                if (n.AsInferTypeNode().TypeParameter.AsTypeParameterDeclaration().Constraint != null)
                    return TypePrecedence.Function;
                return TypePrecedence.TypeOperator;
            case Kind.IndexedAccessType:
            case Kind.ArrayType:
            case Kind.OptionalType:
                return TypePrecedence.Postfix;
            case Kind.TypeQuery:
                return TypePrecedence.TypeOperator;
            case Kind.AnyKeyword:
            case Kind.UnknownKeyword:
            case Kind.StringKeyword:
            case Kind.NumberKeyword:
            case Kind.BigIntKeyword:
            case Kind.SymbolKeyword:
            case Kind.BooleanKeyword:
            case Kind.UndefinedKeyword:
            case Kind.NeverKeyword:
            case Kind.ObjectKeyword:
            case Kind.IntrinsicKeyword:
            case Kind.VoidKeyword:
            case Kind.JSDocAllType:
            case Kind.JSDocNullableType:
            case Kind.JSDocNonNullableType:
            case Kind.LiteralType:
            case Kind.TypePredicate:
            case Kind.TypeReference:
            case Kind.TypeLiteral:
            case Kind.TupleType:
            case Kind.RestType:
            case Kind.ParenthesizedType:
            case Kind.ThisType:
            case Kind.MappedType:
            case Kind.NamedTupleMember:
            case Kind.TemplateLiteralType:
            case Kind.ImportType:
            case Kind.PropertyAccessExpression:
            case Kind.ExpressionWithTypeArguments:
                return TypePrecedence.NonArray;
            default:
                throw new InvalidOperationException($"unhandled TypeNode: {n.Kind}");
        }
    }
}

// precedence.go:214. Flags describe syntax, independently of semantic checking.
[Flags]
public enum OperatorPrecedenceFlags
{
    None = 0,
    NewWithoutArguments = 1 << 0,
    OptionalChain = 1 << 1,
}

public static partial class Ast
{
    // Upstream function: getOperator, precedence.go:189.
    private static Kind getOperator(Node expression)
    {
        switch (expression.Kind)
        {
            case Kind.BinaryExpression:
                return expression.AsBinaryExpression().OperatorToken.Kind;
            case Kind.PrefixUnaryExpression:
                return expression.AsPrefixUnaryExpression().Operator;
            case Kind.PostfixUnaryExpression:
                return expression.AsPostfixUnaryExpression().Operator;
            default:
                return expression.Kind;
        }
    }

    // Upstream function: GetExpressionPrecedence, precedence.go:203.
    public static OperatorPrecedence GetExpressionPrecedence(Node expression)
    {
        var op = getOperator(expression);
        var flags = OperatorPrecedenceFlags.None;
        if (expression.Kind == Kind.NewExpression && expression.ArgumentList() == null)
            flags = OperatorPrecedenceFlags.NewWithoutArguments;
        else if (IsOptionalChain(expression))
            flags = OperatorPrecedenceFlags.OptionalChain;
        return GetOperatorPrecedence(expression.Kind, op, flags);
    }

    // Upstream function: GetOperatorPrecedence, precedence.go:223.
    public static OperatorPrecedence GetOperatorPrecedence(Kind nodeKind, Kind operatorKind, OperatorPrecedenceFlags flags)
    {
        switch (nodeKind)
        {
            case Kind.SpreadElement:
                return OperatorPrecedence.Spread;
            case Kind.YieldExpression:
                return OperatorPrecedence.Yield;
            // !!! By necessity, this differs from the old compiler to better align with ParenthesizerRules. consider backporting
            case Kind.ArrowFunction:
                return OperatorPrecedence.Assignment;
            case Kind.ConditionalExpression:
                return OperatorPrecedence.Conditional;
            case Kind.BinaryExpression:
                switch (operatorKind)
                {
                    case Kind.CommaToken:
                        return OperatorPrecedence.Comma;
                    case Kind.EqualsToken:
                    case Kind.PlusEqualsToken:
                    case Kind.MinusEqualsToken:
                    case Kind.AsteriskAsteriskEqualsToken:
                    case Kind.AsteriskEqualsToken:
                    case Kind.SlashEqualsToken:
                    case Kind.PercentEqualsToken:
                    case Kind.LessThanLessThanEqualsToken:
                    case Kind.GreaterThanGreaterThanEqualsToken:
                    case Kind.GreaterThanGreaterThanGreaterThanEqualsToken:
                    case Kind.AmpersandEqualsToken:
                    case Kind.CaretEqualsToken:
                    case Kind.BarEqualsToken:
                    case Kind.BarBarEqualsToken:
                    case Kind.AmpersandAmpersandEqualsToken:
                    case Kind.QuestionQuestionEqualsToken:
                        return OperatorPrecedence.Assignment;
                    default:
                        return GetBinaryOperatorPrecedence(operatorKind);
                }

            // TODO: Should prefix `++` and `--` be moved to the `Update` precedence?
            case Kind.TypeAssertionExpression:
            case Kind.NonNullExpression:
            case Kind.PrefixUnaryExpression:
            case Kind.TypeOfExpression:
            case Kind.VoidExpression:
            case Kind.DeleteExpression:
            case Kind.AwaitExpression:
                return OperatorPrecedence.Unary;
            case Kind.PostfixUnaryExpression:
                return OperatorPrecedence.Update;
            // !!! By necessity, this differs from the old compiler to better align with ParenthesizerRules. consider backporting
            case Kind.PropertyAccessExpression:
            case Kind.ElementAccessExpression:
                if ((flags & OperatorPrecedenceFlags.OptionalChain) != 0)
                {
                    return OperatorPrecedence.OptionalChain;
                }

                return OperatorPrecedence.Member;
            case Kind.CallExpression:
                if ((flags & OperatorPrecedenceFlags.OptionalChain) != 0)
                {
                    return OperatorPrecedence.OptionalChain;
                }

                return OperatorPrecedence.Member;
            // !!! By necessity, this differs from the old compiler to better align with ParenthesizerRules. consider backporting
            case Kind.NewExpression:
                if ((flags & OperatorPrecedenceFlags.NewWithoutArguments) != 0)
                {
                    return OperatorPrecedence.LeftHandSide;
                }

                return OperatorPrecedence.Member;
            // !!! By necessity, this differs from the old compiler to better align with ParenthesizerRules. consider backporting
            case Kind.TaggedTemplateExpression:
            case Kind.MetaProperty:
            case Kind.ExpressionWithTypeArguments:
                return OperatorPrecedence.Member;
            case Kind.AsExpression:
            case Kind.SatisfiesExpression:
                return OperatorPrecedence.Relational;
            case Kind.ThisKeyword:
            case Kind.SuperKeyword:
            case Kind.ImportKeyword:
            case Kind.Identifier:
            case Kind.PrivateIdentifier:
            case Kind.NullKeyword:
            case Kind.TrueKeyword:
            case Kind.FalseKeyword:
            case Kind.NumericLiteral:
            case Kind.BigIntLiteral:
            case Kind.StringLiteral:
            case Kind.ArrayLiteralExpression:
            case Kind.ObjectLiteralExpression:
            case Kind.FunctionExpression:
            case Kind.ClassExpression:
            case Kind.RegularExpressionLiteral:
            case Kind.NoSubstitutionTemplateLiteral:
            case Kind.TemplateExpression:
            case Kind.OmittedExpression:
            case Kind.JsxElement:
            case Kind.JsxSelfClosingElement:
            case Kind.JsxFragment:
            case Kind.MissingDeclaration:
                return OperatorPrecedence.Primary;
            // !!! By necessity, this differs from the old compiler to support emit. consider backporting
            case Kind.ParenthesizedExpression:
                return OperatorPrecedence.Parentheses;
            default:
                return OperatorPrecedence.Invalid;
        }
    }

    // Upstream function: GetLeftmostExpression, precedence.go:370.
    public static Node GetLeftmostExpression(Node node, bool stopAtCallExpressions)
    {
        while (true)
        {
            switch (node.Kind)
            {
                case Kind.PostfixUnaryExpression:
                    node = node.AsPostfixUnaryExpression().Operand;
                    continue;
                case Kind.BinaryExpression:
                    node = node.AsBinaryExpression().Left;
                    continue;
                case Kind.ConditionalExpression:
                    node = node.AsConditionalExpression().Condition;
                    continue;
                case Kind.TaggedTemplateExpression:
                    node = node.AsTaggedTemplateExpression().Tag;
                    continue;
                case Kind.CallExpression:
                    if (stopAtCallExpressions)
                        return node;
                    goto case Kind.AsExpression;
                case Kind.AsExpression:
                case Kind.ElementAccessExpression:
                case Kind.PropertyAccessExpression:
                case Kind.NonNullExpression:
                case Kind.PartiallyEmittedExpression:
                case Kind.SatisfiesExpression:
                    node = node.Expression();
                    continue;
            }

            return node;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/precedence.go; parser-required syntax and diagnostic type-precedence dependency slice.
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

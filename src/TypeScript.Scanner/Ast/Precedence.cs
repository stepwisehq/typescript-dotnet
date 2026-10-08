// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/ast/precedence.go; parser-required syntax dependency slice.
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

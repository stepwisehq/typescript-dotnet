// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified partial C# port of tsc/internal/evaluator/evaluator.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Original selected numeric/string/entity evaluation for checker diagnostics.
// Written template interpolation and element access remain guarded.
#nullable disable
namespace TypeScript.Scanner;
internal static class Evaluator
{
    internal readonly record struct Result(object Value, bool IsSyntacticallyString, bool ResolvedOtherFiles, bool HasExternalReferences);
    internal delegate Result EvaluatorDelegate(Node expr, Node location);
    internal static Result NewResult(object value, bool isSyntacticallyString, bool resolvedOtherFiles, bool hasExternalReferences) => new(value, isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
    internal static EvaluatorDelegate NewEvaluator(EvaluatorDelegate evaluateEntity, OuterExpressionKinds outerExpressionsToSkip)
    {
        EvaluatorDelegate evaluate = null;
        evaluate = (expr, location) =>
        {
            bool isSyntacticallyString = false, resolvedOtherFiles = false, hasExternalReferences = false;
            // As upstream, the caller chooses which assertions/outer nodes to
            // skip; parentheses are always skipped. Do not evaluate extra syntax.
            expr = Ast.SkipOuterExpressions(expr, outerExpressionsToSkip | OuterExpressionKinds.Parentheses);
            switch (expr.Kind)
            {
                case Kind.PrefixUnaryExpression:
                    var result = evaluate(expr.AsPrefixUnaryExpression().Operand, location);
                    resolvedOtherFiles = result.ResolvedOtherFiles;
                    hasExternalReferences = result.HasExternalReferences;
                    if (result.Value is JsNum.Number value)
                    {
                        switch (expr.AsPrefixUnaryExpression().Operator)
                        {
                            case Kind.PlusToken:
                                return new(value, isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.MinusToken:
                                return new(-value, isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.TildeToken:
                                return new(value.BitwiseNOT(), isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                        }
                    }

                    break;
                case Kind.BinaryExpression:
                    var left = evaluate(expr.AsBinaryExpression().Left, location);
                    var right = evaluate(expr.AsBinaryExpression().Right, location);
                    var op = expr.AsBinaryExpression().OperatorToken.Kind;
                    isSyntacticallyString = (left.IsSyntacticallyString || right.IsSyntacticallyString) && op == Kind.PlusToken;
                    resolvedOtherFiles = left.ResolvedOtherFiles || right.ResolvedOtherFiles;
                    hasExternalReferences = left.HasExternalReferences || right.HasExternalReferences;
                    if (left.Value is JsNum.Number leftNum && right.Value is JsNum.Number rightNum)
                    {
                        switch (op)
                        {
                            case Kind.BarToken:
                                return new(leftNum.BitwiseOR(rightNum), isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.AmpersandToken:
                                return new(leftNum.BitwiseAND(rightNum), isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.GreaterThanGreaterThanToken:
                                return new(leftNum.SignedRightShift(rightNum), isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.GreaterThanGreaterThanGreaterThanToken:
                                return new(leftNum.UnsignedRightShift(rightNum), isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.LessThanLessThanToken:
                                return new(leftNum.LeftShift(rightNum), isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.CaretToken:
                                return new(leftNum.BitwiseXOR(rightNum), isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.AsteriskToken:
                                return new(leftNum * rightNum, isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.SlashToken:
                                return new(leftNum / rightNum, isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.PlusToken:
                                return new(leftNum + rightNum, isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.MinusToken:
                                return new(leftNum - rightNum, isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.PercentToken:
                                return new(leftNum.Remainder(rightNum), isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                            case Kind.AsteriskAsteriskToken:
                                return new(leftNum.Exponentiate(rightNum), isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                        }
                    }

                    if ((left.Value is string || left.Value is JsNum.Number) && (right.Value is string || right.Value is JsNum.Number) && op == Kind.PlusToken)
                    {
                        string leftStr = left.Value is JsNum.Number n1 ? n1.String() : (string)left.Value;
                        string rightStr = right.Value is JsNum.Number n2 ? n2.String() : (string)right.Value;
                        return new(leftStr + rightStr, isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                    }

                    break;
                case Kind.StringLiteral:
                case Kind.NoSubstitutionTemplateLiteral:
                    return new(expr.Text(), true, false, false);
                case Kind.TemplateExpression:
                    throw TypeScript.Checking.Checker.unsupported("Evaluator: source template interpolation");
                case Kind.NumericLiteral:
                    return new(JsNum.FromString(expr.Text()), false, false, false);
                case Kind.Identifier:
                    return evaluateEntity(expr, location);
                case Kind.ElementAccessExpression:
                case Kind.PropertyAccessExpression:
                    if (Ast.IsEntityNameExpression(expr.Expression()))
                        return evaluateEntity(expr, location);
                    break;
            }

            return new(null, isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
        };
        return evaluate;
    }

    internal static string AnyToString(object value) => value switch
    {
        string text => text,
        JsNum.Number number => number.String(),
        bool boolean => boolean ? "true" : "false",
        JsNum.PseudoBigInt bigint => bigint.String(),
        _ => throw new InvalidOperationException("Unhandled case in AnyToString")};
    internal static bool IsTruthy(object value) => value switch
    {
        string text => text.Length != 0,
        JsNum.Number number => number.Value != 0 && !number.IsNaN(),
        bool boolean => boolean,
        JsNum.PseudoBigInt bigint => bigint != new JsNum.PseudoBigInt(),
        _ => throw new InvalidOperationException("Unhandled case in IsTruthy")};
}

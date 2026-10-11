// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/evaluator/evaluator.go, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Constant expression evaluation delegates entity references to the source caller.
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

                    bool leftIsString = TryGetStringValue(left.Value, out var leftText);
                    bool rightIsString = TryGetStringValue(right.Value, out var rightText);
                    if ((leftIsString || left.Value is JsNum.Number) && (rightIsString || right.Value is JsNum.Number) && op == Kind.PlusToken)
                    {
                        if (left.Value is JsNum.Number n1) leftText = ByteStringValue.FromString(n1.String());
                        if (right.Value is JsNum.Number n2) rightText = ByteStringValue.FromString(n2.String());
                        // evaluator.go:102: fixed concatenation owns changed output
                        // and can reuse an operand when the other one is empty.
                        return new(ByteStringValue.Concat(leftText, rightText), isSyntacticallyString, resolvedOtherFiles, hasExternalReferences);
                    }

                    break;
                case Kind.StringLiteral:
                case Kind.NoSubstitutionTemplateLiteral:
                    var literalText = expr.TextValue();
                    return new(literalText.IsNull ? null : literalText, true, false, false);
                case Kind.TemplateExpression:
                    return evaluateTemplateExpression(expr, location, evaluate);
                case Kind.NumericLiteral:
                    return new(JsNum.FromString(expr.TextValue()), false, false, false);
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

    // evaluator.go:124. A nonconstant substitution discards accumulated reference flags, as upstream.
    private static Result evaluateTemplateExpression(Node expr, Node location, EvaluatorDelegate evaluate)
    {
        var text = new GoBuilder();
        text.WriteString(expr.AsTemplateExpression().Head.TextValue());
        var resolvedOtherFiles = false;
        var hasExternalReferences = false;
        foreach (var span in expr.AsTemplateExpression().TemplateSpans.Nodes)
        {
            var spanResult = evaluate(span.Expression(), location);
            if (spanResult.Value == null)
                return new(null, true, false, false);
            text.WriteString(AnyToStringValue(spanResult.Value));
            text.WriteString(span.AsTemplateSpan().Literal.TextValue());
            resolvedOtherFiles |= spanResult.ResolvedOtherFiles;
            hasExternalReferences |= spanResult.HasExternalReferences;
        }
        return new(text.Value(), true, resolvedOtherFiles, hasExternalReferences);
    }

    // A Go string escaping into Result.Value remains one boxed header. Legacy
    // raw-string inputs share this exact content path; supplied null is not a string.
    internal static bool TryGetStringValue(object value, out ByteStringValue text)
    {
        switch (value)
        {
            case ByteStringValue borrowed when !borrowed.IsNull:
                text = borrowed;
                return true;
            case string owned:
                text = ByteStringValue.FromString(owned);
                return true;
            default:
                text = default;
                return false;
        }
    }
    internal static bool IsString(object value) => TryGetStringValue(value, out _);
    // evaluator.go:144. Only actual numeric/bool/bigint formatting owns text;
    // string values already have retained byte content and require no projection.
    internal static ByteStringValue AnyToStringValue(object value) => value switch
    {
        ByteStringValue text when !text.IsNull => text,
        string text => ByteStringValue.FromString(text),
        JsNum.Number number => ByteStringValue.FromString(number.String()),
        bool boolean => ByteStringValue.FromString(boolean ? "true" : "false"),
        JsNum.PseudoBigInt bigint => ByteStringValue.FromString(bigint.String()),
        _ => throw new InvalidOperationException("Unhandled case in AnyToString")};
    internal static string AnyToString(object value) => AnyToStringValue(value).Materialize();
    internal static bool IsTruthy(object value) => value switch
    {
        ByteStringValue text when !text.IsNull => text.Length != 0,
        string text => text.Length != 0,
        JsNum.Number number => number.Value != 0 && !number.IsNaN(),
        bool boolean => boolean,
        JsNum.PseudoBigInt bigint => bigint != new JsNum.PseudoBigInt(),
        _ => throw new InvalidOperationException("Unhandled case in IsTruthy")};
}

// Original binary parenthesization tables and helper algorithms.
#nullable disable
using TypeScript.Scanner;
using AstFactory = TypeScript.Scanner.NodeFactory;
namespace Emit.Checks;
public static partial class PrinterChecks
{
    private static bool IsBinaryOperator(Kind token) => token is Kind.CommaToken or Kind.LessThanToken or Kind.GreaterThanToken or Kind.LessThanEqualsToken or Kind.GreaterThanEqualsToken or Kind.EqualsEqualsToken or Kind.EqualsEqualsEqualsToken or Kind.ExclamationEqualsToken or Kind.ExclamationEqualsEqualsToken or Kind.PlusToken or Kind.MinusToken or Kind.AsteriskToken or Kind.AsteriskAsteriskToken or Kind.SlashToken or Kind.PercentToken or Kind.LessThanLessThanToken or Kind.GreaterThanGreaterThanToken or Kind.GreaterThanGreaterThanGreaterThanToken or Kind.AmpersandToken or Kind.BarToken or Kind.CaretToken or Kind.AmpersandAmpersandToken or Kind.BarBarToken or Kind.QuestionQuestionToken or Kind.EqualsToken or Kind.PlusEqualsToken or Kind.MinusEqualsToken or Kind.AsteriskEqualsToken or Kind.AsteriskAsteriskEqualsToken or Kind.SlashEqualsToken or Kind.PercentEqualsToken or Kind.LessThanLessThanEqualsToken or Kind.GreaterThanGreaterThanEqualsToken or Kind.GreaterThanGreaterThanGreaterThanEqualsToken or Kind.AmpersandEqualsToken or Kind.BarEqualsToken or Kind.BarBarEqualsToken or Kind.AmpersandAmpersandEqualsToken or Kind.QuestionQuestionEqualsToken or Kind.CaretEqualsToken or Kind.InKeyword or Kind.InstanceOfKeyword;
    private static Node MakeSide(string label, Kind kind, AstFactory factory)
    {
        if (kind == Kind.Identifier || kind == Kind.Unknown) return factory.NewIdentifier(label);
        if (kind == Kind.ArrowFunction) return factory.NewArrowFunction(null, null,
            factory.NewNodeList(new List<Node>()), null, null, factory.NewToken(Kind.EqualsGreaterThanToken),
            factory.NewBlock(factory.NewNodeList(new List<Node>()), false));
        if (IsBinaryOperator(kind)) return factory.NewBinaryExpression(null, factory.NewIdentifier(label+"l"),
            null, factory.NewToken(kind), factory.NewIdentifier(label+"r"));
        throw new InvalidOperationException("unsupported kind");
    }
    private readonly record struct BinaryCase(Kind left,Kind op,Kind right,string output);
    private static void TestParenthesizeBinary()
    {
        BinaryCase[] data = [
            new(Kind.Unknown, Kind.CommaToken, Kind.Unknown, "l, r"),
            new(Kind.PlusToken, Kind.CommaToken, Kind.Unknown, "ll + lr, r"),
            new(Kind.PlusToken, Kind.AsteriskToken, Kind.Unknown, "(ll + lr) * r"),
            new(Kind.Unknown, Kind.AsteriskToken, Kind.PlusToken, "l * (rl + rr)"),
            new(Kind.AsteriskToken, Kind.PlusToken, Kind.Unknown, "ll * lr + r"),
            new(Kind.Unknown, Kind.PlusToken, Kind.AsteriskToken, "l + rl * rr"),
            new(Kind.AsteriskToken, Kind.SlashToken, Kind.Unknown, "ll * lr / r"),
            new(Kind.AsteriskAsteriskToken, Kind.SlashToken, Kind.Unknown, "ll ** lr / r"),
            new(Kind.AsteriskToken, Kind.AsteriskAsteriskToken, Kind.Unknown, "(ll * lr) ** r"),
            new(Kind.AsteriskAsteriskToken, Kind.AsteriskAsteriskToken, Kind.Unknown, "(ll ** lr) ** r"),
            new(Kind.Unknown, Kind.AsteriskToken, Kind.AsteriskToken, "l * rl * rr"),
            new(Kind.Unknown, Kind.BarToken, Kind.BarToken, "l | rl | rr"),
            new(Kind.Unknown, Kind.AmpersandToken, Kind.AmpersandToken, "l & rl & rr"),
            new(Kind.Unknown, Kind.CaretToken, Kind.CaretToken, "l ^ rl ^ rr"),
            new(Kind.Unknown, Kind.AmpersandAmpersandToken, Kind.ArrowFunction, "l && (() => { })"),

        ];
        foreach(var rec in data)
        {
            var factory = new AstFactory();
            var file = factory.NewSourceFile(new SourceFileParseOptions("/file.ts", "/file.ts"), "",
                factory.NewNodeList(new List<Node> { factory.NewExpressionStatement(factory.NewBinaryExpression(
                    null, MakeSide("l", rec.left, factory), null, factory.NewToken(rec.op), MakeSide("r", rec.right, factory))) }),
                factory.NewToken(Kind.EndOfFile));
            MarkSyntheticRecursive(file);
            CheckEmit(null, file.AsSourceFile(), rec.output+";");
        }
    }
    private readonly record struct MixingCase(string title,Kind innerOp,Kind outerOp,string side,string output);
    private static void TestParenthesizeBinaryExpressionMixingNullishCoalescing()
    {
        MixingCase[] tests = [
            new("BarBarWithLeftQuestionQuestion", Kind.QuestionQuestionToken, Kind.BarBarToken, "left", "(a ?? b) || c;"),
            new("AmpersandAmpersandWithLeftQuestionQuestion", Kind.QuestionQuestionToken, Kind.AmpersandAmpersandToken, "left", "(a ?? b) && c;"),
            new("BarBarWithRightQuestionQuestion", Kind.QuestionQuestionToken, Kind.BarBarToken, "right", "a || (b ?? c);"),
            new("AmpersandAmpersandWithRightQuestionQuestion", Kind.QuestionQuestionToken, Kind.AmpersandAmpersandToken, "right", "a && (b ?? c);"),
            new("QuestionQuestionWithLeftBarBar", Kind.BarBarToken, Kind.QuestionQuestionToken, "left", "(a || b) ?? c;"),
            new("QuestionQuestionWithLeftAmpersandAmpersand", Kind.AmpersandAmpersandToken, Kind.QuestionQuestionToken, "left", "(a && b) ?? c;"),
            new("QuestionQuestionWithRightBarBar", Kind.BarBarToken, Kind.QuestionQuestionToken, "right", "a ?? (b || c);"),
            new("QuestionQuestionWithRightAmpersandAmpersand", Kind.AmpersandAmpersandToken, Kind.QuestionQuestionToken, "right", "a ?? (b && c);"),

        ];
        foreach(var tt in tests)
        {
            var factory = new AstFactory();
            var innerExpr = factory.NewBinaryExpression(null, factory.NewIdentifier("a"), null,
                factory.NewToken(tt.innerOp), factory.NewIdentifier("b"));
            Node outerExpr;
            if(tt.side == "left") outerExpr = factory.NewBinaryExpression(null, innerExpr, null,
                factory.NewToken(tt.outerOp), factory.NewIdentifier("c"));
            else
            {
                outerExpr = factory.NewBinaryExpression(null, factory.NewIdentifier("a"), null,
                    factory.NewToken(tt.outerOp), innerExpr);
                innerExpr.AsBinaryExpression().Left = factory.NewIdentifier("b");
                innerExpr.AsBinaryExpression().Right = factory.NewIdentifier("c");
            }
            var file = factory.NewSourceFile(new SourceFileParseOptions("/file.ts", "/file.ts"), "",
                factory.NewNodeList(new List<Node> { factory.NewExpressionStatement(outerExpr) }), factory.NewToken(Kind.EndOfFile));
            MarkSyntheticRecursive(file);
            CheckEmit(null, file.AsSourceFile(), tt.output);
        }
    }
}

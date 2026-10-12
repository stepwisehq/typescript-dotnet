// Original pinned printer/printer_test.go assertions; shared managed/Native adaptation.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using AstFactory = TypeScript.Scanner.NodeFactory;
namespace Emit.Checks;
public static partial class PrinterChecks
{
    public static readonly string[] Names = [
        "TestEmit",
        "TestPartiallyEmittedExpression",
        "TestParenthesizeDecorator",
        "TestParenthesizeComputedPropertyName",
        "TestParenthesizeArrayLiteral",
        "TestParenthesizePropertyAccess1",
        "TestParenthesizePropertyAccess2",
        "TestParenthesizePropertyAccess3",
        "TestParenthesizeElementAccess1",
        "TestParenthesizeElementAccess2",
        "TestParenthesizeElementAccess3",
        "TestParenthesizeCall1",
        "TestParenthesizeCall2",
        "TestParenthesizeCall3",
        "TestParenthesizeCall4",
        "TestParenthesizeNew1",
        "TestParenthesizeNew2",
        "TestParenthesizeNew3",
        "TestParenthesizeTaggedTemplate1",
        "TestParenthesizeTaggedTemplate2",
        "TestParenthesizeTypeAssertion1",
        "TestParenthesizeArrowFunction1",
        "TestParenthesizeArrowFunction2",
        "TestParenthesizeDelete",
        "TestParenthesizeVoid",
        "TestParenthesizeTypeOf",
        "TestParenthesizeAwait",
        "TestParenthesizeBinary",
        "TestParenthesizeConditional1",
        "TestParenthesizeConditional2",
        "TestParenthesizeConditional3",
        "TestParenthesizeConditional4",
        "TestParenthesizeConditional5",
        "TestParenthesizeConditional6",
        "TestParenthesizeYield1",
        "TestParenthesizeSpreadElement1",
        "TestParenthesizeSpreadElement2",
        "TestParenthesizeSpreadElement3",
        "TestParenthesizeExpressionWithTypeArguments",
        "TestParenthesizeAsExpression",
        "TestParenthesizeSatisfiesExpression",
        "TestParenthesizeNonNullExpression",
        "TestParenthesizeExpressionStatement1",
        "TestParenthesizeExpressionStatement2",
        "TestParenthesizeExpressionStatement3",
        "TestParenthesizeExpressionDefault1",
        "TestParenthesizeExpressionDefault2",
        "TestParenthesizeExpressionDefault3",
        "TestParenthesizeArrayType",
        "TestParenthesizeOptionalType",
        "TestParenthesizeUnionType1",
        "TestParenthesizeUnionType2",
        "TestParenthesizeIntersectionType",
        "TestParenthesizeReadonlyTypeOperator1",
        "TestParenthesizeReadonlyTypeOperator2",
        "TestParenthesizeKeyofTypeOperator",
        "TestParenthesizeIndexedAccessType",
        "TestParenthesizeConditionalType1",
        "TestParenthesizeConditionalType2",
        "TestParenthesizeConditionalType3",
        "TestParenthesizeConditionalType4",
        "TestNameGeneration",
        "TestNoTrailingCommaAfterTransform",
        "TestTrailingCommaAfterTransform",
        "TestParenthesizeBinaryExpressionMixingNullishCoalescing",
        "TestOmitTrailingSemicolon",
    ];
    public static void Run(string name)
    {
        switch(name)
        {
            case "TestPartiallyEmittedExpression": TestPartiallyEmittedExpression(); break;
            case "TestEmit": TestEmit(); break;
            case "TestParenthesizeDecorator": TestParenthesizeDecorator(); break;
            case "TestParenthesizeComputedPropertyName": TestParenthesizeComputedPropertyName(); break;
            case "TestParenthesizeArrayLiteral": TestParenthesizeArrayLiteral(); break;
            case "TestParenthesizePropertyAccess1": TestParenthesizePropertyAccess1(); break;
            case "TestParenthesizePropertyAccess2": TestParenthesizePropertyAccess2(); break;
            case "TestParenthesizePropertyAccess3": TestParenthesizePropertyAccess3(); break;
            case "TestParenthesizeElementAccess1": TestParenthesizeElementAccess1(); break;
            case "TestParenthesizeElementAccess2": TestParenthesizeElementAccess2(); break;
            case "TestParenthesizeElementAccess3": TestParenthesizeElementAccess3(); break;
            case "TestParenthesizeCall1": TestParenthesizeCall1(); break;
            case "TestParenthesizeCall2": TestParenthesizeCall2(); break;
            case "TestParenthesizeCall3": TestParenthesizeCall3(); break;
            case "TestParenthesizeCall4": TestParenthesizeCall4(); break;
            case "TestParenthesizeNew1": TestParenthesizeNew1(); break;
            case "TestParenthesizeNew2": TestParenthesizeNew2(); break;
            case "TestParenthesizeNew3": TestParenthesizeNew3(); break;
            case "TestParenthesizeTaggedTemplate1": TestParenthesizeTaggedTemplate1(); break;
            case "TestParenthesizeTaggedTemplate2": TestParenthesizeTaggedTemplate2(); break;
            case "TestParenthesizeTypeAssertion1": TestParenthesizeTypeAssertion1(); break;
            case "TestParenthesizeArrowFunction1": TestParenthesizeArrowFunction1(); break;
            case "TestParenthesizeArrowFunction2": TestParenthesizeArrowFunction2(); break;
            case "TestParenthesizeDelete": TestParenthesizeDelete(); break;
            case "TestParenthesizeVoid": TestParenthesizeVoid(); break;
            case "TestParenthesizeTypeOf": TestParenthesizeTypeOf(); break;
            case "TestParenthesizeAwait": TestParenthesizeAwait(); break;
            case "TestParenthesizeBinary": TestParenthesizeBinary(); break;
            case "TestParenthesizeConditional1": TestParenthesizeConditional1(); break;
            case "TestParenthesizeConditional2": TestParenthesizeConditional2(); break;
            case "TestParenthesizeConditional3": TestParenthesizeConditional3(); break;
            case "TestParenthesizeConditional4": TestParenthesizeConditional4(); break;
            case "TestParenthesizeConditional5": TestParenthesizeConditional5(); break;
            case "TestParenthesizeConditional6": TestParenthesizeConditional6(); break;
            case "TestParenthesizeYield1": TestParenthesizeYield1(); break;
            case "TestParenthesizeSpreadElement1": TestParenthesizeSpreadElement1(); break;
            case "TestParenthesizeSpreadElement2": TestParenthesizeSpreadElement2(); break;
            case "TestParenthesizeSpreadElement3": TestParenthesizeSpreadElement3(); break;
            case "TestParenthesizeExpressionWithTypeArguments": TestParenthesizeExpressionWithTypeArguments(); break;
            case "TestParenthesizeAsExpression": TestParenthesizeAsExpression(); break;
            case "TestParenthesizeSatisfiesExpression": TestParenthesizeSatisfiesExpression(); break;
            case "TestParenthesizeNonNullExpression": TestParenthesizeNonNullExpression(); break;
            case "TestParenthesizeExpressionStatement1": TestParenthesizeExpressionStatement1(); break;
            case "TestParenthesizeExpressionStatement2": TestParenthesizeExpressionStatement2(); break;
            case "TestParenthesizeExpressionStatement3": TestParenthesizeExpressionStatement3(); break;
            case "TestParenthesizeExpressionDefault1": TestParenthesizeExpressionDefault1(); break;
            case "TestParenthesizeExpressionDefault2": TestParenthesizeExpressionDefault2(); break;
            case "TestParenthesizeExpressionDefault3": TestParenthesizeExpressionDefault3(); break;
            case "TestParenthesizeArrayType": TestParenthesizeArrayType(); break;
            case "TestParenthesizeOptionalType": TestParenthesizeOptionalType(); break;
            case "TestParenthesizeUnionType1": TestParenthesizeUnionType1(); break;
            case "TestParenthesizeUnionType2": TestParenthesizeUnionType2(); break;
            case "TestParenthesizeIntersectionType": TestParenthesizeIntersectionType(); break;
            case "TestParenthesizeReadonlyTypeOperator1": TestParenthesizeReadonlyTypeOperator1(); break;
            case "TestParenthesizeReadonlyTypeOperator2": TestParenthesizeReadonlyTypeOperator2(); break;
            case "TestParenthesizeKeyofTypeOperator": TestParenthesizeKeyofTypeOperator(); break;
            case "TestParenthesizeIndexedAccessType": TestParenthesizeIndexedAccessType(); break;
            case "TestParenthesizeConditionalType1": TestParenthesizeConditionalType1(); break;
            case "TestParenthesizeConditionalType2": TestParenthesizeConditionalType2(); break;
            case "TestParenthesizeConditionalType3": TestParenthesizeConditionalType3(); break;
            case "TestParenthesizeConditionalType4": TestParenthesizeConditionalType4(); break;
            case "TestNameGeneration": TestNameGeneration(); break;
            case "TestNoTrailingCommaAfterTransform": TestNoTrailingCommaAfterTransform(); break;
            case "TestTrailingCommaAfterTransform": TestTrailingCommaAfterTransform(); break;
            case "TestParenthesizeBinaryExpressionMixingNullishCoalescing": TestParenthesizeBinaryExpressionMixingNullishCoalescing(); break;
            case "TestOmitTrailingSemicolon": TestOmitTrailingSemicolon(); break;
            default: throw new ArgumentException("Unknown original printer test: " + name);
        }
    }
    public static int RunAll() { foreach(var name in Names) Run(name); return Names.Length; }
private static void TestParenthesizeDecorator() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewClassDeclaration(
				factory.NewModifierList(
					new List<Node> {
						factory.NewDecorator(
							factory.NewBinaryExpression(
								null, /*modifiers*/
								factory.NewIdentifier("a"),
								null, /*typeNode*/
								factory.NewToken(Kind.PlusToken),
								factory.NewIdentifier("b") ) ),
					} ),
				factory.NewIdentifier("C"),
				null,
				null,
				factory.NewNodeList(new List<Node> {}) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "@(a + b)\nclass C {\n}");
}

private static void TestParenthesizeComputedPropertyName() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewClassDeclaration(
				null, /*modifiers*/
				factory.NewIdentifier("C"),
				null, /*typeParameters*/
				null, /*heritageClauses*/
				factory.NewNodeList(new List<Node> {
					factory.NewPropertyDeclaration(
						null, /*modifiers*/
						factory.NewComputedPropertyName(
							// will be parenthesized on emit:
							factory.NewBinaryExpression(
								null, /*modifiers*/
								factory.NewIdentifier("a"),
								null, /*typeNode*/
								factory.NewToken(Kind.CommaToken),
								factory.NewIdentifier("b") ) ),
						null, /*postfixToken*/
						null, /*typeNode*/
						null /*initializer*/
					),
				}) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "class C {\n    [(a, b)];\n}");
}

private static void TestParenthesizeArrayLiteral() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewArrayLiteralExpression(
					factory.NewNodeList(
						new List<Node> {
							// will be parenthesized on emit:
							factory.NewBinaryExpression(
								null, /*modifiers*/
								factory.NewIdentifier("a"),
								null, /*typeNode*/
								factory.NewToken(Kind.CommaToken),
								factory.NewIdentifier("b") ),
						} ),
					false /*multiLine*/
				) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "[(a, b)];");
}

private static void TestParenthesizePropertyAccess1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewPropertyAccessExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("b") ),
					null, /*questionDotToken*/
					factory.NewIdentifier("c"),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a, b).c;");
}

private static void TestParenthesizePropertyAccess2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewPropertyAccessExpression(
					// will be parenthesized on emit:
					factory.NewPropertyAccessExpression(
						factory.NewIdentifier("a"),
						factory.NewToken(Kind.QuestionDotToken),
						factory.NewIdentifier("b"),
						NodeFlags.OptionalChain ),
					null, /*questionDotToken*/
					factory.NewIdentifier("c"),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a?.b).c;");
}

private static void TestParenthesizePropertyAccess3() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewPropertyAccessExpression(
					// will be parenthesized on emit:
					factory.NewNewExpression(
						factory.NewIdentifier("a"),
						null, /*typeArguments*/
						null /*arguments*/
					),
					null, /*questionDotToken*/
					factory.NewIdentifier("b"),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(new a).b;");
}

private static void TestParenthesizeElementAccess1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewElementAccessExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("b") ),
					null, /*questionDotToken*/
					factory.NewIdentifier("c"),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a, b)[c];");
}

private static void TestParenthesizeElementAccess2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewElementAccessExpression(
					// will be parenthesized on emit:
					factory.NewPropertyAccessExpression(
						factory.NewIdentifier("a"),
						factory.NewToken(Kind.QuestionDotToken),
						factory.NewIdentifier("b"),
						NodeFlags.OptionalChain ),
					null, /*questionDotToken*/
					factory.NewIdentifier("c"),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a?.b)[c];");
}

private static void TestParenthesizeElementAccess3() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewElementAccessExpression(
					// will be parenthesized on emit:
					factory.NewNewExpression(
						factory.NewIdentifier("a"),
						null, /*typeArguments*/
						null /*arguments*/
					),
					null, /*questionDotToken*/
					factory.NewIdentifier("b"),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(new a)[b];");
}

private static void TestParenthesizeCall1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewCallExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("b") ),
					null, /*questionDotToken*/
					null, /*typeArguments*/
					factory.NewNodeList(new List<Node> {}),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a, b)();");
}

private static void TestParenthesizeCall2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewCallExpression(
					// will be parenthesized on emit:
					factory.NewPropertyAccessExpression(
						factory.NewIdentifier("a"),
						factory.NewToken(Kind.QuestionDotToken),
						factory.NewIdentifier("b"),
						NodeFlags.OptionalChain ),
					null, /*questionDotToken*/
					null, /*typeArguments*/
					factory.NewNodeList(new List<Node> {}),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a?.b)();");
}

private static void TestParenthesizeCall3() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewCallExpression(
					// will be parenthesized on emit:
					factory.NewNewExpression(
						factory.NewIdentifier("C"),
						null, /*typeArguments*/
						null /*arguments*/
					),
					null, /*questionDotToken*/
					null, /*typeArguments*/
					factory.NewNodeList(new List<Node> {}),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(new C)();");
}

private static void TestParenthesizeCall4() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewCallExpression(
					factory.NewIdentifier("a"),
					null, /*questionDotToken*/
					null, /*typeArguments*/
					factory.NewNodeList(new List<Node> {
						factory.NewBinaryExpression(
							null, /*modifiers*/
							factory.NewIdentifier("b"),
							null, /*typeNode*/
							factory.NewToken(Kind.CommaToken),
							factory.NewIdentifier("c") ),
					}),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "a((b, c));");
}

private static void TestParenthesizeNew1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewNewExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("b") ),
					null, /*typeArguments*/
					factory.NewNodeList(new List<Node> {}) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "new (a, b)();");
}

private static void TestParenthesizeNew2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewNewExpression(
					// will be parenthesized on emit:
					factory.NewCallExpression(
						factory.NewIdentifier("C"),
						null, /*questionDotToken*/
						null, /*typeArguments*/
						factory.NewNodeList(new List<Node> {}),
						NodeFlags.None ),
					null, /*typeArguments*/
					null /*arguments*/
				) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "new (C());");
}

private static void TestParenthesizeNew3() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewNewExpression(
					factory.NewIdentifier("C"),
					null, /*typeArguments*/
					factory.NewNodeList(new List<Node> {
						factory.NewBinaryExpression(
							null, /*modifiers*/
							factory.NewIdentifier("a"),
							null, /*typeNode*/
							factory.NewToken(Kind.CommaToken),
							factory.NewIdentifier("b") ),
					}) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "new C((a, b));");
}

private static void TestParenthesizeTaggedTemplate1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewTaggedTemplateExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("b") ),
					null, /*questionDotToken*/
					null, /*typeArguments*/
					factory.NewNoSubstitutionTemplateLiteral("", TokenFlags.None),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a, b) ``;");
}

private static void TestParenthesizeTaggedTemplate2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewTaggedTemplateExpression(
					// will be parenthesized on emit:
					factory.NewPropertyAccessExpression(
						factory.NewIdentifier("a"),
						factory.NewToken(Kind.QuestionDotToken),
						factory.NewIdentifier("b"),
						NodeFlags.OptionalChain ),
					null, /*questionDotToken*/
					null, /*typeArguments*/
					factory.NewNoSubstitutionTemplateLiteral("", TokenFlags.None),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a?.b) ``;");
}

private static void TestParenthesizeTypeAssertion1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewTypeAssertion(
					factory.NewTypeReferenceNode(
						factory.NewIdentifier("T"),
						null /*typeArguments*/
					),
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.PlusToken),
						factory.NewIdentifier("b") ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "<T>(a + b);");
}

private static void TestParenthesizeArrowFunction1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewArrowFunction(
					null, /*modifiers*/
					null, /*typeParameters*/
					factory.NewNodeList(new List<Node> {}),
					null, /*returnType*/
					null, /*fullSignature*/
					factory.NewToken(Kind.EqualsGreaterThanToken),
					// will be parenthesized on emit:
					factory.NewObjectLiteralExpression(
						factory.NewNodeList(new List<Node> {}),
						false /*multiLine*/
					) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "() => ({});");
}

private static void TestParenthesizeArrowFunction2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewArrowFunction(
					null, /*modifiers*/
					null, /*typeParameters*/
					factory.NewNodeList(new List<Node> {}),
					null, /*returnType*/
					null, /*fullSignature*/
					factory.NewToken(Kind.EqualsGreaterThanToken),
					// will be parenthesized on emit:
					factory.NewPropertyAccessExpression(
						factory.NewObjectLiteralExpression(
							factory.NewNodeList(new List<Node> {}),
							false /*multiLine*/
						),
						null, /*questionDotToken*/
						factory.NewIdentifier("a"),
						NodeFlags.None ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "() => ({}.a);");
}

private static void TestParenthesizeDelete() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewDeleteExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.PlusToken),
						factory.NewIdentifier("b") ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "delete (a + b);");
}

private static void TestParenthesizeVoid() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewVoidExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.PlusToken),
						factory.NewIdentifier("b") ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "void (a + b);");
}

private static void TestParenthesizeTypeOf() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewTypeOfExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.PlusToken),
						factory.NewIdentifier("b") ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "typeof (a + b);");
}

private static void TestParenthesizeAwait() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewAwaitExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.PlusToken),
						factory.NewIdentifier("b") ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "await (a + b);");
}

private static void TestParenthesizeConditional1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewConditionalExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("b") ),
					factory.NewToken(Kind.QuestionToken),
					factory.NewIdentifier("c"),
					factory.NewToken(Kind.ColonToken),
					factory.NewIdentifier("d") ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a, b) ? c : d;");
}

private static void TestParenthesizeConditional2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewConditionalExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.EqualsToken),
						factory.NewIdentifier("b") ),
					factory.NewToken(Kind.QuestionToken),
					factory.NewIdentifier("c"),
					factory.NewToken(Kind.ColonToken),
					factory.NewIdentifier("d") ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a = b) ? c : d;");
}

private static void TestParenthesizeConditional3() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewConditionalExpression(
					// will be parenthesized on emit:
					factory.NewArrowFunction(
						null, /*modifiers*/
						null, /*typeParameters*/
						factory.NewNodeList(new List<Node> {}),
						null, /*returnType*/
						null, /*fullSignature*/
						factory.NewToken(Kind.EqualsGreaterThanToken),
						factory.NewBlock(
							factory.NewNodeList(new List<Node> {}),
							false /*multiLine*/
						) ),
					factory.NewToken(Kind.QuestionToken),
					factory.NewIdentifier("a"),
					factory.NewToken(Kind.ColonToken),
					factory.NewIdentifier("b") ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(() => { }) ? a : b;");
}

private static void TestParenthesizeConditional4() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewConditionalExpression(
					// will be parenthesized on emit:
					factory.NewYieldExpression(null, null),
					factory.NewToken(Kind.QuestionToken),
					factory.NewIdentifier("a"),
					factory.NewToken(Kind.ColonToken),
					factory.NewIdentifier("b") ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(yield) ? a : b;");
}

private static void TestParenthesizeConditional5() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewConditionalExpression(
					factory.NewIdentifier("a"),
					factory.NewToken(Kind.QuestionToken),
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("b"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("c") ),
					factory.NewToken(Kind.ColonToken),
					factory.NewIdentifier("d") ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "a ? (b, c) : d;");
}

private static void TestParenthesizeConditional6() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewConditionalExpression(
					factory.NewIdentifier("a"),
					factory.NewToken(Kind.QuestionToken),
					factory.NewIdentifier("b"),
					factory.NewToken(Kind.ColonToken),
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("c"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("d") ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "a ? b : (c, d);");
}

private static void TestParenthesizeYield1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewYieldExpression(
					null, /*asteriskToken*/
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("b") ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "yield (a, b);");
}

// !!! test ASI avoidance from emitExpressionNoASI
////func TestParenthesizeYield2(t *testing.T) {
////}

private static void TestParenthesizeSpreadElement1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewArrayLiteralExpression(
					factory.NewNodeList(
						new List<Node> {
							factory.NewSpreadElement(
								// will be parenthesized on emit:
								factory.NewBinaryExpression(
									null, /*modifiers*/
									factory.NewIdentifier("a"),
									null, /*typeNode*/
									factory.NewToken(Kind.CommaToken),
									factory.NewIdentifier("b") ) ),
						} ),
					false /*multiLine*/
				) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "[...(a, b)];");
}

private static void TestParenthesizeSpreadElement2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewCallExpression(
					factory.NewIdentifier("a"),
					null, /*questionDotToken*/
					null, /*typeArguments*/
					factory.NewNodeList(
						new List<Node> {
							factory.NewSpreadElement(
								// will be parenthesized on emit:
								factory.NewBinaryExpression(
									null, /*modifiers*/
									factory.NewIdentifier("b"),
									null, /*typeNode*/
									factory.NewToken(Kind.CommaToken),
									factory.NewIdentifier("c") ) ),
						} ),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "a(...(b, c));");
}

private static void TestParenthesizeSpreadElement3() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewNewExpression(
					factory.NewIdentifier("a"),
					null, /*typeArguments*/
					factory.NewNodeList(
						new List<Node> {
							factory.NewSpreadElement(
								// will be parenthesized on emit:
								factory.NewBinaryExpression(
									null, /*modifiers*/
									factory.NewIdentifier("b"),
									null, /*typeNode*/
									factory.NewToken(Kind.CommaToken),
									factory.NewIdentifier("c") ) ),
						} ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "new a(...(b, c));");
}

private static void TestParenthesizeExpressionWithTypeArguments() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewExpressionWithTypeArguments(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("b") ),
					factory.NewNodeList(
						new List<Node> {
							factory.NewTypeReferenceNode(
								factory.NewIdentifier("c"),
								null ),
						} ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a, b)<c>;");
}

private static void TestParenthesizeAsExpression() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewAsExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("b") ),
					factory.NewTypeReferenceNode(
						factory.NewIdentifier("c"),
						null ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a, b) as c;");
}

private static void TestParenthesizeSatisfiesExpression() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewSatisfiesExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("b") ),
					factory.NewTypeReferenceNode(
						factory.NewIdentifier("c"),
						null ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a, b) satisfies c;");
}

private static void TestParenthesizeNonNullExpression() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewNonNullExpression(
					// will be parenthesized on emit:
					factory.NewBinaryExpression(
						null, /*modifiers*/
						factory.NewIdentifier("a"),
						null, /*typeNode*/
						factory.NewToken(Kind.CommaToken),
						factory.NewIdentifier("b") ),
					NodeFlags.None ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(a, b)!;");
}

private static void TestParenthesizeExpressionStatement1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewObjectLiteralExpression(
					factory.NewNodeList(
						new List<Node> {} ),
					false /*multiLine*/
				) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "({});");
}

private static void TestParenthesizeExpressionStatement2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewFunctionExpression(
					null, /*modifiers*/
					null, /*asteriskToken*/
					null, /*name*/
					null, /*typeParameters*/
					factory.NewNodeList(
						new List<Node> {} ),
					null, /*returnType*/
					null, /*fullSignature*/
					factory.NewBlock(
						factory.NewNodeList(new List<Node> {}),
						false /*multiLine*/
					) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "(function () { });");
}

private static void TestParenthesizeExpressionStatement3() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExpressionStatement(
				factory.NewClassExpression(
					null, /*modifiers*/
					null, /*name*/
					null, /*typeParameters*/
					null, /*heritageClauses*/
					factory.NewNodeList(
						new List<Node> {} ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "class {\n};");
}

private static void TestParenthesizeExpressionDefault1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExportAssignment(
				null,   /*modifiers*/
				false, /*isExportEquals*/
				null,   /*typeNode*/
				// will be parenthesized on emit:
				factory.NewClassExpression(
					null, /*modifiers*/
					null, /*name*/
					null, /*typeParameters*/
					null, /*heritageClauses*/
					factory.NewNodeList(
						new List<Node> {} ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "export default (class {\n});");
}

private static void TestParenthesizeExpressionDefault2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExportAssignment(
				null,   /*modifiers*/
				false, /*isExportEquals*/
				null,   /*typeNode*/
				// will be parenthesized on emit:
				factory.NewFunctionExpression(
					null, /*modifiers*/
					null, /*asteriskToken*/
					null, /*name*/
					null, /*typeParameters*/
					factory.NewNodeList(
						new List<Node> {} ),
					null, /*returnType*/
					null, /*fullSignature*/
					factory.NewBlock(
						factory.NewNodeList(
							new List<Node> {} ),
						false /*multiLine*/
					) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "export default (function () { });");
}

private static void TestParenthesizeExpressionDefault3() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewExportAssignment(
				null,   /*modifiers*/
				false, /*isExportEquals*/
				null,   /*typeNode*/
				// will be parenthesized on emit:
				factory.NewBinaryExpression(
					null, /*modifiers*/
					factory.NewIdentifier("a"),
					null, /*typeNode*/
					factory.NewToken(Kind.CommaToken),
					factory.NewIdentifier("b") ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "export default (a, b);");
}

private static void TestParenthesizeArrayType() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewTypeAliasDeclaration(
				null,                        /*modifiers*/
				factory.NewIdentifier("_"), /*name*/
				null,                        /*typeParameters*/
				factory.NewArrayTypeNode(
					// will be parenthesized on emit:
					factory.NewUnionTypeNode(
						factory.NewNodeList(
							new List<Node> {
								factory.NewTypeReferenceNode(factory.NewIdentifier("a"), null /*typeArguments*/),
								factory.NewTypeReferenceNode(factory.NewIdentifier("b"), null /*typeArguments*/),
							} ) ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = (a | b)[];");
}

private static void TestParenthesizeOptionalType() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewTypeAliasDeclaration(
				null,                        /*modifiers*/
				factory.NewIdentifier("_"), /*name*/
				null,                        /*typeParameters*/
				factory.NewTupleTypeNode(
					factory.NewNodeList(
						new List<Node> {
							factory.NewOptionalTypeNode(
								// will be parenthesized on emit:
								factory.NewUnionTypeNode(
									factory.NewNodeList(
										new List<Node> {
											factory.NewTypeReferenceNode(factory.NewIdentifier("a"), null /*typeArguments*/),
											factory.NewTypeReferenceNode(factory.NewIdentifier("b"), null /*typeArguments*/),
										} ) ) ),
						} ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = [\n    (a | b)?\n];");
}

private static void TestParenthesizeUnionType1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewTypeAliasDeclaration(
				null,                        /*modifiers*/
				factory.NewIdentifier("_"), /*name*/
				null,                        /*typeParameters*/
				factory.NewUnionTypeNode(
					factory.NewNodeList(
						new List<Node> {
							factory.NewTypeReferenceNode(factory.NewIdentifier("a"), null /*typeArguments*/),
							// will be parenthesized on emit:
							factory.NewFunctionTypeNode(
								null, /*typeParameters*/
								factory.NewNodeList(
									new List<Node> {} ),
								factory.NewTypeReferenceNode(factory.NewIdentifier("b"), null /*typeArguments*/) ),
						} ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = a | (() => b);");
}

private static void TestParenthesizeUnionType2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewTypeAliasDeclaration(
				null,                        /*modifiers*/
				factory.NewIdentifier("_"), /*name*/
				null,                        /*typeParameters*/
				factory.NewUnionTypeNode(
					factory.NewNodeList(
						new List<Node> {
							// will be parenthesized on emit:
							factory.NewInferTypeNode(
								factory.NewTypeParameterDeclaration(
									null,
									factory.NewIdentifier("a"),
									factory.NewTypeReferenceNode(factory.NewIdentifier("b"), null /*typeArguments*/),
									null, /*expression*/
									null /*defaultType*/
								) ),
							factory.NewTypeReferenceNode(factory.NewIdentifier("c"), null /*typeArguments*/),
						} ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = (infer a extends b) | c;");
}

private static void TestParenthesizeIntersectionType() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewTypeAliasDeclaration(
				null,                        /*modifiers*/
				factory.NewIdentifier("_"), /*name*/
				null,                        /*typeParameters*/
				factory.NewIntersectionTypeNode(
					factory.NewNodeList(
						new List<Node> {
							factory.NewTypeReferenceNode(factory.NewIdentifier("a"), null /*typeArguments*/),
							// will be parenthesized on emit:
							factory.NewUnionTypeNode(
								factory.NewNodeList(
									new List<Node> {
										factory.NewTypeReferenceNode(factory.NewIdentifier("b"), null /*typeArguments*/),
										factory.NewTypeReferenceNode(factory.NewIdentifier("c"), null /*typeArguments*/),
									} ) ),
						} ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = a & (b | c);");
}

private static void TestParenthesizeReadonlyTypeOperator1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewTypeAliasDeclaration(
				null,                        /*modifiers*/
				factory.NewIdentifier("_"), /*name*/
				null,                        /*typeParameters*/
				factory.NewTypeOperatorNode(
					Kind.ReadonlyKeyword,
					// will be parenthesized on emit:
					factory.NewUnionTypeNode(
						factory.NewNodeList(
							new List<Node> {
								factory.NewTypeReferenceNode(factory.NewIdentifier("a"), null /*typeArguments*/),
								factory.NewTypeReferenceNode(factory.NewIdentifier("b"), null /*typeArguments*/),
							} ) ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = readonly (a | b);");
}

private static void TestParenthesizeReadonlyTypeOperator2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewTypeAliasDeclaration(
				null,                        /*modifiers*/
				factory.NewIdentifier("_"), /*name*/
				null,                        /*typeParameters*/
				factory.NewTypeOperatorNode(
					Kind.ReadonlyKeyword,
					// will be parenthesized on emit:
					factory.NewTypeOperatorNode(
						Kind.KeyOfKeyword,
						factory.NewTypeReferenceNode(factory.NewIdentifier("a"), null /*typeArguments*/) ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = readonly (keyof a);");
}

private static void TestParenthesizeKeyofTypeOperator() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewTypeAliasDeclaration(
				null,                        /*modifiers*/
				factory.NewIdentifier("_"), /*name*/
				null,                        /*typeParameters*/
				factory.NewTypeOperatorNode(
					Kind.KeyOfKeyword,
					// will be parenthesized on emit:
					factory.NewUnionTypeNode(
						factory.NewNodeList(
							new List<Node> {
								factory.NewTypeReferenceNode(factory.NewIdentifier("a"), null /*typeArguments*/),
								factory.NewTypeReferenceNode(factory.NewIdentifier("b"), null /*typeArguments*/),
							} ) ) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = keyof (a | b);");
}

private static void TestParenthesizeIndexedAccessType() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewTypeAliasDeclaration(
				null,                        /*modifiers*/
				factory.NewIdentifier("_"), /*name*/
				null,                        /*typeParameters*/
				factory.NewIndexedAccessTypeNode(
					// will be parenthesized on emit:
					factory.NewUnionTypeNode(
						factory.NewNodeList(
							new List<Node> {
								factory.NewTypeReferenceNode(factory.NewIdentifier("a"), null /*typeArguments*/),
								factory.NewTypeReferenceNode(factory.NewIdentifier("b"), null /*typeArguments*/),
							} ) ),
					factory.NewTypeReferenceNode(factory.NewIdentifier("c"), null /*typeArguments*/) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = (a | b)[c];");
}

private static void TestParenthesizeConditionalType1() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewTypeAliasDeclaration(
				null,                        /*modifiers*/
				factory.NewIdentifier("_"), /*name*/
				null,                        /*typeParameters*/
				factory.NewConditionalTypeNode(
					// will be parenthesized on emit:
					factory.NewFunctionTypeNode(
						null, /*typeParameters*/
						factory.NewNodeList(
							new List<Node> {} ),
						factory.NewTypeReferenceNode(factory.NewIdentifier("a"), null /*typeArguments*/) ),
					factory.NewTypeReferenceNode(factory.NewIdentifier("b"), null /*typeArguments*/),
					factory.NewTypeReferenceNode(factory.NewIdentifier("c"), null /*typeArguments*/),
					factory.NewTypeReferenceNode(factory.NewIdentifier("d"), null /*typeArguments*/) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = (() => a) extends b ? c : d;");
}

private static void TestParenthesizeConditionalType2() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewTypeAliasDeclaration(
				null,                        /*modifiers*/
				factory.NewIdentifier("_"), /*name*/
				null,                        /*typeParameters*/
				factory.NewConditionalTypeNode(
					factory.NewTypeReferenceNode(factory.NewIdentifier("a"), null /*typeArguments*/),
					// will be parenthesized on emit:
					factory.NewConditionalTypeNode(
						factory.NewTypeReferenceNode(factory.NewIdentifier("b"), null /*typeArguments*/),
						factory.NewTypeReferenceNode(factory.NewIdentifier("c"), null /*typeArguments*/),
						factory.NewTypeReferenceNode(factory.NewIdentifier("d"), null /*typeArguments*/),
						factory.NewTypeReferenceNode(factory.NewIdentifier("e"), null /*typeArguments*/) ),
					factory.NewTypeReferenceNode(factory.NewIdentifier("f"), null /*typeArguments*/),
					factory.NewTypeReferenceNode(factory.NewIdentifier("g"), null /*typeArguments*/) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = a extends (b extends c ? d : e) ? f : g;");
}

private static void TestParenthesizeConditionalType3() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(
		new List<Node> {
			factory.NewTypeAliasDeclaration(
				null,                        /*modifiers*/
				factory.NewIdentifier("_"), /*name*/
				null,                        /*typeParameters*/
				factory.NewConditionalTypeNode(
					factory.NewTypeReferenceNode(factory.NewIdentifier("a"), null /*typeArguments*/),
					factory.NewFunctionTypeNode(
						null, /*typeParameters*/
						factory.NewNodeList(
							new List<Node> {} ),
						// will be parenthesized on emit:
						factory.NewInferTypeNode(
							factory.NewTypeParameterDeclaration(
								null,
								factory.NewIdentifier("b"),
								factory.NewTypeReferenceNode(factory.NewIdentifier("c"), null /*typeArguments*/),
								null, /*expression*/
								null /*defaultType*/
							) ) ),
					factory.NewTypeReferenceNode(factory.NewIdentifier("d"), null /*typeArguments*/),
					factory.NewTypeReferenceNode(factory.NewIdentifier("e"), null /*typeArguments*/) ) ),
		} ), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = a extends () => (infer b extends c) ? d : e;");
}

private static void TestParenthesizeConditionalType4() {
	

	var factory = new AstFactory();
	var file = factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", factory.NewNodeList(new List<Node> {
		factory.NewTypeAliasDeclaration(
			null,                        /*modifiers*/
			factory.NewIdentifier("_"), /*name*/
			null,                        /*typeParameters*/
			factory.NewConditionalTypeNode(
				factory.NewTypeReferenceNode(factory.NewIdentifier("a"), null /*typeArguments*/),
				factory.NewFunctionTypeNode(
					null, /*typeParameters*/
					factory.NewNodeList(
						new List<Node> {} ),
					// will be parenthesized on emit:
					factory.NewUnionTypeNode(
						factory.NewNodeList(
							new List<Node> {
								factory.NewInferTypeNode(
									factory.NewTypeParameterDeclaration(
										null,
										factory.NewIdentifier("b"),
										factory.NewTypeReferenceNode(factory.NewIdentifier("c"), null /*typeArguments*/),
										null, /*expression*/
										null /*defaultType*/
									) ),
								factory.NewTypeReferenceNode(factory.NewIdentifier("d"), null /*typeArguments*/),
							} ) ) ),
				factory.NewTypeReferenceNode(factory.NewIdentifier("e"), null /*typeArguments*/),
				factory.NewTypeReferenceNode(factory.NewIdentifier("f"), null /*typeArguments*/) ) ),
	}), factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( null, file.AsSourceFile(), "type _ = a extends () => (infer b extends c) | d ? e : f;");
}

private static void TestNameGeneration() {
	
	var ec = new EmitContext();
	var file = ec.Factory.NewSourceFile(new SourceFileParseOptions {FileName = "/file.ts", PathKey = "/file.ts"}, "", ec.Factory.NewNodeList(new List<Node> {
		ec.Factory.NewVariableStatement(null, ec.Factory.NewVariableDeclarationList(
			ec.Factory.NewNodeList(new List<Node> {
				ec.Factory.NewVariableDeclaration(ec.Factory.NewTempVariable(), null, null, null),
			}),
			NodeFlags.None )),
		ec.Factory.NewFunctionDeclaration(
			null,
			null,
			ec.Factory.NewIdentifier("f"),
			null,
			ec.Factory.NewNodeList(new List<Node> {}),
			null,
			null,
			ec.Factory.NewBlock(ec.Factory.NewNodeList(new List<Node> {
				ec.Factory.NewVariableStatement(null, ec.Factory.NewVariableDeclarationList(
					ec.Factory.NewNodeList(new List<Node> {
						ec.Factory.NewVariableDeclaration(ec.Factory.NewTempVariable(), null, null, null),
					}),
					NodeFlags.None )),
			}), true) ),
	}), ec.Factory.NewToken(Kind.EndOfFile));

	MarkSyntheticRecursive(file);
	CheckEmit( ec, file.AsSourceFile(), "var _a;\nfunction f() {\n    var _a;\n}");
}

}

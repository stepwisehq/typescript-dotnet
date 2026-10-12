// Adaptations of original emittestutil/parsetestutil helpers; same assertions and synthesis visitor.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
namespace Emit.Checks;
public static partial class PrinterChecks
{
    internal static SourceFile ParseTypeScript(string text, bool jsx) => Parser.ParseSourceFile(
        new SourceFileParseOptions(jsx ? "/main.tsx" : "/main.ts", jsx ? "/main.tsx" : "/main.ts"),
        text, jsx ? ScriptKind.TSX : ScriptKind.TS);
    internal static void CheckDiagnostics(SourceFile file)
    {
        if (file.Diagnostics().Length != 0)
            throw new InvalidOperationException("Original printer parse/reparse assertion: " + file.Diagnostics().Length + " diagnostics");
    }
    private static void Equal(string expected, string actual)
    {
        if (expected != actual) throw new InvalidOperationException($"Original printer assertion: expected {expected}, actual {actual}");
    }
    private static string TrimOneLf(string text) => text.EndsWith('\n') ? text[..^1] : text;
    internal static void CheckEmit(EmitContext context, SourceFile file, string expected)
    {
        var printer = Printer.NewPrinter(new PrinterOptions { NewLine = NewLineKind.LF }, new PrintHandlers(), context);
        var text = printer.EmitSourceFile(file);
        Equal(GoString.ToUtf8(expected), TrimOneLf(text));
        CheckDiagnostics(ParseTypeScript(ScannerString.ToJsString(text), file.LanguageVariant == LanguageVariant.JSX));
    }
    private static void MarkSyntheticRecursive(Node node)
    {
        NodeVisitor visitor = null;
        visitor = new NodeVisitor(n => visitor.VisitEachChild(n), new TypeScript.Scanner.NodeFactory(), new NodeVisitorHooks
        {
            VisitNode = (n, v) => { if (n != null) n.Loc = Core.UndefinedTextRange(); return v.VisitNode(n); },
            VisitToken = (n, v) => { if (n != null) n.Loc = Core.UndefinedTextRange(); return v.VisitNode(n); },
            VisitNodes = (nodes, v) => { if (nodes != null) nodes.Loc = Core.UndefinedTextRange(); return v.VisitNodes(nodes); },
            VisitModifiers = (nodes, v) => { if (nodes != null) nodes.Loc = Core.UndefinedTextRange(); return v.VisitModifiers(nodes); }
        });
        visitor.VisitNode(node);
    }
    private static void TestNoTrailingCommaAfterTransform() => CheckTrailingCommaAfterTransform("[a!]", "[a];");
    private static void TestTrailingCommaAfterTransform() => CheckTrailingCommaAfterTransform("[a!,]", "[a,];");
    private static void CheckTrailingCommaAfterTransform(string input, string expected)
    {
        var file = ParseTypeScript(input, false);
        var context = new EmitContext();
        NodeVisitor visitor = null;
        visitor = context.NewNodeVisitor(node =>
        {
            switch (node.Kind)
            {
                case Kind.NonNullExpression: node = node.Expression(); break;
                default: node = node.VisitEachChild(visitor); break;
            }
            return node;
        });
        file = visitor.VisitSourceFile(file);
        CheckEmit(context, file, expected);
    }
    private static void TestOmitTrailingSemicolon()
    {
        var factory = new TypeScript.Scanner.NodeFactory(new NodeFactoryHooks());
        var methodSignature = factory.NewMethodSignatureDeclaration(null, factory.NewIdentifier("m"), null, null,
            factory.NewNodeList(default(Slice<Node>)), factory.NewKeywordTypeNode(Kind.VoidKeyword));
        var file = ParseTypeScript("interface I {}", false);
        var defaultPrinter = Printer.NewPrinter(new PrinterOptions { NewLine = NewLineKind.LF }, new PrintHandlers(), null);
        Equal("m(): void;", defaultPrinter.Emit(methodSignature, file));
        var omitPrinter = Printer.NewPrinter(new PrinterOptions { NewLine = NewLineKind.LF, OmitTrailingSemicolon = true }, new PrintHandlers(), null);
        Equal("m(): void", omitPrinter.Emit(methodSignature, file));
        var forFile = ParseTypeScript("for (;;) {}", false);
        Equal("for (;;) { }", TrimOneLf(omitPrinter.EmitSourceFile(forFile)));
    }
}

public static partial class PrinterChecks
{
    // Original printer_test.go TestPartiallyEmittedExpression; genuine TypeEraser dependency.
    private static void TestPartiallyEmittedExpression()
    {
        var options = new CompilerOptions();
        var file = ParseTypeScript("return ((container.parent\n    .left as PropertyAccessExpression)\n    .expression as PropertyAccessExpression)\n    .expression;", false);
        var context = new EmitContext();
        file = TypeScript.Transformers.TsTransforms.TypeEraserTransformer.NewTypeEraserTransformer(new TypeScript.Transformers.TransformOptions { CompilerOptions = options, Context = context }).TransformSourceFile(file);
        CheckEmit(context, file, "return container.parent\n    .left\n    .expression\n    .expression;");
    }
}

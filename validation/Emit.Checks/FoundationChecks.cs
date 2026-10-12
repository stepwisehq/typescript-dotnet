// Authored public-behavior controls for the pinned emit synthesis dependency subsystem.
// These supplement original upstream name-generator assertions; no private-state equivalence gate.
#nullable disable
using TypeScript.Scanner;
using TypeScript.DiagnosticPrinting;
using TypeScript.Transformers;
namespace Emit.Checks;
public static class FoundationChecks
{
    public static readonly string[] Names = [
        "OriginalMetadataOwnership", "OriginalOverwriteAndParseNode", "GeneratedCloneIdentity",
        "NestedEnvironmentHoists", "EnvironmentPrologueMerge", "HelperDependenciesAndMoves",
        "ParameterTemporaryHoists", "FunctionAndIterationVisitorHooks", "FactoryExpressionConstruction",
        "ModifierFiltering", "RequiredSpliceOwnership"
    ];
    public static void Run(string name)
    {
        switch (name)
        {
            case "OriginalMetadataOwnership": OriginalMetadataOwnership(); break;
            case "OriginalOverwriteAndParseNode": OriginalOverwriteAndParseNode(); break;
            case "GeneratedCloneIdentity": GeneratedCloneIdentity(); break;
            case "NestedEnvironmentHoists": NestedEnvironmentHoists(); break;
            case "EnvironmentPrologueMerge": EnvironmentPrologueMerge(); break;
            case "HelperDependenciesAndMoves": HelperDependenciesAndMoves(); break;
            case "ParameterTemporaryHoists": ParameterTemporaryHoists(); break;
            case "FunctionAndIterationVisitorHooks": FunctionAndIterationVisitorHooks(); break;
            case "FactoryExpressionConstruction": FactoryExpressionConstruction(); break;
            case "ModifierFiltering": ModifierFiltering(); break;
            case "RequiredSpliceOwnership": RequiredSpliceOwnership(); break;
            default: throw new ArgumentException(name);
        }
    }
    public static int RunAll() { foreach (var name in Names) Run(name); return Names.Length; }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static Slice<Node> Nodes(params Node[] nodes) => Slice<Node>.FromArray(nodes);
    private static void OriginalMetadataOwnership()
    {
        var context = new EmitContext(); var factory = context.Factory;
        var source = factory.NewIdentifier("value"); source.Loc = new(1, 8);
        context.SetEmitFlags(source, EmitFlags.SingleLine | EmitFlags.LocalName);
        context.SetCommentRange(source, new(2, 7)); context.SetSourceMapRange(source, new(3, 6));
        context.SetTokenSourceMapRange(source, Kind.Identifier, new(4, 5));
        context.SetSnippetElement(source, new(SnippetKind.TabStop, 2));
        var helper = new EmitHelper { Name = "owned", Text = "owned();" }; context.AddEmitHelper(source, helper);
        context.AddSyntheticLeadingComment(source, Kind.MultiLineCommentTrivia, "leading", false);
        context.AddSyntheticTrailingComment(source, Kind.MultiLineCommentTrivia, "trailing", true);
        var type = factory.NewKeywordTypeNode(Kind.StringKeyword); context.SetTypeNode(source, type);
        var clone = source.Clone(factory);
        Check(context.Original(clone) == source, "clone original link");
        Check(context.EmitFlags(clone) == context.EmitFlags(source), "copied emit flags");
        Check(context.CommentRange(clone) == new TextRange(2, 7) && context.SourceMapRange(clone) == new TextRange(3, 6), "copied ranges");
        Check(context.TokenSourceMapRange(clone, Kind.Identifier) == (new TextRange(4, 5), true), "copied token range");
        context.SetTokenSourceMapRange(source, Kind.Identifier, new(10, 11));
        Check(context.TokenSourceMapRange(clone, Kind.Identifier) == (new TextRange(4, 5), true), "token map detached");
        Check(context.GetEmitHelpers(clone).Length == 1 && context.GetEmitHelpers(clone)[0] == helper, "helper identities copied");
        Check(!context.GetEmitHelpers(clone).SameStorageRange(context.GetEmitHelpers(source)), "helper header owns required clone");
        Check(context.SnippetElement(clone) == new SnippetElement(SnippetKind.TabStop, 2), "snippet copied");
        Check(context.GetSyntheticLeadingComments(clone).IsNil && context.GetSyntheticTrailingComments(clone).IsNil && context.GetTypeNode(clone) == null, "pinned copyFrom excludes comments and type node");
    }
    private static void OriginalOverwriteAndParseNode()
    {
        var context = new EmitContext(); var source = new TypeScript.Scanner.NodeFactory().NewIdentifier("source");
        var middle = context.Factory.NewIdentifier("middle"); var target = context.Factory.NewIdentifier("target");
        context.SetOriginal(middle, source); context.SetOriginal(target, middle);
        Check(context.MostOriginal(target) == source && context.ParseNode(target) == source, "original chain reaches parse node");
        bool threw = false; try { context.SetOriginal(target, source); } catch (InvalidOperationException) { threw = true; }
        Check(threw, "original rejects overwrite"); context.SetOriginalEx(target, source, true);
        Check(context.Original(target) == source, "explicit overwrite"); context.UnsetOriginal(target);
        Check(context.Original(target) == null && context.ParseNode(target) == null, "unset preserves synthesized node");
    }
    private static void GeneratedCloneIdentity()
    {
        var context = new EmitContext(); var generated = context.Factory.NewTempVariableEx(new() { Prefix = "A", Suffix = "B", Flags = GeneratedIdentifierFlags.ReservedInNestedScopes });
        var clone = generated.Clone(context.Factory);
        Check(context.HasAutoGenerateInfo(clone), "clone retains generated metadata");
        Check(context.GetAutoGenerateInfo(clone) != context.GetAutoGenerateInfo(generated), "generated info detached value copy");
        var names = new NameGenerator { Context = context, GetTextOfNode = static node => node.Text() };
        Check(names.GenerateName(generated) == "A_aB" && names.GenerateName(clone) == "A_aB", "generated ID shared by clone");
        var binary = context.Factory.NewCommaExpression(generated, clone);
        var data = binary.AsBinaryExpression();
        var updated = context.Factory.UpdateBinaryExpression(data, null, clone, null, data.OperatorToken, generated);
        Check(context.Original(updated) == binary && updated != binary, "updated node original hook");
    }
    private static void NestedEnvironmentHoists()
    {
        var context = new EmitContext(); var factory = context.Factory;
        context.StartVariableEnvironment(); context.AddVariableDeclaration(factory.NewIdentifier("outer")); context.AddLexicalDeclaration(factory.NewIdentifier("outerLet"));
        context.StartVariableEnvironment(); context.AddVariableDeclaration(factory.NewIdentifier("inner"));
        var inner = context.EndVariableEnvironment(); var outer = context.EndVariableEnvironment();
        Check(inner.Length == 1 && inner[0].AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes[0].Name().Text() == "inner", "nested var scope");
        Check(outer.Length == 2, "outer var and lexical scopes");
        Check((outer[0].AsVariableStatement().DeclarationList.Flags & NodeFlags.Let) == 0 && (outer[1].AsVariableStatement().DeclarationList.Flags & NodeFlags.Let) != 0, "var vs let declaration flags");
        Check((context.EmitFlags(outer[0]) & EmitFlags.CustomPrologue) != 0 && (context.EmitFlags(outer[1]) & EmitFlags.CustomPrologue) != 0, "hoist prologue markers");
    }
    private static void EnvironmentPrologueMerge()
    {
        var context = new EmitContext(); var factory = context.Factory;
        Node strict = factory.NewExpressionStatement(factory.NewStringLiteral("use strict", TokenFlags.None));
        Node custom = factory.NewExpressionStatement(factory.NewIdentifier("init")); context.SetEmitFlags(custom, EmitFlags.CustomPrologue);
        Node body = factory.NewExpressionStatement(factory.NewIdentifier("body"));
        var source = Nodes(strict, body); var declarations = Nodes(factory.NewExpressionStatement(factory.NewStringLiteral("use strict", TokenFlags.None)), custom);
        var merged = context.MergeEnvironment(source, declarations);
        Check(merged.Length == 3 && merged[0] == strict && merged[1] == custom && merged[2] == body, "prologue duplicate removal and initialization ordering");
        Check(source.Length == 2 && source[1] == body, "source owner unchanged");
        var unchanged = context.MergeEnvironment(source, default); Check(unchanged.SameStorageRange(source), "no-op preserves header");
    }
    private static void HelperDependenciesAndMoves()
    {
        var context = new EmitContext(); var dependency = new EmitHelper { Name = "dependency" }; var helper = new EmitHelper { Name = "helper", Dependencies = Slice<EmitHelper>.FromArray([dependency]) };
        context.RequestEmitHelper(helper); context.RequestEmitHelper(helper);
        var requested = context.ReadEmitHelpers();
        Check(requested.Length == 2 && requested[0] == dependency && requested[1] == helper, "dependency-first ordered unique helpers");
        Check(context.ReadEmitHelpers().IsNil, "empty read is nil"); context.RequestEmitHelper(helper);
        Check(requested[0] == dependency && requested[1] == helper, "read results detached from reusable storage");
        var source = context.Factory.NewIdentifier("source"); var target = context.Factory.NewIdentifier("target");
        context.AddEmitHelper(source, dependency, helper, dependency); context.MoveEmitHelpers(source, target, h => h == helper);
        Check(context.GetEmitHelpers(source).Length == 1 && context.GetEmitHelpers(source)[0] == dependency && context.GetEmitHelpers(target).Length == 1 && context.GetEmitHelpers(target)[0] == helper, "helper move predicate and compaction");
        bool threw = false; try { context.RequestEmitHelper(new EmitHelper { Scoped = true }); } catch (InvalidOperationException) { threw = true; }
        Check(threw, "scoped helper cannot escape into file helper requests");
    }
    private static void ParameterTemporaryHoists()
    {
        var context = new EmitContext(); var factory = context.Factory;
        var initializer = factory.NewIdentifier("init"); var parameter = factory.NewParameterDeclaration(null, null, factory.NewIdentifier("value"), null, null, initializer);
        var visitor = context.NewNodeVisitor(node => { if (node == parameter) context.AddVariableDeclaration(factory.NewTempVariable()); return node; });
        var parameters = context.VisitParameters(factory.NewNodeList(Nodes(parameter)), visitor);
        Check(parameters.Nodes[0].Initializer() == null, "parameter initializer moved into body");
        var declarations = context.EndVariableEnvironment();
        Check(declarations.Length == 2 && declarations[0].Kind == Kind.VariableStatement && declarations[1].Kind == Kind.IfStatement, "temp declaration precedes moved default assignment");
        Check((context.EmitFlags(initializer) & EmitFlags.NoSourceMap) != 0, "initializer map behavior");
    }
    private static void FunctionAndIterationVisitorHooks()
    {
        var context = new EmitContext(); var factory = context.Factory;
        context.StartVariableEnvironment(); context.AddVariableDeclaration(factory.NewIdentifier("temp"));
        var visitor = context.NewNodeVisitor(static node => node);
        var expression = factory.NewIdentifier("result"); var body = context.VisitFunctionBody(expression, visitor);
        Check(body.Kind == Kind.Block && body.Statements().Length == 2 && body.Statements()[1].Kind == Kind.ReturnStatement, "arrow expression converted with hoisted temp");
        Node statement = factory.NewExpressionStatement(factory.NewIdentifier("body"));
        var iterationVisitor = context.NewNodeVisitor(node => { if (node == statement) context.AddLexicalDeclaration(factory.NewIdentifier("letTemp")); return node; });
        var iterationBody = context.VisitIterationBody(statement, iterationVisitor);
        Check(iterationBody.Kind == Kind.Block && iterationBody.Statements().Length == 2 && iterationBody.Statements()[1] == statement, "iteration lexical declarations block");
        var droppingVisitor = context.NewNodeVisitor(static _ => null); var empty = context.VisitEmbeddedStatement(statement, droppingVisitor);
        Check(empty.Kind == Kind.EmptyStatement && context.Original(empty) == statement, "deleted embedded statement retains original empty statement");
    }
    private static void FactoryExpressionConstruction()
    {
        // Go's factory/visitor hooks are embedded value records, not shared hook owners.
        var createCount = 0;
        var hooks = new NodeFactoryHooks { OnCreate = _ => createCount++ };
        var hookFactory = new TypeScript.Scanner.NodeFactory(hooks);
        hooks.OnCreate = _ => throw new InvalidOperationException("factory borrowed hook record");
        hookFactory.NewIdentifier("hook-copy");
        Check(createCount == 1, "factory owns callback record copy");
        var visitorHooks = new NodeVisitorHooks { VisitNode = static (node, _) => node };
        var first = new NodeVisitor(static node => node, hookFactory, visitorHooks);
        var second = new NodeVisitor(static node => node, hookFactory, visitorHooks);
        visitorHooks.VisitNode = null;
        first.Hooks.VisitNode = null;
        Check(second.Hooks.VisitNode != null, "visitor hook copies mutate independently");
        var context = new EmitContext(); var factory = context.Factory;
        var contextualFirst = context.NewNodeVisitor(static node => node);
        var contextualSecond = context.NewNodeVisitor(static node => node);
        contextualFirst.Hooks.VisitParameters = null;
        Check(contextualSecond.Hooks.VisitParameters != null, "context hook template copies remain independent");
        var contextualThird = context.NewNodeVisitor(static node => node);
        Check(contextualThird.Hooks.VisitParameters != null, "visitor mutation leaves later hook construction intact");
        var a = factory.NewIdentifier("a"); var b = factory.NewIdentifier("b"); var c = factory.NewIdentifier("c");
        var nestedParent = factory.NewIdentifier("existing-parent");
        var firstTree = factory.NewBinaryExpression(null, a, null, factory.NewToken(Kind.PlusToken), b);
        firstTree.Parent = nestedParent;
        Ast.SetParentInChildren(firstTree);
        Check(firstTree.Parent == nestedParent && a.Parent == firstTree && b.Parent == firstTree,
            "parent setter preserves root parent and assigns ordered children");
        var secondTree = factory.NewParenthesizedExpression(c);
        Ast.SetParentInChildren(secondTree);
        Check(secondTree.Parent == null && c.Parent == secondTree && a.Parent == firstTree,
            "reused parent setter does not retain prior tree state");
        var comma = factory.InlineExpressions(Nodes(factory.NewCommaExpression(a, b), c));
        Check(comma.AsBinaryExpression().Left.AsBinaryExpression().Left == a && comma.AsBinaryExpression().Right == c, "synthesized comma flattening preserves order");
        Check(factory.InlineExpressions(default) == null && factory.InlineExpressions(Nodes(a)) == a, "inline nil/single expression identities");
        var node = factory.NewClassPrivateFieldGetHelper(a, b, PrivateIdentifierKind.Field, null);
        Check(node.Arguments().Length == 3 && node.Arguments()[2].Text() == "f", "private get helper arguments");
        var requested = context.ReadEmitHelpers(); Check(requested.Length == 1 && requested[0].ImportName == "__classPrivateFieldGet", "factory helper registration");
        var emptyLiteral = factory.NewStringLiteralFromNode(factory.NewVoidZeroExpression());
        Check(emptyLiteral.Text() == "" && !emptyLiteral.TextValue().IsNull, "nontext source defaults to Go empty literal text");
        var optionalTarget = factory.NewCallExpression(a, null, null, factory.NewNodeList(default(Slice<Node>)), NodeFlags.OptionalChain);
        var method = factory.NewMethodCall(optionalTarget, b, default); Check((method.Flags & NodeFlags.OptionalChain) != 0, "method call preserves optional call target");
    }
    private static void ModifierFiltering()
    {
        var context = new EmitContext(); var factory = context.Factory;
        var modifiers = factory.NewModifierList(Nodes(factory.NewModifier(Kind.ExportKeyword), factory.NewModifier(Kind.AsyncKeyword)));
        var kept = Transformers.ExtractModifiers(context, modifiers, ModifierFlags.Export);
        Check(kept.Nodes.Length == 1 && kept.Nodes[0].Kind == Kind.ExportKeyword, "modifier visitor filter");
        Check(Transformers.ExtractModifiers(context, null, ModifierFlags.All) == null, "nil modifier identity");
    }
    private static void RequiredSpliceOwnership()
    {
        var source = Slice<int>.Make(2, 8); source[0] = 1; source[1] = 2;
        var unchanged = Core.Splice(source, 1, 0, default); Check(unchanged.SameStorageRange(source), "no-op splice keeps header");
        var result = Core.Splice(source, 1, 0, Slice<int>.FromArray([3]));
        Check(result.Length == 3 && result[0] == 1 && result[1] == 3 && result[2] == 2 && source.Length == 2 && source[1] == 2, "splice copies despite spare source capacity");
        Check(!result.SameStorageRange(source), "splice required fresh owner");
        var deleted = Core.Splice(source, 1, int.MaxValue, default);
        Check(deleted.Length == 1 && deleted[0] == 1, "64-bit Go end calculation for maximum managed delete count");
    }
}

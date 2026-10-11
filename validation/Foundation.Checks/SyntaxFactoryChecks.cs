using TypeScript.Scanner;

namespace TypeScript.Foundation;
public static partial class SyntaxServiceChecks
{
    private static object Factories()
    {
        var events = new List<object>();
        var factory = new NodeFactory(new NodeFactoryHooks { OnCreate = n => events.Add(new object[] { "create", Snap(n) }), OnUpdate = (n, o) => events.Add(new object[] { "update", Snap(n), Snap(o) }), OnClone = (n, o) => events.Add(new object[] { "clone", Snap(n), Snap(o) }) });
        var name = factory.NewIdentifier("alpha");
        var number = factory.NewNumericLiteral("12", TokenFlags.None);
        var original = factory.NewVariableDeclaration(name, null, null, number);
        original.Loc = new(10, 20);
        original.Flags = NodeFlags.AwaitContext;
        original.Parent = Parse("").AsNode();
        events.Clear();
        var unchanged = factory.UpdateVariableDeclaration(original.AsVariableDeclaration(), name, null, null, number);
        var replacement = factory.NewNumericLiteral("20", 0);
        var changed = factory.UpdateVariableDeclaration(original.AsVariableDeclaration(), name, null, null, replacement);
        var clone = original.Clone(factory);
        var lists = new List<object>();
        foreach (var nodes in new List<Node>? []
        {
            null,
            [],
            [name, number]
        }

        )
        {
            var list = factory.NewNodeList(nodes!);
            list.Loc = new(4, 8);
            var copy = list.Clone(factory);
            lists.Add(new object? [] { !ReferenceEquals(list, copy), list.Nodes.IsNil, copy.Nodes.IsNil, Core.Same(list.Nodes, copy.Nodes), copy.Pos(), copy.End(), Nodes(copy.Nodes) });
        }

        return Map(("unchangedIdentity", ReferenceEquals(unchanged, original)), ("changedIdentity", !ReferenceEquals(changed, original)), ("changedParentNil", changed.Parent is null), ("changedInitializer", ReferenceEquals(changed.Initializer(), replacement)), ("cloneParentNil", clone.Parent is null), ("cloneSharedName", ReferenceEquals(clone.Name(), name)), ("events", events.ToArray()), ("lists", lists.ToArray()), ("nodeCount", factory.NodeCount()), ("textCount", factory.TextCount()));
    }

    private static object Visitors()
    {
        var factory = new NodeFactory();
        var a = factory.NewIdentifier("a");
        var b = factory.NewIdentifier("b");
        var c = factory.NewIdentifier("c");
        var d = factory.NewIdentifier("d");
        var list = factory.NewNodeList([a, b, c]);
        list.Loc = new(3, 9);
        var same = Ast.NewNodeVisitor(n => n, factory, new()).VisitNodes(list);
        var visitor = Ast.NewNodeVisitor(n => ReferenceEquals(n, a) ? null : ReferenceEquals(n, b) ? factory.NewSyntaxList([b, d]) : n, factory, new());
        var changed = visitor.VisitNodes(list);
        var empty = Ast.NewNodeVisitor(_ => null, factory, new()).VisitNodes(factory.NewNodeList([a]));
        var one = Ast.NewNodeVisitor(_ => factory.NewSyntaxList([d]), factory, new()).VisitNode(a);
        var multiple = Ast.NewNodeVisitor(_ => factory.NewSyntaxList([a, b]), factory, new());
        var block = multiple.VisitEmbeddedStatement(a);
        return Map(("sameList", ReferenceEquals(same, list)), ("changedList", !ReferenceEquals(changed, list)), ("changedLoc", new object[] { changed.Pos(), changed.End() }), ("changedOrder", Nodes(changed.Nodes)), ("deletedNodesNil", empty.Nodes.IsNil), ("oneLiftIdentity", ReferenceEquals(one, d)), ("multiNodeThrows", Caught(() => multiple.VisitNode(a))), ("embeddedBlock", Snap(block)), ("embeddedChildren", Nodes(block.AsBlock().Statements.Nodes)), ("nilInput", visitor.VisitNode(null)is null), ("nilList", visitor.VisitNodes(null)is null));
    }

    private static object Accessors()
    {
        var factory = new NodeFactory();
        var identifier = factory.NewIdentifier("x");
        var jsx = factory.NewJsxText("text", false);
        var literal = factory.NewStringLiteral(Raw("é"), 0);
        return Map(("identifierParameterListThrows", Caught(() => identifier.ParameterList())), ("identifierParametersThrows", Caught(() => identifier.Parameters())), ("jsxTextThrows", Caught(() => jsx.Text())), ("literalText", Bytes(literal.Text())));
    }
}

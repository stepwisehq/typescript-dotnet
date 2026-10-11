// Authored alias/reentry regression checks, not additional upstream test ports.
// Source: parser.go parseListIndex:612 / parseDelimitedList:651 and core/arena.go Clone.
using TypeScript.Scanner;

namespace TypeScript.Foundation;

public static class ParserListScratchChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["detached-list-boundary", "nested-large-lists"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "detached-list-boundary": Detached(); break;
            case "nested-large-lists": Nested(); break;
            default: throw new ArgumentException("Unknown parser list scratch check: " + name, nameof(name));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Detached()
    {
        var arena = new GoArena<Node>();
        Require(arena.CloneHeader(default(Slice<Node>)).IsNil, "Nil scratch publishes nil node storage");
        Require(arena.CloneHeader(Slice<Node>.Make(0, 16)).IsNil, "Allocated empty scratch also publishes nil node storage");
        Require(arena.CloneHeader(ReadOnlySpan<Node>.Empty).IsNil, "Empty borrowed span publishes nil node storage");
        var factory = new NodeFactory();
        Node first = factory.NewIdentifier("first"), second = factory.NewIdentifier("second"), replacement = factory.NewIdentifier("replacement");
        Node[] borrowed = [replacement, first, second, replacement];
        var spanPublished = arena.CloneHeader(borrowed.AsSpan(1, 2));
        borrowed[1] = replacement;
        spanPublished[1] = replacement;
        Require(spanPublished.Count == 2 && spanPublished.Capacity == 2 && ReferenceEquals(first, spanPublished[0]) && ReferenceEquals(second, borrowed[2]), "Borrowed offset span and published list have clipped detached storage with shallow node identity");
        var scratch = Slice<Node>.Make(0, 16).Append(first).Append(second);
        var published = arena.CloneHeader(scratch);
        Require(published.Count == 2 && published.Capacity == 2 && ReferenceEquals(first, published[0]) && ReferenceEquals(second, published[1]), "Clone is clipped, shallow, ordered and preserves node identity");
        scratch[0] = replacement;
        Require(ReferenceEquals(first, published[0]), "Later scratch writes cannot change published list storage");
        published[1] = replacement;
        Require(ReferenceEquals(second, scratch[1]), "Public list writes cannot change scratch storage");
        var offset = arena.CloneHeader(scratch.Slice2(1));
        Require(offset.Count == 1 && ReferenceEquals(second, offset[0]), "Clone honors logical offset and length");
        for (int i = 0; i < 32; i++) scratch = scratch.Append(replacement);
        Require(published.Count == 2 && ReferenceEquals(first, published[0]), "Scratch growth cannot alter earlier published length or values");
        var list = factory.NewNodeList(published);
        var clone = list.Clone(factory);
        clone.Nodes[0] = replacement;
        Require(ReferenceEquals(list.Nodes[0], replacement), "NodeList clone shares existing element slots");
        clone.Nodes = clone.Nodes.Append(second);
        Require(list.Nodes.Length == 2 && clone.Nodes.Length == 3, "Clone append retains independent header lengths");
        clone.Nodes[0] = first;
        Require(ReferenceEquals(list.Nodes[0], replacement), "Append past clipped capacity detaches the cloned header");
    }

    private static void Nested()
    {
        foreach (int count in new[] { 15, 16, 17, 20 }) Nested(count);
    }

    private static void Nested(int count)
    {
        // Cover both sides of the source's initial capacity16, including its exact boundary.
        string parameters = string.Join(",", Enumerable.Range(0, count).Select(i => "p" + i));
        string elements = string.Join(",", Enumerable.Range(0, count));
        string body = string.Join("", Enumerable.Range(0, count).Select(i => "const v" + i + "=[" + elements + "];"));
        string text = string.Join("", Enumerable.Range(0, count).Select(i => "function f" + i + "(" + parameters + "){" + body + "}"));
        var file = Parser.ParseSourceFile(new("/nested.ts", "/nested.ts"), text, ScriptKind.TS);
        Require((file.Diagnostics()?.Count ?? 0) == 0, "Nested list parsing has no syntax diagnostics");
        var functions = file.Statements.Nodes;
        Require(functions.Count == count, "Outer source-element list remains complete");
        var allStatementLists = new List<Slice<Node>>();
        foreach (int f in Enumerable.Range(0, count))
        {
            var function = functions[f];
            Require(function.Kind == Kind.FunctionDeclaration && function.Name().Text() == "f" + f, "Nested callback preserves outer declaration order");
            Require(function.Parameters().Count == count, "Delimited parameter list grows independently");
            for (int p = 0; p < count; p++) Require(function.Parameters()[p].Name().Text() == "p" + p, "Parameter order survives nested parsing");
            var statements = function.Body().AsBlock().Statements.Nodes;
            Require(statements.Count == count && allStatementLists.All(other => !Core.Same(other, statements)), "Each returned block list has independent storage");
            allStatementLists.Add(statements);
            for (int v = 0; v < count; v++)
            {
                var declarationNodes = statements[v].AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes;
                Require(declarationNodes.Length == 1, "Each statement retains exactly one declaration");
                var declaration = declarationNodes[0];
                Require(declaration.Name().Text() == "v" + v, "Outer statement order survives inner delimited lists");
                var values = declaration.Initializer().AsArrayLiteralExpression().Elements.Nodes;
                Require(values.Count == count, "Inner array list is complete");
                for (int n = 0; n < count; n++) Require(values[n].Text() == n.ToString(System.Globalization.CultureInfo.InvariantCulture), "Inner element order survives growth");
            }
        }
        Node original = functions[0];
        var otherFile = Parser.ParseSourceFile(new("/later.ts", "/later.ts"), "function later() {}", ScriptKind.TS);
        Require(ReferenceEquals(original, functions[0]) && functions.Count == count && otherFile.Statements.Nodes.Count == 1, "Subsequent parse cannot mutate published lists");
    }
}

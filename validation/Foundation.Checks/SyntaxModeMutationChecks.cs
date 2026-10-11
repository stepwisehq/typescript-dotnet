using TypeScript.Scanner;

namespace TypeScript.Foundation;
public static partial class SyntaxServiceChecks
{
    private static object ModeControls() => new[]
    {
        0,
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        999,
        -1
    }.Select(k =>
    {
        SourceFile? file = null;
        bool threw = Caught(() => file = Parse("const x=1", (ScriptKind)k));
        return (object)new object? []
        {
            k,
            threw,
            file is null ? null : new object[]
            {
                (int)file.ScriptKind,
                (int)file.LanguageVariant,
                Snap(file.AsNode()),
                file.Diagnostics().Length
            }
        };
    }).ToArray();
    private static object? EntityGraph(Node? node)
    {
        if (node is null)
            return null;
        var children = new List<object?>();
        node.ForEachChild(c =>
        {
            children.Add(EntityGraph(c));
            return false;
        });
        return new object? []
        {
            Snap(node),
            Bytes(node.Kind == Kind.Identifier ? node.Text() : ""),
            children.ToArray()
        };
    }

    private static object EntityControls() => new[]
    {
        "A",
        "A.B.C",
        " café . 名 ",
        "A.default",
        "A..B",
        "a()",
        "",
        "A<B>",
        "\\u0041.B"
    }.Select(s => (object)new object? [] { s, EntityGraph(Parser.ParseIsolatedEntityName(s)) }).ToArray();
    private static object MutationControls()
    {
        var factory = new NodeFactory();
        var a = factory.NewIdentifier("a");
        var b = factory.NewIdentifier("b");
        var c = factory.NewIdentifier("c");
        var rows = new List<object>();
        foreach (var mode in new[]
        {
            "ForEachChild",
            "VisitSliceSame",
            "VisitSliceChanged",
            "SyntaxList"
        }

        )
        {
            var caller = Slice<Node>.FromArray([a, b]);
            var seen = new List<string>();
            Slice<Node> result;
            switch (mode)
            {
                case "ForEachChild":
                    var list = factory.NewNodeList(caller);
                    var block = factory.NewBlock(list, true);
                    block.ForEachChild(n =>
                    {
                        seen.Add(n.Text());
                        if (seen.Count == 1)
                            list.Nodes = list.Nodes.Append(c);
                        return false;
                    });
                    caller = list.Nodes;
                    result = caller;
                    break;
                case "VisitSliceSame":
                case "VisitSliceChanged":
                    var visitor = Ast.NewNodeVisitor(n =>
                    {
                        seen.Add(n.Text());
                        if (seen.Count == 1)
                            caller = caller.Append(c);
                        return mode == "VisitSliceChanged" && ReferenceEquals(n, a) ? null : n;
                    }, factory, new());
                    (result, _) = visitor.VisitSlice(caller);
                    break;
                default:
                    var syntaxList = factory.NewSyntaxList(caller);
                    var v = Ast.NewNodeVisitor(n =>
                    {
                        seen.Add(n.Text());
                        if (seen.Count == 1)
                            syntaxList.AsSyntaxList().Children = syntaxList.AsSyntaxList().Children.Append(c);
                        return n;
                    }, factory, new());
                    var updated = syntaxList.VisitEachChild(v);
                    caller = syntaxList.AsSyntaxList().Children;
                    result = updated.AsSyntaxList().Children;
                    break;
            }

            rows.Add(new object? [] { mode, seen.Cast<object>().ToArray(), Nodes(result), Nodes(caller), result.IsNil });
        }

        return rows.ToArray();
    }

    private static object SortControls()
    {
        StableSortControls();
        // Same deterministic key/id inputs as the actual Go slices.SortFunc probe.
        var rows = new List<object>();
        foreach (int length in new[]
        {
            0,
            1,
            2,
            11,
            12,
            13,
            64,
            129
        }

        )
            foreach (string pattern in new[]
            {
                "equal",
                "ascending",
                "descending",
                "modular"
            }

            )
            {
                var input = Enumerable.Range(0, length).Select(i => (Key: pattern switch
                {
                    "equal" => 0,
                    "ascending" => i / 3,
                    "descending" => (length - i) / 3,
                    _ => (i * 17) % 7
                }, Id: i)).ToList();
                var sorted = input.ToList();
                GoSlices.SortFunc(sorted, (a, b) => a.Key.CompareTo(b.Key));
                var sortedHeader = Slice<(int Key, int Id)>.FromArray(input.ToArray());
                GoSlices.SortFunc(sortedHeader, (a, b) => a.Key.CompareTo(b.Key));
                if (!sortedHeader.AsSpan().SequenceEqual(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(sorted)))
                    throw new InvalidOperationException("Borrowed Slice sort differs from the same genuine Go sort row.");
                rows.Add(new object[] { length, pattern, sorted.Select(p => (object)new object[] { p.Key, p.Id }).ToArray() });
            }

        return rows.ToArray();
    }

    // Authored extension: stable duplicate order across Go's initial 20-element
    // insertion blocks and subsequent merge. It adds no original oracle rows.
    private static void StableSortControls()
    {
        GoSlices.SortStableFunc(default(Slice<(int Key, int Id)>), (_, _) =>
            throw new InvalidOperationException("Nil stable sort must not compare elements."));
        GoSlices.SortStableFunc(Slice<(int Key, int Id)>.Make(0), (_, _) =>
            throw new InvalidOperationException("Empty stable sort must not compare elements."));
        foreach (int length in new[] { 21, 65 })
            foreach (bool equalKeys in new[] { false, true })
            {
                var input = Enumerable.Range(0, length)
                    .Select(i => (Key: equalKeys ? 0 : (length - i) % 5, Id: i)).ToArray();
                // Enumerable.OrderBy is a separate standard-library stable sort.
                var expected = input.OrderBy(value => value.Key).ToArray();
                var sorted = Slice<(int Key, int Id)>.FromArray(input);
                GoSlices.SortStableFunc(sorted, (a, b) => a.Key.CompareTo(b.Key));
                if (!sorted.AsSpan().SequenceEqual(expected))
                    throw new InvalidOperationException("Stable sort must retain input order among equal keys across merge blocks.");
            }
    }
}

// Authored full structural clone checks complementing original child-count assertions.
using System.Text.Json;
using System.Text.Json.Nodes;
using TypeScript.Scanner;
using TypeScript.Scanner.ParserValidation;

namespace TypeScript.Foundation;
public static class CloneGraphChecks
{
    public static IEnumerable<string> Cases
    {
        get
        {
            using var fixture = Load();
            foreach (var row in fixture.RootElement.EnumerateArray())
                yield return row.GetProperty("name").GetString() + "/" + row.GetProperty("rootIndex").GetInt32() + "/" + row.GetProperty("cloneMode").GetString();
        }
    }

    private static JsonDocument Load() => JsonDocument.Parse(typeof(CloneGraphChecks).Assembly.GetManifestResourceStream("TypeScript.Foundation.CloneGraphs.json") ?? throw new InvalidOperationException("Missing clone graphs"));
    private static JsonNode? Graph(SourceFile file, Node node)
    {
        var encoder = new Encoder(file, false, true);
        encoder.Add(node);
        encoder.FinishParents();
        var json = JsonSerializer.SerializeToElement(new Response { Nodes = encoder.Nodes }, ContractJson.Default.Response);
        return JsonNode.Parse(json.GetProperty("nodes").GetRawText());
    }

    public static void Run(string name)
    {
        using var fixtures = Load();
        var row = fixtures.RootElement.EnumerateArray().Single(r => name == r.GetProperty("name").GetString() + "/" + r.GetProperty("rootIndex").GetInt32() + "/" + r.GetProperty("cloneMode").GetString());
        var file = Parser.ParseSourceFile(new("/index.ts", "/index.ts"), row.GetProperty("source").GetString()!, (ScriptKind)row.GetProperty("scriptKind").GetInt32());
        var roots = new List<Node>
        {
            file.AsNode()
        };
        if (row.GetProperty("name").GetString() == "LazyJSDoc")
            file.ForEachChild(n =>
            {
                roots.AddRange(n.JSDoc(file) ?? []);
                return false;
            });
        var root = roots[row.GetProperty("rootIndex").GetInt32()];
        var factory = new NodeFactory();
        var clone = row.GetProperty("cloneMode").GetString() switch
        {
            "deep" => factory.DeepCloneNode(root),
            "reparse" => factory.DeepCloneReparse(root),
            "shallow" => root.Clone(factory),
            _ => throw new InvalidOperationException(name)};
        foreach (var pair in new[]
        {
            ("original", root),
            ("clone", clone)
        }

        )
        {
            var actual = Graph(file, pair.Item2);
            var expected = JsonNode.Parse(row.GetProperty(pair.Item1).GetRawText());
            if (!JsonNode.DeepEquals(expected, actual))
                throw new InvalidOperationException(name + ": " + pair.Item1 + " full graph differs from original Go; retain fixture and inspect first structural difference");
        }

        if (!ReferenceEquals(root, clone) != row.GetProperty("distinct").GetBoolean())
            throw new InvalidOperationException(name + ": clone root identity differs");
    }
}

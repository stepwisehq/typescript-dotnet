// Authored independent original-Go parity for the four exported binder helpers.
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TypeScript.Scanner;
using TypeScript.Scanner.BinderValidation;
using Encoder = TypeScript.Scanner.ParserValidation.Encoder;

namespace TypeScript.Binding;
public static class BindingHelperChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["PrivateNames", "StrictPrologues", "ValueDeclarations", "ContainerFlags", "DirectiveLifecycle", "ObserverSensitivity", "OrderSensitivity"];

    private static JsonNode? Json(object? value) => value switch
    {
        null => null,
        bool v => JsonValue.Create(v),
        string v => JsonValue.Create(v),
        int v => JsonValue.Create(v),
        object[] values => new JsonArray(values.Select(Json).ToArray()),
        Dictionary<string, object?> values => new JsonObject(values.Select(v => KeyValuePair.Create(v.Key, Json(v.Value)))),
        _ => throw new InvalidOperationException("Unencoded helper observation")};
    private static Dictionary<string, object?> Map(params (string Name, object? Value)[] values) => values.ToDictionary(v => v.Name, v => v.Value);
    private static string Bytes(string value) => Convert.ToBase64String(Encoding.Latin1.GetBytes(value));
    private static SourceFile Parse(JsonElement row, int kind = 3) => Parser.ParseSourceFile(new("/same.ts", "/same.ts"), Convert.FromBase64String(row.GetProperty("textBase64").GetString()!), (ScriptKind)kind);
    public static void Run(string name)
    {
        using var stream = typeof(BindingHelperChecks).Assembly.GetManifestResourceStream("TypeScript.Binding.Helpers.json") ?? throw new InvalidOperationException("Missing original Go binder helper observations");
        using var expected = JsonDocument.Parse(stream);
        var input = expected.RootElement.GetProperty(name);
        object actual = name switch
        {
            "PrivateNames" => PrivateNames(input),
            "StrictPrologues" => StrictPrologues(input),
            "ValueDeclarations" => ValueDeclarations(input),
            "ContainerFlags" => ContainerFlags(input),
            "DirectiveLifecycle" => DirectiveLifecycle(),
            "ObserverSensitivity" => ObserverSensitivity(),
            "OrderSensitivity" => OrderSensitivity(),
            _ => throw new ArgumentException("Unknown binder helper")};
        using var observed = actual is JsonElement encoded ? JsonDocument.Parse(encoded.GetRawText()) : JsonDocument.Parse(Json(actual)!.ToJsonString());
        if (!JsonElement.DeepEquals(input, observed.RootElement))
            throw new InvalidOperationException(name + ": original=" + input.GetRawText() + ", actual=" + observed.RootElement.GetRawText());
    }

    private static object PrivateNames(JsonElement rows) => rows.EnumerateArray().Select(row =>
    {
        var file = Parser.ParseSourceFile(new("/same.ts", "/same.ts"), "class C {}", ScriptKind.TS);
        Binder.BindSourceFile(file);
        var symbol = file.Statements.Nodes[0].Symbol();
        string id = row.GetProperty("idBits").GetString()!;
        // A friend accesses the actual atomic ID cell; no public or algorithm replacement.
        Interlocked.Exchange(ref symbol.id, unchecked((long)ulong.Parse(id, CultureInfo.InvariantCulture)));
        string descriptionBase64 = row.GetProperty("descriptionBase64").GetString()!;
        string description = Encoding.Latin1.GetString(Convert.FromBase64String(descriptionBase64));
        string name = Binder.GetSymbolNameForPrivateIdentifier(symbol, description);
        return (object)Map(("idBits", id), ("descriptionBase64", descriptionBase64), ("nameBase64", Bytes(name)), ("repeatSame", name == Binder.GetSymbolNameForPrivateIdentifier(symbol, description)));
    }).ToArray();
    private static object StrictPrologues(JsonElement rows) => rows.EnumerateArray().Select(row =>
    {
        var file = Parse(row);
        bool functionBody = row.GetProperty("functionBody").GetBoolean();
        var statements = functionBody ? file.Statements.Nodes[0].Body().Statements() : file.Statements.Nodes;
        var found = Binder.FindUseStrictPrologue(file, statements);
        int index = found is null ? -1 : Core.FindIndex(statements, node => ReferenceEquals(node, found));
        return (object)Map(("textBase64", row.GetProperty("textBase64").GetString()), ("functionBody", functionBody), ("index", index), ("pos", found?.Pos()), ("end", found?.End()));
    }).ToArray();
    private static object ValueDeclarations(JsonElement input)
    {
        var file = Parse(input);
        var statements = file.Statements.Nodes;
        var nodes = new Dictionary<string, Node>
        {
            ["moduleA"] = statements[0],
            ["moduleB"] = statements[1],
            ["function"] = statements[2],
            ["variable"] = statements[3].AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes[0],
            ["binary"] = statements[4].Expression(),
            ["call"] = statements[5].Expression(),
            ["identifier"] = statements[6].Expression(),
            ["access"] = statements[7].Expression(),
        };
        object[] rows = input.GetProperty("rows").EnumerateArray().Select(row =>
        {
            var symbol = new Symbol();
            var selected = new List<object>();
            string[] steps = row.GetProperty("steps").EnumerateArray().Select(v => v.GetString()!).ToArray();
            foreach (string step in steps)
            {
                Binder.SetValueDeclaration(symbol, nodes[step]);
                selected.Add(nodes.Single(pair => ReferenceEquals(pair.Value, symbol.ValueDeclaration)).Key);
            }

            return (object)Map(("steps", steps.Cast<object>().ToArray()), ("selected", selected.ToArray()));
        }).ToArray();
        return Map(("textBase64", input.GetProperty("textBase64").GetString()), ("rows", rows));
    }

    private static object ContainerFlags(JsonElement rows) => rows.EnumerateArray().Select(row =>
    {
        int kind = row.GetProperty("scriptKind").GetInt32();
        var file = Parse(row, kind);
        var nodes = new List<object>();
        bool Visit(Node node)
        {
            nodes.Add(Map(("kind", (int)node.Kind), ("pos", node.Pos()), ("end", node.End()), ("parentKind", node.Parent is null ? null : (int)node.Parent.Kind), ("flags", (int)Binder.GetContainerFlags(node))));
            node.ForEachChild(Visit);
            return false;
        }

        Visit(file.AsNode());
        return (object)Map(("textBase64", row.GetProperty("textBase64").GetString()), ("scriptKind", kind), ("nodes", nodes.ToArray()));
    }).ToArray();
    private static object DirectiveLifecycle()
    {
        var scanner = new TypeScript.Scanner.Scanner();
        var rows = new List<object>();
        void Capture(string stage)
        {
            var directives = scanner.CommentDirectives();
            rows.Add(Map(("stage", stage), ("nil", directives is null), ("directives", (directives ?? []).Select(d => (object)Map(("pos", d.Loc.Pos()), ("end", d.Loc.End()), ("kind", (int)d.Kind))).ToArray()), ("token", (int)scanner.Token()), ("start", scanner.TokenStart()), ("end", scanner.TokenEnd())));
        }

        Capture("new");
        scanner.SetText("// @ts-ignore\nx;\n// @ts-expect-error\ny;");
        Capture("set-directives");
        var zero = scanner.Mark();
        scanner.Scan();
        Capture("first");
        var one = scanner.Mark();
        scanner.Scan();
        Capture("semicolon");
        scanner.Scan();
        Capture("second");
        scanner.Rewind(one);
        Capture("rewind-one");
        scanner.Rewind(zero);
        Capture("rewind-zero");
        scanner.Scan();
        Capture("replay-first");
        scanner.SetText("plain;");
        Capture("set-plain");
        scanner.Scan();
        Capture("plain-token");
        scanner.SetText("");
        Capture("clear-by-empty-text");
        scanner.Reset();
        Capture("reset");
        scanner.SetText("// @ts-ignore\nx;");
        scanner.Scan();
        Capture("new-directive");
        scanner.Reset();
        Capture("reset-populated");
        scanner.Reset();
        Capture("repeat-reset");
        return rows.ToArray();
    }

    private static JsonElement ObserverSensitivity()
    {
        var file = Parser.ParseSourceFile(new("/same.ts", "/same.ts"), "a; b;", ScriptKind.TS);
        Binder.BindSourceFile(file);
        var shared = new SymbolTable();
        var owner = new Symbol
        {
            Name = "observer",
            Flags = SymbolFlags.Property,
            Members = shared,
            Exports = shared
        };
        file.Symbol = owner;
        file.Locals = shared;
        var tail = new FlowList
        {
            Flow = new FlowNode
            {
                Flags = FlowFlags.Start
            }
        };
        var head = new FlowList
        {
            Flow = new FlowNode
            {
                Flags = FlowFlags.Assignment
            },
            Next = tail
        };
        var loop = new FlowNode
        {
            Flags = FlowFlags.LoopLabel,
            Antecedents = head
        };
        head.Flow.Antecedent = loop;
        file.Statements.Nodes[0].Expression().AsIdentifier().FlowNode = loop;
        file.Statements.Nodes[1].Expression().AsIdentifier().FlowNode = new FlowNode
        {
            Flags = FlowFlags.BranchLabel,
            Antecedents = tail
        };
        var encoder = new Encoder(file, false);
        TypeScript.Scanner.ParserValidation.Runner.Encode(file, new(), encoder);
        var binding = new Graph(encoder, lexicalPrivateOwner: true).Encode(file);
        // Existing static source-generated serializer; no dynamic CLR type serialization.
        using var graph = JsonDocument.Parse(JsonSerializer.Serialize(binding, BinderJson.Default.Binding));
        var result = new JsonObject
        {
            ["seededOnly"] = true,
            ["binding"] = JsonNode.Parse(graph.RootElement.GetRawText())
        };
        using var resultJson = JsonDocument.Parse(result.ToJsonString());
        return resultJson.RootElement.Clone();
    }

    private static object OrderSensitivity()
    {
        var request = new CompletionRequest
        {
            Parse = new()
            {
                Text = "class A { #foo=1; m(){ const { #foo: x } = this; let bar; ({ #foo: bar } = this); }}",
                FileName = "/same.ts",
                ScriptKind = 3
            }
        };
        var before = CompletionRunner.Run(request);
        for (int i = 0; i < 8; i++)
            CompletionRunner.Run(new CompletionRequest { Parse = new() { Text = "class Prefix { #p=1; }", FileName = "/same.ts", ScriptKind = 3 } });
        var after = CompletionRunner.Run(request);
        using var first = JsonDocument.Parse(JsonSerializer.Serialize(before, CompletionJson.Default.CompletionResult));
        using var second = JsonDocument.Parse(JsonSerializer.Serialize(after, CompletionJson.Default.CompletionResult));
        return Map(("sameAfterPrivatePrefixes", JsonElement.DeepEquals(first.RootElement, second.RootElement)), ("displayKinds", after.SymbolHelpers.Select(s => (object)s.DisplayName.Kind).ToArray()), ("hasGeneratedPrivateDisplay", after.SymbolHelpers.Any(s => s.DisplayName.Kind == "private")), ("hasOrdinaryPrivateDisplay", after.SymbolHelpers.Any(s => s.DisplayName.Kind == "raw" && s.DisplayName.BytesBase64 == Bytes("#foo"))));
    }
}

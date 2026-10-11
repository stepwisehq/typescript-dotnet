// Authored differential assertions; expected values come from pinned original Go.
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TypeScript.Scanner;

namespace TypeScript.Foundation;
public static partial class SyntaxServiceChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["Names", "Tokens", "Declarations", "Decorators", "Data", "Iterator", "Factories", "Visitors", "Accessors", "Diagnostics", "Modes", "EntityNames", "Mutations", "Sort"];

    private static SourceFile Parse(string s, ScriptKind kind = ScriptKind.TS) => Parser.ParseSourceFile(new("/index.ts", "/index.ts"), s, kind);
    private static bool Caught(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static string Bytes(string s) => Convert.ToBase64String(Encoding.Latin1.GetBytes(s));
    private static string Raw(string s) => Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(s));
    private static object[] Snap(Node n) => [(int)n.Kind, n.Pos(), n.End(), (uint)n.Flags];
    private static object? Nodes(List<Node>? list) => list?.Select(n => (object)Snap(n)).ToArray();
    private static object? Nodes(Slice<Node> nodes)
    {
        if (nodes.IsNil) return null;
        var result = new object[nodes.Length];
        for (int i = 0; i < nodes.Length; i++) result[i] = Snap(nodes[i]);
        return result;
    }
    private static Dictionary<string, object?> Map(params (string Key, object? Value)[] pairs) => pairs.ToDictionary(p => p.Key, p => p.Value);
    private static JsonNode? Json(object? value) => value switch
    {
        null => null,
        string s => JsonValue.Create(s),
        bool b => JsonValue.Create(b),
        int n => JsonValue.Create(n),
        uint n => JsonValue.Create(n),
        object[] list => new JsonArray(list.Select(Json).ToArray()),
        Dictionary<string, object?> map => new JsonObject(map.Select(p => KeyValuePair.Create(p.Key, Json(p.Value)))),
        _ => throw new InvalidOperationException("Unencoded helper observation type " + value.GetType().Name)};
    public static void Run(string name)
    {
        using var resource = typeof(SyntaxServiceChecks).Assembly.GetManifestResourceStream("TypeScript.Foundation.SyntaxServices.json") ?? throw new InvalidOperationException("Missing original syntax observations");
        using var expected = JsonDocument.Parse(resource);
        object actual = name switch
        {
            "Sort" => SortControls(),
            "Modes" => ModeControls(),
            "EntityNames" => EntityControls(),
            "Mutations" => MutationControls(),
            "Diagnostics" => DiagnosticData(),
            "Accessors" => Accessors(),
            "Factories" => Factories(),
            "Visitors" => Visitors(),
            "Names" => Names(),
            "Tokens" => Tokens(),
            "Declarations" => Declarations(),
            "Decorators" => Decorators(),
            "Data" => Data(),
            "Iterator" => Iterator(),
            _ => throw new ArgumentException(name)};
        using var document = JsonDocument.Parse(Json(actual)!.ToJsonString());
        if (!JsonElement.DeepEquals(expected.RootElement.GetProperty(name), document.RootElement))
            throw new InvalidOperationException(name + ": original=" + expected.RootElement.GetProperty(name).GetRawText() + ", actual=" + document.RootElement.GetRawText());
    }

    private static object Names()
    {
        const string s = "/** @typedef {string} DocName */\nconst café='unused'; const caf\\u00e9=1; class C { #p=1; 'str-key'=2; 3=4; }";
        var file = Parse(s, ScriptKind.JS);
        var has = new[]
        {
            "café",
            "unused",
            "DocName",
            "#p",
            "str-key",
            "3",
            "absent"
        }.Select(q => (object)new object[] { Bytes(Raw(q)), file.HasIdentifier(Raw(q)) }).ToArray();
        var table = file.GetNameTable();
        var entries = table.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => (object)new object[] { Bytes(p.Key), p.Value }).ToArray();
        return Map(("source", s), ("has", has), ("table", entries), ("repeatIdentity", ReferenceEquals(table, file.GetNameTable())));
    }

    private static object Tokens()
    {
        const string s = "const x = 1_0; const y = 2;";
        var file = Parse(s);
        var parent = file.Statements.Nodes[0];
        var other = file.Statements.Nodes[1];
        int pos = s.IndexOf("1_0", StringComparison.Ordinal);
        var a = file.GetOrCreateToken(Kind.NumericLiteral, pos, pos + 3, parent, TokenFlags.ContainsSeparator);
        var same = file.GetOrCreateToken(Kind.NumericLiteral, pos, pos + 3, parent, 0);
        var otherParent = file.GetOrCreateToken(Kind.NumericLiteral, pos, pos + 3, other, 0);
        var otherRange = file.GetOrCreateToken(Kind.NumericLiteral, pos, pos + 1, parent, 0);
        parent.Flags |= NodeFlags.Reparsed;
        var hit = file.GetOrCreateToken(Kind.NumericLiteral, pos, pos + 3, parent, 0);
        return Map(("same", ReferenceEquals(a, same)), ("reparsedHit", ReferenceEquals(a, hit)), ("otherParentDistinct", !ReferenceEquals(a, otherParent)), ("otherRangeDistinct", !ReferenceEquals(a, otherRange)), ("parentMatches", ReferenceEquals(a.Parent, parent)), ("rawText", Bytes(a.Text())), ("flags", (int)a.AsNumericLiteral().TokenFlags), ("kindMismatchThrows", Caught(() => file.GetOrCreateToken(Kind.Identifier, pos, pos + 3, parent, 0))), ("reparsedMissThrows", Caught(() => file.GetOrCreateToken(Kind.NumericLiteral, pos, pos + 2, parent, 0))));
    }

    private static object Declarations()
    {
        const string s = "import d, {a,b as B} from 'mod'; export {x,y as Y}; function f(a:number):void; function f(a:number){}; const named=function inner(){}; const {x, p:q}=obj; class C {constructor(public z:number){} p=1;}";
        var file = Parse(s);
        var table = file.GetDeclarationMap();
        var rows = table.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => (object)new object? [] { Bytes(p.Key), Nodes(p.Value) }).ToArray();
        return Map(("source", s), ("entries", rows), ("repeatIdentity", ReferenceEquals(table, file.GetDeclarationMap())), ("bound", file.IsBound()));
    }

    private static object Decorators() => new[]
    {
        "class C{}",
        "export class C{}",
        "@d class C{}",
        "@d export class C{}"
    }.Select(s =>
    {
        var node = Parse(s).Statements.Nodes[0];
        var decorators = node.Decorators();
        return (object)new object? []
        {
            s,
            Nodes(decorators),
            decorators is { Count: > 0 } && Core.Same(decorators, node.Modifiers()?.Nodes ?? default)
        };
    }).ToArray();
    private static object Data()
    {
        var file = Parse("");
        var other = Parse("");
        var key = Ast.NewSourceFileDataKey<object?>();
        var key2 = Ast.NewSourceFileDataKey<object?>();
        int calls = 0;
        var value = new object ();
        var first = file.GetOrComputeData(key, f =>
        {
            calls++;
            if (!ReferenceEquals(f, file))
                throw new InvalidOperationException("wrong file");
            return value;
        });
        var repeat = file.GetOrComputeData(key, _ =>
        {
            calls++;
            return null;
        });
        var second = file.GetOrComputeData(key2, _ => null);
        var otherValue = other.GetOrComputeData(key, _ => null);
        var failed = Ast.NewSourceFileDataKey<int>();
        int failCalls = 0;
        bool threw = Caught(() => file.GetOrComputeData(failed, _ =>
        {
            failCalls++;
            throw new InvalidOperationException("first compute");
        }));
        int after = file.GetOrComputeData(failed, _ =>
        {
            failCalls++;
            return 9;
        });
        return Map(("same", ReferenceEquals(first, repeat)), ("calls", calls), ("nilDifferentKey", second is null), ("nilDifferentFile", otherValue is null), ("firstThrows", threw), ("failureNoRetry", failCalls == 1), ("failureDefault", after), ("invalidKeyThrows", Caught(() => file.GetOrComputeData(new SourceFileDataKey<int>(), _ => 0))));
    }

    private static object Iterator()
    {
        var factory = new NodeFactory();
        var a = factory.NewIdentifier("a");
        var c = factory.NewIdentifier("c");
        var d = factory.NewIdentifier("d");
        var replacement = factory.NewIdentifier("replacement");
        var node = factory.NewVariableDeclaration(a, null, null, c);
        var iterable = node.IterChildren();
        var values = new List<string>();
        foreach (var child in iterable)
        {
            values.Add(child.Text());
            if (values.Count == 1)
                node.AsVariableDeclaration().Initializer = replacement;
        }

        var list = factory.NewNodeList([a, c]);
        var block = factory.NewBlock(list, true);
        var seen = new List<string>();
        foreach (var child in block.IterChildren())
        {
            seen.Add(child.Text());
            if (seen.Count == 1)
                list.Nodes = list.Nodes.Append(d);
        }

        int early = 0;
        foreach (var _ in block.IterChildren())
        {
            early++;
            break;
        }

        return Map(("laterChildReplacement", values.Cast<object>().ToArray()), ("appendCapturedLength", seen.Cast<object>().ToArray()), ("earlyStop", early));
    }
}

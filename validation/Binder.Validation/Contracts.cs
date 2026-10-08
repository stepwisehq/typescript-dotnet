using System.Text.Json.Serialization;
using TypeScript.Scanner.ParserValidation;

namespace TypeScript.Scanner.BinderValidation;
public sealed record BoundField(string Name, string Type, int? Ref);
public sealed record BoundNode(int NodeId, List<BoundField> Fields);
public sealed class BoundName
{
    public string Kind { get; set; } = "raw";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BytesBase64 { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrefixBase64 { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SuffixBase64 { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OwnerSymbolId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OwnerNodeId { get; set; }
}

public sealed class BoundSymbol
{
    public int Id { get; set; }
    public uint Flags { get; set; }
    public BoundName Name { get; set; } = new();
    public bool DeclarationsNull { get; set; }
    public List<int> Declarations { get; set; } = [];
    public int? ValueDeclaration { get; set; }
    public int? Parent { get; set; }
    public int? ExportSymbol { get; set; }
    public int? Members { get; set; }
    public int? Exports { get; set; }
}

public sealed record BoundEntry(BoundName Key, int SymbolId);
public sealed record BoundTable(int Id, List<BoundEntry> Entries);
public sealed class BoundFlow
{
    public int Id { get; set; }
    public uint Flags { get; set; }
    public int? Node { get; set; }
    public int? Antecedent { get; set; }
    public int? Antecedents { get; set; }
}

public sealed class BoundFlowList
{
    public int Id { get; set; }
    public int? Flow { get; set; }
    public int? Next { get; set; }
}

public sealed record BoundPattern(string TextBase64, int StarIndex, int SymbolId);
public sealed class Binding
{
    public List<BoundNode> Nodes { get; set; } = [];
    public List<BoundSymbol> Symbols { get; set; } = [];
    public List<BoundTable> Tables { get; set; } = [];
    public List<BoundFlow> Flows { get; set; } = [];
    public List<BoundFlowList> FlowLists { get; set; } = [];
    public List<CanonicalDiagnostic> Diagnostics { get; set; } = [];
    public bool IsBound { get; set; }
    public int SymbolCount { get; set; }
    public int? GlobalExports { get; set; }
    public bool PatternsNull { get; set; }
    public List<BoundPattern> Patterns { get; set; } = [];
}

public sealed record Snapshot(Response After, Binding Binding);
public sealed record Result(Response Before, Response After, Binding Binding, bool Idempotent);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, MaxDepth = 2048)]
[JsonSerializable(typeof(Request))]
[JsonSerializable(typeof(Result))]
[JsonSerializable(typeof(Snapshot))]
public partial class BinderJson : JsonSerializerContext;

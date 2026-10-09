using System.Text.Json.Serialization;
using TypeScript.Scanner.ParserValidation;

namespace TypeScript.Checking.CollectionsValidation;
public sealed class Request
{
    public string Name { get; set; } = "";
    public string Profile { get; set; } = "Collections";
    public string FileName { get; set; } = "/main.ts";
    public string Text { get; set; } = "";
    public string? TextBytesBase64 { get; set; }
    public bool StoredObservations { get; set; }
    public List<int>? QueryNodeIds { get; set; }
}

public sealed class FullDiagnostic
{
    public int Code { get; set; }
    public int Category { get; set; }
    public int Pos { get; set; }
    public int End { get; set; }
    public string Key { get; set; } = "";
    public List<string>? ArgsBase64 { get; set; }
    public string Text { get; set; } = "";
    public int? FileIndex { get; set; }
    public string SourceBase64 { get; set; } = "";
    public bool ReportsUnnecessary { get; set; }
    public bool ReportsDeprecated { get; set; }
    public bool SkippedOnNoEmit { get; set; }
    public List<FullDiagnostic>? Chain { get; set; }
    public List<FullDiagnostic>? Related { get; set; }
}

public sealed class FileRecord
{
    public int Index { get; set; }
    public string FileName { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public int RootNodeId { get; set; }
    public bool DefaultLibrary { get; set; }
    public bool Declaration { get; set; }
    public bool ExternalModuleJSX { get; set; }
    public bool ExternalModuleForce { get; set; }
    public bool SkipTypeChecking { get; set; }
    public bool IsBound { get; set; }
    public int SymbolCount { get; set; }
    public List<FullDiagnostic>? ParseDiagnostics { get; set; }
    public List<FullDiagnostic>? BindDiagnostics { get; set; }
    public List<FullDiagnostic>? ProgramSemanticDiagnostics { get; set; }
}

public sealed record NodeOwner(int NodeId, int? FileIndex, int? FileNodeId);
public sealed record CollectionQuery(int NodeId, string Text, uint Flags, uint ObjectFlags, string DataType, int? Symbol, bool RepeatType, bool RepeatSymbol);
public sealed record CollectionOptions
{
    public string Profile { get; init; } = "Collections";
    public string Target { get; init; } = "ESNext";
    public bool NoLib { get; init; } = true;
    public bool NoResolve { get; init; } = true;
    public bool SkipModuleResolution { get; init; } = true;
    public bool SkipLibCheck { get; init; } = true;
    public bool StrictNullChecks { get; init; } = true;
    public bool StrictFunctionTypes { get; init; } = true;
    public bool NoImplicitAny { get; init; } = true;
    public bool NoImplicitThis { get; init; } = true;
    public bool StrictBindCallApply { get; init; }
    public bool NoImplicitReturns { get; init; }
    public bool ExactOptionalPropertyTypes { get; init; }
    public bool NoUncheckedIndexedAccess { get; init; }
    public bool DownlevelIteration { get; init; }
}

public sealed class Result
{
    public string Stage { get; set; } = "environment-bootstrap";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StoredGraphValue? StoredGraph { get; set; }
    public CollectionOptions Options { get; set; } = new();
    public List<FileRecord> Files { get; set; } = [];
    public List<CanonicalNode> Nodes { get; set; } = [];
    public List<NodeOwner> NodeOwners { get; set; } = [];
    public Binding Binding { get; set; } = new();
    public int? Globals { get; set; }
    public int? GlobalThisSymbol { get; set; }
    public List<CollectionQuery> Queries { get; set; } = [];
    public List<FullDiagnostic>? GlobalDiagnostics { get; set; }
    public List<FullDiagnostic>? ConfigurationDiagnostics { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, MaxDepth = 2048)]
[JsonSerializable(typeof(Request))]
[JsonSerializable(typeof(Result))]
public partial class CollectionsJson : JsonSerializerContext;

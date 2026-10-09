using System.Text.Json.Serialization;
using TypeScript.Scanner.ParserValidation;

namespace TypeScript.Checking.Validation;
public sealed class Request
{
    public string Profile { get; set; } = "Legacy";
    public bool CallableObservations { get; set; }
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";
    public string? TextBytesBase64 { get; set; }
    public int ScriptKind { get; set; } = 3;
    public string FileName { get; set; } = "/main.ts";
    public bool ExternalModuleJSX { get; set; }
    public bool ExternalModuleForce { get; set; }
    public bool ResolveJSDoc { get; set; }
    public List<int>? QueryNodeIds { get; set; }
    public List<List<int>> RelationPairs { get; set; } = [];
    public bool AdmissionOnly { get; set; }
    public bool ObjectObservations { get; set; }
    public bool DataObservations { get; set; }
    public List<int> DeclaredNodeIds { get; set; } = [];
    public List<int> WidenNodeIds { get; set; } = [];

    public TypeScript.Scanner.ParserValidation.Request ParserInput() => new()
    {
        Name = Name,
        Text = Text,
        TextBytesBase64 = TextBytesBase64,
        ScriptKind = ScriptKind,
        FileName = FileName.Length == 0 ? "/main.ts" : FileName,
        ExternalModuleJSX = ExternalModuleJSX,
        ExternalModuleForce = ExternalModuleForce,
        ResolveJSDoc = ResolveJSDoc
    };
}

public sealed class DiagnosticValue
{
    public int Code { get; set; }
    public int Category { get; set; }
    public int Pos { get; set; }
    public int End { get; set; }
    public string Key { get; set; } = "";
    public List<string> ArgsBase64 { get; set; } = [];
    public string Text { get; set; } = "";
    public string? FileNameBase64 { get; set; }
    public bool ArgsNull { get; set; }
    public bool ReportsUnnecessary { get; set; }
    public bool ReportsDeprecated { get; set; }
    public string SourceBase64 { get; set; } = "";
    public bool SkippedOnNoEmit { get; set; }
    public List<DiagnosticValue> Related { get; set; } = [];
    public List<DiagnosticValue> Chain { get; set; } = [];
}

public sealed class LiteralValue
{
    public string Kind { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BytesBase64 { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Value { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BitsHex { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Negative { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Base10ValueBase64 { get; set; }
}

public sealed class TypeValue
{
    public int Id { get; set; }
    public uint Flags { get; set; }
    public uint ObjectFlags { get; set; }
    public string DataType { get; set; } = "";
    public string Text { get; set; } = "";
    public string? IntrinsicNameBase64 { get; set; }
    public LiteralValue? LiteralValue { get; set; }
    public List<int> Types { get; set; } = [];
    public int? FreshType { get; set; }
    public int? RegularType { get; set; }
    public int? Symbol { get; set; }
    public int? AliasSymbol { get; set; }
    public List<int> AliasTypeArguments { get; set; } = [];
}

public sealed class SymbolValue
{
    public int Id { get; set; }
    public uint Flags { get; set; }
    public uint CheckFlags { get; set; }
    public string NameBase64 { get; set; } = "";
    public bool DeclarationsNull { get; set; }
    public List<int> Declarations { get; set; } = [];
    public int? ValueDeclaration { get; set; }
    public int? Parent { get; set; }
    public int? ExportSymbol { get; set; }
    public int? Members { get; set; }
    public int? Exports { get; set; }
}

public sealed record TableEntry(string KeyBase64, int SymbolId);
public sealed record TableValue(int Id, List<TableEntry> Entries);
public sealed record QueryValue(int NodeId, int Type, int? Symbol);
public sealed record RelationValue(int SourceNodeId, int TargetNodeId, bool Assignable);
public sealed record FixedOptions(string Target = "ESNext", bool NoLib = true, bool NoResolve = true, bool SkipModuleResolution = true, bool StrictNullChecks = true, bool StrictBindCallApply = false, bool NoImplicitAny = false)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Profile { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? NoImplicitThis { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? StrictFunctionTypes { get; init; }
}

public sealed class Result
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CallableGraphValue? CallableGraph { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DataGraphValue? DataGraph { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ObjectGraphValue? ObjectGraph { get; set; }
    public Response Parser { get; set; } = new();
    public List<DiagnosticValue> BindDiagnostics { get; set; } = [];
    public List<DiagnosticValue> CheckerDiagnostics { get; set; } = [];
    public List<DiagnosticValue> GlobalDiagnostics { get; set; } = [];
    public List<TypeValue> Types { get; set; } = [];
    public List<SymbolValue> Symbols { get; set; } = [];
    public List<TableValue> Tables { get; set; } = [];
    public List<QueryValue> Queries { get; set; } = [];
    public List<RelationValue> Relations { get; set; } = [];
    public int? GlobalThisSymbol { get; set; }
    public int? Locals { get; set; }
    public int? Globals { get; set; }
    public FixedOptions Options { get; set; } = new();
}

public sealed record AdmissionResult(bool Admitted, string? Feature, int? Kind, int? Pos, int? End);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, MaxDepth = 2048)]
[JsonSerializable(typeof(Request))]
[JsonSerializable(typeof(Result))]
[JsonSerializable(typeof(AdmissionResult))]
[JsonSerializable(typeof(List<UtilityObservation>))]
public partial class CheckerJson : JsonSerializerContext;

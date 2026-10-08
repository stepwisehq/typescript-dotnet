using System.Text.Json.Serialization;

namespace TypeScript.Scanner.ParserValidation;
public sealed class Request
{
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";
    public string? TextBytesBase64 { get; set; }
    public int ScriptKind { get; set; } = 3;
    public string FileName { get; set; } = "";
    public bool ExternalModuleJSX { get; set; }
    public bool ExternalModuleForce { get; set; }
    public bool ResolveJSDoc { get; set; }
    public List<int> TextProbeIds { get; set; } = [];
    public bool TextProbeIncludeJSDoc { get; set; }
}
public sealed class CanonicalList
{
    public int Pos { get; set; }
    public int End { get; set; }
    public bool NodesNull { get; set; }
    public bool Missing { get; set; }
    public bool TrailingComma { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public uint? ModifierFlags { get; set; }
    public List<int> Items { get; set; } = [];
}
public sealed class CanonicalField
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsNull { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? NodeId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public CanonicalList? List { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public long? Number { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? Boolean { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? BytesBase64 { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<int>? NodeIds { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<string>? StringsBase64 { get; set; }
}
public sealed class CanonicalParent
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? Id { get; set; }
    public int Kind { get; set; }
    public int Pos { get; set; }
    public int End { get; set; }
}
public sealed class CanonicalNode
{
    public int Id { get; set; }
    public int Kind { get; set; }
    public int Pos { get; set; }
    public int End { get; set; }
    public uint Flags { get; set; }
    public string DataType { get; set; } = "";
    public List<CanonicalField> Fields { get; set; } = [];
    public List<int> ChildOrder { get; set; } = [];
    public CanonicalParent? Parent { get; set; }
    public List<int> JsDoc { get; set; } = [];
}
public sealed class CanonicalDiagnostic
{
    public int Code { get; set; }
    public int Category { get; set; }
    public int Pos { get; set; }
    public int End { get; set; }
    public string Key { get; set; } = "";
    public List<string> ArgsBase64 { get; set; } = [];
    public string Text { get; set; } = "";
    public List<CanonicalDiagnostic> Related { get; set; } = [];
    public List<CanonicalDiagnostic> Chain { get; set; } = [];
}
public sealed record CanonicalDirective(int Pos, int End, int Kind);
public sealed record CanonicalTextProbe(int NodeId, string TextBase64, int TokenPos);
public sealed record CanonicalReference(int Pos, int End, string FileNameBase64, int ResolutionMode, bool Preserve);
public sealed record CanonicalCheckJs(bool Enabled, int Pos, int End, int Kind, bool HasTrailingNewLine);
public sealed record CanonicalPragmaArgument(int Pos, int End, string ValueBase64);
public sealed record CanonicalPragma(int Pos, int End, int Kind, bool HasTrailingNewLine, string Name, Dictionary<string, CanonicalPragmaArgument> Args);
public sealed class Metadata
{
    public int ScriptKind { get; set; }
    public int LanguageVariant { get; set; }
    public bool IsDeclarationFile { get; set; }
    public int NodeCount { get; set; }
    public int IdentifierCount { get; set; }
    public int TextCount { get; set; }
    public int? ExternalModuleIndicator { get; set; }
    public int? CommonJSModuleIndicator { get; set; }
    public List<CanonicalDirective> CommentDirectives { get; set; } = [];
    public List<CanonicalReference> ReferencedFiles { get; set; } = [];
    public List<CanonicalReference> TypeReferenceDirectives { get; set; } = [];
    public List<CanonicalReference> LibReferenceDirectives { get; set; } = [];
    public List<int> ReparsedClones { get; set; } = [];
    public List<int> Imports { get; set; } = [];
    public List<int> ModuleAugmentations { get; set; } = [];
    public List<string> AmbientModuleNamesBase64 { get; set; } = [];
    public int UsesUriStyleNodeCoreModules { get; set; }
    public CanonicalCheckJs? CheckJsDirective { get; set; }
    public List<CanonicalPragma> Pragmas { get; set; } = [];
}
public sealed class Response
{
    public List<CanonicalNode> Nodes { get; set; } = [];
    public List<CanonicalDiagnostic> ParseDiagnostics { get; set; } = [];
    public List<CanonicalDiagnostic> JsDiagnostics { get; set; } = [];
    public List<CanonicalDiagnostic> JsDocDiagnostics { get; set; } = [];
    public Metadata Metadata { get; set; } = new();
    public List<CanonicalTextProbe> TextProbes { get; set; } = [];
}
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, MaxDepth = 2048)]
[JsonSerializable(typeof(Request))]
[JsonSerializable(typeof(Response))]
public partial class ContractJson : JsonSerializerContext;

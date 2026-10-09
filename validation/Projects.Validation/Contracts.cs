using System.Text.Json.Serialization;
using TypeScript.Checking.CollectionsValidation;
using TypeScript.Scanner.ParserValidation;

namespace TypeScript.Checking.ProjectsValidation;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class Request
{
    public string Name { get; set; } = "";
    public Dictionary<string, string> Files { get; set; } = [];
    public string Config { get; set; } = "{\"files\":[\"main.ts\"]}";
    public List<QuerySelector>? Queries { get; set; }
}

public sealed record QuerySelector(string File, int Pos, int Kind, int? End = null);
public sealed record QueryRecord(int NodeId, int? Symbol, int Type, string Text, int? Signature, bool RepeatType, bool RepeatSymbol, bool? RepeatSignature);
public sealed record AliasRecord(int Symbol, int? ImmediateTarget, int? UltimateTarget, int? TypeOnlyDeclaration, bool RepeatTarget, bool StoredPresent, int? StoredImmediateTarget, int? StoredAliasTarget, bool Referenced, int? StoredTypeOnlyDeclaration);
public sealed record StarRecord(string NameBase64, int Node);
public sealed record ExportRecord(string NameBase64, int Symbol);
public sealed record ModuleRecord(int Symbol, List<ExportRecord> Exports, bool RepeatSymbols, bool StoredPresent, int? StoredExports, bool TypeOnlyStarsNull, List<StarRecord> TypeOnlyStars, bool Checked);
public sealed record ExportTypeRecord(int Symbol, bool StoredPresent, int? Target, int? OriginatingImport);
public sealed record StoreInventory(string Store, bool Null, int Count, List<int> Owners, int Unrooted);
public sealed record UnrootedExportClone(int Target, int? OriginatingImport, int OwnerNode, uint Flags, uint CheckFlags, string NameBase64, int Count);
public sealed record ResolutionRecord(int FileIndex, int SpecifierNodeId, string SpecifierBase64, int Mode, bool Present, bool Resolved, string? FileName, string? Path, string? OriginalPath, string? Extension, bool UsingTsExtension, bool UsingExtraExtensions, bool ExternalLibrary, bool Custom, string? AlternateResult, bool RepeatSame);
public sealed class FileRecord
{
    public int Index { get; set; }
    public string FileName { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public int RootNodeId { get; set; }
    public bool ProgramFile { get; set; } = true;
    public bool Declaration { get; set; }
    public bool DefaultLibrary { get; set; }
    public bool ExternalModule { get; set; }
    public bool SkipTypeChecking { get; set; }
    public bool IsBound { get; set; }
    public int SymbolCount { get; set; }
    public List<FullDiagnostic>? ParseDiagnostics { get; set; }
    public List<FullDiagnostic>? BindDiagnostics { get; set; }
    public List<FullDiagnostic>? SemanticDiagnostics { get; set; }
}

public sealed class TypeRecord
{
    public int Id { get; set; }
    public string DataType { get; set; } = "";
    public uint Flags { get; set; }
    public uint ObjectFlags { get; set; }
    public int? Symbol { get; set; }
    public int? AliasSymbol { get; set; }
    public List<int>? AliasArguments { get; set; }
    public int? Target { get; set; }
    public List<int>? Arguments { get; set; }
    public List<int>? Constituents { get; set; }
    public int? Constraint { get; set; }
    public int? Default { get; set; }
    public int? Members { get; set; }
    public List<int>? Properties { get; set; }
    public List<int>? Signatures { get; set; }
    public int CallSignatureCount { get; set; }
    public string? IntrinsicNameBase64 { get; set; }
    public string? LiteralKind { get; set; }
    public string? LiteralValue { get; set; }
}

public sealed class SignatureRecord
{
    public int Id { get; set; }
    public uint Flags { get; set; }
    public int MinArgumentCount { get; set; }
    public int ResolvedMinArgumentCount { get; set; }
    public int? Declaration { get; set; }
    public List<int>? TypeParameters { get; set; }
    public List<int>? Parameters { get; set; }
    public int? ThisParameter { get; set; }
    public int? ReturnType { get; set; }
    public int? Target { get; set; }
}

public sealed class Result
{
    public Dictionary<string, int> Options { get; set; } = [];
    public List<string>? TypesOption { get; set; }
    public int ExitCode { get; set; }
    public string? UnsupportedFeature { get; set; }
    public List<FileRecord> Files { get; set; } = [];
    public List<string> DeclaredRoots { get; set; } = [];
    public List<CanonicalNode> Nodes { get; set; } = [];
    public List<NodeOwner> NodeOwners { get; set; } = [];
    public Binding? Binding { get; set; }
    public int? Globals { get; set; }
    public int? GlobalThisSymbol { get; set; }
    public List<QueryRecord> Queries { get; set; } = [];
    public List<TypeRecord> Types { get; set; } = [];
    public List<SignatureRecord> Signatures { get; set; } = [];
    public List<AliasRecord> Aliases { get; set; } = [];
    public List<ModuleRecord> Modules { get; set; } = [];
    public List<ExportTypeRecord> ExportTypes { get; set; } = [];
    public List<UnrootedExportClone> UnrootedExportClones { get; set; } = [];
    public List<ResolutionRecord> Resolutions { get; set; } = [];
    public List<StoreInventory> Inventories { get; set; } = [];
    public List<FullDiagnostic>? GlobalDiagnostics { get; set; }
    public List<FullDiagnostic>? ProgramDiagnostics { get; set; }
    public List<FullDiagnostic>? ConfigDiagnostics { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Request))]
[JsonSerializable(typeof(Result))]
public partial class ProjectJson : JsonSerializerContext
{
}

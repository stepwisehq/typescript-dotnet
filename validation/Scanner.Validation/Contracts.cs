using System.Text.Json.Serialization;

namespace TypeScript.Scanner.Validation;

public sealed class Operation
{
    public string Op { get; set; } = "Scan";
    public bool Argument { get; set; }
    public int Position { get; set; }
    public string Text { get; set; } = "";
}
public sealed class Request
{
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";
    public string? TextBytesBase64 { get; set; }
    public bool? SkipTrivia { get; set; }
    public bool SkipJSDocLeadingAsterisks { get; set; }
    public int LanguageVariant { get; set; }
    public int ScriptTarget { get; set; }
    public Operation[]? Operations { get; set; }
    public UtilityOperation[]? UtilityOperations { get; set; }
}
public sealed class UtilityOperation
{
    public string Op { get; set; } = "";
    public int Position { get; set; }
    public int Line { get; set; }
    public int Character { get; set; }
    public int[] LineStarts { get; set; } = [];
    public bool AllowEdits { get; set; }
    public bool StopAfterLineBreak { get; set; }
    public bool StopAtComments { get; set; }
    public bool InJSDoc { get; set; }
}
public sealed record Comment(int Kind, int Start, int End, bool HasTrailingNewLine);
public sealed class UtilityResult
{
    public string Op { get; set; } = "";
    public int Number { get; set; }
    public bool Boolean { get; set; }
    public string TextBytesBase64 { get; set; } = "";
    public List<Comment> Comments { get; set; } = [];
}
public sealed record Token(int Kind, int FullStart, int Start, int End, string TextBytesBase64, string ValueBytesBase64, int Flags);
public sealed record Diagnostic(int Code, int Start, int Length, string[] Args);
public sealed record Directive(int Start, int End, int Type);
public sealed class Response
{
    public List<Token> Tokens { get; set; } = [];
    public List<Diagnostic> Diagnostics { get; set; } = [];
    public List<Directive> CommentDirectives { get; set; } = [];
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<UtilityResult>? UtilityResults { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Request))]
[JsonSerializable(typeof(Response))]
public partial class ContractJson : JsonSerializerContext;

// Authored ordinary disk CLI; no oracle, validation assets or upstream checkout required.
using System.Text.Json;
using TypeScript.Compiler;
using TypeScript.Checking;
using TypeScript.Scanner;
using TsScanner = TypeScript.Scanner.Scanner;

if (args.Length == 1 && args[0] is "--help" or "-h")
{
    Console.WriteLine("Usage: tscheck --project <tscheck.json> [--json]");
    return 0;
}

bool json = args.Length == 3 && args[2] == "--json";
if (args.Length is not (2 or 3) || args[0] != "--project" || (args.Length == 3 && !json))
{
    Console.Error.WriteLine("Usage: tscheck --project <tscheck.json> [--json]");
    return 2;
}

try
{
    Project project = Project.Load(args[1]);
    IReadOnlyList<Diagnostic> diagnostics = project.Check();
    var output = new CliResult
    {
        ExitCode = diagnostics.Count == 0 ? 0 : 1,
        Diagnostics = diagnostics.Select(Describe).ToArray()
    };
    if (json)
        Console.WriteLine(JsonSerializer.Serialize(output, CliJsonContext.Default.CliResult));
    else
        foreach (CliDiagnostic diagnostic in output.Diagnostics)
            Print(diagnostic);
    return output.ExitCode;
}
catch (Exception ex)when (ex is ProjectException or UnsupportedSemanticFeatureException or IOException or UnauthorizedAccessException)
{
    var output = new CliResult
    {
        ExitCode = 2,
        Error = ex.Message
    };
    if (json)
        Console.WriteLine(JsonSerializer.Serialize(output, CliJsonContext.Default.CliResult));
    else
        Console.Error.WriteLine(ex.Message);
    return 2;
}

static CliDiagnostic Describe(Diagnostic diagnostic)
{
    SourceFile? file = diagnostic.File();
    int line = 0, column = 0;
    if (file is not null && diagnostic.Pos() >= 0)
    {
        var location = TsScanner.GetECMALineAndUTF16CharacterOfPosition(file, diagnostic.Pos());
        line = location.Line + 1;
        column = location.Character + 1;
    }

    return new()
    {
        File = file?.FileName(),
        Code = diagnostic.Code(),
        Category = diagnostic.Category().ToString(),
        Message = diagnostic.String(),
        ByteStart = diagnostic.Pos(),
        ByteEnd = diagnostic.End(),
        Line = line,
        Column = column,
        Chain = diagnostic.MessageChain()?.Select(Describe).ToArray(),
        Related = diagnostic.RelatedInformation()?.Select(Describe).ToArray()
    };
}

static void Print(CliDiagnostic diagnostic)
{
    string location = diagnostic.File is null ? "" : diagnostic.File + (diagnostic.Line > 0 ? $"({diagnostic.Line},{diagnostic.Column})" : "") + ": ";
    Console.Error.WriteLine(location + "TS" + diagnostic.Code + ": " + diagnostic.Message);
    foreach (CliDiagnostic chain in diagnostic.Chain ?? [])
        Print(chain);
    foreach (CliDiagnostic related in diagnostic.Related ?? [])
        Print(related);
}

internal sealed class CliResult
{
    public int ExitCode { get; set; }
    public string? Error { get; set; }
    public CliDiagnostic[] Diagnostics { get; set; } = [];
}

internal sealed class CliDiagnostic
{
    public string? File { get; set; }
    public int Code { get; set; }
    public string Category { get; set; } = "";
    public string Message { get; set; } = "";
    public int ByteStart { get; set; }
    public int ByteEnd { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public CliDiagnostic[]? Chain { get; set; }
    public CliDiagnostic[]? Related { get; set; }
}

[System.Text.Json.Serialization.JsonSerializable(typeof(CliResult))]
internal partial class CliJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}

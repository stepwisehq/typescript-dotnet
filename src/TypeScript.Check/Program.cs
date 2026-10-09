// Authored ordinary disk CLI; no oracle, validation assets or upstream checkout required.
using System.Text.Json;
using TypeScript.Compiler;
using TypeScript.Checking;
using TypeScript.Scanner;
using TsScanner = TypeScript.Scanner.Scanner;

const string usage = "Usage: tscheck --inspect-config <tsconfig> | --config <tsconfig> [--json] | --project <tscheck.json> [--json]";
if (args.Length == 1 && args[0] is "--help" or "-h")
{
    Console.WriteLine(usage);
    return 0;
}

bool json = args.Length == 3 && args[2] == "--json";
if (args.Length is not (2 or 3) || args[0] is not ("--project" or "--config" or "--inspect-config") || (args.Length == 3 && !json))
{
    Console.Error.WriteLine(usage);
    return 2;
}

bool inspect = args[0] == "--inspect-config";
bool fullConfiguration = args[0] != "--project";
json |= inspect;
try
{
    if (inspect)
    {
        ConfigurationInspection inspection = Project.InspectConfiguration(args[1]);
        using var document = JsonDocument.Parse(inspection.ShowConfigurationJSON());
        var output = new CliResult
        {
            ExitCode = inspection.Diagnostics.Count == 0 ? 0 : 1,
            Configuration = document.RootElement.Clone(),
            Files = inspection.Parsed?.FileNames()?.Select(path => DisplayBytes(path.AsString())).ToArray(),
            // Display strings cannot represent arbitrary original bytes. This
            // channel keeps the interpreter's exact filename values observable.
            FileNamesBase64 = inspection.Parsed?.FileNames()?.Select(path => Convert.ToBase64String(RawBytes(path.AsString()))).ToArray(),
            Diagnostics = inspection.Diagnostics.Select(diagnostic => Describe(diagnostic, true)).ToArray()
        };
        Console.WriteLine(JsonSerializer.Serialize(output, CliJsonContext.Default.CliResult));
        return output.ExitCode;
    }

    Project project = fullConfiguration ? Project.LoadConfiguration(args[1]) : Project.Load(args[1]);
    IReadOnlyList<Diagnostic> diagnostics = project.Check();
    var checkedOutput = new CliResult
    {
        ExitCode = diagnostics.Count == 0 ? 0 : 1,
        Diagnostics = diagnostics.Select(diagnostic => Describe(diagnostic, fullConfiguration)).ToArray()
    };
    if (json)
        Console.WriteLine(JsonSerializer.Serialize(checkedOutput, CliJsonContext.Default.CliResult));
    else
        foreach (CliDiagnostic diagnostic in checkedOutput.Diagnostics)
            Print(diagnostic);
    return checkedOutput.ExitCode;
}
catch (ConfigurationDiagnosticsException ex)
{
    var output = new CliResult
    {
        ExitCode = 1,
        Diagnostics = ex.Diagnostics.Select(diagnostic => Describe(diagnostic, true)).ToArray()
    };
    if (json)
        Console.WriteLine(JsonSerializer.Serialize(output, CliJsonContext.Default.CliResult));
    else
        foreach (var diagnostic in output.Diagnostics)
            Print(diagnostic);
    return 1;
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

// Display is Unicode; the inspection filename Base64 channel remains the
// authoritative original byte value, including unrepresentable virtual names.
static byte[] RawBytes(string bytes) => bytes.Select(value => checked((byte)value)).ToArray();
static string DisplayBytes(string bytes) => System.Text.Encoding.UTF8.GetString(RawBytes(bytes));
static CliDiagnostic Describe(Diagnostic diagnostic, bool rawConfigFiles)
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
        File = file is null ? null : rawConfigFiles && Ast.IsJsonSourceFile(file) ? DisplayBytes(file.FileName()) : file.FileName(),
        Code = diagnostic.Code(),
        Category = diagnostic.Category().ToString(),
        Message = diagnostic.String(),
        ByteStart = diagnostic.Pos(),
        ByteEnd = diagnostic.End(),
        Line = line,
        Column = column,
        Chain = diagnostic.MessageChain()?.Select(item => Describe(item, rawConfigFiles)).ToArray(),
        Related = diagnostic.RelatedInformation()?.Select(item => Describe(item, rawConfigFiles)).ToArray()
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

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Configuration { get; set; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Files { get; set; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[]? FileNamesBase64 { get; set; }
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

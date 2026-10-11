// Authored ordinary disk CLI; no oracle, validation assets or upstream checkout required.
using System.Text.Json;
using TypeScript.Compiler;
using TypeScript.Modules;
using TypeScript.Checking;
using TypeScript.Scanner;
using TsScanner = TypeScript.Scanner.Scanner;

const string usage = "Usage: tscheck --runtime-info | --inspect-program <tsconfig> | --inspect-project <tsconfig> | --inspect-config <tsconfig> | --config <tsconfig> [--json] [--noEmit] | --project <tscheck.json> [--json]";
// Authored runtime probe; reports the executing runtime rather than build intent.
if (args.Length == 1 && args[0] == "--runtime-info")
{
    Console.WriteLine("{\"serverGC\":" + (System.Runtime.GCSettings.IsServerGC ? "true" : "false") +
        ",\"processorCount\":" + Environment.ProcessorCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
    return 0;
}

if (args.Length == 1 && args[0] is "--help" or "-h")
{
    Console.WriteLine(usage);
    return 0;
}

// This authored CLI accepts only its existing modes plus the source noEmit
// override for ordinary --config checking; it is not a general flag parser.
bool json = false, noEmit = false;
bool validArguments = args.Length >= 2 && args[0] is
    "--project" or "--config" or "--inspect-config" or "--inspect-project" or "--inspect-program";
if (validArguments)
    foreach (string argument in args.Skip(2))
    {
        if (argument == "--json" && !json)
            json = true;
        else if (argument == "--noEmit" && args[0] == "--config" && !noEmit)
            noEmit = true;
        else
            validArguments = false;
    }
if (!validArguments)
{
    Console.Error.WriteLine(usage);
    return 2;
}

bool inspectProgram = args[0] == "--inspect-program";
bool inspectGraph = args[0] == "--inspect-project";
bool inspect = args[0] == "--inspect-config";
bool fullConfiguration = args[0] != "--project";
json |= inspect || inspectGraph || inspectProgram;
try
{
    if (inspectProgram)
    {
        var inspection = new ProgramInspection(args[1]);
        var program = inspection.Program;
        var failure = program.ModuleResolutionError();
        var output = new CliResult
        {
            ExitCode = failure is not null ? 2 : inspection.Diagnostics.Count != 0 ? 1 : 0,
            Error = failure?.Message,
            Files = MapOutput(program.SourceFiles(), file => DisplayBytes(file.FileNameGoBytes())),
            FileNamesBase64 = MapOutput(program.SourceFiles(), file => Base64(file.FileNameGoBytes())),
            Program = DescribeProgram(inspection),
            Diagnostics = inspection.Diagnostics.Select(diagnostic => Describe(diagnostic, true)).ToArray()
        };
        Console.WriteLine(JsonSerializer.Serialize(output, CliJsonContext.Default.CliResult));
        return output.ExitCode;
    }

    if (inspectGraph)
    {
        ResolvedProjectGraph graph = Project.InspectResolvedGraph(args[1]);
        var graphDiagnostics = graph.ParseDiagnostics.Concat(graph.InclusionDiagnostics).ToArray();
        bool failures = graphDiagnostics.Length != 0 || graph.ResolutionEdges.Any(edge => edge.Error is not null || !edge.Resolution.IsResolved()) ||
            graph.PathReferences.Any(edge => edge.Diagnostic is not null) ||
            graph.TypeDirectives.Any(edge => edge.Scheduling == DependencyScheduling.Unresolved);
        var output = new CliResult
        {
            ExitCode = failures ? 1 : 0,
            Files = MapOutput(graph.SourceFiles, file => DisplayBytes(file.FileNameGoBytes())),
            FileNamesBase64 = MapOutput(graph.SourceFiles, file => Base64(file.FileNameGoBytes())),
            Graph = DescribeGraph(graph),
            Diagnostics = graphDiagnostics.Select(diagnostic => Describe(diagnostic, true)).ToArray()
        };
        Console.WriteLine(JsonSerializer.Serialize(output, CliJsonContext.Default.CliResult));
        return output.ExitCode;
    }

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

    Project project = fullConfiguration ? Project.LoadConfiguration(args[1], noEmit ? new CompilerOptions { NoEmit = Tristate.True } : null) : Project.Load(args[1]);
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
        File = file is null ? null : file.ParseOptions().FileNameIsGoBytes || rawConfigFiles && Ast.IsJsonSourceFile(file) ? DisplayBytes(file.FileName()) : file.FileName(),
        Code = diagnostic.Code(),
        Category = diagnostic.Category().ToString(),
        Message = diagnostic.String(),
        ByteStart = diagnostic.Pos(),
        ByteEnd = diagnostic.End(),
        Line = line,
        Column = column,
        Chain = MapOutput(diagnostic.MessageChain(), item => Describe(item, rawConfigFiles)),
        Related = MapOutput(diagnostic.RelatedInformation(), item => Describe(item, rawConfigFiles))
    };
}

static string Base64(string bytes) => Convert.ToBase64String(RawBytes(bytes));
static CliResolvedModule DescribeResolution(TypeScript.Modules.ResolvedModule value) => new()
{
    File = DisplayBytes(value.ResolvedFileName.AsString()), FileBase64 = Base64(value.ResolvedFileName.AsString()),
    PathBase64 = Base64(value.ResolvedPath.AsString()), OriginalPathBase64 = Base64(value.OriginalPath.AsString()),
    AlternateResultBase64 = Base64(value.AlternateResult.AsString()), Extension = value.Extension,
    UsingTsExtension = value.ResolvedUsingTsExtension, UsingExtraExtensions = value.ResolvedUsingExtraExtensions,
    ExternalLibraryImport = value.IsExternalLibraryImport, CustomResolution = value.IsCustomResolution,
    Package = new() { NameBase64 = Base64(value.PackageId.Name), SubModuleNameBase64 = Base64(value.PackageId.SubModuleName),
        VersionBase64 = Base64(value.PackageId.Version), PeerDependenciesBase64 = Base64(value.PackageId.PeerDependencies) },
    Diagnostics = MapOutput(value.ResolutionDiagnostics, diagnostic => Describe(diagnostic, true))
};
// Cold JSON boundary: retain nil versus allocated-empty while creating the
// schema-owned output array; internal Program storage remains shared headers.
static U[]? MapOutput<T, U>(Slice<T> values, Func<T, U> map)
{
    if (values.IsNil) return null;
    var output = new U[values.Length];
    for (int i = 0; i < values.Length; i++) output[i] = map(values[i]);
    return output;
}

static CliTrace[]? DescribeTraces(Slice<TypeScript.Modules.DiagAndArgs> traces) => MapOutput(traces, trace => new CliTrace
{
    Code = trace.Message.Code(),
    Arguments = MapOutput(trace.Args, value => value switch
    {
        string bytes => new CliTraceArgument { Kind = "string", BytesBase64 = Base64(bytes), Text = DisplayBytes(bytes) },
        RootedFilePath path => new CliTraceArgument { Kind = "rooted-file", BytesBase64 = Base64(path.AsString()), Text = DisplayBytes(path.AsString()) },
        RootedDirectoryPath path => new CliTraceArgument { Kind = "rooted-directory", BytesBase64 = Base64(path.AsString()), Text = DisplayBytes(path.AsString()) },
        RootedPath path => new CliTraceArgument { Kind = "rooted-path", BytesBase64 = Base64(path.AsString()), Text = DisplayBytes(path.AsString()) },
        PathKey path => new CliTraceArgument { Kind = "path-key", BytesBase64 = Base64(path.AsString()), Text = DisplayBytes(path.AsString()) },
        TypeScript.PackageJson.PackageDirectory path => new CliTraceArgument { Kind = "package-directory", BytesBase64 = Base64(path.AsDirectoryPath().AsString()), Text = DisplayBytes(path.AsDirectoryPath().AsString()) },
        null => new CliTraceArgument { Kind = "null" },
        bool boolean => new CliTraceArgument { Kind = "bool", Text = boolean ? "true" : "false" },
        int or long or uint or ulong => new CliTraceArgument { Kind = "number", Text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) },
        _ => throw new InvalidOperationException("Unknown resolution trace argument type: " + value.GetType().Name)
    })
});
static CliGraph DescribeGraph(ResolvedProjectGraph graph) => new()
{
    // Explicit user-visible boundary: no hidden Program/global-library construction.
    Boundary = "Source dependency graph; default libraries, automatic global types, project-reference/content-mapper execution and semantic checking are external.",
    RootsBase64 = graph.RootFileNames.Select(path => Base64(path.AsString())).ToArray(),
    MissingFilesBase64 = graph.MissingFiles.Select(path => Base64(path.AsString())).ToArray(),
    Sources = MapOutput(graph.SourceFiles, file =>
    {
        var metadata = graph.GetSourceFileMetaData(file);
        return new CliGraphSource { FileBase64 = Base64(file.FileNameGoBytes()), PathBase64 = Base64(file.PathKey()),
            ScriptKind = file.ScriptKind.ToString(), Declaration = file.IsDeclarationFile,
            PackageType = metadata.PackageJsonType, PackageDirectoryBase64 = Base64(metadata.PackageJsonDirectory.AsString()),
            ImpliedNodeFormat = metadata.ImpliedNodeFormat.ToString(),
            ExternalModule = Ast.IsExternalModule(file),
            LibraryReferencesBase64 = MapOutput(file.LibReferenceDirectives, reference => Base64(reference.FileName)) };
    })!,
    Edges = graph.ResolutionEdges.Select(edge => new CliGraphEdge
    {
        ImporterBase64 = Base64(edge.ImportingFile.FileNameGoBytes()), ReferenceBase64 = Base64(edge.ModuleReference),
        ByteStart = edge.ModuleSpecifier.Pos(), ByteEnd = edge.ModuleSpecifier.End(), Mode = edge.Mode.ToString(),
        ImportIndex = edge.ImportIndex, Synthetic = edge.SyntheticKind, Scheduling = edge.Scheduling.ToString(),
        TargetBase64 = edge.TargetFile is null ? null : Base64(edge.TargetFile.FileNameGoBytes()),
        Resolution = DescribeResolution(edge.Resolution), Traces = DescribeTraces(edge.Traces), Error = edge.Error?.Message
    }).ToArray(),
    PathReferences = graph.PathReferences.Select(edge => new CliGraphReference
    {
        ImporterBase64 = Base64(edge.ImportingFile.FileNameGoBytes()), ReferenceBase64 = Base64(edge.Reference.FileName),
        ByteStart = edge.Reference.Pos(), ByteEnd = edge.Reference.End(), Scheduling = edge.Scheduling.ToString(),
        FileBase64 = Base64(edge.ResolvedFileName.AsString()), PathBase64 = Base64(edge.ResolvedPath.AsString()),
        TargetBase64 = edge.TargetFile is null ? null : Base64(edge.TargetFile.FileNameGoBytes()),
        Diagnostic = edge.Diagnostic is null ? null : Describe(edge.Diagnostic, true)
    }).ToArray(),
    TypeDirectives = graph.TypeDirectives.Select(edge => new CliGraphReference
    {
        ImporterBase64 = Base64(edge.ImportingFile.FileNameGoBytes()), ReferenceBase64 = Base64(edge.Reference.FileName),
        ByteStart = edge.Reference.Pos(), ByteEnd = edge.Reference.End(), Scheduling = edge.Scheduling.ToString(), Mode = edge.Mode.ToString(),
        FileBase64 = edge.Resolution is null ? null : Base64(edge.Resolution.ResolvedFileName.AsString()),
        PathBase64 = edge.Resolution is null ? null : Base64(edge.Resolution.ResolvedPath.AsString()),
        TargetBase64 = edge.TargetFile is null ? null : Base64(edge.TargetFile.FileNameGoBytes()), Traces = DescribeTraces(edge.Traces),
        TypeResolution = edge.Resolution is null ? null : new CliResolvedTypeDirective
        {
            FileBase64 = Base64(edge.Resolution.ResolvedFileName.AsString()),
            PathBase64 = Base64(edge.Resolution.ResolvedPath.AsString()),
            OriginalPathBase64 = Base64(edge.Resolution.OriginalPath.AsString()),
            Primary = edge.Resolution.Primary, ExternalLibraryImport = edge.Resolution.IsExternalLibraryImport,
            Package = new CliPackage
            {
                NameBase64 = Base64(edge.Resolution.PackageId.Name),
                SubModuleNameBase64 = Base64(edge.Resolution.PackageId.SubModuleName),
                VersionBase64 = Base64(edge.Resolution.PackageId.Version),
                PeerDependenciesBase64 = Base64(edge.Resolution.PackageId.PeerDependencies)
            },
            Diagnostics = MapOutput(edge.Resolution.ResolutionDiagnostics, value => Describe(value, true))
        }
    }).ToArray()
};

static CliProgram DescribeProgram(ProgramInspection inspection)
{
    var program = inspection.Program;
    var modules = new List<CliProgramModule>();
    program.ForEachResolvedModule((result, name, mode, path) => modules.Add(new()
    {
        ImporterPathBase64 = Base64(path.AsString()), NameBase64 = Base64(name), Mode = mode.ToString(),
        Module = result is null ? null : DescribeResolution(result)
    }));
    var types = new List<CliProgramModule>();
    program.ForEachResolvedTypeReferenceDirective((result, name, mode, path) => types.Add(new()
    {
        ImporterPathBase64 = Base64(path.AsString()), NameBase64 = Base64(name), Mode = mode.ToString(),
        Type = result is null ? null : new()
        {
            FileBase64 = Base64(result.ResolvedFileName.AsString()), PathBase64 = Base64(result.ResolvedPath.AsString()),
            OriginalPathBase64 = Base64(result.OriginalPath.AsString()), Primary = result.Primary,
            ExternalLibraryImport = result.IsExternalLibraryImport,
            Package = new() { NameBase64 = Base64(result.PackageId.Name), SubModuleNameBase64 = Base64(result.PackageId.SubModuleName), VersionBase64 = Base64(result.PackageId.Version), PeerDependenciesBase64 = Base64(result.PackageId.PeerDependencies) },
            Diagnostics = MapOutput(result.ResolutionDiagnostics, diagnostic => Describe(diagnostic, true))
        }
    }));
    var references = new List<CliProgramReference>();
    program.RangeResolvedProjectReference((path, config, parent, index) =>
    {
        references.Add(new() { PathBase64 = Base64(path.AsString()), ConfigBase64 = config is null ? null : Base64(config.ConfigName().AsString()), ParentConfigBase64 = Base64(parent.ConfigName().AsString()), Index = index });
        return true;
    });
    return new()
    {
        Boundary = "Program source/library/type/reference construction, syntax/bind and construction diagnostics; semantic checking, emit, build/watch and content-mapper subprocess execution are not requested.",
        SemanticCheckingRequested = false,
        RootsBase64 = (inspection.Configuration.Parsed!.FileNames() ?? []).Select(path => Base64(path.AsString())).ToArray(),
        Sources = MapOutput(program.SourceFiles(), file =>
        {
            var key = PathKey.FromGoString(file.PathKey());
            var metadata = program.GetSourceFileMetaData(key);
            return new CliProgramSource
            {
                FileBase64 = Base64(file.FileNameGoBytes()), PathBase64 = Base64(file.PathKey()), ScriptKind = file.ScriptKind.ToString(), Declaration = file.IsDeclarationFile,
                DefaultLibrary = program.IsSourceFileDefaultLibrary(key), ExternalLibrary = program.IsSourceFileFromExternalLibrary(file),
                ExternalModule = Ast.IsExternalModule(file), PackageType = metadata.PackageJsonType,
                PackageDirectoryBase64 = Base64(metadata.PackageJsonDirectory.AsString()), ImpliedNodeFormat = metadata.ImpliedNodeFormat.ToString(),
                LibraryReferencesBase64 = MapOutput(file.LibReferenceDirectives, reference => Base64(reference.FileName))
            };
        })!,
        // Resolution tables have map ownership; deterministic presentation does
        // not reorder the actual Program sources or per-request trace streams.
        Modules = modules.OrderBy(row => row.ImporterPathBase64, StringComparer.Ordinal).ThenBy(row => row.NameBase64, StringComparer.Ordinal).ThenBy(row => row.Mode, StringComparer.Ordinal).ToArray(),
        Types = types.OrderBy(row => row.ImporterPathBase64, StringComparer.Ordinal).ThenBy(row => row.NameBase64, StringComparer.Ordinal).ThenBy(row => row.Mode, StringComparer.Ordinal).ToArray(),
        References = references.ToArray(), Traces = DescribeTraces(inspection.Traces),
        ConfigurationDiagnostics = inspection.ConfigurationDiagnostics.Select(diagnostic => Describe(diagnostic, true)).ToArray(),
        SyntacticDiagnostics = inspection.SyntacticDiagnostics.Select(diagnostic => Describe(diagnostic, true)).ToArray(),
        BindDiagnostics = inspection.BindDiagnostics.Select(diagnostic => Describe(diagnostic, true)).ToArray(),
        ConstructionDiagnostics = inspection.ProgramDiagnostics.Select(diagnostic => Describe(diagnostic, true)).ToArray(),
        InclusionDiagnostics = inspection.InclusionDiagnostics.Select(diagnostic => Describe(diagnostic, true)).ToArray()
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
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public CliGraph? Graph { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public CliProgram? Program { get; set; }
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

internal sealed class CliProgram
{
    public string Boundary { get; set; } = "";
    public bool SemanticCheckingRequested { get; set; }
    public string[] RootsBase64 { get; set; } = [];
    public CliProgramSource[] Sources { get; set; } = [];
    public CliProgramModule[] Modules { get; set; } = [];
    public CliProgramModule[] Types { get; set; } = [];
    public CliProgramReference[] References { get; set; } = [];
    public CliTrace[]? Traces { get; set; }
    public CliDiagnostic[] ConfigurationDiagnostics { get; set; } = [];
    public CliDiagnostic[] SyntacticDiagnostics { get; set; } = [];
    public CliDiagnostic[] BindDiagnostics { get; set; } = [];
    public CliDiagnostic[] ConstructionDiagnostics { get; set; } = [];
    public CliDiagnostic[] InclusionDiagnostics { get; set; } = [];
}
internal sealed class CliProgramSource
{
    public string FileBase64 { get; set; } = "";
    public string PathBase64 { get; set; } = "";
    public string ScriptKind { get; set; } = "";
    public bool Declaration { get; set; }
    public bool DefaultLibrary { get; set; }
    public bool ExternalLibrary { get; set; }
    public bool ExternalModule { get; set; }
    public string PackageType { get; set; } = "";
    public string PackageDirectoryBase64 { get; set; } = "";
    public string ImpliedNodeFormat { get; set; } = "";
    public string[]? LibraryReferencesBase64 { get; set; }
}
internal sealed class CliProgramModule
{
    public string ImporterPathBase64 { get; set; } = "";
    public string NameBase64 { get; set; } = "";
    public string Mode { get; set; } = "";
    public CliResolvedModule? Module { get; set; }
    public CliResolvedTypeDirective? Type { get; set; }
}
internal sealed class CliProgramReference
{
    public string PathBase64 { get; set; } = "";
    public string? ConfigBase64 { get; set; }
    public string ParentConfigBase64 { get; set; } = "";
    public int Index { get; set; }
}
internal sealed class CliGraph
{
    public string Boundary { get; set; } = "";
    public string[] RootsBase64 { get; set; } = [];
    public string[] MissingFilesBase64 { get; set; } = [];
    public CliGraphSource[] Sources { get; set; } = [];
    public CliGraphEdge[] Edges { get; set; } = [];
    public CliGraphReference[] PathReferences { get; set; } = [];
    public CliGraphReference[] TypeDirectives { get; set; } = [];
}
internal sealed class CliGraphSource
{
    public string FileBase64 { get; set; } = "";
    public string PathBase64 { get; set; } = "";
    public string ScriptKind { get; set; } = "";
    public string PackageType { get; set; } = "";
    public string PackageDirectoryBase64 { get; set; } = "";
    public string ImpliedNodeFormat { get; set; } = "";
    public bool Declaration { get; set; }
    public bool ExternalModule { get; set; }
    public string[]? LibraryReferencesBase64 { get; set; }
}
internal sealed class CliGraphEdge
{
    public string ImporterBase64 { get; set; } = "";
    public string ReferenceBase64 { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Synthetic { get; set; } = "";
    public string Scheduling { get; set; } = "";
    public int ByteStart { get; set; }
    public int ByteEnd { get; set; }
    public int ImportIndex { get; set; }
    public string? TargetBase64 { get; set; }
    public CliResolvedModule Resolution { get; set; } = new();
    public CliTrace[]? Traces { get; set; }
    public string? Error { get; set; }
}
internal sealed class CliGraphReference
{
    public string ImporterBase64 { get; set; } = "";
    public string ReferenceBase64 { get; set; } = "";
    public string Scheduling { get; set; } = "";
    public string Mode { get; set; } = "";
    public int ByteStart { get; set; }
    public int ByteEnd { get; set; }
    public string? FileBase64 { get; set; }
    public string? PathBase64 { get; set; }
    public string? TargetBase64 { get; set; }
    public CliDiagnostic? Diagnostic { get; set; }
    public CliTrace[]? Traces { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public CliResolvedTypeDirective? TypeResolution { get; set; }
}
internal sealed class CliResolvedTypeDirective
{
    public string FileBase64 { get; set; } = "";
    public string PathBase64 { get; set; } = "";
    public string OriginalPathBase64 { get; set; } = "";
    public bool Primary { get; set; }
    public bool ExternalLibraryImport { get; set; }
    public CliPackage Package { get; set; } = new();
    public CliDiagnostic[]? Diagnostics { get; set; }
}
internal sealed class CliResolvedModule
{
    public string File { get; set; } = "";
    public string FileBase64 { get; set; } = "";
    public string PathBase64 { get; set; } = "";
    public string OriginalPathBase64 { get; set; } = "";
    public string AlternateResultBase64 { get; set; } = "";
    public string Extension { get; set; } = "";
    public bool UsingTsExtension { get; set; }
    public bool UsingExtraExtensions { get; set; }
    public bool ExternalLibraryImport { get; set; }
    public bool CustomResolution { get; set; }
    public CliPackage Package { get; set; } = new();
    public CliDiagnostic[]? Diagnostics { get; set; }
}
internal sealed class CliPackage
{
    public string NameBase64 { get; set; } = "";
    public string SubModuleNameBase64 { get; set; } = "";
    public string VersionBase64 { get; set; } = "";
    public string PeerDependenciesBase64 { get; set; } = "";
}
internal sealed class CliTrace { public int Code { get; set; } public CliTraceArgument[]? Arguments { get; set; } }
internal sealed class CliTraceArgument { public string Kind { get; set; } = ""; public string? Text { get; set; } public string? BytesBase64 { get; set; } }

[System.Text.Json.Serialization.JsonSerializable(typeof(CliResult))]
internal partial class CliJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}

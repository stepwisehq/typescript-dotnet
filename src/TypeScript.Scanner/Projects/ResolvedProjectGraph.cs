// Authored inspection result over original source loading and resolution.
// Byte-valued names and source locations stay intact; this is not a full Program.
using TypeScript.Modules;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Vfs;

namespace TypeScript.Compiler;

public enum DependencyScheduling
{
    Scheduled, Unresolved, ResolutionDiagnostic, NoResolve, JavaScriptDisabled,
    JSDocInTypeScript, ModuleAugmentation, ElidedJavaScriptDepth, MissingFile,
    UnsupportedExtension
}

public sealed class ResolvedProjectEdge
{
    public SourceFile ImportingFile { get; internal init; } = null!;
    public Node ModuleSpecifier { get; internal init; } = null!;
    // Original Go byte string, as are SourceFile.Text and typed .AsString values.
    private ByteStringValue moduleReference;
    public string ModuleReference { get => moduleReference.Materialize()!; internal init => moduleReference = ByteStringValue.FromString(value); }
    internal ByteStringValue ModuleReferenceValue { get => moduleReference.Snapshot(); init => moduleReference = value.Snapshot(); }
    public ResolutionMode Mode { get; internal init; }
    public int ImportIndex { get; internal init; }
    public string SyntheticKind { get; internal init; } = "";
    public ResolvedModule Resolution { get; internal init; } = null!;
    public Slice<DiagAndArgs> Traces { get; internal init; }
    public Exception? Error { get; internal init; }
    public DependencyScheduling Scheduling { get; internal set; }
    public SourceFile? TargetFile { get; internal set; }
}

public sealed class ResolvedProjectReferenceEdge
{
    public SourceFile ImportingFile { get; internal init; } = null!;
    public FileReference Reference { get; internal init; } = null!;
    public RootedFilePath ResolvedFileName { get; internal init; }
    public PathKey ResolvedPath { get; internal init; }
    public Diagnostic? Diagnostic { get; internal set; }
    // Actual source processing diagnostic; converted after include reasons exist.
    internal ProcessingDiagnostic? ProcessingDiagnostic { get; init; }
    public DependencyScheduling Scheduling { get; internal set; }
    public SourceFile? TargetFile { get; internal set; }
}

public sealed class ResolvedProjectTypeDirective
{
    public SourceFile ImportingFile { get; internal init; } = null!;
    public FileReference Reference { get; internal init; } = null!;
    public ResolutionMode Mode { get; internal init; }
    public ResolvedTypeReferenceDirective? Resolution { get; internal init; }
    public Slice<DiagAndArgs> Traces { get; internal init; }
    public DependencyScheduling Scheduling { get; internal set; }
    public SourceFile? TargetFile { get; internal set; }
}

public sealed class ResolvedProjectGraph
{
    private readonly ProcessedFiles processed;
    internal readonly IResolutionHost host;
    internal readonly DefaultResolver resolver;
    internal CompilerOptions Options => Configuration.Parsed!.CompilerOptions();
    public ConfigurationInspection Configuration { get; }
    public Slice<SourceFile> SourceFiles => processed.files;
    public IReadOnlyList<RootedFilePath> RootFileNames { get; }
    public IReadOnlyList<ResolvedProjectEdge> ResolutionEdges => processed.graphEdges.AsReadOnly();
    public IReadOnlyList<ResolvedProjectReferenceEdge> PathReferences => processed.pathReferences.AsReadOnly();
    public IReadOnlyList<ResolvedProjectTypeDirective> TypeDirectives => processed.typeDirectives.AsReadOnly();
    public IReadOnlyList<Diagnostic> ParseDiagnostics { get; }
    public IReadOnlyList<Diagnostic> InclusionDiagnostics => processed.processingDiagnostics.AsSpan().ToArray();
    public IReadOnlyList<RootedFilePath> MissingFiles => processed.missingFiles.Select(static key => RootedFilePath.FromValue(key.Value)).ToArray();
    public SourceFileMetaData GetSourceFileMetaData(SourceFile file)
    {
        if (!processed.filesByPath.TryGetValue(file.PathKeyKey(), out var owner) || !ReferenceEquals(owner, file))
            throw new ArgumentException("SourceFile belongs to a different graph.", nameof(file));
        return processed.metadata[file.PathKeyKey()];
    }

    internal IReadOnlyDictionary<ByteStringKey, SourceFile> FilesByPath => processed.filesByPath;
    internal Slice<RootedFilePath> GetRedirectTargets(PathKey path) =>
        processed.redirectTargetsMap.GetValueOrDefault(ByteStringKey.FromValue(path.AsValue()));
    internal static ResolvedProjectGraph FromParsedConfiguration(ParsedCommandLine parsed, IFS fileSystem) =>
        new(new ConfigurationInspection(parsed), fileSystem);

    internal ResolvedProjectGraph(ConfigurationInspection inspection, IFS? fileSystem = null)
    {
        Configuration = inspection;
        inspection.RequireValid();
        var parsed = inspection.Parsed!;
        // Execution of content mappers and references belongs to Program; never
        // parse an untransformed custom source as if it were ordinary TypeScript.
        if (parsed.ContentMappers() is { Count: > 0 })
            throw new ProjectException("contentMapperExecution", "Project graph loading does not execute content mappers.", inspection.ConfigurationPath);
        RootFileNames = (parsed.FileNames() ?? []).AsReadOnly();
        host = new ResolutionDiskHost(fileSystem ?? TypeScript.Vfs.OsVfs.OsVfs.FS(), parsed.BaseDirectory());
        resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = host, CompilerOptions = parsed.CompilerOptions(), ProjectNameValue = parsed.ConfigName().AsValue() });
        processed = FileLoader.ProcessResolvedProjectFiles(host, resolver, parsed.CompilerOptions(), RootFileNames, new ProjectConfiguration(parsed));
        var parseDiagnostics = new List<Diagnostic>();
        foreach (var file in processed.files)
            foreach (var diagnostic in file.Diagnostics()) parseDiagnostics.Add(diagnostic);
        ParseDiagnostics = parseDiagnostics.ToArray();
    }
}

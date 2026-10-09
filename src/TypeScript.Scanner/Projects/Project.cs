// Authored bounded project admission/disk adapter over source-mapped loading,
// relative resolver, diagnostic policy and the shared original checker.
using TypeScript.Checking;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Scanner;

namespace TypeScript.Compiler;
public sealed class Project : ICheckerProgramHost
{
    private readonly ProjectConfiguration configuration;
    private readonly ProjectDiskHost? disk;
    private readonly ResolvedProjectGraph? resolvedGraph;
    private readonly DefaultResolver resolver;
    private readonly Dictionary<string, SourceFile> filesByPath = new(StringComparer.Ordinal);
    private readonly List<SourceFile> sourceFiles = [], roots = [], users = [];
    private readonly List<ProjectResolutionEdge> edges = [];
    private readonly List<Diagnostic> programDiagnostics = [];
    private readonly List<Diagnostic> parseDiagnostics = [];
    private Checker? checker;
    public string ConfigurationPath => configuration.FileName;
    public string RootDirectory => configuration.Root;
    public SourceFile ConfigSourceFile => configuration.SourceFile;
    // Fixed options belong to this loaded snapshot; external callers cannot
    // change resolution/checking options after dependency discovery.
    internal CompilerOptions Options => configuration.Options;

    CompilerOptions ICheckerProgramHost.Options => Options;
    bool ICheckerProgramHost.AllowsNonRelativeModuleSpecifiers => resolvedGraph is not null;
    public IReadOnlyList<SourceFile> SourceFiles => sourceFiles.AsReadOnly();
    public IReadOnlyList<SourceFile> RootFiles => roots.AsReadOnly();
    // The normalized, deduplicated configuration filenames include missing
    // roots; RootFiles contains only their successfully parsed SourceFiles.
    internal IReadOnlyList<string> RootFileNames { get; }
    public IReadOnlyList<SourceFile> UserFiles => users.AsReadOnly();
    public IReadOnlyList<SourceFile> LibraryFiles { get; }
    public IReadOnlyList<ProjectResolutionEdge> ResolutionEdges => edges.AsReadOnly();
    public IReadOnlyList<Diagnostic> ParseDiagnostics => parseDiagnostics.AsReadOnly();
    public IReadOnlyList<Diagnostic> ProgramDiagnostics => programDiagnostics.AsReadOnly();

    public Checker Checker
    {
        get
        {
            if (parseDiagnostics.Count != 0 || users.Count == 0)
                throw new ProjectException("semanticEntry", "The project must have valid existing source files before semantic entry.", ConfigurationPath);
            return checker ??= TypeScript.Checking.Checker.CreateForProgram(this);
        }
    }

    public static Project Load(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath) || configPath.IndexOf('\0') >= 0)
            throw new ProjectException("configurationPath", "The configuration path must be nonempty and contain no NUL characters.", configPath);
        if (!OperatingSystem.IsLinux())
            throw new ProjectException("platform", "This project host currently supports Linux case-sensitive disk identity.", configPath);
        return new Project(new ProjectConfiguration(configPath));
    }

    public static ConfigurationInspection InspectConfiguration(string configPath) => new(configPath);
    public static ResolvedProjectGraph InspectResolvedGraph(string configPath) => new(InspectConfiguration(configPath));
    public static Project LoadConfiguration(string configPath)
    {
        ConfigurationInspection inspection = InspectConfiguration(configPath);
        inspection.RequireValid();
        // Resolve/parse the actual graph before downstream semantic admission.
        ResolvedProjectGraph graph = new(inspection);
        ProjectCheckingAdmission.Validate(inspection.Parsed!, inspection.ConfigurationPath);
        foreach (SourceFile file in graph.SourceFiles)
        {
            RequireImplementationFile(PathByteText.ToUnicode(file.FileName()));
            if (file.Diagnostics() is not { Count: > 0 }) ProjectAdmission.Validate(file, allowNonRelativeModuleSpecifiers: true);
        }
        return new Project(graph);
    }

    private Project(ResolvedProjectGraph graph)
    {
        // This graph was created privately by LoadConfiguration. Keep its actual
        // AST identities, byte text and path keys, while preserving the public
        // checking API's established scalar disk filename role. Standalone graph
        // inspection retains the original byte-valued filenames unchanged.
        foreach (SourceFile file in graph.SourceFiles)
        {
            SourceFileParseOptions parseOptions = file.ParseOptions();
            file.parseOptions = parseOptions with
            {
                FileName = RootedFilePath.FromGoString(file.FileNameGoBytes()).ToUnicode(),
                FileNameIsGoBytes = false
            };
        }
        resolvedGraph = graph;
        configuration = new ProjectConfiguration(graph.Configuration);
        resolver = graph.resolver;
        LibraryFiles = FixedLibraryEnvironment.Load();
        RootFileNames = graph.RootFileNames.Select(path => path.ToUnicode()).ToArray();
        sourceFiles.AddRange(LibraryFiles);
        sourceFiles.AddRange(graph.SourceFiles);
        users.AddRange(graph.SourceFiles);
        foreach (SourceFile library in LibraryFiles) filesByPath.Add(library.PathKey(), library);
        foreach (var pair in graph.FilesByPath) filesByPath[pair.Key] = pair.Value;
        parseDiagnostics.AddRange(graph.ParseDiagnostics);
        programDiagnostics.AddRange(graph.InclusionDiagnostics);
        foreach (var edge in graph.ResolutionEdges)
            edges.Add(new(edge.ImportingFile, edge.ModuleSpecifier, ProjectDiskHost.DecodeModuleReference(edge.ModuleReference), edge.Mode, edge.Resolution, edge.TargetFile));
        foreach (var root in graph.RootFileNames)
            if (filesByPath.TryGetValue(root.AsString(), out var file) && !roots.Contains(file, ReferenceEqualityComparer.Instance)) roots.Add(file);
    }

    private Project(ProjectConfiguration configuration)
    {
        this.configuration = configuration;
        disk = new(configuration.Root);
        var resolutionHost = new LegacyResolutionHost(disk);
        resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = resolutionHost, CompilerOptions = Options, ProjectName = GoString.ToUtf8(configuration.FileName) });
        LibraryFiles = FixedLibraryEnvironment.Load();
        var rootNames = new List<string>();
        foreach (string root in configuration.Files)
        {
            string path;
            if (configuration.Parsed is not null)
            {
                // Genuine configuration discovery already supplies normalized
                // absolute filenames. Keep their identity and inclusion metadata;
                // the legacy entry alone accepts relative user-supplied roots.
                if (root.IndexOf('\0') >= 0)
                    throw new ProjectException("path", "Configuration filename contains a NUL character.", root);
                disk.EnsureBounded(root);
                path = root;
            }
            else
            {
                path = disk.Resolve(configuration.Root, root);
            }

            RequireImplementationFile(path);
            if (!rootNames.Contains(path, StringComparer.Ordinal))
                rootNames.Add(path);
        }

        RootFileNames = rootNames.AsReadOnly();
        ProcessedFiles processed = FileLoader.ProcessAllProgramFiles(disk, resolutionHost, resolver, Options, LibraryFiles, rootNames, configuration);
        sourceFiles.AddRange(processed.files);
        foreach (var pair in processed.filesByPath)
            filesByPath.Add(pair.Key, pair.Value);
        programDiagnostics.AddRange(processed.processingDiagnostics);
        edges.AddRange(processed.resolutionEdges);
        foreach (SourceFile source in sourceFiles)
        {
            if (LibraryFiles.Contains(source, ReferenceEqualityComparer.Instance))
                continue;
            users.Add(source);
            if (source.Diagnostics()is { Count: > 0 } errors)
                parseDiagnostics.AddRange(errors);
        }

        foreach (string name in rootNames)
            if (filesByPath.TryGetValue(name, out SourceFile? source) && !roots.Contains(source, ReferenceEqualityComparer.Instance))
                roots.Add(source);
    }

    internal static void RequireImplementationFile(string path)
    {
        if (!path.EndsWith(".ts", StringComparison.Ordinal) || TsPath.IsDeclarationFileName(path))
            throw new ProjectException("sourceFileForm", "Only user .ts implementations are supported: " + path, path);
    }

    public IReadOnlyList<Diagnostic> Check()
    {
        var all = new List<Diagnostic>(programDiagnostics);
        all.AddRange(parseDiagnostics);
        // Malformed syntax has no semantic completeness promise or checker entry.
        if (parseDiagnostics.Count != 0 || users.Count == 0)
            return Program.SortAndDeduplicateDiagnostics(all) ?? [];
        Checker shared = Checker;
        foreach (SourceFile file in users)
            all.AddRange(Program.GetSemanticDiagnosticsWithChecker(file, Options, shared) ?? []);
        all.AddRange(shared.GetGlobalDiagnostics() ?? []);
        return Program.SortAndDeduplicateDiagnostics(all) ?? [];
    }

    public ResolvedModule? GetResolvedModule(SourceFile importingFile, string moduleReference, ResolutionMode mode)
    {
        ValidateOwned(importingFile);
        // Cached only: checker queries cannot discover different files after loading.
        foreach (ProjectResolutionEdge edge in edges)
            if (ReferenceEquals(edge.ImportingFile, importingFile) && edge.ModuleReference == moduleReference && edge.Mode == mode)
                return edge.Resolution;
        return null;
    }

    ResolvedModule? ICheckerProgramHost.GetResolvedModule(SourceFile importingFile, string moduleReference, ResolutionMode mode) => GetResolvedModule(importingFile, ProjectDiskHost.DecodeModuleReference(moduleReference), mode);
    string ICheckerProgramHost.GetSourceFileNameGoBytes(SourceFile file)
    {
        ValidateOwned(file);
        return file.FileNameGoBytes();
    }

    public SourceFile? GetSourceFileForResolvedModule(ResolvedModule module) => module.IsResolved() && filesByPath.TryGetValue(resolvedGraph is null ? module.ResolvedPath.ToUnicode() : module.ResolvedPath.AsString(), out SourceFile? file) ? file : null;
    public ModuleKind GetEmitModuleFormatOfFile(SourceFile sourceFile)
    {
        ValidateOwned(sourceFile);
        return resolvedGraph is null ? Options.GetEmitModuleKind() : Ast.GetEmitModuleFormatOfFileWorker(RootedFilePath.FromGoString(sourceFile.FileNameGoBytes()), Options, GetCheckingMetadata(sourceFile));
    }

    public ResolutionMode GetImpliedNodeFormatForEmit(SourceFile sourceFile)
    {
        ValidateOwned(sourceFile);
        return resolvedGraph is null ? ResolutionMode.None : Ast.GetImpliedNodeFormatForEmitWorker(RootedFilePath.FromGoString(sourceFile.FileNameGoBytes()), Options.GetEmitModuleKind(), GetCheckingMetadata(sourceFile));
    }

    // fileloader.go:1037/1075: no attribute override, require/import= or dynamic
    // import is admitted. Fixed non-Node ESM returns ESNext at static usages.
    public ResolutionMode GetEmitSyntaxForUsageLocation(SourceFile sourceFile, Node location)
    {
        ValidateOwned(sourceFile);
        return resolvedGraph is null ? ResolutionMode.ESNext : FileLoader.GetEmitSyntaxForUsageLocationWorker(RootedFilePath.FromGoString(sourceFile.FileNameGoBytes()), GetCheckingMetadata(sourceFile), location, Options);
    }

    public ResolutionMode GetModeForUsageLocation(SourceFile sourceFile, Node location)
    {
        ValidateOwned(sourceFile);
        return resolvedGraph is null ? ResolutionMode.ESNext : FileLoader.GetModeForUsageLocation(RootedFilePath.FromGoString(sourceFile.FileNameGoBytes()), GetCheckingMetadata(sourceFile), location, Options);
    }
    public bool IsSourceFileDefaultLibrary(SourceFile sourceFile)
    {
        ValidateOwned(sourceFile);
        return false;
    }

    public ResolutionMode GetDefaultResolutionModeForFile(SourceFile sourceFile)
    {
        ValidateOwned(sourceFile);
        // fileloader.go:1029. Bundler consults resolution modes, but the genuine
        // fixed .ts/ESNext source has no implied Node-format producer.
        return resolvedGraph is null ? ResolutionMode.None : FileLoader.GetDefaultResolutionModeForFile(RootedFilePath.FromGoString(sourceFile.FileNameGoBytes()), GetCheckingMetadata(sourceFile), Options);
    }

    string TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.BaseDirectory() => resolvedGraph is null ? RootDirectory : resolvedGraph.host.GetCurrentDirectory().AsString();
    bool TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.FileNamesAreGoBytes => resolvedGraph is not null;
    CaseSensitivity TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.CaseSensitivity() => resolvedGraph?.host.FS().CaseSensitivity() ?? CaseSensitivity.CaseSensitive;
    IReadOnlyList<string> TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetRedirectTargets(PathKey path) => resolvedGraph?.GetRedirectTargets(path) ?? [];
    string TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetSourceOfProjectReferenceIfOutputIncluded(SourceFile file)
    {
        ValidateOwned(file);
        return resolvedGraph is null ? file.FileName() : file.FileNameGoBytes(); // No project reference redirects.
    }

    bool TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.FileExists(string path) => resolvedGraph is null ? disk!.ModuleSpecifierFileExists(path) : resolvedGraph.host.FS().FileExists(RootedFilePath.FromGoString(path));
    string TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetNearestAncestorDirectoryWithPackageJson(string directory)
    {
        var scoped = resolver.GetPackageScopeForPath(resolvedGraph is null ? RootedDirectoryPath.FromUnicode(directory) : RootedDirectoryPath.FromGoString(directory));
        return scoped?.Exists() == true ? (resolvedGraph is null ? scoped.PackageDirectory.AsDirectoryPath().ToUnicode() : scoped.PackageDirectory.AsDirectoryPath().AsString()) : "";
    }

    TypeScript.PackageJson.InfoCacheEntry? TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetPackageJsonInfo(string path)
    {
        string directory = TsPath.GetDirectoryPath(path);
        var scoped = resolver.GetPackageScopeForPath(resolvedGraph is null ? RootedDirectoryPath.FromUnicode(directory) : RootedDirectoryPath.FromGoString(directory));
        return scoped?.Exists() == true && (resolvedGraph is null ? scoped.PackageDirectory.AsDirectoryPath().ToUnicode() : scoped.PackageDirectory.AsDirectoryPath().AsString()) == directory ? scoped : null;
    }

    ResolvedModule? TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetResolvedModuleFromModuleSpecifier(SourceFile file, Node specifier) => GetResolvedModule(file, ProjectDiskHost.DecodeModuleReference(specifier.Text()), GetModeForUsageLocation(file, specifier));
    private SourceFileMetaData GetCheckingMetadata(SourceFile file) => LibraryFiles.Contains(file, ReferenceEqualityComparer.Instance)
        ? new("", default, (ResolutionMode)Ast.GetImpliedNodeFormatForFile(RootedFilePath.FromGoString(file.FileNameGoBytes()), ""))
        : resolvedGraph!.GetSourceFileMetaData(file);

    private void ValidateOwned(SourceFile sourceFile)
    {
        if (!filesByPath.TryGetValue(sourceFile.PathKey(), out SourceFile? owned) || !ReferenceEquals(owned, sourceFile))
            throw new ArgumentException("SourceFile belongs to a different project.", nameof(sourceFile));
    }
}

public sealed record ProjectResolutionEdge(SourceFile ImportingFile, Node ModuleSpecifier, string ModuleReference, ResolutionMode Mode, ResolvedModule Resolution, SourceFile? TargetFile);

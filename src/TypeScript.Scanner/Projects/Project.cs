// Authored disk API adapter: genuine Program checking for tsconfig and a named
// legacy tscheck.json compatibility entry.
using TypeScript.Checking;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Scanner;
using TypeScript.TsOptions;

namespace TypeScript.Compiler;
public sealed class Project : ICheckerProgramHost
{
    private readonly ProjectConfiguration configuration;
    private readonly ProjectDiskHost? disk;
    private readonly Program? genuineProgram;
    private ICheckerProgramHost ActualHost => genuineProgram ?? throw MissingProgramService("genuine Program");

    private readonly DefaultResolver resolver = null!;
    private readonly Dictionary<ByteStringKey, SourceFile> filesByPath = new(ByteStringKey.Comparer);
    private Slice<SourceFile> sourceFiles;
    private readonly List<SourceFile> roots = [], users = [];
    private readonly List<ProjectResolutionEdge> edges = [];
    private Slice<Diagnostic> programDiagnostics;
    private Slice<Diagnostic> parseDiagnostics;
    private Checker? checker;
    public string ConfigurationPath => configuration.FileName;
    public string RootDirectory => configuration.Root;
    public SourceFile ConfigSourceFile => configuration.SourceFile;
    // Fixed options belong to this loaded snapshot; external callers cannot
    // change resolution/checking options after dependency discovery.
    internal CompilerOptions Options => genuineProgram?.Options() ?? configuration.Options;

    CompilerOptions ICheckerProgramHost.Options => Options;

    bool ICheckerProgramHost.AllowsNonRelativeModuleSpecifiers => genuineProgram is not null;
    public Slice<SourceFile> SourceFiles => sourceFiles;
    public IReadOnlyList<SourceFile> RootFiles => roots.AsReadOnly();
    // The normalized, deduplicated configuration filenames include missing
    // roots; RootFiles contains only their successfully parsed SourceFiles.
    internal IReadOnlyList<string> RootFileNames { get; }
    public IReadOnlyList<SourceFile> UserFiles => users.AsReadOnly();
    public IReadOnlyList<SourceFile> LibraryFiles { get; }
    public IReadOnlyList<ProjectResolutionEdge> ResolutionEdges => edges.AsReadOnly();
    public IReadOnlyList<Diagnostic> ParseDiagnostics => parseDiagnostics.AsSpan().ToArray();
    public IReadOnlyList<Diagnostic> ProgramDiagnostics => programDiagnostics.AsSpan().ToArray();

    void ICheckerProgramHost.BindSourceFiles()
    {
        if (genuineProgram is not null)
            ActualHost.BindSourceFiles();
        else
            foreach (var file in sourceFiles)
                Binder.BindSourceFile(file);
    }

    Dictionary<ByteStringKey, ModeAwareCache<ResolvedModule?>> ICheckerProgramHost.GetResolvedModules() => ActualHost.GetResolvedModules();
    Dictionary<string, bool> ICheckerProgramHost.GetPackagesMap() => ActualHost.GetPackagesMap();
    SourceFileMetaData ICheckerProgramHost.GetSourceFileMetaData(PathKey path) => ActualHost.GetSourceFileMetaData(path);
    (string ModuleReference, Node? Specifier) ICheckerProgramHost.GetJSXRuntimeImportSpecifier(PathKey path) => ActualHost.GetJSXRuntimeImportSpecifier(path);
    (ByteStringValue ModuleReference, Node? Specifier) ICheckerProgramHost.GetJSXRuntimeImportSpecifierValue(PathKey path) => ActualHost.GetJSXRuntimeImportSpecifierValue(path);
    Node? ICheckerProgramHost.GetImportHelpersImportSpecifier(PathKey path) => ActualHost.GetImportHelpersImportSpecifier(path);
    bool ICheckerProgramHost.SourceFileMayBeEmitted(SourceFile file, bool forceDtsEmit) => ActualHost.SourceFileMayBeEmitted(file, forceDtsEmit);
    SourceOutputAndProjectReference? ICheckerProgramHost.GetProjectReferenceFromOutputDts(PathKey path) => ActualHost.GetProjectReferenceFromOutputDts(path);
    ParsedCommandLine? ICheckerProgramHost.GetRedirectForResolution(SourceFile file) => ActualHost.GetRedirectForResolution(file);
    private ProjectException MissingProgramService(string operation) =>
        new("semanticUnsupported", operation + " requires the genuine Program environment.", ConfigurationPath);
    public Checker Checker
    {
        get
        {
            if (checker is not null)
                return checker;
            if (genuineProgram is not null)
            {
                var(actual, done) = genuineProgram.GetTypeChecker();
                try
                {
                    return checker = actual;
                }
                finally
                {
                    done();
                }
            }

            if (parseDiagnostics.Count != 0 || users.Count == 0)
                throw new ProjectException("semanticEntry", "The project must have valid existing source files before semantic entry.", ConfigurationPath);
            return checker = TypeScript.Checking.Checker.CreateForProgram(this);
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
    public static Project LoadConfiguration(string configPath, CompilerOptions? existingOptions = null)
    {
        ConfigurationInspection inspection = new(configPath, existingOptions);
        inspection.RequireValid();
        var fs = TypeScript.Bundled.Bundled.WrapFS(TypeScript.Vfs.OsVfs.OsVfs.FS());
        var host = CompilerHosts.NewCachedFSCompilerHost(fs, TypeScript.Bundled.Bundled.LibPath());
        var program = Program.NewProgram(new() { Config = inspection.Parsed!, Host = host });
        if (program.ModuleResolutionError() is { } error)
            throw new ProjectException("moduleResolution", error.Message, inspection.ConfigurationPath);
        return new Project(inspection, program);
    }

    private Project(ConfigurationInspection inspection, Program program)
    {
        genuineProgram = program;
        configuration = new ProjectConfiguration(inspection);
        // This Program is private to the disk Project adapter. Preserve the public
        // Unicode filename contract while internal source algorithms retain the
        // original bytes through FileNameGoBytes and unchanged typed path keys.
        foreach (var file in program.SourceFiles())
        {
            file.SetFileNameToUnicode();
        }
        sourceFiles = program.SourceFiles();
        foreach (var pair in program.FilesByPath())
            filesByPath.Add(pair.Key, pair.Value);
        var libraries = new List<SourceFile>();
        foreach (var file in sourceFiles)
        {
            if (program.IsSourceFileDefaultLibrary(PathKey.FromValue(file.PathKeyValue())))
                libraries.Add(file);
            else
                users.Add(file);
        }
        LibraryFiles = libraries.ToArray();
        RootFileNames = (inspection.Parsed!.FileNames() ?? []).Select(path => path.ToUnicode()).ToArray();
        foreach (var root in inspection.Parsed.FileNames() ?? [])
            if (program.GetSourceFile(root) is { } file && !roots.Contains(file, ReferenceEqualityComparer.Instance))
                roots.Add(file);
        parseDiagnostics = parseDiagnostics.Append(program.GetSyntacticDiagnostics());
        programDiagnostics = programDiagnostics.Append(program.GetProgramDiagnostics());
        foreach (var file in sourceFiles)
            foreach (var import in file.Imports())
                if (program.GetResolvedModuleFromModuleSpecifier(file, import) is { } resolved)
                    edges.Add(new(file, import, ProjectDiskHost.DecodeModuleReference(import.TextValue()),
                        program.GetModeForUsageLocation(file, import), resolved, program.GetSourceFileForResolvedModule(resolved)));
    }

    private Project(ProjectConfiguration configuration)
    {
        this.configuration = configuration;
        disk = new(configuration.Root);
        var resolutionHost = new LegacyResolutionHost(disk);
        resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = resolutionHost, CompilerOptions = Options, ProjectNameValue = PathByteText.FromUnicodeValue(configuration.FileName) });
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
        sourceFiles = processed.files;
        foreach (var pair in processed.filesByPath)
            filesByPath.Add(pair.Key, pair.Value);
        programDiagnostics = programDiagnostics.Append(processed.processingDiagnostics);
        edges.AddRange(processed.resolutionEdges);
        foreach (SourceFile source in sourceFiles)
        {
            if (LibraryFiles.Contains(source, ReferenceEqualityComparer.Instance))
                continue;
            users.Add(source);
            if (source.Diagnostics()is { Count: > 0 } errors)
                parseDiagnostics = parseDiagnostics.Append(errors);
        }

        foreach (string name in rootNames)
            if (filesByPath.TryGetValue(ByteStringKey.FromString(name), out SourceFile? source) && !roots.Contains(source, ReferenceEqualityComparer.Instance))
                roots.Add(source);
    }

    internal static void RequireImplementationFile(string path) => RequireImplementationFile(ByteStringValue.FromString(path));
    internal static void RequireImplementationFile(ByteStringValue path)
    {
        if (!path.EndsWith(".ts"u8) || TsPath.IsDeclarationFileName(path))
        {
            string text = path.Materialize()!;
            throw new ProjectException("sourceFileForm", "Only user .ts implementations are supported: " + text, text);
        }
    }

    public IReadOnlyList<Diagnostic> Check()
    {
        if (genuineProgram is not null)
        {
            // compiler/program.go:GetDiagnosticsOfAnyProgram (2033). Binding is
            // performed early; its raw diagnostics are published only through
            // semantic policy (SkipTypeChecking and preceding directives).
            var diagnostics = GoSlices.Clip(genuineProgram.GetConfigFileParsingDiagnostics());
            int configDiagnosticCount = diagnostics.Count;
            var syntactic = genuineProgram.GetSyntacticDiagnostics();
            if (syntactic is { Count: > 0 })
                diagnostics = diagnostics.Append(genuineProgram.GetContentMapperDiagnostics());
            diagnostics = diagnostics.Append(syntactic);
            if (diagnostics.Count == configDiagnosticCount)
            {
                diagnostics = diagnostics.Append(genuineProgram.GetProgramDiagnostics());
                _ = genuineProgram.GetBindDiagnostics();
                if (Options.ListFilesOnly != Tristate.True)
                {
                    diagnostics = diagnostics.Append(genuineProgram.GetGlobalDiagnostics());
                    if (diagnostics.Count == configDiagnosticCount)
                    {
                        diagnostics = diagnostics.Append(genuineProgram.GetSemanticDiagnostics());
                        diagnostics = diagnostics.Append(genuineProgram.GetGlobalDiagnostics());
                    }
                }
            }
            // Declaration-output diagnostics remain outside this no-emit disk
            // adapter; this consumer does not acquire an emitter.
            return Program.SortAndDeduplicateDiagnostics(diagnostics).AsSpan().ToArray();
        }

        var all = GoSlices.Clone(programDiagnostics).Append(parseDiagnostics);
        // Malformed syntax has no semantic completeness promise or checker entry.
        if (parseDiagnostics.Count != 0 || users.Count == 0)
            return Program.SortAndDeduplicateDiagnostics(all).AsSpan().ToArray();
        Checker shared = Checker;
        foreach (SourceFile file in users)
            all = all.Append(Program.GetSemanticDiagnosticsWithChecker(file, Options, shared));
        all = all.Append(shared.GetGlobalDiagnostics());
        return Program.SortAndDeduplicateDiagnostics(all).AsSpan().ToArray();
    }

    public ResolvedModule? GetResolvedModule(SourceFile importingFile, string moduleReference, ResolutionMode mode)
    {
        ValidateOwned(importingFile);
        if (genuineProgram is not null)
            return genuineProgram.GetResolvedModule(importingFile, ByteStringValue.FromView(ByteStringView.FromOwnedBytes(System.Text.Encoding.UTF8.GetBytes(moduleReference))), mode);
        // Cached only: checker queries cannot discover different files after loading.
        foreach (ProjectResolutionEdge edge in edges)
            if (ReferenceEquals(edge.ImportingFile, importingFile) && edge.ModuleReference == moduleReference && edge.Mode == mode)
                return edge.Resolution;
        return null;
    }

    ResolvedModule? ICheckerProgramHost.GetResolvedModule(SourceFile importingFile, string moduleReference, ResolutionMode mode) => GetResolvedModuleValue(importingFile, ByteStringValue.FromString(moduleReference), mode);
    ResolvedModule? ICheckerProgramHost.GetResolvedModule(SourceFile importingFile, ByteStringValue moduleReference, ResolutionMode mode) => GetResolvedModuleValue(importingFile, moduleReference, mode);
    private ResolvedModule? GetResolvedModuleValue(SourceFile importingFile, ByteStringValue moduleReference, ResolutionMode mode)
    {
        ValidateOwned(importingFile);
        return genuineProgram is not null ? genuineProgram.GetResolvedModule(importingFile, moduleReference, mode)
            : GetResolvedModule(importingFile, ProjectDiskHost.DecodeModuleReference(moduleReference), mode);
    }
    string ICheckerProgramHost.GetSourceFileNameGoBytes(SourceFile file)
    {
        ValidateOwned(file);
        return file.FileNameGoBytes();
    }

    public SourceFile? GetSourceFileForResolvedModule(ResolvedModule module) => genuineProgram is not null
        ? genuineProgram.GetSourceFileForResolvedModule(module)
        : module.IsResolved() && filesByPath.TryGetValue(ByteStringKey.FromString(module.ResolvedPath.ToUnicode()), out var file) ? file : null;
    public ModuleKind GetEmitModuleFormatOfFile(SourceFile file)
    {
        ValidateOwned(file);
        return genuineProgram?.GetEmitModuleFormatOfFile(file) ?? Options.GetEmitModuleKind();
    }

    ModuleKind ICheckerProgramHost.GetImpliedNodeFormatForEmit(SourceFile file) => (ModuleKind)(int)GetImpliedNodeFormatForEmit(file);
    public ResolutionMode GetImpliedNodeFormatForEmit(SourceFile file)
    {
        ValidateOwned(file);
        return genuineProgram?.GetImpliedNodeFormatForEmit(file) ?? ResolutionMode.None;
    }

    public ResolutionMode GetEmitSyntaxForUsageLocation(SourceFile file, Node location)
    {
        ValidateOwned(file);
        return genuineProgram?.GetEmitSyntaxForUsageLocation(file, location) ?? ResolutionMode.ESNext;
    }

    public ResolutionMode GetModeForUsageLocation(SourceFile file, Node location)
    {
        ValidateOwned(file);
        return genuineProgram?.GetModeForUsageLocation(file, location) ?? ResolutionMode.ESNext;
    }

    public bool IsSourceFileDefaultLibrary(SourceFile file)
    {
        ValidateOwned(file);
        return genuineProgram?.IsSourceFileDefaultLibrary(PathKey.FromValue(file.PathKeyValue())) ?? false;
    }

    public ResolutionMode GetDefaultResolutionModeForFile(SourceFile file)
    {
        ValidateOwned(file);
        return genuineProgram?.GetDefaultResolutionModeForFile(file) ?? ResolutionMode.None;
    }

    private TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost ActualSpecifierHost => ActualHost;

    private readonly TypeScript.Symlinks.KnownSymlinks legacySpecifierSymlinks = TypeScript.Symlinks.KnownSymlinks.NewKnownSymlinks(CaseSensitivity.CaseSensitive);
    RootedDirectoryPath TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.BaseDirectory() => genuineProgram is null ? RootedDirectoryPath.FromUnicode(RootDirectory) : ActualSpecifierHost.BaseDirectory();
    TypeScript.Symlinks.KnownSymlinks TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetSymlinkCache() => genuineProgram?.GetSymlinkCache() ?? legacySpecifierSymlinks;
    RootedDirectoryPath TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetGlobalTypingsCacheLocation() => genuineProgram?.GetGlobalTypingsCacheLocation() ?? default;
    RootedDirectoryPath TypeScript.OutputPaths.IOutputPathsHost.CommonSourceDirectory() => genuineProgram?.CommonSourceDirectory() ?? RootedDirectoryPath.FromUnicode(RootDirectory);
    List<string>? TypeScript.OutputPaths.IOutputPathsHost.ContentMapperExtensions() => genuineProgram?.ContentMapperExtensions() ?? configuration.Parsed?.ContentMapperExtensions();
    CaseSensitivity TypeScript.OutputPaths.IOutputPathsHost.CaseSensitivity() => genuineProgram?.CaseSensitivity() ?? CaseSensitivity.CaseSensitive;
    SourceOutputAndProjectReference? TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetProjectReferenceFromSource(PathKey path) => genuineProgram?.GetProjectReferenceFromSource(path);
    Slice<RootedFilePath> TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetRedirectTargets(PathKey path) => genuineProgram?.GetRedirectTargets(path) ?? default;
    RootedFilePath TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetSourceOfProjectReferenceIfOutputIncluded(SourceFile file)
    {
        ValidateOwned(file);
        return genuineProgram is null ? RootedFilePath.FromValue(file.FileNameGoValue()) : ActualSpecifierHost.GetSourceOfProjectReferenceIfOutputIncluded(file);
    }

    bool TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.FileExists(RootedFilePath path) => genuineProgram is null ? disk!.ModuleSpecifierFileExists(path.ToUnicode()) : ActualSpecifierHost.FileExists(path);
    RootedDirectoryPath TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetNearestAncestorDirectoryWithPackageJson(RootedDirectoryPath directory)
    {
        if (genuineProgram is not null)
            return ActualSpecifierHost.GetNearestAncestorDirectoryWithPackageJson(directory);
        var scoped = resolver.GetPackageScopeForPath(directory);
        return scoped.Exists() ? scoped!.PackageDirectory.AsDirectoryPath() : default;
    }

    InfoCacheEntry? TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetPackageJsonInfo(RootedFilePath path)
    {
        if (genuineProgram is not null)
            return ActualSpecifierHost.GetPackageJsonInfo(path);
        var directory = path.Directory();
        var scoped = resolver.GetPackageScopeForPath(directory);
        return scoped.Exists() && scoped!.PackageDirectory.AsDirectoryPath() == directory ? scoped : null;
    }

    ResolvedModule? TypeScript.ModuleSpecifiers.IModuleSpecifierGenerationHost.GetResolvedModuleFromModuleSpecifier(SourceFile file, Node specifier) => genuineProgram is not null ? ActualSpecifierHost.GetResolvedModuleFromModuleSpecifier(file, specifier) : GetResolvedModuleValue(file, specifier.TextValue(), GetModeForUsageLocation(file, specifier));
    private void ValidateOwned(SourceFile sourceFile)
    {
        if (!filesByPath.TryGetValue(sourceFile.PathKeyKey(), out SourceFile? owned) || !ReferenceEquals(owned, sourceFile))
            throw new ArgumentException("SourceFile belongs to a different project.", nameof(sourceFile));
    }
}

public sealed record ProjectResolutionEdge(SourceFile ImportingFile, Node ModuleSpecifier, string ModuleReference, ResolutionMode Mode, ResolvedModule Resolution, SourceFile? TargetFile);

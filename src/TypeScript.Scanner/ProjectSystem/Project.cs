// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/project/project.go counterpart at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Globalization;
using System.Buffers;
using System.Text.Json;
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.Compiler;
using TypeScript.ContentMapper;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem.Logging;
using TypeScript.TsOptions;
using ConfigParser = TypeScript.TsOptions.TsOptions;
using Program = TypeScript.Compiler.Program;
using Diagnostic = TypeScript.Scanner.Diagnostic;

namespace TypeScript.ProjectSystem;
internal readonly struct ID(ByteStringValue value) : IEquatable<ID>, Logging.IGoLogStringer, Ata.IAtaProjectID, TypeScript.LanguageServices.AutoImport.IProjectID
{
    internal ByteStringValue Value { get; } = value;

    public bool Equals(ID other) => Value.EqualsValue(other.Value);
    public override bool Equals(object? other) => other is ID id && Equals(id);
    public override int GetHashCode() => ByteStringKey.FromValue(Value).GetHashCode();
    public static bool operator ==(ID left, ID right) => left.Equals(right);
    public static bool operator !=(ID left, ID right) => !left.Equals(right);
    public string GoLogString() => Value.Materialize()!;
    // project.go:61 Configured / 91 Inferred / 95 Synthetic.
    internal (ConfiguredProjectID ID, bool Ok) Configured()
    {
        var(path, ok) = TsPath.TryPathKeyFromCanonical(Value);
        return ok ? Projects.ParseConfiguredProjectID(path) : (default, false);
    }

    internal (InferredProjectID ID, bool Ok) Inferred() => (Projects.InferredProjectID, this == Projects.InferredProjectID.AsID());
    internal (SyntheticProjectID ID, bool Ok) Synthetic() => Projects.ParseSyntheticProjectID(Value);
}

internal readonly record struct ConfiguredProjectID(PathKey Key) : Logging.IGoLogStringer
{
    internal PathKey PathKey() => Key;
    internal ID AsID() => new(Key.AsValue());
    public string GoLogString() => Key.AsString();
}

internal readonly record struct InferredProjectID(ByteStringKey Value)
{
    internal ID AsID() => new(Value.Value);
}

internal readonly record struct SyntheticProjectID(ByteStringKey Value)
{
    internal ID AsID() => new(Value.Value);
    // project.go:99. JSON string decoding owns UTF8 bytes before validating the ID.
    internal static SyntheticProjectID UnmarshalJSONFrom(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("synthetic project ID must be a string");
        // Decode directly into the required retained UTF8 owner, as RpcJson does.
        // Multi-segment input is copied once for an unescaped string.
        ByteStringValue value;
        if (!reader.ValueIsEscaped)
            value = ByteStringValue.FromView(reader.HasValueSequence ? ByteStringView.FromOwnedBytes(reader.ValueSequence.ToArray()) : ByteStringView.CopyFrom(reader.ValueSpan));
        else
        {
            var bytes = new byte[checked((int)(reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length))];
            int written = reader.CopyString(bytes);
            value = ByteStringValue.FromView(ByteStringView.FromOwnedBytes(bytes)).Slice(0, written);
        }

        var(id, ok) = Projects.ParseSyntheticProjectID(value);
        return ok ? id : throw new JsonException("invalid synthetic project ID: " + value.Materialize());
    }
}

internal enum ProjectKind
{
    Inferred,
    Configured,
    Synthetic
}

internal enum ProgramUpdateKind
{
    None,
    Cloned,
    SameFileNames,
    NewFiles
}

internal enum PendingReload
{
    None,
    FileNames,
    Full
}

internal static partial class Projects
{
    internal static readonly InferredProjectID InferredProjectID = new(ByteStringKey.FromValue(ByteStringValue.FromString("/dev/null/inferred")));
    internal static readonly ByteStringValue SyntheticProjectPrefix = ByteStringValue.FromString("/dev/null/synthetic/");
    // project.go:48 NewSyntheticProjectID; Go int is signed64 on the verified platform.
    internal static SyntheticProjectID NewSyntheticProjectID(long id)
    {
        if (id <= 0)
            throw new InvalidOperationException("invalid synthetic project ID: " + id.ToString(CultureInfo.InvariantCulture));
        return new(ByteStringKey.FromValue(ByteStringValue.Concat(SyntheticProjectPrefix, ByteStringValue.FromString(id.ToString(CultureInfo.InvariantCulture)))));
    }

    // project.go:69/83 ParseConfiguredProjectID / ConfiguredProjectIDFromPathKey.
    internal static (ConfiguredProjectID ID, bool Ok) ParseConfiguredProjectID(PathKey value)
    {
        var id = new ID(value.AsValue());
        return value.AsValue().Length == 0 || id.Inferred().Ok || id.Synthetic().Ok ? (default, false) : (new(value), true);
    }

    internal static ConfiguredProjectID ConfiguredProjectIDFromPathKey(PathKey value)
    {
        var(id, ok) = ParseConfiguredProjectID(value);
        return ok ? id : throw new InvalidOperationException("invalid configured project ID: " + value.AsString());
    }

    // project.go:112 ParseSyntheticProjectID. AllowLeadingSign retains strconv.Atoi's + prefix and rejects whitespace.
    internal static (SyntheticProjectID ID, bool Ok) ParseSyntheticProjectID(ByteStringValue value)
    {
        if (!value.StartsWith(SyntheticProjectPrefix))
            return (default, false);
        var suffix = value.Slice(SyntheticProjectPrefix.Length);
        bool parsed = suffix.IsBytes ? long.TryParse(suffix.ByteSpan, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var id) : long.TryParse(suffix.CharSpan, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out id);
        return parsed && id > 0 ? (NewSyntheticProjectID(id), true) : (default, false);
    }

    // project_stringer_generated.go:20. Preserve unknown enum values in logging.
    internal static string KindString(ProjectKind kind) => kind switch
    {
        ProjectKind.Inferred => "Inferred",
        ProjectKind.Configured => "Configured",
        ProjectKind.Synthetic => "Synthetic",
        _ => "Kind(" + ((int)kind).ToString(CultureInfo.InvariantCulture) + ")"
    };
}

internal readonly record struct CreateProgramResult(Program Program, ProgramUpdateKind UpdateKind);
internal sealed class Project : TypeScript.LanguageServices.IProject, Dirty.ICloneable<Project>
{
    // project.go:153. Clone retains each pointer/header unless the original owner explicitly clones it.
    internal ProjectKind Kind;
    internal ID id;
    internal RootedDirectoryPath projectDirectory;
    internal RootedFilePath configFileName;
    internal PathKey configFilePath;
    internal bool dirty;
    internal PathKey dirtyFilePath;
    internal ProjectCompilerHost host = null!;
    internal ParsedCommandLine? CommandLine;
    internal ParsedCommandLine? commandLineWithTypingsFiles;
    private ProjectOnce commandLineWithTypingsFilesOnce = new();
    internal Program? Program;
    internal ProgramUpdateKind ProgramUpdateKind;
    internal ulong ProgramLastUpdate;
    internal HashSet<PathKey>? potentialProjectReferences;
    internal WatchedFiles<SyncMap<PathKey, RootedFilePath>?> programFilesWatch = null!;
    internal WatchedFiles<PatternsAndIgnored>? typingsWatch;
    internal WatchedFiles<Slice<RootedFilePath>> contentMapperWatch = null!;
    internal HashSet<PathKey>? contentMapperWatchedFiles;
    internal ProjectCheckerPool? checkerPool;
    internal IModuleResolverFactory? moduleResolverFactory;
    internal ulong moduleResolverID;
    internal Ata.TypingsInfo? installedTypingsInfo;
    internal Slice<RootedFilePath> typingsFiles;
    // project.go:195.
    internal static Project NewConfiguredProject(RootedFilePath name, PathKey path, ProjectCollectionBuilder builder, LogTree? logger)
    {
        var(id, ok) = Projects.ParseConfiguredProjectID(path);
        if (!ok)
            throw new InvalidOperationException(LogFormat.Sprintf("invalid configured project ID: %s", path));
        var project = NewProject(id.AsID(), ProjectKind.Configured, name.Directory(), builder, logger);
        project.configFileName = name;
        project.configFilePath = path;
        return project;
    }

    // project.go:211.
    internal static Project NewInferredProject(RootedDirectoryPath directory, CompilerOptions? options, List<RootedFilePath>? names, List<ProjectReference>? references, MapperHeaders mappers, ProjectCollectionBuilder builder, LogTree? logger)
    {
        var project = NewProject(Projects.InferredProjectID.AsID(), ProjectKind.Inferred, directory, builder, logger);
        options ??= new()
        {
            AllowJs = Tristate.True,
            Module = ModuleKind.ESNext,
            ModuleResolution = ModuleResolutionKind.Bundler,
            Target = ScriptTarget.LatestStandard,
            Jsx = JsxEmit.ReactJSX,
            AllowImportingTsExtensions = Tristate.True,
            StrictNullChecks = Tristate.True,
            StrictFunctionTypes = Tristate.True,
            SourceMap = Tristate.True,
            AllowNonTsExtensions = Tristate.True,
            ResolveJsonModule = Tristate.True
        };
        project.CommandLine = NewInferredProjectCommandLine(options, names, references, mappers, directory, builder.fs.FS().CaseSensitivity());
        return project;
    }

    // project.go:247.
    internal static Project NewSyntheticProject(SyntheticProjectID id, RootedDirectoryPath directory, CompilerOptions options, List<RootedFilePath>? names, List<ProjectReference>? references, MapperHeaders mappers, ProjectCollectionBuilder builder, LogTree? logger)
    {
        var project = NewProject(id.AsID(), ProjectKind.Synthetic, directory, builder, logger);
        project.CommandLine = NewInferredProjectCommandLine(options, names, references, mappers, directory, builder.fs.FS().CaseSensitivity());
        return project;
    }

    // project.go:269. The established command-line List boundary requires a detached
    // owner for an incoming Go slice header; legacy List callers retain their owner.
    internal static ParsedCommandLine NewInferredProjectCommandLine(CompilerOptions options, List<RootedFilePath>? names, List<ProjectReference>? references, MapperHeaders mappers, RootedDirectoryPath directory, CaseSensitivity sensitivity)
    {
        var commandLine = ConfigParser.NewParsedCommandLine(options, names, references, directory, sensitivity);
        commandLine.ParsedConfig.ContentMappers = mappers.AsCommandLineList();
        return commandLine;
    }

    // project.go:282.
    internal static Project NewProject(ID id, ProjectKind kind, RootedDirectoryPath directory, ProjectCollectionBuilder builder, LogTree? logger)
    {
        logger?.Log(LogFormat.Sprintf("Creating %sProject: %s, currentDirectory: %s", Projects.KindString(kind), id, directory));
        var project = new Project
        {
            Kind = kind,
            id = id,
            projectDirectory = directory,
            dirty = true
        };
        bool relative = ProtocolContexts.GetClientCapabilities(builder.ctx).Workspace.DidChangeWatchedFiles.RelativePatternSupport;
        project.programFilesWatch = new(ByteStringValue.Concat(ByteStringValue.FromString("program files for "), id.Value), WatchKind.Create | WatchKind.Change | WatchKind.Delete, relative, ProjectWatches.CreateResolutionLookupGlobMapper(builder.sessionOptions.CurrentDirectory, builder.sessionOptions.DefaultLibraryPath, directory, builder.fs.FS().CaseSensitivity()));
        if (builder.sessionOptions.TypingsLocation.AsValue().Length != 0)
            project.typingsWatch = new(ByteStringValue.FromString("typings installer files"), WatchKind.Create | WatchKind.Change | WatchKind.Delete, relative, static input => input);
        project.contentMapperWatch = ProjectWatches.NewWatchedFilesForPaths(ByteStringValue.Concat(ByteStringValue.FromString("content mapper configuration files for "), id.Value), WatchKind.Create | WatchKind.Change | WatchKind.Delete, relative, builder.sessionOptions.CurrentDirectory, builder.fs.FS().CaseSensitivity());
        return project;
    }

    // project.go:328/336.
    internal RootedDirectoryPath CurrentDirectory() => projectDirectory;
    internal ByteStringValue DisplayName(RootedDirectoryPath cwd)
    {
        if (Kind == ProjectKind.Inferred)
            return projectDirectory.BaseNameValue();
        var name = Kind == ProjectKind.Configured ? ConfigFileName() : RootedFilePath.FromValue(ID().Value);
        var(relative, ok) = CaseSensitivity.CaseInsensitive.RelativePathFromDirectory(cwd, name);
        return ok ? relative.AsValue() : name.AsValue();
    }

    // project.go:350/355/363/370/374/378.
    internal ID ID() => id;
    internal RootedFilePath ConfigFileName() => Kind == ProjectKind.Configured ? configFileName : throw new InvalidOperationException("ConfigFileName called on non-configured project");
    internal PathKey ConfigFilePath() => Kind == ProjectKind.Configured ? configFilePath : throw new InvalidOperationException("ConfigFilePath called on non-configured project");
    public ByteStringValue Id() => id.Value;
    public Program? GetProgram() => Program;
    internal bool IsDirty() => dirty;
    // project.go:385.
    internal Slice<Diagnostic> GetProjectDiagnostics(ProtocolRequestContext context)
    {
        Slice<Diagnostic> combined = default;
        combined = combined.Append(Program!.GetConfigFileParsingDiagnostics());
        combined = combined.Append(Program.GetProgramDiagnostics());
        combined = combined.Append(checkerPool?.GetGlobalDiagnostics() ?? default);
        return TypeScript.Compiler.Program.SortAndDeduplicateDiagnostics(combined);
    }

    // project.go:397/401/405.
    public bool HasFile(RootedFilePath name) => ContainsFile(host.FS().CaseSensitivity().PathKey(name.AsPath()));
    internal bool ContainsFile(PathKey path) => Program?.GetSourceFileByPath(path) != null;
    internal bool IsSourceFromProjectReference(PathKey path) => Program?.IsSourceFromProjectReference(path) == true;
    // project.go:409. A new Once belongs to the clone, while its memoized result pointer is retained.
    public Project Clone() => new()
    {
        Kind = Kind,
        id = id,
        projectDirectory = projectDirectory,
        configFileName = configFileName,
        configFilePath = configFilePath,
        dirty = dirty,
        dirtyFilePath = dirtyFilePath,
        host = host,
        CommandLine = CommandLine,
        commandLineWithTypingsFiles = commandLineWithTypingsFiles,
        Program = Program,
        ProgramUpdateKind = ProgramUpdateKind.None,
        ProgramLastUpdate = ProgramLastUpdate,
        potentialProjectReferences = potentialProjectReferences,
        programFilesWatch = programFilesWatch,
        typingsWatch = typingsWatch,
        contentMapperWatch = contentMapperWatch,
        contentMapperWatchedFiles = contentMapperWatchedFiles,
        checkerPool = checkerPool,
        moduleResolverFactory = moduleResolverFactory,
        moduleResolverID = moduleResolverID,
        installedTypingsInfo = installedTypingsInfo,
        typingsFiles = typingsFiles
    };
    // project.go:450.
    internal void SetCommandLine(ParsedCommandLine commandLine)
    {
        CommandLine = commandLine;
        commandLineWithTypingsFiles = null;
        commandLineWithTypingsFilesOnce = new();
        potentialProjectReferences = null;
        dirty = true;
        dirtyFilePath = default;
    }

    // project.go:460.
    internal ParsedCommandLine GetCommandLineWithTypingsFiles()
    {
        if (typingsFiles.Count == 0 || GetTypeAcquisition()?.Enable != Tristate.True)
            return CommandLine!;
        commandLineWithTypingsFilesOnce.Do(() =>
        {
            if (commandLineWithTypingsFiles == null)
            {
                var original = CommandLine!.FileNames();
                var names = new List<RootedFilePath>((original?.Count ?? 0) + typingsFiles.Count);
                if (original != null)
                    names.AddRange(original);
                foreach (var name in typingsFiles)
                    names.Add(name);
                commandLineWithTypingsFiles = CommandLine.WithFileNames(names);
            }
        });
        return commandLineWithTypingsFiles!;
    }

    // project.go:485/494.
    internal void SetPotentialProjectReference(PathKey path)
    {
        potentialProjectReferences = potentialProjectReferences == null ? new() : new(potentialProjectReferences, potentialProjectReferences.Comparer);
        potentialProjectReferences.Add(path);
    }

    internal bool HasPotentialProjectReference(ProjectTreeRequest request)
    {
        if (CommandLine != null)
        {
            foreach (var path in CommandLine.ResolvedProjectReferencePaths() ?? [])
                if (request.IsProjectReferenced(host.FS().CaseSensitivity().PathKey(path.AsPath())))
                    return true;
        }
        else if (potentialProjectReferences != null)
            foreach (var path in potentialProjectReferences)
                if (request.IsProjectReferenced(path))
                    return true;
        return false;
    }

    // project.go:516. Resolver cleanup runs on every exit, including an UpdateProgram failure.
    internal CreateProgramResult CreateProgram()
    {
        var updateKind = ProgramUpdateKind.NewFiles;
        bool programCloned = false;
        Program newProgram;
        ICheckerPool CreatePool(Program program) => new ProjectCheckerPool(host.sessionOptions.CheckerPoolOptions, program, Log);
        Action? cleanup = null;
        TypeScript.Modules.IResolver CreateResolver(TypeScript.Modules.ResolverOptions options)
        {
            if (moduleResolverFactory == null)
                return TypeScript.Modules.DefaultResolver.NewResolver(options);
            var(resolver, done) = moduleResolverFactory.NewResolver(host.builder!.ctx, options);
            cleanup = done;
            return resolver;
        }

        try
        {
            var commandLine = GetCommandLineWithTypingsFiles();
            if (dirtyFilePath.AsValue().Length != 0 && Program != null && ReferenceEquals(Program.CommandLine(), commandLine))
            {
                SourceFile? dirtyFile;
                (newProgram, dirtyFile, programCloned) = Program.UpdateProgram(dirtyFilePath, host, CreatePool, CreateResolver);
                if (programCloned)
                {
                    updateKind = ProgramUpdateKind.Cloned;
                    foreach (var file in newProgram.SourceFiles())
                        if (!ReferenceEquals(file, dirtyFile) && !file.IsContentMapperFailureStub() && !file.IsContentMapperSupplemental())
                        {
                            if (file.ContentMapper().Length != 0)
                                host.builder!.contentMappedParseCache.Ref(ParseCaches.ContentMappedKeyForFile(file));
                            else
                                host.builder!.parseCache.Ref(ParseCaches.KeyForFile(file));
                        }

                    foreach (var file in newProgram.DuplicateSourceFiles())
                        if (!file.IsContentMapperFailureStub)
                        {
                            if (file.ContentMapper.Length != 0)
                                host.builder!.contentMappedParseCache.Ref(ParseCaches.ContentMappedKeyForDuplicate(file));
                            else
                                host.builder!.parseCache.Ref(ParseCaches.KeyForDuplicate(file));
                        }
                }
                else if (dirtyFile != null)
                {
                    if (dirtyFile.ContentMapper().Length != 0)
                        host.builder!.contentMappedParseCache.Deref(ParseCaches.ContentMappedKeyForFile(dirtyFile));
                    else
                        host.builder!.parseCache.Deref(ParseCaches.KeyForFile(dirtyFile));
                }
            }
            else
                newProgram = TypeScript.Compiler.Program.NewProgram(new() { Host = host, Config = commandLine, UseSourceOfProjectReference = true, TypingsLocation = GetTypeAcquisition()!.Enable == Tristate.True ? host.sessionOptions.TypingsLocation : default, CreateCheckerPool = CreatePool, CreateModuleResolver = CreateResolver });
            if (!programCloned && Program != null && Program.HasSameFileNames(newProgram))
                updateKind = ProgramUpdateKind.SameFileNames;
            newProgram.BindSourceFiles();
            return new(newProgram, updateKind);
        }
        finally
        {
            cleanup?.Invoke();
        }
    }

    // project.go:606/610. log is an unchanged pinned upstream TODO.
    internal WatchedFiles<SyncMap<PathKey, RootedFilePath>?> CloneWatchers() => programFilesWatch.Clone(host.sourceFS.seenFiles);
    internal void Log(string message)
    {
    }

    // project.go:614. File-explanation logging is also an unchanged upstream TODO.
    internal string Print(bool writeNames, bool writeExplanation, GoBuilder builder)
    {
        builder.WriteString(LogFormat.Sprintf("\nProject '%s'\n", ID()));
        if (Program == null)
            builder.WriteString("\tFiles (0) NoProgram\n");
        else
        {
            var files = Program.GetSourceFiles();
            builder.WriteString(LogFormat.Sprintf("\tFiles (%d)\n", files.Count));
            if (writeNames)
                foreach (var file in files)
                {
                    builder.WriteString("\t\t");
                    builder.WriteString(file.FileNameGoValue());
                    builder.WriteString("\n");
                }
        }

        builder.WriteString("-----------------------------------------------");
        return builder.String();
    }

    // project.go:636/655/664/681.
    internal TypeAcquisition? GetTypeAcquisition() => Kind == ProjectKind.Inferred || Kind == ProjectKind.Synthetic ? new()
    {
        Enable = Tristate.True,
        DisableFilenameBasedTypeAcquisition = Tristate.False
    }

    : CommandLine?.TypeAcquisition();
    internal HashSet<ByteStringKey>? GetUnresolvedImports() => Program?.GetUnresolvedImports();
    internal bool ShouldTriggerATA(ulong snapshotID)
    {
        if (Program == null || CommandLine == null || GetTypeAcquisition()?.Enable != Tristate.True)
            return false;
        return installedTypingsInfo == null || ProgramLastUpdate == snapshotID && ProgramUpdateKind == ProgramUpdateKind.NewFiles || !installedTypingsInfo.Equals(ComputeTypingsInfo());
    }

    internal Ata.TypingsInfo ComputeTypingsInfo() => new()
    {
        CompilerOptions = CommandLine!.CompilerOptions(),
        TypeAcquisition = GetTypeAcquisition(),
        UnresolvedImports = GetUnresolvedImports()
    };
}

// sync.Once lifetime adapter shared by project memoization owners. No reset of a live owner.
internal sealed class ProjectOnce
{
    private int done;
    private readonly SemaphoreSlim gate = new(1, 1);
    internal void Do(Action action)
    {
        if (Volatile.Read(ref done) != 0)
            return;
        gate.Wait();
        try
        {
            if (done != 0)
                return;
            try
            {
                action();
            }
            finally
            {
                Volatile.Write(ref done, 1);
            }
        }
        finally
        {
            gate.Release();
        }
    }
}

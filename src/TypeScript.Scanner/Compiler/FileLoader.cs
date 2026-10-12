// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of the selected Program/graph loading responsibilities in
// tsc/internal/compiler/fileloader.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Original Program root/library/type/reference/mapper stages share the actual
// scheduler; the named graph and legacy adapters retain their prior boundaries.
using TypeScript.Checking;
using TypeScript.ContentMapper;
using TypeScript.SpanMap;
using Kind = TypeScript.Scanner.Kind;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Scanner;
using TypeScript.Vfs;
using ConfigParser = TypeScript.TsOptions.TsOptions;

namespace TypeScript.Compiler;
internal sealed class LibResolution
{
    internal ByteStringValue libraryName;
    internal ResolvedModule? resolution;
    internal Slice<DiagAndArgs> trace;
}
internal sealed class FileLoader
{
    private readonly object contentMapperGate = new();
    private readonly Dictionary<Mapper, int> contentMapperFailures = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Mapper> contentMapperInitFailed = new(ReferenceEqualityComparer.Instance);
    private Slice<Diagnostic> contentMapperDiagnostics;
    internal Slice<Diagnostic> ContentMapperDiagnostics() => contentMapperDiagnostics;
    private SourceFile? ParseContentMappedFile(SourceFileParseOptions parseOptions)
    {
        var mapper = opts.Config.GetContentMapperForFileName(TypedFile(parseOptions.FileNameValue))!;
        string label = mapper.DiagnosticName(), transformIdentity = GetContentMapperTransformIdentity(mapper);
        if (ContentMapperUnavailable(mapper)) return EmptyContentMappedFile(parseOptions, mapper.Identity(), transformIdentity);
        var (files, error) = compilerHost!.GetContentMappedSourceFiles(parseOptions, mapper);
        if (error is null) return files.Canonical;
        var source = EmptyContentMappedFile(parseOptions, mapper.Identity(), transformIdentity);
        var transformError = FindError<TransformError>(error);
        if (transformError?.Kind == TransformErrorKind.Initialize)
        {
            RecordContentMapperInitializationFailure(mapper, label, transformError);
            return source;
        }
        if (RecordContentMapperFailure(mapper, label))
        {
            var problem = FindError<MappingError>(error);
            var diagnostic = problem is not null ? ContentMapperMappingDiagnostic(source, label, problem) : ContentMapperTransformDiagnostic(source, label, error);
            var diagnostics = source.Diagnostics();
            diagnostics = diagnostics.Append(diagnostic);
            source.SetDiagnostics(diagnostics);
        }
        return source;
    }
    private static T? FindError<T>(Exception? error) where T : Exception
    {
        for (; error is not null; error = error.InnerException) if (error is T match) return match;
        return null;
    }
    private string GetContentMapperTransformIdentity(Mapper mapper)
    {
        if (compilerHost!.ContentMapperProject() is { } project)
        {
            var (identity, error) = project.Identity(mapper);
            if (error is null) return identity;
        }
        return mapper.TransformIdentityHex(options);
    }
    private SourceFile EmptyContentMappedFile(SourceFileParseOptions parseOptions, string mapperIdentity, string transformIdentity)
    {
        var (content, _) = compilerHost!.FS().ReadFile(TypedFile(parseOptions.FileNameValue));
        var source = Parser.ParseSourceFile(parseOptions, Array.Empty<byte>(), ScriptKind.TS);
        source.SetContentMapperInfo(new() { ContentMapper = mapperIdentity, TransformIdentity = transformIdentity, ParseOptions = parseOptions, VirtualFileName = TypedFile(parseOptions.FileNameValue).AppendSuffix(TsPath.ExtensionTs), OriginalText = content });
        return source;
    }
    internal static Message ContentMapperProjectErrorDiagnostic(Exception error) => FindError<ProjectError>(error)?.Kind switch
    {
        ProjectErrorKind.MalformedResponse => Diagnostics.The_content_mapper_returned_a_project_response_that_could_not_be_decoded,
        ProjectErrorKind.MissingConfigIdentity => Diagnostics.The_content_mapper_did_not_return_configIdentity_which_is_required_when_the_content_mapper_has_dynamicConfig_Colon_true_in_its_package_json,
        ProjectErrorKind.NonAbsoluteWatchedFile => Diagnostics.The_content_mapper_returned_a_non_absolute_path_in_watchedFiles,
        ProjectErrorKind.UnexpectedConfigIdentity => Diagnostics.The_content_mapper_returned_configIdentity_which_is_only_allowed_when_it_declares_dynamicConfig_Colon_true_in_its_package_json,
        ProjectErrorKind.UnexpectedWatchedFiles => Diagnostics.The_content_mapper_returned_watchedFiles_which_is_only_allowed_when_it_declares_dynamicConfig_Colon_true_in_its_package_json,
        _ => Diagnostics.The_content_mapper_process_failed_while_handling_the_project_request
    };
    private static Diagnostic ContentMapperTransformDiagnosticChain(SourceFile file, string label, Message message, params object[] args) => ContentMapperTransformDiagnosticWithDetail(file, label, Ast.NewCompilerDiagnostic(message, args));
    private static Diagnostic ContentMapperTransformDiagnosticWithDetail(SourceFile file, string label, Diagnostic detail) => Ast.NewDiagnostic(file, new(0, 0), Diagnostics.The_content_mapper_0_failed_to_transform_this_file, label).AddMessageChain(detail);
    private static Diagnostic ContentMapperTransformDiagnostic(SourceFile file, string label, Exception error)
    {
        if (FindError<SupplementalFileCollisionError>(error) is { } collision)
            return ContentMapperTransformDiagnosticChain(file, label, Diagnostics.Content_mapper_supplemental_output_file_0_conflicts_with_an_existing_file, collision.FileName.AsString());
        if (FindError<TransformError>(error) is { } transform)
        {
            switch (transform.Kind)
            {
                case TransformErrorKind.Initialize:
                    if (FindError<InitializeError>(transform) is { } initialize)
                    {
                        switch (initialize.Kind)
                        {
                            case InitializeErrorKind.PositionEncoding: return ContentMapperTransformDiagnosticChain(file, label, Diagnostics.The_content_mapper_selected_unsupported_position_encoding_0, initialize.PositionEncoding.AsString());
                            case InitializeErrorKind.EmptyDiagnosticSource: return ContentMapperTransformDiagnosticChain(file, label, Diagnostics.The_content_mapper_diagnostic_source_must_not_be_empty);
                            case InitializeErrorKind.ReservedDiagnosticSource: return ContentMapperTransformDiagnosticChain(file, label, Diagnostics.The_content_mapper_diagnostic_source_0_is_reserved_by_TypeScript, initialize.DiagnosticSource);
                        }
                    }
                    return ContentMapperTransformDiagnosticChain(file, label, Diagnostics.The_content_mapper_process_could_not_be_started_or_initialized);
                case TransformErrorKind.Project: return ContentMapperTransformDiagnosticChain(file, label, ContentMapperProjectErrorDiagnostic(transform));
                case TransformErrorKind.Request: return ContentMapperTransformDiagnosticChain(file, label, Diagnostics.The_content_mapper_process_failed_while_handling_the_transform_request);
                case TransformErrorKind.Response:
                    if (FindError<InvalidVirtualExtensionError>(transform) is { } extension)
                        return ContentMapperTransformDiagnosticChain(file, label, Diagnostics.The_content_mapper_returned_an_output_with_unsupported_virtual_extension_0, extension.Extension);
                    if (FindError<DiagnosticDirectiveError>(transform) is { } directive)
                    {
                        Diagnostic? detail = directive.Kind switch
                        {
                            DiagnosticDirectiveErrorKind.InvalidRange => Ast.NewCompilerDiagnostic(Diagnostics.Diagnostic_directive_0_returned_by_the_content_mapper_has_an_invalid_range, directive.Index),
                            DiagnosticDirectiveErrorKind.InvalidPolicy => Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_returned_a_diagnostic_directive_with_invalid_policy_0, (int)directive.Policy),
                            DiagnosticDirectiveErrorKind.ExpectMissingUnusedDiagnostic => Ast.NewCompilerDiagnostic(Diagnostics.Diagnostic_directive_0_returned_by_the_content_mapper_must_specify_unusedExpectDirectiveIndex_when_there_is_not_exactly_one_unusedExpectDirectiveDiagnostics_entry, directive.Index),
                            DiagnosticDirectiveErrorKind.InvalidUnusedDiagnosticIndex => Ast.NewCompilerDiagnostic(Diagnostics.Diagnostic_directive_0_returned_by_the_content_mapper_has_an_invalid_unusedExpectDirectiveIndex, directive.Index),
                            DiagnosticDirectiveErrorKind.Overlap => Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_returned_diagnostic_directives_with_overlapping_virtual_ranges),
                            _ => null
                        };
                        if (detail is not null)
                        {
                            if (directive.SupplementalIndex >= 0) detail = Ast.NewDiagnosticChain(detail, Diagnostics.The_invalid_diagnostic_directive_is_in_supplemental_output_0_returned_by_the_content_mapper, directive.SupplementalIndex);
                            return ContentMapperTransformDiagnosticWithDetail(file, label, detail);
                        }
                    }
                    return ContentMapperTransformDiagnosticChain(file, label, Diagnostics.The_content_mapper_returned_an_invalid_transform_response);
                case TransformErrorKind.Mappings: return Ast.NewDiagnostic(file, new(0, 0), Diagnostics.The_content_mapper_0_did_not_provide_the_required_position_mappings, label);
            }
        }
        return Ast.NewDiagnostic(file, new(0, 0), Diagnostics.The_content_mapper_0_failed_to_transform_this_file, label);
    }
    private static Diagnostic ContentMapperMappingDiagnostic(SourceFile file, string label, MappingError problem) => problem.Kind switch
    {
        MappingErrorKind.Overlap => Ast.NewDiagnostic(file, new(0, 0), Diagnostics.The_content_mapper_0_produced_overlapping_or_out_of_order_position_mappings_near_virtual_offset_1, label, problem.VirtualPos),
        MappingErrorKind.OutOfBounds => Ast.NewDiagnostic(file, new(0, 0), Diagnostics.The_content_mapper_0_produced_a_position_mapping_that_points_outside_the_original_content_original_offset_1, label, problem.OriginalPos),
        MappingErrorKind.VerbatimMismatch => Ast.NewDiagnostic(file, new(0, 0), Diagnostics.The_content_mapper_0_produced_a_verbatim_mapping_that_does_not_match_the_original_content_virtual_offset_1_original_offset_2, label, problem.VirtualPos, problem.OriginalPos),
        MappingErrorKind.Kind => Ast.NewDiagnostic(file, new(0, 0), Diagnostics.The_content_mapper_0_produced_a_position_mapping_with_an_invalid_kind_near_virtual_offset_1, label, problem.VirtualPos),
        MappingErrorKind.Feature => Ast.NewDiagnostic(file, new(0, 0), Diagnostics.The_content_mapper_0_produced_invalid_mapping_features_near_original_offset_1, label, problem.OriginalPos),
        _ => Ast.NewDiagnostic(file, new(0, 0), Diagnostics.The_content_mapper_0_did_not_provide_the_required_position_mappings, label)
    };
    internal static Diagnostic ContentMapperInitializationDiagnostic(string label, Exception error)
    {
        var initialize = FindError<InitializeError>(error);
        if (initialize is not null && label == "") label = initialize.MapperName;
        var diagnostic = Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_0_could_not_be_initialized, label);
        Diagnostic detail = initialize?.Kind switch
        {
            InitializeErrorKind.ProcessStart => Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_command_0_could_not_be_started_Colon_1, initialize.Command, initialize.Detail),
            InitializeErrorKind.ProcessExit => Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_process_exited_before_responding_to_the_initialize_request_exit_code_0, initialize.ExitCode),
            InitializeErrorKind.NoResponse => Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_did_not_respond_to_the_initialize_request_within_0_seconds, initialize.TimeoutSeconds),
            InitializeErrorKind.InvalidResponse => Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_returned_an_initialize_response_that_could_not_be_decoded_Colon_0, initialize.Detail),
            InitializeErrorKind.Request => Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_s_initialize_request_failed_Colon_0, initialize.Detail),
            InitializeErrorKind.PositionEncoding => Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_selected_unsupported_position_encoding_0, initialize.PositionEncoding.AsString()),
            InitializeErrorKind.EmptyDiagnosticSource => Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_diagnostic_source_must_not_be_empty),
            InitializeErrorKind.ReservedDiagnosticSource => Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_diagnostic_source_0_is_reserved_by_TypeScript, initialize.DiagnosticSource),
            _ => Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_process_could_not_be_started_or_initialized)
        };
        return diagnostic.AddMessageChain(detail);
    }
    internal static Diagnostic ContentMapperProjectDiagnostic(Exception error) => FindError<InitializeError>(error) is not null ? ContentMapperInitializationDiagnostic("", error) : Ast.NewCompilerDiagnostic(ContentMapperProjectErrorDiagnostic(error));
    private bool ContentMapperUnavailable(Mapper? mapper)
    {
        if (mapper is null) return false;
        lock (contentMapperGate) return contentMapperInitFailed.Contains(mapper) || contentMapperFailures.GetValueOrDefault(mapper) >= 5;
    }
    private void RecordContentMapperInitializationFailure(Mapper mapper, string label, Exception error)
    {
        lock (contentMapperGate)
        {
            if (!contentMapperInitFailed.Add(mapper)) return;
            contentMapperDiagnostics = contentMapperDiagnostics.Append(ContentMapperInitializationDiagnostic(label, error));
        }
    }
    private bool RecordContentMapperFailure(Mapper mapper, string label)
    {
        lock (contentMapperGate)
        {
            int failures = contentMapperFailures.GetValueOrDefault(mapper);
            if (failures >= 5) return false;
            contentMapperFailures[mapper] = ++failures;
            if (failures >= 5) contentMapperDiagnostics = contentMapperDiagnostics.Append(Ast.NewCompilerDiagnostic(Diagnostics.The_content_mapper_0_failed_1_times_and_will_not_be_used, label, 5));
            return true;
        }
    }
    internal readonly bool program;
    internal ProgramConfig opts;
    internal ICompilerHost? compilerHost;
    internal TypeScript.Tracing.Tracing? tracing;
    internal ProjectReferenceFileMapperBuilder? projectReferences;
    internal RootedDirectoryPath defaultLibraryPath;
    private List<string>? contentMapperExtensions;
    internal readonly TypeScript.Collections.SyncMap<ByteStringKey, LibFile> pathForLibFileCache = new();
    internal readonly TypeScript.Collections.SyncMap<PathKey, LibResolution> pathForLibFileResolutions = new();
    private readonly object resolutionErrorGate = new();
    internal Exception? moduleResolutionError;
    internal PathKey ToPath(RootedPath file) => host.FS().CaseSensitivity().PathKey(file);
    private FileLoader(ProgramOptions options, bool singleThreaded)
    {
        program = graph = true;
        opts = options.ProgramConfig;
        compilerHost = options.Host;
        tracing = options.Tracing;
        this.options = options.Config.CompilerOptions();
        defaultLibraryPath = compilerHost.DefaultLibraryPath();
        host = new CompilerResolutionHost(compilerHost, options.Config.BaseDirectory());
        includeProcessor = new(new ProjectConfiguration(options.Config), true);
        embeddedFiles = new Dictionary<ByteStringKey, SourceFile>(ByteStringKey.Comparer);
        contentMapperExtensions = options.Config.ContentMapperExtensions();
        supportedExtensions = ConfigParser.GetSupportedExtensions(this.options, contentMapperExtensions);
        supportedExtensionsWithJson = ConfigParser.GetSupportedExtensionsWithJsonIfResolveJsonModule(this.options, supportedExtensions);
        filesParser = new(singleThreaded);
        filesParser.maxDepth = this.options.MaxNodeModuleJsDepth?.Value ?? 0;
        AddProjectReferenceTasks(singleThreaded);
        host = projectReferences!.host;
        var resolverOptions = new ResolverOptions { Host = host, CompilerOptions = this.options, TypingsLocation = options.TypingsLocation, ProjectName = options.ProjectName, ExtraExtensions = contentMapperExtensions };
        resolver = options.CreateModuleResolver is not null ? options.CreateModuleResolver(resolverOptions) : DefaultResolver.NewResolver(resolverOptions);
    }
    internal static (ProcessedFiles Files, ResolutionData ResolutionData, Exception? Error) ProcessAllProgramFiles(ProgramOptions options, bool singleThreaded)
    {
        var loader = new FileLoader(options, singleThreaded);
        var roots = options.Config.FileNames();
        Action? done = options.Tracing?.Push(TypeScript.Tracing.Phase.Program, "processRootFiles", new() { ["count"] = roots?.Count ?? 0 }, false);
        try
        {
            for (int i = 0; i < (roots?.Count ?? 0); i++) loader.AddRootFileTask(ByteStringKey.FromValue(roots![i].AsValue()), ByteStringValue.FromString(options.Config.RootFileNameForDiagnostic(i)), new() { rootIndex = i });
            if (roots is { Count: > 0 } && loader.options.NoLib != Tristate.True)
            {
                if (loader.options.Lib is null)
                {
                    var lib = loader.PathForLibFile(ConfigParser.GetDefaultLibFileName(loader.options));
                    loader.AddRootTask(lib.path, lib, new() { isLibFile = true, isDefaultLib = true });
                }
                else for (int i = 0; i < loader.options.Lib.Count; i++)
                {
                    var (name, ok) = ConfigParser.GetLibFileName(loader.options.Lib[i]);
                    if (ok)
                    {
                        var lib = loader.PathForLibFile(name);
                        loader.AddRootTask(lib.path, lib, new() { isLibFile = true, libIndex = i });
                    }
                    // Unknown library names are diagnosed by configuration parsing,
                    // matching the original loader's TODO/no additional diagnostic.
                }
            }
            if (roots is { Count: > 0 } && !options.SkipModuleResolution) loader.AddAutomaticTypeDirectiveTasks();
            loader.filesParser.Parse(loader, loader.rootTasks);
            return (loader.filesParser.GetProcessedFiles(loader), loader.resolver.GetResolutionData(), loader.moduleResolutionError);
        }
        finally { done?.Invoke(); }
    }
    private void AddRootTask(RootedFilePath file, LibFile lib, FileIncludeReason reason)
    {
        if (options.AllowNonTsExtensions == Tristate.True || file.HasExtension())
            rootTasks = rootTasks.Append(new ParseTask() { normalizedFilePath = FileValue(file), path = KeyValue(ToPath(file.AsPath())), libFile = lib, includeReason = reason });
    }
    private void AddAutomaticTypeDirectiveTasks()
    {
        var file = opts.Config.BaseDirectory().ResolveFile(ModuleUtilities.InferredTypesContainingFile);
        rootTasks = rootTasks.Append(new ParseTask() { normalizedFilePath = FileValue(file), path = KeyValue(ToPath(file.AsPath())), isForAutomaticTypeDirective = true });
    }
    internal void ResolveAutomaticTypeDirectives(ParseTask task)
    {
        var names = DefaultResolver.GetAutomaticTypeDirectiveNameValues(options, opts.Config.BaseDirectory(), compilerHost!.FS());
        if (names.Length == 0) return;
        task.typeResolutionsInFile = new();
        foreach (var name in names)
        {
            var (resolved, trace) = ResolverText.ResolveTypeReferenceDirective(resolver, name.Value, TypedFile(task.normalizedFilePath), ResolutionMode.None);
            task.typeResolutionsInFile![new(name.Value, ResolutionMode.None)] = resolved;
            task.typeResolutionsTrace = task.typeResolutionsTrace.Append(trace);
            var reason = new FileIncludeReason { automaticTypeDirective = name.Value, packageId = resolved?.PackageId ?? default };
            if (resolved!.IsResolved()) task.AddSubTask(FileValue(resolved.ResolvedFileName), KeyValue(resolved.ResolvedPath), reason, resolved.IsExternalLibraryImport, false, resolved!.PackageId);
            else task.processingDiagnostics = task.processingDiagnostics.Append(new ProcessingDiagnostic() { diagnosticReason = reason, message = Diagnostics.Cannot_find_type_definition_file_for_0, args = [name.ToString()] });
        }
    }
    private void AddProjectReferenceTasks(bool singleThreaded)
    {
        projectReferences = new() { config = opts.Config, useSourceOfProjectReference = opts.canUseProjectReferenceSource(), host = host };
        var references = opts.Config.ResolvedProjectReferencePaths();
        if (references is { Count: > 0 }) new ProjectReferenceParser(this, singleThreaded).Parse(ProjectReferenceParser.CreateProjectReferenceParseTasks(references));
    }
    internal int totalFileCount, libFileCount;
    internal void SortLibs(Slice<SourceFile> files) => GoSlices.SortFunc(files, (a, b) => GetDefaultLibFilePriority(a).CompareTo(GetDefaultLibFilePriority(b)));
    private int GetDefaultLibFilePriority(SourceFile file)
    {
        var (relative, ok) = RootedFilePath.FromValue(file.FileNameGoValue()).RelativeTo(defaultLibraryPath);
        if (ok && relative != default)
        {
            var basename = relative.BaseNameValue();
            if (basename.EqualsBytes("lib.d.ts"u8) || basename.EqualsBytes("lib.es6.d.ts"u8)) return 0;
            var name = basename.StartsWith("lib."u8) ? basename.Slice(4) : basename;
            if (name.EndsWith(".d.ts"u8)) name = name.Slice(0, name.Length - 5);
            int index = ConfigParser.Libs.FindIndex(lib => name.EqualsString(lib));
            if (index != -1) return index + 1;
        }
        return ConfigParser.Libs.Count + 2;
    }
    internal LibFile PathForLibFile(string name) => PathForLibFile(ByteStringValue.FromString(name));
    internal LibFile PathForLibFile(ByteStringValue name)
    {
        var nameKey = ByteStringKey.FromValue(name);
        var (cached, ok) = pathForLibFileCache.Load(nameKey);
        if (ok) return cached;
        var path = defaultLibraryPath.ResolveFile(name);
        var key = ToPath(path.AsPath());
        bool replaced = false;
        if (!opts.SkipModuleResolution && options.LibReplacement == Tristate.True && !name.EqualsBytes("lib.d.ts"u8))
        {
            var libraryName = GetLibraryNameValueFromLibFileName(name);
            var resolveFrom = GetInferredLibraryNameResolveFrom(opts.Config.BaseDirectory(), name);
            var (resolved, trace) = ResolveLibrary(libraryName, resolveFrom);
            if (resolved.IsResolved()) (path, key, replaced) = (resolved!.ResolvedFileName, resolved.ResolvedPath, true);
            pathForLibFileResolutions.LoadOrStore(ToPath(resolveFrom.AsPath()), new() { libraryName = libraryName, resolution = resolved, trace = trace });
        }
        return pathForLibFileCache.LoadOrStore(nameKey, new() { NameValue = name, path = path, pathKey = key, Replaced = replaced }).Actual;
    }
    private (ResolvedModule? Resolution, Slice<DiagAndArgs> Trace) ResolveLibrary(ByteStringValue libraryName, RootedFilePath resolveFrom)
    {
        var (resolved, trace, error) = ResolverText.ResolveModuleName(resolver, libraryName, resolveFrom, ResolutionMode.CommonJS);
        RecordModuleResolutionError(error);
        return (resolved, trace);
    }
    private void RecordModuleResolutionError(Exception? error)
    {
        if (error is not null) lock (resolutionErrorGate) moduleResolutionError ??= error;
    }
    internal static string GetLibraryNameFromLibFileName(string libFileName) => GetLibraryNameValueFromLibFileName(ByteStringValue.FromString(libFileName)).Materialize()!;
    internal static ByteStringValue GetLibraryNameValueFromLibFileName(ByteStringValue libFileName)
    {
        var components = GoStrings.Split(libFileName, ".");
        var path = new GoBuilder();
        path.WriteString("@typescript/lib-");
        if (components.Length > 1) path.WriteString(components[1]);
        for (int i = 2; i < components.Length && components[i].Length != 0 && !components[i].EqualsBytes("d"u8); i++)
        {
            path.WriteByte((byte)(i == 2 ? '/' : '-'));
            path.WriteString(components[i]);
        }
        return path.Value();
    }
    internal static RootedFilePath GetInferredLibraryNameResolveFrom(RootedDirectoryPath baseDirectory, string fileName) => GetInferredLibraryNameResolveFrom(baseDirectory, ByteStringValue.FromString(fileName));
    internal static RootedFilePath GetInferredLibraryNameResolveFrom(RootedDirectoryPath baseDirectory, ByteStringValue fileName)
    {
        var path = new GoBuilder();
        path.WriteString("__lib_node_modules_lookup_"); path.WriteString(fileName); path.WriteString("__.ts");
        return baseDirectory.ResolveFile(path.Value());
    }
    internal Slice<ParseTask> rootTasks;
    internal readonly ProjectDiskHost? legacyHost;
    internal IResolutionHost host;
    internal IResolver resolver;
    internal readonly CompilerOptions options;
    internal readonly bool graph;
    private readonly IReadOnlyDictionary<ByteStringKey, SourceFile> embeddedFiles;
    internal readonly FilesParser filesParser = new();
    internal readonly IncludeProcessor includeProcessor;
    private readonly List<List<string>> supportedExtensions, supportedExtensionsWithJson;
    private readonly NodeFactory factory = Ast.NewNodeFactory(default);

    private FileLoader(IResolutionHost host, IResolver resolver, CompilerOptions options, IReadOnlyList<SourceFile> libraries,
        ProjectConfiguration configuration, bool graph, ProjectDiskHost? legacyHost = null)
    {
        this.host = host;
        this.resolver = resolver;
        this.options = options;
        this.graph = graph;
        this.legacyHost = legacyHost;
        includeProcessor = new(configuration, graph);
        embeddedFiles = libraries.ToDictionary(file => file.PathKeyKey(), ByteStringKey.Comparer);
        supportedExtensions = ConfigParser.GetSupportedExtensions(options, null);
        supportedExtensionsWithJson = ConfigParser.GetSupportedExtensionsWithJsonIfResolveJsonModule(options, supportedExtensions);
        filesParser.maxDepth = options.MaxNodeModuleJsDepth?.Value ?? 0;
    }

    internal RootedFilePath TypedFile(string file) => TypedFile(ByteStringValue.FromString(file));
    internal RootedFilePath TypedFile(ByteStringKey file) => TypedFile(file.Value);
    internal RootedFilePath TypedFile(ByteStringValue file) => graph ? RootedFilePath.FromValue(file) : RootedFilePath.FromUnicode(file.Materialize()!);
    internal ByteStringKey FileValue(RootedFilePath file) => ByteStringKey.FromValue(graph ? file.AsValue() : ByteStringValue.FromString(file.ToUnicode()));
    internal ByteStringKey KeyValue(PathKey path) => ByteStringKey.FromValue(graph ? path.AsValue() : ByteStringValue.FromString(PathByteText.ToUnicode(path.AsValue())));
    internal string FileText(RootedFilePath file) => FileValue(file).ToString();
    internal string KeyText(PathKey path) => KeyValue(path).ToString();
    internal string PathBytes(string path) => graph ? path : GoString.ToUtf8(path);
    internal ByteStringValue PathBytes(ByteStringKey path) => graph ? path.Value : ByteStringValue.FromString(GoString.ToUtf8(path.ToString()));
    internal bool FileExists(string path) => host.FS().FileExists(TypedFile(path));
    internal bool FileExists(ByteStringKey path) => host.FS().FileExists(TypedFile(path));
    internal bool IsSupportedExtension(string key) => IsSupportedExtension(ByteStringKey.FromString(key));
    internal bool IsSupportedExtension(ByteStringKey key) => supportedExtensionsWithJson.Any(group => TypedFile(key).ExtensionIsOneOf(group));

    // fileloader.go:175. Neither graph inspection nor the legacy explicit-library
    // environment constructs the default/global library or automatic type universe.
    internal static ProcessedFiles ProcessAllProgramFiles(ProjectDiskHost disk, LegacyResolutionHost resolutionHost, DefaultResolver resolver, CompilerOptions options,
        IReadOnlyList<SourceFile> libraries, IReadOnlyList<string> roots, ProjectConfiguration configuration)
    {
        var loader = new FileLoader(resolutionHost, resolver, options, libraries, configuration, false, disk);
        foreach (SourceFile file in libraries)
            loader.AddRootFileTask(file.FileName(), file.FileName(), new());
        for (int i = 0; i < roots.Count; i++)
        {
            int rootIndex = configuration.Parsed is not null ? i : configuration.Files.ToList().FindIndex(spec => disk.Resolve(configuration.Root, spec) == roots[i]);
            string referenceText = configuration.Parsed is not null ? PathByteText.ToUnicode(configuration.Parsed.RootFileNameForDiagnostic(i)) : roots[i];
            loader.AddRootFileTask(roots[i], referenceText, new() { rootIndex = rootIndex });
        }
        loader.filesParser.Parse(loader, loader.rootTasks);
        return loader.filesParser.GetProcessedFiles(loader);
    }

    internal static ProcessedFiles ProcessResolvedProjectFiles(IResolutionHost host, IResolver resolver, CompilerOptions options,
        IReadOnlyList<RootedFilePath> roots, ProjectConfiguration configuration)
    {
        var loader = new FileLoader(host, resolver, options, [], configuration, true);
        for (int i = 0; i < roots.Count; i++)
            loader.AddRootFileTask(ByteStringKey.FromValue(roots[i].AsValue()), ByteStringValue.FromString(configuration.Parsed!.RootFileNameForDiagnostic(i)), new() { rootIndex = i });
        loader.filesParser.Parse(loader, loader.rootTasks);
        return loader.filesParser.GetProcessedFiles(loader);
    }

    // fileloader.go:253/682. Extension search is a source-loader responsibility,
    // distinct from Module's import candidate search.
    internal void AddRootFileTask(string fileName, string referenceText, FileIncludeReason reason) => AddRootFileTask(ByteStringKey.FromString(fileName), ByteStringValue.FromString(referenceText), reason);
    internal void AddRootFileTask(ByteStringKey fileName, ByteStringValue referenceText, FileIncludeReason reason)
    {
        var(result, key, diagnostic) = GetSourceFileFromReference(TypedFile(fileName), graph ? referenceText : ByteStringValue.FromString(GoString.ToUtf8(referenceText.Materialize()!)));
        rootTasks = rootTasks.Append(new ParseTask
        {
            normalizedFilePath = result != default ? FileValue(result) : fileName,
            path = key != default ? KeyValue(key) : KeyValue(host.FS().CaseSensitivity().PathKey(TypedFile(fileName).AsPath())),
            file = embeddedFiles.GetValueOrDefault(fileName), failedLookup = diagnostic is not null, includeReason = reason,
            processingDiagnostics = diagnostic is null ? default : Slice<ProcessingDiagnostic>.FromArray([new() { diagnosticReason = reason, message = diagnostic.message, args = diagnostic.MessageArgs()?.Select(value => graph ? value : GoString.ToString(value)).ToArray() ?? [] }])
        });
    }

    private (RootedFilePath File, PathKey Key, Diagnostic? Diagnostic) GetSourceFileFromReference(RootedFilePath fileName, ByteStringValue referenceText)
    {
        var text = FileValue(fileName);
        if (embeddedFiles.TryGetValue(text, out SourceFile? library))
            return (fileName, host.FS().CaseSensitivity().PathKey(fileName.AsPath()), null);
        if (!graph) Project.RequireImplementationFile(text.Value);
        bool allowNonTs = options.AllowNonTsExtensions == Tristate.True;
        var diagnosticName = TsPath.NormalizeSlashes(referenceText);
        if (fileName.HasExtension())
        {
            PathKey key = host.FS().CaseSensitivity().PathKey(fileName.AsPath());
            if (!allowNonTs && !IsSupportedExtension(KeyValue(key)))
                return (default, default, UnsupportedExtensionDiagnostic(diagnosticName, fileName));
            if (!(program ? compilerHost!.FS() : host.FS()).FileExists(fileName))
                return (default, default, Ast.NewCompilerDiagnostic(Diagnostics.File_0_not_found, diagnosticName));
            return (fileName, key, null);
        }
        if (allowNonTs)
            return (program ? compilerHost!.FS() : host.FS()).FileExists(fileName) ? (fileName, host.FS().CaseSensitivity().PathKey(fileName.AsPath()), null) :
                (default, default, Ast.NewCompilerDiagnostic(Diagnostics.File_0_not_found, diagnosticName));
        foreach (string extension in supportedExtensions[0])
        {
            RootedFilePath candidate = fileName.AppendSuffix(extension);
            if ((program ? compilerHost!.FS() : host.FS()).FileExists(candidate))
                return (candidate, host.FS().CaseSensitivity().PathKey(candidate.AsPath()), null);
        }
        return (default, default, Ast.NewCompilerDiagnostic(Diagnostics.Could_not_resolve_the_path_0_with_the_extensions_Colon_1,
            diagnosticName, "'" + string.Join("', '", supportedExtensions.SelectMany(group => group)) + "'"));
    }

    internal Diagnostic UnsupportedExtensionDiagnostic(ByteStringValue diagnosticName, RootedFilePath fileName) => fileName.HasJSFileExtension()
        ? Ast.NewCompilerDiagnostic(Diagnostics.File_0_is_a_JavaScript_file_Did_you_mean_to_enable_the_allowJs_option, diagnosticName)
        : Ast.NewCompilerDiagnostic(Diagnostics.File_0_has_an_unsupported_extension_The_only_supported_extensions_are_1,
            diagnosticName, "'" + string.Join("', '", supportedExtensions.SelectMany(group => group)) + "'");

    // fileloader.go:382. Preserve package-directory provenance even when its
    // type field does not affect the effective format for these options.
    internal SourceFileMetaData LoadSourceFileMetaData(ByteStringKey text)
    {
        RootedFilePath file = TypedFile(text);
        if (program && opts.SkipModuleResolution) return new("", default, (ResolutionMode)Ast.GetImpliedNodeFormatForFile(file, ""));
        var scope = resolver.GetResolutionData().NewResolver(program ? projectReferences!.host : host).GetPackageScopeForPath(file.Directory());
        string packageType = "";
        RootedDirectoryPath packageDirectory = default;
        if (scope?.Exists() == true)
        {
            packageDirectory = scope.PackageDirectory.AsDirectoryPath();
            var(value, ok) = scope.Contents!.Fields.Type.GetValue();
            ModuleResolutionKind kind = options.GetModuleResolutionKind();
            if (ok && ((!file.ExtensionIsOneOf([TsPath.ExtensionMts, TsPath.ExtensionCts, TsPath.ExtensionMjs, TsPath.ExtensionCjs]) &&
                kind >= ModuleResolutionKind.Node16 && kind <= ModuleResolutionKind.NodeNext) || file.ContainsLowercaseDirectorySequence("/node_modules/")))
                packageType = value;
        }
        return new(packageType, packageDirectory, (ResolutionMode)Ast.GetImpliedNodeFormatForFile(file, packageType));
    }

    // fileloader.go:410. IFS.ReadFile already returns Go bytes: the byte overload
    // is essential here, avoiding the Unicode overload's additional UTF8 encoding.
    internal SourceFile? ParseSourceFile(ParseTask task)
    {
        if (program)
        {
            var effectiveOptions = projectReferences!.GetCompilerOptionsForFile(TypedFile(task.normalizedFilePath), PathKey.FromValue(task.path.Value));
            var programParseOptions = new SourceFileParseOptions(task.normalizedFilePath.Value, task.path.Value, Ast.GetExternalModuleIndicatorOptions(TypedFile(task.normalizedFilePath), effectiveOptions, task.metadata), FileNameIsGoBytes: true);
            return TypedFile(task.normalizedFilePath).ExtensionIsOneOf(contentMapperExtensions ?? []) ? ParseContentMappedFile(programParseOptions) : compilerHost!.GetSourceFile(programParseOptions);
        }
        if (!graph) Project.RequireImplementationFile(task.normalizedFilePath.Value);
        var(text, ok) = SourceTextReaders.Read(host.FS(), TypedFile(task.normalizedFilePath));
        if (!ok) return null;
        var parseOptions = new SourceFileParseOptions(task.normalizedFilePath.Value, task.path.Value,
            graph ? Ast.GetExternalModuleIndicatorOptions(TypedFile(task.normalizedFilePath), options, task.metadata) : default, FileNameIsGoBytes: graph);
        ScriptKind kind = graph ? Core.GetScriptKindFromFileName(task.normalizedFilePath.Value) : ScriptKind.TS;
        if (kind == ScriptKind.Unknown) kind = ScriptKind.TS;
        return Parser.ParseSourceFileOwned(parseOptions, text, kind);
    }

    // fileloader.go:721. Explicit path references enter the same task scheduler.
    internal void ResolveTripleslashPathReferences(ParseTask task)
    {
        SourceFile file = task.file!;
        foreach (FileReference reference in file.ReferencedFiles)
        {
            var reason = new FileIncludeReason { importingFile = file, reference = reference, kind = IncludeReferenceKind.Path };
            if (options.NoResolve == Tristate.True)
            {
                (task.pathReferences ??= []).Add(new() { ImportingFile = file, Reference = reference, Scheduling = DependencyScheduling.NoResolve });
                continue;
            }
            RootedFilePath normalized = TypedFile(graph ? file.FileNameGoValue() : ByteStringValue.FromString(file.FileName())).Directory().ResolveFile(reference.FileNameValue);
            var(resolved, key, diagnostic) = GetSourceFileFromReference(normalized, reference.FileNameValue);
            if (diagnostic is null && KeyValue(key) == task.path)
                diagnostic = Ast.NewCompilerDiagnostic(Diagnostics.A_file_cannot_have_a_reference_to_itself);
            ProcessingDiagnostic? processing = diagnostic is null ? null : new()
            {
                diagnosticReason = reason, message = diagnostic.message, args = diagnostic.MessageArgs()?.ToArray() ?? []
            };
            // resolveTripleslashPathReference returns nil resolvedRef on either
            // lookup failure or self-reference; retain that actual result boundary.
            if (processing is not null) (resolved, key) = (default, default);
            var edge = new ResolvedProjectReferenceEdge { ImportingFile = file, Reference = reference, ResolvedFileName = resolved,
                ResolvedPath = key, ProcessingDiagnostic = processing, Scheduling = processing is null ? DependencyScheduling.Scheduled : DependencyScheduling.ResolutionDiagnostic };
            (task.pathReferences ??= []).Add(edge);
            if (processing is not null)
                task.processingDiagnostics = task.processingDiagnostics.Append(processing);
            else
                task.AddSubTask(FileValue(resolved), KeyValue(key), reason);
        }
    }

    // fileloader.go:764. Explicit requests are resolved and scheduled; automatic
    // directives/global binding/default libraries are outside graph inspection.
    internal void ResolveTypeReferenceDirectives(ParseTask task)
    {
        SourceFile file = task.file!;
        foreach (FileReference reference in file.TypeReferenceDirectives)
        {
            if (options.NoResolve == Tristate.True)
            {
                (task.typeDirectives ??= []).Add(new() { ImportingFile = file, Reference = reference, Scheduling = DependencyScheduling.NoResolve });
                continue;
            }
            var (redirect, containingName) = program ? projectReferences!.GetRedirectForResolution(file) : (null, TypedFile(graph ? file.FileNameGoValue() : ByteStringValue.FromString(file.FileName())));
            var effectiveOptions = DefaultResolver.GetCompilerOptionsWithRedirect(options, redirect);
            ResolutionMode mode = reference.ResolutionMode != ResolutionMode.None ? reference.ResolutionMode : GetDefaultResolutionModeForFile(TypedFile(graph ? file.FileNameGoValue() : ByteStringValue.FromString(file.FileName())), task.metadata, effectiveOptions);
            var(result, trace) = resolver.ResolveTypeReferenceDirective(reference.FileNameValue, containingName, mode, redirect);
            if (program)
            {
                (task.typeResolutionsInFile ??= new())[new(reference.FileNameValue, mode)] = result;
                task.typeResolutionsTrace = task.typeResolutionsTrace.Append(trace);
            }
            var edge = new ResolvedProjectTypeDirective { ImportingFile = file, Reference = reference, Mode = mode,
                Resolution = result, Traces = trace, Scheduling = result?.IsResolved() == true ? DependencyScheduling.Scheduled : DependencyScheduling.Unresolved };
            (task.typeDirectives ??= []).Add(edge);
            // fileInclude.go:133 leaves the type-reference location PackageId
            // zero; the task still retains the actual ID for package dedup.
            var reason = new FileIncludeReason { importingFile = file, reference = reference, kind = IncludeReferenceKind.Type };
            if (result?.IsResolved() == true)
                task.AddSubTask(FileValue(result.ResolvedFileName), KeyValue(result.ResolvedPath), reason, result.IsExternalLibraryImport, false, result.PackageId);
            else
                task.processingDiagnostics = task.processingDiagnostics.Append(new ProcessingDiagnostic() { kind = ProcessingDiagnosticKind.UnknownReference, diagnosticReason = reason });
        }
    }

    // fileloader.go:821. Synthetic nodes precede real imports, followed by string
    // augmentations. All original resolutions happen before scheduling decisions.
    internal void ResolveImportsAndModuleAugmentations(ParseTask task)
    {
        SourceFile file = task.file!;
        var (redirect, containingName) = program ? projectReferences!.GetRedirectForResolution(file) : (null, TypedFile(graph ? file.FileNameGoValue() : ByteStringValue.FromString(file.FileName())));
        CompilerOptions options = DefaultResolver.GetCompilerOptionsWithRedirect(this.options, redirect);
        Slice<Node> names = default;
        bool js = Ast.IsSourceFileJS(file);
        if (graph && (js || (!file.IsDeclarationFile && (options.GetIsolatedModules() || Ast.IsExternalModule(file)))) && options.ImportHelpers == Tristate.True)
            names = names.Append(task.importHelpersImportSpecifier = CreateSyntheticImport(ByteStringValue.FromString("tslib"), file));
        if (graph && (js || file.ScriptKind == ScriptKind.TSX))
        {
            var runtime = Ast.GetJSXRuntimeImportValue(Ast.GetJSXImplicitImportBaseValue(options, file), options);
            if (runtime.Length != 0) names = names.Append(task.jsxRuntimeImportSpecifier = CreateSyntheticImport(runtime, file));
        }
        int importsStart = names.Length;
        names = names.Append(file.Imports());
        if (graph)
            foreach (var node in file.ModuleAugmentations)
                if (node.Kind == Kind.StringLiteral) names = names.Append(node);
        if (names.Length == 0 || program && opts.SkipModuleResolution) return;
        task.resolutionsInFile = new();
        for (int index = 0; index < names.Length; index++)
        {
            Node entry = names[index];
            var name = entry.TextValue();
            if (name.Length == 0 || Ast.IsSourcePhaseImport(entry.Parent)) continue;
            if (!graph)
            {
                string unicodeName = ProjectDiskHost.DecodeModuleReference(name);
                if (!TsPath.PathIsRelative(unicodeName))
                    throw new ProjectException("moduleSpecifier", "Only relative module specifiers are supported: " + unicodeName, file.FileName());
            }
            ResolutionMode mode = graph ? GetModeForUsageLocation(TypedFile(graph ? file.FileNameGoValue() : ByteStringValue.FromString(file.FileName())), task.metadata, entry, options) : ResolutionMode.ESNext;
            ResolvedModule? value;
            Slice<DiagAndArgs> trace;
            Exception? error;
            if (!graph)
            {
                value = ((LegacyResolutionHost)host).ResolveModuleName((DefaultResolver)resolver, name, TypedFile(graph ? file.FileNameGoValue() : ByteStringValue.FromString(file.FileName())), mode);
                trace = default;
                error = null;
            }
            else
            {
                (value, trace, error) = resolver.ResolveModuleName(name, containingName, mode, redirect);
            }
            if (program) RecordModuleResolutionError(error);
            if (program) task.resolutionsTrace = task.resolutionsTrace.Append(trace);
            ResolvedModule resolved = value ?? new();
            task.resolutionsInFile[new(name, mode)] = resolved;
            int importIndex = index - importsStart;
            var edge = new ResolvedProjectEdge { ImportingFile = file, ModuleSpecifier = entry, ModuleReferenceValue = name,
                Mode = mode, ImportIndex = importIndex, SyntheticKind = importIndex < 0 ? (ReferenceEquals(entry, task.importHelpersImportSpecifier) ? "importHelpers" : "jsxRuntime") : "",
                Resolution = resolved, Traces = trace, Error = error, Scheduling = DependencyScheduling.Unresolved };
            (task.graphEdges ??= []).Add(edge);
            if (!graph)
                (task.resolutionEdges ??= []).Add(new(file, entry, ProjectDiskHost.DecodeModuleReference(name), mode, resolved, null));
            if (!resolved.IsResolved()) continue;
            bool jsFile = !resolved.ResolvedUsingExtraExtensions && !resolved.ResolvedFileName.ExtensionIsOneOf(TsPath.SupportedTSExtensionsWithJsonFlat) && (!program || projectReferences!.GetRedirectForResolution(resolved.ResolvedFileName, resolved.ResolvedPath).Config is null);
            bool jsFromNodeModules = resolved.IsExternalLibraryImport && jsFile && resolved.ResolvedFileName.ContainsLowercaseDirectorySequence("/node_modules/");
            Message? diagnostic = ModuleUtilities.GetResolutionDiagnostic(options, resolved, file);
            edge.Scheduling = diagnostic is not null ? DependencyScheduling.ResolutionDiagnostic :
                options.NoResolve == Tristate.True ? DependencyScheduling.NoResolve :
                jsFile && !options.GetAllowJS() ? DependencyScheduling.JavaScriptDisabled :
                importIndex >= file.Imports().Length ? DependencyScheduling.ModuleAugmentation :
                importIndex >= 0 && !Ast.IsInJSFile(entry) && (entry.Flags & NodeFlags.JSDoc) != 0 ? DependencyScheduling.JSDocInTypeScript : DependencyScheduling.Scheduled;
            if (edge.Scheduling != DependencyScheduling.Scheduled) continue;
            task.AddSubTask(FileValue(resolved.ResolvedFileName), KeyValue(resolved.ResolvedPath),
                new() { importingFile = file, specifier = entry, packageId = resolved.PackageId, syntheticKind = edge.SyntheticKind },
                resolved.IsExternalLibraryImport, jsFromNodeModules, resolved.PackageId, edge);
        }
    }

    // fileloader.go:942. The actual AST parent chain supplies synthetic usage mode.
    private Node CreateSyntheticImport(ByteStringValue text, SourceFile file)
    {
        lock (factory)
        {
        Node specifier = factory.NewStringLiteralWithTextValue(text, TokenFlags.None);
        Node declaration = factory.NewImportDeclaration(null!, null!, specifier, null!);
        specifier.Parent = declaration;
        declaration.Parent = file.AsNode();
        return specifier;
        }
    }

    // fileloader.go:1029–1112.
    internal static ResolutionMode GetDefaultResolutionModeForFile(RootedFilePath file, SourceFileMetaData metadata, CompilerOptions options) =>
        ImportSyntaxAffectsModuleResolution(options) ? Ast.GetImpliedNodeFormatForEmitWorker(file, options.GetEmitModuleKind(), metadata) : ResolutionMode.None;
    internal static bool ImportSyntaxAffectsModuleResolution(CompilerOptions options)
    {
        ModuleResolutionKind kind = options.GetModuleResolutionKind();
        return (kind >= ModuleResolutionKind.Node16 && kind <= ModuleResolutionKind.NodeNext) || options.GetResolvePackageJsonExports() || options.GetResolvePackageJsonImports();
    }
    internal static ResolutionMode GetModeForUsageLocation(RootedFilePath file, SourceFileMetaData metadata, Node usage, CompilerOptions options)
    {
        Node parent = usage.Parent;
        if ((Ast.IsImportDeclaration(parent) || parent.Kind == Kind.JSImportDeclaration || Ast.IsExportDeclaration(parent) || Ast.IsJSDocImportTag(parent)) && Ast.IsExclusivelyTypeOnlyImportOrExport(parent))
        {
            Node attributes = parent.Kind == Kind.JSDocImportTag ? parent.AsJSDocImportTag().Attributes : Ast.GetImportAttributes(parent);
            var(mode, found) = attributes.GetResolutionModeOverride();
            if (found) return mode;
        }
        if (Ast.IsLiteralTypeNode(parent) && Ast.IsImportTypeNode(parent.Parent))
        {
            var(mode, found) = parent.Parent.AsImportTypeNode().Attributes.GetResolutionModeOverride();
            if (found) return mode;
        }
        return ImportSyntaxAffectsModuleResolution(options) ? GetEmitSyntaxForUsageLocationWorker(file, metadata, usage, options) : ResolutionMode.None;
    }
    internal static ResolutionMode GetEmitSyntaxForUsageLocationWorker(RootedFilePath file, SourceFileMetaData metadata, Node usage, CompilerOptions options)
    {
        Node parent = usage.Parent;
        if (Ast.IsRequireCall(parent, false) || (Ast.IsExternalModuleReference(parent) && Ast.IsImportEqualsDeclaration(parent.Parent))) return ResolutionMode.CommonJS;
        ModuleKind mode = Ast.GetEmitModuleFormatOfFileWorker(file, options, metadata);
        Node call = Ast.WalkUpParenthesizedExpressions(parent);
        if (Ast.IsImportCall(call))
            return !Ast.IsSourcePhaseImportCall(call) && Ast.ShouldTransformImportCall(file, options, mode) ? ResolutionMode.CommonJS : ResolutionMode.ESNext;
        return mode == ModuleKind.CommonJS ? ResolutionMode.CommonJS : mode.IsNonNodeESM() || mode == ModuleKind.Preserve ? ResolutionMode.ESNext : ResolutionMode.None;
    }

    internal void ValidateParsedSource(SourceFile file)
    {
        if (graph || embeddedFiles.ContainsKey(file.PathKeyKey()) || file.Diagnostics() is { Count: > 0 }) return;
        ProjectAdmission.Validate(file);
    }
}

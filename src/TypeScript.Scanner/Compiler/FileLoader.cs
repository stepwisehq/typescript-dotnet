// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Selected port of tsc/internal/compiler/fileloader.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e: roots/references, metadata, modes,
// synthetic/import/augmentation dependencies. Full Program environments are external.
using TypeScript.Checking;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Scanner;
using TypeScript.Vfs;
using ConfigParser = TypeScript.TsOptions.TsOptions;

namespace TypeScript.Compiler;
internal sealed class FileLoader
{
    internal readonly List<ParseTask> rootTasks = [];
    internal readonly ProjectDiskHost? legacyHost;
    internal readonly IResolutionHost host;
    internal readonly IResolver resolver;
    internal readonly CompilerOptions options;
    internal readonly bool graph;
    private readonly IReadOnlyDictionary<string, SourceFile> embeddedFiles;
    internal readonly FilesParser filesParser = new();
    internal readonly IncludeProcessor includeProcessor;
    private readonly List<List<string>> supportedExtensions, supportedExtensionsWithJson;
    private readonly NodeFactory factory = Ast.NewNodeFactory(null!);

    private FileLoader(IResolutionHost host, IResolver resolver, CompilerOptions options, IReadOnlyList<SourceFile> libraries,
        ProjectConfiguration configuration, bool graph, ProjectDiskHost? legacyHost = null)
    {
        this.host = host;
        this.resolver = resolver;
        this.options = options;
        this.graph = graph;
        this.legacyHost = legacyHost;
        includeProcessor = new(configuration, graph);
        embeddedFiles = libraries.ToDictionary(file => file.PathKey(), StringComparer.Ordinal);
        supportedExtensions = ConfigParser.GetSupportedExtensions(options, null);
        supportedExtensionsWithJson = ConfigParser.GetSupportedExtensionsWithJsonIfResolveJsonModule(options, supportedExtensions);
        filesParser.maxDepth = options.MaxNodeModuleJsDepth?.Value ?? 0;
    }

    internal RootedFilePath TypedFile(string file) => graph ? RootedFilePath.FromGoString(file) : RootedFilePath.FromUnicode(file);
    internal string FileText(RootedFilePath file) => graph ? file.AsString() : file.ToUnicode();
    internal string KeyText(PathKey path) => graph ? path.AsString() : PathByteText.ToUnicode(path.AsString());
    internal string PathBytes(string path) => graph ? path : GoString.ToUtf8(path);
    internal bool FileExists(string path) => host.FS().FileExists(TypedFile(path));
    internal bool IsSupportedExtension(string key) => supportedExtensionsWithJson.Any(group => TypedFile(key).ExtensionIsOneOf(group));

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
            loader.AddRootFileTask(roots[i].AsString(), configuration.Parsed!.RootFileNameForDiagnostic(i), new() { rootIndex = i });
        loader.filesParser.Parse(loader, loader.rootTasks);
        return loader.filesParser.GetProcessedFiles(loader);
    }

    // fileloader.go:253/682. Extension search is a source-loader responsibility,
    // distinct from Module's import candidate search.
    internal void AddRootFileTask(string fileName, string referenceText, FileIncludeReason reason)
    {
        var(result, key, diagnostic) = GetSourceFileFromReference(TypedFile(fileName), PathBytes(referenceText));
        rootTasks.Add(new ParseTask
        {
            normalizedFilePath = result != default ? FileText(result) : fileName,
            path = key != default ? KeyText(key) : KeyText(host.FS().CaseSensitivity().PathKey(TypedFile(fileName).AsPath())),
            file = embeddedFiles.GetValueOrDefault(fileName), failedLookup = diagnostic is not null, includeReason = reason,
            processingDiagnostics = diagnostic is null ? null : [new() { diagnosticReason = reason, message = diagnostic.message, args = diagnostic.MessageArgs()?.Select(value => graph ? value : GoString.ToString(value)).ToArray() ?? [] }]
        });
    }

    private (RootedFilePath File, PathKey Key, Diagnostic? Diagnostic) GetSourceFileFromReference(RootedFilePath fileName, string referenceText)
    {
        string text = FileText(fileName);
        if (embeddedFiles.TryGetValue(text, out SourceFile? library))
            return (fileName, host.FS().CaseSensitivity().PathKey(fileName.AsPath()), null);
        if (!graph) Project.RequireImplementationFile(text);
        bool allowNonTs = options.AllowNonTsExtensions == Tristate.True;
        string diagnosticName = TsPath.NormalizeSlashes(referenceText);
        if (fileName.HasExtension())
        {
            PathKey key = host.FS().CaseSensitivity().PathKey(fileName.AsPath());
            if (!allowNonTs && !IsSupportedExtension(KeyText(key)))
                return (default, default, UnsupportedExtensionDiagnostic(diagnosticName, fileName));
            if (!host.FS().FileExists(fileName))
                return (default, default, Ast.NewCompilerDiagnostic(Diagnostics.File_0_not_found, diagnosticName));
            return (fileName, key, null);
        }
        if (allowNonTs)
            return host.FS().FileExists(fileName) ? (fileName, host.FS().CaseSensitivity().PathKey(fileName.AsPath()), null) :
                (default, default, Ast.NewCompilerDiagnostic(Diagnostics.File_0_not_found, diagnosticName));
        foreach (string extension in supportedExtensions[0])
        {
            RootedFilePath candidate = fileName.AppendSuffix(extension);
            if (host.FS().FileExists(candidate))
                return (candidate, host.FS().CaseSensitivity().PathKey(candidate.AsPath()), null);
        }
        return (default, default, Ast.NewCompilerDiagnostic(Diagnostics.Could_not_resolve_the_path_0_with_the_extensions_Colon_1,
            diagnosticName, "'" + string.Join("', '", supportedExtensions.SelectMany(group => group)) + "'"));
    }

    internal Diagnostic UnsupportedExtensionDiagnostic(string diagnosticName, RootedFilePath fileName) => fileName.HasJSFileExtension()
        ? Ast.NewCompilerDiagnostic(Diagnostics.File_0_is_a_JavaScript_file_Did_you_mean_to_enable_the_allowJs_option, diagnosticName)
        : Ast.NewCompilerDiagnostic(Diagnostics.File_0_has_an_unsupported_extension_The_only_supported_extensions_are_1,
            diagnosticName, "'" + string.Join("', '", supportedExtensions.SelectMany(group => group)) + "'");

    // fileloader.go:382. Preserve package-directory provenance even when its
    // type field does not affect the effective format for these options.
    internal SourceFileMetaData LoadSourceFileMetaData(string text)
    {
        RootedFilePath file = TypedFile(text);
        var scope = resolver.GetResolutionData().NewResolver(host).GetPackageScopeForPath(file.Directory());
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
        if (!graph) Project.RequireImplementationFile(task.normalizedFilePath);
        var(text, ok) = host.FS().ReadFile(TypedFile(task.normalizedFilePath));
        if (!ok) return null;
        var parseOptions = new SourceFileParseOptions(task.normalizedFilePath, task.path,
            graph ? Ast.GetExternalModuleIndicatorOptions(TypedFile(task.normalizedFilePath), options, task.metadata) : default, FileNameIsGoBytes: graph);
        ScriptKind kind = graph ? Core.GetScriptKindFromFileName(task.normalizedFilePath) : ScriptKind.TS;
        if (kind == ScriptKind.Unknown) kind = ScriptKind.TS;
        return Parser.ParseSourceFile(parseOptions, GoString.Bytes(text), kind);
    }

    // fileloader.go:721. Explicit path references enter the same task scheduler.
    internal void ResolveTripleslashPathReferences(ParseTask task)
    {
        SourceFile file = task.file!;
        foreach (FileReference reference in file.ReferencedFiles ?? [])
        {
            var reason = new FileIncludeReason { importingFile = file, reference = reference, kind = IncludeReferenceKind.Path };
            if (options.NoResolve == Tristate.True)
            {
                (task.pathReferences ??= []).Add(new() { ImportingFile = file, Reference = reference, Scheduling = DependencyScheduling.NoResolve });
                continue;
            }
            RootedFilePath normalized = TypedFile(file.FileName()).Directory().ResolveFile(reference.FileName);
            var(resolved, key, diagnostic) = GetSourceFileFromReference(normalized, reference.FileName);
            if (diagnostic is null && KeyText(key) == task.path)
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
                (task.processingDiagnostics ??= []).Add(processing);
            else
                task.AddSubTask(FileText(resolved), KeyText(key), reason);
        }
    }

    // fileloader.go:764. Explicit requests are resolved and scheduled; automatic
    // directives/global binding/default libraries are outside graph inspection.
    internal void ResolveTypeReferenceDirectives(ParseTask task)
    {
        SourceFile file = task.file!;
        foreach (FileReference reference in file.TypeReferenceDirectives ?? [])
        {
            if (options.NoResolve == Tristate.True)
            {
                (task.typeDirectives ??= []).Add(new() { ImportingFile = file, Reference = reference, Scheduling = DependencyScheduling.NoResolve });
                continue;
            }
            ResolutionMode mode = reference.ResolutionMode != ResolutionMode.None ? reference.ResolutionMode : GetDefaultResolutionModeForFile(TypedFile(file.FileName()), task.metadata, options);
            var(result, trace) = resolver.ResolveTypeReferenceDirective(reference.FileName, TypedFile(file.FileName()), mode);
            var edge = new ResolvedProjectTypeDirective { ImportingFile = file, Reference = reference, Mode = mode,
                Resolution = result, Traces = trace, Scheduling = result?.IsResolved() == true ? DependencyScheduling.Scheduled : DependencyScheduling.Unresolved };
            (task.typeDirectives ??= []).Add(edge);
            // fileInclude.go:133 leaves the type-reference location PackageId
            // zero; the task still retains the actual ID for package dedup.
            var reason = new FileIncludeReason { importingFile = file, reference = reference, kind = IncludeReferenceKind.Type };
            if (result?.IsResolved() == true)
                task.AddSubTask(FileText(result.ResolvedFileName), KeyText(result.ResolvedPath), reason, result.IsExternalLibraryImport, false, result.PackageId);
            else
                (task.processingDiagnostics ??= []).Add(new() { kind = ProcessingDiagnosticKind.UnknownReference, diagnosticReason = reason });
        }
    }

    // fileloader.go:821. Synthetic nodes precede real imports, followed by string
    // augmentations. All original resolutions happen before scheduling decisions.
    internal void ResolveImportsAndModuleAugmentations(ParseTask task)
    {
        SourceFile file = task.file!;
        var names = new List<Node>();
        bool js = Ast.IsSourceFileJS(file);
        if (graph && (js || (!file.IsDeclarationFile && (options.GetIsolatedModules() || Ast.IsExternalModule(file)))) && options.ImportHelpers == Tristate.True)
            names.Add(task.importHelpersImportSpecifier = CreateSyntheticImport("tslib", file));
        if (graph && (js || file.ScriptKind == ScriptKind.TSX))
        {
            string runtime = Ast.GetJSXRuntimeImport(Ast.GetJSXImplicitImportBase(options, file), options);
            if (runtime != "") names.Add(task.jsxRuntimeImportSpecifier = CreateSyntheticImport(runtime, file));
        }
        int importsStart = names.Count;
        names.AddRange(file.Imports() ?? []);
        if (graph) names.AddRange((file.ModuleAugmentations ?? []).Where(node => node.Kind == Kind.StringLiteral));
        if (names.Count == 0) return;
        task.resolutionsInFile = new();
        for (int index = 0; index < names.Count; index++)
        {
            Node entry = names[index];
            string name = entry.Text();
            if (name == "" || Ast.IsSourcePhaseImport(entry.Parent)) continue;
            if (!graph)
            {
                string unicodeName = ProjectDiskHost.DecodeModuleReference(name);
                if (!TsPath.PathIsRelative(unicodeName))
                    throw new ProjectException("moduleSpecifier", "Only relative module specifiers are supported: " + unicodeName, file.FileName());
            }
            ResolutionMode mode = graph ? GetModeForUsageLocation(TypedFile(file.FileName()), task.metadata, entry, options) : ResolutionMode.ESNext;
            ResolvedModule? value;
            List<DiagAndArgs>? trace;
            Exception? error;
            if (!graph)
            {
                value = ((LegacyResolutionHost)host).ResolveModuleName((DefaultResolver)resolver, name, TypedFile(file.FileName()), mode);
                trace = null;
                error = null;
            }
            else
            {
                (value, trace, error) = resolver.ResolveModuleName(name, TypedFile(file.FileName()), mode);
            }
            ResolvedModule resolved = value ?? new();
            task.resolutionsInFile[new(name, mode)] = resolved;
            int importIndex = index - importsStart;
            var edge = new ResolvedProjectEdge { ImportingFile = file, ModuleSpecifier = entry, ModuleReference = name,
                Mode = mode, ImportIndex = importIndex, SyntheticKind = importIndex < 0 ? (ReferenceEquals(entry, task.importHelpersImportSpecifier) ? "importHelpers" : "jsxRuntime") : "",
                Resolution = resolved, Traces = trace, Error = error, Scheduling = DependencyScheduling.Unresolved };
            (task.graphEdges ??= []).Add(edge);
            if (!graph)
                (task.resolutionEdges ??= []).Add(new(file, entry, ProjectDiskHost.DecodeModuleReference(name), mode, resolved, null));
            if (!resolved.IsResolved()) continue;
            bool jsFile = !resolved.ResolvedUsingExtraExtensions && !resolved.ResolvedFileName.ExtensionIsOneOf(TsPath.SupportedTSExtensionsWithJsonFlat);
            bool jsFromNodeModules = resolved.IsExternalLibraryImport && jsFile && resolved.ResolvedFileName.ContainsLowercaseDirectorySequence("/node_modules/");
            Message? diagnostic = ModuleUtilities.GetResolutionDiagnostic(options, resolved, file);
            edge.Scheduling = diagnostic is not null ? DependencyScheduling.ResolutionDiagnostic :
                options.NoResolve == Tristate.True ? DependencyScheduling.NoResolve :
                jsFile && !options.GetAllowJS() ? DependencyScheduling.JavaScriptDisabled :
                importIndex >= (file.Imports()?.Count ?? 0) ? DependencyScheduling.ModuleAugmentation :
                importIndex >= 0 && !Ast.IsInJSFile(entry) && (entry.Flags & NodeFlags.JSDoc) != 0 ? DependencyScheduling.JSDocInTypeScript : DependencyScheduling.Scheduled;
            if (edge.Scheduling != DependencyScheduling.Scheduled) continue;
            task.AddSubTask(FileText(resolved.ResolvedFileName), KeyText(resolved.ResolvedPath),
                new() { importingFile = file, specifier = entry, packageId = resolved.PackageId, syntheticKind = edge.SyntheticKind },
                resolved.IsExternalLibraryImport, jsFromNodeModules, resolved.PackageId, edge);
        }
    }

    // fileloader.go:942. The actual AST parent chain supplies synthetic usage mode.
    private Node CreateSyntheticImport(string text, SourceFile file)
    {
        Node specifier = factory.NewStringLiteral(text, TokenFlags.None);
        Node declaration = factory.NewImportDeclaration(null!, null!, specifier, null!);
        specifier.Parent = declaration;
        declaration.Parent = file.AsNode();
        return specifier;
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
        if (graph || embeddedFiles.ContainsKey(file.PathKey()) || file.Diagnostics() is { Count: > 0 }) return;
        ProjectAdmission.Validate(file);
    }
}

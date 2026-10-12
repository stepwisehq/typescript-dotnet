// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/compiler/emitHost.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

using TypeScript.DiagnosticPrinting;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.TsOptions;
using TypeScript.Transformers.Declarations;

namespace TypeScript.Compiler;
// emitter.go:467. Shared eligibility contract; Program and the per-file host own real environments.
internal interface ISourceFileMayBeEmittedHost
{
    CompilerOptions Options();
    SourceOutputAndProjectReference? GetProjectReferenceFromSource(PathKey path);
    bool IsSourceFileFromExternalLibrary(SourceFile file);
    RootedDirectoryPath BaseDirectory();
    CaseSensitivity CaseSensitivity();
    Slice<SourceFile> SourceFiles();
}

// emitHost.go:20. All operations must be thread-safe.
internal interface ICompilerEmitHost : IDeclarationEmitHost, ISourceFileMayBeEmittedHost
{
    bool IsEmitBlocked(RootedFilePath file);
    Exception? WriteFile(RootedFilePath fileName, string text);
    ModuleKind GetEmitModuleFormatOfFile(HasFileName file);
    IEmitResolver NewEmitResolver(EmitContext context);
}

// emitHost.go:35. The checker lease is retained by the caller for this host's lifetime.
internal sealed class CompilerEmitHost : ICompilerEmitHost
{
    private readonly Program program;
    private readonly Func<EmitContext, IEmitResolver> newEmitResolver;
    private CompilerEmitHost(Program program, Func<EmitContext, IEmitResolver> newEmitResolver)
    {
        this.program = program;
        this.newEmitResolver = newEmitResolver;
    }

    // emitHost.go:40.
    internal static (CompilerEmitHost Host, Action Done) New(CancellationToken context, Program program, SourceFile file)
    {
        var(checker, done) = program.GetTypeCheckerForFile(context, file);
        return (new CompilerEmitHost(program, checker.NewEmitResolver), done);
    }

    // Same host/checker owner; only the actual request checkout retains values.
    internal static (CompilerEmitHost Host, Action Done) New(CompilerRequestContext context, Program program, SourceFile file)
    {
        var(checker, done) = context.Request is { } request ? program.GetTypeCheckerForFile(request, file) : program.GetTypeCheckerForFile(context.Cancellation, file);
        return (new CompilerEmitHost(program, checker.NewEmitResolver), done);
    }

    // emitHost.go:48–136. Delegation preserves the actual Program's metadata and caches.
    public ResolutionMode GetModeForUsageLocation(HasFileName file, Node moduleSpecifier) => program.GetModeForUsageLocation(file, moduleSpecifier);
    public ResolvedModule? GetResolvedModuleFromModuleSpecifier(HasFileName file, Node moduleSpecifier) => program.GetResolvedModuleFromModuleSpecifier(file, moduleSpecifier);
    public ResolutionMode GetDefaultResolutionModeForFile(HasFileName file) => program.GetDefaultResolutionModeForFile(file);
    public ModuleKind GetEmitModuleFormatOfFile(HasFileName file) => program.GetEmitModuleFormatOfFile(file);
    public bool FileExists(RootedFilePath path) => program.FileExists(path);
    public RootedDirectoryPath GetGlobalTypingsCacheLocation() => program.GetGlobalTypingsCacheLocation();
    public RootedDirectoryPath GetNearestAncestorDirectoryWithPackageJson(RootedDirectoryPath directory) => program.GetNearestAncestorDirectoryWithPackageJson(directory);
    public InfoCacheEntry? GetPackageJsonInfo(RootedFilePath path) => program.GetPackageJsonInfo(path);
    public RootedFilePath GetSourceOfProjectReferenceIfOutputIncluded(HasFileName file) => program.GetSourceOfProjectReferenceIfOutputIncluded(file);
    public SourceOutputAndProjectReference? GetProjectReferenceFromSource(PathKey path) => program.GetProjectReferenceFromSource(path);
    public Slice<RootedFilePath> GetRedirectTargets(PathKey path) => program.GetRedirectTargets(path);
    // emitHost.go:91. Original TODO: cache output paths.
    public IDeclarationOutputPaths GetOutputPathsFor(SourceFile file, bool forceDtsPaths) => TypeScript.OutputPaths.OutputPaths.GetOutputPathsFor(file, Options(), this, new() { Dts = forceDtsPaths });
    public bool SourceFileMayBeEmitted(SourceFile file, bool forceDtsEmit) => Program.sourceFileMayBeEmitted(file, this, forceDtsEmit, false);
    public SourceFile? GetSourceFileFromReference(SourceFile origin, FileReference reference) => program.GetSourceFileFromReference(origin, reference);
    public CompilerOptions Options() => program.Options();
    public Slice<SourceFile> SourceFiles() => program.SourceFiles();
    public RootedDirectoryPath BaseDirectory() => program.BaseDirectory();
    public RootedDirectoryPath CommonSourceDirectory() => program.CommonSourceDirectory();
    public List<string>? ContentMapperExtensions() => program.ContentMapperExtensions();
    public CaseSensitivity CaseSensitivity() => program.CaseSensitivity();
    public bool IsEmitBlocked(RootedFilePath file) => program.IsEmitBlocked(file);
    public Exception? WriteFile(RootedFilePath fileName, string text) => program.Host().FS().WriteFile(fileName, text);
    internal Exception? WriteOutputText(RootedFilePath fileName, ByteStringValue text) => TypeScript.Vfs.OutputTextWriters.Write(program.Host().FS(), fileName, text);
    public IEmitResolver NewEmitResolver(EmitContext context) => newEmitResolver(context);
    public bool IsSourceFileFromExternalLibrary(SourceFile file) => program.IsSourceFileFromExternalLibrary(file);
    public TypeScript.Symlinks.KnownSymlinks GetSymlinkCache() => program.GetSymlinkCache();
}

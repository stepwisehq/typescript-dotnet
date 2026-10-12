// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/project/compilerhost.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Compiler;
using TypeScript.ContentMapper;
using TypeScript.ProjectSystem.Logging;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Vfs;

namespace TypeScript.ProjectSystem;
internal sealed class ProjectCompilerHost : ICompilerHost
{
    internal PathKey configFilePath;
    internal SessionOptions sessionOptions = null!;
    internal SourceFS sourceFS = null!;
    internal ConfigFileRegistry? configFileRegistry;
    internal Project? project;
    internal ProjectCollectionBuilder? builder;
    internal LogTree? logger;
    private TypeScript.ContentMapper.IProject? contentMapperProject;
    private readonly ProjectOnce contentMapperOnce = new();
    // compilerhost.go:41.
    internal static ProjectCompilerHost NewCompilerHost(Project project, ProjectCollectionBuilder builder, LogTree? logger) => new()
    {
        configFilePath = project.configFilePath,
        sessionOptions = builder.sessionOptions,
        sourceFS = new(true, builder.fs),
        project = project,
        builder = builder,
        logger = logger
    };
    // compilerhost.go:63. The backing source changes only when finalized snapshot ownership takes over.
    internal void Freeze(SnapshotFS fs, ConfigFileRegistry registry)
    {
        if (builder == null)
            throw new InvalidOperationException("freeze can only be called once");
        sourceFS.source = fs;
        sourceFS.DisableTracking();
        configFileRegistry = registry;
        builder = null;
        project = null;
        logger = null;
    }

    // compilerhost.go:75.
    private void EnsureAlive()
    {
        if (builder == null || project == null)
            throw new InvalidOperationException("method must not be called after snapshot initialization");
    }

    // compilerhost.go:82/87/92.
    public RootedDirectoryPath DefaultLibraryPath() => sessionOptions.DefaultLibraryPath;
    public IFS FS() => sourceFS;
    public ParsedCommandLine? GetResolvedProjectReference(RootedFilePath name, PathKey path)
    {
        if (builder == null)
            return configFileRegistry!.GetConfig(path);
        sourceFS.Track(name);
        return builder.configFileRegistryBuilder.AcquireConfigForProject(name, path, project!, logger);
    }

    // compilerhost.go:104. Acquire parses and binds before publishing the cache entry.
    public SourceFile? GetSourceFile(SourceFileParseOptions options)
    {
        EnsureAlive();
        var handle = sourceFS.GetFileByPath(RootedFilePath.FromValue(options.FileNameValue), PathKey.FromValue(options.PathKeyValue));
        return handle == null ? null : builder!.parseCache.Acquire(ParseCacheKey.New(options, handle.Hash(), handle.Kind()), handle);
    }

    // compilerhost.go:115. Canonical/supplemental files share one cache reference.
    public (SourceFiles Files, Exception? Error) GetContentMappedSourceFiles(SourceFileParseOptions options, Mapper mapper)
    {
        EnsureAlive();
        var handle = sourceFS.GetFileByPath(RootedFilePath.FromValue(options.FileNameValue), PathKey.FromValue(options.PathKeyValue));
        if (handle == null)
            return (default, null);
        var diagnosticLocale = builder!.ctx.Locale;
        var mapperProject = ContentMapperProject();
        if (mapperProject == null)
            return (default, ContentMappers.ErrProjectUnavailable);
        var(identity, error) = mapperProject.Identity(mapper);
        if (error != null)
            return (default, TransformError.NewTransformError(TransformErrorKind.Project, error));
        var transformIdentity = FileContentHash.Hash(ByteStringValue.FromString(identity));
        var key = ParseCaches.ContentMappedKey(options, handle.Hash(), transformIdentity, diagnosticLocale);
        var(files, transformError) = builder.contentMappedParseCache.AcquireOrError(key, () =>
        {
            // The existing external content-mapper transport boundary owns raw Go string text.
            var(transformed, problem) = ContentMappers.TransformAndParse(options, handle.ContentValue().Materialize()!, mapper, mapperProject);
            if (problem != null)
                return (default, problem);
            transformed.Canonical!.Hash = key.Hash;
            Binder.BindSourceFile(transformed.Canonical);
            foreach (var supplemental in transformed.Supplemental)
            {
                supplemental.Hash = key.Hash;
                Binder.BindSourceFile(supplemental);
            }

            return (transformed, null);
        });
        if (transformError == null)
        {
            transformError = ContentMappers.CheckSupplementalFileNameCollisions(files, FS().FileExists);
            if (transformError != null)
            {
                builder.contentMappedParseCache.Deref(key);
                return (default, transformError);
            }
        }

        return (files, transformError);
    }

    // compilerhost.go:156. Initialization after freeze publishes the original nil result once.
    public TypeScript.ContentMapper.IProject? ContentMapperProject()
    {
        contentMapperOnce.Do(() =>
        {
            if (builder == null || builder.contentMapperHost == null)
                return;
            var commandLine = project!.GetCommandLineWithTypingsFiles();
            if ((commandLine.ContentMappers()?.Count ?? 0) == 0)
                return;
            contentMapperProject = builder.contentMapperHost.Project(new() { ConfigFileName = commandLine.ConfigName(), Mappers = commandLine.ContentMappers(), CompilerOptions = commandLine.CompilerOptions() });
        });
        return contentMapperProject;
    }

    // compilerhost.go:175. LogTree nil receivers preserve their original disabled logging behavior.
    public void Trace(Message message, params object[] args) => logger?.Log(message.Localize(TypeScript.Locale.Locales.Default, args));
}

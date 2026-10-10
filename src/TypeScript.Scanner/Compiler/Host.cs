// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/compiler/host.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// ReadFile/ParseSourceFile preserve original byte text and explicit filename roles.
using TypeScript.ContentMapper;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Vfs;
using ConfigParser = TypeScript.TsOptions.TsOptions;

namespace TypeScript.Compiler;
public interface ICompilerHost
{
    IFS FS();
    RootedDirectoryPath DefaultLibraryPath();
    void Trace(Message message, params object[] args);
    SourceFile? GetSourceFile(SourceFileParseOptions options);
    (SourceFiles Files, Exception? Error) GetContentMappedSourceFiles(SourceFileParseOptions options, Mapper mapper);
    IProject? ContentMapperProject();
    ParsedCommandLine? GetResolvedProjectReference(RootedFilePath fileName, PathKey path);
}
public static class CompilerHosts
{
    public static ICompilerHost NewCachedFSCompilerHost(IFS fs, RootedDirectoryPath defaultLibraryPath, IExtendedConfigCache? extendedConfigCache = null, Action<Message, object[]>? trace = null, IProject? contentMapperProject = null) =>
        NewCompilerHost(TypeScript.Vfs.CachedVfs.FS.From(fs), defaultLibraryPath, extendedConfigCache, trace, contentMapperProject);
    public static ICompilerHost NewCompilerHost(IFS fs, RootedDirectoryPath defaultLibraryPath, IExtendedConfigCache? extendedConfigCache = null, Action<Message, object[]>? trace = null, IProject? contentMapperProject = null) =>
        new CompilerHost(fs, defaultLibraryPath, extendedConfigCache, trace ?? ((_, _) => { }), contentMapperProject);
}
internal sealed class CompilerHost(IFS fs, RootedDirectoryPath defaultLibraryPath, IExtendedConfigCache? extendedConfigCache, Action<Message, object[]> trace, IProject? contentMapperProject) : ICompilerHost
{
    public IFS FS() => fs;
    public RootedDirectoryPath DefaultLibraryPath() => defaultLibraryPath;
    public void Trace(Message message, params object[] args) => trace(message, args);
    public SourceFile? GetSourceFile(SourceFileParseOptions options)
    {
        var fileName = options.FileNameIsGoBytes ? RootedFilePath.FromGoString(options.FileName) : RootedFilePath.FromUnicode(options.FileName);
        var (text, ok) = fs.ReadFile(fileName);
        return ok ? Parser.ParseSourceFile(options, GoString.Bytes(text), Core.EnsureScriptKindFromFileName(fileName)) : null;
    }
    public (SourceFiles Files, Exception? Error) GetContentMappedSourceFiles(SourceFileParseOptions options, Mapper mapper)
    {
        if (contentMapperProject is null) return (default, ContentMappers.ErrProjectUnavailable);
        var fileName = options.FileNameIsGoBytes ? RootedFilePath.FromGoString(options.FileName) : RootedFilePath.FromUnicode(options.FileName);
        var (content, ok) = fs.ReadFile(fileName);
        if (!ok) return (default, null);
        var (files, error) = ContentMappers.TransformAndParse(options, content, mapper, contentMapperProject);
        if (error is null) error = ContentMappers.CheckSupplementalFileNameCollisions(files, fs.FileExists);
        return (files, error);
    }
    public IProject? ContentMapperProject() => contentMapperProject;
    public ParsedCommandLine? GetResolvedProjectReference(RootedFilePath fileName, PathKey path) => ConfigParser.GetParsedCommandLineOfConfigFilePath(fileName, path, null, null, fs, extendedConfigCache).Parsed;
}
internal sealed class CompilerResolutionHost(ICompilerHost host, RootedDirectoryPath baseDirectory) : TypeScript.Modules.IResolutionHost
{
    public IFS FS() => host.FS();
    public RootedDirectoryPath GetCurrentDirectory() => baseDirectory;
}

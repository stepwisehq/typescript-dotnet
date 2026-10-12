// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/languageservice.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.LanguageServices.Lsconv;
using TypeScript.LanguageServices.Utilities;
using TypeScript.SourceMap;
using TypeScript.Lsp.Protocol;
using TypeScript.Vfs.VfsMatch;
using CompilerProgram = TypeScript.Compiler.Program;

namespace TypeScript.LanguageServices;
internal sealed partial class LanguageService : TypeScript.SourceMap.Host
{
    private readonly IProjectID projectID;
    private readonly ILanguageServiceHost host;
    private readonly UserPreferences activeConfig;
    private readonly CompilerProgram program;
    private readonly Converters converters;
    private readonly Dictionary<PathKey, DocumentPositionMapper?> documentPositionMappers;
    // internal/ls/languageservice.go:25. Preserve pointer owners and embedded preference value.
    private LanguageService(IProjectID projectID, CompilerProgram program, ILanguageServiceHost host, ByteStringValue activeFile)
    {
        this.projectID = projectID;
        this.host = host;
        this.program = program;
        converters = host.Converters();
        activeConfig = host.GetPreferences(activeFile);
        documentPositionMappers = new();
    }

    internal static LanguageService NewLanguageService(IProjectID projectID, CompilerProgram program, ILanguageServiceHost host, ByteStringValue activeFile) => new(projectID, program, host, activeFile);
    // internal/ls/languageservice.go:42
    internal CompilerProgram GetProgram() => program;
    internal UserPreferences UserPreferences() => activeConfig;
    internal FormatCodeSettings FormatOptions() => activeConfig.FormatCodeSettings;
    private (CompilerProgram Program, SourceFile? File) tryGetProgramAndFile(RootedFilePath fileName)
    {
        var currentProgram = GetProgram();
        return (currentProgram, currentProgram.GetSourceFile(fileName));
    }

    private (CompilerProgram Program, SourceFile File) getProgramAndFile(DocumentUri documentUri)
    {
        var fileName = documentUri.FileName();
        var(currentProgram, file) = tryGetProgramAndFile(fileName);
        if (file == null)
            throw new InvalidOperationException("file not found: " + fileName.AsString());
        return (currentProgram, file);
    }

    // internal/ls/languageservice.go:69. Cache nil mapper results as actual entries too.
    internal DocumentPositionMapper? GetDocumentPositionMapper(RootedFilePath fileName)
    {
        var path = program.PathKeyForFileName(fileName);
        if (!documentPositionMappers.TryGetValue(path, out var mapper))
        {
            mapper = SourceMaps.GetDocumentPositionMapper(this, fileName);
            documentPositionMappers[path] = mapper;
        }

        return mapper;
    }

    public (ByteStringValue Text, bool Ok) ReadFile(RootedFilePath fileName) => host.ReadFile(fileName);
    public CaseSensitivity CaseSensitivity() => host.CaseSensitivity();
    public ECMALineInfo? GetECMALineInfo(RootedFilePath fileName) => host.GetECMALineInfo(fileName);
    // internal/ls/languageservice.go:94. Error identity is supplied by the genuine API counterpart.
    private (View? View, Exception? Error) getPreparedAutoImportView(SourceFile fromFile, Checker typeChecker)
    {
        var registry = host.AutoImportRegistry();
        var registryFile = fromFile.CanonicalSourceFile() ?? fromFile;
        if (!Registry.IsPreparedForImportingFile(registry, RootedFilePath.FromValue(registryFile.FileNameGoValue()), projectID, UserPreferences()))
            return (null, ErrNeedsAutoImports);
        return (View.NewView(registry!, fromFile, projectID, program, typeChecker, UserPreferences().ModuleSpecifierPreferences()), null);
    }

    private View getCurrentAutoImportView(SourceFile fromFile, Checker typeChecker) => View.NewView(host.AutoImportRegistry()!, fromFile, projectID, program, typeChecker, UserPreferences().ModuleSpecifierPreferences());
    internal bool DirectoryExists(RootedDirectoryPath path) => host.DirectoryExists(path);
    internal Slice<RootedFilePath> ReadDirectory(RootedDirectoryPath path, Slice<ByteStringValue> extensions, Slice<ByteStringValue> includes) => host.ReadDirectory(path, extensions, default, includes, VfsMatch.UnlimitedDepth);
    internal Slice<ByteStringValue> GetDirectories(RootedDirectoryPath path) => host.GetDirectories(path);
}

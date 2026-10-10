// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/outputpaths/outputpaths.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.OutputPaths;
public interface IOutputPathsHost
{
    RootedDirectoryPath CommonSourceDirectory();
    List<string>? ContentMapperExtensions();
    CaseSensitivity CaseSensitivity();
}

public sealed class OutputFilePaths
{
    internal RootedFilePath jsFilePath, sourceMapFilePath, declarationFilePath, declarationMapPath;
    public RootedFilePath JsFilePath() => jsFilePath;
    public RootedFilePath SourceMapFilePath() => sourceMapFilePath;
    public RootedFilePath DeclarationFilePath() => declarationFilePath;
    public RootedFilePath DeclarationMapPath() => declarationMapPath;
}

public struct ForceEmitPaths
{
    public bool Dts, Js, DeclarationMap;
}

public static partial class OutputPaths
{
    // outputpaths.go:43–76. Enumeration computes names only; it never emits.
    public static OutputFilePaths GetOutputPathsFor(SourceFile sourceFile, CompilerOptions options, IOutputPathsHost host, ForceEmitPaths force)
    {
        var fileName = RootedFilePath.FromGoString(sourceFile.FileNameGoBytes());
        var ownOutputFilePath = getOwnEmitOutputFilePathForFileName(fileName, options, host, GetOutputExtensionForFileName(fileName, options.Jsx));
        bool isJson = Ast.IsJsonSourceFile(sourceFile);
        bool sameLocation = isJson && host.CaseSensitivity().CompareFilePaths(fileName, ownOutputFilePath) == 0;
        var paths = new OutputFilePaths();
        if (sourceFile.ContentMapper() == "" && (force.Js || options.EmitDeclarationOnly != Tristate.True) && !sameLocation)
        {
            paths.jsFilePath = ownOutputFilePath;
            if (!isJson)
                paths.sourceMapFilePath = GetSourceMapFilePath(paths.jsFilePath, options);
        }

        if (force.Dts || options.GetEmitDeclarations() && !isJson)
        {
            paths.declarationFilePath = getDeclarationEmitOutputFilePathForFileName(fileName, options, host);
            if (options.GetAreDeclarationMapsEnabled() || force.DeclarationMap && options.DeclarationMap == Tristate.True)
                paths.declarationMapPath = paths.declarationFilePath.AppendSuffix(".map");
        }

        return paths;
    }

    public static bool ForEachEmittedFile(IOutputPathsHost host, CompilerOptions options, Func<OutputFilePaths, SourceFile, bool> action, List<SourceFile>? sourceFiles, bool forceDtsEmit)
    {
        foreach (var file in sourceFiles ?? [])
            if (action(GetOutputPathsFor(file, options, host, new() { Dts = forceDtsEmit }), file))
                return true;
        return false;
    }

    public static RootedFilePath GetOutputJSFileName(RootedFilePath inputFileName, CompilerOptions options, IOutputPathsHost host)
    {
        if (options.EmitDeclarationOnly == Tristate.True || isContentMappedFileName(inputFileName, host))
            return default;
        var outputFileName = GetOutputJSFileNameWorker(inputFileName, options, host);
        if (!outputFileName.ExtensionIs(TsPath.ExtensionJson) || host.CaseSensitivity().CompareFilePaths(inputFileName, outputFileName) != 0)
            return outputFileName;
        return default;
    }

    internal static bool isContentMappedFileName(RootedFilePath fileName, IOutputPathsHost host) => fileName.LongestExtension(host.ContentMapperExtensions(), host.CaseSensitivity()) != "";
    public static RootedFilePath GetOutputJSFileNameWorker(RootedFilePath inputFileName, CompilerOptions options, IOutputPathsHost host) => getOutputFileNameWithoutChangingExtension(inputFileName, options.OutDir, host).ChangeExtension(GetOutputExtensionForFileName(inputFileName, options.Jsx));
    public static RootedFilePath GetOutputDeclarationFileNameWorker(RootedFilePath inputFileName, CompilerOptions options, IOutputPathsHost host)
    {
        var directory = options.DeclarationDir;
        if (directory.AsString() == "")
            directory = options.OutDir;
        return ChangeToDeclarationExtension(getOutputFileNameWithoutChangingExtension(inputFileName, directory, host), host);
    }

    public static string GetOutputExtension(string fileName, JsxEmit jsx)
    {
        if (TsPath.FileExtensionIs(fileName, TsPath.ExtensionJson))
            return TsPath.ExtensionJson;
        if (jsx == JsxEmit.Preserve && TsPath.FileExtensionIsOneOf(fileName, new[] { TsPath.ExtensionJsx, TsPath.ExtensionTsx }))
            return TsPath.ExtensionJsx;
        if (TsPath.FileExtensionIsOneOf(fileName, new[] { TsPath.ExtensionMts, TsPath.ExtensionMjs }))
            return TsPath.ExtensionMjs;
        if (TsPath.FileExtensionIsOneOf(fileName, new[] { TsPath.ExtensionCts, TsPath.ExtensionCjs }))
            return TsPath.ExtensionCjs;
        return TsPath.ExtensionJs;
    }

    public static string GetOutputExtensionForFileName(RootedFilePath fileName, JsxEmit jsx) => GetOutputExtension(fileName.AsString(), jsx);
    internal static RootedFilePath getDeclarationEmitOutputFilePathForFileName(RootedFilePath file, CompilerOptions options, IOutputPathsHost host)
    {
        var outputDirectory = options.DeclarationDir.AsString() != "" ? options.DeclarationDir : options.OutDir;
        return ChangeToDeclarationExtension(outputDirectory.AsString() != "" ? GetSourceFileNameInNewDir(file, outputDirectory, host.CommonSourceDirectory(), host.CaseSensitivity()) : file, host);
    }

    public static RootedFilePath ChangeToDeclarationExtension(RootedFilePath path, IOutputPathsHost host)
    {
        var extension = path.LongestExtension(host.ContentMapperExtensions(), CaseSensitivity.CaseSensitive);
        if (extension != "")
            return path.RemoveExtension(extension).AppendSuffix(".d" + extension + ".ts");
        var pathWithoutExtension = path.RemoveFileExtension();
        if (pathWithoutExtension.AsString() == path.AsString())
        {
            extension = path.AnyExtension(null, CaseSensitivity.CaseSensitive);
            if (extension != "")
                pathWithoutExtension = path.RemoveExtension(extension);
        }

        return pathWithoutExtension.AppendSuffix(path.DeclarationEmitExtension());
    }

    internal static RootedFilePath getOutputFileNameWithoutChangingExtension(RootedFilePath inputFileName, RootedDirectoryPath outputDirectory, IOutputPathsHost host)
    {
        if (outputDirectory.AsString() != "")
        {
            var(relativePath, ok) = host.CaseSensitivity().RelativePathFromDirectory(host.CommonSourceDirectory(), inputFileName);
            if (!ok)
                return inputFileName;
            return outputDirectory.ResolveRelativeFile(relativePath);
        }

        return inputFileName;
    }

    public static RootedFilePath GetSourceFileNameInNewDir(RootedFilePath fileName, RootedDirectoryPath newDirectory, RootedDirectoryPath commonSourceDirectory, CaseSensitivity caseSensitivity)
    {
        if (fileName.AsPath() == commonSourceDirectory.AsPath())
            return fileName;
        var(relativePath, ok) = caseSensitivity.RelativeFilePathFromDirectory(commonSourceDirectory, fileName);
        return ok ? newDirectory.ResolveRelativeFile(relativePath) : fileName;
    }

    internal static RootedFilePath getOwnEmitOutputFilePathForFileName(RootedFilePath fileName, CompilerOptions options, IOutputPathsHost host, string extension)
    {
        var output = options.OutDir.AsString() != "" ? GetSourceFileNameInNewDir(fileName, options.OutDir, host.CommonSourceDirectory(), host.CaseSensitivity()) : fileName;
        return output.RemoveFileExtension().AppendSuffix(extension);
    }

    public static RootedFilePath GetSourceMapFilePath(RootedFilePath jsFilePath, CompilerOptions options) => options.SourceMap == Tristate.True && options.InlineSourceMap != Tristate.True ? jsFilePath.AppendSuffix(".map") : default;
    public static RootedFilePath GetBuildInfoFileName(CompilerOptions options, CaseSensitivity caseSensitivity)
    {
        if (!options.IsIncremental() && options.Build != Tristate.True)
            return default;
        if (options.TsBuildInfoFile.AsString() != "")
            return options.TsBuildInfoFile;
        if (options.ConfigFilePath.AsString() == "")
            return default;
        var configFileName = options.ConfigFilePath;
        RootedFilePath buildInfoFileName;
        if (options.OutDir.AsString() != "")
        {
            if (options.RootDir.AsString() != "")
            {
                var(relativePath, ok) = caseSensitivity.RelativePathFromDirectory(options.RootDir, configFileName);
                buildInfoFileName = ok ? options.OutDir.ResolveRelativeFile(relativePath) : configFileName;
            }
            else
                buildInfoFileName = options.OutDir.ResolveFile(configFileName.BaseName());
        }
        else
            buildInfoFileName = configFileName;
        return buildInfoFileName.RemoveFileExtension().AppendSuffix(TsPath.ExtensionTsBuildInfo);
    }
}

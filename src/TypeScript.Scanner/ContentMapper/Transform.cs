// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/contentmapper/transform.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Globalization;
using TypeScript.Scanner;
using TypeScript.SpanMap;

namespace TypeScript.ContentMapper;
public struct SourceFiles
{
    public SourceFile? Canonical;
    public Slice<SourceFile> Supplemental;
}

public static partial class ContentMappers
{
    // Explicit managed parse-option filename role; the source API path remains bytes.
    private static RootedFilePath fileNameFromParseOptions(SourceFileParseOptions options) =>
        options.FileNameIsGoBytes ? RootedFilePath.FromValue(options.FileNameValue.IsNull ? default : options.FileNameValue) :
            RootedFilePath.FromUnicode(options.FileName ?? "");
    public static (SourceFiles Files, Exception? Error) TransformAndParse(SourceFileParseOptions parseOptions, string content, Mapper mapper, IProject project)
    {
        var(identity, error) = project.Identity(mapper);
        if (error is not null)
            return (default, TransformError.NewTransformError(TransformErrorKind.Project, error));
        var(result, transformError) = project.Transform(mapper, new() { FileName = fileNameFromParseOptions(parseOptions), Content = content });
        if (transformError is not null)
            return (default, transformError);
        return ParseResult(parseOptions, content, mapper, identity, result);
    }

    public static (SourceFiles Files, Exception? Error) ParseResult(SourceFileParseOptions parseOptions, string content, Mapper mapper, string transformIdentity, Result result)
    {
        if (result.Mappings is null)
            return (default, TransformError.NewTransformError(TransformErrorKind.Mappings, null));
        if (result.Mappings.Validate(result.Text, content)is { } problem)
            return (default, problem);
        string extension = result.VirtualExtension;
        if (!IsSupportedVirtualExtension(extension))
            return (default, TransformError.NewTransformError(TransformErrorKind.Response, null));
        var baseOptions = parseOptions;
        var virtualFileName = fileNameFromParseOptions(baseOptions).AppendSuffix(extension);
        parseOptions = baseOptions;
        if (isModuleVirtualExtension(extension))
            parseOptions = parseOptions with
            {
                ExternalModuleIndicatorOptions = parseOptions.ExternalModuleIndicatorOptions with
                {
                    Force = true
                }
            };
        var source = Parser.ParseSourceFileOwned(parseOptions, ByteStringView.FromGoBytes(result.Text), Core.GetScriptKindFromFileName(virtualFileName));
        if ((result.Diagnostics?.Count ?? 0) > 0)
        {
            foreach (var diagnostic in result.Diagnostics!)
                diagnostic.SetFile(source);
            source.SetDiagnostics(source.Diagnostics().Append(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(result.Diagnostics)));
        }

        var files = new SourceFiles
        {
            Canonical = source,
            Supplemental = Slice<SourceFile>.Make(0, result.Supplemental?.Count ?? 0)
        };
        for (int i = 0; i < (result.Supplemental?.Count ?? 0); i++)
        {
            var supplemental = result.Supplemental![i];
            if (supplemental.Mappings is null)
                return (default, TransformError.NewTransformError(TransformErrorKind.Mappings, null));
            if (supplemental.Mappings.Validate(supplemental.Text, content)is { } supplementalProblem)
                return (default, supplementalProblem);
            if (!IsSupportedVirtualExtension(supplemental.VirtualExtension))
                return (default, TransformError.NewTransformError(TransformErrorKind.Response, null));
            string suffix = "." + i.ToString(CultureInfo.InvariantCulture) + supplemental.VirtualExtension;
            var supplementalName = fileNameFromParseOptions(baseOptions).AppendSuffix(suffix);
            var supplementalOptions = baseOptions with
            {
                FileNameValue = baseOptions.FileNameIsGoBytes ? supplementalName.AsValue() : ByteStringValue.FromString(supplementalName.ToUnicode()),
                PathKeyValue = PathKey.FromValue(parseOptions.PathKeyValue).AppendCanonicalSuffix(suffix).AsValue()
            };
            if (isModuleVirtualExtension(supplemental.VirtualExtension))
                supplementalOptions = supplementalOptions with
                {
                    ExternalModuleIndicatorOptions = supplementalOptions.ExternalModuleIndicatorOptions with
                    {
                        Force = true
                    }
                };
            files.Supplemental = files.Supplemental.Append(Parser.ParseSourceFileOwned(supplementalOptions, ByteStringView.FromGoBytes(supplemental.Text), Core.GetScriptKindFromFileName(supplementalName)));
        }

        string mapperIdentity = mapper.Identity();
        source.SetContentMapperInfo(new() { ContentMapper = mapperIdentity, TransformIdentity = transformIdentity, ParseOptions = baseOptions, VirtualFileName = virtualFileName, OriginalText = content, SpanMap = result.Mappings, DiagnosticDirectives = result.DiagnosticDirectives is null ? default : Slice<MappedDiagnosticDirective>.FromArray(result.DiagnosticDirectives.ToArray()), SupplementalSourceFiles = files.Supplemental });
        for (int i = 0; i < files.Supplemental.Count; i++)
        {
            var file = files.Supplemental[i];
            var supplemental = result.Supplemental![i];
            file.SetContentMapperInfo(new() { ContentMapper = mapperIdentity, TransformIdentity = transformIdentity, ParseOptions = baseOptions, VirtualFileName = RootedFilePath.FromValue(file.FileNameGoValue()), OriginalText = content, SpanMap = supplemental.Mappings, DiagnosticDirectives = supplemental.DiagnosticDirectives is null ? default : Slice<MappedDiagnosticDirective>.FromArray(supplemental.DiagnosticDirectives.ToArray()), CanonicalSourceFile = source });
        }

        return (files, null);
    }

    internal static bool isModuleVirtualExtension(string extension) => extension is TsPath.ExtensionMts or TsPath.ExtensionCts or TsPath.ExtensionMjs or TsPath.ExtensionCjs;
    public static Exception? CheckSupplementalFileNameCollisions(SourceFiles files, Func<RootedFilePath, bool> fileExists)
    {
        foreach (var file in files.Supplemental)
        {
            var filename = RootedFilePath.FromValue(file.FileNameGoValue());
            if (fileExists(filename))
                return new SupplementalFileCollisionError
                {
                    FileName = filename
                };
        }

        return null;
    }
}

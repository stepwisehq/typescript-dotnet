// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/lsp/server.go:2511–2615.
using TypeScript.ContentMapper;
using TypeScript.Json;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;
using TypeScript.Scanner;
using TsOptionFunctions = TypeScript.TsOptions.TsOptions;

namespace TypeScript.Lsp;
internal sealed partial class Server
{
    private (ProjectInfoResult? Value, Exception? Error) HandleProjectInfo(ProtocolRequestContext context, ProjectInfoParams parameters, RequestMessage request)
    {
        var(project, _, _, error) = session!.GetLanguageServiceAndProjectsForFile(context, parameters.TextDocument.Uri);
        if (error is not null)
            return (null, error);
        return (new ProjectInfoResult { ConfigFilePath = project is not null && project.Kind == ProjectKind.Configured ? project.ConfigFileName().AsValue() : default }, null);
    }

    private (Null Value, Exception? Error) HandleSetContentMapperContributions(ProtocolRequestContext context, SetContentMapperContributionsParams parameters, RequestMessage request)
    {
        var(contributions, error) = ParseContentMapperContributions(parameters.Contributions);
        if (error is not null)
            return (default, error);
        var documents = Core.MapSlice(parameters.OpenDocuments, static document => document.Uri);
        session!.SetContentMapperContributions(context, contributions, documents);
        return (default, null);
    }

    internal static (ContentMapperContributions Value, Exception? Error) ParseContentMapperContributions(Slice<ContentMapperContribution?> values)
    {
        var result = new ContentMapperContributions();
        HashSet<ByteStringKey>? claimedExtensions = null;
        for (int index = 0; index < values.Length; index++)
        {
            var value = values[index];
            if (value is null || value.ContributorId.Length == 0)
                return (result, new Exception("content mapper contribution requires a contributorId"));
            var identity = ByteStringValue.Concat(value.ContributorId, ByteStringValue.FromString("[" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]"));
            var valid = Slice<ByteStringValue>.Make(0, value.Extensions.Length);
            foreach (var extension in value.Extensions)
            {
                if (!IsValidContributedContentMapperExtension(extension))
                    return (result, new Exception("content mapper contribution " + QuoteContribution(identity) + " has invalid extension " + QuoteContribution(extension)));
                valid = valid.Append(extension);
            }

            if (value.InferredProjectContribution is not { } inferred)
                continue;
            var manifest = inferred.Manifest!;
            if (manifest.Name.Length == 0 || manifest.Exec.Length == 0)
                return (result, new Exception("content mapper contribution " + QuoteContribution(identity) + " requires a manifest name and exec"));
            foreach (var option in manifest.CompilerOptions ?? default)
                if (TsOptionFunctions.CommandLineCompilerOptionsMap.Get(option.Materialize()!)is null)
                    return (result, new Exception("content mapper contribution " + QuoteContribution(identity) + " requests unknown compiler option " + QuoteContribution(option)));
            foreach (var extension in valid)
            {
                claimedExtensions ??= new();
                if (!claimedExtensions.Add(ByteStringKey.FromValue(GoStrings.ToLower(extension))))
                    return (result, new Exception("content mapper contributions both claim extension " + QuoteContribution(extension)));
                result.Extensions = result.Extensions.Append(extension);
            }

            var options = new byte[]
            {
                (byte)'{',
                (byte)'}'
            };
            if (inferred.Options is { } rawOptions)
            {
                try
                {
                    // Existing JsonValue requires a complete byte[] owner, so this
                    // byte-header boundary copies the encoded logical range once.
                    options = ProtocolCodec.Marshal(ProtocolCodec.ContentMapperContributionOptions, rawOptions).ByteSpan.ToArray();
                }
                catch (System.Text.Json.JsonException)
                {
                    return (result, new Exception("content mapper contribution " + QuoteContribution(identity) + " has invalid options"));
                }
            }

            // Existing mapper metadata stores raw Go strings/List headers; these
            // are raw byte projections, not Unicode display decoding.
            var mapper = new Mapper
            {
                Package = identity.Materialize()!,
                Extensions = ContributionStringList(valid),
                Options = new JsonValue(options),
                Name = manifest.Name.Materialize()!,
                Version = (manifest.Version ?? default).Materialize() ?? "",
                Exec = ContributionStringList(manifest.Exec),
                CompilerOptions = manifest.CompilerOptions is { } compilerOptions ? ContributionStringList(compilerOptions) : null,
                DynamicConfig = manifest.DynamicConfig ?? false,
                ContributionID = identity.Materialize()!
            };
            if (manifest.Cwd is { } cwd)
            {
                if (!TsPath.PathIsAbsolute(cwd))
                    return (result, new Exception("content mapper contribution " + QuoteContribution(identity) + " has non-absolute cwd"));
                mapper.PackageDirectory = TsPath.RootedDirectoryPathFromAbsolute(cwd);
            }

            result.Mappers = result.Mappers.Append(mapper);
        }

        result.Extensions.AsSpan().Sort(static (left, right) => ByteStringKey.FromValue(left).CompareTo(ByteStringKey.FromValue(right)));
        return (result, null);
    }

    // One required Go clone/mapped List boundary; no intermediate Slice/array.
    private static List<string>? ContributionStringList(Slice<ByteStringValue> values)
    {
        if (values.IsNil)
            return null;
        var result = new List<string>(values.Length);
        foreach (var value in values)
            result.Add(value.Materialize()!);
        return result;
    }

    private static string QuoteContribution(ByteStringValue value) => GoString.ToString(GoQuote.Quote(value.Materialize()!));
    private static bool IsValidContributedContentMapperExtension(ByteStringValue extension)
    {
        if (extension.Length <= 1 || extension[0] != '.' || !TsPath.GetAnyExtensionFromPath(ByteStringValue.Concat(ByteStringValue.FromString("file"), extension), (IEnumerable<string>? )null, CaseSensitivity.CaseSensitive).EqualsValue(extension))
            return false;
        foreach (var group in TsPath.AllSupportedExtensionsWithJson)
            foreach (var native in group)
                if (UnicodeCase.EqualFold(ByteStringValue.FromString(native), extension))
                    return false;
        return true;
    }
}

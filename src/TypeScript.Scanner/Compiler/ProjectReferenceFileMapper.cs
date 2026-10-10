// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/compiler/projectreferencefilemapper.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Typed paths retain original Go bytes.
using TypeScript.Collections;
using TypeScript.Modules;
using TypeScript.Scanner;
using TypeScript.TsOptions;

namespace TypeScript.Compiler;
internal class ProjectReferenceFileMapper
{
    internal ParsedCommandLine config = null!;
    internal bool useSourceOfProjectReference;
    internal HashSet<PathKey> dtsDirectories = [];
    internal Dictionary<PathKey, ParsedCommandLine?> configToProjectReference = [];
    internal Dictionary<PathKey, List<PathKey>?> referencesInConfigFile = [];
    internal Dictionary<PathKey, SourceOutputAndProjectReference> sourceToProjectReference = [];
    internal Dictionary<PathKey, SourceOutputAndProjectReference> outputDtsToProjectReference = [];
    internal SyncMap<PathKey, SourceOutputAndProjectReference?> realpathDtsToSource = new();
    // Go's builder embeds a pointer to this data object. C# inheritance is used
    // during construction; publish a base object with the same shared stores so
    // Program queries do not retain builder dispatch or its construction host.
    internal ProjectReferenceFileMapper PublishData() => new()
    {
        config = config,
        useSourceOfProjectReference = useSourceOfProjectReference,
        dtsDirectories = dtsDirectories,
        configToProjectReference = configToProjectReference,
        referencesInConfigFile = referencesInConfigFile,
        sourceToProjectReference = sourceToProjectReference,
        outputDtsToProjectReference = outputDtsToProjectReference,
        realpathDtsToSource = realpathDtsToSource
    };
    internal IResolutionHost ResolutionHost(IResolutionHost host) => useSourceOfProjectReference && outputDtsToProjectReference.Count != 0 ? new ProjectReferenceDtsFakingHost(host, this) : host;
    internal PathKey RootConfigPath() => config.ConfigFile is null ? default : PathKey.FromGoString(config.ConfigFile.SourceFile.PathKey());
    internal virtual (RootedFilePath File, PathKey Path) GetParseFileRedirect(RootedFilePath file, PathKey path)
    {
        if (useSourceOfProjectReference)
        {
            var source = GetProjectReferenceFromOutputDts(path) ?? realpathDtsToSource.Load(path).Value;
            if (source is not null) return (source.Source, source.SourcePath);
        }
        else
        {
            var output = GetProjectReferenceFromSource(path);
            if (output is not null && output.OutputDts != default) return (output.OutputDts, output.OutputDtsPath);
        }
        return (default, default);
    }
    internal List<ParsedCommandLine?>? GetResolvedProjectReferences()
    {
        if (!referencesInConfigFile.TryGetValue(RootConfigPath(), out var refs)) return null;
        var result = new List<ParsedCommandLine?>(refs?.Count ?? 0);
        foreach (var reference in refs ?? []) result.Add(configToProjectReference.GetValueOrDefault(reference));
        return result;
    }
    internal SourceOutputAndProjectReference? GetProjectReferenceFromSource(PathKey path) => sourceToProjectReference.GetValueOrDefault(path);
    internal SourceOutputAndProjectReference? GetProjectReferenceFromOutputDts(PathKey path) => outputDtsToProjectReference.GetValueOrDefault(path);
    internal bool IsSourceFromProjectReference(PathKey path) => useSourceOfProjectReference && GetProjectReferenceFromSource(path) is not null;
    internal CompilerOptions GetCompilerOptionsForFile(SourceFile file) => GetCompilerOptionsForFile(RootedFilePath.FromGoString(file.FileNameGoBytes()), PathKey.FromGoString(file.PathKey()));
    internal virtual CompilerOptions GetCompilerOptionsForFile(RootedFilePath file, PathKey path) => DefaultResolver.GetCompilerOptionsWithRedirect(config.CompilerOptions(), GetRedirectForResolution(file, path).Config);
    internal ParsedCommandLine? GetRedirectParsedCommandLineForResolution(SourceFile file) => GetRedirectForResolution(file).Config;
    internal (ParsedCommandLine? Config, RootedFilePath Path) GetRedirectForResolution(SourceFile file) => GetRedirectForResolution(RootedFilePath.FromGoString(file.FileNameGoBytes()), PathKey.FromGoString(file.PathKey()));
    internal virtual (ParsedCommandLine? Config, RootedFilePath Path) GetRedirectForResolution(RootedFilePath file, PathKey path)
    {
        var output = GetProjectReferenceFromSource(path);
        if (output is not null) return (output.Resolved, output.Source);
        var result = GetProjectReferenceFromOutputDts(path);
        if (result is not null) return (result.Resolved, result.Source);
        var real = realpathDtsToSource.Load(path).Value;
        return real is not null ? (real.Resolved, real.Source) : (null, file);
    }
    internal (ParsedCommandLine? Config, bool Ok) GetResolvedReferenceFor(PathKey path)
    {
        bool ok = configToProjectReference.TryGetValue(path, out var value);
        return (value, ok);
    }
    internal bool RangeResolvedProjectReference(Func<PathKey, ParsedCommandLine?, ParsedCommandLine, int, bool> callback)
    {
        if ((config.ProjectReferences()?.Count ?? 0) == 0) return false;
        var seen = new HashSet<PathKey> { RootConfigPath() };
        return RangeResolvedReferenceWorker(referencesInConfigFile.GetValueOrDefault(RootConfigPath()), callback, config, seen);
    }
    private bool RangeResolvedReferenceWorker(List<PathKey>? references, Func<PathKey, ParsedCommandLine?, ParsedCommandLine, int, bool> callback, ParsedCommandLine parent, HashSet<PathKey> seen)
    {
        for (int i = 0; i < (references?.Count ?? 0); i++)
        {
            var path = references![i];
            if (!seen.Add(path)) continue;
            var child = configToProjectReference.GetValueOrDefault(path);
            if (!callback(path, child, parent, i)) return false;
            if (!RangeResolvedReferenceWorker(referencesInConfigFile.GetValueOrDefault(path), callback, child!, seen)) return false;
        }
        return true;
    }
    internal bool RangeResolvedProjectReferenceInChildConfig(ParsedCommandLine? childConfig, Func<PathKey, ParsedCommandLine?, ParsedCommandLine, int, bool> callback)
    {
        if (childConfig?.ConfigFile is null) return false;
        var path = PathKey.FromGoString(childConfig.ConfigFile.SourceFile.PathKey());
        return RangeResolvedReferenceWorker(referencesInConfigFile.GetValueOrDefault(path), callback, config, new() { path });
    }
}
internal sealed class ProjectReferenceFileMapperBuilder : ProjectReferenceFileMapper
{
    internal IResolutionHost host = null!;
    internal override (RootedFilePath File, PathKey Path) GetParseFileRedirect(RootedFilePath file, PathKey path)
    {
        if (useSourceOfProjectReference && GetProjectReferenceFromOutputDts(path) is null) ResolveSymlink(file, path);
        return base.GetParseFileRedirect(file, path);
    }
    internal override (ParsedCommandLine? Config, RootedFilePath Path) GetRedirectForResolution(RootedFilePath file, PathKey path)
    {
        if (GetProjectReferenceFromSource(path) is null && GetProjectReferenceFromOutputDts(path) is null) ResolveSymlink(file, path);
        return base.GetRedirectForResolution(file, path);
    }
    internal override CompilerOptions GetCompilerOptionsForFile(RootedFilePath file, PathKey path) => DefaultResolver.GetCompilerOptionsWithRedirect(config.CompilerOptions(), GetRedirectForResolution(file, path).Config);
    private void ResolveSymlink(RootedFilePath file, PathKey path)
    {
        if (realpathDtsToSource.Load(path).Ok) return;
        if ((config.ResolvedProjectReferencePaths()?.Count ?? 0) != 0 && config.CompilerOptions().PreserveSymlinks == Tristate.True)
        {
            if (!file.ContainsLowercaseDirectorySequence("/node_modules/")) realpathDtsToSource.Store(path, null);
            else
            {
                var real = host.FS().CaseSensitivity().PathKey(host.FS().Realpath(file.AsPath()));
                realpathDtsToSource.Store(path, real == path ? null : GetProjectReferenceFromOutputDts(real));
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/module/staticresolver.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Runtime.CompilerServices;
using TypeScript.Scanner;

namespace TypeScript.Modules;
public sealed class StaticResolutionEntry
{
    public string ModuleName = "";
    public RootedDirectoryPath ContainingDirectory;
    public StrongBox<ResolutionMode>? ResolutionMode;
    public ResolvedModule? Result;
}

internal readonly record struct StaticResolutionKey(string ModuleName, PathKey Directory = default, ResolutionMode Mode = default, bool HasDirectory = false, bool HasMode = false);
public sealed class StaticResolutions
{
    internal bool fallbackToResolver;
    private readonly Dictionary<StaticResolutionKey, ResolvedModule?> entries = new();
    internal RootedDirectoryPath currentDirectory;
    private CaseSensitivity caseSensitivity;
    public static (StaticResolutions? Result, Exception? Error) NewStaticResolutions(List<StaticResolutionEntry>? values, bool fallbackToResolver, RootedDirectoryPath currentDirectory, CaseSensitivity caseSensitivity)
    {
        var result = new StaticResolutions
        {
            fallbackToResolver = fallbackToResolver,
            currentDirectory = currentDirectory,
            caseSensitivity = caseSensitivity
        };
        foreach (var entry in values ?? [])
        {
            if (entry.ModuleName == "")
                return (null, new InvalidOperationException("module name is empty"));
            var key = new StaticResolutionKey(entry.ModuleName);
            if (entry.ContainingDirectory != default)
                key = key with
                {
                    Directory = caseSensitivity.PathKey(entry.ContainingDirectory.AsPath()),
                    HasDirectory = true
                };
            if (entry.ResolutionMode is not null)
                key = key with
                {
                    Mode = entry.ResolutionMode.Value,
                    HasMode = true
                };
            if (!result.entries.TryAdd(key, entry.Result))
                return (null, new InvalidOperationException("duplicate static module resolution for " + GoQuote.Quote(entry.ModuleName)));
        }

        return (result, null);
    }

    internal (ResolvedModule? Result, bool Found) Lookup(string moduleName, RootedDirectoryPath containingDirectory, ResolutionMode resolutionMode)
    {
        var directory = caseSensitivity.PathKey(containingDirectory.AsPath());
        StaticResolutionKey[] keys = [new(moduleName, directory, resolutionMode, true, true), new(moduleName, directory, HasDirectory: true), new(moduleName, Mode: resolutionMode, HasMode: true), new(moduleName)];
        foreach (var key in keys)
            if (entries.TryGetValue(key, out var result))
                return (result, true);
        return (null, false);
    }
}

public sealed class StaticResolver : IResolver
{
    private readonly IResolver fallback;
    private readonly StaticResolutions resolutions;
    private StaticResolver(IResolver fallback, StaticResolutions resolutions)
    {
        this.fallback = fallback;
        this.resolutions = resolutions;
    }

    public static StaticResolver NewStaticResolver(IResolver fallback, StaticResolutions resolutions) => new(fallback, resolutions);
    public RootedDirectoryPath BaseDirectory() => fallback.BaseDirectory();
    public (ResolvedModule? Result, List<DiagAndArgs>? Traces, Exception? Error) ResolveModuleName(string moduleName, RootedFilePath containingFile, ResolutionMode resolutionMode, IResolvedProjectReference? redirectedReference = null) => resolveModuleName(moduleName, containingFile, containingFile.Directory(), resolutionMode, redirectedReference);
    public (ResolvedModule? Result, List<DiagAndArgs>? Traces, Exception? Error) ResolveModuleNameFromDirectory(string moduleName, RootedDirectoryPath containingDirectory, ResolutionMode resolutionMode)
    {
        var(result, found) = resolutions.Lookup(moduleName, containingDirectory, resolutionMode);
        if (found)
            return (result, null, null);
        return resolutions.fallbackToResolver ? fallback.ResolveModuleNameFromDirectory(moduleName, containingDirectory, resolutionMode) : (null, null, null);
    }

    private (ResolvedModule? Result, List<DiagAndArgs>? Traces, Exception? Error) resolveModuleName(string moduleName, RootedFilePath containingFile, RootedDirectoryPath containingDirectory, ResolutionMode resolutionMode, IResolvedProjectReference? redirectedReference)
    {
        var(result, found) = resolutions.Lookup(moduleName, containingDirectory, resolutionMode);
        if (found)
            return (result, null, null);
        return resolutions.fallbackToResolver ? fallback.ResolveModuleName(moduleName, containingFile, resolutionMode, redirectedReference) : (null, null, null);
    }

    public (ResolvedTypeReferenceDirective? Result, List<DiagAndArgs>? Traces) ResolveTypeReferenceDirective(string typeReferenceDirectiveName, RootedFilePath containingFile, ResolutionMode resolutionMode, IResolvedProjectReference? redirectedReference = null) => fallback.ResolveTypeReferenceDirective(typeReferenceDirectiveName, containingFile, resolutionMode, redirectedReference);
    public ResolutionData GetResolutionData() => fallback.GetResolutionData();
}

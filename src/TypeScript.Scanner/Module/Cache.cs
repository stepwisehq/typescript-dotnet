// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/module/cache.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Collections;
using TypeScript.PackageJson;
using TypeScript.Scanner;

namespace TypeScript.Modules;
internal readonly record struct ModuleResolutionCacheKey(RootedDirectoryPath ContainingDirectory, ModeAwareCacheKey NameAndMode, RootedFilePath RedirectConfigName);
internal sealed class ModuleResolutionCache
{
    private readonly Dictionary<ModuleResolutionCacheKey, ResolvedModule?> cache = new();
    private readonly object gate = new();
    internal int Size
    {
        get
        {
            lock (gate)
                return cache.Count;
        }
    }

    internal (ResolvedModule? Value, bool Found) Get(ModuleResolutionCacheKey key)
    {
        lock (gate)
            return (cache.GetValueOrDefault(key), cache.ContainsKey(key));
    }

    internal void Set(ModuleResolutionCacheKey key, ResolvedModule? value)
    {
        // Source LoadOrStore: the first published value, including null, wins.
        lock (gate)
            cache.TryAdd(key, value);
    }
}

internal readonly record struct TypeRefDirectiveResolutionCacheKey(RootedDirectoryPath ContainingDirectory, ModeAwareCacheKey NameAndMode, RootedFilePath RedirectConfigName, bool FromInferredTypesContainingFile);
internal sealed class TypeRefDirectiveResolutionCache
{
    private readonly Dictionary<TypeRefDirectiveResolutionCacheKey, ResolvedTypeReferenceDirective?> cache = new();
    private readonly object gate = new();
    internal int Size
    {
        get
        {
            lock (gate)
                return cache.Count;
        }
    }

    internal (ResolvedTypeReferenceDirective? Value, bool Found) Get(TypeRefDirectiveResolutionCacheKey key)
    {
        lock (gate)
            return (cache.GetValueOrDefault(key), cache.ContainsKey(key));
    }

    internal void Set(TypeRefDirectiveResolutionCacheKey key, ResolvedTypeReferenceDirective? value)
    {
        // Source Store replaces a prior value; this intentionally differs from modules.
        lock (gate)
            cache[key] = value;
    }
}

internal sealed class ParsedPatternsCache
{
    private readonly Dictionary<OrderedMap<string, List<string>?>, ParsedPatterns> cache = new(ReferenceEqualityComparer.Instance);
    private readonly object gate = new();
    private ParsedPatterns? nilKey;
    internal int Size
    {
        get
        {
            lock (gate)
                return cache.Count + (nilKey is null ? 0 : 1);
        }
    }

    internal ParsedPatterns Get(OrderedMap<string, List<string>?>? mappings)
    {
        lock (gate)
        {
            if (mappings is null && nilKey is not null)
                return nilKey;
            if (mappings is not null && cache.TryGetValue(mappings, out var existing))
                return existing;
        }

        // Original computation is outside SyncMap publication; concurrent misses
        // may compute independently, but every caller receives the published winner.
        var patterns = DefaultResolver.TryParsePatterns(mappings);
        lock (gate)
        {
            if (mappings is null)
                return nilKey ??= patterns;
            if (cache.TryGetValue(mappings, out var existing))
                return existing;
            cache.Add(mappings, patterns);
            return patterns;
        }
    }
}

public sealed class ResolutionData
{
    internal CompilerOptions compilerOptions = null!;
    internal RootedDirectoryPath typingsLocation;
    internal string projectName = "";
    internal List<string>? extraExtensions;
    internal InfoCache packageJsonInfoCache = null!;
    internal static ResolutionData New(ResolverOptions options) => new()
    {
        compilerOptions = options.CompilerOptions,
        typingsLocation = options.TypingsLocation,
        projectName = options.ProjectName,
        extraExtensions = options.ExtraExtensions,
        packageJsonInfoCache = options.PackageJsonCache ?? new InfoCache(options.Host.FS().CaseSensitivity()),
    };
    public ResolutionData Clone() => new()
    {
        compilerOptions = compilerOptions,
        typingsLocation = typingsLocation,
        projectName = projectName,
        extraExtensions = extraExtensions,
        packageJsonInfoCache = packageJsonInfoCache.Clone(),
    };
    public DefaultResolver NewResolver(IResolutionHost host) => new(this, host);
    public void PackageJsonCacheEntries(Func<PathKey, InfoCacheEntry, bool> callback) => packageJsonInfoCache.Range(callback);
    internal static RootedFilePath GetRedirectConfigName(IResolvedProjectReference? reference) => reference?.ConfigName() ?? default;
}

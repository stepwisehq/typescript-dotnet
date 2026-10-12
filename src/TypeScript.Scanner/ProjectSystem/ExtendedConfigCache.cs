// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/extendedconfigcache.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.IO.Hashing;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using ConfigParser = TypeScript.TsOptions.TsOptions;
using TypeScript.Vfs;

namespace TypeScript.ProjectSystem;
internal readonly record struct ExtendedConfigParseArgs(RootedFilePath FileName, ByteStringValue Content, IFileSource FS, IFS ConfigFS, List<PathKey>? ResolutionStack, IExtendedConfigCache? Cache);
internal sealed class ExtendedConfigCacheEntry
{
    internal TypeScript.TsOptions.ExtendedConfigCacheEntry Parsed = null!;
    internal UInt128 Hash;
}

internal static class ExtendedConfigCaches
{
    // extendedconfigcache.go:29 NewExtendedConfigCache.
    public static OwnerCache<PathKey, ExtendedConfigCacheEntry, ExtendedConfigParseArgs> NewExtendedConfigCache() => new(static (path, args) =>
    {
        var result = new ExtendedConfigCacheEntry
        {
            Parsed = ConfigParser.ParseExtendedConfig(args.FileName, path, args.ResolutionStack, args.ConfigFS, args.Cache)
        };
        result.Hash = Hash(result.Parsed, args);
        return result;
    }, static (_, entry, args) => entry.Hash == 0 || entry.Hash != Hash(entry.Parsed, args));
    // extendedconfigcache.go:41 hash. Missing extended files return the zero hash,
    // so the entry is necessarily reloaded on its next acquisition.
    private static UInt128 Hash(TypeScript.TsOptions.ExtendedConfigCacheEntry entry, ExtendedConfigParseArgs args)
    {
        var hasher = new XxHash128();
        FileContentHash.Append(hasher, args.Content);
        if (entry.ExtendedFileNames()is { } names)
            foreach (var name in names)
            {
                var handle = args.FS.GetFile(name);
                if (handle is null)
                    return 0;
                FileContentHash.Append(hasher, handle.ContentValue());
            }

        return hasher.GetCurrentHashAsUInt128();
    }
}

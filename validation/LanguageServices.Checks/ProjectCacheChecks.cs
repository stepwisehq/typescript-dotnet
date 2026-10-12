// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original tsc/internal/project/refcountcache_test.go assertions, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
// TestRefCountingCaches needs the complete Session owner and is not registered yet.
using TypeScript.ProjectSystem;
using TypeScript.Scanner;
using TypeScript.ContentMapper;

namespace LanguageServices.Checks;
public static class ProjectCacheChecks
{
    public static readonly string[] Names = ["TestContentMappedParseCacheBundleLifetime", "TestContentMappedParseCacheKeyReconstruction", "TestParseCacheBindsBeforePublishing", "TestParseCacheAcquireExistingUsesFullKey", "TestRefCountCacheAcquireExisting", "TestRefCountCacheAcquireExistingRacesFinalRelease"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestContentMappedParseCacheBundleLifetime":
                BundleLifetime();
                break;
            case "TestContentMappedParseCacheKeyReconstruction":
                KeyReconstruction();
                break;
            case "TestParseCacheBindsBeforePublishing":
                BindsBeforePublishing();
                break;
            case "TestParseCacheAcquireExistingUsesFullKey":
                FullKey();
                break;
            case "TestRefCountCacheAcquireExisting":
                AcquireExisting();
                break;
            case "TestRefCountCacheAcquireExistingRacesFinalRelease":
                RacesFinalRelease();
                break;
            default:
                throw new ArgumentException(name);
        }
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException("original project cache assertion failed");
    }

    private static void BundleLifetime()
    {
        var cache = ParseCaches.NewContentMappedParseCache(default);
        var key = new ContentMappedParseCacheKey(new("/component.vue", "/component.vue"), 0);
        var canonical = new SourceFile();
        var supplemental = new SourceFile();
        var produced = new SourceFiles
        {
            Canonical = canonical,
            Supplemental = Slice<SourceFile>.FromArray([supplemental])
        };
        var(acquired, error) = cache.AcquireOrError(key, () => (produced, null));
        Check(error is null);
        Check(ReferenceEquals(acquired.Canonical, canonical));
        Check(ReferenceEquals(acquired.Supplemental[0], supplemental));
        var(reused, reuseError) = cache.AcquireOrError(key, static () => throw new InvalidOperationException("cached bundle should be reused"));
        Check(reuseError is null);
        Check(ReferenceEquals(reused.Canonical, canonical));
        Check(ReferenceEquals(reused.Supplemental[0], supplemental));
        cache.Deref(key);
        Check(cache.Has(key));
        cache.Deref(key);
        Check(!cache.Has(key));
    }

    private static void KeyReconstruction()
    {
        var acquire = new SourceFileParseOptions("/component.box", "/component.box");
        var mapped = acquire with
        {
            ExternalModuleIndicatorOptions = new(Force: true)
        };
        UInt128 hash = System.IO.Hashing.XxHash128.HashToUInt128("cache key"u8);
        var file = Parser.ParseSourceFile(mapped, "export {};", ScriptKind.TS);
        file.Hash = hash;
        file.SetContentMapperInfo(new() { ContentMapper = "mapper", ParseOptions = acquire });
        var expected = new ContentMappedParseCacheKey(acquire, hash);
        Check(ParseCaches.ContentMappedKeyForFile(file) == expected);
        var duplicate = new TypeScript.Compiler.DuplicateSourceFile
        {
            ParseOptions = mapped,
            ContentMapperParseOptions = acquire,
            Hash = hash,
            ContentMapper = "mapper"
        };
        Check(ParseCaches.ContentMappedKeyForDuplicate(duplicate) == expected);
    }

    private static void BindsBeforePublishing()
    {
        var handle = new Overlay(TsPath.RootedFilePathFromNormalized("/index.js"), ByteStringValue.FromString("module.exports = 0;"), 1, ScriptKind.JS);
        var key = ParseCacheKey.New(new("/index.js", "/index.js"), handle.Hash(), handle.Kind());
        var cache = ParseCaches.NewParseCache(default);
        var file = cache.Acquire(key, handle);
        try
        {
            Check(file.IsBound());
            Check(file.CommonJSModuleIndicator is not null);
        }
        finally
        {
            cache.Deref(key);
        }
    }

    private static void FullKey()
    {
        var handle = new CachedFile(TsPath.RootedFilePathFromNormalized("/index.ts"), ByteStringValue.FromString("export {};"));
        var key = ParseCacheKey.New(new("/index.ts", "/index.ts"), handle.Hash(), ScriptKind.TS);
        var cache = ParseCaches.NewParseCache(default);
        var file = cache.Acquire(key, handle);
        var(acquired, ok) = cache.AcquireExisting(key);
        Check(ok);
        Check(ReferenceEquals(acquired, file));
        cache.Deref(key);
        ParseCacheKey[] mismatches = [key with
        {
            ParseOptions = key.ParseOptions with
            {
                FileName = "/INDEX.ts"
            }
        }, key with
        {
            ParseOptions = key.ParseOptions with
            {
                PathKey = "/INDEX.ts"
            }
        }, key with
        {
            Hash = System.IO.Hashing.XxHash128.HashToUInt128("different"u8)
        }, key with
        {
            ScriptKind = ScriptKind.TSX
        }, key with
        {
            ParseOptions = key.ParseOptions with
            {
                ExternalModuleIndicatorOptions = new(JSX: true)
            }
        }, key with
        {
            ParseOptions = key.ParseOptions with
            {
                ExternalModuleIndicatorOptions = new(Force: true)
            }
        }

        ];
        foreach (var mismatch in mismatches)
        {
            var(_, found) = cache.AcquireExisting(mismatch);
            Check(!found);
            Check(!cache.Has(mismatch));
        }

        cache.Deref(key);
        Check(!cache.Has(key));
    }

    private static void AcquireExisting()
    {
        int parseCount = 0;
        var cache = new RefCountCache<string, int, int>(default, (_, value) =>
        {
            parseCount++;
            return value;
        });
        var(value, ok) = cache.AcquireExisting("missing");
        Check(value == 0);
        Check(!ok);
        Check(parseCount == 0);
        Check(cache.Acquire("key", 1) == 1);
        (value, ok) = cache.AcquireExisting("key");
        Check(ok);
        Check(value == 1);
        Check(parseCount == 1);
        cache.Deref("key");
        Check(cache.Has("key"));
        cache.Deref("key");
        Check(!cache.Has("key"));
        (value, ok) = cache.AcquireExisting("key");
        Check(value == 0);
        Check(!ok);
        Check(parseCount == 1);
    }

    private static void RacesFinalRelease()
    {
        for (int i = 0; i < 100; i++)
        {
            // Box represents the original *int, preserving the shared value identity.
            var cache = new RefCountCache<string, StrongBox, StrongBox>(default, static (_, value) => value);
            var value = new StrongBox(1);
            cache.Acquire("key", value);
            using var start = new ManualResetEventSlim();
            bool acquired = false;
            var thread = new Thread(() =>
            {
                start.Wait();
                acquired = cache.AcquireExisting("key").Ok;
            });
            thread.Start();
            start.Set();
            cache.Deref("key");
            thread.Join();
            if (acquired)
                cache.Deref("key");
            Check(!cache.Has("key"));
        }
    }

    private sealed record StrongBox(int Value);
}

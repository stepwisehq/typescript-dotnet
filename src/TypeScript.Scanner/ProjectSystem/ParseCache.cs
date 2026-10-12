// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/parsecache.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Buffers.Binary;
using System.IO.Hashing;
using TypeScript.ContentMapper;
using TypeScript.Scanner;

namespace TypeScript.ProjectSystem;
internal readonly record struct ParseCacheKey(SourceFileParseOptions ParseOptions, ScriptKind ScriptKind, UInt128 Hash)
{
    // parsecache.go:25 NewParseCacheKey.
    public static ParseCacheKey New(SourceFileParseOptions options, UInt128 hash, ScriptKind scriptKind) => new(options, scriptKind == ScriptKind.Unknown ? Core.EnsureScriptKindFromFileName(RootedFilePath.FromValue(options.FileNameValue)) : scriptKind, hash);
}

internal readonly record struct ContentMappedParseCacheKey(SourceFileParseOptions ParseOptions, UInt128 Hash);
internal static class ParseCaches
{
    // parsecache.go:47 contentMappedParseCacheKey.
    public static ContentMappedParseCacheKey ContentMappedKey(SourceFileParseOptions options, UInt128 rawHash, UInt128 transformIdentity, TypeScript.Locale.Locale diagnosticLocale)
    {
        var locale = ByteStringValue.FromString(diagnosticLocale.String());
        byte[] bytes = new byte[32 + locale.Length];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(0, 8), (ulong)(rawHash >> 64));
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(8, 8), (ulong)rawHash);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(16, 8), (ulong)(transformIdentity >> 64));
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(24, 8), (ulong)transformIdentity);
        for (int i = 0; i < locale.Length; i++)
            bytes[32 + i] = (byte)locale[i];
        return new(options, XxHash128.HashToUInt128(bytes));
    }

    // parsecache.go:57–72. Reconstruct the full identity, including acquisition
    // options rather than the transformed file's effective parse options.
    public static ParseCacheKey KeyForFile(SourceFile file) => ParseCacheKey.New(file.ParseOptions(), file.Hash, file.ScriptKind);
    public static ContentMappedParseCacheKey ContentMappedKeyForFile(SourceFile file) => new(file.ContentMapperParseOptions(), file.Hash);
    public static ParseCacheKey KeyForDuplicate(TypeScript.Compiler.DuplicateSourceFile file) => ParseCacheKey.New(file.ParseOptions, file.Hash, file.ScriptKind);
    public static ContentMappedParseCacheKey ContentMappedKeyForDuplicate(TypeScript.Compiler.DuplicateSourceFile file) => new(file.ContentMapperParseOptions, file.Hash);
    // parsecache.go:76 NewParseCache. Binding precedes cache publication.
    public static RefCountCache<ParseCacheKey, SourceFile, IFileHandle> NewParseCache(RefCountCacheOptions options) => new(options, static (key, handle) =>
    {
        var file = Parser.ParseSourceFileOwned(key.ParseOptions, FileContentHash.ByteView(handle.ContentValue()), key.ScriptKind);
        file.Hash = handle.Hash();
        Binder.BindSourceFile(file);
        return file;
    });
    // parsecache.go:94 NewContentMappedParseCache. One reference owns canonical
    // and supplemental files together; ordinary production is deliberately illegal.
    public static RefCountCache<ContentMappedParseCacheKey, SourceFiles, ValueTuple> NewContentMappedParseCache(RefCountCacheOptions options) => new(options, static (_, _) => throw new InvalidOperationException("content-mapped source files must be produced with AcquireOrError"));
}

// Standard .NET xxh3-128 adapter: retain source bytes; legacy Go byte-character
// headers are copied through bounded stack storage, not decoded/re-encoded.
internal static class FileContentHash
{
    public static ByteStringView ByteView(ByteStringValue content)
    {
        if (content.IsBytes)
            return content.ByteView;
        byte[] bytes = new byte[content.Length];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)content[i];
        return ByteStringView.FromOwnedBytes(bytes);
    }

    public static UInt128 Hash(ByteStringValue content)
    {
        if (content.IsBytes)
            return XxHash128.HashToUInt128(content.ByteSpan);
        var hasher = new XxHash128();
        Append(hasher, content);
        return hasher.GetCurrentHashAsUInt128();
    }

    public static void Append(XxHash128 hasher, ByteStringValue content)
    {
        if (content.IsBytes)
        {
            hasher.Append(content.ByteSpan);
            return;
        }

        Span<byte> bytes = stackalloc byte[512];
        for (int start = 0; start < content.Length; start += bytes.Length)
        {
            int count = Math.Min(bytes.Length, content.Length - start);
            for (int i = 0; i < count; i++)
                bytes[i] = (byte)content[start + i];
            hasher.Append(bytes[..count]);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original registry_test.go autoImportStats/singleBucket and actual Session fixture adapters.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.AutoImport;

namespace LanguageServices.Checks;
public static partial class AutoImportRegistryChecks
{
    private static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }

    private static CacheStats Stats(ProjectSessionFixture fixture) => (fixture.Session.Snapshot().AutoImportRegistry() ?? throw new InvalidOperationException("auto import registry not initialized")).GetCacheStats();
    private static BucketStats SingleBucket(Slice<BucketStats> buckets)
    {
        Require(buckets.Length == 1, $"expected 1 bucket, got {buckets.Length}");
        return buckets[0];
    }

    private static void Open(ProjectSessionFixture fixture, DocumentUri uri, string content) => fixture.Session.DidOpenFile(ProtocolRequestContext.Default, uri, 1, ByteStringValue.FromString(content), LanguageKind.TypeScript);
    private static void AutoImports(ProjectSessionFixture fixture, DocumentUri uri)
    {
        var(_, error) = fixture.Session.GetCurrentLanguageServiceWithAutoImports(ProtocolRequestContext.Default, uri);
        if (error is not null)
            throw error;
    }
}

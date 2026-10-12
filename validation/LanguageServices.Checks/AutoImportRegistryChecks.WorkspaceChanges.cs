// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestRegistryLifecycle workspace and pnpm granular-update assertions.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.ProjectSystem;
using TypeScript.LanguageServices.AutoImport;

namespace LanguageServices.Checks;
public static partial class AutoImportRegistryChecks
{
    private static void LifecycleSymlinkedSourceChange()
    {
        const string root = "/home/src/symlinked-monorepo-invalidation";
        var files = SymlinkedMonorepoFiles();
        using var fixture = new ProjectSessionFixture(files);
        var a = AutoImportMonorepoFixture.URIFor(root + "/packages/project-a/src/index.ts");
        var b = AutoImportMonorepoFixture.URIFor(root + "/packages/project-b/src/index.ts");
        Open(fixture, a, FixtureContent(files, a.FileName()));
        AutoImports(fixture, a);
        var bucket = SingleBucket(Stats(fixture).NodeModulesBuckets);
        Require(!bucket.State.Dirty(), "bucket should be clean initially");
        Require(bucket.FileCount > 0, "bucket should have files initially");
        Open(fixture, b, FixtureContent(files, b.FileName()));
        Change(fixture, b, 2, "export const projectBValue: number = 42;");
        var(_, error) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default, a);
        if (error is not null)
            throw error;
        bucket = SingleBucket(Stats(fixture).NodeModulesBuckets);
        Require(bucket.State.Dirty(), "bucket should be dirty after source file change");
        var dirty = bucket.State.DirtyPackages();
        Require(dirty is not null, "dirty packages should be tracked");
        Require(dirty.Contains(ByteStringKey.FromValue(ByteStringValue.FromString("project-b"))), "project-b should be in dirty packages");
        Require(!dirty.Contains(ByteStringKey.FromValue(ByteStringValue.FromString("other-pkg"))), "other-pkg should NOT be in dirty packages");
        Require(dirty.Count == 1, "only one package should be dirty");
        AutoImports(fixture, a);
        Require(!SingleBucket(Stats(fixture).NodeModulesBuckets).State.Dirty(), "bucket should be clean after rebuild");
    }

    private static void LifecyclePnpmWorkspaceGranularity()
    {
        const string root = "/home/src/pnpm-monorepo";
        var files = PnpmMonorepoFiles();
        using var fixture = new ProjectSessionFixture(files, new SessionOptions { CurrentDirectory = RootedDirectoryPath.FromGoString(root), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), PositionEncoding = PositionEncodingKind.UTF8, WatchEnabled = true, LoggingEnabled = true, PushDiagnosticsEnabled = true });
        var a = AutoImportMonorepoFixture.URIFor(root + "/packages/project-a/src/index.ts");
        var b = AutoImportMonorepoFixture.URIFor(root + "/packages/project-b/src/index.ts");
        Open(fixture, a, FixtureContent(files, a.FileName()));
        AutoImports(fixture, a);
        Require(!SingleBucket(Stats(fixture).NodeModulesBuckets).State.Dirty(), "bucket should be clean initially");
        Open(fixture, b, FixtureContent(files, b.FileName()));
        Change(fixture, b, 2, "export const projectBValue: number = 42;");
        var(_, error) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default, a);
        if (error is not null)
            throw error;
        var bucket = SingleBucket(Stats(fixture).NodeModulesBuckets);
        Require(bucket.State.Dirty(), "bucket should be dirty after workspace package change");
        var dirty = bucket.State.DirtyPackages();
        Require(dirty is not null, "dirty packages should be tracked for workspace package");
        Require(dirty.Contains(ByteStringKey.FromValue(ByteStringValue.FromString("project-b"))), "project-b should be in dirty packages");
        Require(dirty.Count == 1, "only project-b should be dirty");
        AutoImports(fixture, a);
        Require(!SingleBucket(Stats(fixture).NodeModulesBuckets).State.Dirty(), "bucket should be clean after rebuild");
        var other = AutoImportMonorepoFixture.URIFor(root + "/packages/project-a/node_modules/.pnpm-store/other-pkg@1.0.0/index.d.ts");
        Open(fixture, other, FixtureContent(files, other.FileName()));
        Change(fixture, other, 2, "export declare function otherFunction(): void;");
        // Preserve the original request-time snapshot lease around the stats read;
        // a background warm/adoption must not replace the asserted snapshot.
        CacheStats? captured = null;
        (_, error) = fixture.Session.WithLanguageServiceAndSnapshot(ProtocolRequestContext.Default, a, (_, snapshot) =>
        {
            captured = snapshot.AutoImportRegistry()!.GetCacheStats();
            return (null, null);
        });
        if (error is not null)
            throw error;
        bucket = SingleBucket(captured!.NodeModulesBuckets);
        Require(bucket.State.Dirty(), "bucket should be dirty after registry package change");
        dirty = bucket.State.DirtyPackages();
        if (dirty is not null)
            Require(!dirty.Contains(ByteStringKey.FromValue(ByteStringValue.FromString("other-pkg"))), "other-pkg should NOT be in dirty packages (should trigger full rebuild)");
    }

    private static string FixtureContent(Program.Checks.CompilerBaselineFileSystem.Input[] files, RootedFilePath file)
    {
        foreach (var input in files)
            if (RootedFilePath.FromGoString(input.Path) == file)
                return input.Contents;
        throw new InvalidOperationException("original fixture file missing: " + file.AsString());
    }
}

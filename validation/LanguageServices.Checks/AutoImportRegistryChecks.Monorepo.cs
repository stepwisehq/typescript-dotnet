// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestRegistryLifecycle monorepo bucket lifetime/dependency assertions.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class AutoImportRegistryChecks
{
    private static void LifecycleClosedProjectBuckets()
    {
        using var monorepo = new AutoImportMonorepoFixture("/home/src/autoimport-monorepo", "monorepo", ["pkg-root"], null, [new("package-a", 1, ["pkg-a"]), new("package-b", 1, ["pkg-b"])]);
        var fixture = monorepo.Fixture;
        var a = monorepo.Packages[0];
        var b = monorepo.Packages[1];
        Open(fixture, a.URI(0), a.Files[0].Contents);
        AutoImports(fixture, a.URI(0));
        Open(fixture, b.URI(0), b.Files[0].Contents);
        AutoImports(fixture, b.URI(0));
        var stats = Stats(fixture);
        Require(stats.NodeModulesBuckets.Length == 3, "expected3 node_modules buckets");
        Require(stats.ProjectBuckets.Length == 2, "expected2 project buckets");
        fixture.Session.DidCloseFile(ProtocolRequestContext.Default, a.URI(0));
        AutoImports(fixture, b.URI(0));
        stats = Stats(fixture);
        Require(stats.NodeModulesBuckets.Length == 2, "expected2 retained node_modules buckets");
        Require(stats.ProjectBuckets.Length == 1, "expected1 retained project bucket");
    }

    private static void LifecycleOpenFileDependencySelection()
    {
        const string root = "/home/src/monorepo";
        const string rootIndex = root + "/index.js";
        const string packageIndex = root + "/packages/a/index.js";
        using var monorepo = new AutoImportMonorepoFixture(root, "monorepo", ["pkg1", "pkg2", "pkg3"], ["pkg1"], [new("a", 0, DependencyNames: ["pkg1", "pkg2"])], [new(rootIndex, "export const monorepoIndex = 1;\n"), new(packageIndex, "export const pkgA = 2;\n")]);
        var fixture = monorepo.Fixture;
        var rootFile = monorepo.ExtraFile(rootIndex);
        var packageFile = monorepo.ExtraFile(packageIndex);
        var rootURI = AutoImportMonorepoFixture.URIFor(rootFile.Path);
        var packageURI = AutoImportMonorepoFixture.URIFor(packageFile.Path);
        fixture.Session.DidOpenFile(ProtocolRequestContext.Default, rootURI, 1, ByteStringValue.FromString(rootFile.Contents), LanguageKind.JavaScript);
        AutoImports(fixture, rootURI);
        Require(DependenciesEqual(SingleBucket(Stats(fixture).NodeModulesBuckets).DependencyNames, "pkg1"), "expected pkg1 dependencies");
        fixture.Session.DidOpenFile(ProtocolRequestContext.Default, packageURI, 1, ByteStringValue.FromString(packageFile.Contents), LanguageKind.JavaScript);
        AutoImports(fixture, packageURI);
        Require(DependenciesEqual(SingleBucket(Stats(fixture).NodeModulesBuckets).DependencyNames, "pkg1", "pkg2"), "expected pkg1+pkg2 dependencies");
        fixture.Session.DidCloseFile(ProtocolRequestContext.Default, packageURI);
        AutoImports(fixture, rootURI);
        Require(DependenciesEqual(SingleBucket(Stats(fixture).NodeModulesBuckets).DependencyNames, "pkg1"), "expected pkg1 after close");
        fixture.Session.DidCloseFile(ProtocolRequestContext.Default, rootURI);
        var untitled = new DocumentUri(ByteStringValue.FromString("untitled:Untitled-1"));
        fixture.Session.DidOpenFile(ProtocolRequestContext.Default, untitled, 0, default, LanguageKind.TypeScript);
        var(_, error) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default, untitled);
        if (error is not null)
            throw error;
        Require(Stats(fixture).NodeModulesBuckets.Length == 0, "expected no node_modules buckets");
    }

    private static void LifecycleResolvedPackagesAcrossProjects()
    {
        const string root = "/home/src/cross-project-deps";
        const string a = root + "/packages/a/index.ts";
        const string b = root + "/packages/b/index.ts";
        using var monorepo = new AutoImportMonorepoFixture(root, "monorepo", ["pkg-listed", "pkg-unlisted"], ["pkg-listed"], [new("a", 0, DependencyNames: ["pkg-listed"]), new("b", 0, DependencyNames: ["pkg-listed"])], [new(a, "import { pkg_unlisted_value } from \"pkg-unlisted\";\nexport const a = pkg_unlisted_value;\n"), new(b, "export const b = 1;\n")]);
        var fixture = monorepo.Fixture;
        var fileA = monorepo.ExtraFile(a);
        var fileB = monorepo.ExtraFile(b);
        Open(fixture, AutoImportMonorepoFixture.URIFor(fileA.Path), fileA.Contents);
        AutoImports(fixture, AutoImportMonorepoFixture.URIFor(fileA.Path));
        Open(fixture, AutoImportMonorepoFixture.URIFor(fileB.Path), fileB.Contents);
        AutoImports(fixture, AutoImportMonorepoFixture.URIFor(fileB.Path));
        var bucket = SingleBucket(Stats(fixture).NodeModulesBuckets);
        Require(bucket.DependencyNames?.Contains(ByteStringKey.FromValue(ByteStringValue.FromString("pkg-listed"))) == true, "pkg-listed should be in dependencies");
        Require(bucket.DependencyNames?.Contains(ByteStringKey.FromValue(ByteStringValue.FromString("pkg-unlisted"))) == true, "pkg-unlisted should be in dependencies because project-a imports it");
    }

    private static bool DependenciesEqual(HashSet<ByteStringKey>? values, params string[] expected)
    {
        if ((values?.Count ?? 0) != expected.Length)
            return false;
        foreach (var name in expected)
            if (values?.Contains(ByteStringKey.FromValue(ByteStringValue.FromString(name))) != true)
                return false;
        return true;
    }
}

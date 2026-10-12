// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// registry_test.go:25. Lifecycle subtests are grouped into the original full
// dispatcher; all fifteen original groups are assembled, execution pending.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.AutoImport;

namespace LanguageServices.Checks;
public static partial class AutoImportRegistryChecks
{
    private static void LifecycleBuildsBuckets()
    {
        using var lifecycle = new AutoImportLifecycleFixture(1);
        var fixture = lifecycle.Fixture;
        var uri = lifecycle.URI(0);
        Open(fixture, uri, lifecycle.SourceFiles[0].Contents);
        var stats = Stats(fixture);
        var projectBucket = SingleBucket(stats.ProjectBuckets);
        var nodeModulesBucket = SingleBucket(stats.NodeModulesBuckets);
        Require(projectBucket.State.Dirty(), "expected dirty project bucket");
        Require(projectBucket.FileCount == 0, "expected initial project bucket file count0");
        Require(nodeModulesBucket.State.Dirty(), "expected dirty node_modules bucket");
        Require(nodeModulesBucket.FileCount == 0, "expected initial node_modules bucket file count0");
        AutoImports(fixture, uri);
        stats = Stats(fixture);
        projectBucket = SingleBucket(stats.ProjectBuckets);
        nodeModulesBucket = SingleBucket(stats.NodeModulesBuckets);
        Require(!projectBucket.State.Dirty(), "expected clean project bucket");
        Require(projectBucket.ExportCount > 0, "expected project exports");
        Require(!nodeModulesBucket.State.Dirty(), "expected clean node_modules bucket");
        Require(nodeModulesBucket.ExportCount > 0, "expected node_modules exports");
    }

    private static void LifecycleSameFileChange()
    {
        using var lifecycle = new AutoImportLifecycleFixture(2);
        var fixture = lifecycle.Fixture;
        var first = lifecycle.URI(0);
        var second = lifecycle.URI(1);
        Open(fixture, first, lifecycle.SourceFiles[0].Contents);
        Open(fixture, second, lifecycle.SourceFiles[1].Contents);
        AutoImports(fixture, first);
        Change(fixture, first, 2, lifecycle.SourceFiles[0].Contents + "// change\n");
        var(_, error) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default, first);
        if (error is not null)
            throw error;
        var stats = Stats(fixture);
        var projectBucket = SingleBucket(stats.ProjectBuckets);
        var nodeModulesBucket = SingleBucket(stats.NodeModulesBuckets);
        var firstPath = fixture.MutableFS.CaseSensitivity().PathKey(first.FileName().AsPath());
        Require(projectBucket.State.Dirty(), "expected changed project bucket dirty");
        Require(projectBucket.State.DirtyFile() == firstPath, "expected dirty file to be first file");
        Require(!nodeModulesBucket.State.Dirty(), "expected node_modules clean");
        Require(nodeModulesBucket.State.DirtyFile() == default(PathKey), "expected no dirty node_modules file");
        AutoImports(fixture, first);
        projectBucket = SingleBucket(Stats(fixture).ProjectBuckets);
        Require(projectBucket.State.Dirty(), "same changed file should not force rebuild");
        Require(projectBucket.State.DirtyFile() == firstPath, "same changed file should retain dirty identity");
        Change(fixture, second, 1, "// new content");
        AutoImports(fixture, first);
        Require(!SingleBucket(Stats(fixture).ProjectBuckets).State.Dirty(), "another changed file should force rebuild");
    }

    private static void LifecycleNewProgramFiles()
    {
        const string root = "/home/src/explicit-files-project";
        using var fixture = new ProjectSessionFixture(new Program.Checks.CompilerBaselineFileSystem.Input[] { new(root + "/tsconfig.json", "{\"compilerOptions\":{\"module\":\"esnext\",\"target\":\"esnext\",\"strict\":true},\"files\":[\"index.ts\"]}"), new(root + "/index.ts", ""), new(root + "/utils.ts", "export const foo = 1;\nexport const bar = 2;") });
        var uri = new DocumentUri(ByteStringValue.FromString("file://" + root + "/index.ts"));
        Open(fixture, uri, "");
        AutoImports(fixture, uri);
        Require(SingleBucket(Stats(fixture).ProjectBuckets).FileCount == 1, "expected initial file count1");
        Change(fixture, uri, 2, "import { foo } from \"./utils\";");
        AutoImports(fixture, uri);
        Require(SingleBucket(Stats(fixture).ProjectBuckets).FileCount == 2, "expected new imported file in bucket");
    }

    private static void LifecycleDependencyChange()
    {
        using var lifecycle = new AutoImportLifecycleFixture(1);
        var fixture = lifecycle.Fixture;
        var uri = lifecycle.URI(0);
        Open(fixture, uri, lifecycle.SourceFiles[0].Contents);
        AutoImports(fixture, uri);
        Require(!SingleBucket(Stats(fixture).NodeModulesBuckets).State.Dirty(), "expected clean node_modules bucket");
        void UpdatePackageJSON(string content)
        {
            var error = fixture.MutableFS.WriteFile(RootedFilePath.FromGoString(AutoImportLifecycleFixture.Root + "/package.json"), content);
            if (error is not null)
                throw error;
            fixture.Session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Changed, Uri = new DocumentUri(ByteStringValue.FromString("file://" + AutoImportLifecycleFixture.Root + "/package.json")) }]));
        }

        UpdatePackageJSON("{\n  \"name\": \"local-project-stable\",\n  \"dependencies\": {\n    \"pkg1\": \"*\"\n  }\n}\n");
        var(_, serviceError) = fixture.Session.GetLanguageService(ProtocolRequestContext.Default, uri);
        if (serviceError is not null)
            throw serviceError;
        Require(!SingleBucket(Stats(fixture).NodeModulesBuckets).State.Dirty(), "unchanged dependencies should remain clean");
        UpdatePackageJSON("{\n  \"name\": \"local-project-stable\",\n  \"dependencies\": {\n    \"pkg1\": \"*\",\n    \"newpkg\": \"*\"\n  }\n}\n");
        AutoImports(fixture, uri);
        Require(SingleBucket(Stats(fixture).NodeModulesBuckets).DependencyNames?.Contains(ByteStringKey.FromValue(ByteStringValue.FromString("newpkg"))) == true, "expected newpkg dependency");
    }

    private static void LifecycleDeletedNodeModules()
    {
        using var lifecycle = new AutoImportLifecycleFixture(1);
        var fixture = lifecycle.Fixture;
        var uri = lifecycle.URI(0);
        var preferences = TypeScript.LanguageServices.Utilities.LsUtil.NewDefaultUserPreferences();
        preferences.IncludeCompletionsForModuleExports = Tristate.True;
        preferences.IncludeCompletionsForImportStatements = Tristate.True;
        Open(fixture, uri, lifecycle.SourceFiles[0].Contents);
        AutoImports(fixture, uri);
        var snapshot = fixture.Session.Snapshot();
        var project = snapshot.GetDefaultProject(uri);
        Require(project is not null, "expected default project");
        var projectID = project.ID();
        Require(Registry.IsPreparedForImportingFile(snapshot.AutoImportRegistry(), uri.FileName(), projectID, preferences), "expected prepared registry");
        Require(Stats(fixture).NodeModulesBuckets.Length == 1, "expected one node_modules bucket");
        var error = fixture.MutableFS.Remove(RootedPath.FromGoString(AutoImportLifecycleFixture.Root + "/node_modules"));
        if (error is not null)
            throw error;
        fixture.Session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Deleted, Uri = new DocumentUri(ByteStringValue.FromString("file://" + AutoImportLifecycleFixture.Root + "/node_modules")) }]));
        AutoImports(fixture, uri);
        snapshot = fixture.Session.Snapshot();
        Require(Registry.IsPreparedForImportingFile(snapshot.AutoImportRegistry(), uri.FileName(), projectID, preferences), "registry should be prepared after node_modules is deleted");
        Require(Stats(fixture).NodeModulesBuckets.Length == 0, "deleted bucket should be removed entirely");
    }

    private static void Change(ProjectSessionFixture fixture, DocumentUri uri, int version, string content) => fixture.Session.DidChangeFile(ProtocolRequestContext.Default, uri, version, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new TextDocumentContentChangeWholeDocument { Text = ByteStringValue.FromString(content) } }]));
}

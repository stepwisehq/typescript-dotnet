// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestRegistryLifecycle deletion and preference invalidation groups.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.LanguageServices.Utilities;

namespace LanguageServices.Checks;
public static partial class AutoImportRegistryChecks
{
    private static void LifecycleDeletedModulesWithPackageChange()
    {
        using var lifecycle = new AutoImportLifecycleFixture(1);
        var fixture = lifecycle.Fixture;
        var uri = lifecycle.URI(0);
        var preferences = LsUtil.NewDefaultUserPreferences();
        preferences.IncludeCompletionsForModuleExports = Tristate.True;
        preferences.IncludeCompletionsForImportStatements = Tristate.True;
        Open(fixture, uri, lifecycle.SourceFiles[0].Contents);
        AutoImports(fixture, uri);
        var snapshot = fixture.Session.Snapshot();
        var project = snapshot.GetDefaultProject(uri);
        Require(project is not null, "expected default project");
        var projectID = project.ID();
        Require(Stats(fixture).NodeModulesBuckets.Length == 1, "expected node_modules bucket");
        var error = fixture.MutableFS.WriteFile(RootedFilePath.FromGoString(AutoImportLifecycleFixture.Root + "/package.json"), "{\"name\": \"app\", \"dependencies\": {}}");
        if (error is not null)
            throw error;
        error = fixture.MutableFS.Remove(RootedPath.FromGoString(AutoImportLifecycleFixture.Root + "/node_modules"));
        if (error is not null)
            throw error;
        fixture.Session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Changed, Uri = AutoImportMonorepoFixture.URIFor(AutoImportLifecycleFixture.Root + "/package.json") }, new() { Type = FileChangeType.Deleted, Uri = AutoImportMonorepoFixture.URIFor(AutoImportLifecycleFixture.Root + "/node_modules") }]));
        AutoImports(fixture, uri);
        snapshot = fixture.Session.Snapshot();
        Require(Registry.IsPreparedForImportingFile(snapshot.AutoImportRegistry(), uri.FileName(), projectID, preferences), "expected prepared registry after combined deletion and package change");
        Require(Stats(fixture).NodeModulesBuckets.Length == 0, "expected deleted node_modules bucket removed");
    }

    private static void LifecycleDeletedPackageDirectory()
    {
        using var lifecycle = new AutoImportLifecycleFixture(1);
        var fixture = lifecycle.Fixture;
        var uri = lifecycle.URI(0);
        Open(fixture, uri, lifecycle.SourceFiles[0].Contents);
        AutoImports(fixture, uri);
        Require(SingleBucket(Stats(fixture).NodeModulesBuckets).ExportCount > 0, "expected initial node_modules exports");
        var error = fixture.MutableFS.Remove(RootedPath.FromGoString(AutoImportLifecycleFixture.Root + "/node_modules/pkg1"));
        if (error is not null)
            throw error;
        fixture.Session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Deleted, Uri = AutoImportMonorepoFixture.URIFor(AutoImportLifecycleFixture.Root + "/node_modules/pkg1") }]));
        AutoImports(fixture, uri);
        Require(SingleBucket(Stats(fixture).NodeModulesBuckets).ExportCount == 0, "expected package directory deletion to remove exports");
    }

    private static void LifecycleFileExcludePatterns()
    {
        using var lifecycle = new AutoImportLifecycleFixture(1);
        var fixture = lifecycle.Fixture;
        var uri = lifecycle.URI(0);
        Open(fixture, uri, lifecycle.SourceFiles[0].Contents);
        AutoImports(fixture, uri);
        var stats = Stats(fixture);
        Require(!SingleBucket(stats.ProjectBuckets).State.Dirty(), "expected clean project bucket");
        Require(!SingleBucket(stats.NodeModulesBuckets).State.Dirty(), "expected clean node_modules bucket");
        var snapshot = fixture.Session.Snapshot();
        var project = snapshot.GetDefaultProject(uri);
        Require(project is not null, "expected default project");
        var projectID = project.ID();
        var preferences = LsUtil.NewDefaultUserPreferences();
        preferences.IncludeCompletionsForModuleExports = Tristate.True;
        preferences.IncludeCompletionsForImportStatements = Tristate.True;
        Require(Registry.IsPreparedForImportingFile(snapshot.AutoImportRegistry(), uri.FileName(), projectID, preferences), "expected prepared with initial preferences");
        var newPreferences = LsUtil.NewDefaultUserPreferences();
        newPreferences.IncludeCompletionsForModuleExports = Tristate.True;
        newPreferences.IncludeCompletionsForImportStatements = Tristate.True;
        newPreferences.AutoImportFileExcludePatterns = Slice<string>.FromArray(["**/node_modules/**/*.d.ts"]);
        fixture.Session.Configure(newPreferences);
        var snapshot2 = fixture.Session.Snapshot();
        Require(!Registry.IsPreparedForImportingFile(snapshot2.AutoImportRegistry(), uri.FileName(), projectID, newPreferences), "expected unprepared after exclude pattern change");
        AutoImports(fixture, uri);
        var snapshot3 = fixture.Session.Snapshot();
        Require(Registry.IsPreparedForImportingFile(snapshot3.AutoImportRegistry(), uri.FileName(), projectID, newPreferences), "IsPreparedForImportingFile should return true after bucket rebuild with new fileExcludePatterns");
    }
}

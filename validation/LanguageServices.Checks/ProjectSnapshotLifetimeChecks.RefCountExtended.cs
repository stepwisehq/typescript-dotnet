// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// refcountcache_test.go TestRefCountingCaches original extended-config lifetimes.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    private const string ExtendedPath = "/user/username/projects/myproject/tsconfig.base.json";
    private static Fixture ExtendedFixture() => new(new Input("/user/username/projects/myproject/tsconfig.json", "{\n\t\t\t\t\"extends\": \"./tsconfig.base.json\"\n\t\t\t}"), new Input(ExtendedPath, "{\n\t\t\t\t\"compilerOptions\": {}\n\t\t\t}"), new Input(Main, "const x = 1;"));
    private static void ReleaseExtendedConfigOnClose()
    {
        using var fixture = ExtendedFixture();
        var session = fixture.Session;
        OpenFile(session, "file://" + Main, "const x = 1;");
        var config = session.Snapshot().ConfigFileRegistry.GetConfig(TsPath.PathKeyFromCanonical("/user/username/projects/myproject/tsconfig.json"))!;
        Check(config.ExtendedSourceFiles()![0] == RootedFilePath.FromGoString(ExtendedPath), "extended source path");
        var cache = session.SnapshotHost.extendedConfigCache;
        var(entry, _) = cache.entries.Load(TsPath.PathKeyFromCanonical(ExtendedPath));
        Check(entry.Owners.Count == 1, "one extended config owner");
        session.DidCloseFile(ProtocolRequestContext.Default, URI("file://" + Main));
        OpenFile(session, "untitled:Untitled-1", "");
        session.WaitForBackgroundTasks();
        Check(!cache.entries.Load(TsPath.PathKeyFromCanonical(ExtendedPath)).Ok, "extended entry deleted after close");
    }

    private static void UnretainedCloneReleasesEntries()
    {
        using var fixture = ExtendedFixture();
        var session = fixture.Session;
        var baseline = session.Snapshot();
        var uri = URI("file://" + Main);
        var clone = baseline.Clone(ProtocolRequestContext.Default, new SnapshotChange { reason = UpdateReason.RequestedLanguageServiceProjectNotLoaded, ResourceRequest = new() { Documents = Slice<DocumentUri>.FromArray([uri]) } }, baseline.Overlays(), null, null);
        ParseCacheKey mainKey;
        var extendedKey = TsPath.PathKeyFromCanonical(ExtendedPath);
        try
        {
            var project = clone.GetDefaultProject(uri);
            Check(project is not null, "clone default project");
            Check(project!.ProgramLastUpdate == clone.id, "program belongs to clone");
            var main = project.Program!.GetSourceFile(RootedFilePath.FromGoString(Main))!;
            mainKey = ParseCaches.KeyForFile(main);
            var(mainEntry, mainOK) = session.SnapshotHost.parseCache.entries.Load(mainKey);
            Check(mainOK, "clone main entry");
            Check(mainEntry.RefCount == 1, "clone main reference");
            var(extendedEntry, extendedOK) = session.SnapshotHost.extendedConfigCache.entries.Load(extendedKey);
            Check(extendedOK, "clone extended config entry");
            Check(extendedEntry.Owners.Count == 1, "clone extended config owner");
        }
        finally
        {
            clone.Deref();
        }

        Check(!session.SnapshotHost.parseCache.entries.Load(mainKey).Ok, "unretained clone main entry released");
        Check(!session.SnapshotHost.extendedConfigCache.entries.Load(extendedKey).Ok, "unretained clone extended entry released");
    }

    private static void CreateProgramRetainsReferencedExtendedConfigs()
    {
        const string appConfig = "/user/username/projects/app/tsconfig.json", appFile = "/user/username/projects/app/index.ts", libConfig = "/user/username/projects/lib/tsconfig.json", libBase = "/user/username/projects/lib/tsconfig.base.json", libFile = "/user/username/projects/lib/index.ts";
        using var fixture = new Fixture(new Input(appConfig, "{\"compilerOptions\":{\"noLib\":true},\"files\":[\"index.ts\"],\"references\":[{\"path\":\"../lib\"}]}"), new Input(appFile, "export const app = 1;"), new Input(libConfig, "{\"extends\":\"./tsconfig.base.json\",\"files\":[\"index.ts\"]}"), new Input(libBase, "{\"compilerOptions\":{\"composite\":true,\"noLib\":true}}"), new Input(libFile, "export const lib = 1;"));
        var session = fixture.Session;
        var appID = Projects.ConfiguredProjectIDFromPathKey(TsPath.PathKeyFromCanonical(appConfig)).AsID();
        var(baseline, baseError) = session.APIUpdate(ProtocolRequestContext.Default, default, new APISnapshotRequest { OpenProjects = [RootedFilePath.FromGoString(appConfig)], EnsurePrograms = [appID] });
        if (baseError is not null)
            throw baseError;
        try
        {
            var app = baseline!.ProjectCollection.GetProject(appID);
            Check(app is not null, "app project exists");
            var appCommandLine = app.CommandLine!; // EnsurePrograms constructed the actual initialized program.
            var request = Slice<APICreateProgramRequest>.FromArray([new() { RootFileNames = appCommandLine.FileNames(), CompilerOptions = appCommandLine.CompilerOptions(), ProjectReferences = appCommandLine.ProjectReferences(), ConfigFileParsingDiagnostics = appCommandLine.Errors }]);
            var(programSnapshot, error) = session.SnapshotHost.CloneSnapshot(ProtocolRequestContext.Default, baseline, default, new APISnapshotRequest { CreatePrograms = request });
            try
            {
                if (error is not null)
                    throw error;
                var project = programSnapshot.CreatedPrograms()[0];
                Check(project is not null, "created program project");
                Check(!ReferenceEquals(project!.Program, app.Program), "distinct synthetic program");
                var(entry, exists) = session.SnapshotHost.extendedConfigCache.entries.Load(TsPath.PathKeyFromCanonical(libBase));
                Check(exists, "referenced extended cache entry");
                bool baseOwns, programOwns;
                int owners;
                entry.Mu.Wait();
                try
                {
                    baseOwns = entry.Owners.Contains(baseline.id);
                    programOwns = entry.Owners.Contains(programSnapshot.id);
                    owners = entry.Owners.Count;
                }
                finally
                {
                    entry.Mu.Release();
                }

                Check(baseOwns, "base owns referenced extended config");
                Check(programOwns, "synthetic snapshot owns referenced extended config");
                Check(owners == 2, "two extended config owners");
                var writeError = fixture.FS.WriteFile(RootedFilePath.FromGoString(libBase), "{\"compilerOptions\":{\"composite\":true,\"noLib\":true,\"strict\":true}}");
                if (writeError is not null)
                    throw writeError;
                var(updated, updateError) = session.SnapshotHost.CloneSnapshot(ProtocolRequestContext.Default, programSnapshot, new FileChangeSummary { Changed = [URI("file://" + libBase)] }, new APISnapshotRequest { EnsurePrograms = [project.ID()] });
                try
                {
                    if (updateError is not null)
                        throw updateError;
                    var next = updated.ProjectCollection.GetProject(project.ID());
                    Check(next is not null, "updated synthetic program exists");
                    Check(!ReferenceEquals(next!.Program, project.Program), "updated synthetic program rebuilt");
                    var references = next.Program!.GetResolvedProjectReferences();
                    Check(references.Length == 1, "one updated project reference");
                    Check(references[0]!.CompilerOptions().Strict == Tristate.True, "referenced strict option reloaded");
                }
                finally
                {
                    updated.Deref();
                }
            }
            finally
            {
                programSnapshot.Deref();
            }
        }
        finally
        {
            baseline!.Deref();
        }
    }
}

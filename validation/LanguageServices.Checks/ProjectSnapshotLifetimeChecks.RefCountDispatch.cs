// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete refcountcache_test.go TestRefCountingCaches original dispatcher/lifetimes.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    private static void FailedAPIUpdatePreservesReferences()
    {
        const string config = "/project/tsconfig.json";
        using var fixture = new Fixture(new Input(config, "{\"compilerOptions\":{\"noLib\":true},\"files\":[\"index.ts\"]}"), new Input("/project/index.ts", "export const value = 1;"));
        var session = fixture.Session;
        var(snapshot, error) = session.APIUpdate(ProtocolRequestContext.Default, default, new APISnapshotRequest { OpenProjects = [RootedFilePath.FromGoString(config)] });
        if (error is not null)
            throw error;
        snapshot!.Deref();
        var key = TsPath.PathKeyFromCanonical(config);
        var(rejected, rejectError) = session.APIUpdate(ProtocolRequestContext.Default, default, new APISnapshotRequest { CloseProjects = [key], ReconfigurePrograms = Slice<APIReconfigureProgramRequest>.FromArray([new() { ProgramID = Projects.NewSyntheticProjectID(999) }]) });
        Check(rejectError is not null && rejectError.Message.Contains("synthetic program not found for reconfiguration", StringComparison.Ordinal), "missing synthetic reconfiguration error");
        Check(rejected is null, "failed API snapshot nil");
        Check(session.Snapshot().ProjectCollection.apiState.OpenProjects![key] == 1, "rejected API preserves open project reference");
    }

    private static void CloseReleasesCurrentSnapshot()
    {
        using var fixture = new Fixture(new Input("/project/index.ts", "export const value = 1;"));
        var session = fixture.Session;
        OpenFile(session, "file:///project/index.ts", "export const value = 1;");
        var file = session.Snapshot().ProjectCollection.InferredProject()!.Program!.GetSourceFile(RootedFilePath.FromGoString("/project/index.ts"))!;
        var key = ParseCaches.KeyForFile(file);
        Check(session.SnapshotHost.parseCache.Has(key), "live snapshot retains source");
        session.Close();
        Check(!session.SnapshotHost.parseCache.Has(key), "Close releases source");
        Check(session.SnapshotHost.programCounter.Len() == 0, "Close releases all programs");
    }

    public static void TestRefCountingCaches()
    {
        List<Exception>? failures = null;
        void Run(string name, Action assertions)
        {
            try
            {
                assertions();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(new InvalidOperationException(name, error));
            }
        }

        Run("parseCache/reuse unchanged file", ReuseUnchangedParseFile);
        Run("parseCache/release file on close", ReleaseParseFileOnClose);
        Run("parseCache/unchanged program does not over-ref", UnchangedProgramDoesNotOverRef);
        Run("parseCache/fallback rebuild does not double-ref changed file", FallbackDoesNotDoubleRefChangedFile);
        Run("parseCache/case-only duplicate loads are released on dispose", CaseOnlyDuplicateReleased);
        Run("parseCache/case-only duplicate imported from multiple files is refcounted once", DuplicateAcrossImportSitesRefCountedOnce);
        Run("extendedConfigCache/release extended configs with project close", ReleaseExtendedConfigOnClose);
        Run("extendedConfigCache/release cache entries for unretained clone", UnretainedCloneReleasesEntries);
        Run("extendedConfigCache/createProgram retains and reloads extended configs from referenced projects", CreateProgramRetainsReferencedExtendedConfigs);
        Run("failed API update preserves API references", FailedAPIUpdatePreservesReferences);
        Run("session close releases the current snapshot", CloseReleasesCurrentSnapshot);
        if (failures is not null)
            throw new AggregateException("original TestRefCountingCaches subtests failed", failures);
    }
}

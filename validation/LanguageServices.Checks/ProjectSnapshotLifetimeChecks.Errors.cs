// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// snapshot_test.go failed API update is not adopted: complete original subtest.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    private static void FailedAPIUpdateNotAdopted()
    {
        using var fixture = new Fixture(new Program.Checks.CompilerBaselineFileSystem.Input("/a.ts", "export const a = 1;"));
        var session = fixture.Session;
        var baseline = session.Snapshot();
        var(rejected, error) = session.SnapshotHost.CloneSnapshot(ProtocolRequestContext.Default, baseline, default, new APISnapshotRequest { RemovePrograms = [Projects.NewSyntheticProjectID(1)] });
        try
        {
            Check(error is not null && error.Message.Contains("synthetic program not found for removal", StringComparison.Ordinal), "API removal error");
            Check(ReferenceEquals(session.Snapshot(), baseline), "failed detached update is not adopted");
            bool panicked = false;
            try
            {
                _ = session.SnapshotHost.CloneSnapshot(ProtocolRequestContext.Default, rejected, default, null);
            }
            catch (Exception)
            {
                panicked = true;
            }

            Check(panicked, "cloning rejected snapshot panics");
        }
        finally
        {
            rejected.Deref();
        }
    }
}

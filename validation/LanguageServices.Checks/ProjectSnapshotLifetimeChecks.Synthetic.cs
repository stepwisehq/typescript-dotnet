// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// snapshot_test.go TestSnapshot: creates and removes synthetic programs subtest.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    private static void SyntheticPrograms()
    {
        using var fixture = new Fixture(new Program.Checks.CompilerBaselineFileSystem.Input("/a.ts", "export const a = 1;"), new Program.Checks.CompilerBaselineFileSystem.Input("/b.ts", "export const b = 1;"));
        var session = fixture.Session;
        var options = new CompilerOptions
        {
            NoLib = Tristate.True
        };
        var create = Slice<APICreateProgramRequest>.Make(2);
        create[0] = new()
        {
            RootFileNames = [RootedFilePath.FromGoString("/a.ts")],
            CompilerOptions = options
        };
        create[1] = new()
        {
            RootFileNames = [RootedFilePath.FromGoString("/b.ts")],
            CompilerOptions = options
        };
        var(created, error) = session.SnapshotHost.CloneSnapshot(ProtocolRequestContext.Default, session.Snapshot(), default, new APISnapshotRequest { CreatePrograms = create });
        try
        {
            if (error is not null)
                throw error;
            var programs = created.CreatedPrograms();
            Check(programs.Length == 2, "created programs count");
            var first = programs[0];
            var second = programs[1];
            Check(first.configFilePath == TsPath.PathKeyFromCanonical(""), "first empty config path");
            Check(second.configFilePath == TsPath.PathKeyFromCanonical(""), "second empty config path");
            var(firstID, ok) = first.ID().Synthetic();
            Check(ok, "first synthetic ID");
            Check(firstID.AsID().Value.EqualsBytes("/dev/null/synthetic/1"u8), "first canonical ID");
            var(removed, removeError) = session.SnapshotHost.CloneSnapshot(ProtocolRequestContext.Default, created, default, new APISnapshotRequest { RemovePrograms = [firstID] });
            try
            {
                if (removeError is not null)
                    throw removeError;
                Check(first is not null, "first project exists");
                Check(second is not null, "second project exists");
                Check(first!.ID() != second!.ID(), "distinct IDs");
                Check(first.CommandLine!.FileNames()!.SequenceEqual(new[] { RootedFilePath.FromGoString("/a.ts") }), "first root names");
                Check(second.CommandLine!.FileNames()!.SequenceEqual(new[] { RootedFilePath.FromGoString("/b.ts") }), "second root names");
                Check(created.ProjectCollection.InferredProject()is null, "no inferred project");
                Check(created.ProjectCollection.SyntheticProjects().Length == 2, "two synthetic projects");
                Check(created.ProjectCollection.LanguageServiceProjects().Length == 0, "no language service projects");
                Check(created.GetLanguageServiceProjectsContainingFile(new DocumentUri(ByteStringValue.FromString("file:///a.ts"))).Length == 0, "no containing service project");
                Check(created.ProjectCollection.GetDefaultProject(TsPath.PathKeyFromCanonical("/a.ts"))is null, "no default project");
                Check(ReferenceEquals(created.ProjectCollection.GetProject(first.ID()), first), "first project retained");
                var(opened, openError) = session.SnapshotHost.CloneSnapshot(ProtocolRequestContext.Default, created, default, new APISnapshotRequest { OpenFiles = new() { [TsPath.PathKeyFromCanonical("/a.ts")] = RootedFilePath.FromGoString("/a.ts") } });
                try
                {
                    if (openError is not null)
                        throw openError;
                    var inferred = opened.ProjectCollection.InferredProject();
                    Check(inferred is not null, "inferred project exists");
                    Check(inferred!.ID().Inferred().Ok, "inferred ID");
                    Check(inferred.configFilePath == TsPath.PathKeyFromCanonical(""), "inferred empty config path");
                    Check(opened.ProjectCollection.LanguageServiceProjects().Length == 1, "one service project");
                    Check(opened.GetLanguageServiceProjectsContainingFile(new DocumentUri(ByteStringValue.FromString("file:///a.ts"))).Length == 1, "one containing service project");
                    Check(ReferenceEquals(opened.ProjectCollection.GetDefaultProject(TsPath.PathKeyFromCanonical("/a.ts")), inferred), "inferred default");
                    Check(ReferenceEquals(opened.ProjectCollection.GetProject(first.ID()), first), "synthetic preserved when opening");
                    Check(removed.ProjectCollection.GetProject(first.ID())is null, "first removed");
                    Check(ReferenceEquals(removed.ProjectCollection.GetProject(second.ID()), second), "second retained");
                    Check(removed.ProjectCollection.SyntheticProjects().Length == 1, "one remaining synthetic");
                }
                finally
                {
                    opened.Deref();
                }
            }
            finally
            {
                removed.Deref();
            }
        }
        finally
        {
            created.Deref();
        }
    }
}

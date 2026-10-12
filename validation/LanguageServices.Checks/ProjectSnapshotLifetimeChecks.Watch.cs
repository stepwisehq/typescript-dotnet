// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original snapshot_test.go no-op watch change and rejected API/host flush assertions.
// Requires same-package pending queue/mutex visibility, not a substitute public event.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    private static void EnqueueWatch(Session session, string uri)
    {
        lock (session.pendingFileChangesMu)
            session.pendingFileChanges = session.pendingFileChanges.Append(new FileChange { Kind = FileChangeKind.WatchChange, URI = URI(uri) });
    }

    private static void RejectedAPIStillFlushesHostChanges()
    {
        using var fixture = new Fixture(new Input("/a.ts", "export const a = 1;"));
        var baseline = fixture.Session.Snapshot();
        EnqueueWatch(fixture.Session, "file:///a.ts");
        var(rejected, error) = fixture.Session.APIUpdate(ProtocolRequestContext.Default, default, new APISnapshotRequest { RemovePrograms = [Projects.NewSyntheticProjectID(1)] });
        Check(error is not null && error.Message.Contains("synthetic program not found for removal", StringComparison.Ordinal), "API removal error");
        Check(rejected is null, "failed API update returns nil snapshot");
        Check(!ReferenceEquals(fixture.Session.Snapshot(), baseline), "flushed host changes advance canonical snapshot");
    }

    private static void NoOpWatchPreservesProgram()
    {
        using var fixture = new Fixture(new Input("/home/projects/TS/p1/tsconfig.json", "{}"), new Input("/home/projects/TS/p1/index.ts", "import { a } from './a'; console.log(a);"), new Input("/home/projects/TS/p1/a.ts", "export const a = 1;"));
        var session = fixture.Session;
        var uri = URI("file:///home/projects/TS/p1/index.ts");
        var config = TsPath.PathKeyFromCanonical("/home/projects/ts/p1/tsconfig.json");
        OpenFile(session, "file:///home/projects/TS/p1/index.ts", "import { a } from './a'; console.log(a);");
        GetService(session, uri);
        var before = session.Snapshot().ProjectCollection.ConfiguredProject(config)!.Program;
        EnqueueWatch(session, "file:///home/projects/TS/p1/a.ts");
        GetService(session, uri);
        var after = session.Snapshot().ProjectCollection.ConfiguredProject(config)!.Program;
        Check(ReferenceEquals(before, after), "no-op watch change should not rebuild the program");
        var error = fixture.FS.WriteFile(RootedFilePath.FromGoString("/home/projects/TS/p1/a.ts"), "export const a = 2;");
        if (error is not null)
            throw error;
        EnqueueWatch(session, "file:///home/projects/TS/p1/a.ts");
        GetService(session, uri);
        var changed = session.Snapshot().ProjectCollection.ConfiguredProject(config)!.Program;
        Check(!ReferenceEquals(before, changed), "real watch change should rebuild the program");
    }
}

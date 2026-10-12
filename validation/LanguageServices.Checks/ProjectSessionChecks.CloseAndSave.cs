// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestSession DidCloseFile/DidSaveFile groups; no whole credit while draft.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProjectSessionChecks
{
    private static void DidCloseFileOriginalAssertions()
    {
        foreach (bool inferred in new[]
        {
            false,
            true
        }

        )
        {
            using var fixture = new ProjectSessionFixture(inferred ? DefaultFiles.Where(input => input.Path != "/home/projects/TS/p1/tsconfig.json") : DefaultFiles);
            var session = fixture.Session;
            Open(session, "file:///home/projects/TS/p1/src/x.ts", "export const x = 1;");
            Open(session, "file:///home/projects/TS/p1/src/index.ts", "import { x } from \"./x\";");
            var error = fixture.MutableFS.Remove(RootedPath.FromGoString("/home/projects/TS/p1/src/x.ts"));
            if (error is not null)
                throw error;
            session.DidCloseFile(ProtocolRequestContext.Default, Uri("file:///home/projects/TS/p1/src/x.ts"));
            Require(Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram().GetSourceFile(File("/home/projects/TS/p1/src/x.ts"))is null, "deleted then closed file must leave the program");
            error = fixture.MutableFS.WriteFile(File("/home/projects/TS/p1/src/x.ts"), "");
            if (error is not null)
                throw error;
            Open(session, "file:///home/projects/TS/p1/src/x.ts", "");
            var file = Service(session, "file:///home/projects/TS/p1/src/x.ts").GetProgram().GetSourceFile(File("/home/projects/TS/p1/src/x.ts"));
            Require(file is not null && ByteStringValue.FromView(file.TextView()).Length == 0, "recreated empty source must enter the program");
        }

        using (var fixture = new ProjectSessionFixture(DefaultFiles))
        {
            Open(fixture.Session, "untitled:Untitled-1", "let x = 1;");
            fixture.Session.DidCloseFile(ProtocolRequestContext.Default, Uri("untitled:Untitled-1"));
            Open(fixture.Session, "untitled:Untitled-2", "");
        }
    }

    private static void DidSaveFileOriginalAssertions()
    {
        foreach (bool saveFirst in new[]
        {
            true,
            false
        }

        )
        {
            using var fixture = new ProjectSessionFixture(DefaultFiles);
            var session = fixture.Session;
            const string uri = "file:///home/projects/TS/p1/src/index.ts";
            Open(session, uri, "import { x } from \"./x\";");
            Require(session.Snapshot().ID() == 1, "opening first file should create snapshot1");
            if (saveFirst)
                session.DidSaveFile(ProtocolRequestContext.Default, Uri(uri));
            WatchChange(session, uri);
            if (!saveFirst)
                session.DidSaveFile(ProtocolRequestContext.Default, Uri(uri));
            session.WaitForBackgroundTasks();
            Require(session.Snapshot().ID() == 1, "save/watch must not force an unnecessary snapshot");
            Open(session, "file:///home/projects/TS/p1/src/x.ts", "export const x = 1;");
            var file = session.Snapshot().GetFile(File("/home/projects/TS/p1/src/index.ts"));
            Require(file is not null && file.MatchesDiskText(), "next snapshot must retain updated overlay disk-match state");
        }
    }
}

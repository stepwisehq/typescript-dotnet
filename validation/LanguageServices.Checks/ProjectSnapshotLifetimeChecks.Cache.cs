// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// snapshot_test.go cached disk files and nonexistent-file original subtests.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    private static void OpenFile(Session session, string uri, string content) => session.DidOpenFile(ProtocolRequestContext.Default, new DocumentUri(ByteStringValue.FromString(uri)), 1, ByteStringValue.FromString(content), LanguageKind.TypeScript);
    private static void CachedDiskFilesCleanedUp()
    {
        using var fixture = new Fixture(new Input("/home/projects/TS/p1/tsconfig.json", "{}"), new Input("/home/projects/TS/p1/index.ts", "import { a } from './a'; console.log(a);"), new Input("/home/projects/TS/p1/a.ts", "export const a = 1;"), new Input("/home/projects/TS/p2/tsconfig.json", "{}"), new Input("/home/projects/TS/p2/index.ts", "import { b } from './b'; console.log(b);"), new Input("/home/projects/TS/p2/b.ts", "export const b = 2;"));
        var session = fixture.Session;
        OpenFile(session, "file:///home/projects/TS/p1/index.ts", "import { a } from './a'; console.log(a);");
        OpenFile(session, "file:///home/projects/TS/p2/index.ts", "import { b } from './b'; console.log(b);");
        var before = session.Snapshot();
        var a = TsPath.PathKeyFromCanonical("/home/projects/ts/p1/a.ts");
        var b = TsPath.PathKeyFromCanonical("/home/projects/ts/p2/b.ts");
        List<Exception>? failures = null;
        void Observe(bool condition, string message)
        {
            if (!condition)
                (failures ??= new()).Add(new InvalidOperationException(message));
        }

        Observe(before.fs.cacheFiles?.GetValueOrDefault(a)is not null, "a.ts cached before close");
        Observe(before.fs.cacheFiles?.GetValueOrDefault(b)is not null, "b.ts cached before close");
        session.DidCloseFile(ProtocolRequestContext.Default, new DocumentUri(ByteStringValue.FromString("file:///home/projects/TS/p1/index.ts")));
        OpenFile(session, "untitled:Untitled-1", "");
        var after = session.Snapshot();
        Observe(after.fs.cacheFiles?.GetValueOrDefault(a)is null, "a.ts removed after project close");
        Observe(after.fs.cacheFiles?.GetValueOrDefault(b)is not null, "b.ts remains cached");
        if (failures is not null)
            throw new AggregateException(failures);
    }

    private static void NonexistentFiles()
    {
        using var fixture = new Fixture(new Input("/home/projects/TS/p1/tsconfig.json", "{}"), new Input("/home/projects/TS/p1/index.ts", "console.log('Hello, world!');"));
        OpenFile(fixture.Session, "file:///home/projects/TS/p1/index.ts", "console.log('Hello, world!');");
        var snapshot = fixture.Session.Snapshot();
        var path = RootedFilePath.FromGoString("/home/projects/TS/p1/nonexistent.ts");
        var handle = snapshot.GetFile(path);
        var(_, ok) = snapshot.ReadFile(path);
        List<Exception>? failures = null;
        if (handle is not null)
            (failures ??= new()).Add(new InvalidOperationException("GetFile should return nil for non-existent file"));
        if (ok)
            (failures ??= new()).Add(new InvalidOperationException("ReadFile should return false for non-existent file"));
        if (failures is not null)
            throw new AggregateException(failures);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// snapshot_test.go actual recomputed options/adopted auto-import original subtests.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    private static DocumentUri URI(string uri) => new(ByteStringValue.FromString(uri));
    private static void GetService(Session session, DocumentUri uri)
    {
        var(_, error) = session.GetLanguageService(ProtocolRequestContext.Default, uri);
        if (error is not null)
            throw error;
    }

    private static void RecomputedParseOptions()
    {
        using var fixture = new Fixture(new Input("/project/node_modules/pkg/index.ts", "export const pkg = 0;"), new Input("/project/src/other.ts", "export const other = 1;"));
        var session = fixture.Session;
        var pkg = URI("file:///project/node_modules/pkg/index.ts");
        var other = URI("file:///project/src/other.ts");
        OpenFile(session, "file:///project/node_modules/pkg/index.ts", "export const pkg = 0;");
        OpenFile(session, "file:///project/src/other.ts", "export const other = 1;");
        GetService(session, pkg);
        var writeError = fixture.FS.WriteFile(RootedFilePath.FromGoString("/project/node_modules/pkg/package.json"), "{ \"type\": \"module\" }");
        if (writeError is not null)
            throw writeError;
        session.DidChangeFile(ProtocolRequestContext.Default, pkg, 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new() { Text = ByteStringValue.FromString("import \"./missing\"; export const pkg = 1;") } }]));
        GetService(session, pkg);
        session.DidChangeFile(ProtocolRequestContext.Default, other, 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new() { Text = ByteStringValue.FromString("export const other = 2;") } }]));
        GetService(session, other);
    }

    private static void AutoImportSnapshotAdopted()
    {
        using var fixture = new Fixture(new Input("/home/projects/TS/p1/tsconfig.json", "{}"), new Input("/home/projects/TS/p1/index.ts", "const value = foo;"), new Input("/home/projects/TS/p1/foo.ts", "export const foo = 1;"));
        var session = fixture.Session;
        var uri = URI("file:///home/projects/TS/p1/index.ts");
        OpenFile(session, "file:///home/projects/TS/p1/index.ts", "const value = foo;");
        GetService(session, uri);
        var baseline = session.Snapshot();
        var prepared = session.SnapshotHost.CloneSnapshotWithAutoImports(ProtocolRequestContext.Default, baseline, uri, null);
        try
        {
            session.TryAdoptSnapshotInBackground(baseline, prepared);
            session.WaitForBackgroundTasks();
            Check(ReferenceEquals(session.Snapshot(), prepared), "prepared auto-import snapshot adopted");
        }
        finally
        {
            prepared.Deref();
        }
    }
}

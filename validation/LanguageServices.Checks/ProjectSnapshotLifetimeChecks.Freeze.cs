// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// snapshot_test.go compilerHost gets frozen with snapshot FS only once original.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    private static void CompilerHostFrozenOnlyOnce()
    {
        using var fixture = new Fixture(new Input("/home/projects/TS/p1/tsconfig.json", "{}"), new Input("/home/projects/TS/p1/index.ts", "console.log('Hello, world!');"));
        var session = fixture.Session;
        OpenFile(session, "file:///home/projects/TS/p1/index.ts", "console.log('Hello, world!');");
        OpenFile(session, "untitled:Untitled-1", "");
        var before = session.Snapshot();
        var uri = URI("file:///home/projects/TS/p1/index.ts");
        session.DidChangeFile(ProtocolRequestContext.Default, uri, 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { Partial = new() { Text = ByteStringValue.FromString("\n"), Range = new() { Start = new() { Line = 0, Character = 24 }, End = new() { Line = 0, Character = 24 } } } }]));
        GetService(session, uri);
        var after = session.Snapshot();
        Check(after.ProjectCollection.ConfiguredProject(TsPath.PathKeyFromCanonical("/home/projects/ts/p1/tsconfig.json"))!.ProgramUpdateKind == ProgramUpdateKind.Cloned, "configured project updated by clone");
        Check(ReferenceEquals(before.ProjectCollection.InferredProject(), after.ProjectCollection.InferredProject()), "inferred project unchanged");
        Check(after.ProjectCollection.InferredProject()!.ProgramUpdateKind == ProgramUpdateKind.NewFiles, "inferred update kind retained");
        Check(ReferenceEquals(after.ProjectCollection.InferredProject()!.host!.sourceFS.source, before.fs), "inferred host retains previous frozen filesystem");
    }
}

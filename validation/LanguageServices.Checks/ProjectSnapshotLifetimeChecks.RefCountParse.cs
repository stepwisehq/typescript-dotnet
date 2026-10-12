// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// refcountcache_test.go TestRefCountingCaches parse-cache lifetime subtests.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    private const string Main = "/user/username/projects/myproject/src/main.ts";
    private const string Utils = "/user/username/projects/myproject/src/utils.ts";
    private static Fixture ParseFixture(string main = "const x = 1;", string utils = "export function util() {}") => new(new Input(Main, main), new Input(Utils, utils));
    private static void OpenParseFiles(Session session)
    {
        OpenFile(session, "file://" + Main, "const x = 1;");
        OpenFile(session, "file://" + Utils, "export function util() {}");
    }

    private static void Replace(Session session, string path, string text, int version = 2) => session.DidChangeFile(ProtocolRequestContext.Default, URI("file://" + path), version, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new() { Text = ByteStringValue.FromString(text) } }]));
    private static void ReplaceRange(Session session, string path, uint end, string text) => session.DidChangeFile(ProtocolRequestContext.Default, URI("file://" + path), 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { Partial = new() { Text = ByteStringValue.FromString(text), Range = new() { Start = new() { Line = 0, Character = 0 }, End = new() { Line = 0, Character = end } } } }]));
    private static TypeScript.Compiler.Program CurrentProgram(Session session, string path)
    {
        var(service, error) = session.GetLanguageService(ProtocolRequestContext.Default, URI("file://" + path));
        if (error is not null)
            throw error;
        return service!.GetProgram();
    }

    private static RefCountCache<ParseCacheKey, SourceFile, IFileHandle>.Entry ParseEntry(Session session, SourceFile file)
    {
        var(entry, ok) = session.SnapshotHost.parseCache.entries.Load(ParseCaches.KeyForFile(file));
        Check(ok, "parse entry exists");
        return entry;
    }

    private static void ReuseUnchangedParseFile()
    {
        using var fixture = ParseFixture();
        var session = fixture.Session;
        OpenParseFiles(session);
        var program = session.Snapshot().ProjectCollection.InferredProject()!.Program!;
        var main = program.GetSourceFile(RootedFilePath.FromGoString(Main))!;
        var utils = program.GetSourceFile(RootedFilePath.FromGoString(Utils))!;
        var mainEntry = ParseEntry(session, main);
        var utilsEntry = ParseEntry(session, utils);
        Check(mainEntry.RefCount == 1, "initial main refCount");
        Check(utilsEntry.RefCount == 1, "initial utils refCount");
        ReplaceRange(session, Main, 12, "const x = 2;");
        var next = CurrentProgram(session, Main);
        session.WaitForBackgroundTasks();
        var newMain = next.GetSourceFile(RootedFilePath.FromGoString(Main))!;
        var newEntry = ParseEntry(session, newMain);
        Check(!ReferenceEquals(newMain, main), "changed main source identity");
        Check(!ReferenceEquals(newEntry, mainEntry), "changed main cache entry identity");
        Check(ReferenceEquals(next.GetSourceFile(RootedFilePath.FromGoString(Utils)), utils), "unchanged utils source identity");
        Check(mainEntry.RefCount == 0, "old main released");
        Check(newEntry.RefCount == 1, "new main retained once");
        Check(utilsEntry.RefCount == 1, "utils retained once");
    }

    private static void ReleaseParseFileOnClose()
    {
        using var fixture = ParseFixture();
        var session = fixture.Session;
        OpenParseFiles(session);
        var program = session.Snapshot().ProjectCollection.InferredProject()!.Program!;
        var main = program.GetSourceFile(RootedFilePath.FromGoString(Main))!;
        var utils = program.GetSourceFile(RootedFilePath.FromGoString(Utils))!;
        var mainEntry = ParseEntry(session, main);
        var utilsEntry = ParseEntry(session, utils);
        Check(mainEntry.RefCount == 1, "initial main reference");
        Check(utilsEntry.RefCount == 1, "initial utils reference");
        session.DidCloseFile(ProtocolRequestContext.Default, URI("file://" + Main));
        CurrentProgram(session, Utils);
        session.WaitForBackgroundTasks();
        Check(utilsEntry.RefCount == 1, "utils reference retained");
        Check(mainEntry.RefCount == 0, "main reference released");
        Check(!session.SnapshotHost.parseCache.entries.Load(ParseCaches.KeyForFile(main)).Ok, "closed main entry deleted");
    }

    private static void UnchangedProgramDoesNotOverRef()
    {
        using var fixture = ParseFixture();
        var session = fixture.Session;
        OpenParseFiles(session);
        var program = session.Snapshot().ProjectCollection.InferredProject()!.Program!;
        var main = program.GetSourceFile(RootedFilePath.FromGoString(Main))!;
        Check(ParseEntry(session, main).RefCount == 1, "initial refCount should be1");
        ReplaceRange(session, Utils, 25, "export function util2() {}");
        var next = CurrentProgram(session, Main);
        session.WaitForBackgroundTasks();
        Check(ReferenceEquals(main, next.GetSourceFile(RootedFilePath.FromGoString(Main))), "main.ts source reused");
        Check(ParseEntry(session, main).RefCount == 1, "only new snapshot retains main");
        session.DidCloseFile(ProtocolRequestContext.Default, URI("file://" + Main));
        session.DidCloseFile(ProtocolRequestContext.Default, URI("file://" + Utils));
        OpenFile(session, "untitled:Untitled-1", "");
        session.WaitForBackgroundTasks();
        var(entry, ok) = session.SnapshotHost.parseCache.entries.Load(ParseCaches.KeyForFile(main));
        if (ok)
            Console.WriteLine("Entry still exists with refCount=" + entry.RefCount);
        Check(!ok, "entry deleted after program disposed");
    }

    private static void FallbackDoesNotDoubleRefChangedFile()
    {
        using var fixture = ParseFixture("const x = 1;", "export const util = 1;");
        var session = fixture.Session;
        OpenFile(session, "file://" + Main, "const x = 1;");
        CurrentProgram(session, Main);
        Replace(session, Main, "import { util } from \"./utils\";\nconst x = util;");
        var next = CurrentProgram(session, Main);
        session.WaitForBackgroundTasks();
        var project = session.Snapshot().ProjectCollection.InferredProject();
        Check(project is not null, "inferred project");
        Check(project!.ProgramUpdateKind == ProgramUpdateKind.NewFiles, "full fallback rebuild");
        var main = next.GetSourceFile(RootedFilePath.FromGoString(Main))!;
        var key = ParseCaches.KeyForFile(main);
        Check(ParseEntry(session, main).RefCount == 1, "changed main retained once");
        session.DidCloseFile(ProtocolRequestContext.Default, URI("file://" + Main));
        OpenFile(session, "untitled:Untitled-1", "");
        session.WaitForBackgroundTasks();
        Check(!session.SnapshotHost.parseCache.entries.Load(key).Ok, "changed main removed after close");
    }
}

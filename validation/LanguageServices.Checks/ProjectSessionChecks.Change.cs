// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestSession change/watch-open subtest assertions; whole credit pending.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class ProjectSessionChecks
{
    private static RootedFilePath File(string raw) => RootedFilePath.FromGoString(raw);
    private static void Change(Session session, string uri, uint start, uint end, string raw) => session.DidChangeFile(ProtocolRequestContext.Default, Uri(uri), 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { Partial = new() { Range = new() { Start = new() { Line = 0, Character = start }, End = new() { Line = 0, Character = end } }, Text = ByteStringValue.FromString(raw) } }]));
    private static void WatchChange(Session session, string uri) => session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Uri = Uri(uri), Type = FileChangeType.Changed }]));
    private static void WatchChangeAndDidOpenSameBatchOriginalAssertions()
    {
        using var fixture = new ProjectSessionFixture([new BaselineFS.Input("/home/projects/TS/p1/tsconfig.json", "{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"noLib\": true,\n\t\t\t\t\t\"strict\": true\n\t\t\t\t}\n\t\t\t}"), new BaselineFS.Input("/home/projects/TS/p1/src/a.ts", "export const a = 1;\n"), new BaselineFS.Input("/home/projects/TS/p1/src/b.ts", "export const b = 1;\n")]);
        var session = fixture.Session;
        Open(session, "file:///home/projects/TS/p1/src/b.ts", "export const b = 1;\n");
        var before = Service(session, "file:///home/projects/TS/p1/src/b.ts").GetProgram().GetSourceFile(File("/home/projects/TS/p1/src/a.ts"));
        Require(before is not null && ByteStringValue.FromView(before.TextView()).EqualsBytes("export const a = 1;\n"u8), "expected original a.ts content");
        const string changed = "export const a = 2;\nexport const extra = true;\n";
        var error = fixture.MutableFS.WriteFile(File("/home/projects/TS/p1/src/a.ts"), changed);
        if (error is not null)
            throw error;
        WatchChange(session, "file:///home/projects/TS/p1/src/a.ts");
        Open(session, "file:///home/projects/TS/p1/src/a.ts", changed);
        var after = Service(session, "file:///home/projects/TS/p1/src/a.ts").GetProgram().GetSourceFile(File("/home/projects/TS/p1/src/a.ts"));
        Require(after is not null && ByteStringValue.FromView(after.TextView()).EqualsValue(ByteStringValue.FromString(changed)), "watch and open in one batch must retain the overlay content");
    }

    private static void DidChangeFileOriginalAssertions()
    {
        using (var fixture = new ProjectSessionFixture(DefaultFiles))
        {
            var session = fixture.Session;
            const string uri = "file:///home/projects/TS/p1/src/x.ts";
            Open(session, uri, "export const x = 1;");
            var before = Service(session, uri).GetProgram();
            Change(session, uri, 17, 18, "2");
            var after = Service(session, uri).GetProgram();
            Require(!ReferenceEquals(before, after), "content change must update program");
            var file = after.GetSourceFile(File("/home/projects/TS/p1/src/x.ts"));
            Require(file is not null && ByteStringValue.FromView(file.TextView()).EqualsBytes("export const x = 2;"u8), "expected changed content");
        }

        using (var fixture = new ProjectSessionFixture(DefaultFiles))
        {
            var session = fixture.Session;
            const string uri = "untitled:Untitled-1";
            Open(session, uri, "let x = 1;");
            var before = Service(session, uri).GetProgram();
            var name = Uri(uri).FileName();
            var file = before.GetSourceFile(name);
            Require(file is not null && ByteStringValue.FromView(file.TextView()).EqualsBytes("let x = 1;"u8), "expected original untitled content");
            Change(session, uri, 8, 9, "2");
            var after = Service(session, uri).GetProgram();
            file = after.GetSourceFile(name);
            Require(!ReferenceEquals(before, after), "untitled update must update program");
            Require(file is not null && ByteStringValue.FromView(file.TextView()).EqualsBytes("let x = 2;"u8), "expected changed untitled content");
        }

        using (var fixture = new ProjectSessionFixture(DefaultFiles))
        {
            var session = fixture.Session;
            const string uri = "file:///home/projects/TS/p1/src/x.ts";
            Open(session, uri, "export const x = 1;");
            var before = Service(session, uri).GetProgram();
            var unchanged = before.GetSourceFile(File("/home/projects/TS/p1/src/index.ts"));
            Change(session, uri, 0, 0, ";");
            var after = Service(session, uri).GetProgram();
            Require(ReferenceEquals(unchanged, after.GetSourceFile(File("/home/projects/TS/p1/src/index.ts"))), "unchanged source owner must be reused");
        }

        using (var fixture = new ProjectSessionFixture(DefaultFiles.Append(new BaselineFS.Input("/home/projects/TS/p1/y.ts", "export const y = 2;"))))
        {
            var session = fixture.Session;
            const string uri = "file:///home/projects/TS/p1/src/index.ts";
            Open(session, uri, "import { x } from \"./x\";");
            var before = Service(session, uri).GetProgram();
            Require(before.GetSourceFile(File("/home/projects/TS/p1/y.ts"))is null, "excluded y.ts must start absent");
            // Original Go raw-string input intentionally contains backslash+n.
            Change(session, uri, 0, 0, "import { y } from \"../y\";\\n");
            Require(Service(session, uri).GetProgram().GetSourceFile(File("/home/projects/TS/p1/y.ts"))is not null, "new import must pull in y.ts");
        }

        var files = DefaultFiles.Select(input => input.Path == "/home/projects/TS/p1/tsconfig.json" ? new BaselineFS.Input(input.Path, "{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"noLib\": true,\n\t\t\t\t\t\"module\": \"nodenext\",\n\t\t\t\t\t\"strict\": true\n\t\t\t\t},\n\t\t\t\t\"include\": [\"src/index.ts\"]\n\t\t\t}") : input);
        using (var fixture = new ProjectSessionFixture(files))
        {
            var session = fixture.Session;
            const string uri = "file:///home/projects/TS/p1/src/index.ts";
            Open(session, uri, "import { x } from \"./x\";");
            Require(Service(session, uri).GetProgram().GetSourceFiles().Length == 2, "initial config should load two source files");
            Change(session, uri, 0, 0, "\n");
            var error = fixture.MutableFS.WriteFile(File("/home/projects/TS/p1/tsconfig.json"), "{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"noLib\": true,\n\t\t\t\t\t\"module\": \"nodenext\",\n\t\t\t\t\t\"strict\": true\n\t\t\t\t},\n\t\t\t\t\"include\": [\"./**/*\"]\n\t\t\t}");
            if (error is not null)
                throw error;
            WatchChange(session, "file:///home/projects/TS/p1/tsconfig.json");
            Require(Service(session, uri).GetProgram().GetSourceFiles().Length == 3, "config reload after single-file change must include third source");
        }
    }
}

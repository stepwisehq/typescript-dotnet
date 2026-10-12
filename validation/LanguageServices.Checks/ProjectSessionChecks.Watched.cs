// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestSession/DidChangeWatchedFiles assertions; draft partial group,
// no whole-function credit until all original groups are present.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProjectSessionChecks
{
    private static void WatchedOpenAndClosedOriginalAssertions()
    {
        using (var fixture = new ProjectSessionFixture(DefaultFiles))
        {
            var session = fixture.Session;
            Open(session, "file:///home/projects/TS/p1/src/x.ts", "export const x = 1;");
            Open(session, "file:///home/projects/TS/p1/src/index.ts", "import { x } from \"./x\";");
            var before = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
            var error = fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/home/projects/TS/p1/src/x.ts"), "export const x = 2;");
            if (error is not null)
                throw error;
            WatchChange(session, "file:///home/projects/TS/p1/src/x.ts");
            Require(ReferenceEquals(before, Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram()), "watch change to open file must retain the program");
        }

        using (var fixture = new ProjectSessionFixture(DefaultFiles))
        {
            var session = fixture.Session;
            Open(session, "file:///home/projects/TS/p1/src/index.ts", "import { x } from \"./x\";");
            var before = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
            var error = fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/home/projects/TS/p1/src/x.ts"), "export const x = 2;");
            if (error is not null)
                throw error;
            WatchChange(session, "file:///home/projects/TS/p1/src/x.ts");
            Require(!ReferenceEquals(before, Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram()), "watch change to closed program file must rebuild the program");
        }
    }

    private static void WatchedNonRootProgramFileOriginalAssertions()
    {
        foreach (var workspace in new[]
        {
            "/",
            "/home/projects/TS/p1",
            "/somewhere/else/entirely"
        }

        )
        {
            Program.Checks.CompilerBaselineFileSystem.Input[] files = [new("/home/projects/TS/p1/tsconfig.json", "{\n\t\t\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\t\t\"noLib\": true,\n\t\t\t\t\t\t\t\t\"module\": \"nodenext\",\n\t\t\t\t\t\t\t\t\"strict\": true\n\t\t\t\t\t\t\t},\n\t\t\t\t\t\t\t\"files\": [\"src/index.ts\"]\n\t\t\t\t\t\t}"), new("/home/projects/TS/p1/src/index.ts", "import { x } from \"../../x\";"), new("/home/projects/TS/x.ts", "export const x = 1;")];
            using var fixture = new ProjectSessionFixture(files, new TypeScript.ProjectSystem.SessionOptions { CurrentDirectory = RootedDirectoryPath.FromGoString(workspace), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), TypingsLocation = RootedDirectoryPath.FromGoString("/home/src/Library/Caches/typescript"), PositionEncoding = PositionEncodingKind.UTF8, WatchEnabled = true, LoggingEnabled = true });
            var session = fixture.Session;
            Open(session, "file:///home/projects/TS/p1/src/index.ts", "import { x } from \"../../x\";");
            var before = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
            session.WaitForBackgroundTasks();
            Require(fixture.WatchesFile(ByteStringValue.FromString("/home/projects/TS/x.ts")), "non-root imported file must be watched independently of workspace cwd");
            var error = fixture.MutableFS.WriteFile(RootedFilePath.FromGoString("/home/projects/TS/x.ts"), "export const x = 2;");
            if (error is not null)
                throw error;
            WatchChange(session, "file:///home/projects/TS/x.ts");
            Require(!ReferenceEquals(before, Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram()), "non-root imported file watch change must rebuild program");
        }
    }

    private static void WatchedConfigDiagnosticsOriginalAssertions()
    {
        using var fixture = new ProjectSessionFixture([new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/tsconfig.json", "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true,\n\t\t\t\t\t\t\"strict\": false\n\t\t\t\t\t}\n\t\t\t\t}"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/src/x.ts", "export declare const x: number | undefined;"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/src/index.ts", "\n\t\t\t\t\timport { x } from \"./x\";\n\t\t\t\t\tlet y: number = x;")]);
        var session = fixture.Session;
        Open(session, "file:///home/projects/TS/p1/src/index.ts", "\n\t\t\t\t\timport { x } from \"./x\";\n\t\t\t\t\tlet y: number = x;");
        var program = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
        using var requestCancellation = new CancellationTokenSource();
        var context = ProtocolRequestContext.Default.WithCancellationToken(requestCancellation.Token).WithRequestID(ByteStringValue.FromString("0"));
        try
        {
            Require(program.GetSemanticDiagnostics(context, program.GetSourceFile(File("/home/projects/TS/p1/src/index.ts"))).Length == 0, "non-strict original program must have zero semantic diagnostics");
            var error = fixture.MutableFS.WriteFile(File("/home/projects/TS/p1/tsconfig.json"), "{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"noLib\": false,\n\t\t\t\t\t\"strict\": true\n\t\t\t\t}\n\t\t\t}");
            if (error is not null)
                throw error;
            WatchChange(session, "file:///home/projects/TS/p1/tsconfig.json");
            program = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
            Require(program.GetSemanticDiagnostics(context, program.GetSourceFile(File("/home/projects/TS/p1/src/index.ts"))).Length == 1, "reloaded strict program must have exactly one semantic diagnostic");
        }
        finally
        {
            requestCancellation.Cancel();
        }
    }

    private static void WatchedDeletionOriginalAssertions()
    {
        var cases = new[]
        {
            (Config: "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"files\": [\"src/index.ts\", \"src/x.ts\"]\n\t\t\t\t}", Active: "/home/projects/TS/p1/src/index.ts", ActiveText: "import { x } from \"./x\";", Closed: "/home/projects/TS/p1/src/x.ts", ClosedText: "export declare const x: number | undefined;", Deleted: "/home/projects/TS/p1/src/x.ts", Explicit: true, Cleanup: true),
            (Config: "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"include\": [\"src\"]\n\t\t\t\t}", Active: "/home/projects/TS/p1/src/x.ts", ActiveText: "let y = x;", Closed: "/home/projects/TS/p1/src/index.ts", ClosedText: "let x = 2;", Deleted: "/home/projects/TS/p1/src/index.ts", Explicit: false, Cleanup: true),
            (Config: "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"include\": [\"src\"]\n\t\t\t\t}", Active: "/home/projects/TS/p1/src/index.ts", ActiveText: "import { x } from \"./sub/x\";", Closed: "/home/projects/TS/p1/src/sub/x.ts", ClosedText: "export const x = 1;", Deleted: "/home/projects/TS/p1/src/sub", Explicit: false, Cleanup: false)
        };
        foreach (var row in cases)
        {
            using var fixture = new ProjectSessionFixture([new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/tsconfig.json", row.Config), new Program.Checks.CompilerBaselineFileSystem.Input(row.Active, row.ActiveText), new Program.Checks.CompilerBaselineFileSystem.Input(row.Closed, row.ClosedText)]);
            using var request = new CancellationTokenSource();
            try
            {
                var context = ProtocolRequestContext.Default.WithCancellationToken(request.Token).WithRequestID(ByteStringValue.FromString("0"));
                var session = fixture.Session;
                var activeUri = "file://" + row.Active;
                Open(session, activeUri, row.ActiveText);
                var program = Service(session, activeUri).GetProgram();
                Require(program.CommandLine().ParsedConfig.FileNames!.Contains(File(row.Closed)), "original root names must include closed file");
                Require(program.GetSemanticDiagnostics(context, program.GetSourceFile(File(row.Active))).Length == 0, "original program must have zero semantic diagnostics");
                var error = fixture.MutableFS.Remove(RootedPath.FromGoString(row.Deleted));
                if (error is not null)
                    throw error;
                session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Deleted, Uri = Uri("file://" + row.Deleted) }]));
                program = Service(session, activeUri).GetProgram();
                Require(program.CommandLine().ParsedConfig.FileNames!.Contains(File(row.Closed)) == row.Explicit, "explicit root must survive deletion; wildcard root must be removed");
                Require(program.GetSemanticDiagnostics(context, program.GetSourceFile(File(row.Active))).Length == 1, "deleted dependency must produce exactly one semantic diagnostic");
                if (row.Explicit)
                    Require(program.GetSourceFile(File(row.Closed))is null, "deleted explicit source must leave program");
                if (row.Cleanup)
                {
                    Open(session, "untitled:Untitled-1", "");
                    Require(session.Snapshot().GetFile(File(row.Closed))is null, "deleted file must leave snapshot after cleanup trigger");
                }
            }
            finally
            {
                request.Cancel();
            }
        }
    }

    private static void WatchedProgramOnlyDirectoryDeletionOriginalAssertions()
    {
        using var fixture = new ProjectSessionFixture([new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/tsconfig.json", "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"files\": [\"src/index.ts\"]\n\t\t\t\t}"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/src/index.ts", "import { x } from \"./sub/x\";"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/src/sub/x.ts", "export const x = 1;")]);
        using var request = new CancellationTokenSource();
        try
        {
            var context = ProtocolRequestContext.Default.WithCancellationToken(request.Token).WithRequestID(ByteStringValue.FromString("0"));
            var session = fixture.Session;
            Open(session, "file:///home/projects/TS/p1/src/index.ts", "import { x } from \"./sub/x\";");
            var program = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
            Require(program.CommandLine().ParsedConfig.FileNames!.Contains(File("/home/projects/TS/p1/src/index.ts")), "explicit active root must be retained");
            Require(program.GetSourceFile(File("/home/projects/TS/p1/src/sub/x.ts"))is not null, "imported file must be in program although absent from config files");
            Require(program.GetSemanticDiagnostics(context, program.GetSourceFile(File("/home/projects/TS/p1/src/index.ts"))).Length == 0, "original import must have no diagnostics");
            var error = fixture.MutableFS.Remove(RootedPath.FromGoString("/home/projects/TS/p1/src/sub"));
            if (error is not null)
                throw error;
            session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Deleted, Uri = Uri("file:///home/projects/TS/p1/src/sub") }]));
            program = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
            Require(program.GetSourceFile(File("/home/projects/TS/p1/src/sub/x.ts"))is null, "deleted program-only dependency must leave program");
            Require(program.GetSemanticDiagnostics(context, program.GetSourceFile(File("/home/projects/TS/p1/src/index.ts"))).Length == 1, "deleted imported module must produce exactly one diagnostic");
        }
        finally
        {
            request.Cancel();
        }
    }

    private static void WatchedSiblingDirectoryRefreshOriginalAssertions()
    {
        foreach (bool openThird in new[]
        {
            false,
            true
        }

        )
        {
            var files = new List<Program.Checks.CompilerBaselineFileSystem.Input>
            {
                new("/home/projects/TS/p1/tsconfig.json", openThird ? "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"files\": [\"index.ts\", \"third.ts\"]\n\t\t\t\t}" : "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"files\": [\"index.ts\"]\n\t\t\t\t}"),
                new("/home/projects/TS/p1/index.ts", "import { content } from \"./f/content\";\n\nexport const value = content;"),
                new("/home/projects/TS/p1/f/content.ts", "export const content = 1;")
            };
            if (openThird)
                files.Add(new("/home/projects/TS/p1/third.ts", "export const third = 3;"));
            using var fixture = new ProjectSessionFixture(files);
            var session = fixture.Session;
            Open(session, "file:///home/projects/TS/p1/index.ts", "import { content } from \"./f/content\";\n\nexport const value = content;");
            Open(session, "file:///home/projects/TS/p1/f/content.ts", "export const content = 1;");
            _ = Service(session, "file:///home/projects/TS/p1/index.ts");
            session.WaitForBackgroundTasks();
            int before = fixture.Client.RefreshDiagnosticsCallCount();
            var error = fixture.MutableFS.Remove(RootedPath.FromGoString("/home/projects/TS/p1/f"));
            if (error is not null)
                throw error;
            session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Deleted, Uri = Uri("file:///home/projects/TS/p1/f") }]));
            if (openThird)
                Open(session, "file:///home/projects/TS/p1/third.ts", "export const third = 3;");
            else
                session.DidCloseFile(ProtocolRequestContext.Default, Uri("file:///home/projects/TS/p1/f/content.ts"));
            session.WaitForBackgroundTasks();
            Require(fixture.Client.RefreshDiagnosticsCallCount() > before, "sibling directory deletion must schedule diagnostics refresh after the original follow-up document event");
        }
    }

    private static void WatchedCreationOriginalAssertions()
    {
        var cases = new[]
        {
            (Config: "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"files\": [\"src/index.ts\", \"src/y.ts\"]\n\t\t\t\t}", ActiveText: "import { y } from \"./y\";", Created: "/home/projects/TS/p1/src/y.ts", CreatedText: "export const y = 1;"),
            (Config: "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"files\": [\"src/index.ts\"]\n\t\t\t\t}", ActiveText: "import { z } from \"./z\";", Created: "/home/projects/TS/p1/src/z.ts", CreatedText: "export const z = 1;"),
            (Config: "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"include\": [\"src\"]\n\t\t\t\t}", ActiveText: "a;", Created: "/home/projects/TS/p1/src/a.ts", CreatedText: "const a = 1;"),
            (Config: "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"files\": [\"src/index.ts\"]\n\t\t\t\t}", ActiveText: "import { helper } from \"./lib/helper\";", Created: "/home/projects/TS/p1/src/lib/helper.ts", CreatedText: "export const helper = 1;")
        };
        foreach (var row in cases)
        {
            using var fixture = new ProjectSessionFixture([new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/tsconfig.json", row.Config), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/src/index.ts", row.ActiveText)]);
            using var request = new CancellationTokenSource();
            try
            {
                var context = ProtocolRequestContext.Default.WithCancellationToken(request.Token).WithRequestID(ByteStringValue.FromString("0"));
                var session = fixture.Session;
                Open(session, "file:///home/projects/TS/p1/src/index.ts", row.ActiveText);
                var program = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
                Require(program.GetSemanticDiagnostics(context, program.GetSourceFile(File("/home/projects/TS/p1/src/index.ts"))).Length == 1, "missing declaration must produce exactly one diagnostic");
                var error = fixture.MutableFS.WriteFile(File(row.Created), row.CreatedText);
                if (error is not null)
                    throw error;
                session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Created, Uri = Uri("file://" + row.Created) }]));
                program = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
                Require(program.GetSemanticDiagnostics(context, program.GetSourceFile(File("/home/projects/TS/p1/src/index.ts"))).Length == 0, "created declaration must resolve diagnostic");
                Require(program.GetSourceFile(File(row.Created))is not null, "created file must enter program");
            }
            finally
            {
                request.Cancel();
            }
        }
    }

    private static void WatchedIrrelevantExtensionsOriginalAssertions()
    {
        using var fixture = new ProjectSessionFixture([new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/tsconfig.json", "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"include\": [\"src\"]\n\t\t\t\t}"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/src/index.ts", "export const x = 1;"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/src/data.txt", "some text")]);
        using var request = new CancellationTokenSource();
        try
        {
            var context = ProtocolRequestContext.Default.WithCancellationToken(request.Token).WithRequestID(ByteStringValue.FromString("0"));
            var session = fixture.Session;
            Open(session, "file:///home/projects/TS/p1/src/index.ts", "export const x = 1;");
            var before = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
            Require(before.GetSemanticDiagnostics(context, before.GetSourceFile(File("/home/projects/TS/p1/src/index.ts"))).Length == 0, "initial program must have zero diagnostics");
            var error = fixture.MutableFS.WriteFile(File("/home/projects/TS/p1/src/data.txt"), "updated text");
            if (error is not null)
                throw error;
            session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Changed, Uri = Uri("file:///home/projects/TS/p1/src/data.txt") }, new() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/TS/p1/src/styles.css") }, new() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/TS/p1/src/image.png") }]));
            Require(ReferenceEquals(before, Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram()), "irrelevant extension changes must retain the program");
        }
        finally
        {
            request.Cancel();
        }
    }

    private static void WatchedPnpmLinkOriginalAssertions()
    {
        using var fixture = new ProjectSessionFixture([new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/pnpm/pnpm-workspace.yaml", "packages:\n  - 'packages/*'"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/pnpm/packages/alpha/package.json", "{ \"name\": \"@repo/alpha\", \"main\": \"index.ts\" }"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/pnpm/packages/alpha/tsconfig.json", "{\n\t\t\t\t\t\"compilerOptions\": { \"noLib\": true, \"composite\": true }\n\t\t\t\t}"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/pnpm/packages/alpha/index.ts", "export const alpha = 1;"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/pnpm/packages/beta/package.json", "{ \"name\": \"@repo/beta\" }"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/pnpm/packages/beta/tsconfig.json", "{\n\t\t\t\t\t\"compilerOptions\": { \"noLib\": true }\n\t\t\t\t}"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/pnpm/packages/beta/index.ts", "import { alpha } from \"@repo/alpha\";")]);
        using var request = new CancellationTokenSource();
        try
        {
            var context = ProtocolRequestContext.Default.WithCancellationToken(request.Token).WithRequestID(ByteStringValue.FromString("0"));
            var session = fixture.Session;
            Open(session, "file:///home/projects/pnpm/packages/beta/index.ts", "import { alpha } from \"@repo/alpha\";");
            var program = Service(session, "file:///home/projects/pnpm/packages/beta/index.ts").GetProgram();
            Require(program.GetSemanticDiagnostics(context, program.GetSourceFile(File("/home/projects/pnpm/packages/beta/index.ts"))).Length == 1, "package must initially be unresolved");
            fixture.MutableFS.MkdirAll(RootedDirectoryPath.FromGoString("/home/projects/pnpm/packages/beta/node_modules/@repo"));
            fixture.MutableFS.AddSymlink(RootedPath.FromGoString("/home/projects/pnpm/packages/beta/node_modules/@repo/alpha"), RootedPath.FromGoString("/home/projects/pnpm/packages/alpha"));
            session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/pnpm/packages/beta/node_modules") }, new() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/pnpm/packages/beta/node_modules/%40repo") }, new() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/pnpm/packages/beta/node_modules/%40repo/alpha") }, new() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/pnpm/pnpm-lock.yaml") }, new() { Type = FileChangeType.Changed, Uri = Uri("file:///home/projects/pnpm/packages/beta/node_modules/.bin/tsc") }, new() { Type = FileChangeType.Changed, Uri = Uri("file:///home/projects/pnpm/packages/beta/node_modules/.bin/tsserver") }]));
            program = Service(session, "file:///home/projects/pnpm/packages/beta/index.ts").GetProgram();
            Require(program.GetSemanticDiagnostics(context, program.GetSourceFile(File("/home/projects/pnpm/packages/beta/index.ts"))).Length == 0, "pnpm symlink creation must resolve the package import");
        }
        finally
        {
            request.Cancel();
        }
    }

    private static void WatchedSymlinkPackageInvalidationOriginalAssertions()
    {
        using var fixture = new ProjectSessionFixture([new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/myproject/tsconfig.json", "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true,\n\t\t\t\t\t\t\"module\": \"nodenext\",\n\t\t\t\t\t\t\"moduleResolution\": \"nodenext\"\n\t\t\t\t\t},\n\t\t\t\t\t\"files\": [\"src/index.ts\"]\n\t\t\t\t}"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/myproject/src/index.ts", "import { foo } from \"mylib\";"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/mylib/package.json", "{\n\t\t\t\t\t\"name\": \"mylib\",\n\t\t\t\t\t\"main\": \"dist/index.js\"\n\t\t\t\t}"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/mylib/dist/index.js", "exports.foo = function() { return 1; };"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/mylib/dist/index.d.ts", "export declare function foo(): number;"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/myproject/node_modules/mylib", Symlink: "/home/projects/mylib")], new TypeScript.ProjectSystem.SessionOptions { CurrentDirectory = RootedDirectoryPath.FromGoString("/home/projects/myproject"), DefaultLibraryPath = TypeScript.Bundled.Bundled.LibPath(), TypingsLocation = RootedDirectoryPath.FromGoString("/home/src/Library/Caches/typescript"), PositionEncoding = PositionEncodingKind.UTF8, WatchEnabled = true, LoggingEnabled = true });
        using var request = new CancellationTokenSource();
        try
        {
            var context = ProtocolRequestContext.Default.WithCancellationToken(request.Token).WithRequestID(ByteStringValue.FromString("0"));
            var session = fixture.Session;
            Open(session, "file:///home/projects/myproject/src/index.ts", "import { foo } from \"mylib\";");
            var program = Service(session, "file:///home/projects/myproject/src/index.ts").GetProgram();
            session.WaitForBackgroundTasks();
            Require(program.GetSemanticDiagnostics(context, program.GetSourceFile(File("/home/projects/myproject/src/index.ts"))).Length == 0, "symlinked package must initially resolve");
            Require(fixture.WatchesFile(ByteStringValue.FromString("/home/projects/mylib/package.json")), "real package JSON must be watched");
            Require(fixture.WatchesFile(ByteStringValue.FromString("/home/projects/mylib/dist/index.d.ts")), "real declaration file must be watched");
            var error = fixture.MutableFS.WriteFile(File("/home/projects/mylib/package.json"), "{\n\t\t\t\t\"name\": \"mylib\"\n\t\t\t}");
            if (error is not null)
                throw error;
            WatchChange(session, "file:///home/projects/mylib/package.json");
            program = Service(session, "file:///home/projects/myproject/src/index.ts").GetProgram();
            Require(program.GetSemanticDiagnostics(context, program.GetSourceFile(File("/home/projects/myproject/src/index.ts"))).Length > 0, "removing package main must invalidate symlinked import resolution");
        }
        finally
        {
            request.Cancel();
        }
    }

    private static void WatchedSymlinkIncludeOriginalAssertions()
    {
        using var fixture = new ProjectSessionFixture([new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/tsconfig.json", "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"include\": [\"src\"]\n\t\t\t\t}"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/src/index.ts", "export const x = 1;"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/shared/utils.ts", "export const util = \"hello\";"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/shared/helpers.ts", "export const helper = 42;")]);
        var session = fixture.Session;
        Open(session, "file:///home/projects/TS/p1/src/index.ts", "export const x = 1;");
        var program = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
        Require(program.CommandLine().ParsedConfig.FileNames!.Contains(File("/home/projects/TS/p1/src/index.ts")), "initial active root must be included");
        Require(!program.CommandLine().ParsedConfig.FileNames!.Contains(File("/home/projects/TS/p1/src/linked/utils.ts")), "linked utility must initially be absent");
        Require(!program.CommandLine().ParsedConfig.FileNames!.Contains(File("/home/projects/TS/p1/src/linked/helpers.ts")), "linked helper must initially be absent");
        fixture.MutableFS.AddSymlink(RootedPath.FromGoString("/home/projects/TS/p1/src/linked"), RootedPath.FromGoString("/home/projects/TS/shared"));
        session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray([new() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/TS/p1/src/linked") }]));
        program = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
        Require(program.CommandLine().ParsedConfig.FileNames!.Contains(File("/home/projects/TS/p1/src/index.ts")), "active root must remain included");
        Require(program.CommandLine().ParsedConfig.FileNames!.Contains(File("/home/projects/TS/p1/src/linked/utils.ts")), "wildcard roots must discover linked utility");
        Require(program.CommandLine().ParsedConfig.FileNames!.Contains(File("/home/projects/TS/p1/src/linked/helpers.ts")), "wildcard roots must discover linked helper");
    }

    private static void WatchedRefreshFilteringOriginalAssertions()
    {
        using var fixture = new ProjectSessionFixture([new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/tsconfig.json", "{\n\t\t\t\t\t\"compilerOptions\": {},\n\t\t\t\t\t\"include\": [\"src\"]\n\t\t\t\t}"), new Program.Checks.CompilerBaselineFileSystem.Input("/home/projects/TS/p1/src/index.ts", "export const x = 1;")]);
        var session = fixture.Session;
        Open(session, "file:///home/projects/TS/p1/src/index.ts", "export const x = 1;");
        session.WaitForBackgroundTasks();
        int baseline = fixture.Client.RefreshDiagnosticsCallCount();
        void Events(bool refresh, params FileEvent[] values)
        {
            session.DidChangeWatchedFiles(ProtocolRequestContext.Default, Slice<FileEvent>.FromArray(values));
            session.WaitForBackgroundTasks();
            int count = fixture.Client.RefreshDiagnosticsCallCount();
            Require(refresh ? count > baseline : count == baseline, "original relevant/irrelevant event batch refresh assertion differs");
            baseline = count;
        }

        Events(false, new FileEvent() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/TS/p1/icon.svg") });
        Events(true, new FileEvent() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/TS/p1/src/new.ts") });
        Events(true, new FileEvent() { Type = FileChangeType.Changed, Uri = Uri("file:///home/projects/TS/p1/tsconfig.json") });
        fixture.MutableFS.MkdirAll(RootedDirectoryPath.FromGoString("/home/projects/TS/p1/node_modules/@types"));
        Events(true, new FileEvent() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/TS/p1/node_modules/@types") });
        Events(true, new FileEvent() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/TS/p1/icon.png") }, new FileEvent() { Type = FileChangeType.Changed, Uri = Uri("file:///home/projects/TS/p1/src/index.ts") });
        Events(false, new FileEvent() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/TS/p1/node_modules/pkg/LICENSE") }, new FileEvent() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/TS/p1/README.md") }, new FileEvent() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/TS/p1/LICENSE.txt") }, new FileEvent() { Type = FileChangeType.Created, Uri = Uri("file:///home/projects/TS/p1/style.css") });
    }
}

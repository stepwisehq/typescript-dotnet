// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete DidOpenFile subtest group of original session_test.go TestSession;
// this partial draft has no whole original-function credit until all groups land.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class ProjectSessionChecks
{
    private static readonly BaselineFS.Input[] DefaultFiles = [new("/home/projects/TS/p1/tsconfig.json", "{\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"noLib\": true,\n\t\t\t\t\"module\": \"nodenext\",\n\t\t\t\t\"strict\": true\n\t\t\t},\n\t\t\t\"include\": [\"src\"]\n\t\t}"), new("/home/projects/TS/p1/src/index.ts", "import { x } from \"./x\";"), new("/home/projects/TS/p1/src/x.ts", "export const x = 1;"), new("/home/projects/TS/p1/config.ts", "let x = 1, y = 2;"), ];
    private static DocumentUri Uri(string raw) => new(ByteStringValue.FromString(raw));
    private static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }

    private static LanguageService Service(Session session, string uri)
    {
        var(value, error) = session.GetLanguageService(ProtocolRequestContext.Default, Uri(uri));
        if (error is not null)
            throw error;
        return value ?? throw new InvalidOperationException("expected language service");
    }

    private static void Open(Session session, string uri, string content, LanguageKind? kind = null) => session.DidOpenFile(ProtocolRequestContext.Default, Uri(uri), 1, ByteStringValue.FromString(content), kind ?? LanguageKind.TypeScript);
    private static void DidOpenFileOriginalAssertions()
    {
        using (var fixture = new ProjectSessionFixture(DefaultFiles))
        {
            var session = fixture.Session;
            Require(session.Snapshot().ProjectCollection.Projects().Length == 0, "expected no initial projects");
            Open(session, "file:///home/projects/TS/p1/src/index.ts", "import { x } from \"./x\";");
            var snapshot = session.Snapshot();
            Require(snapshot.ProjectCollection.Projects().Length == 1, "expected one configured project");
            Require(snapshot.ProjectCollection.ConfiguredProject(CaseSensitivity.CaseInsensitive.PathKey(RootedPath.FromGoString("/home/projects/ts/p1/tsconfig.json")))is not null, "expected configured project");
            var file = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram().GetSourceFile(RootedFilePath.FromGoString("/home/projects/TS/p1/src/x.ts"));
            Require(file is not null && ByteStringValue.FromView(file.TextView()).EqualsBytes("export const x = 1;"u8), "expected imported file with original text");
        }

        using (var fixture = new ProjectSessionFixture(DefaultFiles))
        {
            var session = fixture.Session;
            Open(session, "file:///home/projects/TS/p1/config.ts", "let x = 1, y = 2;");
            var snapshot = session.Snapshot();
            Require(snapshot.ProjectCollection.Projects().Length == 2, "excluded disk file should create configured and inferred projects");
            Require(snapshot.ProjectCollection.ConfiguredProject(CaseSensitivity.CaseInsensitive.PathKey(RootedPath.FromGoString("/home/projects/ts/p1/tsconfig.json")))is not null, "expected configured project");
            Require(snapshot.ProjectCollection.InferredProject()is not null, "expected inferred project");
        }

        using (var fixture = new ProjectSessionFixture(DefaultFiles))
        {
            var session = fixture.Session;
            Open(session, "file:///home/projects/TS/p1/config.ts", "let x = 1, y = 2;");
            Open(session, "untitled:Untitled-1", "x");
            Open(session, "untitled:Untitled-2", "y");
            var snapshot = session.Snapshot();
            Require(snapshot.ProjectCollection.Projects().Length == 1 && snapshot.ProjectCollection.InferredProject()is not null, "in-memory files should retain one inferred project");
        }

        using (var fixture = new ProjectSessionFixture([new BaselineFS.Input("/home/projects/TS/p1/index.js", "import { x } from \"./x\";")]))
        {
            var session = fixture.Session;
            Open(session, "file:///home/projects/TS/p1/index.js", "import { x } from \"./x\";", LanguageKind.JavaScript);
            Require(session.Snapshot().ProjectCollection.Projects().Length == 1, "expected one JS project");
            Require(Service(session, "file:///home/projects/TS/p1/index.js").GetProgram().GetSourceFile(RootedFilePath.FromGoString("/home/projects/TS/p1/index.js"))is not null, "expected JS source file");
        }

        using (var fixture = new ProjectSessionFixture([new BaselineFS.Input("/home/projects/TS/p1/script", "const x = 1;")]))
        {
            var session = fixture.Session;
            Open(session, "file:///home/projects/TS/p1/script", "const x = 1;", new LanguageKind(ByteStringValue.FromString("plaintext")));
            var snapshot = session.Snapshot();
            Require(snapshot.ProjectCollection.Projects().Length == 1 && snapshot.ProjectCollection.InferredProject()is not null, "expected one extensionless inferred project");
            var file = Service(session, "file:///home/projects/TS/p1/script").GetProgram().GetSourceFile(RootedFilePath.FromGoString("/home/projects/TS/p1/script"));
            Require(file is not null && file.ScriptKind == ScriptKind.TS, "extensionless script should use TS kind");
        }
    }
}

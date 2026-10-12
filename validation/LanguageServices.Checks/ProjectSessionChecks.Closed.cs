// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestSession closed-file and jsconfig project-selection assertions.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class ProjectSessionChecks
{
    private static void ClosedFileOriginalAssertions()
    {
        using (var fixture = new ProjectSessionFixture([new BaselineFS.Input("/home/projects/TS/p1/tsconfig.json", "{\n\t\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\t\"noLib\": true,\n\t\t\t\t\t\t\"strict\": true\n\t\t\t\t\t},\n\t\t\t\t\t\"include\": [\"src\"]\n\t\t\t\t}"), new BaselineFS.Input("/home/projects/TS/p1/src/index.ts", "export const x: number = 1;")]))
        {
            var program = Service(fixture.Session, "file:///home/projects/TS/p1/src/index.ts").GetProgram();
            Require(program is not null, "closed configured file must have a program");
            var file = program!.GetSourceFile(File("/home/projects/TS/p1/src/index.ts"));
            Require(file is not null && ByteStringValue.FromView(file.TextView()).EqualsBytes("export const x: number = 1;"u8), "closed configured source must retain original text");
        }

        using (var fixture = new ProjectSessionFixture([new BaselineFS.Input("/home/projects/TS/loose/index.ts", "const greeting: string = \"hello\";")]))
        {
            var program = Service(fixture.Session, "file:///home/projects/TS/loose/index.ts").GetProgram();
            Require(program is not null, "closed loose file must have an inferred program");
            var file = program!.GetSourceFile(File("/home/projects/TS/loose/index.ts"));
            Require(file is not null && ByteStringValue.FromView(file.TextView()).EqualsBytes("const greeting: string = \"hello\";"u8), "closed loose source must retain original text");
        }
    }

    private static void JSConfigSelectionOriginalAssertions()
    {
        using var fixture = new ProjectSessionFixture([new BaselineFS.Input("/home/projects/TS/p1/tsconfig.json", "{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"noLib\": true,\n\t\t\t\t\t\"strict\": true\n\t\t\t\t}\n\t\t\t}"), new BaselineFS.Input("/home/projects/TS/p1/jsconfig.json", "{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"noLib\": true,\n\t\t\t\t\t\"checkJs\": true\n\t\t\t\t}\n\t\t\t}"), new BaselineFS.Input("/home/projects/TS/p1/index.ts", "export const x: number = 1;"), new BaselineFS.Input("/home/projects/TS/p1/app.js", "/** @type {number} */ var y = \"not a number\";")]);
        var session = fixture.Session;
        Open(session, "file:///home/projects/TS/p1/app.js", "/** @type {number} */ var y = \"not a number\";", LanguageKind.JavaScript);
        var project = session.Snapshot().GetDefaultProject(Uri("file:///home/projects/TS/p1/app.js"));
        Require(project is not null && project.ConfigFileName().AsValue().EqualsBytes("/home/projects/TS/p1/jsconfig.json"u8), "JS file must select jsconfig alongside tsconfig");
        Open(session, "file:///home/projects/TS/p1/index.ts", "export const x: number = 1;");
        project = session.Snapshot().GetDefaultProject(Uri("file:///home/projects/TS/p1/index.ts"));
        Require(project is not null && project.ConfigFileName().AsValue().EqualsBytes("/home/projects/TS/p1/tsconfig.json"u8), "TS file must select tsconfig");
    }
}

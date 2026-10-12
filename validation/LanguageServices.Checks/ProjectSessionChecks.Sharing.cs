// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestSession source-file sharing group; whole-function credit pending.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class ProjectSessionChecks
{
    private static void SourceFileSharingOriginalAssertions()
    {
        string[] configurations = ["{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"noLib\": true,\n\t\t\t\t\t\"module\": \"nodenext\",\n\t\t\t\t\t\"strict\": true,\n\t\t\t\t\t\"noCheck\": true\n\t\t\t\t}\n\t\t\t}", "{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"noLib\": true,\n\t\t\t\t\t\"module\": \"nodenext\",\n\t\t\t\t\t\"strict\": true,\n\t\t\t\t\t\"moduleDetection\": \"auto\"\n\t\t\t\t},\n\t\t\t\t\"include\": [\"src\"]\n\t\t\t}"];
        for (int i = 0; i < configurations.Length; i++)
        {
            using var fixture = new ProjectSessionFixture(DefaultFiles.Concat([new BaselineFS.Input("/home/projects/TS/p2/tsconfig.json", configurations[i]), new BaselineFS.Input("/home/projects/TS/p2/src/index.ts", "import { x } from \"../../p1/src/x\";")]));
            var session = fixture.Session;
            Open(session, "file:///home/projects/TS/p1/src/index.ts", "import { x } from \"./x\";");
            Open(session, "file:///home/projects/TS/p2/src/index.ts", "import { x } from \"../../p1/src/x\";");
            Require(session.Snapshot().ProjectCollection.Projects().Length == 2, "expected both configured projects");
            var first = Service(session, "file:///home/projects/TS/p1/src/index.ts").GetProgram().GetSourceFile(File("/home/projects/TS/p1/src/x.ts"));
            var second = Service(session, "file:///home/projects/TS/p2/src/index.ts").GetProgram().GetSourceFile(File("/home/projects/TS/p1/src/x.ts"));
            if (i == 0)
                Require(ReferenceEquals(first, second), "compatible options must share source owner");
            else
                Require(first is not null && second is not null && !ReferenceEquals(first, second), "different parse options must retain distinct source owners");
        }
    }
}

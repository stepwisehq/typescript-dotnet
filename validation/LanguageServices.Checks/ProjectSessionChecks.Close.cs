// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original session_test.go TestSessionCloseDoesNotBlockOnIdleCacheCleanTimer.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class ProjectSessionChecks
{
    public static void TestSessionCloseDoesNotBlockOnIdleCacheCleanTimer()
    {
        using var fixture = new ProjectSessionFixture([new BaselineFS.Input("/home/projects/TS/p1/tsconfig.json", "{\n\t\t\t\"compilerOptions\": { \"noLib\": true }\n\t\t}"), new BaselineFS.Input("/home/projects/TS/p1/src/index.ts", "export const x = 1;")]);
        var session = fixture.Session;
        session.DidOpenFile(ProtocolRequestContext.Default, new DocumentUri(ByteStringValue.FromString("file:///home/projects/TS/p1/src/index.ts")), 1, ByteStringValue.FromString("export const x = 1;"), LanguageKind.TypeScript);
        // The timer is installed synchronously by DidOpenFile; do not cancel it
        // with WaitForBackgroundTasks before observing the genuine Close path.
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        fixture.Dispose();
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
        if (elapsed >= TimeSpan.FromSeconds(5))
            throw new InvalidOperationException("Close must complete well under the idle cache clean delay");
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// snapshot_test.go program update and auto-import registry original subtest.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotLifetimeChecks
{
    private static void ProgramChangeLoadsNodeModules()
    {
        using var fixture = new Fixture(new Input("/home/projects/otherproject/tsconfig.json", "{\n\t\t\t\t\"compilerOptions\": {\n\t\t\t\t\t\"module\": \"commonjs\"\n\t\t\t\t}\n\t\t\t}"), new Input("/home/projects/otherproject/index.ts", ""), new Input("/home/projects/node_modules/foo/package.json", "{\n\t\t\t\t\"types\": \"index.d.ts\",\n\t\t\t\t\"typesVersions\": {\n\t\t\t\t\t\"*\": {\n\t\t\t\t\t\t\"bar/*\": [\"dist/*\"],\n\t\t\t\t\t\t\"exact-match\": [\"dist/index.d.ts\"],\n\t\t\t\t\t\t\"foo/*\": [\"dist/*\"],\n\t\t\t\t\t\t\"*\": [\"dist/*\"]\n\t\t\t\t\t}\n\t\t\t\t}\n\t\t\t}"), new Input("/home/projects/node_modules/foo/nope.d.ts", "export const nope = 0;"), new Input("/home/projects/node_modules/foo/dist/index.d.ts", "export const index = 0;"), new Input("/home/projects/node_modules/foo/dist/blah.d.ts", "export const blah = 0;"), new Input("/home/projects/node_modules/foo/dist/foo/onlyInFooFolder.d.ts", "export const foo = 0;"), new Input("/home/projects/node_modules/foo/dist/subfolder/one.d.ts", "export const one = 0;"));
        var session = fixture.Session;
        var uri = URI("file:///home/projects/otherproject/index.ts");
        OpenFile(session, "file:///home/projects/otherproject/index.ts", "");
        session.DidChangeFile(ProtocolRequestContext.Default, uri, 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { Partial = new() { Text = ByteStringValue.FromString("import {} from \"foo/foo/subfolder/one\";"), Range = new() { Start = new() { Line = 0, Character = 0 }, End = new() { Line = 0, Character = 0 } } } }]));
        var(_, error) = session.GetCurrentLanguageServiceWithAutoImports(ProtocolRequestContext.Default, uri);
        if (error is not null)
            throw error;
    }
}

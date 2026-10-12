// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/autoimport/registry_test.go:1078. All four original subtest assertions.
// Go T.Parallel hints are sequential; registry work retains actual concurrency.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.AutoImport;
using TypeScript.LanguageServices.Utilities;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class AutoImportRegistryChecks
{
    private static void TestAutoImportEntrypointDirectorySearch()
    {
        const string root = "/home/src/entrypoint-search";
        const string package = root + "/node_modules/my-pkg";
        const string initial = "import { main } from \"my-pkg\";";
        BaselineFS.Input[] files = [new(root + "/tsconfig.json", "{\"compilerOptions\":{\"module\":\"commonjs\",\"target\":\"es2020\"}}"), new(root + "/package.json", "{\"name\":\"test-project\",\"dependencies\":{\"my-pkg\":\"*\"}}"), new(root + "/index.ts", initial), new(package + "/package.json", "{\"name\":\"my-pkg\",\"version\":\"1.0.0\",\"types\":\"index.d.ts\"}"), new(package + "/index.d.ts", "export declare const main: number;\n"), new(package + "/extra.d.ts", "export declare const extra: string;\n"), new(package + "/nested/deep.d.ts", "export declare const deep: boolean;\n"), new(package + "/nested/deeper.d.ts", "export declare const deeper: boolean;\n")];
        var uri = new DocumentUri(ByteStringValue.FromString("file://" + root + "/index.ts"));
        List<Exception>? failures = null;
        // default limits to main entrypoint
        RunRegistryGroup("entrypoint group 1", () =>
        {
            using (var fixture = new ProjectSessionFixture(files))
            {
                Open(fixture, uri, initial);
                AutoImports(fixture, uri);
                Require(SingleBucket(Stats(fixture).NodeModulesBuckets).FileCount == 1, "expected only 1 file (main entrypoint) by default");
            }
        }, ref failures);
        // preference enables all files
        RunRegistryGroup("entrypoint group 2", () =>
        {
            using (var fixture = new ProjectSessionFixture(files))
            {
                var preferences = LsUtil.NewDefaultUserPreferences();
                preferences.AutoImportEntrypointDirectorySearch = Tristate.True;
                fixture.Session.Configure(preferences);
                Open(fixture, uri, initial);
                AutoImports(fixture, uri);
                Require(SingleBucket(Stats(fixture).NodeModulesBuckets).FileCount >= 4, "expected at least 4 files from directory search");
            }
        }, ref failures);
        // changing preference triggers rebuild
        RunRegistryGroup("entrypoint group 3", () =>
        {
            using (var fixture = new ProjectSessionFixture(files))
            {
                Open(fixture, uri, initial);
                AutoImports(fixture, uri);
                Require(SingleBucket(Stats(fixture).NodeModulesBuckets).FileCount == 1, "expected only 1 file initially");
                var preferences = LsUtil.NewDefaultUserPreferences();
                preferences.AutoImportEntrypointDirectorySearch = Tristate.True;
                fixture.Session.Configure(preferences);
                var snapshot = fixture.Session.Snapshot();
                var project = snapshot.GetDefaultProject(uri);
                Require(project is not null, "expected default project");
                Require(!Registry.IsPreparedForImportingFile(snapshot.AutoImportRegistry(), RootedFilePath.FromGoString(root + "/index.ts"), project.ID(), preferences), "registry should not be prepared after preference change");
                AutoImports(fixture, uri);
                Require(SingleBucket(Stats(fixture).NodeModulesBuckets).FileCount >= 4, "expected at least 4 files after rebuild with directory search enabled");
            }
        }, ref failures);
        // deep import from program update enables recursive search
        RunRegistryGroup("entrypoint group 4", () =>
        {
            using (var fixture = new ProjectSessionFixture(files))
            {
                Open(fixture, uri, initial);
                AutoImports(fixture, uri);
                Require(SingleBucket(Stats(fixture).NodeModulesBuckets).FileCount == 1, "expected only 1 file before deep import");
                fixture.Session.DidChangeFile(ProtocolRequestContext.Default, uri, 2, Slice<TextDocumentContentChangePartialOrWholeDocument>.FromArray([new() { WholeDocument = new TextDocumentContentChangeWholeDocument { Text = ByteStringValue.FromString("import { main } from \"my-pkg\";\nimport { deep } from \"my-pkg/nested/deep\";\n") } }]));
                AutoImports(fixture, uri);
                Require(SingleBucket(Stats(fixture).NodeModulesBuckets).FileCount >= 4, "expected at least 4 files after deep import triggers recursive search");
            }
        }, ref failures);
        if (failures != null)
            throw new AggregateException("original TestAutoImportEntrypointDirectorySearch groups failed", failures);
    }
}

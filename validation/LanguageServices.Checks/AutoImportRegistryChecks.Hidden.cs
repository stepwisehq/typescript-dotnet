// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete original registry_test.go:993 TestHiddenDirectoriesInNodeModules.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class AutoImportRegistryChecks
{
    private static void TestHiddenDirectoriesInNodeModules()
    {
        const string root = "/home/src/fuse-project";
        const string store = root + "/node_modules/.yarn-store";
        const string package = store + "/some-pkg-npm-1.0.0-abc123/package";
        const string content = "import { debug } from \"some-pkg/debug\";";
        BaselineFS.Input[] files = [new(root + "/tsconfig.json", "{\"compilerOptions\":{\"module\":\"commonjs\",\"target\":\"es2020\",\"strict\":true}}"), new(root + "/package.json", "{\"name\":\"test-project\",\"dependencies\":{\"some-pkg\":\"*\",\"real-package\":\"*\"}}"), new(root + "/index.ts", content), new(root + "/node_modules/real-package/package.json", "{\"name\":\"real-package\",\"version\":\"1.0.0\",\"types\":\"index.d.ts\"}"), new(root + "/node_modules/real-package/index.d.ts", "export declare const realExport: number;\n"), new(root + "/node_modules/some-pkg", Symlink: package), new(package + "/package.json", "{\"name\":\"some-pkg\",\"version\":\"1.0.0\",\"types\":\"index.d.ts\"}"), new(package + "/index.d.ts", "export declare const something: number;\n"), new(package + "/debug/package.json", "{\"main\":\"./debug.js\",\"types\":\"./debug.d.ts\"}"), new(package + "/debug/debug.d.ts", "export declare function debug(msg: string): void;\n"), new(package + "/debug/debug.js", "exports.debug = function(msg) { console.log(msg); };\n"), new(store + "/other-pkg-npm-2.0.0-def456/package/package.json", "{\"name\":\"other-pkg\",\"version\":\"1.0.0\",\"types\":\"index.d.ts\"}"), new(store + "/other-pkg-npm-2.0.0-def456/package/index.d.ts", "export declare const other: string;\n")];
        using var fixture = new ProjectSessionFixture(files);
        var uri = new DocumentUri(ByteStringValue.FromString("file://" + root + "/index.ts"));
        Open(fixture, uri, content);
        AutoImports(fixture, uri);
        var bucket = SingleBucket(Stats(fixture).NodeModulesBuckets);
        Require(bucket.DependencyNames is not null, "DependencyNames should not be nil");
        foreach (var name in bucket.DependencyNames)
            Require(name.Value[0] != '.', "hidden directory should not appear as a dependency name");
    }
}

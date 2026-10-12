// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original TestRegistryLifecycle circular workspace and realpath dedup groups.
using TypeScript.Scanner;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class AutoImportRegistryChecks
{
    private static void LifecycleCircularWorkspaceSymlinks()
    {
        const string a = "/home/src/circular-workspaces/packages/pkg-a";
        const string b = "/home/src/circular-workspaces/packages/pkg-b";
        const string consumer = "export const usesHelper = uniqueHelperValueFromHelperA;\n";
        using var fixture = new ProjectSessionFixture(new BaselineFS.Input[] { new(a + "/tsconfig.json", "{\"compilerOptions\":{\"module\":\"esnext\",\"strict\":true}}"), new(a + "/package.json", "{\"name\":\"pkg-a\",\"dependencies\":{\"pkg-b\":\"*\"}}"), new(a + "/index.ts", "import { b } from \"pkg-b\";\nexport const a = b;\n"), new(a + "/consumer.ts", consumer), new(a + "/helper.ts", "export const uniqueHelperValueFromHelperA = 1;\n"), new(b + "/tsconfig.json", "{\"compilerOptions\":{\"module\":\"esnext\",\"strict\":true}}"), new(b + "/package.json", "{\"name\":\"pkg-b\",\"dependencies\":{\"pkg-a\":\"*\"}}"), new(b + "/index.ts", "import { a } from \"pkg-a\";\nexport const b = a;\n"), new(a + "/node_modules/pkg-b", Symlink: b), new(b + "/node_modules/pkg-a", Symlink: a) });
        var uri = AutoImportMonorepoFixture.URIFor(a + "/consumer.ts");
        Open(fixture, uri, consumer);
        AutoImports(fixture, uri);
        Require(SingleBucket(Stats(fixture).ProjectBuckets).FileCount == 3, "expected all pkg-a project files despite circular workspace symlinks");
    }

    private static void LifecycleRealpathDedupAcrossBuckets()
    {
        const string root = "/home/src/autoimport-realpath-dedupe";
        const string app = root + "/apps/web";
        const string shared = root + "/node_modules/shared";
        const string content = "export const app = 1;\n";
        using var fixture = new ProjectSessionFixture(new BaselineFS.Input[] { new(root + "/package.json", "{\"name\":\"repo-root\",\"private\":true,\"dependencies\":{\"shared\":\"*\"}}"), new(root + "/tsconfig.json", "{\"compilerOptions\":{\"module\":\"esnext\",\"target\":\"esnext\",\"strict\":true},\"include\":[\"apps/**/*\"]}"), new(app + "/package.json", "{\"name\":\"web\",\"private\":true,\"dependencies\":{\"shared\":\"*\"}}"), new(app + "/tsconfig.json", "{\"compilerOptions\":{\"module\":\"esnext\",\"target\":\"esnext\",\"strict\":true},\"include\":[\"src\"]}"), new(app + "/src/index.ts", content), new(shared + "/package.json", "{\"name\":\"shared\",\"version\":\"1.0.0\",\"types\":\"index.d.ts\"}"), new(shared + "/index.d.ts", "export declare const sharedValue: 1;\n"), new(app + "/node_modules/shared", Symlink: shared) });
        var uri = AutoImportMonorepoFixture.URIFor(app + "/src/index.ts");
        Open(fixture, uri, content);
        AutoImports(fixture, uri);
        var stats = Stats(fixture);
        Require(stats.NodeModulesBuckets.Length == 2, "expected both app and repo node_modules buckets");
        Require(stats.UniquePackageCount == 1, "expected one unique package after realpath dedup");
    }
}

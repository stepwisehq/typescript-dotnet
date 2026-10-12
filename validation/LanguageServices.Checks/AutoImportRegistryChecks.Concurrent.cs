// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/ls/autoimport/registry_test.go:1227. Original concurrency is performed
// by the real registry owner; T.Parallel scheduling hints remain sequential here.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using TypeScript.LanguageServices.AutoImport;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
public static partial class AutoImportRegistryChecks
{
    private static void TestUpdateIndexesConcurrentMapSafety()
    {
        const string root = "/home/src/autoimport-fallback-race";
        const int packageCount = 40;
        var files = new List<BaselineFS.Input>
        {
            new(root + "/tsconfig.json", "{\"compilerOptions\":{\"module\":\"esnext\",\"target\":\"esnext\",\"strict\":true}}"),
            new(root + "/index.ts", "export {};\n")
        };
        for (int i = 0; i < packageCount; i++)
        {
            string package = root + "/node_modules/pkg" + i;
            files.Add(new(package + "/package.json", $"{{\"name\":\"pkg{i}\",\"version\":\"1.0.0\",\"main\":\"index.js\"}}"));
            files.Add(new(package + "/index.js", "module.exports = {};\n"));
            string types = root + "/node_modules/@types/pkg" + i;
            files.Add(new(types + "/package.json", $"{{\"name\":\"@types/pkg{i}\",\"version\":\"1.0.0\",\"types\":\"index.d.ts\"}}"));
            files.Add(new(types + "/index.d.ts", $"export declare const foo{i}: number;\n"));
        }

        using var fixture = new ProjectSessionFixture(files);
        var session = fixture.Session;
        var uri = new DocumentUri(ByteStringValue.FromString("file://" + root + "/index.ts"));
        session.DidOpenFile(ProtocolRequestContext.Default, uri, 1, ByteStringValue.FromString("export {};\n"), LanguageKind.TypeScript);
        var(_, error) = session.GetCurrentLanguageServiceWithAutoImports(ProtocolRequestContext.Default, uri);
        if (error is not null)
            throw error;
        var registry = session.Snapshot().AutoImportRegistry() ?? throw new InvalidOperationException("auto import registry not initialized");
        var stats = registry.GetCacheStats();
        if (stats.NodeModulesBuckets.Length != 1)
            throw new InvalidOperationException($"expected 1 bucket, got {stats.NodeModulesBuckets.Length}");
        if (stats.NodeModulesBuckets[0].ExportCount != packageCount)
            throw new InvalidOperationException($"expected {packageCount} exports, got {stats.NodeModulesBuckets[0].ExportCount}");
    }
}

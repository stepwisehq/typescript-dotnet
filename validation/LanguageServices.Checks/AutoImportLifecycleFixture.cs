// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Selected actual autoimporttestutil/fixtures.go SetupLifecycleSession, AddLocalProject,
// AddNamedNodeModulesPackage and AddPackageJSONWithDependencies closure.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;
using BaselineFS = Program.Checks.CompilerBaselineFileSystem;

namespace LanguageServices.Checks;
internal sealed class AutoImportLifecycleFixture : IDisposable
{
    internal const string Root = "/home/src/autoimport-lifecycle";
    internal readonly ProjectSessionFixture Fixture;
    internal readonly BaselineFS.Input[] SourceFiles;
    internal AutoImportLifecycleFixture(int fileCount)
    {
        if (fileCount < 0)
            throw new InvalidOperationException("fileCount must be non-negative");
        var files = new List<BaselineFS.Input>();
        files.Add(new(Root + "/tsconfig.json", "{\n  \"compilerOptions\": {\n    \"module\": \"esnext\",\n    \"target\": \"esnext\",\n    \"strict\": true,\n    \"allowJs\": true,\n    \"checkJs\": true\n  }\n}\n"));
        SourceFiles = new BaselineFS.Input[fileCount];
        for (int i = 1; i <= fileCount; i++)
        {
            var file = new BaselineFS.Input(Root + $"/file{i}.ts", $"export const localExport1_{i} = {i};\n");
            SourceFiles[i - 1] = file;
            files.Add(file);
        }

        files.Add(new(Root + "/node_modules/pkg1/package.json", "{\"name\":\"pkg1\",\"types\":\"index.d.ts\"}"));
        files.Add(new(Root + "/node_modules/pkg1/index.d.ts", "export declare const pkg1_value: number;\n"));
        files.Add(new(Root + "/package.json", "{\n  \"name\": \"local-project-2\",\n  \"dependencies\": {\n    \"pkg1\": \"*\"\n  }\n}\n"));
        Fixture = new ProjectSessionFixture(files);
    }

    internal DocumentUri URI(int index) => new(ByteStringValue.FromString("file://" + SourceFiles[index].Path));
    public void Dispose() => Fixture.Dispose();
}

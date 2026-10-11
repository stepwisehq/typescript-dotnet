// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Pinned internal/checker/checker_test.go: all original behavioral assertions.
// Shared execution adapts t.Parallel only; fixture/config/library bytes are retained.
using Configuration.Checks;
using TypeScript.Scanner;
using TypeScript.Vfs;
using CompilerProgram = TypeScript.Compiler.Program;
using ConfigParser = TypeScript.TsOptions.TsOptions;
using Bundled = TypeScript.Bundled.Bundled;

namespace Declarations.Checks;

public static class CheckerOriginalChecks
{
    public static IReadOnlyList<string> Cases { get; } =
        ["TestGetSymbolAtLocation", "TestGetTypeAtLocationOfTypeOnlyImportClause"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestGetSymbolAtLocation": Symbols(); break;
            case "TestGetTypeAtLocationOfTypeOnlyImportClause": ImportClauseTypes(); break;
            default: throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    private static CompilerProgram Create(Dictionary<string, string> files)
    {
        IFS fs = new ConfigurationFileSystem(
            files.Select(pair => new ConfigurationFileSystem.Input(pair.Key, pair.Value)),
            CaseSensitivity.CaseInsensitive);
        fs = Bundled.WrapFS(fs);
        var host = TypeScript.Compiler.CompilerHosts.NewCompilerHost(fs, Bundled.LibPath());
        var (parsed, errors) = ConfigParser.GetParsedCommandLineOfConfigFile(
            RootedFilePath.FromGoString("/tsconfig.json"), new CompilerOptions(), null, fs, null);
        Check.Equal(errors.Length, 0, "Expected no errors in parsed command line");
        var program = CompilerProgram.NewProgram(new() { Config = parsed!, Host = host });
        program.BindSourceFiles();
        return program;
    }

    private static void Symbols()
    {
        const string content = "interface Foo {\n  bar: string;\n}\ndeclare const foo: Foo;\nfoo.bar;";
        var program = Create(new()
        {
            ["/foo.ts"] = content,
            ["/tsconfig.json"] = "\n\t\t\t\t{\n\t\t\t\t\t\"compilerOptions\": {},\n\t\t\t\t\t\"files\": [\"foo.ts\"]\n\t\t\t\t}\n\t\t\t"
        });
        var (checker, done) = program.GetTypeChecker();
        try
        {
            var file = program.GetSourceFile(RootedFilePath.FromGoString("/foo.ts"))!;
            var interfaceId = file.Statements.Nodes[0].Name();
            var varId = file.Statements.Nodes[1].AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes[0].Name();
            var propAccess = file.Statements.Nodes[2].Expression();
            foreach (var node in new[] { interfaceId, varId, propAccess })
                Check.True(checker.GetSymbolAtLocation(node) is not null, "Expected symbol to be non-nil");
        }
        finally { done(); }
    }

    private static void ImportClauseTypes()
    {
        var program = Create(new()
        {
            ["/types.ts"] = "export type U = number;\nexport default interface D { x: number }",
            ["/main.ts"] = "import type { U } from \"./types\";\nimport type * as types from \"./types\";\nimport { U as V } from \"./types\";\nimport type D from \"./types\";\nexport const u: U = 1;\nexport const v: V = 1;\nexport type W = types.U;\nexport type E = D;",
            ["/tsconfig.json"] = "\n\t\t\t\t{\n\t\t\t\t\t\"compilerOptions\": {},\n\t\t\t\t\t\"files\": [\"types.ts\", \"main.ts\"]\n\t\t\t\t}\n\t\t\t"
        });
        var (checker, done) = program.GetTypeChecker();
        try
        {
            var file = program.GetSourceFile(RootedFilePath.FromGoString("/main.ts"))!;
            Node ImportClauseAt(int index) => file.Statements.Nodes[index].AsImportDeclaration().ImportClause!;
            var regular = checker.GetTypeAtLocation(ImportClauseAt(2));
            foreach (int index in new[] { 0, 1 })
            {
                var type = checker.GetTypeAtLocation(ImportClauseAt(index));
                Check.True(type is not null, $"Expected type of import clause {index} to be non-nil");
                Check.True(ReferenceEquals(type, regular), "type-only import clause uses regular import type");
            }
            var defaultClause = checker.GetTypeAtLocation(ImportClauseAt(3));
            var defaultReference = checker.GetTypeAtLocation(file.Statements.Nodes[7].AsTypeAliasDeclaration().Type);
            Check.True(ReferenceEquals(defaultClause, defaultReference), "default import clause/reference type identity");
        }
        finally { done(); }
    }
}

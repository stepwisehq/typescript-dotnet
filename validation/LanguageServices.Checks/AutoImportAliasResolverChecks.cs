// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/ls/autoimport/aliasresolver_crash_test.go assertion and original fake host.
// Original T.Parallel scheduling hint is sequential; the diagnostic assertion is unchanged.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Vfs;
using TypeScript.LanguageServices.AutoImport;
using Program.Checks;
using CompilerProgram = TypeScript.Compiler.Program;

namespace LanguageServices.Checks;
public static class AutoImportAliasResolverChecks
{
    public static readonly string[] Names = ["TestAliasResolverGetDiagnosticsDoesNotPanic"];
    public static int RunAll()
    {
        Run(Names[0]);
        return 1;
    }

    public static void Run(string name)
    {
        if (name != Names[0])
            throw new ArgumentOutOfRangeException(nameof(name));
        const string fileName = "/pkg/index.ts";
        const string text = "declare function f(arg: { a: string }): () => void;\nexport const x = f({ a: 1 });\n";
        var fs = new CompilerBaselineFileSystem([new(fileName, text)]);
        var host = new FakeCloneHost(fs);
        var file = Parser.ParseSourceFile(new SourceFileParseOptions(FileName: fileName, PathKey: fileName), text, ScriptKind.TS);
        Binder.BindSourceFile(file);
        var resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = new AutoImportResolutionHost(fs, RootedDirectoryPath.FromValue(ByteStringValue.FromString("/"))), CompilerOptions = CompilerOptionHelpers.EmptyCompilerOptions });
        var aliases = new AliasResolver(Slice<SourceFile>.FromArray([file]), null, host, resolver, static (_, _) =>
        {
        });
        var(checker, _) = Checker.NewChecker(aliases);
        checker.GetDiagnostics(default, file); // The original assertion is that checking does not panic.
    }

    private sealed class FakeCloneHost(IFS fs) : IRegistryCloneHost
    {
        public IFS FS() => fs;
        public (IProjectID? ProjectID, CompilerProgram? Program) GetDefaultProject(PathKey path) => (null, null);
        public CompilerProgram? GetProgramForProject(IProjectID projectID) => null;
        public InfoCacheEntry? GetPackageJson(RootedFilePath fileName) => null;
        public SourceFile? GetSourceFile(RootedFilePath fileName, PathKey path) => null;
        public void Dispose()
        {
        }
    }
}

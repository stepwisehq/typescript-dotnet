// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original compiler/program_test.go assertion bodies; read-only fixtures replace
// test seeding writes and subtests execute sequentially in this shared dispatcher.
using Configuration.Checks;
using TypeScript.Compiler;
using TypeScript.Modules;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Vfs;
using CompilerProgram = TypeScript.Compiler.Program;
using Bundled = TypeScript.Bundled.Bundled;
using ConfigParser = TypeScript.TsOptions.TsOptions;

namespace Program.Checks;
public static class ProgramChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestProgramSharedData", "TestIncludeReasonDiagnosticsAreProgramLocal", "TestProgramHostsAndFactories", "TestClonedProgramProjectReferenceResolution", "TestProgram", "TestImportSourceProgram", "TestIncludeProcessorDiagnosticsWithMissingFileCasing"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestProgramSharedData":
                ProgramDataChecks.Run();
                break;
            case "TestIncludeReasonDiagnosticsAreProgramLocal":
                IncludeReason();
                break;
            case "TestProgramHostsAndFactories":
                HostsAndFactories();
                break;
            case "TestClonedProgramProjectReferenceResolution":
                ClonedReferences();
                break;
            case "TestProgram":
                ProgramOrderChecks.Run();
                break;
            case "TestImportSourceProgram":
                ImportSource();
                break;
            case "TestIncludeProcessorDiagnosticsWithMissingFileCasing":
                MissingCasing();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    internal static RootedFilePath File(string text) => RootedFilePath.FromGoString(text);
    internal static PathKey Key(string text) => PathKey.FromGoString(text);
    internal static IFS FS(Dictionary<string, string> files, string? link = null, string? target = null)
    {
        var inputs = files.Select(pair => new ConfigurationFileSystem.Input(pair.Key, pair.Value)).ToList();
        if (link is not null)
            inputs.Add(new(link, Symlink: target));
        return new ConfigurationFileSystem(inputs);
    }

    internal static ICompilerHost Host(IFS fs) => CompilerHosts.NewCompilerHost(fs, RootedDirectoryPath.FromGoString("/"));
    internal static ParsedCommandLine Parse(IFS fs, CompilerOptions? existing = null)
    {
        var(config, diagnostics) = ConfigParser.GetParsedCommandLineOfConfigFile(File("/src/tsconfig.json"), existing, null, fs, null);
        Check.Equal(diagnostics?.Count ?? 0, 0, "original config diagnostics");
        return config!;
    }

    private static void IncludeReason()
    {
        var options = new ProgramConfig
        {
            Config = new ParsedCommandLine()
        };
        var oldProgram = new CompilerProgram
        {
            opts = options
        };
        var newProgram = new CompilerProgram
        {
            opts = options
        };
        var reason = new FileIncludeReason
        {
            rootIndex = 0
        };
        foreach (bool relative in new[]
        {
            false,
            true
        }

        )
        {
            var oldDiagnostic = reason.ToDiagnostic(oldProgram, relative, "");
            var newDiagnostic = reason.ToDiagnostic(newProgram, relative, "");
            Check.True(ReferenceEquals(reason.ToDiagnostic(oldProgram, relative, ""), oldDiagnostic), "old generation reason pointer");
            Check.True(ReferenceEquals(reason.ToDiagnostic(newProgram, relative, ""), newDiagnostic), "new generation reason pointer");
            Check.True(!ReferenceEquals(oldDiagnostic, newDiagnostic), "reason cache is Program-local");
        }
    }

    private static void HostsAndFactories()
    {
        Dictionary<string, string> files = new()
        {
            ["/src/tsconfig.json"] = "{\"compilerOptions\":{\"noLib\":true,\"module\":\"nodenext\"},\"files\":[\"index.ts\"]}",
            ["/src/index.ts"] = "/// <reference types=\"dep\" />\nimport { value } from \"./dep.js\"; export const result = value;",
            ["/src/dep.ts"] = "export const value = 1;",
            ["/src/node_modules/@types/dep/index.d.ts"] = "export {};"
        };
        var fs = FS(files);
        var host = Host(fs);
        var config = Parse(fs);
        int pools = 0, resolvers = 0;
        var tracing = new TypeScript.Tracing.Tracing();
        var p = CompilerProgram.NewProgram(new() { Config = config, Host = host, Tracing = tracing, CreateCheckerPool = program =>
        {
            pools++;
            Check.True(ReferenceEquals(program.Tracing(), tracing), "factory receives tracing");
            return CheckerPool.newCheckerPoolWithTracing(program, program.Tracing());
        }, CreateModuleResolver = options =>
        {
            resolvers++;
            var resolverFiles = new Dictionary<string, string>(files)
            {
                ["/factory-only/package.json"] = "{\"name\":\"factory-host\"}"
            };
            options.Host = new CompilerResolutionHost(Host(FS(resolverFiles)), RootedDirectoryPath.FromGoString("/"));
            return DefaultResolver.NewResolver(options);
        } });
        Check.Equal(pools, 1, "initial pool calls");
        Check.Equal(resolvers, 1, "initial resolver calls");
        Check.True(ReferenceEquals(p.Tracing(), tracing), "retained tracing");
        Check.True(p.GetPackageJsonInfo(File("/factory-only/package.json"))is null, "lazy query does not retain factory host");
        var oldFile = p.GetSourceFile(File("/src/index.ts"))!;
        var resolved = p.GetResolvedModuleFromModuleSpecifier(oldFile, oldFile.Imports()![0]);
        Check.True(resolved.IsResolved(), "initial module resolution");
        var resolvedType = p.GetResolvedTypeReferenceDirectiveFromTypeReferenceDirective(oldFile.TypeReferenceDirectives![0], oldFile);
        Check.True(resolvedType?.IsResolved() == true, "initial type resolution");
        var newFiles = new Dictionary<string, string>(files)
        {
            ["/src/index.ts"] = "\n" + files["/src/index.ts"],
            ["/probe/package.json"] = "{\"name\":\"new-host\"}"
        };
        var newHost = Host(FS(newFiles));
        var(cloned, changed, reused) = p.ReuseProgram(Key("/src/index.ts"), newHost, program =>
        {
            pools++;
            Check.True(ReferenceEquals(program.Host(), newHost), "clone host");
            Check.True(program.Tracing()is null, "clone trace disabled");
            Check.Equal(program.GetSourceFile(File("/src/index.ts"))!.Text(), newFiles["/src/index.ts"], "clone text");
            return CheckerPool.newCheckerPoolWithTracing(program, program.Tracing());
        }, _ => throw new InvalidOperationException("cloning must not invoke resolver construction callbacks"));
        Check.True(reused, "newline reuses Program");
        Check.True(cloned!.Tracing()is null, "clone trace null");
        Check.True(ReferenceEquals(p.Tracing(), tracing), "old tracing unchanged");
        Check.True(ReferenceEquals(changed, cloned.GetSourceFile(File("/src/index.ts"))), "returned file identity");
        Check.True(ReferenceEquals(cloned.GetResolvedModuleFromModuleSpecifier(changed!, changed!.Imports()![0]), resolved), "module result shared");
        Check.True(ReferenceEquals(cloned.GetResolvedTypeReferenceDirectiveFromTypeReferenceDirective(changed.TypeReferenceDirectives![0], changed), resolvedType), "type result shared");
        Check.True(!ReferenceEquals(cloned.resolutionData, p.resolutionData), "distinct generation resolution data");
        Check.True(!ReferenceEquals(cloned.GetCheckerPool(), p.GetCheckerPool()), "distinct checker pools");
        Check.True(cloned.GetPackageJsonInfo(File("/probe/package.json"))is not null, "new host lookup");
        Check.True(p.GetPackageJsonInfo(File("/probe/package.json"))is null, "new lookup does not populate old cache");
        Check.Equal(pools, 2, "clone pool calls");
        Check.Equal(resolvers, 1, "clone resolver calls");
        var(defaults, _, defaultReused) = cloned.ReuseProgram(Key("/src/index.ts"), newHost);
        Check.True(defaultReused, "default pool clone reused");
        Check.True(defaults!.compilerCheckerPool is not null, "default checker pool");
        Check.True(defaults.Tracing()is null && defaults.compilerCheckerPool!.tracing is null, "default trace null");
        Check.Equal(pools, 2, "default clone omits old factory");
        var traced = CompilerProgram.NewProgram(new() { Config = config, Host = newHost, Tracing = tracing });
        Check.True(ReferenceEquals(traced.Tracing(), tracing), "default Program trace");
        Check.True(ReferenceEquals(traced.compilerCheckerPool!.tracing, tracing), "default pool trace");
        newFiles["/src/index.ts"] = "import \"./other.js\";";
        newFiles["/src/other.ts"] = "export {};";
        var rebuildHost = Host(FS(newFiles));
        var(rebuilt, _, rebuildReused) = p.UpdateProgram(Key("/src/index.ts"), rebuildHost);
        Check.True(!rebuildReused, "import change rebuilds");
        Check.True(rebuilt.compilerCheckerPool is not null, "rebuild default pool");
        Check.True(ReferenceEquals(rebuilt.Host(), rebuildHost), "rebuild host");
        Check.True(rebuilt.Tracing()is null && rebuilt.compilerCheckerPool!.tracing is null, "rebuild trace null");
        Check.True(rebuilt.GetSourceFile(File("/src/other.ts"))is not null, "rebuild newly imported file");
        Check.Equal(pools, 2, "rebuild pool factory not retained");
        Check.Equal(resolvers, 1, "rebuild resolver factory not retained");
    }

    private static void ClonedReferences()
    {
        foreach (bool preserve in new[]
        {
            false,
            true
        }

        )
        {
            Dictionary<string, string> files = new()
            {
                ["/src/tsconfig.json"] = "{\"compilerOptions\":{\"noLib\":true,\"module\":\"nodenext\"},\"files\":[\"index.ts\"],\"references\":[{\"path\":\"../reference\"}]}",
                ["/src/index.ts"] = "import { value } from \"reference\"; export const result = value;",
                ["/reference/tsconfig.json"] = "{\"compilerOptions\":{\"composite\":true,\"outDir\":\"dist\"},\"files\":[\"index.ts\"]}",
                ["/reference/package.json"] = "{\"name\":\"reference\",\"version\":\"1.0.0\",\"types\":\"dist/index.d.ts\"}",
                ["/reference/index.ts"] = "export const value = 1;"
            };
            var fs = FS(files, "/src/node_modules/reference", "/reference");
            var host = Host(fs);
            var config = Parse(fs, new() { PreserveSymlinks = preserve ? Tristate.True : Tristate.False });
            var program = CompilerProgram.NewProgram(new() { Config = config, Host = host, UseSourceOfProjectReference = true });
            Check.True(program.GetSourceFile(File("/reference/index.ts"))is not null, "reference source loaded");
            Check.True(!host.FS().FileExists(File("/reference/dist/index.d.ts")), "no generated output invented");
            var newFiles = new Dictionary<string, string>(files)
            {
                ["/src/index.ts"] = "\n" + files["/src/index.ts"],
                ["/probe/package.json"] = "{\"name\":\"new-host\"}"
            };
            var newHost = Host(FS(newFiles, "/src/node_modules/reference", "/reference"));
            var(cloned, _, reused) = program.ReuseProgram(Key("/src/index.ts"), newHost);
            Check.True(reused, "reference clone reused");
            Check.True(ReferenceEquals(cloned!.projectReferenceFileMapper, program.projectReferenceFileMapper), "reference mapper shared");
            Check.True(cloned.GetSourceFile(File("/reference/index.ts"))is not null, "clone reference source loaded");
            Check.True(cloned.GetPackageJsonInfo(File("/probe/package.json"))is not null, "clone host lookup");
            var(resolved, _, error) = cloned.newResolver().ResolveModuleName("reference", File("/src/nested/probe.ts"), ResolutionMode.CommonJS);
            Check.True(error is null, "clone resolver nil error");
            Check.True(resolved.IsResolved(), "clone faking host resolution");
            Check.True(resolved!.ResolvedFileName.AsString().EndsWith("/dist/index.d.ts", StringComparison.Ordinal), "declaration resolution suffix");
        }
    }

    private static void ImportSource()
    {
        foreach (var row in new[]
        {
            (Source: "import source a from \"./a.js\";", Evaluation: "import { a as value } from \"./a.js\";"),
            (Source: "import.source(\"./a.js\");", Evaluation: "import(\"./a.js\");")
        }

        )
        {
            string tail = "import source b from \"missing\"; import.source(\"other\");";
            string content = row.Source + tail;
            Dictionary<string, string> files = new()
            {
                ["/src/tsconfig.json"] = "{\"compilerOptions\":{\"module\":\"esnext\",\"noLib\":true},\"files\":[\"index.ts\"]}",
                ["/src/index.ts"] = content,
                ["/src/a.ts"] = "export const a = 1;"
            };
            var fs = FS(files);
            var config = Parse(fs);
            var program = CompilerProgram.NewProgram(new() { Config = config, Host = Host(fs) });
            var file = program.GetSourceFile(File("/src/index.ts"))!;
            Check.True(program.GetSourceFile(File("/src/a.ts"))is null, "source phase does not load imported source");
            Check.Equal(program.GetResolvedModules()[fs.CaseSensitivity().PathKey(File(file.FileNameGoBytes()).AsPath()).AsString()]?.Count ?? 0, 0, "source phase module cache empty");
            Check.Equal(program.GetUnresolvedImports().Count, 0, "source phase unresolved imports empty");
            Check.Equal(program.collectPackageNames().unresolved.Count, 0, "source phase unresolved packages empty");
            files["/src/index.ts"] = row.Evaluation + tail;
            var(evaluated, evaluatedFile, reused) = program.UpdateProgram(Key("/src/index.ts"), Host(FS(files)));
            program = evaluated;
            file = evaluatedFile!;
            Check.True(!reused, "source/evaluation update rebuilds");
            Check.True(program.GetSourceFile(File("/src/a.ts"))is not null, "evaluation loads imported source");
            foreach (var specifier in file.Imports()!)
                Check.Equal(program.GetResolvedModuleFromModuleSpecifier(file, specifier).IsResolved(), !Ast.IsSourcePhaseImport(specifier.Parent), "phase resolution decision");
            files["/src/index.ts"] = content;
            var(sourceAgain, _, sourceReused) = program.UpdateProgram(Key("/src/index.ts"), Host(FS(files)));
            program = sourceAgain;
            Check.True(!sourceReused, "evaluation/source update rebuilds");
            Check.True(program.GetSourceFile(File("/src/a.ts"))is null, "source phase removes imported source");
            files["/src/index.ts"] = row.Evaluation + content;
            program = CompilerProgram.NewProgram(new() { Config = config, Host = Host(FS(files)) });
            file = program.GetSourceFile(File("/src/index.ts"))!;
            foreach (var specifier in file.Imports()!)
                Check.Equal(program.GetResolvedModuleFromModuleSpecifier(file, specifier).IsResolved(), !Ast.IsSourcePhaseImport(specifier.Parent), "mixed phase resolution decision");
        }
    }

    private static void MissingCasing()
    {
        Check.True(Bundled.Embedded, "source embedded guard");
        var fs = Bundled.WrapFS(FS(new() { ["/src/myFile.ts"] = "export const y = 2;" }));
        var config = ConfigParser.NewParsedCommandLine(new() { SkipDefaultLibCheck = Tristate.True }, [File("/src/MyFile.ts"), File("/src/myFile.ts")], null, RootedDirectoryPath.FromGoString("/"), fs.CaseSensitivity());
        var program = CompilerProgram.NewProgram(new() { Config = config, Host = CompilerHosts.NewCompilerHost(fs, Bundled.LibPath()) });
        _ = program.GetProgramDiagnostics(); // Exact original obligation: does not panic.
    }
}

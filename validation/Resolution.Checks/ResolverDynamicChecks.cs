// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Full input/assertion domains of ten original dynamic/rootDirs resolver bodies.
// Independent top-level/subtest scheduling is sequential; no source cases are removed.
using Configuration.Checks;
using TypeScript.Modules;
using TypeScript.Scanner;

namespace Resolution.Checks;
public static class ResolverDynamicChecks
{
    private const string Dynamic = "^/~ts-uri~/custom/ts-nul-authority";
    public static IReadOnlyList<string> Cases { get; } = ["TestResolveDynamicModuleNameUsingRootDirs", "TestRootDirsPreservesExceptionalDynamicSegments", "TestRootDirsRejectsUnrepresentableDiskSegments", "TestRootDirsEscapesRootLikeDynamicSegments", "TestRootDirsPreservesRelativeDiskSuffixes", "TestRootDirsPreservesRelativeDirectoryIntent", "TestResolveDynamicPackageJSONPath", "TestResolveDynamicPackageSubpathFile", "TestResolveDynamicESMPackageIndexFromReservedDirectory", "TestResolveDynamicDottedDirectory"];

    public static void Run(string name)
    {
        if (name == "TestResolveDynamicModuleNameUsingRootDirs")
        {
            foreach (var row in new (string Root, string Target)[]
            {
                (Dynamic + "/generated", Dynamic + "/generated/~ts-uri-escape~7e74732d7572692d6573636170657e66696c65~.ts"),
                ("c:/generated", "c:/generated/~ts-uri-escape~file.ts")
            }

            )
                Roots(Dynamic + "/src/main.ts", row.Target, Dynamic + "/src", row.Root, Dynamic + "/", "./~ts-uri-escape~file", [], ResolutionMode.ESNext);
            return;
        }

        if (name == "TestRootDirsPreservesExceptionalDynamicSegments")
        {
            Roots(Dynamic + "/src/~ts-uri-escape~2e2e~/main.ts", Dynamic + "/generated/~ts-uri-escape~2e2e~/dep.ts", Dynamic + "/src", Dynamic + "/generated", Dynamic + "/", "./dep", [(Dynamic + "/dep.ts", "export const wrong = 1;")], ResolutionMode.ESNext);
            return;
        }

        if (name == "TestRootDirsRejectsUnrepresentableDiskSegments")
        {
            foreach (string source in new[]
            {
                Dynamic + "/src/~ts-uri-escape~2e2e~/main.ts",
                Dynamic + "/src/c:/main.ts",
                Dynamic + "/src/^/main.ts"
            }

            )
            {
                var options = ResolutionFixtures.Bundler();
                options.RootDirs = [RootedDirectoryPath.FromUnicode(Dynamic + "/src"), RootedDirectoryPath.FromUnicode("c:/generated")];
                var resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = ResolutionFixtures.Host(ResolutionFixtures.Fs((source, ""), ("c:/dep.ts", "export const wrong = 1;")), Dynamic + "/"), CompilerOptions = options });
                var(resolved, _, _) = resolver.ResolveModuleName("./dep", RootedFilePath.FromUnicode(source), ResolutionMode.ESNext, null);
                Check.True(resolved?.IsResolved() != true, source + " original unrepresentable disk path stays unresolved");
            }

            return;
        }

        if (name == "TestRootDirsEscapesRootLikeDynamicSegments")
        {
            foreach (var row in new (string Source, string Target)[]
            {
                (Dynamic + "/src/c:/main.ts", Dynamic + "/generated/~ts-uri-escape~633a~/dep.ts"),
                (Dynamic + "/src/^/main.ts", Dynamic + "/generated/~ts-uri-escape~5e~/dep.ts")
            }

            )
                Roots(row.Source, row.Target, Dynamic + "/src", Dynamic + "/generated", Dynamic + "/", "./dep", [("c:/dep.ts", "export const wrong = 1;"), ("^/dep.ts", "export const wrong = 1;")], ResolutionMode.ESNext);
            return;
        }

        if (name == "TestRootDirsPreservesRelativeDiskSuffixes")
        {
            foreach (var row in new (string Source, string Root, string Target)[]
            {
                ("c:/src/^/main.ts", Dynamic + "/generated", Dynamic + "/generated/~ts-uri-escape~5e~/dep.ts"),
                ("c:/src/c:/main.ts", "d:/generated", "d:/generated/c:/dep.ts")
            }

            )
                Roots(row.Source, row.Target, "c:/src", row.Root, "c:/", "./dep", [("^/dep.ts", "export const wrong = 1;"), ("c:/dep.ts", "export const wrong = 1;")], ResolutionMode.ESNext);
            return;
        }

        if (name == "TestRootDirsPreservesRelativeDirectoryIntent")
        {
            foreach (var row in new (string Name, string Root, string Source, string TargetRoot, string Target)[]
            {
                ("disk to disk", "c:/src", "c:/src/^/main.ts", "d:/generated", "d:/generated/^/pkg/index.ts"),
                ("disk to dynamic", "c:/src", "c:/src/^/main.ts", Dynamic + "/generated", Dynamic + "/generated/~ts-uri-escape~5e~/pkg/index.ts"),
                ("empty suffix to dynamic", "c:/src", "c:/src/main.ts", Dynamic + "/generated", Dynamic + "/generated/index.ts"),
                ("empty suffix to disk", Dynamic + "/src", Dynamic + "/src/main.ts", "d:/generated", "d:/generated/index.ts"),
                ("literal empty segment to dynamic", Dynamic + "/src", Dynamic + "/src/~ts-uri-escape~~/main.ts", Dynamic + "/generated", Dynamic + "/generated/~ts-uri-escape~~/index.ts")
            }

            )
            {
                string moduleName = row.Name.Contains("empty suffix", StringComparison.Ordinal) || row.Name == "literal empty segment to dynamic" ? "./" : "./pkg/";
                Roots(row.Source, row.Target, row.Root, row.TargetRoot, "c:/", moduleName, [("^/pkg/index.ts", "export const wrong = 1;"), ("d:/generated.ts", "export const wrong = 1;")], ResolutionMode.CommonJS);
            }

            return;
        }

        if (name == "TestResolveDynamicPackageJSONPath")
        {
            string source = Dynamic + "/src/main.ts";
            string fallback = Dynamic + "/node_modules/pkg/~ts-uri-escape~7e74732d7572692d6573636170657e7479706573~.d.ts";
            string target = Dynamic + "/node_modules/pkg/ts3.1/~ts-uri-escape~7e74732d7572692d6573636170657e7479706573~.d.ts";
            var resolved = Module(source, target, "pkg", ResolutionMode.ESNext, [(source, ""), (Dynamic + "/node_modules/pkg/package.json", "{\"name\":\"pkg\",\"types\":\"~ts-uri-escape~types.d.ts\",\"typesVersions\":{\"*\":{\"*\":[\"ts3.1/*\"]}}}"), (fallback, "export const fallback: number;"), (target, "export const value: number;")]);
            Check.Equal(resolved.Extension, TsPath.ExtensionDts, "Original dynamic declaration extension");
            Check.True(resolved.ResolvedFileName.IsDeclarationFile(), "Original dynamic declaration classification");
            return;
        }

        if (name == "TestResolveDynamicPackageSubpathFile")
        {
            string source = Dynamic + "/src/main.ts";
            string target = Dynamic + "/node_modules/pkg/~ts-uri-escape~7e74732d7572692d6573636170657e3636366636667e~.ts";
            Module(source, target, "pkg/~ts-uri-escape~666f6f~.ts", ResolutionMode.ESNext, [(source, ""), (Dynamic + "/node_modules/pkg/package.json", "{\"name\":\"pkg\"}"), (target, "export const value = 1;")]);
            return;
        }

        if (name == "TestResolveDynamicESMPackageIndexFromReservedDirectory")
        {
            string source = Dynamic + "/src/main.ts", packageName = "~ts-uri-escape~pkg.js";
            string directory = Dynamic + "/node_modules/~ts-uri-escape~7e74732d7572692d6573636170657e706b672e6a73~";
            string target = directory + "/index.js";
            Module(source, target, packageName, ResolutionMode.ESNext, [(source, ""), (directory + "/package.json", "{\"name\":\"" + packageName + "\"}"), (target, "exports.value = 1;")]);
            return;
        }

        if (name == "TestResolveDynamicDottedDirectory")
        {
            string source = Dynamic + "/src/main.ts";
            string target = Dynamic + "/src/~ts-uri-escape~7e74732d7572692d6573636170657e6469722e6a73~/index.ts";
            Module(source, target, "./~ts-uri-escape~dir.js", ResolutionMode.CommonJS, [(source, ""), (target, "export const value = 1;")]);
            return;
        }

        throw new ArgumentException("Unknown original dynamic resolver body.", nameof(name));
    }

    private static void Roots(string source, string target, string sourceRoot, string targetRoot, string cwd, string name, (string Path, string Contents)[] additional, ResolutionMode mode)
    {
        var options = ResolutionFixtures.Bundler();
        options.Module = mode == ResolutionMode.CommonJS ? ModuleKind.CommonJS : ModuleKind.ESNext;
        options.RootDirs = [RootedDirectoryPath.FromUnicode(sourceRoot), RootedDirectoryPath.FromUnicode(targetRoot)];
        var files = new List<(string Path, string Contents)>
        {
            (source, ""),
            (target, "export const value = 1;")
        };
        files.AddRange(additional);
        var resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = ResolutionFixtures.Host(ResolutionFixtures.Fs(files.ToArray()), cwd), CompilerOptions = options });
        var(resolved, _, _) = resolver.ResolveModuleName(name, RootedFilePath.FromUnicode(source), mode, null);
        Check.True(resolved?.IsResolved() == true, "Original rootDirs resolution: " + source);
        Check.Equal(resolved!.ResolvedFileName, RootedFilePath.FromUnicode(target), "Original rootDirs target: " + source);
    }

    private static ResolvedModule Module(string source, string target, string name, ResolutionMode mode, (string Path, string Contents)[] files)
    {
        var options = ResolutionFixtures.Bundler();
        options.Module = mode == ResolutionMode.CommonJS ? ModuleKind.CommonJS : ModuleKind.ESNext;
        var resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = ResolutionFixtures.Host(ResolutionFixtures.Fs(files), Dynamic + "/"), CompilerOptions = options });
        var(resolved, _, _) = resolver.ResolveModuleName(name, RootedFilePath.FromUnicode(source), mode, null);
        Check.True(resolved?.IsResolved() == true, "Original dynamic module resolves: " + name);
        Check.Equal(resolved!.ResolvedFileName, RootedFilePath.FromUnicode(target), "Original dynamic module target: " + name);
        return resolved!;
    }
}

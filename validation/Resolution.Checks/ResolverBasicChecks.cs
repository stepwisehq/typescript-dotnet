// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete source assertions from three pinned internal/module/resolver_test.go
// functions. Fixtures retain every source file, option, query and predicate.
using Configuration.Checks;
using TypeScript.Modules;
using TypeScript.Scanner;

namespace Resolution.Checks;
public static class ResolverBasicChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestResolveModuleNameTrailingSlash", "TestResolveModuleNameBlockedByNullExport", "TestResolveModuleNameExportTargetWithTrailingSlashDoesNotResolveAsFile"];

    public static void Run(string name)
    {
        if (name == "TestResolveModuleNameTrailingSlash")
        {
            var fs = ResolutionFixtures.Fs(("/repo/node_modules/pkg/package.json", "{\"name\":\"pkg\",\"main\":\"main.js\",\"types\":\"main.d.ts\"}"), ("/repo/node_modules/pkg/main.d.ts", "export const x: number;"), ("/repo/node_modules/pkg/main.js", "exports.x = 1;"), ("/repo/src/file.ts", ""));
            var resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = ResolutionFixtures.Host(fs), CompilerOptions = ResolutionFixtures.Bundler(trace: true) });
            foreach (string specifier in new[]
            {
                "pkg",
                "pkg/"
            }

            )
            {
                var(result, _, _) = resolver.ResolveModuleName(specifier, RootedFilePath.FromUnicode("/repo/src/file.ts"), ResolutionMode.ESNext, null);
                Check.True(result?.IsResolved() == true, specifier + " resolves");
                Check.Equal(result!.ResolvedPath, CaseSensitivity.CaseSensitive.PathKey(result.ResolvedFileName.AsPath()), specifier + " canonical path key");
            }

            return;
        }

        if (name is "TestResolveModuleNameBlockedByNullExport" or "TestResolveModuleNameExportTargetWithTrailingSlashDoesNotResolveAsFile")
        {
            bool blocked = name == "TestResolveModuleNameBlockedByNullExport";
            List<(string Path, string Contents)> files = [("/repo/node_modules/pkg/package.json", blocked ? "{\"name\":\"pkg\",\"exports\":{\".\":null}}" : "{\"name\":\"pkg\",\"exports\":{\".\":\"./index.d.ts/\"}}"), ("/repo/src/file.ts", "")];
            if (!blocked)
                files.Add(("/repo/node_modules/pkg/index.d.ts", "export const x: number;"));
            var resolver = DefaultResolver.NewResolver(new ResolverOptions { Host = ResolutionFixtures.Host(ResolutionFixtures.Fs(files.ToArray())), CompilerOptions = ResolutionFixtures.Bundler() });
            var(result, _, _) = resolver.ResolveModuleName("pkg", RootedFilePath.FromUnicode("/repo/src/file.ts"), ResolutionMode.ESNext, null);
            Check.True(result?.IsResolved() != true, name + " remains unresolved");
            if (blocked)
                Check.Equal(result!.ResolvedPath, PathKey.FromGoString(""), name + " source zero path");
            return;
        }

        throw new ArgumentException("Unknown original basic resolver assertion body.", nameof(name));
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete three remaining internal/module/resolver_internal_test.go bodies.
// The fourth candidate body is source-linked unchanged from accepted checks.
using Configuration.Checks;
using TypeScript.Modules;
using TypeScript.Scanner;

namespace Resolution.Checks;
public static class ResolverInternalChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestPackageJSONPathWithTrailingSeparatorDoesNotResolveAsFile", "TestExtensionReplacementPreservesEmptyAndDotStems", "TestOutputDirectoriesRemoveTrailingSeparators"];

    public static void Run(string name)
    {
        if (name == "TestPackageJSONPathWithTrailingSeparatorDoesNotResolveAsFile")
        {
            var state = new DefaultResolver.resolutionState
            {
                resolver = new DefaultResolver
                {
                    fs = ResolutionFixtures.Fs(("/project/index.d.ts", "export {};"))
                },
                compilerOptions = new()
            };
            var candidate = DefaultResolver.resolveResolutionCandidate("/project", "index.d.ts/");
            var result = state.loadFileNameFromPackageJSONField(Extensions.Declaration, candidate, "./index.d.ts/");
            // Original shouldContinueSearching is precisely a nil pointer test.
            Check.True(result is null, "Original directory-only package JSON candidate continues searching");
            return;
        }

        if (name == "TestExtensionReplacementPreservesEmptyAndDotStems")
        {
            foreach (var row in new (string ImportName, string Expected)[]
            {
                (".js", "/project/.native.ts"),
                ("..js", "/project/..native.ts"),
                ("...js", "/project/...native.ts"),
                (".js", "/project/.native.d.ts")
            }

            )
            {
                var state = new DefaultResolver.resolutionState
                {
                    resolver = new DefaultResolver
                    {
                        resolutionData = new(),
                        fs = ResolutionFixtures.Fs((row.Expected, "export {};"))
                    },
                    compilerOptions = new()
                    {
                        ModuleSuffixes = [".native", ""]
                    }
                };
                var result = state.loadModuleFromFileNoImplicitExtensions(Extensions.TypeScript | Extensions.Declaration, DefaultResolver.resolveResolutionCandidate("/project", row.ImportName));
                Check.True(result is not null, row.Expected + " original extension replacement result");
                Check.Equal(result!.path, RootedFilePath.FromUnicode(row.Expected), row.Expected + " original extension replacement path");
            }

            return;
        }

        if (name == "TestOutputDirectoriesRemoveTrailingSeparators")
        {
            var state = new DefaultResolver.resolutionState
            {
                resolver = new DefaultResolver
                {
                    fs = ResolutionFixtures.Fs()
                },
                compilerOptions = new()
                {
                    DeclarationDir = TsPath.RootedDirectoryPathFromAbsolute("/project/types/"),
                    OutDir = TsPath.RootedDirectoryPathFromAbsolute("/project/dist/")
                }
            };
            var directories = state.getOutputDirectories();
            RootedDirectoryPath[] expected = [RootedDirectoryPath.FromUnicode("/project/types"), RootedDirectoryPath.FromUnicode("/project/dist")];
            Check.Equal(directories!.Count, expected.Length, "Original output directory count");
            for (int i = 0; i < expected.Length; i++)
                Check.Equal(directories[i], expected[i], "Original output directory " + i);
            return;
        }

        throw new ArgumentException("Unknown original internal resolver body.", nameof(name));
    }
}

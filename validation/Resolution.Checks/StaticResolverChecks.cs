// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Assertion-preserving port of internal/module/staticresolver_test.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e; sequential top-level scheduling only.
using System.Runtime.CompilerServices;
using Configuration.Checks;
using TypeScript.Modules;
using TypeScript.Scanner;

namespace Resolution.Checks;
public static class StaticResolverChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestStaticResolver"];

    public static void Run(string name)
    {
        if (name != "TestStaticResolver")
            throw new ArgumentException("Unknown original static resolver assertion body.", nameof(name));
        var fs = ResolutionFixtures.Fs(("/repo/node_modules/fallback/package.json", "{\"name\":\"fallback\",\"types\":\"index.d.ts\"}"), ("/repo/node_modules/fallback/index.d.ts", "export {};"));
        var fallback = DefaultResolver.NewResolver(new ResolverOptions { Host = ResolutionFixtures.Host(fs), CompilerOptions = new() { Module = ModuleKind.ESNext, ModuleResolution = ModuleResolutionKind.Bundler } });
        var esm = new StrongBox<ResolutionMode>(ResolutionMode.ESNext);
        var(resolutions, error) = StaticResolutions.NewStaticResolutions([new() { ModuleName = "provided", Result = new() { ResolvedFileName = RootedFilePath.FromUnicode("/global.d.ts") } }, new() { ModuleName = "provided", ContainingDirectory = RootedDirectoryPath.FromUnicode("/repo/src"), Result = new() { ResolvedFileName = RootedFilePath.FromUnicode("/directory.d.ts") } }, new() { ModuleName = "provided", ResolutionMode = esm, Result = new() { ResolvedFileName = RootedFilePath.FromUnicode("/esm.d.ts") } }, new() { ModuleName = "provided", ContainingDirectory = RootedDirectoryPath.FromUnicode("/repo/src"), ResolutionMode = esm, Result = new() { ResolvedFileName = RootedFilePath.FromUnicode("/directory-esm.d.ts") } }, new() { ModuleName = "unresolved" }], true, RootedDirectoryPath.FromUnicode("/repo"), CaseSensitivity.CaseSensitive);
        Check.True(error is null, "NewStaticResolutions original nil error");
        var resolver = StaticResolver.NewStaticResolver(fallback, resolutions!);
        foreach (var row in new (string Name, string File, ResolutionMode Mode, string Expected)[]
        {
            ("provided", "/repo/src/index.ts", ResolutionMode.ESNext, "/directory-esm.d.ts"),
            ("provided", "/repo/src/index.ts", ResolutionMode.CommonJS, "/directory.d.ts"),
            ("provided", "/repo/other/index.ts", ResolutionMode.ESNext, "/esm.d.ts"),
            ("provided", "/repo/other/index.ts", ResolutionMode.CommonJS, "/global.d.ts"),
            ("fallback", "/repo/src/index.ts", ResolutionMode.ESNext, "/repo/node_modules/fallback/index.d.ts"),
            ("unresolved", "/repo/src/index.ts", ResolutionMode.ESNext, "")
        }

        )
        {
            var(result, _, resolutionError) = resolver.ResolveModuleName(row.Name, RootedFilePath.FromUnicode(row.File), row.Mode, null);
            Check.True(resolutionError is null, row.Name + " original nil error");
            if (row.Expected == "")
                Check.True(result is null, row.Name + " original nil result");
            else
                Check.Equal(result!.ResolvedFileName, RootedFilePath.FromUnicode(row.Expected), row.Name + " source resolved filename");
        }
    }
}

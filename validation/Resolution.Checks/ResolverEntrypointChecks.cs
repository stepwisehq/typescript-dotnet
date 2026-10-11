// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete thirteen original typed-result/entrypoint assertion bodies.
using Configuration.Checks;
using TypeScript.Modules;
using TypeScript.PackageJson;
using TypeScript.Scanner;

namespace Resolution.Checks;
public static class ResolverEntrypointChecks
{
    private const string Dynamic = "^/~ts-uri~/custom/ts-nul-authority";
    public static IReadOnlyList<string> Cases { get; } = ["TestResolvedModuleCarriesCaseInsensitivePath", "TestResolvedTypeReferenceDirectiveCarriesPath", "TestResolvedEntrypointCarriesPath", "TestDynamicExportPatternEntrypointUsesLogicalName", "TestGeneratedDynamicEntrypointSpecifierResolvesEncodedFile", "TestDynamicEntrypointsEncodePhysicalPackageNameAndRootLikePath", "TestDynamicExportPatternPreservesStaticSourcePrefix", "TestDynamicExportPatternKeepsPackageForRootLikeMatch", "TestDynamicEntrypointsPreserveCaseDistinctFiles", "TestDiskExportPatternKeepsReservedPrefixLiteral", "TestDynamicExportPatternUnderExceptionalParent", "TestDynamicExportPatternWithEmptyMatch", "TestUnaddressableEmptyExportPatternIsNotAnEntrypoint"];

    private sealed class CaseInsensitiveViewFileSystem(ConfigurationFileSystem inner) : ForwardingFileSystem(inner)
    {
        // Original wrapper changes declared case mode only, retaining two physical
        // case-distinct files in the case-sensitive underlying source fixture.
        public override CaseSensitivity CaseSensitivity() => TypeScript.Scanner.CaseSensitivity.CaseInsensitive;
    }

    public static void Run(string name)
    {
        if (name is "TestResolvedModuleCarriesCaseInsensitivePath" or "TestResolvedEntrypointCarriesPath")
        {
            var fs = new ConfigurationFileSystem(new[] { new ConfigurationFileSystem.Input("/Repo/node_modules/Pkg/package.json", "{\"name\":\"pkg\",\"types\":\"Index.d.ts\"}"), new ConfigurationFileSystem.Input("/Repo/node_modules/Pkg/Index.d.ts", "export const x: number;"), new ConfigurationFileSystem.Input("/Repo/src/file.ts") }, CaseSensitivity.CaseInsensitive);
            var resolver = New(fs, "/Repo");
            var resolved = Resolve(resolver, "Pkg", "/Repo/src/file.ts");
            Check.True(resolved?.IsResolved() == true, "Original case-insensitive module resolves");
            if (name == "TestResolvedModuleCarriesCaseInsensitivePath")
                Check.Equal(resolved!.ResolvedPath, CaseSensitivity.CaseInsensitive.PathKey(resolved.ResolvedFileName.AsPath()), "Original module canonical path key");
            else
            {
                var entries = Entrypoints(resolver, "Pkg", false);
                Check.Equal(entries.Count, 1, "Original case-insensitive entrypoint count");
                Check.Equal(entries[0].ResolvedPath, CaseSensitivity.CaseInsensitive.PathKey(entries[0].ResolvedFileName.AsPath()), "Original entrypoint canonical path key");
            }

            return;
        }

        if (name == "TestResolvedTypeReferenceDirectiveCarriesPath")
        {
            var fs = new ConfigurationFileSystem(new[] { new ConfigurationFileSystem.Input("/repo/node_modules/@types/pkg/package.json", "{\"name\":\"@types/pkg\",\"types\":\"index.d.ts\"}"), new ConfigurationFileSystem.Input("/repo/node_modules/@types/pkg/index.d.ts", "export const x: number;"), new ConfigurationFileSystem.Input("/repo/src/file.ts") }, CaseSensitivity.CaseInsensitive);
            var resolver = New(fs, "/repo");
            var(resolved, _) = resolver.ResolveTypeReferenceDirective("pkg", RootedFilePath.FromUnicode("/repo/src/file.ts"), ResolutionMode.None, null);
            Check.True(resolved?.IsResolved() == true, "Original directive resolves");
            Check.Equal(resolved!.ResolvedPath, CaseSensitivity.CaseInsensitive.PathKey(resolved.ResolvedFileName.AsPath()), "Original directive canonical path key");
            return;
        }

        if (name == "TestDynamicExportPatternEntrypointUsesLogicalName")
        {
            foreach (var row in new (string Name, string PackageJSON, string Import, string Expected)[]
            {
                ("logical wildcard match", "{\"name\":\"Pkg\",\"exports\":{\"./*\":\"./*.d.ts\"}}", "Pkg/~ts-uri-escape~value", "Pkg/~ts-uri-spec~7e74732d7572692d6573636170657e76616c7565~"),
                ("reserved target pattern", "{\"name\":\"Pkg\",\"exports\":{\"./*\":\"./~ts-uri-escape~*.d.ts\"}}", "Pkg/value", "Pkg/value"),
                ("reserved wildcard with trailer", "{\"name\":\"Pkg\",\"exports\":{\"./*\":\"./*-impl.d.ts\"}}", "Pkg/~ts-uri-escape~value", "Pkg/~ts-uri-spec~7e74732d7572692d6573636170657e76616c7565~")
            }

            )
            {
                string target = Dynamic + "/node_modules/Pkg/~ts-uri-escape~7e74732d7572692d6573636170657e" + (row.Name == "reserved wildcard with trailer" ? "76616c75652d696d706c" : "76616c7565") + "~.d.ts";
                var resolver = Package(Dynamic + "/src/main.ts", Dynamic + "/node_modules/Pkg", row.PackageJSON, (target, "export const value: number;"));
                Check.True(Resolve(resolver, row.Import, Dynamic + "/src/main.ts")?.IsResolved() == true, row.Name + " original export resolves");
                var entries = Entrypoints(resolver, "Pkg", false);
                Check.Equal(entries.Count, 1, row.Name + " original entrypoint count");
                Check.Equal(entries[0].ModuleSpecifier.AsString(), row.Expected, row.Name + " original logical specifier");
            }

            return;
        }

        if (name is "TestGeneratedDynamicEntrypointSpecifierResolvesEncodedFile" or "TestDynamicEntrypointsEncodePhysicalPackageNameAndRootLikePath")
        {
            bool physical = name == "TestDynamicEntrypointsEncodePhysicalPackageNameAndRootLikePath";
            string source = Dynamic + "/src/main.ts";
            string physicalName = "~ts-uri-escape~7e74732d7572692d6573636170657e706b672e6a73~";
            string sourceName = "~ts-uri-spec~7e74732d7572692d6573636170657e706b672e6a73~";
            string directory = Dynamic + "/node_modules/" + (physical ? physicalName : "Pkg");
            string target = directory + (physical ? "/~ts-uri-escape~633a~/value.d.ts" : "/~ts-uri-escape~7e74732d7572692d6573636170657e76616c7565~.d.ts");
            string expected = physical ? sourceName + "/~ts-uri-spec~633a~/value.d.ts" : "Pkg/~ts-uri-spec~7e74732d7572692d6573636170657e76616c7565~.d.ts";
            var resolver = Package(source, directory, physical ? "{\"name\":\"~ts-uri-escape~pkg.js\"}" : "{\"name\":\"Pkg\"}", (target, "export const value: number;"));
            Resolve(resolver, physical ? sourceName : "Pkg", source); // Original cache seeding, no success assertion.
            var entries = Entrypoints(resolver, physical ? physicalName : "Pkg", true);
            Check.Equal(entries.Count, 1, "Original directory-search entrypoint count");
            Check.Equal(entries[0].ModuleSpecifier.AsString(), expected, "Original encoded generated specifier");
            var resolved = Resolve(resolver, expected, source);
            Check.True(resolved?.IsResolved() == true, "Original generated specifier resolves");
            Check.Equal(resolved!.ResolvedFileName, RootedFilePath.FromUnicode(target), "Original generated specifier target");
            return;
        }

        if (name == "TestDynamicEntrypointsPreserveCaseDistinctFiles")
        {
            string source = Dynamic + "/src/main.ts", directory = Dynamic + "/node_modules/Pkg";
            var fs = new CaseInsensitiveViewFileSystem(ResolutionFixtures.Fs((source, ""), (directory + "/package.json", "{\"name\":\"Pkg\",\"types\":\"Foo.d.ts\"}"), (directory + "/Foo.d.ts", "export const upper: number;"), (directory + "/foo.d.ts", "export const lower: number;")));
            var resolver = New(fs, Dynamic + "/");
            Resolve(resolver, "Pkg", source);
            Check.Equal(Entrypoints(resolver, "Pkg", true).Count, 2, "Original case-distinct entrypoint count");
            return;
        }

        if (name is "TestDynamicExportPatternPreservesStaticSourcePrefix" or "TestDynamicExportPatternKeepsPackageForRootLikeMatch" or "TestDiskExportPatternKeepsReservedPrefixLiteral" or "TestDynamicExportPatternUnderExceptionalParent")
        {
            bool disk = name == "TestDiskExportPatternKeepsReservedPrefixLiteral";
            bool parent = name == "TestDynamicExportPatternUnderExceptionalParent";
            bool prefix = name == "TestDynamicExportPatternPreservesStaticSourcePrefix";
            bool rootLike = name == "TestDynamicExportPatternKeepsPackageForRootLikeMatch";
            string basePath = disk ? "/repo" : parent ? Dynamic + "/work/~ts-uri-escape~2e2e~" : Dynamic;
            string source = basePath + "/src/main.ts", packageName = disk ? "pkg" : "Pkg";
            string directory = basePath + "/node_modules/" + packageName;
            string target = directory + (disk ? "/~ts-uri-escape~666f6f~.d.ts" : rootLike ? "/~ts-uri-escape~633a~/value.d.ts" : "/foo.d.ts");
            string expected = prefix ? "Pkg/~ts-uri-escape~prefix/foo" : rootLike ? "Pkg/~ts-uri-spec~633a~/value" : disk ? "pkg/~ts-uri-escape~666f6f~" : "Pkg/foo";
            string packageJSON = prefix ? "{\"name\":\"Pkg\",\"exports\":{\"./~ts-uri-escape~prefix/*\":\"./*.d.ts\"}}" : "{\"name\":\"" + packageName + "\",\"exports\":{\"./*\":\"./*.d.ts\"}}";
            var resolver = Package(source, directory, packageJSON, (target, "export const value: number;"));
            Check.True(Resolve(resolver, expected, source)?.IsResolved() == true, "Original pattern export resolves");
            var entries = Entrypoints(resolver, packageName, false);
            Check.Equal(entries.Count, 1, "Original pattern entrypoint count");
            Check.Equal(entries[0].ModuleSpecifier.AsString(), expected, "Original pattern specifier");
            return;
        }

        if (name is "TestDynamicExportPatternWithEmptyMatch" or "TestUnaddressableEmptyExportPatternIsNotAnEntrypoint")
        {
            bool emptyMatch = name == "TestDynamicExportPatternWithEmptyMatch";
            string source = Dynamic + "/src/main.ts", directory = Dynamic + "/node_modules/Pkg", target = directory + "/index.d.ts";
            var resolver = Package(source, directory, emptyMatch ? "{\"name\":\"Pkg\",\"exports\":{\"./foo/*\":\"./index*.d.ts\"}}" : "{\"name\":\"Pkg\",\"exports\":{\"./*\":\"./index*.d.ts\"}}", (target, "export const value: number;"));
            var resolved = Resolve(resolver, emptyMatch ? "Pkg/foo/" : "Pkg", source);
            if (emptyMatch)
                Check.True(resolved?.IsResolved() == true, "Original empty wildcard export resolves");
            var entries = Entrypoints(resolver, "Pkg", false);
            Check.Equal(entries.Count, emptyMatch ? 1 : 0, "Original empty/unaddressable wildcard count");
            if (emptyMatch)
            {
                resolved = Resolve(resolver, entries[0].ModuleSpecifier.AsString(), source);
                Check.True(resolved?.IsResolved() == true, "Original generated empty wildcard resolves");
                Check.Equal(resolved!.ResolvedFileName, RootedFilePath.FromUnicode(target), "Original generated empty wildcard target");
            }

            return;
        }

        throw new ArgumentException("Unknown original entrypoint assertion body.", nameof(name));
    }

    private static DefaultResolver New(TypeScript.Vfs.IFS fs, string cwd) => DefaultResolver.NewResolver(new ResolverOptions { Host = ResolutionFixtures.Host(fs, cwd), CompilerOptions = ResolutionFixtures.Bundler() });
    private static DefaultResolver Package(string source, string directory, string packageJSON, params (string Path, string Contents)[] files)
    {
        var all = new List<(string Path, string Contents)>
        {
            (source, ""),
            (directory + "/package.json", packageJSON)
        };
        all.AddRange(files);
        return New(ResolutionFixtures.Fs(all.ToArray()), source.StartsWith("/repo/", StringComparison.Ordinal) ? "/repo" : Dynamic + "/");
    }

    private static ResolvedModule? Resolve(DefaultResolver resolver, string name, string source) => resolver.ResolveModuleName(name, RootedFilePath.FromUnicode(source), ResolutionMode.ESNext, null).Result;
    private static Slice<ResolvedEntrypoint> Entrypoints(DefaultResolver resolver, string packageName, bool directorySearch)
    {
        InfoCacheEntry? package = null;
        resolver.PackageJsonCacheEntries((_, entry) =>
        {
            if (!entry.Exists())
                return true;
            package = entry;
            return false;
        });
        Check.True(package is not null, "Original package JSON cache entry exists");
        // len(nil) equals zero upstream; this local header is solely assertion input,
        // not an observer claim that nil and allocated-empty headers are identical.
        return resolver.GetEntrypointsFromPackageJsonInfo(package!, packageName, directorySearch);
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete assertion-preserving port of pinned internal/outputpaths/outputpaths_test.go.
using TypeScript.Scanner;
using Paths = TypeScript.OutputPaths.OutputPaths;
using TypeScript.OutputPaths;

namespace Configuration.Checks;
public static class OutputPathChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestGetSourceFileNameInNewDirSourceMatchesCommonDirectory", "TestGetSourceFileNameInNewDirCanonicalizationShrinksCommonDirectory", "TestGetBuildInfoFileNameAcrossRoots", "TestGetOutputFileNameAcrossRoots", "TestOutputFileNamesPreserveEmptyAndDotStems"];

    private sealed class Host(string directory, List<string>? extensions = null) : IOutputPathsHost
    {
        public RootedDirectoryPath CommonSourceDirectory() => RootedDirectoryPath.FromUnicode(directory);
        public List<string>? ContentMapperExtensions() => extensions;
        public CaseSensitivity CaseSensitivity() => TypeScript.Scanner.CaseSensitivity.CaseInsensitive;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestGetSourceFileNameInNewDirSourceMatchesCommonDirectory":
                Check.Equal(Paths.GetSourceFileNameInNewDir(RootedFilePath.FromUnicode("/project/src"), RootedDirectoryPath.FromUnicode("/project/out"), RootedDirectoryPath.FromUnicode("/project/src"), CaseSensitivity.CaseSensitive), RootedFilePath.FromUnicode("/project/src"), name);
                break;
            case "TestGetSourceFileNameInNewDirCanonicalizationShrinksCommonDirectory":
                Check.Equal(Paths.GetSourceFileNameInNewDir(RootedFilePath.FromUnicode("/kkkk/a.ts"), RootedDirectoryPath.FromUnicode("/out"), RootedDirectoryPath.FromUnicode("/KKKK"), CaseSensitivity.CaseInsensitive), RootedFilePath.FromUnicode("/out/a.ts"), name);
                break;
            case "TestGetBuildInfoFileNameAcrossRoots":
                foreach (var row in new (string Label, string Root, string Out, string Config, string Expected)[]
                {
                    ("same root", "c:/src", "c:/out", "c:/src/project/tsconfig.json", "c:/out/project/tsconfig.tsbuildinfo"),
                    ("empty stem", "c:/src", "c:/out", "c:/src/project/.json", "c:/out/project/.tsbuildinfo"),
                    ("dot stem", "c:/src", "c:/out", "c:/src/project/..json", "c:/out/project/..tsbuildinfo"),
                    ("empty filename stem without rootDir", "", "c:/out", "c:/src/project/.json", "c:/out/.tsbuildinfo"),
                    ("parent-dot filename stem without rootDir", "", "c:/out", "c:/src/project/...json", "c:/out/...tsbuildinfo"),
                    ("empty filename stem without outDir", "", "", "c:/src/project/.json", "c:/src/project/.tsbuildinfo"),
                    ("dot filename stem without outDir", "", "", "c:/src/project/..json", "c:/src/project/..tsbuildinfo"),
                    ("different drive", "c:/src", "c:/out", "d:/project/tsconfig.json", "d:/project/tsconfig.tsbuildinfo"),
                    ("different UNC authority", "//server-a/src", "//server-a/out", "//server-b/project/tsconfig.json", "//server-b/project/tsconfig.tsbuildinfo"),
                    ("different URL authority", "file://server-a/src", "file://server-a/out", "file://server-b/project/tsconfig.json", "file://server-b/project/tsconfig.tsbuildinfo")
                }

                )
                {
                    var buildOptions = new CompilerOptions
                    {
                        Incremental = Tristate.True,
                        RootDir = RootedDirectoryPath.FromUnicode(row.Root),
                        OutDir = RootedDirectoryPath.FromUnicode(row.Out),
                        ConfigFilePath = RootedFilePath.FromUnicode(row.Config)
                    };
                    Check.Equal(Paths.GetBuildInfoFileName(buildOptions, CaseSensitivity.CaseInsensitive), RootedFilePath.FromUnicode(row.Expected), row.Label);
                }

                break;
            case "TestGetOutputFileNameAcrossRoots":
                var input = RootedFilePath.FromUnicode("d:/shared.ts");
                var optionsAcross = new CompilerOptions
                {
                    OutDir = RootedDirectoryPath.FromUnicode("c:/dist")
                };
                var hostAcross = new Host("c:/src");
                Check.Equal(Paths.GetOutputJSFileNameWorker(input, optionsAcross, hostAcross), RootedFilePath.FromUnicode("d:/shared.js"), name + " js");
                Check.Equal(Paths.GetOutputDeclarationFileNameWorker(input, optionsAcross, hostAcross), RootedFilePath.FromUnicode("d:/shared.d.ts"), name + " declaration");
                break;
            case "TestOutputFileNamesPreserveEmptyAndDotStems":
                var host = new Host("/project/src", [".css"]);
                var options = new CompilerOptions
                {
                    OutDir = RootedDirectoryPath.FromUnicode("/project/out")
                };
                foreach (var row in new (string Input, string Js, string Declaration)[]
                {
                    (".ts", ".js", ".d.ts"),
                    ("..ts", "..js", "..d.ts"),
                    ("...ts", "...js", "...d.ts"),
                    (".mts", ".mjs", ".d.mts"),
                    (".d.ts", ".js", ".d.ts"),
                    (".css", ".css", ".d.css.ts")
                }

                )
                {
                    var file = RootedFilePath.FromUnicode("/project/src/" + row.Input);
                    Check.Equal(Paths.GetOutputJSFileNameWorker(file, options, host), RootedFilePath.FromUnicode("/project/out/" + row.Js), row.Input + " js");
                    Check.Equal(Paths.GetOutputDeclarationFileNameWorker(file, options, host), RootedFilePath.FromUnicode("/project/out/" + row.Declaration), row.Input + " declaration");
                }

                break;
            default:
                throw new ArgumentException("Unknown original outputpaths test: " + name, nameof(name));
        }
    }
}

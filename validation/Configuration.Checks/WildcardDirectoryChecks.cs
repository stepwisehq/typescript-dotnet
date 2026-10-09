// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete assertion-preserving port of pinned internal/tsoptions/wildcarddirectories_test.go.
using TypeScript.Scanner;
using Options = TypeScript.TsOptions.TsOptions;

namespace Configuration.Checks;
public static class WildcardDirectoryChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestGetWildcardDirectories_DotPrefixedIncludeWithDotDirExclude", "TestGetWildcardDirectories_DriveRoot", "TestGetWildcardDirectories_NonASCIICharacters"];

    private static List<PathPattern> Patterns(params string[] values) => values.Select(PathPattern.FromUnicode).ToList();
    public static void Run(string name)
    {
        switch (name)
        {
            case "TestGetWildcardDirectories_DotPrefixedIncludeWithDotDirExclude":
                var dot = Options.getWildcardDirectories(Patterns("./app/**/*.ts", "./app/**/*.tsx"), Patterns("**/node_modules", "**/.*/", "./build"), RootedDirectoryPath.FromUnicode("/home/projects/monorepo/apps/web"), CaseSensitivity.CaseSensitive);
                Check.True(dot is not null, name + " nonnil");
                Check.Equal(dot!.Count, 1, name + " full map count");
                Check.True(dot.TryGetValue(RootedDirectoryPath.FromUnicode("/home/projects/monorepo/apps/web/app"), out bool recurse) && recurse, name + " full map association");
                break;
            case "TestGetWildcardDirectories_DriveRoot":
                var drive = Options.getWildcardDirectories(Patterns("*.ts"), null, RootedDirectoryPath.FromUnicode("c:/"), CaseSensitivity.CaseInsensitive);
                Check.True(drive is not null, name + " nonnil");
                Check.Equal(drive!.Count, 1, name + " full map count");
                Check.True(drive.TryGetValue(RootedDirectoryPath.FromUnicode("c:/"), out bool recursive) && !recursive, name + " full map association");
                break;
            case "TestGetWildcardDirectories_NonASCIICharacters":
                foreach (var row in new (string Name, string[] Include, string[] Exclude, string Directory, CaseSensitivity Case)[]
                {
                    ("Norwegian character æ in path", ["src/**/*.test.ts", "src/**/*.stories.ts", "src/**/*.mdx"], ["node_modules"], "C:/Users/TobiasLægreid/dev/app/frontend/packages/react", CaseSensitivity.CaseInsensitive),
                    ("Japanese characters in path", ["src/**/*.ts"], ["テスト"], "/Users/ユーザー/プロジェクト", CaseSensitivity.CaseSensitive),
                    ("Chinese characters in path", ["源代码/**/*.js"], ["节点模块"], "/home/用户/项目", CaseSensitivity.CaseSensitive),
                    ("Various Unicode characters", ["src/**/*.ts"], ["node_modules"], "/Users/Müller/café/naïve/résumé", CaseSensitivity.CaseInsensitive)
                }

                )
                    Check.True(Options.getWildcardDirectories(Patterns(row.Include), Patterns(row.Exclude), RootedDirectoryPath.FromUnicode(row.Directory), row.Case)is not null, row.Name + " expected nonnil result");
                break;
            default:
                throw new ArgumentException("Unknown original wildcard directories test: " + name, nameof(name));
        }
    }
}

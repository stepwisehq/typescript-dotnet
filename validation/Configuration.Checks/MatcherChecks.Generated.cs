// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Generated literal inputs AND original expectation predicates; no execution oracle.
// tools/configuration-generate-matcher-tests.py guards source/table bytes.
#nullable enable
using TypeScript.Scanner;
using TypeScript.Vfs;
using TypeScript.Vfs.VfsMatch;
using M = TypeScript.Vfs.VfsMatch.VfsMatch;
namespace Configuration.Checks;
public static partial class MatcherChecks
{
    private static ConfigurationFileSystem caseInsensitiveHost() => new ConfigurationFileSystem([new("/dev/a.ts"), new("/dev/a.d.ts"), new("/dev/a.js"), new("/dev/b.ts"), new("/dev/b.js"), new("/dev/c.d.ts"), new("/dev/z/a.ts"), new("/dev/z/abz.ts"), new("/dev/z/aba.ts"), new("/dev/z/b.ts"), new("/dev/z/bbz.ts"), new("/dev/z/bba.ts"), new("/dev/x/a.ts"), new("/dev/x/aa.ts"), new("/dev/x/b.ts"), new("/dev/x/y/a.ts"), new("/dev/x/y/b.ts"), new("/dev/js/a.js"), new("/dev/js/b.js"), new("/dev/js/d.min.js"), new("/dev/js/ab.min.js"), new("/ext/ext.ts"), new("/ext/b/a..b.ts")], CaseSensitivity.CaseInsensitive);

    private static ConfigurationFileSystem caseSensitiveHost() => new ConfigurationFileSystem([new("/dev/a.ts"), new("/dev/a.d.ts"), new("/dev/a.js"), new("/dev/b.ts"), new("/dev/b.js"), new("/dev/A.ts"), new("/dev/B.ts"), new("/dev/c.d.ts"), new("/dev/z/a.ts"), new("/dev/z/abz.ts"), new("/dev/z/aba.ts"), new("/dev/z/b.ts"), new("/dev/z/bbz.ts"), new("/dev/z/bba.ts"), new("/dev/x/a.ts"), new("/dev/x/b.ts"), new("/dev/x/y/a.ts"), new("/dev/x/y/b.ts"), new("/dev/q/a/c/b/d.ts"), new("/dev/js/a.js"), new("/dev/js/b.js"), new("/dev/js/d.MIN.js")], CaseSensitivity.CaseSensitive);

    private static ConfigurationFileSystem commonFoldersHost() => new ConfigurationFileSystem([new("/dev/a.ts"), new("/dev/a.d.ts"), new("/dev/a.js"), new("/dev/b.ts"), new("/dev/x/a.ts"), new("/dev/node_modules/a.ts"), new("/dev/bower_components/a.ts"), new("/dev/jspm_packages/a.ts")], CaseSensitivity.CaseInsensitive);

    private static ConfigurationFileSystem dottedFoldersHost() => new ConfigurationFileSystem([new("/dev/x/d.ts"), new("/dev/x/y/d.ts"), new("/dev/x/y/.e.ts"), new("/dev/x/.y/a.ts"), new("/dev/.z/.b.ts"), new("/dev/.z/c.ts"), new("/dev/w/.u/e.ts"), new("/dev/g.min.js/.g/g.ts")], CaseSensitivity.CaseInsensitive);

    private static ConfigurationFileSystem mixedExtensionHost() => new ConfigurationFileSystem([new("/dev/a.ts"), new("/dev/a.d.ts"), new("/dev/a.js"), new("/dev/b.tsx"), new("/dev/b.d.ts"), new("/dev/b.jsx"), new("/dev/c.tsx"), new("/dev/c.js"), new("/dev/d.js"), new("/dev/e.jsx"), new("/dev/f.other")], CaseSensitivity.CaseInsensitive);

    private static ConfigurationFileSystem sameNamedDeclarationsHost() => new ConfigurationFileSystem([new("/dev/a.tsx"), new("/dev/a.d.ts"), new("/dev/b.tsx"), new("/dev/b.ts"), new("/dev/c.tsx"), new("/dev/m.ts"), new("/dev/m.d.ts"), new("/dev/n.tsx"), new("/dev/n.ts"), new("/dev/n.d.ts"), new("/dev/o.ts"), new("/dev/x.d.ts")], CaseSensitivity.CaseInsensitive);

    private static void TestReadDirectory()
    {
        {
            const string context = "TestReadDirectory: defaults include common package folders";
            IFS host = commonFoldersHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, null, M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/dev/b.ts"), context);
                Check.True(Contains(got, "/dev/x/a.ts"), context);
                Check.True(Contains(got, "/dev/node_modules/a.ts"), context);
                Check.True(Contains(got, "/dev/bower_components/a.ts"), context);
                Check.True(Contains(got, "/dev/jspm_packages/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: literal includes without exclusions";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["a.ts", "b.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/a.ts", "/dev/b.ts"], context);
        }
        {
            const string context = "TestReadDirectory: literal includes with non ts extensions excluded";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["a.js", "b.js"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Equal((got?.Count ?? 0), 0, context);
        }
        {
            const string context = "TestReadDirectory: literal includes missing files excluded";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["z.ts", "x.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Equal((got?.Count ?? 0), 0, context);
        }
        {
            const string context = "TestReadDirectory: literal includes with literal excludes";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["b.ts"], ["a.ts", "b.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/a.ts"], context);
        }
        {
            const string context = "TestReadDirectory: literal includes with wildcard excludes";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["*.ts", "z/??z.ts", "*/b.ts"], ["a.ts", "b.ts", "z/a.ts", "z/abz.ts", "z/aba.ts", "x/b.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/z/a.ts", "/dev/z/aba.ts"], context);
        }
        {
            const string context = "TestReadDirectory: literal includes with recursive excludes";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["**/b.ts"], ["a.ts", "b.ts", "x/a.ts", "x/b.ts", "x/y/a.ts", "x/y/b.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/a.ts", "/dev/x/a.ts", "/dev/x/y/a.ts"], context);
        }
        {
            const string context = "TestReadDirectory: case sensitive exclude is respected";
            IFS host = caseSensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["**/b.ts"], ["B.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/B.ts"], context);
        }
        {
            const string context = "TestReadDirectory: explicit includes keep common package folders";
            IFS host = commonFoldersHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["a.ts", "b.ts", "node_modules/a.ts", "bower_components/a.ts", "jspm_packages/a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/dev/b.ts"), context);
                Check.True(Contains(got, "/dev/node_modules/a.ts"), context);
                Check.True(Contains(got, "/dev/bower_components/a.ts"), context);
                Check.True(Contains(got, "/dev/jspm_packages/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: wildcard include sorted order";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["z/*.ts", "x/*.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                string[] expected = ["/dev/z/a.ts", "/dev/z/aba.ts", "/dev/z/abz.ts", "/dev/z/b.ts", "/dev/z/bba.ts", "/dev/z/bbz.ts", "/dev/x/a.ts", "/dev/x/aa.ts", "/dev/x/b.ts"];
                Check.Sequence<string>(got, expected, context);
        }
        {
            const string context = "TestReadDirectory: wildcard include same named declarations excluded";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["*.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/dev/b.ts"), context);
                Check.True(Contains(got, "/dev/a.d.ts"), context);
                Check.True(Contains(got, "/dev/c.d.ts"), context);
        }
        {
            const string context = "TestReadDirectory: wildcard star matches only ts files";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                foreach (string f in got ?? []) {
                Check.True(f.Contains(".ts", StringComparison.Ordinal) || f.Contains(".tsx", StringComparison.Ordinal) || f.Contains(".d.ts", StringComparison.Ordinal), context);
                }
                Check.True(!Contains(got, "/dev/a.js"), context);
                Check.True(!Contains(got, "/dev/b.js"), context);
        }
        {
            const string context = "TestReadDirectory: wildcard question mark single character";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["x/?.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/x/a.ts", "/dev/x/b.ts"], context);
        }
        {
            const string context = "TestReadDirectory: wildcard recursive directory";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/dev/z/a.ts"), context);
                Check.True(Contains(got, "/dev/x/a.ts"), context);
                Check.True(Contains(got, "/dev/x/y/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: double asterisk matches zero-or-more directories";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["x/**/a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Equal((got?.Count ?? 0), 2, context);
                Check.True(Contains(got, "/dev/x/a.ts"), context);
                Check.True(Contains(got, "/dev/x/y/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: wildcard multiple recursive directories";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["x/y/**/a.ts", "x/**/a.ts", "z/**/a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True((got?.Count ?? 0) > 0, context);
        }
        {
            const string context = "TestReadDirectory: wildcard case sensitive matching";
            IFS host = caseSensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/A.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/A.ts"], context);
        }
        {
            const string context = "TestReadDirectory: wildcard missing files excluded";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["*/z.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Equal((got?.Count ?? 0), 0, context);
        }
        {
            const string context = "TestReadDirectory: exclude folders with wildcards";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["z", "x"], ["**/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                foreach (string f in got ?? []) {
                Check.True(!f.Contains("/z/", StringComparison.Ordinal) && !f.Contains("/x/", StringComparison.Ordinal), context);
                }
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/dev/b.ts"), context);
        }
        {
            const string context = "TestReadDirectory: include paths outside project absolute";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["*", "/ext/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/ext/ext.ts"), context);
        }
        {
            const string context = "TestReadDirectory: include paths outside project relative";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["**"], ["*", "../ext/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/ext/ext.ts"), context);
        }
        {
            const string context = "TestReadDirectory: include files containing double dots";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["**"], ["/ext/b/a..b.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/ext/b/a..b.ts"), context);
        }
        {
            const string context = "TestReadDirectory: exclude files containing double dots";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["/ext/b/a..b.ts"], ["/ext/**/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/ext/ext.ts"), context);
                Check.True(!Contains(got, "/ext/b/a..b.ts"), context);
        }
        {
            const string context = "TestReadDirectory: common package folders implicitly excluded";
            IFS host = commonFoldersHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/dev/x/a.ts"), context);
                Check.True(!Contains(got, "/dev/node_modules/a.ts"), context);
                Check.True(!Contains(got, "/dev/bower_components/a.ts"), context);
                Check.True(!Contains(got, "/dev/jspm_packages/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: common package folders explicit recursive include";
            IFS host = commonFoldersHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/a.ts", "**/node_modules/a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/dev/node_modules/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: common package folders wildcard include";
            IFS host = commonFoldersHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["*/a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/x/a.ts"), context);
                Check.True(!Contains(got, "/dev/node_modules/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: common package folders explicit wildcard include";
            IFS host = commonFoldersHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["*/a.ts", "node_modules/a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/x/a.ts"), context);
                Check.True(Contains(got, "/dev/node_modules/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: dotted folders not implicitly included";
            IFS host = dottedFoldersHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["x/**/*", "w/*/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/x/d.ts"), context);
                Check.True(Contains(got, "/dev/x/y/d.ts"), context);
                Check.True(!Contains(got, "/dev/x/.y/a.ts"), context);
                Check.True(!Contains(got, "/dev/x/y/.e.ts"), context);
                Check.True(!Contains(got, "/dev/w/.u/e.ts"), context);
        }
        {
            const string context = "TestReadDirectory: dotted folders explicitly included";
            IFS host = dottedFoldersHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["x/.y/a.ts", "/dev/.z/.b.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/x/.y/a.ts"), context);
                Check.True(Contains(got, "/dev/.z/.b.ts"), context);
        }
        {
            const string context = "TestReadDirectory: dotted folders recursive wildcard matches directories";
            IFS host = dottedFoldersHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/.*/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/x/.y/a.ts"), context);
                Check.True(Contains(got, "/dev/.z/c.ts"), context);
                Check.True(Contains(got, "/dev/w/.u/e.ts"), context);
        }
        {
            const string context = "TestReadDirectory: trailing recursive include returns empty";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Equal((got?.Count ?? 0), 0, context);
        }
        {
            const string context = "TestReadDirectory: trailing recursive exclude removes everything";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["**"], ["**/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Equal((got?.Count ?? 0), 0, context);
        }
        {
            const string context = "TestReadDirectory: multiple recursive directory patterns in includes";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/x/**/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/x/a.ts"), context);
                Check.True(Contains(got, "/dev/x/y/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: multiple recursive directory patterns in excludes";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["**/x/**"], ["**/a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/dev/z/a.ts"), context);
                Check.True(!Contains(got, "/dev/x/a.ts"), context);
                Check.True(!Contains(got, "/dev/x/y/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: implicit globbification expands directory";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["z"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/z/a.ts"), context);
                Check.True(Contains(got, "/dev/z/aba.ts"), context);
                Check.True(Contains(got, "/dev/z/b.ts"), context);
        }
        {
            const string context = "TestReadDirectory: exclude patterns starting with starstar";
            IFS host = caseSensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["**/x"], null, M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                foreach (string f in got ?? []) {
                Check.True(!f.Contains("/x/", StringComparison.Ordinal), context);
                }
        }
        {
            const string context = "TestReadDirectory: include patterns starting with starstar";
            IFS host = caseSensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/x", "**/a/**/b"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/x/a.ts"), context);
                Check.True(Contains(got, "/dev/q/a/c/b/d.ts"), context);
        }
        {
            const string context = "TestReadDirectory: depth limit one";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, null, 1))?.Select(value => value.AsString()).ToList();
                foreach (string f in got ?? []) {
                string suffix = f["/dev/".Length..];
                Check.True(!suffix.Contains("/", StringComparison.Ordinal), context);
                }
        }
        {
            const string context = "TestReadDirectory: depth limit two";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, null, 2))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/dev/z/a.ts"), context);
                Check.True(!Contains(got, "/dev/x/y/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: mixed extensions only ts";
            IFS host = mixedExtensionHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts"], (List<string>?)null, null, M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                foreach (string f in got ?? []) {
                Check.True(f.EndsWith(".ts", StringComparison.Ordinal), context);
                }
        }
        {
            const string context = "TestReadDirectory: mixed extensions ts and tsx";
            IFS host = mixedExtensionHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx"], (List<string>?)null, null, M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                foreach (string f in got ?? []) {
                Check.True(f.EndsWith(".ts", StringComparison.Ordinal) || f.EndsWith(".tsx", StringComparison.Ordinal), context);
                }
        }
        {
            const string context = "TestReadDirectory: mixed extensions js and jsx";
            IFS host = mixedExtensionHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".js", ".jsx"], (List<string>?)null, null, M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                foreach (string f in got ?? []) {
                Check.True(f.EndsWith(".js", StringComparison.Ordinal) || f.EndsWith(".jsx", StringComparison.Ordinal), context);
                }
        }
        {
            const string context = "TestReadDirectory: min js files excluded by wildcard";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".js"], (List<string>?)null, ["js/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/js/a.js"), context);
                Check.True(Contains(got, "/dev/js/b.js"), context);
                Check.True(!Contains(got, "/dev/js/d.min.js"), context);
                Check.True(!Contains(got, "/dev/js/ab.min.js"), context);
        }
        {
            const string context = "TestReadDirectory: min js exclusion is case-sensitive on case-sensitive FS";
            IFS host = caseSensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".js"], (List<string>?)null, ["js/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/js/a.js"), context);
                Check.True(Contains(got, "/dev/js/b.js"), context);
                Check.True(Contains(got, "/dev/js/d.MIN.js"), context);
        }
        {
            const string context = "TestReadDirectory: min js files explicitly included";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".js"], (List<string>?)null, ["js/*.min.js"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/js/d.min.js"), context);
                Check.True(Contains(got, "/dev/js/ab.min.js"), context);
        }
        {
            const string context = "TestReadDirectory: min js files included when pattern mentions .min.";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".js"], (List<string>?)null, ["js/*.min.*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Equal((got?.Count ?? 0), 2, context);
                Check.True(Contains(got, "/dev/js/d.min.js"), context);
                Check.True(Contains(got, "/dev/js/ab.min.js"), context);
        }
        {
            const string context = "TestReadDirectory: exclude literal node_modules folder";
            IFS host = commonFoldersHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["node_modules"], ["**/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(!Contains(got, "/dev/node_modules/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: same named declarations include ts";
            IFS host = sameNamedDeclarationsHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["*.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True((got?.Count ?? 0) > 0, context);
        }
        {
            const string context = "TestReadDirectory: same named declarations include tsx";
            IFS host = sameNamedDeclarationsHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["*.tsx"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                foreach (string f in got ?? []) {
                Check.True(f.EndsWith(".tsx", StringComparison.Ordinal), context);
                }
        }
        {
            const string context = "TestReadDirectory: empty includes returns all matching files";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, null, M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True((got?.Count ?? 0) > 0, context);
                Check.True(Contains(got, "/dev/a.ts"), context);
        }
        {
            const string context = "TestReadDirectory: nil extensions returns all files";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, null, (List<string>?)null, null, M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/dev/a.js"), context);
        }
        {
            const string context = "TestReadDirectory: empty extensions slice returns all files";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [], (List<string>?)null, null, M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True((got?.Count ?? 0) > 0, context);
        }
    }

    private static void TestIsImplicitGlob()
    {
        {
            const string context = "TestIsImplicitGlob: simple";
            Check.Equal(M.IsImplicitGlob("foo"), true, context);
        }
        {
            const string context = "TestIsImplicitGlob: folder";
            Check.Equal(M.IsImplicitGlob("src"), true, context);
        }
        {
            const string context = "TestIsImplicitGlob: with extension";
            Check.Equal(M.IsImplicitGlob("foo.ts"), false, context);
        }
        {
            const string context = "TestIsImplicitGlob: trailing dot";
            Check.Equal(M.IsImplicitGlob("foo."), false, context);
        }
        {
            const string context = "TestIsImplicitGlob: star";
            Check.Equal(M.IsImplicitGlob("*"), false, context);
        }
        {
            const string context = "TestIsImplicitGlob: question";
            Check.Equal(M.IsImplicitGlob("?"), false, context);
        }
        {
            const string context = "TestIsImplicitGlob: star suffix";
            Check.Equal(M.IsImplicitGlob("foo*"), false, context);
        }
        {
            const string context = "TestIsImplicitGlob: question suffix";
            Check.Equal(M.IsImplicitGlob("foo?"), false, context);
        }
        {
            const string context = "TestIsImplicitGlob: dot name";
            Check.Equal(M.IsImplicitGlob("foo.bar"), false, context);
        }
        {
            const string context = "TestIsImplicitGlob: empty";
            Check.Equal(M.IsImplicitGlob(""), true, context);
        }
    }

    private static void TestReadDirectoryEdgeCases()
    {
        {
            const string context = "TestReadDirectoryEdgeCases: rooted include path";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts"], (List<string>?)null, ["/dev/a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
        }
        {
            const string context = "TestReadDirectoryEdgeCases: include with extension in path";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts"], (List<string>?)null, ["a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
        }
        {
            const string context = "TestReadDirectoryEdgeCases: special regex characters in path";
            IFS host = new ConfigurationFileSystem([new("/dev/file+test.ts"), new("/dev/file[0].ts"), new("/dev/file(1).ts"), new("/dev/file$money.ts"), new("/dev/file^start.ts"), new("/dev/file|pipe.ts"), new("/dev/file#hash.ts")], CaseSensitivity.CaseInsensitive);
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts"], (List<string>?)null, ["file+test.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/file+test.ts"), context);
        }
        {
            const string context = "TestReadDirectoryEdgeCases: include pattern starting with question mark";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts"], (List<string>?)null, ["?.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/dev/b.ts"), context);
        }
        {
            const string context = "TestReadDirectoryEdgeCases: include pattern starting with star";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts"], (List<string>?)null, ["*b.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/b.ts"), context);
        }
        {
            const string context = "TestReadDirectoryEdgeCases: case insensitive file matching";
            IFS host = new ConfigurationFileSystem([new("/dev/File.ts"), new("/dev/FILE.ts")], CaseSensitivity.CaseSensitive);
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts"], (List<string>?)null, ["*.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True((got?.Count ?? 0) == 2, context);
        }
        {
            const string context = "TestReadDirectoryEdgeCases: nested subdirectory base path";
            IFS host = caseSensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts"], (List<string>?)null, ["q/a/c/b/d.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/q/a/c/b/d.ts"), context);
        }
        {
            const string context = "TestReadDirectoryEdgeCases: current directory differs from path";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts"], (List<string>?)null, ["z/*.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True((got?.Count ?? 0) > 0, context);
        }
    }

    private static void TestReadDirectoryEmptyIncludes()
    {
        {
            const string context = "TestReadDirectoryEmptyIncludes: empty includes slice behavior";
            IFS host = new ConfigurationFileSystem([new("/root/a.ts")], CaseSensitivity.CaseSensitive);
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/root", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts"], (List<string>?)null, [], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                if ((got?.Count ?? 0) == 0) {
                return;
                }
                Check.True(Contains(got, "/root/a.ts"), context);
        }
    }

    private static void TestReadDirectorySymlinkCycle()
    {
        {
            const string context = "TestReadDirectorySymlinkCycle: detects and skips symlink cycles";
            IFS host = new ConfigurationFileSystem([new("/root/file.ts"), new("/root/a/file.ts"), new("/root/a/b", Symlink: "/root/a")], CaseSensitivity.CaseSensitive);
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/root", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts"], (List<string>?)null, ["**/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                string[] expected = ["/root/file.ts", "/root/a/file.ts"];
                Check.Sequence<string>(got, expected, context);
        }
    }

    private static void TestReadDirectoryMatchesTypeScriptBaselines()
    {
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: sorted in include order then alphabetical";
            IFS host = new ConfigurationFileSystem([new("/dev/z/a.ts"), new("/dev/z/aba.ts"), new("/dev/z/abz.ts"), new("/dev/z/b.ts"), new("/dev/z/bba.ts"), new("/dev/z/bbz.ts"), new("/dev/x/a.ts"), new("/dev/x/aa.ts"), new("/dev/x/b.ts")], CaseSensitivity.CaseInsensitive);
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["z/*.ts", "x/*.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                string[] expected = ["/dev/z/a.ts", "/dev/z/aba.ts", "/dev/z/abz.ts", "/dev/z/b.ts", "/dev/z/bba.ts", "/dev/z/bbz.ts", "/dev/x/a.ts", "/dev/x/aa.ts", "/dev/x/b.ts"];
                Check.Sequence<string>(got, expected, context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: recursive wildcards match dotted directories";
            IFS host = new ConfigurationFileSystem([new("/dev/x/d.ts"), new("/dev/x/y/d.ts"), new("/dev/x/y/.e.ts"), new("/dev/x/.y/a.ts"), new("/dev/.z/.b.ts"), new("/dev/.z/c.ts"), new("/dev/w/.u/e.ts"), new("/dev/g.min.js/.g/g.ts")], CaseSensitivity.CaseInsensitive);
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/.*/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                string[] expected = ["/dev/.z/c.ts", "/dev/g.min.js/.g/g.ts", "/dev/w/.u/e.ts", "/dev/x/.y/a.ts"];
                Check.Equal((got?.Count ?? 0), expected.Length, context);
                foreach (string want in expected ?? []) {
                Check.True(Contains(got, want), context);
                }
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: common package folders implicitly excluded with wildcard";
            IFS host = new ConfigurationFileSystem([new("/dev/a.ts"), new("/dev/a.d.ts"), new("/dev/a.js"), new("/dev/b.ts"), new("/dev/x/a.ts"), new("/dev/node_modules/a.ts"), new("/dev/bower_components/a.ts"), new("/dev/jspm_packages/a.ts")], CaseSensitivity.CaseInsensitive);
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/a.ts", "/dev/x/a.ts"], context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: js wildcard excludes min js files";
            IFS host = new ConfigurationFileSystem([new("/dev/js/a.js"), new("/dev/js/b.js"), new("/dev/js/d.min.js"), new("/dev/js/ab.min.js")], CaseSensitivity.CaseInsensitive);
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".js"], (List<string>?)null, ["js/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/js/a.js", "/dev/js/b.js"], context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: explicit min js pattern includes min files";
            IFS host = new ConfigurationFileSystem([new("/dev/js/a.js"), new("/dev/js/b.js"), new("/dev/js/d.min.js"), new("/dev/js/ab.min.js")], CaseSensitivity.CaseInsensitive);
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".js"], (List<string>?)null, ["js/*.min.js"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                string[] expected = ["/dev/js/ab.min.js", "/dev/js/d.min.js"];
                Check.Equal((got?.Count ?? 0), expected.Length, context);
                foreach (string want in expected ?? []) {
                Check.True(Contains(got, want), context);
                }
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: literal excludes baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["b.ts"], ["a.ts", "b.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/a.ts"], context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: wildcard excludes baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["*.ts", "z/??z.ts", "*/b.ts"], ["a.ts", "b.ts", "z/a.ts", "z/abz.ts", "z/aba.ts", "x/b.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/z/a.ts", "/dev/z/aba.ts"], context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: recursive excludes baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["**/b.ts"], ["a.ts", "b.ts", "x/a.ts", "x/b.ts", "x/y/a.ts", "x/y/b.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/a.ts", "/dev/x/a.ts", "/dev/x/y/a.ts"], context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: question mark baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["x/?.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/x/a.ts", "/dev/x/b.ts"], context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: recursive directory pattern baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/a.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/a.ts", "/dev/x/a.ts", "/dev/x/y/a.ts", "/dev/z/a.ts"], context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: case sensitive baseline";
            IFS host = caseSensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/A.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/A.ts"], context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: exclude folders baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["z", "x"], ["**/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                foreach (string f in got ?? []) {
                Check.True(!f.Contains("/z/", StringComparison.Ordinal) && !f.Contains("/x/", StringComparison.Ordinal), context);
                }
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/dev/b.ts"), context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: implicit glob expansion baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["z"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Sequence<string>(got, ["/dev/z/a.ts", "/dev/z/aba.ts", "/dev/z/abz.ts", "/dev/z/b.ts", "/dev/z/bba.ts", "/dev/z/bbz.ts"], context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: trailing recursive directory baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Equal((got?.Count ?? 0), 0, context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: exclude trailing recursive directory baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["**"], ["**/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.Equal((got?.Count ?? 0), 0, context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: multiple recursive directory patterns baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/x/**/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/x/a.ts"), context);
                Check.True(Contains(got, "/dev/x/aa.ts"), context);
                Check.True(Contains(got, "/dev/x/b.ts"), context);
                Check.True(Contains(got, "/dev/x/y/a.ts"), context);
                Check.True(Contains(got, "/dev/x/y/b.ts"), context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: include dirs with starstar prefix baseline";
            IFS host = caseSensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["**/x", "**/a/**/b"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/x/a.ts"), context);
                Check.True(Contains(got, "/dev/x/b.ts"), context);
                Check.True(Contains(got, "/dev/q/a/c/b/d.ts"), context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: dotted folders not implicitly included baseline";
            IFS host = dottedFoldersHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["x/**/*", "w/*/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/x/d.ts"), context);
                Check.True(Contains(got, "/dev/x/y/d.ts"), context);
                Check.True(!Contains(got, "/dev/x/.y/a.ts"), context);
                Check.True(!Contains(got, "/dev/x/y/.e.ts"), context);
                Check.True(!Contains(got, "/dev/w/.u/e.ts"), context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: include paths outside project baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)null, ["*", "/ext/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/dev/a.ts"), context);
                Check.True(Contains(got, "/ext/ext.ts"), context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: include files with double dots baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["**"], ["/ext/b/a..b.ts"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/ext/b/a..b.ts"), context);
        }
        {
            const string context = "TestReadDirectoryMatchesTypeScriptBaselines: exclude files with double dots baseline";
            IFS host = caseInsensitiveHost();
            RootedDirectoryPath path = TsPath.ToRootedDirectoryPath("/dev", TsPath.RootedDirectoryPathFromAbsolute("/"));
            List<string>? got = GoSlices.ToListOrNull(M.ReadDirectory(host, path, [".ts", ".tsx", ".d.ts"], (List<string>?)["/ext/b/a..b.ts"], ["/ext/**/*"], M.UnlimitedDepth))?.Select(value => value.AsString()).ToList();
                Check.True(Contains(got, "/ext/ext.ts"), context);
                Check.True(!Contains(got, "/ext/b/a..b.ts"), context);
        }
    }

    private static void TestSpecMatcher()
    {
        {
            const string context = "TestSpecMatcher: simple wildcard";
            var matcher = M.NewSpecMatcher((List<string>?)["*.ts"], RootedDirectoryPath.FromGoString("/project"), Usage.Files, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            foreach (string path in (string[])["/project/a.ts", "/project/b.ts", "/project/foo.ts"]) Check.Equal(matcher!.MatchString(path), true, context + path);
            foreach (string path in (string[])["/project/a.js", "/project/sub/a.ts"]) Check.Equal(matcher!.MatchString(path), false, context + path);
        }
        {
            const string context = "TestSpecMatcher: recursive wildcard";
            var matcher = M.NewSpecMatcher((List<string>?)["**/*.ts"], RootedDirectoryPath.FromGoString("/project"), Usage.Files, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            foreach (string path in (string[])["/project/a.ts", "/project/sub/a.ts", "/project/sub/deep/a.ts"]) Check.Equal(matcher!.MatchString(path), true, context + path);
            foreach (string path in (string[])["/project/a.js"]) Check.Equal(matcher!.MatchString(path), false, context + path);
        }
        {
            const string context = "TestSpecMatcher: exclude pattern";
            var matcher = M.NewSpecMatcher((List<string>?)["node_modules"], RootedDirectoryPath.FromGoString("/project"), Usage.Exclude, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            foreach (string path in (string[])["/project/node_modules/foo"]) Check.Equal(matcher!.MatchString(path), true, context + path);
            foreach (string path in (string[])["/project/node_modules", "/project/src"]) Check.Equal(matcher!.MatchString(path), false, context + path);
        }
        {
            const string context = "TestSpecMatcher: case insensitive";
            var matcher = M.NewSpecMatcher((List<string>?)["*.ts"], RootedDirectoryPath.FromGoString("/project"), Usage.Files, CaseSensitivity.CaseInsensitive);
            Check.True(matcher is not null, context + " should not be nil");
            foreach (string path in (string[])["/project/A.TS", "/project/B.Ts"]) Check.Equal(matcher!.MatchString(path), true, context + path);
            foreach (string path in (string[])["/project/a.js"]) Check.Equal(matcher!.MatchString(path), false, context + path);
        }
        {
            const string context = "TestSpecMatcher: multiple specs";
            var matcher = M.NewSpecMatcher((List<string>?)["*.ts", "*.tsx"], RootedDirectoryPath.FromGoString("/project"), Usage.Files, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            foreach (string path in (string[])["/project/a.ts", "/project/b.tsx"]) Check.Equal(matcher!.MatchString(path), true, context + path);
            foreach (string path in (string[])["/project/a.js"]) Check.Equal(matcher!.MatchString(path), false, context + path);
        }
    }

    private static void TestSpecMatcher_MatchString()
    {
        {
            const string context = "TestSpecMatcher_MatchString: simple wildcard files";
            var matcher = M.NewSpecMatcher((List<string>?)["*.ts"], RootedDirectoryPath.FromGoString("/project"), Usage.Files, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            string[] paths = ["/project/a.ts", "/project/sub/a.ts", "/project/a.js"];
            bool[] expected = [true, false, false];
            Check.Equal(paths.Length, expected.Length, context);
            for (int i = 0; i < paths.Length; i++) Check.Equal(matcher!.MatchString(paths[i]), expected[i], context + paths[i]);
        }
        {
            const string context = "TestSpecMatcher_MatchString: recursive wildcard files";
            var matcher = M.NewSpecMatcher((List<string>?)["**/*.ts"], RootedDirectoryPath.FromGoString("/project"), Usage.Files, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            string[] paths = ["/project/a.ts", "/project/sub/a.ts", "/project/a.js"];
            bool[] expected = [true, true, false];
            Check.Equal(paths.Length, expected.Length, context);
            for (int i = 0; i < paths.Length; i++) Check.Equal(matcher!.MatchString(paths[i]), expected[i], context + paths[i]);
        }
        {
            const string context = "TestSpecMatcher_MatchString: exclude pattern matches prefix";
            var matcher = M.NewSpecMatcher((List<string>?)["node_modules"], RootedDirectoryPath.FromGoString("/project"), Usage.Exclude, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            string[] paths = ["/project/node_modules", "/project/node_modules/foo", "/project/src"];
            bool[] expected = [false, true, false];
            Check.Equal(paths.Length, expected.Length, context);
            for (int i = 0; i < paths.Length; i++) Check.Equal(matcher!.MatchString(paths[i]), expected[i], context + paths[i]);
        }
    }

    private static void TestSingleSpecMatcher_MatchString()
    {
        {
            const string context = "TestSingleSpecMatcher_MatchString: single spec wildcard";
            var matcher = M.NewSpecMatcher((List<string>?)["*.ts"], RootedDirectoryPath.FromGoString("/project"), Usage.Files, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            string[] paths = ["/project/a.ts", "/project/sub/a.ts", "/project/a.js"];
            bool[] expected = [true, false, false];
            Check.Equal(paths.Length, expected.Length, context);
            for (int i = 0; i < paths.Length; i++) Check.Equal(matcher!.MatchString(paths[i]), expected[i], context + paths[i]);
        }
        {
            const string context = "TestSingleSpecMatcher_MatchString: single spec trailing starstar exclude allowed";
            var matcher = M.NewSpecMatcher((List<string>?)["**"], RootedDirectoryPath.FromGoString("/project"), Usage.Exclude, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            string[] paths = ["/project/a.ts", "/project/sub/a.ts"];
            bool[] expected = [true, true];
            Check.Equal(paths.Length, expected.Length, context);
            for (int i = 0; i < paths.Length; i++) Check.Equal(matcher!.MatchString(paths[i]), expected[i], context + paths[i]);
        }
    }

    private static void TestSpecMatchers_MatchIndex()
    {
        {
            const string context = "TestSpecMatchers_MatchIndex: index lookup prefers first match";
            var matcher = M.NewSpecMatcher((List<string>?)["*.ts", "*.tsx"], RootedDirectoryPath.FromGoString("/project"), Usage.Files, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            string[] paths = ["/project/a.ts", "/project/a.tsx", "/project/a.js"];
            int[] expected = [0, 1, -1];
            Check.Equal(paths.Length, expected.Length, context);
            for (int i = 0; i < paths.Length; i++) Check.Equal(matcher!.MatchIndex(paths[i]), expected[i], context + paths[i]);
        }
        {
            const string context = "TestSpecMatchers_MatchIndex: exclude index lookup";
            var matcher = M.NewSpecMatcher((List<string>?)["node_modules", "bower_components"], RootedDirectoryPath.FromGoString("/project"), Usage.Exclude, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            string[] paths = ["/project/node_modules", "/project/node_modules/foo", "/project/bower_components", "/project/bower_components/bar", "/project/src"];
            int[] expected = [-1, 0, -1, 1, -1];
            Check.Equal(paths.Length, expected.Length, context);
            for (int i = 0; i < paths.Length; i++) Check.Equal(matcher!.MatchIndex(paths[i]), expected[i], context + paths[i]);
        }
    }

    private static void TestSingleSpecMatcher()
    {
        {
            const string context = "TestSingleSpecMatcher: simple spec";
            var matcher = M.NewSpecMatcher((List<string>?)["*.ts"], RootedDirectoryPath.FromGoString("/project"), Usage.Files, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            foreach (string path in (string[])["/project/a.ts"]) Check.Equal(matcher!.MatchString(path), true, context + path);
            foreach (string path in (string[])["/project/a.js"]) Check.Equal(matcher!.MatchString(path), false, context + path);
        }
        {
            const string context = "TestSingleSpecMatcher: trailing ** non-exclude returns nil";
            var matcher = M.NewSpecMatcher((List<string>?)["**"], RootedDirectoryPath.FromGoString("/project"), Usage.Files, CaseSensitivity.CaseSensitive);
            Check.True(matcher is null, context + " should be nil");
        }
        {
            const string context = "TestSingleSpecMatcher: trailing ** exclude works";
            var matcher = M.NewSpecMatcher((List<string>?)["**"], RootedDirectoryPath.FromGoString("/project"), Usage.Exclude, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            foreach (string path in (string[])["/project/anything", "/project/deep/path"]) Check.Equal(matcher!.MatchString(path), true, context + path);
        }
    }

    private static void TestSpecMatchers()
    {
        {
            const string context = "TestSpecMatchers: multiple specs return correct index";
            var matcher = M.NewSpecMatcher((List<string>?)["*.ts", "*.tsx", "*.js"], RootedDirectoryPath.FromGoString("/project"), Usage.Files, CaseSensitivity.CaseSensitive);
            Check.True(matcher is not null, context + " should not be nil");
            Check.Equal(matcher!.MatchIndex("/project/a.ts"), 0, context);
            Check.Equal(matcher!.MatchIndex("/project/b.tsx"), 1, context);
            Check.Equal(matcher!.MatchIndex("/project/c.js"), 2, context);
            Check.Equal(matcher!.MatchIndex("/project/d.css"), -1, context);
        }
        {
            const string context = "TestSpecMatchers: empty specs returns nil";
            var matcher = M.NewSpecMatcher((List<string>?)[], RootedDirectoryPath.FromGoString("/project"), Usage.Files, CaseSensitivity.CaseSensitive);
            Check.True(matcher is null, context + " should be nil");
        }
    }
}

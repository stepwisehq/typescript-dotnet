// Copyright (c) Microsoft Corporation. Apache 2.0: licenses/TypeScript-LICENSE.txt.
// Original module/resolver_test.go TestNodeModulePackageRoot: complete eleven rows.
using TypeScript.Modules;
using TypeScript.Scanner;

namespace Configuration.Checks;

public static class ModulePackageRootChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestNodeModulePackageRoot"];

    public static void Run(string name)
    {
        if (name != "TestNodeModulePackageRoot")
            throw new ArgumentOutOfRangeException(nameof(name));

        foreach (var row in new (string Name, string Path, bool IsFolder, string Expected)[]
        {
            ("file in package", "/a/node_modules/b/lib/index.d.ts", false, "/a/node_modules/b"),
            ("file in scoped package", "/a/node_modules/@scope/b/lib/index.d.ts", false, "/a/node_modules/@scope/b"),
            ("folder subpath", "/a/node_modules/b/lib/File", true, "/a/node_modules/b"),
            ("folder subpath scoped", "/a/node_modules/@scope/b/lib/File", true, "/a/node_modules/@scope/b"),
            ("package root folder", "/a/node_modules/b", true, "/a/node_modules/b"),
            ("scoped package root folder", "/a/node_modules/@scope/b", true, "/a/node_modules/@scope/b"),
            ("package root interpreted as file", "/a/node_modules/b", false, "/a/node_modules"),
            ("scoped package root interpreted as file", "/a/node_modules/@scope/b", false, "/a/node_modules/@scope"),
            ("scope-only folder", "/a/node_modules/@scope", true, "/a/node_modules/@scope"),
            ("types scope-only folder", "/a/node_modules/@types", true, "/a/node_modules/@types"),
            ("not in node_modules", "/a/src/index.ts", false, ""),
        })
        {
            var actual = row.IsFolder
                ? ModuleUtilities.NodeModulePackageRootForDirectory(TsPath.RootedDirectoryPathFromNormalized(row.Path))
                : ModuleUtilities.NodeModulePackageRootForFile(TsPath.RootedFilePathFromNormalized(row.Path));
            Check.Equal(actual.AsString(), row.Expected, "Original nodeModulesPackageRoot: " + row.Name);
        }
    }
}

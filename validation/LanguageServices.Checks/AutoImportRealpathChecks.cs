// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete original internal/ls/autoimport/util_test.go realpath assertions.
// Uses the existing writable compiler-harness MapFS adaptation; T.Parallel hints are sequential.
using TypeScript.Scanner;
using Program.Checks;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;
using AutoImportApi = TypeScript.LanguageServices.AutoImport.AutoImport;

namespace LanguageServices.Checks;
public static class AutoImportRealpathChecks
{
    public static readonly string[] Names = ["TestGetPackageRealpathFuncs_FollowsNodeModulesSymlinks", "TestGetPackageRealpathFuncs_DuplicateCacheKeys", "TestGetPackageRealpathFuncs_NonSymlinkedPackageWithSymlinkedDeps"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestGetPackageRealpathFuncs_FollowsNodeModulesSymlinks":
                FollowsSymlinks();
                break;
            case "TestGetPackageRealpathFuncs_DuplicateCacheKeys":
                DuplicateCacheKeys();
                break;
            case "TestGetPackageRealpathFuncs_NonSymlinkedPackageWithSymlinkedDeps":
                NonSymlinkedPackage();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown original realpath function");
        }
    }

    private static RootedFilePath File(string value) => RootedFilePath.FromValue(ByteStringValue.FromString(value));
    private static RootedDirectoryPath Directory(string value) => RootedDirectoryPath.FromValue(ByteStringValue.FromString(value));
    private static void Equal(RootedFilePath actual, string expected)
    {
        if (!actual.AsValue().EqualsValue(ByteStringValue.FromString(expected)))
            throw new InvalidOperationException($"Realpath {actual.AsString()} != {expected}");
    }

    private static void FollowsSymlinks()
    {
        var fs = new CompilerBaselineFileSystem([new Input("/symlink-bin/pkg", Symlink: "/real/bin/pkg"), new Input("/real/bin/pkg/index.d.ts", "export declare const a: number;"), new Input("/real/bin/pkg/node_modules/.package-lock.json", "{}"), new Input("/real/bin/pkg/node_modules/dep", Symlink: "/real/dep"), new Input("/real/bin/pkg/node_modules/@scope/dep", Symlink: "/real/scoped-dep"), new Input("/real/dep/index.d.ts", "export declare const b: number;"), new Input("/real/dep/src/utils/helper.d.ts", "export declare const c: number;"), new Input("/real/scoped-dep/index.d.ts", "export declare const d: number;")]);
        var(realpath, _) = AutoImportApi.GetPackageRealpathFuncs(fs, Directory("/symlink-bin/pkg"));
        Equal(realpath(File("/real/bin/pkg/node_modules/.package-lock.json")), "/real/bin/pkg/node_modules/.package-lock.json");
        Equal(realpath(File("/symlink-bin/pkg/index.d.ts")), "/real/bin/pkg/index.d.ts");
        Equal(realpath(File("/symlink-bin/pkg2/index.d.ts")), "/symlink-bin/pkg2/index.d.ts");
        Equal(realpath(File("/real/bin/pkg/node_modules/dep/index.d.ts")), "/real/dep/index.d.ts");
        Equal(realpath(File("/real/bin/pkg/node_modules/dep")), "/real/dep");
        Equal(realpath(File("/real/bin/pkg/node_modules/@scope")), "/real/bin/pkg/node_modules/@scope");
        Equal(realpath(File("/real/bin/pkg/node_modules/@scope/dep")), "/real/scoped-dep");
        Equal(realpath(File("/real/bin/pkg/node_modules/dep/src/utils/helper.d.ts")), "/real/dep/src/utils/helper.d.ts");
    }

    private static void DuplicateCacheKeys()
    {
        var fs = new CompilerBaselineFileSystem([new Input("/workspace/packages/app-a", Symlink: "/store/app-a"), new Input("/workspace/packages/app-b", Symlink: "/store/app-b"), new Input("/store/app-a/index.d.ts", "export declare const a: number;"), new Input("/store/app-b/index.d.ts", "export declare const b: number;"), new Input("/store/app-a/node_modules/shared-lib", Symlink: "/store/shared-lib"), new Input("/store/app-b/node_modules/shared-lib", Symlink: "/store/shared-lib"), new Input("/store/shared-lib/index.d.ts", "export declare const shared: string;")]);
        var(realpathA, _) = AutoImportApi.GetPackageRealpathFuncs(fs, Directory("/workspace/packages/app-a"));
        var(realpathB, _) = AutoImportApi.GetPackageRealpathFuncs(fs, Directory("/workspace/packages/app-b"));
        Equal(realpathA(File("/store/app-a/node_modules/shared-lib/index.d.ts")), "/store/shared-lib/index.d.ts");
        Equal(realpathB(File("/store/app-b/node_modules/shared-lib/index.d.ts")), "/store/shared-lib/index.d.ts");
    }

    private static void NonSymlinkedPackage()
    {
        var fs = new CompilerBaselineFileSystem([new Input("/real/my-pkg/index.d.ts", "export declare const a: number;"), new Input("/real/my-pkg/node_modules/dep", Symlink: "/real/dep"), new Input("/real/dep/index.d.ts", "export declare const b: number;")]);
        var(realpath, _) = AutoImportApi.GetPackageRealpathFuncs(fs, Directory("/real/my-pkg"));
        Equal(realpath(File("/real/my-pkg/index.d.ts")), "/real/my-pkg/index.d.ts");
        Equal(realpath(File("/real/my-pkg/node_modules/dep/index.d.ts")), "/real/dep/index.d.ts");
    }
}

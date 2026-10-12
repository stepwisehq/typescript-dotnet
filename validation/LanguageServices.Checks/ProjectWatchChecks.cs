// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete six original internal/project/watch_test.go functions; case rows retained.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static class ProjectWatchChecks
{
    public static readonly string[] Names = ["TestGetPathComponentsForWatching", "TestNilWatchedFilesClone", "TestResolutionLookupWatcherPreservesDirectorySpelling", "TestResolutionLookupWatcherPreservesNodeModulesSpelling", "TestResolutionLookupWatcherPreservesIncludedDirectorySpelling", "TestResolutionLookupWatcherAggregatesUsingHostCaseSensitivity"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("Original watcher assertion failed");
    }

    private static RootedDirectoryPath Directory(string path) => RootedDirectoryPath.FromGoString(path);
    private static TypeScript.Collections.SyncMap<PathKey, RootedFilePath> Files(CaseSensitivity sensitivity, params string[] names)
    {
        var files = new TypeScript.Collections.SyncMap<PathKey, RootedFilePath>();
        foreach (var name in names)
        {
            var file = RootedFilePath.FromGoString(name);
            files.Store(sensitivity.PathKey(file.AsPath()), file);
        }

        return files;
    }

    private static void Equal(Slice<ByteStringValue> actual, params string[] expected)
    {
        Require(actual.Length == expected.Length);
        for (int i = 0; i < expected.Length; i++)
            Require(actual[i].EqualsBytes(GoString.Bytes(expected[i])));
    }

    public static void Run(string name)
    {
        if (name == Names[0])
        {
            (string Path, string[] Components)[] cases = [("/project", ["/", "project"]), ("C:\\project", ["C:/", "project"]), ("//server/share/project/tsconfig.json", ["//server/share", "project", "tsconfig.json"]), ("\\\\server\\share\\project\\tsconfig.json", ["//server/share", "project", "tsconfig.json"]), ("C:\\Users", ["C:/Users"]), ("C:\\Users\\andrew\\project", ["C:/Users/andrew", "project"]), ("/home", ["/home"]), ("/home/andrew/project", ["/home/andrew", "project"])];
            foreach (var(path, components)in cases)
                Equal(ProjectWatches.GetPathComponentsForWatching(TsPath.ToRootedDirectoryPath(path, default)), components);
            return;
        }

        if (name == Names[1])
        {
            Require(ProjectWatches.Clone<int>(null, 42)is null);
            return;
        }

        var sensitivity = CaseSensitivity.CaseInsensitive;
        if (name == Names[2])
        {
            var result = ProjectWatches.CreateResolutionLookupGlobMapper(Directory("/workspace"), Directory("/lib"), Directory("/current"), sensitivity)(Files(sensitivity, "/External/Dir/file.ts"));
            Require(result.DirectoriesOutsideWorkspace.Length == 1 && result.DirectoriesOutsideWorkspace[0] == Directory("/External/Dir"));
            var watcher = ProjectWatches.NewRecursiveDirectoryWatcher(result.DirectoriesOutsideWorkspace[0], WatchKind.Create, true);
            Require(watcher.GlobPattern.RelativePattern!.BaseUri.URI!.Value.Value.EqualsBytes("file:///External/Dir"u8));
            return;
        }

        if (name == Names[3])
        {
            var result = ProjectWatches.CreateResolutionLookupGlobMapper(Directory("/workspace"), Directory("/lib"), Directory("/current"), sensitivity)(Files(sensitivity, "/External/Node_Modules/pkg/index.d.ts"));
            Equal(result.PatternsInsideWorkspace, "/External/Node_Modules/**/*");
            return;
        }

        if (name == Names[4])
        {
            var result = ProjectWatches.CreateResolutionLookupGlobMapper(Directory("/Workspace"), Directory("/Lib"), Directory("/Project"), sensitivity)(Files(sensitivity, "/Workspace/src/index.ts", "/Project/src/index.ts", "/Lib/lib.d.ts"));
            Equal(result.PatternsInsideWorkspace, "/Workspace/**/*", "/Project/**/*", "/Lib/**/*");
            return;
        }

        if (name != Names[5])
            throw new ArgumentException(name);
        foreach (var policy in new[]
        {
            CaseSensitivity.CaseSensitive,
            CaseSensitivity.CaseInsensitive
        }

        )
        {
            var result = ProjectWatches.CreateResolutionLookupGlobMapper(Directory("/Workspace"), Directory("/Lib"), Directory("/Project"), policy)(Files(policy, "/External/Lib/src/a.ts", "/external/LIB/test/b.ts"));
            if (policy == CaseSensitivity.CaseSensitive)
                Require(result.DirectoriesOutsideWorkspace.Length == 2 && result.DirectoriesOutsideWorkspace[0] == Directory("/External/Lib/src") && result.DirectoriesOutsideWorkspace[1] == Directory("/external/LIB/test"));
            else
                Require(result.DirectoriesOutsideWorkspace.Length == 1 && (result.DirectoriesOutsideWorkspace[0] == Directory("/External/Lib") || result.DirectoriesOutsideWorkspace[0] == Directory("/external/LIB")));
        }
    }
}

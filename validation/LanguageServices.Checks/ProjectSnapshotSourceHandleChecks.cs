// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// snapshotfs_test.go:34–70 countingHandleFileSystem and original cached-handle test.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Vfs;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotChecks
{
    public static readonly string[] Names = ["TestSnapshotFSBuilderCachesReturnedSourceHandle", "TestSnapshotFSBuilder", "TestSnapshotFS", "TestSourceFS", "TestRealpathAliasLifecycle", "TestExpandAndFilterWatchEvents"];
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
            case "TestSnapshotFSBuilderCachesReturnedSourceHandle":
                TestSnapshotFSBuilderCachesReturnedSourceHandle();
                break;
            case "TestSnapshotFSBuilder":
                TestSnapshotFSBuilder();
                break;
            case "TestSnapshotFS":
                TestSnapshotFS();
                break;
            case "TestSourceFS":
                TestSourceFS();
                break;
            case "TestRealpathAliasLifecycle":
                TestRealpathAliasLifecycle();
                break;
            case "TestExpandAndFilterWatchEvents":
                TestExpandAndFilterWatchEvents();
                break;
            default:
                throw new ArgumentException(name);
        }
    }

    private static void TestSnapshotFSBuilderCachesReturnedSourceHandle()
    {
        var fileSystem = new CountingHandleFileSystem();
        var builder = new SnapshotFSBuilder(fileSystem, null, null, null);
        var file = builder.GetFile(File("/src/index.ts"));
        Content(file, fileSystem.Content);
        Require(ReferenceEquals(builder.GetFile(File("/src/index.ts")), file));
        Require(fileSystem.GetFileByPathCalls == 1 && fileSystem.ReadFileCalls == 0);
    }

    private sealed class CountingHandleFileSystem : ILayeredFileSystem
    {
        private readonly OverlayFS lower = new(new ProjectTestFileSystem(TypeScript.Scanner.CaseSensitivity.CaseSensitive), null, PositionEncodingKind.UTF16);
        internal readonly string Content = "export const value = 1;";
        internal int GetFileByPathCalls, ReadFileCalls;
        public IFileHandle? GetFile(RootedFilePath path) => GetFileByPath(path, CaseSensitivity().PathKey(path.AsPath()));
        public IFileHandle? GetFileByPath(RootedFilePath name, PathKey path)
        {
            GetFileByPathCalls++;
            return OverlayFiles.NewCachedFileHandle(name, Text(Content));
        }

        public Dictionary<PathKey, Overlay>? Overlays() => lower.Overlays();
        public CaseSensitivity CaseSensitivity() => lower.CaseSensitivity();
        public bool FileExists(RootedFilePath path) => lower.FileExists(path);
        public bool DirectoryExists(RootedDirectoryPath path) => lower.DirectoryExists(path);
        public Entries GetAccessibleEntries(RootedDirectoryPath path) => lower.GetAccessibleEntries(path);
        public (string Contents, bool Ok) ReadFile(RootedFilePath path)
        {
            ReadFileCalls++;
            return (Content, true);
        }

        public RootedPath Realpath(RootedPath path) => lower.Realpath(path);
        public TypeScript.Vfs.FileInfo? Stat(RootedPath path) => lower.Stat(path);
        public Exception? WriteFile(RootedFilePath path, string content) => lower.WriteFile(path, content);
    }
}

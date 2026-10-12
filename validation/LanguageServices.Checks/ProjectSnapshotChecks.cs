// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Assertion-preserving internal/project/snapshotfs_test.go port. TestAutoImportBuilderFS
// remains pending the genuine auto-import builder owner; no test-only service substitute.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.ProjectSystem.Dirty;
using TypeScript.Lsp.Protocol;
using TypeScript.Vfs;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotChecks
{
    private static RootedFilePath File(string name) => RootedFilePath.FromGoString(name);
    private static RootedDirectoryPath Directory(string name) => RootedDirectoryPath.FromGoString(name);
    private static PathKey Key(string name) => CaseSensitivity.CaseInsensitive.PathKey(RootedPath.FromGoString(name));
    private static ByteStringValue Text(string text) => ByteStringValue.FromString(text);
    private static DocumentUri URI(string name) => TypeScript.LanguageServices.Lsconv.Lsconv.FileNameToDocumentURI(File(name));
    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException("Original SnapshotFS assertion failed");
    }

    private static void Content(IFileHandle? file, string content) => Require(file is not null && file.ContentValue().EqualsValue(Text(content)));
    private static bool Contains(Slice<ByteStringValue> values, string name)
    {
        foreach (var value in values)
            if (value.EqualsValue(Text(name)))
                return true;
        return false;
    }

    private static Dictionary<PathKey, Overlay> Overlays(params (string Name, string Content)[] values) => values.ToDictionary(value => Key(value.Name), value => new Overlay(File(value.Name), Text(value.Content), 0, ScriptKind.Unknown));
    private static SnapshotFSBuilder Builder(ProjectTestFileSystem fs, Dictionary<PathKey, Overlay>? overlays = null, Dictionary<PathKey, CachedFile>? files = null, Dictionary<PathKey, CloneableMap<PathKey, ByteStringValue>>? directories = null, Dictionary<PathKey, RealpathAliasSet>? aliases = null) => new(new OverlayFS(fs, overlays ?? new(), PositionEncodingKind.UTF16), files ?? new(), directories ?? new(), aliases);
    // These explicit fixture directory owners reproduce the original supplied maps.
    private static (Dictionary<PathKey, CachedFile> Files, Dictionary<PathKey, CloneableMap<PathKey, ByteStringValue>> Directories) Existing(params (string Name, string Content)[] values)
    {
        var files = new Dictionary<PathKey, CachedFile>();
        var directories = new Dictionary<PathKey, CloneableMap<PathKey, ByteStringValue>>();
        foreach (var(name, content)in values)
        {
            files.Add(Key(name), new(File(name), Text(content)));
            var child = RootedPath.FromGoString(name);
            while (true)
            {
                var parent = child.Directory().AsPath();
                if (parent == child)
                    break;
                if (!directories.TryGetValue(Key(parent.AsString()), out var directory))
                    directories.Add(Key(parent.AsString()), directory = new(new()));
                directory.Map![Key(child.AsString())] = child.BaseNameValue();
                child = parent;
            }
        }

        return (files, directories);
    }

    private static bool HasDirectory(SnapshotFS snapshot, string path) => snapshot.cacheDirectories?.ContainsKey(Key(path)) == true;
    private static bool HasFile(SnapshotFS snapshot, string path) => snapshot.cacheFiles?.ContainsKey(Key(path)) == true;
    private static bool HasChild(SnapshotFS snapshot, string directory, string child) => snapshot.cacheDirectories?.TryGetValue(Key(directory), out var value) == true && value.Map?.ContainsKey(Key(child)) == true;
    // snapshotfs_test.go:72 TestSnapshotFSBuilder, all eleven original subtests.
    private static void TestSnapshotFSBuilder()
    {
        var fs = new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, new Input("/src/foo.ts", "const foo = 1;"));
        var builder = Builder(fs);
        Content(builder.GetFile(File("/src/foo.ts")), "const foo = 1;");
        var(snapshot, changed) = builder.FinalizeValue();
        Require(changed && HasDirectory(snapshot, "/src") && HasChild(snapshot, "/src", "/src/foo.ts") && HasDirectory(snapshot, "/") && HasChild(snapshot, "/", "/src"));
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/nested/deep/file.ts", "export const x = 1;"));
        builder = Builder(fs);
        Require(builder.GetFile(File("/src/nested/deep/file.ts"))is not null);
        (snapshot, changed) = builder.FinalizeValue();
        Require(changed && HasChild(snapshot, "/src/nested/deep", "/src/nested/deep/file.ts") && HasChild(snapshot, "/src/nested", "/src/nested/deep") && HasChild(snapshot, "/src", "/src/nested") && HasChild(snapshot, "/", "/src"));
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/foo.ts", "const foo = 1;"));
        var existing = Existing(("/src/foo.ts", "const foo = 1;"));
        builder = Builder(fs, files: existing.Files, directories: existing.Directories);
        builder.cacheFiles.Load(Key("/src/foo.ts")).Entry!.Delete();
        (snapshot, changed) = builder.FinalizeValue();
        Require(changed && !HasFile(snapshot, "/src/foo.ts") && !HasDirectory(snapshot, "/src") && !HasDirectory(snapshot, "/"));
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/foo.ts", "const foo = 1;"), new Input("/src/bar.ts", "const bar = 2;"));
        existing = Existing(("/src/foo.ts", "const foo = 1;"), ("/src/bar.ts", "const bar = 2;"));
        builder = Builder(fs, files: existing.Files, directories: existing.Directories);
        builder.cacheFiles.Load(Key("/src/foo.ts")).Entry!.Delete();
        (snapshot, changed) = builder.FinalizeValue();
        Require(changed && !HasFile(snapshot, "/src/foo.ts") && HasFile(snapshot, "/src/bar.ts") && HasDirectory(snapshot, "/src") && !HasChild(snapshot, "/src", "/src/foo.ts") && HasChild(snapshot, "/src", "/src/bar.ts") && HasDirectory(snapshot, "/") && HasChild(snapshot, "/", "/src"));
        existing = Existing(("/src/foo.ts", "const foo = 1;"));
        builder = Builder(fs, files: existing.Files, directories: existing.Directories);
        Require(builder.GetFile(File("/src/bar.ts"))is not null);
        (snapshot, changed) = builder.FinalizeValue();
        Require(changed && HasChild(snapshot, "/src", "/src/foo.ts") && HasChild(snapshot, "/src", "/src/bar.ts"));
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/foo.ts", "const foo = 1;"));
        existing = Existing(("/src/foo.ts", "const foo = 1;"));
        builder = Builder(fs, files: existing.Files, directories: existing.Directories);
        (snapshot, changed) = builder.FinalizeValue();
        Require(!changed && HasChild(snapshot, "/src", "/src/foo.ts"));
        builder = Builder(fs, Overlays(("/src/foo.ts", "const foo = 999;")));
        Content(builder.GetFile(File("/src/foo.ts")), "const foo = 999;");
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/a.ts", "const a = 1;"), new Input("/src/b.ts", "const b = 2;"), new Input("/lib/utils.ts", "export const util = 1;"), new Input("/lib/helpers.ts", "export const helper = 1;"), new Input("/other/single.ts", "const single = 1;"));
        existing = Existing(("/src/a.ts", "const a = 1;"), ("/other/single.ts", "const single = 1;"));
        builder = Builder(fs, files: existing.Files, directories: existing.Directories);
        foreach (var name in new[]
        {
            "/src/b.ts",
            "/lib/utils.ts",
            "/lib/helpers.ts"
        }

        )
            Require(builder.GetFile(File(name))is not null);
        foreach (var name in new[]
        {
            "/src/a.ts",
            "/other/single.ts"
        }

        )
            builder.cacheFiles.Load(Key(name)).Entry!.Delete();
        (snapshot, changed) = builder.FinalizeValue();
        Require(changed && !HasFile(snapshot, "/src/a.ts") && !HasFile(snapshot, "/other/single.ts") && HasFile(snapshot, "/src/b.ts") && HasFile(snapshot, "/lib/utils.ts") && HasFile(snapshot, "/lib/helpers.ts") && !HasDirectory(snapshot, "/other") && HasDirectory(snapshot, "/src") && !HasChild(snapshot, "/src", "/src/a.ts") && HasChild(snapshot, "/src", "/src/b.ts") && HasDirectory(snapshot, "/lib") && HasChild(snapshot, "/lib", "/lib/utils.ts") && HasChild(snapshot, "/lib", "/lib/helpers.ts") && HasChild(snapshot, "/", "/src") && HasChild(snapshot, "/", "/lib") && !HasChild(snapshot, "/", "/other"));
        fs = new(CaseSensitivity.CaseInsensitive);
        builder = Builder(fs, Overlays(("/src/overlay.ts", "const x = 1;"), ("/src/nested/deep.ts", "const y = 2;")));
        var entries = builder.GetAccessibleEntries(Directory("/src"));
        Require(Contains(entries.Files, "overlay.ts") && Contains(entries.Directories, "nested"));
        Require(Contains(builder.GetAccessibleEntries(Directory("/src/nested")).Files, "deep.ts"));
        Require(Contains(builder.GetAccessibleEntries(Directory("/")).Directories, "src"));
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/disk.ts", "const disk = 1;"));
        builder = Builder(fs, Overlays(("/src/overlay.ts", "const overlay = 1;")));
        entries = builder.GetAccessibleEntries(Directory("/src"));
        Require(Contains(entries.Files, "disk.ts") && Contains(entries.Files, "overlay.ts"));
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/a.ts"), new Input("/src/b.ts"), new Input("/src/c.ts"), new Input("/src/d.ts"), new Input("/src/e.ts"));
        builder = Builder(fs, Overlays(("/src/overlay.ts", "")));
        builder.FS().GetAccessibleEntries(Directory("/src"));
        using var start = new ManualResetEventSlim();
        var jobs = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            start.Wait();
            builder.GetAccessibleEntries(Directory("/src"));
        })).ToArray();
        start.Set();
        Task.WaitAll(jobs);
        Require(Contains(builder.GetAccessibleEntries(Directory("/src")).Files, "overlay.ts"));
    }

    private static SnapshotFS Snapshot(ProjectTestFileSystem fs, Dictionary<PathKey, Overlay>? overlays = null, Dictionary<PathKey, CachedFile>? files = null, Dictionary<PathKey, CloneableMap<PathKey, ByteStringValue>>? directories = null) => new(fs.CaseSensitivity(), new OverlayFS(fs, overlays, PositionEncodingKind.UTF16), files ?? new(), directories ?? new(), null);
    // snapshotfs_test.go:580, all seven original SnapshotFS subtests.
    private static void TestSnapshotFS()
    {
        var fs = new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, new Input("/src/foo.ts", "disk content"));
        var snapshot = Snapshot(fs, Overlays(("/src/foo.ts", "overlay content")));
        Content(snapshot.GetFile(File("/src/foo.ts")), "overlay content");
        snapshot = Snapshot(fs, files: new() { [Key("/src/foo.ts")] = new(File("/src/foo.ts"), Text("disk content")) });
        Content(snapshot.GetFile(File("/src/foo.ts")), "disk content");
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/foo.ts", "fs content"));
        snapshot = Snapshot(fs);
        Content(snapshot.GetFile(File("/src/foo.ts")), "fs content");
        fs = new(CaseSensitivity.CaseInsensitive);
        snapshot = Snapshot(fs);
        Require(snapshot.GetFile(File("/src/nonexistent.ts"))is null);
        var overlay = new OverlayFS(fs, Overlays(("/src/foo.ts", "overlay content")), PositionEncodingKind.UTF16);
        Require(overlay.GetFile(File("/src/foo.ts"))!.IsOverlay());
        Require(overlay.GetFile(File("/src/bar.ts"))is null);
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/foo.ts", "disk content"));
        snapshot = Snapshot(fs, Overlays(("/src/foo.ts", "overlay content")));
        Content(snapshot.GetFileByPath(File("/src/foo.ts"), Key("/src/foo.ts")), "overlay content");
        fs = new(CaseSensitivity.CaseInsensitive);
        var existing = Existing(("/src/disk.ts", "disk content"));
        snapshot = Snapshot(fs, Overlays(("/src/overlay.ts", "overlay content")), existing.Files, existing.Directories);
        var entries = snapshot.GetAccessibleEntries(Directory("/src"));
        Require(Contains(entries.Files, "disk.ts") && Contains(entries.Files, "overlay.ts"));
    }

    // snapshotfs_test.go:741, all five original SourceFS subtests.
    private static void TestSourceFS()
    {
        var fs = new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, new Input("/src/foo.ts", "content"));
        var source = new SourceFS(true, Snapshot(fs));
        Require(!source.SeenFile(Key("/src/foo.ts")));
        Require(source.GetFile(File("/src/foo.ts"))is not null);
        Require(source.SeenFile(Key("/src/foo.ts")));
        source = new(false, Snapshot(fs));
        Require(source.GetFile(File("/src/foo.ts"))is not null);
        Require(!source.SeenFile(Key("/src/foo.ts")));
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/foo.ts", "content"), new Input("/src/bar.ts", "content"));
        source = new(true, Snapshot(fs));
        source.GetFile(File("/src/foo.ts"));
        Require(source.SeenFile(Key("/src/foo.ts")));
        source.DisableTracking();
        source.GetFile(File("/src/bar.ts"));
        Require(!source.SeenFile(Key("/src/bar.ts")));
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/foo.ts", "content"));
        source = new(false, Snapshot(fs));
        Require(source.FileExists(File("/src/foo.ts")));
        Require(!source.FileExists(File("/src/nonexistent.ts")));
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/foo.ts", "file content"));
        source = new(false, Snapshot(fs));
        var(content, ok) = source.ReadFile(File("/src/foo.ts"));
        Require(ok && content == "file content");
        Require(!source.ReadFile(File("/src/nonexistent.ts")).Ok);
    }
}

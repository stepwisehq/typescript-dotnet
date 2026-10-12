// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// snapshotfs_test.go:925 TestRealpathAliasLifecycle, all twelve original subtests.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static partial class ProjectSnapshotChecks
{
    private static ProjectTestFileSystem AliasFS(bool second = false, string content = "{\"name\": \"mylib\"}")
    {
        var files = new List<Input>
        {
            new("/project/node_modules/mylib", Symlink: "/packages/mylib"),
            new("/packages/mylib/package.json", content)
        };
        if (second)
            files.Add(new("/project/node_modules/alias", Symlink: "/packages/mylib"));
        return new(CaseSensitivity.CaseInsensitive, files.ToArray());
    }

    private static SnapshotFSBuilder Next(ProjectTestFileSystem fs, SnapshotFS old) => Builder(fs, files: old.cacheFiles, directories: old.cacheDirectories, aliases: old.nodeModulesRealpathAliases);
    private static bool HasAlias(SnapshotFS snapshot, string real, string alias) => snapshot.nodeModulesRealpathAliases?.TryGetValue(Key(real), out var set) == true && set.Paths?.ContainsKey(Key(alias)) == true;
    private static bool HasAliases(SnapshotFS snapshot, string real) => snapshot.nodeModulesRealpathAliases?.ContainsKey(Key(real)) == true;
    private static void TestRealpathAliasLifecycle()
    {
        var fs = new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, new Input("/project/node_modules/mylib", Symlink: "/packages/mylib"), new Input("/packages/mylib/package.json", "{\"name\": \"mylib\", \"main\": \"index.js\"}"), new Input("/packages/mylib/index.d.ts", "export declare const x: number;"), new Input("/project/node_modules/nolink/package.json", "{\"name\": \"nolink\"}"));
        var builder = Builder(fs);
        Content(builder.GetFile(File("/project/node_modules/mylib/package.json")), "{\"name\": \"mylib\", \"main\": \"index.js\"}");
        Require(builder.GetFile(File("/project/node_modules/nolink/package.json"))is not null);
        var(snapshot, _) = builder.FinalizeValue();
        Require(HasAlias(snapshot, "/packages/mylib/package.json", "/project/node_modules/mylib/package.json"));
        Require(!HasAliases(snapshot, "/project/node_modules/nolink/package.json"));
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/project/link", Symlink: "/elsewhere"), new Input("/elsewhere/index.ts", "export const x = 1;"));
        builder = Builder(fs);
        Require(builder.GetFile(File("/project/link/index.ts"))is not null);
        (snapshot, _) = builder.FinalizeValue();
        Require((snapshot.nodeModulesRealpathAliases?.Count ?? 0) == 0);
        fs = AliasFS();
        builder = Builder(fs);
        builder.GetFile(File("/project/node_modules/mylib/package.json"));
        var(first, _) = builder.FinalizeValue();
        var(second, _) = Next(fs, first).FinalizeValue();
        Require(HasAlias(second, "/packages/mylib/package.json", "/project/node_modules/mylib/package.json"));
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/project/node_modules/mylib", Symlink: "/packages/mylib"), new Input("/packages/mylib/package.json", "{\"name\": \"mylib\"}"), new Input("/packages/mylib/index.d.ts", "export declare const x: number;"));
        builder = Builder(fs);
        builder.GetFile(File("/project/node_modules/mylib/package.json"));
        builder.GetFile(File("/project/node_modules/mylib/index.d.ts"));
        (first, _) = builder.FinalizeValue();
        Require(HasAliases(first, "/packages/mylib/package.json") && HasAliases(first, "/packages/mylib/index.d.ts"));
        builder = Next(fs, first);
        builder.cacheFiles.Load(Key("/project/node_modules/mylib/index.d.ts")).Entry!.Delete();
        (second, _) = builder.FinalizeValue();
        Require(HasAlias(second, "/packages/mylib/package.json", "/project/node_modules/mylib/package.json") && !HasAliases(second, "/packages/mylib/index.d.ts"));
        fs = AliasFS(true);
        builder = Builder(fs);
        Require(builder.GetFile(File("/project/node_modules/mylib/package.json"))is not null);
        Require(builder.GetFile(File("/project/node_modules/alias/package.json"))is not null);
        (snapshot, _) = builder.FinalizeValue();
        Require(HasAlias(snapshot, "/packages/mylib/package.json", "/project/node_modules/mylib/package.json") && HasAlias(snapshot, "/packages/mylib/package.json", "/project/node_modules/alias/package.json"));
        builder = Builder(fs);
        builder.GetFile(File("/project/node_modules/mylib/package.json"));
        builder.GetFile(File("/project/node_modules/alias/package.json"));
        (first, _) = builder.FinalizeValue();
        builder = Next(fs, first);
        builder.cacheFiles.Load(Key("/project/node_modules/alias/package.json")).Entry!.Delete();
        (second, _) = builder.FinalizeValue();
        Require(HasAlias(second, "/packages/mylib/package.json", "/project/node_modules/mylib/package.json") && !HasAlias(second, "/packages/mylib/package.json", "/project/node_modules/alias/package.json"));
        fs = AliasFS();
        builder = Builder(fs);
        builder.GetFile(File("/project/node_modules/mylib/package.json"));
        (snapshot, _) = builder.FinalizeValue();
        var change = snapshot.ExpandRealpathAliases(new() { Changed = new() { URI("/packages/mylib/package.json") } });
        Require(change.Changed!.Contains(URI("/packages/mylib/package.json")) && change.Changed.Contains(URI("/project/node_modules/mylib/package.json")));
        fs = AliasFS();
        builder = Builder(fs);
        builder.GetFile(File("/project/node_modules/mylib/package.json"));
        (snapshot, _) = builder.FinalizeValue();
        change = snapshot.ExpandRealpathAliases(new() { Deleted = new() { URI("/packages/mylib/package.json") } });
        Require(change.Deleted!.Contains(URI("/project/node_modules/mylib/package.json")));
        snapshot = Snapshot(new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive));
        change = snapshot.ExpandRealpathAliases(new() { Changed = new() { URI("/some/file.ts") } });
        Require(change.Changed!.Count == 1 && change.Changed.Contains(URI("/some/file.ts")));
        fs = AliasFS(content: "{\"name\": \"mylib\", \"main\": \"index.js\"}");
        builder = Builder(fs);
        Content(builder.GetFile(File("/project/node_modules/mylib/package.json")), "{\"name\": \"mylib\", \"main\": \"index.js\"}");
        (first, _) = builder.FinalizeValue();
        Require(fs.WriteFile(File("/packages/mylib/package.json"), "{\"name\": \"mylib\"}")is null);
        builder = Next(fs, first);
        change = first.ExpandRealpathAliases(new() { Changed = new() { URI("/packages/mylib/package.json") } });
        builder.MarkDirtyFiles(change);
        Content(builder.GetFile(File("/project/node_modules/mylib/package.json")), "{\"name\": \"mylib\"}");
        (second, _) = builder.FinalizeValue();
        Require(HasFile(second, "/project/node_modules/mylib/package.json"));
        Content(second.cacheFiles![Key("/project/node_modules/mylib/package.json")], "{\"name\": \"mylib\"}");
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/project/node_modules/mylib", Symlink: "/packages/mylib"), new Input("/project/node_modules/other", Symlink: "/packages/other"), new Input("/packages/mylib/package.json", "{\"name\": \"mylib\"}"), new Input("/packages/other/package.json", "{\"name\": \"other\"}"));
        builder = Builder(fs);
        builder.GetFile(File("/project/node_modules/mylib/package.json"));
        (first, _) = builder.FinalizeValue();
        builder = Next(fs, first);
        builder.GetFile(File("/project/node_modules/other/package.json"));
        (second, _) = builder.FinalizeValue();
        Require(HasAliases(first, "/packages/mylib/package.json") && !HasAliases(first, "/packages/other/package.json") && HasAliases(second, "/packages/mylib/package.json") && HasAliases(second, "/packages/other/package.json"));
        fs = AliasFS(true);
        builder = Builder(fs);
        builder.GetFile(File("/project/node_modules/mylib/package.json"));
        (first, _) = builder.FinalizeValue();
        var aliases = first.nodeModulesRealpathAliases![Key("/packages/mylib/package.json")];
        Require(aliases.Paths!.Count == 1 && HasAlias(first, "/packages/mylib/package.json", "/project/node_modules/mylib/package.json"));
        builder = Next(fs, first);
        builder.GetFile(File("/project/node_modules/alias/package.json"));
        (second, _) = builder.FinalizeValue();
        Require(second.nodeModulesRealpathAliases![Key("/packages/mylib/package.json")].Paths!.Count == 2 && HasAlias(second, "/packages/mylib/package.json", "/project/node_modules/mylib/package.json") && HasAlias(second, "/packages/mylib/package.json", "/project/node_modules/alias/package.json"));
        Require(aliases.Paths.Count == 1 && !aliases.Paths.ContainsKey(Key("/project/node_modules/alias/package.json")));
    }

    // snapshotfs_test.go:1398, all five original watch-filter subtests.
    private static void TestExpandAndFilterWatchEvents()
    {
        var fs = new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, new Input("/project/index.ts", "export const x = 1;"));
        foreach (var path in new[]
        {
            "/project/node_modules",
            "/project/node_modules/@scope/pkg"
        }

        )
        {
            var result = Builder(fs).ExpandAndFilterWatchEvents(new() { Deleted = new() { URI(path) } }, Array.Empty<string>(), null, null, null);
            Require(result.Deleted!.Contains(URI(path)));
        }

        var change = Builder(fs).ExpandAndFilterWatchEvents(new() { Deleted = new() { URI("/project/build") } }, Array.Empty<string>(), null, null, null);
        Require((change.Deleted?.Count ?? 0) == 0);
        change = Builder(fs).ExpandAndFilterWatchEvents(new() { Changed = new() { URI("/project/mapper.config") }, Deleted = new() { URI("/project/mapper.config") } }, Array.Empty<string>(), new() { Key("/project/mapper.config") }, null, null);
        Require(change.Changed!.Contains(URI("/project/mapper.config")) && change.Deleted!.Contains(URI("/project/mapper.config")));
        fs = new(CaseSensitivity.CaseInsensitive, new Input("/src/foo.ts", "const foo = 1;"));
        var existing = Existing(("/src/foo.ts", "const foo = 1;"));
        change = Builder(fs, files: existing.Files, directories: existing.Directories).ExpandAndFilterWatchEvents(new() { Deleted = new() { URI("/src") } }, Array.Empty<string>(), null, null, null);
        Require(change.Deleted!.Contains(URI("/src/foo.ts")) && !change.Deleted.Contains(URI("/src")));
    }
}

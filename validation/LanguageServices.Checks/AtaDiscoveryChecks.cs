// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/project/ata/discovertypings_test.go TestDiscoverTypings, all nine subtests.
// The accepted virtual fixture filesystem replaces vfstest.FromMap; no Session substitute.
using TypeScript.Scanner;
using TypeScript.Collections;
using TypeScript.ProjectSystem.Ata;
using TypeScript.ProjectSystem.Logging;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;

namespace LanguageServices.Checks;
public static class AtaDiscoveryChecks
{
    public static readonly string[] Names = ["TestDiscoverTypings"];
    private const string Root = "/home/src/projects/project";
    private const string Typings = "/home/src/Library/Caches/typescript";
    public static int RunAll()
    {
        Run(Names[0]);
        return 1;
    }

    public static void Run(string name)
    {
        if (name != Names[0])
            throw new ArgumentException(name);
        // should use mappings from safe list
        Check([new(Root + "/app.js", ""), new(Root + "/jquery.js", ""), new(Root + "/chroma.min.js", "")], [Root + "/app.js", Root + "/jquery.js", Root + "/chroma.min.js"], [], [], [], null, ["jquery", "chroma-js"]);
        // should return node for core modules
        Check([new(Root + "/app.js", "")], [Root + "/app.js"], ["assert", "somename"], [], [], null, ["node", "somename"]);
        // should use cached locations
        Check([new(Root + "/app.js", ""), new(Root + "/node.d.ts", "")], [Root + "/app.js"], ["fs", "bar"], [("node", Root + "/node.d.ts", "1.3.0")], [("node", Registry())], [Root + "/node.d.ts"], ["bar"]);
        // should gracefully handle packages that have been removed from the types-registry
        Check([new(Root + "/app.js", ""), new(Root + "/node.d.ts", "")], [Root + "/app.js"], ["fs", "bar"], [("node", Root + "/node.d.ts", "1.3.0")], [], null, ["node", "bar"]);
        // should search only 2 levels deep
        Check([new(Root + "/app.js", ""), new(Root + "/node_modules/a/package.json", "{ \"name\": \"a\" }"), new(Root + "/node_modules/a/b/package.json", "{ \"name\": \"b\" }")], [Root + "/app.js"], [], [], [], null, ["a"]);
        // should support scoped packages
        Check([new(Root + "/app.js", ""), new(Root + "/node_modules/@a/b/package.json", "{ \"name\": \"@a/b\" }")], [Root + "/app.js"], [], [], [], null, ["@a/b"]);
        // should install expired typings
        Check([new(Root + "/app.js", "")], [Root + "/app.js"], ["http", "commander"], [("node", Typings + "/node_modules/@types/node/index.d.ts", "1.3.0"), ("commander", Typings + "/node_modules/@types/commander/index.d.ts", "1.0.0")], [("node", Registry()), ("commander", Registry())], [Typings + "/node_modules/@types/node/index.d.ts"], ["commander"]);
        // should install expired typings with prerelease version of tsserver
        var withoutCurrent = new Dictionary<ByteStringKey, ByteStringValue>(Registry());
        withoutCurrent.Remove(ByteStringKey.FromString("ts" + Core.VersionMajorMinor()));
        Check([new(Root + "/app.js", "")], [Root + "/app.js"], ["http"], [("node", Typings + "/node_modules/@types/node/index.d.ts", "1.0.0")], [("node", withoutCurrent)], null, ["node"]);
        // prerelease typings are properly handled
        var prerelease = new Dictionary<ByteStringKey, ByteStringValue>(Registry());
        prerelease[ByteStringKey.FromString("ts" + Core.VersionMajorMinor())] = ByteStringValue.FromString("1.3.0-next.1");
        Check([new(Root + "/app.js", "")], [Root + "/app.js"], ["http", "commander"], [("node", Typings + "/node_modules/@types/node/index.d.ts", "1.3.0-next.0"), ("commander", Typings + "/node_modules/@types/commander/index.d.ts", "1.3.0-next.0")], [("node", prerelease), ("commander", Registry())], null, ["node", "commander"]);
    }

    // Exact projecttestutil.TypesRegistryConfig; snapshots used by the two clone subtests.
    private static Dictionary<ByteStringKey, ByteStringValue> Registry() => new()
    {
        [ByteStringKey.FromString("latest")] = ByteStringValue.FromString("1.3.0"),
        [ByteStringKey.FromString("ts2.0")] = ByteStringValue.FromString("1.0.0"),
        [ByteStringKey.FromString("ts2.1")] = ByteStringValue.FromString("1.0.0"),
        [ByteStringKey.FromString("ts2.2")] = ByteStringValue.FromString("1.2.0"),
        [ByteStringKey.FromString("ts2.3")] = ByteStringValue.FromString("1.3.0"),
        [ByteStringKey.FromString("ts2.4")] = ByteStringValue.FromString("1.3.0"),
        [ByteStringKey.FromString("ts2.5")] = ByteStringValue.FromString("1.3.0"),
        [ByteStringKey.FromString("ts2.6")] = ByteStringValue.FromString("1.3.0"),
        [ByteStringKey.FromString("ts2.7")] = ByteStringValue.FromString("1.3.0")
    };
    private static void Check(Input[] files, string[] names, string[] unresolved, (string Name, string Path, string Version)[] cached, (string Name, Dictionary<ByteStringKey, ByteStringValue> Tags)[] registry, string[]? expectedPaths, string[] expectedNames)
    {
        var fs = new ProjectTestFileSystem(CaseSensitivity.CaseInsensitive, files);
        var cache = new SyncMap<ByteStringKey, CachedTyping>();
        foreach (var row in cached)
            cache.Store(ByteStringKey.FromString(row.Name), new() { TypingsLocation = RootedFilePath.FromGoString(row.Path), Version = TypeScript.Semver.Semver.MustParse(row.Version) });
        var entries = new Dictionary<ByteStringKey, Dictionary<ByteStringKey, ByteStringValue>?>();
        foreach (var row in registry)
            entries[ByteStringKey.FromString(row.Name)] = row.Tags;
        var imports = new HashSet<ByteStringKey>();
        foreach (var item in unresolved)
            imports.Add(ByteStringKey.FromString(item));
        var fileNames = Slice<RootedFilePath>.Make(names.Length);
        for (var i = 0; i < names.Length; i++)
            fileNames[i] = RootedFilePath.FromGoString(names[i]);
        var(paths, newNames, watch) = AutomaticTypeAcquisition.DiscoverTypings(fs, LogTree.NewLogTree("DiscoverTypings"), new() { CompilerOptions = new(), TypeAcquisition = new() { Enable = Tristate.True }, UnresolvedImports = unresolved.Length == 0 ? null : imports }, fileNames, RootedDirectoryPath.FromGoString(Root), cache, entries);
        if (expectedPaths == null)
        {
            if (!paths.IsNil)
                throw new InvalidOperationException("Original nil cached paths assertion differs");
        }
        else
        {
            if (paths.Count != expectedPaths.Length)
                throw new InvalidOperationException("Original cached paths length differs");
            for (var i = 0; i < paths.Count; i++)
                if (paths[i] != RootedFilePath.FromGoString(expectedPaths[i]))
                    throw new InvalidOperationException("Original cached path differs");
        }

        var actualNames = new HashSet<ByteStringKey>();
        foreach (var item in newNames)
            actualNames.Add(ByteStringKey.FromValue(item));
        var expected = new HashSet<ByteStringKey>();
        foreach (var item in expectedNames)
            expected.Add(ByteStringKey.FromString(item));
        if (!actualNames.SetEquals(expected))
            throw new InvalidOperationException("Original typing-name set differs");
        var expectedWatch = new[]
        {
            Root + "/bower_components",
            Root + "/node_modules"
        };
        if (watch.Count != expectedWatch.Length)
            throw new InvalidOperationException("Original watcher length differs");
        for (var i = 0; i < watch.Count; i++)
            if (watch[i] != RootedPath.FromGoString(expectedWatch[i]))
                throw new InvalidOperationException("Original watcher path/order differs");
    }
}

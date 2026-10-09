// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/core/nodemodules.go syntax-reference lookup tables.
// Pinned upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
internal static partial class Core
{
    public static readonly HashSet<string> UnprefixedNodeCoreModules = new(StringComparer.Ordinal)
    {
        "assert",
        "assert/strict",
        "async_hooks",
        "buffer",
        "child_process",
        "cluster",
        "console",
        "constants",
        "crypto",
        "dgram",
        "diagnostics_channel",
        "dns",
        "dns/promises",
        "domain",
        "events",
        "fs",
        "fs/promises",
        "http",
        "http2",
        "https",
        "inspector",
        "inspector/promises",
        "module",
        "net",
        "os",
        "path",
        "path/posix",
        "path/win32",
        "perf_hooks",
        "process",
        "punycode",
        "querystring",
        "readline",
        "readline/promises",
        "repl",
        "stream",
        "stream/consumers",
        "stream/promises",
        "stream/web",
        "string_decoder",
        "sys",
        "timers",
        "timers/promises",
        "tls",
        "trace_events",
        "tty",
        "url",
        "util",
        "util/types",
        "v8",
        "vm",
        "wasi",
        "worker_threads",
        "zlib",
    };
    public static readonly HashSet<string> ExclusivelyPrefixedNodeCoreModules = new(StringComparer.Ordinal)
    {
        "node:quic",
        "node:sea",
        "node:sqlite",
        "node:test",
        "node:test/reporters",
    };

    // nodemodules.go:75. Source once-initialized membership table; parser's
    // existing prefix-specific tables retain their separate identities.
    private static readonly Lazy<HashSet<string>> nodeCoreModules = new(() =>
    {
        var modules = new HashSet<string>(StringComparer.Ordinal);
        foreach (string unprefixed in UnprefixedNodeCoreModules)
        {
            modules.Add(unprefixed);
            modules.Add("node:" + unprefixed);
        }
        modules.UnionWith(ExclusivelyPrefixedNodeCoreModules);
        return modules;
    });

    public static HashSet<string> NodeCoreModules() => nodeCoreModules.Value;

    public static string NonRelativeModuleNameForTypingCache(string moduleName) =>
        NodeCoreModules().Contains(moduleName) ? "node" : moduleName;
}

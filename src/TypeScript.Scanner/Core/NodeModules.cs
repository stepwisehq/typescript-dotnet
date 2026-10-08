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
}

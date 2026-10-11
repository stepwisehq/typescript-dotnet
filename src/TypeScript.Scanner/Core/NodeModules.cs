// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of tsc/internal/core/nodemodules.go syntax-reference lookup tables.
// Pinned upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
internal static partial class Core
{
    public static readonly HashSet<ByteStringKey> UnprefixedNodeCoreModules = new(ByteStringKey.Comparer)
    {
        ByteStringKey.FromString("assert"),
        ByteStringKey.FromString("assert/strict"),
        ByteStringKey.FromString("async_hooks"),
        ByteStringKey.FromString("buffer"),
        ByteStringKey.FromString("child_process"),
        ByteStringKey.FromString("cluster"),
        ByteStringKey.FromString("console"),
        ByteStringKey.FromString("constants"),
        ByteStringKey.FromString("crypto"),
        ByteStringKey.FromString("dgram"),
        ByteStringKey.FromString("diagnostics_channel"),
        ByteStringKey.FromString("dns"),
        ByteStringKey.FromString("dns/promises"),
        ByteStringKey.FromString("domain"),
        ByteStringKey.FromString("events"),
        ByteStringKey.FromString("fs"),
        ByteStringKey.FromString("fs/promises"),
        ByteStringKey.FromString("http"),
        ByteStringKey.FromString("http2"),
        ByteStringKey.FromString("https"),
        ByteStringKey.FromString("inspector"),
        ByteStringKey.FromString("inspector/promises"),
        ByteStringKey.FromString("module"),
        ByteStringKey.FromString("net"),
        ByteStringKey.FromString("os"),
        ByteStringKey.FromString("path"),
        ByteStringKey.FromString("path/posix"),
        ByteStringKey.FromString("path/win32"),
        ByteStringKey.FromString("perf_hooks"),
        ByteStringKey.FromString("process"),
        ByteStringKey.FromString("punycode"),
        ByteStringKey.FromString("querystring"),
        ByteStringKey.FromString("readline"),
        ByteStringKey.FromString("readline/promises"),
        ByteStringKey.FromString("repl"),
        ByteStringKey.FromString("stream"),
        ByteStringKey.FromString("stream/consumers"),
        ByteStringKey.FromString("stream/promises"),
        ByteStringKey.FromString("stream/web"),
        ByteStringKey.FromString("string_decoder"),
        ByteStringKey.FromString("sys"),
        ByteStringKey.FromString("timers"),
        ByteStringKey.FromString("timers/promises"),
        ByteStringKey.FromString("tls"),
        ByteStringKey.FromString("trace_events"),
        ByteStringKey.FromString("tty"),
        ByteStringKey.FromString("url"),
        ByteStringKey.FromString("util"),
        ByteStringKey.FromString("util/types"),
        ByteStringKey.FromString("v8"),
        ByteStringKey.FromString("vm"),
        ByteStringKey.FromString("wasi"),
        ByteStringKey.FromString("worker_threads"),
        ByteStringKey.FromString("zlib"),
    };
    public static readonly HashSet<ByteStringKey> ExclusivelyPrefixedNodeCoreModules = new(ByteStringKey.Comparer)
    {
        ByteStringKey.FromString("node:quic"),
        ByteStringKey.FromString("node:sea"),
        ByteStringKey.FromString("node:sqlite"),
        ByteStringKey.FromString("node:test"),
        ByteStringKey.FromString("node:test/reporters"),
    };

    // nodemodules.go:75. Source once-initialized membership table; parser's
    // existing prefix-specific tables retain their separate identities.
    private static readonly Lazy<HashSet<ByteStringKey>> nodeCoreModules = new(() =>
    {
        var modules = new HashSet<ByteStringKey>(ByteStringKey.Comparer);
        foreach (var unprefixed in UnprefixedNodeCoreModules)
        {
            modules.Add(unprefixed);
            var prefixed = new GoBuilder();
            prefixed.WriteString("node:"u8);
            prefixed.WriteString(unprefixed.Value);
            modules.Add(ByteStringKey.FromValue(prefixed.Value()));
        }
        modules.UnionWith(ExclusivelyPrefixedNodeCoreModules);
        return modules;
    });

    public static HashSet<ByteStringKey> NodeCoreModules() => nodeCoreModules.Value;

    internal static bool ContainsNodeCoreModule(ByteStringValue name) => !name.IsNull && name.ContainsIn(NodeCoreModules());
    internal static bool ContainsUnprefixedNodeCoreModule(ByteStringValue name) => !name.IsNull && name.ContainsIn(UnprefixedNodeCoreModules);
    internal static bool ContainsExclusivelyPrefixedNodeCoreModule(ByteStringValue name) => !name.IsNull && name.ContainsIn(ExclusivelyPrefixedNodeCoreModules);

    public static string NonRelativeModuleNameForTypingCache(string moduleName) =>
        ContainsNodeCoreModule(ByteStringValue.FromString(moduleName)) ? "node" : moduleName;
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/ls/autoimport util_test.go, index_test.go and export_test.go assertions.
// Go T.Parallel hints are sequential here; explicit concurrent original tests remain separate.
using TypeScript.Scanner;
using TypeScript.LanguageServices.AutoImport;
using AutoImportApi = TypeScript.LanguageServices.AutoImport.AutoImport;

namespace LanguageServices.Checks;
public static partial class AutoImportOwnerChecks
{
    public static readonly string[] Names = ["TestWordIndices", "TestIndexClone", "TestModuleIDVariants"];
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
            case "TestWordIndices":
                WordIndices();
                break;
            case "TestIndexClone":
                IndexClone();
                break;
            case "TestModuleIDVariants":
                ModuleIDVariants();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown original autoimport function");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static ByteStringValue Bytes(string value) => ByteStringValue.FromString(value);
    private static void WordIndices()
    {
        (string Input, string[] Expected)[] cases = [("camelCase", ["camelCase", "Case"]), ("snake_case", ["snake_case", "case"]), ("ParseURL", ["ParseURL", "URL"]), ("XMLHttpRequest", ["XMLHttpRequest", "HttpRequest", "Request"]), ("hello", ["hello"]), ("HELLO", ["HELLO"]), ("parseHTML5Parser", ["parseHTML5Parser", "HTML5Parser", "Parser"]), ("__proto__", ["__proto__", "proto__"]), ("_private_member", ["_private_member", "member"]), ("a", ["a"]), ("A", ["A"]), ("test__double__underscore", ["test__double__underscore", "double__underscore", "underscore"])];
        foreach (var item in cases)
        {
            var input = Bytes(item.Input);
            var indices = AutoImportApi.WordIndices(input);
            Require(indices.Count == item.Expected.Length, item.Input + ": word count");
            for (int i = 0; i < indices.Count; i++)
                Require(input.Slice(indices[i]).EqualsValue(Bytes(item.Expected[i])), item.Input + ": word suffix");
        }
    }

    private sealed class TestEntry(string name, string package) : INamed
    {
        internal readonly string Package = package;
        private readonly ByteStringValue name = Bytes(name);
        public ByteStringValue Name() => name;
    }

    private static void IndexClone()
    {
        var original = new Index<TestEntry>();
        original.InsertAsWords(new("fooBar", "pkg-a"));
        original.InsertAsWords(new("bazQux", "pkg-b"));
        original.InsertAsWords(new("fooQux", "pkg-a"));
        var cloned = original.Clone(static entry => entry.Package != "pkg-b");
        Require(original.Entries.Count == 3, "Original retains all entries");
        Require(cloned.Entries.Count == 2, "Clone filters pkg-b");
        var results = cloned.Find(Bytes("fooBar"), true);
        Require(results.Count == 1, "Cloned exact search count");
        Require(results[0].Name().EqualsValue(Bytes("fooBar")), "Cloned exact name");
        Require(cloned.Find(Bytes("bazQux"), true).Count == 0, "Excluded name absent");
        Require(cloned.SearchWordPrefix(Bytes("foo")).Count == 2, "Cloned word prefix");
        Require(Index<TestEntry>.CloneNullable(null, static _ => true) == null, "Nil receiver clone");
        Require(new Index<TestEntry>().Clone(static _ => true).Entries.Count == 0, "Empty clone");
        var filtered = new Index<TestEntry>();
        filtered.InsertAsWords(new("fooBar", "pkg-a"));
        filtered.InsertAsWords(new("bazQux", "pkg-b"));
        var empty = filtered.Clone(static _ => false);
        Require(empty.Entries.Count == 0, "All entries filtered");
        Require(empty.IndexCount == 0, "All index entries filtered");
    }

    private static void ModuleIDVariants()
    {
        ModuleID zero = default;
        Require(!zero.AsPathKey().Ok, "Zero module has no path");
        Require(zero.AsValue().Length == 0, "Zero module string empty");
        Require(!zero.IsAmbient(), "Zero module not ambient");
        var path = PathKey.FromValue(Bytes("/project/src/a.ts"));
        var file = ModuleID.File(path);
        var(filePath, ok) = file.AsPathKey();
        Require(ok && filePath == path, "File module path preserved");
        Require(file.AsValue().EqualsValue(path.AsValue()), "File module text preserved");
        Require(!file.IsAmbient(), "File module not ambient");
        var ambient = ModuleID.Ambient(Bytes("node:fs"));
        Require(!ambient.AsPathKey().Ok, "Ambient module no file path");
        Require(ambient.AsValue().EqualsValue(Bytes("node:fs")), "Ambient text preserved");
        Require(ambient.IsAmbient(), "Ambient kind preserved");
    }
}

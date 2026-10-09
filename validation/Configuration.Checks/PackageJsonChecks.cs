// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Assertion-preserving ports of all seven packagejson unit functions at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Parallel scheduling is sequential.
// Test-only anonymous Go JSON structs use explicit field decoding through the
// same raw JSON and typed wrapper APIs; no arbitrary reflection codec is added.
using System.Text;
using TypeScript.Collections;
using TypeScript.Json;
using TypeScript.PackageJson;
using TypeScript.Scanner;

namespace Configuration.Checks;

public static class PackageJsonChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    [
        "TestPackageDirectory", "TestPackageDirectoryCacheIdentityPreservesPresentation",
        "TestForEachAncestorDirectoryStoppingAtGlobalCache", "TestParse",
        "TestExpected", "TestJSONValue", "TestExports"
    ];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestPackageDirectory": PackageDirectory(); break;
            case "TestPackageDirectoryCacheIdentityPreservesPresentation": CacheIdentity(); break;
            case "TestForEachAncestorDirectoryStoppingAtGlobalCache": Ancestors(); break;
            case "TestParse": Parse(); break;
            case "TestExpected": ExpectedFields(); break;
            case "TestJSONValue": JSONValues(); break;
            case "TestExports": Exports(); break;
            default: throw new InvalidOperationException(name);
        }
    }

    private static OrderedMap<string, object?> Raw(string text)
    {
        var (value, error) = JsonCodec.Parse(Encoding.UTF8.GetBytes(text));
        Check.True(error is null, "original json.Unmarshal nil error");
        return (OrderedMap<string, object?>)value!;
    }

    private static void PackageDirectory()
    {
        var directory = PackageDirectories.NewPackageDirectory(TsPath.RootedDirectoryPathFromAbsolute("/package/"), CaseSensitivity.CaseSensitive);
        Check.Equal(directory.String(), "/package", "String");
        Check.Equal(directory.AsDirectoryPath().AsString(), "/package", "AsDirectoryPath");
        Check.Equal(directory.PathKey().AsString(), "/package", "PathKey");
        Check.Equal(directory.Parent().String(), "/", "Parent.String");
        Check.Equal(directory.Parent().PathKey().AsString(), "/", "Parent.PathKey");
    }

    private static void CacheIdentity()
    {
        var cache = new InfoCache(CaseSensitivity.CaseInsensitive);
        var upper = cache.PackageDirectory(RootedDirectoryPath.FromGoString("/Repo/Package"));
        var lower = cache.PackageDirectory(RootedDirectoryPath.FromGoString("/repo/package"));
        Check.Equal(upper.PathKey(), lower.PathKey(), "same key");
        var entry = new InfoCacheEntry { PackageDirectory = upper, DirectoryExists = true, Contents = new PackageJson() };
        Check.True(ReferenceEquals(cache.Set(upper, entry), entry), "Set identity");
        Check.True(ReferenceEquals(cache.Get(lower), entry), "Get identity");
        var corrected = entry.WithPackageDirectory(lower);
        Check.Equal(corrected.PackageDirectory.AsDirectoryPath(), lower.AsDirectoryPath(), "corrected presentation");
        Check.Equal(corrected.PackageDirectory.PathKey(), upper.PathKey(), "corrected key");
        Check.Equal(entry.PackageDirectory.AsDirectoryPath(), upper.AsDirectoryPath(), "original presentation");
    }

    private static void Ancestors()
    {
        var cache = new InfoCache(CaseSensitivity.CaseInsensitive);
        var start = cache.PackageDirectory(RootedDirectoryPath.FromGoString("/Repo/Project/src"));
        List<string> names = [], keys = [];
        PackageDirectories.ForEachAncestorDirectoryStoppingAtGlobalCache<object?>(
            RootedDirectoryPath.FromGoString("/Repo"), start, directory =>
            {
                names.Add(directory.String());
                keys.Add(directory.PathKey().AsString());
                return (null, false);
            });
        Check.Sequence(names, ["/Repo/Project/src", "/Repo/Project", "/Repo"], "names");
        Check.Sequence(keys, ["/repo/project/src", "/repo/project", "/repo"], "keys");
    }

    private static void Parse()
    {
        var tests = new (string Name, string Content, Fields Expected)[]
        {
            ("duplicate names", """
                {
                    "name": "test-package",
                    "name": "test-package",
                    "version": "1.0.0"
                }
                """, new Fields { HeaderFields = new HeaderFields { Name = Expected<string>.Of("test-package"), Version = Expected<string>.Of("1.0.0") } }),
            ("content mapper", """
                {
                    "name": "test-package",
                    "typescript": {
                        "contentMapper": { "exec": ["mapper"], "dynamicConfig": true }
                    }
                }
                """, new Fields
                {
                    HeaderFields = new HeaderFields { Name = Expected<string>.Of("test-package") },
                    ContentMapper = Expected<ContentMapperFields>.Of(new ContentMapperFields
                    {
                        Exec = Expected<List<string>?>.Of(["mapper"]), DynamicConfig = Expected<bool>.Of(true)
                    })
                }),
            ("invalid typescript field is ignored", """{ "name": "test-package", "typescript": "invalid" }""",
                new Fields { HeaderFields = new HeaderFields { Name = Expected<string>.Of("test-package") } })
        };
        foreach (var test in tests)
        {
            var (got, error) = PackageJsonParser.ParseFields(Encoding.UTF8.GetBytes(test.Content));
            Check.True(error is null, test.Name + " nil error");
            // Original cmpopts.IgnoreUnexported: compare every exported field,
            // including default zero fields; private classification/cache state is ignored.
            StringExpected(got.Name, test.Expected.Name, test.Name + ".Name");
            StringExpected(got.Version, test.Expected.Version, test.Name + ".Version");
            StringExpected(got.Type, test.Expected.Type, test.Name + ".Type");
            StringExpected(got.TSConfig, test.Expected.TSConfig, test.Name + ".TSConfig");
            StringExpected(got.Main, test.Expected.Main, test.Name + ".Main");
            StringExpected(got.Types, test.Expected.Types, test.Name + ".Types");
            StringExpected(got.Typings, test.Expected.Typings, test.Name + ".Typings");
            Check.Equal(got.TypesVersions.Type, test.Expected.TypesVersions.Type, test.Name + ".TypesVersions.Type");
            Check.Equal(got.TypesVersions.Value, test.Expected.TypesVersions.Value, test.Name + ".TypesVersions.Value");
            Check.Equal(got.Imports.Type, test.Expected.Imports.Type, test.Name + ".Imports.Type");
            Check.Equal(got.Imports.Value, test.Expected.Imports.Value, test.Name + ".Imports.Value");
            Check.Equal(got.Exports.Type, test.Expected.Exports.Type, test.Name + ".Exports.Type");
            Check.Equal(got.Exports.Value, test.Expected.Exports.Value, test.Name + ".Exports.Value");
            DependencyExpected(got.Dependencies, test.Expected.Dependencies, test.Name + ".Dependencies");
            DependencyExpected(got.DevDependencies, test.Expected.DevDependencies, test.Name + ".DevDependencies");
            DependencyExpected(got.PeerDependencies, test.Expected.PeerDependencies, test.Name + ".PeerDependencies");
            DependencyExpected(got.OptionalDependencies, test.Expected.OptionalDependencies, test.Name + ".OptionalDependencies");
            Check.Equal(got.ContentMapper.Null, test.Expected.ContentMapper.Null, test.Name + ".ContentMapper.Null");
            Check.Equal(got.ContentMapper.Valid, test.Expected.ContentMapper.Valid, test.Name + ".ContentMapper.Valid");
            ListExpected(got.ContentMapper.Value.Exec, test.Expected.ContentMapper.Value.Exec, test.Name + ".Exec");
            ListExpected(got.ContentMapper.Value.CompilerOptions, test.Expected.ContentMapper.Value.CompilerOptions, test.Name + ".CompilerOptions");
            Check.Equal(got.ContentMapper.Value.DynamicConfig.Null, test.Expected.ContentMapper.Value.DynamicConfig.Null, test.Name + ".DynamicConfig.Null");
            Check.Equal(got.ContentMapper.Value.DynamicConfig.Valid, test.Expected.ContentMapper.Value.DynamicConfig.Valid, test.Name + ".DynamicConfig.Valid");
            Check.Equal(got.ContentMapper.Value.DynamicConfig.Value, test.Expected.ContentMapper.Value.DynamicConfig.Value, test.Name + ".DynamicConfig.Value");
        }
    }

    private static void StringExpected(Expected<string> actual, Expected<string> expected, string context)
    {
        Check.Equal(actual.Null, expected.Null, context + ".Null");
        Check.Equal(actual.Valid, expected.Valid, context + ".Valid");
        Check.Equal(actual.Value, expected.Value, context + ".Value");
    }

    private static void ListExpected(Expected<List<string>?> actual, Expected<List<string>?> expected, string context)
    {
        Check.Equal(actual.Null, expected.Null, context + ".Null");
        Check.Equal(actual.Valid, expected.Valid, context + ".Valid");
        Check.Sequence(actual.Value, expected.Value, context + ".Value");
    }

    private static void DependencyExpected(Expected<Dictionary<string, string>?> actual, Expected<Dictionary<string, string>?> expected, string context)
    {
        Check.Equal(actual.Null, expected.Null, context + ".Null");
        Check.Equal(actual.Valid, expected.Valid, context + ".Valid");
        Check.Equal(actual.Value, expected.Value, context + ".Value");
    }

    private static void ExpectedFields()
    {
        var raw = Raw("""{ "name": "test", "version": 2, "exports": null }""");
        static Expected<string> DecodeString(object? value) => Expected<string>.FromRaw(value, item => item is string text ? (text, true) : ("", false));
        var name = DecodeString(raw.GetOrZero("name"));
        var version = DecodeString(raw.GetOrZero("version"));
        var exports = Expected<object?>.FromRaw(raw.GetOrZero("exports"), item => (item, true));
        Expected<string> main = default;
        Check.Equal(name.Valid, true, "Name.Valid");
        Check.Equal(name.Value, "test", "Name.Value");
        Check.Equal(version.Valid, false, "Version.Valid");
        Check.Equal(version.Value, "", "Version.Value");
        Check.True(exports.Null, "Exports.Null");
        Check.Equal(exports.Valid, false, "Exports.Valid");
        Check.Equal(main.Valid, false, "Main.Valid");
        Check.Equal(main.Null, false, "Main.Null");
        Check.Equal(main.Value, "", "Main.Value");
    }

    private static void JSONValues()
    {
        var raw = Raw("""
            {
                "private": true, "false": false, "name": "test", "version": 2,
                "exports": {
                    ".": { "import": "./test.ts", "default": "./test.ts" },
                    "./test": ["./test1.ts", "./test2.ts", null], "./null": null
                }, "imports": null
            }
            """);
        JSONValue Field(string name) => raw.Has(name) ? JSONValue.FromRaw(raw.GetOrZero(name)) : default;
        var privateValue = Field("private");
        var name = Field("name");
        var version = Field("version");
        var exports = Field("exports");
        var imports = Field("imports");
        var notPresent = Field("notPresent");
        Check.Equal(privateValue.Type, JSONValueType.Boolean, "Private.Type");
        Check.Equal(privateValue.Value, true, "Private.Value");
        Check.Equal(name.Type, JSONValueType.String, "Name.Type");
        Check.Equal(name.Value, "test", "Name.Value");
        Check.Equal(version.Type, JSONValueType.Number, "Version.Type");
        Check.Equal(version.Value, 2d, "Version.Value");
        Check.Equal(exports.Type, JSONValueType.Object, "Exports.Type");
        Check.Equal(exports.AsObject().Size(), 3, "Exports.Size");
        Check.Equal(exports.AsObject().GetOrZero(".").Type, JSONValueType.Object, "dot.Type");
        Check.Equal(exports.AsObject().GetOrZero(".").AsObject().GetOrZero("import").Value, "./test.ts", "dot.import");
        var test = exports.AsObject().GetOrZero("./test");
        Check.Equal(test.Type, JSONValueType.Array, "test.Type");
        Check.Equal(test.AsArray()!.Count, 3, "test.Length");
        Check.Equal(test.AsArray()![0].Value, "./test1.ts", "test[0]");
        Check.Equal(test.AsArray()![1].Value, "./test2.ts", "test[1]");
        Check.Equal(test.AsArray()![2].Type, JSONValueType.Null, "test[2]");
        Check.Equal(exports.AsObject().GetOrZero("./null").Type, JSONValueType.Null, "null.Type");
        Check.Equal(imports.Type, JSONValueType.Null, "Imports.Type");
        Check.Equal(imports.Value, null, "Imports.Value");
        Check.Equal(notPresent.Type, JSONValueType.NotPresent, "NotPresent.Type");
        Check.Equal(notPresent.Value, null, "NotPresent.Value");
    }

    private static void Exports()
    {
        var raw = Raw("""
            {
                "imports": { "#foo": { "import": "./foo.ts" } },
                "exports": {
                    ".": { "import": "./test.ts", "default": "./test.ts" },
                    "./test": ["./test1.ts", "./test2.ts", null], "./null": null
                }
            }
            """);
        var imports = ExportsOrImports.FromRaw(raw.GetOrZero("imports"));
        var exports = ExportsOrImports.FromRaw(raw.GetOrZero("exports"));
        Check.True(exports.IsSubpaths(), "Exports.IsSubpaths");
        Check.Equal(exports.AsObject().Size(), 3, "Exports.Size");
        Check.True(exports.AsObject().GetOrZero(".").IsConditions(), "dot.IsConditions");
        Check.True(exports.AsObject().GetOrZero(".").AsObject().GetOrZero("import").Type == JSONValueType.String, "dot.import.Type");
        Check.Equal(exports.AsObject().GetOrZero("./test").AsArray()![2].Type, JSONValueType.Null, "test[2]");
        Check.True(exports.AsObject().GetOrZero("./null").Type == JSONValueType.Null, "null.Type");
        Check.True(imports.IsImports(), "Imports.IsImports");
        Check.Equal(imports.AsObject().Size(), 1, "Imports.Size");
        Check.True(imports.AsObject().GetOrZero("#foo").IsConditions(), "foo.IsConditions");
        Check.True(imports.AsObject().GetOrZero("#foo").AsObject().GetOrZero("import").Type == JSONValueType.String, "foo.import.Type");
    }
}

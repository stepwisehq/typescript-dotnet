// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete seven original content-mapper parsing tests in tsconfigparsing_test.go.
using TypeScript.ContentMapper;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using Options = TypeScript.TsOptions.TsOptions;

namespace Configuration.Checks;

public static class ConfigMapperChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    ["TestContentMappers", "TestContentMapperOptionDiagnosticLocation", "TestContentMappersAreInheritedFromExtendedConfig", "TestContentMappersRequireFlag", "TestUnresolvedContentMapperDoesNotRegisterExtensions", "TestContentMappersValidation", "TestContentMapperExtensionValidationUsesHostCaseSensitivity"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestContentMappers": Positive(); break;
            case "TestContentMapperOptionDiagnosticLocation": Location(); break;
            case "TestContentMappersAreInheritedFromExtendedConfig": Inherited(); break;
            case "TestContentMappersRequireFlag": Flag(); break;
            case "TestUnresolvedContentMapperDoesNotRegisterExtensions": Unresolved(); break;
            case "TestContentMappersValidation": Validation(); break;
            case "TestContentMapperExtensionValidationUsesHostCaseSensitivity": HostCase(); break;
            default: throw new InvalidOperationException("Unknown original mapper parsing test: " + name);
        }
    }

    private static ParsedCommandLine Parse(string text, (string Name, string Content)[] files, bool source, string directory = "/", bool enabled = true, CaseSensitivity sensitivity = CaseSensitivity.CaseSensitive)
    {
        string name = directory.TrimEnd('/') + "/tsconfig.json";
        ConfigurationFileSystem fs = new(files.Where(file => file.Name != name).Select(file => new ConfigurationFileSystem.Input(file.Name, file.Content)).Append(new(name, text)), sensitivity);
        var path = RootedFilePath.FromGoString(name);
        var key = sensitivity.PathKey(path.AsPath());
        var existing = enabled ? new CompilerOptions { RunExternalCode = Tristate.True } : null;
        if (source)
        {
            var config = Options.NewTsconfigSourceFileFromFilePath(path, key, text);
            return Options.ParseJsonSourceFileConfigFileContent(config, fs, RootedDirectoryPath.FromGoString(directory), existing);
        }
        var (raw, _) = Options.ParseConfigFileTextToJson(path, key, text);
        return Options.ParseJsonConfigFileContent(raw, fs, RootedDirectoryPath.FromGoString(directory), existing, path);
    }

    private const string VueManifest = """{ "name": "vue-mapper", "version": "1.2.3", "typescript": { "contentMapper": { "exec": ["node", "./mapper.js"], "dynamicConfig": true } } }""";
    private static IEnumerable<bool> Apis => new[] { false, true };
    private static void Positive()
    {
        const string text = """
            {
                "contentMappers": [
                    { "package": "vue-mapper", "extensions": [".vue"], "options": { "strictTemplates": true } }
                ],
                "include": ["src"]
            }
            """;
        foreach (bool source in Apis)
        {
            var parsed = Parse(text, [("/src/app.ts", "export {}"), ("/src/Component.vue", "<template></template>"), ("/node_modules/vue-mapper/package.json", VueManifest)], source);
            Check.Equal(parsed.Errors?.Count ?? 0, 0, "Mapper parse errors");
            var mappers = parsed.ContentMappers();
            Check.Equal(mappers!.Count, 1, "Mapper count");
            var mapper = mappers[0];
            Check.Equal(mapper.Package, "vue-mapper", "Mapper package");
            Check.Sequence(mapper.Extensions, new[] { ".vue" }, "Mapper definition extensions");
            Check.Equal(GoString.FromBytes(mapper.Options!.Bytes), "{\"strictTemplates\":true}", "Mapper raw options");
            Check.Sequence(parsed.ContentMapperExtensions(), new[] { ".vue" }, "Registered extensions");
            Check.Equal(mapper.Name, "vue-mapper", "Manifest name");
            Check.Equal(mapper.Version, "1.2.3", "Manifest version");
            Check.Sequence(mapper.Exec, new[] { "node", "./mapper.js" }, "Manifest exec");
            Check.True(mapper.DynamicConfig, "Manifest dynamicConfig");
            Check.Equal(mapper.PackageDirectory, RootedDirectoryPath.FromGoString("/node_modules/vue-mapper"), "Manifest directory");
            Check.True(parsed.FileNames()!.Contains(RootedFilePath.FromGoString("/src/Component.vue")), "Mapper-owned source discovered");
            Check.True(parsed.FileNames()!.Contains(RootedFilePath.FromGoString("/src/app.ts")), "Ordinary source discovered");
        }
    }

    private static void Location()
    {
        const string text = """
            {
                "contentMappers": [{
                    "package": "mapper",
                    "extensions": [".vue"],
                    "options": { "plugins": [{ "name": 1 }] }
                }]
            }
            """;
        var parsed = Parse(text, [("/index.ts", "export {};"), ("/node_modules/mapper/package.json", """{ "name": "mapper", "version": "1.0.0", "typescript": { "contentMapper": { "exec": ["mapper"] } } }""")], true);
        var (file, location) = Options.GetContentMapperOptionDiagnosticLocation(parsed, parsed.ContentMappers()![0], [new("plugins", 0, false), new("", 0, true), new("name", 0, false)]);
        Check.Equal(file!.Text()[location.Pos()..location.End()], "1", "Mapper option original syntax location");
    }

    private static void Inherited()
    {
        foreach (bool source in Apis)
        {
            var parsed = Parse("""{ "extends": "./base.json" }""", [("/project/base.json", """{ "contentMappers": [{ "package": "vue-mapper", "extensions": [".vue"] }], "include": ["src"] }"""), ("/project/src/index.ts", "export {};"), ("/project/src/component.vue", "<template></template>"), ("/project/node_modules/vue-mapper/package.json", """{ "name": "vue-mapper", "version": "1.2.3", "typescript": { "contentMapper": { "exec": ["node", "./mapper.js"] } } }""")], source, "/project");
            Check.Equal(parsed.Errors?.Count ?? 0, 0, "Inherited mapper errors");
            Check.Equal(parsed.ContentMappers()!.Count, 1, "Inherited mapper count");
            Check.Equal(parsed.ContentMappers()![0].Package, "vue-mapper", "Inherited package");
            Check.Sequence(parsed.ContentMapperExtensions(), new[] { ".vue" }, "Inherited extensions");
            Check.True(parsed.FileNames()!.Contains(RootedFilePath.FromGoString("/project/src/component.vue")), "Inherited include registers mapper extension");
        }
    }

    private static void Flag()
    {
        foreach (bool source in Apis)
        {
            var parsed = Parse("""{ "contentMappers": [{ "package": "vue-mapper", "extensions": [".vue"] }] }""", [("/app.ts", "export {}")], source, enabled: false);
            Check.True(parsed.Errors?.Any(error => error.Code() == Diagnostics.Content_mappers_require_the_runExternalCode_command_line_flag_to_be_enabled.Code()) == true, "Missing runExternalCode diagnostic");
        }
    }

    private static void Unresolved()
    {
        foreach (bool source in Apis)
        {
            var parsed = Parse("""{ "contentMappers": [{ "package": "missing-mapper", "extensions": [".vue"] }], "include": ["src"] }""", [("/src/app.ts", "export {}"), ("/src/Component.vue", "<template />")], source);
            Check.Equal(parsed.ContentMappers()?.Count ?? 0, 0, "Unresolved mapper count");
            Check.Equal(parsed.ContentMapperExtensions()?.Count ?? 0, 0, "Unresolved mapper extension count");
            Check.True(!parsed.FileNames()!.Contains(RootedFilePath.FromGoString("/src/Component.vue")), "Unresolved mapper does not register extension");
            Check.True(parsed.FileNames()!.Contains(RootedFilePath.FromGoString("/src/app.ts")), "Ordinary source still discovered");
        }
    }

    private static readonly (string Name, string Json, int Code)[] validationCases =
    [
        ("extension without leading dot", """[{ "package": "vue-mapper", "extensions": ["vue"] }]""", Diagnostics.Content_mapper_file_extension_0_must_begin_with_a.Code()),
        ("built-in extension", """[{ "package": "x", "extensions": [".ts"] }]""", Diagnostics.Content_mapper_file_extension_0_is_a_built_in_extension_and_cannot_be_registered_by_a_content_mapper.Code()),
        ("missing extensions", """[{ "package": "x" }]""", Diagnostics.Compiler_option_0_requires_a_value_of_type_1.Code()),
        ("duplicate extension across mappers", """[{ "package": "a", "extensions": [".vue"] }, { "package": "b", "extensions": [".vue"] }]""", Diagnostics.Content_mapper_file_extension_0_is_registered_by_more_than_one_content_mapper.Code()),
        ("extensions is not an array", """[{ "package": "x", "extensions": ".vue" }]""", Diagnostics.Compiler_option_0_requires_a_value_of_type_1.Code()),
        ("extensions contains a non-string", """[{ "package": "x", "extensions": [".vue", 1] }]""", Diagnostics.Compiler_option_0_requires_a_value_of_type_1.Code()),
        ("package is not a string", """[{ "package": ["x"], "extensions": [".vue"] }]""", Diagnostics.Compiler_option_0_requires_a_value_of_type_1.Code()),
        ("missing package", """[{ "extensions": [".vue"] }]""", Diagnostics.Compiler_option_0_requires_a_value_of_type_1.Code()),
        ("options is not an object", """[{ "package": "x", "extensions": [".vue"], "options": ["strict"] }]""", Diagnostics.Compiler_option_0_requires_a_value_of_type_1.Code())
    ];

    private static string Manifest(string name) => "{ \"name\": \"" + name + "\", \"version\": \"1.0.0\", \"typescript\": { \"contentMapper\": { \"exec\": [\"" + name + "\"] } } }";
    private static void Validation()
    {
        foreach (var row in validationCases)
        foreach (bool source in Apis)
        {
            var files = new List<(string, string)> { ("/app.ts", "export {}") };
            if (row.Name == "duplicate extension across mappers")
            {
                files.Add(("/node_modules/a/package.json", Manifest("a")));
                files.Add(("/node_modules/b/package.json", Manifest("b")));
            }
            var parsed = Parse("{ \"contentMappers\": " + row.Json + " }", files.ToArray(), source);
            var diagnostic = parsed.Errors?.Find(error => error.Code() == row.Code);
            Check.True(diagnostic is not null, row.Name + " original diagnostic present");
            if (row.Name == "built-in extension")
            {
                Check.Equal(parsed.ContentMappers()?.Count ?? 0, 0, "Built-in mapper count");
                Check.Equal(parsed.ContentMapperExtensions()?.Count ?? 0, 0, "Built-in extensions count");
            }
            else if (row.Name == "duplicate extension across mappers")
            {
                Check.Equal(parsed.ContentMappers()!.Count, 2, "Duplicate mapper count");
                Check.Sequence(parsed.ContentMappers()![0].Extensions, new[] { ".vue" }, "First extension retained");
                Check.Equal(parsed.ContentMappers()![1].Extensions?.Count ?? 0, 0, "Second extension removed");
                Check.Sequence(parsed.ContentMapperExtensions(), new[] { ".vue" }, "Deduplicated registered extension");
            }
            else if (row.Name is "missing extensions" or "extensions is not an array" or "extensions contains a non-string" or "package is not a string" or "missing package" or "options is not an object")
                Check.Equal(parsed.ContentMappers()?.Count ?? 0, 0, "Invalid mapper omitted");
            if (source)
            {
                Check.True(diagnostic!.File() is not null, row.Name + " diagnostic source retained");
                Check.True(diagnostic.Len() > 0, row.Name + " nonempty diagnostic syntax range");
            }
        }
    }

    private static void HostCase()
    {
        int builtin = Diagnostics.Content_mapper_file_extension_0_is_a_built_in_extension_and_cannot_be_registered_by_a_content_mapper.Code();
        int duplicate = Diagnostics.Content_mapper_file_extension_0_is_registered_by_more_than_one_content_mapper.Code();
        (CaseSensitivity Sensitivity, string Json, int Code)[] rows =
        [
            (CaseSensitivity.CaseInsensitive, """[{ "package": "mapper", "extensions": [".TS"] }]""", builtin),
            (CaseSensitivity.CaseInsensitive, """[{ "package": "a", "extensions": [".vue"] }, { "package": "b", "extensions": [".VUE"] }]""", duplicate),
            (CaseSensitivity.CaseSensitive, """[{ "package": "mapper", "extensions": [".TS"] }]""", builtin),
            (CaseSensitivity.CaseSensitive, """[{ "package": "a", "extensions": [".vue"] }, { "package": "b", "extensions": [".VUE"] }]""", 0)
        ];
        foreach (var row in rows)
        {
            var parsed = Parse("{ \"contentMappers\": " + row.Json + " }", [("/app.ts", "export {};"), ("/node_modules/mapper/package.json", Manifest("mapper")), ("/node_modules/a/package.json", Manifest("a")), ("/node_modules/b/package.json", Manifest("b"))], true, sensitivity: row.Sensitivity);
            if (row.Code == 0) Check.Equal(parsed.Errors?.Count ?? 0, 0, "Case-distinct mapper extensions no errors");
            else Check.True(parsed.Errors?.Any(error => error.Code() == row.Code) == true, "Original host-case diagnostic present");
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original direct assertions from tsconfigparsing_test.go.
using TypeScript.Collections;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Vfs;
using Options = TypeScript.TsOptions.TsOptions;

namespace Configuration.Checks;
public static class ConfigDirectChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestParseJsonConfigFileContentAcceptsJsonRepresentations", "TestParseJsonConfigFileContentPreservesRaw", "TestParseJsonConfigFileContentHandlesNullArrayElements", "TestParseJsonConfigFileContentDefaultsCompileOnSaveToFalse", "TestParseJsonSourceFileConfigFileContentReportsInvalidExtendedConfig", "TestParseJsonSourceFileConfigFileContentWithEmptyExtendedConfig", "TestParseJsonSourceFileConfigFileContentDoesNotDuplicateUnquotedKeyDiagnostics", "TestParseJsonSourceFileConfigFileContentReportsQuestionTokenDiagnostics", "TestParseNullEnumCompilerOptions", "TestExtendedConfigErrorsAppearOnCacheHit", "TestExtendedConfigConfigDirPathsAreNotCached", "TestParseSrcCompiler"];

    public static void Run(string name)
    {
        var fs = new ConfigurationFileSystem([new("/project/index.ts", "export {};")]);
        switch (name)
        {
            case "TestParseJsonConfigFileContentAcceptsJsonRepresentations":
                var(ordered, errors) = Options.ParseConfigFileTextToJson(File("/project/tsconfig.json"), PathKey.FromGoString("/project/tsconfig.json"), """{"compilerOptions":{"strict":true},"files":["index.ts"]}""");
                Check.Equal(errors.Count, 0, "Representation source parse errors");
                OrderedMap<string, object?> typedOrdered = new();
                typedOrdered.Set("compilerOptions", new Dictionary<string, object?> { ["strict"] = true });
                typedOrdered.Set("files", new List<string> { "index.ts" });
                object? [] forms = [ordered, typedOrdered, new Dictionary<string, object?>
                {
                    ["compilerOptions"] = new Dictionary<string, object?>
                    {
                        ["strict"] = true
                    },
                    ["files"] = new List<object?>
                    {
                        "index.ts"
                    }
                }, new Dictionary<string, object?>
                {
                    ["compilerOptions"] = new Dictionary<string, object?>
                    {
                        ["strict"] = true
                    },
                    ["files"] = new List<string>
                    {
                        "index.ts"
                    }
                }

                ];
                foreach (var form in forms)
                {
                    var parsed = Json(form, fs);
                    Check.Sequence(parsed.FileNames(), new[] { File("/project/index.ts") }, "All original JSON representations file list");
                    Check.True(parsed.CompilerOptions().Strict == Tristate.True, "All original JSON representations strict");
                    Check.Equal(GoSlices.ToListOrNull(parsed.Errors)?.Count ?? 0, 0, "All original JSON representations errors");
                }

                break;
            case "TestParseJsonConfigFileContentPreservesRaw":
                var rawParsed = Json(new Dictionary<string, object?> { ["files"] = new List<object?> { "index.ts" }, ["customSetting"] = new Dictionary<string, object?> { ["enabled"] = true }, ["compileOnSave"] = true }, fs);
                Check.Equal(GoSlices.ToListOrNull(rawParsed.Errors)?.Count ?? 0, 0, "Raw preservation errors");
                Check.True(rawParsed.CompileOnSave?.Value == true, "Actual compileOnSave pointer true");
                Check.True(rawParsed.Raw is OrderedMap<string, object?>, "Raw is original normalized ordered map");
                var raw = (OrderedMap<string, object?>)rawParsed.Raw!;
                Check.Sequence(raw.Keys().ToList(), new[] { "compileOnSave", "customSetting", "files" }, "Raw normalized key order");
                Check.True(raw.Has("customSetting"), "Raw custom setting preserved");
                break;
            case "TestParseJsonConfigFileContentHandlesNullArrayElements":
                foreach (string property in new[]
                {
                    "files",
                    "include",
                    "exclude"
                }

                )
                {
                    var parsed = Json(new Dictionary<string, object?> { [property] = new List<object?> { null } }, fs);
                    Check.True(GoSlices.ToListOrNull(parsed.Errors)?.Count > 0, property + " null element diagnostic present");
                    Check.Equal(parsed.Errors![0].Code(), Diagnostics.Compiler_option_0_requires_a_value_of_type_1.Code(), property + " null element diagnostic code");
                }

                break;
            case "TestParseJsonConfigFileContentDefaultsCompileOnSaveToFalse":
                var defaults = Json(new Dictionary<string, object?> { ["files"] = new List<object?> { "index.ts" } }, fs);
                Check.True(defaults.CompileOnSave is not null, "CompileOnSave default pointer allocated");
                Check.Equal(defaults.CompileOnSave!.Value, false, "CompileOnSave default value");
                break;
            case "TestParseJsonSourceFileConfigFileContentReportsInvalidExtendedConfig":
                var invalidFiles = new ConfigurationFileSystem([new("/project/tsconfig.json", "{\n  \"extends\": \"./bad.json\"\n}"), new("/project/bad.json", "{ this is not json"), new("/project/main.ts", "export const x = 1;")]);
                var invalid = Source(invalidFiles, "/project/tsconfig.json", "/project");
                var parseErrors = (GoSlices.ToListOrNull(invalid.Errors) ?? []).Where(error => error.Code() == Diagnostics.X_0_expected.Code()).ToList();
                Check.Equal(parseErrors.Count, 4, "Extended config recovered parse error count");
                Check.Sequence(parseErrors.Select(error => error.MessageArgs()[0]).ToList(), new[] { ":", ",", ",", "}" }, "Extended config token arguments");
                Check.Sequence(parseErrors.Select(error => error.Pos()).ToList(), new[] { 7, 10, 14, 18 }, "Extended config token positions");
                foreach (var error in parseErrors)
                    Check.Equal(error.File()!.FileName(), "/project/bad.json", "Extended config diagnostic owner");
                break;
            case "TestParseJsonSourceFileConfigFileContentWithEmptyExtendedConfig":
                var emptyFiles = new ConfigurationFileSystem([new("/project/tsconfig.json", "{\n  \"extends\": \"./base.json\"\n}"), new("/project/base.json", ""), new("/project/main.ts", "export const x = 1;")]);
                var empty = Source(emptyFiles, "/project/tsconfig.json", "/project");
                Check.True(empty is not null, "Empty base does not panic and returns parsed line");
                Check.Sequence(empty!.FileNames(), new[] { File("/project/main.ts") }, "Empty base discovery");
                break;
            case "TestParseJsonSourceFileConfigFileContentDoesNotDuplicateUnquotedKeyDiagnostics":
                Locations("{\n  compilerOptions: {\n    strict: true\n  }\n}", Diagnostics.String_literal_with_double_quotes_expected.Code(), [(1, 2), (2, 4)], false);
                break;
            case "TestParseJsonSourceFileConfigFileContentReportsQuestionTokenDiagnostics":
                Locations("{\n  compilerOptions?: {\n    strict?: true\n  }\n}", Diagnostics.The_0_modifier_can_only_be_used_in_TypeScript_files.Code(), [(1, 17), (2, 10)], true);
                break;
            case "TestParseNullEnumCompilerOptions":
                const string text = "{\n\t\t\t\"compilerOptions\": {\n\t\t\t\t\"target\": null,\n\t\t\t\t\"module\": null\n\t\t\t}\n\t\t}";
                var nullFiles = new ConfigurationFileSystem([new("/app.ts"), new("/tsconfig.json", text)]);
                var source = Source(nullFiles, "/tsconfig.json", "/");
                var(value, _) = Options.ParseConfigFileTextToJson(File("/tsconfig.json"), PathKey.FromGoString("/tsconfig.json"), text);
                var json = Options.ParseJsonConfigFileContent(value, nullFiles, Dir("/"), null, File("/tsconfig.json"));
                Check.Equal(GoSlices.ToListOrNull(source.Errors)?.Count ?? 0, 0, "Source null enum errors");
                Check.Equal(GoSlices.ToListOrNull(json.Errors)?.Count ?? 0, 0, "JSON null enum errors");
                break;
            case "TestExtendedConfigErrorsAppearOnCacheHit":
                CacheErrors();
                break;
            case "TestExtendedConfigConfigDirPathsAreNotCached":
                CachePaths();
                break;
            case "TestParseSrcCompiler":
                SourceCompiler();
                break;
            default:
                throw new InvalidOperationException("Unknown original config direct test: " + name);
        }
    }

    private static void SourceCompiler()
    {
        string? root = Environment.CurrentDirectory;
        while (root is not null && !System.IO.File.Exists(System.IO.Path.Combine(root, "upstream/TypeScript/tsc/go.mod"))) root = System.IO.Path.GetDirectoryName(root);
        Check.True(root is not null, "Pinned original compiler fixture repository exists");
        var directory = RootedDirectoryPath.FromUnicode(System.IO.Path.Combine(root!, "upstream/TypeScript/tsc/testdata/fixtures/compiler"));
        var config = directory.ResolveFile("tsconfig.json");
        var fs = TypeScript.Vfs.OsVfs.OsVfs.FS();
        var (text, ok) = fs.ReadFile(config);
        Check.True(ok, "Original compiler fixture config read");
        var source = Options.NewTsconfigSourceFileFromFilePath(config, fs.CaseSensitivity().PathKey(config.AsPath()), text);
        var parsed = Options.ParseJsonSourceFileConfigFileContent(source, fs, directory);
        Check.Equal(GoSlices.ToListOrNull(parsed.Errors)?.Count ?? 0, 0, "Original compiler fixture errors");
        var options = parsed.CompilerOptions();
        Check.Sequence(options.Types, Array.Empty<string>(), "Original compiler fixture types allocated empty");
        Check.Equal(options.Module, ModuleKind.NodeNext, "Original compiler fixture module");
        Check.Equal(options.ModuleResolution, ModuleResolutionKind.NodeNext, "Original compiler fixture resolution");
        Check.Equal(options.Target, ScriptTarget.ES2020, "Original compiler fixture target");
        Check.Equal(parsed.FileNames()!.Count, 79, "Original compiler fixture file count");
        foreach (string file in new[] { "checker.ts", "diagnosticInformationMap.generated.ts", "node.d.ts", "program.ts" })
            Check.True(parsed.FileNames()!.Contains(options.ConfigFilePath.Directory().ResolveFile(file)), "Original compiler fixture contains " + file);
    }

    private static RootedFilePath File(string value) => RootedFilePath.FromGoString(value);
    private static RootedDirectoryPath Dir(string value) => RootedDirectoryPath.FromGoString(value);
    private static ParsedCommandLine Json(object? raw, ConfigurationFileSystem fs) => Options.ParseJsonConfigFileContent(raw, fs, Dir("/project"), null, File("/project/tsconfig.json"));
    private static ParsedCommandLine Source(ConfigurationFileSystem fs, string file, string basePath, IExtendedConfigCache? cache = null)
    {
        var(text, ok) = fs.ReadFile(File(file));
        Check.True(ok, "Original fixture config read " + file);
        var source = Options.NewTsconfigSourceFileFromFilePath(File(file), fs.CaseSensitivity().PathKey(RootedPath.FromGoString(file)), text);
        return Options.ParseJsonSourceFileConfigFileContent(source, fs, Dir(basePath), null, null, null, cache);
    }

    private static void Locations(string text, int code, (int Line, int Character)[] expected, bool filtered)
    {
        var fs = new ConfigurationFileSystem([new("/main.ts", "export const x = 1;"), new("/tsconfig.json", text)]);
        var parsed = Source(fs, "/tsconfig.json", "/");
        var diagnostics = GoSlices.ToListOrNull(parsed.GetConfigFileParsingDiagnostics()) ?? [];
        if (filtered)
            diagnostics = diagnostics.Where(error => error.Code() == code).ToList();
        Check.Equal(diagnostics.Count, 2, "Original property diagnostic count");
        for (int i = 0; i < diagnostics.Count; i++)
        {
            if (!filtered)
                Check.Equal(diagnostics[i].Code(), code, "Unquoted property diagnostic code");
            var(line, character) = TypeScript.Scanner.Scanner.GetECMALineAndUTF16CharacterOfPosition(diagnostics[i].File()!, diagnostics[i].Pos());
            Check.Equal(line, expected[i].Line, "Property diagnostic line");
            Check.Equal(character, expected[i].Character, "Property diagnostic UTF16 column");
        }
    }

    private sealed class MemoCache : IExtendedConfigCache
    {
        private Dictionary<PathKey, ExtendedConfigCacheEntry>? entries;
        public ExtendedConfigCacheEntry GetExtendedConfig(RootedFilePath file, PathKey path, List<PathKey>? stack, IFS fs)
        {
            entries ??= [];
            if (entries.TryGetValue(path, out var entry))
                return entry;
            entry = Options.ParseExtendedConfig(file, path, stack, fs, this);
            entries[path] = entry;
            return entry;
        }
    }

    private static void CacheErrors()
    {
        var same = new ConfigurationFileSystem([new("/tsconfig.json", "{\n  \"extends\": \"./base.json\"\n}"), new("/base.json", "{\n  \"excludes\": [\"**/*.ts\"]\n}"), new("/app.ts", "export {}")]);
        var cache = new MemoCache();
        Check.True(GoSlices.ToListOrNull(Source(same, "/tsconfig.json", "/", cache).Errors)?.Count > 0, "First base diagnostics");
        Check.True(GoSlices.ToListOrNull(Source(same, "/tsconfig.json", "/", cache).Errors)?.Count > 0, "Repeated cached base diagnostics");
        var shared = new ConfigurationFileSystem([new("/base.json", "{\n  \"excludes\": [\"**/*.ts\"]\n}"), new("/projA/tsconfig.json", "{\n  \"extends\": \"../base.json\"\n}"), new("/projB/tsconfig.json", "{\n  \"extends\": \"../base.json\"\n}"), new("/projA/app.ts", "export {}"), new("/projB/app.ts", "export {}")]);
        cache = new();
        Check.True(GoSlices.ToListOrNull(Source(shared, "/projA/tsconfig.json", "/", cache).Errors)?.Count > 0, "First project base diagnostics");
        Check.True(GoSlices.ToListOrNull(Source(shared, "/projB/tsconfig.json", "/", cache).Errors)?.Count > 0, "Second project cached base diagnostics");
    }

    private static void CachePaths()
    {
        var fs = new ConfigurationFileSystem([new("/tsconfig.base.json", "{\n  \"compilerOptions\": {\n    \"paths\": {\n      \"@pkg/*\": [\"${configDir}/src/*\"]\n    }\n  }\n}"), new("/packages/a/tsconfig.json", "{\n  \"extends\": \"../../tsconfig.base.json\"\n}"), new("/packages/b/tsconfig.json", "{\n  \"extends\": \"../../tsconfig.base.json\"\n}"), new("/packages/a/index.ts", "export {}"), new("/packages/b/index.ts", "export {}")]);
        var cache = new MemoCache();
        var(first, firstErrors) = Options.GetParsedCommandLineOfConfigFile(File("/packages/a/tsconfig.json"), null, null, fs, cache);
        Check.Equal(firstErrors.Count, 0, "First cache path read errors");
        var(second, secondErrors) = Options.GetParsedCommandLineOfConfigFile(File("/packages/b/tsconfig.json"), null, null, fs, cache);
        Check.Equal(secondErrors.Count, 0, "Second cache path read errors");
        Check.Sequence(second!.CompilerOptions().Paths!.GetOrZero("@pkg/*"), new[] { "/packages/b/src/*" }, "configDir expansion not cached across projects");
    }
}

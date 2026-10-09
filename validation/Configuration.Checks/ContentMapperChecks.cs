// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete original contentmappers_test.go assertions over real metadata APIs.
using TypeScript.ContentMapper;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using Options = TypeScript.TsOptions.TsOptions;

namespace Configuration.Checks;

public static class ContentMapperChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    ["TestGetContentMapperForFileNameUsesLongestExtension", "TestGetContentMapperForFileNameUsesHostCaseSensitivity", "TestGetOutputFileNamesExcludesMapperOwnedOutputs", "TestResolveContentMapperManifest"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestGetContentMapperForFileNameUsesLongestExtension": LongestExtension(); break;
            case "TestGetContentMapperForFileNameUsesHostCaseSensitivity": HostCase(); break;
            case "TestGetOutputFileNamesExcludesMapperOwnedOutputs": MapperOutputs(); break;
            case "TestResolveContentMapperManifest": Manifest(); break;
            default: throw new InvalidOperationException("Unknown content mapper test: " + name);
        }
    }

    private static void LongestExtension()
    {
        Mapper z = new() { Package = "z", Extensions = [".z"] }, yz = new() { Package = "yz", Extensions = [".y.z"] };
        ParsedCommandLine command = new() { ParsedConfig = new() { ContentMappers = [z, yz] } };
        Check.True(ReferenceEquals(command.GetContentMapperForFileName(RootedFilePath.FromGoString("/src/Component.y.z")), yz), "Longest extension mapper identity");
        Check.True(ReferenceEquals(command.GetContentMapperForFileName(RootedFilePath.FromGoString("/src/Component.z")), z), "Short extension mapper identity");
    }

    private static void HostCase()
    {
        Mapper mapper = new() { Extensions = [".vue"] };
        ParsedCommandLine insensitive = new() { ParsedConfig = new() { ContentMappers = [mapper] }, caseSensitivity = CaseSensitivity.CaseInsensitive };
        ParsedCommandLine sensitive = new() { ParsedConfig = new() { ContentMappers = [mapper] }, caseSensitivity = CaseSensitivity.CaseSensitive };
        var file = RootedFilePath.FromGoString("/src/Component.VUE");
        Check.True(ReferenceEquals(insensitive.GetContentMapperForFileName(file), mapper), "Insensitive host mapper identity");
        Check.True(sensitive.GetContentMapperForFileName(file) is null, "Sensitive host no mapper");
    }

    private static void MapperOutputs()
    {
        Mapper mapper = new() { Extensions = [".vue"] };
        ParsedCommandLine command = Options.NewParsedCommandLine(new()
        {
            OutDir = RootedDirectoryPath.FromGoString("/dist"), Declaration = Tristate.True,
            DeclarationMap = Tristate.True, SourceMap = Tristate.True
        }, [RootedFilePath.FromGoString("/src/Component.vue")], null, RootedDirectoryPath.FromGoString("/"), CaseSensitivity.CaseSensitive);
        command.ParsedConfig.ContentMappers = [mapper];
        Check.Sequence(command.GetOutputFileNames().ToList(), new[] { RootedFilePath.FromGoString("/dist/Component.d.vue.ts") }, "Mapper-owned output omission");
    }

    private static void Manifest()
    {
        ConfigurationFileSystem fs = new(new[]
        {
            new ConfigurationFileSystem.Input("/home/project/node_modules/vue-ts-mapper/package.json", """
                {"name":"vue-ts-mapper","version":"1.2.3","typescript":{"contentMapper":{"exec":["node","./dist/mapper.js"],"compilerOptions":["target","jsx"]}}}
                """),
            new ConfigurationFileSystem.Input("/home/node_modules/@scope/noversion/package.json", """
                {"name":"@scope/noversion","typescript":{"contentMapper":{"exec":["run"]}}}
                """),
            new ConfigurationFileSystem.Input("/home/project/node_modules/no-name/package.json", """{"version":"1.0.0"}"""),
            new ConfigurationFileSystem.Input("/home/project/node_modules/no-manifest/package.json", """{"name":"no-manifest"}"""),
            new ConfigurationFileSystem.Input("/home/project/node_modules/no-exec/package.json", """{"name":"no-exec","typescript":{"contentMapper":{}}}"""),
            new ConfigurationFileSystem.Input("/home/project/node_modules/bad-exec/package.json", """{"name":"bad-exec","typescript":{"contentMapper":{"exec":"node ./mapper.js"}}}""")
        }, CaseSensitivity.CaseSensitive);
        var config = RootedFilePath.FromGoString("/home/project/tsconfig.json");
        var (manifest, directory, diagnostic) = Options.resolveContentMapperManifest(fs, config, "vue-ts-mapper");
        Check.True(diagnostic is null, "Manifest diagnostic nil");
        Check.Equal(manifest.Name, "vue-ts-mapper", "Manifest name");
        Check.Equal(manifest.Version, "1.2.3", "Manifest version");
        Check.Equal(directory, RootedDirectoryPath.FromGoString("/home/project/node_modules/vue-ts-mapper"), "Manifest package directory");
        Check.Sequence(manifest.Exec, new[] { "node", "./dist/mapper.js" }, "Verbatim exec argv");
        Check.Sequence(manifest.CompilerOptions, new[] { "target", "jsx" }, "Manifest compiler options");
        (manifest, _, diagnostic) = Options.resolveContentMapperManifest(fs, RootedFilePath.FromGoString("/home/project/src/tsconfig.json"), "@scope/noversion");
        Check.True(diagnostic is null, "Ancestor package resolution diagnostic nil");
        Check.Equal(manifest.Name, "@scope/noversion", "Versionless name");
        Check.Equal(manifest.Version, "", "Versionless version");
        (_, _, diagnostic) = Options.resolveContentMapperManifest(fs, config, "missing-mapper");
        Check.True(diagnostic is not null, "Missing package diagnostic present");
        Check.Equal(diagnostic!.Code(), Diagnostics.The_content_mapper_package_0_could_not_be_resolved.Code(), "Missing package diagnostic code");
        (_, directory, diagnostic) = Options.resolveContentMapperManifest(fs, config, "no-name");
        Check.True(diagnostic is not null, "Missing package name diagnostic present");
        Check.Equal(directory, RootedDirectoryPath.FromGoString("/home/project/node_modules/no-name"), "Unnamed package directory");
        Check.Equal(diagnostic!.Code(), Diagnostics.The_package_json_of_the_content_mapper_package_0_does_not_specify_a_name.Code(), "Unnamed package code");
        (_, _, diagnostic) = Options.resolveContentMapperManifest(fs, config, "no-manifest");
        Check.True(diagnostic is not null, "Missing content mapper diagnostic present");
        Check.Equal(diagnostic!.Code(), Diagnostics.The_package_json_of_the_content_mapper_package_0_does_not_declare_a_typescript_contentMapper_object.Code(), "Missing content mapper code");
        foreach (string package in new[] { "no-exec", "bad-exec" })
        {
            (_, _, diagnostic) = Options.resolveContentMapperManifest(fs, config, package);
            Check.True(diagnostic is not null, package + " exec diagnostic present");
            Check.Equal(diagnostic!.Code(), Diagnostics.The_typescript_contentMapper_exec_of_the_content_mapper_package_0_must_be_a_non_empty_array_of_strings.Code(), package + " exec diagnostic code");
        }
    }
}

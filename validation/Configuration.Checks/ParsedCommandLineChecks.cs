// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete parsedcommandline_test.go body; only parallel scheduling and fixture
// construction are adapted to the shared read-only virtual host.
using TypeScript.Scanner;
using TypeScript.TsOptions;
using Options = TypeScript.TsOptions.TsOptions;

namespace Configuration.Checks;
public static class ParsedCommandLineChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestParsedCommandLine"];

    public static void Run(string name)
    {
        if (name != "TestParsedCommandLine")
            throw new InvalidOperationException("Unknown parsed command line test: " + name);
        string[] paths = ["/dev/a.ts", "/dev/a.d.ts", "/dev/a.js", "/dev/b.ts", "/dev/b.js", "/dev/c.d.ts", "/dev/z/a.ts", "/dev/z/abz.ts", "/dev/z/aba.ts", "/dev/z/b.ts", "/dev/z/bbz.ts", "/dev/z/bba.ts", "/dev/x/a.ts", "/dev/x/aa.ts", "/dev/x/b.ts", "/dev/x/y/a.ts", "/dev/x/y/b.ts", "/dev/js/a.js", "/dev/js/b.js", "/dev/js/d.min.js", "/dev/js/ab.min.js", "/ext/ext.ts", "/ext/b/a..b.ts"];
        var files = new ConfigurationFileSystem(paths.Select(path => new ConfigurationFileSystem.Input(path)));
        var noFiles = new ConfigurationFileSystem([]);
        string[] expected = ["/dev/a.ts", "/dev/b.ts"];
        var literal = Parse("""{"files":["a.ts","b.ts"]}""", files);
        Matches(literal, paths, expected, "Literal files");
        var excluded = Parse("""{"files":["a.ts","b.ts"],"exclude":["b.ts"]}""", files);
        Matches(excluded, paths, expected, "Literal files ignore excludes");
        Matches(excluded.ReloadFileNamesOfParsedCommandLine(noFiles), [], expected, "Literal names survive reload");
        var duplicates = Parse("""{"files":["a.ts","a.ts","b.ts",]}""", files);
        Check.Sequence(duplicates.LiteralFileNames(), expected.Select(RootedFilePath.FromGoString).ToArray(), "Literal duplicate removal");
        var included = Parse("""{"include":["a.ts","b.ts"]}""", files);
        Matches(included, paths, expected, "Literal include");
        Matches(included.ReloadFileNamesOfParsedCommandLine(noFiles), [], expected, "Literal include survives reload");
        var packageInput = new ConfigurationFileSystem.Input("/dev/node_modules/mapper/package.json", """{"name":"mapper","version":"1.0.0","typescript":{"contentMapper":{"exec":["mapper"]}}}""");
        const string mapperText = """{"include":["src"],"contentMappers":[{"package":"mapper","extensions":[".box"]}]}""";
        var mapperSource = Source(mapperText);
        var mapper = Options.ParseJsonSourceFileConfigFileContent(mapperSource, new ConfigurationFileSystem([packageInput]), RootedDirectoryPath.FromGoString("/dev"), new() { RunExternalCode = Tristate.True });
        Check.True(mapper.PossiblyMatchesFileName(RootedFilePath.FromGoString("/dev/src/new.box")), "Mapped included extension");
        Check.True(mapper.PossiblyMatchesFileName(RootedFilePath.FromGoString("/dev/src/new.ts")), "Ordinary included extension");
        Check.True(!mapper.PossiblyMatchesFileName(RootedFilePath.FromGoString("/dev/src/new.vue")), "Unrecognized extension");
        Check.True(!mapper.PossiblyMatchesFileName(RootedFilePath.FromGoString("/dev/other/new.box")), "Mapped file outside include");
        var insensitiveMapper = Options.ParseJsonSourceFileConfigFileContent(mapperSource, new ConfigurationFileSystem([packageInput], CaseSensitivity.CaseInsensitive), RootedDirectoryPath.FromGoString("/dev"), new() { RunExternalCode = Tristate.True });
        Check.True(insensitiveMapper.PossiblyMatchesFileName(RootedFilePath.FromGoString("/dev/src/new.BOX")), "Case insensitive mapped extension");
        var sharedSource = Source("""{"files":["SRC/a.ts"]}""");
        var sensitive = Options.ParseJsonSourceFileConfigFileContent(sharedSource, noFiles, RootedDirectoryPath.FromGoString("/dev"));
        _ = Options.ParseJsonSourceFileConfigFileContent(sharedSource, new ConfigurationFileSystem([], CaseSensitivity.CaseInsensitive), RootedDirectoryPath.FromGoString("/dev"));
        Check.Equal(sensitive.GetMatchedFileSpec(RootedFilePath.FromGoString("/dev/SRC/a.ts")), "SRC/a.ts", "Specs owned by each parsed line");
        var canonical = Parse("""{"files":["src/a.ts"],"include":["src/*.ts"]}""", new ConfigurationFileSystem([new("/dev/src/a.ts")]));
        Check.Sequence(canonical.FileNames(), new[] { RootedFilePath.FromGoString("/dev/src/a.ts") }, "Literal and included canonical identity");
        var identity = Parse("{}", noFiles);
        var withTypings = identity.WithFileNames([RootedFilePath.FromGoString("/dev/index.ts"), RootedFilePath.FromGoString("/cache/@types/pkg/index.d.ts")]);
        Check.Equal(withTypings.ConfigName().AsString(), "/dev/tsconfig.json", "WithFileNames config identity");
        Check.Sequence(withTypings.FileNames(), new[] { RootedFilePath.FromGoString("/dev/index.ts"), RootedFilePath.FromGoString("/cache/@types/pkg/index.d.ts") }, "WithFileNames exact file list");
    }

    private static TsConfigSourceFile Source(string text) => Options.NewTsconfigSourceFileFromFilePath(RootedFilePath.FromGoString("/dev/tsconfig.json"), PathKey.FromGoString("/dev/tsconfig.json"), text);
    private static ParsedCommandLine Parse(string text, ConfigurationFileSystem fs) => Options.ParseJsonSourceFileConfigFileContent(Source(text), fs, RootedDirectoryPath.FromGoString("/dev"));
    private static void Matches(ParsedCommandLine parsed, IReadOnlyList<string> paths, IReadOnlyList<string> expected, string context)
    {
        foreach (string path in paths)
            Check.Equal(parsed.PossiblyMatchesFileName(RootedFilePath.FromGoString(path)), expected.Contains(path), context + ": " + path);
        foreach (string path in expected)
            if (!paths.Contains(path))
                Check.True(parsed.PossiblyMatchesFileName(RootedFilePath.FromGoString(path)), context + " absent fixture: " + path);
    }
}

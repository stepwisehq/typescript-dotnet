// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete static assertion adaptation of parsinghelpers_test.go and decls_test.go.
using TypeScript.Collections;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using Options = TypeScript.TsOptions.TsOptions;

namespace Configuration.Checks;

public static class ParsingHelperChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    ["TestMergeCompilerOptions", "TestMergeCompilerOptionsRawNulls", "TestParseCompilerOptionNoMissingFields", "TestCompilerOptionsDeclaration", "TestCommandLineOptionPathKinds", "TestRawCompilerOptionsFinalizePaths"];

    public static void Run(string name) => CompilerOptionChecks.RunParsingHelper(name);
}

public static partial class CompilerOptionChecks
{
    internal static void RunParsingHelper(string name)
    {
        switch (name)
        {
            case "TestMergeCompilerOptions": TestMergeCompilerOptions(); break;
            case "TestMergeCompilerOptionsRawNulls": TestMergeCompilerOptionsRawNulls(); break;
            case "TestParseCompilerOptionNoMissingFields": TestParseCompilerOptionNoMissingFields(); break;
            case "TestCompilerOptionsDeclaration": TestCompilerOptionsDeclaration(); break;
            case "TestCommandLineOptionPathKinds": TestCommandLineOptionPathKinds(); break;
            case "TestRawCompilerOptionsFinalizePaths": TestRawCompilerOptionsFinalizePaths(); break;
            default: throw new InvalidOperationException("Unknown parsing helper test: " + name);
        }
    }

    private static void TestMergeCompilerOptions()
    {
        Check.True(Options.mergeCompilerOptions(null, null, null) is null, "Merging nil options returns nil");
        CompilerOptions unchanged = new() { Strict = Tristate.True };
        Check.True(ReferenceEquals(Options.mergeCompilerOptions(unchanged, null, null), unchanged) && unchanged.Strict == Tristate.True,
            "Nil source preserves target and Strict");
        foreach (var field in originalFields)
        {
            OrderedMap<string, object?> nullOptions = new(), raw = new();
            nullOptions.Set(field.JsonName, null);
            raw.Set("compilerOptions", nullOptions);
            object? zero = field.Read(new CompilerOptions());
            foreach (object? targetValue in field.EqualityValues())
                foreach (object? sourceValue in field.EqualityValues())
                    foreach (bool explicitNull in new[] { false, true })
                    {
                        CompilerOptions target = new(), source = new();
                        field.Write(target, targetValue);
                        field.Write(source, sourceValue);
                        CompilerOptions expected = target.Clone();
                        if (explicitNull) field.Write(expected, zero);
                        else if (!deepValue(sourceValue, zero)) field.Write(expected, sourceValue);
                        var actual = Options.mergeCompilerOptions(target, source, explicitNull ? raw : null);
                        Check.True(ReferenceEquals(actual, target) && deepOptions(actual, expected), field.Name + " full merge result");
                        if (field.ShallowReference)
                            Check.True(ReferenceEquals(field.Read(target), field.Read(expected)), field.Name + " shallow merge sharing");
                        Check.True(deepValue(field.Read(source), sourceValue), field.Name + " source unchanged");
                    }
        }
    }

    private static void TestMergeCompilerOptionsRawNulls()
    {
        foreach (string name in new[] { "moduleDetection", "ModuleDetection", "moduleDetectionKind", "unknownOption" })
        {
            OrderedMap<string, object?> rawOptions = new(), raw = new();
            rawOptions.Set(name, null);
            raw.Set("compilerOptions", rawOptions);
            CompilerOptions target = new() { ModuleDetection = ModuleDetectionKind.Force };
            CompilerOptions source = new() { ModuleDetection = ModuleDetectionKind.Auto };
            Options.mergeCompilerOptions(target, source, raw);
            ModuleDetectionKind expected = name == "moduleDetection" ? ModuleDetectionKind.None : ModuleDetectionKind.Auto;
            Check.Equal(target.ModuleDetection, expected, name + " merge null");
            Options.mergeCompilerOptions(source, source, raw);
            Check.Equal(source.ModuleDetection, expected, name + " self-merge explicit null");
        }
    }

    private static void TestParseCompilerOptionNoMissingFields()
    {
        foreach (var field in originalFields)
            Check.True(Options.parseCompilerOptions(field.JsonName, field.Read(new CompilerOptions()), new CompilerOptions()),
                field.Name + " has a parser entry");
    }

    private static void TestCompilerOptionsDeclaration()
    {
        Dictionary<string, CommandLineOption> declarations = [];
        foreach (var option in Options.OptionsDeclarations) declarations[option.Name.ToLowerInvariant()] = option;
        Dictionary<string, string> internalOptions = new[] { "allowNonTsExtensions", "build", "configFilePath", "noDtsResolution", "noEmitForJsFiles", "pathsBasePath", "suppressOutputPathCheck", "build" }
            .Distinct().ToDictionary(name => name.ToLowerInvariant());
        foreach (var field in originalFields)
        {
            string lower = field.Name.ToLowerInvariant();
            string name;
            if (declarations.Remove(lower, out var declaration)) name = declaration.Name;
            else
            {
                Check.True(internalOptions.TryGetValue(lower, out _), field.Name + " has an option declaration");
                name = internalOptions[lower];
            }
            Check.Equal(field.JsonName, name, field.Name + " original JSON tag name matches declaration");
            // Independently verify the generated production field metadata too.
            var metadata = OptionFields.CompilerOptions.Single(item => item.Name == field.Name);
            Check.Equal(metadata.JsonName, field.JsonName, field.Name + " complete mapped JSON tag");
        }
        declarations.Remove("plugins");
        Check.Equal(declarations.Count, 0, "No remaining declaration absent from CompilerOptions");
    }

    private static void TestCommandLineOptionPathKinds()
    {
        Dictionary<string, CommandLineOptionPathKind> expected = new()
        {
            ["baseUrl"] = CommandLineOptionPathKind.Directory, ["declarationDir"] = CommandLineOptionPathKind.Directory,
            ["generateCpuProfile"] = CommandLineOptionPathKind.File, ["generateTrace"] = CommandLineOptionPathKind.Directory,
            ["mapRoot"] = CommandLineOptionPathKind.SourceMapLocation, ["outDir"] = CommandLineOptionPathKind.Directory,
            ["outFile"] = CommandLineOptionPathKind.File, ["pprofDir"] = CommandLineOptionPathKind.Directory,
            ["project"] = CommandLineOptionPathKind.FileOrDirectory, ["rootDir"] = CommandLineOptionPathKind.Directory,
            ["sourceRoot"] = CommandLineOptionPathKind.SourceMapLocation, ["tsBuildInfoFile"] = CommandLineOptionPathKind.File
        };
        foreach (var option in Options.OptionsDeclarations)
        {
            bool found = expected.Remove(option.Name, out var pathKind);
            Check.Equal(option.PathKind, found ? pathKind : CommandLineOptionPathKind.None, option.Name + " path kind");
        }
        Check.Equal(expected.Count, 0, "All expected path declarations found");
        foreach (string name in new[] { "rootDirs", "typeRoots" })
        {
            var option = Options.CompilerNameMap.GetOptionDeclarationFromName(name, false);
            Check.True(option is not null && option.Elements()?.PathKind == CommandLineOptionPathKind.Directory, name + " list directory elements");
        }
    }

    private static void TestRawCompilerOptionsFinalizePaths()
    {
        RawCompilerOptions raw = new();
        string text = """
            {"outDir":"dist","rootDirs":["src","${configDir}/generated"],"tsBuildInfoFile":"cache/build.tsbuildinfo",
             "sourceRoot":"sources\\mapped","paths":{"*a":["first/*"],"*":["fallback/*"]},"allowNonTsExtensions":true,
             "suppressOutputPathCheck":true,"configFilePath":"","NoImplicitAny":true}
            """;
        Check.True(raw.UnmarshalJSON(text) is null, "Raw options unmarshal succeeds");
        var (options, diagnostics) = raw.Finalize(RootedDirectoryPath.FromGoString("/project"));
        Check.Equal(diagnostics.Count, 0, "Finalize diagnostics empty");
        Check.Equal(options.OutDir, RootedDirectoryPath.FromGoString("/project/dist"), "Finalized outDir");
        Check.Sequence(options.RootDirs, new[] { RootedDirectoryPath.FromGoString("/project/src"), RootedDirectoryPath.FromGoString("/project/generated") }, "Finalized rootDirs");
        Check.Equal(options.TsBuildInfoFile, RootedFilePath.FromGoString("/project/cache/build.tsbuildinfo"), "Finalized build info");
        Check.Equal(options.SourceRoot, SourceMapLocation.FromGoString("sources/mapped"), "Finalized sourceRoot");
        Check.Sequence(options.Paths!.Keys().ToList(), new[] { "*a", "*" }, "Paths insertion order");
        Check.True(options.Paths.Has("*a"), "Paths contains *a");
        Check.Sequence(options.Paths.GetOrZero("*a"), new[] { "first/*" }, "Unresolved paths array");
        Check.Equal(options.AllowNonTsExtensions, Tristate.True, "Internal allowNonTsExtensions");
        Check.Equal(options.SuppressOutputPathCheck, Tristate.True, "Internal suppressOutputPathCheck");
        Check.Equal(options.ConfigFilePath, default(RootedFilePath), "Empty configFilePath");
        Check.Equal(options.NoImplicitAny, Tristate.Unknown, "Case-sensitive raw key");
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Assertion-preserving translations of the named pinned compileroptions_test.go bodies.
using TypeScript.Collections;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using Options = TypeScript.TsOptions.TsOptions;

namespace Configuration.Checks;

public static partial class CompilerOptionChecks
{
    public static IReadOnlyList<string> Cases { get; } =
    [
        "TestOptionEnumStrings",
        "TestGeneratedCompilerOptionParsingAndClone",
        "TestCompilerOptionsEquality",
        "TestGeneratedCompilerOptionParserCompatibility",
        "TestCompilerOptionConfigDirSubstitution",
        "TestCompilerOptionConfigDirSubstitutionCopyOnWrite"
    ];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestOptionEnumStrings": TestOptionEnumStrings(); break;
            case "TestGeneratedCompilerOptionParsingAndClone": TestGeneratedCompilerOptionParsingAndClone(); break;
            case "TestCompilerOptionsEquality": TestCompilerOptionsEquality(); break;
            case "TestGeneratedCompilerOptionParserCompatibility": TestGeneratedCompilerOptionParserCompatibility(); break;
            case "TestCompilerOptionConfigDirSubstitution": TestCompilerOptionConfigDirSubstitution(); break;
            case "TestCompilerOptionConfigDirSubstitutionCopyOnWrite": TestCompilerOptionConfigDirSubstitutionCopyOnWrite(); break;
            default: throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown original compiler-option test.");
        }
    }

    private static void TestOptionEnumStrings()
    {
        (Func<string> Actual, string Expected)[] normal =
        [
            (() => ModuleResolutionKind.Classic.String(), "Classic"),
            (() => ModuleResolutionKind.Node10.String(), "Node10"),
            (() => ModuleResolutionKind.Node16.String(), "Node16"),
            (() => ModuleResolutionKind.NodeNext.String(), "NodeNext"),
            (() => ModuleResolutionKind.Bundler.String(), "Bundler"),
            (() => JsxEmit.Preserve.String(), "preserve"),
            (() => JsxEmit.ReactNative.String(), "react-native"),
            (() => JsxEmit.React.String(), "react"),
            (() => JsxEmit.ReactJSX.String(), "react-jsx"),
            (() => JsxEmit.ReactJSXDev.String(), "react-jsxdev")
        ];
        foreach (var row in normal) Check.Equal(row.Actual(), row.Expected, row.Expected);
        (Func<string> Actual, string Expected)[] panic =
        [
            (() => ModuleResolutionKind.Unknown.String(), "should not use zero value of ModuleResolutionKind"),
            (() => ((ModuleResolutionKind)(-1)).String(), "unhandled case in ModuleResolutionKind.String"),
            (() => ((ModuleResolutionKind)4).String(), "unhandled case in ModuleResolutionKind.String"),
            (() => JsxEmit.None.String(), "should not use zero value of JsxEmit"),
            (() => ((JsxEmit)(-1)).String(), "unhandled case in JsxEmit.String"),
            (() => ((JsxEmit)6).String(), "unhandled case in JsxEmit.String")
        ];
        foreach (var row in panic)
        {
            string? message = null;
            try { _ = row.Actual(); }
            catch (InvalidOperationException error) { message = error.Message; }
            Check.Equal(message, row.Expected, row.Expected);
        }
    }

    private static void TestGeneratedCompilerOptionParserCompatibility()
    {
        var options = new CompilerOptions();
        Options.ParseCompilerOptions("STRICT", true, options);
        Options.ParseCompilerOptions("moduleDetectionKind", ModuleDetectionKind.Force, options);
        Options.ParseCompilerOptions("lib", new List<string> { "lib.es2025.d.ts" }, options);
        Options.ParseCompilerOptions("strict", null, options);
        Check.True(options.Strict == Tristate.True && options.ModuleDetection == ModuleDetectionKind.Force &&
            options.Lib is not null && options.Lib.SequenceEqual(["lib.es2025.d.ts"]),
            "Parser must preserve aliases, typed lib values, and null handling.");
        CompilerOptions before = options.Clone();
        Options.ParseCompilerOptions("unknownOption", true, options);
        // This original assertion is full structural equality, not production Equals.
        foreach (var field in originalFields)
            Check.True(deepValue(field.Read(options), field.Read(before)),
                "Unknown options must not change field " + field.Name);
    }

    private static void TestCompilerOptionConfigDirSubstitution()
    {
        Options.handleOptionConfigDirTemplateSubstitution(null, RootedDirectoryPath.FromGoString("/config"));
        (string Option, Func<CompilerOptions, string> Read)[] rows =
        [
            ("generateCpuProfile", o => o.GenerateCpuProfile.AsString()),
            ("generateTrace", o => o.GenerateTrace.AsString()),
            ("outFile", o => o.OutFile.AsString()),
            ("outDir", o => o.OutDir.AsString()),
            ("rootDir", o => o.RootDir.AsString()),
            ("tsBuildInfoFile", o => o.TsBuildInfoFile.AsString()),
            ("baseUrl", o => o.BaseUrl.AsString()),
            ("declarationDir", o => o.DeclarationDir.AsString())
        ];
        foreach (var row in rows)
        {
            var options = new parsedCompilerOptions
            {
                CompilerOptions = new(),
                unresolvedPaths = new() { [row.Option] = "${configDir}/output" }
            };
            Options.handleOptionConfigDirTemplateSubstitution(options, RootedDirectoryPath.FromGoString("/config"));
            Check.Equal(row.Read(options.CompilerOptions), "/config/output", row.Option);
        }
    }

    private static void TestCompilerOptionConfigDirSubstitutionCopyOnWrite()
    {
        foreach (string optionName in new[] { "rootDirs", "typeRoots" })
        {
            var options = new parsedCompilerOptions
            {
                CompilerOptions = new(),
                unresolvedPaths = new() { [optionName] = new List<object?> { "${configDir}/types", "unchanged" } }
            };
            Options.handleOptionConfigDirTemplateSubstitution(options, RootedDirectoryPath.FromGoString("/config"));
            var values = optionName == "rootDirs" ? options.CompilerOptions.RootDirs : options.CompilerOptions.TypeRoots;
            Check.Sequence(values?.Select(p => p.AsString()).ToList(), new[] { "/config/types", "/config/unchanged" }, optionName);
        }
        var original = new OrderedMap<string, List<string>?>();
        var unchanged = new List<string> { "unchanged" };
        var changed = new List<string> { "${configDir}/src", "other" };
        original.Set("unchanged", unchanged);
        original.Set("changed", changed);
        var parsed = new parsedCompilerOptions { CompilerOptions = new() { Paths = original } };
        Options.handleOptionConfigDirTemplateSubstitution(parsed, RootedDirectoryPath.FromGoString("/config"));
        var paths = parsed.CompilerOptions.Paths;
        Check.True(!ReferenceEquals(paths, original), "Substitution must clone paths before changing them.");
        Check.Sequence(paths!.GetOrZero("changed"), new[] { "/config/src", "other" }, "Substituted paths");
        Check.Equal(changed[0], "${configDir}/src", "Original changed slice remains intact.");
        Check.True(ReferenceEquals(paths.GetOrZero("unchanged"), unchanged), "Unchanged slice keeps its backing identity.");
        Check.Sequence(paths.Keys().ToList(), new[] { "unchanged", "changed" }, "Paths ordering");
        Options.handleOptionConfigDirTemplateSubstitution(parsed, RootedDirectoryPath.FromGoString("/config"));
        Check.True(ReferenceEquals(parsed.CompilerOptions.Paths, paths), "Unchanged paths keep their map identity.");
    }
}

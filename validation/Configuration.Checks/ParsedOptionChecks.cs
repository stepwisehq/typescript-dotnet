// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete assertion-preserving port of pinned internal/tsoptions/parsedoptions_test.go.
// Static visible-field coverage replaces reflection. Expected differences below
// come from the original populated fixture and each source mutation, not Equals.
using TypeScript.Collections;
using TypeScript.ContentMapper;
using TypeScript.Json;
using TypeScript.Scanner;
using TypeScript.TsOptions;

namespace Configuration.Checks;
public static class ParsedOptionChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestParsedOptionsEquality", "TestParsedOptionsEqualityIgnoresEmptyTypeAcquisitionLists", "TestParsedOptionsEqualityIgnoresPathsAllocation"];

    private static ParsedOptions MakeOptions() => new()
    {
        CompilerOptions = new()
        {
            Strict = Tristate.True
        },
        TypeAcquisition = new()
        {
            Enable = Tristate.True,
            Include = ["a"],
            Exclude = ["b"]
        },
        FileNames = [RootedFilePath.FromUnicode("/a.ts"), RootedFilePath.FromUnicode("/b.ts")],
        ProjectReferences = [new()
        {
            Path = RootedPath.FromUnicode("/project"),
            OriginalPath = "../project",
            Circular = true
        }, null!],
        ContentMappers = [new()
        {
            Package = "mapper",
            Extensions = [".vue"],
            Options = new JsonValue("{\"setting\":true}"u8.ToArray()),
            Name = "mapper",
            Version = "1",
            Exec = ["node", "mapper.js"],
            CompilerOptions = ["target"],
            DynamicConfig = true,
            PackageDirectory = RootedDirectoryPath.FromUnicode("/node_modules/mapper"),
            ContributionID = "extension"
        }, null!]
    };
    private static Mapper MapperOf(ParsedOptions value) => value.ContentMappers![0];
    // Source reflect.DeepEqual expectations on this finite fixture: distinct
    // populated copies equal; clearing any populated visible field differs;
    // each explicit mutation differs; self always equals.
    private static void Compare(ParsedOptions? left, ParsedOptions? right, bool expected, string context) => Check.Equal(left is null ? right is null : left.Equals(right), expected, context);
    private sealed record Field(string Name, Func<ParsedOptions, object?> Read, Action<ParsedOptions> Zero);
    private static readonly Field[] Fields = [new("ParsedOptions.CompilerOptions", p => p.CompilerOptions, p => p.CompilerOptions = null), new("ParsedOptions.TypeAcquisition", p => p.TypeAcquisition, p => p.TypeAcquisition = null), new("ParsedOptions.FileNames", p => p.FileNames, p => p.FileNames = null), new("ParsedOptions.ProjectReferences", p => p.ProjectReferences, p => p.ProjectReferences = null), new("ParsedOptions.ContentMappers", p => p.ContentMappers, p => p.ContentMappers = null), new("Mapper.Package", p => MapperOf(p).Package, p => MapperOf(p).Package = ""), new("Mapper.Extensions", p => MapperOf(p).Extensions, p => MapperOf(p).Extensions = null), new("Mapper.Options", p => MapperOf(p).Options, p => MapperOf(p).Options = null), new("Mapper.Name", p => MapperOf(p).Name, p => MapperOf(p).Name = ""), new("Mapper.Version", p => MapperOf(p).Version, p => MapperOf(p).Version = ""), new("Mapper.Exec", p => MapperOf(p).Exec, p => MapperOf(p).Exec = null), new("Mapper.CompilerOptions", p => MapperOf(p).CompilerOptions, p => MapperOf(p).CompilerOptions = null), new("Mapper.DynamicConfig", p => MapperOf(p).DynamicConfig, p => MapperOf(p).DynamicConfig = false), new("Mapper.PackageDirectory", p => MapperOf(p).PackageDirectory, p => MapperOf(p).PackageDirectory = default), new("Mapper.ContributionID", p => MapperOf(p).ContributionID, p => MapperOf(p).ContributionID = "")];
    private static readonly (string Name, Action<ParsedOptions> Change)[] Changes = [("compiler options", p => p.CompilerOptions!.Strict = Tristate.False), ("nil compiler options", p => p.CompilerOptions = null), ("nil type acquisition", p => p.TypeAcquisition = null), ("type acquisition enable", p => p.TypeAcquisition!.Enable = Tristate.False), ("type acquisition include", p => p.TypeAcquisition!.Include![0] = "other"), ("type acquisition exclude", p => p.TypeAcquisition!.Exclude![0] = "other"), ("type acquisition filename", p => p.TypeAcquisition!.DisableFilenameBasedTypeAcquisition = Tristate.True), ("filenames order", p => p.FileNames = [RootedFilePath.FromUnicode("/b.ts"), RootedFilePath.FromUnicode("/a.ts")]), ("reference path", p => p.ProjectReferences![0].Path = RootedPath.FromUnicode("/other")), ("reference original path", p => p.ProjectReferences![0].OriginalPath = "./other"), ("reference circular", p => p.ProjectReferences![0].Circular = false), ("nil reference", p => p.ProjectReferences![0] = null!), ("reference order", p => (p.ProjectReferences![0], p.ProjectReferences[1]) = (p.ProjectReferences[1], p.ProjectReferences[0])), ("mapper package", p => MapperOf(p).Package = "other"), ("mapper extension", p => MapperOf(p).Extensions![0] = ".other"), ("mapper options", p => MapperOf(p).Options = new JsonValue("{}"u8.ToArray())), ("mapper name", p => MapperOf(p).Name = "other"), ("mapper version", p => MapperOf(p).Version = "2"), ("mapper exec", p => MapperOf(p).Exec![1] = "other.js"), ("mapper compiler options", p => MapperOf(p).CompilerOptions![0] = "jsx"), ("mapper dynamic config", p => MapperOf(p).DynamicConfig = false), ("mapper directory", p => MapperOf(p).PackageDirectory = RootedDirectoryPath.FromUnicode("/other")), ("mapper contribution", p => MapperOf(p).ContributionID = "other"), ("nil mapper", p => p.ContentMappers![0] = null!), ("mapper order", p => (p.ContentMappers![0], p.ContentMappers[1]) = (p.ContentMappers[1], p.ContentMappers[0]))];
    private static readonly (string Name, Action<ParsedOptions, bool> Set)[] Slices = [("ParsedOptions.FileNames", (p, empty) => p.FileNames = empty ? [] : null), ("ParsedOptions.ProjectReferences", (p, empty) => p.ProjectReferences = empty ? [] : null), ("ParsedOptions.ContentMappers", (p, empty) => p.ContentMappers = empty ? [] : null), ("Definition.Extensions", (p, empty) => MapperOf(p).Extensions = empty ? [] : null), ("Definition.Options", (p, empty) => MapperOf(p).Options = empty ? new JsonValue([]) : null), ("Manifest.Exec", (p, empty) => MapperOf(p).Exec = empty ? [] : null), ("Manifest.CompilerOptions", (p, empty) => MapperOf(p).CompilerOptions = empty ? [] : null)];
    public static void Run(string name)
    {
        switch (name)
        {
            case "TestParsedOptionsEquality":
                Compare(null, null, true, "nil/nil");
                Compare(null, new(), false, "nil/zero");
                Compare(new(), null, false, "zero/nil");
                Compare(new(), new(), true, "zero/zero");
                Compare(MakeOptions(), MakeOptions(), true, "populated copies");
                foreach (var field in Fields)
                {
                    var a = MakeOptions();
                    var b = MakeOptions();
                    object? populated = field.Read(b);
                    Check.True(populated is not null && populated is not "" && populated is not false && (populated is not RootedDirectoryPath path || path != default), field.Name + " source fixture populated");
                    field.Zero(b);
                    object? zero = field.Read(b);
                    Check.True(zero is null || zero is "" || zero is false || zero is RootedDirectoryPath zeroPath && zeroPath == default, field.Name + " SetZero");
                    Compare(a, b, false, field.Name + " forward");
                    Compare(b, a, false, field.Name + " reverse");
                }

                foreach (var change in Changes)
                {
                    var a = MakeOptions();
                    var b = MakeOptions();
                    change.Change(b);
                    Compare(a, b, false, change.Name + " forward");
                    Compare(b, a, false, change.Name + " reverse");
                    Compare(b, b, true, change.Name + " self");
                }

                foreach (var slice in Slices)
                {
                    var a = MakeOptions();
                    var b = MakeOptions();
                    slice.Set(a, false);
                    slice.Set(b, true);
                    Compare(a, b, false, slice.Name + " nil/empty");
                    Compare(b, a, false, slice.Name + " empty/nil");
                }

                break;
            case "TestParsedOptionsEqualityIgnoresEmptyTypeAcquisitionLists":
                TypeAcquisition[] acquisitions = [new(), new()
                {
                    Include = []
                }, new()
                {
                    Exclude = []
                }, new()
                {
                    Include = [],
                    Exclude = []
                }

                ];
                foreach (var a in acquisitions)
                    foreach (var b in acquisitions)
                        Compare(new() { TypeAcquisition = a }, new() { TypeAcquisition = b }, true, "all 4x4 nil/empty type-acquisition lists");
                break;
            case "TestParsedOptionsEqualityIgnoresPathsAllocation":
                var left = new ParsedOptions
                {
                    CompilerOptions = new()
                    {
                        Paths = new OrderedMap<string, List<string>?>()
                    }
                };
                var right = new ParsedOptions
                {
                    CompilerOptions = new()
                    {
                        Paths = new OrderedMap<string, List<string>?>(0)
                    }
                };
                Compare(left, right, true, "zero/allocated paths");
                Compare(right, left, true, "allocated/zero paths");
                break;
            default:
                throw new ArgumentException("Unknown original parsed-options test: " + name, nameof(name));
        }
    }
}

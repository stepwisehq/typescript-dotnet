// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Assertion-preserving port of contentmapper/transform_test.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e; injected result parsing only, no IPC.
#nullable disable
using Configuration.Checks;
using TypeScript.ContentMapper;
using TypeScript.Scanner;
using TypeScript.SpanMap;
namespace Program.Checks;
public static class ParseResultChecks
{
    public static IEnumerable<object[]> Cases => new[] { nameof(TestParseResultSupplementalFileExtensions), nameof(TestParseResultAllowsSupplementalModules), nameof(TestParseResultDoesNotLeakCanonicalModuleForcingToSupplementals) }.Select(name => new object[] { name });
    public static void Run(string name)
    {
        switch (name)
        {
            case nameof(TestParseResultSupplementalFileExtensions): TestParseResultSupplementalFileExtensions(); break;
            case nameof(TestParseResultAllowsSupplementalModules): TestParseResultAllowsSupplementalModules(); break;
            case nameof(TestParseResultDoesNotLeakCanonicalModuleForcingToSupplementals): TestParseResultDoesNotLeakCanonicalModuleForcingToSupplementals(); break;
            default: throw new ArgumentOutOfRangeException(nameof(name));
        }
    }
    private static void TestParseResultSupplementalFileExtensions()
    {
        var mappings = SpanMaps.New(null);
        var result = new Result
        {
            VirtualExtension = ".ts", Mappings = mappings,
            Supplemental = [new() { VirtualExtension = ".js", Mappings = mappings }, new() { VirtualExtension = ".jsx", Mappings = mappings }, new() { VirtualExtension = ".ts", Mappings = mappings }, new() { VirtualExtension = ".tsx", Mappings = mappings }, new() { VirtualExtension = ".mts", Mappings = mappings }, new() { VirtualExtension = ".cts", Mappings = mappings }, new() { VirtualExtension = ".json", Mappings = mappings }]
        };
        var (files, error) = ContentMappers.ParseResult(new("/component.astro", "/component.astro", FileNameIsGoBytes: true), "", new() { Extensions = [".astro"], Name = "mapper" }, "transform-identity", result);
        Check.True(error is null, nameof(TestParseResultSupplementalFileExtensions));
        Check.Equal(files.Canonical.ContentMapperTransformIdentity(), "transform-identity", nameof(TestParseResultSupplementalFileExtensions));
        var canonicalSupplementals = files.Canonical.SupplementalSourceFiles();
        Check.Equal(canonicalSupplementals.Count, files.Supplemental.Count, nameof(TestParseResultSupplementalFileExtensions));
        (string fileName, ScriptKind kind)[] expected = [("/component.astro.0.js", ScriptKind.JS), ("/component.astro.1.jsx", ScriptKind.JSX), ("/component.astro.2.ts", ScriptKind.TS), ("/component.astro.3.tsx", ScriptKind.TSX), ("/component.astro.4.mts", ScriptKind.TS), ("/component.astro.5.cts", ScriptKind.TS), ("/component.astro.6.json", ScriptKind.JSON)];
        Check.Equal(files.Supplemental.Count, expected.Length, nameof(TestParseResultSupplementalFileExtensions));
        for (int i = 0; i < expected.Length; i++)
        {
            Check.Equal(files.Supplemental[i].FileName(), expected[i].fileName, nameof(TestParseResultSupplementalFileExtensions));
            Check.Equal(PathKey.FromGoString(files.Supplemental[i].PathKey()), TsPath.PathKeyFromCanonical(expected[i].fileName), nameof(TestParseResultSupplementalFileExtensions));
            Check.Equal(files.Supplemental[i].ScriptKind, expected[i].kind, nameof(TestParseResultSupplementalFileExtensions));
            Check.Equal(files.Supplemental[i].ContentMapperTransformIdentity(), "transform-identity", nameof(TestParseResultSupplementalFileExtensions));
            Check.True(ReferenceEquals(canonicalSupplementals[i], files.Supplemental[i]), nameof(TestParseResultSupplementalFileExtensions));
            Check.True(ReferenceEquals(files.Supplemental[i].CanonicalSourceFile(), files.Canonical), nameof(TestParseResultSupplementalFileExtensions));
        }
    }
    private static void TestParseResultAllowsSupplementalModules()
    {
        var mappings = SpanMaps.New(null);
        var (files, error) = ContentMappers.ParseResult(new("/component.astro", "/component.astro", FileNameIsGoBytes: true), "", new() { Extensions = [".astro"], Name = "mapper" }, "", new()
        {
            Text = "export {};", VirtualExtension = ".ts", Mappings = mappings,
            Supplemental = [new() { Text = "export const value = 1;", VirtualExtension = ".mts", Mappings = mappings }]
        });
        Check.True(error is null, nameof(TestParseResultAllowsSupplementalModules));
        Check.True(Ast.IsExternalModule(files.Supplemental[0]), nameof(TestParseResultAllowsSupplementalModules));
    }
    private static void TestParseResultDoesNotLeakCanonicalModuleForcingToSupplementals()
    {
        var mappings = SpanMaps.New(null);
        var (files, error) = ContentMappers.ParseResult(new("/component.astro", "/component.astro", FileNameIsGoBytes: true), "", new() { Name = "mapper" }, "", new()
        {
            Text = "const canonical = 1;", VirtualExtension = ".mts", Mappings = mappings,
            Supplemental = [new() { Text = "const supplemental = 1;", VirtualExtension = ".ts", Mappings = mappings }]
        });
        Check.True(error is null, nameof(TestParseResultDoesNotLeakCanonicalModuleForcingToSupplementals));
        Check.True(files.Canonical.ParseOptions().ExternalModuleIndicatorOptions.Force, nameof(TestParseResultDoesNotLeakCanonicalModuleForcingToSupplementals));
        Check.True(!files.Supplemental[0].ParseOptions().ExternalModuleIndicatorOptions.Force, nameof(TestParseResultDoesNotLeakCanonicalModuleForcingToSupplementals));
    }
}

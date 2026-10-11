// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete original CLI scenario assertions, including unchanged paired baselines.
using TypeScript.Collections;
using TypeScript.Json;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using T = TypeScript.TsOptions.TsOptions;
using W = TypeScript.DiagnosticWriter.DiagnosticWriter;

namespace Configuration.Checks;

public static partial class CommandLineChecks
{
    private static string Section(string baseline, string start, string end)
    {
        int position = baseline.IndexOf(start, StringComparison.Ordinal);
        Check.True(position >= 0, "Original baseline section " + start);
        string remaining = baseline[(position + start.Length)..];
        int next = remaining.IndexOf(end, StringComparison.Ordinal);
        return next < 0 ? remaining : remaining[..next];
    }

    private static CompilerOptions ReadCompilerBaseline(string bytes)
    {
        var (raw, error) = JsonCodec.Parse(bytes);
        Check.True(error is null && raw is OrderedMap<string, object?>, "Original typed compiler baseline decodes");
        CompilerOptions options = new();
        foreach (var (key, value) in ((OrderedMap<string, object?>)raw!).Entries()) CompilerOptionChecks.ReadOriginalCompilerField(key, value, options);
        return options;
    }

    private static BuildOptions ReadBuildBaseline(string bytes)
    {
        var (raw, error) = JsonCodec.Parse(bytes);
        Check.True(error is null && raw is OrderedMap<string, object?>, "Original typed build baseline decodes");
        BuildOptions result = new();
        foreach (var (key, value) in ((OrderedMap<string, object?>)raw!).Entries())
        {
            Tristate boolean = value is bool flag ? flag ? Tristate.True : Tristate.False : Tristate.Unknown;
            switch (key)
            {
                case "dry": result.Dry = boolean; break;
                case "force": result.Force = boolean; break;
                case "verbose": result.Verbose = boolean; break;
                case "stopBuildOnErrors": result.StopBuildOnErrors = boolean; break;
                case "clean": result.Clean = boolean; break;
                case "builders": result.Builders = new(checked((long)(double)value!)); break;
            }
        }
        return result;
    }

    private static bool BuildEqual(BuildOptions a, BuildOptions b) => a.Dry == b.Dry && a.Force == b.Force && a.Verbose == b.Verbose &&
        a.StopBuildOnErrors == b.StopBuildOnErrors && a.Clean == b.Clean && (a.Builders is null) == (b.Builders is null) && a.Builders?.Value == b.Builders?.Value;

    private static void OriginalScenarios(string group)
    {
        foreach (var scenario in originalScenarios.Where(row => row.Group == group))
        {
            string original = GoString.FromBytes(Convert.FromBase64String(scenario.OriginalBase64));
            string expected = GoString.FromBytes(Convert.FromBase64String(scenario.CurrentBase64));
            var fs = new ReadFixture([]);
            var cwd = RootedDirectoryPath.FromGoString("/project");
            string arguments = "Args::\n[" + string.Join(", ", scenario.Arguments.Select(arg => "\"" + arg + "\"")) + "]";
            string actual;
            if (group == "TestParseBuildCommandLine")
            {
                var parsed = T.ParseBuildCommandLine(scenario.Arguments.ToList(), fs, cwd);
                string projects = string.Join(',', parsed.Projects ?? []);
                string buildBytes = GoString.FromBytes(JsonCodec.Marshal(parsed.BuildOptions));
                string compilerBytes = GoString.FromBytes(JsonCodec.Marshal(parsed.CompilerOptions));
                if (original != "")
                {
                    string oldOptions = Section(original, "buildOptions::\n", "\nWatchOptions::\n");
                    Check.Equal(projects, Section(original, "\nProjects::\n", "\nErrors::\n"), scenario.Name + " projects");
                    Check.True(BuildEqual(ReadBuildBaseline(buildBytes), ReadBuildBaseline(oldOptions)), scenario.Name + " complete build options");
                    Check.True(CompilerOptionChecks.OriginalOptionValuesEqual(ReadCompilerBaseline(compilerBytes), ReadCompilerBaseline(oldOptions)), scenario.Name + " complete compiler options");
                }
                else
                {
                    _ = ReadBuildBaseline(buildBytes);
                    _ = ReadCompilerBaseline(compilerBytes);
                }
                using StringWriter errors = new();
                W.WriteFormatDiagnostics(errors, W.FromASTDiagnostics(parsed.Errors), new() { NewLine = "\n" });
                actual = arguments + "\n\nbuildOptions::\n" + buildBytes + "\n\ncompilerOptions::\n" + compilerBytes + "\n\nProjects::\n" + projects + "\n\nErrors::\n" + errors;
            }
            else
            {
                var diagnostics = T.CompilerOptionsDidYouMeanDiagnostics;
                if (scenario.ExtraKind != "")
                {
                    List<CommandLineOption> declarations = [.. T.OptionsDeclarations, new()
                    {
                        Name = "optionName", Kind = scenario.ExtraKind == "String" ? CommandLineOptionKind.String : CommandLineOptionKind.Number,
                        IsTSConfigOnly = true, Category = Diagnostics.Backwards_Compatibility, Description = Diagnostics.Enable_project_compilation
                    }];
                    diagnostics = T.getParseCommandLineWorkerDiagnostics(declarations);
                }
                var parsed = T.parseCommandLineWorker(diagnostics, scenario.Arguments.ToList(), fs, cwd);
                string filenames = string.Join(',', parsed.fileNames);
                string bytes = GoString.FromBytes(JsonCodec.Marshal(parsed.options));
                var typed = ReadCompilerBaseline(bytes);
                string oldOptions = Section(original, "CompilerOptions::\n", "\nWatchOptions::\n");
                Check.Equal(filenames, Section(original, "\nFileNames::\n", "\nErrors::\n"), scenario.Name + " filenames");
                Check.True(CompilerOptionChecks.OriginalOptionValuesEqual(typed, ReadCompilerBaseline(oldOptions)), scenario.Name + " complete compiler options");
                using StringWriter errors = new();
                W.WriteFormatDiagnostics(errors, W.FromASTDiagnostics(parsed.errors), new() { NewLine = "\n" });
                actual = arguments + "\n\nCompilerOptions::\n" + bytes + "\n\nFileNames::\n" + filenames + "\n\nErrors::\n" + errors;
            }
            Check.Equal(actual, expected, scenario.Name + " unchanged original tsc baseline bytes");
        }
    }

    private static void CustomConditionsNull()
    {
        ConfigurationFileSystem fs = new(new[]
        {
            new ConfigurationFileSystem.Input("/project/tsconfig.json", """{"compilerOptions":{"customConditions":["condition1","condition2"]}}"""),
            new ConfigurationFileSystem.Input("/project/index.ts", """console.log("Hello, World!");""")
        }, CaseSensitivity.CaseSensitive);
        var command = T.ParseCommandLine(["--project", "/project", "--customConditions", "null"], fs, RootedDirectoryPath.FromGoString("/project"));
        Check.True(command.Raw is OrderedMap<string, object?>, "Raw options is OrderedMap");
        var raw = (OrderedMap<string, object?>)command.Raw!;
        Check.True(raw.Has("customConditions"), "customConditions present in raw options");
        Check.True(raw.GetOrZero("customConditions") is null, "customConditions raw null");
        OrderedMap<string, object?> wrapped = new();
        wrapped.Set("compilerOptions", raw);
        var (parsed, errors) = T.GetParsedCommandLineOfConfigFile(RootedFilePath.FromGoString("/project/tsconfig.json"), command.CompilerOptions(), wrapped, fs, null);
        Check.Equal(errors.Count, 0, "Config parse errors empty");
        Check.True(parsed!.CompilerOptions().CustomConditions is null, "Command line null overrides config custom conditions");
    }
}

public static partial class CompilerOptionChecks
{
    internal static void ReadOriginalCompilerField(string key, object? value, CompilerOptions options)
    {
        var field = originalFields.FirstOrDefault(item => item.JsonName.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (field is null || value is null) return;
        // Original tests use json.Unmarshal into typed path string aliases, not
        // the normalized configuration-option converter. Retain those exact bytes.
        object? typed = field.GoType switch
        {
            "tspath.RootedFilePath" => RootedFilePath.FromGoString((string)value),
            "tspath.RootedDirectoryPath" => RootedDirectoryPath.FromGoString((string)value),
            "tspath.RootedPath" => RootedPath.FromGoString((string)value),
            "tspath.SourceMapLocation" => SourceMapLocation.FromGoString((string)value),
            "[]tspath.RootedFilePath" => ((List<object?>)value).Select(item => RootedFilePath.FromGoString((string)item!)).ToList(),
            "[]tspath.RootedDirectoryPath" => ((List<object?>)value).Select(item => RootedDirectoryPath.FromGoString((string)item!)).ToList(),
            _ => null
        };
        if (typed is not null) field.Write(options, typed);
        else T.ParseCompilerOptions(field.JsonName, value, options);
    }

    internal static bool OriginalOptionValuesEqual(CompilerOptions a, CompilerOptions b) => deepOptions(a, b);
}

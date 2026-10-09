// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original config text/JSON/source API baseline assertions; expectations are
// unchanged pinned bytes, never generated from this implementation.
using System.Text;
using TypeScript.Json;
using TypeScript.Scanner;
using TypeScript.TsOptions;
using TypeScript.Vfs;
using Options = TypeScript.TsOptions.TsOptions;
using W = TypeScript.DiagnosticWriter.DiagnosticWriter;

namespace Configuration.Checks;
public static partial class ConfigBaselineChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestParseConfigFileTextToJson", "TestParseJsonConfigFileContent", "TestParseJsonSourceFileConfigFileContent", "TestParseTypeAcquisition"];

    public static void Run(string name)
    {
        if (name == "TestParseConfigFileTextToJson")
        {
            foreach (var row in textCases)
            {
                StringBuilder actual = new();
                for (int i = 0; i < row.Inputs.Length; i++)
                {
                    string text = row.Inputs[i];
                    actual.Append("Input::\n").Append(text).Append("\n");
                    var(raw, errors) = Options.ParseConfigFileTextToJson(RootedFilePath.FromGoString("/apath/tsconfig.json"), PathKey.FromGoString("/apath"), text);
                    actual.Append("Config::\n").Append(Indent(raw)).Append("\nErrors::\n");
                    using StringWriter formatted = new();
                    W.FormatDiagnosticsWithColorAndContext(formatted, W.FromASTDiagnostics(errors, true), new() { NewLine = "\n", CurrentDirectory = RootedDirectoryPath.FromGoString("/"), CaseSensitivity = CaseSensitivity.CaseSensitive });
                    actual.Append(formatted).Append("\n");
                    if (i != row.Inputs.Length - 1)
                        actual.Append("\n");
                }

                Check.Equal(actual.ToString(), Bytes(row.Baseline), row.Title + " unchanged text API baseline");
            }

            return;
        }

        if (name == "TestParseTypeAcquisition")
        {
            VerifyCases(acquisitionCases, false);
            VerifyCases(acquisitionCases, true);
            return;
        }
        bool source = name == "TestParseJsonSourceFileConfigFileContent";
        if (!source && name != "TestParseJsonConfigFileContent")
            throw new InvalidOperationException("Unknown config baseline test: " + name);
        VerifyCases(configCases, source);
    }

    private static void VerifyCases(ConfigCase[] cases, bool source)
    {
        foreach (var row in cases)
        {
            StringBuilder actual = new();
            for (int i = 0; i < row.Inputs.Length; i++)
            {
                var input = row.Inputs[i];
                string basePath = input.Base != "" ? input.Base : TsPath.GetDirectoryPath(TsPath.GetNormalizedAbsolutePath(input.File, "/"));
                string filename = TsPath.GetNormalizedAbsolutePath(input.File, basePath);
                var files = input.Files.ToDictionary(file => file.Name, file => file.Content);
                files[filename] = input.Text;
                var fs = new ConfigurationFileSystem(files.Select(file => new ConfigurationFileSystem.Input(file.Key, file.Value)));
                RootedFilePath filePath = RootedFilePath.FromGoString(filename);
                ParsedCommandLine parsed;
                if (source)
                    parsed = Options.ParseJsonSourceFileConfigFileContent(Options.NewTsconfigSourceFileFromFilePath(filePath, PathKey.FromGoString(filename), input.Text), fs, RootedDirectoryPath.FromGoString(basePath));
                else
                {
                    var(raw, _) = Options.ParseConfigFileTextToJson(filePath, PathKey.FromGoString(filename), input.Text);
                    parsed = Options.ParseJsonConfigFileContent(raw, fs, RootedDirectoryPath.FromGoString(basePath), null, filePath);
                }

                actual.Append("Fs::\n");
                var walkError = TypeScript.Vfs.Vfs.WalkDir(fs, RootedPath.FromGoString("/"), (path, entry, error) =>
                {
                    if (error is not null)
                        return error;
                    if (entry!.Type() == TypeScript.Vfs.FileMode.Regular)
                    {
                        var(contents, ok) = fs.ReadFile(RootedFilePath.FromGoString(path.AsString()));
                        Check.True(ok, "Original baseline fixture read " + path.AsString());
                        actual.Append("//// [").Append(path.AsString()).Append("]\r\n").Append(contents).Append("\r\n\r\n");
                    }

                    return null;
                });
                Check.True(walkError is null, "Original baseline fixture WalkDir");
                actual.Append("\nconfigFileName:: ").Append(input.File).Append("\n");
                if (row.IncludeOptions)
                {
                    actual.Append("CompilerOptions::\n").Append(Indent(parsed.CompilerOptions())).Append("\n\n");
                    if (parsed.TypeAcquisition()is { } acquisition)
                        actual.Append("TypeAcquisition::\n").Append(Indent(acquisition)).Append("\n\n");
                }

                actual.Append("FileNames::\n").Append(string.Join(',', parsed.FileNames()?.Select(file => file.AsString()) ?? [])).Append("\nErrors::\n");
                using StringWriter formatted = new();
                W.FormatDiagnosticsWithColorAndContext(formatted, W.FromASTDiagnostics(parsed.Errors, true), new() { NewLine = "\r\n", CurrentDirectory = RootedDirectoryPath.FromGoString(basePath), CaseSensitivity = CaseSensitivity.CaseSensitive });
                actual.Append(formatted).Append("\n");
                if (i != row.Inputs.Length - 1)
                    actual.Append("\n");
            }

            Check.Equal(actual.ToString(), Bytes(source ? row.SourceBaseline : row.JsonBaseline), row.Title + " unchanged config baseline");
        }
    }

    private static string Bytes(string encoded) => GoString.FromBytes(Convert.FromBase64String(encoded));
    // Whitespace-only indentation of the real compact codec bytes. This finite
    // test adapter preserves all primitive bytes and ordered members; it does
    // not substitute System.Text.Json serialization for the original writer.
    private static string Indent(object? value)
    {
        string compact = GoString.FromBytes(JsonCodec.Marshal(value));
        StringBuilder result = new();
        int depth = 0;
        bool quoted = false, escaped = false;
        void NewLine() => result.Append('\n').Append(' ', depth * 2);
        for (int i = 0; i < compact.Length; i++)
        {
            char ch = compact[i];
            if (quoted)
            {
                result.Append(ch);
                if (escaped)
                    escaped = false;
                else if (ch == '\\')
                    escaped = true;
                else if (ch == '"')
                    quoted = false;
                continue;
            }

            if (ch == '"')
            {
                quoted = true;
                result.Append(ch);
            }
            else if (ch is '{' or '[')
            {
                result.Append(ch);
                if (i + 1 < compact.Length && compact[i + 1] == (ch == '{' ? '}' : ']'))
                    result.Append(compact[++i]);
                else
                {
                    depth++;
                    NewLine();
                }
            }
            else if (ch is '}' or ']')
            {
                depth--;
                NewLine();
                result.Append(ch);
            }
            else if (ch == ',')
            {
                result.Append(ch);
                NewLine();
            }
            else if (ch == ':')
                result.Append(": ");
            else
                result.Append(ch);
        }

        return result.ToString();
    }
}

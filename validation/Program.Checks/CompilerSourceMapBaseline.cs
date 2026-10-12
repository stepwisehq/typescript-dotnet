// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port of internal/testutil/tsbaseline/sourcemap_baseline.go and
// sourcemap_record_baseline.go. Assertions are original, not raw-output substitutes.
using System.Text;
using System.Text.Json;
using TypeScript.Scanner;

namespace Program.Checks;
public static class CompilerSourceMapBaselines
{
    public const string NoContent = "<no content>";
    public static (string? SourceMap, string SourceMapRecord) Observe(CompilerEmitCompilation result) => (ObserveSourceMap(result), ObserveSourceMapRecord(result));
    // sourcemap_baseline.go:18 DoSourcemapBaseline; file/channel naming stays with corpus runner.
    public static string? ObserveSourceMap(CompilerEmitCompilation result)
    {
        var options = result.Options;
        bool declarationMaps = options.GetAreDeclarationMapsEnabled();
        if (options.InlineSourceMap == Tristate.True)
        {
            if (result.Maps.Count > 0 && !declarationMaps)
                throw new InvalidOperationException("No sourcemap files should be generated if inlineSourceMaps was set.");
            return null;
        }

        if (options.SourceMap != Tristate.True && !declarationMaps)
            return null;
        int expectedMapCount = 0;
        if (options.SourceMap == Tristate.True)
            expectedMapCount += result.GetNumberOfJSFiles(false);
        if (declarationMaps)
            expectedMapCount += result.DTS.Count;
        if (result.Maps.Count != expectedMapCount)
            throw new InvalidOperationException("Number of sourcemap files should be same as js files.");
        if (options.NoEmitOnError == Tristate.True && result.Diagnostics.Count != 0 || result.Maps.Count == 0)
            return NoContent;
        var output = new StringBuilder();
        foreach (var map in result.Maps)
        {
            if (output.Length > 0)
                output.Append("\r\n");
            output.Append(CompilerEmitBaselines.FileOutput(map, result.HarnessSettings));
            output.Append(CreateSourceMapPreviewLink(map, result));
        }

        return output.ToString();
    }

    // sourcemap_baseline.go:77 createSourceMapPreviewLink. Preserve pinned suffix association.
    private static string CreateSourceMapPreviewLink(CompilerEmitTestFile map, CompilerEmitCompilation result)
    {
        using var document = JsonDocument.Parse(GoString.Bytes(map.Content));
        var root = document.RootElement;
        string file = root.TryGetProperty("file", out var fileElement) ? GoString.ToUtf8(fileElement.GetString() ?? "") : "";
        var generated = result.Outputs.FirstOrDefault(item => item.UnitName.EndsWith(file, StringComparison.Ordinal));
        if (generated is null)
            return "";
        var sourceFiles = new List<CompilerEmitTestFile?>();
        if (root.TryGetProperty("sources", out var sources) && sources.ValueKind != JsonValueKind.Null)
            foreach (var source in sources.EnumerateArray())
            {
                string name = GoString.ToUtf8(source.GetString() ?? "");
                var input = result.Inputs.FirstOrDefault(item => item.UnitName.EndsWith(name, StringComparison.Ordinal));
                if (input is not null)
                {
                    var programSource = result.Program.GetSourceFile(TsPath.ToRootedFilePath(input.UnitName, result.CurrentDirectory()));
                    if (programSource is not null)
                        input = new(input.UnitName, programSource.OriginalText());
                }

                sourceFiles.Add(input);
            }

        if (sourceFiles.Contains(null))
            return "";
        var hash = new StringBuilder("\n//// https://sokra.github.io/source-map-visualization#base64,");
        hash.Append(Base64EncodeChunk(generated.Content));
        hash.Append(',');
        hash.Append(Base64EncodeChunk(map.Content));
        foreach (var source in sourceFiles)
        {
            hash.Append(',');
            hash.Append(Base64EncodeChunk(source!.Content));
        }

        hash.Append('\n');
        return hash.ToString();
    }

    // sourcemap_baseline.go:123. QueryEscape followed immediately by QueryUnescape
    // is an exact byte identity, including arbitrary UTF8 bytes; no URI conversion is needed.
    private static string Base64EncodeChunk(string rawGoBytes) => Convert.ToBase64String(GoString.Bytes(rawGoBytes));
    // sourcemap_record_baseline.go:12 DoSourcemapRecordBaseline.
    public static string ObserveSourceMapRecord(CompilerEmitCompilation result)
    {
        string actual = NoContent;
        var options = result.Options;
        if (options.SourceMap == Tristate.True || options.InlineSourceMap == Tristate.True || options.DeclarationMap == Tristate.True)
        {
            string record = CompilerEmitBaselines.RemoveTestPathPrefixes(result.GetSourceMapRecord(), false);
            if (!(options.NoEmitOnError == Tristate.True && result.Diagnostics.Count > 0) && record.Length > 0)
                actual = record;
        }

        return actual;
    }
}

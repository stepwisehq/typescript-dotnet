// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Assertion-preserving finite seed adapter of tsc/internal/parser/parser_test.go FuzzParser.
// The original fuzz engine is not reproduced; its extension filter/options/parse body are.
using System.IO.Compression;
using System.Text.Json;
using TypeScript.Scanner;

namespace TypeScript.Foundation;
public static class ParserFuzzChecks
{
    private static readonly HashSet<string> Extensions = new(StringComparer.Ordinal)
    {
        ".ts",
        ".tsx",
        ".d.ts",
        ".js",
        ".jsx",
        ".cts",
        ".d.cts",
        ".cjs",
        ".mts",
        ".d.mts",
        ".mjs",
        ".json"
    };
    public static IEnumerable<object[]> Cases()
    {
        using var resource = typeof(ParserFuzzChecks).Assembly.GetManifestResourceStream("TypeScript.Foundation.ParserSeeds.jsonl.gz") ?? throw new InvalidOperationException("Missing original finite FuzzParser seeds");
        using var gzip = new GZipStream(resource, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        while (reader.ReadLine()is { } line)
        {
            using var row = JsonDocument.Parse(line);
            var value = row.RootElement;
            if (!value.GetProperty("originalFuzzSeed").GetBoolean())
                continue;
            yield return new object[]
            {
                value.GetProperty("name").GetString()!,
                value.GetProperty("extension").GetString()!,
                value.GetProperty("textBytesBase64").GetString()!,
                value.GetProperty("originalFuzzJSX").GetBoolean(),
                value.GetProperty("originalFuzzForce").GetBoolean(),
                value.GetProperty("scriptKind").GetInt32()
            };
        }
    }

    public static bool Run(string name, string extension, string sourceBase64, bool jsx, bool force, int expectedKind)
    {
        // Original t.Skip for extensions outside AllSupportedExtensionsWithJson.
        if (!Extensions.Contains(extension))
            return false;
        string fileName = "/index" + extension;
        var kind = extension.ToLowerInvariant() switch
        {
            ".js" or ".cjs" or ".mjs" => ScriptKind.JS,
            ".jsx" => ScriptKind.JSX,
            ".ts" or ".d.ts" or ".cts" or ".d.cts" or ".mts" or ".d.mts" => ScriptKind.TS,
            ".tsx" => ScriptKind.TSX,
            ".json" => ScriptKind.JSON,
            _ => ScriptKind.Unknown
        };
        if ((int)kind != expectedKind)
            throw new InvalidOperationException(name + ": seed filename inference differs from original Go");
        Parser.ParseSourceFile(new(fileName, fileName, new(jsx, force)), Convert.FromBase64String(sourceBase64), kind);
        return true; // Original assertion is no panic, including malformed recovery.
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/astnav/tokens_test.go:196–245, Go JSON baseline channels.
// Separate TS-JavaScript differential channels remain in the service inventory.
using System.Text.Json.Nodes;
using TypeScript.Scanner;
using TypeScript.AstNavigation;

namespace LanguageServices.Checks;
public static class AstNavBaselineChecks
{
    public static readonly string[] Names = ["GetTokenAtPosition", "GetTouchingPropertyName", "FindPrecedingToken", "FindNextToken"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        if (!Names.Contains(name))
            throw new ArgumentException(name);
        var root = FindRoot();
        var bytes = File.ReadAllBytes(Path.Combine(root, "upstream/TypeScript/tsc/testdata/fixtures/services/mapCode.ts"));
        var file = Parser.ParseSourceFile(new SourceFileParseOptions("/file.ts", "/file.ts"), bytes, ScriptKind.TS);
        var runs = new JsonArray();
        JsonObject? current = null;
        for (int position = 0; position < bytes.Length; position++)
        {
            Node? token = name switch
            {
                "GetTokenAtPosition" => AstNav.GetTokenAtPosition(file, position),
                "GetTouchingPropertyName" => AstNav.GetTouchingPropertyName(file, position),
                "FindPrecedingToken" => AstNav.FindPrecedingToken(file, position),
                _ => FindNext(file, position),
            };
            if (current != null && token != null && current["kind"]!.GetValue<string>() == FormatKind(token.Kind) && current["nodePos"]!.GetValue<int>() == token.Pos() && current["nodeEnd"]!.GetValue<int>() == token.End())
                current["endPos"] = JsonValue.Create(position);
            else
            {
                if (current != null)
                    runs.Add((JsonNode)current);
                current = token == null ? null : new JsonObject
                {
                    ["startPos"] = JsonValue.Create(position),
                    ["endPos"] = JsonValue.Create(position),
                    ["kind"] = JsonValue.Create(FormatKind(token.Kind)),
                    ["nodePos"] = JsonValue.Create(token.Pos()),
                    ["nodeEnd"] = JsonValue.Create(token.End()),
                };
            }
        }

        if (current != null)
            runs.Add((JsonNode)current);
        var expected = JsonNode.Parse(File.ReadAllText(Path.Combine(root, $"upstream/TypeScript/tsc/testdata/baselines/reference/astnav/{name}.mapCode.ts.baseline.json")));
        if (!JsonNode.DeepEquals(runs, expected))
        {
            var expectedRuns = expected!.AsArray();
            int first = 0;
            while (first < Math.Min(runs.Count, expectedRuns.Count) && JsonNode.DeepEquals(runs[first], expectedRuns[first])) first++;
            string actualRow = first < runs.Count ? runs[first]!.ToJsonString() : "<missing>";
            string expectedRow = first < expectedRuns.Count ? expectedRuns[first]!.ToJsonString() : "<missing>";
            throw new InvalidOperationException($"Original AstNav {name} JSON baseline differs at row {first}: expected {expectedRow}; actual {actualRow}; expected count {expectedRuns.Count}, actual count {runs.Count}");
        }
    }

    // tokens_test.go:262–271 toTokenInfo projection, shared by every channel.
    private static string FormatKind(Kind kind)
    {
        string name = kind.String();
        int prefix = name.IndexOf("Kind", StringComparison.Ordinal);
        if (prefix >= 0) name = name.Remove(prefix, 4);
        return name == "EndOfFile" ? "EndOfFileToken" : name;
    }

    // Original FindNextToken baseline catches panics for scanner trivia positions.
    private static Node? FindNext(SourceFile file, int position)
    {
        try
        {
            return AstNav.FindNextToken(AstNav.GetTokenAtPosition(file, position), file.AsNode(), file);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string FindRoot()
    {
        foreach (var start in new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        }

        )
            for (var directory = new DirectoryInfo(start); directory != null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "START-HERE.md")))
                    return directory.FullName;
        throw new DirectoryNotFoundException("Repository root required for unchanged upstream AstNav baselines");
    }
}

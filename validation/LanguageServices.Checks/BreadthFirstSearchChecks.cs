// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Original internal/core/bfs_test.go:13 TestBreadthFirstSearchParallel, all five leaves.
using TypeScript.Collections;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class BreadthFirstSearchChecks
{
    public static int RunAll()
    {
        TestBreadthFirstSearchParallel();
        return 1;
    }

    public static void TestBreadthFirstSearchParallel()
    {
        var graph = new Dictionary<string, Slice<string>>
        {
            ["A"] = Slice<string>.FromArray(["B", "C"]),
            ["B"] = Slice<string>.FromArray(["D"]),
            ["C"] = Slice<string>.FromArray(["D"]),
            ["D"] = Slice<string>.Make(0)
        };
        Slice<string> Children(string node) => graph[node];
        var result = Core.BreadthFirstSearchParallel("A", Children, static node => (node == "D", true));
        Require(result.Stopped, "Expected search to stop at D");
        Path(result.Path, "D", "B", "A");
        var gate = new object ();
        var visitedNodes = new List<string>();
        result = Core.BreadthFirstSearchParallel("A", Children, node =>
        {
            lock (gate)
                visitedNodes.Add(node);
            return (false, false);
        });
        Require(!result.Stopped, "Expected search to not stop early");
        Require(result.Path.IsNil, "Expected nil path when visit function never returns true");
        visitedNodes.Sort(StringComparer.Ordinal);
        Require(visitedNodes.SequenceEqual(new[] { "A", "B", "C", "D" }), "Expected all nodes exactly once");
        var earlyGraph = new Dictionary<string, Slice<string>>
        {
            ["Root"] = Slice<string>.FromArray(["L1A", "L1B"]),
            ["L1A"] = Slice<string>.FromArray(["L2A", "L2B"]),
            ["L1B"] = Slice<string>.FromArray(["L2C"]),
            ["L2A"] = Slice<string>.FromArray(["L3A"]),
            ["L2B"] = Slice<string>.Make(0),
            ["L2C"] = Slice<string>.Make(0),
            ["L3A"] = Slice<string>.Make(0)
        };
        var visited = new SyncSet<string>();
        Core.BreadthFirstSearchParallelEx("Root", node => earlyGraph[node], static node => (node == "L2B", true), new BreadthFirstSearchOptions<string, string>(visited), static node => node);
        foreach (var node in new[]
        {
            "Root",
            "L1A",
            "L1B",
            "L2A",
            "L2B"
        }

        )
            Require(visited.Has(node), "Expected to visit " + node);
        Require(!visited.Has("L3A"), "Expected not to visit L3A");
        visited = new();
        result = Core.BreadthFirstSearchParallelEx("A", Children, static node => (node == "A", false), new BreadthFirstSearchOptions<string, string>(visited), static node => node);
        Require(!result.Stopped, "Expected search to not stop early");
        Path(result.Path, "A");
        foreach (var node in new[]
        {
            "B",
            "C",
            "D"
        }

        )
            Require(visited.Has(node), "Expected to visit " + node);
        result = Core.BreadthFirstSearchParallel("A", Children, static node => node switch
        {
            "A" => (true, false),
            "D" => (true, true),
            _ => (false, false)});
        Require(result.Stopped, "Expected search to stop at D");
        Path(result.Path, "D", "B", "A");
    }

    private static void Path(Slice<string> actual, params string[] expected)
    {
        Require(actual.Count == expected.Length, "Path length");
        for (int i = 0; i < expected.Length; i++)
            Require(actual[i] == expected[i], "Path element");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

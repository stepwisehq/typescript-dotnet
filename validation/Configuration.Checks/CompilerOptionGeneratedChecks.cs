// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete static adaptation of the original reflection-driven generated parsing,
// shallow cloning and equality assertions in compileroptions_test.go.
using System.Collections;
using System.Runtime.CompilerServices;
using TypeScript.Collections;
using TypeScript.Scanner;
using Options = TypeScript.TsOptions.TsOptions;

namespace Configuration.Checks;

public static partial class CompilerOptionChecks
{
    private static object pluginInput()
    {
        var plugin = new OrderedMap<string, object?>();
        plugin.Set("name", "plugin");
        return new List<object?> { plugin };
    }

    private static object pathsInput()
    {
        var paths = new OrderedMap<string, object?>();
        paths.Set("@/*", new List<object?> { "src/*" });
        return paths;
    }

    private static object pathsExpected()
    {
        var paths = new OrderedMap<string, List<string>?>();
        paths.Set("@/*", new List<string> { "src/*" });
        return paths;
    }

    // Finite independent equivalent of the original reflected DeepEqual domain.
    // Uses actual test values, never production Equals or production descriptors.
    private static bool deepValue(object? left, object? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        if (left is StrongBox<long> leftBox && right is StrongBox<long> rightBox) return leftBox.Value == rightBox.Value;
        if (left is OrderedMap<string, List<string>?> leftMap && right is OrderedMap<string, List<string>?> rightMap)
        {
            var leftEntries = leftMap.Entries().ToList();
            var rightEntries = rightMap.Entries().ToList();
            if (leftEntries.Count != rightEntries.Count) return false;
            for (int i = 0; i < leftEntries.Count; i++)
                if (leftEntries[i].Key != rightEntries[i].Key || !deepValue(leftEntries[i].Value, rightEntries[i].Value)) return false;
            return true;
        }
        if (left is IList leftList && right is IList rightList)
        {
            if (left.GetType() != right.GetType() || leftList.Count != rightList.Count) return false;
            for (int i = 0; i < leftList.Count; i++) if (!deepValue(leftList[i], rightList[i])) return false;
            return true;
        }
        return left.Equals(right);
    }

    private static bool deepOptions(CompilerOptions? left, CompilerOptions? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return originalFields.All(field => deepValue(field.Read(left), field.Read(right)));
    }

    private static void TestGeneratedCompilerOptionParsingAndClone()
    {
        foreach (TestField field in originalFields)
        {
            var options = new CompilerOptions();
            Options.ParseCompilerOptions(field.JsonName, field.ParseInput(), options);
            Check.True(deepValue(field.Read(options), field.ParseExpected()), field.Name + " parse result");
            if (field.NumericEnum)
            {
                var numeric = new CompilerOptions();
                Options.ParseCompilerOptions(field.JsonName, 1.0, numeric);
                Check.True(deepValue(field.Read(numeric), field.ParseExpected()), field.Name + " JSON-number parse");
            }
            CompilerOptions clone = options.Clone();
            Check.True(!ReferenceEquals(clone, options) && deepOptions(clone, options), field.Name + " distinct complete clone");
            if (field.ShallowReference)
                Check.True(ReferenceEquals(field.Read(clone), field.Read(options)), field.Name + " shallow pointer/backing identity");
            // Additional authored mutation check confirms the actual mutable *int
            // association after its original Assert.Same-equivalent assertion.
            if (field.Read(options) is StrongBox<long> box)
            {
                box.Value++;
                Check.Equal(((StrongBox<long>)field.Read(clone)!).Value, box.Value, field.Name + " shared pointed mutation");
            }
        }
        Check.True(deepOptions(new CompilerOptions().Clone(), new CompilerOptions()), "Clone preserves every zero value");
    }

    private static void checkEquality(CompilerOptions? left, CompilerOptions? right)
    {
        bool expected = deepOptions(left, right);
        // C# null receivers cannot dispatch methods. This explicit language
        // adaptation preserves the original nil/nil and nil/non-nil domains.
        bool actual = left is null ? right is null : left.Equals(right);
        Check.Equal(actual, expected, "CompilerOptions.Equals independent structural comparison");
    }

    private static void TestCompilerOptionsEquality()
    {
        checkEquality(null, null);
        checkEquality(null, new());
        checkEquality(new(), null);
        checkEquality(new(), new());
        checkEquality(new(), new() { Strict = Tristate.True });
        var allOptions = new CompilerOptions();
        foreach (TestField field in originalFields)
        {
            object?[] values = field.EqualityValues();
            field.Write(allOptions, values[^1]);
            foreach (object? aValue in values)
                foreach (object? bValue in values)
                {
                    var a = new CompilerOptions();
                    var b = new CompilerOptions();
                    field.Write(a, aValue);
                    field.Write(b, bValue);
                    checkEquality(a, b);
                    checkEquality(a, a);
                    checkEquality(a, a.Clone());
                }
        }
        checkEquality(allOptions, allOptions.Clone());
        checkEquality(allOptions, new());
        var paths = new List<OrderedMap<string, List<string>?>?> { null, new(), new(0) };
        List<string>?[] valuesForPaths = [null, [], ["a"], ["b"], ["a", "b"], ["b", "a"]];
        string[][] keysForPaths = [["x", "y"], ["y", "x"]];
        foreach (List<string>? values in valuesForPaths)
            foreach (string[] keys in keysForPaths)
            {
                var map = new OrderedMap<string, List<string>?>();
                foreach (string key in keys) map.Set(key, values);
                paths.Add(map);
                paths.Add(map.Clone());
            }
        var cleared = paths[^1]!.Clone();
        cleared.Clear();
        paths.Add(cleared);
        foreach (var a in paths)
            foreach (var b in paths)
                checkEquality(new() { Paths = a }, new() { Paths = b });
    }
}

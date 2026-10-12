// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Selected validation counterpart of internal/fourslash/statebaseline.go:119–221.
// Keys/results are original raw Go byte strings, not display UTF16 strings.
using TypeScript.Scanner;

namespace LanguageServices.Checks.Fourslash;
internal readonly record struct DiffTableOptions(string Indent, bool SortKeys);
internal sealed class StateDiffTable(DiffTableOptions options)
{
    private readonly Dictionary<string, string> values = new(StringComparer.Ordinal);
    private readonly List<string> order = new();
    internal void Add(string key, string value)
    {
        if (!values.ContainsKey(key))
            order.Add(key);
        values[key] = value;
    }

    internal void Print(TextWriter writer, string header)
    {
        if (order.Count == 0)
            return;
        if (header.Length != 0)
            writer.Write(options.Indent + header + "\n");
        var keys = order.ToArray();
        int width = 0;
        foreach (string key in keys)
            width = Math.Max(width, key.Length);
        if (options.SortKeys)
            Array.Sort(keys, StringComparer.Ordinal);
        foreach (string key in keys)
        {
            // fmt width is runes although the source's keyWidth is byte length.
            int runes = 0;
            foreach (var item in new GoRuneIndexRange(ByteStringValue.FromString(key)))
            {
                _ = item;
                runes++;
            }

            writer.Write(options.Indent + "  " + key + new string (' ', Math.Max(0, width + 1 - runes)) + " " + values[key] + "\n");
        }
    }

    internal static void PrintSlices(TextWriter writer, string header, string[] current, Func<string[]> old, DiffTableOptions options, string change, Func<string, bool>? isDefault)
    {
        var previous = change == "*modified*" ? old() : Array.Empty<string>();
        var table = new StateDiffTable(options);
        foreach (string entry in current)
        {
            string marker = isDefault is not null && isDefault(entry) ? "(default) " : "";
            if (change == "*modified*" && !previous.Contains(entry, StringComparer.Ordinal))
                marker = "*new*";
            table.Add(entry, marker);
        }

        if (change == "*modified*")
            foreach (string entry in previous)
                if (!current.Contains(entry, StringComparer.Ordinal))
                    table.Add(entry, "*deleted*");
        table.Print(writer, header);
    }
}

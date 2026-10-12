// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/fourslash/statebaseline.go:401–531, retains real registry/entry owners.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;

namespace LanguageServices.Checks.Fourslash;
internal sealed partial class StateProjection
{
    private ConfigFileRegistry? serializedRegistry;
    private static string[] Sorted(IEnumerable<string>? values) => (values ?? []).Order(StringComparer.Ordinal).ToArray();
    private static string[] Projects(ConfigFileEntry entry) => Sorted(entry.RetainingProjects?.Select(id => id.GoLogString()));
    private static string[] OpenFiles(ConfigFileEntry entry) => Sorted(entry.RetainingOpenFiles?.Select(path => path.AsString()));
    private static string[] Configs(ConfigFileEntry entry) => Sorted(entry.RetainingConfigs?.Select(path => path.AsString()));
    private static bool Equal(string[] a, string[] b) => a.SequenceEqual(b);
    private static bool AncestorsEqual(ConfigFileNames a, ConfigFileNames b)
    {
        if ((a.Ancestors?.Count ?? 0) != (b.Ancestors?.Count ?? 0))
            return false;
        if (a.Ancestors is null)
            return true;
        foreach (var pair in a.Ancestors)
            if (b.Ancestors is null || !b.Ancestors.TryGetValue(pair.Key, out var value) || value != pair.Value)
                return false;
        return true;
    }

    private void PrintConfigs(Snapshot snapshot, TextWriter writer)
    {
        var registry = snapshot.ProjectCollection.ConfigFileRegistry();
        if (ReferenceEquals(registry, serializedRegistry))
            return;
        var configRows = new SortedDictionary<string, Action<TextWriter>>(StringComparer.Ordinal);
        var nameRows = new SortedDictionary<string, Action<TextWriter>>(StringComparer.Ordinal);
        bool configsChanged = false, namesChanged = false;
        var options = new DiffTableOptions("    ", true);
        if (registry.Configs is not null)
            foreach (var pair in registry.Configs)
            {
                var entry = pair.Value;
                ConfigFileEntry? old = null;
                serializedRegistry?.Configs?.TryGetValue(pair.Key, out old);
                bool projectsChanged = old is not null && !Equal(Projects(entry), Projects(old));
                bool openChanged = old is not null && !Equal(OpenFiles(entry), OpenFiles(old));
                bool retainingChanged = old is not null && !Equal(Configs(entry), Configs(old));
                string change = old is null ? "*new*" : (projectsChanged || openChanged || retainingChanged) ? "*modified*" : "";
                configsChanged |= change.Length != 0;
                configRows[pair.Key.AsString()] = output =>
                {
                    output.Write("  [" + entry.FileName.AsValue().Materialize() + "] " + change + "\n");
                    StateDiffTable.PrintSlices(output, "RetainingProjects:" + (change == "*modified*" && projectsChanged ? " *modified*" : ""), Projects(entry), () => Projects(old!), options, change, null);
                    StateDiffTable.PrintSlices(output, "RetainingOpenFiles:" + (change == "*modified*" && openChanged ? " *modified*" : ""), OpenFiles(entry), () => OpenFiles(old!), options, change, null);
                    StateDiffTable.PrintSlices(output, "RetainingConfigs:" + (change == "*modified*" && retainingChanged ? " *modified*" : ""), Configs(entry), () => Configs(old!), options, change, null);
                };
            }

        if (registry.Names is not null)
            foreach (var pair in registry.Names)
            {
                var entry = pair.Value;
                ConfigFileNames? old = null;
                serializedRegistry?.Names?.TryGetValue(pair.Key, out old);
                bool nearestChanged = old is not null && old.NearestConfigFileName != entry.NearestConfigFileName;
                bool ancestorsChanged = old is not null && !AncestorsEqual(old, entry);
                string change = old is null ? "*new*" : nearestChanged || ancestorsChanged ? "*modified*" : "";
                namesChanged |= change.Length != 0;
                nameRows[pair.Key.AsString()] = output =>
                {
                    output.Write("  [" + pair.Key.AsString() + "] " + change + "\n");
                    output.Write("    NearestConfigFileName: " + entry.NearestConfigFileName.AsValue().Materialize() + (change == "*modified*" && nearestChanged ? " *modified*" : "") + "\n");
                    var table = new StateDiffTable(options);
                    if (entry.Ancestors is not null)
                        foreach (var ancestor in entry.Ancestors)
                        {
                            string marker = "";
                            if (change == "*modified*")
                                marker = old!.Ancestors is null || !old.Ancestors.TryGetValue(ancestor.Key, out var previous) ? "*new*" : previous != ancestor.Value ? "*modified*" : "";
                            table.Add(ancestor.Key.AsValue().Materialize()!, ancestor.Value.AsValue().Materialize() + " " + marker);
                        }

                    if (change == "*modified*" && old!.Ancestors is not null)
                        foreach (var ancestor in old.Ancestors)
                            if (entry.Ancestors is null || !entry.Ancestors.ContainsKey(ancestor.Key))
                                table.Add(ancestor.Key.AsValue().Materialize()!, ancestor.Value.AsValue().Materialize() + " *deleted*");
                    table.Print(output, "Ancestors:" + (change == "*modified*" && ancestorsChanged ? " *modified*" : ""));
                };
            }

        if (serializedRegistry?.Configs is { } oldConfigs)
            foreach (var pair in oldConfigs)
                if (registry.Configs is null || !registry.Configs.ContainsKey(pair.Key))
                {
                    configsChanged = true;
                    configRows[pair.Key.AsString()] = output => output.Write("  [" + pair.Value.FileName.AsValue().Materialize() + "] *deleted*\n");
                }

        if (serializedRegistry?.Names is { } oldNames)
            foreach (var pair in oldNames)
                if (registry.Names is null || !registry.Names.ContainsKey(pair.Key))
                {
                    namesChanged = true;
                    nameRows[pair.Key.AsString()] = output => output.Write("  [" + pair.Key.AsString() + "] *deleted*\n");
                }

        serializedRegistry = registry;
        if (configsChanged)
        {
            writer.Write("Config::\n");
            foreach (var row in configRows.Values)
                row(writer);
        }

        if (namesChanged)
        {
            writer.Write("Config File Names::\n");
            foreach (var row in nameRows.Values)
                row(writer);
        }
    }
}

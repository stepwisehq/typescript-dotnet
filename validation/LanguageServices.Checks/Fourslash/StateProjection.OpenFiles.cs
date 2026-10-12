// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/fourslash/statebaseline.go:332–399, actual candidate project membership.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;

namespace LanguageServices.Checks.Fourslash;
internal sealed partial class StateProjection
{
    private sealed record OpenFileInfo(string DefaultProjectName, string[] AllProjects);
    private Dictionary<string, OpenFileInfo> serializedOpenFiles = new(StringComparer.Ordinal);
    private void PrintOpenFiles(Snapshot snapshot, IEnumerable<string> openFiles, CaseSensitivity sensitivity, TextWriter writer)
    {
        var current = new Dictionary<string, OpenFileInfo>(StringComparer.Ordinal);
        var rows = new SortedDictionary<string, Action<TextWriter>>(StringComparer.Ordinal);
        bool changed = false;
        foreach (string name in openFiles)
        {
            var path = sensitivity.PathKey(RootedPath.FromGoString(name));
            var defaultProject = snapshot.ProjectCollection.GetDefaultProject(path);
            var projects = new List<string>();
            foreach (var project in snapshot.ProjectCollection.Projects())
                if (project.GetProgram()is { } program && program.GetSourceFileByPath(path)is not null)
                    projects.Add(project.ID().GoLogString());
            projects.Sort(StringComparer.Ordinal);
            var info = new OpenFileInfo(defaultProject?.ID().GoLogString() ?? "", projects.ToArray());
            current[name] = info;
            bool exists = serializedOpenFiles.TryGetValue(name, out var old);
            string change = !exists ? "*new*" : old!.DefaultProjectName != info.DefaultProjectName || !old.AllProjects.SequenceEqual(info.AllProjects) ? "*modified*" : "";
            changed |= change.Length != 0;
            rows[name] = output =>
            {
                output.Write("  [" + name + "] " + change + "\n");
                StateDiffTable.PrintSlices(output, "", info.AllProjects, () => old!.AllProjects, new("  ", true), change, id => id == info.DefaultProjectName);
            };
        }

        foreach (string name in serializedOpenFiles.Keys)
            if (!current.ContainsKey(name))
            {
                changed = true;
                rows[name] = output => output.Write("  [" + name + "] *closed*\n");
            }

        serializedOpenFiles = current;
        if (changed)
        {
            writer.Write("Open Files::\n");
            foreach (var row in rows.Values)
                row(writer);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/fourslash/statebaseline.go:253–330; real candidate Program/SourceFile identity.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using CompilerProgram = TypeScript.Compiler.Program;

namespace LanguageServices.Checks.Fourslash;
internal sealed partial class StateProjection
{
    private Dictionary<string, CompilerProgram?> serializedProjects = new(StringComparer.Ordinal);
    private static bool IsLibFile(string name)
    {
        string basename = TsPath.GetBaseFileName(name);
        return basename.StartsWith("lib.", StringComparison.Ordinal) && basename.EndsWith(".d.ts", StringComparison.Ordinal);
    }

    private void PrintProjects(Snapshot snapshot, TextWriter writer)
    {
        var current = new Dictionary<string, CompilerProgram?>(StringComparer.Ordinal);
        var rows = new SortedDictionary<string, Action<TextWriter>>(StringComparer.Ordinal);
        bool changed = false;
        foreach (var project in snapshot.ProjectCollection.Projects())
        {
            string id = project.ID().GoLogString();
            var program = project.GetProgram();
            current[id] = program;
            bool exists = serializedProjects.TryGetValue(id, out var old);
            string change = !exists ? "*new*" : ReferenceEquals(old, program) ? "" : "*modified*";
            changed |= change.Length != 0;
            rows[id] = output =>
            {
                output.Write("  [" + id + "] " + change + "\n");
                var table = new StateDiffTable(new("  ", false));
                if (program is not null)
                    foreach (var file in program.GetSourceFiles())
                    {
                        string name = file.FileName();
                        string fileChange = "";
                        if (change == "*modified*")
                        {
                            if (old is null)
                            {
                                if (!IsLibFile(name))
                                    fileChange = "*new*";
                            }
                            else
                            {
                                var previous = old.GetSourceFileByPath(PathKey.FromValue(file.PathKeyValue()));
                                fileChange = previous is null ? "*new*" : !ReferenceEquals(previous, file) ? "*modified*" : "";
                            }
                        }

                        if (fileChange.Length != 0 || !IsLibFile(name))
                            table.Add(name, fileChange);
                    }

                if (!ReferenceEquals(old, program) && old is not null)
                    foreach (var file in old.GetSourceFiles())
                        if (program is null || program.GetSourceFileByPath(PathKey.FromValue(file.PathKeyValue()))is null)
                            table.Add(file.FileName(), "*deleted*");
                table.Print(output, "");
            };
        }

        foreach (var item in serializedProjects)
        {
            if (current.ContainsKey(item.Key))
                continue;
            changed = true;
            rows[item.Key] = output =>
            {
                output.Write("  [" + item.Key + "] *deleted*\n");
                var table = new StateDiffTable(new("  ", false));
                if (item.Value is not null)
                    foreach (var file in item.Value.GetSourceFiles())
                        if (!IsLibFile(file.FileName()))
                            table.Add(file.FileName(), "");
                table.Print(output, "");
            };
        }

        serializedProjects = current;
        if (changed)
        {
            writer.Write("Projects::\n");
            foreach (var row in rows.Values)
                row(writer);
        }
    }
}

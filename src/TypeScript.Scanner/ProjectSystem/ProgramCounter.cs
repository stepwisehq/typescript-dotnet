// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Compiler;

namespace TypeScript.ProjectSystem;
// internal/project/programcounter.go. Program identity, never structural equality.
internal sealed class ProgramCounter
{
    private readonly object gate = new();
    private Dictionary<Program, int>? refs;
    // programcounter.go:17 Ref.
    internal void Ref(Program program)
    {
        lock (gate)
        {
            refs ??= new(ReferenceEqualityComparer.Instance);
            refs[program] = unchecked(refs.GetValueOrDefault(program) + 1);
        }
    }

    // programcounter.go:26 Deref.
    internal bool Deref(Program program)
    {
        lock (gate)
        {
            if (refs is null || !refs.TryGetValue(program, out var count))
                return false;
            count = unchecked(count - 1);
            if (count < 0)
                throw new InvalidOperationException("program reference count went below zero");
            if (count == 0)
            {
                refs.Remove(program);
                return true;
            }

            refs[program] = count;
            return false;
        }
    }

    // programcounter.go:46 Len.
    internal int Len()
    {
        lock (gate)
            return refs?.Count ?? 0;
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Selected disabled-value/Push guard from tsc/internal/tracing/tracing.go at
// fed0bf24149fb1ed36039212648bafdafc1ea10e. Recording and type tracing are external.
using TypeScript.Scanner;
using TypeScript.Vfs;

namespace TypeScript.Tracing;
public readonly record struct Phase(string Value)
{
    public static readonly Phase Parse = new("parse"), Program = new("program"), Bind = new("bind"), Check = new("check"), CheckTypes = new("checkTypes"), Emit = new("emit"), Session = new("session");
}

public sealed class Tracing
{
    private int traceStarted = 0;
    // The zero source value does not record. Do not silently create a simulated
    // enabled session without the original writable tracing lifecycle dependency.
    public static (Tracing? Tracing, Exception? Error) StartTracing(IFS fileSystem, RootedDirectoryPath traceDirectory, string configFilePath, bool deterministic) => (null, new NotSupportedException("Enabled performance tracing is outside Program construction."));
    // tracing.go:267. Program callers guard nil; the source zero value returns
    // the same inert release function without touching args or allocating events.
    public Action Push(Phase phase, string name, Dictionary<string, object?>? args, bool separateBeginAndEnd)
    {
        if (Volatile.Read(ref traceStarted) == 0)
            return static () =>
            {
            };
        throw new NotSupportedException("Enabled performance tracing is outside Program construction.");
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/logging/logtree.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.ProjectSystem.Logging;
internal sealed class LogTree : ILogCollector
{
    private static long sequence;
    // logtree.go:61–122. Go preserves a typed nil *LogTree inside Logger.
    // CLR interface null cannot enter its nil-safe receiver methods.
    internal static ILogger AsLogger(LogTree? tree) => tree is null ? NilReceiver.Instance : tree;
    private sealed class NilReceiver : ILogger
    {
        internal static readonly NilReceiver Instance = new();
        public void Log(params object?[] message) { }
        public void Logf(string format, params object?[] args) { }
        public void Error(params object?[] message) { }
        public void Errorf(string format, params object?[] args) { }
        public void Warn(params object?[] message) { }
        public void Warnf(string format, params object?[] args) { }
        public void Info(params object?[] message) { }
        public void Infof(string format, params object?[] args) { }
        public ILogger? Verbose() => null;
        public void SetVerbose(bool verbose) { }
        // The original IsVerbose receiver has no nil guard.
        public bool IsVerbose() => throw new NullReferenceException();
    }

    private sealed record Entry(ulong Sequence, DateTimeOffset Time, string Message, LogTree? Child);
    private string name = "";
    private readonly object gate = new();
    private Slice<Entry> logs;
    private LogTree root = null!;
    private int level;
    private bool verbose;
    private int count, stringLength;
    // logtree.go:23 newLogEntry.
    private static Entry NewEntry(LogTree? child, string message) => new(unchecked((ulong)Interlocked.Increment(ref sequence)), DateTimeOffset.Now, message, child);
    // logtree.go:52 NewLogTree.
    public static LogTree NewLogTree(string name)
    {
        var tree = new LogTree
        {
            name = name
        };
        tree.root = tree;
        return tree;
    }

    // logtree.go:60 add. Root counters intentionally precede publication.
    private void Add(Entry entry)
    {
        Interlocked.Add(ref root.stringLength, level + 15 + entry.Message.Length + 1);
        Interlocked.Increment(ref root.count);
        lock (gate)
            logs = logs.Append(entry);
    }

    public void Log(params object? [] message) => Add(NewEntry(null, LogFormat.Sprint(message)));
    public void Logf(string format, params object? [] args) => Add(NewEntry(null, LogFormat.Sprintf(format, args)));
    public bool IsVerbose() => verbose;
    public void SetVerbose(bool value) => verbose = value;
    public ILogger? Verbose() => verbose ? this : null;
    public void Error(params object? [] message) => Log(message);
    public void Errorf(string format, params object? [] args) => Logf(format, args);
    public void Warn(params object? [] message) => Log(message);
    public void Warnf(string format, params object? [] args) => Logf(format, args);
    public void Info(params object? [] message) => Log(message);
    public void Infof(string format, params object? [] args) => Logf(format, args);
    // logtree.go:119 Embed. Preserve exact independently-owned tree counters;
    // embedded roots are not rebound to this root.
    public void Embed(LogTree tree)
    {
        int embeddedCount = Volatile.Read(ref tree.count);
        Interlocked.Add(ref root.stringLength, Volatile.Read(ref tree.stringLength) + embeddedCount * level);
        Interlocked.Add(ref root.count, embeddedCount);
        Add(NewEntry(tree, tree.name));
    }

    // logtree.go:133 Fork.
    public LogTree Fork(string message)
    {
        var child = new LogTree
        {
            level = level + 1,
            root = root,
            verbose = verbose
        };
        Add(NewEntry(child, message));
        return child;
    }

    // logtree.go:143 String. Callers finish writers before requesting output, as
    // upstream does; no extra lock/sort changes publication order.
    public string String()
    {
        if (!ReferenceEquals(root, this))
            throw new InvalidOperationException("can only call String on root LogTree");
        var builder = new GoBuilder();
        string header = "======== " + name + " ========\n";
        builder.Grow(Volatile.Read(ref stringLength) + header.Length);
        builder.WriteString(header);
        WriteLogsRecursive(builder, "");
        return builder.String();
    }

    public override string ToString() => String();
    // logtree.go:156 writeLogsRecursive.
    private void WriteLogsRecursive(GoBuilder builder, string indent)
    {
        foreach (var entry in logs)
        {
            builder.WriteString(indent);
            builder.WriteString(Logger.FormatTime(entry.Time));
            builder.WriteString(" ");
            builder.WriteString(entry.Message);
            builder.WriteString("\n");
            entry.Child?.WriteLogsRecursive(builder, indent + "\t");
        }
    }
}

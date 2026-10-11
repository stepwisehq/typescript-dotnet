// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of selected tsc/internal/core/workgroup.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// WorkGroup and both scheduling implementations; ThrottleGroup is external.
using System.Runtime.ExceptionServices;

namespace TypeScript.Scanner;
public interface IWorkGroup
{
    void Queue(Action action);
    void RunAndWait();
}

internal static partial class Core
{
    public static IWorkGroup NewWorkGroup(bool singleThreaded) =>
        singleThreaded ? new SingleThreadedWorkGroup() : new ParallelWorkGroup();
}

internal sealed class ParallelWorkGroup : IWorkGroup
{
    private readonly object gate = new();
    private bool done;
    private int pending;
    private ExceptionDispatchInfo? failure;

    public void Queue(Action action)
    {
        lock (gate)
        {
            if (done)
                throw new InvalidOperationException("Queue called after RunAndWait returned");
            pending++;
        }
        // Source WaitGroup.Go starts work during Queue. The pending count is
        // reserved before scheduling and includes work recursively queued by it.
        // A work group exposes no Task result. Keep normal ExecutionContext flow
        // and the thread pool's local-queue preference with an explicit value state.
        _ = ThreadPool.QueueUserWorkItem(static state => state.Group.Execute(state.Action),
            (Group: this, Action: action), preferLocal: true);
    }

    private void Execute(Action action)
    {
        try { action(); }
        catch (Exception error)
        {
            // Go propagates a worker panic. Preserve a managed failure for
            // the waiting caller rather than discard a worker exception.
            lock (gate)
                failure ??= ExceptionDispatchInfo.Capture(error);
        }
        finally
        {
            lock (gate)
            {
                pending--;
                if (pending == 0)
                    Monitor.PulseAll(gate);
            }
        }
    }

    public void RunAndWait()
    {
        lock (gate)
        {
            try
            {
                while (pending != 0)
                    Monitor.Wait(gate);
                failure?.Throw();
            }
            finally { done = true; }
        }
    }
}

internal sealed class SingleThreadedWorkGroup : IWorkGroup
{
    private readonly object gate = new();
    private Slice<Action?> actions;
    private int done;
    public void Queue(Action action)
    {
        if (Volatile.Read(ref done) != 0)
            throw new InvalidOperationException("Queue called after RunAndWait returned");
        lock (gate)
            actions = actions.Append(action);
    }

    public void RunAndWait()
    {
        try
        {
            while (Pop() is Action action)
                action();
        }
        finally { Volatile.Write(ref done, 1); }
    }

    private Action? Pop()
    {
        lock (gate)
        {
            if (actions.Count == 0)
                return null;
            int end = actions.Count - 1;
            Action action = actions[end]!;
            actions[end] = null; // core/workgroup.go: pop releases the executed closure.
            actions = actions.Slice2(0, end);
            return action;
        }
    }
}

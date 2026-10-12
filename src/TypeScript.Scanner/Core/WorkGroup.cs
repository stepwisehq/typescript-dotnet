// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of selected tsc/internal/core/workgroup.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
// WorkGroup and both scheduling implementations; ThrottleGroup is external.
using System.Collections.Concurrent;
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
    private readonly ConcurrentQueue<Action> ready = new();
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
            ready.Enqueue(action);
            // A waiting caller can help recursively queued work. Publish and wake
            // under the same gate used for the empty-queue/wait transition.
            Monitor.PulseAll(gate);
        }
        // Queue still starts work immediately, with the accepted static callback
        // and standard ThreadPool scheduling; no per-job Task or thread owner.
        // A callback or waiting caller may claim an action, so compiler work state
        // is explicit rather than relying on the producer's ExecutionContext.
        _ = ThreadPool.QueueUserWorkItem(static group => group.ExecuteNext(), this, preferLocal: true);
    }

    private bool ExecuteNext()
    {
        if (!ready.TryDequeue(out var action))
            return false;
        Execute(action);
        return true;
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
        // Go parks a waiting goroutine without occupying its scheduler worker.
        // CLR Monitor/Lazy waits occupy ThreadPool workers instead. Let the
        // waiting thread run this group's ready actions, including nested groups,
        // before blocking; otherwise queued checker construction can starve.
        try
        {
            while (true)
            {
                if (ExecuteNext())
                    continue;
                lock (gate)
                {
                    if (pending == 0)
                    {
                        done = true;
                        failure?.Throw();
                        return;
                    }
                    // Queue may publish after ExecuteNext finds no work. Its pulse
                    // shares this gate with the check, avoiding a lost wakeup.
                    if (!ready.IsEmpty)
                        continue;
                    Monitor.Wait(gate);
                }
            }
        }
        finally
        {
            lock (gate)
                done = true;
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

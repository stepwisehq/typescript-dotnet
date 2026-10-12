// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/project/background/queue.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.ProjectSystem.Background;
internal sealed class Queue
{
    private readonly object gate = new();
    private bool closed;
    private int pending;
    private readonly record struct TaskState(Queue Owner, CancellationToken Context, Action<CancellationToken> Callback);
    // queue.go:17 NewQueue. The CLR worker callback represents a Go goroutine;
    // no compiler work ordering or special thread-pool setting is introduced.
    public static Queue NewQueue() => new();
    // queue.go:21 Enqueue.
    public void Enqueue(CancellationToken context, Action<CancellationToken> callback)
    {
        lock (gate)
        {
            if (closed || context.IsCancellationRequested)
                return;
            pending++;
        }

        bool queued;
        try
        {
            queued = ThreadPool.UnsafeQueueUserWorkItem(static (TaskState state) =>
            {
                try
                {
                    if (!state.Context.IsCancellationRequested)
                        state.Callback(state.Context);
                }
                finally
                {
                    lock (state.Owner.gate)
                    {
                        state.Owner.pending--;
                        if (state.Owner.pending == 0)
                            Monitor.PulseAll(state.Owner.gate);
                    }
                }
            }, new TaskState(this, context, callback), preferLocal: false);
        }
        catch
        {
            RollBackEnqueue();
            throw;
        }

        if (!queued)
        {
            RollBackEnqueue();
            throw new InvalidOperationException("failed to queue background task");
        }
    }

    // A failed CLR submission has no worker to perform the deferred decrement.
    private void RollBackEnqueue()
    {
        lock (gate)
        {
            pending--;
            if (pending == 0)
                Monitor.PulseAll(gate);
        }
    }

    // queue.go:46 Wait. Unlike Close, this does not prohibit subsequent Enqueue.
    public void Wait()
    {
        lock (gate)
            while (pending != 0)
                Monitor.Wait(gate);
    }

    // queue.go:50 Close.
    public void Close()
    {
        lock (gate)
        {
            closed = true;
            while (pending != 0)
                Monitor.Wait(gate);
        }
    }
}

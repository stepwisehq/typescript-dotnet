// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// internal/lsp/dynamic_queue.go. Draft outside compilation: integration/review pending.
using TypeScript.Scanner;

namespace TypeScript.Lsp;
// The two capacity-one Go channels share exactly one state owner. A condition
// represents its idle/ready admission here; taking a state removes it until the
// caller returns it. This avoids racing asynchronous reads that could consume a
// state after another arm has won. Cancellation only participates in admission.
internal sealed class DynamicQueue<T>
{
    internal sealed class State
    {
        internal Slice<T> Items;
    }

    private readonly object gate = new();
    private State? idle = new();
    private State? ready;
    internal Exception? Put(CancellationToken context, T item)
    {
        if (context.IsCancellationRequested)
            return new OperationCanceledException("context canceled", context);
        var(state, error) = GetAny(context);
        if (error != null)
            return error;
        state!.Items = state.Items.Append(item);
        ReturnReady(state);
        return null;
    }

    internal (T Item, Exception? Error) Get(CancellationToken context)
    {
        if (context.IsCancellationRequested)
            return (default!, new OperationCanceledException("context canceled", context));
        var(state, error) = GetReady(context);
        if (error != null)
            return (default!, error);
        var item = state!.Items[0];
        state.Items[0] = default!;
        state.Items = state.Items.Slice2(1);
        if (state.Items.Length == 0)
        {
            state.Items = default;
            ReturnIdle(state);
        }
        else
            ReturnReady(state);
        return (item, null);
    }

    internal (State? State, Exception? Error) GetAny(CancellationToken context) => Take(context, any: true);
    internal (State? State, Exception? Error) GetReady(CancellationToken context) => Take(context, any: false);
    private (State? State, Exception? Error) Take(CancellationToken context, bool any)
    {
        using var registration = context.UnsafeRegister(static owner =>
        {
            var queue = (DynamicQueue<T>)owner!;
            lock (queue.gate)
                Monitor.PulseAll(queue.gate);
        }, this);
        lock (gate)
        {
            while (true)
            {
                // Like a select, ready state and cancellation can both be eligible.
                // No cancellation recheck occurs after admission returns a state.
                if (any && idle != null)
                {
                    var state = idle;
                    idle = null;
                    return (state, null);
                }

                if (ready != null)
                {
                    var state = ready;
                    ready = null;
                    return (state, null);
                }

                if (context.IsCancellationRequested)
                    return (null, new OperationCanceledException("context canceled", context));
                Monitor.Wait(gate);
            }
        }
    }

    internal void ReturnIdle(State state)
    {
        lock (gate)
        {
            idle = state;
            Monitor.PulseAll(gate);
        }
    }

    private void ReturnReady(State state)
    {
        lock (gate)
        {
            ready = state;
            Monitor.PulseAll(gate);
        }
    }
}

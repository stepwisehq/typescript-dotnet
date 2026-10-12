// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of internal/core/workgroup.go:92–123 at fed0bf24149fb1ed36039212648bafdafc1ea10e.
namespace TypeScript.Scanner;
internal static partial class Core
{
    // workgroup.go:98. The derived errgroup context is discarded upstream.
    internal static ThrottleGroup NewThrottleGroup(CancellationToken context, SemaphoreSlim semaphore) => new(semaphore);
}

// A shared semaphore represents the original occupied-slot channel. Cancellation
// does not interrupt acquisition: the original callback sends unconditionally.
internal sealed class ThrottleGroup
{
    private readonly SemaphoreSlim semaphore;
    private readonly IWorkGroup group = Core.NewWorkGroup(false);
    private Exception? error;
    internal ThrottleGroup(SemaphoreSlim semaphore) => this.semaphore = semaphore;
    // workgroup.go:108. Callback exceptions retain WorkGroup's panic propagation;
    // returned errors retain errgroup's first-error, wait-for-all behavior.
    internal void Go(Func<Exception?> action)
    {
        group.Queue(() =>
        {
            semaphore.Wait();
            try
            {
                var result = action();
                if (result != null)
                    Interlocked.CompareExchange(ref error, result, null);
            }
            finally
            {
                semaphore.Release();
            }
        });
    }

    // workgroup.go:121.
    internal Exception? Wait()
    {
        group.RunAndWait();
        return error;
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Full original TestQueue (five subtests), tsc/internal/project/background/queue_test.go.
using BackgroundQueue = TypeScript.ProjectSystem.Background.Queue;

namespace LanguageServices.Checks;
public static class ProjectBackgroundChecks
{
    public static readonly string[] Names = ["TestQueue"];
    public static int RunAll()
    {
        Run(Names[0]);
        return 1;
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException("original background assertion failed");
    }

    public static void Run(string name)
    {
        if (name != Names[0])
            throw new ArgumentException(name);
        var queue = BackgroundQueue.NewQueue();
        bool executed = false;
        try
        {
            queue.Enqueue(default, _ => executed = true);
            queue.Wait();
            Check(executed);
        }
        finally
        {
            queue.Close();
        }

        queue = BackgroundQueue.NewQueue();
        long counter = 0;
        try
        {
            for (int i = 0; i < 10; i++)
                queue.Enqueue(default, _ => Interlocked.Increment(ref counter));
            queue.Wait();
            Check(counter == 10);
        }
        finally
        {
            queue.Close();
        }

        queue = BackgroundQueue.NewQueue();
        var values = new List<string>();
        var gate = new object ();
        var nested = queue;
        try
        {
            queue.Enqueue(default, context =>
            {
                lock (gate)
                    values.Add("parent");
                nested.Enqueue(context, _ =>
                {
                    lock (gate)
                        values.Add("child");
                });
            });
            queue.Wait();
            lock (gate)
                Check(values.Count == 2);
        }
        finally
        {
            queue.Close();
        }

        queue = BackgroundQueue.NewQueue();
        queue.Close();
        executed = false;
        queue.Enqueue(default, _ => executed = true);
        queue.Wait();
        Check(!executed);
        queue = BackgroundQueue.NewQueue();
        using var started = new ManualResetEventSlim();
        using var finish = new ManualResetEventSlim();
        using var closeStarted = new ManualResetEventSlim();
        using var closed = new ManualResetEventSlim();
        queue.Enqueue(default, _ =>
        {
            started.Set();
            finish.Wait();
        });
        started.Wait();
        var closer = new Thread(() =>
        {
            closeStarted.Set();
            queue.Close();
            closed.Set();
        });
        closer.Start();
        closeStarted.Wait();
        Check(!closed.Wait(TimeSpan.FromMilliseconds(100)));
        finish.Set();
        closed.Wait();
        closer.Join();
    }
}

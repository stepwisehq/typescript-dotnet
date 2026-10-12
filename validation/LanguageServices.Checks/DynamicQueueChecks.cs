// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Three unchanged original internal/lsp/dynamic_queue_test.go functions.
using TypeScript.Lsp;

namespace LanguageServices.Checks;
public static class DynamicQueueChecks
{
    public static readonly string[] Names = ["TestDynamicQueueFIFO", "TestDynamicQueueGetCancellation", "TestDynamicQueuePutCancellationWhileStateUnavailable"];
    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestDynamicQueueFIFO":
            {
                var queue = new DynamicQueue<int>();
                for (var i = 0; i < 1000; i++)
                    Require(queue.Put(default, i) == null, "Put returned an error");
                for (var i = 0; i < 1000; i++)
                {
                    var(item, error) = queue.Get(default);
                    Require(error == null, "Get returned an error");
                    Require(item == i, $"Get() = {item}, want {i}");
                }

                break;
            }

            case "TestDynamicQueueGetCancellation":
            {
                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                var queue = new DynamicQueue<int>();
                var(item, error) = queue.Get(cancellation.Token);
                Require(error is OperationCanceledException, "Get error must be context cancellation");
                Require(item == 0, "Get must return zero value");
                break;
            }

            case "TestDynamicQueuePutCancellationWhileStateUnavailable":
            {
                var queue = new DynamicQueue<int>();
                var(state, error) = queue.GetAny(default);
                Require(error == null, "GetAny returned an error");
                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                var putError = queue.Put(cancellation.Token, 1);
                Require(putError is OperationCanceledException, "Put error must be context cancellation");
                queue.ReturnIdle(state!);
                Require(queue.Put(default, 2) == null, "Put after state return failed");
                var(item, getError) = queue.Get(default);
                Require(getError == null, "Get after state return failed");
                Require(item == 2, $"Get() = {item}, want 2");
                break;
            }

            default:
                throw new ArgumentException(name);
        }
    }
}

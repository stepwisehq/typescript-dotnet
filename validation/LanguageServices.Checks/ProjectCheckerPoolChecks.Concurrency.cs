// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProjectCheckerPoolChecks
{
    // Standard Thread blocking/completion observations replace synctest.Wait.
    // This executes the real checkout; it does not inject or bypass semaphore state.
    private sealed class Acquisition : IDisposable
    {
        private readonly Thread thread;
        private Exception? failure;
        private int acquired, complete;
        internal Checker? Checker;
        internal bool Acquired => Volatile.Read(ref acquired) != 0;

        internal Acquisition(ProjectCheckerPool pool, ProtocolRequestContext context, Checker? expected = null)
        {
            thread = new Thread(() =>
            {
                try
                {
                    var(checker, release) = pool.GetChecker(context, null);
                    try
                    {
                        Checker = checker;
                        if (expected is not null)
                            Require(ReferenceEquals(checker, expected), "expected original checker identity");
                        Volatile.Write(ref acquired, checker is not null ? 1 : 0);
                    }
                    finally
                    {
                        release();
                    }
                }
                catch (Exception error)
                {
                    failure = error;
                }
                finally
                {
                    Volatile.Write(ref complete, 1);
                }
            })
            {
                IsBackground = true
            };
            thread.Start();
        }

        internal void AssertBlocked(string message)
        {
            WaitUntil(() => Volatile.Read(ref complete) != 0 || (thread.ThreadState & ThreadState.WaitSleepJoin) != 0, "acquisition did not reach its actual wait");
            Require(!Acquired && Volatile.Read(ref complete) == 0, message);
        }

        internal void Finish(string message)
        {
            Require(thread.Join(TimeSpan.FromSeconds(10)), message);
            if (failure is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            Require(Acquired, message);
        }

        public void Dispose()
        {
            Require(thread.Join(TimeSpan.FromSeconds(10)), "checkout worker did not finish");
            if (failure is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    public static void TestCheckerPoolSameRequestContention()
    {
        foreach (var lifetime in new[]
        {
            CheckerLifetime.Diagnostics,
            CheckerLifetime.Temporary,
            CheckerLifetime.API
        }

        )
            WithFreshPool((_, pool, _) =>
            {
                using var cancellation = new CancellationTokenSource();
                var context = Context("same-request", lifetime).WithCancellationToken(cancellation.Token);
                var(first, releaseFirst) = pool.GetChecker(context, null);
                using var next = new Acquisition(pool, context, first);
                try
                {
                    next.AssertBlocked("the request ID must not bypass exclusive acquisition");
                    releaseFirst();
                    next.Finish("waiting acquisition should finish after release");
                }
                finally
                {
                    releaseFirst();
                    cancellation.Cancel();
                }
            }, maximumCheckers: 2, sessionMaximumCheckers: 2);
    }

    public static void TestCheckerPoolSameRequestConcurrentQueries()
    {
        var(session, pool) = Setup(new(3, default));
        using var cancellation = new CancellationTokenSource();
        try
        {
            var context = Context("same-request").WithCancellationToken(cancellation.Token);
            var(first, releaseFirst) = pool.GetChecker(context, null);
            var(second, releaseSecond) = pool.GetChecker(context, null);
            try
            {
                Require(!ReferenceEquals(first, second), "overlapping acquisitions must use different checkers");
                Require(pool.checkers.Length - 1 - pool.querySem.CurrentCount == 2, "each acquisition must hold its own slot");
            }
            finally
            {
                releaseFirst();
                releaseSecond();
            }
        }
        finally
        {
            cancellation.Cancel();
            session.Close();
        }
    }

    public static void TestCheckerPoolQueryContention() => WithFreshPool((_, pool, _) =>
    {
        var(first, releaseFirst) = pool.GetChecker(Context("query-hold"), null);
        Require(first is not null, "expected checker");
        using var next = new Acquisition(pool, Context("query-wait"));
        try
        {
            next.AssertBlocked("second query should be blocked while first holds the checker");
            releaseFirst();
            next.Finish("second query should have acquired the checker after release");
        }
        finally
        {
            releaseFirst();
        }
    }, maximumCheckers: 2, sessionMaximumCheckers: 2);
    public static void TestCheckerPoolDiagnosticsContention() => WithFreshPool((_, pool, _) =>
    {
        var(first, releaseFirst) = pool.GetChecker(Context("diag-hold", CheckerLifetime.Diagnostics), null);
        Require(first is not null, "expected checker");
        using var next = new Acquisition(pool, Context("diag-wait", CheckerLifetime.Diagnostics));
        try
        {
            next.AssertBlocked("second diagnostics request should be blocked");
            var(query, releaseQuery) = pool.GetChecker(Context("query-concurrent"), null);
            try
            {
                Require(query is not null && !ReferenceEquals(first, query), "query checker should be different from diagnostics checker");
            }
            finally
            {
                releaseQuery();
            }

            releaseFirst();
            next.Finish("second diagnostics request should have acquired the checker after release");
        }
        finally
        {
            releaseFirst();
        }
    }, maximumCheckers: 2, sessionMaximumCheckers: 2);
    public static void TestCheckerPoolCrossReleaseAffinityWithContention() => WithFreshPool((_, pool, _) =>
    {
        using var cancellation = new CancellationTokenSource();
        var contextA = Context("req-A").WithCancellationToken(cancellation.Token);
        var(first, releaseFirst) = pool.GetChecker(contextA, null);
        Require(first is not null, "expected checker");
        releaseFirst();
        var(second, releaseSecond) = pool.GetChecker(Context("req-B"), null);
        Require(second is not null, "expected checker");
        using var next = new Acquisition(pool, contextA);
        try
        {
            next.AssertBlocked("request A should block while B holds the slot");
            releaseSecond();
            next.Finish("request A should unblock after B releases");
            Require(ReferenceEquals(next.Checker, first), "request A should get the same checker on reacquire");
        }
        finally
        {
            releaseSecond();
            cancellation.Cancel();
        }
    }, maximumCheckers: 2, sessionMaximumCheckers: 2);
    public static void TestCheckerPoolMultipleConcurrentQueryCheckers() => WithFreshPool((_, pool, _) =>
    {
        var(first, releaseFirst) = pool.GetChecker(Context("multi-q-1"), null);
        var(second, releaseSecond) = pool.GetChecker(Context("multi-q-2"), null);
        var(third, releaseThird) = pool.GetChecker(Context("multi-q-3"), null);
        Require(first is not null && second is not null && third is not null, "expected query checkers");
        using var fourth = new Acquisition(pool, Context("multi-q-4"));
        try
        {
            Require(!ReferenceEquals(first, second), "concurrent query checkers should be distinct (1 vs 2)");
            Require(!ReferenceEquals(first, third), "concurrent query checkers should be distinct (1 vs 3)");
            Require(!ReferenceEquals(second, third), "concurrent query checkers should be distinct (2 vs 3)");
            lock (pool.gate)
                Require(!ReferenceEquals(pool.checkers[0], first) && !ReferenceEquals(pool.checkers[0], second) && !ReferenceEquals(pool.checkers[0], third), "query checkers should not occupy the diagnostics slot");
            fourth.AssertBlocked("4th query should block when all 3 query slots are held");
            releaseFirst();
            fourth.Finish("4th query should unblock after one slot is released");
        }
        finally
        {
            releaseFirst();
            releaseSecond();
            releaseThird();
        }
    });
}

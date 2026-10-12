// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProjectCheckerPoolChecks
{
    public static void TestCheckerPoolMinCheckers()
    {
        var(session, pool) = Setup(new(1, TimeSpan.FromSeconds(10)));
        try
        {
            Require(pool.opts.MaxCheckers == 2 && pool.checkers.Length == 2, "maxCheckers=1 should be clamped to 2");
        }
        finally
        {
            session.Close();
        }
    }

    public static void TestCheckerPoolFileAffinity() => WithFreshPool((_, pool, _) =>
    {
        var source = pool.program.GetSourceFile(new RootedFilePath("/src/index.ts"));
        Require(source is not null, "expected source file");
        var(first, releaseFirst) = pool.GetChecker(Context("file-aff-1"), source);
        Require(first is not null, "expected checker");
        releaseFirst();
        var(second, releaseSecond) = pool.GetChecker(Context("file-aff-2"), source);
        try
        {
            Require(second is not null && ReferenceEquals(first, second), "same file should return the same checker via file affinity");
        }
        finally
        {
            releaseSecond();
        }
    });
    public static void TestCheckerPoolCanceledCheckerDisposal() => WithFreshPool((_, pool, _) =>
    {
        var source = pool.program.GetSourceFile(new RootedFilePath("/src/index.ts"));
        Require(source is not null, "expected source file");
        var(first, releaseFirst) = pool.GetChecker(Context("cancel-test"), null);
        Require(first is not null, "expected checker");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        first.GetDiagnostics(cancellation.Token, source!);
        Require(first.WasCanceled(), "expected canceled checker");
        releaseFirst();
        var(second, releaseSecond) = pool.GetChecker(Context("after-cancel"), null);
        try
        {
            Require(!ReferenceEquals(first, second), "should get a new checker, not the canceled one");
        }
        finally
        {
            releaseSecond();
        }
    }, sessionMaximumCheckers: 2);
    public static void TestCheckerPoolRequestAssociationCleanupOnDisposal() => WithFreshPool((_, pool, _) =>
    {
        using var request = new CancellationTokenSource();
        var context = Context("assoc-cleanup-req").WithCancellationToken(request.Token);
        var(checker, release) = pool.GetChecker(context, null);
        Require(checker is not null, "expected checker");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        checker.GetDiagnostics(canceled.Token, pool.program.GetSourceFile(new RootedFilePath("/src/index.ts"))!);
        Require(checker.WasCanceled(), "expected canceled checker");
        release();
        lock (pool.gate)
            Require(!pool.requestAssociations.ContainsKey(ByteStringKey.FromValue(context.RequestID)), "request association should be cleared after checker disposal");
        request.Cancel();
    }, idleTimeout: TimeSpan.FromSeconds(5), sessionMaximumCheckers: 2);
    public static void TestCheckerPoolRequestAssociationCleanupOnContextDone() => WithFreshPool((_, pool, _) =>
    {
        using var request = new CancellationTokenSource();
        var context = Context("ctx-cleanup-req").WithCancellationToken(request.Token);
        var(checker, release) = pool.GetChecker(context, null);
        Require(checker is not null, "expected checker");
        release();
        var key = ByteStringKey.FromValue(context.RequestID);
        lock (pool.gate)
            Require(pool.requestAssociations.ContainsKey(key), "request association should persist after release");
        request.Cancel();
        WaitUntil(() =>
        {
            lock (pool.gate)
                return !pool.requestAssociations.ContainsKey(key);
        }, "request association should be cleaned up after context cancellation");
        lock (pool.gate)
            Require(!pool.requestAssociations.ContainsKey(key), "request association should be cleaned up after context cancellation");
    }, sessionMaximumCheckers: 2);
    public static void TestCheckerPoolDiagnosticsRecreatedAfterIdleDisposal() => WithFreshPool((_, pool, clock) =>
    {
        var(first, releaseFirst) = pool.GetChecker(Context("diag-recreate-1", CheckerLifetime.Diagnostics), null);
        Require(first is not null, "expected checker");
        releaseFirst();
        clock.Advance(TimeSpan.FromSeconds(5));
        lock (pool.gate)
            Require(pool.checkers[0] is null, "diagnostics checker should be disposed");
        var(second, releaseSecond) = pool.GetChecker(Context("diag-recreate-2", CheckerLifetime.Diagnostics), null);
        try
        {
            Require(second is not null && !ReferenceEquals(first, second), "should be a new checker instance");
        }
        finally
        {
            releaseSecond();
        }
    }, idleTimeout: TimeSpan.FromSeconds(5), sessionMaximumCheckers: 2);
    public static void TestCheckerPoolDiscardStillFunctional() => WithFreshPool((_, pool, _) =>
    {
        pool.Discard();
        var(first, releaseFirst) = pool.GetChecker(Context("post-obs"), null);
        Require(first is not null, "should still create checkers after Discard");
        int index = QueryIndex(pool, first);
        Require(index > 0, "checker should be in a query slot");
        releaseFirst();
        lock (pool.gate)
            Require(ReferenceEquals(pool.checkers[index], first), "checker should persist after release on discarded pool");
        var(second, releaseSecond) = pool.GetChecker(Context("post-obs-2"), null);
        try
        {
            Require(ReferenceEquals(first, second), "should get the same checker on discarded pool");
        }
        finally
        {
            releaseSecond();
        }
    }, sessionMaximumCheckers: 2);
    public static void TestCheckerPoolDiscardIdempotent() => WithFreshPool((_, pool, _) =>
    {
        var(first, releaseFirst) = pool.GetChecker(Context("idem-q"), null);
        Require(first is not null, "expected checker");
        releaseFirst();
        pool.Discard();
        lock (pool.gate)
            Require(pool.checkers.Any(checker => checker is not null), "first Discard should keep idle checkers alive");
        pool.Discard();
        var(second, releaseSecond) = pool.GetChecker(Context("post-idem"), null);
        try
        {
            Require(second is not null, "pool should still work after double Discard");
        }
        finally
        {
            releaseSecond();
        }
    }, sessionMaximumCheckers: 2);
    // Standard synchronization replaces synctest.Wait, observing the real callback.
    private static void WaitUntil(Func<bool> condition, string message)
    {
        Require(SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(10)), message);
    }
}

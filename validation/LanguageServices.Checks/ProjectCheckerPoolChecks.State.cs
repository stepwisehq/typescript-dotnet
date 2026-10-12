// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProjectCheckerPoolChecks
{
    public static void TestCheckerPoolDefaultIdleTimeout()
    {
        var(session, pool) = Setup(new(4, default));
        try
        {
            Require(pool.opts.IdleTimeout == TimeSpan.FromSeconds(30), "zero idle timeout should default to 30s");
        }
        finally
        {
            session.Close();
        }
    }

    public static void TestCheckerPoolAPICheckerSurvivesDiscard() => WithFreshPool((_, pool, _) =>
    {
        var context = ProtocolRequestContext.Default.WithCheckerLifetime(CheckerLifetime.API);
        var(first, releaseFirst) = pool.GetChecker(context, null);
        Require(first is not null, "expected API checker");
        releaseFirst();
        pool.Discard();
        lock (pool.gate)
            Require(ReferenceEquals(pool.persistentChecker, first), "API checker should survive Discard");
        var(second, releaseSecond) = pool.GetChecker(context, null);
        try
        {
            Require(ReferenceEquals(first, second), "API checker identity should be stable after Discard");
        }
        finally
        {
            releaseSecond();
        }
    });
    public static void TestCheckerPoolNonCancelableContextNoAffinity() => WithFreshPool((_, pool, _) =>
    {
        var context = Context("uncancelable-req");
        Require(!context.Cancellation.CanBeCanceled, "test precondition: context must be non-cancelable");
        var(checker, release) = pool.GetChecker(context, null);
        Require(checker is not null, "expected checker");
        release();
        lock (pool.gate)
            Require(pool.requestAssociations.Count == 0, "non-cancelable context must not grow requestAssociations");
    });
    public static void TestCheckerPoolCleanupAfterDiscardIsNoop() => WithFreshPool((_, pool, _) =>
    {
        var(checker, release) = pool.GetChecker(Context("discard-cleanup"), null);
        Require(checker is not null, "expected checker");
        release();
        pool.Discard();
        // Source explicitly invokes the original callback after Stop to test its guard.
        pool.CleanupIdleCheckers();
        lock (pool.gate)
        {
            Require(pool.cleanupTimer is null, "cleanup must not reschedule a timer on a discarded pool");
            Require(pool.checkers.Any(checker => checker is not null), "idle checkers must survive cleanup on a discarded pool");
        }
    });
    public static void TestCheckerPoolTakeNewGlobalDiagnostics()
    {
        var(session, pool) = Setup(new(4, TimeSpan.FromSeconds(10)));
        try
        {
            Require(!pool.TakeNewGlobalDiagnostics(), "should report no new globals initially");
            var source = pool.program.GetSourceFile(new RootedFilePath("/src/index.ts"));
            var(checker, release) = pool.GetChecker(Context("global-diag-req", CheckerLifetime.Diagnostics), source);
            Require(checker is not null, "expected checker");
            checker.GetDiagnostics(CancellationToken.None, source!);
            release();
            Require(pool.TakeNewGlobalDiagnostics(), "diagnostics checker should publish missing-lib globals");
            Require(!pool.TakeNewGlobalDiagnostics(), "TakeNewGlobalDiagnostics should reset after first call");
            var(next, nextRelease) = pool.GetChecker(Context("global-diag-req-2", CheckerLifetime.Diagnostics), source);
            Require(next is not null, "expected checker");
            next.GetDiagnostics(CancellationToken.None, source!);
            nextRelease();
            Require(!pool.TakeNewGlobalDiagnostics(), "should not report new globals when checker state is unchanged");
        }
        finally
        {
            session.Close();
        }
    }

    public static void TestCheckerPoolDiagnosticsCrossReleaseAffinity() => WithFreshPool((_, pool, _) =>
    {
        using var cancellation = new CancellationTokenSource();
        var context = Context("diag-affinity", CheckerLifetime.Diagnostics).WithCancellationToken(cancellation.Token);
        var(first, releaseFirst) = pool.GetChecker(context, null);
        Require(first is not null && ReferenceEquals(pool.checkers[0], first), "should be the diagnostics checker");
        releaseFirst();
        var(second, releaseSecond) = pool.GetChecker(context, null);
        try
        {
            Require(ReferenceEquals(first, second), "same diagnostics request should get the same checker after release");
        }
        finally
        {
            releaseSecond();
            cancellation.Cancel();
        }
    });
    public static void TestCheckerPoolLifetimeMismatchIgnoresAssociation() => WithFreshPool((_, pool, _) =>
    {
        using var cancellation = new CancellationTokenSource();
        var diagnostics = Context("mixed", CheckerLifetime.Diagnostics).WithCancellationToken(cancellation.Token);
        var(first, releaseFirst) = pool.GetChecker(diagnostics, null);
        Require(first is not null && ReferenceEquals(pool.checkers[0], first), "diagnostics checker should be at index 0");
        releaseFirst();
        var(query, releaseQuery) = pool.GetChecker(diagnostics.WithCheckerLifetime(CheckerLifetime.Temporary), null);
        try
        {
            Require(query is not null && !ReferenceEquals(first, query), "query should not reuse the diagnostics checker");
            lock (pool.gate)
                Require(!ReferenceEquals(pool.checkers[0], query), "query checker should not be at diagnostics index 0");
        }
        finally
        {
            releaseQuery();
            cancellation.Cancel();
        }
    });
}

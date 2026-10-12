// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProjectCheckerPoolChecks
{
    // Source setupCheckerPoolSession/newTestCheckerPool; real Session supplies Program.
    private static void WithFreshPool(Action<Session, ProjectCheckerPool, LanguageServicesTestClock> assertion, int maximumCheckers = 4, TimeSpan? idleTimeout = null, int sessionMaximumCheckers = 4)
    {
        var(session, _) = Setup(new(sessionMaximumCheckers, TimeSpan.FromSeconds(10)));
        ProjectCheckerPool? pool = null;
        try
        {
            var(service, error) = session.GetLanguageService(ProtocolRequestContext.Default, new DocumentUri(ByteStringValue.FromString("file:///src/index.ts")));
            Require(error is null && service is not null, "expected language service");
            var clock = new LanguageServicesTestClock();
            // Go's initial time.Now is not its zero-time sentinel.
            clock.Advance(TimeSpan.FromTicks(1));
            pool = new ProjectCheckerPool(new(maximumCheckers, idleTimeout ?? TimeSpan.FromSeconds(30)), service!.GetProgram(), static _ =>
            {
            }, clock);
            assertion(session, pool, clock);
        }
        finally
        {
            pool?.Discard();
            session.Close();
        }
    }

    private static ProtocolRequestContext Context(string requestID, CheckerLifetime lifetime = CheckerLifetime.Temporary) => ProtocolRequestContext.Default.WithRequestID(ByteStringValue.FromString(requestID)).WithCheckerLifetime(lifetime);
    public static void TestCheckerPoolDiagnosticsCheckerStableIdentity() => WithFreshPool((_, pool, _) =>
    {
        var(first, releaseFirst) = pool.GetChecker(Context("diag-stable-1", CheckerLifetime.Diagnostics), null);
        Require(first is not null, "diagnostics checker should be created");
        releaseFirst();
        var(second, releaseSecond) = pool.GetChecker(Context("diag-stable-2", CheckerLifetime.Diagnostics), null);
        try
        {
            Require(ReferenceEquals(first, second), "diagnostics checker should be the same instance before idle timeout");
        }
        finally
        {
            releaseSecond();
        }
    });
    public static void TestCheckerPoolDiagnosticsCheckerSurvivesDiscard() => WithFreshPool((_, pool, _) =>
    {
        var(first, releaseFirst) = pool.GetChecker(Context("diag-discard", CheckerLifetime.Diagnostics), null);
        Require(first is not null, "expected checker");
        releaseFirst();
        pool.Discard();
        lock (pool.gate)
            Require(ReferenceEquals(pool.checkers[0], first), "diagnostics checker should survive Discard");
        var(second, releaseSecond) = pool.GetChecker(Context("diag-discard-2", CheckerLifetime.Diagnostics), null);
        try
        {
            Require(ReferenceEquals(first, second), "diagnostics checker identity should be stable after Discard");
        }
        finally
        {
            releaseSecond();
        }
    });
    public static void TestCheckerPoolDiagnosticsCheckerIndependentFromQuery() => WithFreshPool((_, pool, _) =>
    {
        var(diagnostics, releaseDiagnostics) = pool.GetChecker(Context("diag-indep", CheckerLifetime.Diagnostics), null);
        var(query, releaseQuery) = pool.GetChecker(Context("query-indep"), null);
        try
        {
            Require(!ReferenceEquals(diagnostics, query), "diagnostics and query checkers should be different");
        }
        finally
        {
            releaseDiagnostics();
            releaseQuery();
        }
    });
    public static void TestCheckerPoolAPICheckerStableIdentity() => WithFreshPool((_, pool, clock) =>
    {
        var context = ProtocolRequestContext.Default.WithCheckerLifetime(CheckerLifetime.API);
        var(first, releaseFirst) = pool.GetChecker(context, null);
        Require(first is not null, "expected API checker");
        releaseFirst();
        var(second, releaseSecond) = pool.GetChecker(context, null);
        Require(ReferenceEquals(first, second), "API checker should be the same instance");
        releaseSecond();
        clock.Advance(TimeSpan.FromSeconds(60));
        var(third, releaseThird) = pool.GetChecker(context, null);
        try
        {
            Require(ReferenceEquals(first, third), "API checker should survive idle timeout");
        }
        finally
        {
            releaseThird();
        }
    });
    public static void TestCheckerPoolNoRequestID()
    {
        var(session, pool) = Setup(new(4, TimeSpan.FromSeconds(10)));
        try
        {
            var(first, releaseFirst) = pool.GetChecker(ProtocolRequestContext.Default, null);
            Require(first is not null, "expected checker");
            releaseFirst();
            var(second, releaseSecond) = pool.GetChecker(ProtocolRequestContext.Default, null);
            try
            {
                Require(second is not null, "expected checker without request ID");
            }
            finally
            {
                releaseSecond();
            }
        }
        finally
        {
            session.Close();
        }
    }

    public static void TestCheckerPoolDoubleReleaseSafe()
    {
        var(session, pool) = Setup(new(4, TimeSpan.FromSeconds(10)));
        try
        {
            var(first, releaseFirst) = pool.GetChecker(Context("double-release"), null);
            Require(first is not null, "expected checker");
            releaseFirst();
            releaseFirst();
            var(second, releaseSecond) = pool.GetChecker(Context("after-double"), null);
            try
            {
                Require(second is not null, "pool should remain functional after double release");
            }
            finally
            {
                releaseSecond();
            }
        }
        finally
        {
            session.Close();
        }
    }

    public static void TestCheckerPoolAPICheckerDisposedOnCancel() => WithFreshPool((_, pool, _) =>
    {
        var source = pool.program.GetSourceFile(new RootedFilePath("/src/index.ts"));
        Require(source is not null, "expected source file");
        var context = ProtocolRequestContext.Default.WithCheckerLifetime(CheckerLifetime.API);
        var(first, releaseFirst) = pool.GetChecker(context, null);
        Require(first is not null, "expected API checker");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        first.GetDiagnostics(canceled.Token, source!);
        Require(first.WasCanceled(), "expected canceled checker");
        releaseFirst();
        lock (pool.gate)
            Require(pool.persistentChecker is null, "canceled API checker should be dropped on release");
        var(second, releaseSecond) = pool.GetChecker(context, null);
        try
        {
            Require(!ReferenceEquals(first, second), "should get a fresh API checker after cancellation");
        }
        finally
        {
            releaseSecond();
        }
    });
}

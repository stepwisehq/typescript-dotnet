// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProjectCheckerPoolChecks
{
    private static int QueryIndex(ProjectCheckerPool pool, TypeScript.Checking.Checker checker)
    {
        lock (pool.gate)
            for (int index = 1; index < pool.checkers.Length; index++)
                if (ReferenceEquals(pool.checkers[index], checker))
                    return index;
        return 0;
    }

    public static void TestCheckerPoolIdleCleanup() => WithFreshPool((_, pool, clock) =>
    {
        var(diagnostics, releaseDiagnostics) = pool.GetChecker(Context("diag-cleanup", CheckerLifetime.Diagnostics), null);
        Require(diagnostics is not null, "expected diagnostics checker");
        releaseDiagnostics();
        var(query, releaseQuery) = pool.GetChecker(Context("query-cleanup"), null);
        Require(query is not null, "expected query checker");
        releaseQuery();
        int queryIndex = QueryIndex(pool, query);
        lock (pool.gate)
            Require(pool.checkers[0] is not null && queryIndex > 0, "both checkers should exist");
        clock.Advance(TimeSpan.FromSeconds(5));
        lock (pool.gate)
        {
            Require(pool.checkers[0] is null, "diagnostics checker should be disposed after idle timeout");
            Require(pool.checkers[queryIndex] is null, "query checker should be disposed after idle timeout");
        }
    }, idleTimeout: TimeSpan.FromSeconds(5), sessionMaximumCheckers: 2);
    public static void TestCheckerPoolFileAssociationCleanup() => WithFreshPool((_, pool, clock) =>
    {
        var source = pool.program.GetSourceFile(new RootedFilePath("/src/index.ts"));
        Require(source is not null, "expected source file");
        var(checker, release) = pool.GetChecker(Context("file-assoc-req"), source);
        Require(checker is not null, "expected checker");
        release();
        lock (pool.gate)
            Require(pool.fileAssociations.ContainsKey(source!), "file association should exist");
        clock.Advance(TimeSpan.FromSeconds(5));
        lock (pool.gate)
            Require(!pool.fileAssociations.ContainsKey(source!), "file association should be cleared after checker disposal");
    }, idleTimeout: TimeSpan.FromSeconds(5), sessionMaximumCheckers: 2);
    public static void TestCheckerPoolDiscardKeepsIdleCheckers() => WithFreshPool((_, pool, clock) =>
    {
        var(diagnostics, releaseDiagnostics) = pool.GetChecker(Context("obs-diag", CheckerLifetime.Diagnostics), null);
        Require(diagnostics is not null, "expected diagnostics checker");
        releaseDiagnostics();
        var(query, releaseQuery) = pool.GetChecker(Context("obs-query"), null);
        Require(query is not null, "expected query checker");
        releaseQuery();
        lock (pool.gate)
            Require(pool.checkers[0] is not null, "diagnostics checker should exist");
        pool.Discard();
        lock (pool.gate)
        {
            Require(ReferenceEquals(pool.checkers[0], diagnostics), "diagnostics checker should survive Discard");
            Require(QueryIndex(pool, query) > 0, "query checker should survive Discard");
            Require(pool.cleanupTimer is null, "cleanup timer should be stopped after Discard");
        }

        clock.Advance(TimeSpan.FromSeconds(60));
        lock (pool.gate)
            Require(ReferenceEquals(pool.checkers[0], diagnostics), "diagnostics checker should persist indefinitely on discarded pool");
    }, sessionMaximumCheckers: 2);
    public static void TestCheckerPoolDiscardHeldCheckerSurvivesRelease() => WithFreshPool((_, pool, clock) =>
    {
        var(checker, release) = pool.GetChecker(Context("held-obs"), null);
        Require(checker is not null, "expected checker");
        int index = QueryIndex(pool, checker);
        Require(index > 0, "should find the held checker");
        pool.Discard();
        lock (pool.gate)
            Require(ReferenceEquals(pool.checkers[index], checker), "held checker should survive Discard");
        release();
        lock (pool.gate)
            Require(ReferenceEquals(pool.checkers[index], checker), "checker should persist after release on discarded pool");
        clock.Advance(TimeSpan.FromSeconds(60));
        lock (pool.gate)
            Require(ReferenceEquals(pool.checkers[index], checker), "checker should persist indefinitely on discarded pool");
    }, sessionMaximumCheckers: 2);
    public static void TestCheckerPoolStaggeredIdleCleanup() => WithFreshPool((_, pool, clock) =>
    {
        var(first, releaseFirst) = pool.GetChecker(Context("stagger-A"), null);
        Require(first is not null, "expected first checker");
        var(second, releaseSecond) = pool.GetChecker(Context("stagger-B"), null);
        Require(second is not null && !ReferenceEquals(first, second), "B should be a different checker since A is held");
        int firstIndex = QueryIndex(pool, first), secondIndex = QueryIndex(pool, second);
        Require(firstIndex > 0 && secondIndex > 0, "expected query slots");
        releaseFirst();
        clock.Advance(TimeSpan.FromSeconds(6));
        releaseSecond();
        lock (pool.gate)
        {
            Require(pool.checkers[firstIndex] is not null, "checker A should still exist before timer fires");
            Require(pool.checkers[secondIndex] is not null, "checker B should still exist before timer fires");
        }

        clock.Advance(TimeSpan.FromSeconds(11));
        lock (pool.gate)
        {
            Require(pool.checkers[firstIndex] is null, "checker A should be disposed after timer fires");
            Require(pool.checkers[secondIndex] is null, "checker B should be disposed after timer fires");
        }
    }, idleTimeout: TimeSpan.FromSeconds(10));
}

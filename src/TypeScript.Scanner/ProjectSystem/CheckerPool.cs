// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/project/checkerpool.go owner at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Diagnostics;
using TypeScript.Checking;
using TypeScript.Compiler;
using TypeScript.Lsp.Protocol;
using TypeScript.Scanner;
using Diagnostic = TypeScript.Scanner.Diagnostic;

namespace TypeScript.ProjectSystem;
internal sealed class ProjectCheckerPool : ICheckerPool
{
    private static readonly ByteStringValue anonymous = ByteStringValue.FromString("<anonymous>");
    private readonly object gate = new();
    private readonly CheckerPoolOptions opts;
    private readonly Program program;
    private readonly Checker? [] checkers;
    private readonly ByteStringValue[] heldBy;
    private readonly Dictionary<SourceFile, int> fileAssociations = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<ByteStringKey, int> requestAssociations = new();
    private readonly long[] lastReleased;
    private readonly int[] globalDiagCheckerCount;
    private readonly SemaphoreSlim diagSem = new(1, 1);
    private readonly SemaphoreSlim querySem;
    private readonly SemaphoreSlim persistentSem = new(1, 1);
    private readonly Action<string> log;
    private bool discarded;
    private Timer? cleanupTimer;
    private Checker? persistentChecker;
#pragma warning disable CS0414 // checkerpool.go retains this state although the pinned owner never reads it.

    private bool persistentHeld;
#pragma warning restore CS0414
    private Slice<Diagnostic> globalDiagAccumulated;
    private bool globalDiagChanged;
    // checkerpool.go:84 newCheckerPool. Semaphores count free slots instead of occupied channel slots.
    internal ProjectCheckerPool(CheckerPoolOptions options, Program program, Action<string>? log)
    {
        opts = options with
        {
            MaxCheckers = options.MaxCheckers <= 0 ? 4 : Math.Max(options.MaxCheckers, 2),
            IdleTimeout = options.IdleTimeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : options.IdleTimeout
        };
        this.program = program;
        this.log = log ?? NoLog;
        int slots = checked((int)opts.MaxCheckers);
        checkers = new Checker? [slots];
        heldBy = new ByteStringValue[slots];
        lastReleased = new long[slots];
        globalDiagCheckerCount = new int[slots];
        querySem = new(slots - 1, slots - 1);
    }

    private static void NoLog(string message)
    {
    }

    // checkerpool.go:116 holdTag.
    private static ByteStringValue HoldTag(ByteStringValue requestID) => requestID.Length == 0 ? anonymous : requestID;
    // checkerpool.go:123 GetChecker. Ordinary compiler callers have background/default lifetime.
    public (Checker Checker, Action Done) GetChecker(CancellationToken context, SourceFile? file) => GetQueryChecker(context, default, file);
    internal (Checker Checker, Action Done) GetChecker(ProtocolRequestContext context, SourceFile? file)
    {
        var requestID = context.Cancellation.CanBeCanceled ? context.RequestID : default;
        return context.CheckerLifetime switch
        {
            CheckerLifetime.Diagnostics => GetDiagnosticsChecker(context.Cancellation, requestID),
            CheckerLifetime.API => GetPersistentChecker(),
            _ => GetQueryChecker(context.Cancellation, requestID, file)};
    }

    // checkerpool.go:158 tryReacquireForRequest. Acquisition is deliberately not canceled by ctx.
    private (Checker? Checker, Action? Release, bool Ok) TryReacquireForRequest(ByteStringValue requestID, SemaphoreSlim semaphore, bool isDiag)
    {
        semaphore.Wait();
        if (requestID.Length == 0)
            return (null, null, false);
        lock (gate)
        {
            var key = ByteStringKey.FromValue(requestID);
            if (!requestAssociations.TryGetValue(key, out var index))
                return (null, null, false);
            if ((isDiag && index != 0) || (!isDiag && index == 0) || checkers[index] is null)
            {
                requestAssociations.Remove(key);
                return (null, null, false);
            }

            if (heldBy[index].Length != 0)
                return (null, null, false);
            var checker = checkers[index]!;
            heldBy[index] = requestID;
            return (checker, CreateRelease(requestID, index, checker), true);
        }
    }

    // checkerpool.go:194 getDiagnosticsChecker.
    private (Checker Checker, Action Done) GetDiagnosticsChecker(CancellationToken context, ByteStringValue requestID)
    {
        var acquired = TryReacquireForRequest(requestID, diagSem, true);
        if (acquired.Ok)
            return (acquired.Checker!, acquired.Release!);
        lock (gate)
        {
            if (checkers[0] is null)
            {
                log("checkerpool: Creating diagnostics checker");
                checkers[0] = Checker.NewChecker(program).Checker;
            }

            var checker = checkers[0]!;
            heldBy[0] = HoldTag(requestID);
            log("checkerpool: Acquired diagnostics checker for request " + HoldTag(requestID).Materialize());
            AssociateRequest(context, requestID, 0);
            return (checker, CreateRelease(requestID, 0, checker));
        }
    }

    // checkerpool.go:226 getQueryChecker.
    private (Checker Checker, Action Done) GetQueryChecker(CancellationToken context, ByteStringValue requestID, SourceFile? file)
    {
        var acquired = TryReacquireForRequest(requestID, querySem, false);
        if (acquired.Ok)
            return (acquired.Checker!, acquired.Release!);
        lock (gate)
        {
            if (file is not null && fileAssociations.TryGetValue(file, out var index) && index > 0 && checkers[index] is Checker associated && heldBy[index].Length == 0)
            {
                heldBy[index] = HoldTag(requestID);
                AssociateRequest(context, requestID, index);
                return (associated, CreateRelease(requestID, index, associated));
            }

            var(checker, slot) = FindOrCreateQueryCheckerLocked();
            heldBy[slot] = HoldTag(requestID);
            log($"checkerpool: Acquired query checker {slot} for request {HoldTag(requestID).Materialize()}");
            AssociateRequest(context, requestID, slot);
            if (file is not null)
                fileAssociations[file] = slot;
            return (checker, CreateRelease(requestID, slot, checker));
        }
    }

    private void AssociateRequest(CancellationToken context, ByteStringValue requestID, int index)
    {
        if (requestID.Length == 0)
            return;
        var key = ByteStringKey.FromValue(requestID);
        if (requestAssociations.ContainsKey(key))
            return;
        requestAssociations[key] = index;
        RegisterRequestCleanup(context, key);
    }

    // checkerpool.go:270 findOrCreateQueryCheckerLocked.
    private (Checker Checker, int Index) FindOrCreateQueryCheckerLocked()
    {
        for (int i = 1; i < checkers.Length; i++)
            if (checkers[i] is Checker existing && heldBy[i].Length == 0)
                return (existing, i);
        for (int i = 1; i < checkers.Length; i++)
            if (checkers[i] is null)
            {
                log($"checkerpool: Creating query checker {i}");
                var checker = Checker.NewChecker(program).Checker;
                checkers[i] = checker;
                return (checker, i);
            }

        throw new InvalidOperationException("checkerpool: no available query slot despite holding semaphore token");
    }

    // checkerpool.go:289 getPersistentChecker.
    private (Checker Checker, Action Done) GetPersistentChecker()
    {
        persistentSem.Wait();
        Checker checker;
        lock (gate)
        {
            if (persistentChecker is null)
            {
                log("checkerpool: Creating persistent checker");
                persistentChecker = Checker.NewChecker(program).Checker;
            }

            checker = persistentChecker;
            persistentHeld = true;
        }

        int released = 0;
        return (checker, () =>
        {
            if (Interlocked.Exchange(ref released, 1) != 0)
                return;
            lock (gate)
            {
                persistentHeld = false;
                if (checker.WasCanceled())
                {
                    log("checkerpool: Persistent checker was canceled, disposing");
                    if (ReferenceEquals(persistentChecker, checker))
                        persistentChecker = null;
                }
            }

            persistentSem.Release();
        });
    }

    // checkerpool.go:319 createRelease. Transferable, idempotent leases retain this pool.
    private Action CreateRelease(ByteStringValue requestID, int index, Checker checker)
    {
        int released = 0;
        return () =>
        {
            if (Interlocked.Exchange(ref released, 1) != 0)
                return;
            lock (gate)
            {
                if (checker.WasCanceled())
                {
                    log($"checkerpool: Checker {index} for request {HoldTag(requestID).Materialize()} was canceled, disposing");
                    DisposeCheckerLocked(index, checker);
                }
                else
                {
                    if (index == 0)
                        MergeGlobalDiagnosticsFromCheckerLocked(index, checker);
                    heldBy[index] = default;
                    lastReleased[index] = Stopwatch.GetTimestamp();
                    if (!discarded)
                        ScheduleCleanupLocked();
                }
            }

            (index == 0 ? diagSem : querySem).Release();
        };
    }

    // checkerpool.go:361 registerRequestCleanup. AfterFunc schedules asynchronously, even for an already-canceled token.
    private void RegisterRequestCleanup(CancellationToken context, ByteStringKey requestID)
    {
        context.UnsafeRegister(static state =>
        {
            var cleanup = ((ProjectCheckerPool Pool, ByteStringKey RequestID))state!;
            ThreadPool.UnsafeQueueUserWorkItem(static value =>
            {
                lock (value.Pool.gate)
                    value.Pool.requestAssociations.Remove(value.RequestID);
            }, cleanup, preferLocal: false);
        }, (this, requestID));
    }

    // checkerpool.go:373 scheduleCleanupLocked. Stopwatch retains Go time.Now's monotonic deadline behavior.
    private void ScheduleCleanupLocked()
    {
        TimeSpan? earliest = null;
        for (int i = 0; i < checkers.Length; i++)
        {
            if (checkers[i] is null || heldBy[i].Length != 0 || lastReleased[i] == 0)
                continue;
            var delay = opts.IdleTimeout - Stopwatch.GetElapsedTime(lastReleased[i]);
            if (earliest is null || delay < earliest.Value)
                earliest = delay;
        }

        if (earliest is null)
        {
            cleanupTimer?.Dispose();
            cleanupTimer = null;
            return;
        }

        var due = earliest.Value <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : earliest.Value;
        if (cleanupTimer is null)
            cleanupTimer = new Timer(static state => ((ProjectCheckerPool)state!).CleanupIdleCheckers(), this, due, Timeout.InfiniteTimeSpan);
        else
            cleanupTimer.Change(due, Timeout.InfiniteTimeSpan);
    }

    // checkerpool.go:405 cleanupIdleCheckers.
    private void CleanupIdleCheckers()
    {
        lock (gate)
        {
            if (discarded)
                return;
            for (int i = 0; i < checkers.Length; i++)
            {
                if (checkers[i] is not Checker checker || heldBy[i].Length != 0 || lastReleased[i] == 0)
                    continue;
                var idle = Stopwatch.GetElapsedTime(lastReleased[i]);
                if (idle >= opts.IdleTimeout)
                {
                    log($"checkerpool: Disposing idle checker {i} (idle {Logging.LogFormat.Sprintf("%v", idle)})");
                    DisposeCheckerLocked(i, checker);
                }
            }

            ScheduleCleanupLocked();
        }
    }

    // checkerpool.go:437 disposeCheckerLocked.
    private void DisposeCheckerLocked(int index, Checker checker)
    {
        if (!ReferenceEquals(checkers[index], checker))
            throw new InvalidOperationException("checkerpool: disposing an unexpected checker");
        checkers[index] = null;
        heldBy[index] = default;
        globalDiagCheckerCount[index] = 0;
        lastReleased[index] = 0;
        foreach (var item in fileAssociations)
            if (item.Value == index)
                fileAssociations.Remove(item.Key);
        foreach (var item in requestAssociations)
            if (item.Value == index)
                requestAssociations.Remove(item.Key);
    }

    // checkerpool.go:458 mergeGlobalDiagnosticsFromCheckerLocked.
    private void MergeGlobalDiagnosticsFromCheckerLocked(int index, Checker checker)
    {
        var globals = checker.GetGlobalDiagnostics();
        if (globals.Length == globalDiagCheckerCount[index])
            return;
        globalDiagCheckerCount[index] = globals.Length;
        int before = globalDiagAccumulated.Length;
        globalDiagAccumulated = Program.SortAndDeduplicateDiagnostics(globalDiagAccumulated.Append(globals));
        if (globalDiagAccumulated.Length != before)
            globalDiagChanged = true;
    }

    // checkerpool.go:473 GetGlobalDiagnostics.
    internal Slice<Diagnostic> GetGlobalDiagnostics()
    {
        lock (gate)
            return GoSlices.Clone(globalDiagAccumulated);
    }

    // checkerpool.go:481 TakeNewGlobalDiagnostics.
    internal bool TakeNewGlobalDiagnostics()
    {
        lock (gate)
        {
            bool changed = globalDiagChanged;
            globalDiagChanged = false;
            return changed;
        }
    }

    // checkerpool.go:493 Discard. No cancellation/disposal of active or persistent owners.
    internal void Discard()
    {
        lock (gate)
        {
            if (discarded)
                return;
            log("checkerpool: Discarding pool, stopping idle cleanup");
            discarded = true;
            cleanupTimer?.Dispose();
            cleanupTimer = null;
        }
    }
}

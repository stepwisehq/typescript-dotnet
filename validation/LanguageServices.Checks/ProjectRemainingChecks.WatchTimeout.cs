// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Whole internal/project/watchtimeout_test.go:18, pinned fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;
using TypeScript.ProjectSystem;
using TypeScript.Lsp.Protocol;
using Input = Program.Checks.CompilerBaselineFileSystem.Input;
using Range = TypeScript.Lsp.Protocol.Range;

namespace LanguageServices.Checks;
public static partial class ProjectRemainingChecks
{
    internal static void TestUpdateWatchTimeoutAndRollback()
    {
        // Go synctest's scheduling/virtual sleeps become an observed callback-entry
        // barrier, real two-second sleeps and actual background-work completion.
        // Production's one-second CTS timer remains unchanged; elapsed time is
        // unasserted. Completion preserves quiescence before the next phase.
        Groups(("watch retries on next snapshot update after timeout with same watcher identity", () =>
        {
            const string path = "/home/projects/TS/p1/src/index.ts";
            const string content = "export const x = 1;";
            using ManualResetEventSlim entered = new();
            using var fixture = new ProjectSessionFixture([new Input("/home/projects/TS/p1/tsconfig.json", "{\n\t\t\t\"compilerOptions\": { \"noLib\": true, \"strict\": true }\n\t\t}"), new Input(path, content), ]);
            object gate = new();
            List<WatcherID> attemptedIDs = new(), successfulIDs = new();
            int firstBatchDone = 0;
            fixture.Client.WatchFilesFunc = (context, id, watchers) =>
            {
                lock (gate)
                    attemptedIDs.Add(id);
                entered.Set();
                if (Volatile.Read(ref firstBatchDone) == 0)
                {
                    context.Cancellation.WaitHandle.WaitOne();
                    return new OperationCanceledException(context.Cancellation);
                }

                lock (gate)
                    successfulIDs.Add(id);
                return null;
            };
            Open(fixture, path, content);
            // Only a deadlock safety bound; no added one-second assertion.
            if (!entered.Wait(TimeSpan.FromSeconds(30)))
                throw new TimeoutException("Original watch callback did not enter");
            Thread.Sleep(TimeSpan.FromSeconds(2));
            // Callback entry alone cannot establish the subsequent rollback or
            // registration publication. Drain the genuine background batch, as
            // Go synctest scheduling does before the next phase is observed.
            fixture.Session.WaitForBackgroundTasks();
            WatcherID[] firstAttemptIDs;
            lock (gate)
            {
                firstAttemptIDs = attemptedIDs.ToArray();
                Check(firstAttemptIDs.Length >= 1);
                Check(successfulIDs.Count == 0);
            }

            Volatile.Write(ref firstBatchDone, 1);
            Change(fixture, path, "2", new Range { Start = new Position { Line = 0, Character = 18 }, End = new Position { Line = 0, Character = 19 }, });
            _ = Service(fixture, path);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            // Callback entry alone cannot establish the subsequent rollback or
            // registration publication. Drain the genuine background batch, as
            // Go synctest scheduling does before the next phase is observed.
            fixture.Session.WaitForBackgroundTasks();
            lock (gate)
            {
                var retryIDs = attemptedIDs.GetRange(firstAttemptIDs.Length, attemptedIDs.Count - firstAttemptIDs.Length);
                Check(retryIDs.Count >= 1);
                var firstAttemptSet = new HashSet<WatcherID>(firstAttemptIDs);
                var retrySet = new HashSet<WatcherID>(retryIDs);
                foreach (var id in retrySet)
                    Check(firstAttemptSet.Contains(id));
                int successfulRetriedCount = 0;
                foreach (var id in successfulIDs)
                    if (retrySet.Contains(id))
                        successfulRetriedCount++;
                Check(successfulRetriedCount >= 1);
            }
        }));
    }
}

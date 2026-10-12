// Authored runtime-boundary control; not an upstream original test function.
using TypeScript.ProjectSystem;
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;

namespace LanguageServices.Checks;
public static partial class ProjectCheckerPoolChecks
{
    public static int RunAuthoredControls()
    {
        WithFreshPool((_, pool, clock) =>
        {
            var(checker, release) = pool.GetChecker(Context("long-idle"), null);
            int index = QueryIndex(pool, checker);
            Require(index > 0, "expected query slot");
            release();
            // A bounded CLR timer may fire earlier than the original deadline;
            // that callback must retain the checker and schedule the remainder.
            clock.Advance(TimeSpan.FromDays(25));
            lock (pool.gate)
                Require(ReferenceEquals(pool.checkers[index], checker), "long idle timeout must not dispose a checker early");
            clock.Advance(TimeSpan.FromDays(15));
            lock (pool.gate)
                Require(pool.checkers[index] is null, "long idle timeout must dispose at its original deadline");
        }, idleTimeout: TimeSpan.FromDays(40));
        return 1;
    }
}

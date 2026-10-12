// Authored control for the Go WaitGroup's actual multi-phase registry owner adaptation.
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class AutoImportWorkGroupChecks
{
    public static int RunAll()
    {
        var group = new ParallelWorkGroup(reusable: true);
        int completed = 0;
        for (int phase = 0; phase < 4; phase++)
        {
            for (int job = 0; job < 3; job++)
                group.Queue(() =>
                {
                    Interlocked.Increment(ref completed);
                    group.Queue(() => Interlocked.Increment(ref completed));
                });
            group.RunAndWait();
            if (Volatile.Read(ref completed) != (phase + 1) * 6)
                throw new InvalidOperationException("A quiescent registry phase lost or repeated queued work");
        }

        var failed = new ParallelWorkGroup(reusable: true);
        var marker = new InvalidOperationException("terminal registry phase failure");
        failed.Queue(() => throw marker);
        try
        {
            failed.RunAndWait();
            throw new InvalidOperationException("Worker failure was lost");
        }
        catch (InvalidOperationException error)when (ReferenceEquals(error, marker))
        {
        }

        try
        {
            failed.Queue(static () =>
            {
            });
        }
        catch (InvalidOperationException)
        {
            return 1;
        }

        throw new InvalidOperationException("A failed registry owner incorrectly accepted a new phase");
    }
}

// Authored counterpart of original Go synctest.Wait at this progress owner's
// genuine pending channel-read/timer boundary. No events or output are injected.
using System.Diagnostics;

namespace LanguageServices.Checks;
internal sealed class ProgressQuiescence
{
    private readonly object gate = new();
    private Task? read, timer;
    internal void OnWait(Task pendingRead, Task? pendingTimer)
    {
        lock (gate)
        {
            read = pendingRead;
            timer = pendingTimer;
            Monitor.PulseAll(gate);
        }
    }

    internal void Wait()
    {
        long start = Stopwatch.GetTimestamp();
        lock (gate)
        {
            // A completed read means a queued event still needs consumption; a
            // completed timer likewise means the worker has not settled again.
            while (read is null || read.IsCompleted || timer is { IsCompleted: true })
            {
                var remaining = TimeSpan.FromSeconds(30) - Stopwatch.GetElapsedTime(start);
                if (remaining <= TimeSpan.Zero)
                    throw new TimeoutException("progress did not reach its actual blocked read/timer boundary");
                Monitor.Wait(gate, remaining);
            }
        }
    }
}

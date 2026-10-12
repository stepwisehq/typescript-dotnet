// Authored behavior controls for the Linux POSIX checker mutex representation.
// Same controls exercise the portable SemaphoreSlim fallback; no performance claims.
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using TypeScript.Scanner;

namespace Emit.Checks;
public static class CheckerMutexChecks
{
    public static readonly string[] Names = ["MutualExclusion", "TransferableNonReentrant", "OverReleaseAndFallback"];
    public static int RunAll()
    {
        foreach (string name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        foreach (bool fallback in new[]
        {
            false,
            true
        }

        )
        {
            var mutex = new CheckerMutex(forceFallback: fallback);
            if (fallback)
                Check(!mutex.UsesNativeSemaphore, "forced portable fallback");
            else
                AssertSelectedBackend(mutex);
            switch (name)
            {
                case "MutualExclusion":
                    MutualExclusion(mutex);
                    break;
                case "TransferableNonReentrant":
                    TransferableNonReentrant(mutex);
                    break;
                case "OverReleaseAndFallback":
                    OverRelease(mutex);
                    break;
                default:
                    throw new ArgumentException(name);
            }
        }
    }

    private static void AssertSelectedBackend(CheckerMutex mutex)
    {
        bool expectedNative = false;
        if (OperatingSystem.IsLinux() && RuntimeInformation.ProcessArchitecture == Architecture.X64 && NativeLibrary.TryLoad("libc.so.6", out nint library))
        {
            try
            {
                expectedNative = NativeLibrary.TryGetExport(library, "gnu_get_libc_version", out _) && NativeLibrary.TryGetExport(library, "sem_init", out _) && NativeLibrary.TryGetExport(library, "sem_wait", out _) && NativeLibrary.TryGetExport(library, "sem_post", out _) && NativeLibrary.TryGetExport(library, "sem_destroy", out _);
            }
            finally
            {
                NativeLibrary.Free(library);
            }
        }

        Check(mutex.UsesNativeSemaphore == expectedNative, "actual supported Linux x64 glibc selects native; other platforms select fallback");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void MutualExclusion(CheckerMutex mutex)
    {
        int active = 0, completed = 0;
        var failures = new ConcurrentQueue<Exception>();
        var threads = new Thread[4];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                try
                {
                    for (int iteration = 0; iteration < 1000; iteration++)
                    {
                        mutex.Wait();
                        try
                        {
                            Check(Interlocked.Increment(ref active) == 1, "mutex excludes other workers");
                            int next = completed + 1;
                            Thread.SpinWait(16);
                            completed = next;
                            Check(Interlocked.Decrement(ref active) == 0, "sole owner exits");
                        }
                        finally
                        {
                            mutex.Release();
                        }
                    }
                }
                catch (Exception failure)
                {
                    failures.Enqueue(failure);
                }
            })
            {
                IsBackground = true
            };
            threads[i].Start();
        }

        foreach (var thread in threads)
            Check(thread.Join(TimeSpan.FromSeconds(10)), "mutex worker completes");
        if (failures.TryDequeue(out var failure))
            throw failure;
        Check(completed == 4000, "all protected increments retained");
    }

    private static void TransferableNonReentrant(CheckerMutex mutex)
    {
        using var firstAcquired = new ManualResetEventSlim();
        using var secondAcquired = new ManualResetEventSlim();
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                mutex.Wait();
                firstAcquired.Set();
                mutex.Wait(); // Must park even though the same thread holds the first lease.
                secondAcquired.Set();
                mutex.Release();
            }
            catch (Exception error)
            {
                failure = error;
            }
        })
        {
            IsBackground = true
        };
        thread.Start();
        Check(firstAcquired.Wait(TimeSpan.FromSeconds(10)), "first lease acquired");
        Check(!secondAcquired.Wait(TimeSpan.FromMilliseconds(100)), "acquisition is nonreentrant");
        mutex.Release(); // Original Go Unlock and public Done may use another thread.
        Check(thread.Join(TimeSpan.FromSeconds(10)), "cross-thread release wakes recursive waiter");
        if (failure is not null)
            throw failure;
        Check(secondAcquired.IsSet, "second lease acquired after release");
    }

    private static void OverRelease(CheckerMutex mutex)
    {
        bool rejected = false;
        try
        {
            mutex.Release();
        }
        catch (SemaphoreFullException)
        {
            rejected = true;
        }

        Check(rejected, "unheld binary mutex release rejected");
        mutex.Wait();
        mutex.Release();
        rejected = false;
        try
        {
            mutex.Release();
        }
        catch (SemaphoreFullException)
        {
            rejected = true;
        }

        Check(rejected, "second release rejected");
        mutex.Wait();
        mutex.Release(); // Rejection must not corrupt future acquisitions.
    }
}

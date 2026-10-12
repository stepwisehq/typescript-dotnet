// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Original TestSubscribeNoGoroutineLeak. CLR dedicated-thread observation adaptation:
// WatcherBase/debounce loops are dedicated Threads; this is not a goroutine-count equivalence claim.
// The xUnit wrapper must use a DisableParallelization collection; Native RunAll is sequential.
using System.Diagnostics;
using TypeScript.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    private static int RuntimeThreadCount()
    {
        using var process = Process.GetCurrentProcess();
        return process.Threads.Count;
    }

    public static int RunAdaptedRuntimeObservation()
    {
        TestSubscribeNoGoroutineLeak();
        return 1;
    }

    public static void TestSubscribeNoGoroutineLeak()
    {
        foreach (var watcher in AvailableWatchers())
        {
            using var fixture = new Fixture(nameof(TestSubscribeNoGoroutineLeak), 1);
            string dir = fixture.NewTmpDir();
            var(warmup, error) = watcher.WatchDirectory(Text(dir), static (_, _) =>
            {
            });
            Check(error is null && warmup is not null, "warmup failed: " + error?.Message);
            Check(warmup!.Close()is null, "warmup Close failed");
            GC.Collect();
            Thread.Sleep(100);
            int baseline = RuntimeThreadCount();
            for (int index = 0; index < 8; index++)
            {
                var recorder = new Recorder();
                var(watch, subscribeError) = watcher.WatchDirectory(Text(dir), recorder.Callback);
                Check(subscribeError is null && watch is not null, "subscribe failed: " + subscribeError?.Message);
                Check(watch!.Close()is null, "Close failed");
            }

            long start = Stopwatch.GetTimestamp();
            bool settled = false;
            while (Stopwatch.GetElapsedTime(start) < TimeSpan.FromSeconds(2))
            {
                GC.Collect();
                if (RuntimeThreadCount() <= baseline + 2)
                {
                    settled = true;
                    break;
                }

                Thread.Sleep(50);
            }

            Check(settled, $"thread leak: baseline={baseline} now={RuntimeThreadCount()}");
        }
    }
}

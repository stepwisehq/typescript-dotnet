using System.Runtime;
using System.Text.Json;
internal sealed class NoGcRegionPolicy : IDisposable
{
    private readonly GCLatencyMode initial = GCSettings.LatencyMode;
    private readonly string? report;
    private readonly GCLatencyMode effective;
    private const long TotalBudget = 14L * 1024 * 1024 * 1024;
    private const long LohBudget = 2L * 1024 * 1024 * 1024;
    private const long MinimumAvailable = 20L * 1024 * 1024 * 1024;
    private bool started;
    private long availableBytes = -1;
    private string startOutcome = "notAttempted", endOutcome = "notStarted";
    private readonly int[] countsBefore = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
    private readonly long allocatedBefore = GC.GetTotalAllocatedBytes(false);
    internal NoGcRegionPolicy(string memoryInfoPath = "/proc/meminfo", string? reportPath = null)
    {
        report = reportPath ?? Environment.GetEnvironmentVariable("TSC_GC_POLICY_REPORT");
        try
        {
            // Availability is a Linux preflight observation, never an RSS limit.
            if (OperatingSystem.IsLinux())
                foreach (string line in File.ReadLines(memoryInfoPath))
                    if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
                    {
                        availableBytes = checked(long.Parse(line.Split((char[]?)null,
                            StringSplitOptions.RemoveEmptyEntries)[1], System.Globalization.CultureInfo.InvariantCulture) * 1024);
                        break;
                    }
            if (availableBytes < MinimumAvailable) startOutcome = "preflightFallback";
            else
            {
                started = GC.TryStartNoGCRegion(TotalBudget, LohBudget, disallowFullBlockingGC: true);
                startOutcome = started ? "started" : "insufficientMemoryFallback";
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or FormatException or
            OverflowException or ArgumentException or InvalidOperationException or IndexOutOfRangeException)
        {
            startOutcome = "exceptionFallback:" + error.GetType().Name + ":" + error.Message;
        }
        effective = GCSettings.LatencyMode;
    }

    public void Dispose()
    {
        var modeBeforeEnd = GCSettings.LatencyMode;
        int[] countsAfter = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
        long allocatedAfter = GC.GetTotalAllocatedBytes(false);
        if (started)
        {
            try { GC.EndNoGCRegion(); endOutcome = "endedSuccessfully"; }
            catch (InvalidOperationException error) { endOutcome = "regionFailure:" + error.Message; }
        }
        GCSettings.LatencyMode = initial;
        var restored = GCSettings.LatencyMode;
        if (report is null) return;
        try
        {
            using var stream = File.Create(report);
            using var w = new Utf8JsonWriter(stream);
            w.WriteStartObject();
            w.WriteString("initial",initial.ToString());
            w.WriteString("requested","NoGCRegion");
            w.WriteString("effective",effective.ToString());
            w.WriteString("restored",restored.ToString());
            w.WriteBoolean("restoredExact",restored==initial);
            w.WriteBoolean("serverGC",GCSettings.IsServerGC);
            w.WriteNumber("processorCount",Environment.ProcessorCount);
            w.WriteBoolean("noGcRequested", true);
            w.WriteBoolean("noGcStarted", started);
            w.WriteString("noGcStartOutcome", startOutcome);
            w.WriteString("noGcEndOutcome", endOutcome);
            w.WriteString("modeBeforeEnd",modeBeforeEnd.ToString());
            w.WriteBoolean("fallbackAtStart",!started);
            w.WriteNumber("totalBudgetBytes",TotalBudget);
            w.WriteNumber("lohBudgetBytes",LohBudget);
            w.WriteNumber("minimumAvailableBytes",MinimumAvailable);
            w.WriteNumber("preflightAvailableBytes",availableBytes);
            w.WriteNumber("allocatedBeforeBytes",allocatedBefore);
            w.WriteNumber("allocatedAfterBytes",allocatedAfter);
            w.WriteStartArray("collectionCountsBefore");foreach(int count in countsBefore)w.WriteNumberValue(count);
            w.WriteEndArray();
            w.WriteStartArray("collectionCountsAfter");foreach(int count in countsAfter)w.WriteNumberValue(count);
            w.WriteEndArray();
            w.WriteString("noGcLimits","Optional one-shot execution policy; runtime may resume GC automatically; budgets are not a process memory limit. Before/after counters include startup policy work; final serialization occurs after ending/restoring policy.");
            w.WriteEndObject();
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (ArgumentException) { }
        catch (NotSupportedException) { }
    }
}

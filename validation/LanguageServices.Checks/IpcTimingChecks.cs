// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete original internal/ipc/timing_test.go, four test functions.
using TypeScript.Ipc;
using TypeScript.Scanner;

namespace LanguageServices.Checks;
public static class IpcTimingChecks
{
    public static IEnumerable<string> Names => ["TestTimingCollector", "TestServerTimingSnapshotDisabled", "TestTimingCollectorReset", "TestDurationToMillis"];

    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return 4;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestTimingCollector":
                TestTimingCollector();
                break;
            case "TestServerTimingSnapshotDisabled":
                TestServerTimingSnapshotDisabled();
                break;
            case "TestTimingCollectorReset":
                TestTimingCollectorReset();
                break;
            case "TestDurationToMillis":
                TestDurationToMillis();
                break;
            default:
                throw new ArgumentException(name);
        }
    }

    private static ByteStringValue Text(string text) => ByteStringValue.FromString(text);
    private static void Equal<T>(T actual, T expected)
        where T : IEquatable<T>
    {
        if (!actual.Equals(expected))
            throw new InvalidOperationException($"expected {expected}, got {actual}");
    }

    private static void EqualText(ByteStringValue actual, string expected)
    {
        if (!actual.EqualsValue(Text(expected)))
            throw new InvalidOperationException("timing method differs");
    }

    private static void TestTimingCollector()
    {
        var collector = TimingCollector.NewTimingCollector();
        collector.Record(Text("getSourceFile"), 2_000_000);
        collector.Record(Text("getSymbolAtPosition"), 500_000);
        var snapshot = collector.Snapshot();
        Equal(snapshot.Enabled, true);
        Equal(snapshot.Totals.RequestCount, 2UL);
        Equal(snapshot.Totals.TotalProcessingTimeMs, 2.5);
        Equal(snapshot.RecentRequests.Length, 2);
        EqualText(snapshot.RecentRequests[0].Method, "getSourceFile");
        Equal(snapshot.RecentRequests[0].ProcessingTimeMs, 2d);
        EqualText(snapshot.RecentRequests[1].Method, "getSymbolAtPosition");
        Equal(snapshot.RecentRequests[1].ProcessingTimeMs, 0.5);
        collector = TimingCollector.NewTimingCollector();
        string[] methods = ["a", "b", "c", "d", "e", "f", "g"];
        foreach (var method in methods)
            collector.Record(Text(method), 1_000_000);
        snapshot = collector.Snapshot();
        Equal(snapshot.Totals.RequestCount, 7UL);
        Equal(snapshot.RecentRequests.Length, TimingCollector.ServerRecentRequestCapacity);
        for (int i = 0; i < TimingCollector.ServerRecentRequestCapacity; i++)
            EqualText(snapshot.RecentRequests[i].Method, methods[i + 2]);
        collector = TimingCollector.NewTimingCollector();
        collector.Record(Text("x"), -5_000_000_000);
        snapshot = collector.Snapshot();
        Equal(snapshot.Totals.TotalProcessingTimeMs, 0d);
        Equal(snapshot.RecentRequests[0].ProcessingTimeMs, 0d);
    }

    private static void TestServerTimingSnapshotDisabled()
    {
        var snapshot = Ipc.ServerTimingSnapshot(null);
        Equal(snapshot.Enabled, false);
        Equal(snapshot.Totals.RequestCount, 0UL);
        Equal(snapshot.RecentRequests.Length, 0);
    }

    private static void TestTimingCollectorReset()
    {
        var collector = TimingCollector.NewTimingCollector();
        collector.Record(Text("a"), 1_000_000);
        collector.Record(Text("b"), 1_000_000);
        collector.Reset();
        var snapshot = collector.Snapshot();
        Equal(snapshot.Enabled, true);
        Equal(snapshot.Totals.RequestCount, 0UL);
        Equal(snapshot.Totals.TotalProcessingTimeMs, 0d);
        Equal(snapshot.RecentRequests.Length, 0);
        collector.Record(Text("c"), 2_000_000);
        snapshot = collector.Snapshot();
        Equal(snapshot.Totals.RequestCount, 1UL);
        EqualText(snapshot.RecentRequests[0].Method, "c");
    }

    private static void TestDurationToMillis()
    {
        Equal(Ipc.DurationToMillis(1_500_000), 1.5);
        Equal(Ipc.DurationToMillis(0), 0d);
        Equal(Ipc.DurationToMillis(-5_000_000_000), 0d);
        Equal(Ipc.DurationToMillis(500), 0.0005);
        Equal(Ipc.DurationToMillis(1234), 0.001234);
    }
}

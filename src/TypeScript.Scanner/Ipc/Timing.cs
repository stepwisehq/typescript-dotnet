// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Complete internal/ipc/timing.go: nanosecond duration input retains sub-tick precision.
using TypeScript.Scanner;
using TypeScript.Lsp.Protocol;

namespace TypeScript.Ipc;
internal readonly record struct ServerRequestTiming(ByteStringValue Method, double ProcessingTimeMs, long Timestamp) : IProtocolWritable
{
    public void WriteProtocolJSON(ProtocolEncoder encoder)
    {
        encoder.BeginObject();
        encoder.WriteProperty("method"u8);
        encoder.WriteString(Method);
        encoder.WriteProperty("processingTimeMs"u8);
        encoder.WriteAny(ProcessingTimeMs);
        encoder.WriteProperty("timestamp"u8);
        encoder.WriteAny(Timestamp);
        encoder.EndObject();
    }
}

internal struct ServerTimingTotals : IProtocolWritable
{
    internal ulong RequestCount;
    internal double TotalProcessingTimeMs;
    public readonly void WriteProtocolJSON(ProtocolEncoder encoder)
    {
        encoder.BeginObject();
        encoder.WriteProperty("requestCount"u8);
        encoder.WriteAny(RequestCount);
        encoder.WriteProperty("totalProcessingTimeMs"u8);
        encoder.WriteAny(TotalProcessingTimeMs);
        encoder.EndObject();
    }
}

internal readonly record struct ServerTimingInfo(bool Enabled, ServerTimingTotals Totals, Slice<ServerRequestTiming> RecentRequests) : IProtocolWritable
{
    public void WriteProtocolJSON(ProtocolEncoder encoder)
    {
        encoder.BeginObject();
        encoder.WriteProperty("enabled"u8);
        encoder.WriteAny(Enabled);
        encoder.WriteProperty("totals"u8);
        Totals.WriteProtocolJSON(encoder);
        encoder.WriteProperty("recentRequests"u8);
        encoder.BeginArray();
        foreach (var request in RecentRequests)
            request.WriteProtocolJSON(encoder);
        encoder.EndArray();
        encoder.EndObject();
    }
}

internal sealed class TimingCollector
{
    internal const int ServerRecentRequestCapacity = 5;
    private readonly object mutex = new();
    private ServerTimingTotals totals;
    private Slice<ServerRequestTiming> ring;
    private int head;
    internal static TimingCollector NewTimingCollector() => new();
    internal void Record(ByteStringValue method, long durationNanoseconds)
    {
        double processingMs = Ipc.DurationToMillis(durationNanoseconds);
        lock (mutex)
        {
            totals.RequestCount++;
            totals.TotalProcessingTimeMs += processingMs;
            var entry = new ServerRequestTiming(method, processingMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (ring.Length < ServerRecentRequestCapacity)
                ring = ring.Append(entry);
            else
            {
                ring[head] = entry;
                head = (head + 1) % ServerRecentRequestCapacity;
            }
        }
    }

    internal ServerTimingInfo Snapshot()
    {
        lock (mutex)
        {
            var recent = Slice<ServerRequestTiming>.Make(0, ring.Length);
            for (int i = 0; i < ring.Length; i++)
                recent = recent.Append(ring[(head + i) % ring.Length]);
            return new(true, totals, recent);
        }
    }

    internal void Reset()
    {
        lock (mutex)
        {
            totals = default;
            ring = default;
            head = 0;
        }
    }
}

internal static partial class Ipc
{
    internal static readonly ByteStringValue MethodGetServerTiming = ByteStringValue.FromString("getServerTiming");
    internal static readonly ByteStringValue MethodResetServerTiming = ByteStringValue.FromString("resetServerTiming");
    internal static ServerTimingInfo ServerTimingSnapshot(TimingCollector? collector) => collector?.Snapshot() ?? DisabledServerTimingInfo();
    internal static ServerTimingInfo DisabledServerTimingInfo() => new(false, default, Slice<ServerRequestTiming>.Make(0));
    internal static double DurationToMillis(long nanoseconds) => nanoseconds < 0 ? 0 : nanoseconds / 1_000_000d;
}

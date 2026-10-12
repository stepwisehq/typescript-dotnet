// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Original watcher_test.go recorder/assertion helper closure, not a new test framework.
using System.Diagnostics;
using TypeScript.Scanner;
using TypeScript.FsWatch;
using FsWatch = TypeScript.FsWatch.FsWatch;

namespace LanguageServices.Checks;
public static partial class FsWatchBackendChecks
{
    private static ByteStringValue Text(string display) => ByteStringValue.FromString(GoString.ToUtf8(display));
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private readonly record struct WantEvent(EventKind Kind, ByteStringValue Path)
    {
        // Go wantEvent equality compares the path string bytes, not its backing owner.
        public bool Equals(WantEvent other) => Kind == other.Kind && Path.EqualsValue(other.Path);
        public override int GetHashCode() => HashCode.Combine(Kind, ByteStringKey.FromValue(Path));
    }
    private static bool Contains(Slice<WatchEvent> events, EventKind kind, ByteStringValue path)
    {
        foreach (var value in events)
            if (value.Kind == kind && value.Path.EqualsValue(path))
                return true;
        return false;
    }

    private static bool HaveAll(Slice<WatchEvent> events, WantEvent[] expected)
    {
        foreach (var value in expected)
            if (!Contains(events, value.Kind, value.Path))
                return false;
        return true;
    }

    private static Slice<WatchEvent> FilterToWanted(Slice<WatchEvent> events, WantEvent[] expected)
    {
        var paths = new HashSet<ByteStringKey>(expected.Length);
        foreach (var value in expected)
            paths.Add(ByteStringKey.FromValue(value.Path));
        var filtered = Slice<WatchEvent>.Make(0, events.Length);
        foreach (var value in events)
            if (paths.Contains(ByteStringKey.FromValue(value.Path)))
                filtered = filtered.Append(value);
        return filtered;
    }

    private static Slice<WantEvent> ToWantEvents(Slice<WatchEvent> events)
    {
        // Original required make(len(events)) owner after filtering, not a projection view.
        var result = Slice<WantEvent>.Make(events.Length);
        for (int index = 0; index < events.Length; index++)
            result[index] = new(events[index].Kind, events[index].Path);
        return result;
    }

    private static int CompareEvents(WantEvent left, WantEvent right)
    {
        int kind = left.Kind.CompareTo(right.Kind);
        return kind != 0 ? kind : ByteStringKey.FromValue(left.Path).CompareTo(ByteStringKey.FromValue(right.Path));
    }

    private static void AssertEventSet(Slice<WatchEvent> events, WantEvent[] expected)
    {
        var actual = ToWantEvents(FilterToWanted(events, expected));
        GoSliceSort.SortFunc(actual.AsSpan(), CompareEvents);
        GoSliceSort.SortFunc(expected.AsSpan(), CompareEvents);
        AssertWantEvents(actual.AsSpan(), expected, "event set");
    }

    private static void AssertEventSequence(Slice<WatchEvent> events, WantEvent[] expected) => AssertWantEvents(ToWantEvents(FilterToWanted(events, expected)).AsSpan(), expected, "event sequence");
    private static void AssertWantEvents(ReadOnlySpan<WantEvent> actual, ReadOnlySpan<WantEvent> expected, string label)
    {
        if (!actual.SequenceEqual(expected))
            throw new InvalidOperationException(label + " differs\nactual: " + RenderWantEvents(actual) + "\nexpected: " + RenderWantEvents(expected));
    }

    private static string RenderWantEvents(ReadOnlySpan<WantEvent> events)
    {
        var result = new System.Text.StringBuilder("[");
        foreach (var value in events)
        {
            if (result.Length > 1)
                result.Append(", ");
            // Diagnostic display only; equality above retains raw Go byte content.
            result.Append(value.Kind).Append(' ').Append(GoString.ToString(value.Path.Materialize()!));
        }
        return result.Append(']').ToString();
    }
    private sealed class Recorder
    {
        internal readonly object Gate = new();
        internal Slice<WatchEvent> Buffer;
        internal Slice<Exception> Errors;
        internal IWatcher? Watcher;
        internal int Attempt = 1;
        private int Scale => Attempt == 1 ? 1 : Attempt == 2 ? 5 : 15;

        internal TimeSpan Deadline() => TimeSpan.FromSeconds(Watcher == FsWatch.FSEvents() || Watcher == FsWatch.Kqueue() ? 2 : 1) * Scale;
        internal void Callback(Slice<WatchEvent> events, Exception? error)
        {
            lock (Gate)
            {
                if (error is not null)
                    Errors = Errors.Append(error);
                Buffer = Buffer.Append(events);
                Monitor.PulseAll(Gate);
            }
        }

        private Slice<WatchEvent> DrainLocked()
        {
            var value = GoSlices.Clone(Buffer);
            Buffer = default;
            return value;
        }

        internal Slice<WatchEvent> Next(TimeSpan delay)
        {
            long start = Stopwatch.GetTimestamp();
            lock (Gate)
            {
                while (Buffer.Length == 0)
                {
                    var remaining = delay - Stopwatch.GetElapsedTime(start);
                    if (remaining <= TimeSpan.Zero)
                        return default;
                    // Timed Monitor wait is the standard synchronous counterpart
                    // of original cond.Wait plus its timeout broadcast timer.
                    Monitor.Wait(Gate, remaining);
                }

                return DrainLocked();
            }
        }

        internal Slice<WatchEvent> DrainQuiet(TimeSpan delay)
        {
            lock (Gate)
                Buffer = default;
            Thread.Sleep(delay);
            lock (Gate)
                return DrainLocked();
        }

        internal Slice<WatchEvent> Gather(TimeSpan wait, TimeSpan settle)
        {
            var first = Next(wait);
            if (first.Length == 0)
                return default;
            Thread.Sleep(settle);
            lock (Gate)
                return first.Append(DrainLocked());
        }

        internal Slice<WatchEvent> GatherUntilQuiet(TimeSpan initialWait, TimeSpan quiet)
        {
            var all = Next(initialWait);
            if (all.Length == 0)
                return default;
            while (true)
            {
                var more = Next(quiet);
                if (more.Length == 0)
                    return all;
                all = all.Append(more);
            }
        }

        internal Slice<WatchEvent> WaitForEvent(TimeSpan delay, Func<WatchEvent, bool> predicate)
        {
            long start = Stopwatch.GetTimestamp();
            lock (Gate)
                while (true)
                {
                    foreach (var value in Buffer)
                        if (predicate(value))
                            return DrainLocked();
                    var remaining = delay - Stopwatch.GetElapsedTime(start);
                    if (remaining <= TimeSpan.Zero)
                        return DrainLocked();
                    Monitor.Wait(Gate, remaining);
                }
        }

        internal Slice<WatchEvent> WaitForAll(TimeSpan delay, WantEvent[] expected)
        {
            if (expected.Length == 0)
                return default;
            long start = Stopwatch.GetTimestamp();
            var collected = Slice<WatchEvent>.Make(0);
            while (true)
            {
                lock (Gate)
                {
                    collected = collected.Append(Buffer);
                    Buffer = default;
                }

                if (HaveAll(collected, expected))
                    return collected;
                var remaining = delay - Stopwatch.GetElapsedTime(start);
                if (remaining <= TimeSpan.Zero)
                    return collected;
                lock (Gate)
                {
                    if (Buffer.Length != 0)
                        continue;
                    Monitor.Wait(Gate, remaining);
                }
            }
        }
    }

    private static Slice<WatchEvent> ExpectEventSet(Recorder recorder, params WantEvent[] expected)
    {
        var actual = recorder.WaitForAll(recorder.Deadline(), expected);
        AssertEventSet(actual, expected);
        return actual;
    }

    private static Slice<WatchEvent> ExpectEventSequence(Recorder recorder, params WantEvent[] expected)
    {
        var actual = recorder.WaitForAll(recorder.Deadline(), expected);
        AssertEventSequence(actual, expected);
        return actual;
    }

    private static Slice<WatchEvent> ExpectContains(Recorder recorder, EventKind kind, ByteStringValue path)
    {
        var actual = recorder.WaitForEvent(recorder.Deadline(), value => value.Kind == kind && value.Path.EqualsValue(path));
        Check(Contains(actual, kind, path), "expected event was not observed");
        return actual;
    }

    private static void ExpectNoBufferedEvents(Recorder recorder, string message)
    {
        lock (recorder.Gate)
        {
            var actual = GoSlices.Clone(recorder.Buffer);
            recorder.Buffer = default;
            Check(actual.Length == 0, message);
        }
    }

    private static Slice<WatchEvent> FilterEventsForPaths(Slice<WatchEvent> events, params ByteStringValue[] paths)
    {
        var allowed = new HashSet<ByteStringKey>();
        foreach (var path in paths)
            allowed.Add(ByteStringKey.FromValue(path));
        var result = Slice<WatchEvent>.Make(0, events.Length);
        foreach (var value in events)
            if (allowed.Contains(ByteStringKey.FromValue(value.Path)))
                result = result.Append(value);
        return result;
    }

    private static Slice<WatchEvent> ReplayEventList(Slice<WatchEvent> events)
    {
        var list = new EventList();
        foreach (var value in events)
            if (value.Kind == EventKind.Delete)
                list.Remove(value.Path);
            else
                list.Update(value.Path);
        return list.GetEvents();
    }
}

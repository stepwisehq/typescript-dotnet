// Copyright (c) Microsoft Corporation; Copyright (c) 2017-present Devon Govett.
// Original internal/fswatch/eventlist_test.go: seven unchanged assertion functions.
using TypeScript.Scanner;
using TypeScript.FsWatch;

namespace LanguageServices.Checks;
public static class FsWatchEventChecks
{
    public static readonly string[] Names = [nameof(TestEventListCreateThenDelete), nameof(TestEventListDeleteThenCreate), nameof(TestEventListCreateDeleteCreate), nameof(TestEventListErrorIsLatchedAndCleared), nameof(TestEventListDrainIsAtomic), nameof(TestEventListDrainReturnsErrorWithEvents), nameof(TestEventListDrainForSequences)];
    private static ByteStringValue Text(string value) => ByteStringValue.FromString(value);
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    public static int RunAll()
    {
        foreach (var name in Names)
            Run(name);
        return Names.Length;
    }

    public static void Run(string name)
    {
        switch (name)
        {
            case nameof(TestEventListCreateThenDelete):
                TestEventListCreateThenDelete();
                break;
            case nameof(TestEventListDeleteThenCreate):
                TestEventListDeleteThenCreate();
                break;
            case nameof(TestEventListCreateDeleteCreate):
                TestEventListCreateDeleteCreate();
                break;
            case nameof(TestEventListErrorIsLatchedAndCleared):
                TestEventListErrorIsLatchedAndCleared();
                break;
            case nameof(TestEventListDrainIsAtomic):
                TestEventListDrainIsAtomic();
                break;
            case nameof(TestEventListDrainReturnsErrorWithEvents):
                TestEventListDrainReturnsErrorWithEvents();
                break;
            case nameof(TestEventListDrainForSequences):
                TestEventListDrainForSequences();
                break;
            default:
                throw new ArgumentException(name);
        }
    }

    private static void TestEventListCreateThenDelete()
    {
        var events = new EventList();
        events.Create(Text("a"));
        events.Remove(Text("a"));
        Check(events.Size() == 1, "size after create+remove want1");
        Check(events.GetEvents().Count == 0, "getEvents should drop create+delete");
    }

    private static void TestEventListDeleteThenCreate()
    {
        var events = new EventList();
        events.Remove(Text("a"));
        events.Create(Text("a"));
        var values = events.GetEvents();
        Check(values.Count == 1, "expected1 event");
        Check(values[0].Kind == EventKind.Update, "expected update");
    }

    private static void TestEventListCreateDeleteCreate()
    {
        var events = new EventList();
        events.Create(Text("a"));
        events.Remove(Text("a"));
        events.Create(Text("a"));
        var values = events.GetEvents();
        Check(values.Count == 1, "expected1 event");
        Check(values[0].Kind == EventKind.Update, "create+delete+create should coalesce to update");
    }

    private static void TestEventListErrorIsLatchedAndCleared()
    {
        var events = new EventList();
        Check(!events.HasError(), "fresh eventList should have no error");
        Check(events.GetError()is null, "fresh getError want nil");
        events.SetError(new IOException("first"));
        events.SetError(new IOException("second"));
        Check(events.HasError(), "hasError should be true after setError");
        Check(events.GetError()?.Message == "first", "getError want first");
        // Original test-only clear resets entries/error under the same mutex.
        // Drain is the real owner boundary doing exactly those resets; its
        // unused empty snapshot avoids adding a production test-only method.
        _ = events.Drain();
        Check(!events.HasError(), "clear should drop the error");
        Check(events.GetError()is null, "post-clear getError want nil");
    }

    private static void TestEventListDrainIsAtomic()
    {
        var events = new EventList();
        events.Create(Text("a"));
        events.Update(Text("b"));
        events.SetError(new IOException("oops"));
        var(values, error) = events.Drain();
        Check(error is not null, "drain should return the error");
        Check(values.Count == 2, "drain should return2 events");
        var(secondValues, secondError) = events.Drain();
        Check(secondError is null, "second drain should have no error");
        Check(secondValues.Count == 0, "second drain should be empty");
    }

    private static void TestEventListDrainReturnsErrorWithEvents()
    {
        var events = new EventList();
        events.Create(Text("file.txt"));
        events.SetError(new IOException("overflow"));
        var(values, error) = events.Drain();
        Check(error is not null, "expected error from drain");
        Check(values.Count == 1, "expected1 event alongside error");
    }

    private static void TestEventListDrainForSequences()
    {
        var events = new EventList();
        events.Create(Text("file.txt"));
        ulong startAfterCreate = events.Sequence();
        events.Remove(Text("file.txt"));
        var(values, error) = events.DrainForSequences(Slice<ulong>.FromArray([0, startAfterCreate]));
        Check(error is null, "drainForSequences error");
        Check(values[0].Count == 0, "create+delete should cancel for original callback");
        Check(values[1].Count == 1, "expected delete for later callback");
        Check(values[1][0].Kind == EventKind.Delete && values[1][0].Path.EqualsValue(Text("file.txt")), "expected delete for file.txt");
    }
}

// Authored compatibility checks: compare concrete List iteration with the
// unchanged IEnumerable adapter. Go's fixed slice header is a separate policy.
using System.Collections;
using TypeScript.Scanner;

namespace TypeScript.Foundation;

public static class ListValueRangeChecks
{
    public static IReadOnlyList<string> Cases { get; } =
        ["null-and-empty", "versioned-mutation", "captured-current", "reset-and-interfaces", "independent-enumeration", "integer-empty", "integer-order"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "null-and-empty": NullAndEmpty(); break;
            case "versioned-mutation": VersionedMutation(); break;
            case "captured-current": CapturedCurrent(); break;
            case "reset-and-interfaces": ResetAndInterfaces(); break;
            case "independent-enumeration": IndependentEnumeration(); break;
            case "integer-empty": IntegerEmpty(); break;
            case "integer-order": IntegerOrder(); break;
            default: throw new ArgumentException("Unknown value-range case: " + name, nameof(name));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static (bool Value, Type? Error) TryMove<T>(ref T iterator) where T : IEnumerator<int>
    {
        try { return (iterator.MoveNext(), null); }
        catch (Exception error) { return (false, error.GetType()); }
    }

    private static (int Value, Type? Error) TryRead<T>(ref T iterator, bool nongeneric) where T : IEnumerator<int>
    {
        try { return (nongeneric ? (int)((IEnumerator)iterator).Current : iterator.Current, null); }
        catch (Exception error) { return (0, error.GetType()); }
    }

    private static Type? TryReset<T>(ref T iterator) where T : IEnumerator<int>
    {
        try { iterator.Reset(); return null; }
        catch (Exception error) { return error.GetType(); }
    }

    private static (bool Value, Type? Error) Move<T>(ref T actual, ref IEnumerator<int> old) where T : IEnumerator<int>
    {
        var result = TryMove(ref actual);
        Require(result == TryMove(ref old), "MoveNext outcome differs from original interface route");
        return result;
    }

    private static void Read<T>(ref T actual, ref IEnumerator<int> old) where T : IEnumerator<int>
    {
        Require(TryRead(ref actual, false) == TryRead(ref old, false), "Generic Current differs");
        Require(TryRead(ref actual, true) == TryRead(ref old, true), "Nongeneric Current differs");
    }

    private static IEnumerator<int> Original(List<int>? source) =>
        GoRuntime.RangeValues((IEnumerable<int>)source!).GetEnumerator();

    private static void NullAndEmpty()
    {
        foreach (List<int>? source in new List<int>?[] { null, [] })
        {
            var actual = GoRuntime.RangeValues(source!).GetEnumerator();
            var old = Original(source);
            Read(ref actual, ref old);
            Require(Move(ref actual, ref old) == (false, null), "Empty iteration has no element");
            Read(ref actual, ref old);
            Require(TryReset(ref actual) == TryReset(ref old), "Empty reset differs");
            actual.Dispose(); old.Dispose();
        }
        List<int> empty = [];
        var direct = GoRuntime.RangeValues(empty).GetEnumerator();
        var previous = Original(empty);
        empty.Add(7); // Original generic List interface chooses an empty iterator at creation.
        Require(Move(ref direct, ref previous) == (false, null), "Empty-at-creation policy changed");
    }

    private static void VersionedMutation()
    {
        foreach (int mutation in new[] { 0, 1, 2 })
        {
            List<int> source = [10, 20, 30];
            var actual = GoRuntime.RangeValues(source).GetEnumerator();
            var old = Original(source);
            Require(Move(ref actual, ref old) == (true, null), "First value exists");
            switch (mutation)
            {
                case 0: source.Add(40); break;
                case 1: source[0] = 99; break;
                case 2: source.RemoveAt(1); break;
            }
            Read(ref actual, ref old); // Current is the captured value, even after overwrite.
            Require(Move(ref actual, ref old).Error == typeof(InvalidOperationException), "List version check lost");
            Require(TryReset(ref actual) == TryReset(ref old), "Mutation reset outcome differs");
        }
    }

    private static void CapturedCurrent()
    {
        List<int> source = [4, 5];
        var actual = GoRuntime.RangeValues(source).GetEnumerator();
        var old = Original(source);
        Read(ref actual, ref old);
        Require(Move(ref actual, ref old) == (true, null), "First item exists");
        Read(ref actual, ref old);
        actual.Dispose(); old.Dispose(); // List Dispose is the same no-op on both routes.
        Require(Move(ref actual, ref old) == (true, null), "List disposal policy changed");
        Read(ref actual, ref old);
        Require(Move(ref actual, ref old) == (false, null), "Both iterators finish");
        Read(ref actual, ref old);
        Require(Move(ref actual, ref old) == (false, null), "Both remain completed");
    }

    private static void ResetAndInterfaces()
    {
        List<int> source = [8, 9];
        var values = GoRuntime.RangeValues(source);
        var actual = values.GetEnumerator();
        var old = Original(source);
        Move(ref actual, ref old);
        Require(TryReset(ref actual) == TryReset(ref old), "Reset must retain original List state semantics");
        Read(ref actual, ref old);
        Require(Move(ref actual, ref old) == (true, null) && actual.Current == 8, "Reset starts from first value");
        Require(((IEnumerable<int>)values).SequenceEqual(GoRuntime.RangeValues((IEnumerable<int>)source)), "Generic interface route changed");
        IEnumerator nongeneric = ((IEnumerable)values).GetEnumerator();
        IEnumerator previous = ((IEnumerable)source).GetEnumerator();
        Require(nongeneric.MoveNext() == previous.MoveNext() && Equals(nongeneric.Current, previous.Current), "Nongeneric route changed");
        nongeneric.Reset(); previous.Reset();
        Require(nongeneric.MoveNext() == previous.MoveNext() && Equals(nongeneric.Current, previous.Current), "Nongeneric reset differs");
    }

    private static void IndependentEnumeration()
    {
        List<int> source = [1, 2];
        var values = GoRuntime.RangeValues(source);
        source.Add(3); // The wrapper retains the list and acquires its enumerator later.
        var first = values.GetEnumerator(); var second = values.GetEnumerator();
        Require(first.MoveNext() && first.MoveNext() && second.MoveNext(), "Independent iterator positions");
        Require(first.Current == 2 && second.Current == 1, "Iterators must not share position");
        source = [99];
        Require(values.SequenceEqual(new[] { 1, 2, 3 }), "Wrapper retains original reference");
        List<int> observed = [];
        foreach (int value in values) observed.Add(value); // Concrete foreach exercises value path.
        Require(observed.SequenceEqual(new[] { 1, 2, 3 }), "Concrete foreach order differs");
        Require(values.SequenceEqual(observed), "Repeated enumeration must restart");
    }

    private static IEnumerable<int> OldIntegerRange(int count)
    {
        for (int i = 0; i < count; i++) yield return i;
    }

    private static void IntegerEmpty()
    {
        foreach (int count in new[] { int.MinValue, -1, 0 })
        {
            var range = GoRuntime.RangeKeys(count);
            Require(!range.Any() && range.SequenceEqual(OldIntegerRange(count)), "Nonpositive bound must be empty");
            var iterator = range.GetEnumerator();
            Require(!iterator.MoveNext(), "Concrete nonpositive iteration must be empty");
        }
    }

    private static void IntegerOrder()
    {
        foreach (int count in new[] { 1, 17 })
        {
            var range = GoRuntime.RangeKeys(count);
            List<int> observed = [];
            foreach (int value in range) observed.Add(value);
            Require(observed.SequenceEqual(OldIntegerRange(count)), "Concrete signed bound/order changed");
            Require(range.SequenceEqual(observed) && range.SequenceEqual(observed), "Integer repeated generic enumeration differs");
            IEnumerator nongeneric = ((IEnumerable)range).GetEnumerator();
            int index = 0;
            while (nongeneric.MoveNext()) Require((int)nongeneric.Current == index++, "Nongeneric integer order differs");
            Require(index == count, "Nongeneric integer bound differs");
            ((IDisposable)nongeneric).Dispose();
        }
    }
}

// Authored compatibility checks, not upstream Go test bodies. The managed List
// adapter intentionally retains live Count behavior; Go ranges capture a slice
// header. This packet changes allocation, not that existing adaptation policy.
using System.Collections;
using TypeScript.Scanner;

namespace TypeScript.Foundation;

public static class ListRangeChecks
{
    public static IReadOnlyList<string> Cases { get; } =
        ["null-and-order", "mutation-and-current", "reference-and-interfaces"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "null-and-order": NullAndOrder(); break;
            case "mutation-and-current": MutationAndCurrent(); break;
            case "reference-and-interfaces": ReferenceAndInterfaces(); break;
            default: throw new ArgumentException("Unknown list-range check: " + name, nameof(name));
        }
    }
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private static void Pair(KeyValuePair<int, int> actual, int index, int value) =>
        Require(actual.Key == index && actual.Value == value, "Range index/value mismatch");

    private static void NullAndOrder()
    {
        int count = 0;
        foreach (var _ in GoRuntime.Range((List<int>)null!)) count++;
        foreach (var _ in GoRuntime.Range(new List<int>())) count++;
        Require(count == 0, "Null and empty ranges are empty");
        var empty = default(GoListRange<int>).GetEnumerator();
        Require(!empty.MoveNext(), "Default range is empty");
        int[] expected = [13, 21, 34];
        int index = 0;
        foreach (var pair in GoRuntime.Range(new List<int>(expected)))
        {
            Pair(pair, index, expected[index]);
            index++;
        }
        Require(index == 3, "Each element visited once in index order");
    }

    private static void MutationAndCurrent()
    {
        var source = new List<int> { 10, 20 };
        var range = GoRuntime.Range(source);
        source.Add(30); // Changes before enumeration remain visible.
        var iterator = range.GetEnumerator();
        Require(iterator.MoveNext(), "First item exists");
        source[0] = 99;
        Pair(iterator.Current, 0, 10); // Yield captures before a later overwrite.
        source[1] = 22;
        Require(iterator.MoveNext(), "Second item exists");
        Pair(iterator.Current, 1, 22);
        source.Add(40); // Existing adapter checks live Count, with no version check.
        Require(iterator.MoveNext(), "Third item exists"); Pair(iterator.Current, 2, 30);
        Require(iterator.MoveNext(), "Appended fourth item is visited"); Pair(iterator.Current, 3, 40);
        Require(!iterator.MoveNext(), "Range reaches the end");
        source.Add(50);
        Require(!iterator.MoveNext(), "Completed iterator does not resume after append");
        Pair(iterator.Current, 3, 40);

        var shortened = range.GetEnumerator();
        Require(shortened.MoveNext(), "Fresh range starts from zero");
        source.RemoveRange(1, source.Count - 1);
        Require(!shortened.MoveNext(), "Shrinking length ends the existing adapter safely");
    }

    private static void ReferenceAndInterfaces()
    {
        List<int> source = [4, 5];
        var range = GoRuntime.Range(source);
        source = [9];
        var first = range.GetEnumerator(); var second = range.GetEnumerator();
        Require(first.MoveNext() && first.MoveNext() && second.MoveNext(), "Independent range positions");
        Pair(first.Current, 1, 5); Pair(second.Current, 0, 4);
        IEnumerable<KeyValuePair<int, int>> generic = range;
        Require(generic.Select(x => x.Value).SequenceEqual(new[] { 4, 5 }), "Interface route retains original list reference");
        Require(generic.Select(x => x.Key).SequenceEqual(new[] { 0, 1 }), "Repeated interface enumeration starts from zero");
        IEnumerator nongeneric = ((IEnumerable)range).GetEnumerator();
        Require(nongeneric.MoveNext(), "Nongeneric interface is supported");
        Pair((KeyValuePair<int, int>)nongeneric.Current, 0, 4);
        bool rejectedReset = false;
        try { nongeneric.Reset(); } catch (NotSupportedException) { rejectedReset = true; }
        Require(rejectedReset, "Reset remains unsupported like the original yield iterator");
        ((IDisposable)nongeneric).Dispose();
        Require(!nongeneric.MoveNext(), "Dispose ends an active interface iterator");
        first.Dispose();
        Require(!first.MoveNext(), "Dispose ends an active value iterator");
    }
}

// Authored shared Collections support controls. The ordered-set prefix is an
// explicitly adapted subset of original TestOrderedSet; other cases are
// additional source-derived helper observations, not original test-suite ports.
// Private backing topology and slice write-through after mutation are not claimed.
using TypeScript.Scanner;

namespace TypeScript.Checking.CollectionsValidation;
public static class CollectionSupportChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["ordered-set-original-prefix-adapted", "ordered-set-identity-and-live-order", "map-index-captured-length", "same-map-identity-and-mutation", "count-where-captured-length", "concatenate-and-same", "replace-and-delete", "equal-and-contains-nil"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "ordered-set-original-prefix-adapted":
                OrderedSetPrefix();
                break;
            case "ordered-set-identity-and-live-order":
                OrderedSetIdentityAndOrder();
                break;
            case "map-index-captured-length":
                MapIndexCapturedLength();
                break;
            case "same-map-identity-and-mutation":
                SameMapIdentityAndMutation();
                break;
            case "count-where-captured-length":
                CountWhereCapturedLength();
                break;
            case "concatenate-and-same":
                ConcatenateAndSame();
                break;
            case "replace-and-delete":
                ReplaceAndDelete();
                break;
            case "equal-and-contains-nil":
                EqualAndContainsNil();
                break;
            default:
                throw new ArgumentException("Unknown Collections support case: " + name, nameof(name));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Sequence<T>(IEnumerable<T> actual, params T[] expected) => Require(actual.SequenceEqual(expected), "Unexpected support sequence: " + string.Join(",", actual));
    private static void OrderedSetPrefix()
    {
        // ordered_set_test.go TestOrderedSet: preserve the original Add(1,2,3)
        // and three Has assertions with distinct Node identities. The original
        // Delete/Clear/Clone assertions are explicitly omitted, not simulated.
        var factory = new NodeFactory();
        var one = factory.NewIdentifier("one");
        var two = factory.NewIdentifier("two");
        var three = factory.NewIdentifier("three");
        var set = new GoOrderedNodeSet();
        set.Add(one);
        set.Add(two);
        set.Add(three);
        Require(set.Has(one), "Original prefix Has(1)");
        Require(set.Has(two), "Original prefix Has(2)");
        Require(set.Has(three), "Original prefix Has(3)");
    }

    private static void OrderedSetIdentityAndOrder()
    {
        var factory = new NodeFactory();
        var one = factory.NewIdentifier("same");
        var two = factory.NewIdentifier("same");
        var three = factory.NewIdentifier("three");
        var four = factory.NewIdentifier("four");
        var absent = factory.NewIdentifier("same");
        var set = new GoOrderedNodeSet();
        Require(set.Size() == 0 && !set.Has(one), "Zero-value ordered node set");
        set.Add(one);
        set.Add(two);
        set.Add(three);
        set.Add(two);
        Require(set.Size() == 3 && !set.Has(absent), "Node identity and duplicate suppression");
        Sequence(set.Values(), one, two, three);
        var visited = new List<Node>();
        foreach (var node in set.Values())
        {
            visited.Add(node);
            if (ReferenceEquals(node, one))
                set.Add(four);
        }

        Sequence(visited, one, two, three, four);
        Require(set.Size() == 4 && set.Has(four), "Live insertion updates size and membership");
        var stopped = new List<Node>();
        foreach (var node in set.Values())
        {
            stopped.Add(node);
            break;
        }

        Sequence(stopped, one);
    }

    private static void MapIndexCapturedLength()
    {
        Require(Core.MapIndex<int, int>(null, (_, _) => throw new InvalidOperationException("Nil mapper invoked")) is null, "MapIndex retains nil");
        var empty = Core.MapIndex(new List<int>(), (value, index) => value + index);
        Require(empty is { Count: 0 }, "MapIndex retains allocated empty");
        var caller = new List<int>
        {
            1,
            2,
            3
        };
        var visited = new List<int>();
        var result = Core.MapIndex(caller, (value, index) =>
        {
            visited.Add(index);
            if (index == 0)
            {
                caller.Add(99);
                caller[2] = 4;
            }

            return value + index;
        });
        Sequence(visited, 0, 1, 2);
        Sequence(result!, 1, 3, 6);
        Sequence(caller, 1, 2, 4, 99);
    }

    private static void SameMapIdentityAndMutation()
    {
        Require(Core.SameMap<int>(null, _ => throw new InvalidOperationException("Nil mapper invoked")) is null, "SameMap retains nil");
        var empty = new List<int>();
        Require(ReferenceEquals(empty, Core.SameMap(empty, value => value)), "Unchanged empty identity");
        var original = new List<int>
        {
            1,
            2,
            3
        };
        Require(ReferenceEquals(original, Core.SameMap(original, value => value)), "Unchanged whole-list identity");
        var changed = Core.SameMap(original, value => value == 2 ? 20 : value);
        Require(!ReferenceEquals(original, changed), "Changed map owns a new header");
        Sequence(changed!, 1, 20, 3);
        Sequence(original, 1, 2, 3);
        var caller = new List<int>
        {
            1,
            2,
            3
        };
        var visited = new List<int>();
        var appended = Core.SameMapIndex(caller, (value, index) =>
        {
            visited.Add(index);
            if (index == 0)
                caller.Add(99);
            return value;
        });
        Sequence(visited, 0, 1, 2);
        Sequence(appended!, 1, 2, 3);
        Sequence(caller, 1, 2, 3, 99);
        // The managed shortened header is a prefix copy. No assertion of later
        // Go backing-array write-through or identity is made for this mutation.
    }

    private static void CountWhereCapturedLength()
    {
        Require(Core.CountWhere<int>(null, _ => throw new InvalidOperationException("Nil predicate invoked")) == 0, "CountWhere nil");
        var caller = new List<int>
        {
            1,
            2,
            3
        };
        var visited = new List<int>();
        int count = Core.CountWhere(caller, value =>
        {
            visited.Add(value);
            if (value == 1)
                caller.Add(99);
            return value % 2 == 1;
        });
        Require(count == 2, "CountWhere excludes appended suffix");
        Sequence(visited, 1, 2, 3);
        Sequence(caller, 1, 2, 3, 99);
    }

    private static void ConcatenateAndSame()
    {
        var empty = new List<int>();
        var left = new List<int>
        {
            1,
            2
        };
        var right = new List<int>
        {
            3
        };
        Require(Core.Concatenate<int>(null, empty) is null, "Empty right preserves left nil");
        Require(ReferenceEquals(left, Core.Concatenate(left, empty)), "Empty right preserves whole left");
        Require(ReferenceEquals(right, Core.Concatenate(empty, right)), "Empty left preserves whole right");
        Sequence(Core.Concatenate(left, right)!, 1, 2, 3);
        Sequence(left, 1, 2);
        Require(Core.Same<int>(null, empty), "Go empty slices compare alike");
        Require(Core.Same(left, left), "Whole-list Same identity");
        Require(!Core.Same(left, new List<int> { 1, 2 }), "Independent nonempty lists have different storage");
    }

    private static void ReplaceAndDelete()
    {
        var original = new List<int>
        {
            1,
            2,
            3
        };
        var replaced = Core.ReplaceElement(original, 1, 20);
        Require(!ReferenceEquals(original, replaced), "ReplaceElement copies its slice header and values");
        Sequence(replaced, 1, 20, 3);
        Sequence(original, 1, 2, 3);
        var caller = new List<int>
        {
            1,
            2,
            3,
            4
        };
        Require(ReferenceEquals(caller, GoSlices.Delete(caller, 1, 1)), "Empty deletion keeps the header");
        var result = GoSlices.Delete(caller, 1, 3);
        Sequence(result, 1, 4);
        Sequence(caller, 1, 4, 0, 0);
        // The old header's length and cleared tail are compared; later shared
        // writes through the shortened Go slice are outside this representation.
    }

    private static void EqualAndContainsNil()
    {
        Require(GoSlices.Equal<int>(null!, new List<int>()), "Nil and empty element equality");
        Require(!GoSlices.Contains<int>(null!, 1), "Nil contains no element");
        Require(GoSlices.Equal(new List<int> { 1, 2 }, new List<int> { 1, 2 }), "Ordered element equality");
        Require(!GoSlices.Equal(new List<int> { 1, 2 }, new List<int> { 2, 1 }), "Element order remains significant");
        Require(GoSlices.Contains(new List<int> { 1, 2 }, 2), "Contains uses actual element equality");
    }
}

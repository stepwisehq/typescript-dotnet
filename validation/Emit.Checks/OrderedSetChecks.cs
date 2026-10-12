// Shared original public assertions from collections/ordered_set_test.go.
using TypeScript.Collections;

namespace Emit.Checks;
public static class OrderedSetChecks
{
    // ordered_set_test.go:11. Original TestOrderedSet, unchanged public assertions.
    public static int RunAll()
    {
        var s = new OrderedSet<int>();
        s.Add(1);
        s.Add(2);
        s.Add(3);
        Check(s.Has(1));
        Check(s.Has(2));
        Check(s.Has(3));
        Check(s.Delete(2));
        var values = new List<int>();
        foreach (int value in s.Values())
            values.Add(value);
        Check(values.Count == 2);
        Check(values.SequenceEqual(values.Order()));
        s.Clear();
        Check(s.Size() == 0);
        Check(!s.Has(1));
        Check(!s.Has(2));
        Check(!s.Has(3));
        var s2 = s.Clone();
        Check(!ReferenceEquals(s, s2));
        Check(s2.Size() == 0);
        return 1;
    }

    // Authored public behavior; not the Go AllocsPerRun<10 assertion.
    public static int RunAuthoredControls()
    {
        var set = OrderedSet<int>.NewOrderedSetWithSizeHint(1024);
        for (int i = 0; i < 1024; i++)
            set.Add(i);
        Check(set.Size() == 1024);
        int expected = 0;
        foreach (int value in set.Values())
            Check(value == expected++);
        Check(expected == 1024);
        return 1;
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("OrderedSet assertion failed");
    }
}

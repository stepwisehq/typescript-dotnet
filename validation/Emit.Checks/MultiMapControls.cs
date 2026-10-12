// Authored public-boundary controls for the complete collections.MultiMap dependency.
using TypeScript.Collections;
using TypeScript.Scanner;
namespace Emit.Checks;
public static class MultiMapControls
{
    public static int RunAll()
    {
        var hinted = Collections.NewMultiMapWithSizeHint<string, int>(-1);
        Require(hinted.Len() == 0 && !hinted.Has("key"), "Negative hint must create an empty map");
        hinted.Add("key", 7);
        Require(hinted.Get("key").Length == 1 && hinted.Get("key")[0] == 7, "Negative-hint map must accept values");

        var map = new MultiMap<string, int>();
        Require(map.Len() == 0 && map.Get("missing").IsNil, "Zero owner lookup must preserve nil slice");
        map.Add("a", 1);
        map.Add("a", 2);
        map.Add("a", 3);
        map.Remove("a", 2);
        Require(map.Get("a").AsSpan().SequenceEqual(new[] { 1, 3 }), "Removal must preserve remaining value order");
        map.Remove("a", 1);
        map.Remove("a", 3);
        Require(map.Len() == 0 && !map.Has("a"), "Last removal must remove key");

        var grouped = Collections.GroupBy(Slice<int>.FromArray(new[] { 1, 2, 3, 4 }), value => value % 2);
        Require(grouped.Get(1).AsSpan().SequenceEqual(new[] { 1, 3 }) && grouped.Get(0).AsSpan().SequenceEqual(new[] { 2, 4 }), "Grouping must retain input order within each key");
        grouped.Clear();
        Require(grouped.Len() == 0 && grouped.Get(1).IsNil, "Clear must remove all entries");
        return 7;
    }
    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

// Copyright (c) Microsoft Corporation. Apache 2.0: licenses/TypeScript-LICENSE.txt.
// Original ordered_map_test.go storage assertions. The selected JSON subset uses
// the actual static ordered-object decoder; arbitrary-key Go reflection is excluded.
using System.Globalization;
using TypeScript.Collections;
using TypeScript.Json;
using TypeScript.Scanner;

namespace Configuration.Checks;
public static class OrderedMapChecks
{
    public static IReadOnlyList<string> Cases { get; } = ["TestOrderedMapEqualFunc", "TestOrderedMap", "TestOrderedMapClone", "TestOrderedMapClear", "Adapted.TestOrderedMapUnmarshalJSON.StringAny"];

    public static void Run(string name)
    {
        switch (name)
        {
            case "TestOrderedMapEqualFunc":
                Equal();
                break;
            case "TestOrderedMap":
                Map();
                break;
            case "TestOrderedMapClone":
                Clone();
                break;
            case "TestOrderedMapClear":
                Clear();
                break;
            case "Adapted.TestOrderedMapUnmarshalJSON.StringAny":
                Json();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    private static string Pad(int value) => value.ToString(CultureInfo.InvariantCulture).PadLeft(10);
    private static OrderedMap<int, string> From(params MapEntry<int, string>[] rows) => OrderedMaps.NewOrderedMapFromList(rows);
    private static void Equal()
    {
        var zero = new OrderedMap<int, string>();
        var allocated = OrderedMaps.NewOrderedMapWithSizeHint<int, string>(0);
        var ordered = From(new(1, "a"), new(2, "b"));
        var reversed = From(new(2, "b"), new(1, "a"));
        var uppercase = From(new(1, "A"), new(2, "B"));
        var different = ordered.Clone();
        different.Set(2, "c");
        var cleared = ordered.Clone();
        cleared.Clear();
        (string Name, OrderedMap<int, string>? A, OrderedMap<int, string>? B, bool Equal)[] rows = [("nil", null, null, true), ("nil versus empty", null, zero, false), ("empty allocation", zero, allocated, true), ("cleared allocation", zero, cleared, true), ("same", ordered, ordered, true), ("clone", ordered, ordered.Clone(), true), ("different order", ordered, reversed, false), ("different values", ordered, different, false), ("different length", zero, ordered, false), ("custom equality", ordered, uppercase, true)];
        foreach (var row in rows)
        {
            Check.Equal(OrderedMaps.EqualFunc(row.A, row.B, UnicodeCase.EqualFold), row.Equal, row.Name + " forward equality");
            Check.Equal(OrderedMaps.EqualFunc(row.B, row.A, UnicodeCase.EqualFold), row.Equal, row.Name + " reverse equality");
        }
    }

    private static void Map()
    {
        var map = new OrderedMap<int, string>();
        Check.True(!map.Has(1), "Original zero map Has");
        const int count = 1000, start = 1, end = start + count;
        for (int index = start; index < end; index++)
            map.Set(index, Pad(index));
        Check.Equal(map.Size(), count, "Seed count");
        for (int index = end - 1; index >= start; index--)
            map.Set(index, Pad(index));
        Check.Equal(map.Size(), count, "Overwrite count");
        for (int index = start; index < end; index++)
        {
            var(value, ok) = map.Get(index);
            Check.True(ok, "Original Get present");
            Check.Equal(value, Pad(index), "Original Get value");
        }

        foreach (var(key, value)in map.Entries())
            Check.Equal(value, Pad(key), "Original Entries value");
        var keys = map.Keys().ToArray();
        Check.Equal(keys.Length, count, "Original Keys count");
        Check.True(keys.SequenceEqual(keys.Order()), "Original Keys sorted");
        var values = map.Values().ToArray();
        Check.Equal(values.Length, count, "Original Values count");
        Check.True(values.SequenceEqual(values.Order(StringComparer.Ordinal)), "Original Values sorted");
        Check.Equal(map.Keys().First(), start, "Early-break Keys");
        Check.Equal(map.Values().First(), Pad(start), "Early-break Values");
        var first = map.Entries().First();
        Check.Equal(first.Key, start, "Early-break Entries key");
        Check.Equal(first.Value, Pad(start), "Early-break Entries value");
        for (int index = start + 1; index < end; index++)
        {
            var(value, ok) = map.Delete(index);
            Check.True(ok, "Original Delete present");
            Check.Equal(value, Pad(index), "Original Delete value");
            Check.True(!map.Has(index), "Original Has after delete");
            (value, ok) = map.Get(index);
            Check.True(!ok, "Original Get missing");
            Check.Equal(value, "", "Original Get zero string");
            (value, ok) = map.Delete(index);
            Check.True(!ok, "Original Delete missing");
            Check.Equal(value, "", "Original Delete zero string");
        }

        Check.Equal(map.Size(), 1, "One key remains");
        Check.True(map.Has(start), "Start key remains");
        var(last, present) = map.Delete(start);
        Check.True(present, "Original final Delete");
        Check.Equal(last, Pad(start), "Original final value");
        Check.Equal(map.Size(), 0, "Final count");
    }

    private static void Clone()
    {
        var map = From(new(1, "one"), new(2, "two"));
        var clone = map.Clone();
        Check.True(!ReferenceEquals(map, clone), "Original distinct clone");
        Check.Equal(clone.Size(), 2, "Clone count");
        Check.Sequence(clone.Keys().ToArray(), new[] { 1, 2 }, "Clone keys");
        Check.Sequence(clone.Values().ToArray(), new[] { "one", "two" }, "Clone values");
        var(value, ok) = clone.Get(1);
        Check.True(ok, "Clone Get");
        Check.Equal(value, "one", "Clone value");
        map.Delete(1);
        Check.Equal(map.Size(), 1, "Original delete changes source");
        Check.Equal(clone.Size(), 2, "Clone unchanged");
        Check.Sequence(clone.Keys().ToArray(), new[] { 1, 2 }, "Clone keys unchanged");
        Check.Sequence(clone.Values().ToArray(), new[] { "one", "two" }, "Clone values unchanged");
    }

    private static void Clear()
    {
        var map = From(new(1, "one"), new(2, "two"));
        map.Clear();
        Check.Equal(map.Size(), 0, "Original Clear count");
    }

    private static void Json()
    {
        var map = new OrderedMap<string, object?>();
        var error = JsonCodec.UnmarshalObject("{\"a\": 1, \"b\": \"two\", \"c\": { \"d\": 4 } }", map, "cannot unmarshal non-object JSON value into Map");
        Check.Equal(error, null, "Original string-any object decode");
        Check.Equal(map.Size(), 3, "Original decoded map size");
        Check.Equal(map.GetOrZero("a"), (object)1.0, "Original float64 value");
        error = JsonCodec.UnmarshalObject("null", map, "cannot unmarshal non-object JSON value into Map");
        Check.Equal(error, null, "Original null decode succeeds");
        error = JsonCodec.UnmarshalObject("\"foo\"", map, "cannot unmarshal non-object JSON value into Map");
        Check.True(error?.Message.Contains("cannot unmarshal non-object JSON value into Map", StringComparison.Ordinal) == true, "Original non-object rejection text");
    // The original int-key reflection decode is excluded, not emulated.
    }
}

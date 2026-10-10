// Authored .NET adapter checks, not upstream Go test bodies. Existing List append
// deliberately returns a detached copy for additions; Go slice sharing is separate.
using System.Runtime.InteropServices;
using TypeScript.Scanner;

namespace TypeScript.Foundation;

public static class ListAppendChecks
{
    public static IReadOnlyList<string> Cases { get; } =
        ["no-additions-identity", "scalar-and-multiple", "array-span-enumerable", "detached-copies", "generic-and-array-elements"];
    public static void Run(string name)
    {
        switch (name)
        {
            case "no-additions-identity": NoAdditions(); break;
            case "scalar-and-multiple": ScalarAndMultiple(); break;
            case "array-span-enumerable": ArraySpanEnumerable(); break;
            case "detached-copies": DetachedCopies(); break;
            case "generic-and-array-elements": GenericAndArrayElements(); break;
            default: throw new ArgumentException("Unknown list-append check: " + name, nameof(name));
        }
    }
    private sealed class Marker { }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Elements(List<Marker> actual, params Marker[] expected)
    {
        Require(actual.Count == expected.Length, "Append element count");
        for (int i = 0; i < expected.Length; i++) Require(ReferenceEquals(actual[i], expected[i]), "Append shallow element identity/order");
    }
    private static void NoAdditions()
    {
        List<Marker> source = [new Marker()];
        Require(ReferenceEquals(source, GoRuntime.Append(source)), "Zero arguments retain source");
        Require(ReferenceEquals(source, GoRuntime.Append(source, null)), "Null literal retains old array binding");
        Require(ReferenceEquals(source, GoRuntime.Append(source, default)), "Default literal retains old array binding");
        Require(ReferenceEquals(source, GoRuntime.Append(source, Array.Empty<Marker>())), "Empty array retains source");
        Require(ReferenceEquals(source, GoRuntime.Append(source, ReadOnlySpan<Marker>.Empty)), "Empty span retains source");
        Require(ReferenceEquals(source, GoRuntime.Append(source, Enumerable.Empty<Marker>())), "Empty enumerable retains source");
        Require(GoRuntime.Append<Marker>(null!) == null, "Nil source with no additions remains nil");
        var empty = new List<Marker>();
        Require(ReferenceEquals(empty, GoRuntime.Append(empty)), "Empty nonnil source remains same List");
    }
    private static void ScalarAndMultiple()
    {
        var a = new Marker(); var b = new Marker(); var c = new Marker();
        List<Marker> source = [a];
        Elements(GoRuntime.Append(source, b), a, b);
        Elements(GoRuntime.Append(source, b, c), a, b, c);
        Elements(GoRuntime.Append(source, (Marker)null!), a, null!);
        Elements(GoRuntime.Append<Marker>(null!, b), b);
        Elements(source, a);
    }
    private static void ArraySpanEnumerable()
    {
        var a = new Marker(); var b = new Marker(); var c = new Marker();
        List<Marker> source = [a]; Marker[] values = [b, c];
        Elements(GoRuntime.Append(source, values), a, b, c);
        Elements(GoRuntime.Append(source, values.AsSpan(1)), a, c);
        IEnumerable<Marker> enumerable = values;
        Elements(GoRuntime.Append(source, enumerable), a, b, c);
        Marker[]? nilArray = null; IEnumerable<Marker>? nilEnumerable = null;
        Require(ReferenceEquals(source, GoRuntime.Append(source, nilArray!)), "Typed null array retains source");
        Require(ReferenceEquals(source, GoRuntime.Append(source, nilEnumerable!)), "Typed null enumerable retains source");
        Elements(source, a);
        List<Marker> sideEffectSource = [a];
        int enumerationSteps = 0;
        IEnumerable<Marker> DeferredValues()
        {
            enumerationSteps++;
            sideEffectSource.Add(b);
            yield return c;
            enumerationSteps++;
        }
        var materialized = GoRuntime.Append(sideEffectSource, DeferredValues());
        Require(enumerationSteps == 2, "Enumerable is materialized exactly once before copying source");
        Elements(materialized, a, b, c); Elements(sideEffectSource, a, b);
    }
    private static void DetachedCopies()
    {
        var a = new Marker(); var b = new Marker(); var c = new Marker();
        var source = new List<Marker>(8) { a, b };
        var result = GoRuntime.Append(source, c);
        Require(!ReferenceEquals(result, source), "Nonempty addition produces detached List");
        source[0] = c; source.Add(c);
        Elements(result, a, b, c);
        result[1] = a;
        Elements(source, c, b, c);
        var prefix = GoRuntime.Append(result, b);
        result.Clear(); Elements(prefix, a, a, c, b);
        // Appending a span over the input itself must still leave the input untouched.
        var overlap = GoRuntime.Append(source, CollectionsMarshal.AsSpan(source).Slice(1, 1));
        Elements(overlap, c, b, c, b); Elements(source, c, b, c);
    }
    private static List<T> Forward<T>(List<T> source, T value) => GoRuntime.Append(source, value);
    private static void GenericAndArrayElements()
    {
        Require(Forward(new List<int> { 3 }, 5).SequenceEqual(new[] { 3, 5 }), "Generic scalar forwarding");
        var raw = "\u00FF\u00ED\u00A0\u0080";
        var strings = Forward(new List<string>(), raw);
        Require(strings.Count == 1 && ReferenceEquals(strings[0], raw), "Element byte strings are not encoded or decoded");
        Marker[] element = [new Marker()];
        var arrays = Forward(new List<Marker[]>(), element);
        Require(arrays.Count == 1 && ReferenceEquals(arrays[0], element), "Array-valued T remains one scalar element");
        IEnumerable<int> sequenceElement = new List<int> { 1, 2 };
        var sequences = Forward(new List<IEnumerable<int>>(), sequenceElement);
        Require(sequences.Count == 1 && ReferenceEquals(sequences[0], sequenceElement), "Enumerable-valued T remains one scalar element");
    }
}

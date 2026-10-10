// Authored source-boundary checks, not original upstream test bodies. The five
// Clip/write/append outcomes are separately observed by the locked Go witness.
using System.Text.Json.Nodes;
using TypeScript.Checking;
using TypeScript.Scanner;

namespace Program.Checks;

public static class SignatureSliceBoundaryChecks
{
    public static IReadOnlyList<string> Names { get; } =
        ["clipped-boundaries", "producer-normalization", "same-and-map", "public-snapshots"];
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    public static void Run(string name)
    {
        switch (name)
        {
            case "clipped-boundaries": ClippedBoundaries(); break;
            case "producer-normalization": ProducerNormalization(); break;
            case "same-and-map": SameAndMap(); break;
            case "public-snapshots": PublicSnapshots(); break;
            default: throw new ArgumentException("Unknown signature slice boundary: " + name);
        }
    }
    private static void ClippedBoundaries()
    {
        var a = new Signature(); var b = new Signature(); var c = new Signature();
        var d = new Signature(); var e = new Signature();
        var backing = Slice<Signature>.Make(3, 5);
        backing[0] = a; backing[1] = b; backing[2] = c;
        var owner = new StructuredType { signatures = backing, callSignatureCount = 2 };
        var calls = owner.CallSignaturesSlice();
        var constructs = owner.ConstructSignaturesSlice();
        calls[0] = d;
        bool shared = ReferenceEquals(owner.signatures[0], d);
        var appended = calls.Append(e); appended[0] = a;
        bool detached = ReferenceEquals(backing[0], d) && ReferenceEquals(constructs[0], c)
            && calls.Length == 2 && appended.Length == 3;
        var internalView = backing.Slice2(0, 2);
        var internalAppend = internalView.Append(e);
        bool unclipped = ReferenceEquals(constructs[0], e) && backing.Length == 3
            && internalView.Length == 2 && internalAppend.Length == 3;
        Dictionary<string, bool> actual = new(StringComparer.Ordinal)
        {
            ["clippedExistingSlotsShare"] = shared,
            ["clippedAppendDetaches"] = detached,
            ["unclippedAppendCanUseConstructSlot"] = unclipped,
            ["clipPreservesNil"] = GoSlices.Clip(default(Slice<Signature>)).IsNil,
            ["clipPreservesAllocatedEmpty"] = !GoSlices.Clip(Slice<Signature>.Make(0)).IsNil,
        };
        using var stream = typeof(SignatureSliceBoundaryChecks).Assembly.GetManifestResourceStream("Program.Checks.SignatureSliceWitness")
            ?? throw new InvalidOperationException("Go slice boundary witness is missing.");
        using var reader = new StreamReader(stream);
        var expected = JsonNode.Parse(reader.ReadToEnd())!.AsObject();
        Require(expected.Count == actual.Count, "Go witness observation count differs");
        foreach (var (key, value) in actual)
            Require(expected[key]!.GetValue<bool>() == value, "Go slice boundary differs: " + key);
    }
    private static void ProducerNormalization()
    {
        // Exercise the actual source producer. No new observer or graph output.
        var checker = Checker.Create(Parser.ParseSourceFile(new("/slice-boundary.ts", "/slice-boundary.ts"), "", ScriptKind.TS));
        foreach (var empty in new[] { default(Slice<Signature>), Slice<Signature>.Make(0) })
        {
            var owner = checker.newAnonymousType(null, null, empty, empty).AsStructuredType();
            Require(owner.signatures.IsNil && owner.callSignatureCount == 0, "Both empty kinds normalize to nil");
            Require(owner.CallSignatures() is null && owner.ConstructSignatures() is null, "Normalized public snapshots stay nil");
        }
        var a = new Signature(); var b = new Signature(); var c = new Signature();
        var calls = Slice<Signature>.Make(1, 4); calls[0] = a;
        var callOnly = checker.newAnonymousType(null, null, calls).AsStructuredType();
        calls[0] = b;
        Require(callOnly.signatures.Capacity == 1 && ReferenceEquals(callOnly.signatures[0], b), "Single call family shares clipped input");
        var constructs = Slice<Signature>.Make(1, 4); constructs[0] = c;
        var constructOnly = checker.newAnonymousType(null, null, default, constructs).AsStructuredType();
        constructs[0] = a;
        Require(constructOnly.callSignatureCount == 0 && constructOnly.signatures.Capacity == 1
            && ReferenceEquals(constructOnly.signatures[0], a), "Single construct family shares clipped input");
        var both = checker.newAnonymousType(null, null, calls, constructs).AsStructuredType();
        calls[0] = c; constructs[0] = c;
        Require(both.callSignatureCount == 1 && both.signatures.Length == 2
            && ReferenceEquals(both.signatures[0], b) && ReferenceEquals(both.signatures[1], a), "Two families concatenate into detached ordered storage");
    }
    private static void SameAndMap()
    {
        var a = new Signature(); var b = new Signature(); var c = new Signature();
        var values = Slice<Signature>.Make(2, 4); values[0] = a; values[1] = b;
        Require(Core.Same(values, GoSlices.Clip(values)), "Same ignores capacity");
        Require(Core.Same(default(Slice<Signature>), Slice<Signature>.Make(0)), "Same treats all empty headers alike");
        Require(!Core.Same(values, values.Slice2(1)), "Different start/length is not Same");
        Require(!Core.Same(values, GoSlices.Clone(values)), "Detached equal references are not Same storage");
        var unchanged = Core.SameMap(values, signature => signature);
        Require(Core.Same(values, unchanged), "Unchanged map returns original storage");
        int visits = 0;
        var changed = Core.SameMap(values, signature => { visits++; return ReferenceEquals(signature, a) ? c : signature; });
        Require(visits == 2 && !Core.Same(values, changed) && ReferenceEquals(changed[0], c)
            && ReferenceEquals(changed[1], b) && ReferenceEquals(values[0], a), "Changed map owns ordered shallow result");
    }
    private static void PublicSnapshots()
    {
        var a = new Signature(); var b = new Signature();
        var storage = Slice<Signature>.Make(2); storage[0] = a; storage[1] = b;
        var owner = new StructuredType { signatures = storage, callSignatureCount = 1 };
        var calls = owner.CallSignatures(); var constructs = owner.ConstructSignatures();
        Require(ReferenceEquals(calls[0], a) && ReferenceEquals(constructs[0], b), "Public snapshots retain shallow signature references/order");
        calls[0] = b; calls.Clear(); constructs.Add(a);
        Require(ReferenceEquals(storage[0], a) && storage.Length == 2 && owner.ConstructSignatures().Count == 1,
            "Public List mutations do not modify internal signature storage");
        Require(new StructuredType().CallSignatures() is null, "Nil public header remains nil");
        var empty = new StructuredType { signatures = Slice<Signature>.Make(0) };
        Require(empty.CallSignatures() is { Count: 0 } && empty.ConstructSignatures() is { Count: 0 }, "Allocated empty public header remains nonnil");
    }
    public static int RunAll() { foreach (string name in Names) Run(name); return Names.Count; }
}

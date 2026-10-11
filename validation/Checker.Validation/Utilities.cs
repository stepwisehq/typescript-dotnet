using System.Text.Json.Serialization;
using TypeScript.Scanner;

namespace TypeScript.Checking.Validation;
public sealed class UtilityObservation
{
    public string Name { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? BoolValue { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? IntValue { get; set; }
}

public static class Utilities
{
    private sealed class ProbeValue
    {
        public ProbeValue()
        {
        }

        public int Value { get; set; }
    }

    private struct ProbeRecordValue
    {
        public int Value;
    }

    private struct ArenaProbe
    {
        public int Value;
        public object? Owner;
    }

    private delegate ref ArenaProbe ProbeGetter(int index);

    // Authored lifetime controls share the existing xUnit/Native utility seam;
    // they do not add fields/rows to its unchanged genuine-Go observation DTO.
    private static void PackedGrowthControls()
    {
        const int count = 1600;
        var keys = new object[count + 32];
        var symbols = new Symbol[count + 32];
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i] = new object();
            symbols[i] = new Symbol();
        }
        var map = new LinkStore<object, ArenaProbe>();
        var symbolStore = new symbolArenaLinkStore<ArenaProbe>();
        CheckPackedGrowth((int i) => ref map.Get(keys[i]),
            i => map.TryGet(keys[i]), i => map.Has(keys[i]));
        CheckPackedGrowth((int i) => ref symbolStore.Get(symbols[i]),
            i => symbolStore.TryGet(symbols[i]), i => symbolStore.Has(symbols[i]));

        // A lookup on a second store must not alias a slot from the first store,
        // even when both stores receive exactly the same key/symbol objects.
        var otherMap = new LinkStore<object, ArenaProbe>();
        var otherSymbols = new symbolArenaLinkStore<ArenaProbe>();
        Require(!otherMap.Has(keys[0]) && otherMap.TryGet(keys[0]).IsEmpty &&
            !otherSymbols.Has(symbols[0]) && otherSymbols.TryGet(symbols[0]).IsEmpty,
            "new stores start missing independently of an existing store");
        Require(otherMap.Get(keys[0]).Value == 0 && otherSymbols.Get(symbols[0]).Value == 0,
            "new store records start zero");
        Require(!otherMap.TryGet(keys[0]).Equals(map.TryGet(keys[0])) &&
            !otherSymbols.TryGet(symbols[0]).Equals(symbolStore.TryGet(symbols[0])),
            "locations are distinct across stores sharing a key");
    }

    private static void CheckPackedGrowth(ProbeGetter get,
        Func<int, Memory<ArenaProbe>> tryGet, Func<int, bool> has)
    {
        const int count = 1600;
        Require(!has(0) && tryGet(0).IsEmpty && !has(count - 1) && tryGet(count - 1).IsEmpty,
            "lookups do not initialize missing links");
        ref var first = ref get(0);
        var firstLocation = tryGet(0);
        Require(has(0) && firstLocation.Length == 1 && first.Value == 0 && first.Owner is null,
            "an initialized zero record is present");
        Require(!has(1) && tryGet(1).IsEmpty,
            "publishing a zero record does not initialize another link");
        var marker = new object();
        first.Value = 41;
        first.Owner = marker;
        var locations = new Memory<ArenaProbe>[count];
        int chunkStart = 0, chunkLength = 2;
        ArraySegment<ArenaProbe> previous = default;
        for (int i = 0; i < count; i++)
        {
            if (i == chunkStart + chunkLength)
            {
                chunkStart += chunkLength;
                chunkLength = Math.Min(chunkLength * 2, 256);
            }
            ref var slot = ref get(i);
            if (i != 0)
                Require(slot.Value == 0 && slot.Owner is null, "each newly published record is zero");
            var location = tryGet(i);
            Require(has(i) && location.Length == 1 &&
                System.Runtime.CompilerServices.Unsafe.AreSame(ref slot, ref location.Span[0]),
                "Get and escaped Memory alias the same live record");
            Require(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(
                (ReadOnlyMemory<ArenaProbe>)location, out var array) && array.Array is not null &&
                array.Array.Length == chunkLength && array.Offset == i - chunkStart && array.Count == 1,
                "source arena growth retains zeroed 2..256 then fixed-256 chunks");
            if (i != 0)
                Require((i == chunkStart) != ReferenceEquals(previous.Array, array.Array),
                    "only crossing a source chunk boundary changes the backing array");
            previous = array;
            slot.Value = i + 100;
            slot.Owner = marker;
            locations[i] = location;
            Require(System.Runtime.CompilerServices.Unsafe.AreSame(ref first, ref get(0)) &&
                tryGet(0).Equals(firstLocation) && ReferenceEquals(first.Owner, marker),
                "retained first ref and Memory survive chunk and lookup-table growth");
        }
        // Verify every old location, not only transition endpoints: a directory
        // decoding error or relocated slot must fail even if Get and TryGet agree.
        for (int i = 0; i < count; i++)
        {
            Require(locations[i].Equals(tryGet(i)) && locations[i].Span[0].Value == i + 100 &&
                ReferenceEquals(locations[i].Span[0].Owner, marker),
                "all escaped locations retain their original record and owner");
            locations[i].Span[0].Value = -i - 1;
            Require(get(i).Value == -i - 1, "escaped Memory mutations write through to Get");
        }
        first.Value = 901;
        Require(firstLocation.Span[0].Value == 901, "an old ref writes through after all growth");

        void Reenter(int key, int depth)
        {
            ref var live = ref get(key);
            int before = live.Value;
            ref var newlyPublished = ref get(count + key);
            Require(newlyPublished.Value == 0 && newlyPublished.Owner is null,
                "recursive publication of another key starts zero");
            newlyPublished.Value = key + 1;
            if (depth != 0)
                Reenter(key + 1, depth - 1);
            live.Value = before + 7;
            Require(tryGet(key).Span[0].Value == before + 7 &&
                System.Runtime.CompilerServices.Unsafe.AreSame(ref live, ref get(key)),
                "recursive other-key lookups preserve outer live mutation aliases");
        }
        Reenter(0, 24);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("Packed link lifetime control: " + message);
    }

    private sealed class DispatchProbeType(ObjectType result) : TypeBase
    {
        public override ObjectType AsObjectType() => result;
    }

    private sealed class DispatchProbeMapper(Type result) : TypeMapperBase
    {
        internal Type Result = result;
        internal override Type Map(Type type) => Result;
        internal override TypeMapperKind Kind() => TypeMapperKind.Merged;
        internal override bool MapsThisOnly() => true;
    }

    private static void PayloadDispatchControls()
    {
        Type intrinsic = new IntrinsicType();
        var bare = new Type();
        Require(ReferenceEquals(intrinsic, intrinsic.AsIntrinsicType()) && bare.AsIntrinsicType() is null,
            "concrete Type views retain canonical identity and the bare header's null result");
        bool wrongType = false;
        try { intrinsic.AsLiteralType(); }
        catch (InvalidCastException) { wrongType = true; }
        Require(wrongType, "a mismatched Type view retains its checked cast failure");
        Type objectType = new ObjectType();
        Require(ReferenceEquals(objectType, objectType.AsObjectType()) && intrinsic.AsObjectType() is null,
            "embedded Type views retain identity and the nonembedding null result");
        bool bareEmbedded = false;
        try { bare.AsObjectType(); }
        catch (NullReferenceException) { bareEmbedded = true; }
        Require(bareEmbedded, "a bare Type header retains the embedded view's null dispatch failure");
        Type dispatchProbe = new DispatchProbeType(objectType.AsObjectType());
        Require(ReferenceEquals(dispatchProbe.AsObjectType(), objectType),
            "a Type header call retains the actual custom embedded-view override");
        var mapperPayload = new DispatchProbeMapper(intrinsic);
        TypeMapper mapper = mapperPayload;
        Require(ReferenceEquals(mapper.Map(objectType), intrinsic) && mapper.Kind() == TypeMapperKind.Merged &&
            mapper.MapsThisOnly(), "Mapper header calls retain custom payload overrides");
        mapperPayload.Result = objectType;
        Require(ReferenceEquals(mapper.Map(intrinsic), objectType), "Mapper dispatch reads live payload state");
        bool bareMapper = false;
        try { new TypeMapper().Map(intrinsic); }
        catch (NullReferenceException) { bareMapper = true; }
        Require(bareMapper, "a bare Mapper header retains the payload dispatch's null failure");

        var direct = TypeScript.PseudoChecking.PseudoType.NewPseudoTypeDirect(new NodeFactory().NewIdentifier("payload"));
        var barePseudo = new TypeScript.PseudoChecking.PseudoType();
        Require(ReferenceEquals(direct, direct.AsPseudoTypeDirect()) && barePseudo.AsPseudoTypeDirect() is null,
            "concrete PseudoType views retain canonical identity and the bare header's null result");
        bool wrongPseudo = false;
        try { direct.AsPseudoTypeTuple(); }
        catch (InvalidCastException) { wrongPseudo = true; }
        Require(wrongPseudo, "a mismatched PseudoType view retains its checked cast failure");
    }

    public static List<UtilityObservation> Run()
    {
        PayloadDispatchControls();
        PackedGrowthControls();
        var observations = new List<UtilityObservation>();
        void Flag(string name, bool value) => observations.Add(new() { Name = name, BoolValue = value });
        void Count(string name, int value) => observations.Add(new() { Name = name, IntValue = value });
        var store = new PagedLinkStore<ProbeValue>();
        Flag("paged.unallocated", !store.TryGet(1, out _) && !store.Has(1));
        ref var first = ref store.Get(0);
        first = new() { Value = 7 };
        Flag("paged.same-page-zero", store.Has(1) && store.TryGet(1, out var neighbor) && neighbor is null);
        ref var neighborSlot = ref store.Get(1);
        neighborSlot = new() { Value = 8 };
        Flag("paged.stable-distinct-slots", ReferenceEquals(store.Get(0), first) &&
            store.TryGet(1, out neighbor) && ReferenceEquals(neighbor, neighborSlot) &&
            !ReferenceEquals(neighborSlot, first) && store.Get(0)?.Value == 7);
        Flag("paged.other-page-unallocated", !store.TryGet(256, out _));
        ulong low = (65535UL << 8) + 255, high = 65536UL << 8;
        ref var lowSlot = ref store.Get(low);
        lowSlot = new() { Value = 11 };
        ref var highSlot = ref store.Get(high);
        highSlot = new() { Value = 12 };
        // Retained refs must still write through after the page table grows.
        first = new() { Value = 9 };
        store.Get(high + 512) = new() { Value = 13 };
        lowSlot = new() { Value = 14 };
        highSlot = new() { Value = 15 };
        Flag("paged.last-list-first-map-stable", ReferenceEquals(store.Get(low), lowSlot) &&
            ReferenceEquals(store.Get(high), highSlot) && !ReferenceEquals(lowSlot, highSlot) &&
            ReferenceEquals(store.Get(0), first) && store.Get(0)?.Value == 9 &&
            store.Get(low)?.Value == 14 && store.Get(high)?.Value == 15);
        Flag("paged.high-map-neighbor-zero", store.Has(high + 1) &&
            store.TryGet(high + 1, out var highNeighbor) && highNeighbor is null);
        Flag("paged.high-other-page-unallocated", !store.TryGet(high + 256, out _));
        var factory = new NodeFactory();
        Node left, right;
        do
        {
            left = factory.NewIdentifier("left");
            right = factory.NewIdentifier("right");
        }
        while ((Ast.GetNodeId(left) >> 8) != (Ast.GetNodeId(right) >> 8));
        var nodeStore = new nodeLinkStore<ProbeRecordValue>();
        Flag("node.unallocated", nodeStore.TryGet(left).IsEmpty && !nodeStore.Has(left));
        ref var nodeSlot = ref nodeStore.Get(left);
        nodeSlot.Value = 19;
        var nodeLocation = nodeStore.TryGet(left);
        var rightLocation = nodeStore.TryGet(right);
        Flag("node.same-page-zero", nodeStore.Has(right) && !rightLocation.IsEmpty && rightLocation.Span[0].Value == 0);
        Flag("node.stable-identities", nodeStore.TryGet(left).Equals(nodeLocation) &&
            nodeStore.TryGet(right).Equals(rightLocation) && !rightLocation.Equals(nodeLocation) &&
            System.Runtime.CompilerServices.Unsafe.AreSame(ref nodeStore.Get(left), ref nodeSlot) &&
            System.Runtime.CompilerServices.Unsafe.AreSame(ref rightLocation.Span[0], ref nodeStore.Get(right)));
        Symbol a, b;
        do
        {
            a = new()
            {
                Name = "a"
            };
            b = new()
            {
                Name = "b"
            };
        }
        while ((Ast.GetSymbolId(a) >> 8) != (Ast.GetSymbolId(b) >> 8));
        var symbolStore = new symbolArenaLinkStore<ProbeRecordValue>();
        Flag("symbol.unallocated", symbolStore.TryGet(a).IsEmpty && !symbolStore.Has(a));
        ref var symbolSlot = ref symbolStore.Get(a);
        symbolSlot.Value = 29;
        var symbolLocation = symbolStore.TryGet(a);
        Flag("symbol.same-page-still-nil", symbolStore.TryGet(b).IsEmpty && !symbolStore.Has(b));
        ref var otherSymbolSlot = ref symbolStore.Get(b);
        Flag("symbol.stable-distinct-identities", symbolStore.TryGet(a).Equals(symbolLocation) &&
            !symbolStore.TryGet(b).Equals(symbolLocation) &&
            System.Runtime.CompilerServices.Unsafe.AreSame(ref symbolStore.Get(a), ref symbolSlot) &&
            System.Runtime.CompilerServices.Unsafe.AreSame(ref symbolLocation.Span[0], ref symbolSlot) &&
            otherSymbolSlot.Value == 0);
        var file = Parser.ParseSourceFile(default(SourceFileParseOptions), "", ScriptKind.TS);
        var symbol = new Symbol
        {
            Name = "probe"
        };
        Flag("tables.initially-nil", file.Locals is null && symbol.Members is null && symbol.Exports is null);
        var locals = Ast.GetLocals(file.AsNode());
        var members = Ast.GetMembers(symbol);
        var exports = Ast.GetExports(symbol);
        Flag("tables.allocated-empty", locals is not null && members is not null && exports is not null);
        Count("tables.locals-count", locals!.Count);
        Count("tables.members-count", members!.Count);
        Count("tables.exports-count", exports!.Count);
        Flag("tables.stable-lazy-identities", ReferenceEquals(Ast.GetLocals(file.AsNode()), locals) && ReferenceEquals(Ast.GetMembers(symbol), members) && ReferenceEquals(Ast.GetExports(symbol), exports));
        Flag("tables.distinct-identities", !ReferenceEquals(locals, members) && !ReferenceEquals(members, exports) && !ReferenceEquals(locals, exports));
        return observations;
    }
}

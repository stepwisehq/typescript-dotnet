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

    public static List<UtilityObservation> Run()
    {
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

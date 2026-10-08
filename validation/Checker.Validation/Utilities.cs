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

    public static List<UtilityObservation> Run()
    {
        var observations = new List<UtilityObservation>();
        void Flag(string name, bool value) => observations.Add(new() { Name = name, BoolValue = value });
        void Count(string name, int value) => observations.Add(new() { Name = name, IntValue = value });
        var store = new PagedLinkStore<ProbeValue>();
        Flag("paged.unallocated", store.TryGet(1)is null && !store.Has(1));
        var first = store.Get(0);
        first.Value = new()
        {
            Value = 7
        };
        Flag("paged.same-page-zero", store.Has(1) && store.TryGet(1)is { } neighbor && (neighbor.Value?.Value ?? 0) == 0);
        Flag("paged.stable-distinct-slots", ReferenceEquals(store.Get(0), first) && !ReferenceEquals(store.TryGet(1), first));
        Flag("paged.other-page-unallocated", store.TryGet(256)is null);
        ulong low = (65535UL << 8) + 255, high = 65536UL << 8;
        var lowSlot = store.Get(low);
        var highSlot = store.Get(high);
        Flag("paged.last-list-first-map-stable", ReferenceEquals(store.Get(low), lowSlot) && ReferenceEquals(store.Get(high), highSlot) && !ReferenceEquals(lowSlot, highSlot));
        Flag("paged.high-map-neighbor-zero", store.Has(high + 1) && (store.TryGet(high + 1)?.Value?.Value ?? 0) == 0);
        Flag("paged.high-other-page-unallocated", store.TryGet(high + 256)is null);
        var factory = new NodeFactory();
        Node left, right;
        do
        {
            left = factory.NewIdentifier("left");
            right = factory.NewIdentifier("right");
        }
        while ((Ast.GetNodeId(left) >> 8) != (Ast.GetNodeId(right) >> 8));
        var nodeStore = new nodeLinkStore<ProbeValue>();
        Flag("node.unallocated", nodeStore.TryGet(left)is null && !nodeStore.Has(left));
        var nodeSlot = nodeStore.Get(left);
        nodeSlot.Value = 19;
        Flag("node.same-page-zero", nodeStore.Has(right) && nodeStore.TryGet(right)is { Value: 0 });
        Flag("node.stable-identities", ReferenceEquals(nodeStore.Get(left), nodeSlot) && ReferenceEquals(nodeStore.TryGet(right), nodeStore.Get(right)) && !ReferenceEquals(nodeStore.TryGet(right), nodeSlot));
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
        var symbolStore = new symbolArenaLinkStore<ProbeValue>();
        Flag("symbol.unallocated", symbolStore.TryGet(a)is null && !symbolStore.Has(a));
        var symbolSlot = symbolStore.Get(a);
        symbolSlot.Value = 29;
        Flag("symbol.same-page-still-nil", symbolStore.TryGet(b)is null && !symbolStore.Has(b));
        var otherSymbolSlot = symbolStore.Get(b);
        Flag("symbol.stable-distinct-identities", ReferenceEquals(symbolStore.Get(a), symbolSlot) && ReferenceEquals(symbolStore.TryGet(a), symbolSlot) && !ReferenceEquals(otherSymbolSlot, symbolSlot) && otherSymbolSlot.Value == 0);
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

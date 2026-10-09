// Authored nonforcing observations of selected retained flow associations.
#nullable disable
using TypeScript.Scanner;
using System.Buffers.Binary;

namespace TypeScript.Checking;
public sealed partial class Checker
{
    internal sealed record CollectionsFlowToken(string Kind, string Bytes, object Owner);
    private sealed record CollectionsFlowSlot(int Offset, int Width, string Kind, object Owner);
    private sealed record CollectionsFlowInput(byte[] Bytes, List<CollectionsFlowSlot> Slots, bool Ambiguous);
    private readonly Dictionary<keyBuilder, List<CollectionsFlowSlot>> collectionsFlowBuilders = new();
    private readonly Dictionary<CacheHashKey, CollectionsFlowInput> collectionsFlowKeys = new();
    private void verificationCollectionsFlowStart(keyBuilder builder)
    {
        if (authoredCollectionsProfile)
            collectionsFlowBuilders.Add(builder, []);
    }

    private void verificationCollectionsFlowSlot(keyBuilder builder, string kind, object owner, int width)
    {
        if (collectionsFlowBuilders.TryGetValue(builder, out var slots))
            slots.Add(new(builder.Length - width, width, kind, owner));
    }

    private void verificationCollectionsFlowEnd(keyBuilder builder, CacheHashKey key, bool success)
    {
        if (!collectionsFlowBuilders.Remove(builder, out var slots) || !success)
            return;
        var bytes = Convert.FromBase64String(key.preimage);
        var input = new CollectionsFlowInput(bytes, slots, false);
        if (collectionsFlowKeys.TryGetValue(key, out var old) && (old.Ambiguous || !old.Bytes.SequenceEqual(bytes) || old.Slots.Count != slots.Count || old.Slots.Where((slot, i) => slot.Offset != slots[i].Offset || slot.Width != slots[i].Width || slot.Kind != slots[i].Kind || !ReferenceEquals(slot.Owner, slots[i].Owner)).Any()))
            input = input with
            {
                Ambiguous = true
            };
        collectionsFlowKeys[key] = input;
    }

    private List<CollectionsFlowToken> verificationCollectionsFlowTokens(CacheHashKey key)
    {
        if (key == nonDottedNameCacheKey)
            return[new("bytes", "?", null)];
        if (!collectionsFlowKeys.TryGetValue(key, out var input) || input.Ambiguous || !Convert.FromBase64String(key.preimage).SequenceEqual(input.Bytes))
            throw new InvalidOperationException("Unknown flow reference key producer");
        var output = new List<CollectionsFlowToken>();
        int offset = 0;
        foreach (var slot in input.Slots)
        {
            if (slot.Offset < offset || slot.Offset + slot.Width > input.Bytes.Length)
                throw new InvalidOperationException("Overlapping flow key tokens");
            if (slot.Offset > offset)
                output.Add(new("bytes", GoString.FromBytes(input.Bytes[offset..slot.Offset]), null));
            var segment = input.Bytes.AsSpan(slot.Offset, slot.Width);
            switch (slot.Owner)
            {
                case Type type when slot.Kind == "type" && slot.Width == 4 && type.id != 0 && (type.checker == null || ReferenceEquals(type.checker, this)) && BinaryPrimitives.ReadUInt32LittleEndian(segment) == type.id:
                    break;
                case Symbol symbol when slot.Kind == "symbol" && slot.Width == 8 && symbol.id != 0 && BinaryPrimitives.ReadUInt64LittleEndian(segment) == unchecked((ulong)symbol.id):
                    break;
                case Node node when slot.Kind == "node" && slot.Width == 8 && node.id != 0 && BinaryPrimitives.ReadUInt64LittleEndian(segment) == unchecked((ulong)node.id):
                    break;
                default:
                    throw new InvalidOperationException("Invalid flow key entity association");
            }

            output.Add(new(slot.Kind, null, slot.Owner));
            offset = slot.Offset + slot.Width;
        }

        if (offset < input.Bytes.Length)
            output.Add(new("bytes", GoString.FromBytes(input.Bytes[offset..]), null));
        return output;
    }

    internal List<Type> VerificationCollectionsFlowValues()
    {
        var values = flowLoopCache.Values.Concat(narrowedTypes.Values).Concat(assignmentReducedTypes.Values).Concat(contextFreeTypes.Values).Concat(discriminatedContextualTypes.Values).Concat(flowTypeCache == null ? Enumerable.Empty<Type>() : flowTypeCache.Values).Where(t => t != null).ToList();
        foreach (var key in narrowedTypes.Keys)
        {
            values.Add(key.t);
            values.Add(key.candidate);
        }

        foreach (var key in flowLoopCache.Keys)
            foreach (var token in verificationCollectionsFlowTokens(key.refKey))
                if (token.Owner is Type type)
                    values.Add(type);
        return values;
    }

    internal (bool IsNull, int Count, List<(FlowNode Flow, List<CollectionsFlowToken> Key, Type Value)> Entries) VerificationCollectionsFlowLoops()
    {
        var entries = new List<(FlowNode, List<CollectionsFlowToken>, Type)>();
        foreach (var pair in flowLoopCache)
            entries.Add((pair.Key.flowNode, verificationCollectionsFlowTokens(pair.Key.refKey), pair.Value));
        return (false, flowLoopCache.Count, entries);
    }

    internal (bool IsNull, int Count, List<(Type Type, Type Candidate, bool AssumeTrue, bool CheckDerived, Type Value)> Entries) VerificationCollectionsNarrowedTypes()
    {
        var entries = new List<(Type, Type, bool, bool, Type)>();
        foreach (var pair in narrowedTypes)
            entries.Add((pair.Key.t, pair.Key.candidate, pair.Key.assumeTrue, pair.Key.checkDerived, pair.Value));
        return (false, narrowedTypes.Count, entries);
    }

    internal (bool IsNull, int Count, List<(Type Declared, Type Assigned, Type Value)> Entries) VerificationCollectionsAssignmentReducedTypes()
    {
        var entries = new List<(Type, Type, Type)>();
        foreach (var pair in assignmentReducedTypes)
            entries.Add((pair.Key.Item1, pair.Key.Item2, pair.Value));
        return (false, assignmentReducedTypes.Count, entries);
    }

    internal (bool IsNull, int Count, List<(FlowNode Flow, bool Value)> Entries, FlowNode LastFlow, bool LastReachable) VerificationCollectionsReachableFlows() => (false, flowNodeReachable.Count, flowNodeReachable.Select(pair => (pair.Key, pair.Value)).ToList(), lastFlowNode, lastFlowNodeReachable);
    internal (bool LoopStackNull, int LoopStackCount, int ActiveMapperCount, int ActiveMapperCacheCount) VerificationCollectionsFlowLifetime() => (flowLoopStack == null, flowLoopStack?.Count ?? 0, activeMappers.Count, activeTypeMappersCaches.Count);
    internal List<(string Name, bool IsNull, int Count, List<(Node Node, Type Value)> Entries)> VerificationCollectionsExpressionCaches()
    {
        var result = new List<(string, bool, int, List<(Node, Type)>)>();
        foreach (var item in new (string Name, Dictionary<Node, Type> Map)[]
        {
            ("context-free-types", contextFreeTypes),
            ("flow-type-cache", flowTypeCache)
        }

        )
        {
            List<(Node, Type)> entries = item.Map == null ? null : new();
            if (item.Map != null)
                foreach (var pair in item.Map)
                {
                    if (pair.Key == null || !item.Map.TryGetValue(pair.Key, out var actual) || !ReferenceEquals(actual, pair.Value))
                        throw new InvalidOperationException("Expression cache membership mismatch");
                    entries.Add((pair.Key, pair.Value));
                }

            if (entries != null && entries.Count != item.Map.Count)
                throw new InvalidOperationException("Expression cache cardinality mismatch");
            result.Add((item.Name, item.Map == null, item.Map?.Count ?? 0, entries));
        }

        return result;
    }

    internal (bool IsNull, int Count, List<(Node Node, Type ContextualType, Type Value)> Entries) VerificationCollectionsDiscriminatedTypes(IReadOnlyDictionary<ulong, Node> knownNodes, IReadOnlyDictionary<uint, Type> knownTypes)
    {
        var entries = new List<(Node, Type, Type)>();
        var keys = new HashSet<DiscriminatedContextualTypeKey>();
        foreach (var pair in discriminatedContextualTypes)
        {
            if (!knownNodes.TryGetValue(pair.Key.nodeId, out var node) || node == null || node.id == 0 || unchecked((ulong)node.id) != pair.Key.nodeId || !knownTypes.TryGetValue(pair.Key.typeId, out var type) || type == null || type.id == 0 || type.id != pair.Key.typeId || type.checker != null && !ReferenceEquals(type.checker, this))
                throw new InvalidOperationException("Unrooted discriminated contextual cache key");
            var key = new DiscriminatedContextualTypeKey(unchecked((ulong)node.id), type.id);
            if (key != pair.Key || !keys.Add(key) || !discriminatedContextualTypes.TryGetValue(key, out var actual) || !ReferenceEquals(actual, pair.Value))
                throw new InvalidOperationException("Discriminated contextual cache membership mismatch");
            entries.Add((node, type, pair.Value));
        }

        if (entries.Count != discriminatedContextualTypes.Count || keys.Count != discriminatedContextualTypes.Count)
            throw new InvalidOperationException("Discriminated contextual cache cardinality mismatch");
        return (false, discriminatedContextualTypes.Count, entries);
    }
}

// Authored complete associations for the finite selected retained cache families.
using TypeScript.Scanner;

namespace TypeScript.Checking.CollectionsValidation;
public sealed partial class StoredGraph
{
    private string Seed(Type? type) => TypeSeed(type, new(ReferenceEqualityComparer.Instance));
    private int NodeSeed(Node node)
    {
        for (int i = 0; i < syntax.RawNodes.Count; i++)
            if (ReferenceEquals(node, syntax.RawNodes[i]))
                return i;
        throw new InvalidDataException("Unrooted stored-cache node term");
    }

    private string SignatureSeed(Signature signature, HashSet<Signature>? stack = null)
    {
        stack ??= new(ReferenceEqualityComparer.Instance);
        if (!stack.Add(signature))
            throw new InvalidDataException("Signature cache seed cycle");
        try
        {
            string seed = "flags:" + (uint)signature.flags + "/decl:" + (signature.declaration == null ? "null" : NodeSeed(signature.declaration));
            if (ids.TryGetValue(signature, out int id))
                seed += "/root:" + id;
            if (signature.target != null)
                seed += "/target(" + SignatureSeed(signature.target, stack) + ")";
            if (signature.mapper != null)
            {
                if (ids.TryGetValue(signature.mapper, out int mapperId))
                    seed += "/mapper-root:" + mapperId;
                else
                    switch (signature.mapper.data)
                    {
                        case SimpleTypeMapper m:
                            seed += "/simple(" + Seed(m.source) + "," + Seed(m.target) + ")";
                            break;
                        case ArrayTypeMapper m:
                            seed += "/array(" + string.Join(";", (m.sources ?? []).Select(Seed)) + "=>" + string.Join(";", (m.targets ?? []).Select(Seed)) + ")";
                            break;
                        default:
                            throw new InvalidDataException("Unrooted signature cache mapper provenance");
                    }
            }

            if (signature.composite != null)
                seed += "/composite:" + (signature.composite.isUnion ? "true" : "false") + "(" + string.Join(";", signature.composite.signatures.Select(s => SignatureSeed(s, stack))) + ")";
            return seed;
        }
        finally
        {
            stack.Remove(signature);
        }
    }

    private StoredValue Inline(params StoredField[] fields) => new()
    {
        Kind = "inline",
        Fields = [..fields]
    };
    private void Cache(string name, bool isNull, int count, List<(string Seed, Func<StoredValue> Render)> rows)
    {
        if (rows.Count != count || rows.Select(row => row.Seed).Distinct(StringComparer.Ordinal).Count() != count)
            throw new InvalidDataException("Cache cardinality/descriptor collision: " + name);
        output.GlobalCaches.Add(new(name, new StoredValue { Kind = "cache", Null = isNull, Fields = [new("count", Value(count))], Values = rows.OrderBy(row => row.Seed, StringComparer.Ordinal).Select(row => row.Render()).ToList() }));
    }

    private void ReadGlobalCaches()
    {
        var known = KnownInstantiationEntities();
        var tuples = checker.VerificationCollectionsTupleTargets();
        Cache("tuple-targets", tuples.IsNull, tuples.Count, (tuples.Entries ?? []).Select(e => (Seed: string.Join(";", (e.Elements ?? []).Select(element => ((uint)element.flags) + "/" + (element.labeledDeclaration == null ? "null" : NodeSeed(element.labeledDeclaration)))) + "/" + (e.Readonly ? "true" : "false"), Render: (Func<StoredValue>)(() => Inline(new("elements", Value(e.Elements, "list")), new("membership", Value(true)), new("readonly", Value(e.Readonly)), new("value", Value(e.Value, "reference")))))).ToList());
        var signatures = checker.VerificationCollectionsCachedSignatures(known.Types);
        Cache("cached-signatures", signatures.IsNull, signatures.Count, (signatures.Entries ?? []).Select(e => (Seed: SignatureSeed(e.Owner) + "/" + e.Family + "(" + string.Join(";", (e.Arguments ?? []).Select(Seed)) + ")", Render: (Func<StoredValue>)(() => Inline(new("arguments", Value(e.Arguments, "list")), new("family", Value(e.Family)), new("membership", Value(true)), new("owner", Value(e.Owner, "reference")), new("value", Value(e.Value, "reference")))))).ToList());
        foreach (var cache in checker.VerificationCollectionsTypeCaches(known.Types, known.Symbols))
        {
            var rows = new List<(string, Func<StoredValue>)>();
            foreach (var e in cache.Entries ?? [])
            {
                string seed;
                var fields = new List<Func<StoredField>>();
                switch (cache.Name)
                {
                    case "cached-types":
                        seed = e.Kind + "/" + Seed(e.Owner);
                        fields.Add(() => new("kind", Value(e.Kind)));
                        fields.Add(() => new("owner", Value(e.Owner, "reference")));
                        break;
                    case "indexed-access":
                        seed = Seed(e.Object) + "/" + Seed(e.Index) + "/" + e.AccessFlags + "/alias:" + (e.AliasSymbol == null ? "null" : binding.ExistingSymbolId(e.AliasSymbol)) + "(" + string.Join(";", (e.AliasArguments ?? []).Select(Seed)) + ")";
                        fields.Add(() => new("accessFlags", Value(e.AccessFlags)));
                        fields.Add(() => new("aliasArguments", Value(e.AliasArguments, "list")));
                        fields.Add(() => new("aliasSymbol", Value(e.AliasSymbol, "symbol")));
                        fields.Add(() => new("index", Value(e.Index, "reference")));
                        fields.Add(() => new("object", Value(e.Object, "reference")));
                        break;
                    case "substitution":
                        seed = Seed(e.Base) + "/" + Seed(e.Constraint);
                        fields.Add(() => new("base", Value(e.Base, "reference")));
                        fields.Add(() => new("constraint", Value(e.Constraint, "reference")));
                        break;
                    case "template-literal":
                        seed = string.Join(";", (e.Texts ?? []).Select(TypeScript.Scanner.ParserValidation.Encoder.Bytes)) + "(" + string.Join(";", (e.Types ?? []).Select(Seed)) + ")";
                        fields.Add(() => new("texts", Value(e.Texts, "list")));
                        fields.Add(() => new("types", Value(e.Types, "list")));
                        break;
                    default:
                        throw new InvalidDataException("Unknown global type cache " + cache.Name);
                }

                rows.Add((seed, () =>
                {
                    var emitted = fields.Select(f => f()).ToList();
                    emitted.Add(new("membership", Value(true)));
                    emitted.Add(new("value", Value(e.Value, "reference")));
                    return new StoredValue
                    {
                        Kind = "inline",
                        Fields = emitted.OrderBy(f => f.Name, StringComparer.Ordinal).ToList()
                    };
                }));
            }

            Cache(cache.Name, cache.IsNull, cache.Count, rows);
        }

        foreach (var cache in checker.VerificationCollectionsReverseMappedCaches(known.Types))
            Cache(cache.Name, cache.IsNull, cache.Count, (cache.Entries ?? []).Select(e => (Seed: Seed(e.Source) + "/" + Seed(e.Target) + "/" + Seed(e.Constraint), Render: (Func<StoredValue>)(() => Inline(new("constraint", Value(e.Constraint, "reference")), new("membership", Value(true)), new("source", Value(e.Source, "reference")), new("target", Value(e.Target, "reference")), new("value", Value(e.Value, "reference")))))).ToList());
        foreach (var cache in checker.VerificationCollectionsRelations(known.Types))
            Cache("relation-" + cache.Name, cache.IsNull, cache.Count, (cache.Entries ?? []).Select(e => (Seed: e.Family + "/" + e.IntersectionState + "/" + Seed(e.Source) + "/" + Seed(e.Target) + "/" + RelationSeed(e.SourceReference) + "/" + RelationSeed(e.TargetReference), Render: (Func<StoredValue>)(() => Inline(new("family", Value(e.Family)), new("intersectionState", Value(e.IntersectionState)), new("membership", Value(true)), new("result", Value(e.Result)), new("source", Value(e.Source, "reference")), new("sourceReference", RelationValue(e.SourceReference)), new("target", Value(e.Target, "reference")), new("targetReference", RelationValue(e.TargetReference)))))).ToList());
        var properties = checker.VerificationCollectionsPropertiesTypes(known.Types);
        Cache("properties-types", properties.IsNull, properties.Count, (properties.Entries ?? []).Select(e => (Seed: Seed(e.Owner) + "/" + e.Include + "/" + (e.IncludeOrigin ? "true" : "false"), Render: (Func<StoredValue>)(() => Inline(new("include", Value(e.Include)), new("includeOrigin", Value(e.IncludeOrigin)), new("membership", Value(true)), new("owner", Value(e.Owner, "reference")), new("value", Value(e.Value, "reference")))))).ToList());
        var iteration = checker.VerificationCollectionsIterationTypes(known.Types);
        Cache("iteration-types", iteration.IsNull, iteration.Count, (iteration.Entries ?? []).Select(e => (Seed: Seed(e.Owner) + "/" + e.Use, Render: (Func<StoredValue>)(() => Inline(new("membership", Value(true)), new("next", Value(e.Next, "reference")), new("owner", Value(e.Owner, "reference")), new("return", Value(e.Return, "reference")), new("use", Value(e.Use)), new("yield", Value(e.Yield, "reference")))))).ToList());
        foreach (var cache in checker.VerificationCollectionsExpressionCaches())
            Cache(cache.Name, cache.IsNull, cache.Count, (cache.Entries ?? []).Select(e => (Seed: NodeSeed(e.Node).ToString(System.Globalization.CultureInfo.InvariantCulture), Render: (Func<StoredValue>)(() => Inline(new("membership", Value(true)), new("node", Value(e.Node, "node")), new("value", Value(e.Value, "reference")))))).ToList());
        var knownNodes = new Dictionary<ulong, Node>();
        foreach (Node node in syntax.RawNodes)
            if (node.id != 0 && !knownNodes.TryAdd(unchecked((ulong)node.id), node))
                throw new InvalidDataException("Duplicate actual node ID");
        var discriminated = checker.VerificationCollectionsDiscriminatedTypes(knownNodes, known.Types);
        Cache("discriminated-contextual-types", discriminated.IsNull, discriminated.Count, (discriminated.Entries ?? []).Select(e => (Seed: NodeSeed(e.Node) + "/" + Seed(e.ContextualType), Render: (Func<StoredValue>)(() => Inline(new("contextualType", Value(e.ContextualType, "reference")), new("membership", Value(true)), new("node", Value(e.Node, "node")), new("value", Value(e.Value, "reference")))))).ToList());
        ReadFlowCaches(known.Types);
    }

    private string RelationSeed(Checker.CollectionsRelationReference? r) => r == null ? "null" : Seed(r.Target) + "(" + string.Join(";", r.Arguments.Select(a => a.Kind == "parameter" ? "parameter:" + a.Index : a.Kind == "type" ? "type:" + Seed(a.Type) : "reference:" + RelationSeed(a.Reference))) + ")";
    private StoredValue RelationValue(Checker.CollectionsRelationReference? r) => r == null ? new()
    {
        Kind = "inline",
        Null = true
    }

    : Inline(new("arguments", new StoredValue { Kind = "list", Values = r.Arguments.Select(a => Inline(new("index", Value(a.Index)), new("kind", Value(a.Kind)), new("reference", RelationValue(a.Reference)), new("type", Value(a.Type, "reference")))).ToList() }), new("target", Value(r.Target, "reference")));
    private void ReadFlowCaches(Dictionary<uint, Type> known)
    {
        string Flow(FlowNode f) => binding.ExistingFlowId(f).ToString(System.Globalization.CultureInfo.InvariantCulture);
        string Token(Checker.CollectionsFlowToken t) => t.Kind == "bytes" ? "bytes:" + TypeScript.Scanner.ParserValidation.Encoder.Bytes(t.Bytes) : t.Owner switch
        {
            Type type => "type:" + Seed(type),
            Symbol symbol => "symbol:" + binding.ExistingSymbolId(symbol),
            Node node => "node:" + NodeSeed(node),
            _ => throw new InvalidDataException("Unknown flow token")};
        StoredValue TokenValue(Checker.CollectionsFlowToken t) => Inline(new("kind", Value(t.Kind)), new("value", t.Kind == "bytes" ? Value(t.Bytes) : Value(t.Owner, t.Kind == "type" ? "reference" : t.Kind)));
        var loops = checker.VerificationCollectionsFlowLoops();
        Cache("flow-loop", loops.IsNull, loops.Count, (loops.Entries ?? []).Select(e => (Seed: Flow(e.Flow) + "/" + string.Join(";", e.Key.Select(Token)), Render: (Func<StoredValue>)(() => Inline(new("flow", Value(e.Flow, "flow")), new("key", new StoredValue { Kind = "list", Values = e.Key.Select(TokenValue).ToList() }), new("membership", Value(true)), new("value", Value(e.Value, "reference")))))).ToList());
        var narrowed = checker.VerificationCollectionsNarrowedTypes();
        Cache("narrowed-types", narrowed.IsNull, narrowed.Count, (narrowed.Entries ?? []).Select(e => (Seed: Seed(e.Type) + "/" + Seed(e.Candidate) + "/" + (e.AssumeTrue ? "true" : "false") + "/" + (e.CheckDerived ? "true" : "false"), Render: (Func<StoredValue>)(() => Inline(new("assumeTrue", Value(e.AssumeTrue)), new("candidate", Value(e.Candidate, "reference")), new("checkDerived", Value(e.CheckDerived)), new("membership", Value(true)), new("type", Value(e.Type, "reference")), new("value", Value(e.Value, "reference")))))).ToList());
        var assignments = checker.VerificationCollectionsAssignmentReducedTypes();
        Cache("assignment-reduced", assignments.IsNull, assignments.Count, (assignments.Entries ?? []).Select(e => (Seed: Seed(e.Declared) + "/" + Seed(e.Assigned), Render: (Func<StoredValue>)(() => Inline(new("assigned", Value(e.Assigned, "reference")), new("declared", Value(e.Declared, "reference")), new("membership", Value(true)), new("value", Value(e.Value, "reference")))))).ToList());
        var reachable = checker.VerificationCollectionsReachableFlows();
        Cache("flow-reachable", reachable.IsNull, reachable.Count, (reachable.Entries ?? []).Select(e => (Seed: Flow(e.Flow), Render: (Func<StoredValue>)(() => Inline(new("flow", Value(e.Flow, "flow")), new("membership", Value(true)), new("value", Value(e.Value)))))).ToList());
        output.GlobalCaches.Add(new("last-flow", Inline(new("flow", Value(reachable.LastFlow, "flow")), new("reachable", Value(reachable.LastReachable)))));
        var lifetime = checker.VerificationCollectionsFlowLifetime();
        if (lifetime.LoopStackCount != 0 || lifetime.ActiveMapperCount != 0 || lifetime.ActiveMapperCacheCount != 0)
            throw new InvalidDataException("Unclosed active flow/mapper lifetime");
        output.GlobalCaches.Add(new("active-lifetimes", Inline(new("activeMapperCacheCount", Value(lifetime.ActiveMapperCacheCount)), new("activeMapperCount", Value(lifetime.ActiveMapperCount)), new("loopStackCount", Value(lifetime.LoopStackCount)), new("loopStackNull", Value(lifetime.LoopStackNull)))));
    }
}
